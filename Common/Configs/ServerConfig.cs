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
	public class TA_ServerConfig : ModConfig {
		public const string ClientConfigName = "TerrariaAutomationsServerConfig";
		public override ConfigScope Mode => ConfigScope.ServerSide;

		[JsonIgnore]
		public const string AutomationTileSettings = "AutomationTileSettings";
		[Header($"$Mods.{TA_Mod.ModName}.{L_ID_Tags.Configs}.{ClientConfigName}.{AutomationTileSettings}")]

		[DefaultListValue(true)]
		[ReloadRequired]
		public bool BlockPlacersAndBreakersSolidTiles;
	}

	public class TA_ClientConfig : ModConfig {
		public const string ClientConfigName = "TerrariaAutomationsClientConfig";
		public override ConfigScope Mode => ConfigScope.ClientSide;

		//Display Settings
		[JsonIgnore]
		public const string DisplaySettingsKey = "DisplaySettings";
		[Header($"$Mods.{TA_Mod.ModName}.{L_ID_Tags.Configs}.{ClientConfigName}.{DisplaySettingsKey}")]

		[DefaultValue(true)]
		public bool DisplayChestIndicators;

		//Testing Settings
		[JsonIgnore]
		public const string TestingSettingsKey = "TestingSettings";
		[Header($"$Mods.{TA_Mod.ModName}.{L_ID_Tags.Configs}.{ClientConfigName}.{TestingSettingsKey}")]

		[DefaultValue(false)]
		public bool ShowPipeAndStorageColors;
	}
}
