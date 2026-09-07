# AKUN_materials_original

Custom glTF **material** extension used by [com.akun.vrm-ext](https://github.com/a-kun-org/vrm-ext) to preserve the pre-conversion Unity material (shader name + property bag) inside a VRM 1.0 / GLB file produced by [NDMF VRM Exporter](https://github.com/hkrn/ndmf-vrm-exporter).

## Identifier

| Field | Value |
| --- | --- |
| Extension name | `AKUN_materials_original` |
| Attachment point | `materials[*].extensions` |
| Root registration | listed in top-level `extensionsUsed` |
| Schema version | `1` (`version` field) |

> Naming note: this is **not** an official VRM (`VRMC_*`) or Khronos (`KHR_*`) extension. The `AKUN_` prefix identifies the a-kun-org vendor namespace.

## Goals

- After lilToon / Poiyomi / Standard / etc. are converted to MToon or glTF PBR for VRM, tooling can still recover the **original Unity shader name** and scalar/color/vector/texture **property values**.
- Texture **pixels** stay in the VRM as the exporter’s converted textures. This extension stores **names / asset refs only**, not image blobs.

## Example

```json
{
  "materials": [
    {
      "name": "Body",
      "pbrMetallicRoughness": { },
      "extensions": {
        "AKUN_materials_original": {
          "version": 1,
          "name": "Body",
          "shader": "lilToon",
          "keywords": ["_NORMALMAP"],
          "floats": { "_OutlineWidth": 0.1 },
          "ints": { "_Cull": 2 },
          "colors": { "_Color": [1, 1, 1, 1] },
          "vectors": { "_MainTex_ST": [1, 1, 0, 0] },
          "textures": {
            "_MainTex": {
              "name": "body_albedo",
              "path": "Assets/Textures/body_albedo.png",
              "guid": "0123456789abcdef0123456789abcdef"
            }
          }
        }
      }
    }
  ],
  "extensionsUsed": [
    "AKUN_materials_original"
  ]
}
```

## Field reference

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `version` | number (int) | yes | Schema version. Current: `1`. |
| `name` | string | no | Unity material name (clone suffixes trimmed). |
| `shader` | string | recommended | Unity `Material.shader.name` (e.g. `lilToon`). |
| `keywords` | string[] | no | Enabled shader keywords at snapshot time. |
| `floats` | object&lt;string,number&gt; | no | Float / Range properties. |
| `ints` | object&lt;string,integer&gt; | no | Int properties (`ShaderPropertyType.Int`). |
| `colors` | object&lt;string,number[4]&gt; | no | RGBA arrays (Unity color space as stored on the material). |
| `vectors` | object&lt;string,number[4]&gt; | no | XYZW arrays (includes `*_ST` scale/offset when present). |
| `textures` | object&lt;string,TextureRef&gt; | no | Texture property refs (no binary). |

### TextureRef

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `name` | string | recommended | Unity texture object name. |
| `path` | string | no | Editor asset path when available. |
| `guid` | string | no | Unity asset GUID when available. |

Unknown fields should be ignored by readers. Writers should omit empty maps/arrays when convenient.

## Matching rules (writer)

When injecting into an exported VRM:

1. Primary key: material `name`, after trimming and removing trailing `(Clone)` suffixes (same idea as ndmf-vrm-exporter’s `TrimCloneSuffix`).
2. Unmatched glTF materials or unused snapshots should be logged as warnings; export must still succeed.

## Non-goals

- Rebuilding a full Unity material that renders identically outside Unity.
- Embedding texture binary data or large baked atlases in this extension.
- Replacing `VRMC_materials_mtoon` / PBR output; this is additive metadata only.
