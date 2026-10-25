using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss 血条 + 架势槽：BossStage 顶部 UI。Boss 入战时滑入，死亡后收起。
/// </summary>
public class YG3BossHealthBar : MonoBehaviour
{
    public Image hpFill;
    public Image postureFill;
    public CanvasGroup group;
    public float showY = -40f;
    public float hideY = -120f;

    YG3JinZuWuBoss boss;
    RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        boss = FindObjectOfType<YG3JinZuWuBoss>();
        if (group != null)
        {
            group.alpha = 0f;
        }
        if (rt != null)
        {
            Vector2 p = rt.anchoredPosition;
            p.y = hideY;
            rt.anchoredPosition = p;
        }
    }

    public void Refresh()
    {
        if (boss == null)
        {
            boss = FindObjectOfType<YG3JinZuWuBoss>();
            if (boss == null)
            {
                return;
            }
        }

        if (boss.IsActive)
        {
            if (group != null && group.alpha < 1f)
            {
                group.alpha = Mathf.Min(1f, group.alpha + Time.deltaTime * 3f);
            }
            if (rt != null)
            {
                Vector2 p = rt.anchoredPosition;
                p.y = Mathf.Lerp(p.y, showY, Time.deltaTime * 6f);
                rt.anchoredPosition = p;
            }
            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(boss.HpRatio);
            }
            if (postureFill != null)
            {
                postureFill.fillAmount = Mathf.Clamp01(boss.PostureRatio);
            }
        }
        else if (group != null && group.alpha > 0f)
        {
            group.alpha = Mathf.Max(0f, group.alpha - Time.deltaTime * 3f);
        }
    }
}
