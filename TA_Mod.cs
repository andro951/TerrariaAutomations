using androLib;
using androLib.Localization;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations.Common.Configs;
using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.Items;
using TerrariaAutomations.TileData;
using TerrariaAutomations.Tiles;
using TerrariaAutomations.Tiles.TileEntities;

namespace TerrariaAutomations
{
    public class TA_Mod : Mod {
		public static TA_Mod Instance;
		public const string ModName = "TerrariaAutomations";
		public static TA_ClientConfig clientConfig = ModContent.GetInstance<TA_ClientConfig>();
		public static TA_ServerConfig serverConfig = ModContent.GetInstance<TA_ServerConfig>();

		private static List<Hook> hooks = new();
		public override void Load() {
			Instance = this;
			AddNonLoadedContent();

			hooks.Add(new(GlobalAutoExtractor.OnTileRightClickInfo, GlobalAutoExtractor.TileLoaderRightClickDetour));
			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			TA_LocalizationData.RegisterSDataPackage();
		}
		private void AddNonLoadedContent() {
			IEnumerable<Type> types = null;
			try {
				types = Assembly.GetExecutingAssembly().GetTypes();
			}
			catch (ReflectionTypeLoadException e) {
				types = e.Types.Where(t => t != null);
			}

			types = types.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(TA_ModItem)));

			IEnumerable<ModItem> allItems = types.Select(t => Activator.CreateInstance(t)).Where(i => i != null).OfType<ModItem>();

			IEnumerable<ModItem> autoExtractinators = allItems.OfType<AutoExtractinator>().OrderBy(e => e.Tier);

			foreach (ModItem modItem in autoExtractinators) {
				Instance.AddContent(modItem);
			}
		}
		public enum TA_ModPacketID {
			ChestIndicatorInfo,
			AutoFisherUseItem,
			AutoFisherItemSync,
			UpdatePipe,
			SyncAllPipeData,
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			if (Main.netMode == NetmodeID.Server) {
				byte packetID = reader.ReadByte();
				switch ((TA_ModPacketID)packetID) {
					case TA_ModPacketID.AutoFisherItemSync:
						AutoFisherTE.ReceiveItem(reader, whoAmI);
						break;
					case TA_ModPacketID.UpdatePipe:
						TA_TileData.RecievePipeUpdate(reader, whoAmI);
						break;
					default:
						throw new Exception($"Received packet ID: {packetID}.  Not recognized.");
				}
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				byte packetID = reader.ReadByte();
				switch ((TA_ModPacketID)packetID) {
					case TA_ModPacketID.ChestIndicatorInfo:
						ChestIndicatorInfo.Read(reader);
						break;
					case TA_ModPacketID.AutoFisherUseItem:
						AutoFisherTE.ReadAutoFisherUseItem(reader);
						break;
					case TA_ModPacketID.AutoFisherItemSync:
						AutoFisherTE.ReceiveItem(reader);
						break;
					case TA_ModPacketID.UpdatePipe:
						TA_TileData.RecievePipeUpdate(reader, whoAmI);
						break;
					case TA_ModPacketID.SyncAllPipeData:
						TA_TileData.RecieveAllPipeData(reader);
						break;
					default:
						throw new Exception($"Recieved packet ID: {packetID}.  Not recognized.");
				}
			}
		}
	}
}
