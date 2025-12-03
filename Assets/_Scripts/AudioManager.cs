using UnityEngine;

public class AudioManager : MonoBehaviour
{
	[SerializeField]
	private AudioSource sfx;

	[SerializeField]
	private AudioClip stamp;
	[SerializeField]
	[Range(0f, 1f)]
	private float stampVolume;

	public static AudioManager instance;

	private void Awake()
	{
		if (instance)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
	}

	public void PlayStamp()
	{
		sfx.PlayOneShot(stamp, stampVolume);
	}
}
