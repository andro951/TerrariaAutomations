using androLib.Common.Utility;
using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.Items;
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
using Terraria.UI;
using androLib.Tiles;

namespace TerrariaAutomations.Tiles {
	public abstract class BlockPlacer : AndroModTile {
		public abstract int cooldown { get; }
		protected virtual Color MapColor => Color.Gray;
		protected ModTileEntity Entity => ModContent.GetInstance<BlockPlacerTE>();
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

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		protected override void OnTileObjectDrawPreview(short x, short y, TileObjectPreviewData op) {
			Main.LocalPlayer.GetDirectionID(op.Coordinates.X, op.Coordinates.Y, out short directionID);

			if (ItemSlot.ShiftInUse)
				directionID = (short)((directionID + 2) % 4);

			op.Style = directionID;
		}
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

		public static void SendChestDatas(int x, int y) {
			GetChests(x, y, out List<int> storageChests);
			foreach (int chestId in storageChests) {
				GlobalChest.MarkIndicatorChest(chestId);
			}
		}
		private static void GetChests(int x, int y, out List<int> storageChests) {
			storageChests = new();
			int breakerFacingDirection = GetDirectionID(x, y);
			for (int directionID = 0; directionID < 4; directionID++) {
				if (directionID == breakerFacingDirection)
					continue;

				PathDirectionID.GetDirection(directionID, x, y, out int chestX, out int chestY);
				Tile tile = Main.tile[chestX, chestY];
				if (!tile.HasTile)
					continue;

				if (!GlobalChest.ValidTileTypeForStorageChest(tile.TileType))
					continue;

				Point16 chestTopLeft = AndroUtilityMethods.TilePositionToTileTopLeft(chestX, chestY);
				if (AndroUtilityMethods.TryGetChest(chestTopLeft.X, chestTopLeft.Y, out int chestNum))
					storageChests.Add(chestNum);
			}
		}
		private static bool CanPlaceOnTile(Tile tile) => !tile.HasTile || Main.tileCut[tile.TileType];
		public override void HitWire(int i, int j) {
			int placerFacingDirection = GetDirectionID(i, j);
			PathDirectionID.GetDirection(placerFacingDirection, i, j, out int x, out int y);
			if (x < 0 || x > Main.maxTilesX - 1 || y < 0 || y > Main.maxTilesY - 1)
				return;

			Tile target = Main.tile[x, y];

			bool saplingOnly = !CanPlaceOnTile(target) && placerFacingDirection == PathDirectionID.Up;

			GetChests(i, j, out List<int> storageChests);
			
			if (storageChests.Count < 1)
				return;

			if (!Wiring.CheckMech(i, j, cooldown))
				return;

			//Select Block to place
			Item itemToPlace = null;
			foreach (int chestNum in storageChests) {
				if (Main.netMode != NetmodeID.SinglePlayer && Chest.UsingChest(chestNum) > -1)
					continue;

				if (SelectPlacableBlock(chestNum, saplingOnly, out Item item)) {
					itemToPlace = item;
					break;
				}
			}

			if (itemToPlace != null) {
				PathDirectionID.GetDirection(placerFacingDirection, x, y, out int x2, out int y2);
				bool CanUse2ndTileValues = x2 >= 0 && x2 <= Main.maxTilesX - 1 && y2 >= 0 && y2 <= Main.maxTilesY - 1;
				if (CanUse2ndTileValues) {
					Tile target2 = Main.tile[x2, y2];
					if (!CanPlaceOnTile(target2) && (TileID.Sets.CommonSapling[target2.TileType] || TileID.Sets.TreeSapling[target2.TileType]))
						return;//Don't place a block right next to a sapling.
				}

				if (TileID.Sets.CommonSapling[itemToPlace.createTile] || TileID.Sets.TreeSapling[itemToPlace.createTile]) {
					if (CanUse2ndTileValues) {
						Tile target2 = Main.tile[x2, y2];
						if (CanPlaceOnTile(target2)) {
							x = x2;//Place sapling 2 blocks away.
							y = y2;
						}
						else {
							return;//Don't place sapling 1 block away
						}
					}
				}

				AndroUtilityMethods.PlaceTile(x, y, itemToPlace);
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == itemToPlace.createTile) {
					itemToPlace.stack--;
					if (itemToPlace.stack <= 0)
						itemToPlace.TurnToAir();
				}
			}
				
		}
		private static bool SelectPlacableBlock(int chestNum, bool saplingOnly, out Item itemToPlace) {
			itemToPlace = null;
			foreach (Item item in Main.chest[chestNum].item) {
				if (item.NullOrAir() || item.stack < 1)
					continue;

				int createTile = item.createTile;
				if (createTile == -1)
					continue;

				bool sapling = false;
				TileObjectData data = TileObjectData.GetTileData(createTile, 0);
				if (data != null && (data.Width > 1 || data.Height > 1)) {
					sapling = TileID.Sets.CommonSapling[createTile] || TileID.Sets.TreeSapling[createTile];
					if (!sapling)
						continue;
				}

				if (saplingOnly && !sapling)
					continue;

				itemToPlace = item;

				return true;
			}

			return false;
		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill(i, j);
		}
	}
}
