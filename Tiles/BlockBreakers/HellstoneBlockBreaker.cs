using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace TerrariaAutomations.Tiles {
	public class HellstoneBlockBreaker : BlockBreaker {
		public override int pickaxePower => PickaxePowerID.Molten;
		public override int miningCooldown => 60;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.HellstoneBlockBreaker>()) };
		}
	}
}
