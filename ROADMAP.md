# UniGameUI 改進計畫

- 定案日期：2026-11-20（使用者與 Claude 討論後決定）
- 目前版本：0.4.0。三批都已通過審核：0.3.0（第二批與第二批修正）可以打 `v0.3.0` 標籤，0.4.0（第三批與第三批修正）可以打 `v0.4.0` 標籤，兩個標籤都還沒打。第四批只有範圍，尚未寫規格
- 相關文件：`CODEX_REVIEW.md`（審核結果與 N、P、U、M、L 各項目的原始說明）、`AGENTS.md`（每一輪的工作規則）
- 分工：2026-11-20 起，實作改由 Claude 的子 session 負責，Claude 主 session 負責寫規格與驗收。在這之前的實作者是 codex，文件裡提到 codex 的地方是當時的紀錄。

這份文件記錄已定案的方向、四批工作的範圍，以及**第一批、第二批、第二批修正與第三批的詳細規格**。第四批只列範圍，等前一批審核完才會補上詳細規格。沒有詳細規格的批次請不要先做。

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
| 第三批的份量 | 不拆，元件、多語系、輸入與 Popup 選項一次做完（2026-11-20 決定）。 |
| 元件的值變更 | 滑桿、開關、下拉選單、輸入框的值改變時，頁面腳本直接訂閱 Unity 元件的事件，在 `Awake` 訂閱一次。框架不轉送（2026-11-20 決定）。 |
| 捲動清單 | 附「依資料產生項目」的 `UIList`，不做虛擬化（2026-11-20 決定）。 |
| Popup 重用 | 預設每次開啟都重新建立，個別 Popup 可以設定成重用（2026-11-20 決定）。 |
| 導覽的選取記憶 | 往前切頁或開啟 Popup 時從預設選取元件開始；關閉 Popup 或返回時恢復該畫面上次的選取（2026-11-20 第三批審核時採用實作的做法）。 |
| 多個 Scene 的 EventSystem | 工作區自己的 Scene 有超過一個、或所有載入的 Scene 都沒有，才是 Error；其他載入的 Scene 也有時是 Warning（2026-11-20 第三批審核後修訂，見「第三批修正」的 J2）。 |

## 四批範圍

| 批次 | 主題 | 版本 | 狀態 |
|---|---|---|---|
| 第一批 | 讓 agent 寫錯馬上知道 | 0.2.0 | 已實作並通過審核；互動操作待人工確認；後續項目 F1～F6 見 `CODEX_REVIEW.md` |
| 第二批 | 遊戲程式的標準寫法 | 0.3.0 | 已實作並通過審核，G1～G5 的修正也已複驗通過；互動操作待人工確認；小提醒 I1 見 `CODEX_REVIEW.md`，併入第三批 |
| 第三批 | 元件與輸入 | 0.4.0 | 已實作並通過審核，J1～J8 的修正也已複驗通過；互動操作待人工確認；「複製確認框換畫面 ID」的工具待使用者決定 |
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

詳細規格在本文件最後的「第三批規格」。範圍如下：

- 第二批修正的小提醒 I1（見 `CODEX_REVIEW.md`）。
- 起始元件與元件集：安裝到專案的 `Assets`；JSON 計畫用名稱指定元件（例如 `"kind": "Slider"`）；專案可以自行增加元件。現有的按鈕、文字、圖片也改成走元件 Prefab。
- 元件集的預設字型。
- 清單元件 `UIList`：依資料產生項目，不做虛擬化。
- 多語系接口：計畫裡的文字和圖片可以寫鍵值並保留預設內容；切換語言時重新取值。
- 手把與鍵盤導覽、滑鼠懸停取消選取、滑鼠點過的按鈕不留在選取狀態、返回鍵。
- Popup 重用（P2），以及「點遮罩關閉」的選項。
- 確認框（起始元件之一）與它的標準呼叫方式，建立在第二批的 Popup 回傳結果之上。

### 第四批：驗收工具

- Validate 加強：走不到的畫面、沒有出口的畫面、沒綁動作的按鈕、文字裡有字型缺少的字元等，並提供命令列入口與結束代碼。
- 自動走訪：依藍圖從起始頁把每條路由走一遍並輸出報告，提供藍圖視窗按鈕與命令列入口。

### 先不做

更多層級（HUD、提示訊息、載入遮罩）、跨 Scene 常駐、走訪時截圖、Overlay 以外的顯示模式、Safe Area、滑鼠懸停提示、資料綁定、Addressables、清單虛擬化、依語言切換字型、手把按鍵提示圖示、元件的父子階層與自動排版、框架轉送元件的值變更。

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

**狀態：** 已完成（commit `45b7e0b`）。Claude 已在 2026-11-20 複驗通過，結果見 `CODEX_REVIEW.md` 的「第五輪審核」。

- 版本維持 0.3.0，不升版號，也不打標籤。標籤等這一輪複驗通過後才打。
- 變更寫進 `CHANGELOG.md` 的 0.3.0 段落，不另開新段落。
- 驗收的共同條件與前兩批相同：既有測試全數通過、每一項補上測試、測試在有 DOTween 的環境執行、文件同步更新。

