# UniGameUI 改進計畫

- 定案日期：2026-11-20（使用者與 Claude 討論後決定）
- 目前版本：0.5.0。四批都已通過審核。標籤 `v0.3.0`（`0a60b1d`）、`v0.4.0`（`19fa121`）、`v0.5.0`（`9188e4f`）都已打上。repo 在 2026-11-21 發佈到 <https://github.com/UsagiHime5566/UniGameUI>，安裝方式見根目錄的 `README.md`。第五批 0.6.0（藍圖視窗的操作體驗）已實作（`763642a`），待審核，尚未打標籤
- 相關文件：`CODEX_REVIEW.md`（審核結果與 N、P、U、M、L 各項目的原始說明）、`AGENTS.md`（每一輪的工作規則）
- 分工：2026-11-20 起，實作改由 Claude 的子 session 負責，Claude 主 session 負責寫規格與驗收。在這之前的實作者是 codex，文件裡提到 codex 的地方是當時的紀錄。

這份文件記錄已定案的方向、四批工作的範圍，以及**四批與各輪修正的詳細規格**。

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
| 自動走訪 | 要做。「送出事件」類的路由預設跳過。走訪會執行頁面腳本、不傳資料、不模擬輸入（2026-11-20 寫第四批規格時補充）。 |
| 驗收工具的嚴重度 | 第四批新增的檢查都是 Warning，命令列的嚴格模式才把 Warning 當成失敗（2026-11-20 決定）。 |
| 由程式控制的畫面 | 進來的方式不靠路由的畫面可以在藍圖標記，標記後不檢查走不走得到；確認框自動視為由程式控制（2026-11-20 決定）。2026-11-21 修訂：頁面標記之後照樣檢查有沒有出口，標記只代表進來的方式（見第五批追加的 5-8）。 |
| 頁面之間的流程 | 判斷標準是「這件事在哪個畫面發生」，不是「去處由誰決定」。在某一頁發生的換頁畫成那一頁的路由，每個可能的去處各一條，程式只負責挑哪一條；任何畫面都可能發生的事才用訊號（2026-11-21 決定，起因見第五批追加）。 |
| 執行中的藍圖 | Play Mode 時藍圖照常顯示並標示目前的畫面，但不能編輯（2026-11-21 決定）。 |
| 訊號 | 遊戲事件用 `SignalAsync` 送出，由藍圖的「任何畫面」節點決定去哪裡。訊號會等進行中的導航結束再執行；其他導航 API 照舊回傳 `Busy`（2026-11-20 決定）。 |
| 不可返回 | 路由的 `history: Clear` 表示跳過去之後不能返回，`ShowPage` 與 `OpenPopup` 都適用（2026-11-20 決定）。 |
| 發佈 | GitHub repo（<https://github.com/UsagiHime5566/UniGameUI>，2026-11-21 發佈）。其他專案用 git URL 加 `?path=/Packages/com.chrono.unigameui` 安裝，用版本標籤鎖定。安裝步驟寫在根目錄的 `README.md`。 |
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
| 第三批 | 元件與輸入 | 0.4.0 | 已實作並通過審核，J1～J8 的修正也已複驗通過；互動操作待人工確認 |
| 第四批 | 驗收工具 | 0.5.0 | 已實作並通過審核，包含追加的 4-7（訊號與「任何畫面」節點）與 K1～K8 的修正；互動操作待人工確認 |
| 第五批 | 藍圖視窗的操作體驗 | 0.6.0 | 5-1～5-5 已實作，自動驗證通過；2026-11-21 追加 5-6～5-8（執行中的藍圖、訊號路由的線、頁面自己的流程），待實作；外觀待使用者確認，之後才打標籤 |

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
- 訊號與「任何畫面」節點（2026-11-20 追加）：遊戲事件用 `SignalAsync` 送出，要去哪裡畫在藍圖上；路由的 `history: Clear` 設定跳過去之後不能返回。

### 四批完成後的待辦

- （已在第五批的 5-5 實作，待審核）`view.no-exit` 加上不可返回的 Popup：只接了 `Back` 路由的 Popup 以 `history: Clear` 開啟之後關不掉（第九輪審核）。
- IL2CPP Player 的驗證。開發用的電腦沒有安裝模組，四批都只驗證過 Mono。
- 發佈：repo 已在 2026-11-21 建立，從全新的專案用 git URL 安裝已實測可用。版本標籤要另外推上去（`git push origin --tags`），`#v0.5.0` 這種鎖定版本的網址才有效。

- 走訪加 `-unigameui-include-emit` 時 Editor 偶爾在結束時當機（K8）：第八輪與第十輪各一次，報告都已寫出。原因不明。
- Canvas 的顯示模式：使用者希望預設是 Screen Space - Camera 並接上 Main Camera，2026-11-21 決定先擱置。
- Popup 不從 Prefab 複製、直接使用 Scene 裡的物件：2026-11-21 討論過，使用者決定先用用看再說。

### 先不做

更多層級（HUD、提示訊息、載入遮罩）、跨 Scene 常駐、走訪時截圖、Overlay 以外的顯示模式、Safe Area、滑鼠懸停提示、資料綁定、Addressables、清單虛擬化、依語言切換字型、手把按鍵提示圖示、元件的父子階層與自動排版、框架轉送元件的值變更、複製 View Prefab 並換畫面 ID 的工具。

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
  - 不同樣式的確認框：用計畫建立新的 Popup，放進 `title`、`message`、`confirm`、`cancel` 四個元件，掛上 `UIConfirmPopup`，再用 `popupId` 指定。直接複製 Prefab 不行，因為畫面 ID 存在 Prefab 裡；使用者在 2026-11-20 決定不加「複製並換 ID」的工具。
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

# 第四批規格：驗收工具

完成後版本為 0.5.0。交付時升版號，但不要打標籤，等審核通過再打。共七項。

**狀態：** 七項都已實作並通過 Claude 審核（`CODEX_REVIEW.md` 的「第八輪審核」與「第九輪審核」）。4-7 是 2026-11-20 追加的。第八輪提出的 K1～K8 已修正並複驗通過。0.5.0 可以打 `v0.5.0` 標籤。

- 分支 `feat/batch-4`，4-1～4-6 各一個 commit（`d587709`、`088edcb`、`e5f577b`、`77ace71`、`0bdd783`、`d0e395f`），已快轉合併回 `main`。版本已升到 0.5.0，沒有打標籤。
- 四組測試全數通過；Validate 與走訪的命令列在副本專案實際執行過。數量、每一次執行的指令與結果、與規格不同的地方，都在 `CODEX_REVIEW.md` 驗證紀錄的「第四批改進（0.5.0）」。
- 4-7 的 commit：`b33dc06`（本體）、`3de9c6c`（4-3 的調整）、`ae93880`（4-5 的調整與兩點補充）、`b8cc23a`（文件），同樣已快轉合併回 `main`，版本維持 0.5.0。四組測試全數通過；命令列對有訊號路由的 Scene 實際執行過；另外打包了 Windows 執行檔實際執行。紀錄在 `CODEX_REVIEW.md` 驗證紀錄的「第四批追加：4-7 訊號與「任何畫面」節點（0.5.0）」。

