using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria;
using Microsoft.Xna.Framework;

namespace TerrariaAutomations.TileData.Pipes {
	public enum StorageType {
		General,
		DepositOnlyNoWithdrawl,
		WithdrawlOnlyNoDeposit,
	}
	public struct StorageInfo {
		//private static readonly Point16 TopLeftNone = new(short.MaxValue, short.MaxValue);
		private static readonly Point16 BottomRightNone = new(short.MinValue, short.MinValue);
		public SortedDictionary<int, SortedSet<int>> TileLocations = [];
		private readonly Func<int, int, IList<Item>> inventoryFunc;
		private readonly Func<int, int, bool> canUseFunc;
		public readonly IList<Item> Inventory => inventoryFunc?.Invoke(topLeft.X, topLeft.Y);
		public readonly bool CanUse => canUseFunc?.Invoke(topLeft.X, topLeft.Y) ?? true;
		public StorageType StorageType;
		private readonly Point16 topLeft;
		public Vector2 Center;
		public readonly int TileType => Main.tile[topLeft.X, topLeft.Y].TileType;
		public readonly bool CanDepositItemsTo => StorageType switch { StorageType.General or StorageType.DepositOnlyNoWithdrawl => true, _ => false };
		public readonly bool CanWithdrawItemsFrom => StorageType switch { StorageType.General or StorageType.WithdrawlOnlyNoDeposit => true, _ => false };
		public StorageInfo(Func<int, int, IList<Item>> inventoryFunc, int tileX, int tileY, Func<int, int, bool> canUseFunc = null, StorageType storageType = StorageType.General) {
			this.inventoryFunc = inventoryFunc;
			this.canUseFunc = canUseFunc;
			StorageType = storageType;
			Point16 bottomRight = BottomRightNone;
			IEnumerable<Point16> points = AndroUtilityMethods.MultiTileTiles(tileX, tileY);
			topLeft = points.First();//MultiTileTiles always returns the topleft first.
			foreach (Point16 p in points) {
				if (p.X > bottomRight.X)
					bottomRight = new(p.X, bottomRight.Y);

				if (p.Y > bottomRight.Y)
					bottomRight = new(bottomRight.X, p.Y);

				if (TileLocations.TryGetValue(p.X, out SortedSet<int> yx)) {
					yx.Add(p.Y);
				}
				else {
					TileLocations.Add(p.X, [p.Y]);
				}
			}

			Center = new Vector2((topLeft.X + bottomRight.X) / 2f, (topLeft.Y + bottomRight.Y) / 2f);
		}

		public float Distance(Vector2 point) => Vector2.Distance(Center, point);
		public override bool Equals([NotNullWhen(true)] object obj) {
			if (obj == null)
				return false;

			if (obj is not StorageInfo other)
				return false;

			if (Center != other.Center)
				return false;

			if (TileLocations.Count != other.TileLocations.Count)
				return false;

			foreach (KeyValuePair<int, SortedSet<int>> kvp in TileLocations) {
				if (!other.TileLocations.TryGetValue(kvp.Key, out SortedSet<int> value))
					return false;

				if (!kvp.Value.SetEquals(value))
					return false;
			}

			return true;
		}
	}
}