| 項目 | 要做的事 | 狀態 |
|---|---|---|
| G1 | 修正返回歷史，規則見下方 | 已修正。PlayMode 驗證依序與並行切頁、保留更早的歷史及跳過重複的目前頁 |
| G2 | 通知進行中被拒絕的導航請求要記錄警告，規則見下方 | 已修正。七個導航入口的通知期間警告與一般忙碌皆已驗證。指南範本已在 PlayMode 執行 |
| G3 | 反射找不到方法時退回一般的 `Invoke()`；加測試確認反射路徑有效 | 已修正。已測逐一派送，並模擬反射方法不存在時的切頁與 Emit |
| G4 | AgentGuide 補上五點說明 | 已更新。首次顯示時機、失敗後的通知、停用的接收者與兩種路由事件時機已測 |
| G5 | 計畫欄位寫錯位置時報錯；不影響導航的例外改用不同的訊息文字 | 已修正。EditMode 驗證計畫檢查，PlayMode 驗證診斷文字與原始例外 |

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

---

# 第三批規格：元件與輸入

完成後版本為 0.4.0。交付時升版號，但不要打標籤，等審核通過再打。共十一項。

**狀態：** 已實作（分支 `feat/batch-3`，3-11 的 commit 是 `db8691f`，已快轉合併回 `main`），已審核（`CODEX_REVIEW.md` 的「第六輪審核」）。執行期的行為與規格相符；Editor 端的 J1～J8 已依本文件最後的「第三批修正」修正，並通過 Claude 複驗（`CODEX_REVIEW.md` 的「第七輪審核」）。實作時與規格不同的地方及審核結果，也在「第六輪審核」。

- 請依編號順序實作，每完成一項就 commit 一次，方便審核與回溯。3-2 和 3-3 是後面幾項的基礎。
- 驗收的共同條件與前兩批相同：既有測試全數通過、每一項補上測試、測試在有 DOTween 的環境執行、文件與 `CHANGELOG.md` 同步更新。
- 下面的型別、方法與 JSON 欄位名稱是建議。實作時若發現更好的名稱可以調整，但請在交付說明列出與規格不同的地方。
- 規格沒寫到、而且會影響公開 API 或使用方式的事，先問使用者，不要自己決定。

**這一批沿用的原則**

- **計畫欄位都要分得出「沒寫」和「寫了」。** 更新既有內容時只寫入有提供的欄位。`JsonUtility` 分不出布林值有沒有寫，所以新欄位一律用字串、數字（未設定為 NaN）或陣列，不新增布林欄位。
- **欄位寫錯位置要報錯。** 欄位用在不支援它的元件或畫面上時，計畫在套用前就失敗，訊息指出畫面、元件與欄位。
- **計畫沒寫的欄位不要寫成 Prefab 覆寫。** 這樣專案修改元件 Prefab 時，畫面上的實例才會跟著變。

## 3-1. 前一批的後續項目

處理 `CODEX_REVIEW.md` 的 I1：測試開始前重設「只警告一次」的靜態旗標；改寫 AgentGuide 裡那一句不好讀的說明。

## 3-2. 相依套件與輸入設定

- `package.json` 新增相依 `com.unity.inputsystem`，版本與這個 repo 的 `Packages/manifest.json` 相同。
- `UniGameUI.Runtime` 組件新增引用 `Unity.InputSystem` 與 `Unity.TextMeshPro`。
- 建立工作區時，EventSystem 直接使用 `InputSystemUIInputModule`。移除現在的反射查找與 `StandaloneInputModule` 退路。
- 框架讀輸入一律透過 `InputSystemUIInputModule` 上設定的動作（`move`、`submit`、`cancel`、`point`、`leftClick`、`scrollWheel`），不要寫死按鍵。專案換掉動作資產時，框架跟著變。
- Validate 新增兩項 Error：
  - 載入的 Scene 裡不是剛好一個 EventSystem，或它的輸入模組不是 `InputSystemUIInputModule`（現在是 Warning）。
  - Player Settings 的 Active Input Handling 只啟用舊的 Input Manager。
- `CODEX_REVIEW.md` 的「驗證方法」跟著更新。

**驗收：** EditMode 測試確認新工作區的 EventSystem 使用正確的模組，以及上述兩種設定錯誤會被 Validate 擋下。

## 3-3. 元件集與起始元件

**目標：** 畫面上的元件都是專案 `Assets` 裡元件 Prefab 的實例。專案改 Prefab 的美術，所有畫面跟著變；JSON 計畫用名稱指定元件。

### 元件集

```csharp
// Editor 組件
public enum UIWidgetKind
{
    Custom, Text, Image, Panel, Button, Toggle, Slider, ProgressBar, Dropdown, InputField, ScrollList
}

public sealed class UIWidgetSet : ScriptableObject
{
    public TMP_FontAsset DefaultFont { get; }
    public IReadOnlyList<UIWidgetEntry> Widgets { get; }    // 名稱、行為類別、Prefab
}

public static class UIWidgets
{
    public static UIWidgetSet Find();       // 找不到回傳 null
    public static UIWidgetSet Install();    // 補上缺少的起始元件與元件集
    public static void Register(string name, string prefabPath, UIWidgetKind like = UIWidgetKind.Custom);
    public static void SetDefaultFont(string fontAssetPath);
}
```