以下是這一批的規格：

- 請依編號順序實作，每完成一項就 commit 一次。
- 驗收的共同條件與前三批相同：既有測試全數通過、每一項補上測試、測試在有 DOTween 的環境執行、文件與 `CHANGELOG.md` 同步更新。
- 下面的型別、方法、欄位與代碼名稱是建議。實作時若發現更好的名稱可以調整，但請在交付說明列出與規格不同的地方。
- 規格沒寫到、而且會影響公開 API 或使用方式的事，先問使用者，不要自己決定。

**目標：** agent 做完 UI 之後，不必有人進 Editor 點過一遍，就能確認「藍圖沒有接錯、每條路由都走得通」。兩個工具都要能從命令列執行，用結束代碼表示結果，並輸出機器讀得懂的報告。

**這一批的原則**

- **新的檢查預設是 Warning。** 它們指出的是「看起來不對」，不是「一定壞掉」，專案可能有正當理由。命令列的嚴格模式會把 Warning 也當成失敗。
- **每個 Warning 都要有正當的消除方式。** 一直消不掉的警告會讓人不再看警告。每一項檢查都要寫明什麼情況不算。
- **工具不進 Player。** 走訪與報告的程式放在 Editor 組件，`Runtime` 組件不新增公開型別。真的需要時先提出討論。4-7 會新增執行期的公開 API，是這一點的例外。

## 4-1. 問題代碼

**目標：** agent 用穩定的代碼判斷問題種類，不必比對訊息文字。

```csharp
public readonly struct UIIssue
{
    public UIIssueSeverity Severity { get; }
    public string Code { get; }         // 例如 "route.target-missing"
    public string Message { get; }
    public string ViewId { get; }       // 和畫面無關時是 null
    public string ElementId { get; }    // 和元件無關時是 null
}
```

- 既有與新增的每一種問題都要有代碼。代碼用小寫英文，以「主題.問題」命名，定下來之後不要更改。
- 既有的英文訊息一併改成繁體中文。
- `ToString()` 的格式是 `Error [route.target-missing] 訊息`。
- AgentGuide 用一張表列出所有代碼、嚴重度、意思與處理方式。

**驗收：** EditMode 測試確認每一個代碼至少被一個案例觸發，而且 AgentGuide 表格裡的代碼和程式裡的一致。

## 4-2. 由程式控制的畫面

**目標：** 有些畫面不靠路由進出，例如確認框是用 `ConfirmAsync` 開啟、自己關閉。這些畫面要能標記，4-3 的檢查才不會誤報。

- 藍圖的每個畫面多一個設定「由程式控制進出」，預設關閉。
  - 計畫的 View 欄位：`"navigation": "Routes" | "Code"`，預設 `Routes`。和其他 View 選項一樣，只在新建畫面或設了 `updateExisting` 時寫入。
  - 藍圖視窗右欄可以勾選。
  - C#：`UIEntry.NavigatedByCode`（唯讀）與 `UIAuthoring.SetNavigatedByCode(root, id, value)`。
- 這個設定存在工作區的藍圖裡，不存在 View Prefab 裡。同一個 Popup Prefab 在不同 Scene 可以有不同的設定。
- 根節點掛了 `UIConfirmPopup` 的畫面自動視為由程式控制，不必另外設定。
- 這個設定只影響 4-3 的檢查與 4-5 的走訪，不影響執行期的行為。

**驗收：** EditMode 測試涵蓋計畫欄位（新建、部分更新、寫錯值在套用前報錯）、藍圖設定的儲存，以及確認框自動視為由程式控制。

## 4-3. Validate 新增的檢查

以下都是 Warning。「由程式控制」指 4-2 的設定。

| 代碼 | 會回報的情況 | 不算的情況 |
|---|---|---|
| `start.none` | 工作區沒有設定起始頁。 | 所有頁面都由程式控制。 |
| `view.unreachable` | 從起始頁沿著 `ShowPage` 與 `OpenPopup` 路由走不到的頁面或 Popup。 | 起始頁本身；由程式控制的畫面。沒有起始頁時不逐一回報，只回報 `start.none`。 |
| `view.no-exit` | 頁面沒有任何 `ShowPage` 或 `Back` 路由，而且從它沿著 `OpenPopup` 路由開得到的 Popup 也都沒有 `ShowPage` 路由。Popup 沒有任何 `ClosePopup`、`Back` 或 `ShowPage` 路由，也沒有設定點遮罩關閉。（頁面的規則在第四批審核的 K4 放寬：遊戲頁只有「開啟暫停選單」、暫停選單有「回標題」時，遊戲頁算有出口。） | 由程式控制的畫面；工作區只有一個頁面時的那個頁面。 |
| `button.unbound` | View 裡的 `Button` 沒有 `UIButtonAction`（或動作是空的），`onClick` 也沒有在 Inspector 接任何監聽。 | 由程式控制的畫面裡的按鈕；清單項目裡的按鈕。 |
| `text.missing-glyph` | View 裡 TMP 文字的預設內容，有字型和它的 fallback 都沒有的字元。 | 空白與控制字元；富文字標籤本身；Sprite Asset 補得到的字元（後兩項是第四批審核 K7 補上的）。 |

- `button.unbound` 的訊息要建議標準做法：給按鈕 `action` 並接一條路由，要由程式處理就用 `Emit`。
- `text.missing-glyph` 的細節：
  - 檢查範圍是審核實例裡所有的 TMP 文字，包含輸入框的 placeholder，以及下拉選單的每一個選項（用選項文字的字型判斷）。
  - fallback 包含字型資產自己的 fallback 清單，以及 TMP Settings 的全域 fallback。
  - 動態字型（Atlas Population Mode 是 Dynamic）以來源字型檔有沒有這個字為準。
  - **檢查不可以改動任何字型資產。** 不要用會把字元加進圖集的 API。
  - 只檢查預設內容。多語系翻譯後的文字不在範圍內。
  - 訊息列出畫面、元件與缺少的字元（最多十個），並提示處理方式：設定預設字型（`defaultFontPath`），或修改預設文字。

**驗收：** EditMode 測試。

- 每一項檢查各有「會回報」的案例；表格「不算的情況」的每一種也各有案例。
- `text.missing-glyph`：靜態字型缺字、fallback 補得到、動態字型的來源檔有這個字、下拉選單的選項；檢查前後字型資產的內容沒有改變。
- 操作指南的示例計畫套用到沒有設定中文字型的專案之後，新的 Warning 只有確認框「確定」與「取消」的 `text.missing-glyph`。

## 4-4. Validate 的命令列入口與報告

```text
Unity -batchmode -projectPath <專案> -logFile <記錄檔>
  -executeMethod UniGameUI.Editor.UIValidation.RunFromCommandLine
  [-unigameui-scene <Scene 路徑>]... [-unigameui-report <JSON 路徑>] [-unigameui-strict]
```

