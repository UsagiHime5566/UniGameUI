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
- 第三批改進：2026-11-20，commit `db8691f`（0.4.0，Claude 子 session 實作，尚未打標籤）
- 第六輪：2026-11-20，commit `43bbd51`（Claude 審核第三批，發現 J1～J8，待修正）
- 第三批修正：2026-11-20，commit `b29b305`（Claude 子 session 修正 J1～J8）
- 第七輪：2026-11-20，commit `ea61890`（Claude 複驗第三批修正，通過）
- 第四批改進：2026-11-20，commit `d0e395f`（0.5.0，Claude 子 session 實作，尚未打標籤）
- 第四批追加 4-7：2026-11-20，commit `b8cc23a`（訊號與「任何畫面」節點，版本維持 0.5.0，Claude 子 session 實作）
- 第八輪：2026-11-20，commit `ccb981c`（Claude 初步審核第四批的 4-1～4-6，發現 K1～K8）
- 第四批修正：2026-11-20，commit `9de72df`（Claude 子 session 修正 K1～K8，版本維持 0.5.0）
- 第九輪：2026-11-20，commit `f0585c2`（Claude 完整驗收第四批，通過）
- 第五批改進：2026-11-21，commit `763642a`（0.6.0，藍圖視窗的操作體驗，Claude 子 session 實作，尚未打標籤）
- 第十輪：2026-11-21，commit `8cf4285`（Claude 審核第五批，自動驗證通過，外觀待使用者確認）
- 環境：Unity 6000.3.19f1、uGUI 2.0；在獨立的暫存專案驗證，有無 DOTween 都測過

**現況：** 前四批都已通過獨立審核，標籤 `v0.3.0`、`v0.4.0`、`v0.5.0` 都已打上，repo 發佈在 <https://github.com/UsagiHime5566/UniGameUI>。第五批（0.6.0，藍圖視窗的操作體驗）自動驗證能確認的部分都通過，**外觀與手感待使用者照人工確認清單操作**，之後才打 `v0.6.0` 標籤。結果見「第十輪審核」。

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
| L1 | 強制 Overlay、無 Safe Area、無返回鍵 | 返回鍵已在第三批（3-7）實作，Claude 已在 Windows Player 用虛擬的手把與鍵盤驗證；Overlay 以外的模式與 Safe Area 不做 |
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
| P2 | Popup 每次開啟都重新建立 | 已在第三批（3-8）新增重用選項，預設仍是重新建立；PlayMode 與 Windows Player 已驗證 |
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
| I1 | 第五輪的小提醒（測試依賴靜態旗標、指南一句話不好讀） | 已在第三批（3-1）處理：測試開始前重設旗標，指南改成直接說明。Claude 已核對 |
| 3-1～3-11 | 第三批：元件與輸入（0.4.0） | 已實作並通過審核。四組測試、Windows Player 的 64 項檢查與 Editor 抽查都通過；J1～J8 修正後複驗通過。IL2CPP 與實際畫面未驗證，互動操作待人工確認 |
| J1 | 計畫寫出的顏色只固定了一部分，改元件 Prefab 的顏色會讓畫面變色 | 已修正。Claude 用第六輪的步驟複驗，三種顏色欄位都維持計畫寫的值 |
| J2 | 開著另一個有 UI 的 Scene 時，計畫套不進別的 Scene，還可能卡住 | 已修正。Claude 複驗：可以建立與套用到另一個 Scene，失敗後會關回，Validate 的四種情況符合規則 |
| J3 | 登記既有的 View Prefab 時，預檢沒有看 Prefab 裡的內容 | 已修正。Claude 複驗：誤擋與漏擋兩種情況都已改正 |
| J4 | 起始下拉選單的捲軸寬度是 0 | 已修正。Claude 確認產生的 Prefab 寬度是 40；外觀待人工確認 |
| J5～J7 | 選項的鍵值殘留、套用到一半才報錯、不存在的「待套用」 | 已修正。Claude 用第六輪的步驟複驗，三項都已改正 |
| J8 | 讀程式碼發現的四點 | 前三點確認是問題並已修正，由實作者的測試覆蓋，Claude 讀過修正的程式碼。第四點沒有重現 |
| 4-1～4-6 | 第四批：驗收工具（0.5.0） | 已實作並通過審核。四組測試與 12 次命令列實際執行都符合預期；K1～K8 修正後複驗通過。藍圖視窗的互動操作待人工確認 |
| K1 | 警告太多時，真正的錯誤被丟掉，步驟回報成通過 | 已修正。Claude 用第八輪的場景複驗，兩步都如實失敗 |
| K2 | 準備時選到由程式控制的頁面，害無關的步驟失敗 | 已修正。Claude 複驗，`game/pause` 通過 |
| K3 | Popup 的 `ShowPage` 路由指回唯一會開啟它的頁面時誤報通過 | 已修正。Claude 複驗，這一步如實失敗並說明原因。由程式或訊號開啟的 Popup 仍然避開目標頁，接受 |
| K4 | `view.no-exit` 的規則太嚴 | 已依新規則修正。Claude 複驗，`game` 不再被回報 |
| K5 | 幾個問題代碼一碼多用 | 已拆開（共 46 個代碼），由實作者的測試覆蓋。另外三個代碼維持不拆 |
| K6 | `CheckScenes`：沒檢查到也算通過、重複的 Scene、未命名的 Scene | 已修正，由實作者的測試與命令列執行覆蓋 |
| K7 | 讀程式碼發現的七點 | 六點在修正前重現並已修正，一點沒有重現但仍加了檢查。由實作者的測試覆蓋 |
| K8 | Editor 結束時偶發當機 | 指南已註明；結束前多等幾次更新。第九輪的 12 次命令列沒有再發生，效果無法判斷 |
| 5-1～5-5 | 第五批：藍圖視窗的操作體驗（0.6.0） | 已實作。四組測試通過；Claude 用真的事件確認滾輪、中鍵平移、整個節點的選取與拖曳、點空白、點標籤、F 鍵都有接上。**外觀與手感沒有人看過，待使用者照人工確認清單操作** |
| 4-7 | 第四批追加：訊號與「任何畫面」節點（0.5.0） | 已實作並通過審核。Claude 另外打包執行檔，23 項檢查全部通過；有訊號路由的場景 Validate 沒有問題、走訪通過。藍圖視窗的節點操作待人工確認 |

## 第十輪審核：第五批（0.6.0）

五項都已實作，邏輯的部分與規格相符。四組自動測試的數量與結果和實作者的紀錄相同，第四批的命令列與重現場景重跑一次，沒有退步。實作者說明滑鼠與鍵盤事件的接線完全沒有自動測試，所以我另外寫了一個探測，對藍圖視窗送出真正的 UI Toolkit 事件，11 項檢查全部通過。

**結論：自動驗證能確認的部分都通過。這一批的重點是外觀與手感，這部分沒有任何人或工具看過，請使用者照「驗證紀錄」裡「第五批」的人工確認清單實際操作之後，再決定要不要打 `v0.6.0` 標籤。**

**事件接線的探測（批次模式開啟藍圖視窗，用 `SendEvent` 送出事件）**

| 送出的事件 | 結果 |
|---|---|
| 滾輪 | 往下捲縮小、往上捲放大；連續捲動停在 25% 與 200%。 |
| 中鍵按下、移動、放開 | 視角平移的量等於滑鼠移動的量（80, 40）；節點儲存的座標不變，Scene 沒有被標成未儲存。 |
| Alt 加左鍵拖曳 | 一樣會平移。 |
| 在節點本體（不是標題）按下左鍵 | 選取那個節點。 |
| 在節點本體拖曳 | 縮放 200% 時滑鼠移動（120, 60），節點儲存的座標變化（60, 30）。 |
| 點畫布的空白處 | 取消選取。 |
| 點線上的動作標籤 | 選取來源節點，右欄選中那條路由。 |
| 畫布上按 F | 回到顯示全部。 |

這個探測在批次模式執行，視窗沒有實際的版面大小，所以「以游標為中心縮放時游標底下那一點不動」是靠實作者對換算函式的測試確認的，沒有用真的事件驗證。雙擊定位、連接點拖線、右欄分隔線的拖曳也沒有用事件驗證。

**實作者列的「與規格不同的地方」**

十四點都接受，見「驗證紀錄」的「第五批」。兩點另外說明：

- 第 2 點：欄位名稱維持「目的地」，不改成「目標」。既有的介面與文件都用這個詞，維持一致比較重要。
- 第 13 點：安裝網址維持 `#v0.5.0`，等 `v0.6.0` 打了標籤再更新根目錄的 `README.md` 與版本表。

**K8 再次出現**

這一輪 12 次命令列執行裡，加 `-unigameui-include-emit` 的走訪又有一次在結束時當機（結束代碼 139），報告一樣已經正確寫出（`Passed`）。第八輪也是這個指令。兩次都發生在走訪結束時確認框還開著、有程式在等它的結果的情況，可能和這個狀態有關，但沒有證據。第四批加的「多等幾次更新再結束」沒有消除它。照指南的說明以報告檔為準即可，列入後續待辦。

## 第九輪審核：第四批完整驗收（0.5.0）

第四批的七項（4-1～4-7）都已實作，第八輪的 K1～K8 也已修正，沒有發現新的問題。四組自動測試的數量與結果和實作者的紀錄相同。Validate 與走訪的命令列在副本專案實際執行，結束代碼都正確。我另外打包了一個有訊號路由的執行檔，23 項檢查全部通過；第三批的 64 項執行檔檢查也重跑一次，沒有退步。**第四批到此通過，可以打 `v0.5.0` 標籤。** 細節見「驗證紀錄」的「第九輪」。

**K1～K8 的複驗**

- **K1～K4：** 用第八輪同一個場景重跑，四項都已改正。
  - `title/noisy` 與 `noisy/back` 現在是 `Failed`，原因是那筆被 60 筆警告蓋住的 Error。
  - `game/pause` 通過，由程式控制的 `result` 沒有被拿來當準備用的頁面。
  - `pause/restart` 是 `Failed`，原因說明這個 Popup 只會開在 `game` 上面，實際遊玩時這個動作不會有反應。
  - Validate 對 `game` 不再回報 `view.no-exit`。
- **K5、K6、K7：** 由實作者新增的測試覆蓋，測試都通過；K7 有六點在修正前先重現過。我讀過交付紀錄，沒有另外寫探測。
- **K8：** 指南已註明結束代碼不是 0、1、2 時以報告檔為準；命令列寫完報告後多等幾次更新才結束。這一輪我執行的 12 次命令列都正常結束，但樣本太小，不能判斷當機的機率有沒有降低。

**4-7 的確認（執行檔，Mono、High Managed Stripping）**

| 範圍 | 確認的內容 |
|---|---|
| 從任何畫面送出 | 同一個訊號從起始頁、從另一個頁面送出都到得了目標，`data` 送達目標畫面。 |
| 不會漏掉 | 轉場進行中送出：這段期間 `OpenPopupAsync` 仍然回傳 `Busy`，訊號等轉場結束後執行並回傳 `Completed`。連續送出兩個訊號依序執行。在 `OnShown` 裡送出的訊號在那次導航結束後執行。 |
| 取消與錯誤 | 等待中取消 token 回傳 `Canceled`，之後也不會執行。不存在的訊號是 `NotFound`。用 `TriggerAsync` 觸發訊號路由是 `Unavailable`。 |
| `ShowPage` 加 `Clear` | 開著的 Popup 關閉，歷史是空的，`BackAsync()` 回傳 `Unavailable`，按 Esc 只觸發 `BackRequestedAtRoot`。 |
| 不可返回的 Popup | 開啟時清空歷史。Esc、手把的取消鍵與 `BackAsync()` 都關不掉，而 `OnBackRequested` 兩次都有被呼叫。從它自己的路由切頁之後，底下原本的頁面不在歷史裡。`ClosePopup` 路由可以關閉它，關閉後歷史是空的。同一個重用的實例之後用一般方式開啟，可以用返回關閉。 |
| `Emit` 的訊號 | 送到目前最上層的畫面：沒有 Popup 時是頁面，有 Popup 時是 Popup。`ActionEmitted` 的來源是 `*`。 |
| 其他 | `OpenPopupAsync` 傳入 `Replace` 丟出 `ArgumentException`。 |

同一個場景的 Validate 沒有任何 Error 或 Warning：訊號路由的目標都算走得到。走訪的結果是 `Passed`，通過 11 步、略過 4 步；略過的是 `Emit`，以及需要資料才能顯示的 `challenger`（訊號路由與以它為來源的兩條路由）。

**實作者列的「與規格不同的地方」**

4-1～4-6 的十二點、4-7 的二十一點與 K 修正的八點都接受，見「驗證紀錄」裡各自的小節。其中三點另外說明：

- **`view.no-exit` 還沒有考慮不可返回的 Popup**（4-7 第 13 點）。只接了 `Back` 路由的 Popup 以 `Clear` 開啟之後關不掉，Validate 不會提醒，目前只寫在 AgentGuide。情況很少見，不影響這次通過，已列進 `ROADMAP.md` 的後續待辦。
- **K3：** 由程式或訊號開啟的 Popup，走訪仍然會避開目標頁。這種 Popup 可能出現在任何頁面上，這個做法合理。
- **K5：** `view.id-invalid`、`view.review-missing`、`view.prefab-missing` 各包了不只一種情況，維持不拆。對使用者來說它們各是同一種問題，處理方式也相同。

**這一輪沒有驗證的**

- 藍圖視窗的互動：「任何畫面」節點的顯示與拖線、「走訪」按鈕與結果摘要、右欄「由程式控制進出」的勾選、`OpenPopup` 路由的「返回歷史」選單。
- 實體的手把與鍵盤，執行檔用的是虛擬裝置。
- IL2CPP Player。這台電腦沒有 6000.3.19f1 的 IL2CPP 模組。
- 互動 Editor 裡開著 Error Pause 時的走訪。
- K5～K7 的修正只依賴實作者的測試。K8 的修改有沒有效果無法判斷。

## 第八輪審核：第四批（0.5.0）初步結果

這一節由實作者依主 session（Claude）在 2026-11-20 傳來的審核訊息記錄；主 session 當時沒有動這個 checkout。審核的對象是 4-1～4-6（`ccb981c`），4-7 還沒有審核。

**已確認沒問題的：**

- 四組測試和實作者的紀錄相同（131/0/0、68/0/1、74/0/57、64/0/5）。
- Validate 命令列：示例場景結束代碼 0、只有確認框兩筆 `text.missing-glyph`；嚴格模式 1；Scene 不存在 2。
- 走訪命令列：示例場景 `Passed`、結束代碼 0（起始加四步通過、`Emit` 略過）；加 `-unigameui-include-emit` 六步通過；弄壞商店頁後結束代碼 1 並指出失敗的步驟；Scene 不存在與整體逾時都是 2，沒有卡住。

**當時的結論：** 測試與命令列都符合紀錄，但自動走訪有幾個會誤判的情況，請和 4-7 一起修正後再交付。K1～K4 審核者實際執行重現過；K7 是讀程式碼發現的，請實作者先各寫測試確認，是真的才修。修正的紀錄見「驗證紀錄」的「第四批修正（K1～K8）」。

審核者重現 K1～K4 用的場景：頁面 `title`（起始）、`game`、`result`（`"navigation": "Code"`，`OnShowing` 一定丟例外）、`noisy`（`OnShowing` 記 60 筆 Warning 再記一筆 Error），Popup `pause`。路由依序是 `result/retry → ShowPage game`、`title/play → ShowPage game`、`game/pause → OpenPopup pause`、`pause/resume → ClosePopup`、`pause/restart → ShowPage game`、`pause/quit → ShowPage title（Clear）`、`title/noisy → ShowPage noisy`、`noisy/back → Back`。當時的結果：`game/pause` 是 `Failed`（K2）、`pause/restart` 是 `Passed`（K3）、`title/noisy` 與 `noisy/back` 都是 `Passed`（K1）、Validate 對 `game` 回報 `view.no-exit`（K4）。

### K1. 警告太多時，真正的錯誤被丟掉，步驟回報成通過

- **嚴重度：** 高。
- **原因：** `UIWalkRunner` 每一步最多收 50 筆記錄，而且先比數量才看種類。頁面在 `OnShowing` 先記 60 筆 Warning 再 `Debug.LogError`，這一步是 `Passed`，報告裡也沒有那筆 Error。
- **規則：** 「這一步有沒有 Error／Exception」要獨立判斷，不受筆數上限影響。報告裡的記錄可以有上限，但 Error、Exception 優先保留，被省略的筆數要寫出來。

### K2. 準備時選到「由程式控制」的頁面，害無關的步驟失敗

