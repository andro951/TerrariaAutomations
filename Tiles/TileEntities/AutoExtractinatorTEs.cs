using Terraria.ModLoader;
using TerrariaAutomations.Tiles;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace TerrariaAutomations.Tiles.TileEntities
{
    public class WoodAutoExtractinatorTE : AutoExtractinatorTE {
		public override int Timer => 600;
        protected override int ConsumeMultiplier => 1;
        protected override int TileToBeValidOn => ModContent.TileType<WoodAutoExtractinatorTile>();
    }

    public class VanillaAutoExtractinatorTE : AutoExtractinatorTE {
		public override int Timer => 60;
        protected override int ConsumeMultiplier => 1;
        protected override int TileToBeValidOn => TileID.Extractinator;
    }

    public class HellstoneAutoExtractinatorTE : AutoExtractinatorTE {
		public override int Timer => 60;
        protected override int ConsumeMultiplier => 2;
        protected override int TileToBeValidOn => ModContent.TileType<HellstoneAutoExtractinatorTile>();
    }

    public class ChlorophyteAutoExtractinatorTE : AutoExtractinatorTE {
		public override int Timer => 60;
        protected override int ConsumeMultiplier => 4;
        protected override int TileToBeValidOn => TileID.ChlorophyteExtractinator;
	}

    public class LuminiteAutoExtractinatorTE : AutoExtractinatorTE {
        public override int Timer => 60;
        protected override int ConsumeMultiplier => 10;
        protected override int TileToBeValidOn => ModContent.TileType<LuminiteAutoExtractinatorTile>();
	}
}
