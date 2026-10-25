using UnityEngine;
using UnityEngine.EventSystems;

public class AdventureDirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float axis;
    int pointer = int.MinValue;
    PlayerController player;
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer != int.MinValue) return;
        pointer = e.pointerId;
        player = FindObjectOfType<PlayerController>();
        if (player) player.SetAxis(axis, 0);
    }
    public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) Release(); }
    void OnDisable() { Release(); }
    void OnApplicationFocus(bool focus) { if (!focus) Release(); }
    void Release() { pointer = int.MinValue; if (player) player.SetAxis(0, 0); }
}