- **嚴重度：** 中。
- **原因：** 選「另一個頁面」或「Popup 底下的頁面」時沒有排除由程式控制的頁面。它顯示失敗後雖然會換下一個候選，但失敗時的 Error 留在這一步的記錄裡，結果路由明明 `Completed`，步驟卻是 `Failed`。
- **規則：** 候選頁面先排除由程式控制的頁面，沒有其他候選時才用。候選顯示失敗而後來有別的候選成功時，那次失敗的記錄不算進這一步的判定（可以留在報告裡，但要標明是準備時被放棄的候選）。來源是一般頁面的 `Back`，唯一的另一個頁面是開不起來的由程式控制頁面時，記為 `Skipped`，不是 `Failed`。

### K3. Popup 的 `ShowPage` 路由指回唯一會開啟它的頁面時，誤報通過

- **嚴重度：** 中。
- **原因：** `game/pause → OpenPopup pause`、`pause/restart → ShowPage game`：走訪為了避開目標，把 Popup 開在別的頁面上，於是 `restart` 是 `Passed`。實際遊玩時 `pause` 一定開在 `game` 上，這條路由會回傳 `Unavailable`，按鈕沒有反應。
- **規則：** 照規格選底下的頁面（有 `OpenPopup` 路由指到這個 Popup 的頁面），不要為了避開目標而換頁。這一步如實失敗，原因要說明「這個 Popup 只會開在 X 上面，而這條路由要切到 X」。4-1～4-6「與規格不同的地方」第 12 點的做法改回來。

### K4. `view.no-exit` 的規則太嚴（規格修正）

- **嚴重度：** 中。這是規格的問題。
- **原因：** 遊戲頁只有「開啟暫停選單」一條路由、暫停選單有「回標題」時，遊戲頁會被報成沒有出口。
- **新規則：** 頁面自己有 `ShowPage` 或 `Back` 路由，或者從它沿著 `OpenPopup` 路由開得到的任何一個 Popup 有 `ShowPage` 路由，就算有出口。Popup 的規則不變。同步更新 `ROADMAP.md` 4-3 的表格、AgentGuide 與測試。

### K5. 幾個問題代碼一碼多用

- **嚴重度：** 低。代碼定下來之後不能改，趁現在拆開，並更新指南的表格。
- `route.invalid`：空的來源或動作、重複的（來源, 動作）、來源不存在，各一個代碼。
- `element.id-duplicate` 也用在空的 ID。
- `route.target-missing` 也用在目標種類不對。
- `constants.outdated` 也用在只缺產生的 asmdef。

### K6. `CheckScenes` 的結果與還原

- **嚴重度：** 低。
- 沒有任何 Scene 有工作區時，C# 回傳的 `Passed` 是 true、JSON 也是 `"passed": true`，只有命令列會轉成 2。指南建議 Editor 開著時用 C# 入口，「什麼都沒檢查」不應該讀成通過。
- 同一個 Scene 給兩次會被開兩次、數量算兩次。
- 開著多個 Scene 時未命名的 Scene 沒有還原。

### K7. 讀程式碼發現、沒有實際執行的項目

請先各寫測試確認，是真的才修，結果寫進交付說明。

- `UIWalk.cs` 的 `EnteredEditMode` 沒有檢查階段：還在 `Entering` 就回到 Edit Mode（別的工具取消了進入 Play Mode）時，結果是「`Passed`、0 步」，應該是 `NotRun`。
- 命令列啟動後的前幾個 tick 還沒有寫 SessionState、也沒有設期限。這段時間發生 Domain Reload 的話，批次模式的 Editor 不會結束。
- `text.missing-glyph`：`LoadFace` 用快取判斷要不要重新載入字型，但讀另一個動態字型的 `characterLookupTable` 會讓 TMP 自己換掉 FontEngine 目前的字型，之後的 `TryGetGlyphIndex` 可能問錯字型。
- `text.missing-glyph`：富文字標籤被當成一般字元檢查；Sprite Asset 補得到的字元會被報成缺字；清單項目裡的文字被略過，但規格只有 `button.unbound` 才不算清單項目。
- Console 開著「Error Pause」時，步驟裡第一筆 `Debug.LogError` 會暫停 Play Mode，走訪停住直到整體逾時，結果變成 `NotRun` 而不是 `Failed`。
- 進入 Play Mode 時關閉「Reload Scene」的專案，走訪可能接不到起始頁。
- `HasInspectorListener` 把 Call State 是 Off 的監聽也算進去。

### K8. Editor 結束時偶發當機（觀察）

- 十一次走訪裡有一次，報告已經正確寫出（`Passed`），但 Unity 結束時當掉，結束代碼 139。堆疊在 Unity 內部（`WorkerManagerASIO::OnMessageReceived`），不在套件的程式裡，重跑六次沒有再發生。
- 請在 AgentGuide 註明：結束代碼不是 0、1、2 時代表 Editor 本身異常結束，請以報告檔為準。
- 有簡單的做法可以降低機率就加，但這一項沒辦法確定驗證，交付說明要寫清楚做了什麼。

## 第七輪審核：第三批修正（0.4.0）

J1～J8 都已修正，做法符合 `ROADMAP.md`「第三批修正」的規則，沒有發現新的問題。四組自動測試的數量與結果和實作者的紀錄相同；第六輪的重現步驟全部重跑，結果都已改正；執行檔的 64 項檢查重新建置後再跑一次，沒有退步。**第三批到此通過，可以打 `v0.4.0` 標籤。**

各項的確認方式，J1～J7 用的是第六輪同一份重現腳本：

- **J1：** 白色圖片寫 `#FFD25A`、`Text` 寫 `#FF0000`、`Button` 寫 `textColor: #FFFF00`，之後把三個元件 Prefab 的顏色改成黑色，圖片另外改成半透明。三個實例都維持計畫寫的顏色與透明度，沒寫顏色的圖片跟著 Prefab。
- **J2：**
  - 開著有工作區的 Scene 時，計畫可以建立另一個 Scene，也可以套用到既有的另一個 Scene。
  - 失敗的計畫會把自己開啟的 Scene 關回去，接著對原本的 Scene 套用可以成功。
  - Validate 的四種情況都符合規則：工作區的 Scene 有兩個是 Error；另一個 Scene 也有是 Warning；只有另一個 Scene 有時通過；全部停用是 Error，訊息說明「有 EventSystem 但沒有啟用」。
- **J3：** 登記起始確認框並寫 `defaultSelected: confirm` 可以套用。對 `confirm` 按鈕用 `kind: Text` 的 `updateExisting`，在套用前報錯：「原本是 Button，不能用 updateExisting 改成 Text」。
- **J4：** 產生出來的 `Dropdown.prefab`，捲軸的 `sizeDelta` 是 `(40, 0)`。
- **J5：** 三個有鍵值的選項換成兩個沒寫鍵值的選項，在套用前報錯，訊息請計畫同時提供 `optionKeys` 或用空陣列清除。選項與鍵值都沒有變。
- **J6：** `Slider` 的 `min: 5` 在套用前被擋下，Scene 沒有被標成未儲存。
- **J7：** 照第六輪的步驟，改過元件 Prefab 的文字顏色之後登記確認框，`HasReviewOverrides` 是 false。審核實例上仍有 TMP 自己產生的四筆 `m_fontColor32.rgba`，已被略過。另一個 Scene 裡計畫真的改了 `defaultSelected` 的確認框，仍然正確顯示待套用。
- **J8：** 前三點的修正我讀過程式碼，並由實作者新增的測試覆蓋（測試都通過），沒有另外寫探測。第四點實作者沒有重現，保留了測試。

實作者列的九點「規則沒寫到、實作時決定的地方」都接受，見「驗證紀錄」的「第三批修正（J1～J8）」。其中兩點會影響使用方式，已寫進 AgentGuide 與 CHANGELOG：

- 滑桿的 `value` 超出範圍，現在是套用前報錯，以前會被安靜地夾住。起始滑桿是 0 到 1，要寫更大的值必須同時寫 `max`。
- 已經安裝過起始元件的專案不會自動拿到 J4 的修正，要手動把 `Dropdown.prefab` 裡 `Template/Scrollbar` 的寬度改成 40。目前沒有專案在使用。

第六輪「與規格不同的地方」第 7 點：使用者在 2026-11-20 決定不加「複製 View Prefab 並換 ID」的工具，維持指南裡的做法（用計畫建立新的 Popup，再掛上 `UIConfirmPopup`）。第三批沒有待決定的事項。

## 第六輪審核：第三批（0.4.0）

十一項規格都已實作，執行期的行為逐項對照後與規格相符。四組自動測試的數量與結果和實作者的紀錄完全相同。我另外用操作指南的示例計畫建了一個專案，打包成執行檔，用虛擬的手把、鍵盤與滑鼠走過 64 項檢查，全部通過。細節見「驗證紀錄」的「第六輪」。

**當時的結論：還不能打 `v0.4.0` 標籤。**（J1～J8 之後已修正並通過複驗，見「第七輪審核」。）Editor 端有一個會讓畫面顏色悄悄跑掉的問題（J1），另外有兩個會讓計畫套不進去的情況（J2、J3），請修正後再複驗。其餘項目比較小，同一輪一起處理。修正的規則寫在 `ROADMAP.md` 的「第三批修正」。

- J1～J7 都在副本專案上實際執行確認過。
- J8 是讀程式碼發現的，沒有執行。請實作者先用測試確認，是真的才修。
- J2 的根源是第三批規格 3-2 沒有考慮同時開著多個 Scene 的情況，不是實作偏離規格。

### J1. 計畫寫出的顏色只固定了一部分，改元件 Prefab 的顏色會讓畫面變色

- **嚴重度：** 高。這是「改 Prefab 就會改到所有畫面」的核心流程。
- **位置：** `Editor/UIAuthoring.cs` 的 `PinOverrides`，用 `property.prefabOverride` 判斷要不要固定的地方。
- **原因：** Unity 的覆寫是逐個分量記錄的。顏色只要有一個分量和元件 Prefab 不同，整個顏色欄位的 `prefabOverride` 就是 true，固定的步驟被跳過，其餘和 Prefab 相同的分量沒有成為覆寫。透明度通常兩邊都是 1，所以幾乎不會被固定。
- **重現：** 計畫寫 `{ "id": "icon", "kind": "Image", "color": "#FFD25A" }`，之後把 `Image.prefab` 的顏色改成半透明的黑色。
- **結果：**
  - `icon` 變成 `#00D25A`、透明度 50%，既不是計畫寫的顏色，也不是 Prefab 的顏色。
  - `Text` 寫 `"color": "#FF0000"`、`Button` 寫 `"textColor": "#FFFF00"`，在 Prefab 的文字顏色改成黑色之後都變成黑色。
- **既有測試為什麼會通過：** 測試用的 `#FF0000` 和按鈕底色的三個分量剛好都不同，透明度沒有改。
- **陣列也有同樣的缺口**（實作者列在「與規格不同的地方」第 3 點）：`options` 的數量剛好和元件 Prefab 相同時，陣列長度不是覆寫。專案刪掉 Prefab 裡的範例選項，這些實例的選項會跟著消失。這一點是讀程式碼判斷的，沒有執行。

### J2. 開著另一個有 UI 的 Scene 時，計畫套不進別的 Scene，還可能卡住

- **嚴重度：** 中。CLI 不受影響。在開著的 Editor 裡操作時會遇到，例如透過 Unity MCP。
- **位置：** `Editor/UIValidation.cs` 的 EventSystem 檢查，`Editor/UIPlan.cs` 的 `FindScene` 與失敗時的還原。
- **情況一：** Scene A 開著而且有工作區，套用 `scenePath` 指到另一個新 Scene 的計畫（`createScene: true`）。
  - 新 Scene 以附加方式開啟，建立工作區時加上第二個 EventSystem。
  - 套用完的檢查回報「目前 2 個」，整份計畫被還原。還原是乾淨的。
  - 結果是第二個有 UI 的 Scene 沒辦法從開著的 Editor 建立。
- **情況二：** 目標是已經存在的另一個 Scene。
  - 一樣失敗，而且那個 Scene 留在附加開啟的狀態。
  - 之後**任何計畫都會失敗**，包含指到原本那個 Scene 的，直到有人手動卸載它。
- **根源：** 規格 3-2 把「載入的 Scene 裡不是剛好一個 EventSystem」列為 Error，沒有考慮計畫會把目標 Scene 附加開啟。第二批以前這是 Warning。
- **另外（讀程式碼判斷，沒有執行）：** Scene 裡的 EventSystem 物件未啟用時，建立工作區不會補一個新的，檢查卻回報「0 個，建立工作區時會自動加上」，訊息與實際不符。

### J3. 把既有的 View Prefab 登記成新畫面時，預檢沒有看 Prefab 裡的內容

- **嚴重度：** 中。登記起始確認框、或在第二個 Scene 重用同一個 Popup Prefab 時會遇到。
- **位置：** `Editor/UIPlan.cs` 的 `Preflight`。`existingReview` 只從這個工作區已登記的畫面取得。
- **誤擋：** `{ "id": "confirm", "kind": "Popup", "prefabPath": "Assets/UniGameUI/Widgets/Confirm.prefab", "defaultSelected": "confirm" }` 回報「defaultSelected 指到不存在的元件 confirm」，但 Prefab 裡有這個按鈕。
- **漏擋：** 同一個畫面寫 `"elements": [{ "id": "confirm", "kind": "Text", "updateExisting": true, "text": "Yes", "alignment": "Left" }]`，沒有任何錯誤就套用了。
  - 原本記錄為 `Button` 的元件被當成 `Text` 更新，文字改成 `Yes`。
  - `Button` 不支援的 `alignment` 也寫進去了。
  - 「更新時 `kind` 必須相同」的規則被繞過。

### J4. 起始下拉選單的捲軸寬度是 0

- **嚴重度：** 中低。選項超過展開範圍（大約八個以上）時沒有捲軸可以拉。
- **位置：** `Editor/UIStarterWidgets.cs` 建立下拉選單範本的地方。程式先設定直向捲軸的尺寸，之後 `scrollbar.SetDirection(BottomToTop, true)` 把寬高對調。
- **結果：** 產生出來的 `Dropdown.prefab` 裡，捲軸的 `sizeDelta` 是 `(0, 40)`。

### J5. 更新下拉選單的選項後，舊的多語系鍵值還留著

- **嚴重度：** 低。
- **重現：** 先建立三個選項與三個 `optionKeys`，再用 `updateExisting` 只寫 `"options": ["On", "Off"]`。
- **結果：** 選項變成兩個，鍵值仍是原本三個，沒有報錯。執行期「On」會用「Low」的鍵值取值，顯示成錯的文字。

### J6. 有些錯誤要套用到一半才會發現

- **嚴重度：** 低。還原機制有確實復原，沒有留下半套的變更。
- **例子：** `{ "kind": "Slider", "min": 5 }`（起始滑桿的 max 是 1）在建立了前面的元件之後才報錯。前面建立的元件有還原，Scene 檔案沒有變，但 Scene 被標成未儲存。
- **同一類的還有（讀程式碼判斷）：**
  - 下拉選單的 `value` 或 `optionKeys` 數量和既有選項不合。
  - `spacing` 用在內容物件沒有版面配置元件的清單。
  - 自訂元件的 Prefab 缺少該欄位需要的元件，例如比照 `Button` 卻沒有文字，計畫又寫了 `text`。
  - `overlayClick: Close` 用在沒有遮罩的 Popup。
- 這些都能在預檢時從元件 Prefab 或既有的實例算出來。

### J7. 改過元件 Prefab 的文字顏色之後登記確認框，會出現不存在的「待套用」

- **嚴重度：** 低。
- **重現：** 用程式把 `Text.prefab` 與 `Button.prefab` 的文字顏色改成黑色，再用計畫把起始確認框登記到一個新的 Scene。
- **結果：** `UIAuthoring.HasReviewOverrides` 對確認框回傳 true。四筆覆寫都是 TMP 自己衍生的 `m_fontColor32.rgba`，數值和 Prefab 傳下來的顏色相同。藍圖會顯示待套用，進 Play 前也會跳出詢問。
- 顏色改變之前就登記好的確認框沒有這個現象。在 Inspector 手動改顏色會不會觸發，沒有驗證。

### J8. 讀程式碼發現、沒有實際執行的項目

請先各寫一個測試確認，是真的才修。

- **`defaultFontPath` 在 Scene 存檔之後才套用**（`Editor/UIPlan.cs` 的 `Apply`）。換字型失敗時，Scene 已經存檔，字型換到一半，元件集的預設字型也沒有更新。
- **`placeholder` 被安靜略過。** 自訂的輸入框類元件沒有 TMP 的 placeholder 時，`placeholder`、`placeholderKey` 什麼都沒寫，也沒有報錯（`Editor/UIPlanElements.cs`）。
- **`SetDefaultFont` 可能換掉專案改過的字型。** 對輸入框是設定 `field.fontAsset`，TMP 會連 placeholder 一起換，包含已經被專案改成別的字型的那一個（`Editor/UIWidgets.cs` 的 `ReplaceFont`）。
- **Play Mode 中按 Validate 可能誤報。** 沒有設定 `defaultSelected` 的畫面可能被判成「已經遺失」（`Editor/UIValidation.cs` 用 `!ReferenceEquals(selected, null) && !selected` 區分）。

