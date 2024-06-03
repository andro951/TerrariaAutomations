using androLib.Common.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaAutomations.Tiles.Interfaces {
	public interface IUseChestIndicators {
		public virtual bool CheckOnlyLeftRight => true;
		public virtual bool ShouldDisplayChestIndicatorCheckLeftRight(int x, int y, Tile tile) => CheckOnlyLeftRight;
		public virtual bool ShouldDisplayChestIndicatorCheckAll(int x, int y, Tile tile) => !CheckOnlyLeftRight;
		public static bool ShouldDisplayChestIndicatorCheckLeftRight(int x, int y, out IUseChestIndicators iUseChestIndicators) {
			Tile tile = Main.tile[x, y];
			if (!GetTE(x, y, tile.TileType, out iUseChestIndicators))
				return false;

			return iUseChestIndicators.ShouldDisplayChestIndicatorCheckLeftRight(x, y, tile);
		}
		public static bool ShouldDisplayChestIndicatorCheckAll(int x, int y, out IUseChestIndicators iUseChestIndicators) {
			Tile tile = Main.tile[x, y];
			if (!GetTE(x, y, tile.TileType, out iUseChestIndicators))
				return false;

			return iUseChestIndicators.ShouldDisplayChestIndicatorCheckAll(x, y, tile);
		}
		public static bool GetTE(int x, int y, int tileType, out IUseChestIndicators iUseChestIndicators) {
			Point16 tePoint = AndroUtilityMethods.TilePositionToTileTopLeft(x, y);
			if (tileType == TileID.DisplayDoll)
				tePoint = new(tePoint.X + 1, tePoint.Y);

			if (TileEntity.ByPosition.TryGetValue(tePoint, out TileEntity te) && te is IUseChestIndicators foundTE) {
				iUseChestIndicators = foundTE;
				return true;
			}

			iUseChestIndicators = null;
			return false;
		}
	}
}
