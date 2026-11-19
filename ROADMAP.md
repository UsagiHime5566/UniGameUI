# UniGameUI 改進計畫

- 定案日期：2026-11-20（使用者與 Claude 討論後決定）
- 目前版本：0.3.0（第二批已審核；完成「第二批修正」並複驗後才打標籤）
- 相關文件：`CODEX_REVIEW.md`（審核結果與 N、P、U、M、L 各項目的原始說明）、`AGENTS.md`（每一輪的工作規則）

這份文件記錄已定案的方向、四批工作的範圍，以及**第一批、第二批與第二批修正的詳細規格**。第三批以後只列範圍，等前一批審核完才會補上詳細規格。沒有詳細規格的批次請不要先做。

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
| 版本 | 每批交付時升一個版號。標籤等審核通過後才打。 |
| 事件出錯 | 「顯示前」出錯會取消這次導航；「顯示後」、「隱藏前」、「隱藏後」出錯只記錄，導航照常完成（2026-11-20 決定，原因見 `CODEX_REVIEW.md` 的 F1）。 |
| 通知期間的導航 | 在生命週期通知進行中提出的導航請求，維持回傳 `Busy` 並記錄警告，不延後自動執行。標準寫法是先等一個影格再導航（2026-11-20 決定，原因見 `CODEX_REVIEW.md` 的 G2）。 |

## 四批範圍

| 批次 | 主題 | 版本 | 狀態 |
|---|---|---|---|
| 第一批 | 讓 agent 寫錯馬上知道 | 0.2.0 | 已實作並通過審核；互動操作待人工確認；後續項目 F1～F6 見 `CODEX_REVIEW.md` |
| 第二批 | 遊戲程式的標準寫法 | 0.3.0 | 已實作並審核，七項都符合規格；審核發現 G1～G5（見 `CODEX_REVIEW.md`），完成本文件最後的「第二批修正」後才打標籤；互動操作待人工確認 |
| 第三批 | 元件與輸入 | 0.4.0 | 只有範圍 |
| 第四批 | 驗收工具 | 0.5.0 | 只有範圍 |

### 第二批：遊戲程式的標準寫法

詳細規格在本文件最後的「第二批規格」。範圍如下：

- 第一批的後續項目 F1～F6。
- 頁面腳本的生命週期介面：顯示前（帶資料）、顯示後、隱藏前、隱藏後，以及接收「送出事件」類的按鈕動作。`UIView` 提供取得所屬 UIRoot 的屬性。
- 用 ID 取元件，例如 `view.Get<TMP_Text>("title")`。
- 補齊 UIRoot 的 API：取得 View 實例、清空歷史、取代目前頁。
- Popup 回傳結果。
- 全域事件：任何畫面顯示或隱藏、任何按鈕動作觸發時通知一次。
- AgentGuide 新增「遊戲程式」章節和頁面腳本範本，並註明頁面不會自動釋放（`CODEX_REVIEW.md` 的 P3）。

### 第三批：元件與輸入

- 起始元件與元件集：安裝到專案的 `Assets`；JSON 計畫用名稱指定元件（例如 `"kind": "Slider"`）；專案可以自行增加元件。現有的按鈕、文字、圖片也改成走元件 Prefab。
- 工作區的預設字型。
- 多語系接口：計畫裡的文字和圖片可以寫鍵值並保留預設內容；切換語言時重新取值。
- 手把與鍵盤導覽、滑鼠懸停取消選取、滑鼠點過的按鈕不留在選取狀態、返回鍵。
- Popup 快取（P2），以及「點遮罩關閉」的選項。
- 確認框（起始元件之一）與它的標準呼叫方式，建立在第二批的 Popup 回傳結果之上。

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
| 轉場、事件監聽或頁面腳本丟出例外 | 0.2.0 回傳 `Failed`；0.3.0 起依下方 2-1 的階段規則處理 |

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

---

# 第二批規格：遊戲程式的標準寫法