### 文件要補的說明

- 「點遮罩關閉」所說的內容範圍是 `Content` 的矩形，不是看得見的美術。計畫建立的 Popup，`Content` 固定是畫面中央 60% 的範圍；對話框比它小時，範圍內的暗色區域點了不會關閉。要讓範圍貼齊對話框，請調整 `Motion` 的大小。

### 「與規格不同的地方」的審核結果

實作者列了十二點，見「驗證紀錄」的「第三批改進（0.4.0）」。

- **第 1、2、4、5、6、8、9、10、11、12 點：接受。** 第 5 點（往前切頁或開啟 Popup 時從預設選取元件開始，關閉 Popup 或返回時才恢復上次的選取）比規格原本的寫法合理，`ROADMAP.md` 已照這個改。
- **第 3 點：** 固定覆寫的做法接受，但目前只固定了一部分，陣列也沒有處理，見 J1。
- **第 7 點（複製確認框不能換畫面 ID）：** 要不要加「複製 View Prefab 並換 ID」的工具，由使用者決定。Claude 的建議是不加，指南裡的做法（用計畫建立新的 Popup，再掛上 `UIConfirmPopup`）已經夠用。**使用者在 2026-11-20 決定不加。**

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

### 第十輪：Claude 對第五批的驗證

使用 Unity 6000.3.19f1，沿用前幾輪的兩個副本專案，套件內容用 `git archive` 從 `8cf4285` 匯出。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 176 | 0 | 0 |
| 有 DOTween | PlayMode | 81 | 0 | 1 |
| 無 DOTween | EditMode | 106 | 0 | 70 |
| 無 DOTween | PlayMode | 76 | 0 | 6 |

四組的數量與結果都和實作者的紀錄相同。

- **命令列：** 第九輪的 12 種情況重跑，報告的內容都和第九輪相同。結束代碼有一次不同：加 `-unigameui-include-emit` 的走訪是 139（K8），其餘相同。
- **事件接線的探測：** 11 項檢查全部通過，內容見「第十輪審核」。
- **驗證過程的說明：** 探測的第一版有兩項失敗，原因是我讀錯了狀態（拿節點的畫布座標判斷有沒有平移，而平移本來就不會改它）。改成讀視角的平移量之後通過。

**這一輪沒有驗證的：** 所有外觀；雙擊定位、連接點拖線與右欄分隔線的事件接線；視角在 Play Mode 與 Domain Reload 之後是否保留；視窗有實際大小時的版面。

### 第五批（5-1～5-5）

以下是實作者（Claude 的子 session）自己的紀錄，等主 session 審核。分支 `feat/batch-5`，每一項各自 commit，已快轉合併回 `main`。`package.json` 是 0.6.0，沒有打標籤，沒有 push。`Runtime` 沒有改動，藍圖的資料格式沒有變。

| 項目 | commit | 做了什麼 |
|---|---|---|
| 5-1 | `ee3938c` | 畫布改成沒有邊界、可以平移與縮放。座標換算集中在 `UIBlueprintViewport`（畫布座標與視窗座標互換、以某一點為中心縮放、算出「顯示全部」的視角）。滾輪以游標為中心縮放（25%～200%），中鍵或 Alt 加左鍵拖曳平移，格線跟著移動。拿掉捲軸與「座標不可小於 12」的限制。工具列新增「顯示全部」（F）與縮放百分比（點一下回到 100%）。視角依 Scene 記在 `SessionState`，不寫進 Scene；這個工作階段第一次開啟時自動顯示全部。節點拖曳的距離除以縮放比例，拉線的命中判定用畫布座標。 |
| 5-2 | `00451e0` | 虛線的範圍框圍住全部節點（含「任何畫面」），外擴 36，左上角的說明是「Scene 名稱 · N 個頁面 · N 個 Popup」。節點移動（拖曳途中也是）、新增、刪除時更新。`PickingMode.Ignore`，不接收滑鼠事件。 |
| 5-3 | `6edea60` | 整個節點都能選取與拖曳，連接點仍然是拉線。單擊只選取，雙擊定位到 Scene，點空白取消選取。線上的動作標籤可以點，選取來源節點與那條路由。節點用徽章標示種類、起始頁、由程式控制。滑鼠移上去的回饋（邊框、游標）寫在 USS。 |
| 5-4 | `7ec3e00` | 右欄移到新檔案 `UIBlueprintWindowSidebar.cs`，分成可以收合的區塊。寬度可以拖曳（預設 360，最小 300），寬度與收合狀態記在 `SessionState`。路由清單整列可點，「通到這裡的路由」包含訊號路由。編輯區只在選了路由或按「新增路由」後出現，只顯示該行為用得到的欄位。動作重複、名稱不合規則、沒有目的地時不儲存，原因顯示在欄位下面。 |
| 5-5 | `e6b91cc` | `view.no-exit`：被 `history: Clear` 的 `OpenPopup` 路由（一般或訊號）開啟的 Popup，只有 `Back` 路由、沒有 `ClosePopup` 或 `ShowPage`、也沒有點遮罩關閉時回報，訊息指出是哪一條路由以不可返回的方式開啟它。只改 `Editor/UIValidationChecks.cs`。 |
| 文件 | `763642a` | AgentGuide 的「審核」一節改寫、問題代碼表與不可返回 Popup 的說明更新；套件的 `README.md`、`CHANGELOG.md`；`package.json` 升到 0.6.0。 |

**測試結果**

Unity 6000.3.19f1，在兩個副本專案執行（套件內容與 `763642a` 相同）。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 176 | 0 | 0 |
| 有 DOTween | PlayMode | 81 | 0 | 1 |
| 無 DOTween | EditMode | 106 | 0 | 70 |
| 無 DOTween | PlayMode | 76 | 0 | 6 |

EditMode 比第四批多 12 個：`UIBlueprintTests.cs` 7 個（5-1～5-3）、`UIBlueprintSidebarTests.cs` 4 個（5-4）、`UIAuthoringBatch4ChecksTests.cs` 1 個（5-5，六種情況寫在同一個測試裡）。PlayMode 的數量沒有變。Editor 的 log 裡沒有 `UIBlueprint.uss` 的匯入警告。

**測試是怎麼測的（重要）**

視窗的互動都拆成可以直接呼叫的函式，測試呼叫的是這些函式，**沒有送出任何滑鼠或鍵盤事件**，視窗也沒有真的顯示出來（沒有版面配置，畫布大小用後備值 960×600）：

- 視角：`UIBlueprintViewport` 的換算，以及視窗的 `ZoomAt`、`SetZoom`、`PanBy`、`ShowAllNodes`。
- 節點與拉線：`PressNode`（參數是按到的元素與點擊次數）、`MovePointer`、`ReleasePointer`、`ClickCanvas`、`PressEdgeLabel`、`BeginWire`、`DropWire`。
- 右欄：按鈕用反射呼叫 `Clickable.Invoke`（和既有測試相同）；換行為用 `RouteKindChanged`；收合用 `SetSectionCollapsed`；寬度用 `RememberSidebarWidth`。

所以「滑鼠事件有沒有正確接到這些函式」完全沒有自動測試，要靠下面的人工確認。

**與規格不同、或規格沒寫而我自己決定的地方**

1. **既有測試改了一處。** `BlueprintShowsTheAnySourceNodeAndEditsSignalRoutes` 原本選取「任何畫面」後直接使用編輯欄位。規格要求編輯區在選了路由或按「新增路由」後才出現，所以測試多了一步 `BeginAddRoute()`，並加一行確認在那之前沒有編輯區。其餘既有測試沒有改。
2. **欄位名稱維持「目的地」。** 規格寫「目標」，但既有的介面、文件與測試都用「目的地」，沒有改。要統一成「目標」請告訴我。
3. **「行為」選單的文字改了。** 原本是 `Show Page` 這種英文，現在是「切頁（ShowPage）」，和節點、清單的徽章用同一個說法。
4. **「顯示全部」不會放大超過 100%。** 節點很少時維持 100% 並置中，只有放不下時才縮小（最小 25%；節點散得太開、25% 也放不下時，會有節點在視窗外）。
5. **選著一條路由時拉線，是重接那條路由，不是新增。** 這是原本就有的行為，沒有改。要新增第二條，先點空白或按「新增路由」。
6. **雙擊「任何畫面」只會選取。** 它不是畫面，Scene 裡沒有東西可以定位。
7. **還沒存檔的 Scene 共用同一個視角。** 視角用 Scene 的 GUID 記，未命名的 Scene 沒有 GUID。
8. **右欄的寬度在視窗被縮窄而擠壓右欄時也會被記下來**（不小於 300）。我沒有辦法分辨寬度是使用者拖的還是視窗擠的。
9. **起始頁的下拉選單只在「工作區」區塊。** 原本不論選什麼都在最上面；現在選取頁面時用「設為起始頁」按鈕。
10. **編輯區換行為時，目的地、返回歷史、關閉結果會回到預設值。** 這是原本就有的行為。
11. **儲存路由時其他的錯誤也顯示在編輯區**（`route-error`），不跳對話框。拉線、刪除、設為起始頁等其他操作出錯時仍然是對話框。
12. **5-5 的訊息只指出第一條不可返回的路由。** 同一個 Popup 被好幾條 `Clear` 路由開啟時不會全部列出。完全沒有出口路由的 Popup 照舊用原本的訊息。
13. **安裝網址沒有改。** 根目錄與套件的 `README.md` 裡的 `#v0.5.0` 維持原樣，因為 `v0.6.0` 標籤還不存在；根目錄 `README.md` 的版本表也還沒有加 0.6.0 那一列。打標籤時請一併更新。
14. **節點多了一行不拉線的動作**（例如「ping（送出事件）」）。這是 4-7 就有的，這次只把說法改成和右欄一致。

**沒有驗證的部分**

- 所有滑鼠與鍵盤事件的接線：滾輪、中鍵拖曳、Alt 加左鍵拖曳、F 鍵、節點的按下／移動／放開與指標擷取、雙擊（靠事件的 `clickCount`）、點畫布空白、點線上的標籤、點區塊標題收合、拖曳右欄的分隔線。
- 所有外觀：USS 的顏色、邊框、徽章、游標樣式、滑鼠移上去的回饋；`Painter2D` 畫的格線、虛線框與連線；縮放後文字清不清楚。
- 視角、右欄寬度、收合狀態在 Play Mode 與 Domain Reload 之後是否保留。它們存在 `SessionState`，測試只確認了重建視窗與另開一個視窗實例之後還在，沒有真的重新載入。
- 視窗真的有版面配置時的行為：第一次有實際大小時重新「顯示全部」、右欄內容很長時的捲動、視窗很窄時兩邊的最小寬度。
- 沒有在互動 Editor 裡開過這個視窗。

**人工確認清單**

請在互動 Editor 開啟一個有幾個頁面、Popup 與訊號路由的 Scene，**Tools → UniGameUI → Blueprint**：

1. 滾輪縮放：游標底下的那一點不動；縮到 25%、放到 200% 就停；工具列的百分比跟著變，點一下回到 100%。
2. 按住中鍵拖曳、按住 Alt 加左鍵拖曳：畫布平移，格線跟著動，放開後節點位置沒有被改掉（Scene 沒有變成未儲存）。
3. 「顯示全部」與 F 鍵（先點一下畫布）：所有節點都在視窗裡。關掉視窗再開、進出 Play Mode 之後，視角還在。
4. 範圍框：虛線圍住所有節點，拖曳節點時跟著變，左上角的文字正確，而且不會擋住點擊。虛線在各種縮放比例下看起來粗細一致。
5. 節點：滑鼠移上去有回饋，游標變成移動的樣子；移到右邊的連接點時游標不同。按在標題、ID、空白處都能拖曳；在 50% 與 200% 拖曳時節點跟著游標走。
6. 單擊只選取、Scene 視窗不動；雙擊才定位。點空白處取消選取，右欄回到「工作區」。
7. 從連接點拉線到另一個節點（在非 100% 的縮放下也試一次）：線的終點跟著游標，放開後建立路由。
8. 線上的動作標籤：滑鼠移上去有回饋，點了之後選取來源節點，右欄選中那條路由。
9. 右欄的四種狀態（沒有選取、頁面、Popup、「任何畫面」）：區塊的順序、標題列、邊框與間距；徽章的顏色在節點、線上的標籤、右欄三處一致。
10. 路由清單：整列可以點，選中的那一列夠明顯。「通到這裡的路由」點一列會跳到來源節點。
11. 編輯路由：故意填重複的動作、不選目的地，錯誤出現在欄位下面而且看得清楚。「刪除這條路由」和「儲存」不會按錯。
12. 拖曳畫布與右欄之間的分隔線：寬度會變、不會小於最小寬度，重新整理後還在。右欄內容太長時可以捲動。點區塊標題可以收合，換選取後仍然是收合的。
13. 25% 與 200% 時節點上的文字讀不讀得清楚。

### 第九輪：Claude 對第四批的完整驗證

使用 Unity 6000.3.19f1，沿用前幾輪的兩個副本專案。套件內容用 `git archive` 從 `f0585c2` 匯出，沒有動到工作目錄。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 164 | 0 | 0 |
| 有 DOTween | PlayMode | 81 | 0 | 1 |
| 無 DOTween | EditMode | 94 | 0 | 70 |
| 無 DOTween | PlayMode | 76 | 0 | 6 |

四組的數量與結果都和實作者的紀錄相同。

**命令列的實際執行（12 次，都由工具自己結束 Editor）**

| 入口與情況 | 結束代碼 | 結果 |
|---|---:|---|
| Validate，操作指南的示例場景 | 0 | Error 0、Warning 2，都是確認框「確定」「取消」的 `text.missing-glyph`。 |
| Validate，同上加 `-unigameui-strict` | 1 | `passed: false`。 |
| Validate，Scene 不存在 | 2 | `error` 說明找不到這個 Scene。 |
| 走訪，示例場景加上指南的範例腳本 | 0 | `Passed`，通過 5（起始加四條路由）、略過 1（`Emit`）。 |
| 走訪，同上加 `-unigameui-include-emit` | 0 | `Passed`，通過 6。 |
| 走訪，Scene 不存在 | 2 | `NotRun`。 |
| 走訪，`-unigameui-timeout 1` | 2 | `NotRun`，原因是超過整體的時間上限，沒有卡住。 |
| 走訪，商店頁掛上 `OnShowing` 一定丟例外的腳本 | 1 | `Failed`。`workshop/openShop` 與 `shop/back` 失敗，報告裡有例外訊息；其他步驟照常。 |
| Validate，第八輪的重現場景 | 0 | Error 0、Warning 0。 |
| 走訪，第八輪的重現場景 | 1 | `Failed`，通過 5、失敗 3、略過 1，和修正後預期的結果相同。 |
| Validate，有訊號路由的場景 | 0 | Error 0、Warning 0。 |
| 走訪，有訊號路由的場景 | 0 | `Passed`，通過 11、略過 4。 |

**執行檔（Windows 64 位元、Mono、High Managed Stripping、含 DOTween）**

- 有訊號路由的場景：五個頁面、三個 Popup、四條訊號路由，`hud` 與 `shop` 用 0.4 秒的淡入淡出讓轉場跨影格。23 項檢查全部通過，結束碼 0。內容見「第九輪審核」的表格。
- 第三批的探測重跑一次：64 項檢查全部通過，結束碼 0。這一批改了 `Runtime` 的返回與 Popup 開啟流程，目的是確認沒有退步。

**驗證過程的說明**

- 第一次套用訊號場景的計畫被擋下，原因是我用了和前一個場景同名的 Popup，兩個場景因此共用同一個 Prefab，而 Prefab 裡的按鈕在新場景沒有對應的路由。這是框架正確的行為，訊息也指出了是哪個按鈕。我把這個場景的 Popup 改放到自己的資料夾之後重跑。
- 第三批探測附帶的 Editor 抽查有一項不再成立：它預期示例場景的 Validate 沒有任何問題，而第四批依規格會回報確認框的兩筆缺字警告。這是預期中的新行為。
- 第八輪初步審核時，11 次走訪裡有一次 Editor 在結束時當機（K8）。這一輪的 12 次命令列沒有再發生。

### 第四批修正（K1～K8）

以下是實作者（Claude 的子 session）自己的紀錄，等主 session 複驗。修正的對象是「第八輪審核」的 K1～K8，版本維持 0.5.0。分支 `feat/batch-4`，每一項各自 commit（K7 分成 Validate 與走訪兩個），已快轉合併回 `main`，沒有 push，沒有打標籤。

