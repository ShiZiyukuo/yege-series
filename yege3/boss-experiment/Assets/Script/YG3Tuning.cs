using UnityEngine;

/// <summary>
/// 战斗数值与素材路径总表。所有可调参数集中在这里，方便统一调平衡。
/// </summary>
public static class YG3Tuning
{
    // ---------- 路径 ----------
    public const string DIR_IMG = "Assets/Images/";
    public const string DIR_PLAYER = "Assets/Player/";
    public const string DIR_FX = "Assets/Images/Fx/";
    public const string DIR_SND = "Assets/Sounds/";
    public const string DIR_PFB = "Assets/Prefab/";

    // ---------- 层 ----------
    public const int LAYER_GROUND = 8;
    public const int LAYER_ENEMY = 9;
    public const int LAYER_BOSS = 10;
    public const int LAYER_PLAYER_ATTACK = 11;
    public const int LAYER_BOSS_ATTACK = 12;

    // ---------- 玩家 ----------
    public const float PLAYER_SPEED = 4.2f;
    public const float PLAYER_JUMP = 14.1f;   // 高度约 3.9 单位（可越过刺猬顶 ~1.3），滞空 ~1.1s
    public const float PLAYER_GRAVITY = 2.6f;   // 重力倍率：加重后跳跃干脆有力（滞空 ~1.2s 而非 1.9s）
    public const int PLAYER_MAX_HP = 6;
    public const float PLAYER_POISE = 60f;          // 受击架势上限
    public const float PLAYER_POISE_REGEN = 30f;    // 架势每秒回复
    public const float PLAYER_INVULN = 0.9f;        // 受击后无敌秒数
    public const float PLAYER_STUN = 0.4f;          // 架势破硬直秒数

    // ---------- 翻滚（纯位移，无无敌帧、无冷却、不耗耐力） ----------
    public const float ROLL_SPEED = 8.5f;
    public const float ROLL_TIME = 0.30f;

    // ---------- 耐力（仅格挡消耗） ----------
    public const float STAMINA_MAX = 100f;
    public const float STAMINA_BLOCK_HIT = 15f;     // 挡下一次的消耗
    public const float STAMINA_HOLD_PER_SEC = 10f;  // 持盾每秒消耗
    public const float STAMINA_REGEN = 25f;         // 每秒回复
    public const float STAMINA_REGEN_DELAY = 0.8f;  // 停止消耗后多久开始回复
    public const float GUARD_BREAK_STUN = 1.0f;     // 耐力耗尽硬直

    // ---------- 格挡 / 弹反 ----------
    public const float PARRY_WINDOW = 0.13f;        // 按下 L 后的完美弹反窗口
    public const float BLOCK_DAMAGE_RATIO = 0.3f;   // 普通格挡承伤比例
    public const float BLOCK_KNOCK_RATIO = 0.3f;

    // ---------- 挥剑（J，三段连击） ----------
    public const float SWORD1_DAMAGE = 1;
    public const float SWORD2_DAMAGE = 1;
    public const float SWORD3_DAMAGE = 2;
    public const float SWORD1_POISE = 10f;          // 对 Boss 架势伤害
    public const float SWORD2_POISE = 10f;
    public const float SWORD3_POISE = 18f;
    public const float SWORD_RANGE = 2.4f;          // 判定盒宽度（OverlapBox 取全宽）
    public const float SWORD_HEIGHT = 1.6f;         // 判定盒高度
    public const float SWORD_KNOCKBACK = 4f;
    public const float COMBO_RESET = 0.6f;          // 连段延续窗口
    public static readonly float[] SWORD_WINDUP = { 0.10f, 0.08f, 0.14f };
    public static readonly float[] SWORD_ACTIVE = { 0.07f, 0.07f, 0.10f };
    public static readonly float[] SWORD_RECOVER = { 0.17f, 0.17f, 0.30f };

