using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 声音管理：BGM 单声道循环 + SE 一次性播放（带音调变化）。
/// 由 YG3Builder 在每个场景生成并注入 AudioClip 引用。
/// </summary>
public class YG3SoundManager : MonoBehaviour
{
    [System.Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    public List<NamedClip> clips = new List<NamedClip>();
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    public static YG3SoundManager Instance { get; private set; }

    AudioSource seSource;
    AudioSource bgmSource;
    Dictionary<string, AudioClip> table;

    void Awake()
    {
        Instance = this;
        seSource = gameObject.AddComponent<AudioSource>();
        seSource.playOnAwake = false;
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        table = new Dictionary<string, AudioClip>();
        foreach (NamedClip nc in clips)
        {
            if (nc.clip != null && !table.ContainsKey(nc.name))
            {
                table[nc.name] = nc.clip;
            }
        }
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>播放音效。</summary>
    public static void SE(string name, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null || Instance.table == null)
        {
            return;
        }
        AudioClip clip;
        if (Instance.table.TryGetValue(name, out clip) && clip != null)
        {
            Instance.seSource.pitch = pitch;
            Instance.seSource.PlayOneShot(clip, volume);
            Instance.seSource.pitch = 1f;
        }
    }

    /// <summary>切换 BGM（名字不同才切）。</summary>
    public static void BGM(string name)
    {
        if (Instance == null || Instance.table == null)
        {
            return;
        }
        AudioClip clip;
        if (Instance.table.TryGetValue(name, out clip))
        {
            if (Instance.bgmSource.clip == clip && Instance.bgmSource.isPlaying)
            {
                return;
            }
            Instance.bgmSource.clip = clip;
            Instance.bgmSource.Play();
        }
    }

    public static void StopBGM()
    {
        if (Instance != null)
        {
            Instance.bgmSource.Stop();
        }
    }
}
