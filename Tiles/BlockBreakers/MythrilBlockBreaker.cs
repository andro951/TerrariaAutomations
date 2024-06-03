using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace TerrariaAutomations.Tiles {
	public class MythrilBlockBreaker : BlockBreaker {
		public override int pickaxePower => PickaxePowerID.Mythril;
		public override int miningCooldown => 30;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.MythrilBlockBreaker>()) };
		}
	}
}
