using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace TerrariaAutomations.Tiles {
	public abstract class TA_ModTile : ModTile {
		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
	}
}
