using System.Collections;
using System.Collections.Generic;


using UnityEngine;

/// <summary>
/// 挂在 tiggerplace_（BoxCollider2D, Is Trigger）上。
/// 可反复对话：玩家进入按 E → 播 firstchat → 消失
/// → 实时检测 lamp 是否 warm → 是则播 secondchat。
/// </summary>


public class GrandmaChatTrigger : MonoBehaviour
{
    [Header("UI 引用（初始都未激活）")]
    [SerializeField] private GameObject firstChat;
    [SerializeField] private GameObject secondChat;

    [Header("需要检测的物体")]
    [Tooltip("拖 lamp 对象进来")]
    [SerializeField] private GameObject lampObject;

    [Header("交互设置")]
    [SerializeField] private KeyCode interactKey = KeyCode.Backspace;

    [Header("文本显示时长")]
    [SerializeField] private float firstChatDuration = 3f;
    [SerializeField] private float secondChatDuration = 3f;

    private bool playerInRange;
    private bool busy;   // 正在播对话时为 true，防止重复触发

    private void Start()
    {
        if (firstChat != null) firstChat.SetActive(false);
        if (secondChat != null) secondChat.SetActive(false);
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        
        Debug.Log("[GrandmaChatTrigger] 玩家进入触发区", this);
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        
        
        Debug.Log("[GrandmaChatTrigger] 玩家离开触发区", this);
        playerInRange = false;

        // 玩家中途离开时，把正在显示的文本收起，避免残留
        if (busy) StopChatImmediately();
    }

    private void Update()
    {
        if (!playerInRange || busy) return;

        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ChatSequence());
        }
    }

    private IEnumerator ChatSequence()
    {
        busy = true;

        // 第一段：每轮都播
        if (firstChat != null) firstChat.SetActive(true);
        yield return new WaitForSeconds(firstChatDuration);
        if (firstChat != null) firstChat.SetActive(false);

        // 第二段：每轮实时检测 lamp 状态
        if (IsLampWarm())
        {
            // 稍等一下，避免两段文本紧贴
            yield return new WaitForSeconds(0.5f);

            if (secondChat != null) secondChat.SetActive(true);
            yield return new WaitForSeconds(secondChatDuration);
            if (secondChat != null) secondChat.SetActive(false);
        }

        busy = false;
    }

    /// <summary>玩家离开区域时立刻收起文本并中止协程。</summary>
    private void StopChatImmediately()
    {
        StopAllCoroutines();
        if (firstChat != null) firstChat.SetActive(false);
        if (secondChat != null) secondChat.SetActive(false);
        busy = false;
    }

    /// <summary>
    /// 判断 lamp 是否为 warm 状态。
    /// 按你 lamp 的实际结构选一种，多余的删掉。
    /// </summary>
    /// <summary>
/// 判断 lamp 是否为暖光状态：看 lampObject 下 warm 子物体是否激活。
/// </summary>
private bool IsLampWarm()
{
    if (lampObject == null) return false;

    Transform warmChild = lampObject.transform.Find("warm");
    if (warmChild == null)
    {
        Debug.LogWarning($"[GrandmaChatTrigger] {lampObject.name} 下找不到 warm 子物体", this);
        return false;
    }

    return warmChild.gameObject.activeSelf;
}

}