- 一個專案只有一個元件集資產，位置不限，用型別搜尋。超過一個時 Validate 列為 Error。
- 每個項目有三個欄位：名稱（計畫裡的 `kind`，區分大小寫，同一個元件集內不可重複）、行為類別、Prefab。
- **行為類別決定計畫能寫哪些欄位。** 十個內建名稱的行為類別就是自己。專案自訂的元件可以指定「比照哪一個內建類別」，例如 `PrimaryButton` 比照 `Button`、`HpBar` 比照 `ProgressBar`。指定為 `Custom` 的元件只能在計畫裡設定 `rect`，其餘由遊戲程式用 `View.Get<T>()` 操作。
- 比照某個類別的 Prefab 必須有該類別需要的元件（見下表的「程式取用」欄）。缺少時 Validate 列為 Error。

### 起始元件

- **起始元件由程式產生，套件裡不放 Prefab 檔。** `UIWidgets.Install()` 把 Prefab 建在 `Assets/UniGameUI/Widgets/`，並建立元件集資產。
- `Install()` 可以重複執行：已經存在的 Prefab 和項目一律不動，只補上缺少的。專案改過的美術不會被蓋掉；日後套件新增元件時，舊專案也補得到。
- 自動安裝：建立工作區或套用計畫時，專案還沒有元件集就自動安裝，並在 Console 說明建了哪些檔案。另外提供選單 **Tools → UniGameUI → Install Starter Widgets**。
- 計畫用到內建名稱、而元件集裡剛好缺那一個時，自動補上。用到不存在的自訂名稱時報錯，訊息列出可用的名稱。
- 外觀保持中性：使用 Unity 內建的 UI 圖和 TMP 預設字型，不追求美術。各專案會自己改。
- 要產生的 Prefab：下表的十個元件，加上清單項目 `ListItem`（3-4）和確認框 `Confirm`（3-9）。

### 元件一覽

| 名稱 | 內容 | 計畫欄位 | 程式取用 |
|---|---|---|---|
| `Text` | TMP 文字 | `text`、`textKey`、`fontSize`、`fontPath`、`textColor`（`color` 視為同義）、`alignment` | `TMP_Text` |
| `Image` | 圖片 | `color`、`spritePath`、`spriteKey` | `Image` |
| `Panel` | 九宮格底圖，不接收點擊 | `color`、`spritePath`、`spriteKey` | `Image` |
| `Button` | 按鈕與文字 | `text`、`textKey`、`action`、`color`、`spritePath`、`textColor`、`fontSize`、`fontPath` | `Button` |
| `Toggle` | 開關與文字 | `text`、`textKey`、`state`、`textColor`、`fontSize`、`fontPath` | `Toggle` |
| `Slider` | 滑桿 | `min`、`max`、`value`、`numberMode` | `Slider` |
| `ProgressBar` | 進度條 | `value`（0～1） | `UIProgressBar` |
| `Dropdown` | 下拉選單 | `options`、`optionKeys`、`value`（選項索引） | `TMP_Dropdown` |
| `InputField` | 輸入框 | `text`、`placeholder`、`placeholderKey`、`fontSize`、`fontPath` | `TMP_InputField` |
| `ScrollList` | 捲動區域與 `UIList` | `direction`、`spacing`、`itemPrefabPath` | `UIList` |

- 所有元件都接受 `id`、`kind`、`rect`、`updateExisting`。
- 字串列舉的值：`alignment` 為 `Left`、`Center`、`Right`；`state` 為 `On`、`Off`；`numberMode` 為 `Decimal`、`Whole`；`direction` 為 `Vertical`、`Horizontal`。
- `textKey`、`spriteKey`、`placeholderKey`、`optionKeys` 是多語系鍵值，見 3-5。
- `ProgressBar` 用新的執行期元件 `UIProgressBar`：公開 `float Value`（0～1，超出範圍時夾住），用填滿圖片顯示。

### 計畫的行為

- 元件的 `kind` 改為元件集裡的名稱，省略時仍是 `Text`。
- **建立：** 在 View 的 Content 底下建立元件 Prefab 的實例（保持 Prefab 連結），加上 `UIElementId`，物件名稱設為 ID。`UIElementId` 新增一個欄位記錄元件名稱（公開唯讀的 `Kind`）。
- **預設大小：** 新元件的大小取自元件 Prefab 的根節點，不再用固定的 240×72。
- **更新：** `updateExisting` 只能用在由計畫建立的元件，而且 `kind` 必須與原本相同。對手動加上 `UIElementId` 的物件（沒有記錄元件名稱）使用 `updateExisting` 會報錯。
- **Popup 與 Prefab 模式的頁面：** 元件是 View Prefab 裡的巢狀 Prefab 實例。套用覆寫之後，Prefab 本體裡的元件仍然連結到元件 Prefab。
- 元件之間沒有父子關係，全部放在 Content 底下。建立的先後順序就是繪製順序。
- 計畫最上層新增兩個欄位，影響的是整個專案的元件集：
  - `"widgets": [{ "name": "PrimaryButton", "prefabPath": "Assets/UI/PrimaryButton.prefab", "like": "Button" }]`：登記自訂元件。名稱已存在而且內容相同時略過；內容不同時報錯，不覆寫。
  - `"defaultFontPath": "Assets/Fonts/Main SDF.asset"`：設定預設字型，見下一節。

### 預設字型

