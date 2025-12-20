using androLib;
using androLib.Common.Utility;
using androLib.UI.GuideBook;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
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
		protected override List<(Asset<Texture2D> asset, List<(Asset<Texture2D> asset, Action<UIPanel> func)> topics)> sections {
			get {
				if (_sections == null)
					SetupSections();

				return _sections;
			}
		}
		private List<(Asset<Texture2D> asset, List<(Asset<Texture2D> asset, Action<UIPanel> func)> topics)> _sections = null;
		private void SetupSections() {
			_sections = new();
			AddAutoFisherSection();
			AddAutoFisherSection();
		}

		private void AddAutoFisherSection() {
			Main.instance.LoadItem(ItemID.FiberglassFishingPole);
			Asset<Texture2D> autoFisherButtonIcon = TextureAssets.Item[ItemID.FiberglassFishingPole];
			Main.instance.LoadItem(ItemID.Mannequin);
			Asset<Texture2D> autoFisherSetupButtonIcon = TextureAssets.Item[ItemID.Mannequin];
			List<(Asset<Texture2D> asset, Action<UIPanel> func)> topics = new() {
				(autoFisherSetupButtonIcon, AutoFisherSetupTopic),
				(autoFisherSetupButtonIcon, AutoFisherSetupTopic),
				(ModIcon, AutoFisherSetupTopic),
			};

			_sections.Add((autoFisherButtonIcon, topics));
		}
		private void AutoFisherSetupTopic(UIPanel topicPanel) {
			AM_UIList list = new();
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
			AutoSizingUIElement el = new();
			SearchableUIText t = new SearchableUIText(
				"An Auto Fisher can be set up by equipping a Mannequin/Wommanequin with a fishing pole and bait.\n" +
				"8 new slots have been added.  The first slot is the fishing pole slot.  The other 7 are for bait.");
			//t.Recalculate();
			//t.Height.Pixels = t.Text.MeasureString().Y;
			SearchableUIText t2 = new("Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string," +
				"Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string, Testing very long string");
			//t2.Width.Set(0f, 1f);
			CalculatedStyle d2 = t2.GetDimensions();
			CalculatedStyle d3 = t2.GetInnerDimensions();
			CalculatedStyle d4 = t2.GetOuterDimensions();
			//t2.IsWrapped = true;
			//t2.WrappedTextBottomPadding = 0f;
			//t2.SetPadding(0f);
			float height3 = t2.Height.Pixels;
			CalculatedStyle d = t2.GetDimensions();
			CalculatedStyle d1 = t2.GetInnerDimensions();
			CalculatedStyle d5 = t2.GetOuterDimensions();
			//t2.Height.Pixels = t2.Text.MeasureString().Y;
			list.Add(t2);
			el.Add(t);
			el.Add(new SearchableUIText("Next TestLine"));
			list.Add(el);
			//topicPanel.Append(t2);
			for (int i = 0; i < 100; i++) {
				SearchableUIText uIText = new($"Testing text {i}.");
				float height = uIText.Height.Pixels;
				list.Add(uIText);
				float height2 = uIText.Height.Pixels;
			}

			UIScrollbar _scrollbar = new UIScrollbar();
			_scrollbar.SetView(100f, 1000f);
			_scrollbar.Height.Set(0f, 1f);
			_scrollbar.HAlign = 1f;
			list.SetScrollbar(_scrollbar);
			topicPanel.Append(_scrollbar);
			//topicPanel.Append(uIText);
		}
	}
}