完成後版本為 0.3.0。交付時升版號，但不要打標籤，等審核通過再打。共七項；2-1 會動到後面幾項的基礎，請先做。

驗收的共同條件與第一批相同：既有測試全數通過、每一項補上測試、測試在有 DOTween 的環境執行、文件與 `CHANGELOG.md` 同步更新。

下面的型別與方法名稱是建議。實作時若發現更好的名稱可以調整，但請在交付說明列出與規格不同的地方。

## 2-1. 第一批的後續項目

處理 `CODEX_REVIEW.md` 的 F1～F6，問題說明與驗收標準寫在那裡。F1 的做法已定案，完整規則如下，也適用於本批新增的介面方法與全域事件：

| 出錯的地方 | 處理方式 | 回傳 |
|---|---|---|
| 顯示前：`Showing` 事件、`OnShowing` | 取消這次導航並還原，新畫面不顯示 | `Failed` |
| 顯示後、隱藏前、隱藏後：對應的事件與介面方法 | 記錄例外，附上操作與畫面 ID；導航照常完成 | `Completed` |
| 送出事件：`ActionEmitted`、`OnAction` | 記錄例外；畫面狀態不變 | `Failed` |
| 全域事件（2-6）的訂閱者 | 記錄例外；不影響導航，其他訂閱者照常收到 | 不影響 |

- 同一個階段有多個接收者時，其中一個出錯不影響其他接收者被呼叫。「顯示前」例外：第一個出錯就停止並取消。
- 轉場動畫本身出錯（例如自訂動畫缺少元件）維持現行行為：還原並回傳 `Failed`。

## 2-2. 頁面腳本的生命週期

**目標：** 頁面邏輯有唯一的標準寫法，不必接沒有參數的 UnityEvent。

```csharp
public interface IUIViewHandler
{
    void OnShowing(UIView view, object data);
    void OnShown(UIView view);
    void OnHiding(UIView view);
    void OnHidden(UIView view);
    void OnAction(UIView view, string action);
}

[RequireComponent(typeof(UIView))]
public abstract class UIViewBehaviour : MonoBehaviour, IUIViewHandler
{
    public UIView View { get; }
    public UIRoot Root { get; }

    protected virtual void OnShowing(object data) { }
    protected virtual void OnShown() { }
    protected virtual void OnHiding() { }
    protected virtual void OnHidden() { }
    protected virtual void OnAction(string action) { }
}
```

- **標準寫法是繼承 `UIViewBehaviour`，掛在 View 的根節點。** 介面是底層機制，留給無法繼承的類別使用。`UIView` 維持 `sealed`。
- **尋找範圍：** 只找和 `UIView` 同一個 GameObject 上的元件，不找子物件。
- **每個階段的呼叫順序：** 介面接收者（依元件順序）→ View 自己的 UnityEvent → UIRoot 的全域事件。
- **`OnShowing` 的時機：** 畫面啟用之後、進場轉場開始之前，和現在的 `Showing` 事件相同。
- **返回時：** 回到上一頁會再呼叫一次 `OnShowing`，資料是該頁上次顯示時收到的那一份。
- **`OnAction`：** 只有「送出事件」（`Emit`）類的路由會呼叫，而且只送給來源畫面的接收者。之後 `UIRoot.ActionEmitted` 照舊觸發。
- **`UIView.Root`：** 回傳所屬的 UIRoot。由 UIRoot 準備畫面時設定；Edit Mode 下是 null。頁面腳本一律從這裡取得 UIRoot，不要用全域搜尋。
- **資料轉型：** `UIView` 新增 `GetData<T>()` 與 `TryGetData<T>(out T)`。`GetData<T>` 在資料是 null 或型別不符時丟出例外，訊息包含畫面 ID 與期望的型別。

**驗收**

