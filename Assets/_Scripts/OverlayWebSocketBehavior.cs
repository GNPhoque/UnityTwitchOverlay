using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class OverlayWebSocketBehavior : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		Logger.Log($"Local WS received: {e.Data}");

		UnityMainThreadDispatcher.instance.Enqueue(() =>
		{
			try
			{
				var json = JsonConvert.DeserializeObject<dynamic>(e.Data);
				string command = json.command;

				#region CLIP MARKER
				if (command == IniParser.clipMarkerTime)
				{
					WebSocketInteractions.instance.CreateClipMarkerTime();
				}
				else if (command == IniParser.clipMarkerText)
				{
					WebSocketInteractions.instance.CreateClipMarkerText();
				}
				else if (command == IniParser.clipMarkerTimeAndText)
				{
					WebSocketInteractions.instance.CreateClipMarkerTimeAndText();
				}
				#endregion

				#region PAINT
				else if (command == IniParser.paintOne)
				{
					string avatar = json.avatar;
					WebSocketInteractions.instance.DrawSplash(avatarUrl: avatar);
				}
				else if (command == IniParser.paintTen)
				{
					for (int i = 0; i < 10; i++)
					{
						WebSocketInteractions.instance.DrawSplash();
					}
				}
				else if (command == IniParser.paintHundread)
				{
					for (int i = 0; i < 100; i++)
					{
						WebSocketInteractions.instance.DrawSplash();
					}
				}
				else if (command == "paintAt")
				{
					string user = json.user;
				
					WebSocketInteractions.instance.CreditsAddHappyHourPainter(user);
					WebSocketInteractions.instance.DrawSplash((float)json.x, 100 - (float)json.y, 10);
				}
				else if (command == IniParser.paintRemoveOne)
				{
					WebSocketInteractions.instance.RemoveSplash();
				}
				else if (command == IniParser.paintRemoveAll)
				{
					WebSocketInteractions.instance.RemoveAllSplash();
				}
				else if (command == IniParser.paintFillScreenProgressive)
				{
					WebSocketInteractions.instance.StartCoroutine(WebSocketInteractions.instance.FillScreen());
				}
				else if (command == IniParser.paintEmptyScreenProgressive)
				{
					WebSocketInteractions.instance.StartCoroutine(WebSocketInteractions.instance.ClearScreen());
				}
				else if (command == IniParser.phoqueThroughScreen)
				{
					WebSocketInteractions.instance.PhoqueThroughScreen();
				}
				else if (command == IniParser.lurk)
				{
					string user = json.user;

					WebSocketInteractions.instance.Lurk(user);
				}
				else if (command == IniParser.dodo)
				{
					string user = json.user;

					WebSocketInteractions.instance.Dodo(user);
				}
				#endregion

				#region DEATH COUNTER
				else if (command == IniParser.showDeathCounter)
				{
					WebSocketInteractions.instance.ShowText();
				}
				else if (command == IniParser.hideDeathCounter)
				{
					WebSocketInteractions.instance.HideText();
				}
				else if (command == IniParser.updateDeathCounter)
				{
					string value = json.value;
					int number = -1;

					if (!string.IsNullOrEmpty(value) && int.TryParse(value, out number))
					{
						WebSocketInteractions.instance.UpdateText($"Morts : {value}");
						if (WebSocketInteractions.instance.IsDeathCounterVisible())
						{
							if (number % 100 == 0)
							{
								for (int i = 0; i < 100; i++)
								{
									WebSocketInteractions.instance.DrawSplash();
								}
							}
							else if (number % 10 == 0)
							{
								for (int i = 0; i < 10; i++)
								{
									WebSocketInteractions.instance.DrawSplash();
								}
							}
							else
							{
								WebSocketInteractions.instance.DrawSplash();
							}
						}
					}
				}
				#endregion

				#region LOGS
				else if (command == IniParser.showLogs)
				{
					WebSocketInteractions.instance.ShowLogs();
				}
				else if (command == IniParser.hideLogs)
				{
					WebSocketInteractions.instance.HideLogs();
				}
				else if (command == "ERROR")
				{
					throw new Exception("ERROR TEST");
				}
				#endregion

				#region HAPPY HOUR
				else if (command == IniParser.happyhourForce)
				{
					WebSocketInteractions.instance.EnableHappyHour(true);
				}
				else if (command == IniParser.happyhourOn)
				{
					WebSocketInteractions.instance.EnableHappyHour();
				}
				else if (command == IniParser.happyhourOff)
				{
					WebSocketInteractions.instance.DisableHappyHour();
				}
				#endregion

				#region STAMP
				else if (command == IniParser.stampCardDaily)
				{
					string user = json.user;
					string avatar = json.avatar;
					int count = (int)json.viewer_count;

					WebSocketInteractions.instance.SpawnHellos(count);
					WebSocketInteractions.instance.StampCard(user, avatar, ELoyaltyCardType.Daily);
				}
				else if (command == IniParser.stampCardSub)
				{
					string user = json.user;
					string avatar = json.avatar;
					int count = (int)json.count;

					WebSocketInteractions.instance.StampCard(user, avatar, ELoyaltyCardType.Sub, count);
				}
				else if (command == IniParser.fidIn)
				{
					string user = json.user;

					if(user == "WizeBot")
					{
						return;
					}

					WebSocketInteractions.instance.SendFidDetails(user);
				}
				#endregion

				#region CREDITS
				else if (command == IniParser.showCredits)
				{
					WebSocketInteractions.instance.ShowCredits();
				}

				else if (command == IniParser.hideCredits)
				{
					WebSocketInteractions.instance.HideCredits();
				}

				else if (command == IniParser.hypeTrain)
				{
					int level = (int)json.level;

					WebSocketInteractions.instance.CreditsAddHypeTrain(level);
				}

				else if (command == IniParser.raid)
				{
					string user = json.user;
					int value = (int)json.value;

					WebSocketInteractions.instance.CreditsAddRaid(user, value);
					for (int i = 0; i < (int)json.value; i++)
					{
						WebSocketInteractions.instance.DrawSplash();
					}
				}

				else if (command == IniParser.sub)
				{
					string tier = json.tier;
					string user = json.user;

					WebSocketInteractions.instance.CreditsAddSub(user, tier);
				}

				else if (command == IniParser.subGift)
				{
					string user = json.user;
					int count = (int)json.count;

					WebSocketInteractions.instance.CreditsAddSubGift(user, count);
				}

				else if (command == IniParser.bits)
				{
					string user = json.user;
					int count = (int)json.count;

					WebSocketInteractions.instance.CreditsAddBits(user, count);
				}

				else if (command == IniParser.follow)
				{
					string user = json.user;

					WebSocketInteractions.instance.CreditsAddFollow(user);
				}

				else if (command == IniParser.first)
				{
					string user = json.user;

					WebSocketInteractions.instance.CreditsAddFirst(user);
				}

				else if (command == IniParser.chatMessage)
				{
					string user = json.user;

					if (IniParser.chatMessageFilteredNames.Contains(user.ToLower()))
					{
						return;
					}

					WebSocketInteractions.instance.CreditsAddMessage(user);
				}

				else if (command == IniParser.banDef)
				{
					string user = json.user;

					WebSocketInteractions.instance.CreditsRemoveMessage(user);
				}

				else if (command == IniParser.resetCredits)
				{
					WebSocketInteractions.instance.CreditsClearTextFile();
					WebSocketInteractions.instance.ResetStampRedeemedUsers();
				}
				#endregion

				#region HAND TRACKING
				else if (command == IniParser.followHand)
				{
					WebSocketInteractions.instance.FollowHand();
					WebSocketInteractions.instance.MoveToHandPosition(1f, 1f);
				}
				else if (command == IniParser.unfollowHand)
				{
					WebSocketInteractions.instance.UnfollowHand();
				}
				else if (command == IniParser.handPosition)
				{
					float x = (float)json.value.x;
					float y = (float)json.value.y;

					WebSocketInteractions.instance.MoveToHandPosition(x, y);
				}
				#endregion

				#region EXIT
				else if (command == IniParser.exit)
				{
					WebSocketInteractions.instance.Shutdown();
				}
				#endregion

				else if(command == "hanabi")
				{
					WebSocketInteractions.instance.Hanabi(new Vector2(-600, -700), 1f, 8);
					WebSocketInteractions.instance.Hanabi(new Vector2(600, -700), 1f, 8);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error parsing local WS: {ex.Message}");
				Logger.LogError($"Error parsing local WS: {ex.StackTrace}");
			}
		});
	}

	protected override void OnOpen()
	{
		Logger.Log($"[WS OPEN] Client connected (ID : {ID}");
		base.OnOpen();
	}

	protected override void OnClose(CloseEventArgs e)
	{
		Logger.Log($"[WS CLOSE] Code={e.Code}, Reason={e.Reason}");
		base.OnClose(e);
	}

	protected override void OnError(ErrorEventArgs e)
	{
		Logger.LogError($"[WS ERROR] {e.Message}\n{e.Exception}");
		base.OnError(e);
	}
}