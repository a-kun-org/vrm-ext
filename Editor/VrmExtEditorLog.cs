#nullable enable
using UnityEngine;

namespace Akun.VrmExt.Editor
{
    internal static class VrmExtEditorLog
    {
        public const string Prefix = "[VRM Ext]";

        public static void Info(string message) => Debug.Log($"{Prefix} {message}");

        public static void Warning(string message) => Debug.LogWarning($"{Prefix} {message}");

        public static void Error(string message) => Debug.LogError($"{Prefix} {message}");
    }
}
