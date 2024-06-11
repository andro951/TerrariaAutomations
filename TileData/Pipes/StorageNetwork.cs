using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria;
using androLib;
using TerrariaAutomations.Common.Globals;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.IO;
using System.Net;
using Terraria.GameContent.Creative;

namespace TerrariaAutomations.TileData.Pipes {
	public class StorageNetwork {
		private static int NextNetworkKey = 0;
		public static bool TestingNetworks => true;
		private static int GetNewNetworkKey() {
			int originalKey = NextNetworkKey;
			do {
				int key = NextNetworkKey++;
				if (!AllStorageNetworks.ContainsKey(key))
					return key;

			} while (NextNetworkKey != originalKey);

			throw new Exception("Falied to find an available key in AllStorageNetworks.");
		}
		public static SortedDictionary<int, StorageNetwork> AllStorageNetworks = new();
		private static DictionaryGrid<List<PreviousStorageRequest>> PreviousStorageRequests = new();
		public DictionaryGrid<PipeTypeID> Pipes = new();
		public DictionaryGrid<int> StoragesLocations = new();//int value is the Storages dict key.
		private DictionaryGrid<JunctionBoxInfo> JunctionBoxes = new();
		private SortedDictionary<int, StorageInfo> Storages = new();
		private int NextStorageKey = 0;
		public Color testingColor = new(Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());
		public StorageNetwork(int x, int y, int junctionBoxType = -1, int junctionBoxPipeGroup = -1) {
			AddPipe(x, y, junctionBoxType, junctionBoxPipeGroup);
			CheckForStorages(x, y);
		}
		public StorageNetwork(DictionaryGrid<PipeTypeID> pipes, DictionaryGrid<JunctionBoxInfo> junctionBoxes, List<StorageInfo> storageInfos) {
			Pipes = pipes;
			JunctionBoxes = junctionBoxes;
			foreach (StorageInfo storageInfo in storageInfos) {
				if (IsTouching(storageInfo)) {
					AddStorage(storageInfo);
				}
			}
		}
		private int GetNewStorageKey() {
			int originalKey = NextStorageKey;
			do {
				int key = NextStorageKey++;
				if (!Storages.ContainsKey(key))
					return key;

			} while (NextStorageKey != originalKey);

			throw new Exception("Falied to find an available key in Storages.");
		}
		private static bool IsTouchingAnyStorageNetwork(StorageInfo storageInfo, out List<StorageNetwork> touchingNetworks) {
			touchingNetworks = new();
			foreach (StorageNetwork storageNetwork in AllStorageNetworks.Values) {
				if (storageNetwork.IsTouching(storageInfo)) {
					touchingNetworks.Add(storageNetwork);
				}
			}

			return touchingNetworks.Count > 0;
		}
		private bool IsTouching(StorageInfo storageInfo) {
			foreach (KeyValuePair<int, SortedSet<int>> p in storageInfo.TileLocations) {
				int x = p.Key;
				foreach (int y in p.Value) {
					if (IsTouching(x, y, out PipeTypeID pipeType)) {
						//Checks for pipes if needed.
						return true;
					}
				}
			}

			return false;
		}
		/// <param name="directionID">The direction moved to get to x, y</param>
		private bool IsTouching(int x, int y, out PipeTypeID pipeType) {
			if (Pipes.TryGetValue(x, y, out pipeType)) {
				//Checks for pipes if needed.
				return true;
			}

			pipeType = PipeTypeID.None;
			return false;
		}
		public static void PlaceStorageTile(int tileX, int tileY, Func<int, int, IList<Item>> inventoryFunc, Func<int, int, bool> canUse = null, StorageType storageType = StorageType.General) {
			StorageInfo storageInfo = new(inventoryFunc, tileX, tileY, canUse, storageType);
			if (IsTouchingAnyStorageNetwork(storageInfo, out List<StorageNetwork> touchingNetworks)) {
				foreach (StorageNetwork storageNetwork in touchingNetworks) {
					storageNetwork.AddStorage(storageInfo);
				}
			}
		}
		public static void TryRemoveStorage(int tileX, int tileY) {
			foreach (StorageNetwork storageNetwork in AllStorageNetworks.Values) {
				storageNetwork.TryRemovingStroage(tileX, tileY);
			}
		}
		private bool TryRemovingStroage(int tileX, int tileY) {
			if (StoragesLocations.TryGetValue(tileX, tileY, out int storageKey)) {
				StorageInfo storageInfo = Storages[storageKey];
				if (!IsTouching(storageInfo)) {
					foreach (KeyValuePair<int, SortedSet<int>> xSet in storageInfo.TileLocations) {
						foreach (int storageY in xSet.Value) {
							StoragesLocations.TryRemove(xSet.Key, storageY);
						}
					}

					Storages.Remove(storageKey);
					return true;
				}
			}

			return false;
		}
		private bool ContainsStorage(StorageInfo storageInfo) {
			foreach (StorageInfo info in Storages.Values) {
				if (info.Equals(storageInfo))
					return true;
			}

			return false;
		}
		private void AddStorage(StorageInfo storageInfo) {
			if (ContainsStorage(storageInfo))
				return;

			int storageKey = GetNewStorageKey();
			Storages.Add(storageKey, storageInfo);
			foreach (KeyValuePair<int, SortedSet<int>> x in storageInfo.TileLocations) {
				foreach (int y in x.Value) {
					if (!StoragesLocations.TryAdd(x.Key, y, storageKey)) {
						throw new Exception($"StorageLocations already contains ({x.Key}, {y}) {storageKey}");
					}
				}
			}
		}
		private struct PreviousStorageRequest {
			public int NetworkKey;
			public Point16 PipeLocaion;
			public PreviousStorageRequest(int networkKey, Point16 pipeLocaion) {
				NetworkKey = networkKey;
				PipeLocaion = pipeLocaion;
			}
		}
		private static void CheckAddStoragesToFoundListIgnoreDuplicates(List<StorageInfo> storages, IEnumerable<StorageInfo> storagesFromPipe) {
			foreach (StorageInfo storageInfo in storagesFromPipe) {
				bool found = false;
				foreach (StorageInfo info in storages) {
					if (info.Equals(storageInfo)) {
						found = true;
						break;
					}
				}

				if (!found)
					storages.Add(storageInfo);
			}
		}
		/// <summary>
		/// Only Call this once per multitile.  It will check pipes connectting to every tile of the multitile.
		/// </summary>
		public static bool TryGetStorageInventories(int tileX, int tileY, out List<StorageInfo> storageInventories) {
			List<StorageInfo> storages = new();

			//Check previous storage requests
			bool checkAll = true;
			if (PreviousStorageRequests.TryGetValue(tileX, tileY, out List<PreviousStorageRequest> previousRequests)) {
				//Check if no new pipes on the multitile
				bool foundAllPipes = true;
				foreach (Point16 p in AndroUtilityMethods.MultiTileTiles(tileX, tileY)) {
					Tile tile = Main.tile[p.X, p.Y];
					if (!tile.HasPipe())
						continue;

					bool found = false;
					foreach (PreviousStorageRequest previousRequest in previousRequests) {
						if (previousRequest.PipeLocaion == p) {
							found = true;
							break;
						}
					}

					if (!found) {
						foundAllPipes = false;
						break;
					}
				}

				//If no new pipes, use the previous request info.
				if (foundAllPipes) {
					checkAll = false;
					foreach (PreviousStorageRequest previousRequest in previousRequests) {
						if (!AllStorageNetworks.TryGetValue(previousRequest.NetworkKey, out StorageNetwork previousNetwork)) {
							checkAll = true;
							break;
						}

						if (previousNetwork.ConnectedToTileGetStoages(previousRequest.PipeLocaion.X, previousRequest.PipeLocaion.Y, out IEnumerable<StorageInfo> storagesFromPipe)) {
							CheckAddStoragesToFoundListIgnoreDuplicates(storages, storagesFromPipe);
						}
					}
				}
				else {
					PreviousStorageRequests.TryRemove(tileX, tileY);
				}
			}

			//Check all storage networks
			if (checkAll) {
				SortedSet<int> networksChecked = new();
				List<PreviousStorageRequest> requests = new List<PreviousStorageRequest>();
				foreach (Point16 p in AndroUtilityMethods.MultiTileTiles(tileX, tileY)) {
					Tile tile = Main.tile[p.X, p.Y];
					if (!tile.HasPipe())
						continue;

					foreach (KeyValuePair<int, StorageNetwork> storageNetwork in AllStorageNetworks) {
						if (networksChecked.Contains(storageNetwork.Key))
							continue;

						if (storageNetwork.Value.ConnectedToTileGetStoages(p.X, p.Y, out IEnumerable<StorageInfo> storagesFromPipe)) {
							networksChecked.Add(storageNetwork.Key);
							CheckAddStoragesToFoundListIgnoreDuplicates(storages, storagesFromPipe);
							requests.Add(new PreviousStorageRequest(storageNetwork.Key, p));
						}
					}
				}

				if (requests.Count > 0)
					PreviousStorageRequests.AddOrSet(tileX, tileY, requests);
			}

			Vector2 requestLocaiton = new Vector2(tileX, tileY);
			storageInventories = storages.Where(s => s.CanUse).OrderBy(s => s.Distance(requestLocaiton)).ToList();

			return storageInventories.Count > 0;
		}
		private bool ConnectedToTileGetStoages(int pipeTileX, int pipeTileY, out IEnumerable<StorageInfo> storages) {
			if (Pipes.TryGetValue(pipeTileX, pipeTileY, out _)) {
				storages = Storages.Values;
				return true;
			}

			storages = null;
			return false;
		}
		private void Add(StorageNetwork otherToConsume) {
			foreach ((int x, int y, PipeTypeID value) p in otherToConsume.Pipes) {
				if (!Pipes.TryAdd(p.x, p.y, p.value)) {
					if (otherToConsume.JunctionBoxes.TryGetValue(p.x, p.y, out JunctionBoxInfo otherInfo) && JunctionBoxes.TryGetValue(p.x, p.y, out JunctionBoxInfo info) && (info.PipeGroup == 0 && otherInfo.PipeGroup == 1 || info.PipeGroup == 1 && otherInfo.PipeGroup == 0)) {
						JunctionBoxes.Set(p.x, p.y, new(info.JunctionBoxType, 2));
						otherToConsume.JunctionBoxes.TryRemove(p.x, p.y);
					}
					else {
						$"Pipe already exists in network, and isn't a junction box.".LogSimpleNT();
					}
				}
			}

			foreach (KeyValuePair<int, StorageInfo> p in otherToConsume.Storages) {
				AddStorage(p.Value);
			}

			foreach ((int x, int y, JunctionBoxInfo value) p in otherToConsume.JunctionBoxes) {
				 JunctionBoxes.TryAdd(p.x, p.y, p.value);
			}
		}
		private void AddPipe(int tileX, int tileY, int junctionBoxType = -1, int junctionBoxPipeGroup = -1) {
			Tile tile = Main.tile[tileX, tileY];
			Pipes.TryAdd(tileX, tileY, tile.GetPipeTypeID());
			if (junctionBoxType != -1 && junctionBoxPipeGroup != -1) {
				if (!JunctionBoxes.TryAdd(tileX, tileY, new(junctionBoxType, junctionBoxPipeGroup))) {
					if (JunctionBoxes.TryGetValue(tileX, tileY, out JunctionBoxInfo currentInfo)) {
						if (currentInfo.JunctionBoxType == junctionBoxType) {
							if (currentInfo.PipeGroup == 0 && junctionBoxPipeGroup == 1 || currentInfo.PipeGroup == 1 && junctionBoxPipeGroup == 0) {
								JunctionBoxes.Set(tileX, tileY, new(currentInfo.JunctionBoxType, 2));
							}
						}
					}
				}
			}
		}
		private void RemovePipe(int tileX, int tileY) {
			Pipes.TryRemove(tileX, tileY);
			JunctionBoxes.TryRemove(tileX, tileY);
		}
		public struct JunctionBoxInfo {
			public int JunctionBoxType;
			public int PipeGroup;//either 0 or 1 or 2.  Used to determine which group of 2 pipes are included in the half of the junction box.  If 2, it's both parts in case both networks touch.
								 //The 0 pipeGroup is the left pipe and the pipe that connects to the left pipe through the junction box.
			public JunctionBoxInfo(int type, int pipeGroup) {
				JunctionBoxType = type;
				PipeGroup = pipeGroup;
			}
			public static int GetJunctionBoxPipeGroup(int directionID, int junctionBoxType) {
				switch (junctionBoxType) {
					case -1://Not a junction box
						return -1;
					case 0://left to right and up to down
						return directionID switch {
							PathDirectionID.Left or PathDirectionID.Right => 0,
							_ => 1
						};
					case 1://left to up and down to right
						return directionID switch {
							PathDirectionID.Left or PathDirectionID.Up => 0,
							_ => 1
						};
					case 2://left to down and up to right
						return directionID switch {
							PathDirectionID.Left or PathDirectionID.Down => 0,
							_ => 1
						};
				}

				return -1;
			}
			internal static bool CanPassThroughJunctionBox(int directionID, int toDirection, int junctionBoxType) {
				switch (junctionBoxType) {
					case 0://left to right and up to down
						switch (directionID) {
							case PathDirectionID.Left:
								return toDirection == PathDirectionID.Right;
							case PathDirectionID.Right:
								return toDirection == PathDirectionID.Left;
							case PathDirectionID.Up:
								return toDirection == PathDirectionID.Down;
							case PathDirectionID.Down:
								return toDirection == PathDirectionID.Up;
						}
						break;
					case 1://left to up and down to right
						switch (directionID) {
							case PathDirectionID.Left:
								return toDirection == PathDirectionID.Up;
							case PathDirectionID.Up:
								return toDirection == PathDirectionID.Left;
							case PathDirectionID.Down:
								return toDirection == PathDirectionID.Right;
							case PathDirectionID.Right:
								return toDirection == PathDirectionID.Down;
						}
						break;
					case 2://left to down and up to right
						switch (directionID) {
							case PathDirectionID.Left:
								return toDirection == PathDirectionID.Down;
							case PathDirectionID.Down:
								return toDirection == PathDirectionID.Left;
							case PathDirectionID.Up:
								return toDirection == PathDirectionID.Right;
							case PathDirectionID.Right:
								return toDirection == PathDirectionID.Up;
						}
						break;
				}

				throw new Exception($"Invalid call to CanPassThroughJunctionBox(fromDirectionID: {directionID}, toDirection: {toDirection}, junctionBoxType: {junctionBoxType})");
			}
			internal bool ValidDirectionToEnterJunctionBox(int directionToGetHere) {
				int directionID = PathDirectionID.GetOppositeDirection(directionToGetHere);
				switch (PipeGroup) {
					case 0:
						switch (JunctionBoxType) {
							case 0:
								return directionID switch {
									PathDirectionID.Left or PathDirectionID.Right => true,
									_ => false
								};
							case 1:
								return directionID switch {
									PathDirectionID.Left or PathDirectionID.Up => true,
									_ => false
								};
							case 2:
								return directionID switch {
									PathDirectionID.Left or PathDirectionID.Down => true,
									_ => false
								};
						}
						break;
					case 1:
						switch (JunctionBoxType) {
							case 0:
								return directionID switch {
									PathDirectionID.Up or PathDirectionID.Down => true,
									_ => false
								};
							case 1:
								return directionID switch {
									PathDirectionID.Right or PathDirectionID.Down => true,
									_ => false
								};
							case 2:
								return directionID switch {
									PathDirectionID.Up or PathDirectionID.Right => true,
									_ => false
								};
						}
						break;
					case 2:
						return true;
				}

				throw new Exception($"Invalid fromDirectionID to ValidInputDirection().  JunctionBoxType: {JunctionBoxType}, PipeGroup: {PipeGroup}");
			}
		}
		internal static void OnPlacePipe(int tileX, int tileY) {
			bool junctionBox = Main.tile[tileX, tileY].IsJunctionBox(out int junctionBoxType);
			List<SortedDictionary<int, StorageNetwork>> touchingNetworksList = junctionBox ? [new(), new()] : [new()];
			for (int i = 0; i < 4; i++) {
				if (!PathDirectionID.GetDirectionCheckInWorld(i, tileX, tileY, out int x, out int y) || !Main.tile[x, y].HasPipe())
					continue;

				foreach (KeyValuePair<int, StorageNetwork> storageNetowrk in AllStorageNetworks) {
					if (storageNetowrk.Value.IsTouching(x, y, out _)) {
						if (storageNetowrk.Value.JunctionBoxes.TryGetValue(x, y, out JunctionBoxInfo adjacentPipeJunctionBoxInfo)) {
							if (!adjacentPipeJunctionBoxInfo.ValidDirectionToEnterJunctionBox(i))//i is the direction entering the adjacent junction box, so invert.
								continue;
						}

						int pipeGroup = JunctionBoxInfo.GetJunctionBoxPipeGroup(i, junctionBoxType);//i is the direction on the junction box, so no invert.
						int index = pipeGroup;
						if (index == -1)
							pipeGroup = 0;

						//Check if already contains
						if (touchingNetworksList[pipeGroup].ContainsKey(storageNetowrk.Key))
							continue;

						touchingNetworksList[pipeGroup].Add(storageNetowrk.Key, storageNetowrk.Value);
						break;
					}
				}
			}

			if (touchingNetworksList.Count > 1) {
				bool networksAreTouching = false;
				foreach (KeyValuePair<int, StorageNetwork> storageNetwork0 in touchingNetworksList[0]) {
					if (touchingNetworksList[1].ContainsKey(storageNetwork0.Key)) {
						networksAreTouching = true;
						break;
					}
				}

				if (networksAreTouching) {
					foreach (KeyValuePair<int, StorageNetwork> storageNetwork1 in touchingNetworksList[1]) {
						if (touchingNetworksList[0].ContainsKey(storageNetwork1.Key)) {
							continue;
						}

						touchingNetworksList[0].Add(storageNetwork1.Key, storageNetwork1.Value);
					}

					touchingNetworksList.RemoveAt(1);
					junctionBoxType = 2;
				}
			}

			for (int k = 0; k < touchingNetworksList.Count; k++) {
				SortedDictionary<int, StorageNetwork> touchingNetworks = touchingNetworksList[k];
				StorageNetwork myNetwork;
				if (touchingNetworks.Count == 0) {
					if (junctionBox) {
						myNetwork = new(tileX, tileY, junctionBoxType, k);
					}
					else {
						myNetwork = new(tileX, tileY);
					}

					AllStorageNetworks.Add(GetNewNetworkKey(), myNetwork);
				}
				else {
					//Find largest network
					int key = -1;
					int largestCount = -1;
					foreach (KeyValuePair<int, StorageNetwork> touchingNetwork in touchingNetworks) {
						if (touchingNetwork.Value.Pipes.Count > largestCount) {
							largestCount = touchingNetwork.Value.Pipes.Count;
							key = touchingNetwork.Key;
						}
					}

					//Merge smaller networks into largest network
					myNetwork = touchingNetworks[key];
					foreach (KeyValuePair<int, StorageNetwork> touchingNetwork in touchingNetworks) {
						if (touchingNetwork.Key == key)
							continue;

						myNetwork.Add(touchingNetwork.Value);
						AllStorageNetworks.Remove(touchingNetwork.Key);
					}

					if (junctionBox) {
						myNetwork.AddPipe(tileX, tileY, junctionBoxType, k);
					}
					else {
						myNetwork.AddPipe(tileX, tileY);
					}
				}

				if (!myNetwork.StoragesLocations.Contains(tileX, tileY))
					CheckForStorages(tileX, tileY);
			}
		}
		internal static void OnReplacePipe(int tileX, int tileY) {
			//TODO: this hasn't been updated at all, and is currently not used.  Needs junctions
			Tile tile = Main.tile[tileX, tileY];
			PipeTypeID pipeTypeID = tile.GetPipeTypeID();
			foreach (StorageNetwork storageNetwork in AllStorageNetworks.Values) {
				if (storageNetwork.Pipes.TryGetValue(tileX, tileY, out PipeTypeID oldId)) {
					if (oldId != pipeTypeID)
						storageNetwork.Pipes.Set(tileX, tileY, pipeTypeID);

					return;
				}
			}

			throw new Exception("OnReplacePipe: Failed to find a pipe to replace in the StorageNetwork.Pipes");
		}
		internal static void OnRemovePipe(int tileX, int tileY) {
			List<KeyValuePair<int, StorageNetwork>> myNetworks = AllStorageNetworks.Where(p => p.Value.IsTouching(tileX, tileY, out _)).ToList();
			foreach (KeyValuePair<int, StorageNetwork> myNetwork in myNetworks) {
				int junctionBoxPipeGroup;
				int junctionBoxType;
				if (myNetwork.Value.JunctionBoxes.TryGetValue(tileX, tileY, out JunctionBoxInfo junctionBoxInfo)) {
					junctionBoxPipeGroup = junctionBoxInfo.PipeGroup;
					junctionBoxType = junctionBoxInfo.JunctionBoxType;
				}
				else {
					junctionBoxPipeGroup = -1;
					junctionBoxType = -1;
				}

				List<(Point16, int)> touchingPipes = new();
				for (int i = 0; i < 4; i++) {
					int junctionBoxPipeGroup2 = JunctionBoxInfo.GetJunctionBoxPipeGroup(i, junctionBoxType);//i is the direction on this junction box, so no invert.
					if (junctionBoxPipeGroup2 != junctionBoxPipeGroup && junctionBoxPipeGroup != 2)
						continue;

					if (PathDirectionID.GetDirectionCheckInWorld(i, tileX, tileY, out int x, out int y)) {
						if (myNetwork.Value.IsTouching(x, y, out _)) {
							if (myNetwork.Value.JunctionBoxes.TryGetValue(x, y, out JunctionBoxInfo otherJunctionBoxInfo)) {
								if (!otherJunctionBoxInfo.ValidDirectionToEnterJunctionBox(i))//i is the direction entering the adjacent junction box, so invert.
									continue;
							}

							touchingPipes.Add((new Point16(x, y), i));
						}
					}
				}

				myNetwork.Value.RemovePipe(tileX, tileY);

				//Check remove storages
				myNetwork.Value.TryRemovingStroage(tileX, tileY);

				//Check break apart network
				if (touchingPipes.Count > 1) {
					//Check if need to break apart
					List<(DictionaryGrid<PipeTypeID> pipes, DictionaryGrid<JunctionBoxInfo> junctionBoxes)> touchingPipesNetworks = [];
					for (int i = 0; i < touchingPipes.Count; i++) {
						(Point16 location, int directionID) p = touchingPipes[i];
						DictionaryGrid<PipeTypeID> pipes = [];
						SortedDictionary<int, SortedSet<int>> checkedLocations = [];
						DictionaryGrid<JunctionBoxInfo> junctionBoxes = [];
						FindAllPipes(p.location.X, p.location.Y, pipes, checkedLocations, junctionBoxes, p.directionID);

						if (i == 0) {
							bool containsAll = true;
							foreach ((Point16 location, int directionID) p2 in touchingPipes) {
								if (!pipes.Contains(p2.location.X, p2.location.Y)) {//TODO: test this.  May need to check for junctions.  I think it's taken care of by FindAllPipes()
									containsAll = false;
									break;
								}
							}

							if (containsAll)
								break;
						}

						bool addToNetworksList = i == 0;
						if (!addToNetworksList) {
							addToNetworksList = true;
							foreach ((DictionaryGrid<PipeTypeID> pipes, DictionaryGrid<JunctionBoxInfo> junctionBoxes) n in touchingPipesNetworks) {
								if (n.pipes.Contains(p.location.X, p.location.Y)) {//TODO: test this.  May need to check for junctions.  I think it's taken care of by FindAllPipes()
									addToNetworksList = false;
									break;
								}
							}
						}

						if (addToNetworksList)
							touchingPipesNetworks.Add((pipes, junctionBoxes));
					}

					//Break apart
					if (touchingPipesNetworks.Count > 1) {
						List<StorageInfo> storageInfos = new(myNetwork.Value.Storages.Values);
						AllStorageNetworks.Remove(myNetwork.Key);
						for (int i = 0; i < touchingPipesNetworks.Count; i++) {
							StorageNetwork newNetwork = new(touchingPipesNetworks[i].pipes, touchingPipesNetworks[i].junctionBoxes, storageInfos);
							AllStorageNetworks.Add(GetNewNetworkKey(), newNetwork);
						}
					}
				}
				else if (touchingPipes.Count == 0) {
					//Wasn't touching any pipes, so the network is empty
					if (myNetwork.Value.Pipes.Count > 0)
						$"Network {myNetwork.Key} was removed even though it still had pipes.".LogSimpleNT();

					AllStorageNetworks.Remove(myNetwork.Key);
				}
			}
		}