- `-unigameui-scene` 可以重複。省略時檢查 Build Settings 裡啟用的所有 Scene；沒有工作區的 Scene 略過，並在報告裡註明。
- 不要加 `-quit`。工具自己結束 Editor 並設定結束代碼。

| 結束代碼 | 意思 |
|---|---|
| 0 | 沒有 Error。嚴格模式下也沒有 Warning。 |
| 1 | 有 Error，或嚴格模式下有 Warning。 |
| 2 | 沒辦法執行：參數錯誤、Scene 不存在，或沒有任何 Scene 有工作區。 |

- 報告是 JSON，預設寫到 `Library/UniGameUI/validate.json`。摘要同時印到記錄檔。

```json
{
  "tool": "UniGameUI.Validate",
  "version": "0.5.0",
  "strict": false,
  "passed": false,
  "errorCount": 1,
  "warningCount": 1,
  "scenes": [
    {
      "scenePath": "Assets/Scenes/Workshop.unity",
      "hasWorkspace": true,
      "issues": [
        {
          "severity": "Error", "code": "route.target-missing",
          "viewId": "workshop", "elementId": "", "message": "…"
        }
      ]
    }
  ]
}
```

- C# 入口：`UIValidation.CheckScenes(scenePaths, strict)` 回傳同一份報告的物件（`UIValidationReport`，有 `Passed`、`ErrorCount`、`WarningCount`、`ToJson()`），不會結束 Editor。
  - 它會依序開啟各個 Scene，結束後回到原本開著的 Scene。
  - 目前開著的 Scene 有未儲存的修改時丟出例外，不會自動存檔。
- 藍圖視窗的 Validate 按鈕照舊檢查目前的工作區。對話框顯示 Error 與 Warning 的數量；每一筆問題連同代碼記到 Console，點擊可以定位到畫面物件。

**驗收**

- EditMode 測試 `CheckScenes` 與報告的 JSON。結束代碼的判斷要抽成可以測試的函式，三種結果各一個案例。
- 實作者在副本專案實際執行命令列，確認三種結束代碼與報告檔案，結果寫進交付說明。

## 4-5. 自動走訪

**目標：** 在 Play Mode 裡把藍圖上的每條路由實際觸發一次，確認走得通，並輸出報告。

```csharp
public sealed class UIWalkOptions
{
    public bool IncludeEmit;                    // 預設 false
    public float StepTimeoutSeconds = 10f;
    public string ReportPath;                   // null 時使用預設位置
}

public static class UIWalk
{
    public static void Start(UIWalkOptions options = null);    // 對目前開著的 Scene 走訪
    public static UIWalkReport LastReport { get; }             // 這個 Editor 工作階段最後一次的結果
    public static event Action<UIWalkReport> Finished;
    public static void RunFromCommandLine();
}
```

**流程**

1. 先對這個 Scene 執行 Validate。有 Error 就不進 Play Mode，結果是 `NotRun`，報告附上 Validate 的結果。
2. 進入 Play Mode。有待套用到 Prefab 的覆寫時不詢問，直接使用已儲存的 Prefab，並在報告記一筆說明。
3. 等起始頁顯示完成，記為第一步「起始」。沒有起始頁時這一步略過。
4. 依藍圖的順序，每條路由走一步。
5. 離開 Play Mode，寫出報告，觸發 `Finished`。

開啟或關閉 Domain Reload 的專案都要能用。

**每一步**

1. **準備：** 關掉所有 Popup、清空歷史，讓來源畫面成為最上層。準備時不傳資料。
   - 來源是頁面：先顯示另一個頁面，再顯示來源頁，讓歷史裡有一筆，`Back` 路由才有地方回去。另一個頁面優先選有路由通到來源頁的，沒有就用起始頁。工作區只有一個頁面時不必。
   - 來源是 Popup：先顯示一個頁面，再開啟這個 Popup。頁面優先選有 `OpenPopup` 路由指到它的，沒有就用起始頁。
2. **觸發：** `root.TriggerAsync(來源, 動作)`。
3. **判定：** 依下表。另外，這一步從準備到穩定的期間，Console 不可以出現 Error 或 Exception。
4. **等待穩定：** 連續幾個影格沒有導航在進行，才開始下一步。頁面腳本可能在 `OnShown` 之後接著導航，這不算失敗，下一步的準備要能把狀態拉回來。
5. **逾時：** 超過 `StepTimeoutSeconds` 沒有結束，這一步失敗，原因是逾時，接著繼續下一步。

| 路由種類 | 通過條件 |
|---|---|
| `ShowPage` | 回傳 `Completed`，完成時目前頁是目標頁。 |
| `OpenPopup` | 回傳 `Completed`，完成時最上層的 Popup 是目標。 |
| `ClosePopup` | 回傳 `Completed`，來源 Popup 不再開著。 |
| `Back` | 回傳 `Completed`。來源是頁面時回到準備時的上一頁，來源是 Popup 時 Popup 關閉。工作區只有一個頁面而來源是頁面時，這一步略過。 |
| `Emit` | 預設略過。`IncludeEmit` 開啟時觸發，回傳 `Completed` 即通過。 |

**每一步的結果**

- `Passed`、`Failed` 或 `Skipped`，加上原因。
- 準備來源畫面失敗時：
  - 來源是由程式控制的畫面（4-2）：記為 `Skipped`，原因是需要由程式提供資料。
  - 其他情況：記為 `Failed`。靠路由到得了的畫面本來就收不到資料，沒有資料就顯示不出來是真的問題。
- 這一步期間 Console 的 Error、Exception 與 Warning 都收進報告。Warning 不影響結果。
- 一步失敗不影響後面的步驟。

**不在走訪範圍**

- 由程式控制、而且沒有任何路由以它為來源的畫面不會被開啟。它們列在報告的畫面清單裡，註明沒有走訪。
- 走訪不模擬滑鼠、鍵盤或手把輸入，不檢查畫面的外觀，不截圖。
- 走訪會執行頁面腳本的生命週期方法，和實際遊玩相同。`Emit` 預設略過，所以 `OnAction` 預設不會被呼叫。

**報告**

預設寫到 `Library/UniGameUI/walk-<Scene 名稱>.json`。

```json
{
  "tool": "UniGameUI.Walk",
  "version": "0.5.0",
  "scenePath": "Assets/Scenes/Workshop.unity",
  "outcome": "Failed",
  "passedCount": 4,
  "failedCount": 1,
  "skippedCount": 1,
  "steps": [
    {
      "source": "workshop", "action": "openSettings", "kind": "ShowPage", "target": "settings",
      "status": "Passed", "result": "Completed", "reason": "", "seconds": 0.42, "logs": []
    }
  ],
  "views": [
    { "id": "confirm", "kind": "Popup", "visited": false, "reason": "由程式控制" }
  ],
  "validation": { "errorCount": 0, "warningCount": 2, "issues": [] }
}
```

