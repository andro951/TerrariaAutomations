using androLib.Common.Utility;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaAutomations.Tiles.TileEntities;

namespace TerrariaAutomations.Common.Globals {
    public abstract class GlobalExtractorBase : GlobalTile {
        public static bool IsExtractorTile(int tileType) => extractorTileTypes.Contains(tileType);
        private bool TryGetEntityChest(int x, int y, out int chest) => AndroUtilityMethods.TryGetChest(AndroUtilityMethods.TilePositionToTileTopLeft(x, y), out chest);
        private bool TryGetEntity(int x, int y, out ExtractorBaseTE entity) {
            Point16 topLeft = AndroUtilityMethods.TilePositionToTileTopLeft(x, y);
            return TryGetEntity(topLeft, out entity);
        }
        private bool TryGetEntity(Point16 position, out ExtractorBaseTE entity) {
            if (TileEntity.ByPosition.TryGetValue(position, out TileEntity tileEntity)) {
                if (tileEntity is ExtractorBaseTE autoExtractorEntity) {
                    entity = autoExtractorEntity;
                    return true;
                }
            }

            entity = null;
            return false;
        }
        public static void AddExtractorBaseTEGetter(int tileType, Func<ExtractorBaseTE> getter) {
            extractorBaseTEgetters[tileType] = getter;
            extractorTileTypes.Add(tileType);
        }
        public static void AddChestIndicatorOffset(int tileType, Vector2 offset) {
            chestIndicatorOffsets[tileType] = offset;
        }
        private static Dictionary<int, Func<ExtractorBaseTE>> extractorBaseTEgetters = [];
        private static HashSet<int> extractorTileTypes = [];
        private static Dictionary<int, Vector2> chestIndicatorOffsets = [];
        private bool TryGetNewEntity(int tileType, out ExtractorBaseTE autoExtractor_BaseEntity) {
            if (extractorBaseTEgetters.TryGetValue(tileType, out Func<ExtractorBaseTE> getter)) {
                autoExtractor_BaseEntity = getter();
                return true;
            }

            autoExtractor_BaseEntity = null;
            return false;
        }

        internal static Vector2 GetChestIndicatorOffset(int tileType) {
            if (!chestIndicatorOffsets.TryGetValue(tileType, out Vector2 offset)) {
                offset = Vector2.Zero;
            }

            return offset;
        }

        public static void RegisterHooks() {
            On_Player.TileInteractionsUse += On_Player_TileInteractionsUse;
            IL_TileDrawing.CacheSpecialDraws_Part2 += IL_TileDrawing_CacheSpecialDraws_Part2;
            On_WorldGen.PlaceChestDirect += On_WorldGen_PlaceChestDirect;
        }

        private static void On_WorldGen_PlaceChestDirect(On_WorldGen.orig_PlaceChestDirect orig, int x, int y, ushort type, int style, int id) {
            if (IsExtractorTile(type)) {
                if (TileObject.CanPlace(x, y, type, style, 1, out var objectData)) {
                    TileObject.Place(objectData);
                    Chest.CreateChest(objectData.xCoord, objectData.yCoord, id);
                }

                return;
            }

            orig(x, y, type, style, id);
        }
        private static void IL_TileDrawing_CacheSpecialDraws_Part2(ILContext il) {
            ILCursor c = new(il);

            //// if (TileID.Sets.BasicChest[drawData.typeCache])
            //IL_0000: ldsfld bool[] Terraria.ID.TileID / Sets::BasicChest
            //IL_0005: ldarg.3
            //IL_0006: ldfld uint16 Terraria.DataStructures.TileDrawInfo::typeCache
            //IL_000b: ldelem.u1
            //IL_000c: brfalse IL_013d

            FieldReference tileTypeRef = null;
            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(out _),
                i => i.MatchLdarg3(),
                i => i.MatchLdfld(out tileTypeRef),
                i => i.MatchLdelemU1()
                )) {
                throw new Exception("Failed to find instructions for IL_TileDrawing_CacheSpecialDraws_Part2");
            }

