using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace BossChecklist
{
	internal class BossInfo // Inheritance for Event instead?
	{
		internal float progression;
		internal List<int> ids; // TODO: rename to npcIDs
		internal string source; // TODO: rename to modSource
		internal string name; // TODO: localized name and non localized for mod.call 
		internal Func<bool> downed;

		internal List<int> spawnItem;
		internal List<int> loot;
		internal List<int> collection;
		internal string pageTexture;

		internal string info;
		internal Func<bool> available;
		internal bool hidden;
		//internal int spawnItemID;
		internal BossChecklistType type;

		internal string SourceDisplayName => source == "Vanilla" || source == "Unknown" ? source : ModLoader.GetMod(source).DisplayName;

		internal BossInfo(BossChecklistType type, float progression, string source, string name, List<int> ids, Func<bool> downed, Func<bool> available, List<int> spawnItem, List<int> collection, List<int> loot, string pageTexture, string info) {
			this.type = type;
			this.progression = progression;
			this.source = source;
			this.name = name;
			this.ids = ids;
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

		internal static BossInfo MakeVanillaBoss(BossChecklistType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem, string info) {
			return new BossInfo(type, progression, "Vanilla", name, ids, downed, () => true, spawnItem, BossChecklist.bossTracker.SetupCollect(ids[0]), BossChecklist.bossTracker.SetupLoot(ids[0]), $"BossChecklist/Resources/BossTextures/Boss{ids[0]}", info);
		}

		internal static BossInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> npcs, List<int> spawnItem, List<int> loot, string info, string image = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
			// TODO: Event loot? npc? texture?
			return new BossInfo(BossChecklistType.Event, progression, "Vanilla", name, npcs, downed, () => true, spawnItem, new List<int>(), loot, image, info);
		}

		public override string ToString() {
			return $"{progression} {name} {source}";
		}
	}
}
