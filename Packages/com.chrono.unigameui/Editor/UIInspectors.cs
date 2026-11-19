using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniGameUI.Editor
{
    [CustomEditor(typeof(UIRoot))]
    public sealed class UIRootInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = (UIRoot)target;
            var inspector = new VisualElement();
            inspector.Add(new Label($"工作區模式　{root.Mode}"));
            var resolution = new Vector2Field("設計解析度") { value = root.ReferenceResolution };
            var columns = new IntegerField("每排列數") { value = root.ReviewColumns };
            inspector.Add(resolution);
            inspector.Add(columns);
            inspector.Add(new Button(() => UIAuthoring.ConfigureReview(root, resolution.value, columns.value))
                { text = "更新審核排列設定" });
            var transitionMode = new PropertyField(serializedObject.FindProperty("pageTransitionMode"), "切頁動畫")
                { bindingPath = "pageTransitionMode" };
            void RefreshTransitionMode(PlayModeStateChange change)
            {
                transitionMode.SetEnabled(change == PlayModeStateChange.EnteredEditMode);
            }
            transitionMode.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            transitionMode.RegisterCallback<AttachToPanelEvent>(_ =>
                EditorApplication.playModeStateChanged += RefreshTransitionMode);
            transitionMode.RegisterCallback<DetachFromPanelEvent>(_ =>
                EditorApplication.playModeStateChanged -= RefreshTransitionMode);
            inspector.Add(transitionMode);
            inspector.Add(new Button(UIBlueprintWindow.Open) { text = "開啟藍圖" });
            inspector.Add(new Button(() => UIAuthoring.FocusReview(root)) { text = "檢視全部 UI" });
            inspector.Add(new PropertyField(serializedObject.FindProperty("handlesBackKey"), "取消鍵當成返回"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("actionEmitted"), "送出動作事件"));
            return inspector;
        }
    }

    [CustomEditor(typeof(UIView))]
    public sealed class UIViewInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var view = (UIView)target;
            var inspector = new VisualElement();
            inspector.Add(new Label($"View ID　{view.Id}"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("enter"), "進場動畫"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("exit"), "退場動畫"));
            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.Add(new Button(() => UIAnimationPreview.Start(view, true)) { text = "預覽進場" });
            controls.Add(new Button(() => UIAnimationPreview.Start(view, false)) { text = "預覽退場" });
            controls.Add(new Button(UIAnimationPreview.Stop) { text = "停止預覽" });
            controls.SetEnabled(UITransition.IsDOTweenReady && !EditorUtility.IsPersistent(view));
            inspector.Add(controls);
            if (!UIDOTweenSetup.IsReady)
                inspector.Add(new HelpBox(UIDOTweenSetup.SetupMessage(UIDOTweenSetup.Status),
                    HelpBoxMessageType.Info));
            inspector.Add(new PropertyField(serializedObject.FindProperty("defaultSelected"), "預設選取元件"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("blocksBackKey"), "不回應返回鍵"));
            var popupOptions = new VisualElement();
            popupOptions.Add(new PropertyField(serializedObject.FindProperty("reusesInstance"), "關閉後保留並重用"));
            popupOptions.Add(new PropertyField(serializedObject.FindProperty("closesOnOverlayClick"), "點遮罩關閉"));
            popupOptions.style.display = IsPopup(view) ? DisplayStyle.Flex : DisplayStyle.None;
            inspector.Add(popupOptions);
            inspector.Add(new PropertyField(serializedObject.FindProperty("showing"), "Showing"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("shown"), "Shown"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("hiding"), "Hiding"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("hidden"), "Hidden"));
            return inspector;
        }

        // UIView 本身不記錄種類。有遮罩或已經開了 Popup 選項的就是 Popup，否則查所屬工作區的登記。
        private static bool IsPopup(UIView view)
        {
            if (view.Overlay || view.ReusesInstance || view.ClosesOnOverlayClick)
                return true;
            var root = view.GetComponentInParent<UIRoot>(true);
            var entry = root
                ? root.Entries.FirstOrDefault(item => item != null && (item.Review == view || item.Prefab == view))
                : null;
            return entry != null && entry.Kind == UIEntryKind.Popup;
        }
    }

    [CustomEditor(typeof(UIWidgetSet))]
    public sealed class UIWidgetSetInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var set = (UIWidgetSet)target;
            var inspector = new VisualElement();
            inspector.Add(new HelpBox("畫面上的元件都是這裡登記的 Prefab 的實例。修改 Prefab 的美術，所有畫面跟著變。",
                HelpBoxMessageType.Info));

            // 換預設字型時要連同元件 Prefab 裡的文字一起換，所以不直接綁定欄位。
            var font = new ObjectField("預設字型")
            {
                objectType = typeof(TMP_FontAsset), allowSceneObjects = false, value = set.DefaultFont
            };
            font.RegisterValueChangedCallback(change =>
            {
                try
                {
                    UIWidgets.SetDefaultFont(AssetDatabase.GetAssetPath(change.newValue));
                }
                catch (Exception exception)
                {
                    font.SetValueWithoutNotify(set.DefaultFont);
                    EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
                }
            });
            inspector.Add(font);
            inspector.Add(new PropertyField(serializedObject.FindProperty("widgets"), "元件"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("listItem"), "清單項目"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("confirm"), "確認框"));
            inspector.Add(new Button(() =>
            {
                try
                {
                    UIWidgets.Install();
                }
                catch (Exception exception)
                {
                    EditorUtility.DisplayDialog("UniGameUI", exception.Message, "關閉");
                }
            }) { text = "補上缺少的起始元件" });
            return inspector;
        }
    }

    [CustomPropertyDrawer(typeof(UIWidgetEntry))]
    public sealed class UIWidgetEntryDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement();
            row.Add(new PropertyField(property.FindPropertyRelative("name"), "名稱"));
            row.Add(new PropertyField(property.FindPropertyRelative("kind"), "行為類別"));
            row.Add(new PropertyField(property.FindPropertyRelative("prefab"), "Prefab"));
            return row;
        }
    }

    [InitializeOnLoad]
    internal static class UISceneReviewGizmos
    {
        private static readonly Vector3[] corners = new Vector3[5];

        static UISceneReviewGizmos()
        {
            SceneView.duringSceneGui += Draw;
        }

        private static void Draw(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint || !sceneView.in2DMode || EditorApplication.isPlaying)
                return;
            var active = Selection.activeGameObject;
            var root = active ? active.GetComponentInParent<UIRoot>() : null;
            if (!root)
                return;

            using var scope = new Handles.DrawingScope();
            foreach (var entry in root.Entries)
            {
                if (!entry.Review)
                    continue;
                ((RectTransform)entry.Review.transform).GetWorldCorners(corners);
                corners[4] = corners[0];
                Handles.color = entry.Kind == UIEntryKind.Popup
                    ? new Color(0.68f, 0.51f, 0.91f, 0.85f)
                    : new Color(0.41f, 0.71f, 0.95f, 0.85f);
                Handles.DrawAAPolyLine(2, corners);
                Handles.Label(corners[1] + Vector3.up * HandleUtility.GetHandleSize(corners[1]) * 0.16f,
                    entry.Title);
            }
        }
    }
}
