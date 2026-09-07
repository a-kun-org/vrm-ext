#nullable enable
using UnityEngine;

#if VRMEXT_HAS_NDMF
using nadena.dev.ndmf;
#endif

namespace Akun.VrmExt
{
    /// <summary>
    /// Opt-in marker. Attach to the avatar root to enable original-material metadata
    /// capture/injection during NDMF VRM export.
    /// </summary>
    [AddComponentMenu("VRM Ext/Original Materials")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/a-kun-org/vrm-ext")]
#if VRMEXT_HAS_NDMF
    public sealed class VrmExtOriginalMaterials : MonoBehaviour, INDMFEditorOnly
#else
    public sealed class VrmExtOriginalMaterials : MonoBehaviour
#endif
    {
        [Tooltip("When enabled, writes a sidecar JSON snapshot next to the VRM for the Editor menu fallback.")]
        [SerializeField] private bool writeSidecarSnapshot = true;

        public bool WriteSidecarSnapshot => writeSidecarSnapshot;

        // Present so the Inspector shows an enable checkbox.
        private void Start()
        {
        }
    }
}