		/// <summary>
		/// Designed only to be called when a pipe is removed.  This function always needs a fromDirection, or it won't take a junction box on this tile into account.
		/// </summary>
		private static void FindAllPipes(int x, int y, DictionaryGrid<PipeTypeID> pipes, SortedDictionary<int, SortedSet<int>> checkedLocations, DictionaryGrid<JunctionBoxInfo> junctionBoxes, int directionToGetHere) {
			if (checkedLocations.TryGetValue(x, out SortedSet<int> xSet) && xSet.Contains(y))
				return;

			checkedLocations.AddOrCombine(x, y);
			Tile tile = Main.tile[x, y];
			if (!tile.HasPipe())
				return;

			pipes.Add(x, y, tile.GetPipeTypeID());
			int fromDirection = PathDirectionID.GetOppositeDirection(directionToGetHere);
			if (tile.IsJunctionBox(out int junctionBoxType)) {
				int pipeGroup = JunctionBoxInfo.GetJunctionBoxPipeGroup(fromDirection, junctionBoxType);
				JunctionBoxInfo junctionBoxInfo = new(junctionBoxType, pipeGroup);
				junctionBoxes.Add(x, y, junctionBoxInfo);
				for (int i = 0; i < 4; i++) {
					if (i == fromDirection)
						continue;

					if (!JunctionBoxInfo.CanPassThroughJunctionBox(fromDirection, i, junctionBoxType))
						continue;

					if (PathDirectionID.GetDirectionCheckInWorld(i, x, y, out int x2, out int y2))
						FindAllPipes(x2, y2, pipes, checkedLocations, junctionBoxes, i);
				}
			}
			else {
				for (int i = 0; i < 4; i++) {
					if (i == fromDirection)
						continue;

					if (PathDirectionID.GetDirectionCheckInWorld(i, x, y, out int x2, out int y2))
						FindAllPipes(x2, y2, pipes, checkedLocations, junctionBoxes, i);
				}
			}
		}
		public static void OnWorldLoad() {
			StorageNetwork.ClearAll();
			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					Tile tile = Main.tile[x, y];
					if (tile.HasPipe()) {
						OnPlacePipe(x, y);
					}
				}
			}