- 元件集記錄一個預設字型（TMP Font Asset），可以是空的。
- `UIWidgets.SetDefaultFont` 把新字型寫進元件集，並把元件集所有 Prefab 裡「目前使用舊預設字型」的文字換成新字型。舊預設字型是空的時候，以 TMP 的預設字型為準。
- 文字若已經被專案改成別的字型，不要動它。
- 個別元件的 `fontPath` 照舊可用，會成為該實例的覆寫。

### 遊戲程式的寫法

- 滑桿、開關、下拉選單、輸入框的值改變時，**直接訂閱 Unity 元件的事件，框架不轉送**。標準寫法是在 `Awake` 訂閱一次：

```csharp
private void Awake()
{
    View.Get<Slider>(SettingsUI.Elements.SettingsView.Volume).onValueChanged.AddListener(OnVolumeChanged);
}
```

- AgentGuide 要寫明：不要在 `OnShowing` 訂閱，否則每顯示一次就多訂閱一次。要在顯示時設定初始值而不觸發事件，用 `SetValueWithoutNotify`。

**驗收**

- EditMode 測試：
  - `Install()` 產生全部 Prefab 與元件集；再執行一次不改動任何既有檔案；刪掉其中一個 Prefab 後再執行只補那一個。
  - 計畫能建立十種元件，各欄位正確寫入；部分更新只改有提供的欄位。
  - 欄位寫錯位置、`kind` 不存在、更新時 `kind` 不同，都在套用前報錯，而且沒有留下任何變更。
  - 修改元件 Prefab 的顏色後，沒有設定 `color` 的既有實例跟著變，有設定的不變。
  - Popup 裡的元件在套用覆寫後仍然連結到元件 Prefab。
  - 自訂元件的登記、比照類別，以及 `Custom` 類別只接受 `rect`。
  - `SetDefaultFont` 只換掉使用舊預設字型的文字。
- PlayMode 測試：十種元件都能用 `View.Get<T>()` 取得表中的型別；`UIProgressBar.Value` 的顯示與夾住。
- 實際打包的執行檔裡，十種元件都能顯示並取得。

## 3-4. 清單元件 `UIList`

**目標：** 商店、存檔欄位這類「依資料產生項目」的清單有固定寫法。不做虛擬化：所有項目都會建立。

```csharp
[AddComponentMenu("UniGameUI/UI List")]
public sealed class UIList : MonoBehaviour
{
    public GameObject ItemPrefab { get; set; }
    public RectTransform Content { get; }
    public int Count { get; }
    public IReadOnlyList<GameObject> Items { get; }
    public event Action<int> ItemClicked;

    public void SetItems<T>(IReadOnlyList<T> data, Action<GameObject, T, int> bind);
    public void SetCount(int count, Action<GameObject, int> bind);
    public void Clear();

    public static T GetInItem<T>(GameObject item, string elementId) where T : Component;
}
```

- `SetItems` 與 `SetCount` 可以重複呼叫。依索引重用已經建立的項目，不夠就建立，多的停用並保留到下次使用。每次呼叫都會對每個啟用的項目執行 `bind`。
- `ItemClicked`：項目根節點有 `Button` 時，點擊會觸發這個事件並帶索引。事件由清單在建立項目時接好一次，遊戲程式不必在 `bind` 裡訂閱。
- 項目裡的其他按鈕由遊戲程式在 `bind` 裡處理。AgentGuide 要提醒：項目會重用，訂閱前先 `RemoveAllListeners`。
- `GetInItem<T>`：在單一項目內依 `UIElementId` 找元件，尋找順序與 `View.Get<T>()` 相同。
- **`View.Get<T>()` 的 ID 對照表要略過清單項目裡的元件**，項目內的 ID 只在該項目內有意義。
- 內容物件使用版面配置元件，依 `direction` 垂直或水平排列，`spacing` 為間距。
- 起始元件 `ListItem` 是一個帶文字的按鈕，文字的元件 ID 是 `label`。`ScrollList` 預設使用它，可以用計畫的 `itemPrefabPath` 換掉。
- 選取移到清單項目上時，清單自動捲到看得見該項目的位置。只在導覽模式（3-6）下這樣做。

**驗收：** PlayMode 測試涵蓋項目的建立、重用與多餘項目的停用；`ItemClicked` 的索引，以及重複呼叫 `SetItems` 之後點一次只觸發一次；`GetInItem`；`View.Get` 不會拿到項目裡的元件；導覽到畫面外的項目時會捲動。

## 3-5. 多語系接口

**目標：** 框架只負責「什麼時候取值、取不到時顯示什麼」。翻譯內容由專案自己的多語系系統提供。

```csharp
public interface IUILocalizer
{
    bool TryGetText(string key, out string text);
    bool TryGetSprite(string key, out Sprite sprite);
}

public interface IUILocalizationHandler
{
    void OnLocalizationChanged(UIView view);
}

// UIRoot
public IUILocalizer Localizer { get; set; }
public void RefreshLocalization();
public string Localize(string key, string fallback = null);
public event Action LocalizationChanged;
```

