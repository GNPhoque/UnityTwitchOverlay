using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class Database : MonoBehaviour
{
	[SerializeField]
	private Sprite defaultUserPP;

	private FirebaseFirestore storage;

	public static Database instance;

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
	}

	private void Start()
	{
		storage = FirebaseFirestore.DefaultInstance;
	}

	public async Task<LoyaltyCardUser> GetUser(string username, ELoyaltyCardType cardType)
	{
		LoyaltyCardUser user = null;
		try
		{
			DocumentReference reference = FirebaseFirestore.DefaultInstance.Document($"/LoyaltyCards/{(cardType == ELoyaltyCardType.Daily ? "daily" : "subs")}/users/{username.ToLower()}");
			Task<DocumentSnapshot> snapTask = reference.GetSnapshotAsync();
			await snapTask;

			DocumentSnapshot doc = snapTask.Result;
			if (!snapTask.IsCompletedSuccessfully || doc.Exists == false)
			{
				reference = FirebaseFirestore.DefaultInstance.Document($"/LoyaltyCards/{(cardType == ELoyaltyCardType.Daily ? "daily" : "subs")}/users/{username.ToLower()}");
				snapTask = reference.GetSnapshotAsync();
				await snapTask;

				doc = snapTask.Result;
				if (!snapTask.IsCompletedSuccessfully || doc.Exists == false)
				{
					Logger.Log($"User data not found on DB, creating user {username.ToLower()}");
					await CreateNewUser(username.ToLower(), cardType);
					reference = FirebaseFirestore.DefaultInstance.Document($"/LoyaltyCards/{(cardType == ELoyaltyCardType.Daily ? "daily" : "subs")}/users/{username.ToLower()}");
					snapTask = reference.GetSnapshotAsync();
					await snapTask;
					doc = snapTask.Result;
				}
				else
				{
					Logger.Log("User (lowercase) data fetched from DB");
				}
			}
			else
			{
				Logger.Log("User data fetched from DB");
			}

			user = new LoyaltyCardUser();
			user.username = doc.Id.ToLower();
			user.totalStamps = doc.GetValue<int>("totalStamps");
			user.completedCards = doc.GetValue<int>("completedCards");
			user.currentCardStamps = doc.GetValue<int>("currentCardStamps");
			user.stamps = doc.GetValue<Stamp[]>("stamps");

			Logger.Log(user.ToString());
		}
		catch (Exception e)
		{
			Logger.LogError(e.Message);
		}

		return user;
	}

	public async Task CreateNewUser(string username, ELoyaltyCardType cardType)
	{
		try
		{
			DocumentReference doc = storage.Collection($"LoyaltyCards/{(cardType == ELoyaltyCardType.Daily? "daily" : "subs")}/users").Document(username.ToLower());
			Dictionary<string, object> value = new Dictionary<string, object>
			{
				{"totalStamps", 0 },
				{"completedCards", 0 },
				{"currentCardStamps", 0 },
				{"stamps", new Stamp[]
					{
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
						new Stamp(0,0,0),
					} 
				}
			};
			await doc.SetAsync(value);
			Logger.Log($"New user created {username.ToLower()}");
		}
		catch (Exception e)
		{
			Logger.LogError($"Error updating user {username.ToLower()}");
			Logger.LogError(e.Message);
		}
	}

	public async Task<Sprite> GetUserAvatarFromTwitchat(string username, string url)
	{
		if (url == "{AVATAR}" || string.IsNullOrEmpty(url))
		{
			Logger.Log($"Invalid url received from Twitchat, fetching user picture from DB. url = {url}");
			return await GetUserAvatarFromDB(username.ToLower());
		}

		using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
		{
			try
			{
				for (int i = 0; i < 3; i++)
				{
					await uwr.SendWebRequest();

					if (uwr.result != UnityWebRequest.Result.Success)
					{
						Logger.Log($"Error fetching avatar for {username.ToLower()}");
						Logger.Log($"avatar url :{url}");
						Logger.Log(uwr.error);
					}
					else
					{
						await UpdateUserAvatarUrl(username.ToLower(), url);

						var texture = DownloadHandlerTexture.GetContent(uwr);
						Logger.Log("User picture successfully fetched from Twitchat URL");
						return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
					}
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
			}

			return await GetUserAvatarFromDB(username.ToLower());
		}
	}

	public async Task<Sprite> GetUserAvatarFromDB(string username)
	{
		string url = "";

		await storage.Document($"Users/{username.ToLower()}").GetSnapshotAsync().ContinueWithOnMainThread((x) =>
		{
			if (x.IsCompletedSuccessfully)
			{
				x.Result.TryGetValue<string>("avatarUrl", out url);
			}
		});

		if (string.IsNullOrEmpty(url))
		{
			Logger.LogError("Could not get profile picture from empty url");
			return defaultUserPP;
		}

		using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
		{
			try
			{
				for (int i = 0; i < 3; i++)
				{
					await uwr.SendWebRequest();

					if (uwr.result != UnityWebRequest.Result.Success)
					{
						Logger.LogError($"Error fetching avatar for {username.ToLower()}");
						Logger.LogError($"avatar url :{url}");
						Logger.LogError(uwr.error);
					}
					else
					{
						var texture = DownloadHandlerTexture.GetContent(uwr);
						Logger.Log("User picture successfully fetched from DB");
						return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
					}
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
			}

			//Get from DB
			Logger.Log($"Could not fetch user picture from DB with url {url}");
			return defaultUserPP;
		}
	}

	public async Task UpdateUserAvatarUrl(string username, string url)
	{
		try
		{
			DocumentReference doc = storage.Collection("Users").Document(username.ToLower());
			Dictionary<string, string> value = new Dictionary<string, string>
			{
				{ "avatarUrl", url }
			};
			await doc.SetAsync(value);
			Logger.Log($"Updated avatar url for {username.ToLower()} : {url}");
		}
		catch (Exception e)
		{
			Logger.LogError($"Error updating user {username.ToLower()}");
			Logger.LogError(e.Message);
		}
	}

	public void ResetRedeemedUsers()
	{
		storage.Collection("LoyaltyCards/daily/redeemed").GetSnapshotAsync().ContinueWithOnMainThread(x =>
		{
			foreach (var item in x.Result.Documents)
			{
				item.Reference.DeleteAsync();
			}
		});
	}

	public async Task<List<string>> GetAllDailyRedeemUsers()
	{
		List<string> ret = new List<string>();
		await storage.Collection($"LoyaltyCards/daily/redeeemed").GetSnapshotAsync().ContinueWithOnMainThread(x =>
		{
			ret = x.Result.Select(y => y.Id).ToList();
		});

		return ret;
	}

	public async Task<bool> HasUserAlreadyStampedCard(string username)
	{
		bool exists = false;
		await storage.Document($"LoyaltyCards/daily/redeeemed/{username.ToLower()}").GetSnapshotAsync().ContinueWithOnMainThread(x =>
		{
			exists = x.Result.Exists;
		});

		return exists;
	}

	public void AddRedeemedUser(string username)
	{
		storage.Collection("LoyaltyCards/daily/redeemed").Document(username.ToLower()).SetAsync(new Dictionary<string, object>());
	}

	public void UpdateUser(LoyaltyCardUser user, ELoyaltyCardType cardType)
	{
		Dictionary<string, object> data = new Dictionary<string, object>()
		{
			{"totalStamps", user.totalStamps },
			{"currentCardStamps", user.currentCardStamps },
			{"completedCards", user.completedCards },
			{"stamps", user.stamps }
		};
		storage.Collection($"LoyaltyCards/{(cardType == ELoyaltyCardType.Daily ? "daily" : "subs")}/users").Document(user.username.ToLower()).UpdateAsync(data);
	}

	#region VIEWER STATS
	public async void UpdateViewerStats(string username, ViewerStats stats, string date = "")
	{
		try
		{
			DocumentReference doc = storage.Document($"Users/{username.ToLower()}/Stats/{date}");
			await doc.SetAsync(stats);
			Logger.Log($"Updated stats for {username.ToLower()}");
		}
		catch (Exception e)
		{
			Logger.LogError($"Error updating user stats : Users/{username.ToLower()}/Stats/{date}");
			Logger.LogError(e.Message);
			throw e;
		}
	}
	
	public async Task<int> GetUserTotalOuates(string username)
	{
		try
		{
			int total = 0;
			await storage.Collection($"Users/{username.ToLower()}/Stats").GetSnapshotAsync().ContinueWithOnMainThread(x =>
			{
				foreach (var item in x.Result)
				{
					int ouates = 0;
					item.TryGetValue("ouates", out ouates);
					total += ouates;
				}
			});

			return total;
		}
		catch (Exception e)
		{
			Logger.LogError($"Error getting user total ouates: Users/{username.ToLower()}/Stats/");
			Logger.LogError(e.Message);
			throw e;
		}
	}
	#endregion
}