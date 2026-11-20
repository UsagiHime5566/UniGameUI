# 變更紀錄

## 0.5.0（2026-11-20）

- `UIIssue` 新增 `Code`、`ViewId`、`ElementId`，`ToString()` 的格式改為 `Error [route.target-missing] 訊息`。每一種問題都有穩定的代碼，列在 `UIIssueCode` 與操作指南的問題代碼表，嚴重度由代碼決定。原本是英文的 Validate 訊息改成繁體中文。
- 藍圖的畫面可以標記「由程式控制進出」：計畫的 View 欄位 `navigation`（`Routes`、`Code`）、藍圖視窗右欄的勾選、`UIAuthoring.SetNavigatedByCode` 與 `UIEntry.NavigatedByCode`。設定存在工作區，不影響執行期。根節點掛了 `UIConfirmPopup` 的畫面自動視為由程式控制。
- Validate 新增五項 Warning：`start.none`（沒有起始頁）、`view.unreachable`（走不到的畫面）、`view.no-exit`（沒有出口的畫面）、`button.unbound`（沒有動作的按鈕）、`text.missing-glyph`（字型缺少的字元）。每一項都有不算的情況；缺字的檢查不會改動字型資產。
- 新增 Validate 的命令列入口 `UIValidation.RunFromCommandLine`（`-unigameui-scene`、`-unigameui-report`、`-unigameui-strict`）、JSON 報告與結束代碼，以及 `UIValidation.CheckScenes` 與 `UIValidationReport`。藍圖視窗的 Validate 改為顯示 Error 與 Warning 的數量，每一筆問題連同代碼記到 Console，點擊可以定位。
- 新增自動走訪：`UIWalk.Start`、`UIWalkOptions`、`UIWalk.IsRunning`、`UIWalk.LastReport`、`UIWalk.Finished`、`UIWalkReport`，命令列入口 `UIWalk.RunFromCommandLine`（`-unigameui-scene`、`-unigameui-report`、`-unigameui-include-emit`、`-unigameui-step-timeout`、`-unigameui-timeout`），以及藍圖視窗工具列的「走訪」。走訪在 Play Mode 把每條路由觸發一次，寫出 JSON 報告，用結束代碼表示結果。
- 操作指南新增「驗收」一節：交付前的固定步驟、兩個工具的用法、由程式控制的畫面、走訪的範圍與限制、問題代碼表。
- 新增訊號：藍圖多一個固定的「任何畫面」節點，從它拉出去的訊號路由不論目前顯示哪個畫面都成立。`UIRoot.AnySource`（`"*"`）、`UIRoot.SignalAsync(action, data, cancellation)` 與 `UIRoute.IsSignal`；計畫寫 `"source": "*"`；`UIAuthoring.Connect`、`UpdateRoute`、`Disconnect`、`PositionNode` 接受 `UIRoot.AnySource`。訊號路由的種類只能是 `ShowPage`、`OpenPopup`、`Emit`。訊號不會因為忙碌而被拒絕：有導航正在進行時等它結束再執行，多個訊號依送出的順序執行；其他導航 API 照舊回傳 `Busy`。`ShowPage` 與 `OpenPopup` 的訊號可以帶資料。
- `OpenPopup` 路由接受 `history`（`Push`、`Clear`），`UIRoot.OpenPopupAsync` 新增可以指定 `UIHistoryMode` 的多載。`Clear` 開啟的是不可返回的 Popup：開啟時清空歷史；它在最上層時返回鍵、`BackAsync()` 與 `Back` 路由無效；它開著的期間切頁一律清空歷史；`ClosePopup` 路由、`View.Close()` 與點遮罩關閉照常可以關閉它。藍圖視窗的「返回歷史」選單在 `OpenPopup` 路由顯示「可以返回」與「不可返回」。
- Validate 新增兩個代碼：`route.signal-kind-invalid`（訊號路由的種類不對）與 `route.history-invalid`（`OpenPopup` 寫 `Replace`）。訊號路由的目標算走得到，訊號路由不算任何畫面的出口。
- 走訪也會走訊號路由（用 `SignalAsync` 觸發，不傳資料）；`history` 是 `Clear` 的路由另外確認完成後歷史是空的，不可返回的 Popup 另外確認 `BackAsync()` 回傳 `Unavailable`；目標畫面由程式控制而顯示失敗時記為略過；準備或觸發遇到頁面腳本自己的導航（`Busy`）時，等它結束後重來，逾時才算失敗。
- 操作指南新增「由遊戲事件觸發的跳轉」一節：兩種寫法各自的用途與範本、訊號搭配 `Emit`、`history: Clear` 與不可返回的 Popup 的規則。

