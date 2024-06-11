using androLib.Common.Utility;
using TerrariaAutomations.Tiles.TileEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.HitTile;
using TerrariaAutomations.Common.Globals;
using androLib.Common.Globals;
using TerrariaAutomations.Items;
using Terraria.UI;
using androLib.Tiles;
using TerrariaAutomations.TileData.Pipes;
using System.Collections;

namespace TerrariaAutomations.Tiles {
	public abstract class BlockBreaker : AndroModTile {
		protected class PickaxePowerID {
			public const int Wood = 1;

			public const int Copper = 35;
			public const int Iron = 40;
			public const int Lead = 43;
			public const int Silver = 45;
			public const int Tungsten = 50;
			public const int Gold = 55;
			public const int Platinum = 59;
			public const int Nightmare = 65;
			public const int Deathbringer = 70;
			public const int Molten = 100;
			public const int Cobalt = 110;
			public const int Palladium = 130;
			public const int Mythril = 150;
			public const int Orichalcum = 165;
			public const int Adamantite = 180;
			public const int Titanium = 190;
			public const int Chlorophyte = 200;
			public const int Picksaw = 210;
			public const int Luminite = 225;
		}
		public abstract int pickaxePower { get; }
		public abstract int miningCooldown { get; }
		protected virtual Color MapColor => Color.Gray;
		protected ModTileEntity Entity => ModContent.GetInstance<BlockBreakerTE>();
		protected override bool IsValidSolidReplaceTile => true;
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			TileID.Sets.DrawsWalls[Type] = true;
			TileID.Sets.DontDrawTileSliced[Type] = true;
			TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;
			TileID.Sets.CanBeSloped[Type] = true;

			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;
			if (TA_Mod.serverConfig.BlockPlacersAndBreakersSolidTiles) {
				Main.tileSolid[Type] = true;
				Main.tileBlockLight[Type] = true;
			}

