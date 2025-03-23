using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	[BackgroundColor(30, 60, 30, 200)]
	public class BossLogConfiguration : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.BossLogConfig = this;

		[Header("BossLogUI")]

		[BackgroundColor(250, 235, 215)]
		[SliderColor(87, 181, 92)]
		[DefaultValue(typeof(Color), "87, 181, 92, 255"), ColorNoAlpha]
		public Color BossLogColor { get; set; }

		[BackgroundColor(250, 235, 215)]
		[SliderColor(87, 181, 92)]
		[DefaultValue(typeof(Vector2), "-270, -50")]
		[Range(-1920f, 0f)]
		public Vector2 BossLogPos { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		public bool ShowInteractionTooltips { get; set; }

		[Expand(false)]
		[BackgroundColor(100, 70, 60)]
		public DebugTools Debug { get; set; } = new DebugTools();
		public class DebugTools {
			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool ModCallLogVerbose { get; set; }

			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool DisableAutoLocalization { get; set; }

			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool AccessInternalNames { get; set; }

			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool ShowProgressionValue { get; set; }

			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool ShowCollectionType { get; set; }

			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool EnabledResetOptions { get; set; }

			/*
			[BackgroundColor(250, 235, 215)]
			[DefaultValue(false)]
			public bool InactiveBossCheck { get; set; }
			*/

			public override int GetHashCode() {
				return new { ModCallLogVerbose, ShowProgressionValue, AccessInternalNames, ShowCollectionType, DisableAutoLocalization }.GetHashCode();
			}
		}

		[Header("BossLogChecklist")]

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(true)]
		public bool AutomaticChecklist { get; set; }

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(false)]
		public bool ProgressiveChecklist { get; set; }

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.ProgressionPrompt.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.ProgressionPrompt.Tooltip")]
		public bool PromptDisabled { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		public bool HideUnavailable { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		public bool HideUnsupported { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.OnlyBosses.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.OnlyBosses.Tooltip")]
		public bool OnlyShowBossContent { get; set; }

		internal const string Option_Show = "Show";
		internal const string Option_Hide = "Hide";
		internal const string Option_HideWhenCompleted = "Hide When Completed";

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[OptionStrings(new string[] { Option_Show, Option_HideWhenCompleted })]
		[DefaultValue(Option_Show)]
		public string FilterBosses { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[OptionStrings(new string[] { Option_Show, Option_HideWhenCompleted, Option_Hide })]
		[DefaultValue(Option_Show)]
		public string FilterMiniBosses { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[OptionStrings(new string[] { Option_Show, Option_HideWhenCompleted, Option_Hide })]
		[DefaultValue(Option_Show)]
		public string FilterEvents { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		public bool ColoredBossText { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[OptionStrings(new string[] { CheckType_CheckAndEmpty, CheckType_CheckAndX, CheckType_XAndEmpty, CheckType_StrikeThrough })]
		[DefaultValue(CheckType_CheckAndEmpty)]
		public string SelectedCheckmarkType { get; set; }

		public const string CheckType_CheckAndEmpty = "✓  ☐";
		public const string CheckType_CheckAndX = "✓  X";
		public const string CheckType_XAndEmpty = "X  ☐";
		public const string CheckType_StrikeThrough = "Strike-through";

		[BackgroundColor(200, 188, 172)]
		[DefaultValue(true)]
		public bool DrawNextMark { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		public bool ShowProgressBars { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		public bool SpawnItemCraftingChecklist { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.LootChecklist.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.LootChecklist.Tooltip")]
		public bool LootCheckVisibility { get; set; }

		[BackgroundColor(200, 188, 172)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.CheckDroppedLoot.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.CheckDroppedLoot.Tooltip")]
		public bool OnlyCheckDroppedLoot { get; set; }

		/*
		[Header("BlindMode")]

		[BackgroundColor(255, 99, 71)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.EnableProgressionMode.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.EnableProgressionMode.Tooltip")]
		public bool ProgressionModeEnable {
			get => MaskTextures && MaskNames && MaskBossLoot && MaskHardMode;
			set {
				if (value) {
					MaskTextures = true;
					MaskNames = true;
					MaskBossLoot = true;
					MaskHardMode = true;
				}
			}
		}

		[BackgroundColor(255, 99, 71)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.DisableProgressionMode.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.DisableProgressionMode.Tooltip")]
		public bool ProgressionModeDisable {
			get => !MaskTextures && !MaskNames && !MaskBossLoot && !MaskHardMode;
			set {
				if (value) {
					MaskTextures = false;
					MaskNames = false;
					UnmaskNextBoss = false; // UnmaskNextBoss is unnecessary if MaskNames is disabled, so disable it too.
					MaskBossLoot = false;
					MaskHardMode = false;
				}
			}
		}

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		public bool MaskTextures { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		public bool MaskNames { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.UnmaskNextCheck.Label")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.UnmaskNextCheck.Tooltip")]
		public bool UnmaskNextBoss { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		public bool MaskBossLoot { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		public bool MaskHardMode { get; set; }
		*/

		public void UpdateIndicators() {
			BossLogUI Log = BossUISystem.Instance.BossLog;
			string LangIndicator = "Mods.BossChecklist.Log.Indicator";
			string LangCommon = "Mods.BossChecklist.Log.Common";

			Log.Indicators[0].Color = OnlyShowBossContent ? Color.White : Color.DarkGray;
			Log.Indicators[0].hoverText = OnlyShowBossContent ? $"{LangIndicator}.OnlyBossContentEnabled" : $"{LangIndicator}.OnlyBossContentDisabled";

			Log.Indicators[1].Color = AutomaticChecklist ? Color.LightGreen : Color.DarkGray;
			Log.Indicators[1].hoverText = AutomaticChecklist ? $"{LangIndicator}.AutomaticChecklist" : $"{LangIndicator}.ManualChecklist";

			Log.Indicators[2].Color = ProgressiveChecklist ? Color.Tomato : Color.DarkGray;
			Log.Indicators[2].hoverText = Language.GetTextValue($"{LangIndicator}.ProgressionMode", Language.GetTextValue($"{LangCommon}." + (ProgressiveChecklist ? "Enabled" : "Disabled"))); // PartiallyEnabled no longer available

			BossChecklist.instance.Logger.Info(Log.Indicators[2].hoverText);

			Log.BossTab.Anchor = BossLogUI.FindNextEntry(EntryType.Boss); // update the Next entry/boss tab based on Indicator selections

			if (Log.PageNum == BossLogUI.Page_TableOfContents)
				Log.RefreshPageContent();
		}

		public override void OnChanged() {
			if (BossChecklist.instance == null)
				return;

			if (Debug.EnabledResetOptions)
				ShowInteractionTooltips = true; // The Reset Options require the Interaction Toolip tab to function

			UpdateIndicators();
		}
	}

	public class FeatureConfiguration : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.FeatureConfig = this;

		private bool TrackingEnabled;
		private bool NewRecordsEnabled;
		private string interferingNPC = "";
		private string badConfig = "";

		[Header("BossRecords")]

		[DefaultValue(true)]
		public bool RecordTrackingEnabled {
			get => TrackingEnabled;
			set {
				if (!Main.gameMenu) {
					foreach (NPC npc in Main.ActiveNPCs) {
						if (BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int _) is EntryInfo entry) {
							interferingNPC = entry.DisplayName;
							badConfig = Language.GetTextValue("Mods.BossChecklist.Configs.FeatureConfiguration.RecordTrackingEnabled.Label");
							return; // If a boss is active, debug features are disabled until all bosses are inactive
						}
					}
				}
				TrackingEnabled = value;
				if (value is false)
					NewRecordsEnabled = false;
			}
		}

		[DefaultValue(true)]
		public bool AllowNewRecords {
			get => RecordTrackingEnabled && NewRecordsEnabled;
			set {
				if (!Main.gameMenu) {
					foreach (NPC npc in Main.ActiveNPCs) {
						if (BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int _) is EntryInfo entry) {
							interferingNPC = entry.DisplayName;
							badConfig = Language.GetTextValue("Mods.BossChecklist.Configs.FeatureConfiguration.AllowNewRecords.Label");
							return; // If a boss is active, debug features are disabled until all bosses are inactive
						}
					}
				}
				if (TrackingEnabled)
					NewRecordsEnabled = value;
			}
		}


		[DefaultValue(true)]
		public bool NewRecordLogGlow { get; set; }

		[DrawTicks]
		[OptionStrings(new string[] { "Standard", "Simple" })]
		[DefaultValue("Standard")]
		public string TimeValueFormat { get; set; }

		public NPCDefinition DisplayRecordTracking { get; set; } = new NPCDefinition();

		[Header("ChatMessages")]

		[DrawTicks]
		[OptionStrings(new string[] { "Disabled", "Generic", "Unique" })]
		[DefaultValue("Generic")]
		public string DespawnMessageType { get; set; }

		[DrawTicks]
		[OptionStrings(new string[] { "Disabled", "Generic", "Unique" })]
		[DefaultValue("Generic")]
		public string LimbMessages { get; set; }

		[DrawTicks]
		[OptionStrings(new string[] { "Disabled", "Generic", "Unique" })]
		[DefaultValue("Generic")]
		public string MoonMessages { get; set; }

		[DefaultValue(true)]
		public bool TimerSounds { get; set; }

		[Header("ItemMapDetection")]

		[DefaultValue(true)]
		public bool TreasureBagsOnMap { get; set; }

		[DefaultValue(true)]
		public bool FragmentsOnMap { get; set; }

		[DefaultValue(false)]
		public bool ScalesOnMap { get; set; }

		internal bool ItemMapDrawingEnabled => TreasureBagsOnMap || FragmentsOnMap || ScalesOnMap;

		[Header("BossRadar")]

		[DefaultValue(true)]
		public bool EnableBossRadar { get; set; }

		[DefaultValue(false)]
		public bool RadarMiniBosses { get; set; }

		public const float OpacityFloatMin = 0.35f;
		public const float OpacityFloatMax = 0.85f;
		[Range(OpacityFloatMin, OpacityFloatMax)]
		[DefaultValue(0.75f)]
		public float RadarOpacity { get; set; }

		public List<NPCDefinition> RadarBlacklist { get; set; } = new List<NPCDefinition>();

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context) {
			//Range attribute doesn't enforce it onto the value, it's a limit for the UI only, so we have to clamp it here again if user decides to edit it through the json
			//If this isn't in here, OpacityFloat can get negative for example, which will lead to a crash later
			RadarOpacity = Utils.Clamp(RadarOpacity, OpacityFloatMin, OpacityFloatMax);
		}

		public override void OnChanged() {
			BossRadarUI.blacklistChanged = true;

			if (!Main.gameMenu) {
				if (string.IsNullOrEmpty(interferingNPC)) {
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.UpdateAllowTracking);
						packet.Write(RecordTrackingEnabled);
						packet.Write(AllowNewRecords);
						packet.Send();
					}
				}
				else {
					Main.NewText(Language.GetText("Mods.BossChecklist.Configs.Common.InvalidChange").Format(badConfig, interferingNPC), Color.Orange);
					interferingNPC = "";
					badConfig = "";
				}
			}
		}
	}
	/*
		// Code created by Jopojelly, taken from CheatSheet
		private bool IsPlayerLocalServerOwner(Player player) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return Netplay.Connection.Socket.GetRemoteAddress().IsLocalHost();
			}
			for (int plr = 0; plr < Main.maxPlayers; plr++) {
				RemoteClient NetPlayer = Netplay.Clients[plr];
				if (NetPlayer.State == 10 && Main.player[plr] == player && NetPlayer.Socket.GetRemoteAddress().IsLocalHost()) {
					return true;
				}
			}
			return false;
		}

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) {
			if (!IsPlayerLocalServerOwner(Main.player[whoAmI])) {
				message = NetworkText.FromKey("Mods.BossChecklist.Configs.Notice.HostChange");
				return false;
			}
			return true;
		}
	*/
}