- `outcome` 是 `Passed`（沒有失敗的步驟）、`Failed`，或 `NotRun`（Validate 有 Error、沒有工作區、整體逾時）。
- `logs` 的每一筆有種類與訊息。
- `views` 列出每一個畫面在走訪期間有沒有顯示過。

**命令列**

```text
Unity -batchmode -projectPath <專案> -logFile <記錄檔>
  -executeMethod UniGameUI.Editor.UIWalk.RunFromCommandLine
  -unigameui-scene <Scene 路徑> [-unigameui-report <JSON 路徑>]
  [-unigameui-include-emit] [-unigameui-step-timeout <秒>] [-unigameui-timeout <秒>]
```

- 一次走訪一個 Scene，`-unigameui-scene` 必填。
- 不要加 `-quit`。工具會在回到 Edit Mode 之後自己結束 Editor。
- `-unigameui-timeout` 是整體的時間上限，預設 300 秒。超過就結束，結果是 `NotRun`，避免批次工作卡住。

| 結束代碼 | 意思 |
|---|---|
| 0 | `Passed`。可以有略過的步驟。 |
| 1 | `Failed`。 |
| 2 | `NotRun`，或參數錯誤、Scene 不存在。 |

**藍圖視窗**

- 工具列新增「走訪」按鈕，對目前的 Scene 執行。結束後顯示摘要：通過、失敗、略過各幾步，以及失敗步驟的來源、動作與原因。
- 失敗的步驟同時記到 Console。

**驗收**

- 自動測試（實際進入 Play Mode）涵蓋：
  - 全部正常的工作區。五種路由的判定各一個案例；`Emit` 預設略過，開啟選項後會觸發。
  - 目標頁的 `OnShowing` 丟例外：這一步 `Failed`，報告裡有例外訊息，後面的步驟照常。
  - 步驟期間有 `Debug.LogError`：`Failed`。只有 `Debug.LogWarning`：`Passed`，警告在報告裡。
  - 頁面腳本在 `OnShown` 之後接著導航（操作指南 `LaunchPage` 的寫法）：不影響其他步驟的結果。
  - 來源是 Popup；來源是頁面的 `Back`；只有一個頁面時的 `Back`。
  - 由程式控制的來源畫面準備失敗：`Skipped`。
  - 單步逾時。
  - Validate 有 Error 時不進 Play Mode，`outcome` 是 `NotRun`。
- 實作者在副本專案用命令列實際執行，結果寫進交付說明：
  - 操作指南的示例計畫加上範例腳本：`Passed`、結束代碼 0，五條路由有四步通過、一步（`Emit`）略過。
  - 一個會失敗的例子：結束代碼 1。
  - Scene 不存在：結束代碼 2。

## 4-6. 文件

- AgentGuide 新增「驗收」一節，寫成 agent 交付前的固定步驟：
  1. 執行 Validate。Error 要全部解決，Warning 逐筆確認是有意的還是要修。
  2. 執行走訪，結果要是 `Passed`。
  3. 在交付說明附上兩份報告的摘要。
- 同一節說明兩個工具的命令列、C# 入口、報告格式與結束代碼。
- 問題代碼表（4-1）。
- 「由程式控制的畫面」的設定方式與用途（4-2）。
- 走訪的範圍與限制：不傳資料、不模擬輸入、不看外觀、`Emit` 預設略過。
- `README.md` 補上驗收工具的簡介。`CHANGELOG.md` 新增 0.5.0。

**需要人工確認的項目請列在交付說明：** 藍圖視窗的「走訪」按鈕與結果摘要、Validate 的對話框與 Console 定位、右欄「由程式控制進出」的勾選。

## 4-7. 訊號與「任何畫面」節點

**目標：** 遊戲裡發生的事（角色死亡、挑戰者連線、斷線）不論當時在哪個畫面都能觸發跳轉。這種跳轉要能畫在藍圖上，不會因為剛好在轉場而漏掉，而且可以設定跳過去之後能不能返回。

這一項會新增執行期的公開 API，是本批「工具不進 Player」原則的例外。

### 藍圖

- 藍圖多一個固定的節點「任何畫面」。它不能刪除，沒有線可以連進去，也不是畫面，不會出現在 `Entries`。
- 從它拉出去的路由叫「訊號路由」：不論目前顯示的是哪個畫面都成立。
- 路由的來源用常數 `UIRoot.AnySource`（值是 `"*"`）表示。計畫寫 `"source": "*"`：

```json
{ "source": "*", "action": "challengerJoined", "kind": "ShowPage", "target": "challenger", "history": "Clear" }
```

- 訊號路由的種類只能是 `ShowPage`、`OpenPopup`、`Emit`。寫成 `Back` 或 `ClosePopup` 在套用前報錯。
- 訊號的動作名稱在訊號路由之間不可重複。它和各畫面自己的動作互不相干，同名也不會互相觸發。
- 動作名稱照舊產生 ID 常數。
- 藍圖視窗顯示這個節點，可以從它拖線、在右欄編輯。`UIAuthoring.Connect`、`UpdateRoute`、`Disconnect` 的 `source` 接受 `UIRoot.AnySource`。

### 送出訊號

```csharp
// UIRoot
public const string AnySource = "*";

public Awaitable<UINavResult> SignalAsync(
    string action, object data = null, CancellationToken cancellation = default);
```

- **訊號不會因為忙碌而被拒絕。** 有導航正在進行時，訊號等它結束再執行。多個訊號依送出的順序執行。在生命週期方法裡呼叫也一樣，等這次導航結束後執行。
- 這不改變其他 API：`ShowPageAsync`、`OpenPopupAsync`、`TriggerAsync` 等照舊回傳 `Busy`。
- 等待期間 token 被取消，或工作區被停用、銷毀：回傳 `Canceled`。
- 找不到這個動作的訊號路由：回傳 `NotFound` 並記錄錯誤。
- 執行時依路由的種類：

| 種類 | 行為 |
|---|---|
| `ShowPage` | 顯示目標頁並傳入 `data`，依路由的 `history` 處理歷史。開著的 Popup 照切頁的規則關閉。已經在目標頁時回傳 `Unavailable`。 |
| `OpenPopup` | 在目前的畫面上開啟目標 Popup 並傳入 `data`。已經有 Popup 開著時疊在上面。目標已經開著時回傳 `Unavailable`。 |
| `Emit` | 呼叫目前最上層畫面的 `OnAction(action)`，並觸發 `ActionEmitted`，來源是 `UIRoot.AnySource`。不帶資料；有給 `data` 時記錄一筆警告。 |

- `ActionTriggered` 在訊號開始執行時觸發，`ActionCompleted` 在結束時觸發，和一般路由相同。
- 一般路由不傳資料，訊號可以。

### 跳過去之後能不能返回

用路由的 `history` 設定。`OpenPopup` 路由現在也接受這個欄位，一般路由與訊號路由都適用。

