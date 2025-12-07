using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LoyaltyCard : MonoBehaviour
{
	///Stocker les users qui utilisent la carte de fidélité
	///	Pour chaque user : 
	///		conserver les tampons utilisés / en cours + offset position / le total de tampons et cartes complétées (+série sans interruption?)
	///		si déja utilisé, cancel
	///		la pp du user
	///		
	///		recup evenement carte completée
	///		
	/// event récompense dépensée avec user en param
	/// charger la PP sur la carte
	/// charger les tampons actifs et save data
	/// 
	/// slide de la carte sur l'écran
	/// nouveau coup de tampon
	/// 
	/// si carte complétée animation, explosion, feux d'artifice etc
	/// 
	/// slide carte hors de l'écran
	///	

	private class QueuedUser
	{
		public QueuedUser(string _username, string _url, ELoyaltyCardType _cardType, int _count)
		{
			username = _username;
			url = _url;
			cardType = _cardType;
			count = _count;
		}

		public string username;
		public string url;
		public ELoyaltyCardType cardType;
		public int count;
	}

	[Header("CARD")]
	[SerializeField]
	private RectTransform card;
	[SerializeField]
	private Image cardImage;
	[SerializeField]
	private Sprite defaultUserPP;
	[SerializeField]
	private Sprite cardDailySprite;
	[SerializeField]
	private Sprite cardSubSprite;
	[SerializeField]
	private Vector2 cardOutScreenPosition;
	[SerializeField]
	private Vector2 cardInScreenPosition;

	[Header("STAMP")]
	[SerializeField]
	private Transform stampsContainer;
	[SerializeField]
	private RectTransform stamp;
	[SerializeField]
	private Vector2 stampOutScreenPosition;
	[SerializeField]
	private Vector2 stampHoverOffset;

	[Header("STAMPS PLACEMENT")]
	[SerializeField]
	private RectTransform stampPrefab;
	[SerializeField]
	private Vector2 stampRandomOffset;
	[SerializeField]
	private Vector2[] stampPositionsDaily;
	[SerializeField]
	private Vector2[] stampPositionsSub;

	[Header("MISC")]
	[SerializeField]
	private Image ppImage;
	[SerializeField]
	private TextMeshProUGUI usernameText;

	private bool isProcessing;
	private int currentStampIndex;
	private List<QueuedUser> queue = new List<QueuedUser>();
	private LoyaltyCardData data;
	private LoyaltyCardData dataDaily;
	private LoyaltyCardData dataSubs;
	private LoyaltyCardUser currentUser;

	private void Start()
	{
		string file = IniParser.loyaltyCardDailyFile;
		if (!File.Exists(file))
		{
			Logger.LogError($"ERROR | Cannot read loyalty card file : {file} to reset redeemed users");
			return;
		}
		dataDaily = JsonConvert.DeserializeObject<LoyaltyCardData>(File.ReadAllText(file));

		file = IniParser.loyaltyCardSubFile;
		if (!File.Exists(file))
		{
			Logger.LogError($"ERROR | Cannot read loyalty card file : {file} to reset redeemed users");
			return;
		}
		dataSubs = JsonConvert.DeserializeObject<LoyaltyCardData>(File.ReadAllText(file));
	}

	public void ResetRedeemedUsers()
	{
		dataDaily.redeemedUsers.Clear();
		SaveData(ELoyaltyCardType.Daily);

		dataSubs.redeemedUsers.Clear();
		SaveData(ELoyaltyCardType.Sub);
	}

	public void AddToQueue(string username, string avatar, ELoyaltyCardType cardType, int count = 1)
	{
		if (isProcessing || queue.Count > 0)
		{
			queue.Add(new QueuedUser(username, avatar, cardType, count));
			return;
		}

		queue.Add(new QueuedUser(username, avatar, cardType, count));
		StartCoroutine(StampCard());
	}

	private bool HasUserAlreadyStampedCard(QueuedUser user)
	{
		if (user.cardType!= ELoyaltyCardType.Daily)
		{
			//only daily is once per day
			return false;
		}

		data = user.cardType == ELoyaltyCardType.Daily ? dataDaily : dataSubs;			
		string file = user.cardType == ELoyaltyCardType.Daily ? IniParser.loyaltyCardDailyFile : IniParser.loyaltyCardSubFile;

		if(data == null)
		{
			if (!File.Exists(file))
			{
				Logger.LogError($"ERROR | Cannot read loyalty card file : {file} to update card for {user.username}");
				return true;
			}

			data = JsonConvert.DeserializeObject<LoyaltyCardData>(File.ReadAllText(file));
		}

		currentUser = data.users.FirstOrDefault(x=>x.username == user.username);
		if (currentUser == null)
		{
			currentUser = new LoyaltyCardUser(){username = user.username };
			data.users.Add(currentUser);
			return false;
		}
		else if(user.url == "{AVATAR}")
		{
			user.url = currentUser.avatarUrl;
		}


		if (data.redeemedUsers.Contains(user.username))
		{
			return true;
		}

		return false;
	}

	private IEnumerator StampCard()
	{
		QueuedUser user;
		while (queue.Count > 0)
		{
			foreach (Transform item in stampsContainer)
			{
				Destroy(item.gameObject);
			}

			isProcessing = true;
			user = queue.ElementAt(0);
			queue.RemoveAt(0);

			//Check if user has already redeemed today
			if (HasUserAlreadyStampedCard(user))
			{
				Logger.LogError($"{user.username} is trying to stamp his {user.cardType} card again");
				continue;
			}

			Task<Sprite> t=GetUserAvatar(user.username, user.url, user.cardType);

			while (!t.IsCompleted)
			{
				yield return null;
			}
			if (t.IsFaulted)
			{
				Logger.LogError(t.Exception.ToString());
				continue;
			}
			else
			{
				ppImage.sprite = t.Result;
			}

			usernameText.text = $"Valid for : {user.username}";
			cardImage.sprite = user.cardType == ELoyaltyCardType.Daily ? cardDailySprite : cardSubSprite;

			RestoreStamps(user.username);

			Stamp(user.username, user.cardType, user.count);

			if (user.username != "testFidDaily")
			{
				data.redeemedUsers.Add(user.username);
			}

			yield return new WaitUntil(() => isProcessing == false);

			SaveData(user.cardType);
		}
	}

	private void SaveData(ELoyaltyCardType cardType)
	{
		string json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings()
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		});

		string file = cardType == ELoyaltyCardType.Daily ? IniParser.loyaltyCardDailyFile : IniParser.loyaltyCardSubFile;
		File.WriteAllText(file, json);
	}

	private void RestoreStamps(string username)
	{
		for (currentStampIndex = 0; currentStampIndex < currentUser.currentCardStamps; currentStampIndex++)
		{
			SpawnStamp(currentUser.currentCardStampsPositions[currentStampIndex], currentUser.currentCardStampsRotations[currentStampIndex]);
		}
	}

	private void Stamp(string username, ELoyaltyCardType cardType, int count)
	{
		Vector2[] positions = cardType == ELoyaltyCardType.Daily ? stampPositionsDaily : stampPositionsSub;

		Sequence sequence = DOTween.Sequence();

		sequence.Append(card.DOAnchorPos(cardInScreenPosition, 1f)) //Move card in
			.Append(stamp.DOAnchorPos(stampPositionsDaily[currentStampIndex] + stampHoverOffset, 1f)) //Move stamp in
			.AppendInterval(.5f);

		int done = 0;
		print($"Starting loop: {currentStampIndex}");
		int todo = Mathf.Min(count, positions.Length - currentStampIndex);
		for (int i = 0; i < todo; i++)
		{
			if (i > 0)
			{
				sequence.Append(stamp.DOAnchorPos(positions[currentStampIndex] + stampHoverOffset, .2f)); //Move stamp in
			}

			// Apply stamp
			sequence.Append(
				stamp.DOAnchorPos(positions[currentStampIndex], .1f)
				.OnComplete(() =>
				{
					print($"Done loop : {currentStampIndex}");
					currentUser.totalStamps++;
					currentUser.currentCardStamps++;

					SpawnStamp(
						positions[currentStampIndex] + new Vector2(Random.Range(-stampRandomOffset.x, stampRandomOffset.x), Random.Range(-stampRandomOffset.y, stampRandomOffset.y)),
						UnityEngine.Random.Range(0, 360));
					currentStampIndex++;
					Logger.Log($"current stamp index = {currentStampIndex}");
				}))
				//Put stamp up
				.Append(stamp.DOAnchorPos(positions[currentStampIndex] + stampHoverOffset, .1f));
			//TODO : FX ON CARD COMPLETED
			done++;
			currentStampIndex++;
		}


		//Move stamp out
		sequence.Append(stamp.DOAnchorPos(stampOutScreenPosition, 1f)) 
		.AppendInterval(.5f)

		//Move card out
		.Append(card.DOAnchorPos(cardOutScreenPosition, 1f))
		.OnComplete(() =>
		{
			print($"Done end : {done}");
			//if stamps remain
			if (count - done > 0)
			{
				foreach (Transform item in stampsContainer)
				{
					Destroy(item.gameObject);
				}

				currentUser.completedCards++;
				currentUser.currentCardStamps = 0;
				currentStampIndex = 0;

				Stamp(username, cardType, count - done);
			}
			else
			{
				isProcessing = false;
			}
		});
		currentStampIndex -= done;
	}

	private void SpawnStamp(Vector2 pos, float rot)
	{
		AudioManager.instance.PlayStamp();
		RectTransform spawned = Instantiate(stampPrefab, stampsContainer);
		spawned.anchoredPosition = pos;
		spawned.rotation = Quaternion.Euler(0f, 0f, rot);
		currentUser.currentCardStampsPositions[currentStampIndex] = pos;
		currentUser.currentCardStampsRotations[currentStampIndex] = rot;
	}

	public string GetLoyaltyCardsStatusString(string username)
	{
		string ret = $"@{username} ";

		if (dataDaily == null || dataDaily.users.Count == 0)
		{
			dataDaily = JsonConvert.DeserializeObject<LoyaltyCardData>(File.ReadAllText(IniParser.loyaltyCardDailyFile));
		}

		LoyaltyCardUser user = dataDaily.users.FirstOrDefault(x => x.username == username);
		if (user == null)
		{
			ret += "Carte quotidienne : non commencée. ";
		}
		else
		{
			ret += $"Carte quotidienne : {user.currentCardStamps}/10, total : {user.totalStamps}, cartes complétées : {user.completedCards}. ";
		}

		if (dataSubs == null || dataSubs.users.Count == 0)
		{
			dataSubs = JsonConvert.DeserializeObject<LoyaltyCardData>(File.ReadAllText(IniParser.loyaltyCardSubFile));
		}

		user = dataSubs.users.FirstOrDefault(x => x.username == username);
		if (user == null)
		{
			ret += "Carte subs : non commencée. ";
		}
		else
		{
			ret += $"Carte subs : {user.currentCardStamps}/12, total : {user.totalStamps}, cartes complétées : {user.completedCards}.";
		}

		return ret;
	}

	public async Task<Sprite> GetUserAvatar(string pseudo, string url, ELoyaltyCardType cardType)
	{

		LoyaltyCardUser u = (cardType == ELoyaltyCardType.Sub ? dataSubs : dataDaily).users.FirstOrDefault(x => x.username == pseudo);
		if(url == "{AVATAR}")
		{
			if(u != null)
			{
				url = u.avatarUrl;
			}
			else
			{
				Logger.LogError($"Error fetching avatar for {pseudo}, received avatar url : {url}");
				return defaultUserPP;
			}
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
						Logger.LogError($"Error fetching avatar for {pseudo}");
						Logger.LogError($"avatar url :{url}");
						Logger.LogError(uwr.error);
					}
					else
					{
						if (u != null)
						{
							u.avatarUrl = url;
						}
						else
						{
							u = new LoyaltyCardUser()
							{
								username = pseudo,
								avatarUrl = url
							};

							SaveData(cardType);
						}

						var texture = DownloadHandlerTexture.GetContent(uwr);
						return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
					}
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
			}

			return defaultUserPP;
		}
	}
}
