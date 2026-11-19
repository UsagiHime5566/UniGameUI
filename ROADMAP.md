# UniGameUI 改進計畫

- 定案日期：2026-11-20（使用者與 Claude 討論後決定）
- 目前版本：0.1.0（commit `0cabfec`）
- 相關文件：`CODEX_REVIEW.md`（審核結果與 N、P、U、M、L 各項目的原始說明）、`AGENTS.md`（每一輪的工作規則）

這份文件記錄已定案的方向、四批工作的範圍，以及**第一批的詳細規格**。第二批以後只列範圍，等前一批審核完才會補上詳細規格。沒有詳細規格的批次請不要先做。

## 使用情境

- 使用者之後的中小型專案都會用這個框架。
- 目標平台以 PC 為主。
- 框架之上的遊戲程式由 agent 撰寫，所以優先順序是：寫錯要馬上知道、agent 能自己驗收、每件事只有一種標準寫法。
- 目前沒有任何專案在使用，公開 API 可以直接修改，不必保留相容層。

## 已定案的決定

| 主題 | 決定 |
|---|---|
| DOTween | 必裝。沒裝時跳提示，而且 Validate 列為錯誤。執行期保留「瞬間切換」當保險。 |
| `#if DOTWEEN` | 保留。套件必須在沒有 DOTween 時也能編譯，才有辦法跳提示。 |
| DOTween 與版控 | DOTween 不放進這個 repo。 |
| 內建補間 | 不做。 |
| 輸入 | 只支援新的 Input System 套件。 |
| 手把與鍵盤 | 要支援。以最後使用的輸入裝置為準；滑鼠移到任何元件上就取消選取。 |
| 返回鍵 | Esc 與手把取消鍵對應返回。沒有上一頁時不做事，只發出事件。個別畫面可以關閉。 |
| 多語系 | 只開接口，文字和圖片都要能用鍵值。實際翻譯由專案自己的多語系系統提供。 |
| 起始元件 | 要附。安裝時複製到專案的 `Assets`，各專案自行修改美術。畫面上的元件都是這些 Prefab 的實例。 |
| 起始元件清單 | 按鈕、文字、圖片／面板、滑桿、開關、下拉選單、輸入框、捲動清單、進度條、確認框。 |
| Popup 遮罩 | 點遮罩預設不關閉，個別 Popup 可以開啟。 |
| ID 常數 | 自動產生到專案的 `Assets` 底下。 |
| 頁面腳本 | 用介面接收生命週期，`UIView` 維持 `sealed`。 |
| 跨 Scene | 現在不做，之後做成可選功能。頁面腳本一律從自己的 View 取得 UIRoot，不做全域單例。 |
| 自動走訪 | 要做。「送出事件」類的路由預設跳過。 |
| 發佈 | GitHub repo，其他專案用 git URL 加 `?path=/Packages/com.chrono.unigameui` 安裝，用版本標籤鎖定。repo 尚未建立。 |
| 版本 | 每批完成後升一個版號並打標籤。 |

## 四批範圍

| 批次 | 主題 | 版本 | 狀態 |
|---|---|---|---|
| 第一批 | 讓 agent 寫錯馬上知道 | 0.2.0 | 規格已寫，待實作 |
| 第二批 | 遊戲程式的標準寫法 | 0.3.0 | 只有範圍 |
| 第三批 | 元件與輸入 | 0.4.0 | 只有範圍 |
| 第四批 | 驗收工具 | 0.5.0 | 只有範圍 |

### 第二批：遊戲程式的標準寫法

- 頁面腳本的生命週期介面：顯示前（帶資料）、顯示後、隱藏前、隱藏後，以及接收「送出事件」類的按鈕動作。`UIView` 提供取得所屬 UIRoot 的屬性。
- 用 ID 取元件，例如 `view.Get<TMP_Text>("title")`。
- 補齊 UIRoot 的 API：取得 View 實例、清空歷史、取代目前頁。
- Popup 回傳結果，以及確認框的呼叫方式。
- 全域事件：任何畫面顯示或隱藏、任何按鈕動作觸發時通知一次。
- AgentGuide 新增「遊戲程式」章節和頁面腳本範本，並註明頁面不會自動釋放（`CODEX_REVIEW.md` 的 P3）。

