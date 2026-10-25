using UnityEngine;
public class CannonController : MonoBehaviour
{
    public GameObject objPrefab;public float delayTime=3,fireSpeedX=-4,fireSpeedY,length=8;
    Transform player,gate;SpriteRenderer sr;float passed;Color color;
    void Start(){var p=GameObject.FindGameObjectWithTag("Player");if(p)player=p.transform;gate=transform.Find("gate");sr=GetComponent<SpriteRenderer>();color=sr.color;}
    public void ResetCannon(){passed=0;if(sr)sr.color=color;}
    void Update()
    {
        if(!AdventureFlow.WorldPlaying||!player||!gate||!objPrefab)return;
        if(Vector2.Distance(transform.position,player.position)>length){ResetCannon();return;}
        passed+=Time.deltaTime;float warn=Mathf.Clamp01((passed-(delayTime-.5f))/.5f);sr.color=Color.Lerp(color,new Color(1,.57f,.26f),warn);
        if(passed>=delayTime){passed=0;sr.color=color;var obj=Instantiate(objPrefab,gate.position,Quaternion.identity);var rb=obj.GetComponent<Rigidbody2D>();if(rb)rb.velocity=new Vector2(fireSpeedX,fireSpeedY);}
    }
}
