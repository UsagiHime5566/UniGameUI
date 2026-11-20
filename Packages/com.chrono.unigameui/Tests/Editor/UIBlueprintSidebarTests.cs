using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-4：藍圖視窗的右欄。
    // 這裡測的是右欄在各種選取狀態下有哪些區塊、清單的內容、編輯區的欄位與錯誤訊息。
    // 區塊看起來的樣子、寬度實際拖曳的手感，自動測試看不到，要人工確認。
    public sealed partial class UIAuthoringTests
    {
        private static readonly MethodInfo ClickMethod =
            typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void SidebarShowsTheSectionsOfTheSelection()
        {
            var root = NewSidebarWorkspace("Batch5 Sidebar Sections", out var scene);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var focused = new System.Collections.Generic.List<string>();
                window.FocusRequested = focused.Add;

                // 沒有選取：只有「工作區」。
                Assert.That(SidebarSections(window), Is.EqualTo(new[] { "section-workspace" }));
                Assert.That(SidebarSection(window, "workspace").text, Is.EqualTo("工作區"));
                Assert.That(SidebarText(window, "workspace-scene"), Is.Not.Empty);
                Assert.That(SidebarText(window, "workspace-mode"), Is.EqualTo("單一 Scene"));
                Assert.That(window.rootVisualElement.Q<PopupField<string>>("start-page").value, Is.EqualTo("home"));
                Assert.That(SidebarText(window, "workspace-pages"), Is.EqualTo("2 個"));
                Assert.That(SidebarText(window, "workspace-popups"), Is.EqualTo("1 個"));
                Assert.That(SidebarText(window, "workspace-signals"), Is.EqualTo("2 條"));
                Assert.That(SidebarText(window, "workspace-hint"), Is.Not.Empty);

                // 選取頁面：畫面、畫面設定、從這裡出發的路由、通到這裡的路由。還沒選路由，沒有編輯區。
                PressNodeCenter(window, "home");
                Assert.That(SidebarSections(window), Is.EqualTo(new[]
                {
                    "section-view", "section-view-settings", "section-routes", "section-incoming",
                }));
                Assert.That(SidebarSection(window, "view").text, Is.EqualTo("畫面"));
                Assert.That(SidebarSection(window, "view-settings").text, Is.EqualTo("畫面設定"));
                Assert.That(SidebarSection(window, "routes").text, Is.EqualTo("從這裡出發的路由"));
                Assert.That(SidebarSection(window, "incoming").text, Is.EqualTo("通到這裡的路由"));
                Assert.That(SidebarText(window, "view-title"), Is.EqualTo("Home"));
                Assert.That(SidebarText(window, "view-id"), Is.EqualTo("home"));
                Assert.That(SidebarText(window, "view-kind"), Is.EqualTo("頁面"));
                Assert.That(window.rootVisualElement.Q("view-is-start"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q("set-start-page"), Is.Null);
                Assert.That(window.rootVisualElement.Q("preview-page"), Is.Null);
                Assert.That(window.rootVisualElement.Q<Toggle>("navigated-by-code"), Is.Not.Null);
                Assert.That(SidebarText(window, "navigated-by-code-help"), Is.Not.Empty);

                // 出發的路由和資料一致：動作、種類、目標。
                Assert.That(SidebarRows(window, "route-row"), Is.EqualTo(new[]
                {
                    "next|切頁|→ Next", "openTip|開啟 Popup|→ Tip", "ping|送出事件",
                }));
                Assert.That(window.rootVisualElement.Query(className: "route-row-selected").ToList(), Is.Empty);
                Assert.That(window.rootVisualElement.Q("routes-empty"), Is.Null);
                // 起始頁沒有路由通進來：顯示提示。
                Assert.That(SidebarText(window, "incoming-empty"), Does.Contain("起始頁"));

                // 「在 Scene 中定位」。
                ClickButton(window.rootVisualElement.Q<Button>("focus-view"));
                Assert.That(focused, Is.EqualTo(new[] { "home" }));

                // 點一列：選中這條路由，編輯區出現在「通到這裡的路由」之前。
                ClickButton(SidebarRowButtons(window, "route-row")[1]);
                Assert.That(window.SelectedAction, Is.EqualTo("openTip"));
                Assert.That(SidebarSections(window), Is.EqualTo(new[]
                {
                    "section-view", "section-view-settings", "section-routes", "section-route-editor",
                    "section-incoming",
                }));
                Assert.That(SidebarSection(window, "route-editor").text, Is.EqualTo("編輯路由"));
                var marked = window.rootVisualElement.Query<Button>(className: "route-row-selected").ToList();
                Assert.That(marked.Count, Is.EqualTo(1));
                Assert.That(((UIRoute)marked[0].userData).Action, Is.EqualTo("openTip"));
                Assert.That(window.rootVisualElement.Q<Button>("save-route").text, Is.EqualTo("儲存"));
                Assert.That(window.rootVisualElement.Q<Button>("delete-route").ClassListContains("danger-button"), Is.True);
                Assert.That(window.rootVisualElement.Q("cancel-route"), Is.Null);

                // 「新增路由」：空白的編輯區，可以取消。
                ClickButton(window.rootVisualElement.Q<Button>("add-route"));
                Assert.That(window.SelectedAction, Is.Null);
                Assert.That(SidebarSection(window, "route-editor").text, Is.EqualTo("新增路由"));
                Assert.That(window.rootVisualElement.Q<TextField>("route-action").value, Is.Empty);
                Assert.That(window.rootVisualElement.Q("delete-route"), Is.Null);
                ClickButton(window.rootVisualElement.Q<Button>("cancel-route"));
                Assert.That(window.rootVisualElement.Q("section-route-editor"), Is.Null);

                // 不是起始頁的頁面：有「設為起始頁」。通到這裡的路由列出來源與動作，點一列選取來源。
                PressNodeCenter(window, "next");
                Assert.That(window.rootVisualElement.Q("view-is-start"), Is.Null);
                Assert.That(SidebarRows(window, "route-row"), Is.EqualTo(new[] { "back|返回" }));
                Assert.That(SidebarRows(window, "incoming-row"), Is.EqualTo(new[] { "Home|切頁|next" }));
                ClickButton(SidebarRowButtons(window, "incoming-row")[0]);
                Assert.That(window.SelectedId, Is.EqualTo("home"));
                Assert.That(window.SelectedAction, Is.EqualTo("next"));
                Assert.That(window.rootVisualElement.Q("node-home").ClassListContains("selected-node"), Is.True);
                Assert.That(window.rootVisualElement.Q<TextField>("route-action").value, Is.EqualTo("next"));

                PressNodeCenter(window, "next");
                ClickButton(window.rootVisualElement.Q<Button>("set-start-page"));
                Assert.That(root.StartPageId, Is.EqualTo("next"));
                Assert.That(window.rootVisualElement.Q("view-is-start"), Is.Not.Null);

                // Popup：有疊在頁面上預覽；通到這裡的路由包含訊號路由，來源顯示「任何畫面」。
                PressNodeCenter(window, "tip");
                Assert.That(SidebarText(window, "view-kind"), Is.EqualTo("Popup"));
                Assert.That(window.rootVisualElement.Q("set-start-page"), Is.Null);
                Assert.That(window.rootVisualElement.Q("preview-page"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q("preview-popup"), Is.Not.Null);
                Assert.That(SidebarRows(window, "route-row"), Is.EqualTo(new[] { "ok|關閉 Popup" }));
                Assert.That(SidebarRows(window, "incoming-row"), Is.EqualTo(new[]
                {
                    "Home|開啟 Popup|openTip", "任何畫面|開啟 Popup|lost",
                }));
                ClickButton(SidebarRowButtons(window, "incoming-row")[1]);
                Assert.That(window.SelectedId, Is.EqualTo(UIRoot.AnySource));
                Assert.That(window.SelectedAction, Is.EqualTo("lost"));

                // 「任何畫面」：說明、訊號路由、編輯路由。沒有畫面設定，也沒有通到這裡的路由。
                Assert.That(SidebarSections(window), Is.EqualTo(new[]
                {
                    "section-any-source", "section-routes", "section-route-editor",
                }));
                Assert.That(SidebarSection(window, "any-source").text, Is.EqualTo("任何畫面"));
                Assert.That(SidebarSection(window, "routes").text, Is.EqualTo("訊號路由"));
                Assert.That(SidebarText(window, "any-source-help"), Does.Contain("SignalAsync"));
                Assert.That(SidebarText(window, "view-kind"), Is.EqualTo("訊號"));
                Assert.That(SidebarRows(window, "route-row"), Is.EqualTo(new[] { "lost|開啟 Popup|→ Tip", "tick|送出事件" }));
                Assert.That(window.rootVisualElement.Q("navigated-by-code"), Is.Null);
                Assert.That(window.rootVisualElement.Q("focus-view"), Is.Null);
                window.ClickCanvas();
                PressNodeCenter(window, UIRoot.AnySource);
                Assert.That(SidebarSections(window), Is.EqualTo(new[] { "section-any-source", "section-routes" }));

                // 點空白：回到工作區。
                window.ClickCanvas();
                Assert.That(SidebarSections(window), Is.EqualTo(new[] { "section-workspace" }));
                Assert.That(window.rootVisualElement.Q<PopupField<string>>("start-page").value, Is.EqualTo("next"));

                // 沒有路由的畫面：兩個清單都顯示提示。
                UIAuthoring.CreatePage(root, "lonely", "Lonely");
                window.CreateGUI();
                PressNodeCenter(window, "lonely");
                Assert.That(SidebarText(window, "routes-empty"), Is.Not.Empty);
                Assert.That(SidebarText(window, "incoming-empty"), Is.Not.Empty);
                Assert.That(SidebarRowButtons(window, "route-row"), Is.Empty);
                Assert.That(window.rootVisualElement.Q("add-route"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void RouteEditorShowsOnlyTheFieldsOfTheKind()
        {
            NewSidebarWorkspace("Batch5 Sidebar Fields", out var scene);
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();

                // 切頁：目的地與三種返回歷史。
                window.SelectRoute("home", "next");
                Assert.That(KindField(window).value, Is.EqualTo(UIRouteKind.ShowPage));
                Assert.That(KindField(window).choices.Count, Is.EqualTo(5));
                Assert.That(TargetField(window).value, Is.EqualTo("next"));
                Assert.That(TargetField(window).choices, Is.EqualTo(new[] { "", "home", "next" }));
                Assert.That(HistoryField(window).choices, Is.EqualTo(new[]
                {
                    UIHistoryMode.Push, UIHistoryMode.Replace, UIHistoryMode.Clear,
                }));
                Assert.That(ResultField(window), Is.Null);

                // 開啟 Popup：目的地只列 Popup，返回歷史只有兩種。
                window.SelectRoute("home", "openTip");
                Assert.That(TargetField(window).choices, Is.EqualTo(new[] { "", "tip" }));
                Assert.That(HistoryField(window).choices, Is.EqualTo(new[] { UIHistoryMode.Push, UIHistoryMode.Clear }));
                Assert.That(ResultField(window), Is.Null);

                // 返回：沒有目的地、返回歷史、關閉結果。
                window.SelectRoute("next", "back");
                Assert.That(KindField(window).value, Is.EqualTo(UIRouteKind.Back));
                Assert.That(TargetField(window), Is.Null);
                Assert.That(HistoryField(window), Is.Null);
                Assert.That(ResultField(window), Is.Null);
                Assert.That(window.rootVisualElement.Q("route-kind-help"), Is.Not.Null);

                // 關閉 Popup：只有關閉結果。
                window.SelectRoute("tip", "ok");
                Assert.That(KindField(window).value, Is.EqualTo(UIRouteKind.ClosePopup));
                Assert.That(TargetField(window), Is.Null);
                Assert.That(HistoryField(window), Is.Null);
                Assert.That(ResultField(window).value, Is.EqualTo("yes"));

                // 送出事件：都沒有。
                window.SelectRoute("home", "ping");
                Assert.That(TargetField(window), Is.Null);
                Assert.That(HistoryField(window), Is.Null);
                Assert.That(ResultField(window), Is.Null);

                // 在編輯區換行為：欄位跟著換。
                window.RouteKindChanged(UIRouteKind.ClosePopup);
                Assert.That(TargetField(window), Is.Null);
                Assert.That(ResultField(window), Is.Not.Null);
                window.RouteKindChanged(UIRouteKind.OpenPopup);
                Assert.That(TargetField(window).choices, Is.EqualTo(new[] { "", "tip" }));
                Assert.That(HistoryField(window).choices.Count, Is.EqualTo(2));
                Assert.That(ResultField(window), Is.Null);

                // 行為的說法和清單裡的徽章一致，後面附上計畫用的名稱。
                Assert.That(window.rootVisualElement.Q<Button>("route-row").Q<Label>(className: "kind-ShowPage").text,
                    Is.EqualTo("切頁"));

                // 「任何畫面」的編輯區只有三種行為。
                window.SelectRoute(UIRoot.AnySource, "tick");
                Assert.That(KindField(window).choices, Is.EqualTo(new[]
                {
                    UIRouteKind.ShowPage, UIRouteKind.OpenPopup, UIRouteKind.Emit,
                }));
                Assert.That(HistoryField(window), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void RouteEditorReportsProblemsNextToTheField()
        {
            var root = NewSidebarWorkspace("Batch5 Sidebar Errors", out var scene);
            var count = root.Routes.Count;
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                PressNodeCenter(window, "home");
                window.BeginAddRoute();
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Null);
                Assert.That(window.rootVisualElement.Q("route-target-error"), Is.Null);

                // 動作重複：不儲存，錯誤顯示在「動作」旁邊，填的內容留著。
                ActionField(window).value = "next";
                TargetField(window).value = "next";
                HistoryField(window).value = UIHistoryMode.Clear;
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(root.Routes.Count, Is.EqualTo(count));
                Assert.That(SidebarText(window, "route-action-error"), Does.Contain("next"));
                Assert.That(window.rootVisualElement.Q("route-target-error"), Is.Null);
                Assert.That(ActionField(window).value, Is.EqualTo("next"));
                Assert.That(TargetField(window).value, Is.EqualTo("next"));
                Assert.That(HistoryField(window).value, Is.EqualTo(UIHistoryMode.Clear));
                Assert.That(root.Routes.Single(route => route.Source == "home" && route.Action == "next").History,
                    Is.EqualTo(UIHistoryMode.Push));
                // 錯誤訊息緊接在出錯的欄位後面。
                var error = window.rootVisualElement.Q("route-action-error");
                Assert.That(error.parent, Is.SameAs(ActionField(window).parent));
                Assert.That(error.parent.IndexOf(error), Is.EqualTo(error.parent.IndexOf(ActionField(window)) + 1));

                // 動作是空的或有不能用的字元。
                foreach (var bad in new[] { "", "has space" })
                {
                    ActionField(window).value = bad;
                    ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                    Assert.That(root.Routes.Count, Is.EqualTo(count));
                    Assert.That(SidebarText(window, "route-action-error"), Is.Not.Empty);
                    Assert.That(ActionField(window).value, Is.EqualTo(bad));
                }

                // 沒有目標：不儲存，錯誤顯示在「目的地」旁邊。
                ActionField(window).value = "go";
                TargetField(window).value = "";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(root.Routes.Count, Is.EqualTo(count));
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Null);
                Assert.That(SidebarText(window, "route-target-error"), Is.Not.Empty);
                Assert.That(ActionField(window).value, Is.EqualTo("go"));

                // 兩個都有問題時兩個都顯示。
                ActionField(window).value = "ping";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q("route-target-error"), Is.Not.Null);

                // 修好之後儲存：路由建立，選中它，錯誤消失。
                ActionField(window).value = "go";
                TargetField(window).value = "next";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(root.Routes.Count, Is.EqualTo(count + 1));
                var added = root.Routes.Single(route => route.Source == "home" && route.Action == "go");
                Assert.That((added.Kind, added.Target), Is.EqualTo((UIRouteKind.ShowPage, "next")));
                Assert.That(window.SelectedAction, Is.EqualTo("go"));
                Assert.That(SidebarSection(window, "route-editor").text, Is.EqualTo("編輯路由"));
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Null);
                Assert.That(window.rootVisualElement.Q("route-target-error"), Is.Null);

                // 改名成已經有的動作：不儲存。原樣儲存不算重複。
                ActionField(window).value = "ping";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(SidebarText(window, "route-action-error"), Does.Contain("ping"));
                Assert.That(root.Routes.Count(route => route.Source == "home" && route.Action == "go"), Is.EqualTo(1));
                Assert.That(root.Routes.Single(route => route.Source == "home" && route.Action == "ping").Kind,
                    Is.EqualTo(UIRouteKind.Emit));
                ActionField(window).value = "go";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Null);
                Assert.That(root.Routes.Count, Is.EqualTo(count + 1));

                // 換選取之後錯誤不會留著。
                ActionField(window).value = "ping";
                ClickButton(window.rootVisualElement.Q<Button>("save-route"));
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Not.Null);
                window.SelectRoute("home", "next");
                Assert.That(window.rootVisualElement.Q("route-action-error"), Is.Null);

                // 刪除。
                window.SelectRoute("home", "go");
                ClickButton(window.rootVisualElement.Q<Button>("delete-route"));
                Assert.That(root.Routes.Count, Is.EqualTo(count));
                Assert.That(root.Routes.Any(route => route.Action == "go"), Is.False);
                Assert.That(window.SelectedId, Is.EqualTo("home"));
                Assert.That(window.SelectedAction, Is.Null);
                Assert.That(window.rootVisualElement.Q("section-route-editor"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        [Test]
        public void SidebarRemembersItsWidthAndCollapsedSections()
        {
            NewSidebarWorkspace("Batch5 Sidebar Session", out var scene);
            UIBlueprintSession.ForgetSidebarWidth();
            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            var second = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                window.CreateGUI();
                var split = window.rootVisualElement.Q<TwoPaneSplitView>("blueprint-split");
                Assert.That(split.fixedPaneIndex, Is.EqualTo(1));
                Assert.That(split.fixedPaneInitialDimension, Is.EqualTo(UIBlueprintWindow.DefaultSidebarWidth));
                var sidebar = window.rootVisualElement.Q<ScrollView>("blueprint-sidebar");
                Assert.That(sidebar, Is.Not.Null, "右欄內容太長時可以捲動");
                Assert.That(sidebar.style.minWidth.value.value, Is.EqualTo(UIBlueprintWindow.MinSidebarWidth));

                // 寬度：記在工作階段裡，重建視窗、另開視窗都一樣。不會小於最小寬度。
                UIBlueprintWindow.RememberSidebarWidth(420);
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Q<TwoPaneSplitView>("blueprint-split").fixedPaneInitialDimension,
                    Is.EqualTo(420));
                second.CreateGUI();
                Assert.That(second.rootVisualElement.Q<TwoPaneSplitView>("blueprint-split").fixedPaneInitialDimension,
                    Is.EqualTo(420));
                UIBlueprintWindow.RememberSidebarWidth(80);
                Assert.That(UIBlueprintWindow.SidebarWidth, Is.EqualTo(UIBlueprintWindow.MinSidebarWidth));
                UIBlueprintWindow.RememberSidebarWidth(float.NaN);
                Assert.That(UIBlueprintWindow.SidebarWidth, Is.EqualTo(UIBlueprintWindow.MinSidebarWidth));

                // 區塊預設展開；收合之後重畫右欄、重建視窗都還是收合的。
                Assert.That(SidebarSection(window, "workspace").value, Is.True);
                window.SetSectionCollapsed("workspace", true);
                Assert.That(SidebarSection(window, "workspace").value, Is.False);
                window.CreateGUI();
                Assert.That(SidebarSection(window, "workspace").value, Is.False);
                second.CreateGUI();
                Assert.That(SidebarSection(second, "workspace").value, Is.False);

                // 每個區塊各自記：收合「畫面設定」不影響其他區塊，換一個畫面也還是收合的。
                PressNodeCenter(window, "home");
                Assert.That(SidebarSection(window, "view-settings").value, Is.True);
                window.SetSectionCollapsed("view-settings", true);
                PressNodeCenter(window, "next");
                Assert.That(SidebarSection(window, "view-settings").value, Is.False);
                Assert.That(SidebarSection(window, "view").value, Is.True);
                Assert.That(SidebarSection(window, "routes").value, Is.True);

                window.SetSectionCollapsed("workspace", false);
                window.SetSectionCollapsed("view-settings", false);
                Assert.That(SidebarSection(window, "view-settings").value, Is.True);
                window.ClickCanvas();
                Assert.That(SidebarSection(window, "workspace").value, Is.True);
            }
            finally
            {
                UIBlueprintSession.SetSectionCollapsed("workspace", false);
                UIBlueprintSession.SetSectionCollapsed("view-settings", false);
                UIBlueprintSession.ForgetSidebarWidth();
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(second);
                UIBlueprintSession.ForgetView(UIBlueprintSession.SceneKey(scene));
            }
        }

        // 兩個頁面、一個 Popup，五種路由各一條，加上兩條訊號路由。
        private UIRoot NewSidebarWorkspace(string name, out Scene scene)
        {
            scene = NewScene(name);
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "next", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, "home", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "home", "ping", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "next", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "tip", "ok", UIRouteKind.ClosePopup, null, UIHistoryMode.Push, "yes");
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            SceneManager.SetActiveScene(scene);
            return root;
        }

        private static void PressNodeCenter(UIBlueprintWindow window, string id)
        {
            var point = window.View.CanvasToWindow(window.NodeRect(id).center);
            window.PressNode(id, null, point);
            window.ReleasePointer(point);
        }

        private static void ClickButton(Button button)
        {
            Assert.That(button, Is.Not.Null);
            ClickMethod.Invoke(button.clickable, new object[] { null });
        }

        private static string[] SidebarSections(UIBlueprintWindow window)
        {
            return window.rootVisualElement.Query<Foldout>(className: "sidebar-section").ToList()
                .Select(section => section.name).ToArray();
        }

        private static Foldout SidebarSection(UIBlueprintWindow window, string key)
        {
            var section = window.rootVisualElement.Q<Foldout>("section-" + key);
            Assert.That(section, Is.Not.Null, key);
            return section;
        }

        private static string SidebarText(UIBlueprintWindow window, string name)
        {
            var label = window.rootVisualElement.Q<Label>(name);
            Assert.That(label, Is.Not.Null, name);
            return label.text;
        }

        private static System.Collections.Generic.List<Button> SidebarRowButtons(UIBlueprintWindow window, string name)
        {
            return window.rootVisualElement.Query<Button>(name).ToList();
        }

        // 每一列的文字，用 | 接起來：主要的文字、種類、次要的文字。
        private static string[] SidebarRows(UIBlueprintWindow window, string name)
        {
            return SidebarRowButtons(window, name)
                .Select(row => string.Join("|", row.Query<Label>().ToList().Select(label => label.text)))
                .ToArray();
        }

        private static TextField ActionField(UIBlueprintWindow window) =>
            window.rootVisualElement.Q<TextField>("route-action");

        private static PopupField<UIRouteKind> KindField(UIBlueprintWindow window) =>
            window.rootVisualElement.Q<PopupField<UIRouteKind>>("route-kind");

        private static PopupField<string> TargetField(UIBlueprintWindow window) =>
            window.rootVisualElement.Q<PopupField<string>>("route-target");

        private static PopupField<UIHistoryMode> HistoryField(UIBlueprintWindow window) =>
            window.rootVisualElement.Q<PopupField<UIHistoryMode>>("route-history");

        private static TextField ResultField(UIBlueprintWindow window) =>
            window.rootVisualElement.Q<TextField>("route-result");
    }
}
