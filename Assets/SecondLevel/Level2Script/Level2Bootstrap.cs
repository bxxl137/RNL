using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 第二关总控：运行时搭建四个街区、角色、交互与全部界面。
/// 不改动第一关的任何场景/脚本，所有内容都在本场景内自包含生成。
/// </summary>
public class Level2Bootstrap : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fontAsset;

    private const float GroundY = -5.35f;
    private const float PlayerScale = 1.75f;

    private static readonly float[] ZoneOffsets = { 0f, 200f, 400f, 600f };
    private static readonly float[] ZoneMinX = { -8f, -8f, -12.5f, -8f };
    private static readonly float[] ZoneMaxX = { 8f, 8f, 12.5f, 8f };
    private static readonly string[] ZoneNames = { "旧街 · 棋盘", "阿婆早餐店", "街角花园", "裁缝铺" };
    private static readonly string[] NodeNames = { "棋盘", "阿婆早餐店", "街角花园", "裁缝铺" };

    private static readonly float[] BikeXs = { 5.0f, 6.1f, 7.2f };
    private static readonly float[] BikeRotations = { 78f, 86f, -82f };

    // ---------- 美术 ----------
    private Sprite _bgChess, _bgBreakfast, _bgGarden, _bgTailor;
    private Sprite _playerIdle;
    private Sprite[] _playerWalk;
    private Sprite _npcGranny, _npcGardener, _npcTailor;
    private Sprite _chessMessy, _chessClean, _chessComplete;
    private Sprite[] _bikes = new Sprite[3];
    private Sprite[] _flowersAlive = new Sprite[6];
    private Sprite[] _flowersWithered = new Sprite[6];
    private Sprite _pieceChe, _lantern, _promptSprite, _shadow, _glow, _dot;
    private Sprite _dialogueBox, _panelBag, _panelPuzzle, _button, _slot, _toast, _hudPanel, _vignette;
    private Sprite _itemPackage, _itemBreakfast, _itemSeed, _pieceCheIcon, _pieceZuIcon;

    // ---------- 场景对象 ----------
    private Camera _camera;
    private Canvas _canvas;
    private CanvasGroup _fadeGroup;
    private Level2DialogueUI _dialogue;
    private Level2HUD _hud;
    private Level2GardenPuzzleUI _gardenPuzzle;
    private Level2BikePuzzleUI _bikePuzzle;
    private Level2EndingUI _ending;

    private readonly GameObject[] _zones = new GameObject[4];
    private GameObject _playerRig;
    private GameObject _playerGo;
    private Level2PlayerController _player;
    private float _playerBaseY;

    private readonly List<Level2Interactable> _interactables = new List<Level2Interactable>();
    private Level2Interactable _focused;

    private SpriteRenderer _chessRenderer;
    private readonly SpriteRenderer[] _bikeRenderers = new SpriteRenderer[3];
    private GameObject _centerBedDead, _centerBedBloom, _leftBedEmpty, _leftBedBloom;
    private SpriteRenderer _chePiece;
    private readonly SpriteRenderer[] _nodeGlows = new SpriteRenderer[4];
    private readonly bool[] _nodeLit = new bool[4];

    // ---------- 流程状态 ----------
    private int _currentZone;
    private int _fragments;
    private bool _hasPackage, _hasBreakfast, _hasSeed, _hasChe, _hasZu;
    private bool _metGranny, _metTailor;
    private bool _chessCleaned, _chessRestored, _bikeSolved, _gardenSolved, _gardenPlanted;
    private bool _introDone, _transitioning, _endingStarted;
    private bool _dialogueWasOpen;
    private float _dialogueClosedAt = -100f;
    private float _transitionGuardUntil;
    private float _lastNodeLightAt = -100f;

    private InventoryUI[] _legacyInventory;
    private Canvas[] _legacyCanvases;

    // =====================================================================
    // 生命周期
    // =====================================================================

    private void Start()
    {
        if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;

        SuppressLegacyBag();
        LoadArt();
        SetupCamera();
        SetupEventSystem();
        SetupCanvas();
        BuildWorld();
        CreatePlayer();
        BuildUI();

        SetZoneImmediate(0, -6.2f);

        _hud.SetFragments(_fragments);
        for (int i = 0; i < 4; i++) _hud.SetNode(i, false);
        _hud.SetHint("A / D 移动    Enter 交互    E 随身物品");

        StartCoroutine(OpeningRoutine());
    }

    private void OnDestroy()
    {
        RestoreLegacyBag();
    }

    private IEnumerator OpeningRoutine()
    {
        _fadeGroup.alpha = 1f;
        yield return Fade(0f, 0.85f);
        yield return new WaitForSecondsRealtime(0.2f);

        _hud.ShowZoneCaption(ZoneNames[0]);
        yield return new WaitForSecondsRealtime(0.65f);

        Talk(() =>
        {
            _introDone = true;
            _player.canMove = true;
            _hud.ShowToast("四处走走，和街坊们说说话吧");
        },
        L("我", "（是我出门太早了吗？好冷清的感觉）"),
        L("我", "（按理来说不应该是这样的）"),
        L("我", "（口袋里的微光碎片，好像能唤醒街角那些沉睡的节点。）"));
    }

    private void Update()
    {
        if (_endingStarted) return;
        if (_player == null || _hud == null || _dialogue == null) return;

        if (_dialogueWasOpen && !_dialogue.IsOpen) _dialogueClosedAt = Time.unscaledTime;
        _dialogueWasOpen = _dialogue.IsOpen;

        bool modal = _gardenPuzzle.IsOpen || _bikePuzzle.IsOpen;

        // 随身物品
        if (_introDone && !_transitioning && !modal)
        {
            if (_hud.BagOpen)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                    _hud.HideBag();
            }
            else if (!_dialogue.IsOpen && Input.GetKeyDown(KeyCode.E))
            {
                _hud.ShowBag(BuildBagEntries());
            }
        }

        if (!_endingStarted && CountLitNodes() >= 4
            && !_dialogue.IsOpen && !modal && !_hud.BagOpen
            && Time.unscaledTime - _lastNodeLightAt > 0.6f)
        {
            _endingStarted = true;
            StartCoroutine(EndingRoutine());
            return;
        }

        bool canAct = CanAct;
        _player.canMove = canAct;

        if (!canAct)
        {
            ClearFocus();
            return;
        }

        UpdateFocus();

        if (_dialogue.IsOpen || _gardenPuzzle.IsOpen || _bikePuzzle.IsOpen || _hud.BagOpen)
            return;

        CheckZoneTransition();
    }

    private bool CanAct
    {
        get
        {
            return _introDone
                   && !_transitioning
                   && !_endingStarted
                   && !_dialogue.IsOpen
                   && !_gardenPuzzle.IsOpen
                   && !_bikePuzzle.IsOpen
                   && !_hud.BagOpen;
        }
    }

    // =====================================================================
    // 初始化
    // =====================================================================

    private void LoadArt()
    {
        _bgChess = LoadSprite("SecondLevel/Art/bg_chess");
        _bgBreakfast = LoadSprite("SecondLevel/Art/bg_breakfast");
        _bgGarden = LoadSprite("SecondLevel/Art/bg_garden");
        _bgTailor = LoadSprite("SecondLevel/Art/bg_tailor");

        _playerIdle = LoadSprite("SecondLevel/Art/player_idle");
        _playerWalk = new Sprite[10];
        for (int i = 0; i < 10; i++)
            _playerWalk[i] = LoadSprite("SecondLevel/Art/player_walk_" + i);

        _npcGranny = LoadSprite("SecondLevel/Art/npc_breakfast");
        _npcGardener = LoadSprite("SecondLevel/Art/npc_gardener");
        _npcTailor = LoadSprite("SecondLevel/Art/npc_tailor");

        _chessMessy = LoadSprite("SecondLevel/Art/chess_messy");
        _chessClean = LoadSprite("SecondLevel/Art/chess_clean");
        _chessComplete = LoadSprite("SecondLevel/Art/chess_complete");

        _bikes[0] = LoadSprite("SecondLevel/Art/bike_white");
        _bikes[1] = LoadSprite("SecondLevel/Art/bike_pink");
        _bikes[2] = LoadSprite("SecondLevel/Art/bike_blue");

        string[] aliveNames = { "flower_yellow", "flower_red", "flower_blue", "flower_orange", "flower_pink", "flower_purple" };
        for (int i = 0; i < 6; i++)
            _flowersAlive[i] = LoadSprite("SecondLevel/Art/" + aliveNames[i]);

        string[] deadNames = { "flower_w390", "flower_w388", "flower_w389", "flower_wred", "flower_w394", "flower_w396" };
        for (int i = 0; i < 6; i++)
            _flowersWithered[i] = LoadSprite("SecondLevel/Art/" + deadNames[i]);

        _pieceChe = LoadSprite("SecondLevel/Art/piece_che");
        _lantern = LoadSprite("SecondLevel/Art/lantern");
        _promptSprite = LoadSprite("SecondLevel/Art/prompt_bang");
        _shadow = LoadSprite("SecondLevel/Art/shadow");
        _glow = LoadSprite("SecondLevel/Art/glow");
        _dot = LoadSprite("SecondLevel/Art/dot");

        _dialogueBox = LoadSprite("SecondLevel/Art/dialogue_box");
        _panelBag = LoadSprite("SecondLevel/Art/panel_bag");
        _panelPuzzle = LoadSprite("SecondLevel/Art/panel_puzzle");
        _button = LoadSprite("SecondLevel/Art/button");
        _slot = LoadSprite("SecondLevel/Art/slot");
        _toast = LoadSprite("SecondLevel/Art/toast");
        _hudPanel = LoadSprite("SecondLevel/Art/hud_panel");
        _vignette = LoadSprite("SecondLevel/Art/vignette");

        _itemPackage = LoadSprite("SecondLevel/Art/item_package");
        _itemBreakfast = LoadSprite("SecondLevel/Art/item_breakfast");
        _itemSeed = LoadSprite("SecondLevel/Art/item_seed");
        _pieceCheIcon = LoadSprite("SecondLevel/Art/piece_che");
        _pieceZuIcon = LoadSprite("SecondLevel/Art/piece_zu");
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture != null)
        {
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                                   new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            return sprite;
        }

        Debug.LogWarning("[Level2] 找不到美术资源: " + path);
        return null;
    }

    private void SetupCamera()
    {
        _camera = Camera.main;
        if (_camera == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            _camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        _camera.orthographic = true;
        _camera.orthographicSize = 7.5f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);
        _camera.transform.position = new Vector3(0f, 0f, -10f);
        _camera.transform.rotation = Quaternion.identity;
        if (_camera.GetComponent<Level2CameraFollow>() == null)
        {
            _camera.gameObject.AddComponent<Level2CameraFollow>();
        }
    }

    private static void SetupEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void SetupCanvas()
    {
        GameObject go = new GameObject("Level2Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    // =====================================================================
    // 世界搭建
    // =====================================================================

    private void BuildWorld()
    {
    Sprite[] backgrounds = { _bgChess, _bgBreakfast, _bgGarden, _bgTailor };

    for (int i = 0; i < 4; i++)
    {
        GameObject zone = new GameObject("Zone" + i + "_" + ZoneNames[i]);
        zone.transform.position = new Vector3(ZoneOffsets[i], 0f, 0f);
        _zones[i] = zone;

        var bg = CreateArt("Background", backgrounds[i], zone.transform, Vector3.zero, 1.6f, -100);

        // —— 关键：保证背景宽度足以铺满相机视野 ——
        if (backgrounds[i] != null){
    float spriteWorldWidth = backgrounds[i].bounds.size.x * 1.6f;

    // 相机一屏的世界宽度
    float viewWidth = _camera.orthographicSize * _camera.aspect * 2f;

    // 区域宽度 + 一屏宽度 = 相机贴到任意一端也能盖满屏幕
    float requiredWidth = (ZoneMaxX[i] - ZoneMinX[i]) + viewWidth;

    if (spriteWorldWidth < requiredWidth)
    {
        float factor = requiredWidth / spriteWorldWidth;
        bg.transform.localScale = new Vector3(1.6f * factor, bg.transform.localScale.y, 1f);
    }}

    }

    BuildChessZone(_zones[0].transform);
    BuildBreakfastZone(_zones[1].transform);
    BuildGardenZone(_zones[2].transform);
    BuildTailorZone(_zones[3].transform);
    }


    private void BuildChessZone(Transform zone)
    {
        CreateArt("StreetLantern", _lantern, zone,
                  new Vector3(-6.6f, GroundCenterY(_lantern, 0.8f), 0f), 0.8f, 10);

        _chessRenderer = CreateArt("ChessTable", _chessMessy, zone,
                                   new Vector3(3.6f, GroundCenterY(_chessMessy, 0.55f), 0f), 0.55f, 12);

        AddInteractable("chess", 0, 3.6f, 2.3f, 0.8f, OnChessInteract);
        AddNodeGlow(0, zone, new Vector3(3.6f, 1.9f, 0f));
    }

    private void BuildBreakfastZone(Transform zone)
    {
        CreateNpc("Granny", _npcGranny, zone, -2.3f, 1.7f);
        AddInteractable("granny", 1, -2.3f, 2.1f, 2.9f, OnGrannyInteract);
        AddNodeGlow(1, zone, new Vector3(-2.3f, 1.7f, 0f));

        for (int i = 0; i < 3; i++)
        {
            SpriteRenderer bike = CreateArt("Bike" + i, _bikes[i], zone,
                                            new Vector3(BikeXs[i], GroundY + 1.55f, 0f), 1.28f, 12);
            bike.transform.localRotation = Quaternion.Euler(0f, 0f, BikeRotations[i]);
            _bikeRenderers[i] = bike;
        }

        AddInteractable("bikes", 1, 6.1f, 2.1f, 0.15f, OnBikeInteract);
    }

    private void BuildGardenZone(Transform zone)
    {
        MakeSoilBed(zone, -6.5f);
        MakeSoilBed(zone, 0f);
        MakeSoilBed(zone, 6.5f);

        _centerBedDead = BuildFlowerCluster("CenterBedDead", zone, 0f, _flowersWithered, 3, 0.65f, true);
        _centerBedBloom = BuildFlowerCluster("CenterBedBloom", zone, 0f, _flowersAlive, 6, 0.65f, false);
        _leftBedEmpty = BuildEmptyBed("LeftBedEmpty", zone, -6.5f);
        _leftBedBloom = BuildFlowerCluster("LeftBedBloom", zone, -6.5f, _flowersAlive, 3, 0.65f, false);
        BuildFlowerCluster("RightBed", zone, 6.5f, _flowersAlive, 4, 0.62f, true);

        CreateNpc("Gardener", _npcGardener, zone, 10.2f, 1.65f);
        AddInteractable("gardener", 2, 10.2f, 2.0f, 2.9f, OnGardenerInteract);
        AddInteractable("flowerbed", 2, 0f, 2.2f, -1.4f, OnGardenInteract);
        AddInteractable("emptybed", 2, -6.5f, 2.0f, -1.4f, OnPlantInteract);
        AddNodeGlow(2, zone, new Vector3(0f, 1.9f, 0f));
    }

    private void BuildTailorZone(Transform zone)
    {
        CreateArt("ShopLantern", _lantern, zone,
                  new Vector3(1.7f, GroundCenterY(_lantern, 0.8f), 0f), 0.8f, 10);

        CreateNpc("Tailor", _npcTailor, zone, -3.0f, 1.7f);
        AddInteractable("tailor", 3, -3.0f, 2.1f, 2.9f, OnTailorInteract);

        _chePiece = CreateArt("ChePiece", _pieceChe, zone,
                              new Vector3(3.8f, GroundCenterY(_pieceChe, 0.72f), 0f), 0.72f, 11);
        AddNodeGlow(3, zone, new Vector3(3.8f, 1.8f, 0f));
    }

    private void CreateNpc(string name, Sprite sprite, Transform zone, float x, float scale)
    {
        CreateArt(name, sprite, zone, new Vector3(x, GroundCenterY(sprite, scale), 0f), scale, 18);
    }

    private SpriteRenderer CreateArt(string name, Sprite sprite, Transform parent, Vector3 localPosition,
                                     float scale, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private static float GroundCenterY(Sprite sprite, float scale)
    {
        if (sprite == null) return GroundY;
        return GroundY + sprite.bounds.size.y * scale * 0.5f;
    }

    private void MakeSoilBed(Transform zone, float centerX)
    {
        SpriteRenderer soil = CreateArt("SoilBed", _shadow, zone,
                                        new Vector3(centerX, GroundY + 0.12f, 0f), 1f, 2);
        soil.transform.localScale = new Vector3(4.6f, 1.7f, 1f);
        soil.color = new Color(0.24f, 0.17f, 0.11f, 0.8f);
    }

    private GameObject BuildEmptyBed(string name, Transform zone, float centerX)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(zone, false);
        root.transform.localPosition = Vector3.zero;
        return root;
    }

    private GameObject BuildFlowerCluster(string name, Transform zone, float centerX, Sprite[] sprites,
                                          int count, float scale, bool active)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(zone, false);
        root.transform.localPosition = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites != null && sprites.Length > 0 ? sprites[i % sprites.Length] : null;
            float x = centerX + (i - (count - 1) * 0.5f) * 1.08f;
            float y = GroundCenterY(sprite, scale) + (i % 2 == 0 ? 0.04f : -0.03f);
            SpriteRenderer sr = CreateArt("Flower" + i, sprite, root.transform,
                                          new Vector3(x, y, 0f), scale, 8);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 3 - 1) * 2.5f);
        }

        root.SetActive(active);
        return root;
    }

    private void AddNodeGlow(int index, Transform zone, Vector3 position)
    {
        SpriteRenderer glow = CreateArt("NodeGlow" + index, _glow, zone, position, 1.7f, 15);
        glow.color = new Color(1f, 0.82f, 0.52f, 0.10f);
        _nodeGlows[index] = glow;
    }

    private void AddInteractable(string name, int zone, float x, float radius, float promptY, Action action)
    {
        GameObject prompt = null;
        if (_promptSprite != null)
        {
            SpriteRenderer sr = CreateArt("Prompt_" + name, _promptSprite, _zones[zone].transform,
                                          new Vector3(x, promptY, 0f), 0.75f, 30);
            prompt = sr.gameObject;
            Level2PromptBob bob = prompt.AddComponent<Level2PromptBob>();
            bob.Init(new Vector3(x, promptY, 0f));
            prompt.SetActive(false);
        }

        _interactables.Add(new Level2Interactable(name, zone, new Vector2(x, promptY), radius, prompt, action));
    }

    private void CreatePlayer()
    {
        _playerRig = new GameObject("PlayerRig");
        _playerRig.transform.position = Vector3.zero;

        _playerGo = new GameObject("Player");
        _playerGo.transform.SetParent(_playerRig.transform, false);
        _playerBaseY = GroundCenterY(_playerIdle, PlayerScale);
        _playerGo.transform.localPosition = new Vector3(-6.2f, _playerBaseY, 0f);
        _playerGo.transform.localScale = Vector3.one * PlayerScale;

        SpriteRenderer sr = _playerGo.AddComponent<SpriteRenderer>();
        sr.sprite = _playerIdle;
        sr.sortingOrder = 20;

        SpriteRenderer shadow = CreateArt("PlayerShadow", _shadow, _playerGo.transform,
                                          new Vector3(0f, -_playerIdle.bounds.size.y * 0.5f - 0.02f, 0f),
                                          1f, 4);
        shadow.transform.localScale = new Vector3(1.45f, 1f, 1f);
        shadow.color = new Color(0f, 0f, 0f, 0.28f);

        _player = _playerGo.AddComponent<Level2PlayerController>();
        _player.Setup(sr, _playerIdle, _playerWalk, 4.2f, _playerBaseY);
        _player.minX = ZoneMinX[0];
        _player.maxX = ZoneMaxX[0];
        _player.canMove = false;
    }

    private void BuildUI()
    {
        // 暗角
        Image vignette = Level2UI.MakeImage(_canvas.transform, "Vignette", _vignette,
                                            new Vector2(1920f, 1080f), Vector2.zero,
                                            new Color(1f, 1f, 1f, 0.9f), false, false);
        Level2UI.Stretch(vignette.rectTransform, Vector2.zero, Vector2.zero);

        _hud = gameObject.AddComponent<Level2HUD>();
        _hud.Build(_canvas.transform, fontAsset, _dot, _glow, LoadSprite("SecondLevel/Art/fragment"),
                   _panelBag, _toast, _hudPanel);

        _ending = gameObject.AddComponent<Level2EndingUI>();
        _ending.Build(_canvas.transform, fontAsset, _glow, _dot, _button);

        _dialogue = gameObject.AddComponent<Level2DialogueUI>();
        _dialogue.Build(_canvas.transform, fontAsset, _dialogueBox);

        _gardenPuzzle = gameObject.AddComponent<Level2GardenPuzzleUI>();
        _gardenPuzzle.Build(_canvas.transform, fontAsset, _panelPuzzle, _button, _slot,
                            _flowersAlive, _flowersWithered);

        _bikePuzzle = gameObject.AddComponent<Level2BikePuzzleUI>();
        _bikePuzzle.Build(_canvas.transform, fontAsset, _panelPuzzle, _button, _slot, _bikes);

        // 转场黑幕
        Image fade = Level2UI.MakeSolid(_canvas.transform, "FadeOverlay",
                                        new Color(0.012f, 0.016f, 0.026f, 1f),
                                        new Vector2(1920f, 1080f), Vector2.zero);
        Level2UI.Stretch(fade.rectTransform, Vector2.zero, Vector2.zero);
        fade.raycastTarget = false;
        _fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 1f;
        _fadeGroup.blocksRaycasts = false;
        _fadeGroup.interactable = false;

        // 标题页要盖在所有游戏 UI 之上
        Transform endingTitle = _canvas.transform.Find("EndingTitle");
        if (endingTitle != null) endingTitle.SetAsLastSibling();
    }

    // =====================================================================
    // 区域与交互
    // =====================================================================

    private void SetZoneImmediate(int zone, float localX){
    _currentZone = zone;
    for (int i = 0; i < 4; i++) _zones[i].SetActive(i == zone);

    _playerRig.transform.position = new Vector3(ZoneOffsets[zone], 0f, 0f);
    _playerGo.transform.localPosition = new Vector3(localX, _playerBaseY, 0f);
    _player.minX = ZoneMinX[zone];
    _player.maxX = ZoneMaxX[zone];

    // ---- 新增：同步相机跟随目标与水平边界 ----
    ApplyCameraBounds(zone);}

    /// <summary>
