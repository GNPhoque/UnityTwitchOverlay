using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LoyaltyCardData
{
	public List<string> redeemedUsers = new List<string>();
	public List<LoyaltyCardUser> users = new List<LoyaltyCardUser>();
}


[Serializable]
public class LoyaltyCardUser
{
	public string username;
	public string avatarUrl;
	public int totalStamps;
	public int currentCardStamps;
	public int completedCards;
	public Vector2[] currentCardStampsPositions = new Vector2[12];
	public float[] currentCardStampsRotations = new float[12];
}