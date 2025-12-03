using System.IO;

public static class IniParser
{
	public static string configFilePath = "config.ini";

	public static string ip;
	public static string port;
	public static string behaviorName;

	public static string clipMarkerFile;
	public static string creditsFile;
	public static string loyaltyCardDailyFile;
	public static string loyaltyCardSubFile;

	public static string connectionAdress;
	public static string eventSubscriptionAdress;
	public static string broadcasterUserId;
	public static string clientId;
	public static string accessToken;

	public static string happyHourDuration;
	public static string happyHourIntervalMin;
	public static string happyHourIntervalMax;

	public static string rewardPaintOne;
	public static string rewardPaintTen;
	public static string rewardPaintHundread;

	public static string clipMarkerTime;
	public static string clipMarkerText;
	public static string clipMarkerTimeAndText;

	public static string paintOne;
	public static string paintTen;
	public static string paintHundread;
	public static string paintRemoveOne;
	public static string paintRemoveAll;
	public static string paintFillScreenProgressive;
	public static string paintEmptyScreenProgressive;
	public static string phoqueThroughScreen;

	public static string showDeathCounter;
	public static string hideDeathCounter;
	public static string updateDeathCounter;

	public static string showLogs;
	public static string hideLogs;
		
	public static string happyhourForce;
	public static string happyhourOn;
	public static string happyhourOff;

	public static string stampCardDaily;
	public static string stampCardSub;
	public static string fidIn;
	public static string fidOut;

	public static string showCredits;
	public static string hideCredits;
	public static string hypeTrain;
	public static string raid;
	public static string sub;
	public static string subGift;
	public static string bits;
	public static string follow;
	public static string first;
	public static string chatMessage;
	public static string[] chatMessageFilteredNames;
	public static string banDef;
	public static string resetCredits;

	public static string followHand;
	public static string unfollowHand;
	public static string handPosition;

	public static string exit;

	public static void ReadConfig()
	{
		string[] lines = File.ReadAllLines("config.ini");
		foreach (var line in lines)
		{
			if (line.Contains("="))
			{
				string[] parts = line.Split('=');
				switch (parts[0])
				{
					case "ip":
						ip = parts[1];
						break;
					case "port":
						port = parts[1];
						break;
					case "behaviorName":
						behaviorName = parts[1];
						break;

					case "clipMarkerFile":
						clipMarkerFile = parts[1];
						break;
					case "creditsFile":
						creditsFile = parts[1];
						break;
					case "loyaltyCardDailyFile":
						loyaltyCardDailyFile = parts[1];
						break;
					case "loyaltyCardSubFile":
						loyaltyCardSubFile = parts[1];
						break;

					case "connectionAdress":
						connectionAdress = parts[1];
						break;
					case "eventSubscriptionAdress":
						eventSubscriptionAdress = parts[1];
						break;
					case "broadcasterUserId":
						broadcasterUserId = parts[1];
						break;
					case "clientId":
						clientId = parts[1];
						break;
					case "accessToken":
						accessToken = parts[1];
						break;

					case "happyHourDuration":
						happyHourDuration = parts[1];
						break;
					case "happyHourIntervalMin":
						happyHourIntervalMin = parts[1];
						break;
					case "happyHourIntervalMax":
						happyHourIntervalMax = parts[1];
						break;

					case "rewardPaintOne":
						rewardPaintOne = parts[1];
						break;
					case "rewardPaintTen":
						rewardPaintTen = parts[1];
						break;
					case "rewardPaintHundread":
						rewardPaintHundread = parts[1];
						break;

					case "clipMarkerTime":
						clipMarkerTime = parts[1];
						break;
					case "clipMarkerText":
						clipMarkerText = parts[1];
						break;
					case "clipMarkerTimeAndText":
						clipMarkerTimeAndText = parts[1];
						break;

					case "paintOne":
						paintOne = parts[1];
						break;
					case "paintTen":
						paintTen = parts[1];
						break;
					case "paintHundread":
						paintHundread = parts[1];
						break;
					case "paintRemoveOne":
						paintRemoveOne = parts[1];
						break;
					case "paintRemoveAll":
						paintRemoveAll = parts[1];
						break;
					case "paintFillScreenProgressive":
						paintFillScreenProgressive = parts[1];
						break;
					case "paintEmptyScreenProgressive":
						paintEmptyScreenProgressive = parts[1];
						break;
					case "phoqueThroughScreen":
						phoqueThroughScreen = parts[1];
						break;

					case "showDeathCounter":
						showDeathCounter = parts[1];
						break;
					case "hideDeathCounter":
						hideDeathCounter = parts[1];
						break;
					case "updateDeathCounter":
						updateDeathCounter = parts[1];
						break;

					case "showLogs":
						showLogs = parts[1];
						break;
					case "hideLogs":
						hideLogs = parts[1];
						break;

					case "happyhourForce":
						happyhourForce = parts[1];
						break;
					case "happyhourOn":
						happyhourOn = parts[1];
						break;
					case "happyhourOff":
						happyhourOff = parts[1];
						break;

					case "stampCardDaily":
						stampCardDaily = parts[1];
						break;
					case "stampCardSub":
						stampCardSub = parts[1];
						break;
					case "fidIn":
						fidIn = parts[1];
						break;
					case "fidOut":
						fidOut = parts[1];
						break;

					case "showCredits":
						showCredits = parts[1];
						break;
					case "hideCredits":
						hideCredits = parts[1];
						break;
					case "hypeTrain":
						hypeTrain = parts[1];
						break;
					case "raid":
						raid = parts[1];
						break;
					case "sub":
						sub = parts[1];
						break;
					case "subGift":
						subGift = parts[1];
						break;
					case "bits":
						bits = parts[1];
						break;
					case "follow":
						follow = parts[1];
						break;
					case "first":
						first = parts[1];
						break;
					case "chatMessage":
						chatMessage = parts[1];
						break;
					case "chatMessageFilteredNames":
						chatMessageFilteredNames = parts[1].Split(',');
						break;
					case "banDef":
						banDef = parts[1];
						break;
					case "resetCredits":
						resetCredits = parts[1];
						break;

					case "followHand":
						followHand = parts[1];
						break;
					case "unfollowHand":
						unfollowHand = parts[1];
						break;
					case "handPosition":
						handPosition = parts[1];
						break;

					case "exit":
						exit = parts[1];
						break;

					default:
						break;
				}
			}
		}
	}
}