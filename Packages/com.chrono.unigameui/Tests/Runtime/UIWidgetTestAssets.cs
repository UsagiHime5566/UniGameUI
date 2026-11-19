using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniGameUI.Tests
{
    /// <summary>
    /// 進入 Play Mode 之前，把起始元件安裝到測試專用的資料夾，讓 PlayMode 測試能用真正的元件 Prefab。
    /// 不使用專案在 Assets/UniGameUI/Widgets 的元件集，測試結束後整個資料夾會刪掉。
    /// </summary>
    public sealed class UIWidgetTestAssets : IPrebuildSetup, IPostBuildCleanup
    {
        public const string Folder = "Assets/UniGameUI-PlayModeWidgets";
        private const string TmpFolder = "Assets/TextMesh Pro";
        private const string ImportedTmp = "UniGameUI.Tests.ImportedTmp";

        public void Setup()
        {
#if UNITY_EDITOR
            var hadTmp = System.IO.Directory.Exists(TmpFolder) || System.IO.File.Exists(TmpFolder + ".meta");
            if (UnityEditor.AssetDatabase.IsValidFolder(Folder))
                UnityEditor.AssetDatabase.DeleteAsset(Folder);

            var widgets = Type.GetType("UniGameUI.Editor.UIWidgets, UniGameUI.Editor", true);
            var scope = widgets.GetField("FolderOverride", BindingFlags.Static | BindingFlags.NonPublic);
            scope.SetValue(null, Folder);
            try
            {
                widgets.GetMethod("Install", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            }
            finally
            {
                scope.SetValue(null, null);
            }
            UnityEditor.SessionState.SetBool(ImportedTmp, !hadTmp && UnityEditor.AssetDatabase.IsValidFolder(TmpFolder));
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.IsValidFolder(Folder))
                UnityEditor.AssetDatabase.DeleteAsset(Folder);
            if (UnityEditor.SessionState.GetBool(ImportedTmp, false) &&
                UnityEditor.AssetDatabase.IsValidFolder(TmpFolder))
                UnityEditor.AssetDatabase.DeleteAsset(TmpFolder);
            UnityEditor.SessionState.EraseBool(ImportedTmp);
#endif
        }

        public static GameObject Load(string name)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab");
            if (prefab)
                return prefab;
#endif
            throw new InvalidOperationException($"找不到測試用的起始元件「{name}」，這些案例只能在 Editor 裡執行。");
        }
    }
}
