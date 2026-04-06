using UnityEngine;

public class Splash : MonoBehaviour
{
    public bool markedForDestruction = false;

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (markedForDestruction || !collision.gameObject.CompareTag("Clean"))
		{
			return;
		}

		StartCoroutine(WebSocketInteractions.instance.RemoveSplashWithRaclette(this));
	}
}
