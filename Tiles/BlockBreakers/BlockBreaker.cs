﻿using androLib.Common.Utility;
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

namespace TerrariaAutomations.Tiles {
	public abstract class BlockBreaker : ModTile {
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
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			TileID.Sets.DrawsWalls[Type] = true;
			TileID.Sets.DontDrawTileSliced[Type] = true;
			TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;

			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;

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
			On_TileObject.DrawPreview += On_TileObject_DrawPreview;
			On_Player.UpdatePlacementPreview += On_Player_UpdatePlacementPreview;
			On_TileObjectData.CustomPlace += On_TileObjectData_CustomPlace;
			On_WorldGen.KillTile_GetItemDrops += On_WorldGen_KillTile_GetItemDrops;
		}

		private static bool updatePlacementPreview = false;
		private void On_Player_UpdatePlacementPreview(On_Player.orig_UpdatePlacementPreview orig, Player self, Item sItem) {
			if (sItem.createTile != -1) {
				ModTile modTile = ModContent.GetModTile(sItem.createTile);
				if (modTile is Tiles.BlockBreaker or Tiles.BlockPlacer) {
					updatePlacementPreview = true;
				}
			}
			
			orig(self, sItem);
			updatePlacementPreview = false;
		}

		private bool On_TileObjectData_CustomPlace(On_TileObjectData.orig_CustomPlace orig, int type, int style) {
			if (updatePlacementPreview)
				return true;

			return orig(type, style);
		}
		private void On_WorldGen_KillTile_GetItemDrops(On_WorldGen.orig_KillTile_GetItemDrops orig, int x, int y, Tile tileCache, out int dropItem, out int dropItemStack, out int secondaryItem, out int secondaryItemStack, bool includeLargeObjectDrops) {
			orig(x, y, tileCache, out dropItem, out dropItemStack, out secondaryItem, out secondaryItemStack, includeLargeObjectDrops);
			if (dropItem <= ItemID.None && secondaryItem <= ItemID.None)
				return;

			//Intentionally allowed even if not SkyblockWorld
			if (breakingTileX == x && breakingTileY == y) {
				GetChests(blockBreakerX, blockBreakerY, out List<int> storageChests);
				if (dropItem > ItemID.None) {
					foreach (int chestNum in storageChests) {
						if (Main.netMode != NetmodeID.SinglePlayer && Chest.UsingChest(chestNum) > -1)
							continue;

						if (chestNum.TryDepositToChest(dropItem, ref dropItemStack)) {
							dropItem = ItemID.None;
							break;
						}
					}
				}

				if (secondaryItem > ItemID.None) {
					foreach (int chestNum in storageChests) {
						if (Main.netMode != NetmodeID.SinglePlayer && Chest.UsingChest(chestNum) > -1)
							continue;

						if (chestNum.TryDepositToChest(secondaryItem, ref secondaryItemStack)) {
							secondaryItem = ItemID.None;
							break;
						}
					}
				}
			}
		}
		private void On_TileObject_DrawPreview(On_TileObject.orig_DrawPreview orig, SpriteBatch sb, TileObjectPreviewData op, Vector2 position) {
			ModTile modTile = ModContent.GetModTile(op.Type);
			if (modTile is Tiles.BlockBreaker or Tiles.BlockPlacer) {
				Main.LocalPlayer.GetDirectionID(op.Coordinates.X, op.Coordinates.Y, out short directionID);
				op.Style = directionID;
			}

			orig(sb, op, position);
		}

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		public override void PlaceInWorld(int i, int j, Item item) {
			Tile tile = Main.tile[i, j];
			Main.LocalPlayer.GetDirectionID(i, j, out short directionID);

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
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY, 1, TileChangeType.None);
		}

		protected HitTileObject hitData = new();
		private static int breakingTileX;
		private static int breakingTileY;
		private static int blockBreakerX;
		private static int blockBreakerY;
		public static void SendChestDatas(int x, int y) {
			GetChests(x, y, out List<int> storageChests);
			foreach (int chestId in storageChests) {
				GlobalChest.MarkIndicatorChest(chestId);
			}
		}
		private static void GetChests(int x, int y, out List<int> storageChests) {
			int breakerFacingDirection = GetDirectionID(x, y);
			storageChests = new();
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
		public override void HitWire(int i, int j) {
			Tile tile = Main.tile[i, j];
			int directionID = tile.TileFrameX / 18;
			PathDirectionID.GetDirection(directionID, i, j, out int x, out int y);
			Tile target = Main.tile[x, y];
			if (!WorldGen.CanKillTile(x, y))
				return;

			if (target.HasTile) {
				if (!Wiring.CheckMech(i, j, miningCooldown))
					return;

				if (Main.tileContainer[target.TileType] || TileID.Sets.BasicChest[target.TileType])
					return;

				bool fail = pickaxePower < GenericGlobalTile.GetRequiredPickaxePower(target.TileType);

				breakingTileX = x;
				breakingTileY = y;
				blockBreakerX = i;
				blockBreakerY = j;
				WorldGen.KillTile(x, y, fail);
				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);

				breakingTileX = -1;
				breakingTileY = -1;
			}

			//Show drill


		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill(i, j);
		}
	}
}
