using UnityEngine;
public class CameraManager : MonoBehaviour
{
    public float leftLimit,rightLimit,topLimit,bottomLimit;
    public bool isForceScrollX,isForceScrollY;
    public float forceScrollSpeedX=.5f,forceScrollSpeedY=.5f;
    public GameObject subScreen;
    public bool cinematic;
    public float smoothTime=.16f,lookAhead=1.4f;
    Transform player;Vector3 velocity;Camera cam;float lastAspect;
    void Start(){cam=GetComponent<Camera>();var p=GameObject.FindGameObjectWithTag("Player");if(p)player=p.transform;FitBackground();}
    void LateUpdate()
    {
        if(!cam)cam=GetComponent<Camera>();if(Mathf.Abs(lastAspect-cam.aspect)>.01f)FitBackground();
        if(cinematic||!player)return;
        var rb=player.GetComponent<Rigidbody2D>();float look=rb?Mathf.Clamp(rb.velocity.x,-1,1)*lookAhead:0;
        Vector3 target=new Vector3(Mathf.Clamp(player.position.x+look,leftLimit,rightLimit),Mathf.Clamp(player.position.y*.35f,bottomLimit,topLimit),transform.position.z);
        if(isForceScrollX)target.x=transform.position.x+forceScrollSpeedX*Time.deltaTime;
        if(isForceScrollY)target.y=transform.position.y+forceScrollSpeedY*Time.deltaTime;
        transform.position=Vector3.SmoothDamp(transform.position,target,ref velocity,smoothTime);
    }
    public void Snap(){if(!player)return;velocity=Vector3.zero;transform.position=new Vector3(Mathf.Clamp(player.position.x,leftLimit,rightLimit),Mathf.Clamp(player.position.y*.35f,bottomLimit,topLimit),-10);}
    public void FitBackground(){var camera=GetComponent<Camera>();if(!camera||!subScreen)return;var sr=subScreen.GetComponent<SpriteRenderer>();if(!sr||!sr.sprite)return;float h=camera.orthographicSize*2;float s=Mathf.Max(h/sr.sprite.bounds.size.y,h*camera.aspect/sr.sprite.bounds.size.x)*1.06f;subScreen.transform.localScale=new Vector3(s,s,1);subScreen.transform.localPosition=new Vector3(0,0,10);lastAspect=camera.aspect;}
}