| 項目 | commit | 怎麼修 |
|---|---|---|
| K1 | `cec8059` | 一步期間的訊息改由 `StepLogs` 管理。「有沒有 Error」看的是每一筆收到的訊息，和報告留幾筆無關。報告每一步最多 50 筆，Error、Exception、Assert 優先，其餘位置給 Warning，留下來的照原本的順序；沒有列出來的筆數寫在 `logsOmitted`。 |
| K2 | `62cbd6d` | 候選頁面（來源頁之前的「另一個頁面」、Popup 底下的頁面、訊號路由的出發頁）先排除由程式控制的頁面，沒有其他候選時才用。候選顯示不出來而改試下一個時，它記的訊息標上 `note`、不算進判定。來源是頁面的 `Back`，候選只有由程式控制而且都顯示不出來時，記為 `Skipped`。 |
| K3 | `424e333` | Popup 底下的頁面改成沿著 `OpenPopup` 路由往回找（經由別的 Popup 開啟的也算）。只會開在目標頁上面時就開在那一頁，這一步如實失敗，原因說明這件事。 |
| K4 | `06ebb1d` | `view.no-exit`：頁面開得到的 Popup（Popup 再開的 Popup 也算）有切到別頁的 `ShowPage` 路由，就算有出口。`ROADMAP.md` 4-3 的表格與 AgentGuide 已更新。 |
| K5 | `18888c8` | 拆成一個代碼一種問題，見下面的對照表。問題代碼從 41 個變成 46 個。 |
| K6 | `ed32386` | `UIValidationReport` 多一個 `Error`：每一個 Scene 都沒有工作區時是原因，`Passed` 是 false，JSON 相同。同一個 Scene 只檢查一次。未命名的 Scene 和存過檔的 Scene 一起開著時會補回來，作用中的 Scene 不變。 |
| K7 | `6f30547` | Validate 這一側：Call State 是 Off 的監聽、缺字檢查的四點。 |
| K7 | `8db903b` | 走訪這一側：進入途中回到 Edit Mode、命令列啟動時的 Domain Reload、Error Pause、關閉 Reload Scene。 |
| K8 | `9de72df` | 命令列的走訪寫完報告後多等 10 次 Editor 更新才結束 Editor；AgentGuide 註明異常的結束代碼。 |

**K5 的代碼對照**

| 原本 | 現在 |
|---|---|
| `route.invalid`（三種情況） | `route.incomplete`（來源或動作是空的）、`route.source-missing`（來源畫面不存在）、`route.duplicate`（同一個來源有重複的動作）。`route.invalid` 不再使用。 |
| `route.target-missing`（不存在，或種類不符） | `route.target-missing`（不存在或沒有填）、`route.target-kind-mismatch`（種類不符）。 |
| `element.id-duplicate`（空的，或重複） | `element.id-empty`、`element.id-duplicate`。 |
| `constants.outdated`（沒有產生、過期，或只缺 asmdef） | `constants.outdated`、`constants.asmdef-missing`（常數檔是最新的，只缺 asmdef）。 |

**K7 各點的確認結果**

每一點都先寫了測試（或實際執行），在修正前確認，再修。

