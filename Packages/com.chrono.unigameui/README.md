# UniGameUI

適用於 Unity 6.3 與 uGUI。這個套件提供 Scene 2D 審核、可編輯的 UI 藍圖，以及使用 Unity `Awaitable` 與 DOTween 的頁面和 Popup 轉場。

1. 將此套件目錄放進專案的 `Packages/com.chrono.unigameui`，或在 Package Manager 使用 **Install package from disk** 選擇本套件的 `package.json`。
2. 提供此次的 UI 需求，請 agent 先讀取 [AgentGuide.md](Documentation~/AgentGuide.md)。匯入時只有工具和文件。工作區、頁面、Popup、藍圖都由 agent 在接到需求後建立。
3. 在交付 Scene 裡開啟 **Tools → UniGameUI → Blueprint**，用 **檢視全部 UI** 或藍圖節點定位畫面。單 Scene 頁面用 `Ctrl+S` 存檔。Popup 預覽的修改用 **套用到 Prefab** 儲存。

動畫使用 [DOTween 標準版](https://dotween.demigiant.com/download)。匯入 DOTween 後，在 **Tools → Demigiant → DOTween Utility Panel** 完成 **Setup DOTween**。尚未安裝 DOTween 時，轉場會直接切換，導航仍可直接試跑。
