# UniGameUI 操作指南

從這份文件的公開工具操作 Unity。交付時指出實際使用的 Scene、藍圖路徑、Play 驗證結果，以及需要專案自行接上的事件。

## 確認專案

1. 確認 Unity 6.3+、uGUI 2.0，並找出使用者要編輯的 Scene。套件匯入後沒有預建工作區或 UI。
2. 預設採用 `SingleScene`。頁面、參考解析度、節點位置與路由都存於 Scene 的 UniGameUI 工作區。所有 Popup 都是 Prefab。若使用者要共用頁面資產，建立工作區時可以明確指定 `Prefab` 模式。
3. 已存在的工作區沿用原模式與穩定 ID。記錄場景是否有尚未存檔的修改，並查看 `UIAuthoring.HasReviewOverrides(entry)`。有開啟中的 Editor 時，優先在其 Edit Mode 使用公開 C# API 或 **Apply JSON Plan…**。CLI 在該專案已由另一個 Editor 開啟時無法取得專案鎖，使用隔離副本測試。
4. 使用 `UIDOTweenSetup.IsReady` 或 `UIDOTweenSetup.Status` 確認 DOTween。狀態依序區分已就緒、已匯入但待 Setup、未安裝。請在 Edit Mode 完成安裝或 Setup，再套用 JSON 計畫或驗收。未就緒時，Validate 回報一項工作區錯誤，`UIPlan.Apply` 與 `ApplyAsync` 會直接回報安裝說明。執行期保留瞬間切換和一次性警告。

## 安裝 DOTween

- 互動 Editor 使用 **Tools → UniGameUI → Install DOTween…**。藍圖最上方也會顯示安裝或 Setup 提示。若程式在 Editor 裡執行，`UIDOTweenSetup.FindLocalPackage()` 回傳本機標準版套件路徑，`ImportLocalPackage()` 會在未安裝時提交本機匯入。
- CLI 先從 Asset Store 快取的 `Demigiant` 目錄遞迴尋找標準版 `DOTween HOTween v2.unitypackage`。快取在 Windows 的 `%APPDATA%\Unity\Asset Store-5.x`、macOS 的 `~/Library/Unity/Asset Store-5.x` 或 Linux 的 `~/.local/share/unity3d/Asset Store-5.x`。傳入完整路徑，若路徑含空格請加引號。

```text
Unity -batchmode -nographics -projectPath <專案絕對路徑> -importPackage <套件絕對路徑> -quit
```

