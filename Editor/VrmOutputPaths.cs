#nullable enable
using System.IO;
using Akun.VrmExt;
using UnityEngine;

namespace Akun.VrmExt.Editor
{
    /// <summary>
    /// Mirrors ndmf-vrm-exporter's default output path convention:
    /// Assets/NDMF VRM Exporter/{scene}/{avatar}.vrm
    /// </summary>
    internal static class VrmOutputPaths
    {
        public const string ExporterBasePath = "Assets/NDMF VRM Exporter";

        public static string GetDefaultVrmPath(GameObject avatarRoot)
        {
            return $"{GetDefaultBasePath(avatarRoot)}.vrm";
        }

        public static string GetDefaultBasePath(GameObject avatarRoot)
        {
            var sceneName = !string.IsNullOrEmpty(avatarRoot.scene.name)
                ? StripInvalidFileNameCharacters(avatarRoot.scene.name)
                : "Untitled";
            var gameObjectName = MaterialNameUtil.Normalize(StripInvalidFileNameCharacters(avatarRoot.name));
            return $"{ExporterBasePath}/{sceneName}/{gameObjectName}";
        }

        public static string GetSidecarSnapshotPath(string vrmPath)
        {
            return Path.ChangeExtension(vrmPath, ".akun-materials.json");
        }

        public static string StripInvalidFileNameCharacters(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }

        public static string TrimCloneSuffix(string name)
        {
            return MaterialNameUtil.Normalize(name);
        }
    }
}
