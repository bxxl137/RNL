using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;   // 兜底判断场景名用

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("背包预制体")]
    public GameObject inventoryPrefab;

    private GameObject inventoryInstance;
    private bool isOpen = false;

    // ★ 对外暴露，供 GameSceneManager 判断
    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (inventoryPrefab != null)
        {
            inventoryInstance = Instantiate(inventoryPrefab);
            inventoryInstance.transform.SetParent(this.transform, false);

            RectTransform rect = inventoryInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            inventoryInstance.SetActive(false);

            Transform cancelBtnTransform = FindChildByName(inventoryInstance.transform, "cancel");
            if (cancelBtnTransform != null)
            {
                Button closeBtn = cancelBtnTransform.GetComponent<Button>();
                if (closeBtn != null) closeBtn.onClick.AddListener(CloseInventory);
            }
        }
    }

    private void Update()
    {
        if (inventoryInstance == null) return;
        if (Input.GetKeyDown(KeyCode.E)) ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        if (isOpen || inventoryInstance == null) return;

        // ★ StartScene 禁止打开背包
        if (IsInMainMenu()) return;

        isOpen = true;
        inventoryInstance.SetActive(true);
        Time.timeScale = 0f;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RefreshInventoryUI();
    }

    public void CloseInventory()
    {
        if (!isOpen || inventoryInstance == null) return;

        isOpen = false;
        inventoryInstance.SetActive(false);

        // ★ 如果 PauseScene 还开着，不要恢复时间（否则 Pause 会失效）
        bool pauseStillOpen =
            GameSceneManager.Instance != null && GameSceneManager.Instance.IsPauseOpen;
        if (!pauseStillOpen) Time.timeScale = 1f;
    }

    // ★ 主菜单判断：优先用 GameSceneManager，避免场景名硬编码
    private bool IsInMainMenu()
    {
        if (GameSceneManager.Instance != null)
            return GameSceneManager.Instance.IsInMainMenu;

        // 兜底：直接测 StartScene 时 GameSceneManager 可能没起来
        return SceneManager.GetActiveScene().name == "StartScene";
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindChildByName(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}