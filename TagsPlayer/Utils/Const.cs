using System.Collections.Generic;

namespace TagsPlayer.Utils
{
    internal class Const
    {
        public static readonly List<MahApps.Metro.IconPacks.PackIconRemixIconKind> loopType = new() {
            MahApps.Metro.IconPacks.PackIconRemixIconKind.RepeatOneFill,
            MahApps.Metro.IconPacks.PackIconRemixIconKind.ShuffleFill,
            MahApps.Metro.IconPacks.PackIconRemixIconKind.RestartLine
        };

        public static MahApps.Metro.IconPacks.PackIconRemixIconKind GetNextLoopMode(MahApps.Metro.IconPacks.PackIconRemixIconKind kind) {
            var i = loopType.IndexOf(kind);
            if (i == -1) {
                return loopType[0];
            } else {
                return loopType[(i+1) % loopType.Count];
            }
        }
    }
}
