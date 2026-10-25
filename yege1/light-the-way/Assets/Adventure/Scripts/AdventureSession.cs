using System.Collections.Generic;

/// <summary>One journey. Checkpoint retries keep discoveries; chapter restart restores its entry.</summary>
public static class AdventureSession
{
    public static readonly HashSet<string> Collected = new HashSet<string>();
    public static readonly HashSet<string> Activated = new HashSet<string>();
    public static readonly HashSet<int> Completed = new HashSet<int>();
    static HashSet<string> entryItems = new HashSet<string>();
    static HashSet<string> entrySwitches = new HashSet<string>();
    static HashSet<int> entryCompleted = new HashSet<int>();
    static int entryGems;
    static float entrySeconds;
    public static int Gems;
    public static int Deaths;
    public static float Seconds;
    public static int Chapter = -1;
    public static int Stars { get { int n = 0; for (int i = 0; i < 3; i++) if (Collected.Contains("star-" + i)) n++; return n; } }
    public static int Stamps { get { int n = 0; for (int i = 0; i < 3; i++) if (Collected.Contains("stamp-" + i)) n++; return n; } }
    public static bool Add(string id) { return !string.IsNullOrEmpty(id) && Collected.Add(id); }
    public static void Reset()
    {
        Collected.Clear(); Activated.Clear(); Completed.Clear(); Gems = Deaths = 0; Seconds = 0; Chapter = -1;
        GameManager.totalScore = 0;
    }
    public static void Enter(int chapter)
    {
        if (Chapter == chapter) return;
        Chapter = chapter;
        entryItems = new HashSet<string>(Collected); entrySwitches = new HashSet<string>(Activated);
        entryCompleted = new HashSet<int>(Completed); entryGems = Gems; entrySeconds = Seconds;
    }
    public static void RestartChapter()
    {
        Collected.Clear(); Collected.UnionWith(entryItems);
        Activated.Clear(); Activated.UnionWith(entrySwitches);
        Completed.Clear(); Completed.UnionWith(entryCompleted);
        Gems = entryGems; Seconds = entrySeconds;
    }
}
