#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akun.VrmExt
{
    /// <summary>
    /// Schema helpers for the <c>AKUN_materials_original</c> glTF material extension.
    /// </summary>
    public static class AkunMaterialsOriginal
    {
        public const string ExtensionName = "AKUN_materials_original";
        public const int SchemaVersion = 1;

        public static OriginalMaterialExtension? TryParse(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return token.ToObject<OriginalMaterialExtension>(Serializer);
        }

        public static OriginalMaterialExtension? TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<OriginalMaterialExtension>(json, SerializerSettings);
        }

        public static JObject ToJObject(OriginalMaterialExtension extension)
        {
            return JObject.FromObject(extension, Serializer);
        }

        public static string ToJson(OriginalMaterialExtension extension, bool indented = false)
        {
            return JsonConvert.SerializeObject(extension, indented ? Formatting.Indented : Formatting.None,
                SerializerSettings);
        }

        public static JsonSerializerSettings SerializerSettings { get; } = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        public static JsonSerializer Serializer => JsonSerializer.Create(SerializerSettings);
    }

    /// <summary>
    /// Snapshot of a Unity material before VRM/MToon/PBR conversion.
    /// Texture pixel data is intentionally omitted; only names/refs are stored.
    /// </summary>
    [Serializable]
    public sealed class OriginalMaterialExtension
    {
        [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Include)]
        public int Version { get; set; } = AkunMaterialsOriginal.SchemaVersion;

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("shader")]
        public string? Shader { get; set; }

        [JsonProperty("keywords")]
        public List<string>? Keywords { get; set; }

        [JsonProperty("floats")]
        public Dictionary<string, float>? Floats { get; set; }

        [JsonProperty("ints")]
        public Dictionary<string, int>? Ints { get; set; }

        [JsonProperty("colors")]
        public Dictionary<string, float[]>? Colors { get; set; }

        [JsonProperty("vectors")]
        public Dictionary<string, float[]>? Vectors { get; set; }

        [JsonProperty("textures")]
        public Dictionary<string, OriginalTextureRef>? Textures { get; set; }
    }

    [Serializable]
    public sealed class OriginalTextureRef
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("path")]
        public string? Path { get; set; }

        [JsonProperty("guid")]
        public string? Guid { get; set; }
    }

    /// <summary>
    /// File-level snapshot used by NDMF and the Editor menu fallback.
    /// </summary>
    [Serializable]
    public sealed class MaterialSnapshotDocument
    {
        [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Include)]
        public int Version { get; set; } = AkunMaterialsOriginal.SchemaVersion;

        [JsonProperty("avatarName")]
        public string? AvatarName { get; set; }

        [JsonProperty("capturedAtUtc")]
        public string? CapturedAtUtc { get; set; }

        [JsonProperty("materials")]
        public List<OriginalMaterialExtension> Materials { get; set; } = new();

        public static MaterialSnapshotDocument FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MaterialSnapshotDocument>(json,
                       AkunMaterialsOriginal.SerializerSettings)
                   ?? new MaterialSnapshotDocument();
        }

        public string ToJson(bool indented = true)
        {
            return JsonConvert.SerializeObject(this,
                indented ? Formatting.Indented : Formatting.None,
                AkunMaterialsOriginal.SerializerSettings);
        }
    }
}
