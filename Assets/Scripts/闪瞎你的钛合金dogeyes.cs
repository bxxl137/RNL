using System.Collections;
using System.Collections.Generic;


using UnityEngine;

public class 闪瞎你的钛合金dogeyes : MonoBehaviour
{
    [Tooltip("两个灯交替的时间间隔（秒）")]
    public float interval = 1f;

    // 两个子物体
    private GameObject anDeng;
    private GameObject haoDeng;

    private float timer = 0f;
    private bool isAnTurn = true; // true = an_deng 亮, false = hao_deng 亮

    void Start()
    {
        // 按名字查找子物体（必须是 dengdengdeng 的直接子级）
        Transform t = transform.Find("an_deng");
        if (t != null) anDeng = t.gameObject;
        else Debug.LogError("没找到子物体 an_deng");

        t = transform.Find("hao_deng");
        if (t != null) haoDeng = t.gameObject;
        else Debug.LogError("没找到子物体 hao_deng");

        // 初始状态：an_deng 亮，hao_deng 灭
        ApplyState();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer -= interval;   // 用减法而不是清零，避免长时间运行累积误差
            isAnTurn = !isAnTurn;
            ApplyState();
        }
    }

    void ApplyState()
    {
        if (anDeng != null) anDeng.SetActive(isAnTurn);
        if (haoDeng != null) haoDeng.SetActive(!isAnTurn);
    }
}

