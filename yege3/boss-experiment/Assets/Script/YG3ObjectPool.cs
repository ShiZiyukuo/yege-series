using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轻量对象池：按预制体复用箭矢 / 金针 / 震波 / 弧光，避免高频 Instantiate/Destroy。
/// </summary>
public static class YG3ObjectPool
{
    static readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    static Transform root;

    static Transform Root
    {
        get
        {
            if (root == null)
            {
                GameObject go = new GameObject("~YG3Pool");
                Object.DontDestroyOnLoad(go);
                root = go.transform;
            }
            return root;
        }
    }

    public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        Queue<GameObject> q;
        if (!pools.TryGetValue(prefab, out q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        GameObject go = null;
        while (q.Count > 0)
        {
            GameObject candidate = q.Dequeue();
            if (candidate != null)
            {
                go = candidate;
                break;
            }
        }
        if (go == null)
        {
            go = Object.Instantiate(prefab);
        }
        go.transform.SetParent(null, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.SetActive(true);
        return go;
    }

    public static T Get<T>(T prefab, Vector3 pos, Quaternion rot) where T : Component
    {
        return Get(prefab.gameObject, pos, rot).GetComponent<T>();
    }

    public static void Release(GameObject instance, GameObject prefab)
    {
        if (instance == null)
        {
            return;
        }
        Queue<GameObject> q;
        if (!pools.TryGetValue(prefab, out q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        instance.SetActive(false);
        instance.transform.SetParent(Root, false);
        q.Enqueue(instance);
    }

    public static void Preload(GameObject prefab, int count)
    {
        Queue<GameObject> q;
        if (!pools.TryGetValue(prefab, out q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        for (int i = 0; i < count; i++)
        {
            GameObject go = Object.Instantiate(prefab);
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            q.Enqueue(go);
        }
    }

    /// <summary>切场景时清空池引用（对象本体挂在 DontDestroyOnLoad，无碍）。</summary>
    public static void Clear()
    {
        pools.Clear();
    }
}
