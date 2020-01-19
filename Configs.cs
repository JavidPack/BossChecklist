using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
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

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.DrawNextMark")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.DrawNextMark")]
		public bool DrawNextMark { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.ColoredBossText")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.ColoredBossText")]
		public bool ColoredBossText { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.CountDownedBosses")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.CountDownedBosses")]
		public bool CountDownedBosses { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.BossSilhouettes")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.BossSilhouettes")]
		public bool BossSilhouettes { get; set; }

		[DrawTicks]
		[Label("$Mods.BossChecklist.Configs.Label.SelectedCheckmarkType")]
		[OptionStrings(new string[] { "✓  ☐", "✓  X", "X  ☐" })]
		[DefaultValue("✓  ☐")]
		public string SelectedCheckmarkType { get; set; }

		// TODO Config? Show Prev Records: [Show if available, show if not beaten, show if beaten, never show]
		// TODO Config? Clear prev records OnEnterWorld (or maybe make it a button in boss log?)

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
		[Label("$Mods.BossChecklist.Configs.Label.HideUnavailable")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnavailable")]
		public bool HideUnavailable { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.HideUnsupported")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.HideUnsupported")]
		public bool HideUnsupported { get; set; }

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

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.PillarMessages")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.PillarMessages")]
		public bool PillarMessages { get; set; }

		[DefaultValue(false)]
		[Label("$Mods.BossChecklist.Configs.Label.LimbMessages")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.LimbMessages")]
		public bool LimbMessages { get; set; }

		[DefaultValue(true)]
		[Label("$Mods.BossChecklist.Configs.Label.RespawnTimer")]
		public bool RespawnTimerEnabled { get; set; }

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

		[Label("$Mods.BossChecklist.Configs.Label.RadarOpacity")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.RadarOpacity")]
		[Range(0.35f, 0.85f)]
		[DefaultValue(0.75f)]
		public float OpacityFloat { get; set; }

		[Label("$Mods.BossChecklist.Configs.Label.RadarBlacklist")]
		[Tooltip("$Mods.BossChecklist.Configs.Tooltip.RadarBlacklist")]
		public List<NPCDefinition> RadarBlacklist { get; set; } = new List<NPCDefinition>();

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
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public override void OnLoaded() => BossChecklist.DebugConfig = this;
		
		private int processRecord = 0;
		private bool nrEnabled;
		private bool rtEnabled;

		[Header("$Mods.BossChecklist.Configs.Header.Debug")]
		
		[BackgroundColor(85, 55, 120)]
		[DefaultValue(false)]
		[Label("Reset Records Option")]
		[Tooltip("Reset records with a boss by double right-clicking the 'Records' button of the selected boss page")]
		public bool ResetRecordsBool { get; set; }

		[BackgroundColor(85, 55, 120)]
		[DefaultValue(false)]
		[Label("Reset Loot/Collection")]
		[Tooltip("Remove a selected item from your saved loot/collection by double right-clicking the selected item slot\nClear the entire loot/collection list by double right-clicking the 'Loot / Collection' button\nHold Alt for either of these to apply the effect to ALL bosses")]
		public bool ResetLootItems { get; set; }

		[DefaultValue(false)]
		[Label("Show Internal Names")]
		[Tooltip("Replaces boss names with their internal names.\nGood for mod developers who need it for cross-mod content")]
		public bool ShowInternalNames { get; set; }

		[DefaultValue(false)]
		[Label("Show Auto-detected Collection Type")]
		[Tooltip("This will show what items our system has found as a trophy, mask, or music box on the collection page")]
		public bool ShowCollectionType { get; set; }

		[DefaultValue(false)]
		[Label("Truely Dead Check")]
		[Tooltip("When a boss NPC dies, it mentions in chat if the boss is completely gone")]
		public bool ShowTDC { get; set; }

		[BackgroundColor(55, 85, 120)]
		[DefaultValue(false)]
		[Label("Disable new records from being recorded to the boss log")]
		public bool NewRecordsDisabled {
			get { return processRecord == 1 && nrEnabled; }
			set {
				if (!Main.gameMenu) {
					for (int i = 0; i < Main.maxNPCs; i++) {
						if (!Main.npc[i].active) continue;
						if (NPCAssist.ListedBossNum(Main.npc[i]) != -1) {
							Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.Notice.InvalidChange"), Color.Orange);
							return; // If a boss/miniboss is active, debug features are disabled until all bosses are inactive
						}
					}
				}
				if (value) processRecord = 1;
				nrEnabled = value;
			}
		}

		[BackgroundColor(55, 85, 120)]
		[DefaultValue(false)]
		[Label("Disable records from being tracked entirely")]
		public bool RecordTrackingDisabled {
			get { return processRecord == 2 && rtEnabled; }
			set {
				if (!Main.gameMenu) {
					for (int i = 0; i < Main.maxNPCs; i++) {
						if (!Main.npc[i].active) continue;
						if (NPCAssist.ListedBossNum(Main.npc[i]) != -1) {
							Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.Notice.InvalidChange"), Color.Orange);
							return; // If a boss/miniboss is active, debug features are disabled until all bosses are inactive
						}
					}
				}
				if (value) processRecord = 2;
				rtEnabled = value;
			}
		}

		// TODO: Get timers and counters to properly visualize itself in Multiplayer
		[BackgroundColor(55, 85, 120)]
		[Label("Show record timers and counters of selected NPC")]
		[Tooltip("This will only show if Record Tracking is NOT disabled\nNOTE: This debug feature only works in singleplayer currently!")]
		public NPCDefinition ShowTimerOrCounter { get; set; } = new NPCDefinition();

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			return true;
		}
	}
}