# UniGameUI 審核結果（給實作者）

- 審核者：Claude
- 第一輪：2026-11-19，commit `020b424`（H1、H2 由 Claude 修正於 `e8062f8`）
- 第二輪：2026-11-19，commit `0cabfec`（codex 處理第一輪待辦）
- 第一批改進：2026-11-20，`v0.2.0`
- 第三輪：2026-11-20，commit `058ac1a`（Claude 審核第一批）
- 第二批改進：2026-11-20，commit `487f4b6`（0.3.0，尚未打標籤）
- 第四輪：2026-11-20，commit `487f4b6`（Claude 審核第二批）
- 第二批修正：2026-11-20，commit `45b7e0b`
- 第五輪：2026-11-20，commit `45b7e0b`（Claude 複驗第二批修正）
- 環境：Unity 6000.3.19f1、uGUI 2.0；在獨立的暫存專案驗證，有無 DOTween 都測過

**現況：** 第一批與第二批都已通過獨立審核。第二批的 G1～G5 已修正，Claude 複驗後確認全部符合，**0.3.0 可以打 `v0.3.0` 標籤**。複驗結果見「第五輪審核」，剩下的互動操作見「未驗證」。第三批（0.4.0）的詳細規格已寫進 `ROADMAP.md`，待實作。

**分工：** 2026-11-20 起，實作改由 Claude 的子 session 負責，Claude 主 session 負責寫規格與驗收。在這之前的實作者是 codex，這份文件裡提到 codex 的地方是當時的紀錄。

原本標示「需使用者決定」的項目已在 2026-11-20 定案，排進 `ROADMAP.md` 的四批工作裡。要做哪一項、怎麼做，以 `ROADMAP.md` 為準；這份文件保留各項目的問題說明。

處理完任何項目後，請更新下方的狀態表。

## 狀態表

| 項目 | 內容 | 狀態 |
|---|---|---|
| H1 | `updateExisting` 蓋掉人工調整 | 已修正、已驗證 |
| H2 | 沒有 DOTween 時導航失敗 | 已修正、已驗證 |
| M1 | 藍圖無法改路由名稱 | 已修正、已驗證 |
| M2 | Prefab 覆寫未套用時進 Play 沒提示 | 已修正；邏輯已驗證，對話框本身未驗證 |
| M3 | 審核複本被打包進 build | 已修正、已用實際打包驗證 |
| M4 | `UINavResult.Unavailable` 包太多情況 | 已修正，PlayMode 已驗證回傳值與 Console 診斷 |
| M5 | 遊戲端 API 不足 | 已補 View 查詢、歷史、Popup 結果及全域事件，PlayMode 已驗證 |
| L1 | 強制 Overlay、無 Safe Area、無返回鍵 | 返回鍵排在第三批；Overlay 以外的模式與 Safe Area 不做 |
| L2 | 藍圖視窗不跟隨 Scene 切換 | 已修正、已驗證 |
| L3 | 切頁只能依序播放 | 已修正（新增 `Concurrent` 模式）、已驗證 |
| L4 | 沒有縮放預設動畫 | 已修正（新增 `Zoom`）、已驗證 |
| L5 | TMP 資源匯入只有自製解析 | 已修正；互動模式的新路徑未驗證 |
| L6 | 對 Unity 物件用 `??` | 已修正剩餘的 SceneView 判斷，EditMode 已驗證 |
| L7 | 沒有測試；顯示完成事件命名 | 已改為 `Shown`，舊欄位的持久事件已用 Scene 載入驗證 |
| 1-2 | DOTween 必裝 | 三種狀態與本機匯入均已驗證，提示對話框待人工確認 |
| DOTween 疑慮 | 轉場中停用 UIRoot 可能引發 Unity 錯誤 | 已排除（見驗證紀錄） |
| N1～N5 | 第二輪發現的小問題 | 已修正，外部安裝及自動測試通過，實際拖線與 Inspector 操作待人工確認 |
| P1 | 所有畫面共用一個 Canvas | 已新增子 Canvas 與修復工具，已驗證 Prefab 覆寫、raycast 與 Windows Player |
| P2 | Popup 每次開啟都重新建立 | 排在第三批 |
| P3 | 頁面不會釋放 | AgentGuide 已註明單 Scene 與 Prefab 頁面的保留方式 |
| U1 | `UIView` 不能繼承 | 已新增 `IUIViewHandler` 與 `UIViewBehaviour`，PlayMode 與 Windows Player 已驗證 |
| U2 | ID 全是字串 | 已自動產生常數，已驗證存檔、重產、名稱衝突與 Player 編譯 |
| F1 | 事件監聽丟例外會讓導航倒回，可能把玩家困在畫面上 | 已按階段修正，PlayMode 已驗證；Popup 隱藏後的接收者出錯仍可關閉 |
| F2 | 放在 asmdef 裡的遊戲程式引用不到 ID 常數 | 已產生獨立 asmdef，重跑保留現有檔案，已用獨立遊戲組件編譯 |
| F3 | 元件 ID 與所屬畫面 ID 相同時會被擋下 | 已改用 `View` 後綴，已驗證相同 ID 能生成、編譯並在 Player 查找 |
| F4 | `Failed` 記兩筆錯誤，原始例外的堆疊點不到 | 已直接記錄原始例外，PlayMode 與 Windows Player 都取得原始方法堆疊 |
| F5 | 測試跑完留下空資料夾 | 已清理測試建立的空生成目錄與 .meta，已檢查隔離副本 |
| F6 | 套用計畫後會重新編譯，文件沒有提醒 | AgentGuide 的 JSON 與常數段落已補編譯時機，範本依此流程通過編譯 |
| G1 | 用 `Replace` 回到歷史最上面那一頁後按返回，畫面變成空白 | 已修正。依序和並行模式都通過 PlayMode，返回後目前頁仍可操作。Claude 已用原本的重現步驟在 PlayMode 與 Windows Player 複驗 |
| G2 | 在生命週期方法裡導航，會安靜地被拒絕 | 已修正。通知期間的 `Busy` 會記錄上下文，`OnShown` 的指南範本已編譯並在下一影格切頁。Claude 已複驗警告內容，以及轉場中的一般 `Busy` 仍然安靜 |
| G3 | 事件派送依賴 Unity 的非公開方法，失效時沒有退路 | 已加入公開 `Invoke()` 備援。正常反射與模擬方法缺失的路徑都通過 PlayMode。Claude 另外改掉方法名稱實際模擬，導航全部正常；IL2CPP 仍未驗證 |
| G4 | AgentGuide 要補的說明 | 已補齊五點。另驗證瞬間切換時 `OnShown` 可早於 `Start`。Claude 已核對內容 |
| G5 | 零碎項目（計畫欄位寫錯位置沒有提示、訊息用字） | 已修正。JSON 預檢會定位不適用的欄位，通知例外與導航失敗使用不同訊息。Claude 已在 log 確認兩種訊息 |
| I1 | 第五輪的小提醒（測試依賴靜態旗標、指南一句話不好讀） | 不影響通過，併入第三批的第一項 |

