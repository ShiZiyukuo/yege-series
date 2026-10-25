using UnityEngine;
public class SwitchAction : MonoBehaviour
{
    public GameObject targetMoveBlock;
    public Sprite imageOn,imageOff;
    public bool on;
    public string switchId="ferry";
    void Start(){ if(AdventureFlow.Current)on=AdventureSession.Activated.Contains(switchId); Apply(); }
    void Apply(){var sr=GetComponent<SpriteRenderer>();if(sr)sr.sprite=on?imageOn:imageOff;if(targetMoveBlock){var block=targetMoveBlock.GetComponent<MovingBlock>();if(block){if(on)block.Move();else block.Stop();}}}
    void OnTriggerEnter2D(Collider2D c)
    {
        if(!c.CompareTag("Player")||!AdventureFlow.WorldPlaying)return;
        if(AdventureFlow.Current){if(on)return;on=true;AdventureSession.Activated.Add(switchId);Apply();AdventureFlow.Current.SwitchActivated(transform.position);}
        else{on=!on;Apply();}
    }
}
