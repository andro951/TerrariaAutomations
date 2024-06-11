using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.Items;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using androLib;
using TerrariaAutomations.Tiles;
using TerrariaAutomations.TileData.Pipes;
using Terraria.GameContent.Creative;
using androLib.IO.TerrariaAutomations;
using TerrariaAutomations.TileData;
using Microsoft.Xna.Framework;

namespace TerrariaAutomations
{
    public class TA_ModSystem : ModSystem {
		public override void AddRecipes() {
			for (int i = 0; i < AndroMod.VanillaRecipeCount; i++) {
				Recipe recipe = Main.recipe[i];
				if (recipe.createItem.type == ItemID.ChlorophyteExtractinator) {
					recipe.DisableRecipe();
				}
			}

			Recipe.Create(ItemID.Extractinator).AddTile(TileID.Anvils).AddIngredient(ModContent.ItemType<WoodAutoExtractinator>()).AddRecipeGroup($"{AndroMod.ModName}:{AndroModSystem.AnyIronBar}", 20).Register();
			Recipe.Create(ItemID.ChlorophyteExtractinator).AddTile(TileID.MythrilAnvil).AddIngredient(ModContent.ItemType<HellstoneAutoExtractinator>()).AddIngredient(ItemID.ChlorophyteBar, 20).Register();
		}
		public override void PostSetupContent() {
			GlobalAutoExtractor.PostSetupContent();
		}
		public override void PostUpdateEverything() {
			AutoFisherTE.UpdateAll();
			GlobalChest.Update();
		}
		public override void PreSaveAndQuit() {
			StorageNetwork.PreSaveAndQuit();
		}
		public override void Load() {
			AndroMod.OnSendWorldDataToConnectingPlayer += OnSendWorldDataToConnectingPlayer;
		}
		public static void OnSendWorldDataToConnectingPlayer(int clientWhoAmI) {
			if (Main.netMode != NetmodeID.Server)
				return;

			TA_TileData.SendAllPipeData(clientWhoAmI);
			AutoFisherTE.RequestAllTEsToClient(clientWhoAmI);
		}
	}
}