| 種類 | `history` | 結果 |
|---|---|---|
| `ShowPage` | `Push`（預設） | 原本的頁面進歷史，可以返回。 |
| `ShowPage` | `Replace` | 原本的頁面不進歷史。 |
| `ShowPage` | `Clear` | 清空歷史，不能返回。 |
| `OpenPopup` | `Push`（預設） | Popup 可以用返回關閉，回到底下的畫面。 |
| `OpenPopup` | `Clear` | 不能返回，規則見下。 |

`OpenPopup` 寫 `Replace` 在套用前報錯。

以 `Clear` 開啟的 Popup 稱為「不可返回的 Popup」：

1. 開啟時清空歷史。
2. 它在最上層時，返回一律無效。返回鍵不處理，但 `OnBackRequested` 仍然會先被呼叫；`BackAsync()` 與 `Back` 路由回傳 `Unavailable`。
3. 它開著的期間發生的切頁一律清空歷史，不論那條路由的 `history` 寫什麼。底下的頁面不會進歷史。
4. `ClosePopup` 路由、`View.Close()` 與點遮罩關閉（有設定時）照常可以關閉它。這些是設計者明確做出來的出口。關閉後回到底下的畫面，歷史是空的。
5. 這個狀態只屬於這一次開啟，關閉後就沒有了。重用實例的 Popup 也一樣。

- C# 對應：`OpenPopupAsync` 新增可以指定 `UIHistoryMode` 的多載，傳入 `Replace` 丟出 `ArgumentException`。
- 藍圖視窗右欄的「返回歷史」選單，在 `OpenPopup` 路由顯示「可以返回」與「不可返回」兩個選項。

### 和這一批其他項目的關係

- **4-1：** 新的錯誤要有代碼，例如訊號路由的種類不對、`OpenPopup` 的 `history` 寫 `Replace`。
- **4-3：** 訊號路由的目標算走得到，`view.unreachable` 不回報，不論有沒有起始頁。訊號路由不算任何畫面的出口。
- **4-5：** 訊號路由也要走訪，每條一步。準備時顯示起始頁，用 `SignalAsync(action)` 觸發，不傳資料，判定依種類和一般路由相同。`history` 是 `Clear` 時另外確認完成後歷史是空的；不可返回的 Popup 另外確認 `BackAsync()` 回傳 `Unavailable`。
- **4-5 的補充，一般路由也適用：** 目標畫面標記為「由程式控制」（4-2）而顯示失敗時，這一步記為 `Skipped`，原因是需要由程式提供資料。需要資料才能顯示的畫面（例如挑戰者登場）請這樣標記。
- **4-5 的補充：** 走訪期間頁面腳本可能自己送出訊號或觸發導航，例如計時器。走訪的準備與觸發遇到 `Busy` 時，等這次導航結束後重試，逾時才算失敗。

### 文件

AgentGuide 新增「由遊戲事件觸發的跳轉」一節，範本都要實際編譯過。

- 兩種寫法各自的用途：
  - **只在某個畫面才會發生的事**（HUD 上的角色死亡、畫面自己的計時器）：畫成那個畫面的路由，頁面腳本用 `Root.TriggerAsync(View.Id, 動作)` 觸發。這種寫法會遇到 `Busy` 與「上面蓋著 Popup」，範本要示範怎麼檢查。
  - **任何畫面都可能發生的事**（挑戰者連線、斷線）：畫成「任何畫面」的訊號路由，遊戲程式用 `root.SignalAsync(動作, 資料)` 送出。
- `history: Clear` 的用途，以及不可返回的 Popup 的五條規則。
- 訊號搭配 `Emit` 的寫法：最上層的畫面在 `OnAction` 收到之後自己決定要去哪裡。

### 驗收

- PlayMode 測試：
  - 三種種類的訊號；`data` 送達目標；找不到路由。
  - 轉場進行中送出訊號：轉場結束後才執行，回傳執行後的結果。連續送出兩個訊號：依序執行。在 `OnShown` 裡送出：這次導航結束後執行。
  - 等待中取消 token、停用工作區：`Canceled`。
  - 有 Popup 開著時送出 `ShowPage` 訊號；有 Popup 開著時送出 `OpenPopup` 訊號，新的疊在上面。
  - `ShowPage` 加 `Clear`：歷史是空的，`BackAsync()` 回傳 `Unavailable`。
  - 不可返回的 Popup 的五條規則各一個案例：返回鍵、`Back` 路由、從它切頁後底下的頁面不在歷史裡、`ClosePopup` 後歷史是空的、重用的實例第二次以 `Push` 開啟時可以返回。
  - 其他導航 API 遇到忙碌仍然回傳 `Busy`。
- EditMode 測試：計畫的 `"source": "*"`、種類與 `history` 的檢查、部分更新、ID 常數、4-3 的走得到判定。
- 走訪的自動測試加上訊號路由的案例。
- 實際打包的執行檔跑一次：從不同的畫面送出同一個訊號都到得了目標，轉場中送出不會漏掉，不可返回的設定有效。

**需要人工確認的項目：** 藍圖視窗的「任何畫面」節點（顯示、拖線、右欄編輯）與 `OpenPopup` 路由的「返回歷史」選單。

# 第五批規格：藍圖視窗的操作體驗

完成後版本為 0.6.0。交付時升版號，但不要打標籤，等審核通過再打。原本五項，2026-11-21 追加三項（5-6～5-8，見本節最後）。

**狀態：** 已實作，待審核。分支 `feat/batch-5` 已快轉合併回 `main`，`package.json` 是 0.6.0，還沒有打標籤。各項的 commit、測試結果、與規格不同的地方、沒有驗證的部分與人工確認清單，見 `CODEX_REVIEW.md` 的「第五批（5-1～5-5）」驗證紀錄。

- 請依編號順序實作，每完成一項就 commit 一次。
- 驗收的共同條件與前幾批相同：既有測試全數通過、每一項補上測試、文件與 `CHANGELOG.md` 同步更新。
- 這一批只改 Editor 的藍圖視窗（5-5 除外），`Runtime` 不變。藍圖的資料格式不變，既有 Scene 不需要轉換。
- 下面的名稱、數值與版面是建議。實作時若發現更好的做法可以調整，但請在交付說明列出與規格不同的地方。

**目標：** 使用者（2026-11-21）實際操作藍圖視窗後提出的問題：不能縮放、不能拖曳畫面、畫布太小、只有點到節點的名稱才有反應、右欄太簡陋而且沒有分區，容易看錯。這一批把藍圖做成一般節點編輯器該有的操作方式。

## 5-1. 畫布的平移與縮放

- **縮放：** 滾輪以游標的位置為中心縮放，範圍 25% 到 200%，起始 100%。縮放之後游標底下的那一點不可以移動。
- **平移：** 按住滑鼠中鍵拖曳。另外支援 Alt 加左鍵拖曳，給沒有中鍵的裝置用。
- **畫布沒有邊界。** 拿掉目前的捲軸與「座標不可小於 12」的限制，節點可以放在任何位置，包含負的座標。
- **背景格線**跟著平移與縮放移動，讓使用者感覺得到畫布在動。
- **工具列新增：**
  - 「顯示全部」：調整平移與縮放，讓所有節點都在視窗裡。快捷鍵 F（焦點在畫布上時）。
  - 目前的縮放比例，點一下回到 100%。
