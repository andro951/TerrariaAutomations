using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TerrariaAutomations.Common.Globals;
using static TerrariaAutomations.TA_Mod;

namespace TerrariaAutomations {
	internal class TA_ModPlayer : ModPlayer {
		public override void OnEnterWorld() {
			AutoFisherTE.SendAllAutoFisherTEsRequest();
		}
	}
}
