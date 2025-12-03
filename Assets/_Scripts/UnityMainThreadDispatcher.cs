using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Permet d'exécuter du code sur le thread principal de Unity.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
	// File d’actions à exécuter sur le thread Unity
	private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

	public static UnityMainThreadDispatcher instance;

	private void Awake()
	{
		if (instance != null)
		{
			return;
		}

		instance = this;
		DontDestroyOnLoad(this);
	}

	/// <summary>
	/// Ajoute une action à exécuter sur le thread principal
	/// </summary>
	public void Enqueue(Action action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		_executionQueue.Enqueue(action);
	}

	// Appelé à chaque frame sur le thread principal
	private void Update()
	{
		while (_executionQueue.TryDequeue(out var action))
		{
			action?.Invoke();
		}
	}
}