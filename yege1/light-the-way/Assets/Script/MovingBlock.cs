using UnityEngine;
public class MovingBlock : MonoBehaviour
{
    public float moveX,moveY,times=3f,weight=1f;
    public bool isMoveWhenOn,isCanMove=true;
    public bool requireSwitch;
    Vector3 origin; float phase,wait; bool reverse,initialized; Rigidbody2D rider;
    void Awake() { origin=transform.position; initialized=true; }
    void Start()
    {
        if(isMoveWhenOn||requireSwitch) isCanMove=false;
        if(requireSwitch && AdventureFlow.Current)
        {
            foreach(var sw in FindObjectsOfType<SwitchAction>())
                if(sw.targetMoveBlock==gameObject && AdventureSession.Activated.Contains(sw.switchId)) isCanMove=true;
        }
    }
    public void Move() { isCanMove=true; }
    public void Stop() { isCanMove=false; }
    public void ResetPlatform() { if(!initialized)return; transform.position=origin;phase=wait=0;reverse=false;rider=null; }
    void FixedUpdate()
    {
        if(!isCanMove||!AdventureFlow.WorldPlaying)return;
        if(wait>0){wait-=Time.fixedDeltaTime;return;}
        Vector3 before=transform.position;
        phase=Mathf.Min(1,phase+Time.fixedDeltaTime/Mathf.Max(.05f,times));
        transform.position=Vector3.Lerp(origin,origin+new Vector3(moveX,moveY,0),reverse?1-phase:phase);
        if(rider && rider.simulated && rider.velocity.y<=.2f) rider.position+=(Vector2)(transform.position-before);
        if(phase>=1){phase=0;reverse=!reverse;wait=Mathf.Max(0,weight);}
    }
    void OnCollisionStay2D(Collision2D c)
    {
        if(!c.gameObject.CompareTag("Player"))return;
        bool top=false;
        for(int i=0;i<c.contactCount;i++) if(c.GetContact(i).normal.y<-.55f)top=true;
        if(top){rider=c.rigidbody;if(isMoveWhenOn&&!requireSwitch)Move();} else if(rider==c.rigidbody)rider=null;
    }
    void OnCollisionExit2D(Collision2D c){if(c.rigidbody==rider)rider=null;}
    void OnValidate(){times=Mathf.Max(.05f,times);weight=Mathf.Max(0,weight);}
}
