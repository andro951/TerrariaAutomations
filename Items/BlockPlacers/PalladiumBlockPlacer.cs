using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Items {
	public class PalladiumBlockPlacer : BlockPlacer {
		protected override int PrimaryMaterial => ItemID.PalladiumBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int cooldown => new Tiles.PalladiumBlockPlacer().cooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.PalladiumBlockPlacer>;
	}
}
