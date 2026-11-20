using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UniGameUI.Editor
{
    public sealed partial class UIBlueprintWindow : EditorWindow
    {
        internal const float NodeWidth = 210;
        internal const float NodeHeight = 104;
        // 「顯示全部」時，範圍框和畫布邊緣之間留的距離（視窗的像素）。
        private const float FramePadding = 24;
        // 範圍框和最外圍的節點之間留的距離（畫布的單位）。
        internal const float FrameMargin = 36;
        // 範圍框左上角的標示佔的高度（畫布的單位）。
        private const float FrameCaptionHeight = 26;
        // 背景格線的間距（畫布的單位）。
        private const float GridStep = 24;
        // 滑鼠移動超過這個距離（視窗的像素）才算拖曳，否則是點擊。
        private const float DragThreshold = 4;
        private const string GuidePath = "Packages/com.chrono.unigameui/Documentation~/AgentGuide.md";
        private static readonly Color PageColor = new Color32(109, 184, 238, 255);
        private static readonly Color PopupColor = new Color32(195, 156, 237, 255);
        private static readonly Color GridMinorColor = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color GridMajorColor = new Color(1f, 1f, 1f, 0.09f);
        private static readonly Color FrameColor = new Color(1f, 1f, 1f, 0.38f);
        internal const string AnySourceTitle = "任何畫面";

        [SerializeField] private UIRoot workspace;
        [SerializeField] private string selectedId;
        [SerializeField] private string selectedAction;
        private readonly Dictionary<string, VisualElement> cards = new Dictionary<string, VisualElement>();
        // 節點在畫布上的位置（畫布座標）。拖曳途中先改這裡，放開才寫回工作區。
        private readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        private readonly List<(UIRoute Route, Label Label)> edgeLabels = new List<(UIRoute, Label)>();
        private VisualElement sidebar;
        private VisualElement canvas;
        private VisualElement grid;
        private VisualElement edges;
        private VisualElement content;
        private VisualElement frame;
        private Label frameCaption;
        private ToolbarButton zoomButton;
        private TextField actionField;
        private UIBlueprintViewport view = UIBlueprintViewport.Default;
        private string viewKey;
        private bool frameWhenSized;
        // 拖線的終點，畫布座標。
        private Vector2? wireTip;
        private string wireSource;
        private string wireAction;
        private string wireOriginalAction;
        private string dragId;
        private Vector2 dragPointerStart;
        private Vector2 dragNodeStart;
        private bool dragMoved;
        private bool panning;
        private Vector2 panPointer;
        private bool releasing;

        /// <summary>畫布區域還沒有排版時（例如測試裡），計算「顯示全部」用的大小。</summary>
        internal Vector2 FallbackViewSize = new Vector2(960, 600);

        [MenuItem("Tools/UniGameUI/Blueprint", priority = 1)]
        public static void Open()
        {
            GetWindow<UIBlueprintWindow>("UniGameUI 藍圖").Show();
        }

        [MenuItem("Tools/UniGameUI/Frame All UI", priority = 2)]
        private static void FrameAll()
        {
            var root = UIAuthoring.FindWorkspace(SceneManager.GetActiveScene());
            if (root)
                UIAuthoring.FocusReview(root);
        }

        [MenuItem("Tools/UniGameUI/Agent Guide", priority = 30)]
        private static void OpenGuide()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UIBlueprintWindow).Assembly);
            var path = package == null ? GuidePath : System.IO.Path.Combine(package.resolvedPath, "Documentation~/AgentGuide.md");
            EditorUtility.OpenWithDefaultApp(path);
        }

        public void CreateGUI()
        {
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.chrono.unigameui/Editor/UIBlueprint.uss");
            if (stylesheet)
                rootVisualElement.styleSheets.Add(stylesheet);
            if (!workspace)
                workspace = UIAuthoring.FindWorkspace(SceneManager.GetActiveScene());
            Rebuild();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Rebuild;
            EditorApplication.playModeStateChanged += RefreshOnPlayModeChange;
            EditorSceneManager.activeSceneChangedInEditMode += ActiveSceneChanged;
            EditorSceneManager.sceneOpened += SceneOpened;
            EditorSceneManager.sceneClosed += SceneClosed;
        }

        private void OnDisable()
        {
            // 視窗關閉或 Domain 重新載入：不留下任何對執行中工作區的訂閱。
            StopLiveUpdate();
            UnsubscribeLive();
            Undo.undoRedoPerformed -= Rebuild;
            EditorApplication.playModeStateChanged -= RefreshOnPlayModeChange;
            EditorSceneManager.activeSceneChangedInEditMode -= ActiveSceneChanged;
            EditorSceneManager.sceneOpened -= SceneOpened;
            EditorSceneManager.sceneClosed -= SceneClosed;
        }

        private void ActiveSceneChanged(Scene previous, Scene current)
        {
            SetWorkspace(UIAuthoring.FindWorkspace(current));
        }

        private void SceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
                SetWorkspace(UIAuthoring.FindWorkspace(scene));
            else if (!workspace)
                SetWorkspace(UIAuthoring.FindWorkspace(SceneManager.GetActiveScene()));
        }

        private void SceneClosed(Scene scene)
        {
            if (!workspace || workspace.gameObject.scene == scene)
            {
                var active = SceneManager.GetActiveScene();
                SetWorkspace(active == scene ? null : UIAuthoring.FindWorkspace(active));
            }
        }

        private void SetWorkspace(UIRoot next)
        {
            if (workspace != next || !next)
            {
                selectedId = null;
                selectedAction = null;
            }
            workspace = next;
            Rebuild();
        }

        private void RefreshOnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    EndLive();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EndLive();
                    Rebuild();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    EndLive();
                    // 執行中的工作區已經銷毀，回到 Edit Mode 的那一個。
                    if (!workspace)
                        workspace = UIAuthoring.FindWorkspace(SceneManager.GetActiveScene());
                    Rebuild();
                    break;
            }
        }

        private void Rebuild()
        {
            StopLiveUpdate();
            UnsubscribeLive();
            liveMode = EditorApplication.isPlaying || injectedLiveState != null;
            if (liveMode && injectedLiveState == null)
                ResolveLiveWorkspace();
            selectedAction = SelectedRouteAction();
            rootVisualElement.Clear();
            rootVisualElement.EnableInClassList("blueprint-live", liveMode);
            cards.Clear();
            positions.Clear();
            edgeLabels.Clear();
            wireSource = null;
            wireTip = null;
            dragId = null;
            panning = false;
            canvas = null;
            content = null;
            sidebar = null;
            zoomButton = null;
            ClearRouteErrors();

            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);
            var field = new ObjectField("工作區") { objectType = typeof(UIRoot), allowSceneObjects = true, value = workspace };
            field.AddToClassList("workspace-picker");
            field.RegisterValueChangedCallback(change => SetWorkspace(change.newValue as UIRoot));
            toolbar.Add(field);
            AddToolbarButton(toolbar, "重新整理", () =>
            {
                if (!workspace)
                    workspace = UIAuthoring.FindWorkspace(SceneManager.GetActiveScene());
                Rebuild();
            });
            AddToolbarButton(toolbar, "Agent 指南", OpenGuide);

            var dotween = UIDOTweenSetup.Status;
            if (dotween != UIDOTweenStatus.Ready)
            {
                rootVisualElement.Add(new HelpBox(UIDOTweenSetup.SetupMessage(dotween), HelpBoxMessageType.Warning));
                var setup = new Button(UIDOTweenSetup.ShowPrompt) { text = "安裝或設定 DOTween" };
                setup.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                rootVisualElement.Add(setup);
            }

            if (!workspace)
            {
                if (liveMode)
                {
                    // Play Mode 中沒有可以顯示的工作區：持續找，載入的 Scene 有工作區時切換過去。
                    rootVisualElement.Add(new HelpBox(liveLost
                        ? "執行中的 UI 工作區已經停用或銷毀（例如換了 Scene）。載入的 Scene 有啟用中的工作區時，藍圖會自動切換過去。"
                        : "Play Mode 中沒有啟用中的 UI 工作區。載入的 Scene 有工作區時，藍圖會自動切換過去。",
                        HelpBoxMessageType.Info) { name = "live-no-workspace" });
                    if (injectedLiveState == null)
                        StartLiveUpdate();
                    return;
                }
                rootVisualElement.Add(new HelpBox("目前沒有 UI 工作區。可先請 agent 依需求套用計畫，或在這裡建立空白工作區。",
                    HelpBoxMessageType.Info));
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    AddToolbarButton(toolbar, "建立單 Scene 工作區", () => Create(UIAuthoringMode.SingleScene));
                    AddToolbarButton(toolbar, "建立 Prefab 工作區", () => Create(UIAuthoringMode.Prefab));
                }
                return;
            }

            if (!liveMode && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                rootVisualElement.Add(new HelpBox("正在切換 Play Mode。", HelpBoxMessageType.Info));
                return;
            }

            if (liveMode)
            {
                var indicator = new Label("執行中 · 唯讀")
                {
                    name = "live-indicator",
                    tooltip = "Play Mode 中藍圖標示目前的畫面，不能修改：Play Mode 中的修改離開時會被 Unity 還原。",
                };
                indicator.AddToClassList("live-indicator");
                toolbar.Add(indicator);
            }
            else
            {
                AddToolbarButton(toolbar, "檢視全部 UI", () => UIAuthoring.FocusReview(workspace))
                    .tooltip = "在 Scene 視窗框選所有審核畫面。";
                AddToolbarButton(toolbar, "重新排列", () => UIAuthoring.ArrangeReview(workspace));
                AddToolbarButton(toolbar, "Validate", ShowValidation);
                AddToolbarButton(toolbar, "走訪", StartWalk);
                if (workspace.Entries.Any(entry => entry != null &&
                    (UIAuthoring.NeedsViewCanvas(entry.Review) || UIAuthoring.NeedsViewCanvas(entry.Prefab))))
                    AddToolbarButton(toolbar, "補上子 Canvas", () => TryEdit(() => UIAuthoring.RepairViewCanvases(workspace)));
            }

            // 藍圖畫布的視角。
            var frameAll = AddToolbarButton(toolbar, "顯示全部", ShowAllNodes);
            frameAll.name = "frame-all";
            frameAll.tooltip = "調整平移與縮放，讓所有節點都在視窗裡。焦點在畫布上時也可以按 F。";
            zoomButton = AddToolbarButton(toolbar, "100%", ResetZoom);
            zoomButton.name = "zoom-level";
            zoomButton.tooltip = "目前的縮放比例。點一下回到 100%。滾輪縮放，按住中鍵（或 Alt 加左鍵）拖曳平移。";

            // 右欄的寬度可以拖曳，在這個 Editor 工作階段內記住。
            var split = new TwoPaneSplitView(1, SidebarWidth, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "blueprint-split",
            };
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);
            BuildCanvas();
            canvas.style.minWidth = MinCanvasWidth;
            split.Add(canvas);

            sidebar = new ScrollView(ScrollViewMode.Vertical) { name = "blueprint-sidebar" };
            sidebar.AddToClassList("blueprint-sidebar");
            sidebar.style.minWidth = MinSidebarWidth;
            sidebar.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                // 只記使用者拖出來的寬度：寬度沒有變（只有高度變）時不必記。
                if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width) && evt.oldRect.width > 0f)
                    RememberSidebarWidth(evt.newRect.width);
            });
            split.Add(sidebar);
            UpdateStage();
            LoadView();
            if (liveMode)
                BeginLive();
            DrawSidebar();
        }

        private void Create(UIAuthoringMode mode)
        {
            workspace = UIAuthoring.CreateWorkspace(SceneManager.GetActiveScene(), mode);
            Rebuild();
        }

        // 畫布由下到上三層：格線、連線（都用視窗座標畫）、內容（節點與線上的標籤，用畫布座標排，整層跟著視角平移與縮放）。
        private void BuildCanvas()
        {
            canvas = new VisualElement { name = "blueprint-canvas", focusable = true };
            canvas.AddToClassList("blueprint-canvas");
            grid = new VisualElement { name = "blueprint-grid", pickingMode = PickingMode.Ignore };
            grid.AddToClassList("blueprint-layer");
            grid.generateVisualContent += DrawGrid;
            canvas.Add(grid);
            edges = new VisualElement { name = "blueprint-edges", pickingMode = PickingMode.Ignore };
            edges.AddToClassList("blueprint-layer");
            edges.generateVisualContent += DrawEdges;
            canvas.Add(edges);
            content = new VisualElement { name = "blueprint-content", pickingMode = PickingMode.Ignore };
            content.AddToClassList("blueprint-content");
            canvas.Add(content);

            // 範圍框：虛線框住所有節點，左上角標示 Scene 的名稱與數量。它只是標示，不接收滑鼠事件。
            frame = new VisualElement { name = "blueprint-frame", pickingMode = PickingMode.Ignore };
            frame.AddToClassList("blueprint-frame");
            frame.generateVisualContent += DrawFrame;
            frameCaption = new Label { name = "blueprint-frame-caption", pickingMode = PickingMode.Ignore };
            frameCaption.AddToClassList("blueprint-frame-caption");
            frame.Add(frameCaption);
            content.Add(frame);

            foreach (var entry in workspace.Entries)
            {
                positions.Add(entry.Id, entry.GraphPosition);
                CreateCard(entry);
            }
            // 「任何畫面」是固定的節點：不是畫面，不能刪除，只能當訊號路由的來源。
            positions.Add(UIRoot.AnySource, workspace.anySourceGraphPosition);
            CreateAnySourceCard();
            foreach (var route in workspace.Routes.Where(route => UIRoute.TargetsEntry(route.Kind)))
            {
                var label = new Label(route.Action)
                {
                    tooltip = $"{DisplayView(route.Source)} → {DisplayView(route.Target)}（{KindLabel(route.Kind)}）。" +
                        "點一下在右欄選取這條路由。",
                };
                label.AddToClassList("edge-label");
                label.AddToClassList(route.Kind == UIRouteKind.OpenPopup ? "edge-popup" : "edge-page");
                label.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 || evt.altKey)
                        return;
                    canvas.Focus();
                    PressEdgeLabel(label);
                    evt.StopPropagation();
                });
                edgeLabels.Add((route, label));
                content.Add(label);
            }

            canvas.RegisterCallback<WheelEvent>(evt =>
            {
                ZoomAt(evt.localMousePosition, evt.delta.y);
                evt.StopPropagation();
            });
            canvas.RegisterCallback<PointerDownEvent>(evt =>
            {
                canvas.Focus();
                // 按住中鍵拖曳平移；沒有中鍵的裝置用 Alt 加左鍵。
                if (evt.button != 2 && !(evt.button == 0 && evt.altKey))
                {
                    // 節點與線上的標籤會自己處理左鍵；事件到得了這裡，表示按在空白的地方。
                    if (evt.button == 0)
                        ClickCanvas();
                    return;
                }
                panning = true;
                panPointer = WindowPoint(evt.position);
                canvas.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            canvas.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!panning || !canvas.HasPointerCapture(evt.pointerId))
                    return;
                var point = WindowPoint(evt.position);
                PanBy(point - panPointer);
                panPointer = point;
            });
            canvas.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!panning || !canvas.HasPointerCapture(evt.pointerId))
                    return;
                panning = false;
                canvas.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
            canvas.RegisterCallback<PointerCaptureOutEvent>(_ => panning = false);
            canvas.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.F || evt.actionKey || evt.altKey)
                    return;
                ShowAllNodes();
                evt.StopPropagation();
            });
            canvas.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                // 這個工作階段第一次開啟這個 Scene 的藍圖：等畫布有了大小再「顯示全部」一次。
                if (!frameWhenSized || !HasLayout)
                    return;
                frameWhenSized = false;
                ShowAllNodes();
            });
        }

        // 滑鼠事件的位置（整個視窗的座標）換成畫布區域裡的視窗座標。
        private Vector2 WindowPoint(Vector2 worldPosition)
        {
            return canvas.WorldToLocal(worldPosition);
        }

        private bool HasLayout
        {
            get
            {
                if (canvas == null)
                    return false;
                var size = canvas.layout.size;
                return !float.IsNaN(size.x) && !float.IsNaN(size.y) && size.x >= 1f && size.y >= 1f;
            }
        }

        /// <summary>目前的視角。</summary>
        internal UIBlueprintViewport View => view;

        /// <summary>畫布區域的大小（視窗的像素）。還沒有排版時是 <see cref="FallbackViewSize"/>。</summary>
        internal Vector2 ViewSize => HasLayout ? canvas.layout.size : FallbackViewSize;

        /// <summary>節點在畫布上的矩形（畫布座標）。</summary>
        internal Rect NodeRect(string id)
        {
            return new Rect(positions[id], new Vector2(NodeWidth, NodeHeight));
        }

        // 所有節點（包含「任何畫面」）在畫布上佔的範圍。
        private Rect NodeBounds()
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var position in positions.Values)
            {
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position + new Vector2(NodeWidth, NodeHeight));
            }
            return positions.Count == 0 ? new Rect(0, 0, NodeWidth, NodeHeight) : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// 範圍框在畫布上的矩形：所有節點（包含「任何畫面」）佔的範圍，四周加上 <see cref="FrameMargin"/>。
        /// </summary>
        internal Rect FrameRect
        {
            get
            {
                var bounds = NodeBounds();
                return new Rect(bounds.x - FrameMargin, bounds.y - FrameMargin,
                    bounds.width + FrameMargin * 2f, bounds.height + FrameMargin * 2f);
            }
        }

        /// <summary>範圍框左上角的標示：Scene 的名稱，以及頁面與 Popup 各有幾個。</summary>
        internal string FrameCaption
        {
            get
            {
                var sceneName = workspace.gameObject.scene.name;
                var pages = workspace.Entries.Count(entry => entry != null && entry.Kind == UIEntryKind.Page);
                var popups = workspace.Entries.Count(entry => entry != null && entry.Kind == UIEntryKind.Popup);
                return $"{(string.IsNullOrEmpty(sceneName) ? "尚未儲存的 Scene" : sceneName)} · " +
                    $"{pages} 個頁面 · {popups} 個 Popup";
            }
        }

        /// <summary>滾輪縮放：以視窗座標的一點為中心，那一點底下的畫布位置不動。</summary>
        internal void ZoomAt(Vector2 windowPoint, float wheelDelta)
        {
            SetView(view.ZoomedAround(windowPoint, view.Zoom * UIBlueprintViewport.WheelFactor(wheelDelta)));
        }

        /// <summary>把縮放設成指定的比例，以畫布區域的中央為中心。</summary>
        internal void SetZoom(float zoom)
        {
            SetView(view.ZoomedAround(ViewSize * 0.5f, zoom));
        }

        internal void PanBy(Vector2 windowDelta)
        {
            SetView(view.Panned(windowDelta));
        }

        /// <summary>「顯示全部」：調整平移與縮放，讓所有節點都在畫布區域裡。</summary>
        internal void ShowAllNodes()
        {
            if (content == null)
                return;
            // 連範圍框與它上面的標示一起放進視窗。
            var bounds = FrameRect;
            bounds.yMin -= FrameCaptionHeight;
            SetView(UIBlueprintViewport.Framing(bounds, ViewSize, FramePadding));
        }

        private void ResetZoom()
        {
            SetZoom(1f);
        }

        // 視角只存在這個 Editor 工作階段，不寫進 Scene：只是看一下藍圖不應該弄髒 Scene。
        private void SetView(UIBlueprintViewport next)
        {
            view = next;
            if (viewKey != null)
                UIBlueprintSession.SaveView(viewKey, view);
            ApplyView();
        }

        private void ApplyView()
        {
            if (content == null)
                return;
            content.style.translate = new Translate(view.pan.x, view.pan.y);
            content.style.scale = new Scale(new Vector2(view.Zoom, view.Zoom));
            grid.MarkDirtyRepaint();
            edges.MarkDirtyRepaint();
            frame.MarkDirtyRepaint();
            if (zoomButton != null)
                zoomButton.text = $"{Mathf.RoundToInt(view.Zoom * 100f)}%";
        }

        // 每個 Scene 的視角在工作階段內記住。這個工作階段第一次開啟某個 Scene 的藍圖時自動「顯示全部」。
        private void LoadView()
        {
            viewKey = UIBlueprintSession.SceneKey(workspace.gameObject.scene);
            if (UIBlueprintSession.TryLoadView(viewKey, out var saved))
            {
                view = saved;
                ApplyView();
                return;
            }
            frameWhenSized = true;
            ShowAllNodes();
        }

        private void CreateCard(UIEntry entry)
        {
            var isPopup = entry.Kind == UIEntryKind.Popup;
            var card = NewCard("node-" + entry.Id, entry.Id, isPopup ? "popup-node" : "page-node");

            // 種類、起始頁、由程式控制各用一個徽章，不擠在同一行小字裡。
            var badges = AddBadgeRow(card);
            AddBadge(badges, EntryKindLabel(entry.Kind), isPopup ? "badge-popup" : "badge-page");
            if (entry.Id == workspace.StartPageId)
                AddBadge(badges, "起始頁", "badge-start").tooltip = "載入 Scene 之後第一個顯示的頁面。";
            if (UIAuthoring.IsNavigatedByCode(entry))
                AddBadge(badges, "程式控制", "badge-code").tooltip = "這個畫面由程式控制進出，不靠路由。";

            AddNodeText(card, entry.Title, "node-title");
            AddNodeText(card, entry.Id, "node-id").tooltip = "畫面 ID";
            AddLocalActions(card, workspace.Routes.Where(route => route.Source == entry.Id));
            AddPort(card, "從這裡拖曳到另一個節點，建立或重接路由。");
        }

        // 「任何畫面」節點：只有輸出的連接點，沒有線可以連進來。
        private void CreateAnySourceCard()
        {
            var card = NewCard("any-source-node", UIRoot.AnySource, "signal-node");
            AddBadge(AddBadgeRow(card), "訊號", "badge-signal");
            AddNodeText(card, AnySourceTitle, "node-title");
            AddNodeText(card, "SignalAsync(動作, 資料)", "node-id").tooltip =
                "從這裡拉出去的是訊號路由：不論目前顯示哪個畫面都成立，由遊戲程式用 SignalAsync 送出。";
            AddLocalActions(card, workspace.Routes.Where(route => route.IsSignal));
            AddPort(card, "從這裡拖曳到畫面的節點，建立或重接訊號路由。");
        }

        private VisualElement NewCard(string name, string id, string kindClass)
        {
            var card = new VisualElement { name = name };
            card.AddToClassList("blueprint-node");
            card.AddToClassList(kindClass);
            if (id == selectedId)
                card.AddToClassList("selected-node");
            card.style.width = NodeWidth;
            card.style.height = NodeHeight;
            // 執行中的藍圖在節點上方標示「目前」、「Popup 1」等。
            var live = new VisualElement { pickingMode = PickingMode.Ignore };
            live.AddToClassList("node-live");
            card.Add(live);
            cards.Add(id, card);
            content.Add(card);
            RegisterNodePointer(card, id);
            return card;
        }

        private static VisualElement AddBadgeRow(VisualElement card)
        {
            var row = new VisualElement();
            row.AddToClassList("node-badges");
            card.Add(row);
            return row;
        }

        private static Label AddBadge(VisualElement parent, string text, string kindClass)
        {
            var badge = new Label(text);
            badge.AddToClassList("badge");
            badge.AddToClassList(kindClass);
            parent.Add(badge);
            return badge;
        }

        private static Label AddNodeText(VisualElement card, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            card.Add(label);
            return label;
        }

        // 不會拉線到別的畫面的動作（返回、關閉 Popup、送出事件）列在節點的最下面。
        private static void AddLocalActions(VisualElement card, IEnumerable<UIRoute> routes)
        {
            var actions = routes.Where(route => !UIRoute.TargetsEntry(route.Kind))
                .Select(route => $"{route.Action}（{KindLabel(route.Kind)}）");
            var label = AddNodeText(card, string.Join("  ·  ", actions), "node-actions");
            label.tooltip = label.text;
        }

        private static void AddPort(VisualElement card, string tooltip)
        {
            var port = new VisualElement { tooltip = tooltip };
            port.AddToClassList("node-port");
            port.style.top = NodeHeight * 0.5f - 6f;
            card.Add(port);
        }

        internal static string EntryKindLabel(UIEntryKind kind)
        {
            return kind == UIEntryKind.Popup ? "Popup" : "頁面";
        }

        // 整個節點都可以操作：按在連接點上是拖線，其餘的位置是選取與拖曳。
        private void RegisterNodePointer(VisualElement card, string id)
        {
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                // 中鍵與 Alt 加左鍵留給畫布平移。
                if (evt.button != 0 || evt.altKey)
                    return;
                canvas.Focus();
                PressNode(id, evt.target as VisualElement, WindowPoint(evt.position), evt.clickCount);
                card.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (card.HasPointerCapture(evt.pointerId))
                    MovePointer(WindowPoint(evt.position));
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!card.HasPointerCapture(evt.pointerId))
                    return;
                var point = WindowPoint(evt.position);
                // 放掉滑鼠的擷取時會收到「擷取結束」的通知，這裡是正常放開，不要當成被打斷。
                releasing = true;
                card.ReleasePointer(evt.pointerId);
                releasing = false;
                ReleasePointer(point);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerCaptureOutEvent>(_ => CancelPointer());
        }

        /// <summary>要定位到 Scene 裡的畫面時呼叫。沒有指定時用 <see cref="UIAuthoring.FocusReview"/>。</summary>
        internal Action<string> FocusRequested;

        /// <summary>目前選取的節點 ID。沒有選取時是 null 或空字串。</summary>
        internal string SelectedId => selectedId;

        /// <summary>右欄選中的路由的動作名稱。沒有選中時是 null。</summary>
        internal string SelectedAction => SelectedRouteAction();

        /// <summary>
        /// 在節點上按下滑鼠左鍵。<paramref name="target"/> 是按到的元素：連接點是開始拖線；
        /// 其餘的位置（標題、說明文字、空白處）都是選取這個節點並準備拖曳。單擊只選取，雙擊才定位到 Scene。
        /// </summary>
        internal void PressNode(string id, VisualElement target, Vector2 windowPoint, int clickCount = 1)
        {
            // 執行中的藍圖不能修改：只選取，雙擊在 Hierarchy 選取執行中的畫面。
            if (liveMode)
            {
                SelectNode(id);
                if (clickCount >= 2 && id != UIRoot.AnySource)
                    SelectRunningView(id);
                return;
            }
            if (target != null && target.ClassListContains("node-port"))
            {
                BeginWire(id, windowPoint);
                return;
            }

            SelectNode(id);
            if (clickCount >= 2)
            {
                dragId = null;
                // 「任何畫面」不是畫面，Scene 裡沒有東西可以定位。
                if (id != UIRoot.AnySource)
                    RequestFocus(id);
                return;
            }
            BeginNodeDrag(id, windowPoint);
        }

        /// <summary>按著移動：拖線時移動線的終點，否則拖曳節點。</summary>
        internal void MovePointer(Vector2 windowPoint)
        {
            if (wireSource != null)
                MoveWire(windowPoint);
            else
                DragNodeTo(windowPoint);
        }

        /// <summary>放開：拖線時在這一點放下，否則結束拖曳。</summary>
        internal void ReleasePointer(Vector2 windowPoint)
        {
            if (wireSource != null)
                DropWire(windowPoint);
            else
                EndNodeDrag();
        }

        // 操作途中被打斷（例如視窗失去焦點）：拖線取消，拖到一半的節點回到原位。
        private void CancelPointer()
        {
            if (releasing)
                return;
            if (dragId != null && dragMoved && positions.ContainsKey(dragId))
            {
                positions[dragId] = dragNodeStart;
                UpdateStage();
            }
            dragId = null;
            CancelWire();
        }

        /// <summary>點畫布空白的地方：取消選取，右欄回到沒有選取時的內容。</summary>
        internal void ClickCanvas()
        {
            if (string.IsNullOrEmpty(selectedId) && string.IsNullOrEmpty(selectedAction))
                return;
            selectedId = null;
            selectedAction = null;
            RefreshSelection();
        }

        /// <summary>選取一條路由：選取它的來源節點，並在右欄選中這條路由。</summary>
        internal void SelectRoute(string source, string action)
        {
            selectedId = source;
            selectedAction = action;
            addingRoute = false;
            RefreshSelection();
        }

        /// <summary>線上的動作標籤。這條路由沒有畫線（不是切頁或開啟 Popup）時是 null。</summary>
        internal Label EdgeLabel(string source, string action)
        {
            return edgeLabels.FirstOrDefault(pair => pair.Route.Source == source && pair.Route.Action == action).Label;
        }

        /// <summary>點線上的動作標籤：選取那條路由。</summary>
        internal void PressEdgeLabel(Label label)
        {
            var route = edgeLabels.FirstOrDefault(pair => pair.Label == label).Route;
            if (route != null)
                SelectRoute(route.Source, route.Action);
        }

        private void RefreshSelection()
        {
            foreach (var pair in cards)
                pair.Value.EnableInClassList("selected-node", pair.Key == selectedId);
            ClearRouteErrors();
            DrawSidebar();
        }

        /// <summary>開始拖曳節點。<paramref name="windowPoint"/> 是按下的位置（視窗座標）。</summary>
        internal void BeginNodeDrag(string id, Vector2 windowPoint)
        {
            if (liveMode)
                return;
            dragId = id;
            dragPointerStart = windowPoint;
            dragNodeStart = positions[id];
            dragMoved = false;
        }

        /// <summary>
        /// 拖曳途中。滑鼠在視窗裡移動的距離要換算成畫布上的距離，縮放之後節點才會跟著游標走。
        /// </summary>
        internal void DragNodeTo(Vector2 windowPoint)
        {
            if (dragId == null)
                return;
            var delta = windowPoint - dragPointerStart;
            if (!dragMoved && delta.magnitude < DragThreshold)
                return;
            dragMoved = true;
            positions[dragId] = dragNodeStart + view.WindowToCanvasDelta(delta);
            UpdateStage();
        }

        /// <summary>放開。節點有移動時把新的位置寫回工作區並回傳 true；只是點一下時回傳 false。</summary>
        internal bool EndNodeDrag()
        {
            var id = dragId;
            dragId = null;
            if (id == null || !dragMoved)
                return false;
            UIAuthoring.PositionNode(workspace, id, positions[id]);
            return true;
        }

        /// <summary>從節點的連接點開始拖線。</summary>
        internal void BeginWire(string source, Vector2 windowPoint)
        {
            SelectNode(source);
            if (liveMode)
                return;
            wireSource = source;
            wireAction = actionField?.value;
            wireOriginalAction = SelectedRouteAction();
            wireTip = view.WindowToCanvas(windowPoint);
            edges.MarkDirtyRepaint();
        }

        internal void MoveWire(Vector2 windowPoint)
        {
            if (wireSource == null)
                return;
            wireTip = view.WindowToCanvas(windowPoint);
            edges.MarkDirtyRepaint();
        }

        private void CancelWire()
        {
            wireSource = null;
            wireTip = null;
            edges?.MarkDirtyRepaint();
        }

        /// <summary>
        /// 在視窗的某一點放開拖線。底下有另一個畫面的節點時，建立或重接路由：目標是頁面就切頁，是 Popup 就開啟。
        /// </summary>
        internal void DropWire(Vector2 windowPoint)
        {
            if (wireSource == null)
                return;
            var source = wireSource;
            var action = wireAction;
            var originalAction = wireOriginalAction;
            var point = view.WindowToCanvas(windowPoint);
            CancelWire();

            // 「任何畫面」節點沒有線可以連進來，所以只找畫面的節點。
            var target = workspace.Entries.FirstOrDefault(entry => entry.Id != source && NodeRect(entry.Id).Contains(point));
            if (target == null)
                return;
            if (string.IsNullOrWhiteSpace(action))
            {
                var stem = $"open_{target.Id}";
                action = stem;
                for (var suffix = 2; workspace.Routes.Any(route => route.Source == source && route.Action == action); suffix++)
                    action = $"{stem}_{suffix}";
            }
            var original = workspace.Routes.FirstOrDefault(route => route.Source == source &&
                route.Action == originalAction);
            var kind = target.Kind == UIEntryKind.Popup ? UIRouteKind.OpenPopup : UIRouteKind.ShowPage;
            // 重接到另一種畫面時，原本的 history 不一定適用（Popup 沒有 Replace），回到預設值。
            var history = original != null && original.Kind == kind ? original.History : UIHistoryMode.Push;
            TryEdit(() => SaveRoute(source, originalAction, action, kind, target.Id, history, null));
        }

        private string SelectedRouteAction()
        {
            if (!workspace || string.IsNullOrEmpty(selectedAction))
                return null;
            return workspace.Routes.Any(route => route.Source == selectedId && route.Action == selectedAction)
                ? selectedAction : null;
        }

        private void SelectNode(string id)
        {
            if (selectedId == id)
                return;
            selectedId = id;
            selectedAction = null;
            addingRoute = false;
            RefreshSelection();
        }

        // 右欄「由程式控制進出」勾選時呼叫。
        internal void SetNavigatedByCode(string id, bool value)
        {
            TryEdit(() => UIAuthoring.SetNavigatedByCode(workspace, id, value));
        }

        private void SaveRoute(string source, string originalAction, string action, UIRouteKind kind, string target,
            UIHistoryMode history, string result)
        {
            if (originalAction == null)
                UIAuthoring.Connect(workspace, source, action, kind, target, history, result);
            else
                UIAuthoring.UpdateRoute(workspace, source, originalAction, action, kind, target, history, result);
            selectedAction = action;
            addingRoute = false;
        }

        private static string HistoryLabel(UIHistoryMode history) => history switch
        {
            UIHistoryMode.Push => "保留離開的頁面",
            UIHistoryMode.Replace => "略過離開的頁面",
            UIHistoryMode.Clear => "清空歷史",
            _ => history.ToString(),
        };

        // OpenPopup 路由的「返回歷史」只有兩個選項。
        internal static string PopupHistoryLabel(UIHistoryMode history) => history switch
        {
            UIHistoryMode.Push => "可以返回",
            UIHistoryMode.Clear => "不可返回",
            _ => history.ToString(),
        };

        // 背景格線跟著平移與縮放移動。縮得很小時改用比較疏的格線，不會糊成一片。
        private void DrawGrid(MeshGenerationContext context)
        {
            var size = grid.contentRect.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0f || size.y <= 0f)
                return;
            var step = GridStep * view.Zoom;
            while (step < 12f)
                step *= 4f;
            PaintGridLines(context.painter2D, size, step, GridMinorColor);
            PaintGridLines(context.painter2D, size, step * 4f, GridMajorColor);
        }

        private void PaintGridLines(Painter2D painter, Vector2 size, float step, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = 1f;
            painter.BeginPath();
            for (var x = Mathf.Repeat(view.pan.x, step); x < size.x; x += step)
            {
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, size.y));
            }
            for (var y = Mathf.Repeat(view.pan.y, step); y < size.y; y += step)
            {
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(size.x, y));
            }
            painter.Stroke();
        }

        // 範圍框的虛線。這一層會跟著縮放，所以線寬與虛線的長度都除以縮放比例，在螢幕上看起來才固定。
        private void DrawFrame(MeshGenerationContext context)
        {
            var size = frame.contentRect.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0f || size.y <= 0f)
                return;
            var scale = 1f / view.Zoom;
            var painter = context.painter2D;
            painter.strokeColor = FrameColor;
            painter.lineWidth = 1.5f * scale;
            painter.BeginPath();
            PaintDashes(painter, Vector2.zero, new Vector2(size.x, 0f), scale);
            PaintDashes(painter, new Vector2(size.x, 0f), size, scale);
            PaintDashes(painter, size, new Vector2(0f, size.y), scale);
            PaintDashes(painter, new Vector2(0f, size.y), Vector2.zero, scale);
            painter.Stroke();
        }

        private static void PaintDashes(Painter2D painter, Vector2 from, Vector2 to, float scale)
        {
            var length = Vector2.Distance(from, to);
            if (length <= 0f)
                return;
            var direction = (to - from) / length;
            var dash = 10f * scale;
            var gap = 7f * scale;
            for (var offset = 0f; offset < length; offset += dash + gap)
            {
                painter.MoveTo(from + direction * offset);
                painter.LineTo(from + direction * Mathf.Min(offset + dash, length));
            }
        }

        // 連線畫在不跟著縮放的那一層，所以端點要從畫布座標換成視窗座標。
        private void DrawEdges(MeshGenerationContext context)
        {
            var outlet = new Vector2(NodeWidth, NodeHeight * 0.5f);
            var inlet = new Vector2(0, NodeHeight * 0.5f);
            var now = EditorApplication.timeSinceStartup;
            foreach (var route in workspace.Routes)
            {
                if (!UIRoute.TargetsEntry(route.Kind) || !positions.TryGetValue(route.Source, out var from) ||
                    !positions.TryGetValue(route.Target, out var to))
                    continue;
                // 剛走過的路由亮起來，約 2 秒內淡回原本的顏色與粗細。
                var color = route.Kind == UIRouteKind.OpenPopup ? PopupColor : PageColor;
                var flash = RouteFlash(route, now);
                PaintEdge(context.painter2D, view.CanvasToWindow(from + outlet), view.CanvasToWindow(to + inlet),
                    Color.Lerp(color, FlashColor, flash), Mathf.Lerp(2f, 4.5f, flash));
            }
            if (wireTip.HasValue && wireSource != null && positions.TryGetValue(wireSource, out var start))
            {
                PaintEdge(context.painter2D, view.CanvasToWindow(start + outlet), view.CanvasToWindow(wireTip.Value),
                    Color.white, 2f);
            }
        }

        private void PaintEdge(Painter2D painter, Vector2 from, Vector2 to, Color color, float width)
        {
            var curve = Mathf.Max(60f * view.Zoom, Mathf.Abs(from.x - to.x) * 0.45f);
            var arrow = 8f * Mathf.Clamp(view.Zoom, 0.6f, 1.25f);
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.BezierCurveTo(from + Vector2.right * curve, to - Vector2.right * curve, to);
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(to + new Vector2(-arrow, -arrow * 0.5f));
            painter.LineTo(to);
            painter.LineTo(to + new Vector2(-arrow, arrow * 0.5f));
            painter.Stroke();
        }

        // 節點與線上的標籤放在跟著視角移動的那一層，位置用畫布座標。
        private void UpdateStage()
        {
            foreach (var pair in positions)
            {
                var card = cards[pair.Key];
                card.style.left = pair.Value.x;
                card.style.top = pair.Value.y;
            }
            foreach (var (route, label) in edgeLabels)
            {
                if (!positions.TryGetValue(route.Source, out var from) || !positions.TryGetValue(route.Target, out var to))
                {
                    label.style.display = DisplayStyle.None;
                    continue;
                }
                label.style.left = (from.x + NodeWidth + to.x) / 2;
                label.style.top = (from.y + to.y + NodeHeight) / 2 - 16;
            }

            // 範圍框跟著節點：節點移動、新增、刪除之後都重新計算。
            var frameRect = FrameRect;
            frame.style.left = frameRect.x;
            frame.style.top = frameRect.y;
            frame.style.width = frameRect.width;
            frame.style.height = frameRect.height;
            frameCaption.text = FrameCaption;
            frame.MarkDirtyRepaint();
            edges.MarkDirtyRepaint();
        }

        private string DisplayView(string id)
        {
            if (id == UIRoot.AnySource)
                return AnySourceTitle;
            return string.IsNullOrEmpty(id) ? "未指定" : UIAuthoring.FindEntry(workspace, id)?.Title ?? id;
        }

        private static string KindLabel(UIRouteKind kind) => kind switch
        {
            UIRouteKind.ShowPage => "切頁",
            UIRouteKind.OpenPopup => "開啟 Popup",
            UIRouteKind.Back => "返回",
            UIRouteKind.ClosePopup => "關閉 Popup",
            _ => "送出事件"
        };

        private void ShowValidation()
        {
            UIIdConstants.GenerateForValidation(workspace, new List<UIIssue>());
            var issues = UIValidation.Check(workspace);
            // 細節記到 Console：每一筆都帶代碼，點擊可以定位到相關的畫面物件。
            UIValidation.LogIssues(workspace, issues);
            EditorUtility.DisplayDialog("UniGameUI Validate", UIValidation.DialogMessage(issues), "關閉");
        }

        // 進入 Play Mode 把每條路由走一遍。結束、回到 Edit Mode 之後會跳出摘要，失敗的步驟也記在 Console。
        private void StartWalk()
        {
            try
            {
                if (workspace)
                    SceneManager.SetActiveScene(workspace.gameObject.scene);
                UIWalk.StartFromWindow();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("UniGameUI 走訪", exception.Message, "關閉");
            }
        }

        private void TryEdit(Action change)
        {
            try
            {
                change();
                Rebuild();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
            }
        }

        private static ToolbarButton AddToolbarButton(VisualElement toolbar, string text, Action action)
        {
            var button = new ToolbarButton(action) { text = text };
            toolbar.Add(button);
            return button;
        }
    }
}
