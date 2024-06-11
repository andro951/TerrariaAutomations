using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaAutomations.Common.Globals;
using static TerrariaAutomations.TA_Mod;

namespace TerrariaAutomations {
	internal class TA_ModPlayer : ModPlayer {
		public static int PipesBrightness = PipesBrightnessDefault;
		private const int PipesBrightnessDefault = 0;
		private const string PipesBrightnessKey = "PipesBrightness";
		public override void SaveData(TagCompound tag) {
			tag[PipesBrightnessKey] = PipesBrightness;
		}
		public override void LoadData(TagCompound tag) {
			if (!tag.TryGet(PipesBrightnessKey, out PipesBrightness))
				PipesBrightness = PipesBrightnessDefault;
		}
	}
}
