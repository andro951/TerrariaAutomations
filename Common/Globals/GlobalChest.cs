using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria;
using TerrariaAutomations.Tiles.TileEntities;
using Microsoft.Xna.Framework;
using androLib.Common.Utility;
using TerrariaAutomations.Tiles.Interfaces;
using Terraria.DataStructures;
using Terraria.UI;
using TerrariaAutomations.Items;
using TerrariaAutomations.Tiles;

namespace TerrariaAutomations.Common.Globals
{
    public class GlobalChest : GlobalTile {
		private static Asset<Texture2D> chestIndicatorTexture;
		public override void Load() {
			chestIndicatorTexture = ModContent.Request<Texture2D>("TerrariaAutomations/Tiles/AutoExtractors/ExtractinatorIndicatorDot");
		}

		public static SortedDictionary<int, ChestIndicatorInfo> ChestPercentFullInfo = new();
		private static readonly Point16 none = new(-1, -1);
		private static Point16[] allPoints = [
			new(0, -1),
			new(1, -1),
			new(2, 0),
			new(2, 1),
			new(1, -2),
			new(0, -2),
			new(-1, 1),
			new(-1, 0)
		];
		public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			if (Main.netMode != NetmodeID.Server && !TA_Mod.clientConfig.DisplayChestIndicators)
				return;

			//TODO: Config to disable indicators
			if (!ValidTileTypeForDisplayChestIndicators(type))
				return;

			Tile tile = Main.tile[i, j];
			TileObjectData data = TileObjectData.GetTileData(tile);
			if (data == null)
				return;

			int localFrameY = tile.TileFrameY % (data.Height * 18);
			if (localFrameY != 0)
				return;

			int localFrameX = tile.TileFrameX % (data.Width * 18);
			if (localFrameX != 0)
				return;

			if (!ShouldDisplayChestIndicators(i, j, true, data))
				return;

			if (!TryGetChestFill(i, j, out float stackPercentFull, out float slotsPercentFull))
				return;

			Vector2 center = new Point16(i + 1, j).ToWorldCoordinates(-1f, 0f) + GetTileSpecificOffset(tile.TileType);
			float indicatorRadius = 5f;

			Vector2 zero = (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange)) + new Vector2(-3f, -3f);
			Vector2 stackIndicatorPosition = center + new Vector2(-indicatorRadius, 0f);
			float alpha = 1f;
			Color stackColor = GetChestIndicatorColor(stackPercentFull, alpha);
			spriteBatch.Draw(chestIndicatorTexture.Value, stackIndicatorPosition - Main.screenPosition + zero, stackColor);

