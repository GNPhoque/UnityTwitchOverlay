using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Emote : MonoBehaviour
{
	[SerializeField]
	private Rigidbody2D rb;
	[SerializeField]
	private Image image;
	[SerializeField]
	private float minForce;
	[SerializeField]
	private float maxForce;
	[SerializeField]
	private float minDelay;
	[SerializeField]
	private float maxDelay;

	public void Move()
	{
		rb.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(minForce, maxForce), ForceMode2D.Impulse);
		Invoke("Fade", Random.Range(minDelay, maxDelay));
	}

	private void Fade()
	{
		image.DOFade(0f, 1f).OnComplete(() => Destroy(gameObject));
	}
}
