using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Items {
	public class SilverBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.SilverBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.SilverBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.SilverBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.SilverBlockBreaker>;
	}
}