## 第五輪審核：第二批修正（0.3.0）

G1～G5 都已修正，做法符合 `ROADMAP.md`「第二批修正」的規則，沒有發現新的問題。四組自動測試的數量與結果和 codex 的紀錄完全相同，見「驗證紀錄」。**第二批到此通過，可以打 `v0.3.0` 標籤。**

各項的確認方式：

- **G1：** 用第四輪原本的重現步驟重跑。`Replace` 回到主畫面之後歷史是 0，返回安靜地回傳 `Unavailable`，目前頁仍然啟用而且可以操作。`Sequential`、`Concurrent` 和 Windows Player 的結果相同。`ChangePageAsync` 另外擋下「對同一個 View 切頁」。
- **G2：** 在 `OnShown` 裡直接呼叫 `ShowPageAsync`、`OpenPopupForResultAsync`、`View.Close` 都回傳 `Busy`，並記錄警告，例如 `ShowPage 'workshop' 目前導航 ShowPage 'settings' 正在通知 Shown（畫面 'settings'）。請等這次導航結束後再呼叫。`。等一個影格之後再呼叫會成功。轉場動畫播放中的 `Busy` 沒有多出警告。
- **G3：** 把副本裡要反射的方法名稱改掉，模擬 Unity 改版後找不到的情況。十二個探測的導航全部正常，警告只出現一次；丟例外的監聽之後的那個監聽沒有被呼叫，和設計一致。還原後反射路徑恢復，後面的監聽照常被呼叫。
- **G4：** 五點都已寫進 AgentGuide。codex 另外發現「沒有進場動畫時，`OnShown` 可能比 `Start` 早」，寫得比第四輪的描述更準確。
- **G5：** 錯位的 `history`、`result` 由 codex 的 EditMode 測試覆蓋（三個案例都通過）。訊息用字在 log 中確認：不影響導航的例外寫「接收者丟出例外，導航照常處理」，顯示前出錯寫「執行失敗」。

### I1. 小提醒（不影響通過）

以下兩點都很小，併入第三批的第一項處理即可。

- **測試依賴靜態旗標。** `UnityEventFallbackWarnsOnceAndPreservesNavigationExceptionPolicy` 預期會看到「只警告一次」的那筆警告，而這個旗標是靜態欄位。專案若關閉進入 Play Mode 時的 Domain Reload，同一個 Editor 工作階段第二次跑這個測試時旗標還留著，測試會失敗。目前這個 repo 沒有關閉 Domain Reload，所以沒有影響。這一點是從程式碼判讀的，沒有實測。建議測試開始前重設旗標。
- **AgentGuide 有一句不好讀。** 「原畫面如果已收到 `OnHiding`，這次只會收到這個階段的通知。」建議改成直接說明：之後不會收到 `OnHidden`，也不會再收到一次 `OnShowing`。

## 第四輪審核：第二批（0.3.0）

七項規格都已實作，逐項對照後與規格相符，沒有發現規格以外的公開 API 變動。F1～F6 全部確認修正。自動測試的數量與結果和 codex 的紀錄完全相同，見「驗證紀錄」。

審核時發現一個會把玩家困住的問題（G1），**建議修正後再打 `v0.3.0` 標籤**。其餘項目比較小，在同一輪一起處理；G2 的做法已由使用者決定。G1、G2、G3 的根源都是 `ROADMAP.md` 第二批規格沒有寫到那個情況，不是實作偏離規格。

### G1. 用 `Replace` 回到歷史最上面那一頁之後按返回，畫面會變成空白

- **嚴重度：** 高。已在 PlayMode（`Sequential` 與 `Concurrent` 兩種模式）和 Windows Player 重現。
- **位置：** `Runtime/UIRoot.cs` 的 `BackCoreAsync` 與 `ShowPageCoreAsync`。
- **重現：** 從 `workshop` 用 `Push` 切到 `settings`，再用 `Replace` 切回 `workshop`（例如設定頁的「回主畫面」按鈕），然後按返回。
- **結果：**
  - 第二步之後，歷史仍是 `[workshop]`，而目前頁也是 `workshop`。
  - 返回時，`ChangePageAsync` 把同一個 View 同時當成要隱藏和要顯示的頁面，最後 `FinishHiding` 把它關掉。
  - `BackAsync` 回傳 `Completed`，`CurrentPageId` 是 `workshop`，但該頁的 GameObject 沒有啟用，畫面上沒有任何東西。
  - 之後 `ShowPageAsync("workshop")` 回傳 `Unavailable`（已在該頁）。玩家沒有按鈕可按，離不開這個狀態。