- PlayMode 測試：切頁、返回、開關 Popup 時，四個生命週期方法依序被呼叫，資料正確；呼叫順序符合上面的規定。
- `Emit` 路由會呼叫來源畫面的 `OnAction`，其他種類的路由不會。
- `View.Root` 在單 Scene 頁面、Prefab 頁面、Popup 三種情況都正確。
- 2-1 表格裡的每一種出錯情況都有測試，包含「隱藏後出錯的 Popup 仍然關得掉」。

## 2-3. 用 ID 取元件

**目標：** 程式用 JSON 計畫裡的元件 ID 取得元件，不必在 Inspector 拖引用。

```csharp
// UIView
public T Get<T>(string elementId) where T : Component;
public bool TryGet<T>(string elementId, out T component) where T : Component;
public GameObject GetElement(string elementId);
```

- 依 `UIElementId` 尋找，包含未啟用的物件。
- 元件的尋找順序：先找該物件本身，沒有再找它的子物件。例如對按鈕的 ID 取 `TMP_Text`，會拿到按鈕底下的文字。
- `Get<T>` 與 `GetElement` 找不到時丟出例外，訊息包含畫面 ID、元件 ID、要找的型別與 Scene 名稱。`TryGet<T>` 找不到時回傳 false，不記錄。
- 第一次呼叫時建立 ID 對照表並快取。查不到或快取到的物件已被銷毀時，重新掃描一次再判定。
- `UIElementId` 改成可以從 Add Component 選單加入（`UniGameUI/UI Element Id`），讓手動建立的物件也能登記 ID。

**驗收**

- PlayMode 測試：取得元件本身的元件、取得子物件的元件、找不到時的例外訊息、動態新增元件後能取得、`TryGet` 的兩種結果。
- 單 Scene 頁面與 Popup 實例都要測。

## 2-4. UIRoot 的補充 API

```csharp
public enum UIHistoryMode { Push, Replace, Clear }

// UIRoot
public UIView CurrentPage { get; }
public UIView TopPopup { get; }
public IReadOnlyList<UIView> Popups { get; }
public int HistoryCount { get; }

public bool TryGetView(string id, out UIView view);
public void ClearHistory();
public Awaitable<UINavResult> ShowPageAsync(
    string id, object data, UIHistoryMode history, CancellationToken cancellation = default);
```

- **`TryGetView`：** 回傳目前存在的實例。頁面在建立之後就拿得到（單 Scene 模式一開始就有；Prefab 模式要顯示過一次）；Popup 只有開著的時候拿得到。
- **`UIHistoryMode`：**
  - `Push`：現行行為，離開的頁面進入歷史。
  - `Replace`：離開的頁面不進歷史，返回時回到更早的那一頁。
  - `Clear`：切頁成功後清空歷史，適合回到主選單。
  - 歷史只在切頁成功後才變動；失敗或取消時維持原狀。
- **路由也能指定：** `UIRoute` 新增歷史模式，只對 `ShowPage` 有意義。藍圖右欄與 JSON 計畫（`"history": "Replace"`）都要能設定，預設 `Push`。
- **忙碌中的請求：** 維持回傳 `Busy`、不排隊。請在 AgentGuide 寫清楚。

**驗收**

- PlayMode 測試：三種歷史模式下 `HistoryCount` 與返回的結果；切頁失敗時歷史不變；`TryGetView` 在上述各種狀態的回傳值。
- EditMode 測試：JSON 計畫的 `history` 欄位能設定與部分更新；藍圖儲存路由後保留該設定。

## 2-5. Popup 回傳結果

**目標：** 程式可以開一個 Popup，等它關閉並拿到使用者的選擇。

```csharp
public readonly struct UIPopupResult<T>
{
    public UINavResult Status { get; }
    public bool HasValue { get; }
    public T Value { get; }
    public T GetValueOrDefault(T fallback = default);
}

// UIRoot
public Awaitable<UIPopupResult<T>> OpenPopupForResultAsync<T>(
    string id, object data = null, CancellationToken cancellation = default);

// UIView
public Awaitable<UINavResult> Close(object result = null);
```

