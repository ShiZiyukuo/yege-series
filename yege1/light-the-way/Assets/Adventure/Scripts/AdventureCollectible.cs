using UnityEngine;

public class AdventureCollectible : MonoBehaviour
{
    public string id;
    public enum Kind { Gem, Star, Stamp }
    public Kind kind;
    Vector3 origin;
    bool taken;
    void Start()
    {
        origin = transform.position;
        if (AdventureSession.Collected.Contains(id)) { taken = true; gameObject.SetActive(false); }
    }
    void Update()
    {
        if (taken) return;
        transform.position = origin + Vector3.up * Mathf.Sin(Time.time * 2.4f + origin.x) * .10f;
        if (kind != Kind.Gem) transform.Rotate(0, 0, Mathf.Sin(Time.time * 1.2f) * Time.deltaTime * 8);
    }
    public void Take()
    {
        if (taken || AdventureFlow.Current == null || !AdventureFlow.Current.IsPlaying) return;
        taken = true;
        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        if (AdventureSession.Add(id))
        {
            if (kind == Kind.Gem) AdventureSession.Gems++;
            AdventureFlow.Current.Collected(this);
        }
        gameObject.SetActive(false);
    }
}