### 第三批：元件與輸入

- 起始元件與元件集：安裝到專案的 `Assets`；JSON 計畫用名稱指定元件（例如 `"kind": "Slider"`）；專案可以自行增加元件。現有的按鈕、文字、圖片也改成走元件 Prefab。
- 工作區的預設字型。
- 多語系接口：計畫裡的文字和圖片可以寫鍵值並保留預設內容；切換語言時重新取值。
- 手把與鍵盤導覽、滑鼠懸停取消選取、滑鼠點過的按鈕不留在選取狀態、返回鍵。
- Popup 快取（P2），以及「點遮罩關閉」的選項。

### 第四批：驗收工具

- Validate 加強：走不到的畫面、沒有出口的畫面、沒綁動作的按鈕等，並提供命令列入口與結束代碼。
- 自動走訪：依藍圖從起始頁把每條路由走一遍並輸出報告，提供藍圖視窗按鈕與命令列入口。

### 先不做

更多層級（HUD、提示訊息、載入遮罩）、跨 Scene 常駐、走訪時截圖、Overlay 以外的顯示模式、Safe Area、滑鼠懸停提示、資料綁定、Addressables、清單虛擬化。

---

# 第一批規格：讓 agent 寫錯馬上知道

完成後版本為 0.2.0。共六項，可以依序做；1-2 會影響測試環境，建議先做。

驗收的共同條件：

- 既有測試全數通過，每一項都要補上對應的測試。
- 測試在「有 DOTween」的環境執行。沒有 DOTween 時，測試要清楚地略過並說明原因，不要出現一堆不相干的失敗。
- `README.md`、`Documentation~/AgentGuide.md` 跟著更新；在套件根目錄新增 `CHANGELOG.md`，`package.json` 的版本改為 `0.2.0`。

## 1-1. 第二輪審核的小問題

處理 `CODEX_REVIEW.md` 的 N1～N5，做法與驗收標準都寫在那裡。

## 1-2. DOTween 必裝

**目標：** 沒有 DOTween 時，使用者會看到提示，agent 會拿到明確的錯誤。

**狀態判斷**

分成三種：

- 已就緒：有 `DOTWEEN` define。
- 已匯入但 define 不存在：找得到 `DG.Tweening.DOTween` 型別或 `DOTween.dll`。
- 未安裝。

**提示**

- 新增一個 Editor 類別（建議 `Editor/UIDOTweenSetup.cs`），在 Editor 載入時檢查。
- 批次模式不提示；每個 Editor 工作階段只自動跳一次；Play Mode 中不跳。
- 未安裝時的對話框提供三個選擇：
  - 「從本機匯入」：只有在本機 Asset Store 快取找到 DOTween 的 `.unitypackage` 時才顯示。
  - 「開啟 Asset Store 頁面」。
  - 「稍後」。
- 已匯入但 define 不存在時，引導使用者開啟 DOTween Utility Panel 完成 Setup。
- 新增選單 `Tools/UniGameUI/Install DOTween…`，可以隨時再開啟提示。
- 藍圖視窗在未就緒時，於最上方顯示同樣內容的提示列。

**本機快取**

- Asset Store 快取位置依平台不同：Windows 在 `%APPDATA%\Unity\Asset Store-5.x`，macOS 在 `~/Library/Unity/Asset Store-5.x`，Linux 在 `~/.local/share/unity3d/Asset Store-5.x`。
- 請在 `Demigiant` 資料夾底下用檔名搜尋，不要寫死分類資料夾的名稱。
- 已實測：在批次模式用 Unity 的 `-importPackage <路徑> -quit` 匯入這個檔案是可行的，匯入後 `DOTWEEN` define 會自動加到各平台。
- 未實測：在互動 Editor 中用 `AssetDatabase.ImportPackage` 匯入後的行為（DOTween 可能會自己跳出設定視窗）。請實際確認。
- Asset Store 頁面的網址請自行確認有效。

**給 agent 的入口**

- 公開 API：是否已就緒、尋找本機套件路徑、從本機匯入。
- AgentGuide 寫明命令列的做法：先取得本機套件路徑，再用 `-importPackage` 匯入。

**Validate**

