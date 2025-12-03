using Python.Runtime;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class OverlayWindow : MonoBehaviour
{
	#region DLL IMPORTS
	[DllImport("user32.dll")]
	static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll", SetLastError = true)]
	static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
	#endregion

	TwitchApiWebSocket twitch = new TwitchApiWebSocket();
	LocalWebSocket local = new LocalWebSocket();

	#region MONOBEHAVIOUR
	void Start()
	{
		FetchSettings();
		twitch.SetupTwitchWebSocket();
		local.SetupLocalWebSocket();
		WindowFocusManager.GetUnityWindowHandle();
	}

	private void OnDestroy()
	{
		local.KillWS();
		//twitch.ws.Close();
	}

#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
	public static class PythonAutoShutdown
	{
		static PythonAutoShutdown()
		{
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
			{
				if (Python.Runtime.PythonEngine.IsInitialized)
				{
					UnityEngine.Debug.Log("[Python] Shutting down before assembly reload.");
					Python.Runtime.PythonEngine.Shutdown();
				}
			};
		}
	}
#endif
	#endregion

	private void FetchSettings()
	{
		IniParser.ReadConfig();
	}
}