- **視角的保存：** 每個 Scene 的平移與縮放在這個 Editor 工作階段內記住，重新整理視窗、進出 Play Mode、重新載入 Domain 之後都不變。不要寫進 Scene，否則只是看一下就會弄髒 Scene。這個工作階段第一次開啟某個 Scene 的藍圖時自動「顯示全部」。
- 縮放之後，拖曳節點、拖線、線上的標籤、連接點的判定都要正確。節點移動的距離要換算成畫布的座標。

**驗收：** EditMode 測試。

- 以某一點為中心縮放之後，那一點在畫布上的座標不變；縮放比例被夾在範圍內。
- 平移之後節點在視窗裡的位置跟著移動，節點儲存的座標不變。
- 縮放是 50% 與 200% 時拖曳節點，儲存的座標變化量等於滑鼠移動量除以縮放比例。
- 縮放不是 100% 時，從連接點拖線到另一個節點可以建立路由。
- 「顯示全部」之後所有節點都在可視範圍內。
- 重建視窗之後視角不變；Scene 沒有被標成未儲存。

## 5-2. 節點的範圍框

- 用虛線畫一個矩形，框住目前所有的節點，包含「任何畫面」節點，四周留邊距。
- 節點移動、新增、刪除時，範圍框即時更新。
- 範圍框的左上角標示 Scene 的名稱與數量，例如「Workshop · 3 個頁面 · 2 個 Popup」。
- 範圍框只是標示，不接收滑鼠事件，不擋住底下的操作。

**驗收：** EditMode 測試確認範圍框的矩形包住每一個節點加上邊距，節點移動後矩形跟著更新，標示的數量正確，而且它不接收滑鼠事件。

## 5-3. 節點的操作

- **整個節點都可以操作。** 點節點的任何位置都會選取它，按住拖曳就移動它，不再只有標題有反應。連接點照舊用來拖線。
- **滑鼠移到節點上要有回饋：** 節點變色或加框，游標換成移動的樣式；移到連接點上時換成拖線的樣式。
- **單擊只選取，雙擊才定位。** 目前單擊就會把 Scene 視窗移到那個畫面。整個節點都能點之後這樣會太常跳動，所以改成雙擊才定位到 Scene 裡的畫面。
- **點空白的地方取消選取**，右欄回到工作區的摘要。
- **線上的動作標籤可以點。** 點了之後選取來源節點，並在右欄選中這條路由。標籤要有滑鼠移上去的回饋。
- 節點上顯示的資訊要一眼看得懂：名稱、種類（頁面、Popup、訊號）用色塊或徽章區分，起始頁與「由程式控制」用明顯的標記，不要全部擠在同一行小字裡。

**驗收：** EditMode 測試。

- 在節點的標題、說明文字與空白處按下都會選取節點；在這三個位置拖曳都會移動節點。
- 在連接點按下不會移動節點。
- 單擊不會呼叫定位，雙擊會。
- 點空白處之後沒有選取的節點。
- 點動作標籤之後，選取的節點是路由的來源，右欄選中的是那條路由。

## 5-4. 右欄重新設計

**目標：** 右欄要分區塊，每一區有標題，一眼看得出現在在看什麼、可以做什麼。

- **右欄的寬度可以拖曳調整**，有最小寬度，內容太長時自己捲動。寬度在 Editor 工作階段內記住。
- **每個區塊有標題列與明顯的邊界**，區塊之間有間距。區塊可以收合，收合的狀態在工作階段內記住。
- **沒有選取任何節點時**顯示「工作區」區塊：Scene 名稱、模式、起始頁（可以修改）、頁面與 Popup 的數量、訊號路由的數量，以及一句操作提示（選取節點可以編輯、從連接點拖線可以建立路由）。
- **選取畫面時**依序顯示：

| 區塊 | 內容 |
|---|---|
| 畫面 | 名稱、ID、種類。是不是起始頁；頁面有「設為起始頁」的按鈕。「在 Scene 中定位」的按鈕。Popup 另有疊在指定頁面上預覽的功能（現有的功能搬過來）。 |
| 畫面設定 | 現有的畫面選項，例如由程式控制進出。每個選項要有一句說明或 tooltip。 |
| 從這裡出發的路由 | 清單。每一列是動作名稱、種類的徽章、目標畫面，整列可以點選，選中的那一列有明顯的標示。最下面是「新增路由」。沒有路由時顯示一句說明。 |
| 編輯路由 | 選了路由或按了新增才出現。動作、行為、目標、返回歷史、關閉結果，只顯示這個行為用得到的欄位。「儲存」與「刪除」分開放，刪除要和其他按鈕有明顯的區別。 |
| 通到這裡的路由 | 唯讀的清單，列出哪些畫面的哪個動作會到這個畫面，包含訊號路由。點一列會選取來源節點。沒有時顯示一句說明。 |

- **選取「任何畫面」節點時**顯示：說明這個節點用途的區塊、訊號路由的清單、編輯路由。沒有「畫面設定」與「通到這裡的路由」。
- **用字要一致：** 同一個東西在節點、線上的標籤與右欄用同一個詞。種類的徽章在三個地方用相同的顏色。
- **錯誤要顯示在出錯的欄位旁邊**，例如動作名稱重複、沒有選目標，不要只跳對話框。
- 現有的功能都要保留，只是重新安排位置。

**驗收：** EditMode 測試。

- 沒有選取、選取頁面、選取 Popup、選取「任何畫面」四種情況，各自出現的區塊與規格相同。
- 「從這裡出發的路由」與「通到這裡的路由」的內容和藍圖的資料一致，訊號路由有列在目標畫面的「通到這裡的路由」裡。
- 點「通到這裡的路由」的一列會選取來源節點。
- 編輯路由只顯示該行為用得到的欄位：`Back` 沒有目標，`ShowPage` 有三種返回歷史，`OpenPopup` 有兩種，`ClosePopup` 有關閉結果。
- 動作名稱重複或沒有選目標時不會儲存，錯誤訊息出現在右欄裡。
- 既有的藍圖測試全數通過（選項與儲存的行為不變）。

## 5-5. 不可返回的 Popup 沒有出口時要提醒

第四批留下的待辦。

- 會被 `history: Clear` 的 `OpenPopup` 路由開啟的 Popup（一般路由或訊號路由），如果沒有 `ClosePopup` 或 `ShowPage` 路由，也沒有設定點遮罩關閉，Validate 回報 `view.no-exit`。它的 `Back` 路由不算出口，因為不可返回的 Popup 開著時返回無效。
- 由程式控制的 Popup 不算。
- 訊息要說明原因：這個 Popup 會以不可返回的方式開啟。