| 項目 | 修正前的確認 | 結論與修正 |
|---|---|---|
| `EnteredEditMode` 沒有檢查階段 | **沒有重現。** 在 `ExitingEditMode` 用 `EditorApplication.isPlaying = false` 取消進入 Play Mode，Unity 不會發出 `EnteredEditMode`；走訪走的是既有的後備路徑，30 秒後結果本來就是 `NotRun`（案例 `WalkCancelledWhileEnteringPlayModeIsNotRun` 在修正前就通過）。 | 讀程式碼，這個分支確實會得到「`Passed`、0 步」，只是找不到會觸發它的操作。仍然加上階段檢查（成本很低），另用直接呼叫處理函式的案例 `ReturningToEditModeBeforePlayModeWasEnteredIsNotRun` 確認結果是 `NotRun`。 |
| 命令列啟動時的 Domain Reload | **重現。** 在副本專案加一個只在命令列帶 `-probe-reload` 時、啟動後第二次更新要求重新載入腳本的 Editor 腳本。修正前：沒有報告，Editor 不結束，150 秒後由外部終止。 | 排定的走訪一解析完參數就存進 SessionState 並設好期限，重新載入之後由 `Watch` 接著開始。修正後同一個指令結束代碼 0、有報告（見命令列的表格）。自動測試 `CommandLineStartSurvivesADomainReload`。 |
| `LoadFace` 的快取 | **重現。** 兩個來源不同的動態字型（標楷體、LiberationSans），中間讀一次另一個字型還沒建立的對照表：第三段文字的「定」被誤報為缺字。 | 每次查詢前都重新載入字型，不再記目前載入的是哪一個。案例 `GlyphCheckAsksTheRightFontAfterAnotherDynamicFontWasRead`（需要 Windows 的 `kaiu.ttf`，沒有時略過）。 |
| 富文字標籤 | **重現。** `<size=120%>H</size>` 被報成缺 26 個字元。 | 開著富文字時拿掉 TMP 認得的標籤；不是標籤的角括號、TMP 不認得的標籤、`<noparse>` 的內容照舊檢查；`<font>` 裡的文字不檢查。 |
| Sprite Asset | **重現。** 文字的 Sprite Asset 有對應的碼位，仍然被報成缺字。 | 依 TMP 的順序加上文字的 Sprite Asset 與 TMP Settings 的預設 Sprite Asset（含各自的 fallback）。從序列化的資料讀，不呼叫會升級舊格式資產的 API。 |
| 清單項目裡的文字 | **重現。** 清單項目裡的文字沒有被檢查。 | 缺字檢查不再排除清單項目；`button.unbound` 照舊不算清單項目。 |
| Error Pause | **重現。** 用 Editor 內部的 `ConsoleWindow.SetConsoleErrorPause` 開啟後走訪：停在記錯誤的那一步，92 秒後整體逾時，結果是 `NotRun`。 | 走訪期間 `Watch` 發現 Editor 被暫停就讓它繼續，並在報告的 `notes` 記一筆。修正後結果是 `Failed`，後面的步驟照常。 |
| 關閉 Reload Scene | **重現。** 走訪通過，但起始頁在 `OnShown` 記的警告沒有收到，畫面清單把起始頁列成沒有顯示過。關閉與開啟 Domain Reload 兩種組合都一樣。 | 另外從 `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 接上（時間點和 `sceneLoaded` 相同：`Awake` 之後、`Start` 之前），接上時已經顯示著的畫面也算顯示過。 |
| `HasInspectorListener` | **重現。** Call State 是 Off 的監聽被當成有接監聽。 | 只算 Call State 不是 Off 的監聽。 |

**K8 做了什麼**

- AgentGuide「驗收」開頭註明：結束代碼只有 0、1、2；拿到別的數字代表 Editor 本身異常結束，以報告檔為準，報告不存在才需要重跑。
- 命令列的走訪寫完報告後不馬上呼叫 `EditorApplication.Exit`，改成再等 10 次 `EditorApplication.update`。結束代碼存在 SessionState，這段時間 Domain 重新載入也不會忘記要結束。
- **這一項沒辦法確定驗證。** 當機是偶發的，我這邊在這一輪與之前總共幾十次命令列執行裡沒有遇過結束代碼 139，所以無從比較修改前後的機率。能確認的只有：修改後每一種情況的結束代碼都還是對的，沒有任何一次卡住。

**四組測試**

和前幾輪同一組副本專案，Unity 6000.3.19f1。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 164 | 0 | 0 |
| 有 DOTween | PlayMode | 81 | 0 | 1 |
| 無 DOTween | EditMode | 94 | 0 | 70 |
| 無 DOTween | PlayMode | 76 | 0 | 6 |

- 4-7 交付時的基準是 140、81 加 1 略過、80 加 60 略過、76 加 6 略過。既有案例全部通過。
- 新增 EditMode 24 個，PlayMode 沒有新增：走訪 10 個（K1 一個、K2 兩個、K3 一個、K7 六個）、問題代碼的重現案例淨增 5 個、`EachCodeReportsOnlyItsOwnProblem`、K4 一個、K6 兩個、K7 的 Validate 五個。
- 無 DOTween 時多略過 10 個：八個建立工作區來走訪的案例，與 K6 的兩個案例。
- 走訪的十個新案例裡，七個實際進出 Play Mode。其餘三個：取消進入 Play Mode 的案例、直接模擬狀態改變的案例，以及只重新載入 Domain 的命令列案例。

**命令列的實際執行**

和之前同一個副本專案，重新建立。另外建立審核者重現用的場景 `Review.unity`（內容見「第八輪審核」）。

| 入口與參數 | 結束代碼 | 報告摘要 |
|---|---:|---|
| Validate `Review.unity` 加 `-unigameui-strict` | 0 | `passed: true`，Error 0、Warning 0。`game` 沒有 `view.no-exit`（K4）。 |
| 走訪 `Review.unity` | 1 | `Failed`，通過 5、失敗 3、略過 1。`game/pause` 通過，而且沒有任何記錄：`result` 沒有被拿來當候選（K2）。`pause/restart` 失敗，`result` 是 `Unavailable`，原因是「Popup『pause』只會開在『game』上面……實際遊玩時這個動作不會有反應」（K3）。`title/noisy` 與 `noisy/back` 失敗，原因是那筆 Error；各列出 49 筆 Warning 加那一筆 Error，`logsOmitted` 是 11（K1）。`result/retry` 略過。 |
| Validate，同一個沒有工作區的 Scene 給兩次 | 2 | 報告只列一次這個 Scene，`passed: false`，`error` 是「指定的 Scene 都沒有 UniGameUI 工作區」。 |
| Validate，同一個有 Error 的 Scene 給兩次 | 1 | 報告只列一次這個 Scene，Error 1 筆（`start.unknown`），沒有重複計算。 |
| 走訪 `Workshop.unity`，啟動後第二次更新發生 Domain Reload | 0 | `Passed`，通過 5、略過 1，和沒有重新載入時相同，整個程序 11 秒。修正前同一個指令不會結束。 |

- 原本的二十一種情況也重新執行，結束代碼和上一次相同。二十六次都由工具自己結束 Editor。
- 實際打包的執行檔（`Match.unity`，Mono、High stripping）重跑一次：結束代碼 0，六個階段全部通過。這一輪沒有改 `Runtime`。

**與要求不同、或要求沒寫到而由實作者決定的地方**

1. **K1：** 報告的欄位叫 `logsOmitted`（每一步）。留在記憶體裡的訊息另有上限（Error 與 Warning 各 200 筆），超過的只計數，Error 仍然算進判定。
2. **K2：** 不是非有上一頁不可的路由（`Back` 以外），完全不會拿由程式控制的頁面當候選。被放棄的候選不論後來有沒有別的候選成功，它記的訊息都不算進這一步的判定；那個頁面的問題由以它為來源或目標的步驟回報。每一筆記錄多一個 `note` 欄位。
3. **K3：** 有好幾個頁面開得到同一個 Popup 時，`ShowPage` 的路由用目標以外的那一個，這一步會通過。**由程式或訊號開啟的 Popup（沒有任何頁面的路由通到它，或它標記為由程式控制）仍然會避開目標頁**：它可能出現在任何頁面上，「只會開在 X 上面」不成立，照規格用起始頁而起始頁剛好是目標時，會把走得通的路由報成失敗（AgentGuide 的 `Match.unity` 示例就是這種情況）。這和「不要為了避開目標而換頁」的要求有出入，請判斷。
4. **K4：** Popup 的 `ShowPage` 切回這個頁面本身不算出口（`game/pause`、`pause/restart → game`）。訊號開啟的 Popup 不算（訊號路由不屬於任何畫面）。
5. **K5：** 只拆了審核點名的四個代碼。`view.id-invalid`（空的或重複的畫面 ID）、`view.review-missing` 與 `view.prefab-missing`（遺失、缺少引用，或畫面 ID 不符）也各包了不只一種情況，沒有動，請決定要不要一起拆。`constants.asmdef-missing` 只在常數檔是最新的時候回報；常數檔也過期時只回報 `constants.outdated`。
6. **K6：** `UIValidationReport` 多了公開的 `Error`。未命名的 Scene 沒有內容可以留，補回來的是一個新的未命名 Scene（原本有物件時用預設的攝影機與燈光）。
7. **K7：** `EnteredEditMode` 那一點沒有重現，仍然修了（見上表）。富文字只處理 TMP 內建的標籤清單；`<uppercase>` 之類會改變大小寫的標籤與元件的 Font Style 沒有另外處理。`UIWalk` 多了三個只給測試用的 internal 掛點（不結束 Editor、延後開始、模擬 Play Mode 狀態改變）。
8. **K8：** 見上面的說明。

**沒有驗證的**

- K8 的修改有沒有降低當機的機率。
- 互動 Editor 裡的 Error Pause（自動測試是用內部 API 在批次模式開啟的）。
- 4-7 列的項目照舊：藍圖視窗的互動操作、實體裝置、IL2CPP。

### 第四批追加：4-7 訊號與「任何畫面」節點（0.5.0）

以下是實作者（Claude 的子 session）自己的紀錄，等主 session 獨立審核。4-7 是 2026-11-20 在 4-1～4-6 交付之後追加的，版本維持 0.5.0。分支 `feat/batch-4` 已快轉合併回 `main`，沒有 push，沒有打標籤。

| commit | 做了什麼 |
|---|---|
| `1ac6262` | 把 4-7 的規格原文加進 `ROADMAP.md`，「已定案的決定」多兩列（訊號、不可返回）。 |
| `b33dc06` | 4-7 本體。執行期：`UIRoot.AnySource`、`SignalAsync`、`OpenPopupAsync` 可以指定 `UIHistoryMode` 的多載、不可返回的 Popup 的五條規則、`OpenPopup` 路由的 `history`、`UIRoute.IsSignal`。Editor：`UIAuthoring` 的 `Connect`、`UpdateRoute`、`Disconnect`、`PositionNode` 接受 `UIRoot.AnySource`，計畫的 `"source": "*"`，藍圖視窗的「任何畫面」節點與 `OpenPopup` 的「返回歷史」選單，兩個新的問題代碼。 |
| `3de9c6c` | 4-3 的調整：訊號路由的目標算走得到，訊號路由不算任何畫面的出口。 |
| `ae93880` | 4-5 的調整：訊號路由也走訪；`history` 是 `Clear` 的確認；目標畫面由程式控制而顯示失敗時略過；遇到 `Busy` 等待後重來。 |
| `b8cc23a` | 文件：AgentGuide 的「由遊戲事件觸發的跳轉」一節與其他段落的更新、README、CHANGELOG。 |

**四組測試**

和前幾輪同一組副本專案，Unity 6000.3.19f1。測試的程式碼和 `b8cc23a` 的內容逐檔相同。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 140 | 0 | 0 |
| 有 DOTween | PlayMode | 81 | 0 | 1 |
| 無 DOTween | EditMode | 80 | 0 | 60 |
| 無 DOTween | PlayMode | 76 | 0 | 6 |

- 4-1～4-6 交付時的基準是 131、68 加 1 略過、74 加 57 略過、64 加 5 略過。既有案例全部保留並通過；其中兩個走訪的案例加了訊號路由，預期的步驟數跟著改。
- 新增 PlayMode 13 個（`UISignalTests.cs`）。無 DOTween 時多略過 1 個：轉場進行中送出訊號的案例需要跨影格的轉場。
- 新增 EditMode 9 個：兩個新代碼各一個重現案例（代碼共 41 個）、訊號路由的 `UIAuthoring`、Validate、計畫、藍圖視窗各一個、4-3 的走得到判定一個、走訪兩個。無 DOTween 時多略過 3 個：套用計畫的案例與兩個走訪的案例。

**PlayMode 的案例（規格的驗收清單逐項對應）**

| 規格 | 案例 |
|---|---|
| 三種種類的訊號；`data` 送達目標 | `ShowPageSignalsWorkFromAnyViewAndDeliverData`（從起始頁與另一個頁面送出同一個訊號、已經在目標頁是 `Unavailable`、`ActionTriggered` 與 `ActionCompleted`）、`OpenPopupSignalsStackOnTopOfWhateverIsOpen`、`EmitSignalsReachTheTopViewAndKeepApartFromViewActions`（最上層的畫面收到、`ActionEmitted` 的來源是 `*`、和畫面同名的動作互不相干、有給 `data` 記警告）。 |
| 找不到路由 | `SignalsWithoutAUsableRouteAreReported`：`NotFound` 與錯誤訊息；畫面自己的動作不是訊號；`TriggerAsync(UIRoot.AnySource, …)` 是 `Unavailable`；送出前 token 已取消是 `Canceled`；送出前工作區已停用是 `Unavailable`。 |
| 轉場進行中送出；連續送出兩個；在 `OnShown` 裡送出 | `SignalSentDuringATransitionWaitsForItAndReturnsItsOwnResult`（需要 DOTween：兩個訊號在 0.6 秒的轉場期間送出，事件的順序證明它們在轉場結束後才依序執行）、`SignalsSentDuringANavigationRunAfterItInOrder`（在 `OnShown` 裡送出兩個訊號，有無 DOTween 都執行）。 |
| 等待中取消 token、停用工作區 | `WaitingSignalsAreCanceledByTheirTokenOrByDisablingTheWorkspace`，另外確認被取消的訊號不會卡住後面的訊號。 |
| 有 Popup 開著時送出 `ShowPage` 訊號；`ShowPage` 加 `Clear` | `ShowPageSignalWithClearClosesPopupsAndLeavesNowhereToGoBack`。 |
| 有 Popup 開著時送出 `OpenPopup` 訊號 | `OpenPopupSignalsStackOnTopOfWhateverIsOpen`。 |
| 規則一、二：清空歷史、返回鍵 | `UnreturnablePopupClearsHistoryAndIgnoresTheBackKey`：Esc 與手把取消鍵都無效而 `OnBackRequested` 被呼叫了兩次、沒有觸發 `BackRequestedAtRoot`、`BackAsync()` 是 `Unavailable`；點遮罩可以關閉，關閉後歷史是空的。 |
| 規則二：`Back` 路由 | `BackRoutesFromAnUnreturnablePopupAreUnavailable`（用一般路由的 `OpenPopup` 加 `Clear` 開啟），並確認 `Replace` 丟出 `ArgumentException`。 |
| 規則三：從它切頁後底下的頁面不在歷史裡 | `PageChangesFromAnUnreturnablePopupKeepTheHistoryEmpty`：同一條 `Push` 路由從一般 Popup 觸發時照常進歷史；不可返回的 Popup 被別的 Popup 蓋住時切頁也清空。 |
| 規則四：`ClosePopup` 後歷史是空的 | `ClosingAnUnreturnablePopupLeavesAnEmptyHistory`：`ClosePopup` 路由與 `View.Close()`；關閉之後的切頁恢復正常。 |
| 規則五：重用的實例 | `UnreturnableStateBelongsToOneOpeningEvenForReusedInstances`。 |
| 其他導航 API 遇到忙碌仍然回傳 `Busy` | `SignalsSentDuringANavigationRunAfterItInOrder`（`ShowPageAsync`、`OpenPopupAsync`、`TriggerAsync`、`BackAsync`）與轉場的那個案例。 |

**EditMode 與走訪的案例**

- `AuthoringAcceptsTheAnySourceForSignalRoutes`：三種種類與 `history`、`Back` 與 `ClosePopup` 被拒絕、`OpenPopup` 加 `Replace` 被拒絕（訊號與一般路由）、和畫面同名的動作並存、改名不動畫面上同名的按鈕、移除、節點的位置。
- `ValidateReportsSignalRoutesWithTheWrongKindOrHistory`：兩個新代碼；訊號路由的 `route.target-missing` 與 `route.invalid` 的 `ViewId` 是 null；只有訊號有那個動作時，畫面上的按鈕照舊回報 `button.action-unrouted`。
- `PlanWritesSignalRoutesWithTheStarSource`：`"source": "*"`、`Entries` 沒有多出畫面、ID 常數、部分更新（只改 `history`、省略 `history` 時沿用、種類換了回到預設值、沒有 `replace` 時不動）、八種寫錯的計畫在套用前報錯而且什麼都沒有建立。
- `BlueprintShowsTheAnySourceNodeAndEditsSignalRoutes`：節點存在並顯示 `Emit` 的訊號、選取它時右欄沒有畫面才有的選項、行為只有三種、從右欄新增訊號路由、`OpenPopup` 的「返回歷史」只有「可以返回」與「不可返回」（訊號與一般路由）、`Emit` 沒有這個選單。
- `SignalRouteTargetsCountAsReachableButNotAsExits`（4-3）。
- 走訪 `WalkCoversSignalRoutesAndChecksClearedHistory`：四條訊號路由（`ShowPage` 加 `Clear`、`OpenPopup` 加 `Clear`、`OpenPopup`、目標是起始頁的 `ShowPage`）與兩條 `Clear` 的一般路由全部通過，報告裡訊號那幾步的 `source` 是 `*`。
- 走訪 `WalkWaitsForNavigationsStartedByPageScripts`：頁面腳本在走訪關掉它的 Popup 時馬上開始一次 0.5 秒的切頁。一步在觸發時遇到 `Busy`，另一步在準備時遇到；兩步都通過，報告裡有腳本記的警告，花的時間超過轉場的長度。
- 既有的走訪案例加上：`Emit` 的訊號預設略過、開啟 `IncludeEmit` 後送到起始頁的 `OnAction`；目標由程式控制而顯示失敗時（一般路由與訊號路由各一條）記為略過，`result` 照實是 `Failed`。

**命令列的實際執行**

和 4-1～4-6 同一個副本專案，重新建立。除了原本的內容，另外用命令列套用 AgentGuide「由遊戲事件觸發的跳轉」的示例計畫（原文抽出，`Match.unity`），把指南的 `HudPage`、`ChallengerPage` 原文掛到對應的頁面，`MatchEvents` 掛在場景裡並指定工作區。形式和之前相同，沒有 `-quit`。

| 入口與參數 | 結束代碼 | 秒數 | 報告摘要 |
|---|---:|---:|---|
| Validate `-unigameui-scene Assets/Scenes/Match.unity -unigameui-strict` | 0 | 5 | `passed: true`，Error 0、Warning 0。 |
| 走訪 `-unigameui-scene Assets/Scenes/Match.unity` | 0 | 9 | `Passed`，通過 6、略過 3。訊號 `disconnected`（`OpenPopup` 加 `Clear`）通過；訊號 `challengerJoined` 的目標 `challenger` 由程式控制、沒有資料顯示不出來，記為略過，`result` 是 `Failed`；以 `challenger` 為來源的路由也略過；`Emit` 的訊號略過。 |
| 同上加 `-unigameui-include-emit` | 0 | 9 | `Passed`，通過 7、略過 2。`Emit` 的訊號觸發。 |

- 原本的十八種情況也重新執行了一次，結束代碼與報告的數量都和上一次相同。二十一次都由工具自己結束 Editor，每次 5 到 18 秒。

**實際打包的執行檔**

同一個副本專案的 `Match.unity`，Windows、Mono、Managed Stripping Level 是 High，以 `-batchmode -nographics` 執行。打包前把 `hud` 頁的進場設成 0.5 秒的淡入，讓轉場跨影格；場景裡加一個探針腳本，全部通過時以結束代碼 0 結束。**結果：結束代碼 0。**

| 確認的內容 | 做法 |
|---|---|
| 從不同的畫面送出同一個訊號都到得了目標 | 用指南的 `MatchEvents.OnChallengerJoined` 從 `lobby` 與 `hud` 各送一次，`challenger` 頁顯示出各自的名字（資料經過指南的 `ChallengerPage`）。 |
| 轉場中送出不會漏掉 | 在切到 `hud` 的淡入期間送出 `disconnected` 與 `roundTimeUp` 兩個訊號。這段期間 `ShowPageAsync` 回傳 `Busy`；轉場結束後兩個訊號依序執行，各自回傳 `Completed`，Popup 開在 `hud` 上。 |
| 不可返回的設定有效 | `ShowPage` 加 `Clear` 之後歷史是空的、`BackAsync()` 是 `Unavailable`。以 `Clear` 開啟的 Popup：歷史是空的、`BackAsync()` 是 `Unavailable`、虛擬手把的取消鍵與鍵盤的 Esc 都關不掉；它自己的路由可以離開，離開之後歷史是空的。 |
| 訊號搭配 `Emit` | `roundTimeUp` 在 `lobby` 沒有反應，在 `hud` 由指南的 `HudPage.OnAction` 切到結算畫面；Popup 開著時由 Popup 收到，`hud` 沒有反應。 |
| 只在某個畫面發生的事 | 指南的 `HudPage.OnPlayerDefeated`：在 `hud` 還在淡入時呼叫、上面蓋著 Popup 時呼叫，兩次都切到了結算畫面。 |

- 執行期間沒有任何 Error、Exception 或 Assert（探針把它們算成失敗）。

**與規格不同、或規格沒寫到而由實作者決定的地方**

請審核時一併判斷。

1. **多了公開的 `UIRoute.IsSignal`。** 規格只定義 `UIRoot.AnySource`。走訪、Validate 與藍圖視窗都要判斷一條路由是不是訊號路由，遊戲程式訂閱 `ActionTriggered` 時也用得到。
2. **`TriggerAsync(UIRoot.AnySource, 動作)` 回傳 `Unavailable` 並記警告**，提示要用 `SignalAsync`。規格說訊號和畫面的動作互不相干，沒有寫這個呼叫的結果。
3. **送出之前的狀況沿用其他 API 的結果：** 工作區已經停用時回傳 `Unavailable`（規格寫的 `Canceled` 是「等待期間」）；token 在送出前已經取消時回傳 `Canceled`。
4. **訊號路由的種類不對而還是出現在執行期時**（Validate 會擋下來）回傳 `NotFound` 並記錯誤，不會照做。
5. **`Emit` 的訊號在還沒有任何畫面時**只觸發 `ActionEmitted`，回傳 `Completed`。
6. **不可返回的 Popup 在最上層時不會觸發 `BackRequestedAtRoot`。** 規格寫「返回鍵不處理」。
7. **`root.ClosePopupAsync()` 也可以關閉不可返回的 Popup。** 規格列的出口是 `ClosePopup` 路由、`View.Close()` 與點遮罩；程式直接呼叫這個 API 一樣是明確的關閉，走訪的準備也靠它。
8. **規則三看的是「有沒有不可返回的 Popup 開著」，不是「它是不是最上層」；規則二只在它是最上層時成立。** 它上面再疊一個一般的 Popup 時，返回可以關掉上面那個；這時切頁仍然清空歷史。
9. **新的問題代碼是 `route.signal-kind-invalid` 與 `route.history-invalid`**，都是 Error。問題代碼從 39 個變成 41 個。
10. **計畫部分更新時，種類換了而沒有寫 `history`，`history` 回到 `Push`。** 否則把 `ShowPage` 加 `Replace` 的路由改成 `OpenPopup` 會因為沿用 `Replace` 而報錯。
11. **`UIRoot` 多了一個隱藏的序列化欄位**，存「任何畫面」節點在藍圖視窗的位置（它不是畫面，沒有 `UIEntry`）。`UIAuthoring.PositionNode` 也接受 `UIRoot.AnySource`。
12. **藍圖右欄的「行為」從 `EnumField` 換成一般的下拉選單**，顯示的文字不變，才能在「任何畫面」節點只列出三種。拖線把既有路由重接到另一種畫面時，`history` 回到 `Push`（Popup 沒有 `Replace`）。
13. **`view.no-exit` 沒有考慮不可返回的 Popup。** 只接了 `Back` 路由的 Popup 以 `Clear` 開啟之後關不掉，目前 Validate 不會提醒，只寫在 AgentGuide。要不要加這項檢查請決定。
14. **`start.none` 不受訊號影響。** 所有頁面都只靠訊號進入、又沒有起始頁時，仍然回報這筆 Warning。
15. **走訪對 `Clear` 的兩項確認也套用到一般路由**（規格寫在訊號路由那一點）。為了讓「歷史是空的」有意義，準備 `Clear` 的路由時會盡量先放一筆歷史。
16. **走訪準備訊號路由時依序試起始頁、其他靠路由進出的頁面、由程式控制的頁面。** `ShowPage` 訊號的目標是工作區唯一的頁面時，這一步略過。
17. **「目標由程式控制而顯示失敗」的判斷是導航結果為 `Failed`。** 結果是 `Completed`、只是 Console 有錯誤的情況照舊算失敗。報告裡這一步的 `result` 照實記 `Failed`，`status` 是 `Skipped`。
18. **遇到 `Busy` 時整個準備重來**，不只重試那一次呼叫：頁面腳本的導航結束後，畫面已經不是準備到一半的樣子。確認不可返回的 Popup 時 `BackAsync()` 剛好遇到 `Busy` 不算失敗。
19. **`OpenPopupForResultAsync` 沒有加 `history` 的多載**，規格沒有要求。
20. **`Runtime` 沒有新增公開型別。** 新的公開成員是 `UIRoot.AnySource`、`UIRoot.SignalAsync`、`UIRoot.OpenPopupAsync` 的多載與 `UIRoute.IsSignal`。
21. **指南的示例多了一份計畫（`Match.unity`）與三個範本**（`MatchEvents`、`ChallengerPage`、`HudPage`），都在副本專案編譯並實際執行過。`HudPage` 遇到上面蓋著 Popup 時的做法是先關掉 Popup，範本的註解說明也可以改成放棄或等待。

**沒有驗證的**

- 藍圖視窗的「任何畫面」節點：實際的顯示與顏色、拖動節點、從它的輸出端拖線、右欄編輯。自動測試只確認節點存在、右欄的欄位與選項、按「儲存路由」之後的結果。
- `OpenPopup` 路由的「返回歷史」選單在互動 Editor 裡的樣子。
- 實體的手把與鍵盤。執行檔用的是虛擬裝置。
- IL2CPP Player。這台電腦沒有 6000.3.19f1 的 IL2CPP 模組。

### 第四批改進（0.5.0）

以下是實作者（Claude 的子 session）自己的紀錄，等主 session 獨立審核。六項各一個 commit，分支 `feat/batch-4` 已快轉合併回 `main`。

| 項目 | commit | 做了什麼 |
|---|---|---|
| 4-1 | `d587709` | `UIIssue` 加上 `Code`、`ViewId`、`ElementId`，`ToString()` 是 `Error [代碼] 訊息`。39 個代碼列在 `UIIssueCode`，嚴重度由代碼決定。Validate 原本的英文訊息改成繁體中文。AgentGuide 有代碼表。 |
| 4-2 | `088edcb` | `UIEntry.NavigatedByCode`、`UIAuthoring.SetNavigatedByCode`、計畫的 `navigation`、藍圖右欄的勾選。設定存在工作區。掛了 `UIConfirmPopup` 的畫面自動算。 |
| 4-3 | `e5f577b` | 五項 Warning：`start.none`、`view.unreachable`、`view.no-exit`、`button.unbound`、`text.missing-glyph`。 |
| 4-4 | `77ace71` | `UIValidation.RunFromCommandLine`、`CheckScenes`、`UIValidationReport`、JSON 報告與結束代碼。藍圖的 Validate 顯示數量並把每一筆記到 Console。 |
| 4-5 | `0bdd783` | `UIWalk`（`Start`、`IsRunning`、`LastReport`、`Finished`、`RunFromCommandLine`）、`UIWalkOptions`、`UIWalkReport`，藍圖工具列的「走訪」。 |
| 4-6 | `d0e395f` | AgentGuide 的「驗收」一節、README、CHANGELOG、版本 0.5.0。 |

**四組測試**

使用 Unity 6000.3.19f1，和前幾輪同一組副本專案（有、無 DOTween），Active Input Handling 是新的 Input System。EditMode 啟用圖形裝置，PlayMode 使用批次模式。測試的程式碼和 commit 的內容逐檔相同。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 131 | 0 | 0 |
| 有 DOTween | PlayMode | 68 | 0 | 1 |
| 無 DOTween | EditMode | 74 | 0 | 57 |
| 無 DOTween | PlayMode | 64 | 0 | 5 |

- 改動前的基準是 70、68 加 1 略過、23 加 47 略過、64 加 5 略過。既有案例全部保留並通過。
- 新增 EditMode 61 個：問題代碼 41 個（每個代碼各一個案例，共 39 個，加上兩個檢查格式與指南表格的案例）、由程式控制的畫面 3 個、新的檢查 5 個、報告與命令列參數 5 個、走訪 7 個。PlayMode 沒有新增案例；`Tests/Runtime` 多了一個走訪測試用的頁面腳本。
- 既有案例只改了篩選問題的方式：兩個輔助函式與一個案例在挑出 DOTween 與 EventSystem 的問題時改用代碼，不再比對訊息文字。
- 無 DOTween 時多略過 10 個：需要套用計畫或實際走訪的案例。
- 走訪的案例是 EditMode 測試，會**實際進出 Play Mode**，包含進入時的 Domain 重新載入；另有一個案例把 Domain Reload 關掉再走一次。
- 跑完後兩個副本專案的 `Assets` 沒有留下測試用的資料夾，`ProjectSettings` 的 Enter Play Mode 設定也還原了。

**各項的測試涵蓋什麼**

- 4-1：每一個代碼各有一個最小的重現案例，確認回報的代碼、嚴重度，以及訊息是中文。另一個案例讀 AgentGuide 的表格，確認代碼與嚴重度和程式裡的完全一致。新增代碼而沒有補案例或沒有寫進指南，測試會失敗。
- 4-2：計畫欄位的新建、沒有 `updateExisting` 時不寫入、部分更新、寫錯值在套用前報錯；設定不在 Popup 的 Prefab 檔案裡；藍圖右欄的勾選存進工作區、可以 Undo、存檔重開後還在；確認框自動視為由程式控制，而且勾選是唯讀。
- 4-3：五項各有「會回報」的案例，表格「不算的情況」每一種也各有案例。缺字的案例涵蓋靜態字型缺字、字型自己的 fallback 補得到、TMP Settings 的全域 fallback 補得到、動態字型的來源檔有這個字、輸入框的 placeholder、下拉選單的選項、超過十個字元；檢查前後字型資產的 JSON、字元表與字符表的數量、有沒有被標成已修改、存檔後的檔案內容都相同，TMP 的預設字型與它的 fallback 也一起比對。另一個案例把 AgentGuide 的示例計畫原文套用到沒有中文字型的專案，Validate 的結果只有確認框「確定」與「取消」兩筆 `text.missing-glyph`。
- 4-4：`CheckScenes` 依序檢查有 Error、只有 Warning、沒有問題、沒有工作區的四個 Scene，結束後回到原本開著的 Scene；嚴格模式；省略時用 Build Settings；Scene 不存在；開著的 Scene 有未儲存的修改時丟出例外而且不存檔。報告的 JSON 欄位名稱、順序與內容。結束代碼的三種結果。參數的解析與錯誤。
- 4-5：見下面的表格。另有參數解析、結束代碼、Editor 內部錯誤的判斷三個不進 Play Mode 的案例。

| 走訪的案例 | 確認的內容 |
|---|---|
| 全部正常的工作區 | 起始加六條路由。`ShowPage`、頁面的 `Back`、`OpenPopup`、`ClosePopup`、Popup 的 `Back` 都通過，`Emit` 略過而且頁面腳本沒有收到動作。報告檔案和 `LastReport` 相同，欄位名稱與文件一致。同一個 Scene 再走一次並開啟 `IncludeEmit`：`Emit` 觸發，頁面腳本收到動作。 |
| 會失敗的工作區 | 目標頁的 `OnShowing` 丟例外：`Failed`，報告裡有例外訊息。`OnShown` 記 `Debug.LogError`：導航是 `Completed`，這一步仍然 `Failed`。只記 `Debug.LogWarning`：`Passed`，警告在報告裡。照 `LaunchPage` 的寫法在 `OnShown` 之後接著導航：這一步與下一步都通過。由程式控制、沒有資料就開不起來的來源畫面：`Skipped`。進場動畫 60 秒而單步上限 3 秒：`Failed`、原因是逾時，下一步照常通過。結果是 `Failed`，畫面清單正確。 |
| 只有一個頁面、沒有起始頁 | 起始那一步略過，頁面的 `Back` 略過，Popup 的開與關通過。由程式控制而且不是任何路由來源的 Popup 沒有被開啟，列在畫面清單裡。 |
| 關閉 Domain Reload | 走訪通過，`Finished` 在回到 Edit Mode 之後觸發一次。 |
| Validate 有 Error | 不進 Play Mode，`NotRun`，報告附上 Validate 的結果，`Finished` 觸發。沒有工作區的 Scene 也是 `NotRun`。 |

**命令列的實際執行**

在另一個全新的副本專案執行，內容是：用命令列套用 AgentGuide 的示例計畫（原文抽出），把指南的範例腳本 `WorkshopPage`、`SettingsPage`、`UnsavedChangesGuard`、`ShopPage` 原文掛到對應的頁面，另外用指南的 `LaunchPage` 做一個 `Launch.unity`；再建立四個示範用的 Scene：`Broken`（起始頁指到不存在的畫面）、`Failing`（目標頁的 `OnShowing` 丟例外）、`Slow`（進場動畫 60 秒）、`Plain`（沒有工作區）。Build Settings 啟用 `Workshop` 與 `Plain`。這個專案沒有設定中文字型。

每一次都是下面的形式，沒有 `-quit`，套件版本 0.5.0：

```text
Unity.exe -batchmode -projectPath <專案> -logFile <記錄檔> -executeMethod <入口> <參數>
```

Validate 的入口是 `UniGameUI.Editor.UIValidation.RunFromCommandLine`：

| 參數 | 結束代碼 | 秒數 | 報告摘要 |
|---|---:|---:|---|
| `-unigameui-scene Assets/Scenes/Workshop.unity` | 0 | 7 | `passed: true`，Error 0、Warning 2：確認框 `confirm` 與 `cancel` 的 `text.missing-glyph`，沒有其他問題。 |
| 同上加 `-unigameui-strict` | 1 | 5 | `strict: true`、`passed: false`，同樣兩筆 Warning。 |
| `-unigameui-scene Assets/Scenes/Broken.unity -unigameui-scene Assets/Scenes/Workshop.unity` | 1 | 5 | Error 1（`Broken` 的 `start.unknown`），Warning 2。兩個 Scene 都在報告裡。 |
| `-unigameui-scene Assets/Scenes/Nowhere.unity` | 2 | 5 | `error` 是「找不到 Scene」。 |
| `-unigameui-scene Assets/Scenes/Plain.unity` | 2 | 5 | `Plain` 的 `hasWorkspace` 是 false，`error` 是「指定的 Scene 都沒有 UniGameUI 工作區」。 |
| （沒有參數） | 0 | 6 | 檢查 Build Settings 的 `Workshop` 與 `Plain`，報告寫到預設的 `Library/UniGameUI/validate.json`，`Plain` 註明沒有工作區。 |
| `-unigameui-scene -unigameui-report <路徑>`（少了值） | 2 | 5 | `error` 是「參數後面要接一個值」，報告仍然寫到指定的路徑。 |
| `Workshop`，另外加 `-nographics` | 0 | 5 | 和第一列相同。 |

走訪的入口是 `UniGameUI.Editor.UIWalk.RunFromCommandLine`：

| 參數 | 結束代碼 | 秒數 | 報告摘要 |
|---|---:|---:|---|
| `-unigameui-scene Assets/Scenes/Workshop.unity` | 0 | 9 | `Passed`。通過 5 步（起始加四條路由）、略過 1 步（`Emit`）。確認框列在畫面清單，註明由程式控制、不在走訪範圍。 |
| `-unigameui-scene Assets/Scenes/Failing.unity` | 1 | 9 | `Failed`。通過 3、失敗 2：切到會丟例外的頁面是 `Failed`；以那一頁為來源的 `Back` 在準備時失敗。其餘兩條路由通過。 |
| `-unigameui-scene Assets/Scenes/Nowhere.unity` | 2 | 5 | `NotRun`，原因是找不到 Scene。 |
| `-unigameui-scene Assets/Scenes/Broken.unity` | 2 | 7 | `NotRun`，原因是 Validate 有 1 筆 Error，沒有進 Play Mode。 |
| `Slow.unity` 加 `-unigameui-step-timeout 3` | 1 | 14 | `Failed`。兩條路由都是逾時，各花 3 秒，起始那一步通過。 |
| `Slow.unity` 加 `-unigameui-step-timeout 100 -unigameui-timeout 12` | 2 | 19 | `NotRun`，原因是超過整體的時間上限 12 秒。已經走完的起始那一步留在報告裡。Editor 自己結束，沒有卡住。 |
| `Workshop.unity` 加 `-unigameui-include-emit` | 0 | 9 | `Passed`，6 步全部通過。`Emit` 觸發後 `WorkshopPage` 開啟確認框，下一步的準備把它關掉。 |
| `-unigameui-scene Assets/Scenes/Launch.unity`（沒有指定報告） | 0 | 9 | `Passed`。起始頁顯示後自己切到主選單，兩個頁面都顯示過。報告寫到預設的 `Library/UniGameUI/walk-Launch.json`。 |
| 只有 `-unigameui-report <路徑>`（沒有 Scene） | 2 | 5 | `NotRun`，原因是要用 `-unigameui-scene` 指定 Scene。 |
| `Workshop.unity`，另外加 `-nographics` | 0 | 8 | 和第一列相同。 |

- 秒數是整個 Unity 程序從啟動到結束的時間。十八次執行都由工具自己結束 Editor，沒有任何一次需要從外面終止。
- **第一次實際執行時發現一個測試沒抓到的問題：** 每一次走訪的「起始」那一步都失敗，原因是 Unity Editor 自己在啟動時丟了一個例外（`UnityEditor.Search.SearchInit.IndexationOnStartup` 的 `ArgumentOutOfRangeException`），被算在這一步上。它和專案的 UI 無關，也不會出現在測試執行器的環境裡。修正後，堆疊全部在 `UnityEditor` 命名空間裡的 Console 訊息不算在步驟上，另有測試。上表是修正之後的結果。
- 同一次也發現參數寫錯時報告只寫到預設位置，已改成盡量寫到 `-unigameui-report` 指定的路徑。

**與規格不同、或規格沒寫到而由實作者決定的地方**

請審核時一併判斷。

1. **`UIIssueCode` 是公開的類別。** 規格只說每種問題要有代碼。實作提供每個代碼的常數、`All` 與 `SeverityOf(code)`。`UIIssue` 另有一個 internal 的定位物件，給 Console 點擊定位用。
2. **只有 Validate 的訊息改成中文。** `UIAuthoring` 與 `UIPlan` 丟出的例外訊息還有一部分是英文，不在 4-1 的範圍，沒有動。
3. **`view.unreachable` 把由程式控制的畫面也當成出發點。** 規格寫的是從起始頁出發。實作上，由程式控制的畫面是程式打開的，從它出發的路由通到的畫面也算走得到；否則只能靠它進入的畫面會被誤報，或被迫也標成由程式控制。
4. **動作是空的 `UIButtonAction` 會有兩筆問題：** 既有的 `button.action-unrouted`（Error）與新的 `button.unbound`（Warning）。
5. **缺字的檢查多查一層 TMP 的預設字型。** 規格列的是字型自己的 fallback 與 TMP Settings 的全域 fallback。TMP 實際顯示時最後還會用預設字型，所以一起查，否則會誤報。`DynamicOS` 的字型向作業系統查；動態字型讀不到來源字型檔時只看圖集。
6. **Validate 的報告多一個 `error` 欄位**，平常是空字串，結束代碼是 2 時是原因。參數錯誤或 Scene 不存在時也會寫出報告。
7. **命令列與 `CheckScenes` 把每個 Scene 單獨開啟來檢查。** EventSystem 放在另一個共用 Scene 的專案，單獨檢查會回報 `input.event-system-missing`，走訪也一樣只載入一個 Scene。這是目前的限制，已寫進 AgentGuide。要支援的話需要新的參數，沒有擅自加。
8. **命令列與 `CheckScenes` 不會產生 ID 常數**，只回報 `constants.outdated`。藍圖的 Validate 按鈕照舊會先更新常數。
9. **走訪多了 `UIWalk.IsRunning`（公開）。** 進入 Play Mode 時 Domain 重新載入，`Finished` 的訂閱會消失，從另一段程式等待結果要靠它。
10. **走訪的報告多了 `reason` 與 `notes` 兩個欄位。** `reason` 是 `NotRun` 的原因；`notes` 記「有待套用的覆寫，使用已儲存的 Prefab」這類說明。起始那一步的 `kind` 是 `Start`，三個數量包含它。
11. **走訪的報告型別：** `UIWalkReport`、`UIWalkStep`、`UIWalkView`、`UIWalkLog`、`UIWalkValidation`，列舉 `UIWalkOutcome`、`UIWalkStepStatus`；Validate 另有 `UIValidationSceneReport`。全部在 Editor 組件。
12. **準備頁面時，只有 `Back` 一定要有上一頁。** 規格寫來源是頁面時先顯示另一個頁面（有路由通到來源頁的，沒有就用起始頁）。這兩種都沒有時，`Back` 會再試其他頁面，其餘種類的路由就直接顯示來源頁。來源是 Popup 而這一步是 `ShowPage` 時，底下的頁面會避開目標頁，否則切頁會因為「已經在那一頁」而無法執行。
13. **Unity Editor 自己的錯誤不算在步驟上**，原因見上面命令列的說明。沒有堆疊的訊息、專案的程式或這個套件記的訊息照常計算。
14. **命令列的走訪在啟動後等五次 Editor 更新才開始**，讓 Editor 排在啟動時的工作先做完。
15. **不是命令列時也有整體的時間上限**：（路由數 + 2）×（單步上限 + 5）+ 60 秒，避免頁面腳本卡住時永遠不結束。
16. **走訪中斷時的結果是 `NotRun`**（手動停止 Play Mode、整體逾時），已經走完的步驟保留在報告裡。
17. **`Runtime` 組件的變動只有 `UIEntry` 多一個欄位與唯讀屬性 `NavigatedByCode`**（規格指定的），沒有新增公開型別。走訪與報告的程式都在 Editor 組件，不會進 Player。
18. **測試組件：** `UniGameUI.Runtime.Tests` 多了走訪測試用的頁面腳本，`UniGameUI.Editor.Tests` 因此多引用它。`UIWalk` 有一個只給測試用的 internal 掛點（進 Play Mode 之後先等幾秒），因為 Domain 重新載入之後測試要一點時間才接得上。

**沒有驗證的**

- 藍圖視窗的「走訪」按鈕與結束後的摘要對話框、Validate 的對話框與 Console 的點擊定位、右欄「由程式控制進出」的實際勾選。自動測試只確認它們呼叫的函式與產生的文字。
- 在開著視窗的互動 Editor 裡走訪。自動測試與命令列都是批次模式。
- 關閉 Scene Reload 的 Enter Play Mode 設定。測試只涵蓋關閉 Domain Reload。
- `DynamicOS` 字型的缺字檢查。測試用的是 TMP 附的字型檔。
- 有待套用的 Prefab 覆寫時走訪的行為。程式會在報告的 `notes` 記一筆，沒有測試。
- 這一批沒有改 `Runtime` 的行為，沒有重新打包執行檔。IL2CPP 照舊沒有驗證。

### 第七輪：Claude 對第三批修正的獨立驗證

使用 Unity 6000.3.19f1，沿用第六輪的兩個副本專案，套件內容換成 `ea61890`。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 70 | 0 | 0 |
| 有 DOTween | PlayMode | 68 | 0 | 1 |
| 無 DOTween | EditMode | 23 | 0 | 47 |
| 無 DOTween | PlayMode | 64 | 0 | 5 |

四組的數量與結果都和實作者的紀錄相同。

- **執行檔（Windows 64 位元、Mono、High Managed Stripping）：** 重新從操作指南的示例計畫與範例腳本建置，64 項檢查全部通過，結束碼 0。這一輪 `Runtime` 沒有變動，目的是確認沒有退步。
- **Editor 端的抽查：** 27 項通過。第六輪的 19 項之外，新增 J2 的八項：建立另一個 Scene、失敗後關回 Scene、失敗後仍可對原 Scene 套用、套用到既有的另一個 Scene，以及 Validate 的四種情況。
- **第六輪的重現腳本：** 7 項檢查全部通過，其餘結果逐項核對，見「第七輪審核」。
- **J7 另外重跑一次原始步驟：** 登記時沒有寫任何選項的確認框，剛套用完與重新開啟 Scene 之後都不顯示待套用。

**這一輪沒有驗證的：** 實際畫出來的畫面（包含下拉選單的捲軸外觀）、實體手把與滑鼠、IL2CPP、互動 Editor 裡的操作、關閉 Domain Reload 的 Play Mode。J8 的三點修正只讀過程式碼並依賴實作者的測試。

### 第三批修正（J1～J8）

以下是實作者（Claude 的子 session）自己的紀錄，主 session 的複驗見上面的「第七輪」。分支 `fix/batch-3`，八項各一個 commit，已快轉合併回 `main`。版本維持 0.4.0，公開 API 沒有變動。這一輪只改 Editor 組件、測試與文件，`Runtime` 資料夾沒有任何變動。

| 項目 | commit | 修正方式 |
|---|---|---|
| J1 | `3365f02` | 固定覆寫時一路看到序列化的最底層：顏色的 r、g、b、a，陣列的長度與每一項，各自判斷是不是已經是覆寫。`options` 與 `optionKeys` 也固定。陣列長度用「暫時加長一項再改回」讓覆寫成立，多出來那一項留下的覆寫會清掉。 |
| J2 | `0e4d82d` | EventSystem 的檢查改成依所在的 Scene 判斷（規則的四列表格），輸入模組檢查執行期會用到的那一個，訊息分開「沒有」與「有但沒有啟用」。計畫失敗時，為了套用而開啟或新建的 Scene 都關回去，並把作用中的 Scene 設回原本那個。 |
| J3 | `314d591` | 預檢的「既有的畫面」：這個工作區登記過就用審核實例，否則用畫面指到的既有 Prefab。 |
| J4 | `5cec1d0` | 建立範本時 `SetDirection` 不再對調已經排好的寬高。捲軸的 `sizeDelta` 是 `(40, 0)`。 |
| J5 | `9ea5977` | 預檢比對套用後的選項數量與鍵值數量，鍵值不是 0 個又和選項不一樣多就報錯。`"optionKeys": []` 清除所有鍵值。 |
| J6 | `2c47c16` | 滑桿、下拉選單、清單間距、缺少元件或遺失的元件 Prefab、沒有遮罩的 Popup，都在預檢依既有的實例或元件 Prefab 檢查。套用時的檢查保留。 |
| J7 | `baf9112` | `HasReviewOverrides` 略過 `TMP_Text` 的 `m_fontColor32`；`m_fontColor` 照舊算。 |
| J8 | `b29b305` | 見下面「J8 四點的確認」。 |

使用 Unity 6000.3.19f1，和第三批同一組副本專案（有、無 DOTween），Active Input Handling 是新的 Input System。EditMode 啟用圖形裝置，PlayMode 使用批次模式。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 70 | 0 | 0 |
| 有 DOTween | PlayMode | 68 | 0 | 1 |
| 無 DOTween | EditMode | 23 | 0 | 47 |
| 無 DOTween | PlayMode | 64 | 0 | 5 |

- 修正前的基準是 56、67 加 1 略過、20 加 36 略過、63 加 5 略過。既有案例全部保留並通過，新增 EditMode 14 個、PlayMode 1 個。
- 既有案例只改了一個：`NewWorkspaceUsesTheInputSystemModuleAndValidationRejectsOtherInputSetups` 的訊息斷言配合 J2 的新訊息，第二個 EventSystem 明確放進工作區的 Scene。
- 無 DOTween 時多通過的 3 個是不需要套用計畫的新案例：EventSystem 的表格、下拉選單的捲軸、`SetDefaultFont` 對輸入框的判斷。其餘 11 個新的 EditMode 案例需要套用計畫，沒有 DOTween 時略過。
- 新案例都放在 `Tests/Editor/UIAuthoringBatch3FixTests.cs`、`Tests/Editor/UIAuthoringBatch3J8Tests.cs` 與 `Tests/Runtime/UIBatch3J8Tests.cs`。
- 跑完後兩個副本專案的 `Assets` 沒有留下測試用的資料夾。

**各項的測試怎麼確認修正有效**

- J1：白色圖片寫 `#FFD25A`、底色 `#204060` 的面板寫 `#204080`（只有一個分量不同）等七種欄位，之後把元件 Prefab 的顏色改成半透明的黑色，實例都維持計畫寫的值，沒寫顏色的實例跟著 Prefab。選項數量和 Prefab 相同（連文字與鍵值都相同的也測了）時，清空 Prefab 的選項並換掉鍵值，實例不變。Popup 的 Prefab 本體同樣檢查。
- J2：表格四列各一個案例，另外測了訊息的兩種說法與輸入模組的檢查對象。開著一個有工作區的 Scene，計畫建立另一個 Scene 並套用成功，Console 有「只能留一個」的 Warning；對既有的另一個 Scene 也成功並維持附加開啟。失敗的情況測了三種（套用完的檢查失敗、預檢失敗、新建的 Scene 失敗），載入的 Scene 清單和套用前相同，接著對原本的 Scene 套用成功。只開著一個沒存過的空 Scene 時另外測了一個案例。
- J3：把起始確認框登記到新的 Scene。`kind: Text` 的 `updateExisting` 在套用前報錯，工作區與 Prefab 檔案都沒有變；`defaultSelected` 指到不存在或不能選取的元件也被擋下；指到 `confirm` 可以套用。
- J4：產生出來的 Prefab，捲軸的錨點、軸心、`sizeDelta`，以及算出來的寬度 40、高度等於範本；滑動區域與把手都在捲軸的範圍內。
- J5：只換選項而數量不同時報錯並且沒有留下變更；數量相同時只換選項、同時提供新鍵值、用空陣列清除、沒有鍵值的下拉選單、只給鍵值而數量不對，共六種。
- J6：十四種錯誤各自確認在套用前失敗，Scene 沒有被標成未儲存，Scene 檔案、工作區的畫面、元件與 Prefab 都沒有變。起始元件還沒安裝時另外確認失敗的計畫不會安裝元件集。
- J7：照第六輪的步驟改 `Text.prefab` 與 `Button.prefab` 的文字顏色再登記確認框。測試先確認審核實例上確實出現 `m_fontColor32.rgba` 的覆寫（也就是修正前會回報待套用的情況），再確認 `HasReviewOverrides` 是 false；真的改了審核實例的文字顏色之後是 true。

