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
    public abstract class AutoExtractinatorTE : ExtractorBaseTE {
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
    }
}