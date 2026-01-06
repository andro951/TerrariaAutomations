using androLib;
using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations.Items;

namespace TerrariaAutomations.Common.Globals {
    public class TA_VanillaGlobalItem : GlobalItem {
        private class RecipeInfo(int createItemType, List<int> requiredTiles, SortedDictionary<int, int> requiredItems, SortedDictionary<int, int>? RequiredRecipeGroups = null, int createItemStack = 1) {
            public int CreateItemType = createItemType;
            public List<int> RequiredTiles = requiredTiles;
            public int CreateItemStack = createItemStack;
            public SortedDictionary<int, int> RequiredItems = requiredItems;
            public SortedDictionary<int, int>? RequiredRecipeGroups = RequiredRecipeGroups;
            public int GetValue() {
                int value = 0;
                foreach (KeyValuePair<int, int> kvp in RequiredItems) {
                    Item item = new(kvp.Key);
                    value += item.value * kvp.Value;
                }

                if (RequiredRecipeGroups != null) {
                    foreach (KeyValuePair<int, int> kvp in RequiredRecipeGroups) {
                        RecipeGroup group = RecipeGroup.recipeGroups[kvp.Key];
                        Item item = new(group.IconicItemId);
                        value += item.value * kvp.Value;
                    }
                }

                if (CreateItemStack > 1) {
                    value /= CreateItemStack;
                }

                if (value < 1) {
                    $"Value was calculated as less than 1 for item: {CreateItemType}".Log();
                }

                return value;
            }
            public Recipe ToRecipe() {
                Recipe recipe = Recipe.Create(CreateItemType, CreateItemStack);
                foreach (int tile in RequiredTiles) {
                    recipe.AddTile(tile);
                }

                foreach (KeyValuePair<int, int> kvp in RequiredItems) {
                    recipe.AddIngredient(kvp.Key, kvp.Value);
                }

                if (RequiredRecipeGroups != null) {
                    foreach (KeyValuePair<int, int> kvp in RequiredRecipeGroups) {
                        recipe.AddRecipeGroup(kvp.Key, kvp.Value);
                    }
                }

                return recipe;
            }
        }
        private static Dictionary<int, RecipeInfo> recipeInfos = new() {
            [ItemID.Extractinator] = new(ItemID.Extractinator, [
                    TileID.Anvils
                ], new() {
                    [ModContent.ItemType<WoodAutoExtractinator>()] = 1,
                }, new() {
                    [RecipeGroupID.IronBar] = 18,
                }
            ),
            [ItemID.ChlorophyteExtractinator] = new(ItemID.ChlorophyteExtractinator, [
                    TileID.MythrilAnvil
                ], new() {
                    [ModContent.ItemType<HellstoneAutoExtractinator>()] = 1,
                    [ItemID.ChlorophyteBar] = 18,
                }
            ),
            [ItemID.Autohammer] = new(ItemID.Autohammer, [
                    TileID.Anvils
                ], new() {
                    [ItemID.StoneBlock] = 20,
                    [ItemID.Chain] = 2,
                }, new() {
                    [RecipeGroupID.IronBar] = 8,
                    [RecipeGroupID.Wood] = 3,
                }
            ),
        };
        public override void SetDefaults(Item entity) {
            base.SetDefaults(entity);
            switch (entity.type) {
                case ItemID.Extractinator:
                    if (extractinatorValue < 0)
                        throw new Exception("Extractinator value not set before SetDefaults");

                    entity.value = extractinatorValue;
                    break;
                case ItemID.ChlorophyteExtractinator:
                    if (chlorophyteExtractinatorValue < 0)
                        throw new Exception("Chlorophyte Extractinator value not set before SetDefaults");

                    entity.value = chlorophyteExtractinatorValue;
                    break;
                case ItemID.Autohammer:
                    if (autoHammerValue < 0)
                        throw new Exception("Auto Hammer value not set before SetDefaults");

                    entity.value = autoHammerValue;
                    break;
            }
        }
        private static int extractinatorValue {
            get {
                if (_extractinatorValue == null) {
                    SetValues();
                }

                return _extractinatorValue.Value;
            }
        }
        private static int? _extractinatorValue = null;
        private static int chlorophyteExtractinatorValue {
            get {
                if (_chlorophyteExtractinatorValue == null) {
                    SetValues();
                }

                return _chlorophyteExtractinatorValue.Value;
            }
        }
        private static int? _chlorophyteExtractinatorValue = null;
        private static int autoHammerValue {
            get {
                if (_autoHammerValue == null) {
                    SetValues();
                }

                return _autoHammerValue.Value;
            }
        }
        private static int? _autoHammerValue = null;
        private static void SetValues() {
            SetValue(recipeInfos[ItemID.Extractinator], ref _extractinatorValue);
            SetValue(recipeInfos[ItemID.ChlorophyteExtractinator], ref _chlorophyteExtractinatorValue);
            SetValue(recipeInfos[ItemID.Autohammer], ref _autoHammerValue);
        }
        public static void OnAddRecipes() {
            for (int i = 0; i < AndroMod.VanillaRecipeCount; i++) {
                Recipe recipe = Main.recipe[i];
                if (recipe.createItem.type == ItemID.ChlorophyteExtractinator) {
                    recipe.DisableRecipe();
                }
            }

            foreach (KeyValuePair<int, RecipeInfo> kvp in recipeInfos) {
                kvp.Value.ToRecipe().Register();
            }
        }
        private static void SetValue(RecipeInfo recipeInfo, ref int? value) {
            value = recipeInfo.GetValue();
        }
    }
}