- **建議：**
  - 切頁成功後，移除歷史尾端與新的目前頁相同的項目。這樣上面的例子在第二步之後歷史是空的，返回會安靜地回傳 `Unavailable`。
  - `BackCoreAsync` 另外加一道防守：歷史最上面那一頁就是目前頁時，丟掉它再往下找。
- **驗收：**
  - PlayMode 測試涵蓋上述步驟，兩種切頁模式都要測。確認 `HistoryCount`、返回的結果，以及目前頁仍然啟用而且可以操作。
  - 另外測：歷史是 `[A, B]`、目前在 C，用 `Replace` 切到 B 之後，返回會回到 A。

### G2. 在生命週期方法裡導航，會安靜地被拒絕

- **嚴重度：** 中。已在 PlayMode 確認。
- **現況：** `OnShowing`、`OnShown`、`OnHiding`、`OnHidden`，以及對應的 UnityEvent 和全域事件，都在這次導航還沒結束時被呼叫。這時呼叫 `Root.ShowPageAsync`、`OpenPopupAsync`、`OpenPopupForResultAsync` 或 `View.Close`，會回傳 `Busy`，而 `Busy` 不會記錄任何訊息。
- **問題：**
  - 「主選單顯示後立刻跳出每日獎勵」、「啟動畫面顯示後自動進主選單」這類寫法很自然。agent 寫出來之後不會看到任何錯誤，只是沒有反應。
  - `OpenPopupForResultAsync` 的情況更隱蔽：`GetValueOrDefault()` 會回傳預設值，程式會把它當成使用者選了「否」。
- **不受影響的情況：** `OnAction`，以及等待 Popup 結果之後的程式。兩者都已實測可以立刻導航。
- **做法（使用者已在 2026-11-20 決定採用）：**
  - 維持回傳 `Busy`，但在通知進行中被拒絕時用 `LogWarning` 記錄。訊息包含被拒絕的請求、正在通知的階段與畫面 ID，並提示做法。其他情況的 `Busy`（例如轉場中連點按鈕）維持不記錄。
  - AgentGuide 寫明標準寫法：在 `OnShown` 裡先等一個影格（`await Awaitable.NextFrameAsync()`）再導航。
- **沒有採用的做法：** 讓通知期間提出的請求延到這次導航結束後自動執行。寫起來比較順，但框架要多處理請求的先後順序，等實際專案有需要再考慮。完整規則見 `ROADMAP.md` 的「第二批修正」。
- **驗收：** PlayMode 測試確認警告的內容，以及轉場中的一般 `Busy` 仍然不記錄；AgentGuide 的範例實際編譯並執行過。

### G3. 事件派送依賴 Unity 的非公開方法，失效時沒有退路

- **嚴重度：** 低。目前的 Unity 版本沒有問題。
- **位置：** `Runtime/UIEventDispatch.cs`。
- **現況：** 為了讓同一個 UnityEvent 裡的監聽互不影響，用反射呼叫 `UnityEventBase.PrepareInvoke` 與 `BaseInvokableCall.Invoke(object[])`。原因是規格要求「同一階段的接收者，其中一個出錯不影響其他接收者」，而 UnityEvent 的公開 API 做不到這件事。
- **已驗證：** Editor，以及 Mono 加上 High Managed Stripping 的 Windows Player 都正常，原始例外的堆疊也有保留。
- **風險：**
  - 這兩個方法不是公開 API，Unity 改版時可能改名或移除。
  - IL2CPP 的 Player 沒有驗證過。Claude 的電腦沒有安裝 6000.3.19f1 的 IL2CPP 模組。
  - 一旦找不到方法，`UIEventDispatch` 的靜態初始化會丟出例外。之後每一次顯示畫面都會回傳 `Failed`，Console 只看得到 `TypeInitializationException`，整個框架無法導航。這一點是從程式碼判讀的，沒有實測。
- **建議：**
  - 找不到方法時，退回一般的 `Invoke()` 並用 try/catch 包住，同時警告一次。退回後的差別只有一個：同一個 UnityEvent 裡，出錯的監聽之後的監聽不會被呼叫。
  - 加一個測試確認反射路徑在目前的 Unity 版本有效。日後升級 Unity 時，這個測試會先失敗，不必等到執行期才發現。

### G4. AgentGuide 要補的說明

以下行為都已實測。agent 寫頁面腳本時容易踩到，請補進「遊戲程式」章節。

- **`Start()` 比第一次 `OnShowing` 晚執行。** 頁面第一次顯示時的順序是 `Awake`、`OnShowing`、`Start`、`OnShown`。初始化要寫在 `Awake`，或直接在 `OnShowing` 裡用 `View.Get<T>()` 取元件，不要寫在 `Start`。
- **`OnHiding` 之後不一定有 `OnHidden`。** 導航失敗或被取消時，原畫面會還原並繼續顯示。它已經收到 `OnHiding`，之後不會收到 `OnHidden`，也不會再收到一次 `OnShowing`。會改變狀態的收尾工作要寫在 `OnHidden`。
- **停用的接收者元件仍然會收到通知。** 把元件的 `enabled` 設成 false，不會讓它停止接收生命週期方法。
- **`ActionTriggered` 表示路由被接受，不代表畫面一定會變。** 例如沒有上一頁時觸發「返回」類的路由，`ActionTriggered` 仍會觸發，接著 `ActionCompleted` 帶著 `Unavailable`。
- **`ActionCompleted` 的時機。** 關閉 Popup 的路由觸發 `ActionCompleted` 時，等待該 Popup 結果的程式已經先繼續執行，可能已經開始下一次導航。

### G5. 零碎項目

