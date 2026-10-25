using UnityEngine;
using UnityEngine.EventSystems;
public class VirtualPad : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
{
    public float MaxLength=70; public bool is4DPad;
    int pointer=int.MinValue; Vector2 origin; PlayerController player;
    void Start(){origin=GetComponent<RectTransform>().anchoredPosition;player=FindObjectOfType<PlayerController>();}
    public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Move(e);}
    public void OnDrag(PointerEventData e){if(pointer==e.pointerId)Move(e);}
    public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)Release();}
    void Move(PointerEventData e){Vector2 pos;var rt=GetComponent<RectTransform>();if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform,e.position,e.pressEventCamera,out pos))return;pos-=origin;if(!is4DPad)pos.y=0;pos=Vector2.ClampMagnitude(pos,MaxLength);rt.anchoredPosition=origin+pos;if(player)player.SetAxis(Mathf.Abs(pos.x)<8?0:Mathf.Sign(pos.x),0);}
    public void PadDown(){if(player)player.SetAxis(0,0);}
    public void PadDrag(){if(player)player.SetAxis(Input.GetAxisRaw("Horizontal"),0);}
    public void PadUp(){Release();}
    void Release(){pointer=int.MinValue;if(player)player.SetAxis(0,0);var rt=GetComponent<RectTransform>();if(rt)rt.anchoredPosition=origin;}
    void OnDisable(){Release();} void OnApplicationFocus(bool focus){if(!focus)Release();}
}
