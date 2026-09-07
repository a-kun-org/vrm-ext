using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.akun.vrm-ext.Editor.Tests")]

#if VRMEXT_HAS_NDMF
[assembly: nadena.dev.ndmf.ExportsPlugin(typeof(Akun.VrmExt.Editor.VrmExtPlugin))]
#endif
