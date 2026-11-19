using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniGameUI.Editor
{
    public sealed class UIReviewBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report != null || BuildPipeline.isBuildingPlayer)
                StripReview(scene);
        }

        internal static void StripReview(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                foreach (var root in gameObject.GetComponentsInChildren<UIRoot>(true))
                {
                    if (!root || !root.reviewLayer)
                        continue;

                    var review = root.reviewLayer;
                    foreach (var entry in root.Entries)
                    {
                        if (entry == null)
                            continue;
                        if (root.Mode == UIAuthoringMode.SingleScene && entry.Kind == UIEntryKind.Page)
                        {
                            if (entry.Review && entry.Review.transform.IsChildOf(review))
                                entry.Review.transform.SetParent(root.transform, false);
                        }
                        else
                        {
                            entry.review = null;
                        }
                    }

                    root.reviewLayer = null;
                    Object.DestroyImmediate(review.gameObject);
                }
            }
        }
    }
}
