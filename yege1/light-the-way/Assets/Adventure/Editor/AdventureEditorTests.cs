using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AdventureEditorTests
{
    public static void Run()
    {
        AdventureBuilder.Validate();
        EditorSceneManager.OpenScene("Assets/Adventure/Scenes/AdventureTitle.scene");
        SessionState.SetBool("AdventureSmokeRequested",true);
        EditorApplication.playModeStateChanged += OnPlay;
        EditorApplication.isPlaying = true;
    }
    static void OnPlay(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged-=OnPlay;
        new UnityEngine.GameObject("AdventureSmokeRunner").AddComponent<AdventureSmokeRunner>();
    }
}

// Re-register after the play-mode domain reload. This is Editor-only.
[InitializeOnLoad]
public static class AdventureSmokeBootstrap
{
    static AdventureSmokeBootstrap()
    {
        if(!SessionState.GetBool("AdventureSmokeRequested",false))return;
        EditorApplication.playModeStateChanged += change =>
        {
            if(change!=PlayModeStateChange.EnteredPlayMode)return;
            SessionState.SetBool("AdventureSmokeRequested",false);
            if(!UnityEngine.Object.FindObjectOfType<AdventureSmokeRunner>())new UnityEngine.GameObject("AdventureSmokeRunner").AddComponent<AdventureSmokeRunner>();
        };
    }
}
