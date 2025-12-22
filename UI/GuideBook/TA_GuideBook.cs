using androLib;
using androLib.Common.Utility;
using androLib.UI.GuideBook;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

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

        private const string FishingSectionName = "Fishing";
        private const string AutoFisherTopicName = "Auto Fisher";

        private const string StorageNetworksSectionName = "Storage Networks";
        private const string PipesTopicName = "Pipes";
        private void SetupSections() {
			_sections = [
                new(GeneralSectionName, GetVanillaItemTexture(ItemID.Wrench), [
                    new(IntroductionTopicName, GetVanillaItemTexture(ItemID.WireKite), IntroductionSetupTopic),
                    new(ChestIndicatorsTopicName, GetVanillaItemTexture(ItemID.Mannequin), ChestIndicatorsSetupTopic),
                ]),
                new(FishingSectionName, GetVanillaItemTexture(ItemID.FiberglassFishingPole), [
					new(AutoFisherTopicName, GetVanillaItemTexture(ItemID.Mannequin), AutoFisherSetupTopic),
				]),
                new(StorageNetworksSectionName, GetVanillaItemTexture(ItemID.Chest), [
                    new(PipesTopicName, GetVanillaItemTexture(ItemID.Wire), PipesSetupTopic),
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
        }
		private void ChestIndicatorsSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);
            list.Add(GetScreenshotImage("ChestInventoryNoOpenSpaces"));
            list.Add(GetScreenshotImage("ChestInventoryCompletelyFull"));
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
        private void PipesSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);
        }
        private static Asset<Texture2D> GetVanillaItemTexture(int itemID) {
            Main.instance.LoadItem(itemID);
            return TextureAssets.Item[itemID];
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
