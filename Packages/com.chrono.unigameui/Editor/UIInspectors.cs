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
            inspector.Add(new PropertyField(serializedObject.FindProperty("showing"), "Showing"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("shown"), "Shown"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("hiding"), "Hiding"));
            inspector.Add(new PropertyField(serializedObject.FindProperty("hidden"), "Hidden"));
            return inspector;
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
