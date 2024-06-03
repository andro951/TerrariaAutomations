using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Items {
	public class CrimtaneBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.CrimtaneBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.CrimtaneBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.CrimtaneBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.CrimtaneBlockBreaker>;
	}
}
