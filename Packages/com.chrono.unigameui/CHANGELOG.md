# 變更紀錄

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
- EventSystem 的檢查改為依所在的 Scene 判斷：工作區的 Scene 有超過一個，或所有載入的 Scene 都沒有啟用的，才是錯誤；其他載入的 Scene 也有時只是警告；EventSystem 放在共用 Scene 可以通過。開著一個有工作區的 Scene 時，計畫可以套用到另一個 Scene。
- 計畫失敗時，為了套用而開啟的 Scene 會關回去，載入的 Scene 清單和套用前相同。
- 更新既有的下拉選單時，套用後的選項數量和 `optionKeys` 數量不同會在套用前報錯。`"optionKeys": []` 表示清除所有鍵值。
- 計畫的檢查提前：滑桿的 `min`、`max`、`value` 和既有的值合併後不合理（`min` 不小於 `max`，或 `value` 超出範圍）、下拉選單的 `value` 或 `optionKeys` 數量和選項不合、`spacing` 用在內容物件沒有版面配置元件的清單、元件 Prefab 缺少欄位需要的元件或已經遺失、`overlayClick: Close` 用在沒有遮罩的 Popup，都在套用前報錯，不會把 Scene 標成未儲存。

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
