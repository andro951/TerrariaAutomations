using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.ObjectData;
using Terraria.Enums;
using androLib;
using TerrariaAutomations.Tiles;
using System.Collections.Generic;
using androLib.Common.Utility;
using System;

namespace TerrariaAutomations.Items {
	[Autoload(false)]
	public class WoodAutoExtractinator : AutoExtractinator {
		public override int CreateTile => ModContent.TileType<WoodAutoExtractinatorTile>();
		public override int Rarity => ItemRarityID.Green;
		public override int Tier => 0;
		public override int RecipeRequiredTile => TileID.WorkBenches;
		public override List<(int, int)> Ingredients => new() {
			(ItemID.Wood, 100)
		};
	}
}
