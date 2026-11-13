using System;
using UnityEngine;

public struct TouchNotice
{
    public string noticeKey;
    public GameObject sender;
    public GameObject toucher;

    public TouchNotice(string key, GameObject sender, GameObject toucher)
    {
        noticeKey = key;
        this.sender = sender;
        this.toucher = toucher;
    }
}

public static class TouchNoticeBus
{
    public static event Action<TouchNotice> OnNotice;

    public static void Publish(TouchNotice notice)
    {
        OnNotice?.Invoke(notice);
    }

    public static void Subscribe(Action<TouchNotice> handler) => OnNotice += handler;
    public static void Unsubscribe(Action<TouchNotice> handler) => OnNotice -= handler;
}
