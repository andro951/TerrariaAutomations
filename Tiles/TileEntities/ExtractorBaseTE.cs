using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using TerrariaAutomations.Tiles.Interfaces;
using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.TileData.Pipes;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.GameContent;
using System.Reflection;

namespace TerrariaAutomations.Tiles.TileEntities {
    public abstract class ExtractorBaseTE : ModTileEntity, IUseChestIndicators {
        public abstract int Timer { get; }
        protected abstract int TileToBeValidOn { get; }
        protected abstract int ConsumeMultiplier { get; }

        protected int timer = 0;
        public bool TryGetMyChest(out int chest) => AndroUtilityMethods.TryGetChest(Position.X, Position.Y, out chest);
        public override bool IsTileValidForEntity(int x, int y) {
            var tile = Main.tile[x, y];
            return tile.TileType == TileToBeValidOn;
        }
        //public bool ShouldDisplayChestIndicatorCheckLeftRight(int x, int y, Tile tile) => tile.TileFrameY % 54 != 36;//AutoExtractors look on same level and 1 up
        protected int GetChestID() => Chest.FindChest(Position.X, Position.Y);
        public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
        protected bool? junkInMyChest = null;
        protected int sendCounter = 0;
        protected const int sendCounterReset = 60;
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
        protected void GetChests(out List<int> storageChests) {
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
        private static readonly MethodInfo tryGettingItemTraderFromBlockMI = typeof(Player).GetMethod("TryGettingItemTraderFromBlock", BindingFlags.NonPublic | BindingFlags.Static);
        private static ItemTrader TryGettingItemTraderFromBlock(Tile tile) => (ItemTrader)tryGettingItemTraderFromBlockMI.Invoke(null, [tile]);
        private static HashSet<int> checkedItems = new(40);
        private static HashSet<int> duplicateItems = new(40);
        private static List<int> nonAirItems = new(40);
        internal void OnHitWire(int x, int y) {
            if (TryGetMyChest(out int chestNum)) {
                if (Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(chestNum) == -1) {
                    Tile tile = Main.tile[x, y];
                    ItemTrader itemTrader = TryGettingItemTraderFromBlock(tile);

                    Item[] inv = Main.chest[chestNum].item;
                    checkedItems.Clear();
                    duplicateItems.Clear();
                    nonAirItems.Clear();
                    for (int i = 0; i < inv.Length; i++) {
                        Item item = inv[i];
                        if (item.NullOrAir())
                            continue;

                        nonAirItems.Add(i);
                        int itemType = item.type;
                        if (checkedItems.Contains(itemType)) {
                            duplicateItems.Add(itemType);
                        }
                        else {
                            if (item.stack < item.maxStack)
                                checkedItems.Add(itemType);
                        }
                    }

                    if (nonAirItems.Count == 0) {
                        junkInMyChest = false;
                        return;
                    }

                    if (duplicateItems.Count > 0) {
                        for (int i = 0; i < nonAirItems.Count; i++) {
                            int index = nonAirItems[i];
                            Item item = inv[index];
                            if (item.NullOrAir())
                                continue;

                            if (!duplicateItems.Contains(item.type))
                                continue;

                            if (item.stack >= item.maxStack)
                                continue;

                            for (int j = nonAirItems.Count - 1; j > i; j--) {
                                int index2 = nonAirItems[j];
                                Item other = inv[index2];
                                if (item.type != other.type)
                                    continue;

                                int toTransfer = Math.Min(other.stack, item.maxStack - item.stack);
                                item.stack += toTransfer;
                                other.stack -= toTransfer;
                                if (other.stack <= 0)
                                    other.TurnToAir();

                                if (item.stack >= item.maxStack)
                                    break;
                            }
                        }
                    }

                    GetChests(out List<int> storageChests);
                    List<Item[]> inventories = storageChests.Where(c => Main.chest[c] != null && (Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(c) == -1)).Select(c => Main.chest[c].item).ToList();
                    if (StorageNetwork.TryGetStorageInventories(Position.X, Position.Y, out List<StorageInfo> storages)) {
                        inventories.AddRange(
                            storages.Where(s => s.CanDepositItemsTo).Select(s => s.Inventory)
                        );
                    }

                    for (int i = 0; i < nonAirItems.Count; i++) {
                        int index = nonAirItems[i];
                        Item item = inv[index];
                        if (item.NullOrAir())
                            continue;

                        int itemType = item.type;

                        ItemTrader.TradeOption option = null;
                        bool hasItemTrader = itemTrader != null && itemTrader.TryGetTradeOption(item, out option);
                        bool canExtract = hasItemTrader || ItemID.Sets.ExtractinatorMode[itemType] >= 0 && GlobalAutoExtractor.IsExtractinatorTile(tile.TileType);

                        if (!canExtract) {
                            junkInMyChest = true;
                            continue;
                        }

                        int requiredStack = option != null ? option.TakingItemStack : 1;

                        int consumeTimes = Math.Min(ConsumeMultiplier, item.stack / requiredStack);
                        if (consumeTimes == 0)
                            continue;

                        int toConsume = Math.Min(item.stack, consumeTimes * requiredStack);
                        if (toConsume > item.stack)
                            throw new Exception("Logic error in AutoExtractorTE item consumption.");

                        item.stack -= toConsume;
                        if (item.stack <= 0)
                            item.TurnToAir();

                        for (int j = 0; j < consumeTimes; j++) {
                            int type;
                            int stack;
                            if (hasItemTrader) {
                                type = option.GivingITemType;
                                stack = option.GivingItemStack;
                            }
                            else {
                                int extractinatorMode = ItemID.Sets.ExtractinatorMode[itemType];
                                ExtractionItem.AutoExtractinatorUse(extractinatorMode, TileToBeValidOn, out type, out stack);
                            }

                            TryRemovingMyJunk(inv, inventories, itemTrader);
                            TryDepositToChest(inventories, type, ref stack);
                        }

                        break;
                    }

                    checkedItems.Clear();
                    duplicateItems.Clear();
                    nonAirItems.Clear();

                    if (junkInMyChest.HasValue && junkInMyChest.Value)
                        TryRemovingMyJunk(inv, inventories, itemTrader);
                }
            }
            else {
                $"Failed to find chest for the AutoExtractor at ({Position.X}, {Position.Y})".LogSimple();
            }
        }
        private void TryRemovingMyJunk(IList<Item> myChestInv, IEnumerable<IList<Item>> inventories, ItemTrader itemTrader) {
            if (junkInMyChest == false)
                return;

            if (inventories.Count() < 1)
                return;

            junkInMyChest = false;
            foreach (Item item in myChestInv) {
                if (item.NullOrAir() || item.stack < 1)
                    continue;

                if (itemTrader != null ? itemTrader.TryGetTradeOption(item, out _) : ItemID.Sets.ExtractinatorMode[item.type] >= 0)
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
        protected void TryDepositToChest(IEnumerable<IList<Item>> inventories, int itemType, ref int stack) {
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
        internal void PlaceExtractor(int i, int j, int type) {
            Chest.AfterPlacement_Hook(i, j, type);
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
                return;
            }

            Point16 topLeft = AndroUtilityMethods.TileOriginToMultiTileTopLeft(i, j, type);
            Place(topLeft.X, topLeft.Y);
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
    public class ExtractinatorDust : ModDust {
        public override string Texture => $"TerrariaAutomations/Tiles/AutoExtractors/ExtractinatorDust";
        private const int timer = 65;
        private const float endScale = 0.99f;
        private const float scaleStep = (1f - endScale) / (float)timer;
        public override bool Update(Dust dust) {
            dust.scale -= scaleStep;
            if (dust.scale < endScale) {
                dust.active = false;
            }

            return false;
        }
        public override void OnSpawn(Dust dust) {
            dust.scale = 1f;
            dust.noGravity = true;
        }
    }
    */
}