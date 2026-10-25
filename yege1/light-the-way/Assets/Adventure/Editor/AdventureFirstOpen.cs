using System;
using System.IO;
using UnityEditor;

/// <summary>Opening the deliverable assembles the actual scenes once. Never overwrites later edits.</summary>
[InitializeOnLoad]
public static class AdventureFirstOpen
{
    static AdventureFirstOpen()
    {
        // Batch entry points own their lifecycle; don't run two builders at once.
        if (UnityEngine.Application.isBatchMode) return;
        EditorApplication.delayCall += CreateOnce;
    }
    static void CreateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += CreateOnce; return; }
        if ((File.Exists("Assets/Adventure/Scenes/AdventureTitle.scene") && !AdventureBuilder.NeedsLayoutUpgrade) || SessionState.GetBool("AdventureFirstOpenAttemptV2",false)) return;
        SessionState.SetBool("AdventureFirstOpenAttemptV2",true);
        try { AdventureBuilder.Initialize(); }
        catch (Exception e) { UnityEngine.Debug.LogException(e); }
    }
}
