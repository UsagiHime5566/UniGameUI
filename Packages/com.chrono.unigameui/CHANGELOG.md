# 變更紀錄

## 0.3.0（2026-11-20）

- 新增 `UIViewBehaviour`、`IUIViewHandler` 與 `UIView.Root`。頁面可接收顯示、隱藏及來源畫面的 Emit 動作，並用 `GetData<T>` 讀取導航資料。
- 新增依 ID 查找 View 內的元件與物件，支援未啟用的子物件。可從 Add Component 登記 `UIElementId`。
- 新增目前頁面、Popup 堆疊與 View 實例查詢，以及 `Push`、`Replace`、`Clear` 三種切頁歷史模式。
- 新增 `OpenPopupForResultAsync<T>` 與 `UIView.Close(result)`。藍圖與 JSON 計畫支援設定切頁歷史和關閉結果。
- 新增工作區的 View 和路由全域事件。顯示前的接收者出錯時會還原導航，顯示後與隱藏階段出錯時會照常完成。Console 會記錄原始例外與上下文。
- ID 常數會產生獨立的 `UniGameUI.Generated` 組件。`Elements` 下的畫面類別改加 `View` 後綴，例如 `Elements.ConfirmView.Confirm`。指南新增頁面腳本範本和編譯時機說明。

## 0.2.0（2026-11-20）

- 將 DOTween 設為必要依賴，提供本機快取匯入、Asset Store 與 Setup 提示。Validate 和 JSON 計畫會指出缺少依賴，執行期保留瞬間切換保險。
- 新增 `UINavResult.NotFound`、`Failed` 和帶有操作、ID、場景與物件定位的導航診斷。按鈕動作會指出缺少的上層元件。
- 由場景自動產生畫面、動作和元件的 ID 常數，Validate 檢查名稱衝突或檔案過期。
- 新畫面各自帶有子 Canvas 和 GraphicRaycaster，舊畫面可從藍圖補上必要元件。
- 將顯示完成的事件改為 `Shown`，已序列化的 UnityEvent 綁定會遷移。
- 修正 Undo 後的路由選取、Prefab 本體的綁定驗證，以及切頁動畫欄位的 Undo 和 Play Mode 狀態。補上外部安裝的測試啟用方式與 Player 建置清理範圍。

## 0.1.0

- 提供單 Scene 與 Prefab 工作區、uGUI 藍圖、JSON 計畫、Popup 與 DOTween 轉場。
