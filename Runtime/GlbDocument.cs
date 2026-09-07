#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akun.VrmExt
{
    /// <summary>
    /// Minimal GLB (glTF binary) reader/writer focused on JSON-chunk material extensions.
    /// Does not rewrite BIN payloads; only the JSON chunk length/padding changes.
    /// </summary>
    public sealed class GlbDocument
    {
        public const uint Magic = 0x46546C67; // "glTF"
        public const uint Version2 = 2;
        public const uint JsonChunkType = 0x4E4F534A; // "JSON"
        public const uint BinChunkType = 0x004E4942; // "BIN\0"

        public JObject Root { get; }
        public byte[]? BinChunk { get; }

        public GlbDocument(JObject root, byte[]? binChunk = null)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            BinChunk = binChunk;
        }

        public static GlbDocument Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static GlbDocument Load(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(stream);
            var magic = reader.ReadUInt32();
            if (magic != Magic)
            {
                throw new InvalidDataException("Not a GLB/VRM binary (missing glTF magic).");
            }

            var version = reader.ReadUInt32();
            if (version != Version2)
            {
                throw new InvalidDataException($"Unsupported GLB version: {version}");
            }

            var length = reader.ReadUInt32();
            if (length != bytes.Length)
            {
                // Tolerant: some writers leave length stale; still parse chunks by stream.
            }

            byte[]? jsonBytes = null;
            byte[]? binBytes = null;
            while (stream.Position + 8 <= stream.Length)
            {
                var chunkLength = reader.ReadUInt32();
                var chunkType = reader.ReadUInt32();
                var chunkData = reader.ReadBytes((int)chunkLength);
                if (chunkData.Length != chunkLength)
                {
                    throw new EndOfStreamException("Truncated GLB chunk.");
                }

                if (chunkType == JsonChunkType)
                {
                    jsonBytes = chunkData;
                }
                else if (chunkType == BinChunkType)
                {
                    binBytes = chunkData;
                }
            }

            if (jsonBytes == null)
            {
                throw new InvalidDataException("GLB JSON chunk not found.");
            }

            var jsonText = Encoding.UTF8.GetString(TrimChunkPadding(jsonBytes));
            var root = JObject.Parse(jsonText);
            return new GlbDocument(root, binBytes);
        }

        public byte[] SaveToBytes()
        {
            var jsonText = Root.ToString(Formatting.None);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonText);
            var paddedJson = PadToFourBytes(jsonBytes, (byte)' ');

            byte[]? paddedBin = null;
            if (BinChunk != null)
            {
                paddedBin = PadToFourBytes(BinChunk, 0);
            }

            var totalLength = 12u + 8u + (uint)paddedJson.Length;
            if (paddedBin != null)
            {
                totalLength += 8u + (uint)paddedBin.Length;
            }

            using var stream = new MemoryStream((int)totalLength);
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write(Version2);
            writer.Write(totalLength);

            writer.Write((uint)paddedJson.Length);
            writer.Write(JsonChunkType);
            writer.Write(paddedJson);

            if (paddedBin != null)
            {
                writer.Write((uint)paddedBin.Length);
                writer.Write(BinChunkType);
                writer.Write(paddedBin);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public void Save(string path)
        {
            var bytes = SaveToBytes();
            var tempPath = path + ".akun-tmp";
            var backupPath = path + ".akun-bak";
            try
            {
                File.WriteAllBytes(tempPath, bytes);
                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, overwrite: true);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch
            {
                // Best-effort restore if we deleted the original but failed to move.
                if (!File.Exists(path) && File.Exists(backupPath))
                {
                    File.Copy(backupPath, path, overwrite: true);
                }

                throw;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* ignore */ }
                }

                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { /* ignore */ }
                }
            }
        }

        /// <summary>
        /// Injects <see cref="AkunMaterialsOriginal.ExtensionName"/> onto matching materials.
        /// Matching is by trimmed material name (primary). Returns match statistics.
        /// </summary>
        public InjectionResult InjectOriginalMaterials(
            IReadOnlyList<OriginalMaterialExtension> snapshots,
            Func<string, string>? normalizeName = null)
        {
            normalizeName ??= MaterialNameUtil.Normalize;
            var byName = new Dictionary<string, OriginalMaterialExtension>(StringComparer.Ordinal);
            foreach (var snap in snapshots)
            {
                var key = normalizeName(snap.Name ?? string.Empty);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                // First wins; duplicates keep the earliest snapshot.
                if (!byName.ContainsKey(key))
                {
                    byName.Add(key, snap);
                }
            }

            EnsureExtensionsUsed(AkunMaterialsOriginal.ExtensionName);

            var materials = Root["materials"] as JArray;
            if (materials == null)
            {
                return new InjectionResult(0, 0, new List<string>(), new List<string>(byName.Keys));
            }

            var matched = 0;
            var unmatchedGltf = new List<string>();
            var usedKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var token in materials)
            {
                if (token is not JObject material)
                {
                    continue;
                }

                var rawName = material.Value<string>("name") ?? string.Empty;
                var key = normalizeName(rawName);
                if (string.IsNullOrEmpty(key) || !byName.TryGetValue(key, out var snap))
                {
                    unmatchedGltf.Add(rawName);
                    continue;
                }

                var extensions = material["extensions"] as JObject ?? new JObject();
                // Clone so we don't mutate the caller's objects if reused.
                var payload = AkunMaterialsOriginal.ToJObject(CloneForWrite(snap, rawName));
                extensions[AkunMaterialsOriginal.ExtensionName] = payload;
                material["extensions"] = extensions;
                usedKeys.Add(key);
                matched++;
            }

            var unmatchedSnapshots = new List<string>();
            foreach (var key in byName.Keys)
            {
                if (!usedKeys.Contains(key))
                {
                    unmatchedSnapshots.Add(key);
                }
            }

            return new InjectionResult(matched, materials.Count, unmatchedGltf, unmatchedSnapshots);
        }

        public IReadOnlyList<OriginalMaterialExtension> ReadOriginalMaterials()
        {
            var result = new List<OriginalMaterialExtension>();
            if (Root["materials"] is not JArray materials)
            {
                return result;
            }

            foreach (var token in materials)
            {
                if (token is not JObject material)
                {
                    continue;
                }

                var extensions = material["extensions"] as JObject;
                var ext = extensions?[AkunMaterialsOriginal.ExtensionName];
                var parsed = AkunMaterialsOriginal.TryParse(ext);
                if (parsed == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(parsed.Name))
                {
                    parsed.Name = material.Value<string>("name");
                }

                result.Add(parsed);
            }

            return result;
        }

        public void EnsureExtensionsUsed(string extensionName)
        {
            var used = Root["extensionsUsed"] as JArray ?? new JArray();
            var found = false;
            foreach (var item in used)
            {
                if (item.Type == JTokenType.String && item.Value<string>() == extensionName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                used.Add(extensionName);
            }

            Root["extensionsUsed"] = used;
        }

        private static OriginalMaterialExtension CloneForWrite(OriginalMaterialExtension source, string gltfName)
        {
            var json = AkunMaterialsOriginal.ToJson(source);
            var clone = AkunMaterialsOriginal.TryParse(json) ?? new OriginalMaterialExtension();
            if (string.IsNullOrEmpty(clone.Name))
            {
                clone.Name = gltfName;
            }

            clone.Version = AkunMaterialsOriginal.SchemaVersion;
            return clone;
        }

        private static byte[] TrimChunkPadding(byte[] chunk)
        {
            var end = chunk.Length;
            while (end > 0 && (chunk[end - 1] == (byte)' ' || chunk[end - 1] == 0))
            {
                end--;
            }

            if (end == chunk.Length)
            {
                return chunk;
            }

            var trimmed = new byte[end];
            Buffer.BlockCopy(chunk, 0, trimmed, 0, end);
            return trimmed;
        }

        private static byte[] PadToFourBytes(byte[] data, byte pad)
        {
            var rem = data.Length % 4;
            if (rem == 0)
            {
                return data;
            }

            var padded = new byte[data.Length + (4 - rem)];
            Buffer.BlockCopy(data, 0, padded, 0, data.Length);
            for (var i = data.Length; i < padded.Length; i++)
            {
                padded[i] = pad;
            }

            return padded;
        }
    }

    public readonly struct InjectionResult
    {
        public InjectionResult(int matched, int gltfMaterialCount, List<string> unmatchedGltfNames,
            List<string> unmatchedSnapshotNames)
        {
            Matched = matched;
            GltfMaterialCount = gltfMaterialCount;
            UnmatchedGltfNames = unmatchedGltfNames;
            UnmatchedSnapshotNames = unmatchedSnapshotNames;
        }

        public int Matched { get; }
        public int GltfMaterialCount { get; }
        public IReadOnlyList<string> UnmatchedGltfNames { get; }
        public IReadOnlyList<string> UnmatchedSnapshotNames { get; }
    }

    public static class MaterialNameUtil
    {
        private const string CloneSuffix = "(Clone)";

        public static string Normalize(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var value = name.Trim();
            while (value.EndsWith(CloneSuffix, StringComparison.Ordinal))
            {
                value = value[..^CloneSuffix.Length].TrimEnd();
            }

            return value;
        }
    }

    /// <summary>
    /// Public reader API for tooling that opens a VRM/GLB and recovers original material metadata.
    /// </summary>
    public static class OriginalMaterialReader
    {
        public static IReadOnlyList<OriginalMaterialExtension> ReadFromFile(string path)
        {
            var doc = GlbDocument.Load(path);
            return doc.ReadOriginalMaterials();
        }

        public static IReadOnlyList<OriginalMaterialExtension> ReadFromBytes(byte[] glbBytes)
        {
            var doc = GlbDocument.Load(glbBytes);
            return doc.ReadOriginalMaterials();
        }

        public static OriginalMaterialExtension? ReadFromMaterialJson(string materialJsonObject)
        {
            var material = JObject.Parse(materialJsonObject);
            var ext = material["extensions"]?[AkunMaterialsOriginal.ExtensionName];
            return AkunMaterialsOriginal.TryParse(ext);
        }
    }
}
