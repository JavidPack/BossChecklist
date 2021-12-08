using System.Collections.Generic;
using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
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
		}

		public override void Unload() {
			bossChecklistInterface = null;
			BossRadarUIInterface = null;
			BossRadarUI.arrowTexture = null;
			BossRadarUI.whitelistNPCs = null;
			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

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
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Log",
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
			}

			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Log",
					delegate {
						if (UIHoverText != "") {
							BossLogUI.DrawTooltipBG(Main.spriteBatch, UIHoverText, UIHoverTextColor);
						}
						UIHoverText = "";
						UIHoverTextColor = default;
						return true;
					},
					InterfaceScaleType.UI)
				);
			}

			#region DEBUG
			int playerChatIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Player Chat"));
			if (playerChatIndex != -1) {
				layers.Insert(playerChatIndex, new LegacyGameInterfaceLayer("BossChecklist: Debug Timers and Counters",
					delegate {
						PlayerAssist playerAssist = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						int configIndex = NPCAssist.ListedBossNum(BossChecklist.DebugConfig.ShowTimerOrCounter.Type, BossChecklist.DebugConfig.ShowTimerOrCounter.mod);
						if (configIndex != -1) {
							string textKingSlime = $"{BossChecklist.bossTracker.SortedBosses[configIndex].name} (#{configIndex + 1})" +
												$"\nTime: {playerAssist.Tracker_Duration[configIndex]}" +
												$"\nTimes Hit: {playerAssist.Tracker_HitsTaken[configIndex]}" +
												$"\nDeaths: {playerAssist.Tracker_Deaths[configIndex]}";
							Main.spriteBatch.DrawString(FontAssets.MouseText.Value, textKingSlime, new Vector2(20, Main.screenHeight - 175), new Color(1f, 0.388f, 0.278f), 0f, default(Vector2), 1, SpriteEffects.None, 0f);
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#endregion
		}
	}
}
