using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniGameUI.Editor
{
    // 計畫裡的元件：檢查欄位、建立元件 Prefab 的實例、寫入有提供的欄位。
    public static partial class UIPlan
    {
        [Serializable]
        private sealed class WidgetSpec
        {
            public string name;
            public string prefabPath;
            public string like;
        }

        // 新欄位一律用字串、數字（未設定為 NaN）或陣列，才分得出「沒寫」和「寫了」。
        [Serializable]
        private sealed class ElementSpec
        {
            public string id;
            public string kind;
            public bool updateExisting;
            public RectSpec rect;
            public string text;
            public string textKey;
            public string action;
            public float fontSize;
            public string fontPath;
            public string color;
            public string textColor;
            public string alignment;
            public string spritePath;
            public string spriteKey;
            public string state;
            public float min = float.NaN;
            public float max = float.NaN;
            public float value = float.NaN;
            public string numberMode;
            public string[] options;
            public string[] optionKeys;
            public string placeholder;
            public string placeholderKey;
            public string direction;
            public float spacing = float.NaN;
            public string itemPrefabPath;
        }

        private enum TextAlign { Left, Center, Right }
        private enum ToggleState { Off, On }
        private enum NumberMode { Decimal, Whole }

        private const string DefaultElementKind = "Text";

        // 計畫明確寫出的 TMP 欄位要固定成覆寫，這些是對應的序列化欄位。
        private const string FontProperty = "m_fontAsset";
        private const string OptionsProperty = "m_Options.m_Options";
        private static readonly string[] FontSizeProperties = { "m_fontSize", "m_fontSizeBase" };
        private static readonly Vector2 FallbackSize = new Vector2(240, 72);

        // 每個行為類別能在計畫裡寫的欄位。id、kind、rect、updateExisting 所有元件都能用。
        private static readonly Dictionary<UIWidgetKind, string[]> ElementFields =
            new Dictionary<UIWidgetKind, string[]>
            {
                [UIWidgetKind.Custom] = Array.Empty<string>(),
                [UIWidgetKind.Text] = new[]
                    { "text", "textKey", "fontSize", "fontPath", "textColor", "color", "alignment" },
                [UIWidgetKind.Image] = new[] { "color", "spritePath", "spriteKey" },
                [UIWidgetKind.Panel] = new[] { "color", "spritePath", "spriteKey" },
                [UIWidgetKind.Button] = new[]
                    { "text", "textKey", "action", "color", "spritePath", "textColor", "fontSize", "fontPath" },
                [UIWidgetKind.Toggle] = new[] { "text", "textKey", "state", "textColor", "fontSize", "fontPath" },
                [UIWidgetKind.Slider] = new[] { "min", "max", "value", "numberMode" },
                [UIWidgetKind.ProgressBar] = new[] { "value" },
                [UIWidgetKind.Dropdown] = new[] { "options", "optionKeys", "value" },
                [UIWidgetKind.InputField] = new[]
                    { "text", "placeholder", "placeholderKey", "fontSize", "fontPath" },
                [UIWidgetKind.ScrollList] = new[] { "direction", "spacing", "itemPrefabPath" }
            };

        /// <summary>
        /// 套用這份計畫時可以使用的元件名稱：元件集現有的項目、還沒安裝的內建元件，以及計畫自己登記的元件。
        /// </summary>
        private sealed class WidgetCatalog
        {
            private readonly Dictionary<string, (UIWidgetKind Kind, GameObject Prefab)> widgets =
                new Dictionary<string, (UIWidgetKind, GameObject)>(StringComparer.Ordinal);

            public string Names => string.Join("、", widgets.Keys.OrderBy(name => name, StringComparer.Ordinal));

            public bool Contains(string name) => widgets.ContainsKey(name);

            public void Set(string name, UIWidgetKind kind, GameObject prefab) => widgets[name] = (kind, prefab);

            public bool TryGet(string name, out UIWidgetKind kind, out GameObject prefab)
            {
                var found = widgets.TryGetValue(name, out var widget);
                kind = widget.Kind;
                prefab = widget.Prefab;
                return found;
            }
        }

        private static WidgetCatalog ReadCatalog(Plan plan)
        {
            var sets = UIWidgets.FindAll();
            if (sets.Count > 1)
                throw new InvalidOperationException(UIWidgets.MultipleSetsMessage(sets));

            var set = sets.FirstOrDefault();
            var catalog = new WidgetCatalog();
            if (set)
            {
                foreach (var entry in set.Widgets)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Name))
                        catalog.Set(entry.Name, entry.Kind, entry.Prefab);
                }
            }
            // 內建元件缺少時會在套用前自動補上，所以名稱一律可用。
            foreach (var kind in UIWidgets.BuiltIn)
            {
                if (!catalog.Contains(kind.ToString()))
                    catalog.Set(kind.ToString(), kind, null);
            }

            var registered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var widget in plan.widgets ?? Array.Empty<WidgetSpec>())
            {
                if (!registered.Add(widget.name ?? ""))
                    throw new ArgumentException($"計畫的 widgets 重複登記「{widget.name}」。");
                var like = Parse(widget.like, UIWidgetKind.Custom);
                var prefab = UIWidgets.CheckRegistration(set, widget.name, widget.prefabPath, like, out _);
                catalog.Set(widget.name, like, prefab);
            }
            return catalog;
        }

        private static string KindName(ElementSpec spec)
        {
            return string.IsNullOrEmpty(spec.kind) ? DefaultElementKind : spec.kind;
        }

        private static string ElementLabel(string viewId, ElementSpec spec)
        {
            return $"畫面「{viewId}」的元件「{spec.id}」";
        }

        private static void PreflightElements(ViewSpec view, UIView existingReview, WidgetCatalog catalog)
        {
            var elementIds = new HashSet<string>();
            foreach (var element in view.elements ?? Array.Empty<ElementSpec>())
            {
                UIAuthoring.CheckId(element.id);
                if (!elementIds.Add(element.id))
                    throw new ArgumentException($"{view.id}: duplicate element ID '{element.id}'.");

                var label = ElementLabel(view.id, element);
                var name = KindName(element);
                if (!catalog.TryGet(name, out var kind, out _))
                {
                    throw new ArgumentException(
                        $"{label}使用了元件集裡沒有的名稱「{name}」。可用的名稱：{catalog.Names}。");
                }

                var allowed = ElementFields[kind];
                foreach (var field in ProvidedFields(element))
                {
                    if (Array.IndexOf(allowed, field) >= 0)
                        continue;
                    throw new ArgumentException(kind == UIWidgetKind.Custom
                        ? $"{label}（{name}）是 Custom 類別，計畫只能設定 rect，不支援 {field} 欄位。"
                        : $"{label}（{name}）不支援 {field} 欄位。");
                }
                CheckElementValues(label, view.id, element, kind);

                var existing = existingReview ? FindElement(existingReview, element.id) : null;
                if (!existing || !element.updateExisting)
                    continue;
                var recorded = existing.GetComponent<UIElementId>().Kind;
                if (string.IsNullOrEmpty(recorded))
                {
                    throw new ArgumentException(
                        $"{label}不是由計畫建立的（沒有記錄元件名稱），不能使用 updateExisting。");
                }
                if (recorded != name)
                    throw new ArgumentException($"{label}原本是 {recorded}，不能用 updateExisting 改成 {name}。");
            }
        }

        private static IEnumerable<string> ProvidedFields(ElementSpec spec)
        {
            if (spec.text != null) yield return "text";
            if (spec.textKey != null) yield return "textKey";
            if (spec.action != null) yield return "action";
            if (spec.fontSize != 0) yield return "fontSize";
            if (spec.fontPath != null) yield return "fontPath";
            if (spec.color != null) yield return "color";
            if (spec.textColor != null) yield return "textColor";
            if (spec.alignment != null) yield return "alignment";
            if (spec.spritePath != null) yield return "spritePath";
            if (spec.spriteKey != null) yield return "spriteKey";
            if (spec.state != null) yield return "state";
            if (!float.IsNaN(spec.min)) yield return "min";
            if (!float.IsNaN(spec.max)) yield return "max";
            if (!float.IsNaN(spec.value)) yield return "value";
            if (spec.numberMode != null) yield return "numberMode";
            if (spec.options != null) yield return "options";
            if (spec.optionKeys != null) yield return "optionKeys";
            if (spec.placeholder != null) yield return "placeholder";
            if (spec.placeholderKey != null) yield return "placeholderKey";
            if (spec.direction != null) yield return "direction";
            if (!float.IsNaN(spec.spacing)) yield return "spacing";
            if (spec.itemPrefabPath != null) yield return "itemPrefabPath";
        }

        private static void CheckElementValues(string label, string viewId, ElementSpec spec, UIWidgetKind kind)
        {
            if (!string.IsNullOrEmpty(spec.action))
                UIAuthoring.CheckId(spec.action);
            if (spec.rect != null && (IsNonPositive(spec.rect.size.x) || IsNonPositive(spec.rect.size.y)))
                throw new ArgumentException($"{viewId}/{spec.id}: size must be positive.");
            if (spec.fontSize < 0 || !float.IsFinite(spec.fontSize))
                throw new ArgumentException($"{label}的 fontSize 必須是正數。");

            Parse(spec.alignment, TextAlign.Center);
            Parse(spec.state, ToggleState.Off);
            Parse(spec.numberMode, NumberMode.Decimal);
            Parse(spec.direction, UIListDirection.Vertical);
            if (!string.IsNullOrEmpty(spec.color))
                ParseColor(spec.color);
            if (!string.IsNullOrEmpty(spec.textColor))
                ParseColor(spec.textColor);
            if (!string.IsNullOrEmpty(spec.fontPath))
                LoadAsset<TMP_FontAsset>(spec.fontPath);
            if (!string.IsNullOrEmpty(spec.spritePath))
                LoadAsset<Sprite>(spec.spritePath);
            if (!string.IsNullOrEmpty(spec.itemPrefabPath))
                LoadAsset<GameObject>(spec.itemPrefabPath);

            foreach (var (field, number) in new[] { ("min", spec.min), ("max", spec.max), ("value", spec.value) })
            {
                if (float.IsInfinity(number))
                    throw new ArgumentException($"{label}的 {field} 必須是有限的數字。");
            }
            if (!float.IsNaN(spec.min) && !float.IsNaN(spec.max) && spec.min >= spec.max)
                throw new ArgumentException($"{label}的 min 必須小於 max。");
            if (!float.IsNaN(spec.spacing) && (spec.spacing < 0 || float.IsInfinity(spec.spacing)))
                throw new ArgumentException($"{label}的 spacing 不可以是負數。");
            if (spec.options != null && spec.options.Any(option => option == null))
                throw new ArgumentException($"{label}的 options 不可以包含 null。");
            if (spec.optionKeys != null && spec.optionKeys.Any(key => key == null))
                throw new ArgumentException($"{label}的 optionKeys 不可以包含 null，不需要鍵值的選項請寫空字串。");
            if (spec.options != null && spec.optionKeys != null && spec.options.Length != spec.optionKeys.Length)
            {
                throw new ArgumentException($"{label}的 optionKeys 有 {spec.optionKeys.Length} 個，" +
                    $"要和 options 的 {spec.options.Length} 個一樣多。");
            }

            if (float.IsNaN(spec.value))
                return;
            if (kind == UIWidgetKind.ProgressBar && (spec.value < 0 || spec.value > 1))
                throw new ArgumentException($"{label}的 value 必須在 0 到 1 之間。");
            if (kind == UIWidgetKind.Dropdown)
            {
                if (spec.value < 0 || spec.value != Mathf.Floor(spec.value))
                    throw new ArgumentException($"{label}的 value 是選項索引，必須是 0 以上的整數。");
                if (spec.options != null && spec.value >= spec.options.Length)
                    throw new ArgumentException($"{label}的 value 超出 options 的範圍。");
            }
        }

        private static void ApplyElement(UIView view, ElementSpec spec, UIWidgetSet set)
        {
            var gameObject = FindElement(view, spec.id);
            var created = !gameObject;
            if (!created && !spec.updateExisting)
                return;

            var label = ElementLabel(view.Id, spec);
            var name = KindName(spec);
            var entry = set.Find(name) ??
                throw new ArgumentException($"{label}使用了元件集裡沒有的名稱「{name}」。");
            if (created)
            {
                if (!entry.Prefab)
                {
                    throw new InvalidOperationException($"元件集裡「{name}」的 Prefab 遺失。" +
                        "請在元件集重新指定，或執行 Tools → UniGameUI → Install Starter Widgets。");
                }
                gameObject = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, view.Content);
                Undo.RegisterCreatedObjectUndo(gameObject, "Create UI element");
                gameObject.name = spec.id;
                var elementId = GetOrAdd<UIElementId>(gameObject);
                elementId.id = spec.id;
                elementId.kind = name;
                UIAuthoring.RecordOverride(elementId);
            }

            ApplyRect((RectTransform)gameObject.transform, spec.rect ?? new RectSpec(), created, entry.Prefab);
            switch (entry.Kind)
            {
                case UIWidgetKind.Text:
                    ApplyLabel(Require<TMP_Text>(gameObject, label, name), spec, spec.textColor ?? spec.color);
                    break;
                case UIWidgetKind.Image:
                case UIWidgetKind.Panel:
                    ApplyImage(Require<Image>(gameObject, label, name), spec);
                    if (spec.spriteKey != null)
                        SetKey<UILocalizedImage>(Require<Image>(gameObject, label, name), spec.spriteKey);
                    break;
                case UIWidgetKind.Button:
                    ApplyButton(gameObject, spec, label, name);
                    break;
                case UIWidgetKind.Toggle:
                    ApplyToggle(gameObject, spec, label, name);
                    break;
                case UIWidgetKind.Slider:
                    ApplySlider(Require<Slider>(gameObject, label, name), spec, label);
                    break;
                case UIWidgetKind.ProgressBar:
                    ApplyProgressBar(Require<UIProgressBar>(gameObject, label, name), spec);
                    break;
                case UIWidgetKind.Dropdown:
                    ApplyDropdown(Require<TMP_Dropdown>(gameObject, label, name), spec, label);
                    break;
                case UIWidgetKind.InputField:
                    ApplyInputField(Require<TMP_InputField>(gameObject, label, name), spec);
                    break;
                case UIWidgetKind.ScrollList:
                    ApplyScrollList(Require<UIList>(gameObject, label, name), spec, label);
                    break;
            }
        }

        // 沒有提供的版面欄位維持目前的值；新元件從預設值開始，大小取自元件 Prefab 的根節點。
        private static void ApplyRect(RectTransform rect, RectSpec layout, bool created, GameObject prefab)
        {
            if (!created && layout.IsEmpty)
                return;

            Undo.RecordObject(rect, "Set UI layout");
            if (created || !IsUnset(layout.anchor))
            {
                var anchor = Merge(created ? DefaultAnchor : rect.anchorMin, layout.anchor);
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
            }
            if (created || !IsUnset(layout.position))
                rect.anchoredPosition = Merge(created ? Vector2.zero : rect.anchoredPosition, layout.position);
            if (created || !IsUnset(layout.size))
                rect.sizeDelta = Merge(created ? PrefabSize(prefab) : rect.sizeDelta, layout.size);
            UIAuthoring.RecordOverride(rect);
        }

        private static Vector2 PrefabSize(GameObject prefab)
        {
            if (!prefab || prefab.transform is not RectTransform rect)
                return FallbackSize;
            var size = rect.anchorMin == rect.anchorMax ? rect.sizeDelta : rect.rect.size;
            return size.x > 0 && size.y > 0 ? size : FallbackSize;
        }

        private static bool HasLabelFields(ElementSpec spec, string color)
        {
            return spec.text != null || spec.textKey != null || spec.fontSize > 0 ||
                !string.IsNullOrEmpty(spec.fontPath) || !string.IsNullOrEmpty(color) ||
                !string.IsNullOrEmpty(spec.alignment);
        }

        // 多語系鍵值記在目標元件所在的物件上。起始元件已經掛好空的鍵值元件，自訂元件沒有時補上。
        private static void SetKey<T>(Component target, string key) where T : UILocalizedElement
        {
            var localized = GetOrAdd<T>(target.gameObject);
            var serialized = new SerializedObject(localized);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedProperties();
            UIAuthoring.RecordOverride(localized, "key");
        }

        private static void ApplyLabel(TMP_Text text, ElementSpec spec, string color)
        {
            if (!HasLabelFields(spec, color))
                return;
            if (spec.textKey != null)
                SetKey<UILocalizedText>(text, spec.textKey);

            var pinned = new List<string>();
            Undo.RecordObject(text, "Set UI text");
            if (spec.text != null)
            {
                text.text = spec.text;
                pinned.Add("m_text");
            }
            if (spec.fontSize > 0)
            {
                text.fontSize = spec.fontSize;
                pinned.AddRange(FontSizeProperties);
            }
            if (!string.IsNullOrEmpty(spec.fontPath))
            {
                text.font = LoadAsset<TMP_FontAsset>(spec.fontPath);
                pinned.Add(FontProperty);
            }
            if (!string.IsNullOrEmpty(color))
            {
                text.color = ParseColor(color);
                pinned.Add("m_fontColor");
                pinned.Add("m_fontColor32");
            }
            if (!string.IsNullOrEmpty(spec.alignment))
            {
                text.horizontalAlignment = Parse(spec.alignment, TextAlign.Center) switch
                {
                    TextAlign.Left => HorizontalAlignmentOptions.Left,
                    TextAlign.Right => HorizontalAlignmentOptions.Right,
                    _ => HorizontalAlignmentOptions.Center
                };
                pinned.Add("m_HorizontalAlignment");
            }
            UIAuthoring.RecordOverride(text, pinned.ToArray());
        }

        private static void ApplyImage(Image image, ElementSpec spec)
        {
            if (string.IsNullOrEmpty(spec.color) && string.IsNullOrEmpty(spec.spritePath))
                return;

            var pinned = new List<string>();
            Undo.RecordObject(image, "Set UI image");
            if (!string.IsNullOrEmpty(spec.color))
            {
                image.color = ParseColor(spec.color);
                pinned.Add("m_Color");
            }
            if (!string.IsNullOrEmpty(spec.spritePath))
            {
                image.sprite = LoadAsset<Sprite>(spec.spritePath);
                pinned.Add("m_Sprite");
            }
            UIAuthoring.RecordOverride(image, pinned.ToArray());
        }

        private static void ApplyButton(GameObject gameObject, ElementSpec spec, string label, string name)
        {
            var button = Require<Button>(gameObject, label, name);
            if (!string.IsNullOrEmpty(spec.color) || !string.IsNullOrEmpty(spec.spritePath))
            {
                var image = button.targetGraphic as Image;
                ApplyImage(image ? image : Require<Image>(gameObject, label, name), spec);
            }
            if (!string.IsNullOrEmpty(spec.action))
                UIAuthoring.BindButton(button, spec.action);
            if (HasLabelFields(spec, spec.textColor))
                ApplyLabel(Require<TMP_Text>(gameObject, label, name), spec, spec.textColor);
        }

        private static void ApplyToggle(GameObject gameObject, ElementSpec spec, string label, string name)
        {
            var toggle = Require<Toggle>(gameObject, label, name);
            if (!string.IsNullOrEmpty(spec.state))
            {
                Undo.RecordObject(toggle, "Set UI toggle");
                toggle.SetIsOnWithoutNotify(Parse(spec.state, ToggleState.Off) == ToggleState.On);
                UIAuthoring.RecordOverride(toggle, "m_IsOn");
            }
            if (HasLabelFields(spec, spec.textColor))
                ApplyLabel(Require<TMP_Text>(gameObject, label, name), spec, spec.textColor);
        }

        private static void ApplySlider(Slider slider, ElementSpec spec, string label)
        {
            if (float.IsNaN(spec.min) && float.IsNaN(spec.max) && float.IsNaN(spec.value) &&
                string.IsNullOrEmpty(spec.numberMode))
                return;
            var min = Merge(slider.minValue, spec.min);
            var max = Merge(slider.maxValue, spec.max);
            if (min >= max)
                throw new ArgumentException($"{label}套用後的 min（{min}）必須小於 max（{max}）。");

            var pinned = new List<string>();
            Undo.RecordObject(slider, "Set UI slider");
            if (!string.IsNullOrEmpty(spec.numberMode))
            {
                slider.wholeNumbers = Parse(spec.numberMode, NumberMode.Decimal) == NumberMode.Whole;
                pinned.Add("m_WholeNumbers");
            }
            slider.minValue = min;
            slider.maxValue = max;
            if (!float.IsNaN(spec.min))
                pinned.Add("m_MinValue");
            if (!float.IsNaN(spec.max))
                pinned.Add("m_MaxValue");
            if (!float.IsNaN(spec.value))
            {
                slider.SetValueWithoutNotify(spec.value);
                pinned.Add("m_Value");
            }
            UIAuthoring.RecordOverride(slider, pinned.ToArray());
        }

        private static void ApplyProgressBar(UIProgressBar bar, ElementSpec spec)
        {
            if (float.IsNaN(spec.value))
                return;

            Undo.RecordObject(bar, "Set UI progress bar");
            if (bar.Fill)
                Undo.RecordObject(bar.Fill, "Set UI progress bar");
            bar.Value = spec.value;
            UIAuthoring.RecordOverride(bar, "value");
            if (bar.Fill)
                UIAuthoring.RecordOverride(bar.Fill, "m_FillAmount");
        }

        private static void ApplyDropdown(TMP_Dropdown dropdown, ElementSpec spec, string label)
        {
            if (spec.options == null && spec.optionKeys == null && float.IsNaN(spec.value))
                return;
            var count = spec.options?.Length ?? dropdown.options.Count;
            if (!float.IsNaN(spec.value) && spec.value >= count)
                throw new ArgumentException($"{label}的 value 超出選項的範圍（共 {count} 個）。");
            if (spec.optionKeys != null)
            {
                if (spec.optionKeys.Length != count)
                {
                    throw new ArgumentException(
                        $"{label}的 optionKeys 有 {spec.optionKeys.Length} 個，要和選項的 {count} 個一樣多。");
                }
                var localized = GetOrAdd<UILocalizedDropdown>(dropdown.gameObject);
                Undo.RecordObject(localized, "Set UI dropdown keys");
                localized.keys = spec.optionKeys.ToArray();
                UIAuthoring.RecordOverride(localized, "keys");
            }
            if (spec.options == null && float.IsNaN(spec.value))
                return;

            Undo.RecordObject(dropdown, "Set UI dropdown");
            if (dropdown.captionText)
                Undo.RecordObject(dropdown.captionText, "Set UI dropdown");
            if (spec.options != null)
                dropdown.options = spec.options.Select(option => new TMP_Dropdown.OptionData(option)).ToList();
            if (!float.IsNaN(spec.value))
                dropdown.SetValueWithoutNotify((int)spec.value);
            else if (dropdown.value >= count)
                dropdown.SetValueWithoutNotify(Mathf.Max(0, count - 1));
            dropdown.RefreshShownValue();
            var pinned = new List<string>();
            if (spec.options != null)
                pinned.Add(OptionsProperty);
            if (!float.IsNaN(spec.value))
                pinned.Add("m_Value");
            UIAuthoring.RecordOverride(dropdown, pinned.ToArray());
            if (dropdown.captionText)
                UIAuthoring.RecordOverride(dropdown.captionText);
        }

        private static void ApplyInputField(TMP_InputField field, ElementSpec spec)
        {
            var placeholder = field.placeholder as TMP_Text;
            if (spec.placeholderKey != null && placeholder)
                SetKey<UILocalizedText>(placeholder, spec.placeholderKey);
            if (spec.placeholder != null && placeholder)
            {
                Undo.RecordObject(placeholder, "Set UI input placeholder");
                placeholder.text = spec.placeholder;
                UIAuthoring.RecordOverride(placeholder, "m_text");
            }
            if (spec.text == null && spec.fontSize <= 0 && string.IsNullOrEmpty(spec.fontPath))
                return;

            var changed = new List<UnityEngine.Object> { field };
            if (field.textComponent)
                changed.Add(field.textComponent);
            if (placeholder)
                changed.Add(placeholder);
            Undo.RecordObjects(changed.ToArray(), "Set UI input field");
            if (spec.text != null)
            {
                field.SetTextWithoutNotify(spec.text);
                if (field.textComponent)
                    field.textComponent.text = spec.text;
            }
            if (spec.fontSize > 0)
                field.pointSize = spec.fontSize;
            if (!string.IsNullOrEmpty(spec.fontPath))
                field.fontAsset = LoadAsset<TMP_FontAsset>(spec.fontPath);

            var fieldPins = new List<string>();
            var textPins = new List<string>();
            if (spec.text != null)
                fieldPins.Add("m_Text");
            if (spec.fontSize > 0)
            {
                fieldPins.Add("m_GlobalPointSize");
                textPins.AddRange(FontSizeProperties);
            }
            if (!string.IsNullOrEmpty(spec.fontPath))
            {
                fieldPins.Add("m_GlobalFontAsset");
                textPins.Add(FontProperty);
            }
            foreach (var item in changed)
                UIAuthoring.RecordOverride(item, (item == field ? fieldPins : textPins).ToArray());
        }

        private static void ApplyScrollList(UIList list, ElementSpec spec, string label)
        {
            if (!string.IsNullOrEmpty(spec.direction) || !string.IsNullOrEmpty(spec.itemPrefabPath))
            {
                var pinned = new List<string>();
                Undo.RecordObject(list, "Set UI list");
                if (!string.IsNullOrEmpty(spec.direction))
                {
                    list.direction = Parse(spec.direction, UIListDirection.Vertical);
                    pinned.Add("direction");
                }
                if (!string.IsNullOrEmpty(spec.itemPrefabPath))
                {
                    list.itemPrefab = LoadAsset<GameObject>(spec.itemPrefabPath);
                    pinned.Add("itemPrefab");
                }
                UIAuthoring.RecordOverride(list, pinned.ToArray());
            }
            if (float.IsNaN(spec.spacing))
                return;

            var layout = list.Content ? list.Content.GetComponent<HorizontalOrVerticalLayoutGroup>() : null;
            if (!layout)
                throw new ArgumentException($"{label}的內容物件沒有版面配置元件，無法設定 spacing。");
            Undo.RecordObject(layout, "Set UI list spacing");
            layout.spacing = spec.spacing;
            UIAuthoring.RecordOverride(layout, "m_Spacing");
        }

        private static T Require<T>(GameObject gameObject, string label, string name) where T : Component
        {
            var component = gameObject.GetComponentInChildren<T>(true);
            if (!component)
                throw new InvalidOperationException($"{label}（{name}）的 Prefab 缺少 {typeof(T).Name}。");
            return component;
        }
    }
}