            c.Emit(OpCodes.Ldarg_3);
            c.Emit(OpCodes.Ldfld, tileTypeRef);
            c.EmitDelegate((bool basicChest, int tileType) => {
                if (basicChest) {
                    if (IsExtractorTile(tileType))
                        return false;
                }

                return basicChest;
            });
        }
        private static void On_Player_TileInteractionsUse(On_Player.orig_TileInteractionsUse orig, Player self, int myX, int myY) {
            Tile tile = Main.tile[myX, myY];
            if (self.releaseUseTile && self.tileInteractAttempted) {
                int tileType = tile.TileType;
                if (IsExtractorTile(tileType)) {
                    if (TileLoader.RightClick(myX, myY))
                        self.tileInteractionHappened = true;

                    return;
                }
            }

            orig(self, myX, myY);
        }
        public delegate bool orig_TileLoaderRightClick(int i, int j);
        public delegate bool hook_TileLoaderRightClick(orig_TileLoaderRightClick orig, int i, int j);
        public static MethodInfo OnTileRightClickInfo = typeof(TileLoader).GetMethod("RightClick", BindingFlags.Public | BindingFlags.Static);
        public static bool TileLoaderRightClickDetour(orig_TileLoaderRightClick orig, int i, int j) {
            bool result = orig(i, j);
            result |= OnRightClick(i, j);
            return result;
        }
        public static bool OnRightClick(int i, int j) {
            Tile tile = Main.tile[i, j];
            int tileType = tile.TileType;

            if (!IsExtractorTile(tileType))
                return false;

            Point16 topLeft = AndroUtilityMethods.TilePositionToTileTopLeft(i, j);
            int x = topLeft.X;
            int y = topLeft.Y;

            Player player = Main.LocalPlayer;
            Main.mouseRightRelease = false;

            player.CloseSign();
            player.SetTalkNPC(-1);
            Main.npcChatCornerItem = 0;
            Main.npcChatText = "";
            if (Main.editChest) {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = string.Empty;
            }

            if (player.editedChestName) {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
                player.editedChestName = false;
            }

            bool isLocked = Chest.IsLocked(x, y);
            if (Main.netMode == NetmodeID.MultiplayerClient && !isLocked) {
                if (x == player.chestX && y == player.chestY && player.chest != -1) {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
                else {
                    NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, x, y);
                    Main.stackSplit = 600;
                }
            }
            else {
                if (isLocked) {
                    //Chest for the AutoExtractors should never be locked.  Force it to unlock.
                    if (Chest.Unlock(x, y)) {
                        if (Main.netMode == NetmodeID.MultiplayerClient) {
                            NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, x, y);
                        }
                    }
                }
                else {
                    Chest chest = Main.chest[0];
                    if (AndroUtilityMethods.TryGetChest(topLeft, out int chestId)) {
                        Main.stackSplit = 600;
                        if (chestId == player.chest) {
                            player.chest = -1;
                            SoundEngine.PlaySound(SoundID.MenuClose);
                        }
                        else {
                            SoundEngine.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
                            player.OpenChest(x, y, chestId);
                        }

                        Recipe.FindRecipes();
                    }
                }
            }

            return true;
        }
        public override void HitWire(int i, int j, int type) {
            Tile tile = Main.tile[i, j];
            int tileType = tile.TileType;

            if (!IsExtractorTile(tileType))
                return;

            Point16 topLeft = AndroUtilityMethods.TilePositionToTileTopLeft(i, j);
            if (!TileEntity.ByPosition.TryGetValue(topLeft, out TileEntity te) || te is not ExtractorBaseTE ebTE)
                return;

            if (!Wiring.CheckMech(topLeft.X, topLeft.Y, ebTE.Timer))
                return;

            ebTE.OnHitWire(i, j);
        }
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) {
            if (!TryGetNewEntity(type, out ExtractorBaseTE extractorBaseTE))
                return;

            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX % 54 != 0 || tile.TileFrameY % 54 != 18)//TODO: Check this is correct for all tile types
                return;

            extractorBaseTE.Kill(i, j);

            if (!TryGetEntityChest(i, j, out int chestID))
                return;

            Chest chest = Main.chest[chestID];

            if (!Chest.DestroyChest(chest.x, chest.y)) {
                EntitySource_TileBreak source = new EntitySource_TileBreak(i, j, "Breaking AutoExtractor");
                for (int k = 0; k < chest.item.Length; k++) {
                    Item.NewItem(source, i * 16, j * 16, 1, 1, chest.item[k].type, chest.item[k].stack, noBroadcast: false, -1);
                    chest.item[k].TurnToAir();
                }

                Chest.DestroyChestDirect(chest.x, chest.y, chestID);
            }
        }
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) {
            if (!IsExtractorTile(Main.tile[i, j].TileType))
                return true;

            if (TryGetEntityChest(i, j, out int chestId)) {
                Chest chest = Main.chest[chestId];
                bool canDestroyChest = Chest.CanDestroyChest(chest.x, chest.y);
                return canDestroyChest;
            }

            return true;
        }
        public override void PlaceInWorld(int i, int j, int type, Item item) {
            if (!TryGetNewEntity(type, out ExtractorBaseTE autoExtractor_BaseEntity))
                return;

            autoExtractor_BaseEntity.PlaceExtractor(i, j, type);
        }
    }
}