using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.ObjectData;
using System.IO;
using androLib.Common.Utility;
using androLib;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.Tiles.Interfaces;
using Terraria.UI;
using TerrariaAutomations.TileData.Pipes;

namespace TerrariaAutomations.Tiles.TileEntities
{
    public abstract class AutoExtractinatorTE : ModTileEntity, IUseChestIndicators {
        public abstract int Timer { get; }
        protected abstract int TileToBeValidOn { get; }
		protected abstract int ConsumeMultiplier { get; }

		private int timer = 0;
        public bool TryGetMyChest(out int chest) => AndroUtilityMethods.TryGetChest(Position.X, Position.Y, out chest);
        public static int T1 {
            get {
                if (t1 == -1)
                    t1 = ModContent.TileType<WoodAutoExtractinatorTile>();

                return t1;
            }
        }
        private static int t1 = -1;
        public static int T3 {
            get {
                if (t3 == -1)
                    t3 = ModContent.TileType<HellstoneAutoExtractinatorTile>();

                return t3;
            }
        }
        private static int t3 = -1;
        public static int T5 {
            get {
                if (t5 == -1)
                    t5 = ModContent.TileType<LuminiteAutoExtractinatorTile>();

                return t5;
            }
        }
        private static int t5 = -1;

        public override bool IsTileValidForEntity(int x, int y) {
            var tile = Main.tile[x, y];
            return tile.TileType == TileToBeValidOn;
        }
		//public bool ShouldDisplayChestIndicatorCheckLeftRight(int x, int y, Tile tile) => tile.TileFrameY % 54 != 36;//AutoExtractors look on same level and 1 up
		private int GetChestID() => Chest.FindChest(Position.X, Position.Y);
        public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
        private bool? junkInMyChest = null;
		private int sendCounter = 0;
		private const int sendCounterReset = 60;
		public override void Update() {
			if (Main.netMode == NetmodeID.Server) {
				sendCounter++;
				if (sendCounter >= sendCounterReset) {
					sendCounter = 0;
                    if (TryGetMyChest(out int chestNum))
						GlobalChest.MarkIndicatorChest(chestNum);

					GetChests(out List<int> storageChests);
					foreach (int chestId in storageChests) {
						GlobalChest.MarkIndicatorChest(chestId);
					}
				}
			}

			Tile tile = Main.tile[Position.X, Position.Y];
            if (Position.AnyTileWireTopLeft())
				return;

			GlobalAutoExtractor.Instance.HitWire(Position.X, Position.Y, tile.TileType);
        }
		private void GetChests(out List<int> storageChests) {
			int[] chestPositionXOffsets = [-2, 3];
			int[] chestPositionYOffsets = [1, 0];
			storageChests = new();
			for (int i = 0; i < chestPositionXOffsets.Length; i++) {
				int xOffset = chestPositionXOffsets[i];
				for (int j = 0; j < chestPositionYOffsets.Length; j++) {
					int yOffset = chestPositionYOffsets[j];
					int chestX = Position.X + xOffset;
					int chestY = Position.Y + yOffset;
					Tile tile = Main.tile[chestX, chestY];
					if (!tile.HasTile)
						continue;

					if (!GlobalChest.ValidTileTypeForStorageChest(tile.TileType))
						continue;

					if (AndroUtilityMethods.TryGetChest(chestX, chestY, out int chestNum))
						storageChests.Add(chestNum);
				}
			}
		}

		internal void OnHitWire(int x, int y) {
			if (TryGetMyChest(out int chestNum)) {
                if (Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(chestNum) == -1) {
					IList<Item> extractinatorInv = Main.chest[chestNum].item;
					int i = 0;
					GetChests(out List<int> storageChests);
					IEnumerable<IList<Item>> inventories = storageChests.Where(c => Main.chest[c] != null && (Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(c) == -1)).Select(c => Main.chest[c].item);
                    if (StorageNetwork.TryGetStorageInventories(Position.X, Position.Y, out List<StorageInfo> storages))
                        inventories = inventories.Concat(storages.Where(s => s.CanDepositItemsTo).Select(s => s.Inventory));

					for (int z = 0; z < ConsumeMultiplier; z++) {
						for (; i < extractinatorInv.Count; i++) {
							Item item = extractinatorInv[i];
							if (item.NullOrAir())
								continue;

							int extractinatorMode = ItemID.Sets.ExtractinatorMode[item.type];
							if (extractinatorMode == -1) {
								junkInMyChest = true;
								continue;
							}

							if (item.stack > 0)
								item.stack -= 1;

							if (item.stack <= 0)
								item.TurnToAir();

							ExtractionItem.AutoExtractinatorUse(extractinatorMode, TileToBeValidOn, out int type, out int stack);

							TryRemovingMyJunk(extractinatorInv, inventories);
							TryDepositToChest(inventories, type, ref stack);

							break;
						}
					}
				}
			}
			else {
				$"Failed to find chest for the AutoExtractor at ({Position.X}, {Position.Y})".LogSimple();
			}
		}