- **JSON 計畫的欄位寫錯位置時沒有提示。**
  - `history` 寫在 `ShowPage` 以外的路由、`result` 寫在 `ClosePopup` 以外的路由，都會被安靜地忽略；這時連值寫錯也不會報錯。
  - 位置是 `Editor/UIPlan.cs` 的 `ParseHistory` 與 `Editor/UIAuthoring.cs` 的 `SetRouteOptions`。
  - 建議在計畫的檢查階段列為錯誤。這一項是從程式碼判讀的，沒有實測。
- **不影響導航的例外，訊息仍寫「執行失敗」。**
  - 例如 `ShowPage 'next' Hiding 'home' 執行失敗。`，實際上切頁已經完成。從 Console 判讀的 agent 可能以為導航失敗。
  - 建議「顯示後」、「隱藏前」、「隱藏後」三個階段改用不同的文字，例如「接收者丟出例外，導航照常完成」。

## 第三輪審核：第一批（`v0.2.0`）

六項規格都已實作，逐項對照的結果見「驗證紀錄」。以下是審核時發現的後續項目。F1、F2、F3 的根源是 `ROADMAP.md` 第一批規格寫得不夠周全，不是實作錯誤。

### F1. 事件監聽丟例外會讓導航倒回，可能把玩家困在畫面上

使用者已在 2026-11-20 決定採用下面的建議。

- **位置：** `Runtime/UIRoot.cs` 的 `ChangePageAsync`、`OpenPopupCoreAsync`、`ClosePopupCoreAsync`；`Runtime/UIView.cs` 移除了原本包住事件的 try/catch。
- **現況：** `Showing`、`Shown`、`Hiding`、`Hidden` 任何一個監聽丟出例外，導航都會倒回原狀並回傳 `Failed`。這是照規格「事件監聽丟出例外 → `Failed`」做的。
- **問題：** 離開畫面時的監聽一旦有 bug，玩家就離不開那個畫面。已實測：
  - Popup 的 `Hidden` 監聽丟例外：關閉兩次都回傳 `Failed`，Popup 一直開著。
  - 頁面的 `Hiding` 監聽丟例外：切頁兩次都回傳 `Failed`，停在原頁。
  - 頁面的 `Shown` 監聽丟例外：進場動畫播完後又被倒回上一頁。
- **建議：**
  - `Showing` 丟例外：取消這次導航，留在原畫面，回傳 `Failed`。新畫面沒準備好就不該顯示，而且留在原畫面是安全的。
  - `Shown`、`Hiding`、`Hidden` 丟例外：用 `LogException` 記錄（附上操作與 ID），導航照常完成並回傳 `Completed`。
  - `UIRoot.ActionEmitted` 的監聽丟例外：維持回傳 `Failed`，它不涉及畫面狀態。
- **驗收：** PlayMode 測試涵蓋上述三種情況；`Hidden` 監聽有 bug 的 Popup 仍然關得掉。

### F2. 放在 asmdef 裡的遊戲程式引用不到 ID 常數

- **位置：** `Editor/UIIdConstants.cs`，輸出到 `Assets/UniGameUI/Generated`。
- **問題：** 這個資料夾沒有 asmdef，產生的類別會編進 `Assembly-CSharp`。Unity 不允許 asmdef 組件引用 `Assembly-CSharp`，所以遊戲程式只要放在自己的 asmdef 裡，就用不到這些常數。agent 建專案時常會建立 asmdef。
- **建議：** 產生常數時，若資料夾裡沒有 asmdef，就一併建立 `UniGameUI.Generated.asmdef`（不需要任何引用）。AgentGuide 註明：遊戲程式的 asmdef 要引用它。
- **驗收：** EditMode 測試確認 asmdef 會被建立且不會被重複覆寫；另用一個帶 asmdef 的測試腳本確認能引用常數。

### F3. 元件 ID 與所屬畫面 ID 相同時會被擋下

- **位置：** `Editor/UIIdConstants.cs` 的 `CollectNames`，`name == owner` 的檢查。
- **問題：** `Elements` 底下用畫面名稱當類別名稱，而 C# 不允許成員和所在類別同名。結果像「`confirm` Popup 裡的 `confirm` 按鈕」、「`title` 頁的 `title` 文字」這種很自然的命名會變成 Validate 錯誤，整份 JSON 計畫都套不進去。這是從程式碼判讀的，沒有另外實測。
- **建議：** 分組類別加上後綴，例如 `WorkshopUI.Elements.ConfirmView.Confirm`。`Views`、`Actions` 兩組維持現狀。
- **驗收：** 元件 ID 與畫面 ID 相同時可以正常產生；`ROADMAP.md` 與 AgentGuide 的範例跟著更新。

### F4. `Failed` 記兩筆錯誤，原始例外的堆疊點不到

- **位置：** `Runtime/UIRoot.cs` 的 `Failed`。
- **問題：** 先 `LogError` 一次，再把原始例外的文字包進一個新的 `InvalidOperationException` 用 `LogException` 記一次。Console 會出現兩筆內容重複的錯誤，而且原始例外的堆疊變成純文字，不能點擊跳到出錯的那一行。
- **建議：** 保留一行帶上下文的 `LogError`，再對原始例外直接呼叫 `Debug.LogException(exception, this)`。

### F5. 測試跑完留下空資料夾

- **問題：** 套件的 EditMode 測試跑完後，專案裡會留下空的 `Assets/UniGameUI/Generated` 資料夾和對應的 `.meta`。在這個 repo 會變成未追蹤的檔案。
- **建議：** TearDown 時，如果資料夾是測試建立的而且已經清空，就一併刪除。

### F6. 套用計畫後會重新編譯，文件沒有提醒

- **問題：** `UIPlan.Apply` 成功後會寫入常數檔，Editor 接著重新編譯。在開著的 Editor 裡連續執行多個步驟的 agent，下一步可能被打斷。
- **建議：** 在 AgentGuide 的 JSON 計畫與 ID 常數段落註明：套用後要等編譯完成再繼續。

