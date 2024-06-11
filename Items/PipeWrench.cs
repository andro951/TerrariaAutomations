using androLib;
using androLib.Common.Utility;
using androLib.Items.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations.TileData;

namespace TerrariaAutomations.Items {
	public class PipeWrench : TA_ModItem, IRightClickUse {
		public static int PipeWrenchType {
			get {
				if (pipeWrenchType == null)
					pipeWrenchType = ModContent.ItemType<PipeWrench>();

				return pipeWrenchType.Value;
			}
		}
		private static int? pipeWrenchType = null;
		public override void Load() {
			AndroMod.OnCountAmmoForDrawItemSlot += CountPipesForDrawAmmoCount;
		}
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.Wrench);
		}
		public Func<int> GetItemType => () => PipeWrenchType;
		public bool InInteractionRange(Player player, Item item) => IRightClickUse.InWireRenchUseRange(player, item);
		public bool CursorItemIConWhileHoldingItem => true;
		public void PreRightClickUse(Player player, Item item, Action PostUseActions) {}
		public bool? OnUse(Player player, Item item, int tileTargetX, int tileTargetY) {
			if (Main.netMode != NetmodeID.Server) {
				if (player.itemAnimation <= 0 || !player.ItemTimeIsZero || !player.controlUseItem)
					return null;

				if (AndroUtilityMethods.SearchForFirstItem(Pipe.PipeType, out Item pipeItem, TA_TileData.PipeStorages.Select(a => a()), banks: false)) {
					if (!TA_TileData.PlacePipe(tileTargetX, tileTargetY, pipeItem.type))
						return null;

					pipeItem.stack--;
					if (pipeItem.stack <= 0)
						pipeItem.TurnToAir();

					if (Main.netMode == NetmodeID.MultiplayerClient)
						TA_TileData.NetUpdatePipe(tileTargetX, tileTargetY, TA_TileData.PipeUpdatesTypeID.Place);

					return true;
				}
			}

			return null;
		}
		public bool? OnRightClickUse(Player player, Item item, int tileTargetX, int tileTargetY) {
			if (!TA_TileData.RemovePipe(tileTargetX, tileTargetY))
				return null;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				TA_TileData.NetUpdatePipe(tileTargetX, tileTargetY, TA_TileData.PipeUpdatesTypeID.Remove);

			return true;
		}
		private static int CountPipesForDrawAmmoCount(Item item, int count) {
			if (item.type == PipeWrenchType) {
				if (count == -1)
					count = 0;

				AndroUtilityMethods.SearchForItem(Pipe.PipeType, out int itemCount, TA_TileData.PipeStorages.Select(a => a()), banks: false);
				count += itemCount;
			}

			return count;
		}

		public bool PreventTileInteraction(Player player, Item item, int tileTargetX, int tileTargetY) {
			Tile tile = Main.tile[tileTargetX, tileTargetY];
			if (tile.HasPipe())
				return true;

			return false;
		}

		public override string LocalizationTooltip =>
			"Places and removes pipes." +
			"\nRight click to remove";
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.Tool };
		public override string Artist => "";
		public override string Designer => "andro951";
	}
}
