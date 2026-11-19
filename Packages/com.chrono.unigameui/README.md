# UniGameUI

適用於 Unity 6.3 與 uGUI。這個套件提供 Scene 2D 審核、可編輯的 UI 藍圖，以及使用 Unity `Awaitable` 與 DOTween 的頁面和 Popup 轉場。

1. 將此套件目錄放進專案的 `Packages/com.chrono.unigameui`，或在 Package Manager 使用 **Install package from disk** 選擇本套件的 `package.json`。
2. 提供此次的 UI 需求，請 agent 先讀取 [AgentGuide.md](Documentation~/AgentGuide.md)。匯入時只有工具和文件。工作區、頁面、Popup、藍圖都由 agent 在接到需求後建立。
3. 在交付 Scene 裡開啟 **Tools → UniGameUI → Blueprint**，用 **檢視全部 UI** 或藍圖節點定位畫面。單 Scene 頁面用 `Ctrl+S` 存檔。Popup 預覽的修改用 **套用到 Prefab** 儲存。

UniGameUI 需要 [DOTween 標準版](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)。可從 **Tools → UniGameUI → Install DOTween…** 匯入本機快取或開啟 Asset Store，再依提示完成 [Setup DOTween](https://dotween.demigiant.com/getstarted)。Validate 與 JSON 計畫會檢查套件是否就緒。執行期遇到缺少 DOTween 的環境時，會警告一次並以瞬間切換保護導航。

遊戲程式以 `UIViewBehaviour` 接收頁面生命週期，從 `View.Root` 導航，並用 `View.Get<T>(id)` 取得元件。切頁可指定返回歷史，選擇視窗可用 `OpenPopupForResultAsync<T>` 等待關閉結果。音效與數據記錄可以訂閱工作區的全域事件。要在 `OnShown` 接續導航，先用 `Awaitable.NextFrameAsync()` 等一個影格，從同步通知直接提出會回傳 `Busy` 並記錄警告。

儲存場景或套用 JSON 計畫後，工具會在 `Assets/UniGameUI/Generated` 產生 ID 常數。等 Editor 編譯完成再接遊戲腳本。自訂 asmdef 需引用產生常數的 `UniGameUI.Generated` 組件。用法和完整頁面範本見 [AgentGuide 的遊戲程式章節](Documentation~/AgentGuide.md#遊戲程式)。

## 回歸測試

安裝或完成 DOTween Setup 後，在 Unity Test Runner 執行 UniGameUI 的 Edit Mode 和 Play Mode 測試。以 Git URL 或本機磁碟安裝套件時，在專案 `Packages/manifest.json` 的頂層 `testables` 加入 `"com.chrono.unigameui"`，例如 `"testables": ["com.chrono.unigameui"]`。嵌入 `Packages` 的開發專案會自動列出套件測試。 [Unity 套件測試文件](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/cus-pkg-lp/cus-pkg-development/cus-tests)

測試涵蓋導航與事件順序、子 Canvas 的 raycast、ID 常數、元件查找、Prefab、歷史模式、Popup 結果，以及缺少 DOTween 時的行為。Test Runner 會標示需要 DOTween 或圖形裝置的案例。