- **元件：** 新增 `UILocalizedText`（配合 `TMP_Text`）、`UILocalizedImage`（配合 `Image`）、`UILocalizedDropdown`（配合 `TMP_Dropdown`，每個選項一個鍵值）。它們只記錄鍵值。計畫的 `textKey`、`placeholderKey`、`spriteKey`、`optionKeys` 會加上對應的元件。
- **預設內容：** 編輯時寫在元件上的文字、圖片、選項就是預設內容，Scene 與審核畫面照常顯示它。計畫可以同時給預設內容與鍵值，例如 `"text": "開始遊戲", "textKey": "menu.start"`。
- **取值時機：**
  - 每次畫面顯示前，在 `OnShowing` 之前。頁面腳本之後仍可以改寫文字。
  - 呼叫 `RefreshLocalization()` 或指定新的 `Localizer` 時，對所有現存的畫面實例重新取值，包含未顯示的頁面與保留中的 Popup。
  - `UIList` 建立或重用項目時。
- **取不到時：** 顯示預設內容。有設定 `Localizer` 而鍵值查不到時，每個鍵值警告一次，訊息包含畫面與元件。沒有設定 `Localizer` 時不警告。
- **`Localize(key, fallback)`：** 給頁面腳本組合動態文字用。查不到時回傳 `fallback`；`fallback` 是 null 時回傳鍵值本身，方便發現漏翻。
- **語言切換的通知：** `RefreshLocalization()` 重新取值之後，依序呼叫每個現存畫面根節點上的 `IUILocalizationHandler`，再觸發 `LocalizationChanged`。`UIViewBehaviour` 實作這個介面，提供 `protected virtual void OnLocalizationChanged()`。頁面腳本自己組出來的文字在這裡更新。接收者出錯時記錄例外，不影響其他接收者。
- 不處理依語言切換字型。多語言的字元涵蓋請用 TMP 的 fallback 字型。

**驗收**

- PlayMode 測試：有鍵值的文字、圖片、下拉選項在顯示前取值；切換語言後現存畫面更新；查不到時顯示預設內容且只警告一次；`Localize` 的三種結果；`OnLocalizationChanged` 的呼叫與例外隔離；清單項目也會取值。
- EditMode 測試：計畫的四種鍵值欄位，以及部分更新。

## 3-6. 手把與鍵盤導覽

**目標：** 用手把或鍵盤可以操作所有畫面；改用滑鼠時不留下選取外框。

```csharp
public enum UIInputMode { Pointer, Navigation }

// UIRoot
public UIInputMode InputMode { get; }
public event Action<UIInputMode> InputModeChanged;
public event Action<UIView, GameObject> ElementSelected;

// UIView
public Selectable DefaultSelected { get; }
```

**輸入模式**

| 輸入 | 結果 |
|---|---|
| 滑鼠移動、按鍵、滾輪，或觸控 | 進入 `Pointer` 模式 |
| 手把的任何 UI 動作，或鍵盤的方向輸入（`move`） | 進入 `Navigation` 模式 |
| 鍵盤的 `submit`、`cancel` | 不改變模式 |

- 起始模式是 `Pointer`。
- 輸入框正在編輯時不切換模式。

**選取規則**

- 「最上層畫面」指目前可以操作的那一個：有 Popup 時是最上層的 Popup，否則是目前頁。
- **預設選取元件：** 每個 View 可以指定一個（計畫的 View 欄位 `"defaultSelected": "<元件 ID>"`，Inspector 也能設定）。沒有指定時，取該 View 裡階層順序第一個可操作的 `Selectable`。
- **進入 `Navigation` 模式，或在該模式下最上層畫面改變時：** 選取這個畫面上次被選取的元件；它已經不能用時，選取預設選取元件。往前切到一個頁面、或開啟一個 Popup 時（包含重用的實例），從預設選取元件開始；關閉 Popup 或返回時才恢復上次的選取（審核後依實作調整）。
- 從 `Pointer` 進入 `Navigation` 的那一次輸入只用來建立選取，不要求同時移動。
- **進入 `Pointer` 模式時：** 清除選取。
- **`Pointer` 模式下點擊結束後：** 清除選取，按鈕不會留在選取狀態。正在編輯的輸入框與展開中的下拉選單除外。
- 每個 View 記住自己最後被選取的元件，返回該畫面時恢復。
- 選取只會落在最上層畫面裡。底下的畫面已經被設成不可操作，請用測試確認導覽不會跑到那裡。
- `ElementSelected`：在 `Navigation` 模式下，選取的元件改變時觸發，帶所屬的 View 與被選取的物件。游標移動的音效接這裡。
- 執行期找不到 EventSystem 或 `InputSystemUIInputModule` 時記錄一筆 Error，導覽與返回鍵停用，其餘功能照常。

**驗收**

- PlayMode 測試用 Input System 的測試工具模擬鍵盤、手把與滑鼠，涵蓋：
  - 上表三種輸入造成的模式變化，以及 `InputModeChanged`。
  - 進入導覽模式時選取預設元件；開關 Popup 與返回後恢復各畫面上次的選取。
  - 滑鼠移動後選取被清除；滑鼠點擊按鈕後不留選取；輸入框編輯中保持選取。
  - 有 Popup 時，方向輸入不會選到底下頁面的元件。
  - `ElementSelected` 的觸發與參數。
- EditMode 測試：計畫的 `defaultSelected`，以及指到不存在或不可選取的元件時報錯。

## 3-7. 返回鍵

**目標：** Esc 與手把的取消鍵等同返回，個別畫面可以關掉或接手處理。

