using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniGameUI.Editor
{
    [InitializeOnLoad]
    internal static class UIReviewPlayMode
    {
        static UIReviewPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || Application.isBatchMode)
                return;

            UIAnimationPreview.Stop();
            // 自動走訪不詢問，直接使用已儲存的 Prefab；它的報告會記一筆說明。
            if (UIWalk.IsRunning)
                return;
            var pending = PendingReviews();
            if (pending.Count == 0)
                return;

            var names = pending.Select(entry => $"{entry.Review.gameObject.scene.name} / {entry.Title}");
            var message = "下列畫面有待套用的 Prefab 覆寫。直接進入會使用目前已儲存的 Prefab。\n\n" +
                string.Join("\n", names);
            var choice = EditorUtility.DisplayDialogComplex("UniGameUI Prefab 覆寫", message,
                "套用並進入", "取消", "直接進入");
            if (choice == 2)
                return;
            if (choice != 0)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            try
            {
                ApplyPendingReviews(pending);
            }
            catch (Exception exception)
            {
                EditorApplication.isPlaying = false;
                EditorUtility.DisplayDialog("UniGameUI 無法套用 Prefab", exception.Message, "關閉");
            }
        }

        internal static List<UIEntry> PendingReviews()
        {
            var pending = new List<UIEntry>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
                    continue;
                foreach (var gameObject in scene.GetRootGameObjects())
                {
                    foreach (var root in gameObject.GetComponentsInChildren<UIRoot>(true))
                        pending.AddRange(root.Entries.Where(UIAuthoring.HasReviewOverrides));
                }
            }
            return pending;
        }

        internal static void ApplyPendingReviews(IReadOnlyList<UIEntry> pending)
        {
            var shared = pending.GroupBy(entry => entry.Prefab).FirstOrDefault(group => group.Count() > 1);
            if (shared != null)
            {
                throw new InvalidOperationException($"「{shared.Key.name}」有多個預覽同時修改。請選擇要手動套用的預覽。");
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply UniGameUI Prefab overrides");
            try
            {
                foreach (var entry in pending)
                    UIAuthoring.ApplyReviewOverrides(entry);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
    }
}
