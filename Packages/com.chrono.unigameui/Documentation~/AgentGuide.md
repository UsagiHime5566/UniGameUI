# UniGameUI 操作指南

從這份文件的公開工具操作 Unity。交付時指出實際使用的 Scene、藍圖路徑、Play 驗證結果，以及需要專案自行接上的事件。

## 確認專案

1. 確認 Unity 6.3+、uGUI 2.0、Input System 套件，並找出使用者要編輯的 Scene。套件匯入後沒有預建工作區或 UI。
2. 預設採用 `SingleScene`。頁面、參考解析度、節點位置與路由都存於 Scene 的 UniGameUI 工作區。所有 Popup 都是 Prefab。若使用者要共用頁面資產，建立工作區時可以明確指定 `Prefab` 模式。
3. 已存在的工作區沿用原模式與穩定 ID。記錄場景是否有尚未存檔的修改，並查看 `UIAuthoring.HasReviewOverrides(entry)`。有開啟中的 Editor 時，優先在其 Edit Mode 使用公開 C# API 或 **Apply JSON Plan…**。CLI 在該專案已由另一個 Editor 開啟時無法取得專案鎖，使用隔離副本測試。
4. 使用 `UIDOTweenSetup.IsReady` 或 `UIDOTweenSetup.Status` 確認 DOTween。狀態依序區分已就緒、已匯入但待 Setup、未安裝。請在 Edit Mode 完成安裝或 Setup，再套用 JSON 計畫或驗收。未就緒時，Validate 回報一項工作區錯誤，`UIPlan.Apply` 與 `ApplyAsync` 會直接回報安裝說明。執行期保留瞬間切換和一次性警告。
5. 只支援新的 Input System。**Project Settings → Player → Active Input Handling** 要選 `Input System Package (New)` 或 `Both`，改完要重新啟動 Editor。載入的 Scene 裡要有剛好一個 EventSystem，輸入模組是 `InputSystemUIInputModule`；建立工作區時會自動加上。這三點不符合時 Validate 都會回報錯誤，JSON 計畫也套不進去。

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
- `startPage` 是可選的頁面 ID，指定空字串可清除起始頁。路由種類有 `ShowPage`、`OpenPopup`、`Back`、`ClosePopup`、`Emit`。前兩種需要 `target`。同一來源頁的一個 `action` 對應一條路由。
- `ShowPage` 可設定 `"history": "Replace"`。可用值為 `Push`（預設）、`Replace`、`Clear`。`ClosePopup` 可設定字串結果，例如 `"result": "yes"`。省略或留空代表這條路由關閉時沒有結果。將任一欄位寫在其他種類的路由上，計畫會在套用前指出路由與欄位。
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
- `textKey`、`spriteKey`、`placeholderKey`、`optionKeys` 是多語系鍵值，見「多語系」。`optionKeys` 的數量要和選項一樣多，不需要鍵值的選項寫空字串。
- 矩形座標是 Unity RectTransform 的 anchor 與參考解析度下的座標，Y 向上。新元件省略 `rect.size` 時，大小取自元件 Prefab 的根節點。
- 元件之間沒有父子關係，全部放在 View 的 Content 底下。建立的先後順序就是繪製順序，要當底圖的 `Panel` 請寫在前面。
- **欄位寫錯位置會報錯。** 欄位用在不支援它的元件上（例如 `Slider` 寫 `text`）、`kind` 不在元件集裡、更新時 `kind` 和原本不同，計畫都會在套用前失敗，訊息指出畫面、元件與欄位，而且不留下任何變更。
- `updateExisting` 只能用在由計畫建立的元件。手動加上 `UIElementId` 的物件沒有記錄元件名稱，請直接在 Editor 修改。
- **計畫沒寫的欄位不會成為 Prefab 覆寫**，之後修改元件 Prefab 時這些實例會跟著變。計畫寫了的欄位會固定在該實例上，即使數值剛好和元件 Prefab 相同。
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
- 設定時會把元件集所有 Prefab 裡「目前使用舊預設字型」的文字換成新字型。已經被專案改成別的字型的文字不會動。
- 個別元件的 `fontPath` 照舊可用，會成為該實例的覆寫。多語言的字元涵蓋請用 TMP 的 fallback 字型，框架不處理依語言切換字型。

## ID 常數

遊戲程式一律使用產生的常數。場景存檔、成功套用 JSON 或在藍圖按 **Validate** 時，工具會產生或更新 `Assets/UniGameUI/Generated/<場景名稱>UI.cs`。首次使用未命名的場景時，先存成 Scene 資產。也可選 **Tools → UniGameUI → Generate ID Constants**，或呼叫 `UIIdConstants.Generate(root)`。等 Editor 編譯和組件重載完成再引用新的常數。

