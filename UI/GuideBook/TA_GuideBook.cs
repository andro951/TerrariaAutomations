using androLib;
using androLib.Common.Utility;
using androLib.UI.GuideBook;
using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using TerrariaAutomations.Items;

namespace TerrariaAutomations.UI.GuideBook {
	internal class TA_GuideBook : androLib.UI.GuideBook.GuideBook {
		public override string ModName => TA_Mod.ModName;
		protected override List<GuideBookSection> sections {
			get {
				if (_sections == null || Debugger.IsAttached)
					SetupSections();

				return _sections;
			}
		}
		private List<GuideBookSection> _sections = null;

        private const string GeneralSectionName = "General";
        private const string IntroductionTopicName = "Introduction";
        private const string ChestIndicatorsTopicName = "Chest Indicators";

        private const string ProcessingSectionName = "Processing";
        private const string AutoExtractinatorsTopicName = "Auto Extractinators";

        private const string FishingSectionName = "Fishing";
        private const string AutoFisherTopicName = "Auto Fisher";

        private const string BlockInteractionSectionName = "Block Interaction";
        private const string BlockBreakersTopicName = "Block Breakers";
        private const string BlockPlacersTopicName = "Block Placers";

        private const string StorageNetworksSectionName = "Storage Networks";
        private const string PipesTopicName = "Pipes";
        private void SetupSections() {
			_sections = [
                new(GeneralSectionName, GetModItemTexture(ModContent.ItemType<PipeWrench>()), [
                    new(IntroductionTopicName, GetVanillaItemTexture(ItemID.WireKite), IntroductionSetupTopic),
                    new(ChestIndicatorsTopicName, GetVanillaItemTexture(ItemID.Chest), ChestIndicatorsSetupTopic),
                ]),
                new(ProcessingSectionName, GetVanillaItemTexture(ItemID.Furnace), [
                    new(AutoExtractinatorsTopicName, GetVanillaItemTexture(ItemID.Extractinator), AutoExtractinatorSetupTopic),
                ]),
                new(FishingSectionName, GetVanillaItemTexture(ItemID.FiberglassFishingPole), [
					new(AutoFisherTopicName, GetVanillaItemTexture(ItemID.Mannequin), AutoFisherSetupTopic),
				]),
                new(BlockInteractionSectionName, GetVanillaItemTexture(ItemID.StoneBlock), [
                    new(BlockBreakersTopicName, GetModItemTexture(ModContent.ItemType<CopperBlockBreaker>()), BlockBreakersSetupTopic),
                    new(BlockPlacersTopicName, GetModItemTexture(ModContent.ItemType<CopperBlockPlacer>()), BlockPlacersSetupTopic),
                ]),
                new(StorageNetworksSectionName, GetVanillaItemTexture(ItemID.Chest), [
                    new(PipesTopicName, GetModItemTexture(ModContent.ItemType<Pipe>()), PipesSetupTopic),
                ]),
            ];
		}
        private void AddListAndScrollbar(UIPanel topicPanel, out AM_UIList list) {
            list = new();
            list.Width.Set(0f, 1f);
            list.Height.Set(0f, 1f);
            list.ListPadding = 5f;
            list.ManualSortMethod = (List<UIElement> elements) => { };
            list.FilterMethod = (UIElement el) => {
                if (_searchString == null || _searchString == "")
                    return true;

                if (el is ISearchableUIElement searchableElement) {
                    foreach (string txt in searchableElement.SearchStrings) {
                        if (txt.Contains(_searchString)) {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            };
            topicPanel.Append(list);

            UIScrollbar _scrollbar = new UIScrollbar();
            _scrollbar.SetView(100f, 1000f);
            _scrollbar.Height.Set(0f, 0.98f);
            _scrollbar.HAlign = 1.03f;
            _scrollbar.VAlign = 0.51f;
            list.SetScrollbar(_scrollbar);
            topicPanel.Append(_scrollbar);
        }
		private void IntroductionSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(MakeText("Terraria Automations is for automating simple tasks such as fishing and using items on an extractinator."));
            list.Add(MakeText("Content added by Terraria Automations:"));
            list.Add(MakeTextButton(AutoExtractinatorsTopicName, (_, _) => DisplayTopic(ProcessingSectionName, AutoExtractinatorsTopicName)));
            list.Add(MakeTextButton(AutoFisherTopicName, (_, _) => DisplayTopic(FishingSectionName, AutoFisherTopicName)));
            list.Add(MakeTextButton(BlockBreakersTopicName, (_, _) => DisplayTopic(BlockInteractionSectionName, BlockBreakersTopicName)));
            list.Add(MakeTextButton(BlockPlacersTopicName, (_, _) => DisplayTopic(BlockInteractionSectionName, BlockPlacersTopicName)));
            list.Add(MakeTextButton(PipesTopicName, (_, _) => DisplayTopic(StorageNetworksSectionName, PipesTopicName)));
            list.Add(MakeTextButton(ChestIndicatorsTopicName, (_, _) => DisplayTopic(GeneralSectionName, ChestIndicatorsTopicName)));
        }
		private void ChestIndicatorsSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);
            list.Add(MakeText("Chests connected to automation machines will display fill indicators above them."));
            list.Add(GetScreenshotImage("ChestIndicators"));
            list.Add(MakeText("The right indicator represents how many spaces in the chest have items.  Green is empty.  Red is full.\r\nThe left indicator represents how full the stacks of items are in the chest. Red is full."));
            list.Add(MakeNewLine());

            list.Add(GetScreenshotImage("ChestInventoryNoOpenSpaces"));
            list.Add(MakeText("Example of a chest filled with items."));
            list.Add(MakeNewLine());

            list.Add(GetScreenshotImage("ChestInventoryCompletelyFull"));
            list.Add(MakeText("Example of a chest with all slots completely filled."));
        }
        private void AutoExtractinatorSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(MakeTitle("General"));
            list.Add(GetScreenshotImage("AutoExtractinators"));
            list.Add(MakeText("Terraria Automations modifies the vanilla extractinator, and adds 3 new extractinator tiers.\nRight clicking on an Extractinator will open it's internal inventory (which is just a chest).  Place items in the chest for it to auto-extract them.  Extracted items are pushed to chests on either side of the Extractinator, or spawned if no chests are present or they are full."));
            list.Add(GetScreenshotImage("AutoExtractinatorInventory"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Wood Extractinator"));
            list.Add(GetScreenshotImage("Wood AutoExtractinator"));
            list.Add(GetScreenshotImage("WoodExtractinatorCrafting"));
            list.Add(MakeText("1 extraction every 10 seconds"));
            list.Add(MakeText("Manual use 5x slower than the vanilla Extractinator"));
            list.Add(MakeText("10% chance to receive no item"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Extractinator (Vanilla)"));
            list.Add(GetScreenshotImage("Extractinator"));
            list.Add(GetScreenshotImage("ExtractinatorCrafting"));
            list.Add(MakeText("1 extraction every second"));
            list.Add(MakeText("Manual use (no change, 1x speed)"));
            list.Add(MakeText("Vanilla drop behavior"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Hellstone Auto Extractinator"));
            list.Add(GetScreenshotImage("Hell AutoExtractinator"));
            list.Add(GetScreenshotImage("HellExtractinatorCrafting"));
            list.Add(MakeText("2 extractions every second"));
            list.Add(MakeText("Manual use speed insteased to 1.67x"));
            list.Add(MakeText("10% chance to double extracted item stack size"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Chlorophyte Extractinator (Vanilla)"));
            list.Add(GetScreenshotImage("Chlorophyte Extractinator"));
            list.Add(GetScreenshotImage("ChlorophyteExtractinatorCrafting"));
            list.Add(MakeText("4 extractions every second"));
            list.Add(MakeText("Manual use (no change, 3x speed)"));
            list.Add(MakeText("20% chance to double extracted item stack size"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Luminite Auto Extractinator"));
            list.Add(GetScreenshotImage("Luminite AutoExtractinator"));
            list.Add(GetScreenshotImage("LuminiteExtractinatorCrafting"));
            list.Add(MakeText("10 extractions every second"));
            list.Add(MakeText("Manual use speed increased to 5x"));
            list.Add(MakeText("30% chance to double extracted item stack size"));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Drop Chance"));
            list.Add(MakeText("If a mod uses my TerrariaAutomations.ExtractTypeSet to add extractinator loot, the chance of getting more desireable loot is affected: (Currently doesn't support vanilla drop tables)\r\n\t\t\t\t\tWood: reduced by 0.65x\r\n\t\t\t\t\tHellstone: 1.35x\r\n\t\t\t\t\tVanilla Chlorophyte: 1.7x\r\n\t\t\t\t\tLuminite: 2.05x"));
        }
        private void AutoFisherSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            //Description
            list.Add(MakeTitle("Description"));
			list.Add(MakeText("An auto fisher can be constructed using at a minimum, a fishing rod, a Mannequin and fishing bait.  It will continuously try to catch fish until it runs out of bait."));
            list.Add(GetScreenshotImage("AutoFisher"));
			list.Add(MakeNewLine());

            //Setup
            list.Add(MakeTitle("Setup"));
			list.Add(MakeText("Place a Mannequin next to a body of water about the same place you would stand if fishing yourself.  Right click on the Mannequin to open it's inventory.  Place the fishing pole and bait into the lower slots.  That's it!  As soon as you close the Mannequin's inventory, it will start fishing.  You can optionally equip the Mannequin with fishing gear like the Angler's outfit or fishing accessories.  They will provide the same bonuses as when worn by a player."));
            list.Add(GetScreenshotImage("AutoFisherInventory"));
            list.Add(MakeNewLine());

            //Storge
            list.Add(MakeTitle("Storage"));
			list.Add(MakeText("You may notice that the fisher just flings it's drop onto the ground.  Instead, try placing a chest directly behind the Mannequin and it's drops will be placed in the chest instead."));
            list.Add(GetScreenshotImage("AutoFisherChest"));
            list.Add(MakeTextButton("What are the dots for?", (_, _) => DisplayTopic(GeneralSectionName, ChestIndicatorsTopicName)));
            list.Add(MakeText("If you are so inclined, you can instead connect a pipe to the Mannequin and it will deposit into your storage network instead."));
			list.Add(GetScreenshotImage("AutoFisherNetwork"));
            list.Add(MakeTextButton("What's a storage network?", (_, _) => DisplayTopic(StorageNetworksSectionName, PipesTopicName)));
            list.Add(MakeNewLine());
        }
        private void BlockBreakersSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(GetScreenshotImage("BlockBreakers"));
            list.Add(MakeText("Block Breakers are simple machines that break the block directly in front of them."));
            list.Add(MakeText("Ok, but how is that useful?  Well, with only vanilla content, it's not very useful.  It's most useful for setups where a block will repeatedly form in the same spot.  In my Enguaged Skyblock mod, many blocks are created in new ways such as a stone or silt generator, making block breakers much more useful."));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Crafting"));
            list.Add(GetScreenshotImage("WoodBreakerCrafting"));
            list.Add(GetScreenshotImage("IronBreakerCrafting"));
            list.Add(MakeText("All Breakers are crafted from 8 bars and 50 stone blocks. (except wood which is wood instead of bars.)"));
        }
        private void BlockPlacersSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(GetScreenshotImage("BlockPlacers"));
            list.Add(MakeText("Block Placers are simple machines that place a block directly in front of them from a connected inventory."));
            list.Add(MakeText("Ok, but how is that useful?  Well, with only vanilla content, it's not very useful.  It's most useful for setups where a block will change type when placed in certain conditions.  In my Enguaged Skyblock mod, many blocks are created in new ways such as sand touching lava turning it to sandstone making block placers much more useful."));
            list.Add(MakeNewLine());

            list.Add(MakeTitle("Crafting"));
            list.Add(GetScreenshotImage("WoodPlacerCrafting"));
            list.Add(GetScreenshotImage("IronPlacerCrafting"));
            list.Add(MakeText("All Placers are crafted from 8 bars and 50 stone blocks. (except wood which is wood instead of bars.)"));
        }
        private void PipesSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(MakeText("Pipes are similar to vanilla wire.  They are placed with a Pipe Wrench (Right click with a Pipe Wrench to remove.)"));
            list.Add(MakeText("Pipes connect chests and automation tiles together to form a storage network.  Automations tiles will use chests in the network for storing /using items."));
            list.Add(MakeText("Pipes are compatible with vanilla Junction Boxes."));
            list.Add(MakeText("Pipes and the Pipe Wrench are sold by the Mechanic.  Pipes can also be crafted from iron / lead bars."));
            list.Add(GetScreenshotImage("AutoFisherNetwork"));
        }
        private static Asset<Texture2D> GetVanillaItemTexture(int itemID) {
            Main.instance.LoadItem(itemID);
            return TextureAssets.Item[itemID];
        }
        private static Asset<Texture2D> GetModItemTexture(int itemID) {
            return ModContent.Request<Texture2D>(
                ItemLoader.GetItem(itemID).Texture
            );
        }
        private static SearchableUIText MakeTitle(string title) => new SearchableUIText(title, 1f, true);
		private static SearchableUIText MakeText(string text) => new SearchableUIText(text);
        private static SearchableUIText MakeTextButton(string text, Action<UIMouseEvent, UIElement> onClick) {
            SearchableUIText panel = new(text);
			panel.OnLeftClick += (me, e) => onClick(me, e);
			panel.TextColor = new(255, 92, 58);
			return panel;
		}
		private static UIText MakeNewLine() => new UIText("");
		public static UIImage GetScreenshotImage(string screenshotName) {
			Asset<Texture2D> asset = ModContent.Request<Texture2D>($"TerrariaAutomations/Content/Guidebook/Screenshots/{screenshotName}", AssetRequestMode.ImmediateLoad);
			UIImage image = new UIImage(asset);
			return image;
		}
	}
}
