using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触屏按键输入缓冲：移动端按钮按下 → 置位缓冲，玩家 Update 里消费。
/// 键盘 J/K/L/Shift 与触屏按钮走同一入口。
/// </summary>
public static class YG3MobileInput
{
    public static bool pressAttack;
    public static bool pressBow;
    public static bool pressRoll;
    public static bool pressGuard;
    public static bool pressJump;
    public static bool GuardHeld;

    public static bool ConsumeAttack() { bool v = pressAttack; pressAttack = false; return v; }
    public static bool ConsumeBow() { bool v = pressBow; pressBow = false; return v; }
    public static bool ConsumeRoll() { bool v = pressRoll; pressRoll = false; return v; }
    public static bool ConsumeGuard() { bool v = pressGuard; pressGuard = false; return v; }
    public static bool ConsumeJump() { bool v = pressJump; pressJump = false; return v; }

    public static void ResetAll()
    {
        pressAttack = pressBow = pressRoll = pressGuard = pressJump = false;
        GuardHeld = false;
    }
}

/// <summary>移动端动作按钮（剑 / 弓 / 盾 / 翻滚 / 跳跃）。</summary>
public class YG3MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Kind { Attack, Bow, Roll, Guard, Jump }
    public Kind kind;

    public void OnPointerDown(PointerEventData data)
    {
        switch (kind)
        {
            case Kind.Attack: YG3MobileInput.pressAttack = true; break;
            case Kind.Bow: YG3MobileInput.pressBow = true; break;
            case Kind.Roll: YG3MobileInput.pressRoll = true; break;
            case Kind.Guard: YG3MobileInput.pressGuard = true; YG3MobileInput.GuardHeld = true; break;
            case Kind.Jump: YG3MobileInput.pressJump = true; break;
        }
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (kind == Kind.Guard)
        {
            YG3MobileInput.GuardHeld = false;
        }
    }
}
