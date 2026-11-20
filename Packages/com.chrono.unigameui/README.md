# UniGameUI

適用於 Unity 6.3、uGUI 與 Input System。這個套件提供 Scene 2D 審核、可編輯的 UI 藍圖、可以直接修改美術的起始元件，以及使用 Unity `Awaitable` 與 DOTween 的頁面和 Popup 轉場。

1. 在 Package Manager 按 **+**，選 **Install package from git URL…**，貼上 `https://github.com/UsagiHime5566/UniGameUI.git?path=/Packages/com.chrono.unigameui#v0.5.0`。網址最後的 `#v0.5.0` 是版本標籤，用來鎖定版本。也可以把這個套件目錄放進專案的 `Packages/com.chrono.unigameui`，或用 **Install package from disk** 選擇本套件的 `package.json`。相依的 uGUI（`com.unity.ugui`）與 Input System（`com.unity.inputsystem`）會由 Package Manager 一併安裝。用 git URL 安裝需要電腦裝有 Git。
2. 到 **Project Settings → Player → Active Input Handling** 選 `Input System Package (New)` 或 `Both`，並依提示重新啟動 Editor。套件只支援新的 Input System；只啟用舊的 Input Manager 時，Validate 會回報錯誤。
3. 提供此次的 UI 需求，請 agent 先讀取 [AgentGuide.md](Documentation~/AgentGuide.md)。匯入時只有工具和文件。工作區、頁面、Popup、藍圖都由 agent 在接到需求後建立。
4. 在交付 Scene 裡開啟 **Tools → UniGameUI → Blueprint**，用 **檢視全部 UI** 或藍圖節點定位畫面。單 Scene 頁面用 `Ctrl+S` 存檔。Popup 預覽的修改用 **套用到 Prefab** 儲存。

