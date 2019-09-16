using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace BossChecklist
{
	internal class BossInfo // Inheritance for Event instead?
	{
		internal float progression;
		internal List<int> npcIDs;
		internal string modSource;
		internal string name; // TODO: localized name and non localized for mod.call 
		internal Func<bool> downed;

		internal List<int> spawnItem;
		internal List<int> loot;
		internal List<int> collection;
		internal string pageTexture;

		internal string info;
		internal Func<bool> available;
		internal bool hidden;
		internal BossChecklistType type;

		internal string SourceDisplayName => modSource == "Vanilla" || modSource == "Unknown" ? modSource : ModLoader.GetMod(modSource).DisplayName;

		internal BossInfo(BossChecklistType type, float progression, string modSource, string name, List<int> npcIDs, Func<bool> downed, Func<bool> available, List<int> spawnItem, List<int> collection, List<int> loot, string pageTexture, string info) {
			this.type = type;
			this.progression = progression;
			this.modSource = modSource;
			this.name = name;
			this.npcIDs = npcIDs;
			this.downed = downed;
			this.available = available ?? (() => true);

			this.spawnItem = spawnItem;
			this.collection = collection;
			this.loot = loot;
			this.pageTexture = pageTexture;
			if (this.pageTexture == null || !Terraria.ModLoader.ModContent.TextureExists(this.pageTexture)) {
				this.pageTexture = $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
				BossChecklist.instance.Logger.Info($"Boss Display Texture for {SourceDisplayName} {this.name} named {this.pageTexture} missing");
			}
			this.info = info;
			this.hidden = false;
		}

		internal static BossInfo MakeVanillaBoss(BossChecklistType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			return new BossInfo(type, progression, "Vanilla", name, ids, downed, () => true, spawnItem, BossChecklist.bossTracker.SetupCollect(ids[0]), BossChecklist.bossTracker.SetupLoot(ids[0]), $"BossChecklist/Resources/BossTextures/Boss{ids[0]}", BossChecklist.bossTracker.SetupSpawnDesc(ids[0]));
		}

		internal static BossInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> spawnItem, string image = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
			return new BossInfo(BossChecklistType.Event, progression, "Vanilla", name, BossChecklist.bossTracker.SetupEventNPCList(name), downed, () => true, spawnItem, BossChecklist.bossTracker.SetupEventCollectibles(name), BossChecklist.bossTracker.SetupEventNPCList(name), image, BossChecklist.bossTracker.SetupEventSpawnDesc(name));
		}

		public override string ToString() => $"{progression} {name} {modSource}";
	}
}