		private void TryRemovingMyJunk(IList<Item> myChestInv, IEnumerable<IList<Item>> inventories) {
            if (junkInMyChest == false)
                return;

            if (inventories.Count() < 1)
                return;

            junkInMyChest = false;
			foreach (Item item in myChestInv) {
                if (item.NullOrAir() || item.stack < 1)
                    continue;

                if (ItemID.Sets.ExtractinatorMode[item.type] >= 0)
                    continue;

                bool deposited = false;
                foreach (IList<Item> inv in inventories) {
					if (inv.Deposit(item, out int _)) {
                        deposited = true;
						break;
					}
				}

                if (!deposited)
                    junkInMyChest = true;
            }
		}

		private void TryDepositToChest(IEnumerable<IList<Item>> inventories, int itemType, ref int stack)
        {
            if (itemType <= ItemID.None)
                return;

            if (stack <= 0)
                return;

            while (stack > 0) {
                Item item = new(itemType, stack);
                int itemStack = stack;
                if (item.stack > item.maxStack) {
                    item.stack = item.maxStack;
                    itemStack = item.stack;
                }

                bool deposited = false;
                foreach (IList<Item> inv in inventories) {
                    if (inv == null)
                        continue;

                    if (inv.Deposit(item, out int _)) {
                        deposited = true;
                        break;
                    }
                }

                if (!deposited) {
					int chest = GetChestID();
					if (chest != -1 && (Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(chest) == -1)) {
						IList<Item> inv = Main.chest[chest]?.item;
                        if (inv != null) {
                            if (inv.Deposit(item, out int junkAmount))
								deposited = true;

							if (junkAmount > 0)
								junkInMyChest = true;
						}
					}
					
                    if (!deposited) {
						Vector2 extractorWordCoordinates = Position.ToWorldCoordinates();
						int number = Item.NewItem(null, (int)extractorWordCoordinates.X, (int)extractorWordCoordinates.Y, 1, 1, item.type, item.stack, noBroadcast: false, -1);

						if (Main.netMode == NetmodeID.MultiplayerClient) {
							NetMessage.SendData(MessageID.SyncItem, -1, -1, null, number, 1f);
						}

						item.stack = 0;
					}
                }

                stack -= itemStack - item.stack;
            }
        }

		internal void PlaceAutoExtractor(int i, int j, int type) {
			Chest.AfterPlacement_Hook(i, j, type);
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
				return;
			}

            Point16 topLeft = AndroUtilityMethods.TileOriginToMultiTileTopLeft(i, j, type);
			Place(topLeft.X , topLeft.Y);
		}
	}

    //Don't Delete.  For testing chests and AutoExtractors - andro951
    /*
    public class TestingModSystem : ModSystem {
		public override void PostUpdateEverything() {
			for (int i = 0; i < Main.chest.Length; i++) {
                Chest chest = Main.chest[i];
                if (chest == null || chest.item == null)
                    continue;

                Point chestPosition = new(chest.x, chest.y);
                Dust testDust = Dust.NewDustPerfect(chestPosition.ToWorldCoordinates(), ModContent.DustType<ExtractinatorDust>(), Vector2.Zero, newColor: Color.Yellow);
                testDust.noGravity = true;
            }
		}
	}
    */
	public class ExtractinatorDust : ModDust {
        public override string Texture => $"TerrariaAutomations/Tiles/AutoExtractors/ExtractinatorDust";
        private const int timer = 65;
        private const float endScale = 0.99f;
        private const float scaleStep = (1f - endScale) / (float)timer;
        public override bool Update(Dust dust)
        {
            dust.scale -= scaleStep;
            if (dust.scale < endScale)
            {
                 dust.active = false;
            }

            return false;
        }
        public override void OnSpawn(Dust dust)
        {
            dust.scale = 1f;
            dust.noGravity = true;
        }
    }
}
