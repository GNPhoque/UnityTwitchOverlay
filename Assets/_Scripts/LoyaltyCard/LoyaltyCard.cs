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
	private List<QueuedUser> queue = new List<QueuedUser>();
	private LoyaltyCardUser currentUser;


	public void ResetRedeemedUsers()
	{
		Database.instance.ResetRedeemedUsers();
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

	private IEnumerator StampCard()
	{
		QueuedUser queuedUser;
		while (queue.Count > 0)
		{
			foreach (Transform item in stampsContainer)
			{
				Destroy(item.gameObject);
			}

			isProcessing = true;
			queuedUser = queue.ElementAt(0);
			queue.RemoveAt(0);

			bool result = false;
			Task<bool> task = Database.instance.HasUserAlreadyStampedCard(queuedUser.username);
			while (!task.IsCompleted)
			{
				yield return null;
			}
			if (task.IsFaulted)
			{
				Logger.LogError(task.Exception.ToString());
				continue;
			}
			result = task.Result;

			//Check if user has already redeemed today
			if (queuedUser.cardType == ELoyaltyCardType.Daily && task.Result)
			{
				Logger.LogError($"{queuedUser.username} is trying to stamp his {queuedUser.cardType} card again");
				continue;
			}

			Task<Sprite> t = Database.instance.GetUserAvatarFromTwitchat(queuedUser.username, queuedUser.url);

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

			Task<LoyaltyCardUser> tl= Database.instance.GetUser(queuedUser.username, queuedUser.cardType);

			while (!tl.IsCompleted)
			{
				yield return null;
			}
			if (tl.IsFaulted)
			{
				Logger.LogError(tl.Exception.ToString());
				continue;
			}
			else
			{
				currentUser = tl.Result;
			}
			

			usernameText.text = $"Valid for : {queuedUser.username}";
			cardImage.sprite = queuedUser.cardType == ELoyaltyCardType.Daily ? cardDailySprite : cardSubSprite;

			RestoreStamps();

			Stamp(queuedUser.username, queuedUser.cardType, queuedUser.count);

			if (queuedUser.username != "testFidDaily")
			{
				Database.instance.AddRedeemedUser(queuedUser.username);
			}

			yield return new WaitUntil(() => isProcessing == false);


			if (currentUser.currentCardStamps >= (queuedUser.cardType == ELoyaltyCardType.Daily?stampPositionsDaily : stampPositionsSub).Count())
			{
				currentUser.completedCards++;
				currentUser.currentCardStamps = 0;
				WebSocketInteractions.instance.AddDailyCardCompleted(currentUser.username);
			}
			Database.instance.UpdateUser(currentUser, queuedUser.cardType);
		}
	}

	private void RestoreStamps()
	{
		int currentCardStamps = currentUser.currentCardStamps;
		for (currentCardStamps = 0; currentCardStamps < currentUser.currentCardStamps; currentCardStamps++)
		{
			SpawnStamp(new Vector2(currentUser.stamps[currentCardStamps].x, currentUser.stamps[currentCardStamps].y), currentUser.stamps[currentCardStamps].rotation, true);
		}
	}

	private void Stamp(string username, ELoyaltyCardType cardType, int count)
	{
		Vector2[] positions = cardType == ELoyaltyCardType.Daily ? stampPositionsDaily : stampPositionsSub;

		Sequence sequence = DOTween.Sequence();

		sequence.Append(card.DOAnchorPos(cardInScreenPosition, 1f)) //Move card in
			.Append(stamp.DOAnchorPos(positions[currentUser.currentCardStamps] + stampHoverOffset, 1f)) //Move stamp in
			.AppendInterval(.5f);

		int done = 0;
		print($"Starting loop: {currentUser.currentCardStamps}");
		int todo = Mathf.Min(count, positions.Length - currentUser.currentCardStamps);
		for (int i = 0; i < todo; i++)
		{
			if (i > 0)
			{
				sequence.Append(stamp.DOAnchorPos(positions[currentUser.currentCardStamps] + stampHoverOffset, .2f)); //Move stamp in
			}

			// Apply stamp
			sequence.Append(
				stamp.DOAnchorPos(positions[currentUser.currentCardStamps], .1f)
				.OnComplete(() =>
				{
					print($"Done loop : {currentUser.currentCardStamps}");
					currentUser.totalStamps++;

					SpawnStamp(
						positions[currentUser.currentCardStamps] + new Vector2(Random.Range(-stampRandomOffset.x, stampRandomOffset.x), Random.Range(-stampRandomOffset.y, stampRandomOffset.y)),
						UnityEngine.Random.Range(0, 360), false);
					currentUser.currentCardStamps++;
					Logger.Log($"current stamp index = {currentUser.currentCardStamps}");
				}))
				//Put stamp up
				.Append(stamp.DOAnchorPos(positions[currentUser.currentCardStamps] + stampHoverOffset, .1f));
			//TODO : FX ON CARD COMPLETED
			done++;
			currentUser.currentCardStamps++;
			if (currentUser.currentCardStamps > positions.Count())
			{
				currentUser.currentCardStamps = 0;
			}
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

				Stamp(username, cardType, count - done);
			}
			else
			{
				isProcessing = false;
			}
		});

		if(currentUser.currentCardStamps > 0)
		{
			currentUser.currentCardStamps -= done;
		}
	}

	private void SpawnStamp(Vector2 pos, float rot, bool silence)
	{
		if (!silence)
		{
			AudioManager.instance.PlayStamp();
		}
		RectTransform spawned = Instantiate(stampPrefab, stampsContainer);
		spawned.anchoredPosition = pos;
		spawned.rotation = Quaternion.Euler(0f, 0f, rot);
		currentUser.stamps[currentUser.currentCardStamps] = new Stamp(pos.x, pos.y, rot);
	}

	public IEnumerator GetLoyaltyCardsStatusString(string username, Action<string> callback)
	{
		string ret = $"@{username} ";

		Task<LoyaltyCardUser> tl = Database.instance.GetUser(username, ELoyaltyCardType.Daily);
		LoyaltyCardUser user;

		while (!tl.IsCompleted)
		{
			yield return null;
		}
		if (tl.IsFaulted)
		{
			Logger.LogError(tl.Exception.ToString());
			callback(null);
			yield break;
		}
		else
		{
			user = tl.Result;
		}
		if (user == null)
		{
			ret += "Carte quotidienne : non commencée. ";
		}
		else
		{
			ret += $"Carte quotidienne : {user.currentCardStamps}/10, total : {user.totalStamps}, cartes complétées : {user.completedCards}. ";
		}


		tl = Database.instance.GetUser(username, ELoyaltyCardType.Sub);

		while (!tl.IsCompleted)
		{
			yield return null;
		}
		if (tl.IsFaulted)
		{
			Logger.LogError(tl.Exception.ToString());
			callback(null);
			yield break;
		}
		else
		{
			user = tl.Result;
		}
		if (user == null)
		{
			ret += "Carte subs : non commencée. ";
		}
		else
		{
			ret += $"Carte subs : {user.currentCardStamps}/12, total : {user.totalStamps}, cartes complétées : {user.completedCards}.";
		}

		callback(ret);
	}
}
