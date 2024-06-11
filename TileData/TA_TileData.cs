using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using System.Security.Cryptography;
using androLib.Common.Utility;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.GameContent.UI;
using Terraria.ID;
using System.Reflection;
using Terraria.GameInput;
using static Terraria.GameContent.UI.WiresUI;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.UI.Gamepad;
using System.Collections;
using Newtonsoft.Json.Linq;
using androLib.Tiles.TileData;
using TerrariaAutomations.Items;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using TerrariaAutomations.TileData.Pipes;
using androLib.IO.TerrariaAutomations;

namespace TerrariaAutomations.TileData {
	public static class TA_TileData {
		public static List<Func<IList<Item>>> PipeStorages = new();
		public static void RegisterAdditionalPipeInventory(Func<IList<Item>> storage) {
			PipeStorages.Add(storage);
		}
		private static bool placingPipe = false;
		public static bool PlacePipe(int tileX, int tileY, int itemType) {
			if (!WorldGen.InWorld(tileX, tileY))
				return false;

			Tile tile = Main.tile[tileX, tileY];
			bool pipeRemoved = false;
			if (tile.HasPipe()) {
				if (tile.IsPipeItemType(itemType))
					return false;

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					placingPipe = true;
					pipeRemoved = RemovePipe(tileX, tileY);
					placingPipe = false;
				}
				else {
					RemovePipe(tileX, tileY);
				}
			}

			if (!tile.SetPipeItemType(itemType)) {
				if (pipeRemoved)
					StorageNetwork.OnRemovePipe(tileX, tileY);

				return false;
			}

			int worldX = tileX * 16;
			int worldY = tileY * 16;
			SoundEngine.PlaySound(SoundID.Dig, new Vector2(worldX, worldY));
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				if (pipeRemoved) {
					StorageNetwork.OnReplacePipe(tileX, tileY);
				}
				else {
					StorageNetwork.OnPlacePipe(tileX, tileY);
				}
			}

