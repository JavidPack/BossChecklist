using System.Collections.Generic;
using System.Linq;
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
	class BossUISystem : ModSystem {
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

		public override void AddRecipes() {
			//bossTracker.FinalizeLocalization();
			BossChecklist.bossTracker.FinalizeEventNPCPools();
			BossChecklist.bossTracker.FinalizeOrphanData(); // Add any remaining boss data, including added NPCs, loot, collectibles and spawn items.
			BossChecklist.bossTracker.FinalizeEntryLootTables(); // Generate boss loot data. Treasurebag is also determined in this.
			BossChecklist.bossTracker.FinalizeCollectibleTypes(); // Collectible types have to be determined AFTER all items in orphan data has been added.
			BossChecklist.bossTracker.FinalizeEntryData(); // Finalize all boss data. Entries cannot be further edited beyond this point.
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
				
			}
			// This doesn't work perfectly.
			//if (BossChecklistUI.Visible) {
			//	layers.RemoveAll(x => LayersToHideWhenChecklistVisible.Contains(x.Name));
			//}
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"BossChecklist: Boss Checklist",
					delegate {
						if (BossChecklistUI.Visible) {
							bossChecklistInterface?.Draw(Main.spriteBatch, new GameTime());
						}
						return true;
					},
					InterfaceScaleType.UI)
				);

				layers.Insert(++mouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Log UI",
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
						if (!string.IsNullOrEmpty(UIHoverText))
							DrawTooltipBackground(Language.GetTextValue(UIHoverText), UIHoverTextColor);

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

						if (NPCAssist.GetEntryInfo(BossChecklist.ClientConfig.DisplayRecordTracking.Type, out int recordIndex) is not EntryInfo entry)
							return true;

						if (!Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllStoredRecords.ContainsKey(Main.ActiveWorldFileData.UniqueId.ToString()))
							return true;

						if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld is not List<BossRecord> records || records.Count == 0)
							return true;

						PersonalStats personalrecords = records[recordIndex].stats;
						
						string debugText =
							$"[#{entry.GetIndex}] {entry.DisplayName} [{recordIndex}]" +
							$"\nTime: {PersonalStats.TimeConversion(personalrecords.Tracker_Duration)}" +
							$"\nTimes Hit: {personalrecords.Tracker_HitsTaken}" +
							$"\nDeaths: {personalrecords.Tracker_Deaths}";
						Main.spriteBatch.DrawString(FontAssets.MouseText.Value, debugText, new Vector2(20, Main.screenHeight - 175), Color.Tomato);
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#endregion
		}

		/// <summary>
		/// <para>Draws backgrounds for texts similar to the ones used for item tooltips.</para>
		/// <para>ModifyInterfaceLayers will use this method when hovering over an element that changes the <see cref="UIHoverText"/></para>
		/// </summary>
		/// <param name="text"></param>
		/// <param name="textColor"></param>
		private void DrawTooltipBackground(string text, Color textColor = default) {
			if (text == "")
				return;

			int padd = 20;
			Vector2 stringVec = FontAssets.MouseText.Value.MeasureString(RemoveChatTags(text));
			Rectangle bgPos = new Rectangle(Main.mouseX + 20, Main.mouseY + 20, (int)stringVec.X + padd, (int)stringVec.Y + padd - 5);
			bgPos.X = Utils.Clamp(bgPos.X, 0, Main.screenWidth - bgPos.Width);
			bgPos.Y = Utils.Clamp(bgPos.Y, 0, Main.screenHeight - bgPos.Height);

			Vector2 textPos = new Vector2(bgPos.X + padd / 2, bgPos.Y + padd / 2);
			if (textColor == default) {
				textColor = Main.MouseTextColorReal;
			}

			Utils.DrawInvBG(Main.spriteBatch, bgPos, new Color(23, 25, 81, 255) * 0.925f);
			Utils.DrawBorderString(Main.spriteBatch, text, textPos, textColor);
		}

		/// <summary>
		/// Removes chat tags from the decalred mod's displayname, presenting it in its pure text form.
		/// </summary>
		public static string RemoveChatTags(Mod mod) => RemoveChatTags(mod.DisplayName);
		public static string RemoveChatTags(string text) => string.Join("", ChatManager.ParseMessage(text, Color.White).Where(x => x.GetType() == typeof(TextSnippet)).Select(x => x.Text));
	}
}
