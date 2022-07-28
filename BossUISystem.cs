using System.Collections.Generic;
using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist
{
	class BossUISystem : ModSystem
	{
		public static BossUISystem Instance { get; private set; }

		internal static UserInterface bossChecklistInterface;
		internal BossChecklistUI bossChecklistUI;
		internal UserInterface BossLogInterface;
		internal BossLogUI BossLog;
		internal static UserInterface BossRadarUIInterface;
		internal BossRadarUI BossRadarUI;

		internal List<string> OptedModNames;
		internal string UIHoverText = "";
		internal Color UIHoverTextColor = default;

		//Zoom level, (for UIs)
		public static Vector2 ZoomFactor; //0f == fully zoomed out, 1f == fully zoomed in

		public override void Load() {
			Instance = this;

			if (!Main.dedServ) {
				bossChecklistUI = new BossChecklistUI();
				bossChecklistUI.Activate();
				bossChecklistInterface = new UserInterface();

				UICheckbox.checkboxTexture = Mod.Assets.Request<Texture2D>("UIElements/checkBox");
				UICheckbox.checkmarkTexture = Mod.Assets.Request<Texture2D>("UIElements/checkMark");

				BossLog = new BossLogUI();
				BossLog.Activate();
				BossLogInterface = new UserInterface();
				BossLogInterface.SetState(BossLog);

				//important, after setup has been initialized
				BossRadarUI = new BossRadarUI();
				BossRadarUI.Activate();
				BossRadarUIInterface = new UserInterface();
				BossRadarUIInterface.SetState(BossRadarUI);
			}

			OptedModNames = new List<string>();
		}

		public override void Unload() {
			bossChecklistInterface = null;
			BossRadarUIInterface = null;
			BossRadarUI.arrowTexture = null;
			BossRadarUI.whitelistNPCs = null;
			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

#if TML_2022_06
		// TODO: Remove after preview becomes stable in August
#else
		public override void AddRecipes() {
			//bossTracker.FinalizeLocalization();
			BossChecklist.bossTracker.FinalizeOrphanData(); // Add any remaining boss data, including added NPCs, loot, collectibles and spawn items.
			BossChecklist.bossTracker.FinalizeBossLootTables(); // Generate boss loot data. Treasurebag is also determined in this.
			BossChecklist.bossTracker.FinalizeCollectionTypes(); // Collectible types have to be determined AFTER all items in orphan data has been added.
			BossChecklist.bossTracker.FinalizeBossData(); // Finalize all boss data. Entries cannot be further edited beyond this point.
		}
#endif

		public override void UpdateUI(GameTime gameTime) {
			bossChecklistInterface?.Update(gameTime);
			BossLogInterface?.Update(gameTime);
			BossRadarUI?.Update(gameTime);
		}

		public override void ModifyTransformMatrix(ref SpriteViewMatrix transform) {
			//this is needed for Boss Radar, so it takes the range at which to draw the icon properly
			ZoomFactor = transform.Zoom - (Vector2.UnitX + Vector2.UnitY);
		}

		public override void PostDrawFullscreenMap(ref string mouseText) {
			MapAssist.DrawFullscreenMap();
		}

		private string[] LayersToHideWhenChecklistVisible = new string[] {
			"Vanilla: Map / Minimap", "Vanilla: Resource Bars"
		};

		//int lastSeenScreenWidth;
		//int lastSeenScreenHeight;
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			//if (BossChecklistUI.visible)
			//{
			//	layers.RemoveAll(x => x.Name == "Vanilla: Resource Bars" || x.Name == "Vanilla: Map / Minimap");
			//}

			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"BossChecklist: Boss Checklist",
					delegate {
						if (BossChecklistUI.Visible) {
							bossChecklistInterface?.Draw(Main.spriteBatch, new GameTime());

							if (BossChecklistUI.hoverText != "") {
								float x = FontAssets.MouseText.Value.MeasureString(BossChecklistUI.hoverText).X;
								Vector2 vector = new Vector2((float)Main.mouseX, (float)Main.mouseY) + new Vector2(16f, 16f);
								if (vector.Y > (float)(Main.screenHeight - 30)) {
									vector.Y = (float)(Main.screenHeight - 30);
								}
								if (vector.X > (float)(Main.screenWidth - x - 30)) {
									vector.X = (float)(Main.screenWidth - x - 30);
								}
								//Utils.DrawBorderStringFourWay(Main.spriteBatch, Main.fontMouseText, BossChecklistUI.hoverText,
								//	vector.X, vector.Y, new Color((int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor), Color.Black, Vector2.Zero, 1f);
								//	Utils.draw

								//ItemTagHandler.GenerateTag(item)
								int hoveredSnippet = -1;
								TextSnippet[] array = ChatManager.ParseMessage(BossChecklistUI.hoverText, Color.White).ToArray();
								ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, array,
									vector, 0f, Vector2.Zero, Vector2.One, out hoveredSnippet/*, -1f, 2f*/);

								if (hoveredSnippet > -1) {
									array[hoveredSnippet].OnHover();
									//if (Main.mouseLeft && Main.mouseLeftRelease)
									//{
									//	array[hoveredSnippet].OnClick();
									//}
								}
							}
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			// This doesn't work perfectly.
			//if (BossChecklistUI.Visible) {
			//	layers.RemoveAll(x => LayersToHideWhenChecklistVisible.Contains(x.Name));
			//}
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Log UI",
					delegate {
						BossLogInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);

				layers.Insert(++mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Radar",
					delegate {
						BossRadarUIInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);

				layers.Insert(++mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Custom UI Hover Text",
					delegate {
						// Detect if the hover text is a single localization key and draw the hover text accordingly
						if (UIHoverText != "") {
							string text = UIHoverText.StartsWith("$Mods.") ? Language.GetTextValue(UIHoverText.Substring(1)) : UIHoverText;
							BossLogUI.DrawTooltipBG(Main.spriteBatch, text, UIHoverTextColor);
						}
						// Reset text and color back to default state
						UIHoverText = "";
						UIHoverTextColor = default;
						return true;
					},
					InterfaceScaleType.UI)
				);
			}

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}

			#region DEBUG
			int playerChatIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Player Chat"));
			if (playerChatIndex != -1) {
				layers.Insert(playerChatIndex, new LegacyGameInterfaceLayer("BossChecklist: Record Tracker Debugger",
					delegate {
						// Currently, this debug feature is limited to singleplayer as the server does not display its info.
						if (Main.netMode != NetmodeID.SinglePlayer)
							return true;

						int configIndex = NPCAssist.GetBossInfoIndex(BossChecklist.DebugConfig.ShowTimerOrCounter.Type, true);
						if (configIndex == -1)
							return true;

						int recordIndex = BossChecklist.bossTracker.SortedBosses[configIndex].GetRecordIndex;
						if (recordIndex == -1)
							return true;

						PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						string debugText =
							$"[#{configIndex}] {BossChecklist.bossTracker.SortedBosses[configIndex].DisplayName} [{recordIndex}]" +
							$"\nTime: {modPlayer.Tracker_Duration[recordIndex]}" +
							$"\nTimes Hit: {modPlayer.Tracker_HitsTaken[recordIndex]}" +
							$"\nDeaths: {modPlayer.Tracker_Deaths[recordIndex]}";
						Main.spriteBatch.DrawString(FontAssets.MouseText.Value, debugText, new Vector2(20, Main.screenHeight - 175), Color.Tomato);
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#endregion
		}
	}
}
