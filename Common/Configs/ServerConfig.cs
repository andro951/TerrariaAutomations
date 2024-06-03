using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Terraria.ID;
using androLib.Common.Globals;
using androLib.Common.Utility;
using Terraria;

namespace TerrariaAutomations.Common.Configs
{
	//public class TA_ServerConfig : ModConfig {
	//	public override ConfigScope Mode => ConfigScope.ServerSide;

		
	//}

	public class TA_ClientConfig : ModConfig {
		public const string ClientConfigName = "TerrariaAutomationsClientConfig";
		public override ConfigScope Mode => ConfigScope.ClientSide;

		//Display Settings
		[JsonIgnore]
		public const string DisplaySettingsKey = "DisplaySettings";
		[Header($"$Mods.{TA_Mod.ModName}.{L_ID_Tags.Configs}.{ClientConfigName}.{DisplaySettingsKey}")]

		[DefaultValue(true)]
		public bool DisplayChestIndicators;

		//[DefaultValue(100)]
		//[Range(0, (int)byte.MaxValue)]
		//public int UITransparency;

	}
}
