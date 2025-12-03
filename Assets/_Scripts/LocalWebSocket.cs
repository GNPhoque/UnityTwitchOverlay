using UnityEngine;
using WebSocketSharp.Server;

public class LocalWebSocket
{
	public static WebSocketServer wssv;
	public void SetupLocalWebSocket()
	{
		wssv = new WebSocketServer($"{IniParser.ip}:{IniParser.port}");
		wssv.AddWebSocketService<OverlayWebSocketBehavior>($"/{IniParser.behaviorName}");
		wssv.Start();
		Logger.Log($"Local WebSocket server started at {IniParser.ip}:{IniParser.port}/{IniParser.behaviorName}");
	}

	public void KillWS()
	{
		wssv?.Stop();
	}
}
