using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations.Items;

namespace TerrariaAutomations.Common.Globals {
	internal class TA_ShopGlobalNPC : GlobalNPC {
		public override void ModifyShop(NPCShop shop) {
			if (shop.NpcType == NPCID.Mechanic) {
				shop.Add<PipeWrench>();
				shop.Add<Pipe>();
			}
		}
	}
}
