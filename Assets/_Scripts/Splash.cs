using UnityEngine;

public class Splash : MonoBehaviour
{
    public bool markedForDestruction;

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (markedForDestruction || !collision.gameObject.CompareTag("Clean"))
		{
			return;
		}

		markedForDestruction = true;
		StartCoroutine(WebSocketInteractions.instance.RemoveSplashWithRaclette(this));
	}
}
