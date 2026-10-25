using UnityEngine;

public class AdventureMarker : MonoBehaviour
{
    public enum Kind { Checkpoint, Hint, Vista }
    public Kind kind;
    public string message;
    public Transform spawn;
    public SpriteRenderer lightSprite;
    bool shown;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || AdventureFlow.Current == null || !AdventureFlow.Current.IsPlaying) return;
        if (kind == Kind.Checkpoint)
        {
            AdventureFlow.Current.Checkpoint(spawn ? spawn.position : transform.position + Vector3.up);
            if (lightSprite) lightSprite.color = new Color(1, .86f, .40f);
        }
        if (!shown) { shown = true; AdventureFlow.Current.Toast(message, 3.2f); }
    }
}
