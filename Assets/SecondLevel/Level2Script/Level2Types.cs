using System;
using UnityEngine;

/// <summary>
/// 第二关的一句台词。speaker 为显示在对话框左上角的名字。
/// </summary>
[Serializable]
public class Level2Line
{
    public string speaker;
    public string text;

    public Level2Line(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

/// <summary>
/// 第二关的可交互点。使用距离判定，避免依赖玩家身上的碰撞体。
/// </summary>
public class Level2Interactable
{
    public string name;
    public int zone;
    public Vector2 localPosition;
    public float radius;
    public GameObject prompt;
    public Action action;

    public Level2Interactable(string name, int zone, Vector2 localPosition, float radius, GameObject prompt, Action action)
    {
        this.name = name;
        this.zone = zone;
        this.localPosition = localPosition;
        this.radius = radius;
        this.prompt = prompt;
        this.action = action;
    }
}

/// <summary>
/// 随身物品面板的一行。
/// </summary>
public class Level2BagEntry
{
    public Sprite icon;
    public string title;
    public string description;
    public string count;

    public Level2BagEntry(Sprite icon, string title, string description, string count)
    {
        this.icon = icon;
        this.title = title;
        this.description = description;
        this.count = count;
    }
}
