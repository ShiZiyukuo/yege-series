using UnityEngine;

public class AdventureParallax : MonoBehaviour
{
    public float factor = .3f;
    Vector3 initial;
    float cameraX;
    void Start() { initial = transform.position; cameraX = Camera.main.transform.position.x; }
    void LateUpdate()
    {
        if (Camera.main) transform.position = initial + Vector3.right * (Camera.main.transform.position.x - cameraX) * factor;
    }
}