- 未就緒時，`UIValidation.Check` 回報一個工作區層級的 Error，訊息要說明怎麼安裝。取代現在每個 View 各一個的警告。
- 結果是 `UIPlan.Apply` 在沒有 DOTween 時會失敗並帶出這個訊息。這是預期行為。

**執行期**

- `UITransition.PlayAsync` 在沒有 DOTween 時的瞬間切換與一次性警告維持不變。

**版控**

- DOTween 不進這個 repo。請在 `.gitignore` 排除 DOTween 匯入與 Setup 產生的檔案（`Assets/Plugins/Demigiant` 及其 `.meta`、`DOTweenSettings.asset` 等，以實際產生的檔案為準）。
- **`ProjectSettings/ProjectSettings.asset` 裡的 `DOTWEEN` define 不可以 commit。** 這個檔案有納入版控；如果 define 被 commit，但 DOTween 不在 repo 裡，別台電腦開專案時套件會整個編譯失敗，連提示都跳不出來。

**驗收**

- 沒有 DOTween：`UIValidation.Check` 有上述 Error；`UIPlan.Apply` 丟出包含安裝說明的例外。
- 有 DOTween：沒有這個 Error。
- 尋找本機套件的邏輯有測試（用暫存資料夾模擬快取結構）。
- 對話框本身無法自動測試，請在交付說明中列為「需使用者手動確認」。

## 1-3. 導航失敗要說原因

**目標：** agent 看 Console 就知道哪個 ID 或哪條路由出錯。

**`UINavResult` 改為六種**

| 值 | 意義 | Console |
|---|---|---|
| `Completed` | 完成 | 不記錄 |
| `Busy` | 轉場進行中 | 不記錄 |
| `Canceled` | 被取消 | 不記錄 |
| `Unavailable` | 請求合法，但目前狀態下不能執行 | `LogWarning`，見下方例外 |
| `NotFound` | 設定裡找不到，屬於設定錯誤 | `LogError` |
| `Failed` | 執行時丟出例外 | `LogException` 加上一行說明 |

**現有情況的對應**

| 情況 | 結果 |
|---|---|
| ID 是空字串、頁面或 Popup 的 ID 不存在 | `NotFound` |
| ID 存在但種類不對（對 Popup 呼叫 `ShowPageAsync`，或反過來） | `NotFound` |
| Popup 的 entry 沒有 Prefab | `NotFound` |
| `TriggerAsync` 找不到 `(source, action)` 對應的路由 | `NotFound` |
| 路由的種類無法識別 | `Failed` |
| 已經在該頁 | `Unavailable` |
| 該 Popup 已經開著 | `Unavailable` |
| 沒有 Popup 可關 | `Unavailable` |
| `TriggerAsync` 的來源不是目前最上層的畫面 | `Unavailable` |
| UIRoot 未啟用或尚未就緒 | `Unavailable` |
| 沒有上一頁可返回 | `Unavailable`，**不記錄**（第三批的返回鍵會讓這變成常態） |
| 轉場、事件監聽或頁面腳本丟出例外 | `Failed` |

**訊息內容**

- 每一則都要包含：呼叫的操作、相關的 ID 或 `(source, action)`、UIRoot 所在的 Scene 名稱。
- 例：`UniGameUI: ShowPage 找不到頁面 'setings'（Scene: Workshop）。`
- `Debug` 呼叫要帶上 UIRoot 當 context，點訊息可以定位物件。

**順便修正**

- `UIRoot.Awake` 的設定錯誤訊息要指出是哪個 ID、哪條路由重複或缺漏。現在只說「contains a missing or duplicate view ID」。
- `UIButtonAction` 找不到上層的 `UIView` 或 `UIRoot` 時，現在是安靜地不做事，改為 `LogError`。

**驗收**

- 上表每一種情況都有 PlayMode 測試，檢查回傳值，並用 `LogAssert.Expect` 檢查訊息包含對應的 ID。
- AgentGuide 列出六種結果的意義。

## 1-4. 自動產生 ID 常數

**目標：** 打錯 ID 變成編譯錯誤。

**產生的內容**

- 每個工作區一個檔案，位置 `Assets/UniGameUI/Generated/<類別名稱>.cs`。
- 類別名稱是 Scene 名稱轉成 PascalCase 再加 `UI`，例如 `Workshop.unity` 產生 `WorkshopUI`。
- 命名空間 `UniGameUI.Generated`。
- 檔案開頭要有自動產生的標記與「不要手動修改」的說明。

