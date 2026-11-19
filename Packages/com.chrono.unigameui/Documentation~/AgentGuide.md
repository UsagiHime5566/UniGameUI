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
- `pageTransitionMode` 可用 `Sequential` 或 `Concurrent`。新工作區預設依序播放。要讓切頁的進場和退場重疊，設定 `Concurrent` 或在 `UIRoot` Inspector 修改「切頁動畫」。
- 元件支援 `Text`、`Button`、`Image`。矩形座標是 Unity RectTransform 的 anchor 與參考解析度下的座標，Y 向上。可指定 `fontSize`、`fontPath`（TMP Font Asset）、`textColor`、`color`、`spritePath`。首次建立文字時，工具會安裝 uGUI 附帶的 TMP Essential Resources。若專案曾匯入部分資源，先透過 **Window → TextMeshPro → Import TMP Essential Resources** 完成安裝。使用中文或其他語系時，交付前確認字型涵蓋所需字符。
- `enter`、`exit` 可各自選 `None`、`Fade`、`Slide`、`RotateFade`、`Zoom` 或 `Custom`。例如 `"enter": { "preset": "Zoom", "duration": 0.2, "zoomScale": 0.8, "ease": "OutBack" }`。Zoom 倍率相對於原本的 Motion 尺寸，預設為 0.85。Slide 可設定方向 `Left`、`Right`、`Up`、`Down` 和 `distance`，RotateFade 可設定 `angle`。Ease 有 `Linear`、`InQuad`、`OutQuad`、`InOutQuad`、`OutBack`。
- 既有頁面可直接新增其他 ID 的元件。要修改既有元件時，指定該元件 ID、原 `kind`、`updateExisting: true` 與要寫入的欄位。若要更新既有 View 的背景或動畫，在 View 設 `updateExisting: true`。要改既有路由在線上加 `replace: true`。平常省略這些選項，重跑可保留人工編修。
- 更新時只寫入有提供的欄位。`rect` 的 `anchor`、`position`、`size` 與 `enter`／`exit` 的各欄位若省略，維持目前的值；例如只給 `"rect": { "size": { "x": 320, "y": 80 } }` 不會移動元件，只給 `"enter": { "duration": 0.4 }` 不會改動畫種類。
- 新建立的 Prefab 會存入本次生成的內容。既有 Prefab 的調整會出現在 Scene 預覽並標示待套用，由使用者用 **套用到 Prefab** 審核後提交。先從藍圖查看待套用狀態。

## ID 常數

遊戲程式一律使用產生的常數。場景存檔、成功套用 JSON 或在藍圖按 **Validate** 時，工具會產生或更新 `Assets/UniGameUI/Generated/<場景名稱>UI.cs`。首次使用未命名的場景時，先存成 Scene 資產。也可選 **Tools → UniGameUI → Generate ID Constants**，或呼叫 `UIIdConstants.Generate(root)`。

```csharp
using UniGameUI.Generated;

var result = await root.TriggerAsync(WorkshopUI.Views.Workshop, WorkshopUI.Actions.OpenArchive);
```

`Views` 含頁面與 Popup ID，`Actions` 含路由與按鈕使用的動作，`Elements` 依畫面分組，例如 `WorkshopUI.Elements.Workshop.OpenArchive`。避免轉成相同 PascalCase 名稱的 ID 或工作區 Scene 名稱。程式呼叫 `UIValidation.Check(root)` 只讀取目前配置，名稱衝突會報錯，檔案不在或過期會警告。

## 公開 C# API

- `UIAuthoring.FindWorkspace(scene)`、`CreateWorkspace(scene, mode)`、`ConfigureReview(root, resolution, columns)`。
- `UIAuthoring.CreatePage(root, id, title, prefabPath)`、`CreatePopup(root, id, title, prefabPath)` 回傳 Scene 中的 `UIView`。將自訂 uGUI 階層加到 `view.Content`。保留外框的排列用途；`view.Motion` 是預設動畫作用的位置。
- `UIAuthoring.FindEntry(root, id)`、`UIPlan.FindElement(view, id)`、`BindButton(button, action)`、`Connect(root, source, action, kind, target)`、`Disconnect(root, source, action)`、`SetStartPage(root, id)`。
- `UIAuthoring.UpdateRoute(root, source, originalAction, action, kind, target)` 用於更新既有路由。改名時一併更新來源 View 的按鈕綁定，並合併為一次 Undo。若來源是 Prefab 審核實例，將覆寫套用後 Prefab 才會使用新動作名稱。
- `UIAuthoring.SetTransition(view, entering, preset, duration, direction, ease, distance, angle, zoomScale)` 和 `SetPageTransitionMode(root, mode)`。
- `UIAuthoring.ArrangeReview(root)`、`FocusReview(root, id)`、`PreviewPopupOnPage(root, popupId, pageId)`、`HasReviewOverrides(entry)`、`ApplyReviewOverrides(root, id)`。最後一項適合提交本次新建立的 Prefab。既有實例的覆寫先交給使用者審核。
- `UIValidation.Check(root)` 回傳結構化錯誤與警告。對於自訂子階層，用 Unity Editor 的 Undo 和序列化 API 儲存變更。

