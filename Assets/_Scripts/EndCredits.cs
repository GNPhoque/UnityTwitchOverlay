using System;
using System.Collections.Generic;

[Serializable]
public class CreditsData
{
	public List<int> hypeTrains = new List<int>();
	public List<RaidData> raids = new List<RaidData>();
	public List<SubData> subs = new List<SubData>();
	public Dictionary<string, int> subgifts = new Dictionary<string, int>();
	public List<string> follows = new List<string>();
	public Dictionary<string, int> bits = new Dictionary<string, int>();
	public List<string> vips = new List<string>();
	public Dictionary<string, int> userMessages = new Dictionary<string, int>();
	public Dictionary<string, int> happyHourPaints = new Dictionary<string, int>();
}

public class RaidData
{
	public string User { get; set; }
	public int Viewers { get; set; }
}

public class SubData
{
	public string User { get; set; }
	public string Tier { get; set; }
}