第四批審核後的修正（K1～K8，版本維持 0.5.0）：

- 走訪：一步有沒有 Error 不再受記錄筆數的上限影響。之前頁面先記 50 筆以上的 Warning 再記 Error 時，這一步會被當成通過。報告每一步最多列 50 筆，Error 與 Exception 優先保留，沒有列出來的筆數寫在新的 `logsOmitted`。
- 走訪：準備時先顯示的候選頁面會排除由程式控制的頁面，沒有其他候選時才用。候選顯示不出來而改試下一個時，它記的訊息留在報告裡並在新的 `note` 欄位註明，不算進這一步的判定。來源是頁面的 `Back`，能當上一頁的只有顯示不出來的由程式控制頁面時，記為略過。
- 走訪：來源是 Popup 時，底下的頁面照實際會開啟它的頁面選。Popup 只會開在某一頁上面、而它的 `ShowPage` 路由正好要切到那一頁時，這一步如實失敗，原因會說明。之前為了避開目標而換頁，這種實際上沒有反應的路由會被當成通過。
- Validate：`view.no-exit` 的頁面規則放寬。頁面開得到的 Popup 有切到別頁的 `ShowPage` 路由時，這個頁面算有出口。
- **問題代碼拆開，一個代碼只代表一種問題。** `route.invalid` 拆成 `route.incomplete`（來源或動作是空的）、`route.source-missing`（來源畫面不存在）、`route.duplicate`（重複的動作），`route.invalid` 不再使用。目的地種類不符從 `route.target-missing` 分出來成為 `route.target-kind-mismatch`。空的元件 ID 從 `element.id-duplicate` 分出來成為 `element.id-empty`。只缺產生的 asmdef 從 `constants.outdated` 分出來成為 `constants.asmdef-missing`。
- `UIValidation.CheckScenes`：每一個 Scene 都沒有工作區時，`UIValidationReport.Passed` 是 false，新的 `Error` 屬性與 JSON 的 `error` 是原因，不再和命令列的結束代碼 2 不一致。同一個 Scene 寫了兩次只檢查一次。未命名的 Scene 和存過檔的 Scene 一起開著時，檢查完也會還原。
- Validate：Call State 設成 Off 的 `onClick` 監聽不算有接監聽（`button.unbound`）。`text.missing-glyph` 不再把富文字標籤當成字元，Sprite Asset 補得到的字元不算缺字，清單項目裡的文字也會檢查；讀過另一個動態字型之後不會再問錯字型檔。
- 走訪：命令列啟動後、開始之前發生 Domain Reload 時，排定的走訪會接著開始，批次模式的 Editor 不會停在那裡。還沒進入 Play Mode 就回到 Edit Mode 時，結果是 `NotRun`。Console 開著 Error Pause 時，走訪會讓被暫停的 Play Mode 繼續。Enter Play Mode Options 關閉 Reload Scene 的專案，起始頁的顯示與它記的訊息也接得到。
- 命令列的走訪寫完報告之後，多等幾次 Editor 更新才結束 Editor。操作指南註明：結束代碼不是 0、1、2 時代表 Editor 本身異常結束，請以報告檔為準。

## 0.4.0（2026-11-20）

