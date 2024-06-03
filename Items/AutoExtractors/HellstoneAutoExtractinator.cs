using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using TerrariaAutomations.Tiles;
using System.Collections.Generic;

namespace TerrariaAutomations.Items {
	[Autoload(false)]
	public class HellstoneAutoExtractinator : AutoExtractinator {
		public override int CreateTile => ModContent.TileType<HellstoneAutoExtractinatorTile>();
		public override int Rarity => ItemRarityID.LightRed;
		public override int Tier => 2;
		public override int RecipeRequiredTile => TileID.Anvils;
		public override List<(int, int)> Ingredients => new() {
			(ItemID.HellstoneBar, 20),
			(ItemID.Extractinator, 1)
		};
	}
}
