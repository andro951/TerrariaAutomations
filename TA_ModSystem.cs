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
			TA_VanillaGlobalItem.OnAddRecipes();
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