```csharp
using UniGameUI.Generated;

var result = await root.TriggerAsync(WorkshopUI.Views.Workshop, WorkshopUI.Actions.OpenSettings);
```

`Views` 含頁面與 Popup ID，`Actions` 含路由與按鈕使用的動作，`Elements` 按畫面分組並加上 `View` 後綴，例如 `WorkshopUI.Elements.WorkshopView.OpenSettings`。畫面和其元件可以使用相同的 ID，例如 `WorkshopUI.Elements.ConfirmView.Confirm`。同一組內的 ID 或工作區 Scene 名稱仍須避免轉成相同的 PascalCase 名稱。清單項目裡的元件 ID 不會產生常數。`UIValidation.Check(root)` 會回報名稱衝突並提示常數過期。

工具會在生成目錄沒有 asmdef 時建立 `UniGameUI.Generated.asmdef`。遊戲程式若有自己的 asmdef，請在它的 references 加入 `UniGameUI.Generated`。若生成目錄已有自訂 asmdef，請引用該組件。

## Editor C# API

- `UIAuthoring.FindWorkspace(scene)`、`CreateWorkspace(scene, mode)`、`ConfigureReview(root, resolution, columns)`。
- `UIAuthoring.CreatePage(root, id, title, prefabPath)`、`CreatePopup(root, id, title, prefabPath)` 回傳 Scene 中的 `UIView`。將自訂 uGUI 階層加到 `view.Content`。保留外框的排列用途；`view.Motion` 是預設動畫作用的位置。
- `UIAuthoring.FindEntry(root, id)`、`UIPlan.FindElement(view, id)`、`BindButton(button, action)`、`Connect(root, source, action, kind, target, history, result)`、`Disconnect(root, source, action)`、`SetStartPage(root, id)`。`history`、`result` 是可選參數。
- `UIAuthoring.UpdateRoute(root, source, originalAction, action, kind, target, history, result)` 用於更新既有路由。改名時一併更新來源 View 的按鈕綁定，並合併為一次 Undo。若來源是 Prefab 審核實例，將覆寫套用後 Prefab 才會使用新動作名稱。
- `UIAuthoring.SetTransition(view, entering, preset, duration, direction, ease, distance, angle, zoomScale)` 和 `SetPageTransitionMode(root, mode)`。
- `UIAuthoring.ArrangeReview(root)`、`FocusReview(root, id)`、`PreviewPopupOnPage(root, popupId, pageId)`、`HasReviewOverrides(entry)`、`ApplyReviewOverrides(root, id)`。最後一項適合提交本次新建立的 Prefab。既有實例的覆寫先交給使用者審核。
- `UIWidgets.Find()` 回傳元件集（沒有時是 null），`Install()` 補上缺少的起始元件，`Register(name, prefabPath, like)` 登記自訂元件，`SetDefaultFont(fontAssetPath)` 設定預設字型，`ConfirmPrefabPath` 是起始確認框的路徑。
- `UIValidation.Check(root)` 回傳結構化錯誤與警告。對於自訂子階層，用 Unity Editor 的 Undo 和序列化 API 儲存變更。

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

遊戲程式可呼叫 `await root.ShowPageAsync(id, data)`、`await root.OpenPopupAsync(id, data)`、`await root.ClosePopupAsync()` 或 `await root.BackAsync()`。每個 Unity `Awaitable` 只等待一次。轉場中收到的導航請求會立即回傳 `Busy`，不排隊。

`root.ShowPageAsync(id, data, history)` 的 `Push` 會把離開的頁面加入歷史，`Replace` 會略過離開的頁面，`Clear` 會在切頁成功後清空歷史。切頁或返回成功後會移除歷史尾端與目前頁相同的項目。失敗或取消時保留原歷史。`HistoryCount` 可查還能返回幾頁，`ClearHistory()` 可直接清空。返回會先關最上層的 Popup。返回上一頁會重用頁面實例與上次顯示時的資料。

`CurrentPage`、`TopPopup`、`Popups` 可查目前畫面。`TryGetView(id, out var view)` 取得已存在的實例。單 Scene 頁面隨 Scene 載入，Prefab 頁面在首次顯示時建立，兩種頁面都會留到 Scene 卸載。Popup 在開啟期間才可查到。

`OpenPopupAsync` 等到進場結束。`OpenPopupForResultAsync<T>` 等到退場與隱藏通知結束。Popup 內可呼叫 `View.Close(value)`，或在 ClosePopup 路由設定字串結果。只有最上層且開著的 Popup 可以透過 `View.Close` 關閉。

