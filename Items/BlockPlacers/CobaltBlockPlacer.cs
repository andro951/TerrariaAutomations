using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Items {
	public class CobaltBlockPlacer : BlockPlacer {
		protected override int PrimaryMaterial => ItemID.CobaltBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int cooldown => new Tiles.CobaltBlockPlacer().cooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.CobaltBlockPlacer>;
	}
}