### 流程上的提醒

`v0.2.0` 的標籤是在審核之前打的。之後建議等審核通過再打標籤；這次的修正併入第二批（0.3.0），不另外重打 0.2.0。

## 第二輪發現的小問題

### N1. 藍圖拖線會用到過期的路由選取

- **位置：** `Editor/UIBlueprintWindow.cs` 的 `RegisterWireDrag`，`wireOriginalAction = selectedAction`。
- **問題：** `selectedAction` 在所選路由被 Undo 移除後不會清掉。這時從同一個節點拖線，會呼叫 `UpdateRoute` 去更新一條已不存在的路由，跳出「Unknown route」對話框，線也接不上。
- **重現：** 選節點 A，儲存一條新路由，按 Ctrl+Z，再從 A 的輸出端拖到另一個節點。
- **建議：** 拖線開始時確認該路由還在，不在就當成新增（`wireOriginalAction = null`）；或在 `Rebuild` 時清掉已失效的 `selectedAction`。
- **驗收：** 上述步驟會正常建立新路由。

### N2. 路由改名後，Prefab 本體的按鈕仍是舊名稱，Validate 卻沒有報錯

- **位置：** `Editor/UIValidation.cs`，按鈕綁定只檢查 `entry.Review`。
- **問題：** Popup 與 Prefab 模式的頁面，執行期用的是 `entry.Prefab`。`UpdateRoute` 改名後，新名稱只寫在審核實例的覆寫上；覆寫套用前，Prefab 本體的按鈕對不到任何路由，執行時按了沒反應。目前只有泛用的「有待套用覆寫」警告，而且在批次或 agent 流程中不會有進 Play 前的對話框。
- **建議：** 對使用 Prefab 的 entry，額外檢查 `entry.Prefab` 底下的 `UIButtonAction`。找不到對應路由時列為 Error，訊息指明要先套用覆寫。
- **驗收：** 改名後不套用，`UIValidation.Check` 會回報 Error；套用後消失。請補一個 EditMode 測試。

### N3. `UIRoot` Inspector 的「切頁動畫」欄位

- **位置：** `Editor/UIInspectors.cs`。
- **問題：** Undo 後欄位不會更新；在 Play Mode 改它會讓 `CheckRoot` 丟出例外。
- **建議：** 改用綁定 `pageTransitionMode` 的 `PropertyField`，或在 Play Mode 停用該欄位並監聽 Undo。

### N4. 測試在其他專案看不到

- **問題：** 套件的測試只有在「嵌入專案」時會自動出現在 Test Runner。之後用 git URL 或 disk 安裝到別的專案時，要在該專案 `manifest.json` 加 `"testables": ["com.chrono.unigameui"]` 才看得到。
- **建議：** 在 README 的「回歸測試」補上這段說明。
- **附註：** 測試 asmdef 用的是舊式的 `"optionalUnityReferences": ["TestAssemblies"]`，在 6000.3.19f1 實測可用。若要跟上目前做法，可改成明確引用 `UnityEngine.TestRunner`、`UnityEditor.TestRunner`、`nunit.framework.dll`，並加上 `UNITY_INCLUDE_TESTS`。

### N5. 零碎項目

- `Editor/UIAuthoring.cs` 的 `FocusReview` 還留著 `SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>()`，請改成明確的 null 判斷（L6 的遺漏）。
- `UIReviewBuildProcessor` 只在 Player build 時剝除。打包成 AssetBundle 或 Addressables 的 Scene 不會被剝除；執行期仍可正常運作，但請在 AgentGuide 註明。
- `UIReviewPlayMode` 的對話框寫「下列頁面」，實際上也包含 Popup，文字可改成「下列畫面」。

## 新增待辦：效能

以下三項來自讀程式碼的判斷，沒有用 Profiler 量測。

### P1. 所有畫面共用一個 Canvas

- **位置：** `Editor/UIAuthoring.cs` 的 `CreateView`；`Runtime/UIRoot.cs` 的 `CreateLayer`。
- **問題：** View 根節點只有 `RectTransform`、`UIView`、`CanvasGroup`，全部掛在 UIRoot 的同一個 Canvas 下。uGUI 只要 Canvas 內有元素變動，整個 Canvas 都要重新合批。影響包括：
  - Popup 播放動畫時，底下的頁面每一幀跟著重算。
  - `Concurrent` 模式下兩個頁面同時動，成本加倍。
  - 頁面上任何會動的元素（計時、數值）都會拖累同一 Canvas 的其他畫面。
- **建議：**
  - `CreateView` 在 View 根節點加上 `Canvas` 與 `GraphicRaycaster`（巢狀 Canvas 需要自己的 Raycaster 才收得到點擊）。
  - 既有工作區要能補上：`UIValidation` 對缺少子 Canvas 的 View 給警告，並提供一鍵修正；使用 Prefab 的 View 要記得處理覆寫。
- **驗收：**
  - 新建與修正後的 View 根節點都有 `Canvas` 和 `GraphicRaycaster`。
  - 補一個 PlayMode 測試，確認子 Canvas 下的按鈕仍能透過 EventSystem 的 raycast 點到，而且被蓋住的頁面收不到點擊。
  - 既有測試全數通過。

### P2. Popup 每次開啟都重新建立

- **位置：** `Runtime/UIRoot.cs` 的 `OpenPopupCoreAsync`（`Instantiate`）與 `ClosePopupCoreAsync`（`Destroy`）。
- **問題：** 常開的 Popup（確認框、提示）每次都有建立成本和記憶體回收，手機上可能出現小頓挫。
- **建議：** 在 `UIEntry` 加一個選項（例如 `keepInstance`，預設關閉）。開啟時，關閉的 Popup 只停用、不銷毀，下次開啟重複使用同一個實例。藍圖右欄與 JSON 計畫都要能設定。
- **驗收：** PlayMode 測試：選項開啟時，開、關、再開拿到同一個實例；選項關閉時維持現行行為。