`UIPopupResult<T>.Status` 反映開啟失敗、正常關閉或取消。`HasValue` 表示收到型別相符的結果，確認後可讀取 `Value`，也可直接呼叫 `GetValueOrDefault(fallback)`。返回、返回鍵、點遮罩、切頁或未指定結果的關閉會回傳 `Completed`，此時 `HasValue` 為 false。型別不符時 Console 會指出實際和期望的型別。取消 token、停用或銷毀工作區，以及 Popup 被外部銷毀都會以 `Canceled` 結束等待。取消 token 或停用工作區時，已開啟的 Popup 保持原狀。

### Popup 的兩個選項

- **重用**（計畫的 `"popupCache": "Reuse"`，或 View Inspector 的「關閉後保留並重用」）：關閉後不銷毀，停用並保留；下次開啟用同一個實例，並移到 Popup 層的最上面。生命週期方法、資料、等待結果的行為都和重新建立時相同，`TryGetView` 與 `Popups` 仍然只包含開著的 Popup。**重用的 Popup 會留著上次的狀態**，例如捲動位置、輸入內容、頁面腳本的欄位，要在 `OnShowing` 重設。預設是每次開啟都重新建立。
- **點遮罩關閉**（計畫的 `"overlayClick": "Close"`，或 View Inspector 的「點遮罩關閉」）：點擊遮罩等同不帶結果的關閉。點在 Popup 的內容範圍內不算。只在該 Popup 是最上層而且沒有轉場進行時有效，和 `backKey` 互相獨立。

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
3. 最上層畫面設定為不回應返回鍵（計畫的 `"backKey": "Ignore"`）：不處理。
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
| `Busy` | 另一個導航正在進行。從同步通知提出時會記錄警告，動畫播放期間的一般忙碌請求安靜回傳。 |
| `Canceled` | 操作取消，安靜回傳。 |
| `Unavailable` | 請求合法，目前狀態無法執行。通常會警告。沒有上一頁的返回安靜回傳。 |
| `NotFound` | 畫面 ID、Prefab 或路由設定缺失或不符合請求。Console 會記錄錯誤。 |
| `Failed` | 轉場、事件或導航執行時發生例外。Console 會記錄操作說明與例外堆疊。 |

診斷會帶呼叫的操作、相關 ID 或來源與動作，以及工作區的 Scene 名稱。

自訂 DOTween 轉場請繼承 `UIDOTweenAnimation`，在 `Build(Sequence sequence, UIView view, bool entering)` 加入 tween，掛到該 View 並選 `Custom`。播放、取消和還原 `Motion`／`CanvasGroup` 由框架管理。自訂動畫可用 `AppendCallback` 插入回呼。

## 審核

1. **Tools → UniGameUI → Blueprint** 會開啟藍圖。選節點可定位對應 Scene 頁面。用右欄編輯動作、切換種類、目標、返回歷史、關閉結果和起始頁，也能從輸出端拖線。在 Play Mode 用實際按鈕試走新增的互動。
2. **檢視全部 UI** 會把 Scene 切到 2D 並框選所有審核畫面。Popup 可以在工作區右欄疊在指定頁面上預覽，用 **重新排列** 回到網格。
3. 在 View Inspector 可預覽進場或退場，並設定預設選取元件、是否回應返回鍵；Popup 另外有「關閉後保留並重用」與「點遮罩關閉」。`UIRoot` 的 Inspector 可以關掉「取消鍵當成返回」。C# 可用 `UIAnimationPreview.Start(view, entering)` 與 `UIAnimationPreview.Stop()`。Validate 會核對 Scene 預覽和執行期 Prefab 上的按鈕；改名後遇到 Prefab 綁定錯誤，套用對應預覽的覆寫。若舊 View 缺少子 Canvas，按藍圖工具列的 **補上子 Canvas**，工具會將所需元件補到 Prefab 本體，其他預覽覆寫繼續由使用者審核。
4. 在互動 Editor 進 Play 前，若有待套用的覆寫，可以選 **套用並進入**、**直接進入** 或 **取消**。直接進入會使用目前已儲存的 Prefab。若多個 Scene 的審核實例同時修改同一 Prefab，先選擇要手動套用的實例。

Player 建置處理器會從複製的 Scene 中移除審核框與 Prefab 審核實例，並保留單 Scene 模式的頁面物件。打包成 AssetBundle 或 Addressables 的 Scene 會保留審核複本，執行期會停用審核層，並依藍圖使用已儲存的 Prefab。
