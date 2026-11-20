using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-6：執行中的藍圖。這裡不進入 Play Mode，用直接指定的執行中狀態測試節點的標示、右欄的內容與唯讀。
    // 實際進出 Play Mode 的測試在 UIBlueprintPlayModeTests。外觀（顏色、亮起再淡掉）要人工確認。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void LiveStateMarksTheCurrentPagePopupsAndTransitions()
        {
            // 目前的頁面、兩層 Popup（上面那層正在進場）、返回歷史三筆。
            var state = LiveState("home", new[] { "tip", "menu" }, new[] { "next", "home", "next" });
            state.Transitioning = true;
            state.InputMode = UIInputMode.Navigation;
            state.Entering.Add("menu");

            Assert.That(state.Top, Is.EqualTo("menu"));
            Assert.That(state.NodeClasses("home"), Is.EqualTo(new[] { "live-current" }));
            Assert.That(state.NodeClasses("tip"), Is.EqualTo(new[] { "live-popup" }));
            Assert.That(state.NodeClasses("menu"), Is.EqualTo(new[] { "live-popup", "live-top", "live-transition" }));
            Assert.That(state.NodeClasses("next"), Is.Empty);
            Assert.That(state.NodeClasses(UIRoot.AnySource), Is.Empty);
            Assert.That(Markers(state, "home"), Is.EqualTo(new[] { "目前" }));
            Assert.That(Markers(state, "tip"), Is.EqualTo(new[] { "Popup 1" }));
            Assert.That(Markers(state, "menu"), Is.EqualTo(new[] { "Popup 2", "最上層", "進場中" }));
            Assert.That(Markers(state, "next"), Is.Empty);

            // 沒有 Popup 時，可以操作的是目前的頁面。退場中的畫面另外標示。
            var plain = LiveState("next", new string[0], new string[0]);
            plain.Leaving.Add("home");
            Assert.That(plain.Top, Is.EqualTo("next"));
            Assert.That(plain.NodeClasses("next"), Is.EqualTo(new[] { "live-current", "live-top" }));
            Assert.That(Markers(plain, "next"), Is.EqualTo(new[] { "目前" }));
            Assert.That(Markers(plain, "home"), Is.EqualTo(new[] { "退場中" }));

            // 藍圖視窗照這個狀態標示節點，右欄最上面是「執行中」與「最近的導航」。
            var root = NewSidebarWorkspace("Batch5 Live Markers", out var scene);
            UIAuthoring.CreatePopup(root, "menu", "Menu", $"{folder}/Menu.prefab");
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.IsLive, Is.False);
                window.ShowLiveStateForTests(state);
                Assert.That(window.IsLive, Is.True);
                Assert.That(window.IsWatchingLiveState, Is.False, "沒有進入 Play Mode 時不訂閱任何東西");
                Assert.That(window.rootVisualElement.ClassListContains("blueprint-live"), Is.True);

                Assert.That(Node(window, "node-home").ClassListContains("live-current"), Is.True);
                Assert.That(Node(window, "node-menu").ClassListContains("live-top"), Is.True);
                Assert.That(Node(window, "node-tip").ClassListContains("live-top"), Is.False);
                Assert.That(Node(window, "node-next").ClassListContains("live-current"), Is.False);
                Assert.That(LiveBadges(window, "node-menu"), Is.EqualTo(new[] { "Popup 2", "最上層", "進場中" }));
                Assert.That(LiveBadges(window, "node-home"), Is.EqualTo(new[] { "目前" }));
                Assert.That(LiveBadges(window, "node-next"), Is.Empty);
                // 節點原本的徽章不受影響。
                Assert.That(Node(window, "node-home").Q(className: "node-badges").Query<Label>().ToList()
                    .Select(label => label.text), Is.EqualTo(new[] { "頁面", "起始頁" }));

                Assert.That(SidebarSections(window), Is.EqualTo(new[]
                {
                    "section-live", "section-nav-log", "section-workspace",
                }));
                Assert.That(SidebarSection(window, "live").text, Is.EqualTo("執行中"));
                Assert.That(SidebarSection(window, "nav-log").text, Is.EqualTo("最近的導航"));
                Assert.That(SidebarText(window, "live-page"), Is.EqualTo("Home"));
                Assert.That(SidebarText(window, "live-popups"), Is.EqualTo("由下到上：\n1. Tip\n2. Menu（最上層）"));
                Assert.That(SidebarText(window, "live-history"), Is.EqualTo("3 筆，由近到遠：\n1. Next\n2. Home\n3. Next"));
                Assert.That(SidebarText(window, "live-transitioning"), Is.EqualTo("是"));
                Assert.That(SidebarText(window, "live-input"), Is.EqualTo("導覽（鍵盤或手把）"));
                Assert.That(SidebarText(window, "nav-log-empty"), Is.Not.Empty);

                // 工具列：標示唯讀，沒有會修改藍圖或要在 Edit Mode 做的按鈕。
                Assert.That(window.rootVisualElement.Q<Label>("live-indicator"), Is.Not.Null);
                var buttons = window.rootVisualElement.Query<ToolbarButton>().ToList().Select(button => button.text)
                    .ToArray();
                Assert.That(buttons, Does.Not.Contain("Validate").And.Not.Contain("走訪").And.Not.Contain("重新排列")
                    .And.Not.Contain("檢視全部 UI"));
                Assert.That(buttons, Does.Contain("顯示全部"));

                // 狀態改變：沒有 Popup、沒有歷史、不在轉場中。
                window.ShowLiveStateForTests(LiveState("next", new string[0], new string[0]));
                Assert.That(Node(window, "node-home").ClassListContains("live-current"), Is.False);
                Assert.That(Node(window, "node-next").ClassListContains("live-current"), Is.True);
                Assert.That(SidebarText(window, "live-popups"), Is.EqualTo("（沒有）"));
                Assert.That(SidebarText(window, "live-history"), Is.EqualTo("（空的）"));
                Assert.That(SidebarText(window, "live-transitioning"), Is.EqualTo("否"));
                Assert.That(SidebarText(window, "live-input"), Is.EqualTo("滑鼠"));

                // 回到可以編輯的藍圖：標示都拿掉。
                window.ShowLiveStateForTests(null);
                Assert.That(window.IsLive, Is.False);
                Assert.That(window.rootVisualElement.ClassListContains("blueprint-live"), Is.False);
                Assert.That(window.rootVisualElement.Query(className: "live-current").ToList(), Is.Empty);
                Assert.That(SidebarSections(window), Is.EqualTo(new[] { "section-workspace" }));
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void LiveBlueprintIsReadOnly()
        {
            var root = NewSidebarWorkspace("Batch5 Live Read Only", out var scene);
            var routes = root.Routes.Count;
            var home = UIAuthoring.FindEntry(root, "home");
            var resting = home.GraphPosition;
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var focused = new System.Collections.Generic.List<string>();
                window.FocusRequested = focused.Add;
                window.ShowLiveStateForTests(LiveState("home", new string[0], new string[0]));

                // 不能拖曳節點，也不能拖線。
                var point = window.View.CanvasToWindow(window.NodeRect("home").center);
                window.PressNode("home", null, point);
                window.MovePointer(point + new Vector2(120, 80));
                window.ReleasePointer(point + new Vector2(120, 80));
                Assert.That(window.SelectedId, Is.EqualTo("home"), "可以選取");
                Assert.That(window.NodeRect("home").position, Is.EqualTo(resting));
                Assert.That(home.GraphPosition, Is.EqualTo(resting));
                var port = Node(window, "node-home").Q(className: "node-port");
                window.PressNode("home", port, point);
                window.MovePointer(window.View.CanvasToWindow(window.NodeRect("next").center));
                window.ReleasePointer(window.View.CanvasToWindow(window.NodeRect("next").center));
                window.BeginWire("home", point);
                window.DropWire(window.View.CanvasToWindow(window.NodeRect("next").center));
                Assert.That(root.Routes.Count, Is.EqualTo(routes));

                // 雙擊不定位到 Scene（執行中的畫面在 Hierarchy 選取；沒有執行中的工作區時不做事）。
                window.PressNode("home", null, point, 2);
                window.ReleasePointer(point);
                Assert.That(focused, Is.Empty);
                Assert.That(window.SelectRunningView("home"), Is.False);

                // 右欄照常顯示，但都是唯讀。
                Assert.That(SidebarSections(window), Is.EqualTo(new[]
                {
                    "section-live", "section-nav-log", "section-view", "section-view-settings", "section-routes",
                    "section-incoming",
                }));
                Assert.That(window.rootVisualElement.Q("set-start-page"), Is.Null);
                Assert.That(window.rootVisualElement.Q("preview-page"), Is.Null);
                Assert.That(window.rootVisualElement.Q("add-route"), Is.Null);
                var focus = window.rootVisualElement.Q<Button>("focus-view");
                Assert.That(focus.text, Is.EqualTo("在 Hierarchy 選取"));
                Assert.That(focus.enabledSelf, Is.False, "畫面沒有在執行時不能按");
                Assert.That(window.rootVisualElement.Q<Toggle>("navigated-by-code").enabledSelf, Is.False);

                // 每一列多一個「觸發」：來源是最上層的畫面才能按。
                Assert.That(TriggerStates(window), Is.EqualTo(new[] { "next=True", "openTip=True", "ping=True" }));
                Assert.That(SidebarText(window, "trigger-help"), Does.Contain("TriggerAsync"));
                PressNodeCenter(window, "next");
                Assert.That(TriggerStates(window), Is.EqualTo(new[] { "back=False" }));
                // 訊號路由隨時可以按。
                PressNodeCenter(window, UIRoot.AnySource);
                Assert.That(TriggerStates(window), Is.EqualTo(new[] { "lost=True", "tick=True" }));
                Assert.That(SidebarText(window, "trigger-help"), Does.Contain("SignalAsync"));
                // 沒有執行中的工作區時按了也不做事。
                ClickButton(window.rootVisualElement.Query<Button>("trigger-route").First());
                Assert.That(window.NavigationRecords, Is.Empty);

                // 選了路由：內容看得到，不能修改，沒有儲存與刪除。
                window.SelectRoute("home", "next");
                Assert.That(SidebarSection(window, "route-editor").text, Is.EqualTo("路由內容"));
                Assert.That(ActionField(window).value, Is.EqualTo("next"));
                Assert.That(ActionField(window).enabledInHierarchy, Is.False);
                Assert.That(KindField(window).enabledInHierarchy, Is.False);
                Assert.That(TargetField(window).enabledInHierarchy, Is.False);
                Assert.That(window.rootVisualElement.Q("save-route"), Is.Null);
                Assert.That(window.rootVisualElement.Q("delete-route"), Is.Null);
                Assert.That(SidebarText(window, "route-read-only"), Is.Not.Empty);
                // 「新增路由」打開的編輯區也不會出現。
                window.BeginAddRoute();
                Assert.That(window.rootVisualElement.Q("section-route-editor"), Is.Null);

                // 沒有選取時，起始頁也不能改。
                window.ClickCanvas();
                Assert.That(window.rootVisualElement.Q<PopupField<string>>("start-page").enabledSelf, Is.False);

                // 回到 Edit Mode 的藍圖：可以拖曳、可以編輯。
                window.ShowLiveStateForTests(null);
                PressNodeCenter(window, "home");
                Assert.That(window.rootVisualElement.Q("add-route"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q("trigger-route"), Is.Null);
                Assert.That(window.rootVisualElement.Q<Toggle>("navigated-by-code").enabledSelf, Is.True);
                Assert.That(root.Routes.Count, Is.EqualTo(routes));
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void NavigationLogKeepsTheLatestTwentyAndMarksProblems()
        {
            var root = NewSidebarWorkspace("Batch5 Live Log", out var scene);
            var go = root.Routes.Single(route => route.Source == "home" && route.Action == "next");
            var lost = root.Routes.Single(route => route.IsSignal && route.Action == "lost");
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.ShowLiveStateForTests(LiveState("home", new string[0], new string[0]));

                // 路由的一筆：來源、動作、種類、目的地、結果。訊號的來源是「任何畫面」。
                var record = window.RouteRecord(go, UINavResult.Completed);
                Assert.That((record.Source, record.Action, record.Kind, record.Target, record.ByCode),
                    Is.EqualTo(("Home", "next", UIRouteKind.ShowPage, "Next", false)));
                Assert.That(window.RouteRecord(lost, UINavResult.Busy).Source, Is.EqualTo("任何畫面"));
                // 程式直接呼叫的標成「程式」，返回鍵另外標示。
                var direct = window.DirectRecord(new UIDirectNavigation(UIRouteKind.OpenPopup, "tip",
                    UINavResult.Completed, false));
                Assert.That((direct.Source, direct.Action, direct.Kind, direct.Target, direct.TargetId, direct.ByCode),
                    Is.EqualTo(("程式", (string)null, UIRouteKind.OpenPopup, "Tip", "tip", true)));
                var back = window.DirectRecord(new UIDirectNavigation(UIRouteKind.Back, null,
                    UINavResult.Unavailable, true));
                Assert.That((back.Source, back.Target, back.IsProblem), Is.EqualTo(("返回鍵", (string)null, true)));

                // 超過 20 筆時只留最新的 20 筆，新的在上面。
                for (var index = 0; index < 25; index++)
                {
                    window.RecordNavigation(index % 5 == 4
                        ? window.RouteRecord(lost, UINavResult.Failed)
                        : window.RouteRecord(go, UINavResult.Completed));
                }
                window.RecordNavigation(direct);
                Assert.That(window.NavigationRecords.Count, Is.EqualTo(UIBlueprintNavLog.Capacity));
                Assert.That(window.NavigationRecords[0], Is.SameAs(direct));
                var rows = window.rootVisualElement.Query<Button>("nav-record").ToList();
                Assert.That(rows.Count, Is.EqualTo(20));
                Assert.That(window.rootVisualElement.Q("nav-log-empty"), Is.Null);
                Assert.That(RecordText(rows[0]), Is.EqualTo("程式|開啟 Popup|→ Tip|Completed"));
                Assert.That(RecordText(rows[1]), Is.EqualTo("任何畫面 · lost|開啟 Popup|→ Tip|Failed"));
                Assert.That(RecordText(rows[2]), Is.EqualTo("Home · next|切頁|→ Next|Completed"));

                // 結果不是 Completed 的用醒目的樣式。
                var problems = rows.Where(row => row.ClassListContains("nav-problem")).ToArray();
                Assert.That(problems.Length, Is.EqualTo(4));
                Assert.That(problems.All(row => ((UIBlueprintNavRecord)row.userData).Result == UINavResult.Failed),
                    Is.True);
                Assert.That(problems[0].Q<Label>(className: "nav-result").ClassListContains("nav-result-problem"),
                    Is.True);
                Assert.That(rows[0].Q<Label>(className: "nav-result").ClassListContains("nav-result-problem"),
                    Is.False);

                // 點一筆：路由的選取那條路由，程式直接呼叫的選取目的地。
                ClickButton(rows[1]);
                Assert.That((window.SelectedId, window.SelectedAction), Is.EqualTo((UIRoot.AnySource, "lost")));
                ClickButton(window.rootVisualElement.Query<Button>("nav-record").First());
                Assert.That(window.SelectedId, Is.EqualTo("tip"));
                Assert.That(window.SelectedAction, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void WalkedRoutesFlashAndFadeWithinTwoSeconds()
        {
            // 剛走過時最亮，2 秒內淡到原本的樣子。
            Assert.That(UIBlueprintLive.FlashLevel(0), Is.EqualTo(1f));
            Assert.That(UIBlueprintLive.FlashLevel(0.2), Is.EqualTo(1f));
            Assert.That(UIBlueprintLive.FlashLevel(1.15), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(UIBlueprintLive.FlashLevel(1.9), Is.GreaterThan(0f).And.LessThan(0.1f));
            Assert.That(UIBlueprintLive.FlashLevel(2), Is.EqualTo(0f));
            Assert.That(UIBlueprintLive.FlashLevel(-0.5), Is.EqualTo(0f));
            var previous = 1f;
            for (var time = 0.0; time <= 2.0; time += 0.05)
            {
                var level = UIBlueprintLive.FlashLevel(time);
                Assert.That(level, Is.LessThanOrEqualTo(previous));
                previous = level;
            }

            var root = NewSidebarWorkspace("Batch5 Live Flash", out var scene);
            var go = root.Routes.Single(route => route.Source == "home" && route.Action == "next");
            var open = root.Routes.Single(route => route.Source == "home" && route.Action == "openTip");
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                window.ShowLiveStateForTests(LiveState("next", new string[0], new string[0]));
                window.FlashRoute(go, 100);
                Assert.That(window.RouteFlash(go, 100), Is.EqualTo(1f));
                Assert.That(window.RouteFlash(go, 101), Is.GreaterThan(0f).And.LessThan(1f));
                Assert.That(window.RouteFlash(go, 102.5), Is.EqualTo(0f));
                Assert.That(window.RouteFlash(open, 100), Is.EqualTo(0f), "沒走過的路由不亮");
                Assert.That(window.EdgeLabel("home", "next").ClassListContains("edge-flash"), Is.True);
                Assert.That(window.EdgeLabel("home", "openTip").ClassListContains("edge-flash"), Is.False);

                // 淡掉之後標籤回到原本的樣子。
                window.FlashRoute(open, 103);
                Assert.That(window.EdgeLabel("home", "next").ClassListContains("edge-flash"), Is.False);
                Assert.That(window.EdgeLabel("home", "openTip").ClassListContains("edge-flash"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        private static UIBlueprintLiveState LiveState(string page, string[] popups, string[] history)
        {
            var state = new UIBlueprintLiveState { CurrentPage = page };
            state.Popups.AddRange(popups);
            state.History.AddRange(history);
            return state;
        }

        private static string[] Markers(UIBlueprintLiveState state, string id)
        {
            return state.Markers(id).Select(marker => marker.Text).ToArray();
        }

        private static VisualElement Node(UIBlueprintWindow window, string name)
        {
            var node = window.rootVisualElement.Q(name);
            Assert.That(node, Is.Not.Null, name);
            return node;
        }

        private static string[] LiveBadges(UIBlueprintWindow window, string name)
        {
            return Node(window, name).Q(className: "node-live").Query<Label>(className: "badge").ToList()
                .Select(badge => badge.text).ToArray();
        }

        // 「觸發」按鈕：動作=能不能按。
        private static string[] TriggerStates(UIBlueprintWindow window)
        {
            return window.rootVisualElement.Query<Button>("trigger-route").ToList()
                .Select(button => $"{((UIRoute)button.userData).Action}={button.enabledSelf}").ToArray();
        }

        // 最近的導航的一列，不含時間，用 | 接起來。
        private static string RecordText(VisualElement row)
        {
            return string.Join("|", row.Query<Label>().ToList()
                .Where(label => !label.ClassListContains("nav-time")).Select(label => label.text));
        }
    }
}
