using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist.Systems
{
	class BossLogSystem : ModSystem {
		public static BossLogSystem Instance { get; private set; }

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

		public static HashSet<string> HiddenEntries = new HashSet<string>();
		public static HashSet<string> MarkedEntries = new HashSet<string>();

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

		public override void ClearWorld() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
		}
		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenEntries);
			var MarkedAsDownedList = new List<string>(MarkedEntries);
			tag["HiddenBossesList"] = HiddenBossesList;
			tag["downed_Forced"] = MarkedAsDownedList;
		}

		public override void LoadWorldData(TagCompound tag) {
			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenEntries.Add(bossKey);
			}

			var MarkedAsDownedList = tag.GetList<string>("downed_Forced");
			foreach (var bossKey in MarkedAsDownedList) {
				MarkedEntries.Add(bossKey);
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(HiddenEntries.Count);
			foreach (var bossKey in HiddenEntries) {
				writer.Write(bossKey);
			}

			writer.Write(MarkedEntries.Count);
			foreach (var bossKey in MarkedEntries) {
				writer.Write(bossKey);
			}
		}

		public override void NetReceive(BinaryReader reader) {
			HiddenEntries.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				HiddenEntries.Add(reader.ReadString());
			}

			MarkedEntries.Clear();
			count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				MarkedEntries.Add(reader.ReadString());
			}

			// Update checklist to match Hidden and Marked Downed entries
			if (BossChecklistUI.Visible)
				Instance.bossChecklistUI.UpdateCheckboxes();

			if (Instance.BossLog.BossLogVisible && Instance.BossLog.PageNum == -1) {
				Instance.BossLog.RefreshPageContent();
			}
		}

		public override void AddRecipes() {
			BossChecklist.instance.LoggingInitialization();
			//bossTracker.FinalizeLocalization();
			BossChecklist.bossTracker.FinalizeEventNPCPools();
			BossChecklist.bossTracker.FinalizeOrphanData(); // Add any remaining boss data, including added NPCs, loot, collectibles and spawn items.
			BossChecklist.bossTracker.FinalizeCollectibleTypes(); // Collectible types have to be determined AFTER all items in orphan data has been added.
			BossChecklist.bossTracker.FinalizeEntryLootTables(); // Generate boss loot data. Must come after collectible type finalization as it set the treasure bag.
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

			#region DEBUG
			int playerChatIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Player Chat"));
			if (playerChatIndex != -1) {
				layers.Insert(playerChatIndex, new LegacyGameInterfaceLayer("BossChecklist: Record Tracker Debugger",
					delegate {
						if (Main.netMode != NetmodeID.SinglePlayer)
							return true; // Currently, this debug feature is limited to singleplayer as the server does not display its info

						if (BossChecklist.FeatureConfig.DisplayRecordTracking.IsUnloaded || BossChecklist.FeatureConfig.DisplayRecordTracking.Type == NPCID.None)
							return true; // The Display Record Tracking config must be loaded and cannot be selected as none

						if (BossChecklist.bossTracker.FindBossEntryByNPC(BossChecklist.FeatureConfig.DisplayRecordTracking.Type, out int recordIndex) is not EntryInfo entry)
							return true; // The selected NPC must also be assigned to an entry

						RecordModPlayer modplayer = Main.LocalPlayer.GetModPlayer<RecordModPlayer>();
						if (modplayer.RecordsForWorld is not List<PersonalRecords> personalrecords || !modplayer.PlayerRecordsInitialized)
							return true; // The player's records must be initialized to be displayed

						string debugText =
							$"Boss Checklist: Record Tracker" +
							$"\n[#{entry.GetIndex}] {entry.DisplayName} ({recordIndex})" +
							$"\n{(personalrecords[recordIndex].Tracker_Deaths > 0 ? $"[i:{ItemID.Tombstone}] {personalrecords[recordIndex].Tracker_Deaths} " : "")}" +
							$"[i:{ItemID.ArmorBracing}] {personalrecords[recordIndex].Tracker_HitsTaken} " +
							$"[i:{ItemID.Stopwatch}] {PersonalRecords.TimeConversion(personalrecords[recordIndex].Tracker_Duration)}";

						// This positioning draws the text above the health bar of the active boss
						Vector2 barCenter = Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1f) + new Vector2(0f, -50f);
						Vector2 debugPos = Utils.CenteredRectangle(barCenter, new Vector2(456, 22)).TopLeft() - new Vector2(0, 24);
						debugPos.Y -= FontAssets.MouseText.Value.MeasureString(debugText).Y;
						Utils.DrawBorderString(Main.spriteBatch, debugText, debugPos, Color.Tomato);
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
			var bgPos = new Rectangle(Main.mouseX + 20, Main.mouseY + 20, (int)stringVec.X + padd, (int)stringVec.Y + padd - 5);
			bgPos.X = Utils.Clamp(bgPos.X, 0, Main.screenWidth - bgPos.Width);
			bgPos.Y = Utils.Clamp(bgPos.Y, 0, Main.screenHeight - bgPos.Height);

			var textPos = new Vector2(bgPos.X + padd / 2, bgPos.Y + padd / 2);
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

	public class BossLogModPlayer : ModPlayer {
		public bool hasOpenedTheBossLog; // For the 'never opened' button glow for players who haven't noticed the new feature yet.
		public bool enteredWorldReset; // When players jon a different world, the boss log PageNum should reset back to its original state
		public List<ItemDefinition> BossItemsCollected;
		public bool IsItemResearched(int itemType) => Player.creativeTracker.ItemSacrifices.TryGetSacrificeNumbers(itemType, out int count, out int max) && count == max;

		public override void Initialize() {
			hasOpenedTheBossLog = false;
			enteredWorldReset = false;
			BossItemsCollected = new List<ItemDefinition>();
		}

		public override void SaveData(TagCompound tag) {
			tag["BossLogPrompt"] = hasOpenedTheBossLog;
			tag["BossLootObtained"] = BossItemsCollected;
		}

		public override void LoadData(TagCompound tag) {
			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt"); // saved state of the unopened boss log prompt
			BossItemsCollected = tag.GetList<ItemDefinition>("BossLootObtained").ToList(); // Prepare the collectibles for the player.
		}

		public override void OnEnterWorld() {
			enteredWorldReset = true; // PageNum starts out with an invalid number so jumping between worlds will always reset the BossLog when toggled
		}

		public override void UpdateDead() {
			if (Main.netMode == NetmodeID.Server || Player.whoAmI == 255 || Player.whoAmI != Main.myPlayer)
				return;

			if (BossChecklist.FeatureConfig.TimerSounds && Player.respawnTimer > 0 && Player.respawnTimer <= 180 && Player.respawnTimer % 60 == 0)
				SoundEngine.PlaySound(SoundID.MaxMana); // Timer sounds when a player is about to respawn
		}

		public override bool OnPickup(Item item) {
			if (Main.netMode == NetmodeID.Server || Player.whoAmI == 255)
				return base.OnPickup(item);

			// Only add the item to the list if it is not already present
			if (BossChecklist.bossTracker.EntryLootCache[item.type] && !BossItemsCollected.Any(x => x.Type == item.type))
				BossItemsCollected.Add(new ItemDefinition(item.type)); // Adds items that are picked up to the collected boss loot list

			return base.OnPickup(item);
		}
	}
}