**J8 四點的確認**

四點都先寫測試，在修正前的程式上執行一次。

| 項目 | 修正前執行的結果 | 處理 |
|---|---|---|
| `defaultFontPath` | 測試失敗，確認是問題。讓第三個 Prefab 換字型時丟出例外，Scene 檔案已經是失敗那份計畫的內容。 | 已修正。換字型改在檢查通過之後、Scene 存檔之前；先記下會被改寫的 Prefab 檔案內容，失敗時寫回，後續步驟失敗時由計畫的還原一併復原。 |
| `placeholder` | 測試失敗，確認是問題。沒有 placeholder 的輸入框類元件寫了 `placeholder` 或 `placeholderKey`，沒有任何錯誤。 | 已修正。套用前報錯，訊息指出畫面與元件。 |
| `SetDefaultFont` | 測試失敗，確認是問題。專案已經改成別的字型的 placeholder 被換成新的預設字型。 | 已修正。只改輸入框自己記的字型欄位，文字與 placeholder 各自判斷。 |
| Play Mode 的 Validate | 測試通過，**沒有重現**。 | 沒有改程式，測試留著當防護。 |

第四點的確認方式：PlayMode 測試在 Play Mode 中呼叫 `UIValidation.Check`，工作區裡登記了三種沒有設定預設選取元件的畫面：從資產載入的 Popup Prefab 本體、在 Play Mode 用 `Instantiate` 複製出來並登記成審核畫面的實例、測試用程式直接建立的頁面。沒有任何一個被回報「已經遺失」。接著把預設選取元件指到一個物件再銷毀它，確實回報遺失，表示這個檢查在 Play Mode 仍然有效。**沒有涵蓋的情況：** 進入 Play Mode 之前就存在 Scene 裡、由 Unity 在進入 Play Mode 時重新載入的審核畫面（測試的畫面是在 Play Mode 裡建立的），以及關閉 Domain Reload 的 Play Mode。

