# UniGameUI 審核結果（給 codex）

- 審核日期：2026-11-19
- 審核者：Claude
- 範圍：commit `020b424` 的 `Packages/com.chrono.unigameui`
- 環境：Unity 6000.3.19f1、uGUI 2.0；驗證環境沒有安裝 DOTween

H1、H2 已由 Claude 修正，尚未 commit。請在目前工作區的版本上繼續，不要回退這兩處修改。下方「待處理」依優先序排列。標示「需使用者決定」的項目牽涉 API 設計，動手前先跟使用者確認方向。

## 已確認的 Unity 行為

以下在 Unity 6000.3.19f1 實測過，後續修改請以此為準：

- `JsonUtility.FromJson` 遇到缺漏欄位時：
  - 字串維持 `null`。
  - 陣列維持 `null`。
  - **巢狀的 `[Serializable]` 類別不會是 `null`**，而是用預設建構子建立實例，欄位初始值會執行。
- Edit Mode 中，`GetComponent<T>()` 對不存在的元件回傳真正的 `null`，所以 `GetComponent<T>() ?? AddComponent<T>()` 在 Edit Mode 正常。
- 執行期已確認可用：
  - 套用 AgentGuide 的範例計畫（含 TMP Essential Resources 自動安裝）。
  - PlayMode 的起始頁、開啟和關閉 Popup、轉場中阻擋重複請求。

## 已修正

### H1. `updateExisting` 會把人工調整蓋回預設值

**原因：** 依上面的 JsonUtility 行為，`ElementSpec.rect`、`ViewSpec.enter`／`exit` 永遠不是 `null`。所以：

- `if (created || spec.rect != null)` 永遠成立。
- `if (animation == null) return;` 永遠不成立。

**實測：** 只想改背景與按鈕文字的計畫，把進場動畫從 Slide/0.4 重設成 None/0.25，按鈕從 (111,222)、400×90 重設成 (0,0)、240×72。

**修正內容：** 檔案為 `Editor/UIPlan.cs`。

- `AnimationSpec` 的數值欄位預設改為 `NaN`，`RectSpec` 的三個 `Vector2` 預設改為 `(NaN, NaN)`。兩者都加上 `IsEmpty` 判斷。
- 版面更新改為逐欄位合併：沒指定的 anchor、position、size 維持元件目前的值；新建元件仍從預設值開始（置中、(0,0)、240×72）。
- 動畫更新改為逐欄位合併：沒指定的欄位維持 View 目前的轉場設定；整個 spec 都沒給就跳過。
- Preflight 的尺寸檢查改用 `IsNonPositive`，未指定（NaN）不算錯。
- AgentGuide 補上「省略的欄位維持目前的值」的說明與範例。

### H2. 沒有 DOTween 時，有動畫的轉場會讓導航整個失敗

**原因：** `UITransition.PlayAsync` 在 `#else` 分支丟出 `InvalidOperationException`。`UIRoot.RunAsync` 接住後回傳 `Unavailable`，並還原切頁。起始頁若設了 Fade，進 Play 就是黑畫面。

**修正內容：** `#else` 分支改為瞬間切換，並用 `Debug.LogWarning` 警告一次（`Runtime/UITransition.cs`）。

- 同步更新了 `UIValidation` 的警告文字、`UIViewInspector` 的 HelpBox、README 與 AgentGuide。

## 待處理

### M1. 藍圖沒辦法改路由名稱，改名會多一條路由

- **位置：** `Editor/UIBlueprintWindow.cs` 的「儲存路由」（約 388–393 行）。
- **問題：** 選取既有路由後修改「動作」欄再儲存，`UIAuthoring.Connect` 會用新的 `(source, action)` 建立第二條路由。舊路由還在，按鈕上的 `UIButtonAction` 也還綁著舊名稱。
- **建議：**
  - 記住目前選取路由的原始 action。名稱有變時，在同一個 Undo 群組裡移除舊路由、建立新路由。
  - 同步更新該 View 底下 `action` 等於舊名稱的 `UIButtonAction`，記得用 `UIAuthoring.RecordOverride`。
  - 新名稱若已被同一來源使用，要擋下並提示。
