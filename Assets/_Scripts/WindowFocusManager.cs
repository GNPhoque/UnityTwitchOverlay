using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WindowFocusManager
{
	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr SetActiveWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern bool AllowSetForegroundWindow(int dwProcessId);

	[DllImport("user32.dll", SetLastError = true)]
	static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
	[DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
	[DllImport("user32.dll")] static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
	[DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern IntPtr SetFocus(IntPtr hWnd);

	// Constantes Win32
	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_TRANSPARENT = 0x00000020;

	private static int originalStyle;
	private static IntPtr unityWindow;
	private static uint unityWindowPID;
	private static IntPtr previousWindow;
	private static uint previousWindowPID;
	const int SW_SHOW = 5;
	const int SW_RESTORE = 9;

	public static void GetUnityWindowHandle()
	{
		unityWindow = GetActiveWindow();
		GetWindowThreadProcessId(unityWindow, out unityWindowPID);
		AllowSetForegroundWindow((int)unityWindowPID);
		originalStyle = GetWindowLong(unityWindow, GWL_EXSTYLE);
	}

	public static void FocusForKeyboard()
	{
		DisableClickThrough();
		// Récupère le handle de la fenêtre Unity en cours d’exécution
		IntPtr hwnd = GetActiveWindow();
		if (hwnd == IntPtr.Zero)
		{
			Debug.LogWarning("Impossible de trouver la fenêtre Unity");
			return;
		}

		SetFocus(hwnd);   // Donne le focus clavier à Unity
		Debug.Log("Focus clavier donné à la fenêtre Unity");
	}

	public static void FocusUnityWindowGPT()
	{
		DisableClickThrough();

		unityWindow = GetActiveWindow();
		previousWindow = GetForegroundWindow();

		if (unityWindow == previousWindow)
			return; // déjà au premier plan

		uint fgThread;
		GetWindowThreadProcessId(previousWindow, out fgThread);
		uint appThread = GetCurrentThreadId();

		// Attacher le thread du premier plan pour contourner la règle
		AttachThreadInput(fgThread, appThread, true);
		ShowWindow(unityWindow, SW_SHOW);
		SetForegroundWindow(unityWindow);
		AttachThreadInput(fgThread, appThread, false);

		Debug.Log("Focus demandé sur la fenêtre Unity");

		RestoreOriginalStyle();
	}

	// Sauvegarde la fenêtre active et met le focus sur la fenêtre Unity
	public static void FocusUnityWindow()
	{
		DisableClickThrough();

		previousWindow = GetForegroundWindow();
		GetWindowThreadProcessId(previousWindow, out previousWindowPID);
		AllowSetForegroundWindow((int)previousWindowPID);

		Logger.Log($"PREVIOUS : {previousWindow}, PREVIOUSPID {previousWindowPID}, UNITY : {unityWindow}, UNITYPID : {unityWindowPID}");

		// Handle de la fenêtre Unity

		if (unityWindow != IntPtr.Zero)
		{
			SetForegroundWindow(unityWindow);
			SetActiveWindow(unityWindow);
			ShowWindow(unityWindow, SW_RESTORE);
			SetFocus(unityWindow);
			Logger.Log("Focus donné à l'overlay Unity");
		}
		else
		{
			Logger.LogError("Impossible d'obtenir la fenêtre Unity");
		}

		RestoreOriginalStyle();
	}

	/// <summary>
	/// Retire temporairement le WS_EX_TRANSPARENT pour que la fenêtre puisse recevoir le focus ou des clics
	/// </summary>
	public static void DisableClickThrough()
	{
		int style = GetWindowLong(unityWindow, GWL_EXSTYLE);
		style &= ~WS_EX_TRANSPARENT; // retirer le flag
		SetWindowLong(unityWindow, GWL_EXSTYLE, style);

		Debug.Log("Click-through désactivé : la fenêtre Unity peut recevoir le focus");
	}

	/// <summary>
	/// Restaure le style original si nécessaire
	/// </summary>
	public static void RestoreOriginalStyle()
	{
		SetWindowLong(unityWindow, GWL_EXSTYLE, originalStyle);
		Debug.Log("Style original restauré");
	}

	// Restaure le focus à la fenêtre précédente
	public static void RestorePreviousFocus()
	{
		if (previousWindow != IntPtr.Zero)
		{
			SetForegroundWindow(previousWindow);
			Logger.Log("Focus rendu à la fenêtre précédente");
		}
		else
		{
			Logger.LogError("Aucune fenêtre précédente connue");
		}
	}
}