匯入後重新啟動 Editor 確認 `UIDOTweenSetup.IsReady`。若顯示待 Setup，開啟 **Tools → Demigiant → DOTween Utility Panel**，執行 **Setup DOTween**。 [DOTween Asset Store 頁面](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) · [DOTween 安裝文件](https://dotween.demigiant.com/getstarted)

## 入口

- 開啟目標 Scene 後，**Tools → UniGameUI → Apply JSON Plan…** 可套用計畫。計畫可以省略 `scenePath` 以指定目前作用中的 Scene，然後依一般 Unity 流程存檔。
- CLI 用 `-executeMethod UniGameUI.Editor.UIPlan.RunFromCommandLine -unigameui-plan <JSON 絕對路徑>`。指定 `-batchmode -quit -projectPath <專案絕對路徑> -logFile <記錄檔絕對路徑>`。CLI 模式需要明確的 `scenePath`，成功後存入指定 Scene。
- 已有 Unity Editor C# 執行能力時，互動模式使用 `await UniGameUI.Editor.UIPlan.ApplyAsync(json)`。選單與此 API 會透過 Unity 原生套件匯入處理首次使用的 TMP 資源。需要在同一呼叫完成的批次或同步工具可以使用 `UIPlan.Apply(json)`。先檢查目前目標和已存在物件。

## JSON 計畫

以下是一份語法示例。依當次需求提供頁面、文字、視覺、按鈕和路由。

```json
{
  "scenePath": "Assets/Scenes/Workshop.unity",
  "createScene": true,
  "referenceResolution": { "x": 1280, "y": 720 },
  "reviewColumns": 3,
  "startPage": "workshop",
  "views": [
    {
      "id": "workshop",
      "title": "Workshop",
      "kind": "Page",
      "background": "#13253A",
      "elements": [
        {
          "id": "openArchive",
          "kind": "Button",
          "text": "Open archive",
          "action": "openArchive",
          "rect": {
            "anchor": { "x": 0.5, "y": 0.5 },
            "position": { "x": 0, "y": 0 },
            "size": { "x": 300, "y": 76 }
          }
        }
      ]
    },
    {
      "id": "archive",
      "title": "Archive",
      "kind": "Popup",
      "prefabPath": "Assets/UI/Archive.prefab",
      "background": "#25314A",
      "elements": [
        {
          "id": "closeArchive",
          "kind": "Button",
          "text": "Close",
          "action": "closeArchive"
        }
      ]
    }
  ],
  "routes": [
    { "source": "workshop", "action": "openArchive", "kind": "OpenPopup", "target": "archive" },
    { "source": "archive", "action": "closeArchive", "kind": "ClosePopup" }
  ]
}
```

- `views[].id`、`elements[].id` 和 `action` 使用 ASCII 字母、數字、底線或連字號。ID 是定位與接線用的鍵。Hierarchy 物件名稱可以調整。
- `mode` 可用 `SingleScene` 或 `Prefab`。只有確定需要新 Scene 時才給 `createScene: true`。新工作區預設參考解析度 1920×1080、每排 3 格。
- `startPage` 是可選的頁面 ID，指定空字串可清除起始頁。路由種類有 `ShowPage`、`OpenPopup`、`Back`、`ClosePopup`、`Emit`。前兩種需要 `target`。同一來源頁的一個 `action` 對應一條路由。
- `ShowPage` 可設定 `"history": "Replace"`。可用值為 `Push`（預設）、`Replace`、`Clear`。`ClosePopup` 可設定字串結果，例如 `"result": "yes"`。省略或留空代表這條路由關閉時沒有結果。將任一欄位寫在其他種類的路由上，計畫會在套用前指出路由與欄位。
- `pageTransitionMode` 可用 `Sequential` 或 `Concurrent`。新工作區預設依序播放。要讓切頁的進場和退場重疊，設定 `Concurrent` 或在 `UIRoot` Inspector 修改「切頁動畫」。
- 元件支援 `Text`、`Button`、`Image`。矩形座標是 Unity RectTransform 的 anchor 與參考解析度下的座標，Y 向上。可指定 `fontSize`、`fontPath`（TMP Font Asset）、`textColor`、`color`、`spritePath`。首次建立文字時，工具會安裝 uGUI 附帶的 TMP Essential Resources。若專案曾匯入部分資源，先透過 **Window → TextMeshPro → Import TMP Essential Resources** 完成安裝。使用中文或其他語系時，交付前確認字型涵蓋所需字符。
- `enter`、`exit` 可各自選 `None`、`Fade`、`Slide`、`RotateFade`、`Zoom` 或 `Custom`。例如 `"enter": { "preset": "Zoom", "duration": 0.2, "zoomScale": 0.8, "ease": "OutBack" }`。Zoom 倍率相對於原本的 Motion 尺寸，預設為 0.85。Slide 可設定方向 `Left`、`Right`、`Up`、`Down` 和 `distance`，RotateFade 可設定 `angle`。Ease 有 `Linear`、`InQuad`、`OutQuad`、`InOutQuad`、`OutBack`。
- 既有頁面可直接新增其他 ID 的元件。要修改既有元件時，指定該元件 ID、原 `kind`、`updateExisting: true` 與要寫入的欄位。若要更新既有 View 的背景或動畫，在 View 設 `updateExisting: true`。更新既有路由時，指定原 `source`、`action`、`replace: true` 和要改的欄位。平常省略這些選項，重跑可保留人工編修。
- 更新時只寫入有提供的欄位。`rect` 的 `anchor`、`position`、`size` 與 `enter`／`exit` 的各欄位若省略，維持目前的值；例如只給 `"rect": { "size": { "x": 320, "y": 80 } }` 不會移動元件，只給 `"enter": { "duration": 0.4 }` 不會改動畫種類。
- 新建立的 Prefab 會存入本次生成的內容。既有 Prefab 的調整會出現在 Scene 預覽並標示待套用，由使用者用 **套用到 Prefab** 審核後提交。先從藍圖查看待套用狀態。
- 成功套用計畫後，新增或改寫常數會觸發 Editor 重新編譯。等編譯和組件重載完成，再執行下一個 Editor 步驟或進入 Play Mode。

## ID 常數

遊戲程式一律使用產生的常數。場景存檔、成功套用 JSON 或在藍圖按 **Validate** 時，工具會產生或更新 `Assets/UniGameUI/Generated/<場景名稱>UI.cs`。首次使用未命名的場景時，先存成 Scene 資產。也可選 **Tools → UniGameUI → Generate ID Constants**，或呼叫 `UIIdConstants.Generate(root)`。等 Editor 編譯和組件重載完成再引用新的常數。

```csharp
using UniGameUI.Generated;

var result = await root.TriggerAsync(WorkshopUI.Views.Workshop, WorkshopUI.Actions.OpenArchive);
```

`Views` 含頁面與 Popup ID，`Actions` 含路由與按鈕使用的動作，`Elements` 按畫面分組並加上 `View` 後綴，例如 `WorkshopUI.Elements.WorkshopView.OpenArchive`。畫面和其元件可以使用相同的 ID，例如 `WorkshopUI.Elements.ConfirmView.Confirm`。同一組內的 ID 或工作區 Scene 名稱仍須避免轉成相同的 PascalCase 名稱。`UIValidation.Check(root)` 會回報名稱衝突並提示常數過期。

工具會在生成目錄沒有 asmdef 時建立 `UniGameUI.Generated.asmdef`。遊戲程式若有自己的 asmdef，請在它的 references 加入 `UniGameUI.Generated`。若生成目錄已有自訂 asmdef，請引用該組件。

## Editor C# API

- `UIAuthoring.FindWorkspace(scene)`、`CreateWorkspace(scene, mode)`、`ConfigureReview(root, resolution, columns)`。
- `UIAuthoring.CreatePage(root, id, title, prefabPath)`、`CreatePopup(root, id, title, prefabPath)` 回傳 Scene 中的 `UIView`。將自訂 uGUI 階層加到 `view.Content`。保留外框的排列用途；`view.Motion` 是預設動畫作用的位置。
- `UIAuthoring.FindEntry(root, id)`、`UIPlan.FindElement(view, id)`、`BindButton(button, action)`、`Connect(root, source, action, kind, target, history, result)`、`Disconnect(root, source, action)`、`SetStartPage(root, id)`。`history`、`result` 是可選參數。
- `UIAuthoring.UpdateRoute(root, source, originalAction, action, kind, target, history, result)` 用於更新既有路由。改名時一併更新來源 View 的按鈕綁定，並合併為一次 Undo。若來源是 Prefab 審核實例，將覆寫套用後 Prefab 才會使用新動作名稱。
- `UIAuthoring.SetTransition(view, entering, preset, duration, direction, ease, distance, angle, zoomScale)` 和 `SetPageTransitionMode(root, mode)`。
- `UIAuthoring.ArrangeReview(root)`、`FocusReview(root, id)`、`PreviewPopupOnPage(root, popupId, pageId)`、`HasReviewOverrides(entry)`、`ApplyReviewOverrides(root, id)`。最後一項適合提交本次新建立的 Prefab。既有實例的覆寫先交給使用者審核。
- `UIValidation.Check(root)` 回傳結構化錯誤與警告。對於自訂子階層，用 Unity Editor 的 Undo 和序列化 API 儲存變更。

## 遊戲程式

### 頁面腳本

繼承 `UIViewBehaviour`，把腳本掛在 `UIView` 的同一個 GameObject 上。已有基底類別的腳本可以實作 `IUIViewHandler`。使用 `View.Root` 取得所屬工作區，工作區準備畫面後才會設定此屬性，Edit Mode 下為 null。需要讀取顯示資料時用 `View.GetData<T>()`，它在資料為 null 或型別不符時會指出畫面和期望的型別。資料可省略時用 `View.TryGetData<T>(out var data)`。

下列範本對應 `Workshop.unity` 的 `workshop` 頁、`title` 文字元件、`deleteSave` Emit 路由和 `confirm` Popup。確認按鈕的 ClosePopup 路由回傳 `"yes"`。使用 asmdef 時，讓此腳本所在的組件引用 `UniGameUI.Runtime`、`UniGameUI.Generated`、`Unity.TextMeshPro`。

```csharp
using System;
using TMPro;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class WorkshopPage : UIViewBehaviour
{
    protected override void OnShowing(object data)
    {
        View.Get<TMP_Text>(WorkshopUI.Elements.WorkshopView.Title).text = "工坊";
    }

    protected override async void OnAction(string action)
    {
        if (action != WorkshopUI.Actions.DeleteSave)
            return;

        try
        {
            var answer = await Root.OpenPopupForResultAsync<string>(
                WorkshopUI.Views.Confirm, "確定刪除存檔？");
            if (answer.GetValueOrDefault() == "yes")
                DeleteSave();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void DeleteSave()
    {
        // 在這裡呼叫專案的存檔服務。
        Debug.Log("收到刪除存檔的確認。", this);
    }
}
```

`View.Get<T>(id)` 先找登記 ID 的物件，再找它底下的子物件，包含未啟用的物件。`View.GetElement(id)` 取得登記物件本身。`Get` 和 `GetElement` 查找失敗時會指出畫面、元件、型別和場景。`View.TryGet<T>(id, out var component)` 查找失敗時安靜回傳 false。手動加入的元件可從 **Add Component → UniGameUI → UI Element Id** 登記。

### 導航、返回與 Popup

遊戲程式可呼叫 `await root.ShowPageAsync(id, data)`、`await root.OpenPopupAsync(id, data)`、`await root.ClosePopupAsync()` 或 `await root.BackAsync()`。每個 Unity `Awaitable` 只等待一次。轉場中收到的導航請求會立即回傳 `Busy`，不排隊。

`root.ShowPageAsync(id, data, history)` 的 `Push` 會把離開的頁面加入歷史，`Replace` 會略過離開的頁面，`Clear` 會在切頁成功後清空歷史。切頁或返回成功後會移除歷史尾端與目前頁相同的項目。失敗或取消時保留原歷史。`HistoryCount` 可查還能返回幾頁，`ClearHistory()` 可直接清空。返回會先關最上層的 Popup。返回上一頁會重用頁面實例與上次顯示時的資料。

`CurrentPage`、`TopPopup`、`Popups` 可查目前畫面。`TryGetView(id, out var view)` 取得已存在的實例。單 Scene 頁面隨 Scene 載入，Prefab 頁面在首次顯示時建立，兩種頁面都會留到 Scene 卸載。Popup 在開啟期間才可查到。

`OpenPopupAsync` 等到進場結束。`OpenPopupForResultAsync<T>` 等到退場與隱藏通知結束。Popup 內可呼叫 `View.Close(value)`，或在 ClosePopup 路由設定字串結果。只有最上層且開著的 Popup 可以透過 `View.Close` 關閉。

`UIPopupResult<T>.Status` 反映開啟失敗、正常關閉或取消。`HasValue` 表示收到型別相符的結果，確認後可讀取 `Value`，也可直接呼叫 `GetValueOrDefault(fallback)`。返回、切頁或未指定結果的關閉會回傳 `Completed`，此時 `HasValue` 為 false。型別不符時 Console 會指出實際和期望的型別。取消 token、停用或銷毀工作區，以及 Popup 被外部銷毀都會以 `Canceled` 結束等待。取消 token 或停用工作區時，已開啟的 Popup 保持原狀。

### 生命週期與導航結果

每一階段依序呼叫 View 根節點上的介面接收者（依元件順序）、View Inspector 的 UnityEvent，再發出 `UIRoot.ViewShowing`、`ViewShown`、`ViewHiding` 或 `ViewHidden`。畫面啟用後、進場動畫開始前會觸發 `OnShowing(data)`。切頁時，依序收到舊頁 Hiding、新頁 Showing、舊頁 Hidden、新頁 Shown。隱藏後與顯示後的通知會等轉場成功。

- 第一次顯示時，會先執行 `Awake` 與 `OnShowing`。有跨影格的進場動畫時，已啟用接收者的 `Start` 在 `OnShowing` 與 `OnShown` 之間執行。瞬間切換時，`OnShown` 也可能早於 `Start`。把首次顯示所需的初始化放在 `Awake`，或在 `OnShowing` 裡用 `View.Get<T>()` 取得元件。 [Unity Start 文件](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/monobehaviour/start)
- 導航失敗或取消後，原畫面會還原並繼續顯示。它如果已經收到 `OnHiding`，之後不會收到 `OnHidden`，也不會再收到一次 `OnShowing`。需要在離開後才改變狀態的收尾工作放在 `OnHidden`。
- 把接收者元件的 `enabled` 設為 false 時，框架仍會呼叫它的生命週期方法。

`Emit` 路由依序呼叫來源畫面的 `OnAction(action)` 和 `UIRoot.ActionEmitted`。後者可以從 Inspector 接線，會提供來源 ID 和動作。`UIRoot.ActionTriggered` 表示路由已被接受，可用來播放按鈕音效。以沒有歷史的 Back 路由為例，會先發出 `ActionTriggered`，接著由 `ActionCompleted` 回報 `Unavailable`。

`UIRoot.ActionCompleted` 帶路由和執行結果，包含執行後的失敗或取消。關閉 Popup 的路由發出 `ActionCompleted` 時，等待該 Popup 結果的程式已先繼續執行，可能已經開始下一次導航。

在 View 的生命週期方法、對應的 UnityEvent、工作區的 View 事件及非 Emit 路由的 `ActionTriggered` 同步呼叫導航時，框架會回傳 `Busy` 並記錄警告。要在 `OnShown` 接續導航，先用 `Awaitable.NextFrameAsync()` 等一個影格。下例掛在 `Launch.unity` 的 `launch` 起始頁，目標頁 ID 是 `mainMenu`。 [Unity NextFrameAsync 文件](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/awaitable/nextframeasync)

```csharp
using System;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class LaunchPage : UIViewBehaviour
{
    protected override async void OnShown()
    {
        try
        {
            var root = Root;
            await Awaitable.NextFrameAsync();
            if (!this || !root || root.CurrentPage != View)
                return;

            var result = await root.ShowPageAsync(LaunchUI.Views.MainMenu, null, UIHistoryMode.Clear);
            if (result == UINavResult.Busy)
                Debug.LogWarning("主選單切換遇到另一個導航，請在它結束後再呼叫。", this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
```

| 出錯的地方 | 處理方式 |
|---|---|
| `OnShowing`、`Showing` | 第一個接收者出錯就停止，還原原畫面並回傳 `Failed`。 |
| 顯示後、隱藏前、隱藏後 | 記錄例外，同階段其餘接收者照常執行，導航回傳 `Completed`。 |
| `OnAction`、`ActionEmitted` | 記錄例外並繼續通知其他接收者，Emit 回傳 `Failed`。 |
| 工作區全域事件 | 個別記錄例外，其他訂閱者和導航結果照常。 |
| 轉場動畫 | 還原畫面並回傳 `Failed`。 |

`async void` 的非同步例外由腳本自行捕捉。框架接不到第一個 `await` 後拋出的例外。

Unity 的逐一通知方法無法取得時，框架會警告一次，並以 `UnityEvent.Invoke()` 呼叫。此時同一個 UnityEvent 會在第一個監聽丟出例外時停止，其餘階段仍依上表處理。

導航結果共有六種。

| 結果 | 意義與 Console 診斷 |
|---|---|
| `Completed` | 操作完成。個別事件的例外仍會按上述規則記錄。 |
| `Busy` | 另一個導航正在進行。從同步通知提出時會記錄警告，動畫播放期間的一般忙碌請求安靜回傳。 |
| `Canceled` | 操作取消，安靜回傳。 |
| `Unavailable` | 請求合法，目前狀態無法執行。通常會警告。沒有上一頁的返回安靜回傳。 |
| `NotFound` | 畫面 ID、Prefab 或路由設定缺失或不符合請求。Console 會記錄錯誤。 |
| `Failed` | 轉場、事件或導航執行時發生例外。Console 會記錄操作說明與例外堆疊。 |

診斷會帶呼叫的操作、相關 ID 或來源與動作，以及工作區的 Scene 名稱。

自訂 DOTween 轉場請繼承 `UIDOTweenAnimation`，在 `Build(Sequence sequence, UIView view, bool entering)` 加入 tween，掛到該 View 並選 `Custom`。播放、取消和還原 `Motion`／`CanvasGroup` 由框架管理。自訂動畫可用 `AppendCallback` 插入回呼。

## 審核

1. **Tools → UniGameUI → Blueprint** 會開啟藍圖。選節點可定位對應 Scene 頁面。用右欄編輯動作、切換種類、目標、返回歷史、關閉結果和起始頁，也能從輸出端拖線。在 Play Mode 用實際按鈕試走新增的互動。
2. **檢視全部 UI** 會把 Scene 切到 2D 並框選所有審核畫面。Popup 可以在工作區右欄疊在指定頁面上預覽，用 **重新排列** 回到網格。
3. 在 View Inspector 可預覽進場或退場。C# 可用 `UIAnimationPreview.Start(view, entering)` 與 `UIAnimationPreview.Stop()`。Validate 會核對 Scene 預覽和執行期 Prefab 上的按鈕；改名後遇到 Prefab 綁定錯誤，套用對應預覽的覆寫。若舊 View 缺少子 Canvas，按藍圖工具列的 **補上子 Canvas**，工具會將所需元件補到 Prefab 本體，其他預覽覆寫繼續由使用者審核。
4. 在互動 Editor 進 Play 前，若有待套用的覆寫，可以選 **套用並進入**、**直接進入** 或 **取消**。直接進入會使用目前已儲存的 Prefab。若多個 Scene 的審核實例同時修改同一 Prefab，先選擇要手動套用的實例。

Player 建置處理器會從複製的 Scene 中移除審核框與 Prefab 審核實例，並保留單 Scene 模式的頁面物件。打包成 AssetBundle 或 Addressables 的 Scene 會保留審核複本，執行期會停用審核層，並依藍圖使用已儲存的 Prefab。
