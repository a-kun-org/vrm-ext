# VRM Ext (`com.akun.vrm-ext`)

NDMF 向けの補助パッケージです。[NDMF VRM Exporter](https://github.com/hkrn/ndmf-vrm-exporter) で VRChat アバターを VRM 1.0 に書き出すとき、**変換前の Unity マテリアル（シェーダー名とプロパティ袋）** を glTF のカスタム拡張 `AKUN_materials_original` として VRM/GLB に残します。

> これは ndmf-vrm-exporter のフォークではありません。エクスポーターの内部 API には依存せず、NDMF の前後パス＋ GLB 後処理で連携します。

## 特徴

- **オプトイン**: アバタールートに `VrmExtOriginalMaterials` を付けると有効
- **タイミング**: Optimizing フェーズで ndmf-vrm-exporter の直前にスナップショット → 書き出し直後に注入
- **安全**: 注入失敗時も可能な限り元の VRM を壊さない（一時ファイル経由の置換）
- **フォールバック**: `Tools/VRM Ext/Inject original material metadata into VRM…`（VRM + サイドカー JSON）

## 必要環境

| パッケージ | バージョン |
| --- | --- |
| Unity | 2022.3 系 |
| [NDMF](https://github.com/bdunderscore/ndmf) | >= 1.6 |
| [NDMF VRM Exporter](https://github.com/hkrn/ndmf-vrm-exporter) | >= 1.4（必須・手動インストール） |
| `com.unity.nuget.newtonsoft-json` | 3.2.1（package.json 依存） |

VPM の `vpmDependencies` には NDMF のみを入れています。`ndmf-vrm-exporter` は別リポジトリのため、**先に exporter を入れたうえで本パッケージを入れてください**。

## インストール

### Git URL（UPM）

`Window → Package Manager → + → Add package from git URL…`

```text
https://github.com/a-kun-org/vrm-ext.git
```

パッケージはリポジトリルートにあります（`package.json` がルート）。

### VPM

VPM リポジトリを公開している場合はそこから。当面は Git URL を推奨します。

## 使い方

1. アバターに **NDMF VRM Exporter** の `VRM Export Description` を付ける（従来どおり）
2. 同じアバタールートに **VRM Ext / Original Materials**（`VrmExtOriginalMaterials`）を追加
3. いつも通り VRM をエクスポート（NDMF Console / ビルド）
4. 出力 VRM（既定: `Assets/NDMF VRM Exporter/<Scene>/<Avatar>.vrm`）の各 `materials[*].extensions.AKUN_materials_original` を確認

### メタデータの確認

- Unity: `Tools → VRM Ext → Read original material metadata from VRM…`
- または GLB を展開し、JSON 内の `AKUN_materials_original` を見る
- スキーマは [EXTENSIONS.md](./EXTENSIONS.md)

### カスタム出力パス（Build UI）の場合

NDMF プラットフォーム Build UI で任意パスに書き出すと、注入パスが既定パスとずれることがあります。その場合:

1. エクスポート時に書かれた `.akun-materials.json`（または `Tools → VRM Ext → Save material snapshot from selection…`）を用意
2. `Tools → VRM Ext → Inject original material metadata into VRM…` で VRM と JSON を指定

## 仕組み（概要）

```text
Transforming … (MA / その他)
        ↓
Optimizing
  AAO / MA / TTT の後
  → [VRM Ext] マテリアルスナップショット（シェーダー名・keywords・float/int/color/vector/texture 参照）
  → [ndmf-vrm-exporter] VRM 書き出し（MToon/PBR 変換）
  → [VRM Ext] GLB JSON に AKUN_materials_original を注入 + extensionsUsed 更新
```

マテリアル対応付けの主キーは **名前**（末尾 `(Clone)` 除去後）。不一致は警告ログのみで、エクスポート自体は継続します。

## 制限

- テクスチャのピクセルはこの拡張に含めません（名前 / パス / GUID のみ）
- スナップショットは「エクスポート直前のアバター上マテリアル」です。それより前に破棄・置換されたプロパティは復元できません
- カスタム出力パスへの自動注入は未対応（メニューフォールバックを使用）
- 本パッケージは ndmf-vrm-exporter の内部実装をパッチしません

## 手動検証チェックリスト

1. lilToon（または任意シェーダー）アバターに exporter + `VrmExtOriginalMaterials` を付与
2. VRM を出力し、ファイルサイズが極端に増えていないこと（テクスチャ二重埋め込みがないこと）を確認
3. `Read original material metadata…` で元シェーダー名が取れること
4. コンポーネントを外すと拡張が付かないこと
5. 注入失敗を模擬しても（壊れた JSON など）元 VRM が残ること

## API（読取）

```csharp
using Akun.VrmExt;

var materials = OriginalMaterialReader.ReadFromFile("path/to/avatar.vrm");
foreach (var m in materials)
{
    Debug.Log($"{m.Name} => {m.Shader}");
}
```

## License

MIT © a-kun-org

---

## English

Companion NDMF package for [ndmf-vrm-exporter](https://github.com/hkrn/ndmf-vrm-exporter). Opt in with `VrmExtOriginalMaterials` on the avatar root. Before export it snapshots Unity materials; after the exporter writes the VRM it injects `AKUN_materials_original` (shader name + property bag, texture refs only). See [EXTENSIONS.md](./EXTENSIONS.md). Install via `https://github.com/a-kun-org/vrm-ext.git`. Requires NDMF ≥ 1.6 and ndmf-vrm-exporter ≥ 1.4 (install exporter first).
