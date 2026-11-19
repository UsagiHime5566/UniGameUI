# UniGameUI 操作指南

從這份文件的公開工具操作 Unity。交付時指出實際使用的 Scene、藍圖路徑、Play 驗證結果，以及需要專案自行接上的事件。

## 確認專案

1. 確認 Unity 6.3+、uGUI 2.0，並找出使用者要編輯的 Scene。套件匯入後沒有預建工作區或 UI。
2. 預設採用 `SingleScene`。頁面、參考解析度、節點位置與路由都存於 Scene 的 UniGameUI 工作區。所有 Popup 都是 Prefab。若使用者要共用頁面資產，建立工作區時可以明確指定 `Prefab` 模式。
3. 已存在的工作區沿用原模式與穩定 ID。記錄場景是否有尚未存檔的修改，並查看 `UIAuthoring.HasReviewOverrides(entry)`。有開啟中的 Editor 時，優先在其 Edit Mode 使用公開 C# API 或 **Apply JSON Plan…**。CLI 在該專案已由另一個 Editor 開啟時無法取得專案鎖，使用隔離副本測試。
4. 選用動畫前確認 `UITransition.IsDOTweenReady`。導入官方 DOTween 標準版並完成 **Setup DOTween**，等待編譯。尚未安裝 DOTween 時，所有轉場都會直接切換並在 Console 警告一次，導航仍可試跑；驗證工具也會回報這些動畫。

## 入口

- 開啟目標 Scene 後，**Tools → UniGameUI → Apply JSON Plan…** 可套用計畫。計畫可以省略 `scenePath` 以指定目前作用中的 Scene，然後依一般 Unity 流程存檔。
- CLI 用 `-executeMethod UniGameUI.Editor.UIPlan.RunFromCommandLine -unigameui-plan <JSON 絕對路徑>`。指定 `-batchmode -quit -projectPath <專案絕對路徑> -logFile <記錄檔絕對路徑>`。CLI 模式需要明確的 `scenePath`，成功後存入指定 Scene。
- 已有 Unity Editor C# 執行能力時，呼叫 `UniGameUI.Editor.UIPlan.Apply(json)` 或下方公開 C# API。先檢查目前目標和已存在物件。GUI 與 JSON 建立的頁面與路由都使用同一份 Scene 資料。

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
- 元件支援 `Text`、`Button`、`Image`。矩形座標是 Unity RectTransform 的 anchor 與參考解析度下的座標，Y 向上。可指定 `fontSize`、`fontPath`（TMP Font Asset）、`textColor`、`color`、`spritePath`。首次建立文字時，工具會安裝 uGUI 附帶的 TMP Essential Resources。若專案曾匯入部分資源，先透過 **Window → TextMeshPro → Import TMP Essential Resources** 完成安裝。使用中文或其他語系時，交付前確認字型涵蓋所需字符。
- `enter`、`exit` 可各自選 `None`、`Fade`、`Slide`、`RotateFade`、`Custom`，例如 `"enter": { "preset": "Slide", "duration": 0.2, "direction": "Left", "distance": 180, "ease": "OutQuad" }`。方向有 `Left`、`Right`、`Up`、`Down`。另可設定 `angle`，以及 `Linear`、`InQuad`、`OutQuad`、`InOutQuad`、`OutBack`。
- 既有頁面可直接新增其他 ID 的元件。要修改既有元件時，指定該元件 ID、原 `kind`、`updateExisting: true` 與要寫入的欄位。若要更新既有 View 的背景或動畫，在 View 設 `updateExisting: true`。要改既有路由在線上加 `replace: true`。平常省略這些選項，重跑可保留人工編修。
- 更新時只寫入有提供的欄位。`rect` 的 `anchor`、`position`、`size` 與 `enter`／`exit` 的各欄位若省略，維持目前的值；例如只給 `"rect": { "size": { "x": 320, "y": 80 } }` 不會移動元件，只給 `"enter": { "duration": 0.4 }` 不會改動畫種類。
- 新建立的 Prefab 會存入本次生成的內容。既有 Prefab 的調整會出現在 Scene 預覽並標示待套用，由使用者用 **套用到 Prefab** 審核後提交。先從藍圖查看待套用狀態。

