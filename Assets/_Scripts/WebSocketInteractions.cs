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
	public async void DrawSplash(float left = -1, float top = -1, int duration = -1, string avatarUrl = "", string pseudo = "")
	{
		so = null;
		if (!string.IsNullOrEmpty(avatarUrl))
		{
			so = avatarSo;
			so.sprite = await loyaltyCard.GetUserAvatar(pseudo, avatarUrl, ELoyaltyCardType.Daily);
		}

		if(so == null)
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
		throughScreenImage.anchoredPosition = new Vector2(-2000, throughScreenImage.anchoredPosition.y);
		throughScreenImage.DOAnchorPosX(2000, 5f).SetEase(Ease.Linear);
	}

	public void Lurk(string user)
	{
		Vector2 spawnPosition = GetRandomPointOnScreeen();
		print(spawnPosition);
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

	public void SendFidDetails(string username)
	{
		string message = loyaltyCard.GetLoyaltyCardsStatusString(username);
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
}
