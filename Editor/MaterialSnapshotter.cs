#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Akun.VrmExt.Editor
{
    internal static class MaterialSnapshotter
    {
        public static MaterialSnapshotDocument Capture(GameObject avatarRoot)
        {
            var document = new MaterialSnapshotDocument
            {
                AvatarName = MaterialNameUtil.Normalize(avatarRoot.name),
                CapturedAtUtc = System.DateTime.UtcNow.ToString("o"),
                Materials = new List<OriginalMaterialExtension>(),
            };

            var seen = new HashSet<int>();
            var renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                if (materials == null)
                {
                    continue;
                }

                foreach (var material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    var id = material.GetInstanceID();
                    if (!seen.Add(id))
                    {
                        continue;
                    }

                    document.Materials.Add(CaptureMaterial(material));
                }
            }

            return document;
        }

        public static OriginalMaterialExtension CaptureMaterial(Material material)
        {
            var extension = new OriginalMaterialExtension
            {
                Name = MaterialNameUtil.Normalize(material.name),
                Shader = material.shader != null ? material.shader.name : null,
                Keywords = new List<string>(),
                Floats = new Dictionary<string, float>(),
                Ints = new Dictionary<string, int>(),
                Colors = new Dictionary<string, float[]>(),
                Vectors = new Dictionary<string, float[]>(),
                Textures = new Dictionary<string, OriginalTextureRef>(),
            };

            if (material.shaderKeywords != null)
            {
                foreach (var keyword in material.shaderKeywords)
                {
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        extension.Keywords.Add(keyword);
                    }
                }
            }

            var shader = material.shader;
            if (shader == null)
            {
                return extension;
            }

            var count = shader.GetPropertyCount();
            for (var i = 0; i < count; i++)
            {
                var propName = shader.GetPropertyName(i);
                if (string.IsNullOrEmpty(propName) || !material.HasProperty(propName))
                {
                    continue;
                }

                switch (shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        extension.Floats![propName] = material.GetFloat(propName);
                        break;
                    case ShaderPropertyType.Int:
                        extension.Ints![propName] = material.GetInteger(propName);
                        break;
                    case ShaderPropertyType.Color:
                    {
                        var c = material.GetColor(propName);
                        extension.Colors![propName] = new[] { c.r, c.g, c.b, c.a };
                        break;
                    }
                    case ShaderPropertyType.Vector:
                    {
                        var v = material.GetVector(propName);
                        extension.Vectors![propName] = new[] { v.x, v.y, v.z, v.w };
                        break;
                    }
                    case ShaderPropertyType.Texture:
                    {
                        var tex = material.GetTexture(propName);
                        if (tex == null)
                        {
                            break;
                        }

                        var path = AssetDatabase.GetAssetPath(tex);
                        string? guid = null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            guid = AssetDatabase.AssetPathToGUID(path);
                        }

                        extension.Textures![propName] = new OriginalTextureRef
                        {
                            Name = tex.name,
                            Path = string.IsNullOrEmpty(path) ? null : path,
                            Guid = string.IsNullOrEmpty(guid) ? null : guid,
                        };

                        // Also capture common scale/offset as a vector when present.
                        var stName = propName + "_ST";
                        if (material.HasProperty(stName))
                        {
                            var st = material.GetVector(stName);
                            extension.Vectors![stName] = new[] { st.x, st.y, st.z, st.w };
                        }

                        break;
                    }
                }
            }

            return extension;
        }
    }
}
