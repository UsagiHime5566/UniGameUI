using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace UniGameUI.Editor.Tests
{
    // 第四批審核後的修正（K1～K8）裡，Validate 這一側的案例。
    public sealed partial class UIAuthoringTests
    {
        // K7：Call State 是 Off 的監聽不會被呼叫，不算有接監聽。
        [Test]
        public void InspectorListenersThatAreSwitchedOffDoNotCountAsBound()
        {
            var root = WorkspaceWithHomePage("Batch4Fix Listener State");
            var home = UIAuthoring.FindEntry(root, "home").Review;

            var wired = FixButton(home, "Wired");
            UnityEventTools.AddBoolPersistentListener(wired.onClick, home.gameObject.SetActive, false);
            var off = FixButton(home, "SwitchedOff");
            UnityEventTools.AddBoolPersistentListener(off.onClick, home.gameObject.SetActive, false);
            off.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
            // 兩個監聽裡只要有一個會被呼叫就算。
            var mixed = FixButton(home, "Mixed");
            UnityEventTools.AddBoolPersistentListener(mixed.onClick, home.gameObject.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(mixed.onClick, home.gameObject.SetActive, true);
            mixed.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);

            var issues = Issues(root, UIIssueCode.ButtonUnbound);
            Assert.That(issues.Select(issue => ((Component)issue.Context).name), Is.EqualTo(new[] { "SwitchedOff" }),
                string.Join("\n", issues));
        }

        // K7：富文字標籤不是要畫出來的字元。
        [Test]
        public void GlyphCheckSkipsRichTextTags()
        {
            var root = WorkspaceWithHomePage("Batch4Fix Rich Text");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var font = NewFont("OnlyHi", "Hi", false);

            FixText(home, "Tagged", font,
                "<size=120%>H</size><b>i</b><color=#FF0000>H</color><#00FF00>i</color>" +
                "<sprite name=\"coin\"><br><align=\"center\">H</align><link=\"a b\">i</link>");
            // <font> 裡的文字用的是另一個字型，這裡不檢查。
            FixText(home, "OtherFont", font, "H<font=\"Somewhere SDF\">XYZ</font>i");
            // <noparse> 裡的標籤會照字面畫出來。
            FixText(home, "NoParse", font, "H<noparse><b></noparse>i");
            // 不是標籤的角括號，以及 TMP 不認得的標籤，都會照字面畫出來。
            FixText(home, "NotATag", font, "H < i > H<hello>i");
            // 關掉富文字時，標籤就是一般的字元。
            var literal = FixText(home, "Literal", font, "<b>Hi</b>");
            literal.richText = false;

            WithoutGlobalFonts(() =>
            {
                var issues = Issues(root, UIIssueCode.TextMissingGlyph);
                var byObject = issues.ToDictionary(issue => ((Component)issue.Context).name, issue => issue.Message);
                Assert.That(byObject.Keys, Is.EquivalentTo(new[] { "NoParse", "NotATag", "Literal" }),
                    string.Join("\n", issues));
                Assert.That(byObject["NoParse"], Does.Contain("「<」「b」「>」").And.Not.Contain("「n」"));
                Assert.That(byObject["NotATag"], Does.Contain("「<」「>」「h」「e」「l」「o」"));
                Assert.That(byObject["Literal"], Does.Contain("「<」「b」「>」「/」"));
            });
        }

        // K7：Sprite Asset 補得到的字元（例如表情符號）TMP 畫得出來。
        [Test]
        public void GlyphCheckAcceptsCharactersCoveredBySpriteAssets()
        {
            var root = WorkspaceWithHomePage("Batch4Fix Sprites");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var font = NewFont("OnlyHi", "Hi", false);
            var icons = NewSpriteAsset("Icons", 0x2605);
            var more = NewSpriteAsset("MoreIcons", 0x1F3AE);
            icons.fallbackSpriteAssets = new System.Collections.Generic.List<TMP_SpriteAsset> { more };
            EditorUtility.SetDirty(icons);
            AssetDatabase.SaveAssets();

            // 文字自己的 Sprite Asset 與它的 fallback。
            FixText(home, "Starred", font, "H\u2605i\U0001F3AE").spriteAsset = icons;
            // 沒有指定 Sprite Asset 的文字照舊回報。
            FixText(home, "Plain", font, "H\u2605i");

            var defaultSprites = TMP_Settings.defaultSpriteAsset;
            WithoutGlobalFonts(() =>
            {
                TMP_Settings.defaultSpriteAsset = null;
                try
                {
                    var issues = Issues(root, UIIssueCode.TextMissingGlyph);
                    Assert.That(issues.Select(issue => ((Component)issue.Context).name), Is.EqualTo(new[] { "Plain" }),
                        string.Join("\n", issues));

                    // TMP Settings 的預設 Sprite Asset 是最後一層。
                    TMP_Settings.defaultSpriteAsset = icons;
                    Assert.That(Issues(root, UIIssueCode.TextMissingGlyph), Is.Empty);
                }
                finally
                {
                    TMP_Settings.defaultSpriteAsset = defaultSprites;
                }
            });
            Assert.That(EditorUtility.IsDirty(icons), Is.False, "檢查不可以改動 Sprite Asset");
        }

        // K7：清單項目裡的文字也是預設內容。規格只有 button.unbound 才不算清單項目。
        [Test]
        public void GlyphCheckIncludesTextInsideListItems()
        {
            var root = WorkspaceWithHomePage("Batch4Fix List Text");
            var home = UIAuthoring.FindEntry(root, "home").Review;
            var font = NewFont("OnlyHi", "Hi", false);

            var listObject = new GameObject("List", typeof(RectTransform), typeof(UIList));
            listObject.transform.SetParent(home.Content, false);
            var listContent = new GameObject("Content", typeof(RectTransform));
            listContent.transform.SetParent(listObject.transform, false);
            listObject.GetComponent<UIList>().content = (RectTransform)listContent.transform;
            var item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(listContent.transform, false);
            var label = new GameObject("ItemLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(item.transform, false);
            label.GetComponent<TMP_Text>().font = font;
            label.GetComponent<TMP_Text>().text = "HiZ";

            WithoutGlobalFonts(() =>
            {
                var issue = Issues(root, UIIssueCode.TextMissingGlyph).Single();
                Assert.That(((Component)issue.Context).name, Is.EqualTo("ItemLabel"));
                Assert.That(issue.Message, Does.Contain("「Z」"));
            });
            // 清單項目裡的按鈕照舊不回報 button.unbound。
            Assert.That(Issues(root, UIIssueCode.ButtonUnbound), Is.Empty);
        }

        // K7：讀另一個動態字型的對照表時，TMP 會把字型引擎目前的字型換掉。之後問原本的字型不可以問錯。
        [Test]
        public void GlyphCheckAsksTheRightFontAfterAnotherDynamicFontWasRead()
        {
            const string systemFont = "C:/Windows/Fonts/kaiu.ttf";
            if (!File.Exists(systemFont))
                Assert.Ignore("這個案例需要一個有中文字的字型檔（Windows 的標楷體）。");
            var root = WorkspaceWithHomePage("Batch4Fix Font Faces");
            var home = UIAuthoring.FindEntry(root, "home").Review;

            // 兩個動態字型來自不同的字型檔：一個有中文字，一個沒有。
            var copied = $"{folder}/Kai.ttf";
            File.Copy(systemFont, copied, true);
            AssetDatabase.ImportAsset(copied);
            var chinese = NewFontFrom(AssetDatabase.LoadAssetAtPath<Font>(copied), "Chinese", "", true);
            var latin = NewFont("Latin", "A", true);

            FixText(home, "First", chinese, "\u78BA");
            FixText(home, "Second", latin, "A");
            FixText(home, "Third", chinese, "\u5B9A");

            // 模擬剛從磁碟載入的字型資產：對照表還沒建立，第一次讀的時候 TMP 會載入這個字型的字型檔。
            var table = typeof(TMP_FontAsset).GetField("m_CharacterLookupDictionary",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(table, Is.Not.Null, "TMP 的欄位名稱變了，這個案例要跟著調整");
            table.SetValue(latin, null);

            WithoutGlobalFonts(() =>
            {
                var issues = Issues(root, UIIssueCode.TextMissingGlyph);
                Assert.That(issues, Is.Empty, string.Join("\n", issues));
            });
        }

        private static Button FixButton(UIView view, string name)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(view.Content, false);
            return button.GetComponent<Button>();
        }

        private static TMP_Text FixText(UIView view, string name, TMP_FontAsset font, string content)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(view.Content, false);
            var label = text.GetComponent<TMP_Text>();
            label.font = font;
            label.text = content;
            return label;
        }

        // 只有一個 Sprite 字元的 Sprite Asset，存成檔案。unicode 是那個字元對應的碼位。
        private TMP_SpriteAsset NewSpriteAsset(string name, uint unicode)
        {
            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = name;
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var glyph = new TMP_SpriteGlyph(0, new GlyphMetrics(32, 32, 0, 32, 32), new GlyphRect(0, 0, 32, 32), 1f, 0);
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(unicode, glyph) { name = name });
            asset.UpdateLookupTables();

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            AssetDatabase.CreateAsset(asset, $"{folder}/{name}.asset");
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