			Color mapColor = MapColor;
			mapColor.A = byte.MaxValue;
			AddMapEntry(mapColor, CreateMapEntryName());

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.UsesCustomCanPlace = false;
			TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);
		}
		public override void Load() {
			//On_WorldGen.KillTile_GetItemDrops += On_WorldGen_KillTile_GetItemDrops;
			On_WorldGen.EmptyTileCheck += On_WorldGen_EmptyTileCheck;
		}

		internal static void OnSpawn(Item item, IEntitySource context) {
			if (breakingBlock) {
				if (TryDepositToTouchingChests(item))
					return;

				TryDepositToStorageNetwork(item);
			}
		}

		private static bool TryDepositToTouchingChests(Item item) {
			Tile blockBreakerTile = Main.tile[blockBreakerX, blockBreakerY];
			ModTile modTile = TileLoader.GetTile(blockBreakerTile.TileType);
			if (modTile is not BlockBreaker)
				return false;

			GetChests(blockBreakerX, blockBreakerY, out List<int> storageChests);
			if (storageChests.Count == 0)
				return false;

			if (breakingTree) {
				foreach (int chestNum in storageChests) {
					Chest chest = Main.chest[chestNum];
					if (chest.DepositVisualizeChestTransfer(item))
						return true;
				}
			}
			else {
				foreach (int chestNum in storageChests) {
					Chest chest = Main.chest[chestNum];
					if (chest.item.Deposit(item, out _))
						return true;
				}
			}

			return false;
		}

		private static void TryDepositToStorageNetwork(Item item) {
			if (!StorageNetwork.TryGetStorageInventories(blockBreakerX, blockBreakerY, out List<StorageInfo> storages))
				return;

			IEnumerable<IList<Item>> inventories = storages.Where(s => s.CanDepositItemsTo).Select(s => s.Inventory);
			if (breakingTree) {
				int stack = item.stack;
				foreach (IList<Item> inv in inventories) {
					if (inv.Deposit(item, out _))
						break;
				}

				int transferred = stack - item.stack;
				if (transferred != 0) {
					//TODO: Visualize the transfer to the breaker instead of chest

				}
			}
			else {
				foreach (IList<Item> inv in inventories) {
					if (inv.Deposit(item, out _))
						return;
				}
			}
		}

		private bool On_WorldGen_EmptyTileCheck(On_WorldGen.orig_EmptyTileCheck orig, int startX, int endX, int startY, int endY, int ignoreID) {
			if (TileID.Sets.CommonSapling[ignoreID]) {
				if (startX < 0)
					return false;

				if (endX >= Main.maxTilesX)
					return false;

				if (startY < 0)
					return false;

				if (endY >= Main.maxTilesY)
					return false;

				bool flag = false;
				if (ignoreID != -1 && TileID.Sets.CommonSapling[ignoreID])
					flag = true;

				for (int i = startX; i < endX + 1; i++) {
					for (int j = startY; j < endY + 1; j++) {
						if (!Main.tile[i, j].HasTile)
							continue;

						if (j == endY && int.Abs(i - (startX + endX) / 2) == 2 && TileLoader.GetTile(Main.tile[i, j].TileType) is BlockBreaker or BlockPlacer)
							continue;

						switch (ignoreID) {
							case -1:
								return false;
							case 11: {
								ushort type = Main.tile[i, j].TileType;
								if (type == 11)
									continue;

								return false;
							}
							case 71: {
								ushort type = Main.tile[i, j].TileType;
								if (type == 71)
									continue;

								return false;
							}
						}

						if (flag) {
							if (TileID.Sets.CommonSapling[Main.tile[i, j].TileType])
								break;

							if (TileID.Sets.IgnoredByGrowingSaplings[Main.tile[i, j].TileType])
								continue;

							return false;
						}
					}
				}

				return true;
			}

			return orig(startX, endX, startY, endY, ignoreID);
		}
		protected override void OnTileObjectDrawPreview(short x, short y, TileObjectPreviewData op) {
			Main.LocalPlayer.GetDirectionID(op.Coordinates.X, op.Coordinates.Y, out short directionID);

			if (ItemSlot.ShiftInUse)
				directionID = (short)((directionID + 2) % 4);

			op.Style = directionID;
		}

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		public override void PlaceInWorld(int i, int j, Item item) {
			Tile tile = Main.tile[i, j];
			Main.LocalPlayer.GetDirectionID(i, j, out short directionID);

			if (ItemSlot.ShiftInUse)
				directionID = (short)((directionID + 2) % 4);

			SetTileDirection(tile, directionID);
			Entity.Hook_AfterPlacement(i, j, tile.TileType, 0, 0, 0);
		}
		private static int GetDirectionID(int x, int y) => Main.tile[x, y].TileFrameX / 18;
		public override bool Slope(int i, int j) {
			Tile tile = Main.tile[i, j];
			short directionID = (short)((tile.TileFrameX / 18 + 3) % 4);
			SetTileDirection(tile, directionID);

			SoundEngine.PlaySound(SoundID.Dig, new Point16(i, j).ToWorldCoordinates());

			return false;
		}
		private void SetTileDirection(Tile tile, short directionID) {
			tile.TileFrameX = (short)(directionID * 18);
			tile.TileFrameY = 0;
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY, 1, TileChangeType.None);
		}

		protected HitTileObject hitData = new();
		private static bool breakingTree = false;
		private static bool breakingBlock = false;
		private static int blockBreakerX;
		private static int blockBreakerY;
		public static void SendChestDatas(int x, int y) {
			GetChests(x, y, out List<int> storageChests);
			foreach (int chestId in storageChests) {
				GlobalChest.MarkIndicatorChest(chestId);
			}
		}
		public static void GetChests(int x, int y, out List<int> storageChests) {
			int breakerFacingDirection = GetDirectionID(x, y);
			storageChests = new();
			for (int directionID = 0; directionID < 4; directionID++) {
				if (directionID == breakerFacingDirection)
					continue;

				PathDirectionID.GetDirection(directionID, x, y, out int chestX, out int chestY);
				Tile tile = Main.tile[chestX, chestY];
				if (!tile.HasTile)
					continue;

				if (!GlobalChest.ValidTileTypeForStorageChestIncludeExtractinators(tile.TileType))
					continue;

				Point16 chestTopLeft = AndroUtilityMethods.TilePositionToTileTopLeft(chestX, chestY);
				if (AndroUtilityMethods.TryGetChest(chestTopLeft.X, chestTopLeft.Y, out int chestNum))
					storageChests.Add(chestNum);
			}
		}

		public override void HitWire(int i, int j) {
			Tile tile = Main.tile[i, j];
			int directionID = tile.TileFrameX / 18;
			PathDirectionID.GetDirection(directionID, i, j, out int x, out int y);
			if (x < 0 || x > Main.maxTilesX - 1 || y < 0 || y > Main.maxTilesY - 1)
				return;

			if (!Wiring.CheckMech(i, j, miningCooldown))
				return;

			Tile target = Main.tile[x, y];
			bool shouldBreak = target.HasTile && WorldGen.CanKillTile(x, y);

			bool tree = false;
			if (!shouldBreak) {
				PathDirectionID.GetDirection(directionID, x, y, out int x2, out int y2);
				if (x2 >= 0 && x2 <= Main.maxTilesX - 1 && y2 >= 0 && y2 <= Main.maxTilesY - 1) {
					Tile target2 = Main.tile[x2, y2];
					if (target2.HasTile && TileID.Sets.IsATreeTrunk[target2.TileType] && WorldGen.CanKillTile(x2, y2)) {
						x = x2;
						y = y2;
						shouldBreak = true;
						tree = true;
					}
				}
			}
			else {
				tree = TileID.Sets.IsATreeTrunk[target.TileType];
			}

			if (!shouldBreak)
				return;

			if (Main.tileContainer[target.TileType] || TileID.Sets.BasicChest[target.TileType])
				return;

			if (TileID.Sets.CommonSapling[target.TileType] || TileID.Sets.TreeSapling[target.TileType])
				return;

			bool fail = pickaxePower < GenericGlobalTile.GetRequiredPickaxePower(target.TileType);

			breakingTree = tree;
			breakingBlock = true;
			blockBreakerX = i;
			blockBreakerY = j;
			WorldGen.KillTile(x, y, fail);
			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);

			breakingTree = false;
			breakingBlock = false;
		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill(i, j);
		}
	}
	public class BlockBreakerGlobalItem : GlobalItem {
		public override void OnSpawn(Item item, IEntitySource source) {
			BlockBreaker.OnSpawn(item, source);
		}
	}
}
