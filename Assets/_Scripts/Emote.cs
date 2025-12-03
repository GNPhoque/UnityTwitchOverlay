using DG.Tweening;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Emote : MonoBehaviour
{
	[SerializeField]
	private Rigidbody2D rb;
	[SerializeField]
	private TextMeshProUGUI text;
	[SerializeField]
	private Image image;
	[SerializeField]
	private GifPlayer gifPlayer;
	[SerializeField]
	private float minForce;
	[SerializeField]
	private float maxForce;
	[SerializeField]
	private float minDelay;
	[SerializeField]
	private float maxDelay;

	public void SetText(string value)
	{
		text.text = value;
	}

	public void SetSprite(Sprite sprite)
	{
		image.sprite = sprite;
	}

	public void SetGif(List<UniGif.GifTexture> data, int width, int height)
	{
		gifPlayer.LoadGif(data, width, height);
		gifPlayer.Play();
	}

	public void Move()
	{
		rb.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(minForce, maxForce), ForceMode2D.Impulse);
		StartCoroutine(Fade());
	}

	private IEnumerator Fade()
	{
		yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

		float opacity = 1f;
		Color color = Color.white;

		while (opacity > 0)
		{
			if(image) image.color = color;
			if(gifPlayer) gifPlayer.SetColor(color);
			text.color = color;

			yield return null;
			opacity -= Time.deltaTime;
			color = new Color(1f, 1f, 1f, opacity);
		}

		Destroy(gameObject);
	}
}