## Play 與事件

用藍圖與實際按鈕試走每條新增的互動。首頁切到下一頁後，返回會取回前一頁的實例與 UI 狀態。若有 Popup，先關頂層。轉場中收到的導覽請求會回報 `Busy`。

遊戲程式可呼叫 `await root.ShowPageAsync(id, data)`、`await root.OpenPopupAsync(id, data)`、`await root.ClosePopupAsync()` 或 `await root.BackAsync()`。每個回傳的 Unity `Awaitable` 只等待一次。資料可在 `UIView.Showing` 觸發後透過 `view.Data` 取得。

導航結果共有六種。

| 結果 | 意義與 Console 診斷 |
|---|---|
| `Completed` | 操作完成，安靜回傳。 |
| `Busy` | 另一個轉場正在進行，安靜回傳。 |
| `Canceled` | 操作取消，安靜回傳。 |
| `Unavailable` | 請求合法，目前狀態無法執行。通常會警告。沒有上一頁的返回安靜回傳。 |
| `NotFound` | 畫面 ID、Prefab 或路由設定缺失或不符合請求。Console 會記錄錯誤。 |
| `Failed` | 轉場、事件或導航執行時發生例外。Console 會記錄操作說明與例外堆疊。 |

診斷會帶呼叫的操作、相關 ID 或來源與動作，以及工作區的 Scene 名稱。

在 View Inspector 可接 `Showing`、`Shown`、`Hiding`、`Hidden`。頁面切換成功時，舊頁 `Hiding`、新頁 `Showing`、舊頁 `Hidden`、新頁 `Shown` 依序觸發。`Hidden` 與 `Shown` 在兩段動畫都完成後確認切頁結果，動畫取消時可還原舊頁。藍圖的 `Emit` 會從 `UIRoot.ActionEmitted` 發出來源 ID 和動作名稱。程式端用 `root.ActionEmitted.AddListener(OnAction)` 訂閱，其中 `OnAction` 接收兩個 `string`，在停用時用 `RemoveListener` 移除。

自訂 DOTween 轉場請繼承 `UIDOTweenAnimation`，在 `Build(Sequence sequence, UIView view, bool entering)` 加入 tween，掛到該 View 並選 `Custom`。播放、取消和還原 `Motion`／`CanvasGroup` 由框架管理。自訂動畫可用 `AppendCallback` 插入回呼。

## 審核

1. **Tools → UniGameUI → Blueprint** 會開啟藍圖。選節點可定位對應 Scene 頁面。用右欄編輯動作、切換種類、目標和起始頁，也能從輸出端拖線。
2. **檢視全部 UI** 會把 Scene 切到 2D 並框選所有審核畫面。Popup 可以在工作區右欄疊在指定頁面上預覽，用 **重新排列** 回到網格。
3. 在 View Inspector 可預覽進場或退場。C# 可用 `UIAnimationPreview.Start(view, entering)` 與 `UIAnimationPreview.Stop()`。Validate 會核對 Scene 預覽和執行期 Prefab 上的按鈕；改名後遇到 Prefab 綁定錯誤，套用對應預覽的覆寫。若舊 View 缺少子 Canvas，按藍圖工具列的 **補上子 Canvas**，工具會將所需元件補到 Prefab 本體，其他預覽覆寫繼續由使用者審核。
4. 在互動 Editor 進 Play 前，若有待套用的覆寫，可以選 **套用並進入**、**直接進入** 或 **取消**。直接進入會使用目前已儲存的 Prefab。若多個 Scene 的審核實例同時修改同一 Prefab，先選擇要手動套用的實例。

Player 建置處理器會從複製的 Scene 中移除審核框與 Prefab 審核實例，並保留單 Scene 模式的頁面物件。打包成 AssetBundle 或 Addressables 的 Scene 會保留審核複本，執行期會停用審核層，並依藍圖使用已儲存的 Prefab。
