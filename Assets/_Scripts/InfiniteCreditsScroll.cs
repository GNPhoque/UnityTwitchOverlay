using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InfiniteCreditsScroll : MonoBehaviour
{
	public RectTransform scrollParent; // ex: CreditsParent
	public float scrollSpeed = 30f;
	private List<RectTransform> sections = new List<RectTransform>();
	private float totalHeight;

	void Start()
	{
		// Récupérer tous les TMP (ou RectTransform enfants)
		foreach (Transform child in scrollParent)
		{
			var rect = child as RectTransform;
			if (rect != null)
				sections.Add(rect);
		}

		// Calculer la hauteur totale
		totalHeight = 0f;
		foreach (var sec in sections)
		{
			totalHeight += sec.rect.height;
		}
	}

	void Update()
	{
		// Déplacer le parent vers le haut
		scrollParent.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

		// Si le premier bloc est complètement sorti par le haut, on le remet en bas
		var first = sections[0];
		if (first.anchoredPosition.y + scrollParent.anchoredPosition.y > totalHeight)
		{
			// trouver le dernier
			var last = sections[sections.Count - 1];
			float newY = last.anchoredPosition.y - first.rect.height;

			first.anchoredPosition = new Vector2(first.anchoredPosition.x, newY);

			// réordonner la liste
			sections.RemoveAt(0);
			sections.Add(first);
		}
	}
}