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
	[Label("$Mods.BossChecklist.Configs.Title.BossLogCustomization")]
	public class BossLogConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.BossLogConfig = this;

		[Header("$Mods.BossChecklist.Configs.Header.BossLogUI")]

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(typeof(Color), "87, 181, 92, 255"), ColorNoAlpha]
		[Label("$Mods.BossChecklist.Configs.Label.BossLogColor")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.BossLogColor")]
		public Color BossLogColor { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(typeof(Vector2), "-270, -50")]
		[Range(-1920f, 0f)]
		[Label("$Mods.BossChecklist.Configs.Label.BossLogPos")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.BossLogPos")]
		public Vector2 BossLogPos { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.BossLogChecklist")]

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.OnlyBosses")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.OnlyBosses")]
		public bool OnlyBosses { get; set; }

		// TODO: [??] Change HideUnavailable and HideUnsupported to OptionStrings to allow users to choose betweem
		// 1.) Show on Table of Contents, but skip through page navigation
		// 2.) Show on Table of Contents, and allow visibility through page navigation
		// 3.) Hide on Table of Contents and Page Navigation

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.HideUnavailable")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnavailable")]
		public bool HideUnavailable { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.HideUnsupported")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnsupported")]
		public bool HideUnsupported { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterBosses")]
		[OptionStrings(new string[] { "Show", "Hide when completed" })]
		[DefaultValue("Show")]
		public string FilterBosses { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterMiniBosses")]
		[OptionStrings(new string[] { "Show", "Hide when completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterMiniBosses { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterEvents")]
		[OptionStrings(new string[] { "Show", "Hide when completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterEvents { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.DrawNextMark")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.DrawNextMark")]
		public bool DrawNextMark { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.ColoredBossText")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ColoredBossText")]
		public bool ColoredBossText { get; set; }

		[SliderColor(87, 181, 92)]
		[BackgroundColor(250, 235, 215)]
		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.SelectedCheckmarkType")]
		[OptionStrings(new string[] { "✓  ☐", "✓  X", "X  ☐", "Strike-through" })]
		[DefaultValue("✓  ☐")]
		public string SelectedCheckmarkType { get; set; }

		[BackgroundColor(250, 235, 215)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.LootChecklist")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.LootChecklist")]
		public bool LootCheckVisibility { get; set; }
		
		[Header("$Mods.BossChecklist.Configs.Header.BlindMode")]

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ProgressionPrompt")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ProgressionPrompt")]
		public bool PromptDisabled { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskTextures")]
		public bool MaskTextures { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskNames")]
		public bool MaskNames { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.UnmaskNextCheck")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.UnmaskNextCheck")]
		public bool UnmaskNextBoss { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskBossLoot")]
		public bool MaskBossLoot { get; set; }

		[BackgroundColor(178, 34, 34)]
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskHardMode")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.MaskHardMode")]
		public bool MaskHardMode { get; set; }

		internal bool AnyProgressionModeConfigUsed => MaskTextures || MaskNames || MaskBossLoot || MaskHardMode;

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			return true;
		}
	}

	[Label("$Mods.BossChecklist.Configs.Title.OtherFeatures")]
	public class ClientConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.ClientConfig = this;

		[Header("$Mods.BossChecklist.Configs.Header.ChatMessages")]

		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.DespawnMessageType")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.DespawnMessageType")]
		[OptionStrings(new string[] { "Disabled", "Generic", "Unique" })]
		[DefaultValue("Generic")]
		public string DespawnMessageType { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.LimbMessages")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.LimbMessages")]
		public bool LimbMessages { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.PillarMessages")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.PillarMessages")]
		public bool PillarMessages { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.TimerSounds")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.TimerSounds")]
		public bool TimerSounds { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.ItemMapDetection")]

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.TreasureBags")]
		public bool TreasureBagsBool { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.Fragments")]
		public bool FragmentsBool { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.Scales")]
		public bool ScalesBool { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.BossRadar")]

		[Label("$Mods.BossChecklist.Configs.Label.BossRadar")]
		[DefaultValue(true)]
		public bool BossRadarBool { get; set; }

		[Label("$Mods.BossChecklist.Configs.Label.RadarMiniBosses")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.RadarMiniBosses")]
		[DefaultValue(false)]
		public bool RadarMiniBosses { get; set; }

		public const float OpacityFloatMin = 0.35f;
		public const float OpacityFloatMax = 0.85f;
		[Label("$Mods.BossChecklist.Configs.Label.RadarOpacity")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.RadarOpacity")]
		[Range(OpacityFloatMin, OpacityFloatMax)]
		[DefaultValue(0.75f)]
		public float OpacityFloat { get; set; }

		[Label("$Mods.BossChecklist.Configs.Label.RadarBlacklist")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.RadarBlacklist")]
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
	[Label("$Mods.BossChecklist.Configs.Title.Debug")]
	public class DebugConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public override void OnLoaded() => BossChecklist.DebugConfig = this;
		
		private int processRecord = 0;
		private bool nrEnabled;
		private bool rtEnabled;

		[Header("$Mods.BossChecklist.Configs.Header.Debug")]

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ModCallVerbose")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ModCallVerbose")]
		public bool ModCallLogVerbose { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ShowProgressionValue")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ShowProgressionValue")]
		public bool ShowProgressionValue { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.AccessInternalNames")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.AccessInternalNames")]
		public bool AccessInternalNames { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.CollectionTypeDetection")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.CollectionTypeDetection")]
		public bool ShowCollectionType { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.InactiveBossCheck")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.InactiveBossCheck")]
		public bool ShowInactiveBossCheck { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.DebugRecordTracker")]

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.DisableNewRecords")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.DisableNewRecords")]
		public bool NewRecordsDisabled {
			get => processRecord == 1 && nrEnabled;
			set {
				if (!Main.gameMenu) {
					for (int i = 0; i < Main.maxNPCs; i++) {
						if (!Main.npc[i].active) {
							continue;
						}
						if (NPCAssist.GetBossInfoIndex(Main.npc[i].type) != -1) {
							Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.Notice.InvalidChange"), Color.Orange);
							return; // If a boss/miniboss is active, debug features are disabled until all bosses are inactive
						}
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
		[Label("$Mods.BossChecklist.Configs.Label.DisableRecordTracking")]
		public bool RecordTrackingDisabled {
			get => processRecord == 2 && rtEnabled;
			set {
				if (!Main.gameMenu) {
					for (int i = 0; i < Main.maxNPCs; i++) {
						if (!Main.npc[i].active) {
							continue;
						}
						if (NPCAssist.GetBossInfoIndex(Main.npc[i].type) != -1) {
							Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.Notice.InvalidChange"), Color.Orange);
							return; // If a boss/miniboss is active, debug features are disabled until all bosses are inactive
						}
					}
				}
				if (value) {
					processRecord = 2;
				}
				rtEnabled = value;
			}
		}

		// TODO: Get timers and counters to properly visualize itself in Multiplayer
		[BackgroundColor(255, 250, 250)]
		[Label("$Mods.BossChecklist.Configs.Label.ShowRecordTracking")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ShowRecordTracking")]
		public NPCDefinition ShowTimerOrCounter { get; set; } = new NPCDefinition();

		[Header("$Mods.BossChecklist.Configs.Header.DebugResetData")]

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetLoot")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetLoot")]
		public bool ResetLootItems { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetRecords")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetRecords")]
		public bool ResetRecordsBool { get; set; }

		[BackgroundColor(80, 80, 80)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetForcedDowns")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetForcedDowns")]
		public bool ResetForcedDowns { get; set; }

		[BackgroundColor(255, 250, 250)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetHiddenEntries")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetHiddenEntries")]
		public bool ResetHiddenEntries { get; set; }

		[Header("[i:2172] [c/ffeb6e:Feature Testing]")]

		[BackgroundColor(255, 99, 71)]
		[DefaultValue(true)]
		[Label("Disable Boss Record feature testing")]
		[Tooltip("Boss Checklist may currently have issues related to Boss Records feature." +
			"\nThis is a temporary config to prevent any Record Tracking code from running and will be removed when the feature is fully developed and out of the testing phase." +
			"\nPlease keep this config enabled unless you are helping test out the feature and are willing to send reports to the mod developers.")]
		public bool DISABLERECORDTRACKINGCODE { get; set; }

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