### P3. 頁面不會釋放（低優先）

- **問題：** 單 Scene 模式的頁面隨 Scene 全部載入；Prefab 模式的頁面開過就一直留在 `pages` 裡。另外 `entry.prefab` 是直接引用，所有 Popup Prefab 會跟著 Scene 一起載入。
- **建議：** 中小型專案可以接受，先在 AgentGuide 寫明這個特性即可。之後若有需要，再考慮釋放不在歷史裡的 Prefab 模式頁面。

## 已定案的設計項目

以下各項的方向已經決定，結論與排程見 `ROADMAP.md`。這裡保留當初的問題說明與考慮過的做法。

### U1. `UIView` 是 `sealed`，頁面腳本沒有好用的生命週期

- **問題：** 不能寫 `class SettingsPage : UIView`。現在只能另外掛元件、接沒有參數的 UnityEvent，再自己從 `view.Data` 轉型。
- **可能做法：**
  - A：解除 `sealed`，加上 `OnShowing(object data)` 等虛擬方法。
  - B：`UIView` 維持不變，新增介面（例如 `IUIViewHandler`），`UIView` 在顯示和隱藏時呼叫同物件上實作該介面的元件。
- **決定：** 採用 B。Scene 與 Prefab 裡的 `UIView` 元件型別不必替換，編輯工具不受影響。

### U2. ID 全是字串

- **問題：** View ID、action 名稱打錯字要到執行時才知道。
- **可能做法：** 由工作區自動產生常數類別（例如 `UIIds.Workshop`、`UIActions.OpenArchive`），在儲存或 Validate 時更新。

### M4. `UINavResult.Unavailable` 包了太多情況

- **位置：** `Runtime/UIRoot.cs` 的 `RunAsync` 與 `TriggerAsync`。
- **問題：** 找不到 ID、已經在該頁、route 不存在、程式丟出例外，回傳的都是 `Unavailable`。
- **可能做法：** 至少把例外拆成 `Failed`，找不到目標拆成 `NotFound`。這會改到公開 enum。

### M5. 遊戲端 API 不足

- 拿不到 View 實例（`TryGetView`、`CurrentPage`、`TopPopup`）。
- Popup 沒辦法回傳結果，例如確認框的是或否。
- 沒有清空歷史或「取代目前頁」的方式，`history` 會無限增長。
- 轉場中的請求一律回 `Busy`，不排隊。

### L1. 畫面設定與輸入

- `UIRoot.Awake` 強制使用 `ScreenSpaceOverlay`。
- 沒有 Safe Area 支援。
- 沒有 Esc／Android 返回鍵對應 `BackAsync`。

### L7. `Showed` 改名為 `Shown`

- 這是公開 API，會影響已接好的 UnityEvent。

## 已確認的 Unity 行為

以下都在 Unity 6000.3.19f1 實測過：

- `JsonUtility.FromJson` 遇到缺漏欄位時，字串與陣列維持 `null`，但巢狀的 `[Serializable]` 類別會用預設建構子建立實例。
- `Vector2` 只給一個分量時，另一個分量維持欄位初始值（所以 NaN 哨兵可用）。
- Edit Mode 中，`GetComponent<T>()` 對不存在的元件回傳真正的 `null`。
- 匯入 DOTween 的 `.unitypackage` 後，`DOTWEEN` define 會自動加到所有平台。
- 轉場中停用 UIRoot：取消會正常還原，不會出現 “GameObject is already being activated or deactivated” 錯誤。
- `IProcessSceneWithReport` 的剝除只作用在 build 用的 Scene 複本，磁碟上的 Scene 檔不變。

## 驗證紀錄

### 第二批修正複驗（0.3.0）

使用 Unity 6000.3.19f1 的隔離副本。先在修正前用四個案例重現目前頁留在返回歷史與通知期間缺少警告，再執行修正後的完整測試。EditMode 使用圖形裝置，PlayMode 以批次模式執行。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 25 | 0 | 0 |
| 有 DOTween | PlayMode | 38 | 0 | 1 |
| 無 DOTween | EditMode | 17 | 0 | 8 |
| 無 DOTween | PlayMode | 35 | 0 | 4 |

- 有 DOTween 時略過無 DOTween 的瞬間切換案例。無 DOTween 時略過需要 DOTween 的計畫套用或動畫案例。
- 另在副本模擬 UnityEvent 的反射方法不存在，確認公開 `Invoke()` 路徑只警告一次、切頁完成，帶兩個參數的 Emit 也能執行。
- 在有 DOTween 的副本套用 Launch 計畫，將 AgentGuide 的 `LaunchPage` 原文放進獨立 asmdef，引用實際生成的 ID 常數。獨立 PlayMode 案例確認起始頁收到 `OnShown` 後，主選單在下一影格顯示。
- Windows 64 位元 Mono Player 使用 High Managed Stripping，實際驗證指南範本、通知中被拒絕的返回與警告、`Replace` 後返回仍可操作目前頁，以及持久 UnityEvent 的後續監聽和原始例外堆疊。Player 結束碼為 0。

### 第五輪：Claude 對第二批修正的獨立驗證

以上是 codex 自己的紀錄。Claude 另外在自己的暫存專案重跑，EditMode 啟用圖形裝置，PlayMode 使用批次模式。四組數字與上表完全相同：

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 25 | 0 | 0 |
| 有 DOTween | PlayMode | 38 | 0 | 1 |
| 無 DOTween | EditMode | 17 | 0 | 8 |
| 無 DOTween | PlayMode | 35 | 0 | 4 |

另外確認了以下幾點：