- 新增相依 `com.unity.inputsystem`，只支援新的 Input System。新工作區的 EventSystem 直接使用 `InputSystemUIInputModule`；輸入模組不對，或 Active Input Handling 只啟用舊的 Input Manager 時，Validate 回報錯誤。
- 新增元件集 `UIWidgetSet` 與 `UIWidgets`。畫面上的元件改為專案 `Assets/UniGameUI/Widgets` 裡元件 Prefab 的實例，修改 Prefab 的美術會反映到所有畫面。起始元件有文字、圖片、面板、按鈕、開關、滑桿、進度條、下拉選單、輸入框、捲動清單，另附清單項目與確認框；安裝可以重複執行，只補上缺少的。
- JSON 計畫的 `kind` 改為元件集裡的名稱，新增各元件的欄位、最上層的 `widgets`（登記自訂元件）與 `defaultFontPath`（預設字型）。欄位寫在不支援它的元件上、`kind` 不存在、更新時 `kind` 不同，都會在套用前報錯。只有計畫寫出的欄位會成為 Prefab 覆寫。`updateExisting` 只能用在由計畫建立的元件。
- `UIElementId` 新增 `Kind`。新元件的預設大小取自元件 Prefab，不再是固定的 240×72。
- 新增 `UIProgressBar`、`UIList`（依資料產生項目、`ItemClicked`、`GetInItem`）與 `UIListLayout`。`View.Get<T>()` 與 ID 常數不包含清單項目裡的元件。
- 新增多語系接口：`IUILocalizer`、`UIRoot.Localizer`、`RefreshLocalization()`、`Localize()`、`LocalizationChanged`，以及 `UILocalizedText`、`UILocalizedImage`、`UILocalizedDropdown`。計畫支援 `textKey`、`spriteKey`、`placeholderKey`、`optionKeys`。`UIViewBehaviour` 新增 `OnLocalizationChanged()`。
- 新增手把與鍵盤導覽：`UIRoot.InputMode`、`InputModeChanged`、`ElementSelected`，`UIView.DefaultSelected` 與計畫的 `defaultSelected`。以最後使用的輸入裝置為準，改用滑鼠時清除選取，滑鼠點過的按鈕不留在選取狀態，每個畫面記住自己的選取。
- 新增返回鍵：Esc 與手把的取消鍵等同返回。`UIRoot.HandlesBackKey`、`BackRequestedAtRoot`，`UIView.BlocksBackKey` 與計畫的 `backKey`，`IUIBackHandler` 與 `UIViewBehaviour.OnBackRequested()`。
- 新增 Popup 的重用（`popupCache`、`UIView.ReusesInstance`）與點遮罩關閉（`overlayClick`、`UIView.ClosesOnOverlayClick`）。`UIView` 新增 `Overlay`。
- 新增確認框：`UIRoot.ConfirmAsync()`、`UIConfirmPopup`、`UIConfirmRequest` 與起始的 `Confirm.prefab`。
- Validate 新增元件集、預設選取元件與 Popup 選項的檢查。`UIView`、`UIRoot` 與元件集的 Inspector 顯示新的設定。
- View Prefab 以不同的畫面 ID 登記時，訊息改為說明畫面 ID 不符。
- 測試在重設「只警告一次」的旗標後才檢查 UnityEvent 的備援警告。

第三批審核後的修正（J1～J8，版本維持 0.4.0）：

