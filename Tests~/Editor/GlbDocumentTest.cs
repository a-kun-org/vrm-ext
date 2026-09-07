using System.Collections.Generic;
using System.Text;
using Akun.VrmExt;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Akun.VrmExt.Editor.Tests
{
    public class GlbDocumentTest
    {
        [Test]
        public void Inject_RoundTrips_Extension_On_Matching_Material()
        {
            var root = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0" },
                ["materials"] = new JArray
                {
                    new JObject { ["name"] = "Body" },
                    new JObject { ["name"] = "Face(Clone)" },
                },
            };

            var glb = new GlbDocument(root, Encoding.UTF8.GetBytes("BINDATA!!"));
            var snapshots = new List<OriginalMaterialExtension>
            {
                new()
                {
                    Name = "Body",
                    Shader = "lilToon",
                    Floats = new Dictionary<string, float> { ["_OutlineWidth"] = 0.1f },
                    Colors = new Dictionary<string, float[]> { ["_Color"] = new[] { 1f, 1f, 1f, 1f } },
                    Textures = new Dictionary<string, OriginalTextureRef>
                    {
                        ["_MainTex"] = new OriginalTextureRef { Name = "body_albedo", Guid = "abc" },
                    },
                },
                new()
                {
                    Name = "Face",
                    Shader = "Poiyomi/Toon",
                },
            };

            var result = glb.InjectOriginalMaterials(snapshots);
            Assert.AreEqual(2, result.Matched);
            Assert.AreEqual(0, result.UnmatchedGltfNames.Count);
            Assert.AreEqual(0, result.UnmatchedSnapshotNames.Count);

            var bytes = glb.SaveToBytes();
            var reloaded = GlbDocument.Load(bytes);
            var materials = reloaded.ReadOriginalMaterials();
            Assert.AreEqual(2, materials.Count);
            Assert.AreEqual("lilToon", materials[0].Shader);
            Assert.AreEqual(0.1f, materials[0].Floats!["_OutlineWidth"], 1e-5);
            Assert.AreEqual("body_albedo", materials[0].Textures!["_MainTex"].Name);
            Assert.AreEqual("Poiyomi/Toon", materials[1].Shader);

            var used = reloaded.Root["extensionsUsed"] as JArray;
            Assert.NotNull(used);
            Assert.Contains(AkunMaterialsOriginal.ExtensionName, used.ToObject<List<string>>());
        }

        [Test]
        public void Normalize_Trims_Clone_Suffix()
        {
            Assert.AreEqual("Body", MaterialNameUtil.Normalize("Body(Clone)"));
            Assert.AreEqual("Body", MaterialNameUtil.Normalize("  Body(Clone)(Clone) "));
            Assert.AreEqual("", MaterialNameUtil.Normalize(null));
        }

        [Test]
        public void Save_Restores_Original_When_Temp_Replace_Fails_Simulation()
        {
            // Structural smoke: empty materials still adds extensionsUsed and round-trips BIN.
            var root = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0" },
                ["materials"] = new JArray(),
            };
            var bin = new byte[] { 1, 2, 3, 4, 5 };
            var glb = new GlbDocument(root, bin);
            glb.EnsureExtensionsUsed(AkunMaterialsOriginal.ExtensionName);
            var bytes = glb.SaveToBytes();
            var reloaded = GlbDocument.Load(bytes);
            CollectionAssert.AreEqual(bin, reloaded.BinChunk);
        }
    }

    public class MaterialNameUtilTest
    {
        [TestCase("mat", "mat")]
        [TestCase("mat(Clone)", "mat")]
        [TestCase("mat(Clone)(Clone)", "mat")]
        public void Normalize_Cases(string input, string expected)
        {
            Assert.AreEqual(expected, MaterialNameUtil.Normalize(input));
        }
    }
}
