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
	[Label("$Mods.BossChecklist.Configs.Title.BossLogCustomization")]
	public class BossLogConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.BossLogConfig = this;

		[Header("$Mods.BossChecklist.Configs.Header.BossLogUI")]

		[DefaultValue(typeof(Color), "87, 181, 92, 255")]
		[Label("$Mods.BossChecklist.Configs.Label.BossLogColor")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.BossLogColor")]
		public Color BossLogColor { get; set; }

		[DefaultValue(typeof(Vector2), "-270, -50")]
		[Range(-1920f, 0f)]
		[Label("$Mods.BossChecklist.Configs.Label.BossLogPos")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.BossLogPos")]
		public Vector2 BossLogPos { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.BossLogChecklist")]

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.OnlyBosses")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.OnlyBosses")]
		public bool OnlyBosses { get; set; }
		
		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.HideUnavailable")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnavailable")]
		public bool HideUnavailable { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.HideUnsupported")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnsupported")]
		public bool HideUnsupported { get; set; }
		
		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterBosses")]
		[OptionStrings(new string[] { "Show", "Hide when completed" })]
		[DefaultValue("Show")]
		public string FilterBosses { get; set; }

		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterMiniBosses")]
		[OptionStrings(new string[] { "Show", "Hide when completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterMiniBosses { get; set; }

		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.FilterEvents")]
		[OptionStrings(new string[] { "Show", "Hide when completed", "Hide" })]
		[DefaultValue("Show")]
		public string FilterEvents { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.DrawNextMark")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.DrawNextMark")]
		public bool DrawNextMark { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.ColoredBossText")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ColoredBossText")]
		public bool ColoredBossText { get; set; }

		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.SelectedCheckmarkType")]
		[OptionStrings(new string[] { "✓  ☐", "✓  X", "X  ☐", "Strike-through" })]
		[DefaultValue("✓  ☐")]
		public string SelectedCheckmarkType { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.LootChecklist")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.LootChecklist")]
		public bool LootCheckVisibility { get; set; }
		
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.CountDownedBosses")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.CountDownedBosses")]
		public bool CountDownedBosses { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.BlindMode")]

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ProgressionPrompt")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ProgressionPrompt")]
		public bool PromptDisabled { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskTextures")]
		public bool MaskTextures { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskNames")]
		public bool MaskNames { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.UnmaskNextCheck")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.UnmaskNextCheck")]
		public bool UnmaskNextBoss { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskBossLoot")]
		public bool MaskBossLoot { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.MaskHardMode")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.MaskHardMode")]
		public bool MaskHardMode { get; set; }

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

	[Label("$Mods.BossChecklist.Configs.Title.Debug")]
	public class DebugConfiguration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public override void OnLoaded() => BossChecklist.DebugConfig = this;
		
		private int processRecord = 0;
		private bool nrEnabled;
		private bool rtEnabled;

		[Header("$Mods.BossChecklist.Configs.Header.Debug")]

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ModCallVerbose")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ModCallVerbose")]
		public bool ModCallLogVerbose { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.AccessInternalNames")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.AccessInternalNames")]
		public bool AccessInternalNames { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.CollectionTypeDetection")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.CollectionTypeDetection")]
		public bool ShowCollectionType { get; set; }

		[BackgroundColor(55, 85, 120)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.InactiveBossCheck")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.InactiveBossCheck")]
		public bool ShowInactiveBossCheck { get; set; }

		[Header("$Mods.BossChecklist.Configs.Header.DebugBossLogFeatures")]

		[BackgroundColor(85, 55, 120)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetLoot")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetLoot")]
		public bool ResetLootItems { get; set; }

		[BackgroundColor(85, 55, 120)]
		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.ResetRecords")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ResetRecords")]
		public bool ResetRecordsBool { get; set; }

		[BackgroundColor(55, 85, 120)]
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

		[BackgroundColor(55, 85, 120)]
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

		[BackgroundColor(55, 85, 120)]
		[DefaultValue(true)]
		[Label("KEEP THIS CONFIG ENABLED FOR NOW")]
		[Tooltip("Boss Checklist is currently going through some issues right now related to Boss Records." +
			"\nThis is a temporary config to prevent any Record Tracking code from running and will be removed when the issue is fully resolved." +
			"\nPlease keep this config ENABLED unless you are helping test to see if things run smoothly again before this config is removed." +
			"\nSorry for any trouble this has caused. -SheepishShepherd ")]
		public bool DISABLERECORDTRACKINGCODE { get; set; }

		// TODO: Get timers and counters to properly visualize itself in Multiplayer
		[BackgroundColor(55, 85, 120)]
		[Label("$Mods.BossChecklist.Configs.Label.ShowRecordTracking")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ShowRecordTracking")]
		public NPCDefinition ShowTimerOrCounter { get; set; } = new NPCDefinition();

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