```csharp
public interface IUIBackHandler
{
    bool OnBackRequested(UIView view);    // 回傳 true 表示已經處理
}

// UIRoot
public bool HandlesBackKey { get; set; }      // 預設 true，可在 Inspector 設定
public event Action BackRequestedAtRoot;

// UIView
public bool BlocksBackKey { get; }
```

收到 `cancel` 動作時，依序判斷：

1. `HandlesBackKey` 為 false、正在轉場、輸入框正在編輯、或下拉選單展開中：不處理。下拉選單會自己收起。
2. 呼叫最上層畫面根節點上的 `IUIBackHandler`（依元件順序）。有一個回傳 true 就停止，框架不再做事。
3. 最上層畫面設定為不回應返回鍵（計畫的 View 欄位 `"backKey": "Ignore"`，預設 `"Back"`）：不處理。
4. 沒有開著的 Popup、也沒有上一頁：觸發 `BackRequestedAtRoot`，不呼叫 `BackAsync`。「在主選單按 Esc 問要不要離開遊戲」接這裡。
5. 其餘情況呼叫 `BackAsync()`。

- `UIViewBehaviour` 實作 `IUIBackHandler`，提供 `protected virtual bool OnBackRequested() => false;`。暫停選單、「尚未儲存，確定離開？」這類需求覆寫它。
- `OnBackRequested` 不在導航進行中被呼叫，裡面可以直接導航。
- `OnBackRequested` 丟出例外時記錄下來，當成沒有處理，繼續往下判斷。這和 F1 的原則一致：出錯不能讓玩家離不開畫面。
- 返回鍵關閉用 `OpenPopupForResultAsync` 開的 Popup 時，結果是沒有值。
- 這個流程只管按鍵。畫面上的「返回」按鈕照舊走路由。

**驗收：** PlayMode 測試涵蓋上述五個步驟的每一個分支、`OnBackRequested` 丟例外，以及返回鍵關閉等待結果的 Popup。EditMode 測試計畫的 `backKey`。

## 3-8. Popup 的重用與點遮罩關閉

```csharp
// UIView，兩者只對 Popup 有意義
public bool ReusesInstance { get; }
public bool ClosesOnOverlayClick { get; }
```

- 計畫的 View 欄位：`"popupCache": "Recreate" | "Reuse"`（預設 `Recreate`）、`"overlayClick": "Ignore" | "Close"`（預設 `Ignore`）。寫在頁面上會報錯。View 的 Inspector 也要能設定。
- `UIView` 新增對遮罩物件的引用，建立 Popup 時設定。

**重用（`CODEX_REVIEW.md` 的 P2）**

- 設為 `Reuse` 的 Popup 關閉後不銷毀，停用並保留；下次開啟時用同一個實例，並移到 Popup 層的最上面。
- 生命週期方法、資料、等待結果的行為都和重新建立時相同。
- `TryGetView` 與 `Popups` 仍然只包含開著的 Popup。
- 保留中的實例被外部銷毀時，下次開啟重新建立。
- AgentGuide 要提醒：重用的 Popup 會留著上次的狀態，例如捲動位置與輸入內容，要在 `OnShowing` 重設。

**點遮罩關閉**

- 設為 `Close` 時，點擊遮罩等同不帶結果的關閉。點在 Popup 內容上不算。
- 只在該 Popup 是最上層而且沒有轉場進行時有效。
- 和 `backKey` 互相獨立。

**驗收：** PlayMode 測試確認重用時是同一個實例、每次開啟都收到新的資料與 `OnShowing`、等待結果正常、外部銷毀後能重新建立；點遮罩在兩種設定下的行為，以及點內容不會關閉。EditMode 測試兩個計畫欄位與寫在頁面上的錯誤。

## 3-9. 確認框

**目標：** 「確定要……嗎？」只要一行程式。

```csharp
public sealed class UIConfirmRequest
{
    public string Message;
    public string Title;          // null 時隱藏標題
    public string ConfirmText;    // null 時使用 Prefab 上的文字
    public string CancelText;
}

[AddComponentMenu("UniGameUI/UI Confirm Popup")]
public sealed class UIConfirmPopup : UIViewBehaviour { }

// UIRoot
public const string DefaultConfirmPopupId = "confirm";

public Awaitable<bool> ConfirmAsync(
    string message, string confirmText = null, string cancelText = null, string title = null,
    string popupId = DefaultConfirmPopupId, CancellationToken cancellation = default);
```

- **起始元件 `Confirm`：** 一個 Popup 的 View Prefab，畫面 ID 是 `confirm`，根節點掛 `UIConfirmPopup`。內含元件 `title`、`message`、`confirm`、`cancel`，都是元件 Prefab 的實例。
- **`UIConfirmPopup`：**
  - 在 `OnShowing` 讀取資料並填入文字。資料可以是 `UIConfirmRequest` 或單純的字串（當成訊息）。其他型別丟出例外，依 F1 的規則會取消開啟。
  - 兩個按鈕不走路由。確定呼叫 `View.Close(true)`，取消呼叫 `View.Close(false)`。
  - 兩個按鈕的預設文字是「確定」與「取消」，並帶多語系鍵值 `ui.confirm` 與 `ui.cancel`。
