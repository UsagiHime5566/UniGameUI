using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-6：執行中的藍圖。這些案例開著藍圖視窗實際進出 Play Mode。
    // 為了在進出 Play Mode 之後還拿得到視窗與工作區，測試期間關閉 Domain 重新載入（Scene 照常重新載入）。
    public sealed class UIBlueprintPlayModeTests
    {
        private const string Folder = "Assets/UniGameUI-BlueprintPlayTests";
        private const string WidgetsFolder = Folder + "/Widgets";
        private const string TmpFolder = "Assets/TextMesh Pro";
        private const string HadTmpKey = "UniGameUI.Editor.Tests.BlueprintPlay.HadTmp";
        private const string TmpRecordedKey = "UniGameUI.Editor.Tests.BlueprintPlay.TmpRecorded";
        private const string OptionsKey = "UniGameUI.Editor.Tests.BlueprintPlay.PlayModeOptions";
        private const double WaitSeconds = 15;

        private static readonly MethodInfo ClickMethod =
            typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly string[] RootEvents =
        {
            "ViewShowing", "ViewShown", "ViewHiding", "ViewHidden", "ActionTriggered", "ActionCompleted",
            "InputModeChanged", "DirectNavigationFinished",
        };

        private UIBlueprintWindow window;

        [TearDown]
        public void TearDown()
        {
            if (window)
                Object.DestroyImmediate(window);
            window = null;
            UIWidgets.FolderOverride = null;
            var saved = SessionState.GetString(OptionsKey, "");
            if (saved.Length > 0)
            {
                var parts = saved.Split('|');
                EditorSettings.enterPlayModeOptionsEnabled = parts[0] == "1";
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)int.Parse(parts[1]);
                SessionState.EraseString(OptionsKey);
            }
            UIBlueprintSession.ForgetView("untitled");
            // 案例失敗時 Editor 可能還在 Play Mode，這時不能換 Scene；測試執行器之後會結束 Play Mode。
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        [OneTimeTearDown]
        public void RemoveTmpResources()
        {
            if (SessionState.GetBool(TmpRecordedKey, false) && !SessionState.GetBool(HadTmpKey, true) &&
                AssetDatabase.IsValidFolder(TmpFolder))
                AssetDatabase.DeleteAsset(TmpFolder);
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
            SessionState.EraseBool(HadTmpKey);
            SessionState.EraseBool(TmpRecordedKey);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator BlueprintFollowsTheRunningUIAndLetsGoWhenLeaving()
        {
            var root = NewWorkspace();
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{Folder}/Tip.prefab");
            UIAuthoring.CreatePopup(root, "menu", "Menu", $"{Folder}/Menu.prefab");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.Connect(root, "home", "go", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, "next", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "home", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "tip", "openMenu", UIRouteKind.OpenPopup, "menu");
            UIAuthoring.Connect(root, "tip", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "menu", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, UIRoot.AnySource, "toNext", UIRouteKind.ShowPage, "next");
            Assert.That(UIValidation.Check(root).Where(issue => issue.Severity == UIIssueSeverity.Error), Is.Empty);

            SessionState.SetString(OptionsKey, (EditorSettings.enterPlayModeOptionsEnabled ? "1" : "0") + "|" +
                (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            window.CreateGUI();
            Assert.That(window.IsLive, Is.False);
            Assert.That(window.IsWatchingLiveState, Is.False, "Edit Mode 不訂閱任何東西");

            yield return new EnterPlayMode(false);

            // 進入 Play Mode：同一張藍圖，用執行中的工作區，起始頁是目前的頁面。
            yield return WaitFor(() => window.LiveRoot && window.LiveState?.CurrentPage == "home" &&
                !window.LiveState.Transitioning, "藍圖標示起始頁");
            var live = window.LiveRoot;
            Assert.That(window.IsLive, Is.True);
            Assert.That(window.IsWatchingLiveState, Is.True);
            Assert.That(window.rootVisualElement.ClassListContains("blueprint-live"), Is.True);
            Assert.That(Node("node-home").ClassListContains("live-current"), Is.True);
            Assert.That(Handlers(live), Is.Not.Empty, "Play Mode 中有訂閱");

            // 走一條路由：目前的頁面換了，最近的導航有這一筆，那條線亮起來。
            var go = live.Routes.Single(route => route.Source == "home" && route.Action == "go");
            Fire(live.TriggerAsync("home", "go"));
            yield return WaitFor(() => window.LiveState.CurrentPage == "next" && !window.LiveState.Transitioning,
                "切到 next");
            Assert.That(window.NavigationRecords[0].Route, Is.SameAs(go));
            Assert.That(window.NavigationRecords[0].Result, Is.EqualTo(UINavResult.Completed));
            Assert.That(window.RouteFlash(go, EditorApplication.timeSinceStartup), Is.GreaterThan(0f));
            Assert.That(window.LiveState.History, Is.EqualTo(new[] { "home" }));
            Assert.That(Node("node-next").ClassListContains("live-current"), Is.True);
            Assert.That(Node("node-home").ClassListContains("live-current"), Is.False);

            // 程式直接呼叫：只更新節點的標示，最近的導航標成「程式」。
            Fire(live.BackAsync());
            yield return WaitFor(() => window.LiveState.CurrentPage == "home" && !window.LiveState.Transitioning,
                "返回 home");
            var back = window.NavigationRecords[0];
            Assert.That((back.Source, back.Kind, back.Result, back.ByCode),
                Is.EqualTo(("程式", UIRouteKind.Back, UINavResult.Completed, true)));

            // 疊兩層 Popup：標出順序，最上層的另外標示。
            Fire(live.TriggerAsync("home", "openTip"));
            yield return WaitFor(() => window.LiveState.Popups.SequenceEqual(new[] { "tip" }) &&
                !window.LiveState.Transitioning, "開啟 tip");
            Fire(live.TriggerAsync("tip", "openMenu"));
            yield return WaitFor(() => window.LiveState.Popups.SequenceEqual(new[] { "tip", "menu" }) &&
                !window.LiveState.Transitioning, "開啟 menu");
            Assert.That(Node("node-menu").ClassListContains("live-top"), Is.True);
            Assert.That(Node("node-tip").ClassListContains("live-popup"), Is.True);
            Assert.That(Node("node-tip").ClassListContains("live-top"), Is.False);
            Assert.That(Node("node-home").ClassListContains("live-current"), Is.True);
            Assert.That(window.rootVisualElement.Q<Label>("live-popups").text,
                Is.EqualTo("由下到上：\n1. Tip\n2. Menu（最上層）"));

            // 雙擊節點：在 Hierarchy 選取執行中的畫面。
            Assert.That(live.TryGetView("tip", out var tipView), Is.True);
            PressNode("tip", 2);
            Assert.That(Selection.activeGameObject, Is.SameAs(tipView.gameObject));

            // 「觸發」：來源是最上層的畫面才能按。
            PressNode("tip", 1);
            Assert.That(Trigger("close").enabledSelf, Is.False, "tip 上面蓋著 menu");
            PressNode("menu", 1);
            Assert.That(Trigger("close").enabledSelf, Is.True);
            Click(Trigger("close"));
            yield return WaitFor(() => window.LiveState.Popups.SequenceEqual(new[] { "tip" }) &&
                !window.LiveState.Transitioning, "用觸發按鈕關閉 menu");
            var closed = window.NavigationRecords[0];
            Assert.That((closed.Route?.Source, closed.Route?.Action, closed.Result),
                Is.EqualTo(("menu", "close", UINavResult.Completed)));

            // 訊號路由隨時可以按，從「任何畫面」節點亮起。
            PressNode(UIRoot.AnySource, 1);
            var toNext = live.Routes.Single(route => route.IsSignal && route.Action == "toNext");
            Click(Trigger("toNext"));
            yield return WaitFor(() => window.LiveState.CurrentPage == "next" && window.LiveState.Popups.Count == 0 &&
                !window.LiveState.Transitioning, "訊號切到 next");
            Assert.That(window.NavigationRecords[0].Route, Is.SameAs(toNext));
            Assert.That(window.NavigationRecords[0].Source, Is.EqualTo("任何畫面"));
            Assert.That(window.RouteFlash(toNext, EditorApplication.timeSinceStartup), Is.GreaterThan(0f));

            // 遊戲程式送的訊號也記下來。
            Fire(live.SignalAsync("lost"));
            yield return WaitFor(() => window.LiveState.Popups.SequenceEqual(new[] { "tip" }) &&
                !window.LiveState.Transitioning, "訊號開啟 tip");
            Assert.That(window.NavigationRecords[0].Action, Is.EqualTo("lost"));
            Assert.That(window.rootVisualElement.Query<Button>("nav-record").ToList().Count,
                Is.EqualTo(window.NavigationRecords.Count));

            // 另一個藍圖視窗：關掉時取消它自己的訂閱。
            var second = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            second.CreateGUI();
            Assert.That(second.LiveRoot, Is.SameAs(live));
            Assert.That(Handlers(live).Any(target => ReferenceEquals(target, second)), Is.True);
            Object.DestroyImmediate(second);
            Assert.That(Handlers(live).Any(target => ReferenceEquals(target, second)), Is.False);
            Assert.That(UpdateTargets().Any(target => ReferenceEquals(target, second)), Is.False);

            // 工作區被停用：顯示說明、取消訂閱；再啟用時切換回來。
            live.enabled = false;
            yield return WaitFor(() => window.LiveRoot == null &&
                window.rootVisualElement.Q("live-no-workspace") != null, "停用工作區之後顯示說明");
            Assert.That(Handlers(live).Any(target => ReferenceEquals(target, window)), Is.False);
            live.enabled = true;
            yield return WaitFor(() => window.LiveRoot == live, "重新啟用之後切換回來");
            Assert.That(Handlers(live).Any(target => ReferenceEquals(target, window)), Is.True);

            yield return new ExitPlayMode();
            yield return null;

            // 離開 Play Mode：事件都取消訂閱，回到可以編輯的藍圖，視角不變。
            Assert.That(window.IsLive, Is.False);
            Assert.That(window.LiveRoot, Is.Null);
            Assert.That(window.IsWatchingLiveState, Is.False);
            Assert.That(Handlers(live).Any(target => ReferenceEquals(target, window)), Is.False);
            Assert.That(UpdateTargets().Any(target => ReferenceEquals(target, window)), Is.False);
            Assert.That(window.rootVisualElement.ClassListContains("blueprint-live"), Is.False);
            Assert.That(window.rootVisualElement.Query(className: "live-current").ToList(), Is.Empty);
            PressNode("home", 1);
            Assert.That(window.rootVisualElement.Q("add-route"), Is.Not.Null);
            Assert.That(window.rootVisualElement.Q("trigger-route"), Is.Null);
        }

        private VisualElement Node(string name)
        {
            var node = window.rootVisualElement.Q(name);
            Assert.That(node, Is.Not.Null, name);
            return node;
        }

        private void PressNode(string id, int clicks)
        {
            var point = window.View.CanvasToWindow(window.NodeRect(id).center);
            window.PressNode(id, null, point, clicks);
            window.ReleasePointer(point);
        }

        private Button Trigger(string action)
        {
            var button = window.rootVisualElement.Query<Button>("trigger-route").ToList()
                .FirstOrDefault(candidate => ((UIRoute)candidate.userData).Action == action);
            Assert.That(button, Is.Not.Null, action);
            return button;
        }

        private static void Click(Button button)
        {
            ClickMethod.Invoke(button.clickable, new object[] { null });
        }

        // 導航的結果由藍圖記錄，這裡不必等它。
        private static async void Fire(Awaitable<UINavResult> navigation)
        {
            await navigation;
        }

        // 訂閱了工作區事件的物件。
        private static object[] Handlers(UIRoot root)
        {
            return RootEvents.SelectMany(name =>
            {
                var field = typeof(UIRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, name);
                var handler = field.GetValue(root) as Delegate;
                return handler == null ? Array.Empty<object>() : handler.GetInvocationList().Select(item => item.Target);
            }).ToArray();
        }

        private static object[] UpdateTargets()
        {
            var update = EditorApplication.update;
            return update == null ? Array.Empty<object>() : update.GetInvocationList().Select(item => item.Target).ToArray();
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what)
        {
            var deadline = EditorApplication.timeSinceStartup + WaitSeconds;
            while (!condition())
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    Assert.Fail($"等待逾時：{what}");
                yield return null;
            }
        }

        private static UIRoot NewWorkspace()
        {
            if (!UIDOTweenSetup.IsReady)
                Assert.Ignore("此案例需要 DOTween，請先匯入並完成 Setup DOTween。");
            if (!SessionState.GetBool(TmpRecordedKey, false))
            {
                SessionState.SetBool(TmpRecordedKey, true);
                SessionState.SetBool(HadTmpKey, Directory.Exists(TmpFolder) || File.Exists(TmpFolder + ".meta"));
            }

            // 用沒有存檔的 Scene：存檔會產生 ID 常數的程式碼，測試途中不要觸發重新編譯。
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            UIWidgets.FolderOverride = WidgetsFolder;
            return UIAuthoring.CreateWorkspace(scene);
        }
    }
}
