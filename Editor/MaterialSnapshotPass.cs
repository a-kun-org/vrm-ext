#nullable enable
#if VRMEXT_HAS_NDMF
using System.IO;
using Akun.VrmExt;
using nadena.dev.ndmf;
using UnityEngine;

namespace Akun.VrmExt.Editor
{
    internal static class MaterialSnapshotPass
    {
        public static void Execute(BuildContext context)
        {
            var root = context.AvatarRootObject;
            if (root == null)
            {
                return;
            }

            var state = context.GetState<MaterialSnapshotState>();
            if (!root.TryGetComponent<VrmExtOriginalMaterials>(out var marker) || !marker.enabled)
            {
                state.Enabled = false;
                return;
            }

            // Require the exporter component so we don't snapshot unrelated builds.
            var exporterType = System.Type.GetType(
                "com.github.hkrn.NdmfVrmExporterComponent, NDMFVRMExporter");
            if (exporterType == null || root.GetComponent(exporterType) == null)
            {
                VrmExtEditorLog.Warning(
                    "VrmExtOriginalMaterials is present but NdmfVrmExporterComponent was not found. Skipping.");
                state.Enabled = false;
                return;
            }

            state.Enabled = true;
            state.Document = MaterialSnapshotter.Capture(root);
            state.ExpectedVrmPath = VrmOutputPaths.GetDefaultVrmPath(root);
            state.SidecarSnapshotPath = VrmOutputPaths.GetSidecarSnapshotPath(state.ExpectedVrmPath);

            if (marker.WriteSidecarSnapshot)
            {
                try
                {
                    var dir = Path.GetDirectoryName(state.SidecarSnapshotPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(state.SidecarSnapshotPath, state.Document.ToJson());
                    VrmExtEditorLog.Info(
                        $"Wrote material snapshot sidecar ({state.Document.Materials.Count} materials): {state.SidecarSnapshotPath}");
                }
                catch (System.Exception e)
                {
                    VrmExtEditorLog.Warning($"Failed to write sidecar snapshot JSON: {e.Message}");
                }
            }

            VrmExtEditorLog.Info(
                $"Captured {state.Document.Materials.Count} original materials before VRM export.");
        }
    }
}
#endif
