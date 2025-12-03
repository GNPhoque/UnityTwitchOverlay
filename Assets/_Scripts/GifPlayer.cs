using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum GifPlayerStatus
{
	None,
	Loading,
	Ready,
	Playing,
}

public class GifPlayer : MonoBehaviour
{
	[SerializeField]
	private RawImage image;
	[SerializeField]
	private UniGifImageAspectController aspectController;

	private List<UniGif.GifTexture> gifTextures;

	public GifPlayerStatus status = GifPlayerStatus.None;

	private int gifTextureIndex;
	private float delayTime;

	private void Update()
	{
		switch (status)
		{
			case GifPlayerStatus.None:
				break;

			case GifPlayerStatus.Loading:
				break;

			case GifPlayerStatus.Ready:
				break;

			case GifPlayerStatus.Playing:
				if (image == null || gifTextures == null || gifTextures.Count <= 0)
				{
					return;
				}
				if (delayTime > Time.time)
				{
					return;
				}
				// Change texture
				gifTextureIndex++;
				if (gifTextureIndex >= gifTextures.Count)
				{
					gifTextureIndex = 0;
				}
				image.texture = gifTextures[gifTextureIndex].m_texture2d;
				delayTime = Time.time + gifTextures[gifTextureIndex].m_delaySec;
				break;
			default:
				break;
		}
	}

	public static IEnumerator SetGifFromUrlCoroutine(string url, Action<List<UniGif.GifTexture>> callback)
	{
		if (string.IsNullOrEmpty(url))
		{
			Debug.LogError("URL is nothing.");
			yield break;
		}

		string path;
		if (url.StartsWith("http"))
		{
			// from WEB
			path = url;
		}
		else
		{
			// from StreamingAssets
			path =$"file:///{Application.streamingAssetsPath}/{url}";
		}

		// Load file
		using (WWW www = new WWW(path))
		{
			yield return www;

			if (string.IsNullOrEmpty(www.error) == false)
			{
				Debug.LogError("File load error.\n" + www.error);
				yield break;
			}

			// Get GIF textures
			yield return WebSocketInteractions.instance.StartCoroutine(UniGif.GetTextureListCoroutine(www.bytes, (gifTexList, loopCount, width, height) =>
			{
				if (gifTexList != null)
				{
					callback(gifTexList);
				}
				else
				{
					Debug.LogError("Gif texture get error.");
				}
			},
			 FilterMode.Bilinear, TextureWrapMode.Clamp));
		}
	}

	/// <summary>
	/// Clear GIF texture
	/// </summary>
	public void Clear()
	{
		if (image != null)
		{
			image.texture = null;
		}

		if (gifTextures != null)
		{
			for (int i = 0; i < gifTextures.Count; i++)
			{
				if (gifTextures[i] != null)
				{
					if (gifTextures[i].m_texture2d != null)
					{
						Destroy(gifTextures[i].m_texture2d);
						gifTextures[i].m_texture2d = null;
					}
					gifTextures[i] = null;
				}
			}
			gifTextures.Clear();
			gifTextures = null;
		}

		status = GifPlayerStatus.None;
	}

	public void Play()
	{
		if (status != GifPlayerStatus.Ready)
		{
			Debug.LogWarning("State is not READY.");
			return;
		}
		if (image == null || gifTextures == null || gifTextures.Count <= 0)
		{
			Debug.LogError("Raw Image or GIF Texture is nothing.");
			return;
		}
		status = GifPlayerStatus.Playing;
		image.texture = gifTextures[0].m_texture2d;
		delayTime = Time.time + gifTextures[0].m_delaySec;
		gifTextureIndex = 0;
	}

	public void LoadGif(List<UniGif.GifTexture> textures, int width, int height)
	{
		Clear();
		gifTextures = textures;
		aspectController.FixAspectRatio(width, height);
		status = GifPlayerStatus.Ready;
	}

	public void SetColor(Color color)
	{
		image.color = color;
	}
}
