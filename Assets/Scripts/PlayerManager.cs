using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerManager : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 4f;

    [Header("移动锁")]
    [SerializeField] private bool canMove = true;

    private readonly HashSet<MoveLockReason> _lockReasons = new HashSet<MoveLockReason>();

    public bool CanMove
    {
        get => canMove;
        set
        {
            if (canMove == value) return;
            canMove = value;
            ApplyLockBehavior();
            OnMovementLockChanged?.Invoke(!canMove,
                _lockReasons.Count > 0 ? FirstReason() : MoveLockReason.None);
        }
    }

    public event System.Action<bool, MoveLockReason> OnMovementLockChanged;
    public bool IsMovementLocked => !canMove;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private float inputX;
    private float lastFacing = 1f;   // 记住最后一次朝向，停下时保持不变

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int FacingHash = Animator.StringToHash("Facing");

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr  = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        canMove = _lockReasons.Count == 0;
        lastFacing = sr.flipX ? -1f : 1f;
    }

    private void Update()
    {
        if (!canMove)
        {
            inputX = 0f;
            anim.SetFloat(SpeedHash, 0f);
            anim.SetFloat(FacingHash, lastFacing);
            return;
        }

        inputX = Input.GetAxisRaw("Horizontal");

        // Speed：0 或 1，驱动 idle / walk 切换
        anim.SetFloat(SpeedHash, Mathf.Abs(inputX));

        if (inputX > 0.01f)       lastFacing = 1f;
        else if (inputX < -0.01f) lastFacing = -1f;

        // Facing：只在有输入时更新，停下保持原朝向
        anim.SetFloat(FacingHash, lastFacing);

        // 翻转仍然用 SpriteRenderer
        if (inputX > 0.01f)       sr.flipX = false;
        else if (inputX < -0.01f) sr.flipX = true;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);
    }

    // ---------------- 移动锁 ----------------

    public void AddMoveLock(MoveLockReason reason)
    {
        if (!_lockReasons.Add(reason)) return;
        bool wasLocked = !canMove;
        canMove = false;
        ApplyLockBehavior();
        if (!wasLocked) OnMovementLockChanged?.Invoke(true, reason);
    }

    public void RemoveMoveLock(MoveLockReason reason)
    {
        bool removed = _lockReasons.Remove(reason);
        bool wasLocked = !canMove;
        if (removed) canMove = _lockReasons.Count == 0;
        if (removed) ApplyLockBehavior();
        if (removed && wasLocked && canMove)
            OnMovementLockChanged?.Invoke(false, reason);
    }

    public void ClearAllLocks()
    {
        if (_lockReasons.Count == 0) return;
        bool wasLocked = !canMove;
        _lockReasons.Clear();
        canMove = true;
        ApplyLockBehavior();
        if (wasLocked) OnMovementLockChanged?.Invoke(false, MoveLockReason.None);
    }

    private void ApplyLockBehavior()
    {
        if (canMove) return;
        if (rb != null) rb.velocity = Vector2.zero;
        if (anim != null)
        {
            anim.SetFloat(SpeedHash, 0f);
            anim.SetFloat(FacingHash, lastFacing);
        }
    }

    private MoveLockReason FirstReason()
    {
        foreach (var r in _lockReasons) return r;
        return MoveLockReason.None;
    }
}

public enum MoveLockReason
{
    None,
    Story,
    Interaction,
    UI,
    Cutscene,
    Organizing,
}
