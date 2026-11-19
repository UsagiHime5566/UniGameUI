using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniGameUI.Editor
{
    public enum UIDOTweenStatus { Ready, Imported, Missing }

    [InitializeOnLoad]
    public static class UIDOTweenSetup
    {
        private const string PromptKey = "UniGameUI.DOTweenSetup.Prompted";
        private const string InstallMenu = "Tools/UniGameUI/Install DOTween…";
        private const string UtilityPanel = "Tools/Demigiant/DOTween Utility Panel";
        private const string AssetStoreUrl = "https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676";

        static UIDOTweenSetup()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += PromptOnce;
                EditorApplication.playModeStateChanged += state =>
                {
                    if (state == PlayModeStateChange.EnteredEditMode)
                        EditorApplication.delayCall += PromptOnce;
                };
            }
        }

        public static bool IsReady => UITransition.IsDOTweenReady;

        public static UIDOTweenStatus Status
        {
            get
            {
                if (IsReady)
                    return UIDOTweenStatus.Ready;

                var hasType = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(assembly => assembly.GetType("DG.Tweening.DOTween", false) != null);
                if (hasType || AssetDatabase.FindAssets("DOTween").Any(guid =>
                    Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid))
                        .Equals("DOTween.dll", StringComparison.OrdinalIgnoreCase)))
                    return UIDOTweenStatus.Imported;

                return UIDOTweenStatus.Missing;
            }
        }

        public static string FindLocalPackage()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var unity = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity"),
                RuntimePlatform.OSXEditor => Path.Combine(home, "Library", "Unity"),
                RuntimePlatform.LinuxEditor => Path.Combine(home, ".local", "share", "unity3d"),
                _ => null
            };
            return unity == null ? null : FindLocalPackage(Path.Combine(unity, "Asset Store-5.x"));
        }

        internal static string FindLocalPackage(string cache)
        {
            if (string.IsNullOrEmpty(cache))
                return null;
            var publisher = Path.Combine(cache, "Demigiant");
            if (!Directory.Exists(publisher))
                return null;

            try
            {
                return Directory.EnumerateFiles(publisher, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        AttributesToSkip = FileAttributes.ReparsePoint
                    })
                    .Where(path => Path.GetExtension(path).Equals(".unitypackage", StringComparison.OrdinalIgnoreCase))
                    .Where(path => IsStandardPackage(Path.GetFileNameWithoutExtension(path)))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .FirstOrDefault();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static bool IsStandardPackage(string name)
        {
            var parts = name.Split(new[] { ' ', '(', ')', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 && parts[0].Equals("DOTween", StringComparison.OrdinalIgnoreCase) &&
                (parts.Length == 1 || (parts.Length == 3 &&
                    parts[1].Equals("HOTween", StringComparison.OrdinalIgnoreCase) &&
                    parts[2].Equals("v2", StringComparison.OrdinalIgnoreCase)));
        }

        public static bool ImportLocalPackage()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Status != UIDOTweenStatus.Missing)
                return false;
            var path = FindLocalPackage();
            if (path == null)
                return false;
            AssetDatabase.ImportPackage(path, false);
            return true;
        }

        internal static string SetupMessage(UIDOTweenStatus status)
        {
            return status == UIDOTweenStatus.Imported
                ? "已匯入 DOTween。請開啟 Tools → Demigiant → DOTween Utility Panel，執行 Setup DOTween，啟用 DOTWEEN define。"
                : "UniGameUI 需要 DOTween。請由 Tools → UniGameUI → Install DOTween… 匯入 DOTween，並完成 Setup DOTween。";
        }

        internal static void CheckReady()
        {
            var status = Status;
            if (status != UIDOTweenStatus.Ready)
                throw new InvalidOperationException(SetupMessage(status));
        }

        private static void PromptOnce()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
                IsReady || SessionState.GetBool(PromptKey, false))
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += PromptOnce;
                return;
            }

            SessionState.SetBool(PromptKey, true);
            ShowPrompt();
        }

        [MenuItem(InstallMenu, priority = 31)]
        internal static void ShowPrompt()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            SessionState.SetBool(PromptKey, true);
            var status = Status;
            if (status == UIDOTweenStatus.Ready)
            {
                EditorUtility.DisplayDialog("UniGameUI DOTween", "DOTween 已就緒。", "關閉");
                return;
            }
            if (status == UIDOTweenStatus.Imported)
            {
                if (EditorUtility.DisplayDialog("設定 DOTween", SetupMessage(status), "開啟 Utility Panel", "稍後") &&
                    !EditorApplication.ExecuteMenuItem(UtilityPanel))
                    Debug.LogWarning(SetupMessage(status));
                return;
            }

            if (FindLocalPackage() == null)
            {
                if (EditorUtility.DisplayDialog("安裝 DOTween", SetupMessage(status), "開啟 Asset Store 頁面", "稍後"))
                    Application.OpenURL(AssetStoreUrl);
                return;
            }

            var choice = EditorUtility.DisplayDialogComplex("安裝 DOTween", SetupMessage(status),
                "從本機匯入", "稍後", "開啟 Asset Store 頁面");
            if (choice == 0)
                ImportLocalPackage();
            else if (choice == 2)
                Application.OpenURL(AssetStoreUrl);
        }
    }
}
