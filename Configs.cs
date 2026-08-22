using BossChecklist.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
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
		public class DebugTools
		{
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
			public bool EnableBossStateToggle { get; set; }

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

		private FilterType BossFilter;
		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DefaultValue(FilterType.Show)]
		public FilterType FilterBosses {
			get => BossFilter;
			set => BossFilter = (value is FilterType.Hide) ? FilterType.HideWhenCompleted : value;
		}

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[DefaultValue(FilterType.Show)]
		public FilterType FilterMiniBosses { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[DefaultValue(FilterType.Show)]
		public FilterType FilterEvents { get; set; }

		public enum FilterType {
			Show,
			[EnumMember(Value = "Hide When Completed")]
			HideWhenCompleted,
			Hide
		}

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		public bool ColoredBossText { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[DefaultValue(CheckType.Check_Empty)]
		public CheckType SelectedCheckmarkType { get; set; }

		public enum CheckType {
			[EnumMember(Value = "✓  ☐")]
			Check_Empty,
			[EnumMember(Value = "✓  X")]
			Check_X,
			[EnumMember(Value = "X  ☐")]
			X_Empty,
			[EnumMember(Value = "Strike-through")]
			StrikeThrough
		}

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
			BossLogUI Log = BossLogSystem.Instance.BossLog;

			Log.Indicators[0].Color = OnlyShowBossContent ? Color.White : Color.DarkGray;
			Log.Indicators[0].hoverText = OnlyShowBossContent ? "Log.Indicator.OnlyBossContentEnabled" : "Log.Indicator.OnlyBossContentDisabled";

			Log.Indicators[1].Color = AutomaticChecklist ? Color.LightGreen : Color.DarkGray;
			Log.Indicators[1].hoverText = AutomaticChecklist ? "Log.Indicator.AutomaticChecklist" : "Log.Indicator.ManualChecklist";

			Log.Indicators[2].Color = ProgressiveChecklist ? Color.Tomato : Color.DarkGray;
			Log.Indicators[2].hoverText = "Log.Indicator.ProgressionMode";
			Log.Indicators[2].hoverTextParams = new object[] { Mod.GetLocalization($"Log.Common.{(ProgressiveChecklist ? "Enabled" : "Disabled")}") }; // PartiallyEnabled no longer available

			BossChecklist.instance.Logger.Info(Log.Indicators[2].hoverText);

			Log.NextBossTab.Anchor = BossLogUI.FindNextEntry(EntryType.Boss); // update the Next entry/boss tab based on Indicator selections

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
							badConfig = Mod.GetLocalization("Configs.FeatureConfiguration.RecordTrackingEnabled.Label").Value;
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
							badConfig = Mod.GetLocalization("Configs.FeatureConfiguration.AllowNewRecords.Label").Value;
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
		[DefaultValue(TimeFormat.Standard)]
		public TimeFormat TimeValueFormat { get; set; }

		public enum TimeFormat {
			Standard,
			Simple
		}

		public NPCDefinition DisplayRecordTracking { get; set; } = new NPCDefinition();

		[Header("ChatMessages")]

		[DrawTicks]
		[DefaultValue(MessageType.Generic)]
		public MessageType DespawnMessageType { get; set; }

		[DrawTicks]
		[DefaultValue(MessageType.Generic)]
		public MessageType LimbMessages { get; set; }

		[DrawTicks]
		[DefaultValue(MessageType.Generic)]
		public MessageType MoonMessages { get; set; }

		public enum MessageType {
			Disabled,
			Generic,
			Unique
		}

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
					Main.NewText(Mod.GetLocalization("Configs.Common.InvalidChange").Format(badConfig, interferingNPC), Color.Orange);
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