UniGameUI 需要 [DOTween 標準版](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)。可從 **Tools → UniGameUI → Install DOTween…** 匯入本機快取或開啟 Asset Store，再依提示完成 [Setup DOTween](https://dotween.demigiant.com/getstarted)。Validate 與 JSON 計畫會檢查套件是否就緒。執行期遇到缺少 DOTween 的環境時，會警告一次並以瞬間切換保護導航。

## 元件

畫面上的元件都是專案 `Assets/UniGameUI/Widgets` 裡元件 Prefab 的實例：文字、圖片、面板、按鈕、開關、滑桿、進度條、下拉選單、輸入框、捲動清單，另外附清單項目與確認框。第一次建立工作區或套用 JSON 計畫時會自動安裝，也可以用 **Tools → UniGameUI → Install Starter Widgets**。起始元件的外觀只用 Unity 內建的 UI 圖和 TMP 預設字型，請直接修改這些 Prefab 的美術，所有畫面會跟著變。專案自己的 Prefab 可以登記成元件，JSON 計畫用名稱指定。TMP 的預設字型沒有中文字，要顯示中文請在元件集設定預設字型。

## 遊戲程式

遊戲程式以 `UIViewBehaviour` 接收頁面生命週期，從 `View.Root` 導航，並用 `View.Get<T>(id)` 取得元件。切頁可指定返回歷史，選擇視窗可用 `OpenPopupForResultAsync<T>` 等待關閉結果，「確定要……嗎？」用 `await Root.ConfirmAsync(message)`。滑桿、開關、下拉選單、輸入框的值變更直接訂閱 Unity 元件的事件；清單用 `UIList.SetItems`。音效與數據記錄可以訂閱工作區的全域事件。要在 `OnShown` 接續導航，先用 `Awaitable.NextFrameAsync()` 等一個影格，從同步通知直接提出會回傳 `Busy` 並記錄警告。

在某一頁發生、依遊戲邏輯的結果去不同地方的換頁（「進入下一天」可能到開店前、週結或結局），畫成那一頁的路由，每個去處各一條，頁面腳本用 `NavigateAsync(動作)` 挑其中一條；它遇到轉場會等它結束再執行。任何畫面都可能發生的遊戲事件（挑戰者連線、斷線）才用 `root.SignalAsync(動作, 資料)` 送出，要去哪裡畫在藍圖的「任何畫面」節點上。路由的 `history: Clear` 設定跳過去之後不能返回，`ShowPage` 與 `OpenPopup` 都適用。

手把與鍵盤可以操作所有畫面，改用滑鼠時不留下選取外框；Esc 與手把的取消鍵等同返回，個別畫面可以關掉或用 `OnBackRequested` 接手。多語系只開接口：文字、圖片與下拉選項可以帶鍵值，專案實作 `IUILocalizer` 提供翻譯，切換語言後呼叫 `root.RefreshLocalization()`。

儲存場景或套用 JSON 計畫後，工具會在 `Assets/UniGameUI/Generated` 產生 ID 常數。等 Editor 編譯完成再接遊戲腳本。自訂 asmdef 需引用產生常數的 `UniGameUI.Generated` 組件。用法和完整頁面範本見 [AgentGuide 的遊戲程式章節](Documentation~/AgentGuide.md#遊戲程式)。

## 驗收工具

- **藍圖視窗** 的畫布可以用滾輪縮放、按住中鍵（或 Alt 加左鍵）拖曳平移，**顯示全部** 一次看到所有節點。整個節點都可以點選與拖曳，雙擊定位到 Scene 裡的畫面。右欄分成有標題的區塊：畫面、畫面設定、從這裡出發的路由、編輯路由、通到這裡的路由。訊號路由畫成比較淡的虛線，也可以整個關掉。
- **執行中的藍圖**：Play Mode 時藍圖照常顯示，即時標示目前的頁面、疊放的 Popup 與轉場中的畫面，剛走過的路由會亮起再淡掉；右欄列出執行中的狀態與最近 20 筆導航，每條路由可以按「觸發」走一次。這時藍圖是唯讀的。
- **Validate** 檢查藍圖的設定，每一筆問題有穩定的代碼：路由的目的地不存在、走不到的畫面、沒有出口的畫面、沒有動作的按鈕、字型缺少的字元等。
- **自動走訪** 進入 Play Mode，把藍圖上的每條路由實際觸發一次，確認走得通。
- 兩個工具都能從命令列執行（`UIValidation.RunFromCommandLine`、`UIWalk.RunFromCommandLine`），用結束代碼表示結果並寫出 JSON 報告，也可以從藍圖視窗的 **Validate** 與 **走訪** 按鈕執行。agent 交付前的固定步驟、報告格式與問題代碼表在操作指南的「驗收」一節。

## 回歸測試

安裝或完成 DOTween Setup 後，在 Unity Test Runner 執行 UniGameUI 的 Edit Mode 和 Play Mode 測試。以 Git URL 或本機磁碟安裝套件時，在專案 `Packages/manifest.json` 的頂層 `testables` 加入 `"com.chrono.unigameui"`，例如 `"testables": ["com.chrono.unigameui"]`。嵌入 `Packages` 的開發專案會自動列出套件測試。 [Unity 套件測試文件](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/cus-pkg-lp/cus-pkg-development/cus-tests)

測試涵蓋導航與事件順序、子 Canvas 的 raycast、ID 常數、元件查找、Prefab、歷史模式、Popup 結果、起始元件與元件集、清單、多語系、手把與鍵盤導覽、返回鍵、確認框、Validate 的每一種問題代碼、自動走訪（實際進出 Play Mode），以及缺少 DOTween 時的行為。Test Runner 會標示需要 DOTween 或圖形裝置的案例。測試使用自己的資料夾安裝起始元件（`Assets/UniGameUI-TestWidgets` 與 `Assets/UniGameUI-PlayModeWidgets`），結束後會刪除，不會動到專案在 `Assets/UniGameUI/Widgets` 的元件。導覽與返回鍵的案例用 Input System 的測試工具模擬鍵盤、手把與滑鼠。
