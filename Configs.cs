using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	[BackgroundColor(30, 60, 30, 200)]
	public class BossLogConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.BossLogConfig = this;

		[Header("$Mods.BossChecklist.Configs.BossLogConfiguration.Header.BossLogUI")]

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(typeof(Color), "87, 181, 92, 255"), ColorNoAlpha]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.BossLogColor")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.BossLogColor")]
		public Color BossLogColor { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(typeof(Vector2), "-270, -50")]
		[Range(-1920f, 0f)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.BossLogPos")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.BossLogPos")]
		public Vector2 BossLogPos { get; set; }

		[Header("$Mods.BossChecklist.Configs.BossLogConfiguration.Header.BossLogChecklist")]

		// TODO: [??] Change HideUnavailable and HideUnsupported to OptionStrings to allow users to choose betweem
		// 1.) Show on Table of Contents, but skip through page navigation
		// 2.) Show on Table of Contents, and allow visibility through page navigation
		// 3.) Hide on Table of Contents and Page Navigation

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.HideUnavailable")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.HideUnavailable")]
		public bool HideUnavailable { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.HideUnsupported")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.HideUnsupported")]
		public bool HideUnsupported { get; set; }

		private bool BossesOnly;

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.OnlyBosses")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.OnlyBosses")]
		public bool OnlyShowBossContent {
			get => BossesOnly;
			set {
				BossesOnly = value;
				if (value) {
					FilterMiniBosses = "Hide";
					FilterEvents = "Hide";
				}
			}
		}

		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.FilterBosses")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.FilterBosses")]
		[OptionStrings(new string[] { "Show", "Hide When Completed" })]
		[DefaultValue("Show")]
		public string FilterBosses { get; set; }

		private string MB_Value = "Show";
		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.FilterMiniBosses")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.FilterMiniBosses")]
		[OptionStrings(new string[] { "Show", "Hide When Completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterMiniBosses {
			get => MB_Value;
			set {
				MB_Value = value;
				if (value != "Hide") {
					BossesOnly = false;
				}
			}
		}

		private string E_Value = "Show";
		[SliderColor(87, 181, 92)]
		[BackgroundColor(200, 188, 172)]
		[DrawTicks]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.FilterEvents")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.FilterEvents")]
		[OptionStrings(new string[] { "Show", "Hide When Completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterEvents {
			get => E_Value;
			set {
				E_Value = value;
				if (value != "Hide") {
					BossesOnly = false;
				}
			}
		}

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.ColoredBossText")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.ColoredBossText")]
		public bool ColoredBossText { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.SelectedCheckmarkType")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.SelectedCheckmarkType")]
		[OptionStrings(new string[] { "✓  ☐", "✓  X", "X  ☐", "Strike-through" })]
		[DefaultValue("✓  ☐")]
		public string SelectedCheckmarkType { get; set; }

		[BackgroundColor(200, 188, 172)]
		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.DrawNextMark")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.DrawNextMark")]
		public bool DrawNextMark { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.LootChecklist")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.LootChecklist")]
		public bool LootCheckVisibility { get; set; }

		[BackgroundColor(200, 188, 172)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.CheckDroppedLoot")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.CheckDroppedLoot")]
		public bool OnlyCheckDroppedLoot { get; set; }

		[Header("$Mods.BossChecklist.Configs.BossLogConfiguration.Header.BlindMode")]

		[BackgroundColor(255, 99, 71)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.EnableProgressionMode")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.EnableProgressionMode")]
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
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.DisableProgressionMode")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.DisableProgressionMode")]
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

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.ProgressionPrompt")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.ProgressionPrompt")]
		public bool PromptDisabled { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.MaskTextures")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.MaskTextures")]
		public bool MaskTextures { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.MaskNames")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.MaskNames")]
		public bool MaskNames { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.UnmaskNextCheck")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.UnmaskNextCheck")]
		public bool UnmaskNextBoss { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.MaskBossLoot")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.MaskBossLoot")]
		public bool MaskBossLoot { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Label.MaskHardMode")]
		[TooltipKey("$Mods.BossChecklist.Configs.BossLogConfiguration.Tooltip.MaskHardMode")]
		public bool MaskHardMode { get; set; }

		internal bool AnyProgressionModeConfigUsed => MaskTextures || MaskNames || MaskBossLoot || MaskHardMode;

		public void UpdateIndicators() {
			BossLogUI Log = BossUISystem.Instance.BossLog;
			string LangIndicator = "Mods.BossChecklist.Log.Indicator";
			string LangCommon = "Mods.BossChecklist.Log.Common";

			Log.Indicators[0].Color = OnlyShowBossContent ? Color.LightGreen : Color.DarkGray;
			Log.Indicators[0].hoverText = OnlyShowBossContent ? $"{LangIndicator}.OnlyBossContentEnabled" : $"{LangIndicator}.OnlyBossContentDisabled";

			if (ProgressionModeEnable) {
				Log.Indicators[1].Color = Color.Tomato;
				Log.Indicators[1].hoverText = Language.GetTextValue($"{LangIndicator}.ProgressionMode", Language.GetTextValue($"{LangCommon}.Enabled"));
			}
			else if (AnyProgressionModeConfigUsed) {
				Log.Indicators[1].Color = Color.Salmon;
				Log.Indicators[1].hoverText = Language.GetTextValue($"{LangIndicator}.ProgressionMode", Language.GetTextValue($"{LangCommon}.PartiallyEnabled"));
			}
			else {
				Log.Indicators[1].Color = Color.DarkGray;
				Log.Indicators[1].hoverText = Language.GetTextValue($"{LangIndicator}.ProgressionMode", Language.GetTextValue($"{LangCommon}.Disabled"));
			}
			BossChecklist.instance.Logger.Info(Log.Indicators[1].hoverText);
		}

		public override void OnChanged() {
			if (BossChecklist.instance == null)
				return;

			UpdateIndicators();
		}

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			return true;
		}
	}

	public class ClientConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.ClientConfig = this;

		[Header("$Mods.BossChecklist.Configs.OtherFeatures.Header.ChatMessages")]

		[DrawTicks]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.DespawnMessageType")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.DespawnMessageType")]
		[OptionStrings(new string[] { "Disabled", "Generic", "Unique" })]
		[DefaultValue("Generic")]
		public string DespawnMessageType { get; set; }

		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.LimbMessages")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.LimbMessages")]
		public bool LimbMessages { get; set; }

		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.PillarMessages")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.PillarMessages")]
		public bool PillarMessages { get; set; }

		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.TimerSounds")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.TimerSounds")]
		public bool TimerSounds { get; set; }

		[Header("$Mods.BossChecklist.Configs.OtherFeatures.Header.ItemMapDetection")]

		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.TreasureBags")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.TreasureBags")]
		public bool TreasureBagsBool { get; set; }

		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.Fragments")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.Fragments")]
		public bool FragmentsBool { get; set; }

		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.Scales")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.Scales")]
		public bool ScalesBool { get; set; }

		[Header("$Mods.BossChecklist.Configs.OtherFeatures.Header.BossRadar")]

		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.BossRadar")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.BossRadar")]
		public bool BossRadarBool { get; set; }

		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.RadarMiniBosses")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.RadarMiniBosses")]
		public bool RadarMiniBosses { get; set; }

		public const float OpacityFloatMin = 0.35f;
		public const float OpacityFloatMax = 0.85f;
		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.RadarOpacity")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.RadarOpacity")]
		[Range(OpacityFloatMin, OpacityFloatMax)]
		[DefaultValue(0.75f)]
		public float OpacityFloat { get; set; }

		[LabelKey("$Mods.BossChecklist.Configs.OtherFeatures.Label.RadarBlacklist")]
		[TooltipKey("$Mods.BossChecklist.Configs.OtherFeatures.Tooltip.RadarBlacklist")]
		public List<NPCDefinition> RadarBlacklist { get; set; } = new List<NPCDefinition>();

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context) {
			//Range attribute doesn't enforce it onto the value, it's a limit for the UI only, so we have to clamp it here again if user decides to edit it through the json
			//If this isn't in here, OpacityFloat can get negative for example, which will lead to a crash later
			OpacityFloat = Utils.Clamp(OpacityFloat, OpacityFloatMin, OpacityFloatMax);
		}

		public override void OnChanged() {
			BossRadarUI.blacklistChanged = true;
		}

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			return true;
		}
	}

	[BackgroundColor(128, 83, 0, 200)]
	public class DebugConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public override void OnLoaded() => BossChecklist.DebugConfig = this;
		
		private int processRecord = 0;
		private bool nrEnabled;
		private bool rtEnabled;

		[Header("$Mods.BossChecklist.Configs.DebugConfiguration.Header.Debug")]

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ModCallVerbose")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ModCallVerbose")]
		public bool ModCallLogVerbose { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ShowProgressionValue")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ShowProgressionValue")]
		public bool ShowProgressionValue { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.AccessInternalNames")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.AccessInternalNames")]
		public bool AccessInternalNames { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.CollectionTypeDetection")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.CollectionTypeDetection")]
		public bool ShowCollectionType { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.InactiveBossCheck")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.InactiveBossCheck")]
		public bool ShowInactiveBossCheck { get; set; }

		[Header("$Mods.BossChecklist.Configs.DebugConfiguration.Header.DebugRecordTracker")]

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.DisableNewRecords")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.DisableNewRecords")]
		public bool NewRecordsDisabled {
			get => processRecord == 1 && nrEnabled;
			set {
				if (!Main.gameMenu) {
					foreach (NPC npc in Main.npc) {
						if (!npc.active)
							continue;

						EntryInfo entry = NPCAssist.GetEntryInfo(npc.type);
						if (entry == null || entry.type != EntryType.Boss)
							continue;

						Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.DebugConfiguration.Notice.InvalidChange", entry.DisplayName), Color.Orange);
						return; // If a boss is active, debug features are disabled until all bosses are inactive
					}
				}
				if (value) {
					processRecord = 1;
				}
				nrEnabled = value;
			}
		}

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.DisableRecordTracking")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.DisableRecordTracking")]
		public bool RecordTrackingDisabled {
			get => processRecord == 2 && rtEnabled;
			set {
				if (!Main.gameMenu) {
					foreach (NPC npc in Main.npc) {
						if (!npc.active)
							continue;

						EntryInfo entry = NPCAssist.GetEntryInfo(npc.type);
						if (entry == null || entry.type != EntryType.Boss)
							continue;

						Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.DebugConfiguration.Notice.InvalidChange", entry.DisplayName), Color.Orange);
						return; // If a boss is active, debug features are disabled until all bosses are inactive
					}
				}
				if (value) {
					processRecord = 2;
				}
				rtEnabled = value;
			}
		}

		[BackgroundColor(255, 250, 250)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ShowRecordTracking")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ShowRecordTracking")]
		public NPCDefinition ShowTimerOrCounter { get; set; } = new NPCDefinition();

		[Header("$Mods.BossChecklist.Configs.DebugConfiguration.Header.DebugResetData")]

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ResetLoot")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ResetLoot")]
		public bool ResetLootItems { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ResetRecords")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ResetRecords")]
		public bool ResetRecordsBool { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ResetForcedDowns")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ResetForcedDowns")]
		public bool ResetForcedDowns { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.ResetHiddenEntries")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.ResetHiddenEntries")]
		public bool ResetHiddenEntries { get; set; }

		[Header("$Mods.BossChecklist.Configs.DebugConfiguration.Header.FeatureTesting")]

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.RecordFeatureTesting")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.RecordFeatureTesting")]
		public bool DISABLERECORDTRACKINGCODE { get; set; }

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(true)]
		[LabelKey("$Mods.BossChecklist.Configs.DebugConfiguration.Label.WorldRecordFeatureTesting")]
		[TooltipKey("$Mods.BossChecklist.Configs.DebugConfiguration.Tooltip.WorldRecordFeatureTesting")]
		public bool DisableWorldRecords { get; set; }

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

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			if (!IsPlayerLocalServerOwner(Main.player[whoAmI])) {
				message = Language.GetTextValue("Mods.BossChecklist.Configs.Notice.HostChange");
				return false;
			}
			return true;
		}
	}
}