**規則沒寫到、實作時決定的地方**

請複驗時一併判斷。

1. **J2 的表格沒有列的情況：** 工作區的 Scene 沒有 EventSystem，其他載入的 Scene 有兩個以上。實作是 Warning（和第三列同樣的提醒），並且每一個的輸入模組都檢查。
2. **J2 關回 Scene 的細節：** Scene 原本列在 Hierarchy 但沒有載入時，失敗後回到沒有載入的狀態，不從 Hierarchy 移除。目標 Scene 取代了唯一一個沒存過的空 Scene 時，失敗後換回一個新的未命名 Scene（原本有物件就建立帶預設物件的）。
3. **J3 登記過的畫面：** 規則說「不論這個工作區有沒有登記過它」都以 Prefab 的內容為準。實作上登記過的畫面用審核實例，它是那個 Prefab 的實例，另外包含還沒套用回 Prefab 的修改，計畫實際寫入的對象也是它。
4. **J5 也套用在新元件：** 元件 Prefab 自己帶有鍵值，計畫只寫了數量不同的 `options` 時同樣報錯。
5. **J6 滑桿的 `value`：** 超出合併後範圍的 `value` 以前會被 Unity 安靜地夾住，現在是套用前報錯。起始滑桿是 0 到 1，計畫要寫更大的值必須同時寫 `max`。
6. **J6 起始元件還沒安裝時：** 沒有 Prefab 可以看，預檢用起始元件的預設值（滑桿 0 到 1、下拉選單三個選項）。這些值改成常數，和建立起始元件的程式共用。
7. **J6 的例外型別：** 元件 Prefab 遺失或缺少元件時，預檢丟出 `InvalidOperationException`，和原本套用時相同；其餘是 `ArgumentException`。
8. **J4 不會更新已經安裝的專案：** 安裝只補上缺少的，既有的 `Dropdown.prefab` 不會被改。CHANGELOG 寫了手動修正的方式（把 `Template/Scrollbar` 的寬度改成 40）。
9. **J8 的測試掛點：** `UIWidgets.ReplacingFont` 是 internal，只用來在測試裡模擬換字型換到一半失敗。

**這一輪沒有驗證的：** 沒有重新打包執行檔（`Runtime` 沒有變動）；IL2CPP；互動 Editor 裡的操作，包含透過 Unity MCP 在開著的 Editor 對另一個 Scene 套用計畫；下拉選單選項超過展開範圍時捲軸的外觀與拖曳；J8 第四點上面列的兩種情況。

### 第六輪：Claude 對第三批的獨立驗證

使用 Unity 6000.3.19f1，兩個全新的副本專案（有、無 DOTween），Active Input Handling 設為新的 Input System，套件內容取自 `43bbd51`。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 56 | 0 | 0 |
| 有 DOTween | PlayMode | 67 | 0 | 1 |
| 無 DOTween | EditMode | 20 | 0 | 36 |
| 無 DOTween | PlayMode | 63 | 0 | 5 |

四組的數量與結果都和實作者的紀錄相同。跑完後副本專案的 `Assets` 沒有留下測試用的資料夾。

**執行檔（Windows 64 位元、Mono、High Managed Stripping、含 DOTween）**

1. 用 CLI 套用操作指南的示例計畫（原文抽出）。再用 CLI 套用第二份計畫：在既有的 `workshop` 頁加兩顆按鈕，新增一個 `Reuse` 加 `Close` 的 Popup，以及一個 `backKey: Ignore` 的頁面。
2. 指南的五個類別（`WorkshopPage`、`SettingsPage`、`ShopPage`、`GameLocalizer`、`UnsavedChangesGuard`）原文放進獨立的 asmdef，掛到對應的畫面。Validate 沒有任何問題，24 個由計畫建立的元件都是元件 Prefab 的實例。
3. 執行檔以 `-batchmode -nographics` 執行，用 `InputSystem.AddDevice` 建立虛擬的手把、鍵盤與滑鼠。64 項檢查全部通過，結束碼 0。

| 範圍 | 確認的內容 |
|---|---|
| 元件 | 十種元件在頁面顯示前就帶著計畫寫的值，並用 `View.Get<T>()` 取得表中的型別。面板不接收點擊。 |
| 輸入模式 | 鍵盤的確定與取消不改變模式。鍵盤方向鍵進入導覽並選取 `defaultSelected`，那一次輸入沒有同時移動。事件順序是 `InputModeChanged` 再 `ElementSelected`。滑鼠移動回到 `Pointer` 並清除選取，點擊後不留選取。 |
| 選取 | 往前切頁選到第一個可操作的元件。Popup 開著時連續八次方向輸入都留在 Popup 裡。關閉 Popup 與返回後恢復各畫面上次的選取。重用的確認框第二次開啟仍從「取消」開始。 |
| 返回鍵 | 最底層按 Esc 觸發 `BackRequestedAtRoot` 一次。`UnsavedChangesGuard` 接手後開啟確認框；取消鍵關閉確認框並留在原頁，確定後才返回。`backKey: Ignore` 的頁面擋下 Esc 與手把的取消鍵，畫面上的返回按鈕照常。 |
| 輸入框 | 滑鼠點擊進入編輯，編輯中按方向鍵不切換模式；第一次 Esc 只結束編輯，第二次才返回。手把導覽到輸入框不會自動進入編輯；確定鍵開始編輯，編輯中的方向輸入留在輸入框，取消鍵結束編輯，再按一次才返回。 |
| 清單 | 頁面腳本填入三項。項目裡的 ID 不在 View 的查找範圍。點擊項目回報索引一次。30 項時用手把往下 25 次，選取的項目捲進可視範圍。 |
| 多語系 | 指定 `Localizer` 後，沒有顯示的頁面與保留中的確認框都更新。查不到的鍵值各警告一次，重新整理後不重複。換掉翻譯表再呼叫 `RefreshLocalization()`，沒有翻譯的文字退回預設內容。`Localize` 的三種結果與頁面腳本的 `OnLocalizationChanged` 都正確。 |
| Popup | 點在內容範圍內不關閉。點遮罩關閉，等待結果的程式收到「沒有值」。第二次開啟是同一個實例。關閉路由的結果照常送達。 |
| 確認框 | 自訂按鈕文字、沒有標題時隱藏、多語系文字與預設文字並存。沒有登記時回傳 false，警告裡附上登記用的那一行計畫。 |

**Editor 端的抽查（`-executeMethod`，19 項通過）**

- 安裝兩次，`Assets/UniGameUI/Widgets` 裡所有檔案的雜湊完全相同。刪掉 `Toggle.prefab` 後只重建它，元件集跟著更新。
- 沒寫顏色的按鈕跟著元件 Prefab 變色。顏色寫成和 Prefab 完全相同的值，或三個分量都不同的值，修改 Prefab 後維持不變。這兩種剛好是 J1 不會發生的情況。
- 欄位寫錯元件時在套用前失敗，訊息指出畫面、元件與欄位，Scene 檔案沒有變。
- 部分更新只改有寫的欄位。更新時換 `kind`、更新手動加上的元件都會報錯。
- Validate 對第二個 EventSystem、頁面開了 Popup 專用的選項、預設選取元件在畫面外，都回報 Error。
- 把元件登記成比照 `Button` 而 Prefab 沒有 `Button`，會報錯。

**針對 Editor 端疑點的實際執行**

Editor 端的程式碼另外交給一個不知道我的結論的子代理審閱。它提出的疑點我逐一在副本專案上執行，結果如下。

| 項目 | 執行結果 |
|---|---|
| J1 | 白色圖片寫 `#FFD25A`，Prefab 改成半透明黑色後變成 `#00D25A`、透明度 50%。`Text` 的 `#FF0000` 與 `Button` 的 `textColor: #FFFF00` 在 Prefab 文字改成黑色後都變成黑色。沒寫顏色的圖片正確跟著 Prefab。 |
| J2 | 開著一個有工作區的 Scene，套用指到新 Scene 的計畫：失敗並完整還原。套用指到既有 Scene 的計畫：失敗，該 Scene 留在附加開啟，接著指到原 Scene 的計畫也失敗。 |
| J3 | 登記起始確認框並寫 `defaultSelected: confirm`：被擋下。對確認框的 `confirm` 按鈕用 `kind: Text` 的 `updateExisting`：套用成功，文字與對齊都被改寫。 |
| J4 | 產生出來的 `Dropdown.prefab`，捲軸 `sizeDelta` 是 `(0, 40)`，錨點是右側直條。 |
| J5 | 三個有鍵值的選項換成兩個沒寫鍵值的選項：套用成功，鍵值仍是三個。 |
| J6 | `Slider` 的 `min: 5` 被擋下。前一個元件與後面的 Popup 都沒有留下，Scene 檔案相同，Prefab 數量相同，Scene 被標成未儲存。 |
| J7 | 改過元件 Prefab 的文字顏色之後登記的確認框，`HasReviewOverrides` 是 true，四筆都是 `m_fontColor32.rgba`。同一個專案裡先登記的確認框是 false。新建立、內含下拉選單與輸入框的 Popup 是 false。 |

**驗證過程的說明**

- 第一輪 Editor 抽查中斷，原因是我的腳本在開著 Scene 的情況下套用另一個 Scene 的計畫，這就是 J2。
- 第二輪執行檔有一項失敗，原因是我的探測腳本把 TMP 的 `onSelect` 當成「進入編輯」，它其實在被選取時就會觸發。修正腳本後，第三輪 64 項全部通過。
- TMP 的輸入框在「被選取但沒有編輯」時收到取消鍵，會把自己排進下一次更新啟用。我另外量了這個情況：按下手把的取消鍵之後 120 個影格內，輸入框沒有任何一個影格是編輯狀態，畫面正常返回。

**這一輪沒有驗證的：** 實際畫出來的畫面、實體手把與滑鼠、IL2CPP、互動 Editor 的 Inspector 與選單、關閉 Domain Reload 的 Play Mode。

### 第三批改進（0.4.0）

以下是實作者（Claude 的子 session）自己的紀錄，等主 session 獨立複驗。十一項各一個 commit（`240ed57` 到 `db8691f`），分支 `feat/batch-3` 已快轉合併回 `main`。

使用 Unity 6000.3.19f1 的隔離副本，Active Input Handling 設為新的 Input System。EditMode 啟用圖形裝置，PlayMode 使用批次模式。

| 環境 | 測試 | 通過 | 失敗 | 略過 |
|---|---|---:|---:|---:|
| 有 DOTween | EditMode | 56 | 0 | 0 |
| 有 DOTween | PlayMode | 67 | 0 | 1 |
| 無 DOTween | EditMode | 20 | 0 | 36 |
| 無 DOTween | PlayMode | 63 | 0 | 5 |

