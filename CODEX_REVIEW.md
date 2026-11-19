# UniGameUI 審核結果（給 codex）

- 審核者：Claude
- 第一輪：2026-11-19，commit `020b424`（H1、H2 由 Claude 修正於 `e8062f8`）
- 第二輪：2026-11-19，commit `0cabfec`（codex 處理第一輪待辦）
- 第一批改進：2026-11-20，`v0.2.0`
- 環境：Unity 6000.3.19f1、uGUI 2.0；在獨立的暫存專案驗證，有無 DOTween 都測過

**現況：** 第一批的六項改進已實作，Unity 自動測試與 Windows Player 已通過。本文件保留各項目的原始說明與互動操作的待驗證清單。

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
| M5 | 遊戲端 API 不足 | 已定案，排在第二批 |
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
| P3 | 頁面不會釋放 | 排在第二批（只在文件註明） |
| U1 | `UIView` 不能繼承 | 已定案採用介面做法，排在第二批 |
| U2 | ID 全是字串 | 已自動產生常數，已驗證存檔、重產、名稱衝突與 Player 編譯 |

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