- **驗收：** 改名後只剩一條路由；按鈕綁定跟著改；Validate 沒有錯誤；一次 Undo 可以完整還原。

### M2. Popup（或 Prefab 模式頁面）在 Scene 的修改沒套用前，Play 看不到

- **問題：** 執行期一律從 `entry.Prefab` 建立實例，Scene 裡審核用實例的覆寫不會生效。目前只有手動按 Validate 才會看到警告。
- **建議：** 用 `EditorApplication.playModeStateChanged` 監聽 `ExitingEditMode`，只要有任何 entry 的 `UIAuthoring.HasReviewOverrides` 為真，就跳對話框，提供「套用並進入」、「直接進入」、「取消」三個選項。
- **驗收：** 有未套用的覆寫時進 Play 會先提示；選擇套用後，Popup 的外觀和 Scene 一致。

### M3. 審核用的複本會被打包進 build

- **位置：** `Runtime/UIRoot.cs:65` 只把 `reviewLayer` 停用。
- **問題：** 實測確認 Scene 會保留每個 Popup 的審核實例；Prefab 模式下連頁面也是。這些在 build 裡會跟著 Scene 一起反序列化。再加上 `entry.prefab` 是直接引用，載入 Scene 時所有 Popup Prefab 都會載入。
- **建議：**
  - 加一個 Editor 端的 `IProcessSceneWithReport`：build 時保留 SingleScene 模式的頁面（必要時先搬到 Pages 圖層），刪除其餘審核框與審核實例。
  - 或在 `UIRoot.Awake` 搬完頁面後 `Destroy(reviewLayer.gameObject)`。這招只能避免執行期常駐，無法避免載入，所以前者較好。
- **驗收：** build 出的 Scene 不含 Popup 審核實例；SingleScene 模式的頁面仍正常。

### M4. `UINavResult.Unavailable` 包了太多情況

- **位置：** `Runtime/UIRoot.cs`，`RunAsync`（約 183 行）與 `TriggerAsync`（約 155 行）。
- **問題：** 找不到 ID、已經在該頁、route 不存在、程式丟出例外，回傳的都是 `Unavailable`。遊戲端分不出是設定錯誤還是程式壞了。
- **建議：** 至少把例外拆成 `Failed`，找不到目標拆成 `NotFound`。這會改到公開 enum，請一併更新 AgentGuide。**需使用者決定**命名與要拆到多細。

### M5. 遊戲端 API 不足（需使用者決定）

以下是常見需求，目前做不到，或要繞路才能做到：

- 拿不到 View 實例。建議提供 `TryGetView(id, out UIView)` 或 `CurrentPage`／`TopPopup` 屬性。
- Popup 沒辦法回傳結果，例如確認框的是或否。可以考慮 `OpenPopupAsync<TResult>` 搭配 `UIView.Close(result)`，或另開一個等待關閉的 API。
- 沒有清空歷史或「取代目前頁」的方式。`history` 會無限增長，例如遊戲結束回主選單時應該清空。
- 轉場中的請求一律回 `Busy`，不排隊。這點至少要在文件寫清楚，或提供排隊選項。

### 低優先

- **L1（需使用者決定）：**
  - `UIRoot.Awake` 強制使用 `ScreenSpaceOverlay`，不支援 Camera 模式（例如要在 UI 上疊粒子或後處理）。
  - 沒有 Safe Area 支援。
  - 沒有 Esc／Android 返回鍵對應 `BackAsync`。
