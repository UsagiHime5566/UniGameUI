using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Tests
{
    public sealed partial class UINavigationTests
    {
        // 第六輪審核 J8：Play Mode 中的 Validate 會不會把「沒有設定預設選取元件」判成「已經遺失」。
        // Validate 在 Editor 組件裡，這個測試組件不能直接引用它，所以用反射呼叫。
        [UnityTest]
        public IEnumerator ValidationInPlayModeTellsUnsetDefaultSelectionFromAMissingOne()
        {
            CreateRoot();
            // 從資產載入的 Prefab，以及在 Play Mode 複製出來的實例：兩者都是 Unity 反序列化出來的物件。
            // Validate 會檢查登記的審核畫面與 Prefab 本體，所以各登記一個。
            var prefab = UIWidgetTestAssets.Load("PlainView").GetComponent<UIView>();
            var copy = Object.Instantiate(prefab, root.reviewLayer);
            root.entries.Add(new UIEntry { id = "copied", kind = UIEntryKind.Page, review = copy });
            root.entries.Add(new UIEntry { id = "plain", kind = UIEntryKind.Popup, prefab = prefab });
            AddNavButton(first, "start", Vector2.zero);
            rootObject.SetActive(true);
            yield return StartPage();
            yield return Expect(root.OpenPopupAsync("plain"), UINavResult.Completed);
            Assert.That(root.TopPopup.DefaultSelected == null, Is.True);
            Assert.That(copy.DefaultSelected == null && prefab.DefaultSelected == null, Is.True);

            Assert.That(Missing(), Is.Empty, "沒有設定的預設選取元件不是遺失");

            // 真的指到一個已經銷毀的物件時要回報。
            var doomed = new GameObject("Doomed", typeof(RectTransform), typeof(Image), typeof(Button));
            doomed.transform.SetParent(copy.Content, false);
            copy.defaultSelected = doomed.GetComponent<Button>();
            Assert.That(Missing(), Is.Empty);
            Object.DestroyImmediate(doomed);
            Assert.That(Missing().Single(), Does.Contain("copied"));

            string[] Missing()
            {
                var validation = Type.GetType("UniGameUI.Editor.UIValidation, UniGameUI.Editor", true);
                var check = validation.GetMethod("Check", new[] { typeof(UIRoot) });
                return ((IEnumerable)check.Invoke(null, new object[] { root })).Cast<object>()
                    .Select(issue => issue.ToString()).Where(issue => issue.Contains("已經遺失")).ToArray();
            }
        }
    }
}
