using UnityEditor;
#if DOTWEEN
using DG.Tweening;
using UnityEditor.SceneManagement;
using UnityEngine;
#endif

namespace UniGameUI.Editor
{
    [InitializeOnLoad]
    public static class UIAnimationPreview
    {
#if DOTWEEN
        private static UIView view;
        private static UIViewPose pose;
        private static Sequence sequence;
        private static double startedAt;

        static UIAnimationPreview()
        {
            EditorSceneManager.sceneSaving += (_, _) => Stop();
            EditorApplication.playModeStateChanged += _ => Stop();
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            Undo.undoRedoPerformed += Stop;
        }
#endif

        public static void Start(UIView target, bool entering)
        {
#if DOTWEEN
            if (!target || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorUtility.IsPersistent(target) || !target.gameObject.activeInHierarchy ||
                !target.Motion || !target.Group || PrefabStageUtility.GetPrefabStage(target.gameObject) != null)
                return;
            Stop();
            var transition = entering ? target.Enter : target.Exit;
            if (transition.Preset == UITransitionPreset.None ||
                (transition.Preset != UITransitionPreset.Custom && transition.Duration <= 0))
                return;
            view = target;
            pose = new UIViewPose(target);
            try
            {
                sequence = UITransition.CreateSequence(target, transition, entering, pose);
                sequence.SetAutoKill(false).ForceInit();
                startedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += Update;
            }
            catch (System.Exception exception)
            {
                Stop();
                Debug.LogException(exception, target);
            }
#endif
        }

        public static void Stop()
        {
#if DOTWEEN
            EditorApplication.update -= Update;
            sequence?.Kill();
            sequence = null;
            pose.Restore(view);
            view = null;
            SceneView.RepaintAll();
#endif
        }

#if DOTWEEN
        private static void Update()
        {
            if (!view || sequence == null || !sequence.IsActive())
            {
                Stop();
                return;
            }
            var duration = sequence.Duration();
            var elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
            if (elapsed > duration + 0.12f)
            {
                Stop();
                return;
            }
            sequence.Goto(Mathf.Min(duration, elapsed), false);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
}