- **Prefab 的預設設定：** 預設選取 `cancel`；`popupCache` 為 `Reuse`；`backKey` 為 `Back`；`overlayClick` 為 `Ignore`。
- **登記到工作區：** 和其他 Popup 一樣寫在計畫裡，指向起始 Prefab：`{ "id": "confirm", "kind": "Popup", "prefabPath": "Assets/UniGameUI/Widgets/Confirm.prefab" }`。多個 Scene 可以登記同一個 Prefab。
- **`ConfirmAsync`：**
  - 只有按下確定時回傳 true。取消、返回鍵、等待被取消都回傳 false。
  - 確認框沒有顯示出來時（例如 `NotFound`、`Busy`）回傳 false，並記錄一筆警告說明原因。`NotFound` 的訊息要附上登記用的那一行計畫。
  - 專案可以複製 Prefab 做出不同樣式的確認框，用 `popupId` 指定。
- AgentGuide 的 `WorkshopPage` 範本改用 `ConfirmAsync`。

**驗收：** PlayMode 測試涵蓋確定、取消、返回鍵三種結果；字串與 `UIConfirmRequest` 兩種資料；沒有標題時隱藏；連續開兩次時文字不會殘留；沒有登記時的回傳值與訊息。範本實際編譯，並在打包的執行檔裡跑過一次確定與一次取消。

## 3-10. Validate 與 Inspector

- Validate 新增 Error：
  - 元件集不存在或超過一個；元件記錄的名稱在元件集裡找不到；元件 Prefab 缺少行為類別需要的元件。
  - `defaultSelected` 指到的物件不在該 View 裡，或沒有 `Selectable`。
  - 頁面設定了只對 Popup 有意義的選項。
- `UIView` 的 Inspector 顯示預設選取元件與返回鍵設定；Popup 另外顯示重用與點遮罩關閉。
- `UIRoot` 的 Inspector 顯示 `HandlesBackKey`。

## 3-11. 文件

AgentGuide 更新以下內容，範例都要實際編譯過：

- 元件一覽表：名稱、計畫欄位、程式取用的型別。自訂元件的登記方式與比照類別。
- 元件 Prefab 的位置，以及「改 Prefab 就會改到所有畫面」的工作方式。預設字型的設定方式。
- 值變更的標準寫法（3-3）與 `UIList` 的範例（3-4）。
- 實作 `IUILocalizer` 的最小範例、切換語言的呼叫方式、`OnLocalizationChanged` 的用途。
- 導覽模式與選取規則的摘要、返回鍵的五個步驟、`OnBackRequested` 的範例。
- Popup 的兩個選項，以及重用時要重設狀態。
- `ConfirmAsync` 的用法與登記方式。

`README.md` 的安裝說明補上 Input System 相依。

**需要人工確認的項目請列在交付說明：** 起始元件的實際外觀、用真的滑鼠與手把操作時的選取表現、下拉選單與輸入框搭配手把的操作。

# 第三批修正

第三批的審核（`CODEX_REVIEW.md` 的「第六輪審核」）發現 J1～J8，各項的重現方式與原因寫在那裡，這裡只寫修正的規則。版本維持 0.4.0。修正後由 Claude 複驗，通過才打 `v0.4.0` 標籤。

**狀態：** 已修正並通過 Claude 複驗（`CODEX_REVIEW.md` 的「第七輪審核」）。0.4.0 可以打 `v0.4.0` 標籤。

- 分支 `fix/batch-3`，J1～J8 各一個 commit（`3365f02` 到 `b29b305`），已快轉合併回 `main`。版本維持 0.4.0，沒有打標籤。
- 四組測試全數通過。數量、各項的修正方式、規則沒寫到而由實作者決定的地方，都在 `CODEX_REVIEW.md` 驗證紀錄的「第三批修正」。
- J8 的前三點用測試確認是問題並已修正。第四點（Play Mode 的 Validate）沒有重現，保留測試、沒有改程式。

以下是這一輪的規則：

- 每一項都要補測試，既有測試要全數通過。每完成一項就 commit 一次。
- J8 的四點先各寫一個測試確認，是真的才修；不是問題的在交付說明裡寫明怎麼確認的。
- 公開 API 沒有變動。使用方式有變動的地方（J2、J5）要同步更新 `AgentGuide.md` 與 `CHANGELOG.md`。

## J1 的規則：計畫寫出的欄位要整個固定

- 計畫寫出的欄位，不論數值和元件 Prefab 相同或不同，之後修改元件 Prefab 都要維持計畫寫的值。
- 複合欄位要固定每一個分量，例如顏色的 r、g、b、a。
- 陣列欄位（`options`、`optionKeys`）也要固定，包含長度與每一項。
- 計畫沒寫的欄位照舊不成為覆寫。

**驗收：** EditMode 測試。

- 用「只有部分分量和 Prefab 不同」的顏色，例如白色圖片寫 `#FFD25A`。之後把元件 Prefab 的顏色四個分量都改掉，實例維持計畫寫的值。
- 涵蓋 `Image`、`Panel`、`Button` 的 `color`，`Text` 的 `color` 與 `textColor`，`Button` 與 `Toggle` 的 `textColor`。
- `options` 的數量和元件 Prefab 相同時，清空 Prefab 的選項之後實例維持不變。

## J2 的規則：EventSystem 的檢查與附加開啟的 Scene

