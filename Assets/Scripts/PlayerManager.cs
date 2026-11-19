using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerManager : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 6f;

    [Header("移动锁")]
    [SerializeField] private bool canMove = true;

    // ★ 新增：多场景边界配置
    [System.Serializable]
    public class SceneBoundary
    {
        [Tooltip("场景名（Build Settings 里的名字，区分大小写）")]
        public string sceneName = "";
        [Tooltip("左边界 X（最小 X）")]
        public float minX = -10f;
        [Tooltip("右边界 X（最大 X）")]
        public float maxX = 10f;
    }

    [Header("水平边界（按场景配置）")]
    [Tooltip("勾选后才启用本功能")]
    [SerializeField] private bool enableSceneBoundary = true;
    [Tooltip("每个场景一组边界；当前场景不在列表里则不限制")]
    [SerializeField] private List<SceneBoundary> sceneBoundaries = new List<SceneBoundary>();
    [Tooltip("Gizmos 画线的高度范围，仅用于编辑器可视化")]
    [SerializeField] private float gizmoHalfHeight = 6f;

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
    private float lastFacing = 1f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int FacingHash = Animator.StringToHash("Facing");

    // ★ 新增：当前生效的边界（null 表示本场景不限制）
    private SceneBoundary _activeBoundary;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        canMove = _lockReasons.Count == 0;
        lastFacing = sr.flipX ? -1f : 1f;
    }

    // ★ 新增：场景加载后刷新边界
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshActiveBoundary();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshActiveBoundary();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshActiveBoundary();
    }

    private void RefreshActiveBoundary()
    {
        _activeBoundary = null;

        if (!enableSceneBoundary || sceneBoundaries == null) return;

        string current = SceneManager.GetActiveScene().name;
        foreach (var b in sceneBoundaries)
        {
            if (b == null || string.IsNullOrEmpty(b.sceneName)) continue;
            if (b.sceneName == current)
            {
                // 防御：min/max 填反了自动交换
                if (b.maxX < b.minX)
                {
                    float tmp = b.minX;
                    b.minX = b.maxX;
                    b.maxX = tmp;
                }
                _activeBoundary = b;
                break;
            }
        }
    }
    // ★ 新增结束

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
        anim.SetFloat(SpeedHash, Mathf.Abs(inputX));

        if (inputX > 0.01f) lastFacing = 1f;
        else if (inputX < -0.01f) lastFacing = -1f;

        anim.SetFloat(FacingHash, lastFacing);
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);

        // ★ 新增：按当前场景边界钳制
        if (_activeBoundary != null)
        {
            Vector2 pos = rb.position;
            float nextX = pos.x + rb.velocity.x * Time.fixedDeltaTime;
            float clampedX = Mathf.Clamp(nextX, _activeBoundary.minX, _activeBoundary.maxX);

            if (!Mathf.Approximately(clampedX, nextX))
            {
                pos.x = clampedX;
                rb.position = pos;
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }
    }

    // ★ 新增：编辑器里预览"当前打开场景"的边界
    private void OnDrawGizmosSelected()
    {
        if (!enableSceneBoundary || sceneBoundaries == null) return;

        string current = Application.isPlaying
            ? SceneManager.GetActiveScene().name
            : gameObject.scene.name;

        foreach (var b in sceneBoundaries)
        {
            if (b == null) continue;
            bool isCurrent = b.sceneName == current;

            Gizmos.color = isCurrent ? Color.yellow : new Color(1f, 1f, 0f, 0.25f);
            Vector3 lTop = new Vector3(b.minX, gizmoHalfHeight, 0f);
            Vector3 lBot = new Vector3(b.minX, -gizmoHalfHeight, 0f);
            Vector3 rTop = new Vector3(b.maxX, gizmoHalfHeight, 0f);
            Vector3 rBot = new Vector3(b.maxX, -gizmoHalfHeight, 0f);

            Gizmos.DrawLine(lTop, lBot);
            Gizmos.DrawLine(rTop, rBot);
        }
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