using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UniGameUI.Editor
{
    internal static class UITmpResources
    {
        private const string AssetFolder = "Assets/TextMesh Pro";
        private const int TarBlockSize = 512;
        private const string PackageName = "TMP Essential Resources";
        private static bool importing;

        private sealed class Resource
        {
            public string Path;
            public byte[] Data;
            public byte[] Meta;
        }

        public static async Awaitable ImportWithEditorAsync()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings"))
                return;
            if (importing)
                throw new InvalidOperationException("TMP Essential Resources are already being imported.");

            var bundle = FindBundle();
            var completion = new AwaitableCompletionSource();
            var deadline = EditorApplication.timeSinceStartup + 60;
            string error = null;
            importing = true;
            EditorApplication.update += CheckImport;
            AssetDatabase.importPackageFailed += ImportFailed;
            AssetDatabase.importPackageCancelled += ImportCanceled;
            try
            {
                AssetDatabase.ImportPackage(bundle, false);
                await completion.Awaitable;
            }
            finally
            {
                EditorApplication.update -= CheckImport;
                AssetDatabase.importPackageFailed -= ImportFailed;
                AssetDatabase.importPackageCancelled -= ImportCanceled;
                importing = false;
            }

            void CheckImport()
            {
                if (error != null)
                    completion.TrySetException(new InvalidOperationException(error));
                else if (!EditorApplication.isUpdating && Resources.Load<TMP_Settings>("TMP Settings"))
                    completion.TrySetResult();
                else if (EditorApplication.timeSinceStartup >= deadline)
                    completion.TrySetException(new TimeoutException("TMP Essential Resources did not finish importing."));
            }

            void ImportFailed(string name, string message)
            {
                if (name == PackageName)
                    error = message;
            }

            void ImportCanceled(string name)
            {
                if (name == PackageName)
                    error = "TMP Essential Resources import was canceled.";
            }
        }

        public static void Ensure()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings"))
                return;

            var destination = Path.Combine(Application.dataPath, "TextMesh Pro");
            if (Directory.Exists(destination) || File.Exists(destination + ".meta"))
                throw new InvalidOperationException(
                    "Finish the existing TMP Essential Resources import from Window > TextMeshPro before creating text.");

            var bundle = FindBundle();

            // The regular Unity package import is queued and may not run inside -executeMethod -quit.
            var temp = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
            var staging = Path.Combine(temp, "UniGameUI-" + Guid.NewGuid().ToString("N"));
            var stagedFolder = Path.Combine(staging, AssetFolder);
            try
            {
                foreach (var resource in ReadPackage(bundle))
                {
                    var path = resource.Path;
                    if (string.IsNullOrEmpty(path) || resource.Meta == null ||
                        (path != AssetFolder && !path.StartsWith(AssetFolder + "/", StringComparison.Ordinal)) ||
                        path.Contains('\\') || Array.Exists(path.Split('/'), part =>
                            part is ".." or "." or "" || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                        throw new InvalidDataException("Unexpected asset path in the TMP Essential Resources package.");

                    var location = Path.Combine(staging, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(location));
                    if (resource.Data == null)
                        Directory.CreateDirectory(location);
                    else
                        File.WriteAllBytes(location, resource.Data);
                    File.WriteAllBytes(location + ".meta", resource.Meta);
                }

                Directory.Move(stagedFolder, destination);
                try
                {
                    File.Move(stagedFolder + ".meta", destination + ".meta");
                }
                catch
                {
                    Directory.Move(destination, stagedFolder);
                    throw;
                }
            }
            finally
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!Resources.Load<TMP_Settings>("TMP Settings"))
                throw new InvalidOperationException("TextMeshPro Essential Resources could not be loaded.");
        }

        private static string FindBundle()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            var bundle = package == null ? "" : Path.Combine(package.resolvedPath, "Package Resources",
                PackageName + ".unitypackage");
            if (!File.Exists(bundle))
                throw new FileNotFoundException("The installed uGUI package is missing TMP Essential Resources.", bundle);
            return bundle;
        }

        private static IEnumerable<Resource> ReadPackage(string bundle)
        {
            using var file = File.OpenRead(bundle);
            using var archive = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new BinaryReader(archive);
            var resources = new Dictionary<string, Resource>();
            while (true)
            {
                var header = reader.ReadBytes(TarBlockSize);
                if (header.Length == 0 || header[0] == 0)
                    break;
                if (header.Length != TarBlockSize)
                    throw new InvalidDataException("The TMP Essential Resources archive is truncated.");

                var name = Encoding.UTF8.GetString(header, 0, 100).TrimEnd('\0').TrimStart('.', '/');
                var size = Convert.ToInt32(Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' '), 8);
                var data = reader.ReadBytes(size);
                var padding = (TarBlockSize - size % TarBlockSize) % TarBlockSize;
                if (data.Length != size || reader.ReadBytes(padding).Length != padding)
                    throw new InvalidDataException("The TMP Essential Resources archive is truncated.");

                var parts = name.Split('/');
                if (parts.Length != 2 || parts[0].Length != 32 ||
                    parts[1] is not ("pathname" or "asset" or "asset.meta"))
                    continue;
                if (!resources.TryGetValue(parts[0], out var resource))
                {
                    resource = new Resource();
                    resources.Add(parts[0], resource);
                }

                switch (parts[1])
                {
                    case "pathname":
                        resource.Path = Encoding.UTF8.GetString(data).TrimEnd('\0', '\n', '\r');
                        break;
                    case "asset":
                        resource.Data = data;
                        break;
                    case "asset.meta":
                        resource.Meta = data;
                        break;
                }
            }
            return resources.Values;
        }
    }
}