```csharp
namespace UniGameUI.Generated
{
    public static class WorkshopUI
    {
        public static class Views
        {
            public const string Workshop = "workshop";
            public const string Archive = "archive";
        }

        public static class Actions
        {
            public const string OpenArchive = "openArchive";
            public const string CloseArchive = "closeArchive";
        }

        public static class Elements
        {
            public static class Workshop
            {
                public const string OpenArchive = "openArchive";
            }
        }
    }
}
```

- `Views`：所有頁面與 Popup 的 ID。
- `Actions`：整個工作區裡路由與 `UIButtonAction` 用到的動作名稱，取聯集。
- `Elements`：依 View 分組，因為元件 ID 只在同一個 View 內唯一。使用 Prefab 的 View 從審核實例讀取。

**命名規則**

- ID 允許字母、數字、底線、連字號。轉成常數名稱時以底線與連字號為分隔轉成 PascalCase；開頭是數字時前面加底線。
- 兩個不同的 ID 轉出相同的常數名稱時（例如 `open_archive` 與 `open-archive`），`UIValidation.Check` 回報 Error。
- 不同資料夾有同名 Scene、導致類別名稱相同時，同樣回報 Error。

**什麼時候產生**

- Scene 存檔時（該 Scene 有工作區）。
- `UIPlan.Apply` 成功後。
- 藍圖視窗按 Validate 時。
- 選單 `Tools/UniGameUI/Generate ID Constants`，以及一個公開 API。
- 內容沒變就不寫檔，避免無謂的重新編譯。
- `UIValidation.Check` 本身維持唯讀，只在檔案過期或不存在時回報 Warning。

**驗收**

- EditMode 測試：建立工作區後產生的檔案內容符合預期；增刪 View 後重新產生會反映變更；內容沒變時檔案的修改時間不變；名稱衝突會報錯。
- 測試結束要清掉產生的檔案。
- AgentGuide 寫明：遊戲程式一律使用常數，不要直接寫字串。

## 1-5. 每個 View 一個子 Canvas

背景說明見 `CODEX_REVIEW.md` 的 P1。

**做法**

- `UIAuthoring.CreateView` 在 View 根節點加上 `Canvas` 與 `GraphicRaycaster`。巢狀 Canvas 需要自己的 Raycaster 才收得到點擊。
- 不要開啟 `overrideSorting`，繪製順序維持由階層決定。
- `CanvasGroup` 留在同一個物件上。
- 既有工作區：`UIValidation.Check` 對缺少子 Canvas 的 View 回報 Warning，藍圖視窗提供一鍵補上。使用 Prefab 的 View 要處理好覆寫。

**風險：TMP 可能造成假的 Prefab 覆寫**

- TextMeshPro 會自動在它所屬的 Canvas 上開啟需要的 `additionalShaderChannels`。加了子 Canvas 之後，這個修改會落在 View 自己的 Canvas 上；如果 View 是 Prefab 實例，可能被判定為待套用的覆寫。
- 這點我沒有實測。建議建立子 Canvas 時就把 TMP 需要的 channel 設好，並用測試確認。

**驗收**

- 新建的 View 根節點有 `Canvas` 與 `GraphicRaycaster`。
- EditMode 測試：用計畫新建一個含文字的 Popup 後，`UIAuthoring.HasReviewOverrides` 為 false。
- PlayMode 測試：Popup 開啟時，透過 EventSystem 的 raycast 可以點到 Popup 的按鈕，點不到被蓋住的頁面按鈕。如果批次模式下無法做 raycast，請說明原因並改用可行的檢查。
- 既有測試全數通過，包含實際打包後的行為不變（`UIReviewBuildProcessor` 的測試）。

## 1-6. `Showed` 改名為 `Shown`

- `UIView` 的欄位、屬性、Inspector 標籤、文件、測試全部改名。
- 序列化欄位加上 `[FormerlySerializedAs("showed")]`，避免已接好的 UnityEvent 遺失。
- 不保留舊名稱的屬性。

**驗收：** 套件內搜尋不到 `Showed`（`FormerlySerializedAs` 的字串除外）；測試通過。
