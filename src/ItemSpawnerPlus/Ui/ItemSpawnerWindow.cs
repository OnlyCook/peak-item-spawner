using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItemSpawnerPlus
{
    // MenuWindow subclass so the game's own systems give us cursor unlock, player
    // input blocking and pause/cancel close for free (GUIManager.UpdateWindowStatus
    // walks MenuWindow.AllActiveWindows). Visuals are all procedural (see ModChrome),
    // rendered on an overlay canvas above the game UI
    public class ItemSpawnerWindow : MenuWindow
    {
        public override bool openOnStart => false;
        public override bool selectOnOpen => false;
        public override bool closeOnPause => true;
        public override bool closeOnUICancel => true;
        public override GameObject panel => _root;

        public bool MenuOpen { get; private set; }

        private ManualLogSource _log;
        private PluginConfig _cfg;

        private GameObject _root;
        private Image _dimImage;
        private float _dimFadeElapsed;
        private const float DimFadeDuration = 0.25f;

        private RectTransform _panelRect;
        private Image _panelFillImage;
        private Image _grainImage;
        private TextMeshProUGUI _titleText;
        private RectTransform _footerRow;
        private TextMeshProUGUI _footerKeyText;
        private TextMeshProUGUI _footerLabelText;
        private TextMeshProUGUI _emptyText;
        private TMP_InputField _searchInput;
        private TextMeshProUGUI _searchPlaceholder;
        private GameObject _clearBtn;
        private const float ClearBtnSize = 20f;
        private int _lastLang = -1;
        private bool _lastShowInternal;
        private RectTransform _gridContent;
        private ScrollRect _scrollRect;
        private float _savedScroll = 1f;
        private TMP_FontAsset _font;

        private GameObject _loadingRoot;
        private bool _uiWarmedUp;
        private bool _warmingUp;
        private bool _heavyBuilt;
        private bool _entriesBuilt;

        private int _jagFrame;
        private float _jagFrameTimer;
        private int _lastPanelW, _lastPanelH;

        private readonly List<Item> _items = new List<Item>();
        private readonly List<GameObject> _tiles = new List<GameObject>();
        private readonly List<string> _tileSearchNames = new List<string>();
        private readonly List<ItemClass> _tileClass = new List<ItemClass>();
        private readonly List<ItemCategory> _tileCategory = new List<ItemCategory>();

        private enum FilterKey { Vanilla, Modded, Special, Food, Equipment }

        private RectTransform _filterBtnRect;
        private GameObject _filterMenu;
        private RectTransform _filterMenuRect;
        private bool _filterOpen;
        private readonly Dictionary<FilterKey, bool> _filterState = new()
        {
            [FilterKey.Vanilla] = true, [FilterKey.Modded] = true, [FilterKey.Special] = true,
            [FilterKey.Food] = true, [FilterKey.Equipment] = true,
        };
        private readonly Dictionary<FilterKey, (Image box, Image tick, TextMeshProUGUI label)> _filterRows = new();

        private RectTransform _cookBtnRect;
        private GameObject _cookMenu;
        private RectTransform _cookMenuRect;
        private bool _cookOpen;
        private int _cookLevel;
        private Slider _cookSlider;
        private TextMeshProUGUI _cookNameLabel;
        private TextMeshProUGUI _cookTitleLabel;

        private const float ReferenceHeight = 1080f;
        private static float CanvasWidthUnits => (float)Screen.width / Screen.height * ReferenceHeight;
        private const float PanelWidth = 1120f;
        // vertical gap between stacked elements and panel top/bottom pad
        private const float Margin = 24f;
        // horizontal inset from the panel edge to the title / search bar / grid, wider
        // than Margin because the baked panel border eats ~11px of it
        private const float GridSide = 44f;
        private const float TitleHeight = 44f;
        private const float SearchHeight = 46f;
        private const float FooterHeight = 36f;
        private const float FilterBtnSize = 46f;
        private const float FilterGap = 8f;
        private const int GridColumns = 6;
        private const int MaxVisibleRows = 4;
        private const float CellH = 172f;
        private const float CellSpacing = 12f;
        // padding between the scroll container's own background panel and the tiles
        private const float GridInset = 16f;
        private const float ScrollbarWidth = 9f;

        private const float GridTopInset = Margin + TitleHeight + Margin + SearchHeight + Margin;
        private const float GridBottomInset = Margin + FooterHeight + Margin;

        // panel height tracks the item count (capped at MaxVisibleRows) so there is
        // never a void below the grid
        private const float PanelChromeHeight = GridTopInset + GridBottomInset;

        private static readonly MethodInfo _baseOpen =
            typeof(MenuWindow).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo _baseClose =
            typeof(MenuWindow).GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // internal on CharacterItems, so bound by reflection (spawning is not a hot path)
        private static readonly MethodInfo _spawnInHand =
            typeof(CharacterItems).GetMethod("SpawnItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public void Init(ManualLogSource log, PluginConfig cfg)
        {
            _log = log;
            _cfg = cfg;
        }

        private void Awake()
        {
            // built here (not lazily) so MenuWindow.Start's StartClosed() has a
            // non-null panel to deactivate, the heavy sprite bake is still deferred
            try
            {
                _font = ModChrome.FindGameFont();
                _root = new GameObject("ItemSpawnerPlus_Menu", typeof(RectTransform));
                _root.transform.SetParent(transform, false);
                var canvas = _root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                _root.AddComponent<GraphicRaycaster>();
                ModChrome.ApplyWidescreenScaler(canvas);
                _root.SetActive(false);
            }
            catch (Exception e)
            {
                _log?.LogError($"ItemSpawnerWindow.Awake failed (menu will not render): {e}");
            }
        }

        public void ToggleMenu()
        {
            if (MenuOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            if (MenuOpen || _root == null) return;
            EnsureEventSystem();
            MenuOpen = true;

            if (!_uiWarmedUp)
            {
                EnsureLoadingUi();
                _loadingRoot?.SetActive(true);
                if (!_warmingUp)
                {
                    _warmingUp = true;
                    StartCoroutine(WarmUpThenShow());
                }
            }
            else
            {
                ShowReal(skipDimFade: false);
            }
        }

        public void CloseMenu()
        {
            if (!MenuOpen) return;
            MenuOpen = false;
            _loadingRoot?.SetActive(false);
            InvokeBase(_baseClose);
            if (_root != null && _root.activeSelf && !MenuWindow.AllActiveWindows.Contains(this))
                _root.SetActive(false);
        }

        private IEnumerator WarmUpThenShow()
        {
            yield return null;
            ShowReal(skipDimFade: true);
            _loadingRoot?.SetActive(false);
            _uiWarmedUp = true;
            _warmingUp = false;
        }

        private void ShowReal(bool skipDimFade)
        {
            EnsureHeavyUi();

            // real Open() runs Show()/Initialize()/OnOpen()/SetInputActive and adds
            // us to AllActiveWindows, which is what drives cursor + input blocking
            if (!InvokeBase(_baseOpen))
            {
                if (!MenuWindow.AllActiveWindows.Contains(this)) MenuWindow.AllActiveWindows.Add(this);
                _root.SetActive(true);
                if (!_entriesBuilt) { RefreshEntries(); _entriesBuilt = true; }
                SetInputActive(active: true);
            }

            RefreshFooter(); // after activation so its layout rebuild takes effect
            LayoutPanel();
            RelayoutTiles(restoreScroll: true);
            StartCoroutine(RelayoutNextFrame());
            FocusSearch();

            if (skipDimFade)
            {
                if (_dimImage != null) _dimImage.color = ModChrome.DimColor;
                _dimFadeElapsed = DimFadeDuration;
            }
            else
            {
                if (_dimImage != null)
                    _dimImage.color = new Color(ModChrome.DimColor.r, ModChrome.DimColor.g, ModChrome.DimColor.b, 0f);
                _dimFadeElapsed = 0f;
            }
        }

        protected override void Initialize()
        {
            if (!_entriesBuilt) { RefreshEntries(); _entriesBuilt = true; }
        }

        protected override void OnClose()
        {
            MenuOpen = false;
            if (_scrollRect != null) _savedScroll = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition);
            _loadingRoot?.SetActive(false);
            SetFilterMenu(false);
            SetCookMenu(false);
            // eat the same-frame pause the Escape close would otherwise trigger
            PauseSuppressPatch.SuppressNextOpen();
        }

        private bool InvokeBase(MethodInfo m)
        {
            if (m == null) return false;
            try { m.Invoke(this, null); return true; }
            catch (Exception e) { _log?.LogWarning($"ItemSpawnerWindow: base {m.Name}() invoke failed: {e.Message}"); return false; }
        }

        private static bool PointerOver(RectTransform rt) =>
            rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);

        protected override void Update()
        {
            base.Update(); // keeps closeOnPause / closeOnUICancel (Esc) working

            if (_heavyBuilt)
            {
                int lang = (int)LocalizedText.CURRENT_LANGUAGE;
                bool internalNames = _cfg != null && _cfg.ShowInternalNames.Value;
                if (_lastLang < 0) { _lastLang = lang; _lastShowInternal = internalNames; }
                else if (lang != _lastLang || internalNames != _lastShowInternal)
                {
                    _lastLang = lang;
                    _lastShowInternal = internalNames;
                    Relocalize();
                }
            }

            if (!MenuOpen || _root == null || !_root.activeSelf) return;

            // close a dropdown on a click that lands outside it and its own button
            if (Input.GetMouseButtonDown(0))
            {
                if (_filterOpen && !PointerOver(_filterMenuRect) && !PointerOver(_filterBtnRect))
                    SetFilterMenu(false);
                if (_cookOpen && !PointerOver(_cookMenuRect) && !PointerOver(_cookBtnRect))
                    SetCookMenu(false);
            }

            if (_dimImage != null && _dimFadeElapsed < DimFadeDuration)
            {
                _dimFadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_dimFadeElapsed / DimFadeDuration);
                _dimImage.color = new Color(ModChrome.DimColor.r, ModChrome.DimColor.g, ModChrome.DimColor.b, ModChrome.DimColor.a * t);
            }

            if (MinimalUi) return; // flat panel sprite never changes

            _jagFrameTimer += Time.unscaledDeltaTime;
            if (_jagFrameTimer >= ModChrome.JagFrameInterval)
            {
                _jagFrameTimer -= ModChrome.JagFrameInterval;
                _jagFrame = (_jagFrame + 1) % ModChrome.JagFrameCount;
                if (_panelFillImage != null && _lastPanelW > 0)
                    _panelFillImage.sprite = ModChrome.PanelSprite(_lastPanelW, _lastPanelH, _jagFrame, false);
            }
        }

        private bool MinimalUi => _cfg != null && _cfg.MinimalUi.Value;

        private static void EnsureEventSystem()
        {
            try
            {
                if (EventSystem.current != null) return;
                if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
                var go = new GameObject("ItemSpawnerPlus_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            catch { }
        }

        private void FocusSearch()
        {
            try
            {
                if (_searchInput == null) return;
                _searchInput.Select();
                _searchInput.ActivateInputField();
            }
            catch { }
        }

        private void EnsureLoadingUi()
        {
            if (_loadingRoot != null || _root == null) return;
            try
            {
                _loadingRoot = new GameObject("ItemSpawnerPlus_Loading", typeof(RectTransform));
                _loadingRoot.transform.SetParent(transform, false);
                var canvas = _loadingRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                ModChrome.ApplyWidescreenScaler(canvas);

                var dimGo = new GameObject("Dim", typeof(RectTransform));
                dimGo.transform.SetParent(_loadingRoot.transform, false);
                var dim = dimGo.AddComponent<Image>();
                dim.color = ModChrome.DimColor;
                ModChrome.StretchFull((RectTransform)dimGo.transform);

                var text = ModChrome.MakeText(_loadingRoot.transform, "LoadingText", 30f, ModChrome.TitleColor,
                    TextAlignmentOptions.Center, _font);
                ModChrome.ApplyChromeTextStyle(text);
                text.text = SpawnerLocalization.Get(SpawnerText.Loading);
                ModChrome.StretchFull((RectTransform)text.transform);

                _loadingRoot.SetActive(false);
            }
            catch (Exception e)
            {
                _log?.LogError($"ItemSpawnerWindow.EnsureLoadingUi failed (non-fatal): {e}");
            }
        }

        private void EnsureHeavyUi()
        {
            if (_heavyBuilt || _root == null) return;
            try
            {
                var dimGo = new GameObject("Dim", typeof(RectTransform));
                dimGo.transform.SetParent(_root.transform, false);
                _dimImage = dimGo.AddComponent<Image>();
                _dimImage.color = new Color(ModChrome.DimColor.r, ModChrome.DimColor.g, ModChrome.DimColor.b, 0f);
                ModChrome.StretchFull((RectTransform)dimGo.transform);

                var panelGo = new GameObject("Panel", typeof(RectTransform));
                panelGo.transform.SetParent(_root.transform, false);
                _panelFillImage = panelGo.AddComponent<Image>();
                _panelFillImage.type = Image.Type.Simple;
                _panelFillImage.color = Color.white;
                _panelRect = (RectTransform)panelGo.transform;
                _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                _panelRect.pivot = new Vector2(0.5f, 0.5f);

                // separate inset Mask child: putting Mask on the panel Image itself
                // swaps it to a stencil-only material and drops the border
                var maskGo = new GameObject("GrainMask", typeof(RectTransform));
                maskGo.transform.SetParent(panelGo.transform, false);
                var maskImage = maskGo.AddComponent<Image>();
                maskImage.sprite = ModChrome.PanelInnerMaskSprite();
                maskImage.type = Image.Type.Sliced;
                maskImage.color = Color.white;
                var mask = maskGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var maskRect = (RectTransform)maskGo.transform;
                maskRect.anchorMin = Vector2.zero;
                maskRect.anchorMax = Vector2.one;
                maskRect.offsetMin = new Vector2(ModChrome.PanelBorderThickness, ModChrome.PanelBorderThickness);
                maskRect.offsetMax = new Vector2(-ModChrome.PanelBorderThickness, -ModChrome.PanelBorderThickness);

                var grainGo = new GameObject("Grain", typeof(RectTransform));
                grainGo.transform.SetParent(maskGo.transform, false);
                _grainImage = grainGo.AddComponent<Image>();
                _grainImage.sprite = Sprite.Create(ModChrome.PanelGrainTexture(),
                    new Rect(0, 0, ModChrome.GrainTextureSize, ModChrome.GrainTextureSize), new Vector2(0.5f, 0.5f), 100f);
                _grainImage.type = Image.Type.Simple;
                _grainImage.color = Color.white;
                _grainImage.raycastTarget = false;
                ModChrome.StretchFull((RectTransform)grainGo.transform);

                BuildTitle(panelGo.transform);
                BuildSearch(panelGo.transform);
                BuildGrid(panelGo.transform);
                BuildFooter(panelGo.transform);
                BuildCookButton(panelGo.transform);
                BuildFilterButton(panelGo.transform);
                BuildCookMenu(panelGo.transform);
                BuildFilterMenu(panelGo.transform); // last: renders above the grid

                _heavyBuilt = true;

                // build + position the tiles while _root is still inactive: their
                // Button color tint then applies instantly on the SetActive that
                // MenuWindow.Open does, instead of flashing white for a frame
                LayoutPanel();
                if (!_entriesBuilt) { RefreshEntries(); _entriesBuilt = true; }
                RelayoutTiles();
            }
            catch (Exception e)
            {
                _log?.LogError($"ItemSpawnerWindow.EnsureHeavyUi failed (non-fatal, menu will not render): {e}");
            }
        }

        private void BuildTitle(Transform panel)
        {
            _titleText = ModChrome.MakeText(panel, "Title", 30f, ModChrome.TitleColor, TextAlignmentOptions.Top, _font);
            ModChrome.ApplyChromeTextStyle(_titleText);
            _titleText.text = SpawnerLocalization.Get(SpawnerText.Title);
            var rt = (RectTransform)_titleText.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(GridSide, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-GridSide, rt.offsetMax.y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, TitleHeight);
            rt.anchoredPosition = new Vector2(0f, -Margin);
        }

        private void BuildSearch(Transform panel)
        {
            var bgGo = new GameObject("SearchBar", typeof(RectTransform));
            bgGo.transform.SetParent(panel, false);
            var bg = bgGo.AddComponent<Image>();
            bg.sprite = ModChrome.MakeCapSprite(10f);
            bg.type = Image.Type.Sliced;
            bg.color = ModChrome.PanelInsetColor;
            // stretched horizontally: drive both x AND y purely from offsetMin/offsetMax
            // (setting anchoredPosition.x afterwards would re-centre it and discard the
            // asymmetric right inset that makes room for the filter button)
            var bgRect = (RectTransform)bgGo.transform;
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.offsetMin = new Vector2(GridSide, -(Margin + TitleHeight + Margin + SearchHeight));
            bgRect.offsetMax = new Vector2(-(GridSide + 2f * (FilterBtnSize + FilterGap)), -(Margin + TitleHeight + Margin));

            var areaGo = new GameObject("TextArea", typeof(RectTransform));
            areaGo.transform.SetParent(bgGo.transform, false);
            areaGo.AddComponent<RectMask2D>();
            var areaRect = (RectTransform)areaGo.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            // right inset leaves room for the clear button; the asymmetric vertical
            // inset nudges the string onto the bar's optical centre (Daruma sits low)
            areaRect.offsetMin = new Vector2(18f, 9f);
            areaRect.offsetMax = new Vector2(-(18f + ClearBtnSize + 6f), -3f);

            // Left (= vertical Middle, a font metric): stable, never shifts as the
            // typed string's descenders/ascenders change, unlike the geometry-based
            // MidlineLeft
            _searchPlaceholder = ModChrome.MakeText(areaGo.transform, "Placeholder", 20f,
                new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Left, _font);
            _searchPlaceholder.text = SpawnerLocalization.Get(SpawnerText.SearchPlaceholder);
            ModChrome.StretchFull((RectTransform)_searchPlaceholder.transform);
            var placeholder = _searchPlaceholder;

            var text = ModChrome.MakeText(areaGo.transform, "Text", 20f, ModChrome.TitleColor,
                TextAlignmentOptions.Left, _font);
            ModChrome.StretchFull((RectTransform)text.transform);

            _searchInput = bgGo.AddComponent<TMP_InputField>();
            _searchInput.textViewport = areaRect;
            _searchInput.textComponent = text;
            _searchInput.placeholder = placeholder;
            if (_font != null) _searchInput.fontAsset = _font;
            _searchInput.pointSize = 20f;
            _searchInput.lineType = TMP_InputField.LineType.SingleLine;
            _searchInput.customCaretColor = true;
            _searchInput.caretColor = ModChrome.TitleColor;
            _searchInput.caretWidth = 2;
            _searchInput.selectionColor = new Color(1f, 0.82f, 0.22f, 0.4f);
            _searchInput.onValueChanged.AddListener(OnSearchChanged);

            var clearGo = new GameObject("ClearButton", typeof(RectTransform));
            clearGo.transform.SetParent(bgGo.transform, false);
            var cImg = clearGo.AddComponent<Image>();
            cImg.sprite = ModChrome.ClearSprite();
            cImg.color = Color.white; // Button tint does the shading
            var cBtn = clearGo.AddComponent<Button>();
            cBtn.targetGraphic = cImg;
            var cc = cBtn.colors;
            cc.normalColor = new Color(1f, 1f, 1f, 0.55f);
            cc.highlightedColor = Color.white;
            cc.pressedColor = new Color(1f, 0.85f, 0.4f, 1f);
            cc.fadeDuration = 0.06f;
            cBtn.colors = cc;
            cBtn.onClick.AddListener(ClearSearch);
            var cr = (RectTransform)clearGo.transform;
            cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
            cr.pivot = new Vector2(1f, 0.5f);
            cr.sizeDelta = new Vector2(ClearBtnSize, ClearBtnSize);
            cr.anchoredPosition = new Vector2(-13f, 0f);
            _clearBtn = clearGo;
            _clearBtn.SetActive(false);
        }

        private void ClearSearch()
        {
            if (_searchInput == null) return;
            _searchInput.text = string.Empty;
            try { _searchInput.ActivateInputField(); } catch { }
        }

        private void BuildGrid(Transform panel)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panel, false);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.22f);
            scrollImg.sprite = ModChrome.MakeCapSprite(12f);
            scrollImg.type = Image.Type.Sliced;
            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(GridSide, GridBottomInset);
            scrollRt.offsetMax = new Vector2(-GridSide, -GridTopInset);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            viewportGo.AddComponent<RectMask2D>();
            var viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportRt.pivot = new Vector2(0f, 1f);

            // top-left corner anchored, size set explicitly in RelayoutTiles, tiles
            // positioned by hand (GridLayoutGroup / ContentSizeFitter both fought this)
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _gridContent = (RectTransform)contentGo.transform;
            _gridContent.anchorMin = new Vector2(0f, 1f);
            _gridContent.anchorMax = new Vector2(0f, 1f);
            _gridContent.pivot = new Vector2(0f, 1f);
            _gridContent.anchoredPosition = Vector2.zero;

            BuildScrollbar(panel);

            _scrollRect.viewport = viewportRt;
            _scrollRect.content = _gridContent;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 40f;
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            _emptyText = ModChrome.MakeText(scrollGo.transform, "Empty", 20f, ModChrome.FooterColor,
                TextAlignmentOptions.Center, _font);
            ModChrome.StretchFull((RectTransform)_emptyText.transform);
            _emptyText.gameObject.SetActive(false);
        }

        // lives in the panel's right-hand Margin band (which holds no tiles) so it
        // never steals width from the grid or offsets it
        private void BuildScrollbar(Transform panel)
        {
            var barGo = new GameObject("Scrollbar", typeof(RectTransform));
            barGo.transform.SetParent(panel, false);
            var barImg = barGo.AddComponent<Image>();
            barImg.sprite = ModChrome.MakeCapSprite(ScrollbarWidth * 0.5f);
            barImg.type = Image.Type.Sliced;
            barImg.color = new Color(0f, 0f, 0f, 0.30f);
            var barRt = (RectTransform)barGo.transform;
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.sizeDelta = new Vector2(ScrollbarWidth, -(GridTopInset + GridBottomInset));
            // centered in the visible right band (panel border inner edge <-> last column)
            barRt.anchoredPosition = new Vector2(
                -(GridSide + ModChrome.PanelBorderThickness) * 0.5f,
                (GridBottomInset - GridTopInset) * 0.5f);

            var areaGo = new GameObject("SlidingArea", typeof(RectTransform));
            areaGo.transform.SetParent(barGo.transform, false);
            var areaRt = (RectTransform)areaGo.transform;
            ModChrome.StretchFull(areaRt);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(areaGo.transform, false);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = ModChrome.MakeCapSprite(ScrollbarWidth * 0.5f);
            handleImg.type = Image.Type.Sliced;
            handleImg.color = Color.white; // Scrollbar color tint multiplies this, keep it neutral
            var handleRt = (RectTransform)handleGo.transform;
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            var sb = barGo.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.handleRect = handleRt;
            sb.targetGraphic = handleImg;
            var colors = sb.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.32f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.5f);
            colors.pressedColor = new Color(1f, 0.82f, 0.22f, 0.75f);
            sb.colors = colors;

            _scrollRect.verticalScrollbar = sb;
        }

        private void BuildFooter(Transform panel)
        {
            var rowGo = new GameObject("Footer", typeof(RectTransform));
            rowGo.transform.SetParent(panel, false);
            var rowRect = (RectTransform)rowGo.transform;
            _footerRow = rowRect;
            rowRect.anchorMin = new Vector2(0.5f, 0f);
            rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.sizeDelta = new Vector2(400f, FooterHeight);
            rowRect.anchoredPosition = new Vector2(0f, Margin);

            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var rowFitter = rowGo.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var badgeGo = new GameObject("Badge", typeof(RectTransform));
            badgeGo.transform.SetParent(rowGo.transform, false);
            var badgeImage = badgeGo.AddComponent<Image>();
            badgeImage.sprite = ModChrome.BadgeSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = Color.white;
            var badgeLayout = badgeGo.AddComponent<HorizontalLayoutGroup>();
            badgeLayout.childAlignment = TextAnchor.MiddleCenter;
            badgeLayout.padding = new RectOffset(10, 10, 4, 4);
            badgeLayout.childControlWidth = true;
            badgeLayout.childControlHeight = true;
            var badgeFitter = badgeGo.AddComponent<ContentSizeFitter>();
            badgeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            badgeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _footerKeyText = ModChrome.MakeText(badgeGo.transform, "Key", 16f, ModChrome.KeyTextColor,
                TextAlignmentOptions.Midline, _font);

            _footerLabelText = ModChrome.MakeText(rowGo.transform, "Label", 17f, ModChrome.FooterColor,
                TextAlignmentOptions.Midline, _font);
            ModChrome.ApplyChromeTextStyle(_footerLabelText);
            var labelFitter = _footerLabelText.gameObject.AddComponent<ContentSizeFitter>();
            labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RefreshFooter()
        {
            if (_footerKeyText == null) return;
            string key = _cfg != null ? _cfg.ToggleKey.Value.ToString() : "F5";
            _footerKeyText.text = $"{key} / Esc";
            _footerLabelText.text = SpawnerLocalization.Get(SpawnerText.Close);

            // TMP preferred size is stale until the mesh regenerates, so a rebound key
            // or a re-localized label would not resize the badge/row without this
            if (_footerRow != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_footerRow);
            }
        }

        // re-pull every localized string (called on a live language switch); tile
        // names come from Item.GetName() so RefreshEntries re-sorts + relabels them
        private void Relocalize()
        {
            try
            {
                if (_titleText != null) _titleText.text = SpawnerLocalization.Get(SpawnerText.Title);
                if (_searchPlaceholder != null) _searchPlaceholder.text = SpawnerLocalization.Get(SpawnerText.SearchPlaceholder);
                RefreshFooter();
                RelocalizeFilterMenu();
                UpdateCookLabels();
                RefreshEntries();
                OnSearchChanged(_searchInput != null ? _searchInput.text : string.Empty);
            }
            catch (Exception e)
            {
                _log?.LogWarning($"ItemSpawnerWindow.Relocalize failed: {e.Message}");
            }
        }

        // --- filter button + dropdown ---

        private void BuildFilterButton(Transform panel)
        {
            var go = new GameObject("FilterButton", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            _filterBtnRect = (RectTransform)go.transform;
            _filterBtnRect.anchorMin = _filterBtnRect.anchorMax = new Vector2(1f, 1f);
            _filterBtnRect.pivot = new Vector2(1f, 1f);
            _filterBtnRect.sizeDelta = new Vector2(FilterBtnSize, SearchHeight);
            _filterBtnRect.anchoredPosition = new Vector2(-GridSide, -(Margin + TitleHeight + Margin));

            var bg = go.AddComponent<Image>();
            bg.sprite = ModChrome.MakeCapSprite(10f);
            bg.type = Image.Type.Sliced;
            bg.color = ModChrome.PanelInsetColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            var hv = go.AddComponent<TileHover>();
            hv.Target = bg;
            hv.Normal = ModChrome.PanelInsetColor;
            hv.Hover = ModChrome.TileHoverColor;
            hv.Press = ModChrome.TilePressColor;
            btn.onClick.AddListener(ToggleFilterMenu);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = ModChrome.FilterSprite();
            icon.raycastTarget = false;
            icon.color = Color.white;
            var ir = (RectTransform)iconGo.transform;
            ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(30f, 30f);
        }

        private const float FilterRowH = 40f;
        private const float FilterMenuW = 224f;
        private const float FilterMenuPad = 10f;
        private const float FilterSectionGap = 12f;

        private static readonly (FilterKey key, SpawnerText label)[] _filterDefs =
        {
            (FilterKey.Vanilla, SpawnerText.FilterVanilla),
            (FilterKey.Modded, SpawnerText.FilterModded),
            (FilterKey.Special, SpawnerText.FilterSpecial),
            (FilterKey.Food, SpawnerText.FilterFood),
            (FilterKey.Equipment, SpawnerText.FilterEquipment),
        };

        private void BuildFilterMenu(Transform panel)
        {
            _filterMenu = new GameObject("FilterMenu", typeof(RectTransform));
            _filterMenu.transform.SetParent(panel, false);
            _filterMenuRect = (RectTransform)_filterMenu.transform;
            _filterMenuRect.anchorMin = _filterMenuRect.anchorMax = new Vector2(1f, 1f);
            _filterMenuRect.pivot = new Vector2(1f, 1f);
            float h = FilterMenuPad * 2f + _filterDefs.Length * FilterRowH + FilterSectionGap;
            _filterMenuRect.sizeDelta = new Vector2(FilterMenuW, h);
            _filterMenuRect.anchoredPosition = new Vector2(-GridSide, -(Margin + TitleHeight + Margin + SearchHeight + 6f));

            var bg = _filterMenu.AddComponent<Image>();
            bg.sprite = ModChrome.MakeCapSprite(12f);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(ModChrome.PanelBorderColor.r, ModChrome.PanelBorderColor.g, ModChrome.PanelBorderColor.b, 0.98f);

            _filterRows.Clear();
            for (int i = 0; i < _filterDefs.Length; i++)
            {
                // the class facet (first 3) and the category facet (last 3) sit apart
                float y = FilterMenuPad + i * FilterRowH + (i >= 3 ? FilterSectionGap : 0f);
                BuildFilterRow(_filterDefs[i].key, _filterDefs[i].label, y);
            }

            _filterMenu.SetActive(false);
            RefreshFilterChecks();
        }

        private void BuildFilterRow(FilterKey key, SpawnerText label, float topOffset)
        {
            var rowGo = new GameObject("Row_" + key, typeof(RectTransform));
            rowGo.transform.SetParent(_filterMenu.transform, false);
            var rr = (RectTransform)rowGo.transform;
            rr.anchorMin = new Vector2(0f, 1f);
            rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.offsetMin = new Vector2(FilterMenuPad, -(topOffset + FilterRowH));
            rr.offsetMax = new Vector2(-FilterMenuPad, -topOffset);

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = ModChrome.MakeCapSprite(8f);
            rowBg.type = Image.Type.Sliced;
            rowBg.color = new Color(0f, 0f, 0f, 0f);
            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = rowBg;
            btn.transition = Selectable.Transition.None;
            var hv = rowGo.AddComponent<TileHover>();
            hv.Target = rowBg;
            hv.Normal = new Color(0f, 0f, 0f, 0f);
            hv.Hover = new Color(1f, 1f, 1f, 0.10f);
            hv.Press = new Color(1f, 1f, 1f, 0.16f);
            btn.onClick.AddListener(() => OnFilterToggle(key));

            var boxGo = new GameObject("Box", typeof(RectTransform));
            boxGo.transform.SetParent(rowGo.transform, false);
            var box = boxGo.AddComponent<Image>();
            box.sprite = ModChrome.MakeCapSprite(5f);
            box.type = Image.Type.Sliced;
            box.raycastTarget = false;
            var br = (RectTransform)boxGo.transform;
            br.anchorMin = br.anchorMax = new Vector2(0f, 0.5f);
            br.pivot = new Vector2(0f, 0.5f);
            br.sizeDelta = new Vector2(22f, 22f);
            br.anchoredPosition = new Vector2(4f, 0f);

            var tickGo = new GameObject("Tick", typeof(RectTransform));
            tickGo.transform.SetParent(boxGo.transform, false);
            var tick = tickGo.AddComponent<Image>();
            tick.sprite = ModChrome.CheckSprite();
            tick.raycastTarget = false;
            tick.color = new Color(0.10f, 0.09f, 0.03f);
            ModChrome.StretchFull((RectTransform)tickGo.transform);

            var lbl = ModChrome.MakeText(rowGo.transform, "Label", 17f, ModChrome.TitleColor,
                TextAlignmentOptions.MidlineLeft, _font);
            lbl.raycastTarget = false;
            lbl.text = SpawnerLocalization.Get(label);
            var lr = (RectTransform)lbl.transform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.offsetMin = new Vector2(36f, 0f);
            lr.offsetMax = new Vector2(-4f, 0f);

            _filterRows[key] = (box, tick, lbl);
        }

        private bool ClassAllowed(ItemClass c)
        {
            var k = c == ItemClass.Modded ? FilterKey.Modded
                : c == ItemClass.Special ? FilterKey.Special : FilterKey.Vanilla;
            return _filterState[k];
        }

        // OR within the category facet; an item in neither group is unaffected
        private bool CategoryAllowed(ItemCategory cat)
        {
            if (cat == ItemCategory.None) return true;
            if ((cat & ItemCategory.Food) != 0 && _filterState[FilterKey.Food]) return true;
            if ((cat & ItemCategory.Equipment) != 0 && _filterState[FilterKey.Equipment]) return true;
            return false;
        }

        private void OnFilterToggle(FilterKey key)
        {
            _filterState[key] = !_filterState[key];
            RefreshFilterChecks();
            OnSearchChanged(_searchInput != null ? _searchInput.text : string.Empty);
        }

        private void RefreshFilterChecks()
        {
            foreach (var kv in _filterRows)
            {
                bool on = _filterState[kv.Key];
                var (box, tick, _) = kv.Value;
                if (box != null) box.color = on ? ModChrome.TileHoverColor : new Color(0f, 0f, 0f, 0.45f);
                if (tick != null) tick.enabled = on;
            }
        }

        private void RelocalizeFilterMenu()
        {
            foreach (var def in _filterDefs)
                if (_filterRows.TryGetValue(def.key, out var r) && r.label != null)
                    r.label.text = SpawnerLocalization.Get(def.label);
        }

        private void ToggleFilterMenu() { if (!_filterOpen) SetCookMenu(false); SetFilterMenu(!_filterOpen); }

        private void SetFilterMenu(bool open)
        {
            _filterOpen = open;
            if (_filterMenu != null) _filterMenu.SetActive(open);
        }

        // --- cook level button + dropdown ---

        private void BuildCookButton(Transform panel)
        {
            var go = new GameObject("CookButton", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            _cookBtnRect = (RectTransform)go.transform;
            _cookBtnRect.anchorMin = _cookBtnRect.anchorMax = new Vector2(1f, 1f);
            _cookBtnRect.pivot = new Vector2(1f, 1f);
            _cookBtnRect.sizeDelta = new Vector2(FilterBtnSize, SearchHeight);
            _cookBtnRect.anchoredPosition = new Vector2(-(GridSide + FilterBtnSize + FilterGap), -(Margin + TitleHeight + Margin));

            var bg = go.AddComponent<Image>();
            bg.sprite = ModChrome.MakeCapSprite(10f);
            bg.type = Image.Type.Sliced;
            bg.color = ModChrome.PanelInsetColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            var hv = go.AddComponent<TileHover>();
            hv.Target = bg;
            hv.Normal = ModChrome.PanelInsetColor;
            hv.Hover = ModChrome.TileHoverColor;
            hv.Press = ModChrome.TilePressColor;
            btn.onClick.AddListener(ToggleCookMenu);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = ModChrome.FlameSprite();
            icon.raycastTarget = false;
            icon.color = Color.white;
            var ir = (RectTransform)iconGo.transform;
            ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(30f, 30f);
        }

        private const float CookMenuW = 268f;
        private const float CookMenuPad = 14f;
        private const float CookSliderH = 22f; // == handle diameter
        private const int CookMax = 12;

        private void BuildCookMenu(Transform panel)
        {
            _cookMenu = new GameObject("CookMenu", typeof(RectTransform));
            _cookMenu.transform.SetParent(panel, false);
            _cookMenuRect = (RectTransform)_cookMenu.transform;
            _cookMenuRect.anchorMin = _cookMenuRect.anchorMax = new Vector2(1f, 1f);
            _cookMenuRect.pivot = new Vector2(1f, 1f);
            _cookMenuRect.sizeDelta = new Vector2(CookMenuW, CookMenuPad * 2f + 16f + 22f + 10f + CookSliderH);
            _cookMenuRect.anchoredPosition = new Vector2(-(GridSide + FilterBtnSize + FilterGap),
                -(Margin + TitleHeight + Margin + SearchHeight + 6f));

            var bg = _cookMenu.AddComponent<Image>();
            bg.sprite = ModChrome.MakeCapSprite(12f);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(ModChrome.PanelBorderColor.r, ModChrome.PanelBorderColor.g, ModChrome.PanelBorderColor.b, 0.98f);

            _cookTitleLabel = ModChrome.MakeText(_cookMenu.transform, "Title", 15f,
                new Color(0.82f, 0.87f, 1f), TextAlignmentOptions.Center, _font);
            _cookTitleLabel.raycastTarget = false;
            var tr = (RectTransform)_cookTitleLabel.transform;
            tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
            tr.offsetMin = new Vector2(CookMenuPad, -(CookMenuPad + 16f));
            tr.offsetMax = new Vector2(-CookMenuPad, -CookMenuPad);

            _cookNameLabel = ModChrome.MakeText(_cookMenu.transform, "Name", 18f, ModChrome.TitleColor,
                TextAlignmentOptions.Center, _font);
            _cookNameLabel.raycastTarget = false;
            var nr = (RectTransform)_cookNameLabel.transform;
            nr.anchorMin = new Vector2(0f, 1f); nr.anchorMax = new Vector2(1f, 1f); nr.pivot = new Vector2(0.5f, 1f);
            nr.offsetMin = new Vector2(CookMenuPad, -(CookMenuPad + 16f + 24f));
            nr.offsetMax = new Vector2(-CookMenuPad, -(CookMenuPad + 16f));

            BuildCookSlider();

            _cookMenu.SetActive(false);
            UpdateCookLabels();
        }

        private void BuildCookSlider()
        {
            const float hh = CookSliderH * 0.5f;
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(_cookMenu.transform, false);
            var sr = (RectTransform)sliderGo.transform;
            sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(1f, 0f); sr.pivot = new Vector2(0.5f, 0f);
            sr.offsetMin = new Vector2(CookMenuPad + 4f, CookMenuPad);
            sr.offsetMax = new Vector2(-(CookMenuPad + 4f), CookMenuPad + CookSliderH);

            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(sliderGo.transform, false);
            var trackImg = track.AddComponent<Image>();
            trackImg.sprite = ModChrome.MakeCapSprite(4f);
            trackImg.type = Image.Type.Sliced;
            trackImg.color = new Color(0f, 0f, 0f, 0.45f);
            var trr = (RectTransform)track.transform;
            trr.anchorMin = new Vector2(0f, 0.5f); trr.anchorMax = new Vector2(1f, 0.5f); trr.pivot = new Vector2(0.5f, 0.5f);
            trr.offsetMin = new Vector2(hh, -4f); trr.offsetMax = new Vector2(-hh, 4f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var far = (RectTransform)fillArea.transform;
            far.anchorMin = new Vector2(0f, 0.5f); far.anchorMax = new Vector2(1f, 0.5f); far.pivot = new Vector2(0.5f, 0.5f);
            far.offsetMin = new Vector2(hh, -4f); far.offsetMax = new Vector2(-hh, 4f);
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = ModChrome.MakeCapSprite(4f);
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.80f, 0.56f, 0.20f, 0.98f);
            var flr = (RectTransform)fill.transform;
            flr.anchorMin = Vector2.zero; flr.anchorMax = Vector2.one; flr.offsetMin = Vector2.zero; flr.offsetMax = Vector2.zero;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var har = (RectTransform)handleArea.transform;
            har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one;
            har.offsetMin = new Vector2(hh, 0f); har.offsetMax = new Vector2(-hh, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = ModChrome.MakeCapSprite(24f); // full circle
            handleImg.type = Image.Type.Simple;
            handleImg.color = new Color(1f, 0.95f, 0.85f);
            var hr = (RectTransform)handle.transform;
            hr.sizeDelta = new Vector2(CookSliderH, 0f); // width fixed, height = HandleArea height (== CookSliderH)

            _cookSlider = sliderGo.AddComponent<Slider>();
            _cookSlider.direction = Slider.Direction.LeftToRight;
            _cookSlider.minValue = 0f;
            _cookSlider.maxValue = CookMax;
            _cookSlider.wholeNumbers = true;
            _cookSlider.fillRect = flr;
            _cookSlider.handleRect = hr;
            _cookSlider.targetGraphic = handleImg;
            _cookSlider.SetValueWithoutNotify(_cookLevel);
            _cookSlider.onValueChanged.AddListener(OnCookChanged);
        }

        private static string CookName(int level)
        {
            SpawnerText k = level <= 0 ? SpawnerText.CookUncooked
                : level == 1 ? SpawnerText.CookCooked
                : level == 2 ? SpawnerText.CookWellDone
                : level == 3 ? SpawnerText.CookBurnt
                : SpawnerText.CookIncinerated;
            return SpawnerLocalization.Get(k);
        }

        private void UpdateCookLabels()
        {
            if (_cookTitleLabel != null) _cookTitleLabel.text = SpawnerLocalization.Get(SpawnerText.CookTitle);
            if (_cookNameLabel != null) _cookNameLabel.text = _cookLevel + " · " + CookName(_cookLevel);
        }

        private void OnCookChanged(float v)
        {
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 0, CookMax);
            // levels 3+ all render identically (Burnt/Incinerated), skip the icon repaint
            bool visualChange = ClampVisual(lvl) != ClampVisual(_cookLevel);
            _cookLevel = lvl;
            UpdateCookLabels();
            if (visualChange) ApplyCookTint();
        }

        private static int ClampVisual(int level) => level > 3 ? 3 : level;

        private static Color CookColor(int level)
        {
            try { return ItemCooking.GetCookColor(level); }
            catch { return Color.white; }
        }

        private void ApplyCookTint()
        {
            var c = CookColor(_cookLevel);
            for (int i = 0; i < _tiles.Count; i++)
            {
                var icon = _tiles[i] != null ? _tiles[i].GetComponentInChildren<RawImage>(true) : null;
                if (icon != null) icon.color = c;
            }
        }

        private void ToggleCookMenu() { if (!_cookOpen) SetFilterMenu(false); SetCookMenu(!_cookOpen); }

        private void SetCookMenu(bool open)
        {
            _cookOpen = open;
            if (_cookMenu != null) _cookMenu.SetActive(open);
        }

        private void LayoutPanel()
        {
            if (_panelRect == null) return;
            int rows = Mathf.Max(1, Mathf.CeilToInt(_items.Count / (float)GridColumns));
            int visRows = Mathf.Clamp(rows, 1, MaxVisibleRows);
            float bodyH = visRows * CellH + (visRows - 1) * CellSpacing + 2f * GridInset;
            float w = Mathf.Min(PanelWidth, CanvasWidthUnits - 80f) + 2f * ModChrome.PanelOuterMargin;
            float h = Mathf.Min(PanelChromeHeight + bodyH, ReferenceHeight - 100f) + 2f * ModChrome.PanelOuterMargin;
            _panelRect.sizeDelta = new Vector2(w, h);
            _lastPanelW = Mathf.RoundToInt(w);
            _lastPanelH = Mathf.RoundToInt(h);
            bool minimal = MinimalUi;
            _panelFillImage.sprite = ModChrome.PanelSprite(_lastPanelW, _lastPanelH, _jagFrame, minimal);
            if (_grainImage != null) _grainImage.gameObject.SetActive(!minimal);
        }

        private IEnumerator RelayoutNextFrame()
        {
            yield return null;
            if (MenuOpen) RelayoutTiles(restoreScroll: true);
        }

        // positions every visible tile by hand in a 6-column grid spanning
        // panelW - 2*GridSide; the scrollbar lives in the right GridSide band and
        // never affects this. restoreScroll keeps the previous scroll offset (menu
        // re-open), otherwise it jumps to the top (a search / filter change)
        private void RelayoutTiles(bool restoreScroll = false)
        {
            if (_gridContent == null || _scrollRect == null) return;
            try
            {
                float scrollW = _lastPanelW - 2f * GridSide;
                float cellW = Mathf.Max(40f, (scrollW - 2f * GridInset - (GridColumns - 1) * CellSpacing) / GridColumns);

                int slot = 0;
                for (int i = 0; i < _tiles.Count; i++)
                {
                    var t = _tiles[i];
                    if (!t.activeSelf) continue;
                    int row = slot / GridColumns;
                    int col = slot % GridColumns;
                    var rt = (RectTransform)t.transform;
                    rt.sizeDelta = new Vector2(cellW, CellH);
                    rt.anchoredPosition = new Vector2(
                        GridInset + col * (cellW + CellSpacing),
                        -(GridInset + row * (CellH + CellSpacing)));
                    slot++;
                }

                int rows = Mathf.Max(1, Mathf.CeilToInt(slot / (float)GridColumns));
                _gridContent.sizeDelta = new Vector2(scrollW,
                    2f * GridInset + rows * CellH + (rows - 1) * CellSpacing);
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = restoreScroll ? Mathf.Clamp01(_savedScroll) : 1f;
            }
            catch { }
        }

        private void RefreshEntries()
        {
            try
            {
                _items.Clear();
                var db = ItemDatabase.Instance;
                if (db != null && db.Objects != null)
                {
                    // no IsValidToSpawn() filter: it hides coop-only items in solo
                    // (Cursed Skull, Ritual Dagger, ...) and custom-run-disabled items,
                    // which a cheat spawner should still let you spawn. props / unused
                    // variants are surfaced via the Special tile class instead
                    foreach (var item in db.Objects)
                        if (item != null) _items.Add(item);
                }
                _items.Sort((a, b) => string.Compare(TileLabel(a), TileLabel(b), StringComparison.OrdinalIgnoreCase));

                _tileSearchNames.Clear();
                _tileClass.Clear();
                _tileCategory.Clear();
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (i < _tiles.Count) UpdateTile(_tiles[i], item);
                    else _tiles.Add(BuildTile(item));
                    _tileSearchNames.Add(SearchBlob(item));
                    _tileClass.Add(ItemClassifier.Classify(item));
                    _tileCategory.Add(ItemClassifier.CategoriesOf(item));
                }
                for (int i = _items.Count; i < _tiles.Count; i++)
                    _tiles[i].SetActive(false);

                _emptyText.text = SpawnerLocalization.Get(SpawnerText.NoItems);
                _emptyText.gameObject.SetActive(_items.Count == 0);

                _log?.LogInfo($"Item Spawner Plus: listed {_items.Count} spawnable item(s).");
            }
            catch (Exception e)
            {
                _log?.LogError($"ItemSpawnerWindow.RefreshEntries failed: {e}");
            }
        }

        // the localized name the player normally sees; falls back to the raw
        // UIData.itemName (a loc key for some items) then the prefab name
        private static string DisplayName(Item item)
        {
            try
            {
                string loc = null;
                try { loc = item.GetName(); } catch { }
                if (!string.IsNullOrEmpty(loc) && !loc.StartsWith("LOC:", StringComparison.OrdinalIgnoreCase))
                    return loc;
            }
            catch { }
            return RawName(item);
        }

        private static string RawName(Item item)
        {
            try
            {
                string n = item.UIData != null ? item.UIData.itemName : null;
                return string.IsNullOrEmpty(n) ? item.gameObject.name : n;
            }
            catch { return item.gameObject.name; }
        }

        // what the tile actually shows / sorts by (config toggles internal vs localized)
        private string TileLabel(Item item) =>
            (_cfg != null && _cfg.ShowInternalNames.Value) ? RawName(item) : DisplayName(item);

        // matched against the search box: the localized name, both internal names, and
        // (when the localized name is Chinese) its pinyin + initials so pinyin typists
        // can find items too. every form is whitespace-stripped (and the query is too)
        // so "mian hua tang" and "mianhuatang" both match
        private static string SearchBlob(Item item)
        {
            string display = DisplayName(item);
            string raw = RawName(item);
            string go = null;
            try { go = item.gameObject.name; } catch { }

            var sb = new System.Text.StringBuilder(96);
            sb.Append(Squash(display)).Append('\n').Append(Squash(raw)).Append('\n').Append(Squash(go));
            PinyinIndex.Append(display, sb); // pinyin forms are already whitespace-free
            return sb.ToString().ToLowerInvariant();
        }

        // normalises both the query and the search blob so matching ignores case
        // (caller lowercases), whitespace, hyphens/dashes, apostrophes and diacritics:
        // "cure all" / "cure-all" -> "cureall"; "grune"/"grüne" -> "grune";
        // "scouts tenacity" -> "scoutstenacity"; "antidoto" matches "antídoto"
        private static string Squash(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = s.Replace("ß", "ss").Replace("ø", "o").Replace("Ø", "O")
                 .Replace("æ", "ae").Replace("Æ", "AE").Replace("œ", "oe").Replace("Œ", "OE")
                 .Replace("đ", "d").Replace("Đ", "D").Replace("ł", "l").Replace("Ł", "L");
            string nfd;
            try { nfd = s.Normalize(System.Text.NormalizationForm.FormD); } catch { nfd = s; }
            var sb = new System.Text.StringBuilder(nfd.Length);
            foreach (char c in nfd)
            {
                if (System.Char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (char.IsWhiteSpace(c)) continue;
                if (c == '-' || c == '‐' || c == '–' || c == '—') continue;
                if (c == '\'' || c == '’' || c == '‘' || c == '`' || c == '´') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static Texture GetIcon(Item item)
        {
            try { return item.UIData != null ? item.UIData.GetIcon() : null; }
            catch { try { return item.UIData != null ? item.UIData.icon : null; } catch { return null; } }
        }

        private GameObject BuildTile(Item item)
        {
            var tileGo = new GameObject("Tile", typeof(RectTransform));
            tileGo.transform.SetParent(_gridContent, false);
            var tileRt = (RectTransform)tileGo.transform;
            tileRt.anchorMin = tileRt.anchorMax = new Vector2(0f, 1f);
            tileRt.pivot = new Vector2(0f, 1f);
            tileRt.sizeDelta = new Vector2(120f, CellH); // real width set in RelayoutTiles

            var bg = tileGo.AddComponent<Image>();
            bg.sprite = ModChrome.TileSprite();
            bg.type = Image.Type.Sliced;
            bg.color = ModChrome.PanelInsetColor;

            var btn = tileGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None; // TileHover writes bg.color directly

            var hover = tileGo.AddComponent<TileHover>();
            hover.Target = bg;
            hover.Normal = ModChrome.PanelInsetColor;
            hover.Hover = ModChrome.TileHoverColor;
            hover.Press = ModChrome.TilePressColor;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(tileGo.transform, false);
            var icon = iconGo.AddComponent<RawImage>();
            icon.raycastTarget = false;
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(104f, 104f);
            iconRect.anchoredPosition = new Vector2(0f, -14f);

            // fills the gap between the icon's bottom edge and the tile's bottom edge,
            // text centered in it; auto-shrinks (down to fontSizeMin) so very long
            // modded names still fit instead of clipping
            var label = ModChrome.MakeText(tileGo.transform, "Name", 15f, ModChrome.TitleColor,
                TextAlignmentOptions.Center, _font);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8.5f;
            label.fontSizeMax = 15f;
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            // +2 on both: the icon art has empty space top and bottom, so a true
            // centre reads as slightly low
            labelRect.offsetMin = new Vector2(6f, 8f);
            labelRect.offsetMax = new Vector2(-6f, 52f);

            UpdateTile(tileGo, item);
            return tileGo;
        }

        private void UpdateTile(GameObject tileGo, Item item)
        {
            tileGo.SetActive(true);
            tileGo.name = "Tile_" + item.gameObject.name;

            var icon = tileGo.GetComponentInChildren<RawImage>(true);
            if (icon != null)
            {
                var tex = GetIcon(item);
                icon.texture = tex;
                icon.enabled = tex != null;
                icon.color = CookColor(_cookLevel);
            }

            var label = tileGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = TileLabel(item);

            var baseColor = ModChrome.TileColorFor(ItemClassifier.Classify(item));
            var bg = tileGo.GetComponent<Image>();
            if (bg != null) bg.color = baseColor;
            var hover = tileGo.GetComponent<TileHover>();
            if (hover != null) { hover.Normal = baseColor; hover.Apply(); }

            var btn = tileGo.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var captured = item;
            btn.onClick.AddListener(() => Spawn(captured));
        }

        private void OnSearchChanged(string raw)
        {
            if (_clearBtn != null) _clearBtn.SetActive(!string.IsNullOrEmpty(raw));
            string q = Squash(raw ?? string.Empty).ToLowerInvariant();
            int shown = 0;
            for (int i = 0; i < _items.Count && i < _tiles.Count; i++)
            {
                bool classOk = i >= _tileClass.Count || ClassAllowed(_tileClass[i]);
                bool catOk = i >= _tileCategory.Count || CategoryAllowed(_tileCategory[i]);
                bool textOk = q.Length == 0 || (i < _tileSearchNames.Count && _tileSearchNames[i].IndexOf(q, StringComparison.Ordinal) >= 0);
                bool match = classOk && catOk && textOk;
                if (_tiles[i].activeSelf != match) _tiles[i].SetActive(match);
                if (match) shown++;
            }
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(_items.Count == 0 || shown == 0);
                _emptyText.text = SpawnerLocalization.Get(_items.Count == 0 ? SpawnerText.NoItems : SpawnerText.NoMatches);
            }
            RelayoutTiles();
        }

        private void Spawn(Item item)
        {
            try
            {
                var local = Character.localCharacter;
                if (!PhotonNetwork.IsConnected || local == null || local.refs == null || local.refs.items == null || _spawnInHand == null)
                {
                    _log?.LogInfo("Item Spawner Plus: spawn ignored, not in an active run.");
                    return;
                }

                // snapshot BEFORE the spawn (which may run synchronously when we are the
                // master) so CookAfterSpawn can tell the new instance apart
                HashSet<int> before = null;
                if (_cookLevel > 0)
                {
                    before = new HashSet<int>();
                    try { foreach (var it in Item.ALL_ACTIVE_ITEMS) if (it != null) before.Add(it.GetInstanceID()); }
                    catch { before = null; }
                }

                _spawnInHand.Invoke(local.refs.items, new object[] { item.gameObject.name });
                _log?.LogInfo($"Item Spawner Plus: spawned {item.gameObject.name}.");

                if (before != null) StartCoroutine(CookAfterSpawn(item.itemID, _cookLevel, before));
            }
            catch (Exception e)
            {
                _log?.LogError($"ItemSpawnerWindow.Spawn failed: {e}");
            }
        }

        private static int ReadCook(Item it)
        {
            try
            {
                if (it.data != null && it.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var v))
                    return v.Value;
            }
            catch { }
            return 0;
        }

        // the spawn RPCs to the master and the item replicates back a few frames later
        // (in hand, or on the ground if the inventory is full). watch ALL_ACTIVE_ITEMS
        // for the new instance, then keep re-sending SetCookedAmountRPC until it sticks
        // (an inventory sync from the master can otherwise reset it right after)
        private IEnumerator CookAfterSpawn(ushort itemId, int level, HashSet<int> seen)
        {
            Item target = null;
            for (int i = 0; i < 90 && target == null; i++)
            {
                yield return null;
                try
                {
                    foreach (var it in Item.ALL_ACTIVE_ITEMS)
                    {
                        if (it == null || it.itemID != itemId || seen.Contains(it.GetInstanceID())) continue;
                        if (it.photonView == null) continue;
                        target = it;
                        break;
                    }
                }
                catch { }
            }
            if (target == null)
            {
                _log?.LogInfo($"Item Spawner Plus: cook target (id {itemId}) not found, item spawned uncooked.");
                yield break;
            }

            for (int j = 0; j < 24; j++)
            {
                try
                {
                    target.photonView.RPC("SetCookedAmountRPC", RpcTarget.All, level);
                    if (target.data != null)
                    {
                        target.GetData<IntItemData>(DataEntryKey.CookedAmount).Value = level;
                        if (target.cooking != null) target.cooking.UpdateCookedBehavior();
                    }
                }
                catch (Exception e) { _log?.LogWarning($"CookAfterSpawn RPC failed: {e.Message}"); }

                for (int k = 0; k < 5; k++) yield return null;
                if (target == null || ReadCook(target) == level) yield break;
            }
        }
    }
}
