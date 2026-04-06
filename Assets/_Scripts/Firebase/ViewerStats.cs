using Firebase.Firestore;

[FirestoreData]
public class ViewerStats
{
	[FirestoreProperty]
	public int messages { get; set; }
	[FirestoreProperty]
	public int ouates { get; set; }
	[FirestoreProperty]
	public int raid { get; set; }
	[FirestoreProperty]
	public int raidOut { get; set; }
	[FirestoreProperty]
	public int raidViewers { get; set; }
	[FirestoreProperty]
	public int stampDaily { get; set; }
	[FirestoreProperty]
	public int stampSub { get; set; }
	[FirestoreProperty]
	public int taches { get; set; }
	[FirestoreProperty]
	public int vip { get; set; }
}