			Vector2 slotsIndicatorPosition = center + new Vector2(indicatorRadius, 0f);
			Color slotsColor = GetChestIndicatorColor(slotsPercentFull, alpha);
			spriteBatch.Draw(chestIndicatorTexture.Value, slotsIndicatorPosition - Main.screenPosition + zero, slotsColor);
		}
		private static bool ShouldDisplayChestIndicators(int chestX, int chestY, bool remove = false, TileObjectData data = null) {
			Tile tile = Main.tile[chestX, chestY];
			if (data == null) {
				if (GlobalAutoExtractor.IsExtractinator(tile.TileType))
					return true;
			}
			else if (data.Height != 2 || data.Width != 2) {
				if (!GlobalAutoExtractor.IsExtractinator(tile.TileType))
					return false;
			}

			bool found = false;
			Point16[] topSidePoints = [new(chestX - 1, chestY), new(chestX + 2, chestY)];
			foreach (Point16 p in topSidePoints) {
				if (IUseChestIndicators.ShouldDisplayChestIndicatorCheckLeftRight(p.X, p.Y, out _)) {
					found = true;
					break;
				}
			}

			if (!found) {
				foreach (Point16 p in allPoints) {
					if (IUseChestIndicators.ShouldDisplayChestIndicatorCheckAll(p.X + chestX, p.Y + chestY, out _)) {
						found = true;
						break;
					}
				}
			}

			if (!found) {
				if (remove && Main.netMode == NetmodeID.MultiplayerClient) {
					int chestLocaion = GlobalChestStaticMethods.PositionValue(chestX, chestY);
					ChestPercentFullInfo.Remove(chestLocaion);
				}

				return false;
			}

			return true;
		}
		private static Vector2 GetTileSpecificOffset(int tileType) {
			if (GlobalAutoExtractor.IsExtractinator(tileType))
				return GlobalAutoExtractor.GetOffset(tileType);

			return Vector2.Zero;
		}

		public static bool ValidTileTypeForStorageChest(int tileType) {
			if (GlobalAutoExtractor.IsExtractinator(tileType))
				return false;

			switch (tileType) {
				case TileID.Containers:
				case TileID.Containers2:
				case TileID.Dressers:
					return true;
				default:
					return false;
			}
		}
		public static bool ValidTileTypeForDisplayChestIndicators(int tileType) {
			if (Main.tileContainer[tileType])
				return true;

			switch (tileType) {
				case TileID.Containers:
				case TileID.Containers2:
				case TileID.Dressers:
				case TileID.Extractinator:
				case TileID.ChlorophyteExtractinator:
					return true;
				default:
					return false;
			}
		}
		private bool TryGetChestFill(int x, int y, out float stackPercentFull, out float slotsPercentFull) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (ChestPercentFullInfo.TryGetValue(GlobalChestStaticMethods.PositionValue(x, y), out ChestIndicatorInfo info)) {
					stackPercentFull = info.StackPercentFull;
					slotsPercentFull = info.SlotsPercentFull;
					return true;
				}
			}
			else {
				if (AndroUtilityMethods.TryGetChest(x, y, out int chestId)) {
					Chest chest = Main.chest[chestId];
					Item[] inv = chest.item;
					inv.PercentFull(out stackPercentFull, out slotsPercentFull);
					return true;
				}
			}

			stackPercentFull = 0f;
			slotsPercentFull = 0f;
			return false;
		}
		private static Color GetChestIndicatorColor(float percentFull, float alpha) {
			float red;
			float green;
			float blue = 0f;
			if (percentFull == 0f) {
				red = 0f;
				green = 1f;
				blue = 0.7f;
			}
			else if (percentFull < 0.5f) {
				green = 1f;
				red = percentFull * 2f;
			}
			else {
				red = 1f;
				if (percentFull == 1f) {
					green = 0f;
				}
				else {
					green = 1f - (percentFull - 0.5f) * 1.6f + 0.2f;
				}
			}

			return new Color(red, green, blue, alpha);
		}
		private static float counter = 0f;
		private static int lastSent = -1;

		internal static void Update() {
			if (Main.netMode != NetmodeID.Server)
				return;

			float size = ChestPercentFullInfo.Count;
			counter += size / 300f;//Send each info every 5 seconds
			int numThisTick = (int)counter;
			if (numThisTick < 1)
				return;

			counter -= numThisTick;
			bool foundFirst = false;
			List<int> toRemove = new();
			for (int i = 0; i < numThisTick;) {
				foreach (int v in ChestPercentFullInfo.Keys) {
					Point16 p = GlobalChestStaticMethods.ValueToPosition(v);
					if (!foundFirst) {
						if (v < lastSent) {
							continue;
						}
						else {
							foundFirst = true;
							continue;
						}
					}
					
					if (!ShouldDisplayChestIndicators(p.X, p.Y)) {
						toRemove.Add(v);
						int available = ChestPercentFullInfo.Count - toRemove.Count;
						if (numThisTick > available)
							numThisTick = available;
					}
					else {
						ChestPercentFullInfo[v].WriteAndSendChestLocation(p);
						i++;

						if (i == numThisTick) {
							lastSent = v;
							break;
						}
					}
				}

				foundFirst = true;
			}

			foreach (int v in toRemove) {
				ChestPercentFullInfo.Remove(v);
			}
		}
		public static void MarkIndicatorChest(int chestId) {
			Chest chest = Main.chest[chestId];
			if (chest == null)
				return;

			Item[] inv = chest.item;
			inv.PercentFull(out float stackPercentFull, out float slotsPercentFull);
			int v = GlobalChestStaticMethods.PositionValue(chest.x, chest.y);
			if (ChestPercentFullInfo.ContainsKey(v)) {
				ChestPercentFullInfo[v] = new(stackPercentFull, slotsPercentFull);
			}
			else {
				ChestPercentFullInfo.Add(v, new(stackPercentFull, slotsPercentFull));
			}
		}
	}
	public struct ChestIndicatorInfo {
		public float StackPercentFull;
		public float SlotsPercentFull;
		private static byte FloatToByte(float f) => (byte)(f * byte.MaxValue);
		private static float ByteToFloat(byte b) => b / (float)byte.MaxValue;
		public void WriteAndSendChestLocation(Point16 location) {
			//$"Send Chest Indicator: {location}, {this}".LogSimple();
			ModPacket packet = TA_Mod.Instance.GetPacket();
			packet.Write((byte)TA_Mod.TA_ModPacketID.ChestIndicatorInfo);
			packet.Write(location.X);
			packet.Write(location.Y);
			packet.Write(FloatToByte(StackPercentFull));
			packet.Write(FloatToByte(SlotsPercentFull));
			packet.Send();
		}
		public static void Read(BinaryReader reader) {
			int x = reader.ReadInt16();
			int y = reader.ReadInt16();
			int v = GlobalChestStaticMethods.PositionValue(x, y);
			ChestIndicatorInfo info = new ChestIndicatorInfo(reader);
			//$"Read ChestInfo: ({x}, {y}), {info}".LogSimpleNT();
			if (GlobalChest.ChestPercentFullInfo.ContainsKey(v)) {
				GlobalChest.ChestPercentFullInfo[v] = info;
			}
			else {
				GlobalChest.ChestPercentFullInfo.Add(v, info);
			}
		}
		public ChestIndicatorInfo(float stackPercentFull, float slotsPercentFull) {
			StackPercentFull = stackPercentFull;
			SlotsPercentFull = slotsPercentFull;
		}
		private ChestIndicatorInfo(BinaryReader reader) {
			StackPercentFull = ByteToFloat(reader.ReadByte());
			SlotsPercentFull = ByteToFloat(reader.ReadByte());
		}
		public override string ToString() {
			return $"stack {StackPercentFull.PercentString()}, slots {SlotsPercentFull.PercentString()}";
		}
	}
	public static class GlobalChestStaticMethods {
		public static int PositionValue(this Point16 p) => PositionValue(p.X, p.Y);
		public static int PositionValue(int x, int y) => x + y * Main.maxTilesX;
		internal static Point16 ValueToPosition(this int v) {
			int y = v / Main.maxTilesX;
			int x = v % Main.maxTilesX;
			return new Point16(x, y);
		}
	}
}
