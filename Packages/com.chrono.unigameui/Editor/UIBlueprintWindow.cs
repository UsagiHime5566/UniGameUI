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
    public sealed class UIBlueprintWindow : EditorWindow
    {
        private const float NodeWidth = 210;
        private const float NodeHeight = 94;
        private const string GuidePath = "Packages/com.chrono.unigameui/Documentation~/AgentGuide.md";
        private static readonly Color PageColor = new Color32(109, 184, 238, 255);
        private static readonly Color PopupColor = new Color32(195, 156, 237, 255);
        internal const string AnySourceTitle = "任何畫面";

        [SerializeField] private UIRoot workspace;
        [SerializeField] private string selectedId;
        [SerializeField] private string selectedAction;
        private readonly Dictionary<string, VisualElement> cards = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        private readonly List<(UIRoute Route, Label Label)> edgeLabels = new List<(UIRoute, Label)>();
        private VisualElement sidebar;
        private VisualElement stage;
        private VisualElement edges;
        private ScrollView scroll;
        private TextField actionField;
        private Vector2 scrollOffset;
        private Vector2? wireTip;
        private string wireSource;
        private string wireAction;
        private string wireOriginalAction;

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
                scrollOffset = Vector2.zero;
                scroll = null;
            }
            workspace = next;
            Rebuild();
        }

        private void RefreshOnPlayModeChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.EnteredPlayMode)
                Rebuild();
        }

        private void Rebuild()
        {
            selectedAction = SelectedRouteAction();
            if (scroll != null)
                scrollOffset = scroll.scrollOffset;
            rootVisualElement.Clear();
            cards.Clear();
            positions.Clear();
            edgeLabels.Clear();
            wireSource = null;
            wireTip = null;
            scroll = null;
            sidebar = null;

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
                rootVisualElement.Add(new HelpBox("目前沒有 UI 工作區。可先請 agent 依需求套用計畫，或在這裡建立空白工作區。",
                    HelpBoxMessageType.Info));
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    AddToolbarButton(toolbar, "建立單 Scene 工作區", () => Create(UIAuthoringMode.SingleScene));
                    AddToolbarButton(toolbar, "建立 Prefab 工作區", () => Create(UIAuthoringMode.Prefab));
                }
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                rootVisualElement.Add(new HelpBox("結束 Play Mode 後可編輯藍圖。", HelpBoxMessageType.Info));
                return;
            }

            AddToolbarButton(toolbar, "檢視全部 UI", () => UIAuthoring.FocusReview(workspace));
            AddToolbarButton(toolbar, "重新排列", () => UIAuthoring.ArrangeReview(workspace));
            AddToolbarButton(toolbar, "Validate", ShowValidation);
            AddToolbarButton(toolbar, "走訪", StartWalk);
            if (workspace.Entries.Any(entry => entry != null &&
                (UIAuthoring.NeedsViewCanvas(entry.Review) || UIAuthoring.NeedsViewCanvas(entry.Prefab))))
                AddToolbarButton(toolbar, "補上子 Canvas", () => TryEdit(() => UIAuthoring.RepairViewCanvases(workspace)));

            var split = new TwoPaneSplitView(1, 308, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);
            scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            split.Add(scroll);
            stage = new VisualElement();
            stage.AddToClassList("blueprint-stage");
            scroll.Add(stage);
            edges = new VisualElement { pickingMode = PickingMode.Ignore };
            edges.AddToClassList("blueprint-edges");
            edges.generateVisualContent += DrawEdges;
            stage.Add(edges);

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
                var label = new Label(route.Action) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("edge-label");
                edgeLabels.Add((route, label));
                stage.Add(label);
            }
            UpdateStage();
            var graphScroll = scroll;
            graphScroll.schedule.Execute(() => graphScroll.scrollOffset = scrollOffset);

            sidebar = new ScrollView();
            sidebar.AddToClassList("blueprint-sidebar");
            split.Add(sidebar);
            DrawSidebar();
        }

        private void Create(UIAuthoringMode mode)
        {
            workspace = UIAuthoring.CreateWorkspace(SceneManager.GetActiveScene(), mode);
            Rebuild();
        }

        private void CreateCard(UIEntry entry)
        {
            var card = new VisualElement();
            card.AddToClassList("blueprint-node");
            if (entry.Kind == UIEntryKind.Popup)
                card.AddToClassList("popup-node");
            if (entry.Id == selectedId)
                card.AddToClassList("selected-node");
            card.style.width = NodeWidth;
            card.style.height = NodeHeight;
            cards.Add(entry.Id, card);
            stage.Add(card);

            var header = new Label(entry.Title);
            header.AddToClassList("node-title");
            card.Add(header);
            RegisterNodeDrag(header, entry.Id);

            var details = new Label($"{(entry.Kind == UIEntryKind.Popup ? "Popup" : "頁面")}  ·  {entry.Id}" +
                (entry.Id == workspace.StartPageId ? "  ·  起始" : "") +
                (UIAuthoring.IsNavigatedByCode(entry) ? "  ·  程式控制" : ""));
            details.AddToClassList("node-detail");
            card.Add(details);

            var localActions = workspace.Routes.Where(route => route.Source == entry.Id && !UIRoute.TargetsEntry(route.Kind))
                .Select(route => $"{route.Action} ({KindLabel(route.Kind)})");
            var footer = new Label(string.Join("  ·  ", localActions));
            footer.AddToClassList("node-detail");
            footer.tooltip = footer.text;
            card.Add(footer);

            var port = new Label { tooltip = "拖曳到另一個節點以建立或重接路由" };
            port.AddToClassList("node-port");
            card.Add(port);
            RegisterWireDrag(port, entry.Id);
        }

        // 「任何畫面」節點：只有輸出的連接點，沒有線可以連進來。
        private void CreateAnySourceCard()
        {
            var card = new VisualElement { name = "any-source-node" };
            card.AddToClassList("blueprint-node");
            card.AddToClassList("signal-node");
            if (selectedId == UIRoot.AnySource)
                card.AddToClassList("selected-node");
            card.style.width = NodeWidth;
            card.style.height = NodeHeight;
            cards.Add(UIRoot.AnySource, card);
            stage.Add(card);

            var header = new Label(AnySourceTitle);
            header.AddToClassList("node-title");
            card.Add(header);
            RegisterNodeDrag(header, UIRoot.AnySource);

            var details = new Label("訊號  ·  SignalAsync")
            {
                tooltip = "從這裡拉出去的是訊號路由：不論目前顯示哪個畫面都成立，由遊戲程式用 SignalAsync 送出。",
            };
            details.AddToClassList("node-detail");
            card.Add(details);

            var localActions = workspace.Routes
                .Where(route => route.IsSignal && !UIRoute.TargetsEntry(route.Kind))
                .Select(route => $"{route.Action} ({KindLabel(route.Kind)})");
            var footer = new Label(string.Join("  ·  ", localActions));
            footer.AddToClassList("node-detail");
            footer.tooltip = footer.text;
            card.Add(footer);

            var port = new Label { tooltip = "拖曳到畫面節點以建立或重接訊號路由" };
            port.AddToClassList("node-port");
            card.Add(port);
            RegisterWireDrag(port, UIRoot.AnySource);
        }

        private void RegisterNodeDrag(VisualElement handle, string id)
        {
            Vector2 pointerStart = default;
            Vector2 nodeStart = default;
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                SelectNode(id);
                pointerStart = evt.position;
                nodeStart = positions[id];
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!handle.HasPointerCapture(evt.pointerId))
                    return;
                var position = nodeStart + (Vector2)evt.position - pointerStart;
                positions[id] = new Vector2(Mathf.Max(12, position.x), Mathf.Max(12, position.y));
                UpdateStage();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!handle.HasPointerCapture(evt.pointerId))
                    return;
                handle.ReleasePointer(evt.pointerId);
                if (positions[id] != nodeStart)
                    UIAuthoring.PositionNode(workspace, id, positions[id]);
                else if (id != UIRoot.AnySource)
                    UIAuthoring.FocusReview(workspace, id);
                evt.StopPropagation();
            });
        }

        private void RegisterWireDrag(VisualElement port, string source)
        {
            port.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                SelectNode(source);
                wireSource = source;
                wireAction = actionField?.value;
                wireOriginalAction = SelectedRouteAction();
                wireTip = stage.WorldToLocal((Vector2)evt.position);
                port.CapturePointer(evt.pointerId);
                edges.MarkDirtyRepaint();
                evt.StopPropagation();
            });
            port.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!port.HasPointerCapture(evt.pointerId))
                    return;
                wireTip = stage.WorldToLocal((Vector2)evt.position);
                edges.MarkDirtyRepaint();
            });
            port.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!port.HasPointerCapture(evt.pointerId))
                    return;
                port.ReleasePointer(evt.pointerId);
                var point = stage.WorldToLocal((Vector2)evt.position);
                var target = workspace.Entries.FirstOrDefault(entry =>
                    entry.Id != source && new Rect(positions[entry.Id], new Vector2(NodeWidth, NodeHeight)).Contains(point));
                var action = wireAction;
                var originalAction = wireOriginalAction;
                wireSource = null;
                wireTip = null;
                edges.MarkDirtyRepaint();
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
                evt.StopPropagation();
            });
            port.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                wireSource = null;
                wireTip = null;
                edges.MarkDirtyRepaint();
            });
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
            foreach (var pair in cards)
                pair.Value.EnableInClassList("selected-node", pair.Key == id);
            DrawSidebar();
        }

        private void DrawSidebar()
        {
            sidebar.Clear();
            var pages = workspace.Entries.Where(entry => entry.Kind == UIEntryKind.Page).Select(entry => entry.Id).ToList();
            var startupChoices = new List<string> { "" };
            startupChoices.AddRange(pages);
            var startup = new PopupField<string>("起始頁", startupChoices,
                Mathf.Max(0, startupChoices.IndexOf(workspace.StartPageId)), DisplayView, DisplayView);
            startup.RegisterValueChangedCallback(evt => TryEdit(() => UIAuthoring.SetStartPage(workspace, evt.newValue)));
            sidebar.Add(startup);
            var isAnySource = selectedId == UIRoot.AnySource;
            var selected = isAnySource ? null : UIAuthoring.FindEntry(workspace, selectedId);
            if (!isAnySource && selected == null)
            {
                sidebar.Add(new HelpBox("選取節點以檢視頁面或編輯路由。", HelpBoxMessageType.Info));
                return;
            }

            var sourceId = isAnySource ? UIRoot.AnySource : selected.Id;
            var title = new Label(isAnySource ? AnySourceTitle : selected.Title);
            title.AddToClassList("sidebar-title");
            sidebar.Add(title);
            if (isAnySource)
            {
                sidebar.Add(new HelpBox("從這個節點拉出去的是訊號路由：不論目前顯示哪個畫面都成立。" +
                    "遊戲程式用 root.SignalAsync(動作, 資料) 送出；有導航正在進行時，訊號會等它結束再執行。",
                    HelpBoxMessageType.Info) { name = "any-source-help" });
            }
            else
            {
                DrawViewOptions(selected, pages);
            }

            var local = workspace.Routes.Where(route => route.Source == sourceId).ToArray();
            foreach (var route in local)
            {
                var target = UIRoute.TargetsEntry(route.Kind) ? $" → {DisplayView(route.Target)}" : "";
                sidebar.Add(new Button(() =>
                {
                    selectedAction = route.Action;
                    DrawSidebar();
                }) { text = $"{route.Action}  ·  {KindLabel(route.Kind)}{target}" });
            }
            var current = local.FirstOrDefault(route => route.Action == selectedAction);
            actionField = new TextField("動作") { value = current?.Action ?? "" };
            sidebar.Add(actionField);
            // 訊號路由只能切頁、開啟 Popup 或送出事件。
            var kinds = ((UIRouteKind[])Enum.GetValues(typeof(UIRouteKind)))
                .Where(kind => !isAnySource || UIAuthoring.IsSignalKind(kind)).ToList();
            var currentKind = current?.Kind ?? UIRouteKind.ShowPage;
            var kindField = new PopupField<UIRouteKind>("行為", kinds,
                kinds.Contains(currentKind) ? currentKind : UIRouteKind.ShowPage, KindName, KindName)
            {
                name = "route-kind",
            };
            sidebar.Add(kindField);
            var targetSlot = new VisualElement();
            sidebar.Add(targetSlot);
            var historySlot = new VisualElement();
            sidebar.Add(historySlot);
            var resultField = new TextField("關閉結果")
            {
                name = "route-result",
                value = current?.Result ?? "",
                tooltip = "留空時不回傳結果。",
            };
            sidebar.Add(resultField);
            PopupField<string> targetField = null;
            PopupField<UIHistoryMode> historyField = null;
            void SetChoices(UIRouteKind kind, string target, UIHistoryMode history)
            {
                targetSlot.Clear();
                targetField = null;
                // 「返回歷史」只有切頁與開啟 Popup 有。Popup 沒有 Replace：只分可以返回與不可返回。
                historySlot.Clear();
                historyField = null;
                if (kind == UIRouteKind.ShowPage || kind == UIRouteKind.OpenPopup)
                {
                    var isPopup = kind == UIRouteKind.OpenPopup;
                    var modes = isPopup
                        ? new List<UIHistoryMode> { UIHistoryMode.Push, UIHistoryMode.Clear }
                        : new List<UIHistoryMode> { UIHistoryMode.Push, UIHistoryMode.Replace, UIHistoryMode.Clear };
                    Func<UIHistoryMode, string> label = isPopup ? PopupHistoryLabel : HistoryLabel;
                    historyField = new PopupField<UIHistoryMode>("返回歷史", modes,
                        modes.Contains(history) ? history : UIHistoryMode.Push, label, label)
                    {
                        name = "route-history",
                        tooltip = isPopup
                            ? "不可返回的 Popup：開啟時清空歷史，返回鍵與 Back 路由無效，只能用 ClosePopup 路由、" +
                              "View.Close() 或點遮罩關閉。"
                            : "切到目的地之後，離開的頁面要不要留在返回歷史裡。",
                    };
                    historyField.style.display = DisplayStyle.Flex;
                    historySlot.Add(historyField);
                }
                resultField.style.display = kind == UIRouteKind.ClosePopup ? DisplayStyle.Flex : DisplayStyle.None;
                if (!UIRoute.TargetsEntry(kind))
                    return;
                var type = kind == UIRouteKind.ShowPage ? UIEntryKind.Page : UIEntryKind.Popup;
                var choices = new List<string> { "" };
                choices.AddRange(workspace.Entries.Where(entry => entry.Kind == type).Select(entry => entry.Id));
                targetField = new PopupField<string>("目的地", choices, Mathf.Max(0, choices.IndexOf(target)),
                    DisplayView, DisplayView);
                targetSlot.Add(targetField);
            }
            SetChoices(kindField.value, current?.Target, current?.History ?? UIHistoryMode.Push);
            kindField.RegisterValueChangedCallback(evt => SetChoices(evt.newValue, null, UIHistoryMode.Push));
            sidebar.Add(new Button(() => TryEdit(() => SaveRoute(sourceId, current?.Action,
                actionField.value, kindField.value, targetField?.value,
                historyField?.value ?? UIHistoryMode.Push, resultField.value)))
            {
                name = "save-route",
                text = "儲存路由",
            });
            if (current != null)
            {
                sidebar.Add(new Button(() => TryEdit(() =>
                {
                    UIAuthoring.Disconnect(workspace, sourceId, current.Action);
                    selectedAction = null;
                })) { text = "移除所選路由" });
                sidebar.Add(new Button(() =>
                {
                    selectedAction = null;
                    DrawSidebar();
                }) { text = "新增另一條路由" });
            }
        }

        // 右欄裡只有畫面才有的部分：定位、預覽疊加、由程式控制、待套用的覆寫。
        private void DrawViewOptions(UIEntry selected, List<string> pages)
        {
            sidebar.Add(new Button(() => UIAuthoring.FocusReview(workspace, selected.Id)) { text = "在 Scene 定位" });
            if (selected.Kind == UIEntryKind.Popup && pages.Count > 0)
            {
                var context = new PopupField<string>("疊在頁面", pages, 0, DisplayView, DisplayView);
                sidebar.Add(context);
                sidebar.Add(new Button(() => UIAuthoring.PreviewPopupOnPage(workspace, selected.Id, context.value))
                    { text = "預覽疊加" });
            }
            // 確認框用 ConfirmAsync 開啟、自己關閉，自動視為由程式控制，不能取消。
            var isConfirm = UIAuthoring.IsConfirmPopup(selected.Review) || UIAuthoring.IsConfirmPopup(selected.Prefab);
            var byCode = new Toggle("由程式控制進出")
            {
                name = "navigated-by-code",
                value = UIAuthoring.IsNavigatedByCode(selected),
                tooltip = isConfirm
                    ? "確認框由 ConfirmAsync 開啟並自己關閉，自動視為由程式控制。"
                    : "這個畫面不靠路由開啟或關閉。勾選後 Validate 不檢查它走不走得到、有沒有出口。",
            };
            byCode.SetEnabled(!isConfirm);
            byCode.RegisterValueChangedCallback(evt => SetNavigatedByCode(selected.Id, evt.newValue));
            sidebar.Add(byCode);
            if (UIAuthoring.HasReviewOverrides(selected))
            {
                sidebar.Add(new HelpBox("這個預覽有待套用的 Prefab 覆寫。", HelpBoxMessageType.Warning));
                sidebar.Add(new Button(() => TryEdit(() => UIAuthoring.ApplyReviewOverrides(workspace, selected.Id)))
                    { text = "套用到 Prefab" });
            }
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

        // 「行為」選單顯示的文字，和 EnumField 的顯示方式相同。
        private static string KindName(UIRouteKind kind) => ObjectNames.NicifyVariableName(kind.ToString());

        private void DrawEdges(MeshGenerationContext context)
        {
            foreach (var route in workspace.Routes)
            {
                if (!UIRoute.TargetsEntry(route.Kind) || !positions.TryGetValue(route.Source, out var from) ||
                    !positions.TryGetValue(route.Target, out var to))
                    continue;
                PaintEdge(context.painter2D, from + new Vector2(NodeWidth, NodeHeight * 0.5f),
                    to + new Vector2(0, NodeHeight * 0.5f),
                    route.Kind == UIRouteKind.OpenPopup ? PopupColor : PageColor);
            }
            if (wireTip.HasValue && positions.TryGetValue(wireSource, out var start))
                PaintEdge(context.painter2D, start + new Vector2(NodeWidth, NodeHeight * 0.5f), wireTip.Value, Color.white);
        }

        private static void PaintEdge(Painter2D painter, Vector2 from, Vector2 to, Color color)
        {
            var curve = Mathf.Max(60, Mathf.Abs(from.x - to.x) * 0.45f);
            painter.strokeColor = color;
            painter.lineWidth = 2;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.BezierCurveTo(from + Vector2.right * curve, to - Vector2.right * curve, to);
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(to + new Vector2(-8, -4));
            painter.LineTo(to);
            painter.LineTo(to + new Vector2(-8, 4));
            painter.Stroke();
        }

        private void UpdateStage()
        {
            var size = new Vector2(720, 540);
            foreach (var pair in positions)
            {
                var card = cards[pair.Key];
                card.style.left = pair.Value.x;
                card.style.top = pair.Value.y;
                size = Vector2.Max(size, pair.Value + new Vector2(NodeWidth + 48, NodeHeight + 48));
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
            stage.style.width = size.x;
            stage.style.height = size.y;
            edges.MarkDirtyRepaint();
        }

        private string DisplayView(string id)
        {
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

        private static void AddToolbarButton(VisualElement toolbar, string text, Action action)
        {
            toolbar.Add(new ToolbarButton(action) { text = text });
        }
    }
}
