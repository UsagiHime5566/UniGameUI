using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 4-7：「任何畫面」節點的訊號路由，以及 OpenPopup 路由的 history。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void AuthoringAcceptsTheAnySourceForSignalRoutes()
        {
            var root = WorkspaceWithHomePage("Signal Authoring");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            var binding = CreateBinding(home, "challenger");

            // 三種種類都可以，history 照寫的存下來。
            UIAuthoring.Connect(root, UIRoot.AnySource, "challenger", UIRouteKind.ShowPage, "next", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "notice", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            Assert.That(root.Routes.Select(route => (route.Source, route.Action, route.Kind, route.Target, route.History)),
                Is.EqualTo(new[]
                {
                    ("*", "challenger", UIRouteKind.ShowPage, "next", UIHistoryMode.Clear),
                    ("*", "lost", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear),
                    ("*", "notice", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Push),
                    ("*", "tick", UIRouteKind.Emit, "", UIHistoryMode.Push),
                }));
            Assert.That(root.Routes, Has.All.Matches<UIRoute>(route => route.IsSignal));
            // 「任何畫面」不是畫面。
            Assert.That(root.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "home", "next", "tip" }));
            Assert.That(UIAuthoring.FindEntry(root, UIRoot.AnySource), Is.Null);

            // 不能是 Back 或 ClosePopup；Popup 沒有 Replace。
            foreach (var kind in new[] { UIRouteKind.Back, UIRouteKind.ClosePopup })
            {
                var rejected = Assert.Throws<ArgumentException>(() =>
                    UIAuthoring.Connect(root, UIRoot.AnySource, "wrong", kind));
                Assert.That(rejected.Message, Does.Contain("訊號路由").And.Contain(kind.ToString()));
            }
            foreach (var source in new[] { UIRoot.AnySource, "home" })
            {
                var rejected = Assert.Throws<ArgumentException>(() =>
                    UIAuthoring.Connect(root, source, "wrong", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Replace));
                Assert.That(rejected.Message, Does.Contain("Push").And.Contain("Clear").And.Contain("Replace"));
            }
            Assert.That(root.Routes.Count, Is.EqualTo(4));

            // 畫面可以有同名的動作，兩者互不相干。
            UIAuthoring.Connect(root, "home", "challenger", UIRouteKind.Emit);
            Assert.That(root.Routes.Count(route => route.Action == "challenger"), Is.EqualTo(2));

            // 改名只動訊號路由，不會去改畫面上同名的按鈕。
            UIAuthoring.UpdateRoute(root, UIRoot.AnySource, "challenger", "rival", UIRouteKind.ShowPage, "next");
            Assert.That(root.Routes[0].Action, Is.EqualTo("rival"));
            Assert.That(root.Routes[0].History, Is.EqualTo(UIHistoryMode.Push));
            Assert.That(binding.Action, Is.EqualTo("challenger"));
            Assert.That(root.Routes.Single(route => route.Source == "home").Action, Is.EqualTo("challenger"));
            Assert.Throws<ArgumentException>(() =>
                UIAuthoring.UpdateRoute(root, UIRoot.AnySource, "rival", "tick", UIRouteKind.Emit));
            Assert.Throws<ArgumentException>(() =>
                UIAuthoring.UpdateRoute(root, UIRoot.AnySource, "rival", "rival", UIRouteKind.Back));
            Assert.Throws<ArgumentException>(() =>
                UIAuthoring.UpdateRoute(root, UIRoot.AnySource, "ghost", "ghost", UIRouteKind.Emit));

            // 一般路由的 OpenPopup 也接受 history；換成沒有 history 的種類時回到預設值。
            UIAuthoring.Connect(root, "home", "lock", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear);
            var locked = root.Routes.Single(route => route.Action == "lock");
            Assert.That(locked.History, Is.EqualTo(UIHistoryMode.Clear));
            Assert.That(locked.IsSignal, Is.False);
            UIAuthoring.UpdateRoute(root, "home", "lock", "lock", UIRouteKind.Emit, null, UIHistoryMode.Clear);
            Assert.That(locked.History, Is.EqualTo(UIHistoryMode.Push));

            UIAuthoring.Disconnect(root, UIRoot.AnySource, "rival");
            UIAuthoring.Disconnect(root, UIRoot.AnySource, "challenger");
            Assert.That(root.Routes.Select(route => $"{route.Source}/{route.Action}"), Is.EqualTo(new[]
            {
                "*/lost", "*/notice", "*/tick", "home/challenger", "home/lock",
            }));

            // 節點的位置存在工作區上。
            UIAuthoring.PositionNode(root, UIRoot.AnySource, new Vector2(40, 320));
            Assert.That(root.anySourceGraphPosition, Is.EqualTo(new Vector2(40, 320)));
            Assert.Throws<ArgumentException>(() => UIAuthoring.PositionNode(root, "ghost", Vector2.zero));
        }

        [Test]
        public void ValidateReportsSignalRoutesWithTheWrongKindOrHistory()
        {
            var root = WorkspaceWithHomePage("Signal Validate");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.Connect(root, UIRoot.AnySource, "challenger", UIRouteKind.ShowPage, "next", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "home", "lock", UIRouteKind.OpenPopup, "tip", UIHistoryMode.Clear);
            Assert.That(RouteIssues(), Is.Empty);

            // 訊號路由的種類不對。
            root.routes.Add(new UIRoute { source = UIRoot.AnySource, action = "leave", kind = UIRouteKind.Back });
            root.routes.Add(new UIRoute { source = UIRoot.AnySource, action = "close", kind = UIRouteKind.ClosePopup });
            var kinds = Issues(root, UIIssueCode.RouteSignalKindInvalid);
            Assert.That(kinds.Length, Is.EqualTo(2));
            Assert.That(kinds[0].Code, Is.EqualTo("route.signal-kind-invalid"));
            Assert.That(kinds[0].Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(kinds[0].ViewId, Is.Null, "訊號路由不屬於任何畫面");
            Assert.That(kinds[0].Message, Does.Contain("leave").And.Contain("Back"));
            Assert.That(kinds[1].Message, Does.Contain("close").And.Contain("ClosePopup"));
            root.routes.RemoveRange(root.routes.Count - 2, 2);

            // OpenPopup 沒有 Replace：一般路由與訊號路由都回報。
            root.routes.Add(new UIRoute
            {
                source = "home", action = "bad", kind = UIRouteKind.OpenPopup, target = "tip",
                history = UIHistoryMode.Replace,
            });
            root.routes.Add(new UIRoute
            {
                source = UIRoot.AnySource, action = "bad", kind = UIRouteKind.OpenPopup, target = "tip",
                history = UIHistoryMode.Replace,
            });
            var histories = Issues(root, UIIssueCode.RouteHistoryInvalid);
            Assert.That(histories.Select(issue => issue.ViewId), Is.EqualTo(new[] { "home", null }));
            Assert.That(histories[0].Code, Is.EqualTo("route.history-invalid"));
            Assert.That(histories[0].Severity, Is.EqualTo(UIIssueSeverity.Error));
            Assert.That(histories[0].Message, Does.Contain("home/bad").And.Contain("Replace"));
            root.routes.RemoveRange(root.routes.Count - 2, 2);

            // 訊號路由的其他錯誤沿用原本的代碼。
            root.routes.Add(new UIRoute
            {
                source = UIRoot.AnySource, action = "nowhere", kind = UIRouteKind.ShowPage, target = "ghost",
            });
            var missing = Issues(root, UIIssueCode.RouteTargetMissing).Single();
            Assert.That(missing.ViewId, Is.Null);
            Assert.That(missing.Message, Does.Contain("nowhere").And.Contain("ghost"));
            root.routes[^1] = new UIRoute { source = UIRoot.AnySource, action = "tick", kind = UIRouteKind.Emit };
            var duplicate = Issues(root, UIIssueCode.RouteDuplicate).Single();
            Assert.That(duplicate.ViewId, Is.Null);
            root.routes.RemoveAt(root.routes.Count - 1);
            Assert.That(RouteIssues(), Is.Empty);

            // 訊號的動作和畫面的動作互不相干：按鈕的動作只認自己畫面的路由。
            CreateBinding(home, "challenger");
            var unrouted = Issues(root, UIIssueCode.ButtonActionUnrouted).Single();
            Assert.That(unrouted.Message, Does.Contain("challenger"));

            string[] RouteIssues()
            {
                return UIValidation.Check(root).Where(issue => issue.Code.StartsWith("route.", StringComparison.Ordinal))
                    .Select(issue => issue.ToString()).ToArray();
            }
        }

        [Test]
        public void PlanWritesSignalRoutesWithTheStarSource()
        {
            RequireDOTween();
            SceneManager.SetActiveScene(NewScene("Signal Plan"));
            var root = UIPlan.Apply($@"{{
                ""views"": [
                    {{ ""id"": ""home"" }},
                    {{ ""id"": ""challenger"" }},
                    {{ ""id"": ""alert"", ""kind"": ""Popup"", ""prefabPath"": ""{folder}/Alert.prefab"" }}
                ],
                ""startPage"": ""home"",
                ""routes"": [
                    {{ ""source"": ""*"", ""action"": ""challengerJoined"", ""kind"": ""ShowPage"",
                       ""target"": ""challenger"", ""history"": ""Clear"" }},
                    {{ ""source"": ""*"", ""action"": ""disconnected"", ""kind"": ""OpenPopup"",
                       ""target"": ""alert"", ""history"": ""Clear"" }},
                    {{ ""source"": ""*"", ""action"": ""tick"", ""kind"": ""Emit"" }},
                    {{ ""source"": ""home"", ""action"": ""openAlert"", ""kind"": ""OpenPopup"", ""target"": ""alert"" }},
                    {{ ""source"": ""home"", ""action"": ""tick"", ""kind"": ""Emit"" }}
                ]
            }}");

            Assert.That(root.Entries.Select(entry => entry.Id), Is.EqualTo(new[] { "home", "challenger", "alert" }));
            Assert.That(Routes(), Is.EqualTo(new[]
            {
                "*/challengerJoined ShowPage challenger Clear",
                "*/disconnected OpenPopup alert Clear",
                "*/tick Emit  Push",
                "home/openAlert OpenPopup alert Push",
                "home/tick Emit  Push",
            }));
            Assert.That(UIValidation.Check(root).Where(issue => issue.Code.StartsWith("route.", StringComparison.Ordinal)),
                Is.Empty);

            // 動作名稱照舊產生 ID 常數。
            var constants = File.ReadAllText(UIIdConstants.Generate(root));
            Assert.That(constants, Does.Contain("public const string ChallengerJoined = \"challengerJoined\";")
                .And.Contain("public const string Disconnected = \"disconnected\";")
                .And.Contain("public const string Tick = \"tick\";"));
            Assert.That(constants, Does.Not.Contain("\"*\""));

            // 部分更新：只寫要改的欄位，其餘沿用。
            UIPlan.Apply(@"{ ""routes"": [
                { ""source"": ""*"", ""action"": ""challengerJoined"", ""replace"": true, ""history"": ""Push"" },
                { ""source"": ""*"", ""action"": ""disconnected"", ""replace"": true, ""target"": ""alert"" },
                { ""source"": ""home"", ""action"": ""openAlert"", ""replace"": true, ""history"": ""Clear"" }
            ] }");
            Assert.That(Routes(), Is.EqualTo(new[]
            {
                "*/challengerJoined ShowPage challenger Push",
                "*/disconnected OpenPopup alert Clear",
                "*/tick Emit  Push",
                "home/openAlert OpenPopup alert Clear",
                "home/tick Emit  Push",
            }));
            // 沒有寫 replace 時不動既有的路由。
            UIPlan.Apply(@"{ ""routes"": [
                { ""source"": ""*"", ""action"": ""disconnected"", ""kind"": ""Emit"" } ] }");
            Assert.That(Routes()[1], Is.EqualTo("*/disconnected OpenPopup alert Clear"));
            // 種類換了，history 回到預設值。
            UIPlan.Apply(@"{ ""routes"": [
                { ""source"": ""*"", ""action"": ""disconnected"", ""replace"": true,
                  ""kind"": ""ShowPage"", ""target"": ""challenger"" } ] }");
            Assert.That(Routes()[1], Is.EqualTo("*/disconnected ShowPage challenger Push"));

            // 套用前就報錯，而且什麼都沒有改。
            var before = Routes();
            Rejected(@"{ ""source"": ""*"", ""action"": ""leave"", ""kind"": ""Back"" }", "訊號路由", "Back");
            Rejected(@"{ ""source"": ""*"", ""action"": ""close"", ""kind"": ""ClosePopup"" }", "訊號路由", "ClosePopup");
            Rejected(@"{ ""source"": ""*"", ""action"": ""bad"", ""kind"": ""OpenPopup"", ""target"": ""alert"",
                ""history"": ""Replace"" }", "OpenPopup", "Replace");
            Rejected(@"{ ""source"": ""home"", ""action"": ""bad"", ""kind"": ""OpenPopup"", ""target"": ""alert"",
                ""history"": ""Replace"" }", "OpenPopup", "Replace");
            Rejected(@"{ ""source"": ""home"", ""action"": ""openAlert"", ""replace"": true, ""history"": ""Replace"" }",
                "OpenPopup", "Replace");
            Rejected(@"{ ""source"": ""*"", ""action"": ""bad"", ""kind"": ""Emit"", ""history"": ""Clear"" }",
                "history", "Emit");
            Rejected(@"{ ""source"": ""*"", ""action"": ""bad"", ""kind"": ""ShowPage"", ""target"": ""alert"" }",
                "alert");
            Rejected(@"{ ""source"": ""*"", ""action"": ""twice"", ""kind"": ""Emit"" },
                { ""source"": ""*"", ""action"": ""twice"", ""kind"": ""Emit"" }", "twice");
            Assert.That(Routes(), Is.EqualTo(before));

            string[] Routes()
            {
                return root.Routes.Select(route =>
                    $"{route.Source}/{route.Action} {route.Kind} {route.Target} {route.History}").ToArray();
            }

            void Rejected(string routes, params string[] expected)
            {
                // 同一份計畫裡另外帶一個新畫面，確認報錯時連畫面都沒有建立。
                var error = Assert.Throws<ArgumentException>(() => UIPlan.Apply(
                    @"{ ""views"": [{ ""id"": ""extra"" }], ""routes"": [" + routes + "] }"));
                foreach (var part in expected)
                    Assert.That(error.Message, Does.Contain(part));
                Assert.That(UIAuthoring.FindEntry(root, "extra"), Is.Null);
            }
        }

        [Test]
        public void BlueprintShowsTheAnySourceNodeAndEditsSignalRoutes()
        {
            var scene = NewScene("Signal Blueprint");
            var root = UIAuthoring.CreateWorkspace(scene);
            UIAuthoring.CreatePage(root, "home", "Home");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.Connect(root, "home", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "home", "next", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            SceneManager.SetActiveScene(scene);

            var window = ScriptableObject.CreateInstance<UIBlueprintWindow>();
            try
            {
                var click = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

                // 節點固定存在，顯示 Emit 的訊號動作；沒有選取時右欄不顯示它的內容。
                Select(null, null);
                var node = window.rootVisualElement.Q("any-source-node");
                Assert.That(node, Is.Not.Null);
                var labels = node.Query<Label>().ToList().Select(label => label.text).ToArray();
                Assert.That(labels, Has.Some.EqualTo(UIBlueprintWindow.AnySourceTitle));
                Assert.That(labels, Has.Some.Contains("tick"));
                Assert.That(UIBlueprintWindow.AnySourceTitle, Is.EqualTo("任何畫面"));
                Assert.That(window.rootVisualElement.Q("any-source-help"), Is.Null);

                // 選取節點：右欄只有路由的編輯，沒有畫面才有的選項；行為只有三種。
                Select(UIRoot.AnySource, null);
                Assert.That(window.rootVisualElement.Q("any-source-help"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q<Toggle>("navigated-by-code"), Is.Null);
                // 編輯區要選了路由或按「新增路由」才出現。
                Assert.That(Kind(), Is.Null);
                window.BeginAddRoute();
                Assert.That(Kind().choices, Is.EqualTo(new[]
                {
                    UIRouteKind.ShowPage, UIRouteKind.OpenPopup, UIRouteKind.Emit,
                }));
                Assert.That(History().choices, Is.EqualTo(new[]
                {
                    UIHistoryMode.Push, UIHistoryMode.Replace, UIHistoryMode.Clear,
                }));

                // 新增一條訊號路由。
                Field<TextField>("動作").value = "challenger";
                Field<PopupField<string>>("目的地").value = "next";
                History().value = UIHistoryMode.Clear;
                Save();
                var added = root.Routes.Single(route => route.Action == "challenger");
                Assert.That((added.Source, added.Kind, added.Target, added.History),
                    Is.EqualTo((UIRoot.AnySource, UIRouteKind.ShowPage, "next", UIHistoryMode.Clear)));

                // OpenPopup 的「返回歷史」只有可以返回與不可返回。
                Select(UIRoot.AnySource, "lost");
                Assert.That(Kind().value, Is.EqualTo(UIRouteKind.OpenPopup));
                Assert.That(History().choices, Is.EqualTo(new[] { UIHistoryMode.Push, UIHistoryMode.Clear }));
                Assert.That(History().choices.Select(UIBlueprintWindow.PopupHistoryLabel),
                    Is.EqualTo(new[] { "可以返回", "不可返回" }));
                Assert.That(History().value, Is.EqualTo(UIHistoryMode.Push));
                History().value = UIHistoryMode.Clear;
                Save();
                Assert.That(root.Routes.Single(route => route.Action == "lost").History, Is.EqualTo(UIHistoryMode.Clear));
                Assert.That(History().value, Is.EqualTo(UIHistoryMode.Clear));

                // Emit 的訊號沒有「返回歷史」。
                Select(UIRoot.AnySource, "tick");
                Assert.That(Kind().value, Is.EqualTo(UIRouteKind.Emit));
                Assert.That(window.rootVisualElement.Q<PopupField<UIHistoryMode>>("route-history"), Is.Null);

                // 一般畫面的 OpenPopup 路由也有同樣的選單；畫面的行為照舊有五種。
                Select("home", "openTip");
                Assert.That(Kind().choices.Count, Is.EqualTo(5));
                Assert.That(History().choices, Is.EqualTo(new[] { UIHistoryMode.Push, UIHistoryMode.Clear }));
                History().value = UIHistoryMode.Clear;
                Save();
                Assert.That(root.Routes.Single(route => route.Action == "openTip").History,
                    Is.EqualTo(UIHistoryMode.Clear));
                Select("home", "next");
                Assert.That(History().choices.Count, Is.EqualTo(3));

                void Select(string id, string action)
                {
                    var serialized = new SerializedObject(window);
                    serialized.FindProperty("selectedId").stringValue = id ?? "";
                    serialized.FindProperty("selectedAction").stringValue = action ?? "";
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    window.CreateGUI();
                }

                PopupField<UIRouteKind> Kind() => window.rootVisualElement.Q<PopupField<UIRouteKind>>("route-kind");
                PopupField<UIHistoryMode> History() =>
                    window.rootVisualElement.Q<PopupField<UIHistoryMode>>("route-history");

                T Field<T>(string label) where T : VisualElement
                {
                    var found = window.rootVisualElement.Query<T>().ToList()
                        .FirstOrDefault(field => (field as BaseField<string>)?.label == label);
                    Assert.That(found, Is.Not.Null, label);
                    return found;
                }

                void Save()
                {
                    var button = window.rootVisualElement.Q<UnityEngine.UIElements.Button>("save-route");
                    click.Invoke(button.clickable, new object[] { null });
                }
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }
    }
}
