using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
public class TimerImage
{
	public bool isTimerOver;
	public string index;
	public Sprite sprite;
}

public class WebSocketInteractions : MonoBehaviour
{
	[DllImport("user32.dll", SetLastError = true)]	static extern int GetWindowLong(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll")]	static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll")]	static extern IntPtr GetActiveWindow();
	const int GWL_EXSTYLE = -20;
	const int WS_EX_TRANSPARENT = 0x00000020;
	private IntPtr hwnd;

	[SerializeField]
	private Transform canvas;
	[SerializeField]
	private TextMeshProUGUI streamEndTimer;
	[SerializeField]
	private CanvasScaler canvasScaler;
	[SerializeField]
	private Image splatterPrefab;
	[SerializeField]
	private GifPlayer gifPlayerPrefab;

	[SerializeField]
	private TextMeshProUGUI deathCounterText;

	[SerializeField]
	private TMP_InputField clipMarkerInputField;

	[SerializeField]
	private RectTransform creditsSectionsContainer;
	[SerializeField]
	private List<TextMeshProUGUI> creditsSections;
	[SerializeField]
	private float creditsScrollSpeed;
	[SerializeField]
	private float creditsTitleFontSize;
	[SerializeField]
	private float creditsTextFontSize;
	[SerializeField]
	TMP_SpriteAsset medalsSpriteAsset;
	private float creditsBlockHeight;
	[SerializeField]
	private Color32 creditsTitleColor;
	[SerializeField]
	private Color32 creditsDataColor;

	[SerializeField]
	private RectTransform bigRaclette;
	[SerializeField]
	private SplashSO ctrlSplash;
	[SerializeField]
	private SplashSO altSplash;
	[SerializeField]
	private SplashSO shiftSplash;
	[SerializeField]
	private List<SplashSO> splashes = new List<SplashSO>();
	private Dictionary<SplashSO, List<UniGif.GifTexture>> gifTextures = new Dictionary<SplashSO, List<UniGif.GifTexture>>();

	private bool isHappyHourOn;
	private Coroutine happyHourCoroutine;
	private Coroutine creditsScrollCoroutine;

	[SerializeField]
	private LoyaltyCard loyaltyCard;

	[SerializeField]
	private GameObject hand;

	private bool isFollowingHand;
	private HandTrackingPython python;

	[SerializeField]
	private SplashSO avatarSo;
	private SplashSO so;

	[SerializeField]
	private Emote emotePrefab;
	[SerializeField]
	private Emote emoteGifPrefab;
	[SerializeField]
	private RectTransform throughScreenImage;
	[SerializeField]
	private string lurkGifPath;
	private List<UniGif.GifTexture> lurkGif;
	[SerializeField]
	private Sprite dodoSprite;

	[SerializeField]
	private Transform timerLayout;
	[SerializeField]
	private List<TimerImage> timerImages;
	private List<Emote> timersReadyToDelete = new List<Emote>();

	[Header("SteamAchievement")]
	[SerializeField]
	private float steamAchievementMoveSpeed;
	[SerializeField]
	private float steamAchievementUpPosition;
	[SerializeField]
	private float steamAchievementDownPosition;
	[SerializeField]
	private RectTransform steamAchievement;
	[SerializeField]
	private Image steamAchievementImage;
	[SerializeField]
	private TextMeshProUGUI steamAchievementTitle;
	[SerializeField]
	private TextMeshProUGUI steamAchievementDescription;

	private bool isPhoqueThroughScreenGoing;

	public static WebSocketInteractions instance;


	private void Awake()
	{
		if(instance != null)
		{
			return;
		}

		instance = this;
	}

	private void Start()
	{
		hwnd = GetActiveWindow();
		python = new HandTrackingPython();

		foreach (var splash in splashes)
		{
			if(splash.isGif == false)
			{
				continue;
			}
						
			StartCoroutine(GifPlayer.SetGifFromUrlCoroutine(splash.gifPath, (textures) => gifTextures.Add(splash, textures)));
		}
		StartCoroutine(GifPlayer.SetGifFromUrlCoroutine(lurkGifPath, (textures) => lurkGif = textures));

		happyHourCoroutine = StartCoroutine(StartHappyHourRandom());

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		int totalSubs = 0;
		foreach (var sub in data.subs)
		{
			totalSubs += sub.Months == 0 ? 1 : sub.Months;
		}
		UpdateStreamEndTimer(totalSubs);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Vector3 pos = new Vector3(Random.Range(0, canvasScaler.referenceResolution.x), Random.Range(0, canvasScaler.referenceResolution.y), 0f);
			Instantiate(splatterPrefab, pos, Quaternion.identity, canvas);
		}
	}

	#region SPLASH
	public async void DrawSplash(float left = -1, float top = -1, int duration = -1, string avatarUrl = "", string pseudo = "", bool ctrl = false, bool alt = false, bool shift = false)
	{
		so = null;
		if (!string.IsNullOrEmpty(avatarUrl))
		{
			so = avatarSo;
			so.sprite = await Database.instance.GetUserAvatarFromTwitchat(pseudo, avatarUrl);
		}

		if (ctrl)
		{
			so = ctrlSplash;
		}
		else if (alt)
		{
			so = altSplash;
		}
		else if (shift)
		{
			so = shiftSplash;
		}

		if (so == null)
		{
			so = GetWeightedImage();
		}

		Vector2 pos;
		if (left == -1 && top == -1)
		{
			pos = new Vector2(Random.Range(0, canvasScaler.referenceResolution.x), Random.Range(0, canvasScaler.referenceResolution.y));
		}
		else
		{
			pos = new Vector2(canvasScaler.referenceResolution.x * left / 100f, canvasScaler.referenceResolution.y * top / 100f);
		}

		Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

		GameObject splash;
		if (so.isGif == false)
		{
			splash = Instantiate(splatterPrefab, canvas).gameObject;
			Image image = splash.GetComponent<Image>();
			image.sprite = so.sprite;
			if (so.colorList.Count > 0)
			{
				image.color = so.colorList[Random.Range(0, so.colorList.Count - 1)];
			}
		}
		else
		{
			splash = Instantiate(gifPlayerPrefab, canvas).gameObject;
			GifPlayer image = splash.GetComponent<GifPlayer>();
			image.LoadGif(gifTextures[so], so.width, so.height);
			if (so.colorList.Count > 0)
			{
				image.SetColor(so.colorList[Random.Range(0, so.colorList.Count - 1)]);
			}
			image.Play();
		}

		RectTransform rt = splash.GetComponent<RectTransform>();
		rt.anchoredPosition = pos;
		rt.rotation = rot;
		rt.sizeDelta = new Vector2(so.width, so.height);
		rt.GetComponent<BoxCollider2D>().size = new Vector2(so.width, so.height);

		StartCoroutine(SplashEaseIn(rt));
		//StartCoroutine(RemoveSplashAfterDelay(3, splash.gameObject));
		StartCoroutine(RemoveSplashAfterDelay(duration == -1 ? so.duration : duration, splash.gameObject));
	}

	private SplashSO GetWeightedImage()
	{
		int totalWeight = 0;

		foreach (var item in splashes)
		{
			totalWeight += item.weight;
		}

		int rng = Random.Range(0, totalWeight);
		int progress = 0;

		foreach (var item in splashes)
		{
			progress += item.weight;
			if (progress > rng)
			{
				return item;
			}
		}

		return null;
	}

	private IEnumerator SplashEaseIn(RectTransform rt, float easeDuration = .05f)
	{
		float currentDuration = 0f;
		rt.localScale = Vector2.zero;

		while (currentDuration < easeDuration)
		{
			yield return null;
			rt.localScale = Vector2.one * currentDuration / easeDuration;
			currentDuration += Time.deltaTime;
		}

		rt.localScale = Vector2.one;
	}

	private IEnumerator SplashEaseOut(RectTransform rt, float easeDuration = .1f)
	{
		float currentDuration = 0f;
		rt.localScale = Vector2.one;

		while (currentDuration < easeDuration)
		{
			yield return null;
			rt.localScale = Vector2.one - (Vector2.one * currentDuration / easeDuration);
			currentDuration += Time.deltaTime;
		}

		rt.localScale = Vector2.zero;
	}

	public IEnumerator RemoveSplashWithRaclette(Splash splash)
	{
		if (splash.markedForDestruction)
		{
			yield break;
		}

		splash.markedForDestruction = true;
		yield return SplashEaseOut(splash.transform as RectTransform);
		RemoveSplash(splash.gameObject);
	}

	private IEnumerator RemoveSplashAfterDelay(int delay, GameObject toDestroy)
	{
		yield return new WaitForSeconds(delay);

		if (toDestroy == null)
		{
			yield break;
		}

		yield return SplashEaseOut(toDestroy.transform as RectTransform);
		RemoveSplash(toDestroy);
	}

	public void RemoveSplash(GameObject toDestroy = null)
	{
		if (toDestroy != null)
		{
			Destroy(toDestroy);
		}
		else
		{
			if (canvas.childCount <= 0)
			{
				return;
			}
			else
			{
				Destroy(canvas.GetChild(0).gameObject);
			}
		}
	}

	public void RemoveAllSplash()
	{
		//for (int i = 0; i < canvas.childCount; i++)
		//{
		//	Destroy(canvas.GetChild(i).gameObject);
		//}

		bigRaclette.anchoredPosition = new Vector2(1650,bigRaclette.anchoredPosition.y);
		bigRaclette.DOAnchorPosX(-1650, 5f);
	}

	public IEnumerator FillScreen()
	{
		for (int i = 0; i < 100; i++)
		{
			DrawSplash();
			yield return new WaitForSeconds(.01f);
		}
	}

	public IEnumerator ClearScreen()
	{
		float wait = 1f / canvas.childCount;
		while (canvas.childCount > 0) 
		{
			Destroy(canvas.GetChild(0).gameObject);
			yield return new WaitForSeconds(wait);
		}
	}

	public void PhoqueThroughScreen()
	{
		if (isPhoqueThroughScreenGoing)
		{
			return;
		}

		isPhoqueThroughScreenGoing = true;
		throughScreenImage.anchoredPosition = new Vector2(-2000, throughScreenImage.anchoredPosition.y);
		throughScreenImage.DOAnchorPosX(2000, 5f).SetEase(Ease.Linear).OnComplete(() => isPhoqueThroughScreenGoing = false);
	}

	public void Lurk(string user)
	{
		Vector2 spawnPosition = GetRandomPointOnScreeen();
		Emote lurk = Instantiate(emoteGifPrefab, canvas);
		((RectTransform)lurk.transform).anchoredPosition = spawnPosition;
		lurk.SetText(user);
		lurk.SetGif(lurkGif, avatarSo.width, avatarSo.height);
		lurk.Move();
	}

	public void Dodo(string user)
	{
		Vector2 spawnPosition = GetRandomPointOnScreeen();
		print(spawnPosition);
		Emote dodo = Instantiate(emotePrefab, canvas);
		((RectTransform)dodo.transform).anchoredPosition = spawnPosition;
		dodo.SetText(user);
		dodo.SetSprite(dodoSprite);
		dodo.Move();
	}
	#endregion

	#region CLIP MARKER
	public void CreateClipMarkerTime()
	{
		if (!File.Exists(IniParser.clipMarkerFile))
		{
			Logger.LogError($"ERROR | Cannot read clip marker file : {IniParser.clipMarkerFile}");
			return;
		}

		using var sw = File.AppendText(IniParser.clipMarkerFile);
		{
			sw.Write($"{Environment.NewLine}{DateTime.Now}");
		}
	}

	public void CreateClipMarkerText()
	{
		if (!File.Exists(IniParser.clipMarkerFile))
		{
			Logger.Log($"ERROR | Cannot read clip marker file : {IniParser.clipMarkerFile}");
			return;
		}

		WindowFocusManager.FocusUnityWindow();

		// Active et prépare le champ
		clipMarkerInputField.gameObject.SetActive(true);
		clipMarkerInputField.text = "";

		// Sélectionne le champ dans l’EventSystem
		EventSystem.current.SetSelectedGameObject(clipMarkerInputField.gameObject);

		// Force le TMP_InputField à être prêt pour la saisie
		clipMarkerInputField.ActivateInputField();
		clipMarkerInputField.Select();

		clipMarkerInputField.onEndEdit.AddListener(SetClipMarkerName);
	}
	private void SetClipMarkerName(string name)
	{
		using var sw = File.AppendText(IniParser.clipMarkerFile);
		{
			sw.WriteLine($"\t\t{name}");
		}
		clipMarkerInputField.gameObject.SetActive(false);
		clipMarkerInputField.onEndEdit.RemoveListener(SetClipMarkerName);

		Logger.Log($"Clip marker enregistré : {name}");
		WindowFocusManager.RestorePreviousFocus();
	}

	public void CreateClipMarkerTimeAndText()
	{
		if (!File.Exists(IniParser.clipMarkerFile))
		{
			Logger.Log($"ERROR | Cannot read clip marker file : {IniParser.clipMarkerFile}");
			return;
		}

		CreateClipMarkerTime();
		CreateClipMarkerText();
	}
	#endregion

	#region DEATH COUNTER
	public bool IsDeathCounterVisible()
	{
		return deathCounterText.gameObject.activeSelf;
	}

	public void ShowText()
	{
		deathCounterText.gameObject.SetActive(true);
	}

	public void HideText()
	{
		deathCounterText.gameObject.SetActive(false);
	}

	public void UpdateText(string message)
	{
		deathCounterText.text = message;
	}
	#endregion

	#region LOGS
	public void ShowLogs()
	{
		Logger.instance.ShowLogs(true, true);
		DisableClickThrough();
	}

	public void HideLogs()
	{
		Logger.instance.ShowLogs(false);
		EnableClickThrough();
	}

	public void EnableClickThrough()
	{
		int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
		SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
	}

	public void DisableClickThrough()
	{
		int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
		SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
	}
	#endregion

	#region HAPPY HOUR
	public void RestartHappyHourCoroutine()
	{
		happyHourCoroutine = StartCoroutine(StartHappyHourRandom());
	}

	public IEnumerator StartHappyHourRandom()
	{
		while (true)
		{
			float rng = Random.Range(float.Parse(IniParser.happyHourIntervalMin), float.Parse(IniParser.happyHourIntervalMax));
			//WAIT 30-60 minutes
			yield return new WaitForSeconds(rng);
			//START HAPPYHOUR
			EnableHappyHour();
			//WAIT 10 minutes
			yield return new WaitForSeconds(float.Parse(IniParser.happyHourDuration));
		}
	}

	public void EnableHappyHour(bool force = false)
	{
		string json = "{\"topic\":\"happyHourOn\"}";
		LocalWebSocket.wssv.WebSocketServices[$"/{IniParser.behaviorName}"].Sessions.Broadcast(json);
		Logger.Log($"Message broadcasted : {json}");
		isHappyHourOn = true;
		if (force)
		{
			if (happyHourCoroutine != null)
			{
				StopCoroutine(happyHourCoroutine);
			}

			Invoke("RestartHappyHourCoroutine", float.Parse(IniParser.happyHourDuration));
		}
	}

	public void DisableHappyHour()
	{
		string json = "{\"topic\":\"happyHourOff\"}";
		LocalWebSocket.wssv.WebSocketServices[$"/{IniParser.behaviorName}"].Sessions.Broadcast(json);
		Logger.Log($"Message broadcasted : {json}");
		isHappyHourOn = false;

		if (happyHourCoroutine == null) 
		{
			happyHourCoroutine = StartCoroutine(StartHappyHourRandom());
		}
	}
	#endregion

	#region STAMP
	public void ResetStampRedeemedUsers()
	{
		loyaltyCard.ResetRedeemedUsers();
	}

	public void StampCard(string username, string avatar, ELoyaltyCardType cardType, int number = 1)
	{
		loyaltyCard.AddToQueue(username, avatar, cardType, number);
	}
	
	public void AddDailyCardCompleted(string username)
	{
		Emote emote = Instantiate(emotePrefab, timerLayout);
		emote.SetKinematic();
		emote.SetText(username);
		timersReadyToDelete.Add(emote);
	}

	public IEnumerator SendFidDetails(string username)
	{
		string message = "";
		yield return StartCoroutine(loyaltyCard.GetLoyaltyCardsStatusString(username, (ret) => message = ret));

		if (string.IsNullOrEmpty(message))
		{
			yield break;
		}

		string json = $"{{\"topic\":\"fidOut\", \"text\":\"{message}\"}}";
		LocalWebSocket.wssv.WebSocketServices[$"/{IniParser.behaviorName}"].Sessions.Broadcast(json);
		Logger.Log($"Message broadcasted : {json}");
	}
	#endregion

	#region GENERIQUE
	public void ShowCredits()
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to show credits");
			return;
		}

		creditsSectionsContainer.gameObject.SetActive(true);
		UpdateCredits(true);

		creditsScrollCoroutine = StartCoroutine(SlideCredits());
	}
	private IEnumerator SlideCredits()
	{
		creditsSectionsContainer.anchoredPosition = Vector3.zero;

		while (true)
		{
			creditsSectionsContainer.anchoredPosition += Vector2.up * Time.deltaTime * creditsScrollSpeed;
			if(creditsSectionsContainer.anchoredPosition.y > creditsBlockHeight)
			{
				creditsSectionsContainer.anchoredPosition += Vector2.down * creditsBlockHeight;
			}
			yield return null;
		}
	}
	public void HideCredits()
	{
		creditsSectionsContainer.gameObject.SetActive(false);
		if (creditsScrollCoroutine != null)
		{
			StopCoroutine(creditsScrollCoroutine);
		}
	}

	private void UpdateCredits(bool force = false)
	{
		// Si on n'a pas besoin de rafraîchir, on sort
		if (creditsScrollCoroutine == null && !force) return;

		if (creditsSections.Count == 0)
		{
			foreach (TextMeshProUGUI item in creditsSectionsContainer.GetComponentsInChildren<TextMeshProUGUI>())
			{
				creditsSections.Add(item);
			} 
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();

		int sectionIndex = 0;
		for (int j = 0; j < 3; j++)
		{
			sectionIndex++;
			if (data.hypeTrains.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Trains de la Hype</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.hypeTrains)
				{
					creditsSections.ElementAt(sectionIndex).text += $"\nTrain de niveau {item}";
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.raids.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Raid</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.raids)
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{item.User} : {item.Viewers}";
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.subs.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Subs</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.subs)
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{item.User} : {item.Tier}";
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.subgifts.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				int i = 0;
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Subgifts</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.subgifts.OrderByDescending(x => x.Value))
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{AddMedal(i)}{item.Key} {item.Value}";
					i++;
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.bits.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				int i = 0;
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Bits</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.bits.OrderByDescending(x => x.Value))
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{AddMedal(i)}{item.Key} {item.Value}";
					i++;
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.follows.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Followers</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.follows)
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{item}";
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.vips.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>VIPS</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.vips)
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{item}";
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.userMessages.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				int i = 0;
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Chatters</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.userMessages.OrderByDescending(x=>x.Value))
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{AddMedal(i)}{item.Key} {item.Value}";
					i++;
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
			if (data.happyHourPaints.Any())
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(true);
				int i = 0;
				creditsSections.ElementAt(sectionIndex).text = $"<size={creditsTitleFontSize}px><color=#{ColorUtility.ToHtmlStringRGBA(creditsTitleColor)}>Happy Hour Painters</color></size>\n<color=#{ColorUtility.ToHtmlStringRGBA(creditsDataColor)}>";
				foreach (var item in data.happyHourPaints.OrderByDescending(x => x.Value))
				{
					creditsSections.ElementAt(sectionIndex).text += $"\n{AddMedal(i)}{item.Key} : {item.Value}";
					i++;
				}
				creditsSections.ElementAt(sectionIndex).text += $"\n\n</color><size=1><color=#00000000>-</color></size>\n";
			}
			else
			{
				creditsSections.ElementAt(sectionIndex).gameObject.SetActive(false);
			}

			sectionIndex++;
		}

		LayoutRebuilder.ForceRebuildLayoutImmediate(creditsSectionsContainer);
		creditsBlockHeight = 0;
		for (int i = 0; i < 10; i++)
		{
			creditsBlockHeight += creditsSections[i].rectTransform.rect.height;
		}
	}

	private string AddMedal(int i, int repetition = 0)
	{

		string suffix = repetition > 0 ? $"_{repetition}" : "";
		switch (i)
		{
			case 0:
				return $"<sprite name=\"medals_gold{suffix}\">";
			case 1:
				return $"<sprite name=\"medals_silver{suffix}\">";
			case 2:
				return $"<sprite name=\"medals_bronze{suffix}\">";
			default:
				return "";
		}

	}

	public void CreditsAddHypeTrain(int level)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add HypeTrain : {level}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		data.hypeTrains.Add(level);

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddRaid(string raider, int viewers)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add Raid : {raider}, {viewers}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		data.raids.Add(new RaidData { User = raider, Viewers = viewers });

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddSub(string user, string tier)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add Raid : {user}, {tier}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		data.subs.Add(new SubData { User = user, Tier = tier });

		int totalSubs = 0;
		foreach (var sub in data.subs)
		{
			totalSubs += sub.Months == 0 ? 1 : sub.Months;
		}
		UpdateStreamEndTimer(totalSubs);

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddSubGift(string user, int count)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add subgift : {user}, {count}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.subgifts.ContainsKey(user))
		{
			data.subgifts[user] = data.subgifts[user] + count;
		}
		else
		{
			data.subgifts.Add(user, count);
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddBits(string user, int bits)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add subgift : {user}, {bits}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.bits.ContainsKey(user))
		{
			data.bits[user] = data.bits[user] + bits;
		}
		else
		{
			data.bits.Add(user, bits);
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddFollow(string user)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add Follow : {user}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		data.follows.Add(user);

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddFirst(string user)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile} to add VIP : {user}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		data.vips.Add(user);

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsAddMessage(string chatter)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.userMessages.ContainsKey(chatter))
		{
			data.userMessages[chatter] = data.userMessages[chatter] + 1;
		}
		else
		{
			data.userMessages.Add(chatter, 1);
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsRemoveMessage(string chatter)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.userMessages.ContainsKey(chatter))
		{
			data.userMessages.Remove(chatter);
		}
		else
		{
			Logger.LogError($"Could not remove {chatter}");
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	public void CreditsClearTextFile()
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile}");
			return;
		}

		File.WriteAllText(IniParser.creditsFile, "");
		UpdateCredits();
	}
	public void CreditsAddOuates(string user, int value)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile}");
			return;
		}

		StartCoroutine(GetUserTotalOuates(user, value));
		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.ouates.ContainsKey(user))
		{
			data.ouates[user] = data.ouates[user] + value;
		}
		else
		{
			data.ouates.Add(user, value);
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}


	private IEnumerator GetUserTotalOuates(string user, int newOuates)
	{
		//TODO : get current ouates total
		int result = 0;
		Task<int> task = Database.instance.GetUserTotalOuates(user);

		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.IsFaulted)
		{
			Logger.LogError(task.Exception.ToString());
			yield break;
		}
		else
		{
			result = task.Result;
		}

		if(result < 100000 && result + newOuates > 100000)
		{
			Logger.Log($"{user} reached 100K Ouates!");

			Sprite sprite = null;
			Task<Sprite> ts = Database.instance.GetUserAvatarFromDB(user);

			while (!ts.IsCompleted)
			{
				yield return null;
			}
			if (ts.IsFaulted)
			{
				Logger.LogError(ts.Exception.ToString());
				yield break;
			}
			else
			{
				sprite = ts.Result;
			}

			StartCoroutine(ShowSteamAchievement(sprite, "100K!", "100K ouates dépensées", 5f));
			yield break;
		}
		else
		{
			Logger.Log($"{user} total ouates : {result + newOuates}");
		}
	}

	public void CreditsAddHappyHourPainter(string painter)
	{
		if (!File.Exists(IniParser.creditsFile))
		{
			Logger.LogError($"ERROR | Cannot read credits file : {IniParser.creditsFile}");
			return;
		}

		CreditsData data = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
		if (data.happyHourPaints.ContainsKey(painter))
		{
			data.happyHourPaints[painter] = data.happyHourPaints[painter] + 1;
		}
		else
		{
			data.happyHourPaints.Add(painter, 1);
		}

		string json = JsonConvert.SerializeObject(data);
		File.WriteAllText(IniParser.creditsFile, json);
		UpdateCredits();
	}
	#endregion

	#region HAND TRACKING
	public void FollowHand()
	{
		isFollowingHand = true;
		python.processWebcam = true;
		hand.SetActive(true);
	}

	public void UnfollowHand()
	{
		isFollowingHand = false;
		python.processWebcam = false;
		hand.SetActive(false);
	}

	public void MoveToHandPosition(float left, float top)
	{
		if (!isFollowingHand)
		{
			return;
		}

		left = 1 - left;
		left -= .1f;
		left *= 1.1f;

		top -= .2f;
		top *= 1.25f;

		Logger.Log($"Hand Tracking : {left}, {top}");

		//if (Mathf.Abs(Screen.width * hand.transform.position.x - left) < .01f && Math.Abs(SystemParameters.PrimaryScreenHeight * raclettePosition.y - top) < .01f)
		//{
		//	return;
		//}

		hand.transform.position = new Vector3(canvasScaler.referenceResolution.x * left, Screen.height * -top + canvasScaler.referenceResolution.y);
		//Canvas.SetLeft(hand, (int)SystemParameters.PrimaryScreenWidth * left);
		//Canvas.SetTop(hand, (int)SystemParameters.PrimaryScreenHeight * top);
	}
	#endregion

	#region TIMER
	public void AddTimer(string timerName, int duration)
	{
		TimerImage ti = timerImages.FirstOrDefault(x => x.index == timerName);

		if (ti == null)
		{
			Logger.LogError($"Could not find timer name {timerName}");
			return;
		}

		Emote emote = Instantiate(emotePrefab, timerLayout);
		emote.SetKinematic();
		emote.SetSprite(ti.sprite);

		StartCoroutine(TimerCountdown(emote, duration, ti));
	}

	private IEnumerator TimerCountdown(Emote emote,  int duration, TimerImage ti)
	{
		int minutes = Mathf.FloorToInt(duration / 60);
		int seconds = Mathf.FloorToInt(duration % 60);
		if (minutes > 0)
		{
			emote.SetText($"{minutes}:{seconds}");
		}
		else
		{
			emote.SetText($"{seconds}");
		}

		while (duration > 0)
		{
			yield return new WaitForSeconds(1f);
			duration--;
			minutes = Mathf.FloorToInt(duration / 60);
			seconds = Mathf.FloorToInt(duration % 60);
			if(minutes > 0)
			{
				emote.SetText($"{minutes}:{seconds}");
			}
			else
			{
				emote.SetText($"{seconds.ToString("00")}");
			}
		}

		timersReadyToDelete.Add(emote);
	}

	public void RemoveDoneTimers()
	{
		List<Emote> toDelete = new List<Emote>();

		foreach (var item in timersReadyToDelete)
		{
			Destroy(item.gameObject);
		}

		timersReadyToDelete.Clear();
	}
	#endregion

	public void Hanabi(Vector2 startPosition, float explosionHeight, int count, Sprite sprite = null, List<UniGif.GifTexture> gif = null)
	{
		List<Emote> emotes = CreateHanabi(count, sprite, gif);

		foreach (var emote in emotes)
		{
			RectTransform rt = (RectTransform)emote.transform;
			rt.anchoredPosition = startPosition;
			rt.DOAnchorPosY(explosionHeight, 1f).OnComplete(() => emote.Move());
		}
	}

	public List<Emote> CreateHanabi(int count, Sprite sprite = null, List<UniGif.GifTexture> gif = null)
	{
		List<Emote> emotes = new List<Emote>();

		for (int i = 0; i < count; i++)
		{
			if(sprite != null)
			{
				emotes.Add(HanabiSprite(sprite));
			}
			else if(gif != null)
			{
				emotes.Add(HanabiGif(gif));
			}
			else
			{
				SplashSO splash = GetWeightedImage();
				if (splash.isGif)
				{
					emotes.Add(HanabiGif(gifTextures[splash]));
				}
				else
				{
					emotes.Add(HanabiSprite(splash.sprite));

				}
			}
		}

		return emotes;
	}

	public Emote HanabiSprite(Sprite sprite)
	{
		Emote emote = Instantiate(emotePrefab, canvas);
		emote.SetSprite(sprite);
		return emote;
	}

	public Emote HanabiGif(List<UniGif.GifTexture> textures)
	{
		Emote emote = Instantiate(emoteGifPrefab, canvas);
		emote.SetGif(textures, avatarSo.width, avatarSo.height);
		return emote;
	}

	public void SpawnHellos(int count)
	{
		Vector2 spawnPosition = GetRandomPointOnScreeen();
		for (int i = 0; i < count; i++)
		{
			Emote hello = Instantiate(emotePrefab, canvas);
			((RectTransform)hello.transform).anchoredPosition = spawnPosition;
			hello.Move();
		}
	}

	private Vector2 GetRandomPointOnScreeen()
	{
		return new Vector2(Random.Range(-1280f, 1280f), Random.Range(-720f, 720f));
	}

	public void Shutdown()
	{
		Application.Quit();
	}

	public async Task<bool> UpdateViewerStats(string date = "")
	{
		try
		{
			CreditsData creditsData = JsonConvert.DeserializeObject<CreditsData>(File.ReadAllText(IniParser.creditsFile)) ?? new CreditsData();
			Dictionary<string, ViewerStats> stats = new Dictionary<string, ViewerStats>();

			foreach (var item in creditsData.userMessages)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.Key.ToLower()))
				{
					vs = stats[item.Key.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.messages = item.Value;

				if (stats.ContainsKey(item.Key.ToLower()))
				{
					stats[item.Key.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.Key.ToLower(), vs);
				}
			}

			foreach (var item in creditsData.ouates)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.Key.ToLower()))
				{
					vs = stats[item.Key.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.ouates = item.Value;

				if (stats.ContainsKey(item.Key.ToLower()))
				{
					stats[item.Key.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.Key.ToLower(), vs);
				}

			}

			foreach (var item in creditsData.raids)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.User.ToLower()))
				{
					vs = stats[item.User.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.raid++;
				vs.raidViewers += item.Viewers;

				if (stats.ContainsKey(item.User.ToLower()))
				{
					stats[item.User.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.User.ToLower(), vs);
				}
			}
			//TODO : RaidOut

			List<string> dailies = await Database.instance.GetAllDailyRedeemUsers();
			foreach (var item in dailies)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.ToLower()))
				{
					vs = stats[item];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.stampDaily = 1;

				if (stats.ContainsKey(item.ToLower()))
				{
					stats[item.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.ToLower(), vs);
				}
			}

			foreach (var item in creditsData.subgifts)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.Key.ToLower()))
				{
					vs = stats[item.Key.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.stampSub = item.Value;

				if (stats.ContainsKey(item.Key.ToLower()))
				{
					stats[item.Key.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.Key.ToLower(), vs);
				}
			}

			foreach (var item in creditsData.subs)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.User.ToLower()))
				{
					vs = stats[item.User.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.stampSub = item.Months;

				if (stats.ContainsKey(item.User.ToLower()))
				{
					stats[item.User.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.User.ToLower(), vs);
				}
			}

			foreach (var item in creditsData.happyHourPaints)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.Key.ToLower()))
				{
					vs = stats[item.Key.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.taches = item.Value;

				if (stats.ContainsKey(item.Key.ToLower()))
				{
					stats[item.Key.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.Key.ToLower(), vs);
				}
			}

			foreach (var item in creditsData.vips)
			{
				ViewerStats vs;
				if (stats.ContainsKey(item.ToLower()))
				{
					vs = stats[item.ToLower()];
				}
				else
				{
					vs = new ViewerStats();
				}

				vs.vip = 1;

				if (stats.ContainsKey(item.ToLower()))
				{
					stats[item.ToLower()] = vs;
				}
				else
				{
					stats.Add(item.ToLower(), vs);
				}
			}

			//stats.Add("TEST", new ViewerStats() { messages = 10, ouates = 1500, stampDaily=1, taches=25 });

			foreach (var item in stats)
			{
				Database.instance.UpdateViewerStats(item.Key, item.Value, date);
			}

			return true;
		}
		catch (Exception e)
		{
			Logger.LogError(e.Message);
		}
		return false;
	}

	private IEnumerator ShowSteamAchievement(Sprite sprite, string title, string description, float duration)
	{
		steamAchievementImage.sprite = sprite;
		steamAchievementTitle.text = title;
		steamAchievementDescription.text = description;

		while(steamAchievement.anchoredPosition.y< steamAchievementUpPosition)
		{
			steamAchievement.anchoredPosition += Vector2.up * steamAchievementMoveSpeed * Time.deltaTime;
			yield return null;
		}

		yield return new WaitForSeconds(duration);

		while (steamAchievement.anchoredPosition.y > steamAchievementDownPosition)
		{
			steamAchievement.anchoredPosition -= Vector2.up * steamAchievementMoveSpeed * Time.deltaTime;
			yield return null;
		}
	}

	private void UpdateStreamEndTimer(int totalSubs)
	{
		int hour = 21;
		string minutes = "00";
		minutes = totalSubs % 2 == 0 ? "00" : "30";
		hour += totalSubs / 2;

		streamEndTimer.text = $"Fin du live : {hour}:{minutes}";
	}
}
