using UnityEngine;

public class AdventureParticle : MonoBehaviour
{
    public Vector3 velocity;
    public float life = .7f;
    public bool ambient;
    float age;
    SpriteRenderer sr;
    Color color;
    Vector3 origin;
    void Start() { sr = GetComponent<SpriteRenderer>(); color = sr.color; origin = transform.position; }
    void Update()
    {
        if (ambient)
        {
            transform.position = origin + new Vector3(Mathf.Sin(Time.time * .27f + origin.x) * .7f, Mathf.Sin(Time.time * .55f + origin.y) * .35f, 0);
            sr.color = new Color(color.r, color.g, color.b, (.25f + .22f * Mathf.Sin(Time.time + origin.x)));
            return;
        }
        age += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity += Vector3.down * Time.deltaTime * 1.8f;
        sr.color = new Color(color.r, color.g, color.b, color.a * (1 - age / life));
        transform.localScale *= 1 - Time.deltaTime * .6f;
        if (age >= life) Destroy(gameObject);
    }
}
