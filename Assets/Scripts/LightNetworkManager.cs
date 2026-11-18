using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局常驻的光点网络管理器。
/// 监听背包物品消耗 → 在角色处生成光点 → 场景内光点 ≥2 时两两连线。
/// </summary>
public class LightNetworkManager : MonoBehaviour
{
    public static LightNetworkManager Instance { get; private set; }

    [Header("预制体")]
    [Tooltip("拖 Assets/Prefabs/LightPointPrefab")]
    [SerializeField] private GameObject lightPointPrefab;
    [Tooltip("拖 Assets/Prefabs/LightBeamPrefab")]
    [SerializeField] private GameObject lightBeamPrefab;

    [Header("角色")]
    [Tooltip("留空则自动按 Tag 找 Player")]
    [SerializeField] private Transform playerTransform;

    [Header("光点参数")]
    [Tooltip("光点相对角色的偏移，避免正好盖住角色")]
    [SerializeField] private Vector3 pointOffset = new Vector3(0f, 0.5f, 0f);
    [Tooltip("同一位置重复生成时，随机抖动范围")]
    [SerializeField] private float randomJitter = 0.15f;

    [Header("光线参数")]
    [Tooltip("光线线宽（世界单位）")]
    [SerializeField] private float beamWidth = 0.06f;

    // 当前场景中的光点
    private readonly List<GameObject> _points = new List<GameObject>();
    // 当前场景中的光线（与点的连线配对生成，每次重建）
    private readonly List<GameObject> _beams = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // ★ 跨场景常驻核心
    }

    private void OnEnable()
    {
        InventoryManager.OnItemRemoved += HandleItemRemoved;
    }

    private void OnDisable()
    {
        InventoryManager.OnItemRemoved -= HandleItemRemoved;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    private void Update()
    {
        // 角色可能还没找到（异步加载场景），持续尝试
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // 每帧刷新光线几何（光点会跟随角色/场景，位置会变）
        UpdateBeams();
    }

    // ================== 事件响应 ==================

    private void HandleItemRemoved(string itemId, int amount)
    {
        Debug.Log($"[光网] 检测到消耗 {itemId} ×{amount}，生成光点");

        // 一次消耗多少就生成多少个光点（通常 amount = 1）
        for (int i = 0; i < Mathf.Max(1, amount); i++)
        {
            SpawnPoint();
        }

        RebuildBeams();
    }

    // ================== 光点 ==================

    private void SpawnPoint()
    {
           // 加这行，光点就跨场景存在

        if (lightPointPrefab == null)
        {
            Debug.LogWarning("[光网] lightPointPrefab 未赋值，无法生成光点");
            return;
        }

        Vector3 basePos = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 jitter = new Vector3(
            Random.Range(-randomJitter, randomJitter),
            Random.Range(-randomJitter, randomJitter),
            0f);

        GameObject point = Instantiate(lightPointPrefab, basePos + pointOffset + jitter, Quaternion.identity);
        point.name = $"LightPoint_{_points.Count}";
        _points.Add(point);
        
        DontDestroyOnLoad(point);   // 加这行，光点就跨场景存在

    }

    // ================== 光线 ==================

    /// <summary>根据当前光点数量重建所有光线（两两相连）</summary>
    private void RebuildBeams()
    {
        // 清掉旧光线
        foreach (var b in _beams)
        {
            if (b != null) Destroy(b);
        }
        _beams.Clear();

        if (_points.Count < 2) return;
        if (lightBeamPrefab == null)
        {
            Debug.LogWarning("[光网] lightBeamPrefab 未赋值，无法生成光线");
            return;
        }

        // 两两相连
        for (int i = 0; i < _points.Count; i++)
        {
            for (int j = i + 1; j < _points.Count; j++)
            {
                if (_points[i] == null || _points[j] == null) continue;

                GameObject beam = Instantiate(lightBeamPrefab);
                beam.name = $"LightBeam_{i}_{j}";
                _beams.Add(beam);
            }
        }

        UpdateBeams();
    }

    /// <summary>每帧更新所有光线的位置、旋转、缩放</summary>
    private void UpdateBeams()
    {
        if (_beams.Count == 0) return;

        // 清理已销毁的点
        _points.RemoveAll(p => p == null);

        // 按同样顺序重建配对表
        int beamIndex = 0;
        for (int i = 0; i < _points.Count; i++)
        {
            for (int j = i + 1; j < _points.Count; j++)
            {
                if (beamIndex >= _beams.Count) return;

                GameObject beam = _beams[beamIndex];
                if (beam == null) { beamIndex++; continue; }

                Vector3 a = _points[i].transform.position;
                Vector3 b = _points[j].transform.position;
                Vector3 dir = b - a;
                float len = dir.magnitude;

                beam.transform.position = a + dir * 0.5f;             // 中点
                beam.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                beam.transform.localScale = new Vector3(len, beamWidth, 1f);   // 沿 x 拉伸

                beamIndex++;
            }
        }
    }

    // ================== 场景切换处理 ==================

    /// <summary>如果你需要手动清空当前场景的光点（比如换场景后重新开始）</summary>
    public void ClearAllPoints()
    {
        foreach (var p in _points) if (p != null) Destroy(p);
        foreach (var b in _beams) if (b != null) Destroy(b);
        _points.Clear();
        _beams.Clear();
    }
}
