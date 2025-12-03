using OpenCvSharp;
using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class HandTrackingPython
{
	VideoCapture capture;
	Mat frame = new Mat();
	PyObject ht;
	bool disposed;

	public bool processWebcam;

	public HandTrackingPython()
	{
		try
		{
			if (!PythonEngine.IsInitialized)
			{
				Runtime.PythonDLL = @"C:\Python312\python312.dll";
				PythonEngine.Initialize();
				PythonEngine.PythonHome = @"C:\Python312";
				PythonEngine.PythonPath = PythonEngine.PythonHome + @"\Lib;" + PythonEngine.PythonHome + @"\Lib\site-packages";


				using (Py.GIL())
				{
					dynamic sys = Py.Import("sys");

					// Chemin vers ton projet Unity (au-dessus de Assets/)
					string projectRoot = Environment.CurrentDirectory;
					sys.path.append(projectRoot);

					// Optionnel : log pour vérifier
					//foreach (var p in sys.path)
					//	Logger.Log($"Python sys.path entry: {p}");
				}

				PythonEngine.BeginAllowThreads(); 
			}

			capture = new VideoCapture(2);
			if (capture.IsOpened())
			{
				//Console.WriteLine(videoCapture.GetBackendName());
				Logger.Log($"Camera disponible");
				Logger.Log(capture.GetBackendName());

			}
			else
			{
				Logger.Log($"Camera non disponible");
				return;
			}

			WebSocketInteractions.instance.StartCoroutine(CaptureLoop());
		}
		catch (Exception e)
		{
			Logger.LogError(e.Message);
			Logger.LogError(e.StackTrace);
		}
	}

	public void Dispose()
	{
		if (disposed) return;
		disposed = true;

		if (PythonEngine.IsInitialized)
		{
			try
			{
				PythonEngine.Shutdown();// ferme le runtime proprement
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
				Logger.LogError(e.StackTrace);
				throw;
			}
		}
	}

	private IEnumerator CaptureLoop()
	{
		var py = Py.GIL();
		dynamic np = Py.Import("numpy");
		dynamic cv2 = Py.Import("cv2");
		ht = Py.Import("handTracking"); // Remplace par le nom de ton script Python

		while (true)
		{
			yield return null;

			if (!processWebcam)
			{
				continue;
			}

			capture.Read(frame);

			List<(float x, float y)> positions = GetHandLandmarks(frame, np, cv2);

			//Console.WriteLine($"Fetched {positions.Count}positions this frame");
			//foreach (var item in positions)
			//{
			//	Console.WriteLine($"{item.x}, {item.y}");
			//}

			if (positions.Count > 0)
			{
				UnityMainThreadDispatcher.instance.Enqueue(() => WebSocketInteractions.instance.MoveToHandPosition(positions.First().x, positions.First().y));
			}
		}
	}

	public List<(float x, float y)> GetHandLandmarks(Mat frame, dynamic np, dynamic cv2)
	{
		try
		{
			Mat rgbFrame = new Mat();
			Cv2.CvtColor(frame, rgbFrame, ColorConversionCodes.BGR2RGB); // MediaPipe attend du RGB

			// Copier les données brutes
			byte[] frameData = new byte[rgbFrame.Rows * rgbFrame.Cols * rgbFrame.ElemSize()];
			Marshal.Copy(rgbFrame.Data, frameData, 0, frameData.Length);

			int width = frame.Width;
			int height = frame.Height;

			dynamic handTracking = ht;

			// Convertir les données de l'image en un tableau numpy
			dynamic npframe = np.frombuffer(frameData, np.uint8).reshape(height, width, 3);
			dynamic landmarks = handTracking.get_hand_landmarks(npframe);

			List<(float x, float y)> result = new List<(float x, float y)>();
			if (landmarks != null)
			{
				foreach (var landmark in landmarks)
				{
					result.Add((landmark[0], landmark[1]));
				}
			}
			return result;
		}
		catch (Exception e)
		{
			Logger.LogError(e.Message);
			Logger.LogError(e.StackTrace);
			return null;
		}
	}
}
