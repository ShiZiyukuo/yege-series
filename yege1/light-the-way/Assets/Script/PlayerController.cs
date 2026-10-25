using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 3f, jump = 9f;
    public LayerMask groundLayer;
    public string stopAnime="PlayerStop", moveAnime="PlayerMove", jumpAnime="PlayerJump", goalAnime="PlayerGoal", deadAnime="PlayerOver";
    public static string gameState="playing";
    public int score;
    public float jumpBuffer=.12f, coyoteTime=.10f;
    Rigidbody2D rb; Animator anim; SpriteRenderer sprite; CapsuleCollider2D capsule;
    float axis, requested=-100, groundedAt=-100, suppressGroundUntil=-100;
    bool touch, wasGrounded, frozen; string pose; Vector2 frozenVelocity;
    RaycastHit2D[] hits=new RaycastHit2D[8];
    void Awake() { rb=GetComponent<Rigidbody2D>(); anim=GetComponent<Animator>(); sprite=GetComponent<SpriteRenderer>(); capsule=GetComponent<CapsuleCollider2D>(); rb.interpolation=RigidbodyInterpolation2D.Interpolate; }
    void Start() { if(!AdventureFlow.Current) gameState="playing"; PlayPose(stopAnime); }
    bool Playing { get { return AdventureFlow.Current ? AdventureFlow.Current.IsPlaying : gameState=="playing"; } }
    void Update()
    {
        if(!Playing) return;
        if(!touch) axis=Input.GetAxisRaw("Horizontal");
        if(Input.GetButtonDown("Jump")) Jump();
        if(Mathf.Abs(axis)>.01f) sprite.flipX=axis<0;
    }
    void FixedUpdate()
    {
        if(!Playing || frozen) return;
        bool ground=Time.time>=suppressGroundUntil && rb.velocity.y<=.15f && CheckOnGround();
        if(ground) groundedAt=Time.time;
        rb.velocity=new Vector2(axis*speed,rb.velocity.y);
        if(Time.time-requested<=jumpBuffer && Time.time-groundedAt<=coyoteTime)
        {
            rb.velocity=new Vector2(rb.velocity.x,0); rb.AddForce(Vector2.up*jump,ForceMode2D.Impulse);
            requested=groundedAt=-100; suppressGroundUntil=Time.time+.13f; ground=false;
        }
        if(ground&&!wasGrounded && AdventureFlow.Current) AdventureFlow.Current.Burst(transform.position+Vector3.down*.8f,new Color(.95f,.89f,.69f,.5f),4);
        wasGrounded=ground;
        PlayPose(ground?(Mathf.Abs(axis)>.01f?moveAnime:stopAnime):jumpAnime);
    }
    bool CheckOnGround()
    {
        if(!capsule) return false;
        Bounds b=capsule.bounds;
        var filter=new ContactFilter2D(); filter.SetLayerMask(groundLayer); filter.useTriggers=false;
        int n=Physics2D.BoxCast(new Vector2(b.center.x,b.min.y+.08f),new Vector2(b.size.x*.72f,.06f),0,Vector2.down,filter,hits,.20f);
        for(int i=0;i<n;i++) if(hits[i].collider && hits[i].normal.y>.55f && hits[i].collider!=capsule) return true;
        return false;
    }
    public void Jump() { if(Playing) requested=Time.time; }
    public void SetAxis(float h,float v) { axis=Mathf.Clamp(h,-1,1); touch=Mathf.Abs(axis)>.01f; }
    public void ClearInput() { axis=0; touch=false; requested=groundedAt=-100; wasGrounded=false; }
    public void SetFrozen(bool value)
    {
        if(rb)
        {
            if(value&&!frozen)frozenVelocity=rb.velocity;
            rb.simulated=!value;
            rb.velocity=value?Vector2.zero:(frozen?frozenVelocity:rb.velocity);
        }
        frozen=value;if(capsule)capsule.enabled=true;
    }
    public void ResetMotion(){frozenVelocity=Vector2.zero;if(rb)rb.velocity=Vector2.zero;ClearInput();}
    public void PlayPose(string value) { if(pose==value) return; pose=value; if(anim) anim.Play(value); }
    void OnDisable() { ClearInput(); }
    void OnApplicationFocus(bool focus) { if(!focus) ClearInput(); }
    void OnTriggerEnter2D(Collider2D c) { Hit(c.gameObject); }
    void OnCollisionEnter2D(Collision2D c) { Hit(c.gameObject); }
    void Hit(GameObject obj)
    {
        if(!Playing) return;
        var collectible=obj.GetComponent<AdventureCollectible>(); if(collectible) { collectible.Take(); return; }
        if(obj.CompareTag("Dead")) GameOver();
        else if(obj.CompareTag("Goal")) Goal();
        else if(obj.CompareTag("ScoreItem")) { var item=obj.GetComponent<ItemData>(); if(item) {score+=item.value; item.enabled=false;} obj.SetActive(false); }
    }
    public void Goal() { if(!Playing)return; if(AdventureFlow.Current){ AdventureFlow.Current.RequestExit();return;} PlayPose(goalAnime); gameState="gameclear";rb.velocity=Vector2.zero; }
    public void GameOver() { if(!Playing)return; if(AdventureFlow.Current){ AdventureFlow.Current.Die();return;} PlayPose(deadAnime);gameState="gameover";rb.velocity=Vector2.zero;capsule.enabled=false;rb.AddForce(Vector2.up*5,ForceMode2D.Impulse); }
}