- 改動前的基準是 25、38 加 1 略過、17 加 8 略過、35 加 4 略過。既有案例全部保留並通過，新增 EditMode 31 個、PlayMode 29 個。
- 有 DOTween 時略過只供無 DOTween 環境執行的瞬間切換案例。無 DOTween 時，EditMode 略過所有需要套用 JSON 計畫的案例（沒有 DOTween 時計畫會被擋下），PlayMode 略過需要真實動畫的案例，包含新增的「轉場中按返回鍵」。
- 既有的 PlayMode 案例只改了共用的建立程式：現在會建立一個帶 `InputSystemUIInputModule` 的 EventSystem，因為工作區在執行期找不到它會記錄錯誤。raycast 的案例改用這個共用的 EventSystem。
- 導覽、返回鍵、點遮罩與確認框的 PlayMode 案例用 Input System 的 `InputTestFixture` 建立鍵盤、滑鼠、手把與觸控螢幕。事件只排進佇列，由下一個影格的輸入更新處理，順序和實際執行時相同（輸入、輸入模組、工作區）。
- 測試用的起始元件安裝在自己的資料夾：EditMode 用 `Assets/UniGameUI-TestWidgets`，會改到元件的案例另外用各自的暫存資料夾；PlayMode 用 `Assets/UniGameUI-PlayModeWidgets`，由 `IPrebuildSetup` 在進入 Play Mode 前安裝。測試不會動到 `Assets/UniGameUI/Widgets`。跑完後確認副本專案的 `Assets` 沒有留下這些資料夾、`Assets/UniGameUI`，以及測試自己匯入的 `Assets/TextMesh Pro`。

**打包驗證（Windows 64 位元、Mono、High Managed Stripping、含 DOTween）**

先用腳本把 AgentGuide 裡所有的 C# 與 JSON 區塊原文抽出來，再照下面的順序在全新的副本專案執行。指南印出來的內容就是實際套用、編譯與執行的內容。

1. 用 CLI（`UIPlan.RunFromCommandLine`）套用指南的示例計畫。專案原本沒有元件集，套用時自動安裝起始元件，建立 `Workshop.unity`（四個 View、五條路由、登記起始確認框）並產生常數。
2. 用 CLI 套用只有兩個頁面的 `Launch.unity` 計畫，給 `LaunchPage` 範本用。
3. 開著 Workshop Scene，用 `UIPlan.Apply` 套用指南的自訂元件計畫（`PrimaryButton` 比照 `Button`、`Minimap` 是 `Custom`）。
4. 指南的七個類別（`WorkshopPage`、`SettingsPage`、`ShopPage`、`GameLocalizer`、`LanguageSwitcher`、`UnsavedChangesGuard`、`LaunchPage`）與兩段只有陳述式的片段，放進獨立的 asmdef 編譯通過。這個 asmdef 引用 `UniGameUI.Runtime`、`UniGameUI.Generated`、`Unity.TextMeshPro`、`UnityEngine.UI`、`Unity.InputSystem`。把頁面腳本掛到對應的 View 之後，Validate 沒有任何問題，打包成功。
5. 執行檔以 `-batchmode -nographics` 執行，結束碼 0。探測腳本依序確認：
   - **十種元件：** `Text`、`Image`、`Panel`、`Button`、`Toggle`、`Slider`、`ProgressBar`、`Dropdown`、`InputField`、`ScrollList` 都在啟用的畫面上，有啟用的 Graphic 與子 Canvas，並用 `View.Get<T>()` 取得表中的型別。兩個自訂元件也取得。改滑桿的值會進到頁面腳本在 `Awake` 訂閱的監聽。清單有三個項目，`GetInItem` 取得文字，點擊觸發 `ItemClicked`。
   - **確認框：** 透過 `WorkshopPage` 的 `ConfirmAsync` 確定一次（執行了刪除）、取消一次（沒有執行），第二次開啟是同一個實例。另外直接呼叫 `ConfirmAsync` 各一次，分別回傳 true 與 false。
   - **虛擬手把**（`InputSystem.AddDevice<Gamepad>()` 加 `QueueStateEvent`）：方向鍵進入導覽模式並選取 `defaultSelected` 指定的按鈕，再按一次移到下一顆，確定鍵開啟商店頁，取消鍵返回並恢復原本的選取，在最底層再按一次觸發 `BackRequestedAtRoot`。之後用虛擬滑鼠移動，回到 `Pointer` 模式並清除選取。
   - **切換語言：** `RefreshLocalization()` 之後現存頁面的文字更新，`OnLocalizationChanged` 更新了頁面腳本自己組出來的文字。沒有翻譯的鍵值各警告一次，訊息帶畫面與元件。
6. 批次模式的圖形裝置是 Null。這裡的「顯示」指物件啟用而且有可以繪製的 Graphic，**沒有驗證實際畫出來的畫面**，外觀要人工確認。
7. **沒有驗證 IL2CPP**，這台電腦沒有 6000.3.19f1 的 IL2CPP 模組。

**與規格不同的地方**

規格說型別、方法與欄位名稱可以調整，以下是實際調整與補上的部分，請審核時一併判斷。

1. **元件集多記兩個 Prefab。** `UIWidgetSet` 多了 `ListItem` 與 `Confirm` 兩個屬性，用來記錄清單項目與確認框的 Prefab，這樣它們和十個元件一樣可以搬位置，重複安裝時也知道缺不缺。另外有 `UIWidgetSet.Find(name)` 與 `UIWidgets.ConfirmPrefabPath`。
2. **清單多了方向的設定。** `UIList` 多了 `Direction` 屬性與 `UIListDirection` 列舉；內容物件上的版面配置是新的 `UIListLayout`（一個可以切換方向的 Layout Group）。計畫的 `direction` 只寫入清單上的欄位，執行期才套用到版面配置與 ScrollRect。專案把內容物件換成 Unity 內建的 Layout Group 時，清單只負責產生項目，不改排列設定。
3. **計畫寫出的欄位一律固定成覆寫。** Unity 只把「和 Prefab 不同」的值記成覆寫。為了符合「有設定的不變」，計畫寫出的欄位即使數值剛好和元件 Prefab 相同，也會固定成覆寫（先暫時改成別的值讓覆寫成立，再改回來）。陣列欄位（`options`、`optionKeys`）沒有這樣處理。
4. **View 的選項跟著 `updateExisting`。** `defaultSelected`、`backKey`、`popupCache`、`overlayClick` 和背景、動畫一樣，只在新建 View 或 View 設了 `updateExisting` 時寫入；這四個欄位的檢查則不論有沒有 `updateExisting` 都會做。
5. **選取的記憶只用在「回到」畫面。** 關閉 Popup 回到底下的畫面、或返回上一頁時，恢復該畫面上次的選取。往前切到一個頁面、或開啟一個 Popup（包含重用的實例）時，從預設選取元件開始。規格寫的是「選取這個畫面上次被選取的元件」；照字面做的話，重用的確認框第二次開啟會停在上次按的「確定」，和「預設選取取消」衝突。
6. **手把的取消鍵在 `Pointer` 模式下會同時切換模式並返回。** 規格說進入導覽的那一次輸入只用來建立選取；實作上，方向與確定確實不會同時移動或按下，但取消鍵仍然照返回鍵的五個步驟處理，玩家不必按兩次才能返回。
7. **複製確認框不能換畫面 ID。** View Prefab 的畫面 ID 存在 Prefab 裡，Inspector 不能改，複製出來的檔案仍然是 `confirm`，用別的 ID 登記會在套用前報錯（訊息已改成說明畫面 ID 不符）。同一個工作區要有第二種樣式的確認框時，做法是用計畫建立新的 Popup、放進 `title`、`message`、`confirm`、`cancel` 四個元件，再把 `UIConfirmPopup` 掛到根節點，已用 EditMode 測試確認可行並寫進 AgentGuide。**這和規格「複製 Prefab，用 popupId 指定」的字面不同，要不要加一個「複製 View Prefab 並換 ID」的工具，請使用者決定。**
8. **查不到鍵值的警告以工作區為單位。** 每個鍵值在同一個 `UIRoot` 只警告一次，切換語言不會重新計算；文字與圖片的鍵值分開計。`root.Localize()` 查不到時也算在內。`IUILocalizer` 自己丟例外時記錄例外，當成查不到。
9. **Validate 多了三項規格沒列的錯誤：** 元件集裡有空白或重複的名稱、元件集的項目沒有 Prefab、Popup 設了點遮罩關閉但沒有遮罩物件。
10. **起始輸入框不會一選到就進入編輯。** 起始的 `InputField` 把 TMP 的 `shouldActivateOnSelect` 關掉，按確定或用滑鼠點擊才開始編輯。否則用手把或鍵盤移到輸入框時，方向輸入會被輸入框吃掉，走不出來。
11. **其他新增的公開成員：** `UIView.Overlay`；`UIConfirmPopup` 的四個元件 ID 常數；`UILocalizedText.Key`、`UILocalizedImage.Key`、`UILocalizedDropdown.Keys`；抽象基底 `UILocalizedElement`。
12. **測試組件可以存取 Runtime 的 internal。** `UniGameUI.Runtime` 新增了對 `UniGameUI.Editor.Tests` 的 `InternalsVisibleTo`。

**實作時發現、值得知道的行為**

- **TMP 的預設字型沒有中文字。** 確認框的預設按鈕文字「確定」、「取消」在沒有設定預設字型的專案會顯示成方框，並有 TMP 的缺字警告。已寫進 AgentGuide 與 README：要顯示中文請先用 `defaultFontPath` 或 `UIWidgets.SetDefaultFont` 設定字型。
- **刪掉被確認框用到的元件 Prefab 會弄壞確認框。** `Confirm.prefab` 巢狀引用 `Text.prefab` 與 `Button.prefab`。把這兩個檔案刪掉再重新安裝，新的 Prefab 是不同的 GUID，確認框裡的巢狀實例會變成遺失，要連 `Confirm.prefab` 一起刪掉重裝。場景裡既有的元件實例也是同樣的情況。這是 Prefab 連結本身的限制，沒有另外處理。
- **換 Scene 會讓先前拿到的元件集物件失效。** Editor 換 Scene 時會卸載沒有被引用的資產，之後要重新呼叫 `UIWidgets.Find()`。套件自己的流程都是在開好 Scene 之後才取元件集。
- **`InputSystemUIInputModule` 的確定與取消只看當前影格。** 它用 `WasPerformedThisDynamicUpdate()` 判斷，所以測試要把事件排進佇列、讓下一個影格的輸入更新處理；在協程中途直接更新輸入系統，模組會漏掉那次按鍵。
- **輸入框自己會用取消鍵結束編輯。** TMP 的輸入框收到取消時會結束編輯，所以編輯中按 Esc 的結果是「結束編輯」，再按一次才是返回。

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

在暫存區建一個最小專案，`Packages/manifest.json` 放 ugui 2.0.0、inputsystem 1.19.0、test-framework 1.6.0，以及 jsonserialize、uielements、ui、imgui、audio、physics、physics2d 模組（後三個是 DOTween 的模組腳本需要的）。

0.4.0 起套件只支援新的 Input System，副本專案第一次啟動前要先把 Active Input Handling 設成新系統，否則 Validate 會回報錯誤，導覽與返回鍵的測試也收不到輸入。做法是先放一個只有幾行的 `ProjectSettings/ProjectSettings.asset`，Unity 第一次開啟時會補齊其餘欄位：

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  runInBackground: 1
  activeInputHandler: 1
```

`runInBackground: 1` 是給打包驗證用的：以 `-batchmode` 執行的 Player 沒有視窗焦點，關閉時 `InputSystemUIInputModule` 不處理輸入。

把套件複製到 `Packages/` 後執行：

1. `-runTests -testPlatform EditMode` 與 `-runTests -testPlatform PlayMode`。EditMode 不要加 `-nographics`。
2. 用 `-importPackage` 匯入本機 Asset Store 快取裡的 DOTween，再跑一次 PlayMode。實務上準備兩個副本專案（有、無 DOTween）比較省時間，兩個可以同時跑。
3. 用 `-executeMethod` 呼叫 `BuildPipeline.BuildPlayer`，再執行產出的執行檔。

0.4.0 的打包驗證分成四次啟動：用 `UIPlan.RunFromCommandLine` 套用計畫（產生常數）、套用第二份計畫、放進遊戲腳本後用 `-executeMethod` 掛上頁面腳本並打包、執行執行檔。遊戲腳本要等常數產生之後才放進專案，否則第一次啟動就會編譯失敗。在執行檔裡模擬手把時，先把 `InputSystem.settings.backgroundBehavior` 設成 `IgnoreFocus`，再用 `InputSystem.AddDevice<Gamepad>()` 與 `InputSystem.QueueStateEvent` 送出按鍵，每次按下與放開之間等幾個影格。

使用者的 Editor 開著專案時，CLI 拿不到專案鎖，所以一律在副本上驗證。

## 未驗證

第四批（0.5.0）以下項目需要在互動 Editor 人工確認：

- **藍圖的「任何畫面」節點（4-7）。** 節點固定出現在藍圖上，左邊框與連接點是橘色；可以拖動，位置存檔後還在；沒有線可以連進去；選取它時右欄只有路由的編輯，「行為」只有 `ShowPage`、`OpenPopup`、`Emit`；從它的輸出端拖線到頁面或 Popup 會建立訊號路由。
- **`OpenPopup` 路由的「返回歷史」選單（4-7）。** 顯示「可以返回」與「不可返回」兩個選項，一般畫面與「任何畫面」節點都一樣；`ShowPage` 路由照舊是三個選項。
- **藍圖工具列的「走訪」。** 按下後進入 Play Mode、走完自己回到 Edit Mode，跳出摘要：通過、失敗、略過各幾步，以及失敗步驟的來源、動作與原因；失敗的步驟同時出現在 Console。
- **藍圖的 Validate。** 對話框顯示 Error 與 Warning 各幾筆；Console 裡每一筆帶代碼，點擊可以定位到相關的畫面物件（例如沒有動作的按鈕、缺字的文字）。
- **藍圖右欄的「由程式控制進出」。** 勾選、取消、Undo；確認框是勾選而且不能改；節點上顯示「程式控制」。
- **有待套用的覆寫時走訪。** 不應該跳出「套用並進入」的詢問，報告的 `notes` 應該有一筆說明。

第三批（0.4.0）以下項目沒有驗證，需要在互動 Editor 或實機人工確認。其中選取、返回鍵、輸入框搭配手把的**邏輯**，Claude 已在第六輪用虛擬裝置於 Windows Player 驗證過；下面要確認的是外觀、手感與實體裝置。

- **起始元件的實際外觀。** 自動測試與打包驗證都沒有看畫面。請在 Scene 與 Game 視窗確認十個元件、清單項目與確認框的樣子、大小與對齊，以及下拉選單展開後的清單。
- **用真的滑鼠與手把操作時的選取表現。** 改用滑鼠時選取外框是否消失、點過的按鈕是否不留選取、手把第一次輸入是否只出現選取而不移動、關閉 Popup 與返回後選取是否回到原位。自動測試用的是模擬裝置。
- **下拉選單與輸入框搭配手把的操作。** 展開、選擇、取消收起；輸入框按確定進入編輯、取消或確定結束編輯。實體鍵盤輸入文字時方向鍵與 WASD 不會切換模式。
- **清單用手把捲動時的手感**，以及項目很多時的表現（不做虛擬化）。
- **Inspector 的新欄位：** `UIView` 的「預設選取元件」、「不回應返回鍵」，Popup 的「關閉後保留並重用」、「點遮罩關閉」；`UIRoot` 的「取消鍵當成返回」；元件集的 Inspector，特別是在那裡換預設字型。自動測試只確認欄位存在而且綁定正確。
- **選單 Tools → UniGameUI → Install Starter Widgets**，以及互動模式第一次安裝時 TMP Essential Resources 的匯入。
- **IL2CPP Player。** 這台電腦沒有 6000.3.19f1 的 IL2CPP 模組。
- **實體手把的按鍵對應。** 框架讀的是輸入模組上的動作，測試用的是預設動作與虛擬的標準手把。
- **關閉 Domain Reload 的 Play Mode。** 沒有實測。
- **多個 Scene 同時開著、各有 EventSystem 的情況。** Claude 已在第六輪實測，結果是 J2。J2 已修正，EditMode 測試涵蓋規則的四種情況與失敗後的還原；在開著的互動 Editor 實際操作（例如透過 Unity MCP）待複驗。
- **下拉選單選項超過展開範圍時的捲軸。** J4 修正後只用 EditMode 檢查了 Prefab 裡的數值與算出來的矩形。請加八個以上的選項，確認捲軸看得到、拖得動。
- **Play Mode 中按 Validate。** J8 的第四點用 PlayMode 測試沒有重現。進入 Play Mode 之前就存在 Scene 裡的審核畫面沒有另外測，請在有畫面沒設定預設選取元件的 Scene 進 Play 後按一次藍圖的 Validate，確認沒有「預設選取元件已經遺失」。

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
