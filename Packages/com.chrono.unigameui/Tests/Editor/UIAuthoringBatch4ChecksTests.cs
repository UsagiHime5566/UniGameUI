using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    // 第四批 4-3：Validate 新增的五項 Warning。每一項都有「會回報」與「不算」的案例。
    public sealed partial class UIAuthoringTests
    {
        private const string TmpSourceFont = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        [Test]
        public void MissingStartPageAndUnreachableViewsAreWarnings()
        {
            var root = WorkspaceWithHomePage("Batch4 Reachability");
            UIAuthoring.CreatePage(root, "orphan", "Orphan");

            // 沒有起始頁：只回報 start.none，不逐一回報走不到的畫面。
            var none = Issues(root, UIIssueCode.StartNone).Single();
            Assert.That(none.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(none.Message, Does.Contain("startPage"));
            Assert.That(Issues(root, UIIssueCode.ViewUnreachable), Is.Empty);

            // 不算：所有頁面都由程式控制。
            UIAuthoring.SetNavigatedByCode(root, "home", true);
            Assert.That(Issues(root, UIIssueCode.StartNone), Is.Not.Empty, "還有一個頁面不是由程式控制");
            UIAuthoring.SetNavigatedByCode(root, "orphan", true);
            Assert.That(Issues(root, UIIssueCode.StartNone), Is.Empty);
            UIAuthoring.SetNavigatedByCode(root, "home", false);
            UIAuthoring.SetNavigatedByCode(root, "orphan", false);

            // 設了起始頁之後，走不到的頁面與 Popup 各回報一筆。
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.CreatePage(root, "settings", "Settings");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.CreatePopup(root, "lost", "Lost", $"{folder}/Lost.prefab");
            UIAuthoring.Connect(root, "home", "openSettings", UIRouteKind.ShowPage, "settings");
            UIAuthoring.Connect(root, "settings", "openTip", UIRouteKind.OpenPopup, "tip");
            // Back、ClosePopup、Emit 不會帶玩家到新的畫面。
            UIAuthoring.Connect(root, "settings", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "tip", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "home", "ping", UIRouteKind.Emit);
            Assert.That(Issues(root, UIIssueCode.StartNone), Is.Empty);
            var unreachable = Issues(root, UIIssueCode.ViewUnreachable);
            Assert.That(unreachable.Select(issue => issue.ViewId), Is.EquivalentTo(new[] { "orphan", "lost" }));
            foreach (var issue in unreachable)
            {
                Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Warning));
                Assert.That(issue.Message, Does.Contain(issue.ViewId).And.Contain("home"));
            }

            // 不算：由程式控制的畫面。它是程式打開的，所以從它出發的路由也算走得到。
            UIAuthoring.SetNavigatedByCode(root, "lost", true);
            UIAuthoring.Connect(root, "lost", "openOrphan", UIRouteKind.ShowPage, "orphan");
            Assert.That(Issues(root, UIIssueCode.ViewUnreachable), Is.Empty);
            UIAuthoring.Disconnect(root, "lost", "openOrphan");
            Assert.That(Issues(root, UIIssueCode.ViewUnreachable).Select(issue => issue.ViewId),
                Is.EqualTo(new[] { "orphan" }));

            // 補上路由之後消失。起始頁本身沒有任何路由指向它，也不算。
            UIAuthoring.Connect(root, "settings", "openOrphan", UIRouteKind.ShowPage, "orphan");
            Assert.That(Issues(root, UIIssueCode.ViewUnreachable), Is.Empty);
        }

        // 4-7 追加：「任何畫面」的訊號路由。
        [Test]
        public void SignalRouteTargetsCountAsReachableButNotAsExits()
        {
            var root = WorkspaceWithHomePage("Signal Reachability");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.CreatePage(root, "challenger", "Challenger");
            UIAuthoring.CreatePage(root, "versus", "Versus");
            UIAuthoring.CreatePopup(root, "alert", "Alert", $"{folder}/Alert.prefab");
            Assert.That(Unreachable(), Is.EquivalentTo(new[] { "challenger", "versus", "alert" }));

            // 訊號路由的目標算走得到；Emit 的訊號不會帶玩家到新的畫面。
            UIAuthoring.Connect(root, UIRoot.AnySource, "joined", UIRouteKind.ShowPage, "challenger", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, UIRoot.AnySource, "tick", UIRouteKind.Emit);
            Assert.That(Unreachable(), Is.EquivalentTo(new[] { "versus", "alert" }));
            UIAuthoring.Connect(root, UIRoot.AnySource, "lost", UIRouteKind.OpenPopup, "alert");
            Assert.That(Unreachable(), Is.EqualTo(new[] { "versus" }));

            // 從訊號路由的目標出發的路由也算。
            UIAuthoring.Connect(root, "challenger", "fight", UIRouteKind.ShowPage, "versus");
            Assert.That(Unreachable(), Is.Empty);

            // 訊號路由不算任何畫面的出口：home 與 versus 沒有自己的 ShowPage 或 Back，alert 關不掉。
            Assert.That(Issues(root, UIIssueCode.ViewNoExit).Select(issue => issue.ViewId),
                Is.EquivalentTo(new[] { "home", "versus", "alert" }));
            UIAuthoring.Connect(root, "home", "start", UIRouteKind.ShowPage, "versus");
            UIAuthoring.Connect(root, "versus", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "alert", "close", UIRouteKind.ClosePopup);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);

            // 沒有起始頁時照舊只回報 start.none，不逐一回報走不到的畫面。
            UIAuthoring.SetStartPage(root, null);
            Assert.That(Issues(root, UIIssueCode.StartNone).Length, Is.EqualTo(1));
            Assert.That(Unreachable(), Is.Empty);

            string[] Unreachable()
            {
                return Issues(root, UIIssueCode.ViewUnreachable).Select(issue => issue.ViewId).ToArray();
            }
        }

        [Test]
        public void ViewsWithoutAWayOutAreWarnings()
        {
            var root = WorkspaceWithHomePage("Batch4 Exits");
            UIAuthoring.SetStartPage(root, "home");

            // 不算：工作區只有一個頁面時的那個頁面。
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);

            // 頁面：要有 ShowPage 或 Back。OpenPopup 與 Emit 不算出口。
            UIAuthoring.CreatePage(root, "settings", "Settings");
            UIAuthoring.CreatePage(root, "shop", "Shop");
            UIAuthoring.CreatePopup(root, "tip", "Tip", $"{folder}/Tip.prefab");
            UIAuthoring.Connect(root, "home", "openSettings", UIRouteKind.ShowPage, "settings");
            UIAuthoring.Connect(root, "home", "openShop", UIRouteKind.ShowPage, "shop");
            UIAuthoring.Connect(root, "settings", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "shop", "openTip", UIRouteKind.OpenPopup, "tip");
            UIAuthoring.Connect(root, "shop", "buy", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "tip", "close", UIRouteKind.ClosePopup);
            var page = Issues(root, UIIssueCode.ViewNoExit).Single();
            Assert.That(page.ViewId, Is.EqualTo("shop"));
            Assert.That(page.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(page.Message, Does.Contain("shop").And.Contain("Back"));
            UIAuthoring.SetNavigatedByCode(root, "shop", true);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty, "由程式控制的頁面不算");
            UIAuthoring.SetNavigatedByCode(root, "shop", false);
            UIAuthoring.Connect(root, "shop", "back", UIRouteKind.Back);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);

            // Popup：ClosePopup、Back、ShowPage 任何一種，或點遮罩關閉。
            foreach (var id in new[] { "stuck", "byBack", "byPage", "byOverlay", "byCode" })
            {
                UIAuthoring.CreatePopup(root, id, id, $"{folder}/{id}.prefab");
                UIAuthoring.Connect(root, "home", "open_" + id, UIRouteKind.OpenPopup, id);
            }
            UIAuthoring.Connect(root, "stuck", "ping", UIRouteKind.Emit);
            UIAuthoring.Connect(root, "byBack", "back", UIRouteKind.Back);
            UIAuthoring.Connect(root, "byPage", "leave", UIRouteKind.ShowPage, "settings");
            EditPrefab($"{folder}/byOverlay.prefab",
                contents => contents.GetComponent<UIView>().closesOnOverlayClick = true);
            UIAuthoring.SetNavigatedByCode(root, "byCode", true);
            var popup = Issues(root, UIIssueCode.ViewNoExit).Single();
            Assert.That(popup.ViewId, Is.EqualTo("stuck"));
            Assert.That(popup.Message, Does.Contain("stuck").And.Contain("ClosePopup").And.Contain("遮罩"));
            UIAuthoring.Connect(root, "stuck", "close", UIRouteKind.ClosePopup);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);
        }

        // K4：遊戲頁只有「開啟暫停選單」，暫停選單有「回標題」：遊戲頁離得開。
        [Test]
        public void PagesCanLeaveThroughAPopupTheyOpen()
        {
            var root = WorkspaceWithHomePage("Batch4Fix Popup Exits");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.CreatePage(root, "game", "Game");
            UIAuthoring.CreatePopup(root, "pause", "Pause", $"{folder}/Pause.prefab");
            UIAuthoring.CreatePopup(root, "confirm", "Confirm quit", $"{folder}/ConfirmQuit.prefab");
            UIAuthoring.Connect(root, "home", "play", UIRouteKind.ShowPage, "game");
            UIAuthoring.Connect(root, "game", "pause", UIRouteKind.OpenPopup, "pause");
            UIAuthoring.Connect(root, "pause", "resume", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "confirm", "cancel", UIRouteKind.ClosePopup);

            // 暫停選單只能關閉：遊戲頁沒有出口。
            Assert.That(NoExit(), Is.EqualTo(new[] { "game" }));
            // 切回遊戲頁本身不算離開。
            UIAuthoring.Connect(root, "pause", "restart", UIRouteKind.ShowPage, "game");
            Assert.That(NoExit(), Is.EqualTo(new[] { "game" }));

            // 暫停選單有切到別頁的 ShowPage：算有出口。
            UIAuthoring.Connect(root, "pause", "quit", UIRouteKind.ShowPage, "home");
            Assert.That(NoExit(), Is.Empty);
            UIAuthoring.Disconnect(root, "pause", "quit");
            Assert.That(NoExit(), Is.EqualTo(new[] { "game" }));

            // Popup 再開的 Popup 也算。
            UIAuthoring.Connect(root, "pause", "askQuit", UIRouteKind.OpenPopup, "confirm");
            Assert.That(NoExit(), Is.EqualTo(new[] { "game" }));
            UIAuthoring.Connect(root, "confirm", "quit", UIRouteKind.ShowPage, "home");
            Assert.That(NoExit(), Is.Empty);

            // 訊號開啟的 Popup 不算這個頁面的出口：訊號路由不屬於任何畫面。
            UIAuthoring.Disconnect(root, "pause", "askQuit");
            UIAuthoring.Connect(root, UIRoot.AnySource, "askQuit", UIRouteKind.OpenPopup, "confirm");
            Assert.That(NoExit(), Is.EqualTo(new[] { "game" }));

            string[] NoExit()
            {
                return Issues(root, UIIssueCode.ViewNoExit).Select(issue => issue.ViewId).ToArray();
            }
        }

        [Test]
        public void ButtonsThatDoNothingAreWarnings()
        {
            var root = WorkspaceWithHomePage("Batch4 Buttons");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            UIAuthoring.Connect(root, "home", "go", UIRouteKind.Emit);

            // 會回報：沒有 UIButtonAction，或動作是空的。
            NewButton(home, "Bare");
            SetElementId(NewButton(home, "Registered").gameObject, "idle");
            NewButton(home, "Blank").gameObject.AddComponent<UIButtonAction>();
            var issues = Issues(root, UIIssueCode.ButtonUnbound);
            Assert.That(issues.Length, Is.EqualTo(3));
            Assert.That(issues.Select(issue => issue.ViewId), Is.All.EqualTo("home"));
            Assert.That(issues.Select(issue => issue.ElementId), Is.EquivalentTo(new[] { null, "idle", null }));
            Assert.That(issues.Any(issue => issue.Message.Contains("Bare")), Is.True);
            Assert.That(issues.Any(issue => issue.Message.Contains("Blank")), Is.True);
            // 訊息建議標準做法。
            foreach (var issue in issues)
                Assert.That(issue.Message, Does.Contain("action").And.Contain("路由").And.Contain("Emit"));

            // 不算：有動作的按鈕、在 Inspector 接了監聽的按鈕。
            UIAuthoring.BindButton(NewButton(home, "Bound"), "go");
            UnityEventTools.AddBoolPersistentListener(NewButton(home, "Wired").onClick, home.gameObject.SetActive, false);

            // 不算：清單項目裡的按鈕。
            var listObject = new GameObject("List", typeof(RectTransform), typeof(UIList));
            listObject.transform.SetParent(home.Content, false);
            var listContent = new GameObject("Content", typeof(RectTransform));
            listContent.transform.SetParent(listObject.transform, false);
            listObject.GetComponent<UIList>().content = (RectTransform)listContent.transform;
            var item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(listContent.transform, false);

            // 不算：由程式控制的畫面裡的按鈕。
            var loading = UIAuthoring.CreatePage(root, "loading", "Loading");
            NewButton(loading, "Skip");
            UIAuthoring.SetNavigatedByCode(root, "loading", true);

            Assert.That(Issues(root, UIIssueCode.ButtonUnbound).Length, Is.EqualTo(3));
            UIAuthoring.SetNavigatedByCode(root, "loading", false);
            Assert.That(Issues(root, UIIssueCode.ButtonUnbound).Select(issue => issue.ViewId),
                Has.Exactly(1).EqualTo("loading"));

            static Button NewButton(UIView view, string name)
            {
                var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(view.Content, false);
                return button.GetComponent<Button>();
            }
        }

        [Test]
        public void MissingGlyphsAreWarningsAndTheCheckLeavesFontAssetsAlone()
        {
            var widgets = UseIsolatedWidgets();
            var root = WorkspaceWithHomePage("Batch4 Glyphs");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var set = UIWidgets.Find();

            // 都來自同一個字型檔，沒有中文字。靜態字型的圖集只有 A、B、C；動態字型的圖集是空的。
            var dynamic = NewFont("Dynamic", "", true);
            var staticOnly = NewFont("StaticOnly", "ABC", false);
            var staticWithFallback = NewFont("StaticWithFallback", "ABC", false, dynamic);

            var lacking = NewText(home, "Lacking", staticOnly, "ABC\tDE \n D");
            SetElementId(lacking.gameObject, "caption");
            NewText(home, "Covered", staticWithFallback, "ABCDE");
            NewText(home, "FromSource", dynamic, "Hello, world!");
            NewText(home, "Chinese", dynamic, "確定 OK");
            NewText(home, "Empty", staticOnly, "");
            NewText(home, "Blank", staticOnly, " \t\n​");
            NewText(home, "Many", staticOnly, "abcdefghijklmnop");

            // 輸入框的 placeholder 與下拉選單的選項也在範圍內。
            var input = (GameObject)PrefabUtility.InstantiatePrefab(set.Find("InputField").Prefab, home.Content);
            input.name = "playerName";
            SetElementId(input, "playerName");
            var placeholder = (TMP_Text)input.GetComponent<TMP_InputField>().placeholder;
            placeholder.font = staticOnly;
            placeholder.text = "AX";
            var dropdownObject = (GameObject)PrefabUtility.InstantiatePrefab(set.Find("Dropdown").Prefab, home.Content);
            dropdownObject.name = "quality";
            SetElementId(dropdownObject, "quality");
            var dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            dropdown.captionText.font = staticWithFallback;
            dropdown.itemText.font = staticOnly;
            dropdown.options = new[] { "AB", "CQ", "AR" }.Select(text => new TMP_Dropdown.OptionData(text)).ToList();
            dropdown.captionText.text = "AB";
            dropdown.itemText.text = "A";

            // 檢查之前的狀態。字型資產這時已經被 TMP 讀過一次，之後的差異就是檢查造成的。
            // TMP 的預設字型與它的 fallback 也一起看：檢查會查到它們，一樣不可以動。
            AssetDatabase.SaveAssets();
            var created = new[] { dynamic, staticOnly, staticWithFallback };
            var watched = created.Append(TMP_Settings.defaultFontAsset)
                .Concat(TMP_Settings.defaultFontAsset.fallbackFontAssetTable ?? new List<TMP_FontAsset>())
                .Where(font => font).Distinct().ToArray();
            var files = created.ToDictionary(font => font, font => File.ReadAllBytes(AssetDatabase.GetAssetPath(font)));
            var states = watched.ToDictionary(font => font, font => EditorJsonUtility.ToJson(font));
            var counts = watched.ToDictionary(font => font,
                font => (font.characterTable.Count, font.glyphTable.Count));
            foreach (var font in created)
                Assert.That(EditorUtility.IsDirty(font), Is.False, font.name + "（檢查前）");
            Assert.That(dynamic.characterTable.Any(character => character.unicode == 'H'), Is.False);

            WithoutGlobalFonts(() =>
            {
                var issues = Issues(root, UIIssueCode.TextMissingGlyph);
                var byObject = issues.ToDictionary(issue => ((Component)issue.Context).name);
                Assert.That(byObject.Keys, Is.EquivalentTo(
                    new[] { "Lacking", "Chinese", "Many", "Placeholder", "quality" }), string.Join("\n", issues));

                // 靜態字型：只看圖集裡有的字元。空白與控制字元不算，同一個字元只列一次。
                var first = byObject["Lacking"];
                Assert.That(first.Severity, Is.EqualTo(UIIssueSeverity.Warning));
                Assert.That(first.ViewId, Is.EqualTo("home"));
                Assert.That(first.ElementId, Is.EqualTo("caption"));
                Assert.That(first.Message, Does.Contain("「D」「E」").And.Contain("StaticOnly")
                    .And.Contain("defaultFontPath").And.Not.Contain("「A」"));

                // 動態字型：來源字型檔沒有的字元才算。
                Assert.That(byObject["Chinese"].Message, Does.Contain("「確」「定」").And.Not.Contain("「O」"));

                // 最多列十個，其餘只報數量。
                Assert.That(byObject["Many"].Message, Does.Contain("「a」").And.Contain("「j」")
                    .And.Not.Contain("「k」").And.Contain("16 個"));

                // placeholder 與選項。選項用項目文字的字型判斷。
                Assert.That(byObject["Placeholder"].ElementId, Is.EqualTo("playerName"));
                Assert.That(byObject["Placeholder"].Message, Does.Contain("「X」"));
                Assert.That(byObject["quality"].ElementId, Is.EqualTo("quality"));
                Assert.That(byObject["quality"].Message, Does.Contain("選項").And.Contain("「Q」「R」"));
            });

            // TMP Settings 的全域 fallback 補得到的字元不算。
            WithoutGlobalFonts(() =>
            {
                TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset> { dynamic };
                Assert.That(Issues(root, UIIssueCode.TextMissingGlyph)
                    .Select(issue => ((Component)issue.Context).name), Is.EqualTo(new[] { "Chinese" }));
            });

            // TMP 的預設字型也是最後的 fallback：它有這些英文字母，所以只剩中文字。
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null);
            Assert.That(Issues(root, UIIssueCode.TextMissingGlyph)
                .Select(issue => ((Component)issue.Context).name), Is.EqualTo(new[] { "Chinese" }));

            // 檢查不可以改動任何字型資產：圖集沒有多出字元，資產沒有被標成已修改，存檔之後檔案內容相同。
            // 動態字型被問過「Hello, world!」這些字元，但它們沒有被加進圖集。
            Assert.That(dynamic.characterTable.Any(character => character.unicode == 'H'), Is.False);
            foreach (var font in watched)
            {
                Assert.That((font.characterTable.Count, font.glyphTable.Count), Is.EqualTo(counts[font]), font.name);
                Assert.That(EditorJsonUtility.ToJson(font), Is.EqualTo(states[font]), font.name);
            }
            foreach (var font in created)
                Assert.That(EditorUtility.IsDirty(font), Is.False, font.name + "（檢查後）");
            AssetDatabase.SaveAssets();
            foreach (var font in created)
                Assert.That(File.ReadAllBytes(AssetDatabase.GetAssetPath(font)), Is.EqualTo(files[font]), font.name);
            Assert.That(widgets, Is.Not.Empty);

            static TMP_Text NewText(UIView view, string name, TMP_FontAsset font, string content)
            {
                var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(view.Content, false);
                var label = text.GetComponent<TMP_Text>();
                label.font = font;
                label.text = content;
                return label;
            }
        }

        [Test]
        public void GuideExamplePlanOnlyWarnsAboutTheConfirmButtonsWithoutAChineseFont()
        {
            RequireDOTween();
            UseIsolatedWidgets();
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UIValidation).Assembly);
            var guide = File.ReadAllText(Path.Combine(package.resolvedPath, "Documentation~", "AgentGuide.md"));
            var plan = Regex.Match(guide, "```json\\s*(\\{.*?\\})\\s*```", RegexOptions.Singleline).Groups[1].Value;
            Assert.That(plan, Does.Contain("\"startPage\": \"workshop\""), "要是操作指南的第一份示例計畫");

            // 示例計畫原文，只把 Scene 與確認框的路徑換到測試用的資料夾。
            var scenePath = $"{folder}/Workshop.unity";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            var root = UIPlan.Apply(plan.Replace("Assets/Scenes/Workshop.unity", scenePath)
                .Replace("Assets/UniGameUI/Widgets/Confirm.prefab", UIWidgets.ConfirmPrefabPath), true);
            scenes.Add(root.gameObject.scene);
            Assert.That(root.Entries.Count, Is.EqualTo(4));
            Assert.That(root.Routes.Count, Is.EqualTo(5));

            var issues = UIValidation.Check(root);
            Assert.That(issues.Where(issue => issue.Severity == UIIssueSeverity.Error), Is.Empty);
            var added = new[]
            {
                UIIssueCode.StartNone, UIIssueCode.ViewUnreachable, UIIssueCode.ViewNoExit,
                UIIssueCode.ButtonUnbound, UIIssueCode.TextMissingGlyph
            };
            var warnings = issues.Where(issue => added.Contains(issue.Code)).ToArray();
            Assert.That(warnings.Select(issue => (issue.Code, issue.ViewId, issue.ElementId)), Is.EquivalentTo(new[]
            {
                (UIIssueCode.TextMissingGlyph, "confirm", "confirm"),
                (UIIssueCode.TextMissingGlyph, "confirm", "cancel")
            }), string.Join("\n", issues));
            Assert.That(warnings.Single(issue => issue.ElementId == "confirm").Message, Does.Contain("「確」「定」"));
            Assert.That(warnings.Single(issue => issue.ElementId == "cancel").Message, Does.Contain("「取」「消」"));
            // 除了這兩筆，示例計畫沒有其他 Warning。
            Assert.That(issues.Count, Is.EqualTo(2), string.Join("\n", issues));
        }

        private static UIIssue[] Issues(UIRoot root, string code)
        {
            return UIValidation.Check(root).Where(issue => issue.Code == code).ToArray();
        }

        // 從 TMP 附的字型檔建立字型資產並存成檔案。characters 是預先放進圖集的字元。
        private TMP_FontAsset NewFont(string name, string characters, bool dynamic, params TMP_FontAsset[] fallbacks)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(TmpSourceFont);
            Assert.That(source, Is.Not.Null, TmpSourceFont);
            var font = TMP_FontAsset.CreateFontAsset(source, 48, 4, GlyphRenderMode.SDFAA, 256, 256,
                AtlasPopulationMode.Dynamic, false);
            font.name = name;
            if (characters.Length > 0)
                Assert.That(font.TryAddCharacters(characters), Is.True, name);
            if (!dynamic)
                font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.fallbackFontAssetTable = fallbacks.ToList();

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            AssetDatabase.CreateAsset(font, $"{folder}/{name}.asset");
            font.material.name = name + " Material";
            font.atlasTexture.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Assert.That(EditorUtility.IsDirty(font), Is.False);
            return font;
        }

        // 暫時拿掉 TMP Settings 的全域 fallback 與預設字型，讓案例只看字型資產自己的內容。
        private static void WithoutGlobalFonts(Action body)
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            var fallbacks = TMP_Settings.fallbackFontAssets;
            TMP_Settings.defaultFontAsset = null;
            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
            try
            {
                body();
            }
            finally
            {
                TMP_Settings.defaultFontAsset = defaultFont;
                TMP_Settings.fallbackFontAssets = fallbacks;
            }
        }
    }
}
