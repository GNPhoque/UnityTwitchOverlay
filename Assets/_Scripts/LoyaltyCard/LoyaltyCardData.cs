using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LoyaltyCardData
{
	public List<string> redeemedUsers { get; set; }
	public List<LoyaltyCardUser> users { get; set; }
}


[Serializable]
[FirestoreData]
public class LoyaltyCardUser
{
	[FirestoreProperty]
	public string username { get; set; }
	[FirestoreProperty]
	public string avatarUrl { get; set; }
	[FirestoreProperty]
	public int totalStamps { get; set; }
	[FirestoreProperty]
	public int currentCardStamps { get; set; }
	[FirestoreProperty]
	public int completedCards { get; set; }
	[FirestoreProperty]
	public Stamp[] stamps {  get; set; }

	public override string ToString()
	{
		return $"username : {username}, avatarUrl : {avatarUrl}, totalStamps : {totalStamps}, currentCardStamps : {currentCardStamps}, completedCards : {completedCards} , stamps : {string.Join(", ", from t in stamps select $"{t.x}, {t.y}, {t.rotation}")}";
	}
}

[FirestoreData]
public struct Stamp
{
	public Stamp(float X, float Y, float ROT)
	{
		x = X;
		y = Y;
		rotation = ROT;
	}

	[FirestoreProperty]
	public float x { get; set; }
	[FirestoreProperty]
	public float y { get; set; }
	[FirestoreProperty]
	public float rotation { get; set; }
}