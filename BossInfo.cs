using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace BossChecklist
{
	internal class BossInfo // Inheritance for Event instead?
	{
		internal float progression;
		internal List<int> npcIDs;
		internal string modSource;
		internal string name;
		internal Func<bool> downed;

		internal List<int> spawnItem;
		internal List<int> loot;
		internal List<int> collection;
		internal string pageTexture;
		internal string overrideIconTexture;

		internal string info;
		internal Func<bool> available;
		internal bool hidden;
		internal BossChecklistType type;

		internal string SourceDisplayName => modSource == "Vanilla" || modSource == "Unknown" ? modSource : ModLoader.GetMod(modSource).DisplayName;

		internal BossInfo(BossChecklistType type, float progression, string modSource, string name, List<int> npcIDs, Func<bool> downed, Func<bool> available, List<int> spawnItem, List<int> collection, List<int> loot, string pageTexture, string info, string overrideIconTexture = "") {
			this.type = type;
			this.progression = progression;
			this.modSource = modSource;
			this.name = name.StartsWith("$") ? Language.GetTextValue(name.Substring(1)) : name;
			this.npcIDs = npcIDs;
			this.downed = downed;
			this.available = available ?? (() => true);
			this.spawnItem = spawnItem;
			this.collection = collection;
			this.loot = loot;
			this.pageTexture = pageTexture;
			if (this.pageTexture == null || !ModContent.TextureExists(this.pageTexture)) {
				if (SourceDisplayName != "Vanilla" && SourceDisplayName != "Unknown") BossChecklist.instance.Logger.Info($"Boss Display Texture for {SourceDisplayName} {this.name} named {this.pageTexture} is missing");
				this.pageTexture = $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			}
			this.overrideIconTexture = overrideIconTexture;
			if ((this.overrideIconTexture == null || !ModContent.TextureExists(this.pageTexture)) && this.overrideIconTexture != "") {
				// If unused, no overriding is needed. If used, we attempt to override the texture used for the boss head icon in the Boss Log.
				if (SourceDisplayName != "Vanilla" && SourceDisplayName != "Unknown") BossChecklist.instance.Logger.Info($"Boss Head Icon Texture for {SourceDisplayName} {this.name} named {this.overrideIconTexture} is missing");
				this.overrideIconTexture = "Terraria/NPC_Head_0";
			}
			this.info = info;
			this.hidden = false;
		}

		internal static BossInfo MakeVanillaBoss(BossChecklistType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			Func<bool> avail = () => true;
			if (name == "Eater of Worlds") avail = () => !WorldGen.crimson;
			else if (name == "Brain of Cthulhu") avail = () => WorldGen.crimson;
			return new BossInfo(type, progression, "Vanilla", name, ids, downed, avail, spawnItem, BossChecklist.bossTracker.SetupCollect(ids[0]), BossChecklist.bossTracker.SetupLoot(ids[0]), $"BossChecklist/Resources/BossTextures/Boss{ids[0]}", BossChecklist.bossTracker.SetupSpawnDesc(ids[0]));
		}

		internal static BossInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> spawnItem, string image = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
			return new BossInfo(BossChecklistType.Event, progression, "Vanilla", name, BossChecklist.bossTracker.SetupEventNPCList(name), downed, () => true, spawnItem, BossChecklist.bossTracker.SetupEventCollectibles(name), BossChecklist.bossTracker.SetupEventLoot(name), image, BossChecklist.bossTracker.SetupEventSpawnDesc(name));
		}

		public override string ToString() => $"{progression} {name} {modSource}";
	}

	internal class OrphanInfo
	{
		internal OrphanType type;
		internal string modSource;
		internal string name;
		internal List<int> values;

		internal string SourceDisplayName => modSource == "Vanilla" || modSource == "Unknown" ? modSource : ModLoader.GetMod(modSource).DisplayName;

		internal OrphanInfo(OrphanType type, string modSource, string name, List<int> values) {
			this.type = type;
			this.modSource = modSource;
			this.name = name;
			this.values = values;
		}
	}
}
