using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 用程式組出起始元件的 Prefab。外觀只用 Unity 內建的 UI 圖和元件集的預設字型，各專案自行修改美術。
    /// </summary>
    internal static class UIStarterWidgets
    {
        private static readonly Color Accent = new Color32(45, 106, 204, 255);
        private static readonly Color DarkText = new Color32(50, 50, 50, 255);
        private static readonly Color Panel = new Color(0.10f, 0.13f, 0.18f, 0.92f);
        private static readonly Color Track = new Color(0.20f, 0.23f, 0.28f, 1f);
        private static readonly Color Row = new Color32(58, 66, 82, 255);

        internal static GameObject BuildWidget(UIWidgetKind kind, string path, UIWidgetSet set)
        {
            return Save(path, scene => kind switch
            {
                UIWidgetKind.Text => CreateText(scene, set),
                UIWidgetKind.Image => CreateImage(scene),
                UIWidgetKind.Panel => CreatePanelImage(scene),
                UIWidgetKind.Button => CreateButton(scene, set),
                UIWidgetKind.Toggle => CreateToggle(scene, set),
                UIWidgetKind.Slider => CreateSlider(scene),
                UIWidgetKind.ProgressBar => CreateProgressBar(scene),
                UIWidgetKind.Dropdown => CreateDropdown(scene, set),
                UIWidgetKind.InputField => CreateInputField(scene, set),
                UIWidgetKind.ScrollList => CreateScrollList(scene, set),
                _ => throw new ArgumentException($"沒有名為「{kind}」的起始元件。")
            });
        }

        internal static GameObject BuildListItem(string path, UIWidgetSet set)
        {
            return Save(path, scene => CreateListItem(scene, set));
        }

        /// <summary>
        /// 在預覽 Scene 裡組出物件再存成 Prefab，過程不會動到使用者開著的 Scene。
        /// </summary>
        internal static GameObject Save(string path, Func<Scene, GameObject> create)
        {
            UIAuthoring.EnsureAssetFolder(path);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = create(scene);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out var saved);
                if (!saved || !prefab)
                    throw new InvalidOperationException($"無法儲存起始元件「{path}」。");
                return prefab;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static GameObject CreateText(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "Text", new Vector2(320, 60));
            Label(root, "Text", 32, Color.white, TextAlignmentOptions.Center, set);
            return root.gameObject;
        }

        private static GameObject CreateImage(Scene scene)
        {
            var root = Root(scene, "Image", new Vector2(160, 160));
            Graphic(root, null, Color.white, false);
            return root.gameObject;
        }

        private static GameObject CreatePanelImage(Scene scene)
        {
            var root = Root(scene, "Panel", new Vector2(480, 320));
            Graphic(root, "Background.psd", Panel, false);
            return root.gameObject;
        }

        private static GameObject CreateButton(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "Button", new Vector2(240, 72));
            var image = Graphic(root, "UISprite.psd", Accent, true);
            root.gameObject.AddComponent<Button>().targetGraphic = image;

            var label = Child("Label", root);
            Fill(label, new Vector2(12, 4), new Vector2(-12, -4));
            Label(label, "Button", 32, Color.white, TextAlignmentOptions.Center, set);
            return root.gameObject;
        }

        private static GameObject CreateToggle(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "Toggle", new Vector2(280, 48));
            var background = Child("Background", root);
            background.anchorMin = background.anchorMax = new Vector2(0f, 0.5f);
            background.pivot = new Vector2(0f, 0.5f);
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = new Vector2(44, 44);
            var backgroundImage = Graphic(background, "UISprite.psd", Color.white, true);

            var checkmark = Child("Checkmark", background);
            Fill(checkmark, new Vector2(6, 6), new Vector2(-6, -6));
            var checkmarkImage = Graphic(checkmark, "Checkmark.psd", DarkText, false);

            var label = Child("Label", root);
            Fill(label, new Vector2(58, 0), Vector2.zero);
            // 點文字也能切換，和 Unity 內建的 Toggle 一致。
            Label(label, "Toggle", 28, Color.white, TextAlignmentOptions.Left, set).raycastTarget = true;

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = true;
            return root.gameObject;
        }

        private static GameObject CreateSlider(Scene scene)
        {
            var root = Root(scene, "Slider", new Vector2(320, 40));
            var background = Child("Background", root);
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.sizeDelta = Vector2.zero;
            Graphic(background, "Background.psd", Track, true);

            var fillArea = Child("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.anchoredPosition = new Vector2(-10, 0);
            fillArea.sizeDelta = new Vector2(-40, 0);
            var fill = Child("Fill", fillArea);
            Graphic(fill, "UISprite.psd", Accent, false);
            fill.sizeDelta = new Vector2(20, 0);

            var handleArea = Child("Handle Slide Area", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.sizeDelta = new Vector2(-40, 0);
            var handle = Child("Handle", handleArea);
            var handleImage = Graphic(handle, "Knob.psd", Color.white, true);
            handle.sizeDelta = new Vector2(40, 0);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0.5f;
            return root.gameObject;
        }

        private static GameObject CreateProgressBar(Scene scene)
        {
            var root = Root(scene, "ProgressBar", new Vector2(320, 28));
            Graphic(root, "Background.psd", Track, false);

            var fill = Child("Fill", root);
            Fill(fill, new Vector2(3, 3), new Vector2(-3, -3));
            // Filled 類型需要 Sprite 才會依 fillAmount 裁切；沒有 Sprite 時 Image 會畫成整塊。
            var fillImage = Graphic(fill, "UISprite.psd", Accent, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0.5f;

            var bar = root.gameObject.AddComponent<UIProgressBar>();
            bar.fill = fillImage;
            bar.value = 0.5f;
            return root.gameObject;
        }

        private static GameObject CreateDropdown(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "Dropdown", new Vector2(320, 60));
            var backgroundImage = Graphic(root, "UISprite.psd", Color.white, true);

            var label = Child("Label", root);
            Fill(label, new Vector2(20, 12), new Vector2(-50, -14));
            var caption = Label(label, "Option A", 28, DarkText, TextAlignmentOptions.Left, set);
            caption.textWrappingMode = TextWrappingModes.NoWrap;

            var arrow = Child("Arrow", root);
            arrow.anchorMin = arrow.anchorMax = new Vector2(1f, 0.5f);
            arrow.sizeDelta = new Vector2(40, 40);
            arrow.anchoredPosition = new Vector2(-30, 0);
            Graphic(arrow, "DropdownArrow.psd", Color.white, false);

            var template = Child("Template", root);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0, 4);
            template.sizeDelta = new Vector2(0, 300);
            Graphic(template, "UISprite.psd", Color.white, true);

            var viewport = Child("Viewport", template);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = new Vector2(-36, 0);
            viewport.pivot = new Vector2(0f, 1f);
            Graphic(viewport, "UIMask.psd", Color.white, true);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = Child("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 56);

            var item = Child("Item", content);
            item.anchorMin = new Vector2(0f, 0.5f);
            item.anchorMax = new Vector2(1f, 0.5f);
            item.sizeDelta = new Vector2(0, 40);
            var itemBackground = Child("Item Background", item);
            Fill(itemBackground, Vector2.zero, Vector2.zero);
            var itemBackgroundImage = Graphic(itemBackground, null, new Color32(245, 245, 245, 255), true);
            var itemCheckmark = Child("Item Checkmark", item);
            itemCheckmark.anchorMin = itemCheckmark.anchorMax = new Vector2(0f, 0.5f);
            itemCheckmark.sizeDelta = new Vector2(40, 40);
            itemCheckmark.anchoredPosition = new Vector2(20, 0);
            var itemCheckmarkImage = Graphic(itemCheckmark, "Checkmark.psd", DarkText, false);
            var itemLabel = Child("Item Label", item);
            Fill(itemLabel, new Vector2(40, 2), new Vector2(-20, -4));
            var itemText = Label(itemLabel, "Option A", 28, DarkText, TextAlignmentOptions.Left, set);
            itemText.textWrappingMode = TextWrappingModes.NoWrap;
            var itemToggle = item.gameObject.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = true;

            var scrollbarRect = Child("Scrollbar", template);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = Vector2.one;
            scrollbarRect.sizeDelta = new Vector2(40, 0);
            Graphic(scrollbarRect, "Background.psd", new Color32(225, 225, 225, 255), true);
            var slidingArea = Child("Sliding Area", scrollbarRect);
            Fill(slidingArea, new Vector2(20, 20), new Vector2(-20, -20));
            var scrollbarHandle = Child("Handle", slidingArea);
            var scrollbarHandleImage = Graphic(scrollbarHandle, "UISprite.psd", new Color32(160, 160, 160, 255), true);
            scrollbarHandle.sizeDelta = new Vector2(40, 40);
            var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = scrollbarHandle;
            scrollbar.targetGraphic = scrollbarHandleImage;
            scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            var templateScroll = template.gameObject.AddComponent<ScrollRect>();
            templateScroll.content = content;
            templateScroll.viewport = viewport;
            templateScroll.horizontal = false;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;
            templateScroll.verticalScrollbar = scrollbar;
            templateScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            templateScroll.verticalScrollbarSpacing = -6;

            var dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = backgroundImage;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.options.Add(new TMP_Dropdown.OptionData("Option A"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Option B"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Option C"));
            dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject CreateInputField(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "InputField", new Vector2(320, 60));
            Graphic(root, "InputFieldBackground.psd", Color.white, true);

            var textArea = Child("Text Area", root);
            Fill(textArea, new Vector2(20, 12), new Vector2(-20, -14));
            textArea.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-16, -10, -16, -10);

            var placeholderRect = Child("Placeholder", textArea);
            Fill(placeholderRect, Vector2.zero, Vector2.zero);
            var placeholderColor = DarkText;
            placeholderColor.a = 0.5f;
            var placeholder = Label(placeholderRect, "Enter text...", 28, placeholderColor,
                TextAlignmentOptions.Left, set);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            placeholder.extraPadding = true;
            placeholderRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var textRect = Child("Text", textArea);
            Fill(textRect, Vector2.zero, Vector2.zero);
            var text = Label(textRect, "", 28, DarkText, TextAlignmentOptions.Left, set);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.extraPadding = true;

            var field = root.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = textArea;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = text.font;
            field.pointSize = 28;
            // 用手把或鍵盤移到輸入框時不要立刻進入編輯，按確定才開始；否則方向輸入會被輸入框吃掉。
            field.shouldActivateOnSelect = false;
            return root.gameObject;
        }

        private static GameObject CreateScrollList(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "ScrollList", new Vector2(480, 360));
            // 背景要接收點擊，在空白處拖曳才捲得動。
            Graphic(root, "Background.psd", Panel, true);

            var viewport = Child("Viewport", root);
            Fill(viewport, new Vector2(8, 8), new Vector2(-8, -8));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Child("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<UIListLayout>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.SetVertical(true);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var list = root.gameObject.AddComponent<UIList>();
            list.content = content;
            list.itemPrefab = set ? set.listItem : null;
            return root.gameObject;
        }

        private static GameObject CreateListItem(Scene scene, UIWidgetSet set)
        {
            var root = Root(scene, "ListItem", new Vector2(440, 64));
            var image = Graphic(root, "UISprite.psd", Row, true);
            root.gameObject.AddComponent<Button>().targetGraphic = image;

            var label = Child("Label", root);
            Fill(label, new Vector2(16, 4), new Vector2(-16, -4));
            Label(label, "Item", 28, Color.white, TextAlignmentOptions.Left, set);
            label.gameObject.AddComponent<UIElementId>().id = "label";
            return root.gameObject;
        }

        internal static RectTransform Root(Scene scene, string name, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var rect = (RectTransform)gameObject.transform;
            rect.sizeDelta = size;
            return rect;
        }

        internal static RectTransform Child(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Fill(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            UIRoot.Stretch(rect);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Image Graphic(RectTransform rect, string sprite, Color color, bool raycastTarget)
        {
            var image = rect.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + sprite);
                if (image.sprite && image.sprite.border != Vector4.zero)
                    image.type = Image.Type.Sliced;
            }
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TextMeshProUGUI Label(RectTransform rect, string text, float size, Color color,
            TextAlignmentOptions alignment, UIWidgetSet set)
        {
            // TextMeshProUGUI 第一次加上時會改掉預設大小的 RectTransform，所以加完再還原版面。
            var anchorMin = rect.anchorMin;
            var anchorMax = rect.anchorMax;
            var offsetMin = rect.offsetMin;
            var offsetMax = rect.offsetMax;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            if (set && set.defaultFont)
                label.font = set.defaultFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}
