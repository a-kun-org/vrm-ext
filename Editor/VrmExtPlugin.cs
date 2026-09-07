#nullable enable
#if VRMEXT_HAS_NDMF
using nadena.dev.ndmf;

namespace Akun.VrmExt.Editor
{
    /// <summary>
    /// NDMF plugin: snapshot materials immediately before ndmf-vrm-exporter, then inject
    /// AKUN_materials_original into the written VRM/GLB.
    /// </summary>
    internal sealed class VrmExtPlugin : Plugin<VrmExtPlugin>
    {
        public const string PluginQualifiedName = "com.akun.vrm-ext";
        public const string NdmfVrmExporterQualifiedName = "com.github.hkrn.ndmf-vrm-exporter";

        public override string QualifiedName => PluginQualifiedName;
        public override string DisplayName => "VRM Ext";

        protected override void Configure()
        {
            // Match exporter platform filter so we run for VRChat and the VRM exporter platform.
            var platforms = new[]
            {
                WellKnownPlatforms.VRChatAvatar30,
                NdmfVrmExporterQualifiedName,
            };

            // Snapshot as late as possible before export so Modular Avatar / AAO / TTT
            // have already resolved materials, while shaders are still pre-MToon conversion.
            // ndmf-vrm-exporter converts materials only inside its Optimizing Export pass.
            InPhase(BuildPhase.Optimizing).OnPlatforms(platforms, seq =>
            {
                seq.AfterPlugin("com.anatawa12.avatar-optimizer")
                    .AfterPlugin("nadena.dev.modular-avatar")
                    .AfterPlugin("net.rs64.tex-trans-tool")
                    .BeforePlugin(NdmfVrmExporterQualifiedName)
                    .Run("VRM Ext: Snapshot original materials", MaterialSnapshotPass.Execute);
            });

            InPhase(BuildPhase.Optimizing).OnPlatforms(platforms, seq =>
            {
                seq.AfterPlugin(NdmfVrmExporterQualifiedName)
                    .Run("VRM Ext: Inject original material metadata", GlbInjectionPass.Execute);
            });
        }
    }
}
#endif
