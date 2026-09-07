#nullable enable
using System;
using System.Collections.Generic;

namespace Akun.VrmExt.Editor
{
    /// <summary>
    /// BuildContext state carrying the pre-export material snapshot.
    /// </summary>
    internal sealed class MaterialSnapshotState
    {
        public MaterialSnapshotDocument Document { get; set; } = new();
        public string? ExpectedVrmPath { get; set; }
        public string? SidecarSnapshotPath { get; set; }
        public bool Enabled { get; set; }
    }
}