			for (int i = 0; i < Main.chest.Length; i++) {
				Chest chest = Main.chest[i];
				if (chest == null)
					continue;

				CheckForChest(chest.x, chest.y);
			}
		}

		private static void ClearAll() {
			AllStorageNetworks.Clear();
			PreviousStorageRequests.Clear();
		}
		internal static void PreSaveAndQuit() {
			ClearAll();
		}

		public static void Load() {
			On_Chest.CreateChest += On_Chest_CreateChest;
			On_Chest.AfterPlacement_Hook += On_Chest_AfterPlacement_Hook;
			On_Chest.DestroyChest += On_Chest_DestroyChest;
			On_Chest.DestroyChestDirect += On_Chest_DestroyChestDirect;
			WorldFile.OnWorldLoad += OnWorldLoad;
		}

		private static void On_Chest_DestroyChestDirect(On_Chest.orig_DestroyChestDirect orig, int X, int Y, int id) {
			orig(X, Y, id);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			TryRemoveStorage(X, Y);
		}

		private static bool On_Chest_DestroyChest(On_Chest.orig_DestroyChest orig, int X, int Y) {
			bool result = orig(X, Y);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return result;

			TryRemoveStorage(X, Y);

			return result;
		}

		private static int On_Chest_AfterPlacement_Hook(On_Chest.orig_AfterPlacement_Hook orig, int x, int y, int type, int style, int direction, int alternate) {
			int result = orig(x, y, type, style, direction, alternate);

			if (Main.netMode != NetmodeID.SinglePlayer)
				return result;

			CheckForChest(x, y);
			return result;
		}

		private static int On_Chest_CreateChest(On_Chest.orig_CreateChest orig, int X, int Y, int id) {
			int result = orig(X, Y, id);
			CheckForChestTopLeft(X, Y, result);

			return result;
		}
		private static void CheckForStorages(int x, int y) {
			CheckForChest(x, y);
		}
		private static bool CheckForChest(int x, int y) {
			Point16 topLeft = AndroUtilityMethods.TilePositionToTileTopLeft(x, y);
			int chestNum = Chest.FindChest(topLeft.X, topLeft.Y);
			if (chestNum == -1)
				return false;

			return CheckForChestTopLeft(topLeft.X, topLeft.Y, chestNum);
		}
		private static bool CheckForChestTopLeft(int X, int Y, int chestNum) {
			Tile tile = Main.tile[X, Y];
			if (Main.tileContainer[tile.TileType]) {
				if (GlobalAutoExtractor.IsExtractinator(tile.TileType)) {
					PlaceStorageTile(X, Y, GetVanillaChestInventory, CanUseChest, StorageType.WithdrawlOnlyNoDeposit);
				}
				else {
					PlaceStorageTile(X, Y, GetVanillaChestInventory, CanUseChest);
				}

				return true;
			}

			return false;
		}

		private static IList<Item> GetVanillaChestInventory(int x, int y) {
			if (AndroUtilityMethods.TryGetChest(x, y, out int chestNum))
				return Main.chest[chestNum]?.item;

			return null;
		}
		private static bool CanUseChest(int x, int y) {
			if (AndroUtilityMethods.TryGetChest(x, y, out int chestNum))
				return Main.netMode == NetmodeID.SinglePlayer || Chest.UsingChest(chestNum) == -1;

			return false;
		}
		internal static void OnPlaceJunctionBox(int i, int j) {
			if (!Main.tile[i, j].HasPipe())
				return;

			OnRemovePipe(i, j);
			OnPlacePipe(i, j);
		}
		internal static void OnSlopeJunctionBox(int i, int j) {
			if (!Main.tile[i, j].HasPipe())
				return;

			OnRemovePipe(i, j);
			OnPlacePipe(i, j);
		}

		internal static void OnKillJunctionBox(int i, int j) {
			if (!Main.tile[i, j].HasPipe())
				return;

			OnRemovePipe(i, j);
			OnPlacePipe(i, j);
		}
	}
}
