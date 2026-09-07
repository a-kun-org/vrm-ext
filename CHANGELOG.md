# Changelog

## 0.1.0

### Added

- Initial `com.akun.vrm-ext` package.
- Opt-in `VrmExtOriginalMaterials` component.
- NDMF Optimizing passes: snapshot materials before `com.github.hkrn.ndmf-vrm-exporter`, then inject `AKUN_materials_original` into the default VRM output.
- Sidecar `.akun-materials.json` for menu-based fallback injection.
- Editor menus under **Tools → VRM Ext**.
- Runtime/editor reader API: `OriginalMaterialReader`, `GlbDocument`, `AkunMaterialsOriginal`.
- Extension schema documentation in `EXTENSIONS.md`.