- **`OpenPopupAsync` 不變：** Popup 開啟完成就結束等待。
- **`OpenPopupForResultAsync<T>`：** 等到 Popup 完全關閉（退場轉場播完、隱藏後的通知發出）才結束等待。
- **`Status`：**
  - 開啟失敗：回傳開啟時的結果（`NotFound`、`Busy` 等），`HasValue` 為 false。
  - 正常關閉：`Completed`。
  - 等待期間 UIRoot 被停用或銷毀，或 `cancellation` 被取消：`Canceled`。取消只結束等待，不會自動關閉 Popup。
- **`HasValue`：**
  - 用 `Close(result)` 或帶有結果的關閉路由關閉，而且結果可以轉成 `T`：true。
  - 用返回、不帶結果的關閉、切頁等方式關閉：false。
  - 結果的型別和 `T` 不符：false，並用 `LogError` 指出兩個型別。
- **`UIView.Close`：** 只對「開著而且在最上層」的 Popup 有效。其他情況（頁面、不在最上層、已關閉）回傳 `Unavailable` 並警告。
- **路由也能帶結果：** `UIRoute` 新增一個字串結果，只對 `ClosePopup` 有意義。藍圖右欄與 JSON 計畫（`"result": "yes"`）都要能設定。這樣簡單的選擇視窗不需要任何腳本，呼叫端用 `OpenPopupForResultAsync<string>` 取得。
- **不能留下永遠等不到的呼叫：** 所有關閉途徑、Popup 被外部銷毀、UIRoot 停用，都要讓等待結束。
- 不是用 `OpenPopupForResultAsync` 開的 Popup，被 `Close(result)` 關閉時，結果直接丟棄，不記錄。

**驗收**

- PlayMode 測試涵蓋：用 `Close(result)` 取得結果、用帶結果的路由取得結果、返回關閉、切頁時被關閉、型別不符、開啟失敗、等待中停用 UIRoot、等待中取消 token、Popup 被外部銷毀。每一種都要確認等待有結束，以及 `Status` 與 `HasValue` 的值。
- EditMode 測試：JSON 計畫的 `result` 欄位。

## 2-6. 全域事件

**目標：** 音效、數據埋點這類橫跨所有畫面的功能，只要在一個地方訂閱。

```csharp
// UIRoot
public event Action<UIView> ViewShowing;
public event Action<UIView> ViewShown;
public event Action<UIView> ViewHiding;
public event Action<UIView> ViewHidden;
public event Action<UIRoute> ActionTriggered;
public event Action<UIRoute, UINavResult> ActionCompleted;
```

- 四個畫面事件在對應階段、View 自己的 UnityEvent 之後觸發。
- `ActionTriggered`：`TriggerAsync` 找到路由並確定可以執行時立刻觸發，在轉場開始之前。按鈕音效接這裡。找不到路由、來源不是最上層、忙碌中都不觸發。
- `ActionCompleted`：該路由執行結束時觸發，帶著結果，包含失敗與取消。
- 訂閱者出錯的處理見 2-1。
- 現有的 `ActionEmitted`（UnityEvent）保留，方便在 Inspector 接線。

**驗收：** PlayMode 測試確認各事件的觸發時機與參數；一個訂閱者丟出例外時，導航結果不變，其他訂閱者照常收到。

## 2-7. 文件

AgentGuide 新增「遊戲程式」章節，內容包括：

- 一份完整的頁面腳本範本，示範繼承 `UIViewBehaviour`、使用產生的 ID 常數、用 ID 取元件、處理送出事件、開 Popup 並等待結果。下面是示意，常數名稱以 F3 修正後的實際輸出為準：

```csharp
using TMPro;
using UniGameUI;
using UniGameUI.Generated;

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

        var answer = await Root.OpenPopupForResultAsync<string>(WorkshopUI.Views.Confirm, "確定刪除存檔？");
        if (answer.GetValueOrDefault() == "yes")
            DeleteSave();
    }

    private void DeleteSave()
    {
    }
}
```