- 計畫寫出的欄位整個固定成覆寫：顏色的每一個分量、`options` 與 `optionKeys` 的數量和每一項。之前顏色只固定了和元件 Prefab 不同的分量，修改 Prefab 的顏色會讓畫面變成兩者混合的顏色。
- EventSystem 的檢查改為依所在的 Scene 判斷：工作區的 Scene 有超過一個，或所有載入的 Scene 都沒有啟用的，才是錯誤；其他載入的 Scene 也有時只是警告；EventSystem 放在共用 Scene 可以通過。開著一個有工作區的 Scene 時，計畫可以套用到另一個 Scene。
- 計畫失敗時，為了套用而開啟的 Scene 會關回去，載入的 Scene 清單和套用前相同。
- 畫面指到的 View Prefab 已經存在時，預檢以 Prefab 裡的內容為準：`defaultSelected` 可以指到 Prefab 裡的元件，`updateExisting` 的規則（只能更新由計畫建立的元件、`kind` 必須相同）不再被繞過。
- 修正起始下拉選單範本的捲軸寬度是 0。已經安裝過起始元件的專案不會自動更新，請把 `Dropdown.prefab` 裡 `Template/Scrollbar` 的寬度改成 40。
- 更新既有的下拉選單時，套用後的選項數量和 `optionKeys` 數量不同會在套用前報錯。`"optionKeys": []` 表示清除所有鍵值。
- 計畫的檢查提前：滑桿的 `min`、`max`、`value` 和既有的值合併後不合理（`min` 不小於 `max`，或 `value` 超出範圍）、下拉選單的 `value` 或 `optionKeys` 數量和選項不合、`spacing` 用在內容物件沒有版面配置元件的清單、元件 Prefab 缺少欄位需要的元件或已經遺失、`overlayClick: Close` 用在沒有遮罩的 Popup，都在套用前報錯，不會把 Scene 標成未儲存。
- `placeholder`、`placeholderKey` 寫在沒有 TMP placeholder 的輸入框類元件上時，在套用前報錯，不再安靜地略過。
- 計畫的 `defaultFontPath` 改在 Scene 存檔之前套用。換字型或存檔失敗時，元件 Prefab、元件集的預設字型與 Scene 檔案都回到套用前的狀態。
- `SetDefaultFont` 對輸入框的文字與 placeholder 各自判斷，只換掉目前使用舊預設字型的那一個。
- 藍圖與 Validate 不再把 TMP 自己衍生的文字顏色欄位當成待套用的覆寫。改過元件 Prefab 的文字顏色之後登記的畫面，不會再顯示不存在的「待套用」。

## 0.3.0（2026-11-20）

- 新增 `UIViewBehaviour`、`IUIViewHandler` 與 `UIView.Root`。頁面可接收顯示、隱藏及來源畫面的 Emit 動作，並用 `GetData<T>` 讀取導航資料。
- 新增依 ID 查找 View 內的元件與物件，支援未啟用的子物件。可從 Add Component 登記 `UIElementId`。
- 新增目前頁面、Popup 堆疊與 View 實例查詢，以及 `Push`、`Replace`、`Clear` 三種切頁歷史模式。
- 新增 `OpenPopupForResultAsync<T>` 與 `UIView.Close(result)`。藍圖與 JSON 計畫支援設定切頁歷史和關閉結果。
- 新增工作區的 View 和路由全域事件。顯示前的接收者出錯時會還原導航，顯示後與隱藏階段出錯時會照常完成。Console 會記錄原始例外與上下文。
- ID 常數會產生獨立的 `UniGameUI.Generated` 組件。`Elements` 下的畫面類別改加 `View` 後綴，例如 `Elements.ConfirmView.Confirm`。指南新增頁面腳本範本和編譯時機說明。
- 修正切頁和返回後的歷史，會移除尾端與目前頁相同的項目，避免返回時把目前畫面關掉。
- 在同步通知期間提出導航時會回傳 `Busy` 並記錄上下文。指南補上 `OnShown` 等下一影格再導航的範本與生命週期時機。
- UnityEvent 的逐一派送方法無法取得時會警告一次並使用公開的 `Invoke()`。仍可繼續的通知例外與導航失敗會顯示不同的診斷文字。
- JSON 計畫會回報用於不適用路由的 `history` 或 `result` 欄位。

## 0.2.0（2026-11-20）

- 將 DOTween 設為必要依賴，提供本機快取匯入、Asset Store 與 Setup 提示。Validate 和 JSON 計畫會指出缺少依賴，執行期保留瞬間切換保險。
- 新增 `UINavResult.NotFound`、`Failed` 和帶有操作、ID、場景與物件定位的導航診斷。按鈕動作會指出缺少的上層元件。
- 由場景自動產生畫面、動作和元件的 ID 常數，Validate 檢查名稱衝突或檔案過期。
- 新畫面各自帶有子 Canvas 和 GraphicRaycaster，舊畫面可從藍圖補上必要元件。
- 將顯示完成的事件改為 `Shown`，已序列化的 UnityEvent 綁定會遷移。
- 修正 Undo 後的路由選取、Prefab 本體的綁定驗證，以及切頁動畫欄位的 Undo 和 Play Mode 狀態。補上外部安裝的測試啟用方式與 Player 建置清理範圍。

## 0.1.0

- 提供單 Scene 與 Prefab 工作區、uGUI 藍圖、JSON 計畫、Popup 與 DOTween 轉場。
