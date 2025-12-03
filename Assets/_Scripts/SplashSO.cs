using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Splash", menuName = "Splash")]
public class SplashSO : ScriptableObject
{
	public Sprite sprite;
	public string gifPath;
	public List<Color> colorList;
	public int width;
	public int height;
	public int duration;
	public int weight;
	public bool isGif;
}
