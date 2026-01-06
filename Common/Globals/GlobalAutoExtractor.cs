using TerrariaAutomations.Tiles.TileEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Reflection;
using Terraria.GameContent.Drawing;
using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Terraria.ObjectData;
using androLib.Common.Utility;
using Terraria.Map;
using TerrariaAutomations.Common.Globals;
using Microsoft.Xna.Framework;
using TerrariaAutomations.Tiles;

namespace TerrariaAutomations.Common.Globals {
	public class GlobalAutoExtractor : GlobalExtractorBase {
		public static GlobalAutoExtractor Instance;
		public static bool IsExtractinatorTile(int tileType) => extractinatorTileTypes.Contains(tileType);
		private static HashSet<int> extractinatorTileTypes = [];
        public static int GetTier(int extractinatorBlockType) {
			if (TileLoader.GetTile(extractinatorBlockType) is AutoExtractorTile autoExtractorTile)
				return autoExtractorTile.Tier;

			if (extractinatorBlockType == TileID.Extractinator)
				return 1;

			if (extractinatorBlockType == TileID.ChlorophyteExtractinator)
				return 3;

			return 0;
		}
        public override void Load() {
            Instance = this;
        }
        public static void PostSetupContent() {
			List<int> vanillaExtractors = new() {
				TileID.Extractinator,
				TileID.ChlorophyteExtractinator
			};

			foreach (int type in vanillaExtractors) {
				Main.tileContainer[type] = true;
				TileID.Sets.BasicChest[type] = true;
				TileID.Sets.IsAContainer[type] = true;
			}

			extractinatorTileTypes.Add(AutoExtractinatorTE.T1);
            extractinatorTileTypes.Add(TileID.Extractinator);
            extractinatorTileTypes.Add(AutoExtractinatorTE.T3);
            extractinatorTileTypes.Add(TileID.ChlorophyteExtractinator);
            extractinatorTileTypes.Add(AutoExtractinatorTE.T5);

            AddExtractorBaseTEGetter(AutoExtractinatorTE.T1, ModContent.GetInstance<WoodAutoExtractinatorTE>);
            AddExtractorBaseTEGetter(TileID.Extractinator, ModContent.GetInstance<VanillaAutoExtractinatorTE>);
            AddExtractorBaseTEGetter(AutoExtractinatorTE.T3, ModContent.GetInstance<HellstoneAutoExtractinatorTE>);
            AddExtractorBaseTEGetter(TileID.ChlorophyteExtractinator, ModContent.GetInstance<ChlorophyteAutoExtractinatorTE>);
            AddExtractorBaseTEGetter(AutoExtractinatorTE.T5, ModContent.GetInstance<LuminiteAutoExtractinatorTE>);
			
			Vector2 offset = new(18f, 10f);
			Vector2 chlorophyteOffset = new(0f, 16f);
            AddChestIndicatorOffset(AutoExtractinatorTE.T1, offset);
            AddChestIndicatorOffset(TileID.Extractinator, offset);
            AddChestIndicatorOffset(AutoExtractinatorTE.T3, offset);
            AddChestIndicatorOffset(TileID.ChlorophyteExtractinator, chlorophyteOffset);
            AddChestIndicatorOffset(AutoExtractinatorTE.T5, chlorophyteOffset);
        }
	}
}