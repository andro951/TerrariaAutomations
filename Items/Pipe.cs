using androLib;
using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Items {
	public class Pipe : TA_ModItem {
		public static int PipeType {
			get {
				if (pipeType == null)
					pipeType = ModContent.ItemType<Pipe>();

				return pipeType.Value;
			}
		}
		private static int? pipeType = null;
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.Wire);
		}
		public override void Load() {
			On_Item.FitsAmmoSlot += On_Item_FitsAmmoSlot;
			On_Item.CanCombineStackInWorld += On_Item_CanCombineStackInWorld;
		}
		private bool On_Item_CanCombineStackInWorld(On_Item.orig_CanCombineStackInWorld orig, Item self) {
			if (orig(self))
				return true;

			if (self.type == PipeType)
				return true;

			return false;
		}
		private bool On_Item_FitsAmmoSlot(On_Item.orig_FitsAmmoSlot orig, Item self) {
			if (orig(self))
				return true;

			if (self.type == PipeType)
				return true;

			return false;
		}
		public override void AddRecipes() {
			CreateRecipe((int)MathF.Round((float)ItemID.IronBar.CSI().value / (float)Item.value))
				.AddRecipeGroup($"{AndroMod.ModName}:{AndroModSystem.AnyIronBar}")
				.AddTile(TileID.Anvils)
				.Register();
		}
		
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.CraftingMaterial };
		public override string Artist => "";
		public override string Designer => "andro951";
	}
}