- **L2：** 藍圖視窗只在 Undo 與 Play Mode 切換時重建。切換或關閉 Scene 後仍指向舊的 `workspace`，建議監聽 `EditorSceneManager.activeSceneChangedInEditMode`、`sceneOpened`、`sceneClosed`。
- **L3：** 切頁是先播完舊頁退場，再播新頁進場，總時間是兩段相加。舊頁的 `Hidden` 要等新頁進場播完才觸發。如果要支援同時播放或交叉淡化，需要調整 `ChangePageAsync` 的還原邏輯。
- **L4：** `UIViewPose` 有記錄 `Scale`，但沒有任何預設動畫用到。建議加一個 `Zoom`／`Pop` 預設，這是 Popup 最常用的效果。
- **L5：** `UITmpResources` 自己解析 `.unitypackage`（tar.gz），實測可用但比較脆弱。在互動 Editor 中可以優先使用 TMP 自帶的匯入 API，CLI 模式才走自製解析。
- **L6：** `GetComponent<T>() ?? ...` 在 Edit Mode 正常，但對 `UnityEngine.Object` 用 `??` 會被 Rider 警告，Play Mode 下語意也不保證。建議改用 `TryGetComponent`。
- **L7：**
  - `Showed` 文法上應為 `Shown`。這是公開 API，改名前請先確認。
  - 套件沒有附自動化測試。建議至少加上 `UIPlan` 部分更新的 EditMode 測試，以及導航狀態機的 PlayMode 測試。

## 未驗證（需安裝 DOTween）

- DOTween 動畫的播放、取消與 `UIViewPose` 還原。
- **疑慮：** UIRoot 在轉場中被停用時，`OnDisable` 會 `Cancel`，`AwaitableCompletionSource` 的接續可能同步執行。接下來 `ChangePageAsync` 的 catch 會在父物件停用的過程中呼叫子物件的 `SetActive`，可能觸發 Unity 的 “GameObject is already being activated or deactivated” 錯誤。請在裝了 DOTween 的環境寫 PlayMode 測試確認。

## 其他注意

- 工作區有兩個未 commit 的 `ProjectSettings` 變更，不是這個框架產生的，應該是其他 Unity 套件在 Editor 開啟時寫入的。commit 框架時不要混進去。
  - 新增了 `APP_UI_EDITOR_ONLY` define。
  - `EditorBuildSettings` 多了 `com.unity.dt.app-ui` 設定物件。

## 驗證紀錄（H1、H2 修正後）

在暫存的 Unity 6000.3.19f1 專案中嵌入修改後的套件（沒有 DOTween）。編譯沒有錯誤。

| 測試 | 結果 |
|---|---|
| 全新套用 AgentGuide 範例計畫 | 成功，2 個 View、2 條路由 |
| 先人工設定進場 Slide/0.4/Left/OutBack/300、按鈕 (111,222)、400×90，再套用只改背景與文字的 `updateExisting` 計畫 | 動畫與版面都保留，背景與文字已更新 |
| 只給 `"enter": { "duration": 0.9 }` 與 `"rect": { "size": { "x": 500 } }` | 只有 duration 變成 0.9、寬度變成 500，高度維持 90，其他不變 |
| 新增未指定 `rect` 的元件 | 置中、(0,0)、240×72，與修正前相同 |
| PlayMode：起始頁設 Slide、Popup 退場設 Fade，無 DOTween | 起始頁正常顯示，Popup 能開能關，只警告一次 |

注意：`Vector2` 只給一個分量（例如只給 `x`）時，JsonUtility 會讓另一個分量維持 NaN，由合併邏輯保留目前的值。這也是實測確認的。

驗證方法：在暫存區建一個最小專案，`Packages/manifest.json` 只放 ugui 2.0.0、test-framework 1.6.0，以及 jsonserialize、uielements、ui、imgui 模組；把套件複製到 `Packages/` 後依序執行：

1. 用 `-batchmode -nographics -executeMethod <探測方法> -quit` 跑 Editor 端檢查。
2. 用 `-runTests -testPlatform PlayMode` 跑導航測試。

使用者的 Editor 開著專案時，CLI 拿不到專案鎖，所以一律在副本上驗證。
