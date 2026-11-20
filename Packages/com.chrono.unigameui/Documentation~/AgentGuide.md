# UniGameUI 操作指南

從這份文件的公開工具操作 Unity。交付前照「驗收」一節執行 Validate 與走訪。交付時指出實際使用的 Scene、藍圖路徑、兩份驗收報告的摘要，以及需要專案自行接上的事件。

## 確認專案

1. 確認 Unity 6.3+、uGUI 2.0、Input System 套件，並找出使用者要編輯的 Scene。套件匯入後沒有預建工作區或 UI。
2. 預設採用 `SingleScene`。頁面、參考解析度、節點位置與路由都存於 Scene 的 UniGameUI 工作區。所有 Popup 都是 Prefab。若使用者要共用頁面資產，建立工作區時可以明確指定 `Prefab` 模式。
3. 已存在的工作區沿用原模式與穩定 ID。記錄場景是否有尚未存檔的修改，並查看 `UIAuthoring.HasReviewOverrides(entry)`。有開啟中的 Editor 時，優先在其 Edit Mode 使用公開 C# API 或 **Apply JSON Plan…**。CLI 在該專案已由另一個 Editor 開啟時無法取得專案鎖，使用隔離副本測試。
4. 使用 `UIDOTweenSetup.IsReady` 或 `UIDOTweenSetup.Status` 確認 DOTween。狀態依序區分已就緒、已匯入但待 Setup、未安裝。請在 Edit Mode 完成安裝或 Setup，再套用 JSON 計畫或驗收。未就緒時，Validate 回報一項工作區錯誤，`UIPlan.Apply` 與 `ApplyAsync` 會直接回報安裝說明。執行期保留瞬間切換和一次性警告。
5. 只支援新的 Input System。**Project Settings → Player → Active Input Handling** 要選 `Input System Package (New)` 或 `Both`，改完要重新啟動 Editor。EventSystem 的輸入模組要是 `InputSystemUIInputModule`；建立工作區時，目標 Scene 沒有 EventSystem 就會自動加上。Active Input Handling 或輸入模組不對時 Validate 會回報錯誤，JSON 計畫也套不進去。EventSystem 的數量依下列規則檢查：
   - 工作區所在的 Scene 有超過一個啟用的 EventSystem：錯誤。
   - 所有載入的 Scene 加起來沒有啟用的 EventSystem：錯誤。訊息會說明是完全沒有，還是有但沒有啟用。
   - 工作區的 Scene 有一個，其他載入的 Scene 也有：只是警告。執行期同時載入這些 Scene 時只能留一個。
   - 工作區的 Scene 沒有，其他載入的 Scene 剛好有一個：通過。這是把 EventSystem 放在共用 Scene 的做法，輸入模組檢查的是那一個。

## 安裝 DOTween

- 互動 Editor 使用 **Tools → UniGameUI → Install DOTween…**。藍圖最上方也會顯示安裝或 Setup 提示。若程式在 Editor 裡執行，`UIDOTweenSetup.FindLocalPackage()` 回傳本機標準版套件路徑，`ImportLocalPackage()` 會在未安裝時提交本機匯入。
- CLI 先從 Asset Store 快取的 `Demigiant` 目錄遞迴尋找標準版 `DOTween HOTween v2.unitypackage`。快取在 Windows 的 `%APPDATA%\Unity\Asset Store-5.x`、macOS 的 `~/Library/Unity/Asset Store-5.x` 或 Linux 的 `~/.local/share/unity3d/Asset Store-5.x`。傳入完整路徑，若路徑含空格請加引號。

```text
Unity -batchmode -nographics -projectPath <專案絕對路徑> -importPackage <套件絕對路徑> -quit
```