- 各階段的呼叫順序，以及出錯時的處理規則（2-1 的表格）。
- `async void` 的方法在第一個 `await` 之後丟出的例外，框架接不到，要自己處理。
- 轉場中的請求回傳 `Busy`，不排隊。
- 頁面不會自動釋放：單 Scene 模式的頁面隨 Scene 載入，Prefab 模式的頁面顯示過就一直保留（`CODEX_REVIEW.md` 的 P3）。
- 遊戲程式放在 asmdef 裡時，要引用常數所在的組件（F2）。
- 套用 JSON 計畫後要等編譯完成（F6）。

**驗收：** 範本的程式碼要實際編譯過。請把它放進測試專案確認，不要只貼在文件裡。

---

# 第二批修正（0.3.0，打標籤之前）

第二批審核後的修正。問題說明、重現步驟與驗收標準寫在 `CODEX_REVIEW.md` 的 G1～G5。

- 版本維持 0.3.0，不升版號，也不打標籤。標籤等這一輪複驗通過後才打。
- 變更寫進 `CHANGELOG.md` 的 0.3.0 段落，不另開新段落。
- 驗收的共同條件與前兩批相同：既有測試全數通過、每一項補上測試、測試在有 DOTween 的環境執行、文件同步更新。

| 項目 | 要做的事 | 狀態 |
|---|---|---|
| G1 | 修正返回歷史，規則見下方 | 待實作（這一項沒修好不能打標籤） |
| G2 | 通知進行中被拒絕的導航請求要記錄警告，規則見下方 | 待實作（使用者已在 2026-11-20 確認採用下方做法） |
| G3 | 反射找不到方法時退回一般的 `Invoke()`；加測試確認反射路徑有效 | 待實作 |
| G4 | AgentGuide 補上五點說明 | 待實作 |
| G5 | 計畫欄位寫錯位置時報錯；不影響導航的例外改用不同的訊息文字 | 待實作 |

## G1 的規則

- 切頁成功、歷史更新之後，只要歷史最上面那一頁等於新的目前頁，就把它移除，重複到不相等或歷史為空為止。三種歷史模式與返回都適用。
- 做完之後恆成立：歷史最上面那一頁不等於目前頁。`HistoryCount` 反映的是「還能返回幾次」。
- `BackCoreAsync` 另外防守：取出的上一頁若等於目前頁，丟掉它再往下找；找不到就當成沒有上一頁，安靜地回傳 `Unavailable`。
- 任何情況下都不可以對同一個 View 同時執行隱藏和顯示。

## G2 的規則

- **不做的事：** 不把通知期間提出的請求延後自動執行。原因見 `CODEX_REVIEW.md` 的 G2。
- **通知進行中的定義：** `OnShowing`、`OnShown`、`OnHiding`、`OnHidden`、View 上對應的 UnityEvent，以及 `ViewShowing`、`ViewShown`、`ViewHiding`、`ViewHidden`、非 `Emit` 路由的 `ActionTriggered`，這些接收者正在被同步呼叫的期間。
- 這段期間提出的導航請求（`ShowPageAsync`、`OpenPopupAsync`、`OpenPopupForResultAsync`、`ClosePopupAsync`、`BackAsync`、`TriggerAsync`、`View.Close`）維持回傳 `Busy`，但要用 `LogWarning` 記錄一筆。
- 訊息包含被拒絕的請求、正在進行的導航、通知的階段與畫面 ID、Scene 名稱，並提示「請等這次導航結束後再呼叫」。
- 其他情況的 `Busy` 維持不記錄，例如轉場動畫播放中玩家連點按鈕。
- AgentGuide 寫明標準寫法並附範例：在 `OnShown` 裡先等一個影格再導航。範例要包含 `async void` 的例外處理，並且實際編譯、在 PlayMode 執行過。
