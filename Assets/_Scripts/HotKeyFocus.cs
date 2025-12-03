using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class HotKeyFocus : MonoBehaviour
{
	// Constantes Win32
	[DllImport("user32.dll")]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll")]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	private const int HOTKEY_ID = 0x0001;
	private const uint MOD_CONTROL = 0x0002;
	private const uint MOD_ALT = 0x0001;
	private const uint VK_M = 0x4D; // touche M
	private const uint WM_HOTKEY = 0x0312;
	private const uint PM_REMOVE = 0x0001;

	private IntPtr hwnd;

	void Start()
	{
		hwnd = GetActiveWindow();
		// Ctrl + Alt + M
		RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_M);
		Logger.Log("HotKey registered (Ctrl+Alt+M)");
	}

	void OnDestroy()
	{
		UnregisterHotKey(hwnd, HOTKEY_ID);
	}

	void Update()
	{
		// Lis tous les messages WM_HOTKEY disponibles
		while (PeekMessage(out MSG msg, IntPtr.Zero, WM_HOTKEY, WM_HOTKEY, PM_REMOVE))
		{
			if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
			{
				Debug.Log("Hotkey triggered!");
				FocusUnityWindow();
			}
		}
	}

	private void FocusUnityWindow()
	{
		// Ici Windows considère que c'est déclenché par une action utilisateur
		SetForegroundWindow(hwnd);
		Logger.Log("Focus forcé sur Unity");
	}

	// --- interop PeekMessage
	[DllImport("user32.dll")]
	private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd,
										   uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

	[StructLayout(LayoutKind.Sequential)]
	private struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public IntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public POINT pt;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT { public int x, y; }
}