## 公開 C# API

- `UIAuthoring.FindWorkspace(scene)`、`CreateWorkspace(scene, mode)`、`ConfigureReview(root, resolution, columns)`。
- `UIAuthoring.CreatePage(root, id, title, prefabPath)`、`CreatePopup(root, id, title, prefabPath)` 回傳 Scene 中的 `UIView`。將自訂 uGUI 階層加到 `view.Content`。保留外框的排列用途；`view.Motion` 是預設動畫作用的位置。
- `UIAuthoring.FindEntry(root, id)`、`UIPlan.FindElement(view, id)`、`BindButton(button, action)`、`Connect(root, source, action, kind, target)`、`Disconnect(root, source, action)`、`SetStartPage(root, id)`。
- `UIAuthoring.SetTransition(view, entering, preset, duration, direction, ease, distance, angle)`。
- `UIAuthoring.ArrangeReview(root)`、`FocusReview(root, id)`、`PreviewPopupOnPage(root, popupId, pageId)`、`HasReviewOverrides(entry)`、`ApplyReviewOverrides(root, id)`。最後一項適合提交本次新建立的 Prefab。既有實例的覆寫先交給使用者審核。
- `UIValidation.Check(root)` 回傳結構化錯誤與警告。對於自訂子階層，用 Unity Editor 的 Undo 和序列化 API 儲存變更。

## Play 與事件

用藍圖與實際按鈕試走每條新增的互動。首頁切到下一頁後，返回會取回前一頁的實例與 UI 狀態。若有 Popup，先關頂層。轉場中收到的導覽請求會回報 `Busy`。

遊戲程式可呼叫 `await root.ShowPageAsync(id, data)`、`await root.OpenPopupAsync(id, data)`、`await root.ClosePopupAsync()` 或 `await root.BackAsync()`。每個回傳的 Unity `Awaitable` 只等待一次。資料可在 `UIView.Showing` 觸發後透過 `view.Data` 取得。

在 View Inspector 可接 `Showing`、`Showed`、`Hiding`、`Hidden`。頁面切換成功時，舊頁 `Hiding`、新頁 `Showing`、舊頁 `Hidden`、新頁 `Showed` 依序觸發。動畫取消會還原準備中的切頁。藍圖的 `Emit` 會從 `UIRoot.ActionEmitted` 發出來源 ID 和動作名稱。程式端用 `root.ActionEmitted.AddListener(OnAction)` 訂閱，其中 `OnAction` 接收兩個 `string`；在停用時用 `RemoveListener` 移除。

自訂 DOTween 轉場請繼承 `UIDOTweenAnimation`，在 `Build(Sequence sequence, UIView view, bool entering)` 加入 tween，掛到該 View 並選 `Custom`。播放、取消和還原 `Motion`／`CanvasGroup` 由框架管理。自訂動畫可用 `AppendCallback` 插入回呼。

## 審核

1. **Tools → UniGameUI → Blueprint** 會開啟藍圖。選節點可定位對應 Scene 頁面。用右欄編輯動作、切換種類、目標和起始頁，也能從輸出端拖線。
2. **檢視全部 UI** 會把 Scene 切到 2D 並框選所有審核畫面。Popup 可以在工作區右欄疊在指定頁面上預覽，用 **重新排列** 回到網格。
3. 在 View Inspector 可預覽進場或退場。C# 可用 `UIAnimationPreview.Start(view, entering)` 與 `UIAnimationPreview.Stop()`。確認 `Validate` 的錯誤已修正，檢查待套用 Prefab 覆寫，再以 Play Mode 試走實際流程。