			return true;
		}
		public static bool RemovePipe(int tileX, int tileY) {
			if (!WorldGen.InWorld(tileX, tileY))
				return false;

			Tile tile = Main.tile[tileX, tileY];
			if (!tile.HasPipe())
				return false;

			int pipeItemType = tile.PipeItemType();
			int worldX = tileX * 16;
			int worldY = tileY * 16;
			SoundEngine.PlaySound(SoundID.Dig, new Vector2(worldX, worldY));
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Item.NewItem(WorldGen.GetItemSource_FromTileBreak(tileX, tileY), worldX, worldY, 16, 16, pipeItemType);

			tile.HasPipe(false);
			if (Main.netMode != NetmodeID.MultiplayerClient && !placingPipe)
				StorageNetwork.OnRemovePipe(tileX, tileY);

			for (int k = 0; k < 5; k++) {
				Dust.NewDust(new Vector2(worldX, worldY), 16, 16, DustID.Adamantite, newColor: new(100, 100, 100));
			}

			return true;
		}

		#region Net

		public enum PipeUpdatesTypeID : byte {
			Place,
			Remove
		}
		internal static void NetUpdatePipe(int tileTargetX, int tileTargetY, PipeUpdatesTypeID updateType, int ignoreClient = -1) {
			ModPacket modPacket = TA_Mod.Instance.GetPacket();
			modPacket.Write((byte)TA_Mod.TA_ModPacketID.UpdatePipe);
			modPacket.Write((byte)updateType);
			modPacket.Write((short)tileTargetX);
			modPacket.Write((short)tileTargetY);
			switch (updateType) {
				case PipeUpdatesTypeID.Place:
					modPacket.Write((short)Main.tile[tileTargetX, tileTargetY].PipeItemType());
					break;
			}

			modPacket.Send(ignoreClient: ignoreClient);
			//$"NetUpdatePipe() tileTargetX: {tileTargetX}, tileTargetY: {tileTargetY}, updateType: {updateType}, ignoreClient: {ignoreClient}".LogSimpleNT();
		}
		internal static void RecievePipeUpdate(BinaryReader reader, int whoAmI) {
			PipeUpdatesTypeID updateType = (PipeUpdatesTypeID)reader.ReadByte();
			int tileX = reader.ReadInt16();
			int tileY = reader.ReadInt16();
			switch (updateType) {
				case PipeUpdatesTypeID.Place:
					int pipeItemType = reader.ReadInt16();
					PlacePipe(tileX, tileY, pipeItemType);
					break;
				case PipeUpdatesTypeID.Remove:
					RemovePipe(tileX, tileY);
					break;
				default:
					throw new Exception($"Received PipeUpdateTypeID: {updateType}.  Not recognized.");
			}

			//$"RecievePipeUpdate() tileX: {tileX}, tileY: {tileY}, updateType: {updateType}, whoAmI: {whoAmI}".LogSimpleNT();

			if (Main.netMode == NetmodeID.Server)
				NetUpdatePipe(tileX, tileY, updateType, whoAmI);
		}

		internal static void RecieveAllPipeData(BinaryReader reader) => TA_WorldFile.RecieveAllPipeData(reader);
		internal static void SendAllPipeData(int plr) {
			ModPacket modPacket = TA_Mod.Instance.GetPacket();
			modPacket.Write((byte)TA_Mod.TA_ModPacketID.SyncAllPipeData);
			TA_WorldFile.WriteAllPipeData(modPacket);
			modPacket.Send(plr);
			//$"SendAllPipeData; plr: {plr}".LogSimpleNT();
		}

		#endregion
	}
	public enum PipeTypeID : byte {
		Basic,


		None = 0b10000000,//128 Even if a pipe is set to None, it will be shifted to the left by 1 to make it 0
	}
	public class TileDataTestingGlobalTile : GlobalTile {
		private static string PipeSpritePath = "TerrariaAutomations/TileData/Pipes/Sprites";
		private static Asset<Texture2D> basicPipeTexture = ModContent.Request<Texture2D>($"{PipeSpritePath}/BasicPipe");
		private static Asset<Texture2D> builderIcons = ModContent.Request<Texture2D>($"{PipeSpritePath}/BuilderIcons");
		public override void Load() {
			On_Main.DrawWires += On_Main_DrawWires;
			On_Main.DrawBuilderAccToggles += BuilderAccButton.On_Main_DrawBuilderAccToggles;
			StorageNetwork.Load();
			On_Player.ItemCheck_UseMiningTools_TryPoundingTile += On_Player_ItemCheck_UseMiningTools_TryPoundingTile;
			On_WorldGen.KillTile += On_WorldGen_KillTile;
		}

		#region BuilderAccButton

		private static void GetBuilderAccsCountToShow(Player plr, out int blockReplaceIcons, out int torchGodIcons, out int totalDrawnIcons) {
			blockReplaceIcons = 1;
			torchGodIcons = (plr.unlockedBiomeTorches ? 1 : 0);
			totalDrawnIcons = plr.InfoAccMechShowWires.ToInt() * 6 + plr.rulerLine.ToInt() + plr.rulerGrid.ToInt() + plr.autoActuator.ToInt() + plr.autoPaint.ToInt() + blockReplaceIcons + torchGodIcons;
		}
		public struct BuilderAccButton {
			public Asset<Texture2D> texture;
			public Action onClick;
			public Func<bool> shouldDraw;
			public Func<string> hoverText;
			private static Rectangle rectangle = new Rectangle(0, 16, 14, 14);
			private static Color defaultColor = Color.White;
			private static Color defaultColor2 = new Color(127, 127, 127);
			public Vector2 vector = Vector2.Zero;
			private static bool totalDrawIcons10orMore = false;
			private bool mouseHovering {
				get {
					if (_mouseHovering == null) {
						_mouseHovering = Utils.CenteredRectangle(vector, new Vector2(14f)).Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;
					}

					return _mouseHovering.Value;
				}
			}
			private bool? _mouseHovering = null;
			private bool mouseClick => mouseHovering && Main.mouseLeft && Main.mouseLeftRelease;
			public BuilderAccButton(Asset<Texture2D> texture, Action onClick, Func<bool> shouldDraw, Func<string> hoverText) {
				this.texture = texture;
				this.onClick = onClick;
				this.shouldDraw = shouldDraw;
				this.hoverText = hoverText;
			}
			public static void On_Main_DrawBuilderAccToggles(On_Main.orig_DrawBuilderAccToggles orig, Main self, Vector2 start) {
				orig(self, start);
				if (!Main.playerInventory)
					return;

				Player player = Main.LocalPlayer;
				if (player.sign >= 0)
					return;

				int num = UILinkPointNavigator.Shortcuts.BUILDERACCCOUNT - 1;
				int[] builderAccStatus = player.builderAccStatus;
				GetBuilderAccsCountToShow(player, out var blockReplaceIcons, out var torchGodIcons, out var totalDrawnIcons);
				start.Y += 24 * torchGodIcons;
				totalDrawIcons10orMore = totalDrawnIcons >= 10;

				for (int i = 0; i < builderAccButtons.Length; i++) {
					BuilderAccButton button = builderAccButtons[i];
					if (!button.shouldDraw())
						continue;

					button.vector = start + new Vector2(0f, num * 24);
					if (totalDrawIcons10orMore)
						button.vector.Y -= 24f;

					button.DefaultDraw(player);

					UILinkPointNavigator.Shortcuts.BUILDERACCCOUNT++;
				}
			}
			public void DefaultDraw(Player player) {
				rectangle.X = 4 * 16;
				Color color = ((TA_ModPlayer.PipesBrightness == 0) ? defaultColor : ((TA_ModPlayer.PipesBrightness == 1) ? defaultColor2 : ((TA_ModPlayer.PipesBrightness == 2) ? defaultColor2.MultiplyRGBA(new Color(0.66f, 0.66f, 0.66f, 0.66f)) : defaultColor2.MultiplyRGBA(new Color(0.33f, 0.33f, 0.33f, 0.33f)))));
				if (mouseHovering) {
					player.mouseInterface = true;
					Main.instance.MouseText(hoverText());
					Main.mouseText = true;
				}

				if (mouseClick) {
					onClick();
					SoundEngine.PlaySound(SoundID.MenuTick);
					Main.mouseLeftRelease = false;
				}

				Main.spriteBatch.Draw(texture.Value, vector, rectangle, color, 0f, rectangle.Size() / 2f, 1f, SpriteEffects.None, 0f);
				if (mouseHovering)
					Main.spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, vector, null, Main.OurFavoriteColor, 0f, TextureAssets.InfoIcon[13].Value.Size() / 2f, 1f, SpriteEffects.None, 0f);
			}
		}
		private static BuilderAccButton[] builderAccButtons = [
			new BuilderAccButton(builderIcons, () => {
				TA_ModPlayer.PipesBrightness++;
				if (TA_ModPlayer.PipesBrightness > 3)
					TA_ModPlayer.PipesBrightness = 0;
			}, () => true, () => {
				string status = "";
					switch (TA_ModPlayer.PipesBrightness) {
						case 0:
							status = Language.GetTextValue("GameUI.Bright");
							break;
						case 1:
							status = Language.GetTextValue("GameUI.Normal");
							break;
						case 2:
							status = Language.GetTextValue("GameUI.Faded");
							break;
						case 3:
							status = Language.GetTextValue("GameUI.Hidden");
							break;
					}

				return $"{"Pipes"} : {status}";
			})
		];

		#endregion

		private void On_Main_DrawWires(On_Main.orig_DrawWires orig, Main self) {
			DrawPipes();
			orig(self);
		}
		private static void DrawPipes() {
			Player player = Main.LocalPlayer;

			Item heldItem = player.HeldItem;
			bool displayPipes = heldItem.ModItem is PipeWrench;
			displayPipes |= player.InfoAccMechShowWires;
			if (!displayPipes) {
				displayPipes = heldItem.type switch {
					ItemID.WirePipe => true,
					_ => false
				};
			}

			if (!displayPipes)
				return;

			Rectangle spriteMap = new Rectangle(0, 0, 16, 16);//1x1
			Vector2 zero = Vector2.Zero;
			//Main.DrawWiresSpecialTiles.Clear();
			bool drawPipes = true;
			int leftTileX = (int)((Main.screenPosition.X) / 16f -1f);
			int rightTileX = (int)((Main.screenPosition.X + (float)Main.screenWidth) / 16f) + 2;
			int topTileY = (int)((Main.screenPosition.Y) / 16f - 1f);
			int bottomTileY = (int)((Main.screenPosition.Y + (float)Main.screenHeight) / 16f) + 5;
			if (leftTileX < 0)
				leftTileX = 0;

			if (rightTileX > Main.maxTilesX)
				rightTileX = Main.maxTilesX;

			if (topTileY < 0)
				topTileY = 0;

			if (bottomTileY > Main.maxTilesY)
				bottomTileY = Main.maxTilesY;

			Point screenOverdrawOffset = Main.GetScreenOverdrawOffset();
			for (int y = topTileY + screenOverdrawOffset.Y; y < bottomTileY - screenOverdrawOffset.Y; y++) {
				for (int x = leftTileX + screenOverdrawOffset.X; x < rightTileX - screenOverdrawOffset.X; x++) {
					//bool redWireLeft = false;
					//bool redWireRight = false;
					//bool redWireAbove = false;
					//bool redWireBelow = false;
					//bool bgyWireLeft;
					//bool bgyWireRight;
					//bool bgyWireAbove;
					//bool bgyWireBelow;
					//bool multipleWiresInConnectingTile;
					//float wireTypeCount = 0f;
					Tile tile = Main.tile[x, y];
					if (drawPipes) {
						int spriteMapYOffset = 0;
						if (tile.HasTile) {
							if (tile.TileType == TileID.WirePipe) {
								switch (tile.TileFrameX / 18) {
									case 0:
										spriteMapYOffset += 72;
										break;
									case 1:
										spriteMapYOffset += 144;
										break;
									case 2:
										spriteMapYOffset += 216;
										break;
								}
							}
							//else if (tile.TileType == TileID.PixelBox) {
							//	spriteMapYOffset += 72;
							//}
						}

						if (tile.HasPipe()) {
							//wireTypeCount += 1f;
							int redSpriteSheetX = 0;
							if (Main.tile[x, y - 1].HasPipe()) {
								redSpriteSheetX += 18;
								//redWireAbove = true;
							}

							if (Main.tile[x + 1, y].HasPipe()) {
								redSpriteSheetX += 36;
								//redWireRight = true;
							}

							if (Main.tile[x, y + 1].HasPipe()) {
								redSpriteSheetX += 72;
								//redWireBelow = true;
							}

							if (Main.tile[x - 1, y].HasPipe()) {
								redSpriteSheetX += 144;
								//redWireLeft = true;
							}

							spriteMap.Y = spriteMapYOffset;
							spriteMap.X = redSpriteSheetX;
							Color color = Lighting.GetColor(x, y);
							if (TA_Mod.clientConfig.ShowPipeAndStorageColors) {
								List<Color> colors = new();
								foreach (StorageNetwork storageNetwork in StorageNetwork.AllStorageNetworks.Values) {
									if (storageNetwork.Pipes.Contains(x, y)) {
										colors.Add(storageNetwork.testingColor);
									}
								}

								Color networkColor = colors.Count > 0 ? colors.BlendColors() : Color.White;

								switch (TA_ModPlayer.PipesBrightness) {
									case 0:
										color = networkColor;
										break;
									case 1:
										color = new(networkColor.R / 2 + color.R / 2, networkColor.G / 2 + color.G / 2, networkColor.B / 2 + color.B / 2, networkColor.A / 2 + color.A / 2);
										break;
									case 2:
										color *= 0.5f;
										break;
									case 3:
										color = Color.Transparent;
										break;
								}
							}
							else {
								switch (TA_ModPlayer.PipesBrightness) {
									case 0:
										float mult = 0.5f;
										byte add = (byte)(byte.MaxValue * mult);
										color *= 0.5f;
										color = new(color.R + add, color.G + add, color.B + add, 255);
										break;
									case 2:
										color *= 0.5f;
										break;
									case 3:
										color = Color.Transparent;
										break;
								}
							}

							if (color == Color.Transparent) {
								//wireTypeCount -= 1f;
							}
							else {
								Main.spriteBatch.Draw(basicPipeTexture.Value, new Vector2(x * 16 - (int)Main.screenPosition.X, y * 16 - (int)Main.screenPosition.Y), spriteMap, color, 0f, zero, 1f, SpriteEffects.None, 0f);
							}
						}

						/*
						if (tile.BlueWire) {
							bgyWireLeft = (bgyWireRight = (bgyWireAbove = (bgyWireBelow = (multipleWiresInConnectingTile = false))));
							wireTypeCount += 1f;
							int blueSpriteSheetX = 0;
							if (Main.tile[j, i - 1].BlueWire) {
								blueSpriteSheetX += 18;
								bgyWireAbove = true;
								if (redWireAbove)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j + 1, i].BlueWire) {
								blueSpriteSheetX += 36;
								bgyWireRight = true;
								if (redWireRight)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j, i + 1].BlueWire) {
								blueSpriteSheetX += 72;
								bgyWireBelow = true;
								if (redWireBelow)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j - 1, i].BlueWire) {
								blueSpriteSheetX += 144;
								bgyWireLeft = true;
								if (redWireLeft)
									multipleWiresInConnectingTile = true;
							}

							if (wireTypeCount > 1f)
								multipleWiresInConnectingTile = true;

							spriteMap.Y = spriteMapYOffset + 18;
							spriteMap.X = blueSpriteSheetX;
							Color color2 = Lighting.GetColor(j, i);
							switch (blueBrightStyle) {
								case 0:
									color2 = Color.White;
									break;
								case 2:
									color2 *= 0.5f;
									break;
								case 3:
									color2 = Color.Transparent;
									break;
							}

							if (color2 == Color.Transparent) {
								wireTypeCount -= 1f;
							}
							else {
								Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), spriteMap, color2 * (1f / wireTypeCount), 0f, zero, 1f, SpriteEffects.None, 0f);
								if (bgyWireAbove) {
									if (multipleWiresInConnectingTile && !redWireAbove)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(18, spriteMap.Y, 16, 6), color2, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireAbove = true;
								}

								if (bgyWireBelow) {
									if (multipleWiresInConnectingTile && !redWireBelow)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(0f, 10f), new Microsoft.Xna.Framework.Rectangle(72, spriteMap.Y + 10, 16, 6), color2, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireBelow = true;
								}

								if (bgyWireRight) {
									if (multipleWiresInConnectingTile && !redWireRight)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(10f, 0f), new Microsoft.Xna.Framework.Rectangle(46, spriteMap.Y, 6, 16), color2, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireRight = true;
								}

								if (bgyWireLeft) {
									if (multipleWiresInConnectingTile && !redWireLeft)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(144, spriteMap.Y, 6, 16), color2, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireLeft = true;
								}
							}
						}

						if (tile.GreenWire) {
							bgyWireLeft = (bgyWireRight = (bgyWireAbove = (bgyWireBelow = (multipleWiresInConnectingTile = false))));
							wireTypeCount += 1f;
							int greenSpriteSheetX = 0;
							if (Main.tile[j, i - 1].GreenWire) {
								greenSpriteSheetX += 18;
								bgyWireAbove = true;
								if (redWireAbove)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j + 1, i].GreenWire) {
								greenSpriteSheetX += 36;
								bgyWireRight = true;
								if (redWireRight)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j, i + 1].GreenWire) {
								greenSpriteSheetX += 72;
								bgyWireBelow = true;
								if (redWireBelow)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j - 1, i].GreenWire) {
								greenSpriteSheetX += 144;
								bgyWireLeft = true;
								if (redWireLeft)
									multipleWiresInConnectingTile = true;
							}

							if (wireTypeCount > 1f)
								multipleWiresInConnectingTile = true;

							spriteMap.Y = spriteMapYOffset + 36;
							spriteMap.X = greenSpriteSheetX;
							Microsoft.Xna.Framework.Color color3 = Lighting.GetColor(j, i);
							switch (greenBrightStyle) {
								case 0:
									color3 = Microsoft.Xna.Framework.Color.White;
									break;
								case 2:
									color3 *= 0.5f;
									break;
								case 3:
									color3 = Microsoft.Xna.Framework.Color.Transparent;
									break;
							}

							if (color3 == Microsoft.Xna.Framework.Color.Transparent) {
								wireTypeCount -= 1f;
							}
							else {
								Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), spriteMap, color3 * (1f / wireTypeCount), 0f, zero, 1f, SpriteEffects.None, 0f);
								if (bgyWireAbove) {
									if (multipleWiresInConnectingTile && !redWireAbove)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(18, spriteMap.Y, 16, 6), color3, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireAbove = true;
								}

								if (bgyWireBelow) {
									if (multipleWiresInConnectingTile && !redWireBelow)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(0f, 10f), new Microsoft.Xna.Framework.Rectangle(72, spriteMap.Y + 10, 16, 6), color3, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireBelow = true;
								}

								if (bgyWireRight) {
									if (multipleWiresInConnectingTile && !redWireRight)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(10f, 0f), new Microsoft.Xna.Framework.Rectangle(46, spriteMap.Y, 6, 16), color3, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireRight = true;
								}

								if (bgyWireLeft) {
									if (multipleWiresInConnectingTile && !redWireLeft)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(144, spriteMap.Y, 6, 16), color3, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireLeft = true;
								}
							}
						}

						if (tile.YellowWire) {
							bgyWireLeft = (bgyWireRight = (bgyWireAbove = (bgyWireBelow = (multipleWiresInConnectingTile = false))));
							wireTypeCount += 1f;
							int yellowSpriteSheetX = 0;
							if (Main.tile[j, i - 1].YellowWire) {
								yellowSpriteSheetX += 18;
								bgyWireAbove = true;
								if (redWireAbove)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j + 1, i].YellowWire) {
								yellowSpriteSheetX += 36;
								bgyWireRight = true;
								if (redWireRight)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j, i + 1].YellowWire) {
								yellowSpriteSheetX += 72;
								bgyWireBelow = true;
								if (redWireBelow)
									multipleWiresInConnectingTile = true;
							}

							if (Main.tile[j - 1, i].YellowWire) {
								yellowSpriteSheetX += 144;
								bgyWireLeft = true;
								if (redWireLeft)
									multipleWiresInConnectingTile = true;
							}

							if (wireTypeCount > 1f)
								multipleWiresInConnectingTile = true;

							spriteMap.Y = spriteMapYOffset + 54;
							spriteMap.X = yellowSpriteSheetX;
							Microsoft.Xna.Framework.Color color4 = Lighting.GetColor(j, i);
							switch (yellowBrightStyle) {
								case 0:
									color4 = Microsoft.Xna.Framework.Color.White;
									break;
								case 2:
									color4 *= 0.5f;
									break;
								case 3:
									color4 = Microsoft.Xna.Framework.Color.Transparent;
									break;
							}

							if (color4 == Microsoft.Xna.Framework.Color.Transparent) {
								wireTypeCount -= 1f;
							}
							else {
								Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), spriteMap, color4 * (1f / wireTypeCount), 0f, zero, 1f, SpriteEffects.None, 0f);
								if (bgyWireAbove) {
									if (multipleWiresInConnectingTile && !redWireAbove)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(18, spriteMap.Y, 16, 6), color4, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireAbove = true;
								}

								if (bgyWireBelow) {
									if (multipleWiresInConnectingTile && !redWireBelow)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(0f, 10f), new Microsoft.Xna.Framework.Rectangle(72, spriteMap.Y + 10, 16, 6), color4, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireBelow = true;
								}

								if (bgyWireRight) {
									if (multipleWiresInConnectingTile && !redWireRight)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y) + new Vector2(10f, 0f), new Microsoft.Xna.Framework.Rectangle(46, spriteMap.Y, 6, 16), color4, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireRight = true;
								}

								if (bgyWireLeft) {
									if (multipleWiresInConnectingTile && !redWireLeft)
										Main.spriteBatch.Draw(TextureAssets.WireNew.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(144, spriteMap.Y, 6, 16), color4, 0f, zero, 1f, SpriteEffects.None, 0f);

									redWireLeft = true;
								}
							}
						}
						*/
					}

					//if (Main.tile[j, i].HasActuator && (Lighting.Brightness(j, i) > 0f || actuatorsBrghtStyle == 0)) {
					//	Color color5 = Lighting.GetColor(j, i);
					//	switch (actuatorsBrghtStyle) {
					//		case 0:
					//			color5 = Color.White;
					//			break;
					//		case 2:
					//			color5 *= 0.5f;
					//			break;
					//		case 3:
					//			color5 = Color.Transparent;
					//			break;
					//	}

					//	Main.spriteBatch.Draw(TextureAssets.Actuator.Value, new Vector2(j * 16 - (int)Main.screenPosition.X, i * 16 - (int)Main.screenPosition.Y), new Microsoft.Xna.Framework.Rectangle(0, 0, TextureAssets.Actuator.Width(), TextureAssets.Actuator.Height()), color5 * actuatorColorMult, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
					//}

					//if (tile.HasTile) {
					//	ushort type = tile.TileType;
					//	if (type == TileID.LogicSensor && tile.TileFrameY == 36)
					//		Main.DrawWiresSpecialTiles.Add(Tuple.Create(j, i, tile.TileType));
					//}
				}
			}

			//for (int k = 0; k < Main.DrawWiresSpecialTiles.Count; k++) {
			//	Tuple<int, int, ushort> tuple = Main.DrawWiresSpecialTiles[k];
			//	ushort type = tuple.Item3;
			//	if (type == 423) {
			//		Vector2 start = new Vector2(tuple.Item1 * 16 - 32 - 1, tuple.Item2 * 16 - 160 - 1) + zero2;
			//		Vector2 end = new Vector2(tuple.Item1 * 16 + 48 + 1, tuple.Item2 * 16 + 1) + zero2;
			//		Utils.DrawRectangle(Main.spriteBatch, start, end, Microsoft.Xna.Framework.Color.LightSeaGreen, Microsoft.Xna.Framework.Color.LightSeaGreen, 2f);
			//	}
			//}

			TimeLogger.DetailedDrawTime(34);
		}
		public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			if (TA_Mod.clientConfig.ShowPipeAndStorageColors) {
				List<Color> colors = new();
				foreach (StorageNetwork storageNetwork in StorageNetwork.AllStorageNetworks.Values) {
					if (storageNetwork.StoragesLocations.Contains(i, j)) {
						colors.Add(storageNetwork.testingColor);
					}
				}

				if (colors.Count > 0) {
					Color color = colors.BlendColors();
					AndroUtilityMethods.DrawTestingDot(i, j, color, spriteBatch);
				}
			}

			return;
		}

		public override void PlaceInWorld(int i, int j, int type, Item item) {
			if (type == TileID.WirePipe) {
				StorageNetwork.OnPlaceJunctionBox(i, j);
			}
			//Check for chests and other compatible storages being placed
		}
		private void On_Player_ItemCheck_UseMiningTools_TryPoundingTile(On_Player.orig_ItemCheck_UseMiningTools_TryPoundingTile orig, Player self, Item sItem, int tileHitId, ref bool hitWall, int x, int y) {
			Tile tile = Main.tile[x, y];
			int tileFrameX = tile.TileFrameX;
			bool junctionBoxChange = tile.TileType == TileID.WirePipe;

			orig(self, sItem, tileHitId, ref hitWall, x, y);

			if (junctionBoxChange) {
				if (tileFrameX != tile.TileFrameX)
					StorageNetwork.OnSlopeJunctionBox(x, y);
			}
		}
		private void On_WorldGen_KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem) {
			Tile tile = Main.tile[i, j];
			bool hasTile = tile.HasTile;
			bool junctionBoxChange = hasTile && tile.TileType == TileID.WirePipe;

			orig(i, j, fail, effectOnly, noItem);

			if (junctionBoxChange) {
				if (!tile.HasTile)
					StorageNetwork.OnKillJunctionBox(i, j);
			}
		}
	}
	public static class PipeDataStaticMethods {
		public static bool HasPipe(this Tile tile) => tile.Get<TilePipeData>().HasPipe;
		public static void HasPipe(this Tile tile, bool value) => tile.Get<TilePipeData>().HasPipe = value;
		public static byte PipeType(this Tile tile) => tile.Get<TilePipeData>().PipeType;
		public static PipeTypeID GetPipeTypeID(this Tile tile) => (PipeTypeID)tile.PipeType();
		public static void PipeType(this Tile tile, byte pipeType) => tile.Get<TilePipeData>().PipeType = pipeType;
		public static void PipeType(this Tile tile, PipeTypeID pipeType) => tile.PipeType((byte)pipeType);
		public static void ResetPipeData(this Tile tile) => tile.PipeData(0);
		public static int PipeItemType(this Tile tile) {
			if (!tile.HasPipe())
				return -1;

			return tile.PipeType() switch {

				_ => Pipe.PipeType
			};
		}
		public static bool IsPipeItemType(this Tile tile, int pipeItemType) => tile.HasPipe() && tile.PipeItemType() == pipeItemType;
		public static bool SetPipeItemType(this Tile tile, int pipeItemType) {
			if (pipeItemType == -1) {
				tile.ResetPipeData();
				return false;
			}

			byte pipeData = tile.PipeData();
			if (pipeItemType == Pipe.PipeType) {
				tile.HasPipe(true);
				tile.PipeType(PipeTypeID.Basic);
			}

			return tile.PipeData() != pipeData;
		}
		public static bool IsJunctionBox(this Tile tile, out int junctionBoxType) {
			if (!tile.HasTile || tile.TileType != TileID.WirePipe) {
				junctionBoxType = -1;
				return false;
			}

			junctionBoxType = tile.TileFrameX / 18;
			
			return true;
		}
		public static Color BlendColors(this IList<Color> colors) {
			float fraction = 1f / colors.Count;
			for (int k = 0; k < colors.Count; k++) {
				colors[k] *= fraction;
			}

			return new(colors.Sum(c => c.R), colors.Sum(c => c.G), colors.Sum(c => c.B));
		}
	}
}