- **探測：** 第四輪用來重現 G1、G2 的探測全部重跑，另外加了四個（等一個影格後導航、轉場中的一般 `Busy`、Popup 在自己的 `OnShown` 裡關閉、`Hiding` 監聽丟例外）。結果見「第五輪審核」。
- **實際打包（Mono、High Managed Stripping、含 DOTween）：**
  - 範本頁面、確認 Popup 與回傳結果和第四輪一樣正常。
  - `Replace` 回主畫面後按返回得到 `Unavailable`，頁面仍然顯示而且可以操作。
  - 在 `ViewShown` 裡開 Popup 得到 `Busy`，log 有對應的警告。
  - `Hiding` 監聽丟例外時，第二個監聽仍被呼叫；log 有原始例外的堆疊，指到丟例外的那一行。
- **模擬反射失效：** 見「第五輪審核」的 G3。

以下 Claude 沒有重做，以 codex 的紀錄為準：AgentGuide 的 `LaunchPage` 範本放進獨立 asmdef 編譯並執行。Claude 用自己寫的等效探測確認了同一個寫法可行。

### 第二批改進（0.3.0）

使用 Unity 6000.3.19f1 的隔離副本。EditMode 啟用圖形裝置，PlayMode 使用批次模式。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 22 | 0 | 0 |
| 有 DOTween | PlayMode | 27 | 0 | 1 |
| 無 DOTween | EditMode | 17 | 0 | 5 |
| 無 DOTween | PlayMode | 25 | 0 | 3 |

- 有 DOTween 時略過只供無 DOTween 環境執行的瞬間切換案例。無 DOTween 時略過需要 DOTween 的 JSON 套用或動畫案例。
- EditMode 驗證藍圖右欄的儲存回呼及 Scene 重新載入皆保留歷史與關閉結果。測試結束後，在套用消費端計畫之前確認生成目錄及其 .meta 已清除。
- 用 CLI 套用 Workshop 計畫，將 AgentGuide 的 `WorkshopPage` 原文放進獨立 asmdef，引用實際生成的 `UniGameUI.Generated`。Windows 64 位元 Player 使用 Mono 與 High Managed Stripping，建置成功。執行驗證頁面腳本更新文字、Emit 開啟 Popup Prefab、關閉路由回傳 `yes`，以及持久 UnityEvent 的原始例外堆疊和後續監聽。Player 結束碼為 0。

### 第四輪：Claude 對第二批的獨立驗證

以上是 codex 自己的紀錄。Claude 另外在自己的暫存專案重跑，EditMode 啟用圖形裝置，PlayMode 使用批次模式。四組數字與上表完全相同：

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 22 | 0 | 0 |
| 有 DOTween | PlayMode | 27 | 0 | 1 |
| 無 DOTween | EditMode | 17 | 0 | 5 |
| 無 DOTween | PlayMode | 25 | 0 | 3 |

另外確認了以下幾點：

- **F5：** 專案原本沒有 `Assets/UniGameUI` 時，EditMode 測試跑完不會留下這個資料夾。專案已經有產生的常數與 asmdef 時，測試跑完兩者都還在，`WorkshopUI.cs` 的內容沒有變。
- **套用範例計畫：** 路由的 `history` 與 `result` 正確寫入，Validate 沒有任何問題。產生的常數包含 `Elements.ConfirmView.Confirm`（元件 ID 與畫面 ID 相同），同一個資料夾有 `UniGameUI.Generated.asmdef`。
- **範本與 asmdef：** 把 AgentGuide 的 `WorkshopPage` 原文放進另一個 asmdef，引用 `UniGameUI.Runtime`、`UniGameUI.Generated`、`Unity.TextMeshPro`，可以編譯。
- **實際打包（Mono、High Managed Stripping、含 DOTween）：**
  - 頁面腳本更新了標題文字。
  - `Emit` 路由開啟確認 Popup，Popup 的腳本用 `GetData<string>()` 顯示訊息。
  - 帶結果的關閉路由讓呼叫端收到 `yes`；不帶結果的關閉路由沒有觸發確認後的動作。
- **例外處理：** `Hiding` 的第一個監聽丟例外時，第二個監聽仍被呼叫，切頁照常完成。Player 的 log 裡有原始例外的堆疊，指到丟例外的那一行。
- **顯示前出錯：** `OnShowing` 丟例外時回傳 `Failed`。原頁面恢復顯示而且可以操作，透明度與互動狀態都有還原；新頁面的資料與歷史都沒有變動。
- **Popup 結果：** 等待結束時導航鎖已經釋放，接著立刻切頁會成功。
- G1、G2、G4 所述的行為。

以下 Claude 沒有另外寫測試，以 codex 的自動測試為準，這些測試都在上表裡通過：藍圖視窗的儲存邏輯、Prefab 模式頁面的 `View.Root`、Popup 結果的各種取消途徑。

### 第一批改進（`v0.2.0`）

使用 Unity 6000.3.19f1 的隔離專案。EditMode 使用可用圖形裝置驗證 Scene View，PlayMode 以批次模式執行。

| 測試 | 通過 | 失敗 | 略過 |
|---|---:|---:|---:|
| EditMode，有 DOTween | 19 | 0 | 0 |
| PlayMode，有 DOTween | 13 | 0 | 1 |
| EditMode，無 DOTween | 16 | 0 | 3 |
| PlayMode，無 DOTween | 11 | 0 | 3 |
| 外部磁碟安裝，以 `testables` 啟用單項 EditMode | 1 | 0 | 0 |

略過的是只適用於有 DOTween 或無 DOTween 的案例。另以只有 DOTween DLL 的專案確認待 Setup 的驗證錯誤，並在非批次 Editor 從本機官方套件呼叫 `ImportLocalPackage()`，確認匯入後重新編譯並進入就緒狀態。

