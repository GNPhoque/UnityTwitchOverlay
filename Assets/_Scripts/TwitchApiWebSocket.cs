using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

public class TwitchApiWebSocket
{
	public WebSocket ws;
	private bool eventsSubscribed;

	public void SetupTwitchWebSocket()
	{
		Logger.Log("Starting Twitch WS Connection");
		ws = new WebSocket(IniParser.connectionAdress);
		//ws.Log.Level = LogLevel.Trace;
		//ws.Log.Output = (logData, file) => Logger.Log(logData.Message);
		ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
		//WebSocket ws = new WebSocket("ws://127.0.0.1:8080/ws"); // local tests with twitch CLI
		ws.OnOpen += Ws_OnOpen;
		ws.OnMessage += Ws_OnMessage;
		Logger.Log($"Connecting to {IniParser.connectionAdress}");
		ws.Connect();

	}

	private void Ws_OnOpen(object sender, EventArgs e)
	{
		Logger.Log("Twich Connection OnOpen");
	}

	private async void Ws_OnMessage(object sender, MessageEventArgs e)
	{
		UnityMainThreadDispatcher.instance.Enqueue(async () =>
		{
			if (e.Data.StartsWith("{{"))
			{
				e.Data.Remove(0, 1);
				e.Data.Remove(e.Data.Length - 2, 1);
			}

			var json = JsonConvert.DeserializeObject<dynamic>(e.Data);

			if (json.metadata.message_type == "session_keepalive")
			{
				Logger.Log($"{DateTime.Now} Received Keep Alive");
				return;
			}
			else if (json.metadata.message_type == "session_reconnect")
			{
				string url = json.payload.session.reconnect_url;
				WebSocket ws = new WebSocket(url);
				ws.OnOpen += Ws_OnOpen;
				ws.OnMessage += Ws_OnMessage;
				ws.Connect();
				return;
			}
			else if (json.metadata.message_type == "session_welcome")
			{
				string sessionId = json.payload.session.id;
				if (!eventsSubscribed)
				{
					eventsSubscribed = true;
					await SubscribeChannelPointsAsync(sessionId);
					//await SubscribeRaidsAsync(sessionId);
				}
				return;
			}
			else if (json.payload.@event != null && json.metadata.subscription_type != null)
			{
				if (json.metadata.subscription_type == "channel.channel_points_custom_reward_redemption.add")
				{
					if (json.payload.@event.reward.title == IniParser.rewardPaintOne)
					{
						Logger.Log($"{DateTime.Now} Received Une tâche");
						WebSocketInteractions.instance.DrawSplash();
						return;
					}
					else if (json.payload.@event.reward.title == IniParser.rewardPaintTen)
					{
						Logger.Log($"{DateTime.Now} Received 10 Tâches");
						for (int i = 0; i < 10; i++)
						{
							WebSocketInteractions.instance.DrawSplash();
						}
						return;
					}
					else if (json.payload.@event.reward.title == IniParser.rewardPaintHundread)
					{
						Logger.Log($"{DateTime.Now} Received 100 Tâches");
						for (int i = 0; i < 100; i++)
						{
							WebSocketInteractions.instance.DrawSplash();
						}
						return;
					}
				}
				else if (json.metadata.subscription_type == "channel.raid")
				{

				}
			}
			Logger.Log(e.Data);
			Logger.Log(json.payload.ToString());
		});
	}

	private async Task SubscribeChannelPointsAsync(string sessionId)
	{
		var payload = new
		{
			type = "channel.channel_points_custom_reward_redemption.add",
			version = "1",
			condition = new
			{
				broadcaster_user_id = IniParser.broadcasterUserId
			},
			transport = new
			{
				method = "websocket",
				session_id = sessionId
			}
		};

		string json = JsonConvert.SerializeObject(payload);

		using (HttpClient client = new HttpClient())
		{
			HttpRequestMessage message = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(IniParser.eventSubscriptionAdress),
				//RequestUri = new Uri("http://127.0.0.1:8080/eventsub/subscriptions"), // local tests with twitch CLI
				Headers =
						{
							{ "Authorization", $"Bearer {IniParser.accessToken}" },
							{ "Client-Id", IniParser.clientId }
						},
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			var response = await client.SendAsync(message);
			string responseBody = await response.Content.ReadAsStringAsync();

			Logger.Log($"SUBSCRIPTION : {response.StatusCode}");
			Logger.Log(responseBody);
		}
	}

	private async Task SubscribeRaidsAsync(string sessionId)
	{
		var payload = new
		{
			type = "channel.raid",
			version = "1",
			condition = new
			{
				to_broadcaster_user_id = IniParser.broadcasterUserId
			},
			transport = new
			{
				method = "websocket",
				session_id = sessionId
			}
		};

		string json = JsonConvert.SerializeObject(payload);

		using (HttpClient client = new HttpClient())
		{
			HttpRequestMessage message = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(IniParser.eventSubscriptionAdress),
				//RequestUri = new Uri("http://127.0.0.1:8080/eventsub/subscriptions"), // local tests with twitch CLI
				Headers =
						{
							{ "Authorization", $"Bearer {IniParser.accessToken}" },
							{ "Client-Id", IniParser.clientId }
						},
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};

			var response = await client.SendAsync(message);
			string responseBody = await response.Content.ReadAsStringAsync();

			Logger.Log($"SUBSCRIPTION : {response.StatusCode}");
			Logger.Log(responseBody);
		}
	}
}
