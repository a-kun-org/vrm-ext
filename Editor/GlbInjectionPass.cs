#nullable enable
#if VRMEXT_HAS_NDMF
using System;
using System.IO;
using Akun.VrmExt;
using nadena.dev.ndmf;

namespace Akun.VrmExt.Editor
{
    internal static class GlbInjectionPass
    {
        public static void Execute(BuildContext context)
        {
            var state = context.GetState<MaterialSnapshotState>();
            if (!state.Enabled || state.Document.Materials.Count == 0)
            {
                return;
            }

            var vrmPath = state.ExpectedVrmPath;
            if (string.IsNullOrEmpty(vrmPath) || !File.Exists(vrmPath))
            {
                VrmExtEditorLog.Warning(
                    "VRM output not found at the default ndmf-vrm-exporter path. " +
                    "If you exported via NDMF platform Build UI to a custom path, use " +
                    "Tools/VRM Ext/Inject original material metadata into VRM… with the sidecar JSON. " +
                    $"Expected: {vrmPath}");
                return;
            }

            try
            {
                var result = InjectIntoVrm(vrmPath, state.Document);
                VrmExtEditorLog.Info(
                    $"Injected {AkunMaterialsOriginal.ExtensionName} into {result.Matched}/{result.GltfMaterialCount} materials: {vrmPath}");

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
                // Never break the export: leave the original VRM intact (GlbDocument.Save restores on failure).
                VrmExtEditorLog.Error($"Material metadata injection failed; VRM left unchanged if possible. {e}");
            }
        }

        internal static InjectionResult InjectIntoVrm(string vrmPath, MaterialSnapshotDocument document)
        {
            var glb = GlbDocument.Load(vrmPath);
            var result = glb.InjectOriginalMaterials(document.Materials);
            glb.Save(vrmPath);
            return result;
        }
    }
}
#endif