**驗收：** EditMode 測試涵蓋會回報、有 `ClosePopup`、有 `ShowPage`、點遮罩關閉、由程式控制五種情況；同一個 Popup 只被 `Push` 的路由開啟時不回報。

## 文件與人工確認

- AgentGuide「審核」一節更新藍圖的操作方式：縮放、平移、顯示全部、整個節點可以拖曳、雙擊定位、點標籤選路由、右欄的區塊。
- `README.md` 與 `CHANGELOG.md` 同步更新。
- **藍圖視窗的外觀與手感沒辦法用自動測試確認。** 請在交付說明列出人工確認的清單，至少包含：滾輪縮放是否以游標為中心、中鍵拖曳、範圍框的樣子、節點的滑鼠回饋、雙擊定位、右欄各區塊在四種選取狀態下的版面、右欄寬度的拖曳、縮放到 25% 與 200% 時文字是否還讀得清楚。

## 第五批追加（5-6～5-8）

2026-11-21 追加，版本維持 0.6.0（`v0.6.0` 還沒打，使用者要在這三項完成後一起確認外觀）。起因有兩個：

- 使用者：藍圖在 Play Mode 不顯示，但執行中看得到「現在在哪個節點」才是這個工具的意義。
- 使用端專案（MaidProject）的 agent 把日循環的每一次換頁都做成「任何畫面」的訊號路由，並把所有頁面標成由程式控制。功能都正常，Validate 沒有問題、走訪通過，但藍圖只看得到所有線從「任何畫面」放射出去，看不出「標題 → 開店前 → 營業中 → 打烊後 → 日終結算」的流程。那個 agent 寫了檢討文件，結論是套件與使用端兩邊都有原因：
  - 使用端用錯了判斷標準：問的應該是「這件事在哪個畫面發生」，而不是「去處由誰決定」。
  - 套件端讓錯的做法比較省事：指南裡頁面自己的路由的範例（`HudPage`）帶著 40 行的重試迴圈，訊號只要一行而且「不會 Busy」；指南沒有「按鈕 → 遊戲邏輯 → 依結果走這一頁的其中一條路由」的範例；「由程式控制」的標記會連「沒有出口」的警告一起壓掉，Validate 因此沒有機會提醒。

請依 5-6、5-7、5-8 的順序實作，每完成一項就 commit 一次。

## 5-6. 執行中的藍圖

**目標：** Play Mode 時藍圖照常顯示，即時標示目前在哪個畫面，並看得到剛剛走了哪條路由。編輯功能關閉。

**顯示**

- 進入 Play Mode 後顯示同一張藍圖，使用執行中那個 Scene 的工作區。視角（平移與縮放）沿用 Edit Mode 的，離開 Play Mode 後也不變。
- **目前的頁面**：節點有明顯的強調（例如亮色粗框加「目前」的徽章），一眼看得出來。
- **開著的 Popup**：同樣強調，並標出疊放的順序（例如「Popup 1」、「Popup 2」）。最上層、目前可以操作的那一個要和其他區分開來。
- **轉場中**：正在進出的畫面有不同的標示，轉場結束後消失。
- **剛走過的路由**：那條線與線上的標籤亮起來，約 2 秒內淡回原本的樣子。訊號路由也一樣，從「任何畫面」節點亮起。
- 不是透過路由的導航（程式直接呼叫 `ShowPageAsync`、`OpenPopupAsync`、`BackAsync` 等）不亮線，只更新節點的強調。
- 狀態改變時才重畫，不要每個影格都重建畫面。
- 工作區在 Play Mode 中被停用或銷毀（例如換 Scene）時，藍圖顯示說明；載入的新 Scene 有工作區時切換過去。

**操作**

- 可以平移、縮放、顯示全部、選取節點、點線上的標籤。
- **不能修改藍圖**：不能拖曳節點、不能拖線、右欄不能編輯。Play Mode 中的修改離開時會被 Unity 還原，開放編輯只會讓人以為改到了。
- 雙擊節點：在 Hierarchy 選取執行中的那個畫面物件（畫面還沒建立時，例如沒開過的 Popup，就不做事）。

**右欄**

- 「執行中」區塊放在最上面：目前的頁面、開著的 Popup（由下到上）、返回歷史（`HistoryCount` 筆，由近到遠列出畫面）、是否轉場中、輸入模式（滑鼠或導覽）。
- 「最近的導航」區塊：最近 20 筆，新的在上面。每一筆有時間、來源與動作（訊號的來源是「任何畫面」；程式直接呼叫的標成「程式」）、目的地與結果。結果不是 `Completed` 的用醒目的顏色。點一筆會選取它的路由。
- 選取節點時，原本的區塊照常顯示但都是唯讀。「從這裡出發的路由」的每一列多一個「觸發」按鈕：
  - 一般路由：來源是目前最上層的畫面時才能按，呼叫 `TriggerAsync`。
  - 訊號路由：隨時可以按，呼叫 `SignalAsync`，不帶資料。
  - 結果寫進「最近的導航」。這是給使用者測試流程用的，不需要的資料不傳，所以需要資料的畫面會顯示失敗，這是預期中的。
- 走訪進行中時，藍圖照常即時顯示，但「觸發」按鈕停用，以免干擾走訪。

**實作的限制**

- 執行中的資料只能透過 `UIRoot` 現有的公開成員與事件取得（`CurrentPage`、`Popups`、`HistoryCount`、`IsTransitioning`、`InputMode`、`ViewShowing`／`ViewShown`／`ViewHiding`／`ViewHidden`、`ActionTriggered`、`ActionCompleted`）。需要新增時，只能加 `internal` 成員給 Editor 組件用，不要新增公開 API。
- 返回歷史的內容目前沒有公開的讀取方式，可以加一個 `internal` 的唯讀清單。
- 訂閱的事件在離開 Play Mode、換工作區、關閉視窗時都要取消。
- 不可以影響執行期的行為與效能：藍圖沒有開著時不訂閱任何東西。

**驗收**

- EditMode 測試：把「執行中的狀態」轉成節點標示與右欄內容的邏輯寫成可以直接測的函式。目前的頁面、疊了兩個 Popup、轉場中、返回歷史三筆、最近的導航超過 20 筆時只留 20 筆、結果不是 `Completed` 的標示。
- PlayMode 測試：實際進入 Play Mode、開著藍圖視窗，切頁、開 Popup、送訊號之後，視窗記錄的目前頁面、Popup、最近的導航都正確；按「觸發」按鈕會走那條路由；離開 Play Mode 後回到可編輯的狀態，事件都取消訂閱了。
- 這一項的重點是外觀，請在人工確認清單加上：目前頁面的強調是否一眼看得出來、剛走過的線亮起再淡掉、Popup 疊放的順序、右欄「執行中」與「最近的導航」好不好讀、觸發按鈕。

## 5-7. 訊號路由的線另外處理

**目標：** 「任何畫面」節點拉出去的線很多時，不要蓋過畫面之間的流程。