匯入後重新啟動 Editor 確認 `UIDOTweenSetup.IsReady`。若顯示待 Setup，開啟 **Tools → Demigiant → DOTween Utility Panel**，執行 **Setup DOTween**。 [DOTween Asset Store 頁面](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) · [DOTween 安裝文件](https://dotween.demigiant.com/getstarted)

## 入口

- 開啟目標 Scene 後，**Tools → UniGameUI → Apply JSON Plan…** 可套用計畫。計畫可以省略 `scenePath` 以指定目前作用中的 Scene，然後依一般 Unity 流程存檔。
- CLI 用 `-executeMethod UniGameUI.Editor.UIPlan.RunFromCommandLine -unigameui-plan <JSON 絕對路徑>`。指定 `-batchmode -quit -projectPath <專案絕對路徑> -logFile <記錄檔絕對路徑>`。CLI 模式需要明確的 `scenePath`，成功後存入指定 Scene。
- 已有 Unity Editor C# 執行能力時，互動模式使用 `await UniGameUI.Editor.UIPlan.ApplyAsync(json)`。選單與此 API 會透過 Unity 原生套件匯入處理首次使用的 TMP 資源。需要在同一呼叫完成的批次或同步工具可以使用 `UIPlan.Apply(json)`。先檢查目前目標和已存在物件。

## JSON 計畫

以下是一份完整的示例，用到全部十種元件、起始確認框與 View 的選項。依當次需求提供頁面、元件和路由。

```json
{
  "scenePath": "Assets/Scenes/Workshop.unity",
  "createScene": true,
  "referenceResolution": { "x": 1920, "y": 1080 },
  "startPage": "workshop",
  "views": [
    {
      "id": "workshop",
      "title": "Workshop",
      "background": "#13253A",
      "defaultSelected": "openSettings",
      "elements": [
        {
          "id": "title", "text": "Workshop", "textKey": "workshop.title", "fontSize": 56,
          "rect": { "position": { "x": 0, "y": 320 }, "size": { "x": 800, "y": 90 } }
        },
        {
          "id": "openSettings", "kind": "Button", "text": "Settings", "action": "openSettings",
          "rect": { "position": { "x": 0, "y": 120 }, "size": { "x": 320, "y": 76 } }
        },
        {
          "id": "openShop", "kind": "Button", "text": "Shop", "action": "openShop",
          "rect": { "position": { "x": 0, "y": 20 }, "size": { "x": 320, "y": 76 } }
        },
        {
          "id": "deleteSave", "kind": "Button", "text": "Delete save", "action": "deleteSave",
          "color": "#A33A3A", "rect": { "position": { "x": 0, "y": -80 }, "size": { "x": 320, "y": 76 } }
        }
      ]
    },
    {
      "id": "settings",
      "title": "Settings",
      "background": "#1B2B22",
      "elements": [
        { "id": "frame", "kind": "Panel", "rect": { "size": { "x": 900, "y": 720 } } },
        {
          "id": "icon", "kind": "Image", "color": "#FFD25A",
          "rect": { "position": { "x": -360, "y": 280 }, "size": { "x": 64, "y": 64 } }
        },
        {
          "id": "volume", "kind": "Slider", "min": 0, "max": 1, "value": 0.8,
          "rect": { "position": { "x": 0, "y": 200 } }
        },
        {
          "id": "mute", "kind": "Toggle", "text": "Mute", "textKey": "settings.mute", "state": "Off",
          "rect": { "position": { "x": 0, "y": 120 } }
        },
        {
          "id": "quality", "kind": "Dropdown", "options": ["Low", "Medium", "High"],
          "optionKeys": ["quality.low", "quality.medium", "quality.high"], "value": 1,
          "rect": { "position": { "x": 0, "y": 30 } }
        },
        {
          "id": "playerName", "kind": "InputField", "placeholder": "Name", "placeholderKey": "settings.name",
          "rect": { "position": { "x": 0, "y": -60 } }
        },
        { "id": "storage", "kind": "ProgressBar", "value": 0.4, "rect": { "position": { "x": 0, "y": -140 } } },
        {
          "id": "back", "kind": "Button", "text": "Back", "action": "back",
          "rect": { "position": { "x": 0, "y": -260 } }
        }
      ]
    },
    {
      "id": "shop",
      "title": "Shop",
      "background": "#2B1B2B",
      "elements": [
        { "id": "gold", "text": "0 G", "alignment": "Right", "rect": { "position": { "x": 300, "y": 400 } } },
        {
          "id": "goods", "kind": "ScrollList", "direction": "Vertical", "spacing": 8,
          "rect": { "position": { "x": 0, "y": 40 }, "size": { "x": 520, "y": 560 } }
        },
        {
          "id": "back", "kind": "Button", "text": "Back", "action": "back",
          "rect": { "position": { "x": 0, "y": -360 } }
        }
      ]
    },
    { "id": "confirm", "kind": "Popup", "prefabPath": "Assets/UniGameUI/Widgets/Confirm.prefab" }
  ],
  "routes": [
    { "source": "workshop", "action": "openSettings", "kind": "ShowPage", "target": "settings" },
    { "source": "workshop", "action": "openShop", "kind": "ShowPage", "target": "shop" },
    { "source": "workshop", "action": "deleteSave", "kind": "Emit" },
    { "source": "settings", "action": "back", "kind": "Back" },
    { "source": "shop", "action": "back", "kind": "Back" }
  ]
}
```

- `views[].id`、`elements[].id` 和 `action` 使用 ASCII 字母、數字、底線或連字號。ID 是定位與接線用的鍵。Hierarchy 物件名稱可以調整。
- `mode` 可用 `SingleScene` 或 `Prefab`。只有確定需要新 Scene 時才給 `createScene: true`。新工作區預設參考解析度 1920×1080、每排 3 格。
- `scenePath` 指到的 Scene 還沒開啟時，計畫會自己開啟或建立它：只開著一個沒存過的空 Scene 時取代它，否則以附加方式開啟，套用成功後維持開著。所以開著一個有工作區的 Scene 時，也可以對另一個 Scene 套用計畫。計畫失敗時，為了套用而開啟的 Scene 會關回去，原本開著的 Scene 不動。
- `startPage` 是可選的頁面 ID，指定空字串可清除起始頁。路由種類有 `ShowPage`、`OpenPopup`、`Back`、`ClosePopup`、`Emit`。前兩種需要 `target`。同一來源頁的一個 `action` 對應一條路由。
- `"source": "*"` 是藍圖上「任何畫面」節點的訊號路由，不論目前顯示哪個畫面都成立，由遊戲程式用 `SignalAsync` 送出。種類只能是 `ShowPage`、`OpenPopup`、`Emit`。見「由遊戲事件觸發的跳轉」。
- `ShowPage` 可設定 `"history": "Replace"`。可用值為 `Push`（預設）、`Replace`、`Clear`。`OpenPopup` 也可以設定 `history`，可用值為 `Push`（預設，可以用返回關閉）與 `Clear`（不可返回），規則見「由遊戲事件觸發的跳轉」的「跳過去之後能不能返回」。`ClosePopup` 可設定字串結果，例如 `"result": "yes"`。省略或留空代表這條路由關閉時沒有結果。將任一欄位寫在其他種類的路由上，或 `OpenPopup` 寫 `Replace`，計畫會在套用前指出路由與欄位。
- `pageTransitionMode` 可用 `Sequential` 或 `Concurrent`。新工作區預設依序播放。要讓切頁的進場和退場重疊，設定 `Concurrent` 或在 `UIRoot` Inspector 修改「切頁動畫」。
- `enter`、`exit` 可各自選 `None`、`Fade`、`Slide`、`RotateFade`、`Zoom` 或 `Custom`。例如 `"enter": { "preset": "Zoom", "duration": 0.2, "zoomScale": 0.8, "ease": "OutBack" }`。Zoom 倍率相對於原本的 Motion 尺寸，預設為 0.85。Slide 可設定方向 `Left`、`Right`、`Up`、`Down` 和 `distance`，RotateFade 可設定 `angle`。Ease 有 `Linear`、`InQuad`、`OutQuad`、`InOutQuad`、`OutBack`。
- 既有頁面可直接新增其他 ID 的元件。要修改既有元件時，指定該元件 ID、原 `kind`、`updateExisting: true` 與要寫入的欄位。若要更新既有 View 的背景、動畫或下面的 View 選項，在 View 設 `updateExisting: true`。更新既有路由時，指定原 `source`、`action`、`replace: true` 和要改的欄位。平常省略這些選項，重跑可保留人工編修。
- 更新時只寫入有提供的欄位。`rect` 的 `anchor`、`position`、`size` 與 `enter`／`exit` 的各欄位若省略，維持目前的值；例如只給 `"rect": { "size": { "x": 320, "y": 80 } }` 不會移動元件，只給 `"enter": { "duration": 0.4 }` 不會改動畫種類。
- 新建立的 Prefab 會存入本次生成的內容。既有 Prefab 的調整會出現在 Scene 預覽並標示待套用，由使用者用 **套用到 Prefab** 審核後提交。先從藍圖查看待套用狀態。
- 成功套用計畫後，新增或改寫常數會觸發 Editor 重新編譯。等編譯和組件重載完成，再執行下一個 Editor 步驟或進入 Play Mode。

### View 的選項

以下欄位寫在 `views[]` 的項目上，都可以省略。

| 欄位 | 值 | 說明 |
|---|---|---|
| `defaultSelected` | 元件 ID | 用手把或鍵盤操作時優先選取的元件。必須是這個 View 裡可以被選取的元件（按鈕、開關、滑桿、下拉選單、輸入框）。空字串清除。 |
| `backKey` | `Back`（預設）、`Ignore` | `Ignore` 時這個畫面不回應返回鍵。 |
| `popupCache` | `Recreate`（預設）、`Reuse` | 只能寫在 Popup。`Reuse` 時關閉後保留實例，下次開啟重用。 |
| `overlayClick` | `Ignore`（預設）、`Close` | 只能寫在 Popup。`Close` 時點擊遮罩等同不帶結果的關閉。 |
| `navigation` | `Routes`（預設）、`Code` | `Code` 表示這個畫面由程式控制進出，不靠路由。只影響 Validate 與走訪，見「驗收」的「由程式控制的畫面」。 |

## 元件

畫面上的元件都是**元件集**裡 Prefab 的實例。計畫的 `kind` 是元件集裡的名稱，區分大小寫，省略時是 `Text`。所有元件都接受 `id`、`kind`、`rect`、`updateExisting`；下表是各元件另外能寫的欄位，以及遊戲程式用 `View.Get<T>(id)` 取用的型別。

| 名稱 | 內容 | 計畫欄位 | 程式取用 |
|---|---|---|---|
| `Text` | TMP 文字 | `text`、`textKey`、`fontSize`、`fontPath`、`textColor`（`color` 視為同義）、`alignment` | `TMP_Text` |
| `Image` | 圖片 | `color`、`spritePath`、`spriteKey` | `Image` |
| `Panel` | 九宮格底圖，不接收點擊 | `color`、`spritePath`、`spriteKey` | `Image` |
| `Button` | 按鈕與文字 | `text`、`textKey`、`action`、`color`、`spritePath`、`textColor`、`fontSize`、`fontPath` | `Button` |
| `Toggle` | 開關與文字 | `text`、`textKey`、`state`、`textColor`、`fontSize`、`fontPath` | `Toggle` |
| `Slider` | 滑桿 | `min`、`max`、`value`、`numberMode` | `Slider` |
| `ProgressBar` | 進度條 | `value`（0～1） | `UIProgressBar` |
| `Dropdown` | 下拉選單 | `options`、`optionKeys`、`value`（選項索引） | `TMP_Dropdown` |
| `InputField` | 輸入框 | `text`、`placeholder`、`placeholderKey`、`fontSize`、`fontPath` | `TMP_InputField` |
| `ScrollList` | 捲動區域與 `UIList` | `direction`、`spacing`、`itemPrefabPath` | `UIList` |

- 字串列舉的值：`alignment` 為 `Left`、`Center`、`Right`；`state` 為 `On`、`Off`；`numberMode` 為 `Decimal`、`Whole`；`direction` 為 `Vertical`、`Horizontal`。
- `textKey`、`spriteKey`、`placeholderKey`、`optionKeys` 是多語系鍵值，見「多語系」。`optionKeys` 的數量要和選項一樣多，不需要鍵值的選項寫空字串。用 `updateExisting` 更新既有的下拉選單時，套用後的選項數量和鍵值數量不同會在套用前報錯：換選項時請同時提供 `optionKeys`，或寫 `"optionKeys": []` 清除所有鍵值。本來就沒有鍵值的下拉選單不受影響。
- 矩形座標是 Unity RectTransform 的 anchor 與參考解析度下的座標，Y 向上。新元件省略 `rect.size` 時，大小取自元件 Prefab 的根節點。
- 元件之間沒有父子關係，全部放在 View 的 Content 底下。建立的先後順序就是繪製順序，要當底圖的 `Panel` 請寫在前面。
- **欄位寫錯位置會報錯。** 欄位用在不支援它的元件上（例如 `Slider` 寫 `text`）、`kind` 不在元件集裡、更新時 `kind` 和原本不同，計畫都會在套用前失敗，訊息指出畫面、元件與欄位，而且不留下任何變更。
- **數值和元件目前的內容對不上，也會在套用前報錯。** 更新時依既有的實例檢查，新元件依元件 Prefab 檢查（起始元件還沒安裝時依它的預設值）。
  - `Slider` 的 `min`、`max`、`value` 和目前的值合併之後，`min` 必須小於 `max`，`value` 必須在範圍內。起始滑桿的範圍是 0 到 1，要寫更大的 `value` 請同時寫 `max`。
  - `Dropdown` 的 `value` 不能超出選項的數量，`optionKeys` 的數量要和選項相同。
  - `spacing` 只能用在內容物件有版面配置元件的清單。
  - `placeholder`、`placeholderKey` 只能用在輸入框有 TMP placeholder 的元件。
  - 元件 Prefab 缺少欄位需要的元件（例如比照 `Button` 的自訂元件沒有文字，計畫卻寫了 `text`），或元件集裡的 Prefab 已經遺失。
- 畫面指到的 View Prefab 已經存在時（例如把起始確認框登記到另一個 Scene），上面這些檢查與 `defaultSelected` 都依 Prefab 裡的內容判斷，`updateExisting` 的規則也一樣適用。
- `updateExisting` 只能用在由計畫建立的元件。手動加上 `UIElementId` 的物件沒有記錄元件名稱，請直接在 Editor 修改。
- **計畫沒寫的欄位不會成為 Prefab 覆寫**，之後修改元件 Prefab 時這些實例會跟著變。計畫寫了的欄位會固定在該實例上，即使數值剛好和元件 Prefab 相同。固定的是整個欄位：顏色的四個分量、`options` 與 `optionKeys` 的數量和每一項，之後不論怎麼修改元件 Prefab 都維持計畫寫的值。
- 首次建立元件時，工具會安裝 uGUI 附帶的 TMP Essential Resources。若專案曾匯入部分資源，先透過 **Window → TextMeshPro → Import TMP Essential Resources** 完成安裝。

### 元件集與起始元件

- 一個專案只有一個元件集資產（`UIWidgetSet`），位置不限。專案還沒有元件集時，建立工作區或套用計畫會自動安裝；也可以用 **Tools → UniGameUI → Install Starter Widgets** 或 `UIWidgets.Install()`。Console 會列出建了哪些檔案。
- 起始元件建在 `Assets/UniGameUI/Widgets/`：上表十個元件、清單項目 `ListItem.prefab`、確認框 `Confirm.prefab`，以及元件集 `UIWidgetSet.asset`。
- **改 Prefab 就會改到所有畫面。** 起始元件的外觀只用 Unity 內建的 UI 圖和 TMP 預設字型，各專案直接修改這些 Prefab 的美術。
- 安裝可以重複執行：已經存在的 Prefab 和項目一律不動，只補上缺少的。專案改過的美術不會被蓋掉。計畫用到的內建元件剛好缺少時也會自動補上。
- 元件集超過一個、元件記錄的名稱不在元件集裡、元件 Prefab 缺少該類別需要的元件，Validate 都會回報錯誤。
- 起始的輸入框在被選取時不會自動進入編輯，要按確定或用滑鼠點擊才開始，這樣用手把或鍵盤才走得出輸入框。

### 自訂元件

專案可以把自己的 Prefab 登記成元件，並指定它「比照」哪一個內建類別。比照的類別決定計畫能寫哪些欄位，Prefab 必須有該類別的元件（上表「程式取用」欄）。沒有指定時是 `Custom`，計畫只能設定 `rect`，其餘由遊戲程式用 `View.Get<T>()` 操作。

```json
{
  "widgets": [
    { "name": "PrimaryButton", "prefabPath": "Assets/UI/PrimaryButton.prefab", "like": "Button" },
    { "name": "Minimap", "prefabPath": "Assets/UI/Minimap.prefab" }
  ],
  "views": [
    {
      "id": "workshop",
      "elements": [
        { "id": "confirmBuild", "kind": "PrimaryButton", "text": "Build", "action": "build" },
        { "id": "minimap", "kind": "Minimap", "rect": { "position": { "x": 760, "y": 380 } } }
      ]
    }
  ],
  "routes": [{ "source": "workshop", "action": "build", "kind": "Emit" }]
}
```

- `widgets` 寫在計畫最上層，影響的是整個專案的元件集。名稱已存在而且內容相同時略過，內容不同時報錯、不覆寫。C# 的寫法是 `UIWidgets.Register(name, prefabPath, like)`。
- 內建的十個名稱不能重新登記。要換掉內建元件的樣子，直接修改它的 Prefab，或在元件集的 Inspector 改指到別的 Prefab。

### 預設字型

- 元件集記錄一個預設字型（TMP Font Asset），可以是空的，空的時候使用 TMP 的預設字型。TMP 的預設字型沒有中文字，要顯示中文請先設定含有所需字元的字型；確認框的預設按鈕文字「確定」、「取消」也是中文。
- 計畫最上層寫 `"defaultFontPath": "Assets/Fonts/Main SDF.asset"`，或呼叫 `UIWidgets.SetDefaultFont(path)`，也可以在元件集的 Inspector 指定。
- 設定時會把元件集所有 Prefab 裡「目前使用舊預設字型」的文字換成新字型。已經被專案改成別的字型的文字不會動。輸入框的文字與 placeholder 各自判斷。
- 計畫的 `defaultFontPath` 在其他檢查都通過之後、Scene 存檔之前套用。換字型或存檔失敗時，元件 Prefab、元件集的預設字型與 Scene 檔案都維持套用前的狀態。
- 個別元件的 `fontPath` 照舊可用，會成為該實例的覆寫。多語言的字元涵蓋請用 TMP 的 fallback 字型，框架不處理依語言切換字型。

## ID 常數

遊戲程式一律使用產生的常數。場景存檔、成功套用 JSON 或在藍圖按 **Validate** 時，工具會產生或更新 `Assets/UniGameUI/Generated/<場景名稱>UI.cs`。首次使用未命名的場景時，先存成 Scene 資產。也可選 **Tools → UniGameUI → Generate ID Constants**，或呼叫 `UIIdConstants.Generate(root)`。等 Editor 編譯和組件重載完成再引用新的常數。

```csharp
using UniGameUI.Generated;

var result = await root.TriggerAsync(WorkshopUI.Views.Workshop, WorkshopUI.Actions.OpenSettings);
```

`Views` 含頁面與 Popup ID，`Actions` 含路由（包含訊號路由）與按鈕使用的動作，`Elements` 按畫面分組並加上 `View` 後綴，例如 `WorkshopUI.Elements.WorkshopView.OpenSettings`。畫面和其元件可以使用相同的 ID，例如 `WorkshopUI.Elements.ConfirmView.Confirm`。同一組內的 ID 或工作區 Scene 名稱仍須避免轉成相同的 PascalCase 名稱。清單項目裡的元件 ID 不會產生常數。`UIValidation.Check(root)` 會回報名稱衝突並提示常數過期。

工具會在生成目錄沒有 asmdef 時建立 `UniGameUI.Generated.asmdef`。遊戲程式若有自己的 asmdef，請在它的 references 加入 `UniGameUI.Generated`。若生成目錄已有自訂 asmdef，請引用該組件。

## Editor C# API

- `UIAuthoring.FindWorkspace(scene)`、`CreateWorkspace(scene, mode)`、`ConfigureReview(root, resolution, columns)`。
- `UIAuthoring.CreatePage(root, id, title, prefabPath)`、`CreatePopup(root, id, title, prefabPath)` 回傳 Scene 中的 `UIView`。將自訂 uGUI 階層加到 `view.Content`。保留外框的排列用途；`view.Motion` 是預設動畫作用的位置。
- `UIAuthoring.FindEntry(root, id)`、`UIPlan.FindElement(view, id)`、`BindButton(button, action)`、`Connect(root, source, action, kind, target, history, result)`、`Disconnect(root, source, action)`、`SetStartPage(root, id)`。`history`、`result` 是可選參數。`Connect`、`UpdateRoute`、`Disconnect` 的 `source` 可以是 `UIRoot.AnySource`，表示「任何畫面」的訊號路由；`PositionNode(root, UIRoot.AnySource, position)` 移動藍圖上的那個節點。
- `UIAuthoring.UpdateRoute(root, source, originalAction, action, kind, target, history, result)` 用於更新既有路由。改名時一併更新來源 View 的按鈕綁定，並合併為一次 Undo。若來源是 Prefab 審核實例，將覆寫套用後 Prefab 才會使用新動作名稱。
- `UIAuthoring.SetTransition(view, entering, preset, duration, direction, ease, distance, angle, zoomScale)` 和 `SetPageTransitionMode(root, mode)`。
- `UIAuthoring.ArrangeReview(root)`、`FocusReview(root, id)`、`PreviewPopupOnPage(root, popupId, pageId)`、`HasReviewOverrides(entry)`、`ApplyReviewOverrides(root, id)`。最後一項適合提交本次新建立的 Prefab。既有實例的覆寫先交給使用者審核。
- `UIWidgets.Find()` 回傳元件集（沒有時是 null），`Install()` 補上缺少的起始元件，`Register(name, prefabPath, like)` 登記自訂元件，`SetDefaultFont(fontAssetPath)` 設定預設字型，`ConfirmPrefabPath` 是起始確認框的路徑。
- `UIAuthoring.SetNavigatedByCode(root, id, value)` 標記畫面由程式控制進出，`UIEntry.NavigatedByCode` 讀取這個標記。
- `UIValidation.Check(root)` 回傳目前工作區的問題，每一筆有穩定的代碼。`UIValidation.CheckScenes(scenePaths, strict)` 依序檢查多個 Scene 並回傳報告。`UIWalk.Start(options)` 開始自動走訪。三者的用法見「驗收」。對於自訂子階層，用 Unity Editor 的 Undo 和序列化 API 儲存變更。

## 遊戲程式

### 頁面腳本

繼承 `UIViewBehaviour`，把腳本掛在 `UIView` 的同一個 GameObject 上。已有基底類別的腳本可以實作 `IUIViewHandler`。使用 `View.Root` 取得所屬工作區，工作區準備畫面後才會設定此屬性，Edit Mode 下為 null。需要讀取顯示資料時用 `View.GetData<T>()`，它在資料為 null 或型別不符時會指出畫面和期望的型別。資料可省略時用 `View.TryGetData<T>(out var data)`。

下列範本對應上面示例計畫的 `workshop` 頁：`title` 文字元件、`deleteSave` Emit 路由和登記好的確認框。使用 asmdef 時，讓此腳本所在的組件引用 `UniGameUI.Runtime`、`UniGameUI.Generated`、`Unity.TextMeshPro`。

```csharp
using System;
using TMPro;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class WorkshopPage : UIViewBehaviour
{
    protected override void OnShowing(object data)
    {
        View.Get<TMP_Text>(WorkshopUI.Elements.WorkshopView.Title).text = "Workshop";
    }

    protected override async void OnAction(string action)
    {
        if (action != WorkshopUI.Actions.DeleteSave)
            return;

        try
        {
            if (await Root.ConfirmAsync("Delete the save?", confirmText: "Delete"))
                DeleteSave();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void DeleteSave()
    {
        // 在這裡呼叫專案的存檔服務。
        Debug.Log("收到刪除存檔的確認。", this);
    }
}
```

`View.Get<T>(id)` 先找登記 ID 的物件，再找它底下的子物件，包含未啟用的物件。`View.GetElement(id)` 取得登記物件本身。`Get` 和 `GetElement` 查找失敗時會指出畫面、元件、型別和場景。`View.TryGet<T>(id, out var component)` 查找失敗時安靜回傳 false。手動加入的元件可從 **Add Component → UniGameUI → UI Element Id** 登記。清單項目裡的元件不算在 View 裡，用 `UIList.GetInItem<T>()` 取得。

### 元件的值變更

滑桿、開關、下拉選單、輸入框的值改變時，**直接訂閱 Unity 元件的事件，框架不轉送**。標準寫法是在 `Awake` 訂閱一次。不要在 `OnShowing` 訂閱，否則每顯示一次就多訂閱一次。要在顯示時填入目前的值而不觸發事件，用 `SetValueWithoutNotify`（開關是 `SetIsOnWithoutNotify`，輸入框是 `SetTextWithoutNotify`）。

下列範本對應示例計畫的 `settings` 頁。需要引用 `UnityEngine.UI`。

```csharp
using TMPro;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsPage : UIViewBehaviour
{
    private Slider volume;
    private Toggle mute;
    private TMP_Dropdown quality;
    private TMP_InputField playerName;

    private void Awake()
    {
        volume = View.Get<Slider>(WorkshopUI.Elements.SettingsView.Volume);
        mute = View.Get<Toggle>(WorkshopUI.Elements.SettingsView.Mute);
        quality = View.Get<TMP_Dropdown>(WorkshopUI.Elements.SettingsView.Quality);
        playerName = View.Get<TMP_InputField>(WorkshopUI.Elements.SettingsView.PlayerName);

        // 值變更直接訂閱 Unity 元件的事件，只在 Awake 訂閱一次。
        volume.onValueChanged.AddListener(OnVolumeChanged);
        mute.onValueChanged.AddListener(OnMuteChanged);
        quality.onValueChanged.AddListener(OnQualityChanged);
        playerName.onEndEdit.AddListener(OnNameEdited);
    }

    protected override void OnShowing(object data)
    {
        // 顯示時填入目前的設定。這些方法不會觸發上面的事件。
        volume.SetValueWithoutNotify(AudioListener.volume);
        mute.SetIsOnWithoutNotify(AudioListener.pause);
        quality.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, quality.options.Count - 1));
        playerName.SetTextWithoutNotify(PlayerPrefs.GetString("playerName", ""));
        View.Get<UIProgressBar>(WorkshopUI.Elements.SettingsView.Storage).Value = 0.4f;
    }

    private void OnVolumeChanged(float value) => AudioListener.volume = value;
    private void OnMuteChanged(bool muted) => AudioListener.pause = muted;
    private void OnQualityChanged(int index) => QualitySettings.SetQualityLevel(index);
    private void OnNameEdited(string text) => PlayerPrefs.SetString("playerName", text);
}
```

`UIProgressBar.Value` 的範圍是 0～1，超出範圍會被夾住。

### 清單

`ScrollList` 元件上的 `UIList` 依資料產生項目。所有項目都會建立，不做虛擬化，適合商店、存檔欄位這類數量不大的清單。

下列範本對應示例計畫的 `shop` 頁。

```csharp
using System.Collections.Generic;
using TMPro;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class ShopPage : UIViewBehaviour
{
    private readonly List<string> goods = new List<string> { "Wooden sword", "Iron shield", "Potion" };
    private UIList list;
    private int gold = 120;

    private void Awake()
    {
        list = View.Get<UIList>(WorkshopUI.Elements.ShopView.Goods);
        list.ItemClicked += OnItemClicked;
    }

    protected override void OnShowing(object data)
    {
        list.SetItems(goods, (item, name, index) =>
        {
            UIList.GetInItem<TMP_Text>(item, "label").text = name;
        });
        ShowGold();
    }

    // 語言切換後，頁面腳本自己組出來的文字在這裡更新。
    protected override void OnLocalizationChanged()
    {
        ShowGold();
    }

    private void ShowGold()
    {
        var format = Root.Localize("shop.gold", "{0} G");
        View.Get<TMP_Text>(WorkshopUI.Elements.ShopView.Gold).text = string.Format(format, gold);
    }

    private void OnItemClicked(int index)
    {
        Debug.Log($"選了「{goods[index]}」。", this);
    }
}
```

- `SetItems(data, bind)` 與 `SetCount(count, bind)` 可以重複呼叫。項目依索引重用，不夠就建立，多的停用並保留到下次使用。每次呼叫都會對每個啟用的項目執行 `bind`。`Clear()` 把數量設成 0。
- 項目根節點有 `Button` 時，點擊會觸發 `ItemClicked` 並帶索引。這個事件由清單在建立項目時接好一次，不必在 `bind` 裡訂閱，也不要對項目根節點的按鈕呼叫 `RemoveAllListeners`。
- 項目裡的其他按鈕在 `bind` 裡處理。項目會重用，訂閱前先 `RemoveAllListeners`，否則同一顆按鈕會累積多個監聽。
- `UIList.GetInItem<T>(item, elementId)` 在單一項目內依元件 ID 找元件，找不到時丟出例外。起始的 `ListItem` 是一個帶文字的按鈕，文字的元件 ID 是 `label`。用計畫的 `itemPrefabPath` 或 `list.ItemPrefab` 可以換成專案自己的項目 Prefab；自訂項目裡要讓程式取用的物件，從 **Add Component → UniGameUI → UI Element Id** 登記 ID。
- `Items` 是目前啟用的項目，`Count` 是數量。用手把或鍵盤把選取移到畫面外的項目時，清單會自動捲到看得見它的位置。
- `direction` 與 `spacing` 由內容物件上的 `UIListLayout` 套用；邊距與對齊在它的 Inspector 調整。

### 多語系

框架只負責「什麼時候取值、取不到時顯示什麼」。翻譯內容由專案自己的多語系系統提供：實作 `IUILocalizer`，指定給 `root.Localizer`。

```csharp
using System.Collections.Generic;
using UniGameUI;
using UnityEngine;

public sealed class GameLocalizer : IUILocalizer
{
    private readonly Dictionary<string, string> texts = new Dictionary<string, string>();
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public void Load(IReadOnlyDictionary<string, string> table)
    {
        texts.Clear();
        foreach (var pair in table)
            texts[pair.Key] = pair.Value;
    }

    public void SetSprite(string key, Sprite sprite) => sprites[key] = sprite;

    public bool TryGetText(string key, out string text) => texts.TryGetValue(key, out text);

    public bool TryGetSprite(string key, out Sprite sprite) => sprites.TryGetValue(key, out sprite);
}
```

```csharp
using System.Collections.Generic;
using UniGameUI;
using UnityEngine;

public sealed class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private UIRoot root;
    private readonly GameLocalizer localizer = new GameLocalizer();

    private void Start()
    {
        localizer.Load(new Dictionary<string, string> { ["workshop.title"] = "Workshop", ["shop.gold"] = "{0} G" });
        // 指定來源時，所有現存的畫面會立刻重新取值。
        root.Localizer = localizer;
    }

    public void UseGerman()
    {
        localizer.Load(new Dictionary<string, string> { ["workshop.title"] = "Werkstatt", ["shop.gold"] = "{0} Gold" });
        // 切換語言後呼叫一次。
        root.RefreshLocalization();
    }
}
```

- 計畫的 `textKey`、`placeholderKey`、`spriteKey`、`optionKeys` 會把鍵值記在對應的元件上（`UILocalizedText`、`UILocalizedImage`、`UILocalizedDropdown`）。**編輯時寫在元件上的文字、圖片、選項就是預設內容**，Scene 與審核畫面照常顯示它；計畫可以同時給預設內容與鍵值，例如 `"text": "Start game", "textKey": "menu.start"`。
- 取值的時機有三個：每次畫面顯示前（在 `OnShowing` 之前，頁面腳本之後仍可以改寫文字）；呼叫 `RefreshLocalization()` 或指定新的 `Localizer` 時，對所有現存的畫面實例重新取值，包含沒有顯示的頁面與保留中的 Popup；`UIList` 建立或重用項目時。
- 鍵值查不到時顯示預設內容。有設定 `Localizer` 時，每個查不到的鍵值會警告一次，訊息包含畫面與元件；沒有設定 `Localizer` 時不警告。
- `root.Localize(key, fallback)` 給頁面腳本組合動態文字用。查不到時回傳 `fallback`；`fallback` 是 null 時回傳鍵值本身，方便發現漏翻。
- `RefreshLocalization()` 重新取值之後，會依序呼叫每個現存畫面的 `OnLocalizationChanged()`，再觸發 `root.LocalizationChanged`。頁面腳本自己組出來的文字在 `OnLocalizationChanged()` 更新，如上面的 `ShopPage`。接收者出錯時記錄例外，不影響其他接收者。

### 導航、返回與 Popup

遊戲程式可呼叫 `await root.ShowPageAsync(id, data)`、`await root.OpenPopupAsync(id, data)`、`await root.ClosePopupAsync()` 或 `await root.BackAsync()`。每個 Unity `Awaitable` 只等待一次。轉場中收到的導航請求會立即回傳 `Busy`，不排隊。唯一的例外是 `root.SignalAsync(action, data)`：訊號會等進行中的導航結束再執行，用在不論當時在哪個畫面都要送達的遊戲事件，見「由遊戲事件觸發的跳轉」。

`root.ShowPageAsync(id, data, history)` 的 `Push` 會把離開的頁面加入歷史，`Replace` 會略過離開的頁面，`Clear` 會在切頁成功後清空歷史。切頁或返回成功後會移除歷史尾端與目前頁相同的項目。失敗或取消時保留原歷史。`HistoryCount` 可查還能返回幾頁，`ClearHistory()` 可直接清空。返回會先關最上層的 Popup。返回上一頁會重用頁面實例與上次顯示時的資料。

`CurrentPage`、`TopPopup`、`Popups` 可查目前畫面。`TryGetView(id, out var view)` 取得已存在的實例。單 Scene 頁面隨 Scene 載入，Prefab 頁面在首次顯示時建立，兩種頁面都會留到 Scene 卸載。Popup 在開啟期間才可查到。

`OpenPopupAsync` 等到進場結束。`root.OpenPopupAsync(id, data, UIHistoryMode.Clear)` 開啟不可返回的 Popup，規則見「由遊戲事件觸發的跳轉」。`OpenPopupForResultAsync<T>` 等到退場與隱藏通知結束。Popup 內可呼叫 `View.Close(value)`，或在 ClosePopup 路由設定字串結果。只有最上層且開著的 Popup 可以透過 `View.Close` 關閉。

`UIPopupResult<T>.Status` 反映開啟失敗、正常關閉或取消。`HasValue` 表示收到型別相符的結果，確認後可讀取 `Value`，也可直接呼叫 `GetValueOrDefault(fallback)`。返回、返回鍵、點遮罩、切頁或未指定結果的關閉會回傳 `Completed`，此時 `HasValue` 為 false。型別不符時 Console 會指出實際和期望的型別。取消 token、停用或銷毀工作區，以及 Popup 被外部銷毀都會以 `Canceled` 結束等待。取消 token 或停用工作區時，已開啟的 Popup 保持原狀。

### Popup 的兩個選項

- **重用**（計畫的 `"popupCache": "Reuse"`，或 View Inspector 的「關閉後保留並重用」）：關閉後不銷毀，停用並保留；下次開啟用同一個實例，並移到 Popup 層的最上面。生命週期方法、資料、等待結果的行為都和重新建立時相同，`TryGetView` 與 `Popups` 仍然只包含開著的 Popup。**重用的 Popup 會留著上次的狀態**，例如捲動位置、輸入內容、頁面腳本的欄位，要在 `OnShowing` 重設。預設是每次開啟都重新建立。
- **點遮罩關閉**（計畫的 `"overlayClick": "Close"`，或 View Inspector 的「點遮罩關閉」）：點擊遮罩等同不帶結果的關閉。點在 Popup 的內容範圍內不算。只在該 Popup 是最上層而且沒有轉場進行時有效，和 `backKey` 互相獨立。
  - **內容範圍是 `Content` 的矩形，不是看得見的美術。** 計畫建立的 Popup，`Content` 是畫面中央 60% 的範圍；對話框比它小時，範圍內的暗色區域點了不會關閉。要讓範圍貼齊對話框，請調整 `Motion` 的大小。
  - 既有的 Popup 沒有遮罩物件時，計畫會在套用前報錯。

### 確認框

`ConfirmAsync` 開啟確認框並等待玩家的選擇，只有按下確定時回傳 true。取消、返回鍵、等待被取消都回傳 false。

```csharp
if (await Root.ConfirmAsync("Delete the save?"))
    DeleteSave();

var leave = await Root.ConfirmAsync(
    "Unsaved changes will be lost.", confirmText: "Leave", cancelText: "Stay", title: "Leave?");
```

- **先登記到工作區。** 和其他 Popup 一樣寫在計畫的 `views`，指向起始 Prefab：`{ "id": "confirm", "kind": "Popup", "prefabPath": "Assets/UniGameUI/Widgets/Confirm.prefab" }`。多個 Scene 可以登記同一個 Prefab。
- 確認框沒有顯示出來時（例如沒有登記、另一個導航正在進行）回傳 false，並記錄一筆警告說明原因；沒有登記時會附上上面那一行計畫。
- `confirmText`、`cancelText` 省略時使用 Prefab 上的文字，預設是「確定」與「取消」，並帶多語系鍵值 `ui.confirm` 與 `ui.cancel`。`title` 省略時隱藏標題。
- 起始確認框的設定：預設選取取消鍵、重用實例、回應返回鍵、點遮罩不關閉。外觀直接修改 `Confirm.prefab`，裡面的四個元件 `title`、`message`、`confirm`、`cancel` 是元件 Prefab 的實例。
- 確認框本身是一個掛了 `UIConfirmPopup` 的 Popup，資料可以是 `UIConfirmRequest` 或單純的字串（當成訊息），所以也能用 `OpenPopupForResultAsync<bool>("confirm", request)` 開啟。
- **第二種樣式的確認框：** View Prefab 的畫面 ID 存在 Prefab 裡，複製出來的檔案仍然是 `confirm`，不能用別的 ID 登記。同一個工作區要有另一個樣式時，用計畫建立一個新的 Popup（例如 `quitConfirm`），放進同樣四個 ID 的元件（兩個 `Text`、兩個 `Button`），再把 `UIConfirmPopup` 掛到它的根節點（**Add Component → UniGameUI → UI Confirm Popup**），然後呼叫 `ConfirmAsync(message, popupId: "quitConfirm")`。只想換掉整個專案的確認框外觀時，直接修改起始 Prefab 即可。

### 手把、鍵盤與滑鼠

框架讀輸入一律透過 `InputSystemUIInputModule` 上設定的動作（`move`、`submit`、`cancel`、`point`、`leftClick`、`scrollWheel`），不寫死按鍵。專案換掉模組上的動作資產，框架跟著變。

`root.InputMode` 是最後使用的輸入方式，起始為 `Pointer`，改變時觸發 `root.InputModeChanged`。

| 輸入 | 結果 |
|---|---|
| 滑鼠移動、按鍵、滾輪，或觸控 | 進入 `Pointer` 模式 |
| 手把的任何 UI 動作，或鍵盤的方向輸入 | 進入 `Navigation` 模式 |
| 鍵盤的確定、取消 | 不改變模式 |

- 輸入框正在編輯時不切換模式。
- **最上層畫面**指目前可以操作的那一個：有 Popup 時是最上層的 Popup，否則是目前頁。選取只會落在最上層畫面裡。
- 進入 `Navigation` 模式，或在該模式下最上層畫面改變時，選取這個畫面上次被選取的元件；它已經不能用時，選取預設選取元件（計畫的 `defaultSelected`）；沒有指定時，取階層順序第一個可操作的元件。
- 每個畫面記住自己最後被選取的元件，關閉 Popup 或返回時恢復。往前切到一個頁面、或開啟一個 Popup 時，從預設選取元件開始。
- 從 `Pointer` 進入 `Navigation` 的那一次輸入只用來建立選取，不會同時移動或按下。
- 進入 `Pointer` 模式時清除選取；用滑鼠點過的按鈕不會留在選取狀態。正在編輯的輸入框與展開中的下拉選單除外。
- `root.ElementSelected` 在 `Navigation` 模式下選取的元件改變時觸發，帶所屬的 View 與被選取的物件。游標移動的音效接這裡。
- 執行期找不到啟用中的 EventSystem 或 `InputSystemUIInputModule` 時會記錄一筆錯誤，導覽與返回鍵停用，其餘功能照常。

### 返回鍵

Esc 與手把的取消鍵等同返回。收到取消動作時，依序判斷：

1. `root.HandlesBackKey` 為 false、正在轉場、輸入框正在編輯、或下拉選單展開中：不處理。輸入框與下拉選單會自己用取消鍵結束編輯或收起。
2. 呼叫最上層畫面的 `OnBackRequested()`。回傳 true 表示已經處理，框架不再做事。
3. 最上層畫面設定為不回應返回鍵（計畫的 `"backKey": "Ignore"`），或它是不可返回的 Popup（以 `history: Clear` 開啟）：不處理。
4. 沒有開著的 Popup、也沒有上一頁：觸發 `root.BackRequestedAtRoot`，不呼叫 `BackAsync`。「在主選單按 Esc 問要不要離開遊戲」接這裡。
5. 其餘情況呼叫 `BackAsync()`。

這個流程只管按鍵。畫面上的「返回」按鈕照舊走路由。`OnBackRequested` 不在導航進行中被呼叫，裡面可以直接導航；它丟出例外時會記錄下來，當成沒有處理。用返回鍵關閉 `OpenPopupForResultAsync` 開的 Popup 時，結果是沒有值。

下列範本掛在示例計畫的 `settings` 頁，有尚未儲存的變更時先問過玩家再離開。

```csharp
using System;
using UniGameUI;
using UnityEngine;

public sealed class UnsavedChangesGuard : UIViewBehaviour
{
    public bool HasUnsavedChanges { get; set; }

    protected override bool OnBackRequested()
    {
        if (!HasUnsavedChanges)
            return false;

        AskBeforeLeaving();
        return true;
    }

    private async void AskBeforeLeaving()
    {
        try
        {
            if (!await Root.ConfirmAsync("Unsaved changes will be lost.", confirmText: "Leave", cancelText: "Stay"))
                return;
            HasUnsavedChanges = false;
            await Root.BackAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
```

### 生命週期與導航結果

每一階段依序呼叫 View 根節點上的介面接收者（依元件順序）、View Inspector 的 UnityEvent，再發出 `UIRoot.ViewShowing`、`ViewShown`、`ViewHiding` 或 `ViewHidden`。畫面啟用後、進場動畫開始前會觸發 `OnShowing(data)`。切頁時，依序收到舊頁 Hiding、新頁 Showing、舊頁 Hidden、新頁 Shown。隱藏後與顯示後的通知會等轉場成功。

- 第一次顯示時，會先執行 `Awake` 與 `OnShowing`。有跨影格的進場動畫時，已啟用接收者的 `Start` 在 `OnShowing` 與 `OnShown` 之間執行。瞬間切換時，`OnShown` 也可能早於 `Start`。把首次顯示所需的初始化放在 `Awake`，或在 `OnShowing` 裡用 `View.Get<T>()` 取得元件。 [Unity Start 文件](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/monobehaviour/start)
- 導航失敗或取消後，原畫面會還原並繼續顯示。它如果已經收到 `OnHiding`，之後不會收到 `OnHidden`，也不會再收到一次 `OnShowing`。需要在離開後才改變狀態的收尾工作放在 `OnHidden`。
- 把接收者元件的 `enabled` 設為 false 時，框架仍會呼叫它的生命週期方法。

`Emit` 路由依序呼叫來源畫面的 `OnAction(action)` 和 `UIRoot.ActionEmitted`。後者可以從 Inspector 接線，會提供來源 ID 和動作。`UIRoot.ActionTriggered` 表示路由已被接受，可用來播放按鈕音效。以沒有歷史的 Back 路由為例，會先發出 `ActionTriggered`，接著由 `ActionCompleted` 回報 `Unavailable`。

`UIRoot.ActionCompleted` 帶路由和執行結果，包含執行後的失敗或取消。關閉 Popup 的路由發出 `ActionCompleted` 時，等待該 Popup 結果的程式已先繼續執行，可能已經開始下一次導航。

在 View 的生命週期方法、對應的 UnityEvent、工作區的 View 事件及非 Emit 路由的 `ActionTriggered` 同步呼叫導航時，框架會回傳 `Busy` 並記錄警告。要在 `OnShown` 接續導航，先用 `Awaitable.NextFrameAsync()` 等一個影格。下例掛在 `Launch.unity` 的 `launch` 起始頁，目標頁 ID 是 `mainMenu`。 [Unity NextFrameAsync 文件](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/awaitable/nextframeasync)

```csharp
using System;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class LaunchPage : UIViewBehaviour
{
    protected override async void OnShown()
    {
        try
        {
            var root = Root;
            await Awaitable.NextFrameAsync();
            if (!this || !root || root.CurrentPage != View)
                return;

            var result = await root.ShowPageAsync(LaunchUI.Views.MainMenu, null, UIHistoryMode.Clear);
            if (result == UINavResult.Busy)
                Debug.LogWarning("主選單切換遇到另一個導航，請在它結束後再呼叫。", this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
```

| 出錯的地方 | 處理方式 |
|---|---|
| `OnShowing`、`Showing` | 第一個接收者出錯就停止，還原原畫面並回傳 `Failed`。 |
| 顯示後、隱藏前、隱藏後 | 記錄例外，同階段其餘接收者照常執行，導航回傳 `Completed`。 |
| `OnAction`、`ActionEmitted` | 記錄例外並繼續通知其他接收者，Emit 回傳 `Failed`。 |
| 工作區全域事件 | 個別記錄例外，其他訂閱者和導航結果照常。 |
| `OnBackRequested` | 記錄例外，當成沒有處理，繼續判斷返回鍵。 |
| `OnLocalizationChanged`、`LocalizationChanged` | 記錄例外，其餘接收者照常執行。 |
| 轉場動畫 | 還原畫面並回傳 `Failed`。 |

`async void` 的非同步例外由腳本自行捕捉。框架接不到第一個 `await` 後拋出的例外。

Unity 的逐一通知方法無法取得時，框架會警告一次，並以 `UnityEvent.Invoke()` 呼叫。此時同一個 UnityEvent 會在第一個監聽丟出例外時停止，其餘階段仍依上表處理。

導航結果共有六種。

| 結果 | 意義與 Console 診斷 |
|---|---|
| `Completed` | 操作完成。個別事件的例外仍會按上述規則記錄。 |
| `Busy` | 另一個導航正在進行。從同步通知提出時會記錄警告，動畫播放期間的一般忙碌請求安靜回傳。`SignalAsync` 不會回傳這個結果，它會等進行中的導航結束。 |
| `Canceled` | 操作取消，安靜回傳。 |
| `Unavailable` | 請求合法，目前狀態無法執行。通常會警告。沒有上一頁的返回安靜回傳。 |
| `NotFound` | 畫面 ID、Prefab 或路由設定缺失或不符合請求。Console 會記錄錯誤。 |
| `Failed` | 轉場、事件或導航執行時發生例外。Console 會記錄操作說明與例外堆疊。 |

診斷會帶呼叫的操作、相關 ID 或來源與動作，以及工作區的 Scene 名稱。

自訂 DOTween 轉場請繼承 `UIDOTweenAnimation`，在 `Build(Sequence sequence, UIView view, bool entering)` 加入 tween，掛到該 View 並選 `Custom`。播放、取消和還原 `Motion`／`CanvasGroup` 由框架管理。自訂動畫可用 `AppendCallback` 插入回呼。

### 由遊戲事件觸發的跳轉

遊戲裡發生的事也常常要換畫面：角色死亡、挑戰者連線、斷線。這種跳轉一樣畫在藍圖上，依「這件事會在哪裡發生」分成兩種寫法。

| 這件事 | 畫在藍圖的哪裡 | 觸發方式 |
|---|---|---|
| **只在某個畫面才會發生**：HUD 上的角色死亡、畫面自己的計時器 | 那個畫面自己的路由 | 頁面腳本呼叫 `Root.TriggerAsync(View.Id, 動作)` |
| **任何畫面都可能發生**：挑戰者連線、斷線 | 「任何畫面」節點拉出去的**訊號路由** | 遊戲程式呼叫 `root.SignalAsync(動作, 資料)` |

下面是一個對戰遊戲 Scene 的計畫，兩種寫法都用到。`"source": "*"` 就是「任何畫面」節點。

```json
{
  "scenePath": "Assets/Scenes/Match.unity",
  "createScene": true,
  "startPage": "lobby",
  "views": [
    {
      "id": "lobby",
      "title": "Lobby",
      "elements": [
        { "id": "practice", "kind": "Button", "text": "Practice", "action": "practice" }
      ]
    },
    {
      "id": "hud",
      "title": "HUD",
      "elements": [
        { "id": "timer", "text": "99", "rect": { "position": { "x": 0, "y": 440 } } }
      ]
    },
    {
      "id": "result",
      "title": "Result",
      "elements": [
        { "id": "toLobby", "kind": "Button", "text": "Back to lobby", "action": "toLobby" }
      ]
    },
    {
      "id": "challenger",
      "title": "Challenger",
      "navigation": "Code",
      "elements": [
        { "id": "name", "text": "Challenger", "fontSize": 56, "rect": { "position": { "x": 0, "y": 120 } } },
        { "id": "accept", "kind": "Button", "text": "Fight", "action": "accept" }
      ]
    },
    {
      "id": "disconnected",
      "kind": "Popup",
      "title": "Disconnected",
      "elements": [
        { "id": "message", "text": "Connection lost", "rect": { "position": { "x": 0, "y": 80 } } },
        { "id": "ok", "kind": "Button", "text": "OK", "action": "ok", "rect": { "position": { "x": 0, "y": -80 } } }
      ]
    }
  ],
  "routes": [
    { "source": "lobby", "action": "practice", "kind": "ShowPage", "target": "hud" },
    { "source": "hud", "action": "defeated", "kind": "ShowPage", "target": "result", "history": "Replace" },
    { "source": "result", "action": "toLobby", "kind": "ShowPage", "target": "lobby", "history": "Clear" },
    { "source": "challenger", "action": "accept", "kind": "ShowPage", "target": "hud", "history": "Clear" },
    { "source": "disconnected", "action": "ok", "kind": "ShowPage", "target": "lobby" },
    { "source": "*", "action": "challengerJoined", "kind": "ShowPage", "target": "challenger", "history": "Clear" },
    { "source": "*", "action": "disconnected", "kind": "OpenPopup", "target": "disconnected", "history": "Clear" },
    { "source": "*", "action": "roundTimeUp", "kind": "Emit" }
  ]
}
```

**訊號路由**

- 藍圖上有一個固定的節點「任何畫面」。它不是畫面，不能刪除，也沒有線可以連進去，不會出現在 `Entries`。從它拉出去的路由不論目前顯示哪個畫面都成立。C# 用常數 `UIRoot.AnySource`（值是 `"*"`）表示這個來源，`UIRoute.IsSignal` 可以判斷一條路由是不是訊號路由。
- 種類只能是 `ShowPage`、`OpenPopup`、`Emit`。寫成 `Back` 或 `ClosePopup`，計畫會在套用前報錯。
- 訊號的動作名稱在訊號路由之間不可重複。它和各畫面自己的動作互不相干，同名也不會互相觸發：按鈕的 `action` 只會對到自己畫面的路由，`TriggerAsync` 不能觸發訊號路由。動作名稱一樣會產生 ID 常數。
- `root.SignalAsync(action, data, cancellation)` 送出訊號，依路由的種類執行：

| 種類 | 行為 |
|---|---|
| `ShowPage` | 顯示目標頁並傳入 `data`，依路由的 `history` 處理歷史。開著的 Popup 照切頁的規則關閉。已經在目標頁時回傳 `Unavailable`。 |
| `OpenPopup` | 在目前的畫面上開啟目標 Popup 並傳入 `data`。已經有 Popup 開著時疊在上面。目標已經開著時回傳 `Unavailable`。 |
| `Emit` | 呼叫目前最上層畫面的 `OnAction(action)`，並觸發 `ActionEmitted`，來源是 `UIRoot.AnySource`。不帶資料；有給 `data` 時記錄一筆警告。 |

- **訊號不會因為忙碌而被拒絕。** 有導航正在進行時，訊號等它結束再執行；多個訊號依送出的順序執行；在生命週期方法裡送出也一樣，等這次導航結束後執行。所以 `await` 拿到的是實際執行之後的結果，不會是 `Busy`。這不改變其他 API：`ShowPageAsync`、`OpenPopupAsync`、`TriggerAsync` 等照舊回傳 `Busy`。
- 等待期間 token 被取消，或工作區被停用、銷毀：回傳 `Canceled`。找不到這個動作的訊號路由：回傳 `NotFound` 並記錄錯誤。
- `ActionTriggered` 在訊號開始執行時觸發，`ActionCompleted` 在結束時觸發，和一般路由相同。
- 一般路由不傳資料，訊號可以。目標畫面一定要有資料才能顯示時，請在藍圖把它標記為由程式控制（計畫的 `"navigation": "Code"`，如上面的 `challenger`），走訪才知道這一步需要由程式提供資料。

送出訊號的是遊戲程式，不必是頁面腳本。下列範本對應上面的計畫，掛在場景裡的任何物件上，並在 Inspector 指定工作區。

```csharp
using System;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class MatchEvents : MonoBehaviour
{
    [SerializeField] private UIRoot root;

    // 連線層收到事件時呼叫這三個方法。
    public void OnChallengerJoined(string challengerName)
    {
        Send(MatchUI.Actions.ChallengerJoined, new ChallengerInfo { Name = challengerName });
    }

    public void OnDisconnected()
    {
        Send(MatchUI.Actions.Disconnected, null);
    }

    public void OnRoundTimeUp()
    {
        Send(MatchUI.Actions.RoundTimeUp, null);
    }

    private async void Send(string action, object data)
    {
        try
        {
            // 不必先檢查 IsTransitioning，也不必管目前在哪個畫面：
            // 有導航正在進行時，訊號會等它結束再執行，回傳的是實際執行之後的結果。
            var result = await root.SignalAsync(action, data);
            if (result != UINavResult.Completed && result != UINavResult.Canceled)
                Debug.LogWarning($"訊號 {action} 的結果是 {result}。", this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}

// 挑戰者的資料，隨訊號傳給 challenger 頁。
public sealed class ChallengerInfo
{
    public string Name;
}
```

`challenger` 頁用 `View.GetData<T>()` 取得訊號帶來的資料。需要引用 `Unity.TextMeshPro`。

```csharp
using TMPro;
using UniGameUI;
using UniGameUI.Generated;

public sealed class ChallengerPage : UIViewBehaviour
{
    protected override void OnShowing(object data)
    {
        // 這個畫面一定要有資料：沒有資料時 GetData 會丟出例外，畫面不會顯示。
        var challenger = View.GetData<ChallengerInfo>();
        View.Get<TMP_Text>(MatchUI.Elements.ChallengerView.Name).text = challenger.Name;
    }
}
```

**只在某個畫面發生的事**

畫成那個畫面自己的路由，由頁面腳本觸發。`TriggerAsync` 只在來源是目前最上層的畫面、而且沒有導航正在進行時才會執行，所以這種寫法要自己處理兩種情況：

- **`Busy`：** 有導航正在進行。等它結束再試。
- **上面蓋著 Popup：** 來源頁不是最上層，`TriggerAsync` 回傳 `Unavailable`。依遊戲的設計決定要先關掉 Popup、等 Popup 關閉，還是放棄。

下列範本掛在 `hud` 頁。角色死亡只會在 HUD 上發生，所以走 `hud` 自己的 `defeated` 路由。

```csharp
using System;
using UniGameUI;
using UniGameUI.Generated;
using UnityEngine;

public sealed class HudPage : UIViewBehaviour
{
    // 遊戲邏輯在角色死亡時呼叫。
    public void OnPlayerDefeated()
    {
        GoToResult();
    }

    // 「任何畫面」的 Emit 訊號只通知目前最上層的畫面，由它決定要怎麼反應。
    // 回合時間到：在 HUD 上要切到結算畫面，其他畫面不必處理。
    protected override void OnAction(string action)
    {
        if (action == MatchUI.Actions.RoundTimeUp)
            GoToResult();
    }

    private async void GoToResult()
    {
        try
        {
            var root = Root;
            while (this && root)
            {
                // 有導航正在進行時 TriggerAsync 會回傳 Busy：等它結束再試。
                if (root.IsTransitioning)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    continue;
                }

                // 那次導航結束之後已經不在這個畫面：這件事不必再處理。
                if (root.CurrentPage != View)
                    return;

                // 上面蓋著 Popup 時 hud 不是最上層，TriggerAsync 會回傳 Unavailable。
                // 這裡的做法是先把 Popup 關掉；也可以改成直接放棄，或等 Popup 關閉。
                if (root.TopPopup)
                {
                    var closed = await root.ClosePopupAsync();
                    if (closed != UINavResult.Completed && closed != UINavResult.Busy)
                        return;
                    continue;
                }

                var result = await root.TriggerAsync(View.Id, MatchUI.Actions.Defeated);
                if (result == UINavResult.Busy)
                    continue;
                if (result != UINavResult.Completed)
                    Debug.LogWarning($"切到結算畫面的結果是 {result}。", this);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // 畫面被銷毀，或 Play Mode 結束。
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
```

**訊號搭配 `Emit`**

`Emit` 的訊號不指定要去哪裡，只通知目前最上層的畫面，由它在 `OnAction` 自己決定。適合「同一件事在不同畫面有不同反應」的情況：上面的 `roundTimeUp` 在 HUD 上要切到結算畫面（`HudPage.OnAction`），在大廳或其他畫面什麼都不必做。

- 有 Popup 開著時，收到的是最上層的 Popup，不是底下的頁面。
- `OnAction` 不在導航進行中被呼叫，裡面可以直接導航。
- 不想寫在頁面腳本裡時，訂閱 `root.ActionEmitted`，來源是 `UIRoot.AnySource`。

**跳過去之後能不能返回**

用路由的 `history` 設定。`ShowPage` 與 `OpenPopup` 路由都接受這個欄位，一般路由與訊號路由都適用。

| 種類 | `history` | 結果 |
|---|---|---|
| `ShowPage` | `Push`（預設） | 原本的頁面進歷史，可以返回。 |
| `ShowPage` | `Replace` | 原本的頁面不進歷史。 |
| `ShowPage` | `Clear` | 清空歷史，不能返回。 |
| `OpenPopup` | `Push`（預設） | Popup 可以用返回關閉，回到底下的畫面。 |
| `OpenPopup` | `Clear` | 不能返回，規則見下。 |

`OpenPopup` 寫 `Replace` 會在套用前報錯。C# 對應 `root.OpenPopupAsync(id, data, UIHistoryMode.Clear)`，傳入 `Replace` 丟出 `ArgumentException`。藍圖視窗右欄的「返回歷史」選單，在 `OpenPopup` 路由顯示「可以返回」與「不可返回」。

`Clear` 用在「這件事發生之後，之前的畫面就不該再回得去」：挑戰者登場之後不能按返回回到練習；斷線的提示不能用返回鍵關掉、繼續操作底下的畫面。

以 `Clear` 開啟的 Popup 稱為**不可返回的 Popup**：

1. 開啟時清空歷史。
2. 它在最上層時，返回一律無效。返回鍵不處理，但 `OnBackRequested` 仍然會先被呼叫；`BackAsync()` 與 `Back` 路由回傳 `Unavailable`。這時也不會觸發 `BackRequestedAtRoot`。
3. 它開著的期間發生的切頁一律清空歷史，不論那條路由的 `history` 寫什麼。底下的頁面不會進歷史。
4. `ClosePopup` 路由、`View.Close()` 與點遮罩關閉（有設定時）照常可以關閉它，這些是設計者明確做出來的出口；程式呼叫 `root.ClosePopupAsync()` 也可以。關閉後回到底下的畫面，歷史是空的。
5. 這個狀態只屬於這一次開啟，關閉後就沒有了。重用實例的 Popup 也一樣：同一個 Popup 下次以 `Push` 開啟時可以返回。

所以不可返回的 Popup 一定要有自己的出口：`ClosePopup` 或 `ShowPage` 路由，或由程式關閉。只接了 `Back` 路由的 Popup 以 `Clear` 開啟之後會關不掉。

## 審核

1. **Tools → UniGameUI → Blueprint** 會開啟藍圖。選節點可定位對應 Scene 頁面。用右欄編輯動作、切換種類、目標、返回歷史、關閉結果和起始頁，也能從輸出端拖線；右欄的「由程式控制進出」標記不靠路由進出的畫面。固定的「任何畫面」節點用來畫訊號路由：選取它在右欄編輯，或從它的輸出端拖線到目標畫面；它的行為只有 `ShowPage`、`OpenPopup`、`Emit` 三種。工具列的 **Validate** 檢查目前的工作區，**走訪** 進入 Play Mode 把每條路由走一遍，兩者的說明在「驗收」。在 Play Mode 用實際按鈕試走新增的互動。
2. **檢視全部 UI** 會把 Scene 切到 2D 並框選所有審核畫面。Popup 可以在工作區右欄疊在指定頁面上預覽，用 **重新排列** 回到網格。
3. 在 View Inspector 可預覽進場或退場，並設定預設選取元件、是否回應返回鍵；Popup 另外有「關閉後保留並重用」與「點遮罩關閉」。`UIRoot` 的 Inspector 可以關掉「取消鍵當成返回」。C# 可用 `UIAnimationPreview.Start(view, entering)` 與 `UIAnimationPreview.Stop()`。Validate 會核對 Scene 預覽和執行期 Prefab 上的按鈕；改名後遇到 Prefab 綁定錯誤，套用對應預覽的覆寫。若舊 View 缺少子 Canvas，按藍圖工具列的 **補上子 Canvas**，工具會將所需元件補到 Prefab 本體，其他預覽覆寫繼續由使用者審核。
4. 在互動 Editor 進 Play 前，若有待套用的覆寫，可以選 **套用並進入**、**直接進入** 或 **取消**。直接進入會使用目前已儲存的 Prefab。若多個 Scene 的審核實例同時修改同一 Prefab，先選擇要手動套用的實例。

Player 建置處理器會從複製的 Scene 中移除審核框與 Prefab 審核實例，並保留單 Scene 模式的頁面物件。打包成 AssetBundle 或 Addressables 的 Scene 會保留審核複本，執行期會停用審核層，並依藍圖使用已儲存的 Prefab。

## 驗收

交付前固定做這三步，不必有人進 Editor 點過一遍，就能確認藍圖沒有接錯、每條路由都走得通：

1. **執行 Validate。** Error 要全部解決。Warning 逐筆確認：是有意的就留著，並在交付說明寫明原因；不是就修掉。
2. **執行走訪。** 結果要是 `Passed`。
3. **在交付說明附上兩份報告的摘要：** Validate 的 Error 與 Warning 數量、留下的每一筆 Warning 與原因；走訪通過、失敗、略過各幾步。

兩個工具都能從命令列執行，用結束代碼表示結果，並寫出 JSON 報告；也都有 C# 入口與藍圖視窗的按鈕。專案已經由另一個 Editor 開啟時，命令列拿不到專案鎖，請改用那個 Editor 裡的 C# 入口，或在專案的副本上執行。

### Validate

```text
Unity -batchmode -projectPath <專案絕對路徑> -logFile <記錄檔絕對路徑>
  -executeMethod UniGameUI.Editor.UIValidation.RunFromCommandLine
  [-unigameui-scene <Scene 路徑>]... [-unigameui-report <JSON 路徑>] [-unigameui-strict]
```

- **不要加 `-quit`。** 工具會自己結束 Editor 並設定結束代碼。
- `-unigameui-scene` 是專案裡的路徑，例如 `Assets/Scenes/Workshop.unity`，可以重複。省略時檢查 Build Settings 裡啟用的所有 Scene。沒有工作區的 Scene 會略過，報告裡它的 `hasWorkspace` 是 false。
- `-unigameui-strict` 是嚴格模式：Warning 也算沒有通過。
- 報告預設寫到 `Library/UniGameUI/validate.json`，摘要同時印到記錄檔。

| 結束代碼 | 意思 |
|---|---|
| 0 | 沒有 Error。嚴格模式下也沒有 Warning。 |
| 1 | 有 Error，或嚴格模式下有 Warning。 |
| 2 | 沒辦法執行：參數錯誤、Scene 不存在，或沒有任何 Scene 有工作區。 |

```text
{
  "tool": "UniGameUI.Validate",
  "version": "0.5.0",
  "strict": false,
  "passed": false,
  "errorCount": 1,
  "warningCount": 0,
  "error": "",
  "scenes": [
    {
      "scenePath": "Assets/Scenes/Workshop.unity",
      "hasWorkspace": true,
      "issues": [
        {
          "severity": "Error", "code": "route.target-missing",
          "viewId": "workshop", "elementId": "", "message": "…"
        }
      ]
    }
  ]
}
```

- `error` 平常是空字串。結束代碼是 2 時，它是沒辦法執行的原因；這時報告仍然會寫出來，參數寫錯也一樣。
- C# 入口是 `UIValidation.CheckScenes(scenePaths, strict)`，回傳 `UIValidationReport`（`Passed`、`ErrorCount`、`WarningCount`、`Scenes`、`ToJson()`），不會結束 Editor。它依序開啟各個 Scene，結束後回到原本開著的 Scene；目前開著的 Scene 有未儲存的修改時丟出例外，不會自動存檔。只檢查目前的工作區時用 `UIValidation.Check(root)`。
- 命令列與 `CheckScenes` 是把每個 Scene **單獨開啟**來檢查的。EventSystem 放在另一個共用 Scene 的專案，單獨檢查會回報 `input.event-system-missing`；這種專案請在同時開著這些 Scene 的 Editor 裡用 `UIValidation.Check(root)`。
- 命令列與 `CheckScenes` 只檢查，不會產生或更新 ID 常數。常數過期時回報 `constants.outdated`。
- 藍圖視窗的 **Validate** 檢查目前的工作區，並先更新 ID 常數。對話框顯示 Error 與 Warning 各幾筆；每一筆問題連同代碼記到 Console，點擊可以定位到相關的畫面物件。

### 由程式控制的畫面

有些畫面不靠路由進出：確認框是 `ConfirmAsync` 開啟、自己關閉的；載入畫面、過場畫面常常是程式直接顯示的。請在藍圖把這種畫面標記為**由程式控制進出**，否則 Validate 會提醒它走不到、沒有出口。

- 設定方式有三種：計畫的 View 欄位 `"navigation": "Code"`（預設是 `Routes`，和其他 View 選項一樣只在新建畫面或設了 `updateExisting` 時寫入）；藍圖視窗右欄的「由程式控制進出」；`UIAuthoring.SetNavigatedByCode(root, id, true)`。
- 標記之後：Validate 不檢查它走不走得到（`view.unreachable`）、有沒有出口（`view.no-exit`）、裡面的按鈕有沒有動作（`button.unbound`）；所有頁面都由程式控制時不回報 `start.none`；走訪不要求它能單靠路由開啟。
- 由程式控制的畫面是程式打開的，所以從它出發的路由通到的畫面也算走得到。
- **一定要有資料才能顯示的畫面請這樣標記**，例如由訊號帶資料過來的挑戰者登場畫面。走訪不傳資料，路由的目標是這種畫面而顯示失敗時，那一步記為略過而不是失敗。
- 根節點掛了 `UIConfirmPopup` 的畫面自動視為由程式控制，不必設定，藍圖上也不能取消。
- 這個設定存在工作區的藍圖裡，不在 View Prefab 裡；同一個 Popup Prefab 在不同 Scene 可以有不同的設定。它只影響 Validate 與走訪，不影響執行期的行為。
- 靠路由進出的畫面不要為了消除 Warning 而標記。真的走不到，就補上路由。

### 走訪

走訪在 Play Mode 把藍圖上的每條路由實際觸發一次，確認走得通。

```text
Unity -batchmode -projectPath <專案絕對路徑> -logFile <記錄檔絕對路徑>
  -executeMethod UniGameUI.Editor.UIWalk.RunFromCommandLine
  -unigameui-scene <Scene 路徑> [-unigameui-report <JSON 路徑>]
  [-unigameui-include-emit] [-unigameui-step-timeout <秒>] [-unigameui-timeout <秒>]
```

- 一次走訪一個 Scene，`-unigameui-scene` 必填。
- **不要加 `-quit`。** 工具在走訪結束、回到 Edit Mode 之後自己結束 Editor。
- `-unigameui-step-timeout` 是每一步的時間上限，預設 10 秒。`-unigameui-timeout` 是整體的時間上限，預設 300 秒；超過就結束，結果是 `NotRun`，批次工作不會卡住。
- 報告預設寫到 `Library/UniGameUI/walk-<Scene 名稱>.json`，摘要同時印到記錄檔。

| 結束代碼 | 意思 |
|---|---|
| 0 | `Passed`。可以有略過的步驟。 |
| 1 | `Failed`：至少有一步失敗。 |
| 2 | `NotRun`：Validate 有 Error、Scene 沒有工作區、整體逾時；或參數錯誤、Scene 不存在。 |

**流程**

1. 先對這個 Scene 執行 Validate。有 Error 就不進 Play Mode，結果是 `NotRun`，報告附上 Validate 的結果。
2. 進入 Play Mode。有待套用到 Prefab 的覆寫時不詢問，直接使用已儲存的 Prefab，報告的 `notes` 會記一筆。
3. 等起始頁顯示完成，記為第一步（`kind` 是 `Start`）。沒有起始頁時這一步略過。
4. 依藍圖的順序，每條路由走一步，「任何畫面」的訊號路由也一樣。一步失敗不影響後面的步驟。
5. 離開 Play Mode，寫出報告。

**每一步**

1. 準備：關掉所有 Popup、清空歷史，讓來源畫面成為最上層。來源是頁面時，先顯示一個有路由通到它的頁面（沒有就用起始頁），再顯示來源頁，`Back` 才有地方回去。來源是 Popup 時，先顯示有 `OpenPopup` 路由指到它的頁面（沒有就用起始頁），再開啟它。訊號路由沒有來源畫面，準備時顯示起始頁；`ShowPage` 的目標剛好是起始頁時，改從別的頁面出發。`history` 是 `Clear` 的路由會盡量先讓歷史裡有一筆，才看得出歷史真的被清空了。
2. 觸發：一般路由是 `root.TriggerAsync(來源, 動作)`，訊號路由是 `root.SignalAsync(動作)`，不傳資料。
3. 判定：依下表。另外，這一步從準備到穩定的期間，Console 不可以出現 Error 或 Exception；只有 Warning 不影響結果。Unity Editor 自己的錯誤（堆疊全部在 `UnityEditor` 命名空間裡，例如 Editor 建立搜尋索引失敗）和專案的 UI 無關，不算。
4. 等待穩定：連續幾個影格沒有導航在進行，才開始下一步。頁面腳本在 `OnShown` 之後接著導航（上面 `LaunchPage` 的寫法）不算失敗。
5. 超過單步的時間上限時，這一步失敗，原因是逾時，進行中的導航會被取消，接著繼續下一步。

走訪期間頁面腳本可能自己導航或送出訊號，例如計時器。準備或觸發遇到 `Busy` 時，走訪等這次導航結束，從準備重新來過；逾時才算失敗。

| 路由種類 | 通過條件 |
|---|---|
| `ShowPage` | 回傳 `Completed`，完成時目前頁是目標頁。 |
| `OpenPopup` | 回傳 `Completed`，完成時最上層的 Popup 是目標。 |
| `ClosePopup` | 回傳 `Completed`，來源 Popup 不再開著。 |
| `Back` | 回傳 `Completed`。來源是頁面時回到準備時的上一頁，來源是 Popup 時 Popup 關閉。工作區只有一個頁面而來源是頁面時，這一步略過。 |
| `Emit` | 預設略過。加上 `-unigameui-include-emit`（C# 是 `IncludeEmit`）時觸發，回傳 `Completed` 即通過。 |

訊號路由依種類用同一張表判定。另外有兩項確認，一般路由與訊號路由都適用：

- `history` 是 `Clear` 的 `ShowPage` 與 `OpenPopup`：完成後歷史要是空的。
- 不可返回的 Popup（`OpenPopup` 加 `Clear`）：開啟之後 `BackAsync()` 要回傳 `Unavailable`。

每一步的結果是 `Passed`、`Failed` 或 `Skipped`，加上原因。需要由程式提供資料的情況記為 `Skipped`，不算失敗：

- 準備來源畫面失敗，而來源是由程式控制的畫面。
- 目標畫面是由程式控制的畫面，而觸發的結果是 `Failed`（顯示不出來）。這一步的 `result` 仍然照實記為 `Failed`。

其他情況記為 `Failed`，因為靠路由到得了的畫面本來就收不到資料，沒有資料就顯示不出來是真的問題。

**範圍與限制**

- **不傳資料。** 走訪顯示畫面時 `data` 都是 null。靠路由進得去的畫面，`OnShowing` 要能在沒有資料時正常顯示。
- **不模擬輸入。** 走訪直接觸發路由，不按按鈕，也不操作滑鼠、鍵盤或手把。按鈕有沒有接到路由由 Validate 檢查。
- **不看外觀。** 不檢查畫面長什麼樣子，不截圖。
- **會執行頁面腳本。** 生命週期方法和實際遊玩時一樣會被呼叫。`Emit` 預設略過，所以 `OnAction` 預設不會被呼叫。
- 由程式控制、而且沒有任何路由以它為來源的畫面不會被開啟；它列在報告的 `views` 裡，註明沒有走訪。
- 走訪用的是目前 Scene 在 Editor 裡的內容，不必先存檔。Domain Reload 開啟或關閉的專案都可以用。
- 和 Validate 的命令列一樣，走訪只載入這一個 Scene。

**報告**

```text
{
  "tool": "UniGameUI.Walk",
  "version": "0.5.0",
  "scenePath": "Assets/Scenes/Workshop.unity",
  "outcome": "Failed",
  "reason": "",
  "passedCount": 4,
  "failedCount": 1,
  "skippedCount": 1,
  "steps": [
    {
      "source": "", "action": "", "kind": "Start", "target": "workshop",
      "status": "Passed", "result": "", "reason": "", "seconds": 0.05, "logs": [], "logsOmitted": 0
    },
    {
      "source": "workshop", "action": "openSettings", "kind": "ShowPage", "target": "settings",
      "status": "Passed", "result": "Completed", "reason": "", "seconds": 0.42, "logs": [], "logsOmitted": 0
    }
  ],
  "views": [
    { "id": "confirm", "kind": "Popup", "visited": false, "reason": "由程式控制，而且沒有以它為來源的路由，不在走訪範圍。" }
  ],
  "validation": { "errorCount": 0, "warningCount": 2, "issues": [] },
  "notes": []
}
```

- `outcome` 是 `Passed`（沒有失敗的步驟）、`Failed` 或 `NotRun`。`NotRun` 時 `reason` 是原因，已經走完的步驟仍然列在 `steps`。
- 三個數量包含起始那一步。
- `result` 是觸發路由得到的導航結果；沒有觸發時是空字串。
- 訊號路由那一步的 `source` 是 `"*"`。
- `logs` 是這一步期間 Console 的 Error、Exception、Assert 與 Warning，每一筆有 `type` 與 `message`。最多列 50 筆，Error、Exception 與 Assert 優先保留；沒有列出來的筆數是 `logsOmitted`。**步驟的結果看的是全部的訊息，不受這個上限影響**：警告再多，只要有一筆 Error，這一步就是失敗。
- `views` 列出藍圖上每一個畫面在走訪期間有沒有顯示過，沒有的話 `reason` 是原因。
- `validation.issues` 的格式和 Validate 報告的 `issues` 相同。

**C# 與藍圖視窗**

- `UIWalk.Start(options)` 對目前作用中的 Scene 走訪，例如 `UIWalk.Start(new UIWalkOptions { IncludeEmit = true, StepTimeoutSeconds = 5f })`。要在 Edit Mode 呼叫；它立刻返回，走訪在之後的 Play Mode 進行。`options` 可以省略，三個選項是 `IncludeEmit`、`StepTimeoutSeconds` 與 `ReportPath`。
- `UIWalk.IsRunning` 在走訪進行中是 true。結束、回到 Edit Mode 之後，從 `UIWalk.LastReport` 讀取結果（`Outcome`、`PassedCount`、`FailedCount`、`SkippedCount`、`Steps`、`Views`、`ToJson()`），它是這個 Editor 工作階段最後一次走訪的結果。
- `UIWalk.Finished` 在寫出報告之後觸發。進入 Play Mode 時如果重新載入 Domain（Unity 的預設），之前訂閱的方法會消失，所以從另一段程式等待結果時，請等 `IsRunning` 變回 false 再讀 `LastReport`。
- 藍圖視窗工具列的 **走訪** 對目前的 Scene 執行。結束後跳出摘要：通過、失敗、略過各幾步，以及失敗步驟的來源、動作與原因。失敗的步驟同時記到 Console。

### 問題代碼

`UIValidation.Check(root)` 回傳的每一筆 `UIIssue` 都有 `Severity`、`Code`、`Message`、`ViewId` 與 `ElementId`。**判斷問題種類請用 `Code`**，訊息文字是給人看的，之後可能調整；代碼定下來之後不會更改，每個代碼的嚴重度也是固定的。`ViewId`、`ElementId` 在問題和畫面或元件無關時是 null。`ToString()` 的格式是 `Error [route.target-missing] 訊息`。所有代碼也列在 `UIIssueCode`。

| 代碼 | 嚴重度 | 意思 | 處理方式 |
|---|---|---|---|
| `workspace.missing` | Error | Scene 沒有 UniGameUI 工作區。 | 套用計畫，或用 `UIAuthoring.CreateWorkspace` 建立。 |
| `dotween.not-ready` | Error | DOTween 沒有安裝，或還沒執行 Setup DOTween。 | 見「安裝 DOTween」。 |
| `view.id-invalid` | Error | 藍圖裡有畫面的 ID 是空的或重複。 | 用計畫或 `UIAuthoring` 建立畫面，不要直接改工作區的資料。 |
| `view.review-missing` | Error | 畫面在 Scene 裡的審核物件被刪掉、缺少必要的引用，或畫面 ID 不符。 | 還原被刪掉的物件；救不回來時重新建立這個畫面。 |
| `view.prefab-missing` | Error | Popup 或 Prefab 模式頁面的 Prefab 遺失、缺少必要的引用，或畫面 ID 不符。 | 還原 Prefab 檔案，或重新登記這個畫面。 |
| `view.overrides-pending` | Warning | 審核畫面有還沒套用到 Prefab 的覆寫，執行期用的是已儲存的 Prefab。 | 由使用者在藍圖按「套用到 Prefab」，或捨棄覆寫。 |
| `view.canvas-missing` | Warning | 畫面缺少子 Canvas 或 GraphicRaycaster（0.2.0 以前建立的畫面）。 | 在藍圖按「補上子 Canvas」。 |
| `view.animation-missing` | Error | 動畫選了 `Custom`，但畫面上沒有 `UIDOTweenAnimation` 的元件。 | 掛上自訂動畫元件，或改用其他動畫。 |
| `view.default-selected-missing` | Error | 預設選取元件已經被刪掉。 | 重新指定或清除 `defaultSelected`。 |
| `view.default-selected-outside` | Error | 預設選取元件不在這個畫面裡。 | 改指到這個畫面裡的元件。 |
| `view.popup-option-on-page` | Error | 頁面開啟了只對 Popup 有意義的選項。 | 在 Inspector 關掉「關閉後保留並重用」與「點遮罩關閉」。 |
| `view.overlay-missing` | Error | Popup 設定了點遮罩關閉，但沒有遮罩物件。 | 關掉這個選項，或補回遮罩。 |
| `view.unreachable` | Warning | 從起始頁沿著 `ShowPage` 與 `OpenPopup` 路由走不到這個頁面或 Popup。起始頁本身、由程式控制的畫面、訊號路由的目標（以及從它們出發走得到的畫面）不算；沒有起始頁時只回報 `start.none`。 | 補上通往它的路由。由程式開啟的畫面標記為由程式控制。 |
| `view.no-exit` | Warning | 頁面沒有任何 `ShowPage` 或 `Back` 路由；或 Popup 沒有 `ClosePopup`、`Back`、`ShowPage` 路由，也沒有設定點遮罩關閉。由程式控制的畫面不算；工作區只有一個頁面時那個頁面不算。訊號路由不算任何畫面的出口。 | 補上離開的路由。由程式切換或關閉的畫面標記為由程式控制。 |
| `start.unknown` | Error | 起始頁不存在，或不是頁面。 | 用計畫的 `startPage` 或藍圖重新指定。 |
| `start.none` | Warning | 工作區沒有設定起始頁。所有頁面都由程式控制時不算。 | 用計畫的 `startPage` 或藍圖指定；頁面全部由程式顯示時，把它們標記為由程式控制。 |
| `route.invalid` | Error | 路由的來源畫面不存在、動作是空的，或同一個來源有重複的動作（訊號路由之間動作重複也算）。 | 用計畫或藍圖重新建立這條路由。 |
| `route.target-missing` | Error | `ShowPage` 或 `OpenPopup` 路由的目的地不存在，或種類不符。 | 修正路由的 `target`，或補上目的地的畫面。 |
| `route.signal-kind-invalid` | Error | 「任何畫面」的訊號路由種類是 `Back` 或 `ClosePopup`。 | 訊號路由只能是 `ShowPage`、`OpenPopup` 或 `Emit`。改掉種類，或把它改成某個畫面自己的路由。 |
| `route.history-invalid` | Error | `OpenPopup` 路由的 `history` 是 `Replace`。 | 改成 `Push`（可以返回）或 `Clear`（不可返回）。 |
| `element.id-duplicate` | Error | 同一個畫面裡有空的或重複的元件 ID。 | 修改其中一個物件的 `UIElementId`。 |
| `element.kind-unknown` | Error | 元件記錄的元件名稱不在元件集裡。 | 把元件登記回元件集，或刪掉這個元件重新建立。 |
| `button.action-unrouted` | Error | 按鈕的動作找不到對應的路由。 | 補上路由，或修改按鈕的 `action`。訊息提到 Prefab 本體時，請修正審核實例並套用覆寫。 |
| `button.unbound` | Warning | 畫面裡的 `Button` 沒有動作（沒有 `UIButtonAction`，或動作是空的），`onClick` 也沒有在 Inspector 接監聽。由程式控制的畫面與清單項目裡的按鈕不算。 | 給按鈕 `action` 並接一條路由；要由程式處理時用 `Emit` 路由，在頁面腳本的 `OnAction` 接收。 |
| `text.missing-glyph` | Warning | TMP 文字的預設內容（包含輸入框的 placeholder 與下拉選單的選項）有字型和它的 fallback 都沒有的字元。空白與控制字元不算。訊息列出缺少的字元，最多十個。 | 設定含有這些字元的預設字型（`defaultFontPath`），或修改預設文字。 |
| `input.event-system-multiple` | Error | 工作區所在的 Scene 有超過一個啟用的 EventSystem。 | 只保留一個。 |
| `input.event-system-missing` | Error | 載入的 Scene 裡沒有 EventSystem。 | 在工作區的 Scene 或共用的 Scene 加回一個。 |
| `input.event-system-disabled` | Error | 載入的 Scene 裡有 EventSystem，但沒有啟用。 | 啟用它的 GameObject 與元件。 |
| `input.event-system-shared` | Warning | 一起載入的 Scene 有不只一個啟用的 EventSystem。 | 執行期同時載入這些 Scene 時只能留一個；分開載入時可以忽略。 |
| `input.module-invalid` | Error | EventSystem 的輸入模組不是 `InputSystemUIInputModule`。 | 換成 Input System UI Input Module。 |
| `input.handling-legacy` | Error | Active Input Handling 只啟用舊的 Input Manager。 | 見「確認專案」第 5 點。 |
| `widgets.set-missing` | Error | 專案沒有元件集。 | 執行 **Tools → UniGameUI → Install Starter Widgets**。 |
| `widgets.set-multiple` | Error | 專案裡有超過一個元件集。 | 只保留一個。 |
| `widgets.name-invalid` | Error | 元件集裡有空白或重複的名稱。 | 在元件集的 Inspector 修正。 |
| `widgets.prefab-missing` | Error | 元件集的項目沒有 Prefab。 | 重新指定；內建元件可以用 Install Starter Widgets 補回。 |
| `widgets.component-missing` | Error | 元件 Prefab 缺少它比照的類別需要的元件。 | 補上元件，或改登記成別的類別。 |
| `constants.file-occupied` | Error | ID 常數要寫入的檔案已經被另一個 Scene 或使用者的檔案占用。 | 把那個檔案移走或改名。 |
| `constants.name-conflict` | Error | 兩個 ID 會產生同一個常數名稱，或和所在的類別同名。 | 修改其中一個 ID。 |
| `constants.scene-conflict` | Error | 兩個有工作區的 Scene 會產生同名的常數類別。 | 把其中一個 Scene 改名。 |
| `constants.scene-unsaved` | Warning | Scene 還沒有存成資產，無法產生 ID 常數。 | 儲存 Scene。 |
| `constants.outdated` | Warning | ID 常數還沒有產生，或和目前的藍圖不一致。 | 儲存 Scene，或在藍圖按 **Validate**。 |
