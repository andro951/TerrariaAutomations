using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace TerrariaAutomations.Tiles {
	public class AdamantiteBlockBreaker : BlockBreaker {
		public override int pickaxePower => PickaxePowerID.Adamantite;
		public override int miningCooldown => 20;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.AdamantiteBlockBreaker>()) };
		}
	}
}
