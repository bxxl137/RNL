using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerManager : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 4f;//后期改数值，也许地图大小也会有所影响体验

    [Header("移动锁")]
    [SerializeField] private bool canMove = true;

    //是否允许移动。整理杂物、对话、强制剧情时设为 false(可能需要的功能)
    public bool CanMove
    {
        get => canMove;//只读不改
        set
        {
            canMove = value;
            if (!canMove)
            {
                rb.velocity = Vector2.zero;        // 立即停下
                anim.SetFloat(SpeedHash, 0f);      // 回 Idle
            }
        }
    }

    private Rigidbody2D rb;
    private Animator anim;//v0.1
    private SpriteRenderer sr;
    private float inputX;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");//静态变量

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr  = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;                                        // 无重力(因为会议说是没用跳跃，所以删除重力，后期碰撞需求再改)
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;      // 防止碰撞后翻滚，冻结角,可以删除
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;//看不懂
    }

    private void Update()
    {
        if (!canMove)
        {
            inputX = 0f;
            anim.SetFloat(SpeedHash, 0f);
            return;
        }

        inputX = Input.GetAxisRaw("Horizontal");   // 只有 -1 / 0 / 1，无惯性

        // 动画参数：用绝对值，Walk 动画不区分左右
        anim.SetFloat(SpeedHash, Mathf.Abs(inputX));

        // 朝向翻转：用 SpriteRenderer.flipX，不动 Scale，避免影响子物体和碰撞体
        if (inputX > 0.01f)       sr.flipX = false;
        else if (inputX < -0.01f) sr.flipX = true;
    }

    private void FixedUpdate()
    {
        // 只改水平速度
        rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);//也许可以修改？毕竟没用Y
    }
}

