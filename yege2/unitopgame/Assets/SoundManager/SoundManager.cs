using UnityEngine;

/// <summary>BGM 种类（教程 10.4.2 的 BGMType）。</summary>
public enum BGMType { None, Title, Game, Boss }

/// <summary>SE 种类（教程 10.4.2 的 SEType）。</summary>
public enum SEType
{
    Shoot, Killed, Damage, Get, DoorOpen, DoorClosed, BossKilled, Button, Clear, GameOver
}

/// <summary>
/// 教程 10.4「奏响多个BGM和SE」。
/// 单例 + DontDestroyOnLoad，保证场景切换时 BGM 不中断；
/// BGM 只在切换曲目时更换 AudioClip，SE 使用 PlayOneShot 叠加播放。
/// </summary>
public class SoundManager : MonoBehaviour
{
    public AudioClip bgmTitle;
    public AudioClip bgmGame;
    public AudioClip bgmBoss;

    public AudioClip seShoot;
    public AudioClip seKilled;
    public AudioClip seDamage;
    public AudioClip seGet;
    public AudioClip seDoorOpen;
    public AudioClip seDoorClosed;
    public AudioClip seBossKilled;
    public AudioClip seButton;

    public AudioClip meClear;
    public AudioClip meGameOver;

    public static BGMType playingBGM = BGMType.None;
    public static SoundManager soundManager;

    AudioSource audioSource;

    void Awake()
    {
        // 第一次读到场景中时保留自身，第二次以后生成出来的直接删除
        if (soundManager == null)
        {
            soundManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (soundManager != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }

    /// <summary>播放指定的 BGM；与当前曲目相同时不重新开始。</summary>
    public static void PlayBgm(BGMType type)
    {
        if (soundManager == null)
        {
            return;
        }
        if (type == playingBGM)
        {
            return;
        }

        AudioClip clip = soundManager.GetBgmClip(type);
        if (clip == null)
        {
            StopBgm();
            return;
        }

        soundManager.audioSource.clip = clip;
        soundManager.audioSource.loop = true;
        soundManager.audioSource.Play();
        playingBGM = type;
    }

    /// <summary>停止 BGM。</summary>
    public static void StopBgm()
    {
        if (soundManager == null)
        {
            return;
        }
        soundManager.audioSource.Stop();
        playingBGM = BGMType.None;
    }

    /// <summary>播放一次 SE。</summary>
    public static void SEPlay(SEType type)
    {
        if (soundManager == null)
        {
            return;
        }
        AudioClip clip = soundManager.GetSeClip(type);
        if (clip == null)
        {
            return;
        }
        soundManager.audioSource.PlayOneShot(clip);
    }

    AudioClip GetBgmClip(BGMType type)
    {
        switch (type)
        {
            case BGMType.Title: return bgmTitle;
            case BGMType.Game: return bgmGame;
            case BGMType.Boss: return bgmBoss;
            default: return null;
        }
    }

    AudioClip GetSeClip(SEType type)
    {
        switch (type)
        {
            case SEType.Shoot: return seShoot;
            case SEType.Killed: return seKilled;
            case SEType.Damage: return seDamage;
            case SEType.Get: return seGet;
            case SEType.DoorOpen: return seDoorOpen;
            case SEType.DoorClosed: return seDoorClosed;
            case SEType.BossKilled: return seBossKilled;
            case SEType.Button: return seButton;
            case SEType.Clear: return meClear;
            case SEType.GameOver: return meGameOver;
            default: return null;
        }
    }
}