/// 根据区块与当前屏幕宽高比，计算相机允许移动的 X 范围，
/// 并把玩家设为跟随目标。区块比视野窄时直接锁死在中心，避免黑边。
/// </summary>
/// <summary>
/// 根据区块与当前屏幕宽高比，计算相机允许移动的 X 范围，
/// 并把玩家设为跟随目标。区块比视野窄时直接锁死在中心，避免黑边。
/// </summary>
          /// <summary>
/// 根据区块与当前屏幕宽高比，计算相机允许移动的 X 范围，
/// 并把玩家设为跟随目标。区块比视野窄时直接锁死在中心，避免黑边。
/// </summary>
private void ApplyCameraBounds(int zone)
{
    var cam = _camera != null ? _camera.GetComponent<Level2CameraFollow>() : null;
    if (cam == null) return;

    // 把玩家设为跟随目标
    if (_playerGo != null)
        cam.SetTarget(_playerGo.transform);

    // 区域的世界坐标左右边缘
    float left  = ZoneMinX[zone] + ZoneOffsets[zone];
    float right = ZoneMaxX[zone] + ZoneOffsets[zone];

    cam.SetZone(left, right);
}



    private void CheckZoneTransition()
    {
        if (_transitioning || _endingStarted) return;
        if (Time.unscaledTime < _transitionGuardUntil) return;

        float x = _playerGo.transform.localPosition.x;
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);

        if (right && x >= ZoneMaxX[_currentZone] - 0.06f && _currentZone < 3)
        {
            _transitionGuardUntil = Time.unscaledTime + 0.7f;
            StartCoroutine(SwitchZone(_currentZone + 1, ZoneMinX[_currentZone + 1] + 1.3f));
        }
        else if (left && x <= ZoneMinX[_currentZone] + 0.06f && _currentZone > 0)
        {
            _transitionGuardUntil = Time.unscaledTime + 0.7f;
            StartCoroutine(SwitchZone(_currentZone - 1, ZoneMaxX[_currentZone - 1] - 1.3f));
        }
    }

    private IEnumerator SwitchZone(int target, float entryX)
    {
        _transitioning = true;
        _player.canMove = false;
        ClearFocus();

        yield return Fade(1f, 0.28f);

        _zones[_currentZone].SetActive(false);
        _currentZone = target;
        _zones[_currentZone].SetActive(true);

        _playerRig.transform.position = new Vector3(ZoneOffsets[target], 0f, 0f);
        _playerGo.transform.localPosition = new Vector3(entryX, _playerBaseY, 0f);
        _player.minX = ZoneMinX[target];
        _player.maxX = ZoneMaxX[target];

        _hud.ShowZoneCaption(ZoneNames[target]);

        yield return new WaitForSecondsRealtime(0.12f);
        yield return Fade(0f, 0.36f);

        _transitioning = false;
        _transitionGuardUntil = Time.unscaledTime + 0.25f;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = _fadeGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _fadeGroup.alpha = target;
    }

    private void ClearFocus()
    {
        if (_focused != null && _focused.prompt != null) _focused.prompt.SetActive(false);
        _focused = null;
    }

    private void UpdateFocus()
    {
        float px = _playerGo.transform.localPosition.x;
        Level2Interactable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _interactables.Count; i++)
        {
            Level2Interactable item = _interactables[i];
            if (item.zone != _currentZone) continue;

            float distance = Mathf.Abs(px - item.localPosition.x);
            if (distance <= item.radius && distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
            }
        }

        if (best != _focused)
        {
            if (_focused != null && _focused.prompt != null) _focused.prompt.SetActive(false);
            _focused = best;
            if (_focused != null && _focused.prompt != null) _focused.prompt.SetActive(true);
        }

        if (_focused == null || _focused.action == null) return;
        if (Time.unscaledTime - _dialogueClosedAt < 0.16f) return;

        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (enter) _focused.action();
    }

    // =====================================================================
    // 棋盘
    // =====================================================================

    private void OnChessInteract()
    {
        if (!_chessCleaned)
        {
            Talk(() => StartCoroutine(CleanChessRoutine()),
                L("我", "棋盘上落满了叶子，棋子也东倒西歪。"),
                L("我", "（先把它收拾干净吧。）"));
            return;
        }

        if (_chessRestored)
        {
            Talk(null, L("我", "（棋盘恢复了原样，暖光停在棋子上。）"));
            return;
        }

        if (!_hasChe || !_hasZu)
        {
            string missing;
            if (!_hasChe && !_hasZu) missing = "（还缺一个车和一个卒。）";
            else if (!_hasChe) missing = "（还缺一个车。）";
            else missing = "（还缺一个卒。）";

            Talk(null, L("我", missing), L("我", "（……会在哪里呢？）"));
            return;
        }

        if (_fragments < 1)
        {
            Talk(null,
                L("我", "（棋盘上还缺一点微光。）"),
                L("我", "（先去别处找一枚微光碎片吧。）"));
            return;
        }

        Talk(CompleteChess,
            L("我", "（把「车」和「卒」放回原位。）"),
            L("我", "（棋盘上的灰尘轻轻浮了起来。）"));
    }

    private IEnumerator CleanChessRoutine()
    {
        _transitioning = true;
        _player.canMove = false;
        yield return Fade(1f, 0.3f);

        _chessRenderer.sprite = _chessClean;
        _chessCleaned = true;

        yield return new WaitForSecondsRealtime(0.15f);
        yield return Fade(0f, 0.38f);
        _transitioning = false;

        Talk(null,
            L("我", "（少了一个车和一个卒。）"),
            L("我", "（……会在哪里呢？）"));
    }

    private void CompleteChess()
    {
        _hasChe = false;
        _hasZu = false;
        _chessRestored = true;
        AddFragments(-1);
        _chessRenderer.sprite = _chessComplete;
        _hud.ShowToast("棋局复原了");
        LightNode(0);

        Talk(null, L("我", "（最后一枚棋子落定，棋盘上浮起一层暖光。）"));
    }

    // =====================================================================
    // 早餐店
    // =====================================================================

    private void OnGrannyInteract()
    {
        if (!_metGranny)
        {
            _metGranny = true;
            Talk(null,
                L("阿婆", "早啊，孩子。今天怎么这么早？"),
                L("我", "阿婆早，我出来走走。"),
                L("阿婆", "天还冷着呢。店里刚生上火，进来坐坐？"),
                L("我", "（可是我没带钱……）"),
                L("阿婆", "别站在风口里，暖和点再走。"));
            return;
        }

        if (_nodeLit[1])
        {
            Talk(null,
                L("阿婆", "趁热吃，凉了就不香了。"),
                L("阿婆", "路上慢点，别总低着头。"));
            return;
        }

        if (_hasPackage)
        {
            if (_fragments < 1)
            {
                Talk(null,
                    L("我", "（包裹上还沾着一点微光，可我手里的碎片不够。）"),
                    L("阿婆", "怎么了？站在门口发什么呆。"));
                return;
            }

            Talk(DeliverPackage,
                L("我", "阿婆，这个包裹是给您的吗？"),
                L("阿婆", "哎呀，这是我托人带的酵母粉，怎么会在车篮里……"),
                L("阿婆", "谢谢你，孩子。这个给你，热乎的。"));
            return;
        }

        Talk(null,
            L("我", "（我没钱买早餐。）"),
            L("阿婆", "先欠着，下次再说。别饿着肚子到处跑。"));
    }

    private void DeliverPackage()
    {
        _hasPackage = false;
        _hasBreakfast = true;
        AddFragments(-1);
        AddFragments(1);
        _hud.ShowToast("获得：阿婆的早餐");
        LightNode(1);

        Talk(null,
            L("阿婆", "等你回来，阿婆再给你留一份。"),
            L("我", "（早餐还带着温度。裁缝铺的灯，好像也亮着。）"));
    }

    // =====================================================================
    // 自行车棚
    // =====================================================================

    private void OnBikeInteract()
    {
        if (_bikeSolved)
        {
            Talk(null,
                L("我", "（自行车已经扶正了，车篮里空空的。）"),
                L("我", "（车脚架还能用，至少不会再倒下来。）"));
            return;
        }

        _bikePuzzle.Open(OnBikeSolved, OnBikeClosed);
    }

    private void OnBikeSolved()
    {
        _bikeSolved = true;
        _hasPackage = true;
        AddFragments(1);
        _hud.ShowToast("获得：麻绳包裹");
        _hud.ShowToast("微光碎片 +1");

        for (int i = 0; i < 3; i++)
        {
            if (_bikeRenderers[i] == null) continue;
            Sprite sprite = _bikeRenderers[i].sprite;
            float centerY = GroundCenterY(sprite, 1.28f);
            _bikeRenderers[i].transform.localRotation = Quaternion.identity;
            _bikeRenderers[i].transform.localPosition = new Vector3(BikeXs[i], centerY, 0f);
        }

        Talk(null,
            L("我", "车脚架只是松了，扶起来还能用。"),
            L("我", "（包裹上别着「阿婆早餐店」的纸条，先送过去吧。）"));
    }

    private void OnBikeClosed()
    {
        Talk(null, L("我", "（还是先把倒下的车扶起来吧。）"));
    }

    // =====================================================================
    // 花园
    // =====================================================================

    private void OnGardenInteract()
    {
        if (!_gardenSolved)
        {
            Talk(() => _gardenPuzzle.Open(OnGardenSolved, null),
                L("我", "（隔壁花园似乎很久没人打理了……）"),
                L("我", "（花坛里的花都枯了，可它们原来的位置好像还记得。）"));
            return;
        }

        if (!_gardenPlanted)
        {
            Talk(null,
                L("我", "（中间的花坛已经活过来了。）"),
                L("我", "（左边还有一块空地，可以种点什么。）"));
            return;
        }

        Talk(null, L("我", "（花坛里的花，在风里轻轻晃着。）"));
    }

    private void OnGardenSolved()
    {
        _gardenSolved = true;
        if (_centerBedDead != null) _centerBedDead.SetActive(false);
        if (_centerBedBloom != null) _centerBedBloom.SetActive(true);
        _hud.ShowToast("花坛重新焕发了生机");

        Talk(null,
            L("园丁", "……是你把花救活的？"),
            L("园丁", "我试了好多回，怎么摆都不对。"),
            L("我", "（花坛的顺序，和它们原本的样子有关。）"));
    }

    private void OnGardenerInteract()
    {
        if (!_gardenSolved)
        {
            Talk(null,
                L("园丁", "这些花……我试了很多次，都救不回来。"),
                L("园丁", "（他指了指中间的花坛，又低下头去。）"));
            return;
        }

        if (!_hasSeed && !_gardenPlanted)
        {
            _hasSeed = true;
            AddFragments(1);
            _hud.ShowToast("获得：花种子");
            _hud.ShowToast("微光碎片 +1");

            Talk(null,
                L("园丁", "这些种子你拿着。"),
                L("园丁", "左边的空地一直空着，我总想着……总该种点什么。"),
                L("我", "（种子在掌心微微发亮。）"));
            return;
        }

        if (_hasSeed && !_gardenPlanted)
        {
            Talk(null, L("园丁", "把种子种进左边的空花坛吧，土我已经松过了。"));
            return;
        }

        Talk(null,
            L("园丁", "明年春天，这里会开满花。"),
            L("园丁", "到时候，你也来看看。"));
    }

    private void OnPlantInteract()
    {
        if (_gardenPlanted)
        {
            Talk(null, L("我", "（花开得很好。）"));
            return;
        }

        if (!_hasSeed)
        {
            Talk(null,
                L("我", "（花坛空着，土也干了。）"),
                L("我", "（也许园丁那里有种子。）"));
            return;
        }

        Talk(() =>
        {
            _hasSeed = false;
            _gardenPlanted = true;
            if (_leftBedEmpty != null) _leftBedEmpty.SetActive(false);
            if (_leftBedBloom != null) _leftBedBloom.SetActive(true);
            _hud.ShowToast("花园的节点亮起来了");
            LightNode(2);

            Talk(null,
                L("园丁", "……发芽了。"),
                L("我", "（四个方向的微光，好像又近了一点。）"));
        },
        L("我", "（把种子埋进土里。）"),
        L("我", "（微光顺着指尖落了下去。）"));
    }

    // =====================================================================
    // 裁缝铺
    // =====================================================================

    private void OnTailorInteract()
    {
        if (!_metTailor)
        {
            Talk(() =>
            {
                _metTailor = true;
                _hasChe = true;
                if (_chePiece != null) _chePiece.gameObject.SetActive(false);
                _hud.ShowToast("获得：棋子「车」");
            },
            L("裁缝", "……门没锁，进来吧。"),
            L("我", "师傅，我在找一枚棋子。"),
            L("裁缝", "棋子？门口那块石头底下好像压着一个。"),
            L("我", "（原来在这里。）"));
            return;
        }

        if (_nodeLit[3])
        {
            Talk(null,
                L("裁缝", "夜深了，路上小心。"),
                L("裁缝", "衣裳改好了，就挂在门后。"));
            return;
        }

        if (!_hasBreakfast)
        {
            Talk(null,
                L("裁缝", "……我还不饿。"),
                L("我", "（也许阿婆那里有热乎的早餐。）"));
            return;
        }

        if (_fragments < 1)
        {
            Talk(null,
                L("我", "（手里的微光还不够亮。）"),
                L("裁缝", "……还站着做什么？"));
            return;
        }

        Talk(DeliverBreakfast,
            L("我", "师傅，您还没吃早饭吧？"),
            L("裁缝", "……习惯了。"),
            L("我", "阿婆让我带了一份，还热着。"),
            L("裁缝", "……多少钱？"),
            L("我", "不用，阿婆说街坊之间不用算这个。"),
            L("裁缝", "（他停下手里的活，从抽屉里翻出一个小布包。）"),
            L("裁缝", "这个卒，很多年前就落在我这儿了。你拿去吧。"));
    }

    private void DeliverBreakfast()
    {
        _hasBreakfast = false;
        _hasZu = true;
        AddFragments(-1);
        AddFragments(1);
        _hud.ShowToast("获得：棋子「卒」");
        LightNode(3);

        Talk(null,
            L("裁缝", "……替我把门带上。"),
            L("我", "（四个人的微光，好像开始互相靠近了。）"));
    }

    // =====================================================================
    // 节点与结局
    // =====================================================================

    private void LightNode(int index)
    {
        if (index < 0 || index >= 4 || _nodeLit[index]) return;
        _nodeLit[index] = true;
        _hud.SetNode(index, true);
        if (_nodeGlows[index] != null)
            _nodeGlows[index].color = new Color(1f, 0.82f, 0.52f, 0.55f);

        _hud.ShowToast("微光节点已点亮 · " + NodeNames[index]);
        _lastNodeLightAt = Time.unscaledTime;
    }

    private int CountLitNodes()
    {
        int count = 0;
        for (int i = 0; i < 4; i++) if (_nodeLit[i]) count++;
        return count;
    }

    private IEnumerator EndingRoutine()
    {
        _transitioning = true;
        _player.canMove = false;
        ClearFocus();

        yield return new WaitForSecondsRealtime(1.2f);

        _hud.HideAll();
        _transitioning = false;

        List<Level2Line> lines = new List<Level2Line>
        {
            L("我", "棋盘、早餐店、花园、裁缝铺……"),
            L("我", "原来这些微光，一直都是连在一起的。"),
            L("我", "天快亮了。"),
        };

        _ending.Play(lines, _dialogue, ReturnToTitle);
    }

    private void ReturnToTitle()
    {
        RestoreLegacyBag();
        if (Application.CanStreamedLevelBeLoaded("StartScene"))
        {
            SceneManager.LoadScene("StartScene");
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.Log("[Level2] 没有可返回的场景，请手动把 StartScene 加入 Build Settings。");
    }

    // =====================================================================
    // 背包与旧系统隔离
    // =====================================================================

    private List<Level2BagEntry> BuildBagEntries()
    {
        List<Level2BagEntry> entries = new List<Level2BagEntry>();

        if (_hasPackage)
            entries.Add(new Level2BagEntry(_itemPackage, "麻绳包裹",
                "油纸包得严严实实，别着「阿婆早餐店」的纸条。", "×1"));

        if (_hasBreakfast)
            entries.Add(new Level2BagEntry(_itemBreakfast, "阿婆的早餐",
                "还带着温度的豆浆和饭团。", "×1"));

        if (_hasSeed)
            entries.Add(new Level2BagEntry(_itemSeed, "花种子",
                "园丁给的种子，在微光下泛着淡淡的光。", "×1"));

        if (_hasChe)
            entries.Add(new Level2BagEntry(_pieceCheIcon, "棋子「车」",
                "沉甸甸的木棋子，底部刻着一个「车」字。", "×1"));

        if (_hasZu)
            entries.Add(new Level2BagEntry(_pieceZuIcon, "棋子「卒」",
                "边角磨得发亮，像是被人摩挲了很多年。", "×1"));

        return entries;
    }

    private void SuppressLegacyBag()
    {
        _legacyInventory = FindObjectsOfType<InventoryUI>(true);
        for (int i = 0; i < _legacyInventory.Length; i++)
        {
            if (_legacyInventory[i] != null) _legacyInventory[i].enabled = false;
        }

        List<Canvas> canvases = new List<Canvas>();
        Canvas[] all = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < all.Length; i++)
        {
            Canvas c = all[i];
            if (c != null && c.name == "BagCanvas" && c.gameObject.scene.IsValid())
            {
                c.gameObject.SetActive(false);
                canvases.Add(c);
            }
        }
        _legacyCanvases = canvases.ToArray();
    }

    private void RestoreLegacyBag()
    {
        if (_legacyInventory != null)
        {
            for (int i = 0; i < _legacyInventory.Length; i++)
                if (_legacyInventory[i] != null) _legacyInventory[i].enabled = true;
            _legacyInventory = null;
        }

        if (_legacyCanvases != null)
        {
            for (int i = 0; i < _legacyCanvases.Length; i++)
                if (_legacyCanvases[i] != null) _legacyCanvases[i].gameObject.SetActive(true);
            _legacyCanvases = null;
        }
    }

    // =====================================================================
    // 工具
    // =====================================================================

    private void AddFragments(int delta)
{
    int before = _fragments;                       // ★ 记录改前值
    _fragments = Mathf.Max(0, _fragments + delta);
    _hud.SetFragments(_fragments);

    // ★ 新增：只有真的增加了才响（delta > 0 且数量确实变了）
    if (delta > 0 && _fragments > before)
        Level2SFX.Play(Level2SFX.ShardPickup, 1f);
}


    private void Talk(Action onDone, params Level2Line[] lines)
    {
        _dialogue.Show(new List<Level2Line>(lines), onDone);
    }

    private static Level2Line L(string speaker, string text)
    {
        return new Level2Line(speaker, text);
    }
}
