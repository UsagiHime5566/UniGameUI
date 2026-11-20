using System.Linq;
using NUnit.Framework;

namespace UniGameUI.Editor.Tests
{
    // 第五批 5-8：頁面自己的流程。「由程式控制」只代表進來的方式，頁面照樣要有出口；起始頁不需要這個標記。
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void NavigatedByCodeOnlyCoversTheWayIntoAPage()
        {
            var root = WorkspaceWithHomePage("Batch5 Flow Exits");
            UIAuthoring.SetStartPage(root, "home");
            UIAuthoring.CreatePage(root, "morning", "Morning");
            UIAuthoring.CreatePage(root, "dayEnd", "Day end");
            UIAuthoring.CreatePopup(root, "picker", "Picker", $"{folder}/Picker.prefab");
            UIAuthoring.Connect(root, "home", "newGame", UIRouteKind.ShowPage, "morning", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, "morning", "toDayEnd", UIRouteKind.ShowPage, "dayEnd", UIHistoryMode.Clear);
            UIAuthoring.Connect(root, "morning", "pick", UIRouteKind.OpenPopup, "picker");
            UIAuthoring.Connect(root, "picker", "close", UIRouteKind.ClosePopup);
            UIAuthoring.Connect(root, "dayEnd", "nextDay", UIRouteKind.Emit);

            // 日終結算只有 Emit：沒有出口。訊息提示用這一頁的路由與 NavigateAsync。
            var plain = Issues(root, UIIssueCode.ViewNoExit).Single();
            Assert.That(plain.ViewId, Is.EqualTo("dayEnd"));
            Assert.That(plain.Message, Does.Contain("dayEnd").And.Contain("這一頁的路由").And.Contain("NavigateAsync")
                .And.Not.Contain("由程式控制只表示進來的方式"));

            // 標記為由程式控制：照樣回報，並說明標記只代表進來的方式。
            UIAuthoring.SetNavigatedByCode(root, "dayEnd", true);
            var marked = Issues(root, UIIssueCode.ViewNoExit).Single();
            Assert.That(marked.ViewId, Is.EqualTo("dayEnd"));
            Assert.That(marked.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(marked.Message, Does.Contain("由程式控制只表示進來的方式").And.Contain("NavigateAsync"));
            // 只被訊號路由帶進來的頁面也一樣：訊號路由不是任何畫面的出口。
            UIAuthoring.Connect(root, UIRoot.AnySource, "showDayEnd", UIRouteKind.ShowPage, "dayEnd");
            Assert.That(Issues(root, UIIssueCode.ViewNoExit).Select(issue => issue.ViewId),
                Is.EqualTo(new[] { "dayEnd" }));

            // 每個可能的去處各一條這一頁的路由：不回報。
            UIAuthoring.Connect(root, "dayEnd", "toMorning", UIRouteKind.ShowPage, "morning", UIHistoryMode.Clear);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);

            // Popup 標記為由程式控制：照舊不檢查出口（確認框這類 Popup 是自己用 View.Close() 關閉的）。
            UIAuthoring.Disconnect(root, "picker", "close");
            Assert.That(Issues(root, UIIssueCode.ViewNoExit).Select(issue => issue.ViewId),
                Is.EqualTo(new[] { "picker" }));
            UIAuthoring.SetNavigatedByCode(root, "picker", true);
            Assert.That(Issues(root, UIIssueCode.ViewNoExit), Is.Empty);
            // 走不走得到也照舊不檢查。
            UIAuthoring.Disconnect(root, "morning", "pick");
            Assert.That(Issues(root, UIIssueCode.ViewUnreachable), Is.Empty);
        }

        [Test]
        public void StartPageMarkedAsNavigatedByCodeIsAWarning()
        {
            var root = WorkspaceWithHomePage("Batch5 Start By Code");
            UIAuthoring.CreatePage(root, "next", "Next");
            UIAuthoring.Connect(root, "home", "go", UIRouteKind.ShowPage, "next");
            UIAuthoring.Connect(root, "next", "back", UIRouteKind.Back);
            UIAuthoring.SetStartPage(root, "home");
            Assert.That(Issues(root, UIIssueCode.StartNavigatedByCode), Is.Empty);

            // 其他頁面標記不算。
            UIAuthoring.SetNavigatedByCode(root, "next", true);
            Assert.That(Issues(root, UIIssueCode.StartNavigatedByCode), Is.Empty);

            UIAuthoring.SetNavigatedByCode(root, "home", true);
            var issue = Issues(root, UIIssueCode.StartNavigatedByCode).Single();
            Assert.That(issue.Severity, Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(UIIssueCode.SeverityOf(UIIssueCode.StartNavigatedByCode), Is.EqualTo(UIIssueSeverity.Warning));
            Assert.That(issue.ViewId, Is.EqualTo("home"));
            Assert.That(issue.Message, Does.Contain("home").And.Contain("起始頁").And.Contain("Start"));
            Assert.That(UIIssueCode.All, Does.Contain("start.navigated-by-code"));

            // 沒有起始頁時不回報這一項。
            UIAuthoring.SetStartPage(root, null);
            Assert.That(Issues(root, UIIssueCode.StartNavigatedByCode), Is.Empty);
        }
    }
}