實際建置並執行 Windows Player。單 Scene 與 Prefab 模式都使用生成的 ID 常數導航，成功切頁、開關 Popup 並返回。Popup 按鈕命中 raycast，底下頁面的按鈕沒有命中。兩種模式的 Player 場景都已剝除審核複本。

### 第三輪：Claude 對第一批的獨立驗證

以上是 codex 自己的紀錄。Claude 另外在自己的暫存專案（批次模式、沒有圖形裝置）重跑，結果一致：

| 測試 | 通過 | 失敗 | 略過 |
|---|---:|---:|---:|
| EditMode，有 DOTween | 18 | 0 | 1 |
| PlayMode，有 DOTween | 13 | 0 | 1 |
| EditMode，無 DOTween | 15 | 0 | 4 |
| PlayMode，無 DOTween | 11 | 0 | 3 |

EditMode 多略過的一個是需要圖形裝置的 Scene View 案例。另外確認了以下幾點：

- **DOTween 三種狀態：** 有 define 時為 `Ready`；移除 define 但保留檔案時為 `Imported`；刪除檔案後為 `Missing`。`FindLocalPackage` 找得到本機快取的套件。
- **套用範例計畫：** 三個 View 的根節點都有子 Canvas，Validate 沒有任何問題，產生的 `WorkshopUI.cs` 內容正確。
- **實際打包（單 Scene 模式，含 DOTween）：** 遊戲腳本用產生的常數導航，可以編譯；切頁、返回、開關 Popup 正常；打包後的 View 都有子 Canvas，審核層已剝除。
- **導航診斷：** 在執行檔的 log 中看到預期的訊息與回傳值，例如找不到的頁面回傳 `NotFound` 並記錄 ID 與 Scene，已在該頁回傳 `Unavailable`，沒有上一頁的返回安靜地回傳 `Unavailable`。
- **事件監聽丟例外：** 行為如 F1 所述。

Prefab 模式的打包、外部磁碟安裝、互動 Editor 中的匯入，Claude 沒有重做，以 codex 的紀錄為準。

### 第二輪（commit `0cabfec`）

| 測試 | 沒有 DOTween | 有 DOTween |
|---|---|---|
| EditMode（套件自帶 6 個） | 6 通過 | 6 通過 |
| PlayMode（套件自帶 4 個） | 2 通過、2 略過 | 3 通過、1 略過 |

略過的案例是依環境切換的：兩個動畫案例只在有 DOTween 時執行，一個「無 DOTween 也能導航」的案例只在沒有 DOTween 時執行。

另外實際打包了一個 Windows 執行檔（含 DOTween、`Concurrent` 模式，轉場用到 Fade、Slide、Zoom），以 `-batchmode -nographics` 執行並自動點按鈕：

- Editor 的 Scene 有 3 個 `UIView`（2 頁加 1 個 Popup 審核實例）；打包後剩 2 個，UIRoot 底下沒有 `UI Review`。
- 起始頁、切到設定頁、返回、開啟 Popup、關閉 Popup 全部正常，log 沒有例外。

### 第一輪（H1、H2 修正後）

- 只改背景與文字的 `updateExisting` 計畫：動畫與版面保留。
- 只給 `"enter": { "duration": 0.9 }` 與 `"rect": { "size": { "x": 500 } }`：只有這兩個值改變。
- 沒有 DOTween 時，設了 Slide 和 Fade 的畫面可以正常導航，只警告一次。

### 驗證方法

在暫存區建一個最小專案，`Packages/manifest.json` 放 ugui 2.0.0、test-framework 1.6.0，以及 jsonserialize、uielements、ui、imgui、audio、physics、physics2d 模組（後三個是 DOTween 的模組腳本需要的），把套件複製到 `Packages/` 後執行：

1. `-runTests -testPlatform EditMode` 與 `-runTests -testPlatform PlayMode`。
2. 用 `-importPackage` 匯入本機 Asset Store 快取裡的 DOTween，再跑一次 PlayMode。
3. 用 `-executeMethod` 呼叫 `BuildPipeline.BuildPlayer`，再執行產出的執行檔。

使用者的 Editor 開著專案時，CLI 拿不到專案鎖，所以一律在副本上驗證。

## 未驗證

第二批的藍圖儲存邏輯與錯誤計畫的預檢已由測試覆蓋。互動 Editor 待人工確認錯誤計畫的對話框、藍圖右欄「返回歷史」選單、「關閉結果」輸入框及拖線，以及 Add Component 中的 `UniGameUI/UI Element Id`。本次 Unity 安裝只有 Windows Mono 模組，IL2CPP Player 未執行。

第一批仍需在互動 Editor 人工確認以下操作。

- DOTween 未就緒時的啟動提示、每個工作階段只提示一次、Play Mode 的提示狀態，以及本機匯入、Asset Store、稍後與 Utility Panel 的按鈕。
- 儲存藍圖路由後 Undo，再從同一節點的輸出端拖線。切頁動畫欄位的 Undo 顯示與 Play Mode 停用。
- 從藍圖按 **Validate** 更新常數，以及按 **補上子 Canvas** 修復舊 Scene。兩個入口所呼叫的生成與修復已用自動測試覆蓋。

以下只能在互動的 Editor 裡操作，批次模式測不到：

- M2 進 Play 前的三選一對話框（「套用並進入」、「直接進入」、「取消」）。
- L5 的 `UITmpResources.ImportWithEditorAsync`（互動模式的 TMP 資源匯入）。
- 藍圖視窗的滑鼠操作（拖節點、拖線、右欄編輯）。

另外沒有測過打包成 AssetBundle 或 Addressables 的 Scene。

## 其他注意

- 工作區有兩個未 commit 的 `ProjectSettings` 變更，不是這個框架產生的，commit 時不要混進去。
  - 新增了 `APP_UI_EDITOR_ONLY` define。
  - `EditorBuildSettings` 多了 `com.unity.dt.app-ui` 設定物件。