Validate 與套用計畫最後的檢查，EventSystem 的部分改成下表。這取代規格 3-2 的「載入的 Scene 裡不是剛好一個 EventSystem」。

| 情況 | 結果 |
|---|---|
| 工作區所在的 Scene 裡有超過一個啟用的 EventSystem | Error |
| 所有載入的 Scene 加起來沒有啟用的 EventSystem | Error |
| 工作區的 Scene 有一個，其他載入的 Scene 也有 | Warning，提醒執行期同時載入這些 Scene 時只能留一個 |
| 工作區的 Scene 沒有，其他載入的 Scene 剛好有一個 | 通過。這是把 EventSystem 放在共用 Scene 的做法 |

- 輸入模組的檢查對象是執行期會用到的那一個：工作區的 Scene 有就檢查它，沒有就檢查其他 Scene 的那一個。模組不對仍然是 Error。
- 訊息要分清楚「沒有 EventSystem」和「有 EventSystem 但沒有啟用」。
- 建立工作區時，目標 Scene 沒有 EventSystem 就加一個，照舊。
- **計畫失敗還原時，這次為了套用而開啟的 Scene 都要關回去**，不論是新建的還是既有的。原本就開著的 Scene 不動。
- 計畫成功時，附加開啟的目標 Scene 維持開著，照舊。
- 執行期的行為不變。

**驗收：** EditMode 測試。

- 上表四種情況各一個案例。
- 開著一個有工作區的 Scene 時，計畫可以建立另一個 Scene 並套用成功，Console 有上表的 Warning。
- 計畫失敗之後，載入的 Scene 清單和套用前相同；接著對原本的 Scene 套用計畫可以成功。

## J3 的規則：預檢要看既有 Prefab 的內容

- 畫面指到的 Prefab 已經存在時，預檢把 Prefab 裡的內容當成「既有的畫面」，不論這個工作區有沒有登記過它。
- `defaultSelected`、元件 ID 重複，以及 `updateExisting` 的所有規則（只能更新由計畫建立的元件、`kind` 必須相同、欄位必須是該元件支援的），都依這份內容檢查。

**驗收：** EditMode 測試，把起始確認框登記到新的 Scene。

- `defaultSelected` 指到 Prefab 裡的按鈕可以套用。
- 對 `confirm` 按鈕用 `kind: Text` 的 `updateExisting`，在套用前報錯，沒有留下任何變更。

## J4 的規則：下拉選單的捲軸

- 起始 `Dropdown` 範本的捲軸要有正確的寬度，滑動區域落在捲軸範圍內。

**驗收：** EditMode 測試檢查產生出來的 Prefab。選項超過展開範圍時的外觀列入人工確認。

## J5 的規則：選項與鍵值的數量要一致

- 更新既有的下拉選單時，套用後的選項數量和鍵值數量不同，就在套用前報錯，訊息請計畫同時提供 `optionKeys`。
- `"optionKeys": []` 表示清除所有鍵值。
- 沒有鍵值的下拉選單不受影響。

**驗收：** EditMode 測試涵蓋報錯、同時提供新鍵值、用空陣列清除三種情況。

## J6 的規則：能在預檢發現的錯誤都移到預檢

以下檢查改在預檢階段完成，資料從元件 Prefab 或既有的實例取得。還原機制保留，當成最後一道防線。

- 滑桿的 `min`、`max`、`value` 和既有的值合併之後是否合理。
- 下拉選單的 `value`、`optionKeys` 數量和既有選項是否相符。
- `spacing` 用在內容物件沒有版面配置元件的清單。
- 元件 Prefab 缺少該欄位需要的元件，或元件集項目的 Prefab 遺失。
- `overlayClick: Close` 用在沒有遮罩的 Popup。

**驗收：** EditMode 測試確認每一種都在套用前失敗，Scene 沒有被標成未儲存，也沒有留下任何變更。

## J7 的規則：TMP 衍生的顏色欄位不算待套用

- `HasReviewOverrides` 不把 TMP 自己衍生的 `m_fontColor32` 當成待套用的修改。`m_fontColor` 本身被覆寫時仍然算。

**驗收：** EditMode 測試重現第六輪的步驟，確認框不顯示待套用；真的改了審核實例的文字顏色時仍然顯示。

## J8 的規則：確認後才修

四點各寫一個測試。確認是問題的，照下面處理。

- **`defaultFontPath`：** 換字型失敗時，Scene 檔案與元件 Prefab 都要回到套用前的狀態。
- **`placeholder`：** 元件沒有可以寫入的 placeholder 時在套用前報錯。
- **`SetDefaultFont`：** 輸入框的文字與 placeholder 各自判斷，只換掉目前使用舊預設字型的那一個。
- **Play Mode 的 Validate：** 沒有設定 `defaultSelected` 的畫面不可以被判成遺失。

## 文件

- AgentGuide 補上：「點遮罩關閉」的內容範圍是 `Content` 的矩形，計畫建立的 Popup 是畫面中央 60%；要貼齊對話框請調整 `Motion` 的大小。
- AgentGuide「確認專案」第 5 點的 EventSystem 說明改成 J2 的規則。
- AgentGuide 的 `optionKeys` 說明補上 J5 的規則。
- `CHANGELOG.md` 的 0.4.0 補記這一輪的修正。
- `CODEX_REVIEW.md` 更新狀態表與「未驗證」。
