using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Logger : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI logsText;
	[SerializeField]
	float timeBeforeHideLogs;

	private Coroutine hideLogsCoroutine;

	public static Logger instance;

	private void Awake()
	{
		if (instance != null)
		{
			return;
		}

		instance = this;
	}

	public static void Log(string message)
	{
		UnityMainThreadDispatcher.instance.Enqueue(() =>
		{
			Debug.Log(message);
			instance.logsText.text += $"{Environment.NewLine}{message}";
		});
	}

	public static void LogError(string message)
	{
		UnityMainThreadDispatcher.instance.Enqueue(() =>
		{
			Debug.LogError(message);
			instance.logsText.text += $"{Environment.NewLine}<color=red>{message}</color>";
			instance.ShowLogs();
		});
	}

	public void ShowLogs(bool show = true, bool permanent = false)
	{
		UnityMainThreadDispatcher.instance.Enqueue(() =>
		{
			logsText.gameObject.SetActive(show);

			if (hideLogsCoroutine != null)
			{
				StopCoroutine(hideLogsCoroutine);
			}

			if (show && !permanent)
			{
				hideLogsCoroutine = StartCoroutine(HideLogsAfterDelay());
			}
		});
	}

	private IEnumerator HideLogsAfterDelay()
	{
		yield return new WaitForSeconds(timeBeforeHideLogs);
		logsText.gameObject.SetActive(false);
	}
}