- 訊號路由的線用和一般路由不同的樣式：虛線、比較淡，顏色和「任何畫面」節點一致。滑鼠移到線或標籤上、或選取「任何畫面」節點時才變清楚。
- 工具列新增「顯示訊號路由」的開關，預設開啟，狀態在 Editor 工作階段內記住。關閉時訊號路由的線與標籤都不畫，「任何畫面」節點照常顯示，並在節點上標出它有幾條路由。
- 5-6 的「剛走過的路由」亮起時，訊號路由即使被關閉也要短暫畫出來。

**驗收：** EditMode 測試確認兩種線的樣式不同、開關關閉時沒有畫訊號路由的線與標籤、開關的狀態在重建視窗之後保留。外觀列入人工確認。

## 5-8. 頁面自己的流程

**目標：** 「在這一頁發生、依結果去不同地方」是遊戲最常見的流程，它的標準寫法要比訊號更省事，畫出來的藍圖看得出遊玩順序。用錯的時候 Validate 要提醒。

**新的 API：頁面腳本走自己的路由**

```csharp
// UIViewBehaviour
protected Awaitable<UINavResult> NavigateAsync(string action, CancellationToken cancellation = default);

// UIRoot
public Awaitable<UINavResult> TriggerWhenIdleAsync(string source, string action, CancellationToken cancellation = default);
```

- `NavigateAsync(action)` 等於 `Root.TriggerWhenIdleAsync(View.Id, action, cancellation)`。
- `TriggerWhenIdleAsync` 和 `SignalAsync` 一樣**不會因為忙碌而被拒絕**：有導航正在進行時等它結束，和訊號共用同一個排隊順序。輪到它的時候：
  - 來源是最上層的畫面：照 `TriggerAsync` 執行，回傳結果。
  - 來源還在畫面上，但上面蓋著 Popup：回傳 `Unavailable` 並記錄警告，說明是被哪個 Popup 蓋住。要先關 Popup 還是放棄，由遊戲決定。
  - 來源已經不在畫面上（等待期間切走了）：安靜地回傳 `Canceled`。這件事已經不必處理。
  - 找不到路由：`NotFound`，和 `TriggerAsync` 相同。
- 等待期間 token 被取消、工作區被停用或銷毀：`Canceled`。
- `TriggerAsync` 本身不變，照舊立即回傳 `Busy`。按鈕的 `UIButtonAction` 也不變。

**Validate：「由程式控制」不再豁免頁面的出口**

修改已定案的決定「由程式控制的畫面」：標記只代表「進來的方式不靠路由」，不代表「離開的方式不靠路由」。

| 檢查 | 頁面 | Popup |
|---|---|---|
| `view.unreachable` | 標記之後不檢查（不變） | 標記之後不檢查（不變） |
| `view.no-exit` | **標記之後照樣檢查**（新） | 標記之後不檢查（不變）。確認框這類 Popup 是自己用 `View.Close()` 關閉的 |
| `button.unbound` | 標記之後不檢查（不變） | 標記之後不檢查（不變） |

- 頁面的 `view.no-exit` 訊息要提示：「由程式控制只表示進來的方式。這一頁發生的換頁請畫成這一頁的路由，用 `NavigateAsync` 依結果挑其中一條。」
- 工作區只有一個頁面時照舊不回報。
- 走訪不受影響。

**新的 Warning：`start.navigated-by-code`**

- 起始頁標記為由程式控制時回報。起始頁是框架在 `Start` 時顯示的，不帶資料，不需要這個標記；它常是「把所有頁面都標記」的跡象。

**AgentGuide**

- 「由遊戲事件觸發的跳轉」一節改寫成「頁面之間的流程」，開頭是一張判斷表，**判斷標準是「這件事在哪裡發生」，不是「去處由誰決定」**：

| 這件事 | 畫在哪裡 | 怎麼觸發 |
|---|---|---|
| 在某一頁按了按鈕，依遊戲邏輯的結果去不同地方（「進入下一天」可能到開店前、週結或結局） | **那一頁自己的路由**，每個可能的去處各一條 | 按鈕用 `Emit`，`OnAction` 跑完遊戲邏輯之後 `await NavigateAsync(依結果挑的動作)` |
| 只在某一頁才會發生的遊戲事件（HUD 上的角色死亡、這一頁的計時器） | 那一頁自己的路由 | 頁面腳本 `await NavigateAsync(動作)` |
| 任何頁面都可能發生的事（挑戰者連線、斷線、讀檔後跳到存檔當時的畫面） | 「任何畫面」的訊號路由 | `root.SignalAsync(動作, 資料)` |

- 新增第一列的範本，實際編譯並在執行檔跑過：日終結算頁的「進入下一天」，`OnAction` 呼叫遊戲邏輯，依結果走 `toMorning`、`toWeeklyReview`、`toEnding` 三條路由的其中一條，三條都是 `history: Clear`。附上對應的計畫片段。
- `HudPage` 範本改用 `NavigateAsync`，重試迴圈拿掉，只保留「上面蓋著 Popup」的處理。
- 寫明反例：不要把每一頁的換頁都做成訊號、把所有頁面標成由程式控制。功能上能動，但藍圖看不出流程，走訪也驗證不到順序。
- 「由程式控制的畫面」一節：定義改成「進來的方式不靠路由」，例子只留確認框、需要資料才能顯示的畫面（由訊號帶資料開啟的挑戰者登場）。拿掉「載入畫面、過場畫面」這兩個例子；它們離開時應該走自己的路由。補一句：「去處由遊戲狀態決定，不等於不靠路由。」
- 「導航、返回與 Popup」一節補上 `TriggerWhenIdleAsync` 與 `NavigateAsync`，和 `TriggerAsync`、`SignalAsync` 的差別列成一張表。

**驗收**

- PlayMode 測試：
  - `NavigateAsync` 在 `OnAction` 裡呼叫，走到路由的目的地。
  - 轉場進行中呼叫：等轉場結束後執行，回傳 `Completed`。和訊號一起排隊時依送出的順序執行。
  - 等待期間來源頁被切走：`Canceled`，沒有警告。
  - 來源頁被 Popup 蓋住：`Unavailable`，有警告並指出是哪個 Popup。
  - 找不到路由、token 取消、停用工作區。
  - `TriggerAsync` 遇到忙碌仍然回傳 `Busy`。
- EditMode 測試：由程式控制的頁面沒有出口時回報 `view.no-exit`，有 `ShowPage` 路由時不回報；由程式控制的 Popup 沒有出口時不回報；`start.navigated-by-code`；問題代碼表與程式一致。
- 指南的新範本在副本專案編譯，執行檔裡依三種結果各走一次，走訪也通過（三條路由都被走到）。
- 操作指南第一個示例計畫（`Workshop.unity`）的 Validate 結果不變。

## 第五批追加的文件

- 5-6、5-7 的藍圖操作寫進 AgentGuide「審核」一節；5-8 照上面的說明。
- `CHANGELOG.md` 的 0.6.0 補記三項。`README.md` 的簡介補上「執行中的藍圖」。
- 人工確認清單併入第五批原本的清單，交付時一份就好。
