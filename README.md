# UniGameUI

給 Unity 6.3 用的 uGUI 框架。用 JSON 計畫或藍圖視窗建立頁面與 Popup，附可以直接改美術的起始元件、手把與鍵盤導覽、多語系接口、遊戲事件訊號，能從命令列執行的 Validate 與自動走訪，以及在 Play Mode 即時標示目前畫面的執行中藍圖。設計上假設框架之上的遊戲程式由 agent 撰寫。

這個 repo 同時是套件的開發專案，套件本體在 [Packages/com.chrono.unigameui](Packages/com.chrono.unigameui)。

## 需求

| 項目 | 說明 |
|---|---|
| Unity | 6000.3（Unity 6.3）以上。 |
| Git | 電腦要安裝 Git，而且在 PATH 上。Package Manager 靠它下載套件。 |
| DOTween | 標準版（免費），從 Asset Store 取得。安裝方式見下面第 3 步。 |
| Input System | 只支援新的 Input System。`com.unity.inputsystem` 與 `com.unity.ugui` 會由 Package Manager 自動安裝。 |

## 安裝

### 1. 加入套件

用下面任一種方法。

**方法一：Package Manager**

1. 開啟 **Window → Package Management → Package Manager**。
2. 按左上角的 **+**，選 **Install package from git URL…**。
3. 貼上網址，按 **Install**。

```text
https://github.com/UsagiHime5566/UniGameUI.git?path=/Packages/com.chrono.unigameui#v0.5.0
```

**方法二：修改 `Packages/manifest.json`**

在 `dependencies` 裡加一行，存檔後回到 Unity：

```json
"com.chrono.unigameui": "https://github.com/UsagiHime5566/UniGameUI.git?path=/Packages/com.chrono.unigameui#v0.5.0"
```

網址最後的 `#v0.5.0` 是版本標籤，用來鎖定版本，可用的版本見 [Tags](https://github.com/UsagiHime5566/UniGameUI/tags)。拿掉 `#v0.5.0` 會安裝 `main` 分支當時最新的內容。

### 2. 切換到新的 Input System

1. 開啟 **Edit → Project Settings → Player → Other Settings**。
2. 把 **Active Input Handling** 設成 `Input System Package (New)` 或 `Both`。
3. 依提示重新啟動 Editor。

只啟用舊的 Input Manager 時，Validate 會回報錯誤，JSON 計畫也套不進去。

### 3. 安裝 DOTween

1. 在 Asset Store 把 [DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) 加到自己的帳號。
2. 回到 Unity，選 **Tools → UniGameUI → Install DOTween…**。它會匯入本機已經下載的套件，沒有下載過就開啟 Asset Store 的頁面。
3. 匯入後開啟 **Tools → Demigiant → DOTween Utility Panel**，按 **Setup DOTween…**。

DOTween 不在這個 repo 裡，每個專案都要自己安裝。沒有它套件仍然可以編譯，但 Validate 會回報錯誤，JSON 計畫套不進去，執行期的轉場會變成瞬間切換。

### 4. 確認

- 選單出現 **Tools → UniGameUI**。
- **Tools → UniGameUI → Blueprint** 可以開啟，Console 沒有錯誤。

剛裝好的專案裡沒有任何預先建立的 UI。工作區、頁面、Popup 與起始元件都是第一次套用計畫時才建立的。

## 開始使用

把 UI 的需求交給 agent，請它先讀操作指南：

- 在 Unity 裡選 **Tools → UniGameUI → Agent Guide**，會開啟指南的檔案。
- 檔案的位置在 `Library/PackageCache/com.chrono.unigameui@<雜湊>/Documentation~/AgentGuide.md`。
- 也可以直接看 GitHub 上的 [AgentGuide.md](Packages/com.chrono.unigameui/Documentation~/AgentGuide.md)。

套件的功能簡介見 [套件的 README](Packages/com.chrono.unigameui/README.md)，每個版本的變更見 [CHANGELOG](Packages/com.chrono.unigameui/CHANGELOG.md)。

## 更新與移除

- **更新到新版本：** 把 `Packages/manifest.json` 裡網址最後的標籤改成新的版本，例如 `#v0.5.0` 改成 `#v0.6.0`，存檔後回到 Unity 就會重新下載。更新前請先看 CHANGELOG。
- **沒有鎖定標籤時：** 在 Package Manager 選 UniGameUI，按 **Update**，會抓 `main` 分支最新的內容。
- **起始元件不會被覆寫。** `Assets/UniGameUI/Widgets` 裡的元件 Prefab 安裝之後就屬於專案，更新套件不會動它們。
- **移除：** 在 Package Manager 選 UniGameUI，按 **Remove**。`Assets/UniGameUI` 底下產生的元件與 ID 常數是專案的檔案，需要的話請自己刪除。

## 執行套件的測試

平常不需要。想在自己的專案裡跑套件的測試時，在 `Packages/manifest.json` 最外層加上：

```json
"testables": ["com.chrono.unigameui"]
```

測試要在裝好 DOTween 的專案裡執行，在 **Window → General → Test Runner** 可以看到 Edit Mode 與 Play Mode 兩組。

## 版本

| 版本 | 內容 |
|---|---|
| `v0.5.0` | 驗收工具與訊號：問題代碼、Validate 與自動走訪的命令列、「任何畫面」節點與 `SignalAsync`、不可返回的跳轉。 |
| `v0.4.0` | 元件與輸入：起始元件、清單、多語系接口、手把與鍵盤導覽、返回鍵、確認框。 |
| `v0.3.0` | 遊戲程式的標準寫法：頁面腳本的生命週期、用 ID 取元件、返回歷史、Popup 結果、全域事件。 |
| `v0.2.0` | 基礎改進：導航結果與診斷訊息、自動產生 ID 常數、每個畫面一個子 Canvas。 |

## 開發這個套件

1. `git clone https://github.com/UsagiHime5566/UniGameUI.git`
2. 用 Unity 6000.3.19f1 開啟 clone 下來的資料夾。
3. 照上面的第 3 步安裝 DOTween。DOTween 的檔案與 `ProjectSettings` 裡的 `DOTWEEN` define 都不要 commit。

工作規則在 [AGENTS.md](AGENTS.md)，各批的規格在 [ROADMAP.md](ROADMAP.md)，審核與驗證紀錄在 [CODEX_REVIEW.md](CODEX_REVIEW.md)。
