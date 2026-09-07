#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Akun.VrmExt.Editor
{
    internal static class VrmExtMenuItems
    {
        private const string MenuRoot = "Tools/VRM Ext/";

        [MenuItem(MenuRoot + "Inject original material metadata into VRM…", false, 100)]
        private static void InjectMenu()
        {
            var vrmPath = EditorUtility.OpenFilePanel("Select VRM/GLB", "Assets", "vrm,glb");
            if (string.IsNullOrEmpty(vrmPath))
            {
                return;
            }

            var jsonPath = EditorUtility.OpenFilePanel(
                "Select material snapshot JSON (.akun-materials.json)",
                Path.GetDirectoryName(vrmPath) ?? "Assets",
                "json");
            if (string.IsNullOrEmpty(jsonPath))
            {
                return;
            }

            try
            {
                var document = MaterialSnapshotDocument.FromJson(File.ReadAllText(jsonPath));
                if (document.Materials == null || document.Materials.Count == 0)
                {
                    EditorUtility.DisplayDialog("VRM Ext", "Snapshot JSON contains no materials.", "OK");
                    return;
                }

                var glb = GlbDocument.Load(vrmPath);
                var result = glb.InjectOriginalMaterials(document.Materials);
                glb.Save(vrmPath);

                EditorUtility.DisplayDialog(
                    "VRM Ext",
                    $"Injected {AkunMaterialsOriginal.ExtensionName} into {result.Matched}/{result.GltfMaterialCount} materials.\n\n{vrmPath}",
                    "OK");

                foreach (var name in result.UnmatchedGltfNames)
                {
                    VrmExtEditorLog.Warning($"No snapshot for glTF material '{name}'.");
                }

                foreach (var name in result.UnmatchedSnapshotNames)
                {
                    VrmExtEditorLog.Warning($"Snapshot material '{name}' was not found in the VRM.");
                }
            }
            catch (Exception e)
            {
                VrmExtEditorLog.Error(e.ToString());
                EditorUtility.DisplayDialog("VRM Ext", $"Injection failed:\n{e.Message}", "OK");
            }
        }

        [MenuItem(MenuRoot + "Read original material metadata from VRM…", false, 101)]
        private static void ReadMenu()
        {
            var vrmPath = EditorUtility.OpenFilePanel("Select VRM/GLB", "Assets", "vrm,glb");
            if (string.IsNullOrEmpty(vrmPath))
            {
                return;
            }

            try
            {
                var materials = OriginalMaterialReader.ReadFromFile(vrmPath);
                if (materials.Count == 0)
                {
                    EditorUtility.DisplayDialog(
                        "VRM Ext",
                        $"No {AkunMaterialsOriginal.ExtensionName} extensions found.",
                        "OK");
                    return;
                }

                var lines = new System.Text.StringBuilder();
                lines.AppendLine($"Found {materials.Count} material(s):");
                foreach (var m in materials)
                {
                    lines.AppendLine($"- {m.Name}  shader={m.Shader}");
                }

                VrmExtEditorLog.Info(lines.ToString());
                EditorUtility.DisplayDialog("VRM Ext", lines.ToString(), "OK");
            }
            catch (Exception e)
            {
                VrmExtEditorLog.Error(e.ToString());
                EditorUtility.DisplayDialog("VRM Ext", $"Read failed:\n{e.Message}", "OK");
            }
        }

        [MenuItem(MenuRoot + "Save material snapshot from selection…", false, 110)]
        private static void SnapshotSelectionMenu()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("VRM Ext", "Select an avatar root GameObject first.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Save material snapshot JSON",
                "Assets",
                $"{MaterialNameUtil.Normalize(go.name)}.akun-materials.json",
                "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                var doc = MaterialSnapshotter.Capture(go);
                File.WriteAllText(path, doc.ToJson());
                EditorUtility.DisplayDialog(
                    "VRM Ext",
                    $"Saved {doc.Materials.Count} materials to:\n{path}",
                    "OK");
            }
            catch (Exception e)
            {
                VrmExtEditorLog.Error(e.ToString());
                EditorUtility.DisplayDialog("VRM Ext", $"Snapshot failed:\n{e.Message}", "OK");
            }
        }
    }
}
