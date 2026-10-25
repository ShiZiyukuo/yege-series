using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Tests lifecycle and actual physics. Teleported item checks are not a playthrough.</summary>
public class AdventureSmokeRunner : MonoBehaviour
{
    readonly List<string> report=new List<string>();
    bool failed;
    float watchdog;
    void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){failed=true;report.Add("ERROR: "+text);}}
    void Update(){watchdog+=Time.unscaledDeltaTime;if(watchdog>300){report.Add("FAIL: runtime smoke watchdog timeout");File.WriteAllLines("AdventureRuntimeChecks.txt",report);EditorApplication.Exit(1);}}
    void Check(bool condition,string name){report.Add((condition?"PASS: ":"FAIL: ")+name);if(!condition)failed=true;}
    IEnumerator Start()
    {
        yield return null;
        var title=AdventureFlow.Current;Check(title&&title.mode==AdventureFlow.Mode.Title,"title initializes");
        title.mainButton.onClick.Invoke();yield return null;yield return null;
        for(int chapter=0;chapter<3;chapter++)
        {
            var flow=AdventureFlow.Current;
            Check(flow&&flow.chapter==chapter,"chapter sequence "+chapter);
            flow.mainButton.onClick.Invoke();yield return new WaitForSeconds(.4f);
            var player=FindObjectOfType<PlayerController>();var rb=player.GetComponent<Rigidbody2D>();
            Check(flow.IsPlaying&&rb.simulated,"chapter playable "+chapter);
            // Real physics jump at the initial safe spawn: gain height, land, no permanent jump buffer.
            float y=player.transform.position.y;player.Jump();yield return new WaitForSeconds(.20f);
            Check(player.transform.position.y>y+.4f,"physical jump rises "+chapter);
            yield return new WaitForSeconds(1.1f);
            player.SetAxis(0,0);
            flow.Pause();Check(flow.state==AdventureFlow.State.Paused&&!rb.simulated,"pause freezes physics "+chapter);
            flow.mainButton.onClick.Invoke();Check(flow.IsPlaying&&rb.simulated,"pause resumes "+chapter);
            // Traverse the main route using movement and real jumps, without moving the transform.
            float routeTarget=88+chapter*10;
            float routeDeadline=Time.realtimeSinceStartup+75;
            while(player.transform.position.x<routeTarget && Time.realtimeSinceStartup<routeDeadline)
            {
                if(flow.IsPlaying){player.SetAxis(1,0);player.Jump();}
                yield return new WaitForFixedUpdate();
            }
            player.ClearInput();rb.velocity=new Vector2(0,rb.velocity.y);
            Check(player.transform.position.x>=routeTarget,"physical main route traversable "+chapter);
            if(player.transform.position.x<routeTarget)
            {
                File.WriteAllLines("AdventureRuntimeChecks.txt",report);
                Debug.LogError("[Adventure] Main route blocked at "+player.transform.position);
                EditorApplication.Exit(1);yield break;
            }
            var star=FindObjectsOfType<AdventureCollectible>().Single(i=>i.kind==AdventureCollectible.Kind.Star);
            var stamp=FindObjectsOfType<AdventureCollectible>().Single(i=>i.kind==AdventureCollectible.Kind.Stamp);
            // Contract checks deliberately invoke pickup; they do not claim route reachability.
            star.Take();star.Take();stamp.Take();
            Check(AdventureSession.Stars==chapter+1&&AdventureSession.Stamps==chapter+1,"pickup is unique "+chapter);
            if(chapter==1)
            {
                var sw=FindObjectOfType<SwitchAction>();
                player.transform.position=sw.transform.position;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                var platform=sw.targetMoveBlock.GetComponent<MovingBlock>();
                Check(sw.on&&platform.isCanMove,"ferry activated by switch");
                player.SetFrozen(true);var begin=platform.transform.position;
                yield return new WaitForSeconds(platform.times*2+platform.weight*2+.2f);
                Check(platform.isCanMove&&Vector3.Distance(begin,platform.transform.position)<2,"ferry returns and stays enabled");
                player.SetFrozen(false);
            }
            flow.Die();yield return new WaitForSecondsRealtime(1.1f);
            Check(flow.IsPlaying&&rb.simulated&&player.GetComponent<Collider2D>().enabled,"checkpoint restores actor "+chapter);
            Check(AdventureSession.Collected.Contains("star-"+chapter)&&AdventureSession.Collected.Contains("stamp-"+chapter),"retry keeps discoveries "+chapter);
            if(chapter==1) Check(FindObjectOfType<SwitchAction>().on&&FindObjectOfType<SwitchAction>().targetMoveBlock.GetComponent<MovingBlock>().isCanMove,"retry keeps ferry enabled");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                yield return new WaitForEndOfFrame();Directory.CreateDirectory("QA");ScreenCapture.CaptureScreenshot("QA/chapter"+(chapter+1)+".png");
            }
            if(chapter==2)flow.endingSeconds=.3f;
            flow.RequestExit();yield return new WaitForSecondsRealtime(1.8f);
        }
        Check(AdventureFlow.Current.mode==AdventureFlow.Mode.Journal,"ending reaches journal");
        Check(AdventureSession.Stars==3&&AdventureSession.Stamps==3,"journal records complete journey");
        AdventureFlow.Current.mainButton.onClick.Invoke();yield return null;yield return null;
        Check(AdventureSession.Stars==0&&AdventureSession.Stamps==0&&AdventureFlow.Current.chapter==0,"second journey resets progress");
        File.WriteAllLines("AdventureRuntimeChecks.txt",report);
        Debug.Log("[Adventure] RUNTIME_CHECKS_"+(failed?"FAIL":"PASS"));
        EditorApplication.Exit(failed?1:0);
    }
}