    // ---------- 射箭（K，无限箭） ----------
    public const float ARROW_COOLDOWN = 0.35f;
    public const float ARROW_SPEED = 13f;
    public const float ARROW_DAMAGE = 1f;
    public const float ARROW_POISE = 6f;
    public const float ARROW_LIFE = 1.6f;

    // ---------- 刺猬 ----------
    public const float HEDGEHOG_HP = 2f;
    public const float HEDGEHOG_SPEED = 1.5f;
    public const float HEDGEHOG_CHASE_SPEED = 2.4f;   // 慢于玩家(4.2)，玩家可拉开距离
    public const float HEDGEHOG_REACTION = 6.5f;    // 发现玩家距离
    public const float HEDGEHOG_CURL_RANGE = 1.6f;  // 进入滚刺距离
    public const float HEDGEHOG_ROLL_SPEED = 4.3f;   // 略快于玩家，保留压迫感但可控
    public const float HEDGEHOG_ROLL_TIME = 1.1f;   // 一次滚刺持续
    public const float HEDGEHOG_CONTACT_DMG = 1f;
    public const float HEDGEHOG_CONTACT_POISE = 15f;
    public const float HEDGEHOG_SCORE = 100;

    // ---------- 金之祖巫 ----------
    public const float BOSS_MAX_HP = 60f;
    public const float BOSS_TRIGGER_DISTANCE = 9f;  // 进入战斗的距离
    public const float BOSS_GROGGY = 1.8f;          // 破防硬直
    public const float BOSS_FLINCH = 0.9f;          // 弹反踉跄
    public const float BOSS_GROGGY_TAKE_RATIO = 1.5f;
    public const float BOSS_EXECUTE_RATIO = 3.0f;   // 处决伤害倍率
    public const float BOSS_PARRY_POSTURE = 30f;    // 完美弹反架势伤害
    public static readonly float[] BOSS_BREAK_THRESHOLD = { 100f, 130f, 160f };
    public static readonly float[] BOSS_ACT_INTERVAL = { 2.2f, 1.7f, 1.2f };
    public static readonly float[] BOSS_TELEGRAPH = { 0.6f, 0.5f, 0.35f };

    // 金针齐射
    public const int VOLLEY_COUNT_BASE = 5;         // 一波金针数量（阶段加成）
    public const float VOLLEY_SPREAD = 55f;         // 扇形总角度
    public const float VOLLEY_SPEED = 7.5f;
    public const float VOLLEY_DAMAGE = 1f;
    public const float VOLLEY_POISE = 8f;
    public const float VOLLEY_SLOW = 1.5f;          // 命中迟缓秒数

    // 震波
    public const float SHOCK_SPEED = 6.0f;
    public const float SHOCK_DAMAGE = 2f;
    public const float SHOCK_POISE = 100f;          // 必破架势
    public const float SHOCK_LIFE = 3.0f;

    // 召唤
    public const int SUMMON_MAX_ALIVE = 3;          // 场上刺猬上限

    // ---------- 演出 ----------
    public const float HITSTOP_LIGHT = 0.05f;
    public const float HITSTOP_PARRY = 0.12f;
    public const float HITSTOP_EXECUTE = 0.25f;
    public const float SHAKE_LIGHT = 0.08f;
    public const float SHAKE_HEAVY = 0.22f;

    // ---------- 音效 ----------
    public const string SE_SHOOT = "se_shoot";
    public const string SE_DAMAGE = "se_damage";
    public const string SE_KILLED = "se_killed";
    public const string SE_BOSS_KILLED = "se_bosskilled";
    public const string SE_GET = "se_get";
    public const string SE_BUTTON = "se_button";
    public const string BGM_TITLE = "BGM_title";
    public const string BGM_GAME = "BGM_game_00";
    public const string BGM_BOSS = "BGM_boss";
    public const string ME_CLEAR = "ME_Clear";
    public const string ME_GAME_OVER = "ME_GameOver";
}
