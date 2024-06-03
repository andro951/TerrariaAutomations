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

namespace TerrariaAutomations.Tiles {
	public abstract class BlockPlacer : ModTile {
		public abstract int cooldown { get; }
		protected virtual Color MapColor => Color.Gray;
		protected ModTileEntity Entity => ModContent.GetInstance<BlockPlacerTE>();
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

		private int sendCounter = 0;
		private const int sendCounterReset = 60;
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
		public override void HitWire(int i, int j) {
			int placerFacingDirection = GetDirectionID(i, j);
			PathDirectionID.GetDirection(placerFacingDirection, i, j, out int x, out int y);
			Tile target = Main.tile[x, y];

			if (target.HasTile)
				return;

			GetChests(i, j, out List<int> storageChests);
			
			if (storageChests.Count < 1)
				return;

			if (!Wiring.CheckMech(i, j, cooldown))
				return;

			//Select Block to place
			int tileToPlace = -1;
			foreach (int chestNum in storageChests) {
				if (Main.netMode != NetmodeID.SinglePlayer && Chest.UsingChest(chestNum) > -1)
					continue;

				if (SelectPlacableBlockAndConsumeItem(chestNum, out tileToPlace))
					break;
			}

			if (tileToPlace != -1)
				AndroUtilityMethods.PlaceTile(x, y, tileToPlace);
		}
		private static bool SelectPlacableBlockAndConsumeItem(int chestNum, out int tileType) {
			tileType = -1;
			foreach (Item item in Main.chest[chestNum].item) {
				if (item.NullOrAir() || item.stack < 1)
					continue;

				int createTile = item.createTile;
				if (createTile == -1)
					continue;

				TileObjectData data = TileObjectData.GetTileData(createTile, 0);
				if (data != null && (data.Width > 1 || data.Height > 1))
					continue;

				item.stack--;
				if (item.stack <= 0)
					item.TurnToAir();

				tileType = createTile;
				return true;
			}

			return false;
		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill(i, j);
		}
	}
}
