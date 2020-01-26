using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.ObjectData;

namespace BossChecklist
{
	internal class BossInfo // Inheritance for Event instead?
	{
		internal float progression;
		internal List<int> npcIDs;
		internal string modSource;
		internal string name; // display name
		internal string internalName; // This should be unique per mod.
		internal string Key => modSource + " " + internalName; // This localization-ignoring string is used for cross mod queries and networking, is totally unique.
		internal Func<bool> downed;

		internal List<int> spawnItem;
		internal List<int> loot;
		internal List<int> collection;
		internal List<CollectionType> collectType;

		internal string despawnMessage;
		internal string pageTexture;
		internal string overrideIconTexture;

		internal string info;
		internal Func<bool> available;
		internal bool hidden;
		internal EntryType type;

		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : ModLoader.GetMod(modSource).DisplayName;

		internal BossInfo(EntryType type, float progression, string modSource, string name, List<int> npcIDs, Func<bool> downed, Func<bool> available, List<int> spawnItem, List<int> collection, List<int> loot, string pageTexture, string info, string despawnMessage = "", string overrideIconTexture = "") {
			this.type = type;
			this.progression = progression;
			this.modSource = modSource;
			this.internalName = name.StartsWith("$") ? name.Substring(name.LastIndexOf('.') + 1) : name;
			this.name = name;
			this.npcIDs = npcIDs ?? new List<int>();
			this.downed = downed;
			this.spawnItem = spawnItem ?? new List<int>();
			this.collection = collection ?? new List<int>();
			this.collectType = SetupCollectionTypes(this.collection);
			this.loot = loot ?? new List<int>();
			this.info = info ?? "";
			if (this.info == "") this.info = "Mods.BossChecklist.BossLog.DrawnText.NoInfo";
			this.despawnMessage = despawnMessage?.StartsWith("$") == true ? despawnMessage.Substring(1) : despawnMessage;
			if ((this.despawnMessage == null || this.despawnMessage == "") && type == EntryType.Boss) {
				this.despawnMessage = "Mods.BossChecklist.BossVictory.Generic";
			}
			this.pageTexture = pageTexture ?? $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			if (!ModContent.TextureExists(this.pageTexture) && this.pageTexture != $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
				if (SourceDisplayName != "Terraria" && SourceDisplayName != "Unknown") BossChecklist.instance.Logger.Info($"Boss Display Texture for {SourceDisplayName} {this.name} named {this.pageTexture} is missing");
				this.pageTexture = $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			}
			this.overrideIconTexture = overrideIconTexture ?? "";
			if (!ModContent.TextureExists(this.overrideIconTexture) && this.overrideIconTexture != "") {
				// If unused, no overriding is needed. If used, we attempt to override the texture used for the boss head icon in the Boss Log.
				if (SourceDisplayName != "Terraria" && SourceDisplayName != "Unknown") BossChecklist.instance.Logger.Info($"Boss Head Icon Texture for {SourceDisplayName} {this.name} named {this.overrideIconTexture} is missing");
				this.overrideIconTexture = "Terraria/NPC_Head_0";
			}
			this.available = available ?? (() => true);

			this.hidden = false;
		}

		// Workaround for vanilla events with illogical translation keys.
		internal BossInfo WithCustomTranslationKey(string translationKey) {
			this.name = Language.GetTextValue(translationKey.Substring(1));
			return this;
		}

		internal static BossInfo MakeVanillaBoss(EntryType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			Func<bool> avail = () => true;
			if (name == "$NPCName.EaterofWorldsHead") avail = () => !WorldGen.crimson;
			else if (name == "$NPCName.BrainofCthulhu") avail = () => WorldGen.crimson;
			string nameKey = name.Substring(name.LastIndexOf("."));
			string tremor = name == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";
			return new BossInfo(
				type,
				progression,
				"Terraria",
				name,
				ids,
				downed,
				avail,
				spawnItem,
				BossChecklist.bossTracker.SetupCollect(ids[0]),
				BossChecklist.bossTracker.SetupLoot(ids[0]),
				$"BossChecklist/Resources/BossTextures/Boss{ids[0]}",
				$"Mods.BossChecklist.BossSpawnInfo{nameKey}{tremor}",
				$"Mods.BossChecklist.BossVictory{nameKey}"
			);
		}

		internal static BossInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.Replace(" ", "").Replace("'", "");
			return new BossInfo(
				EntryType.Event,
				progression,
				"Terraria",
				name,
				BossChecklist.bossTracker.SetupEventNPCList(name),
				downed,
				() => true,
				spawnItem,
				BossChecklist.bossTracker.SetupEventCollectibles(name),
				BossChecklist.bossTracker.SetupEventLoot(name),
				$"BossChecklist/Resources/BossTextures/Event{nameKey}",
				$"Mods.BossChecklist.BossSpawnInfo.{nameKey}"
			);
		}

		internal List<CollectionType> SetupCollectionTypes(List<int> collection) {
			List<CollectionType> setup = new List<CollectionType>();
			foreach (int type in collection) {
				Item temp = new Item();
				temp.SetDefaults(type);
				if (temp.headSlot > 0 && temp.vanity) setup.Add(CollectionType.Mask);
				else if (BossChecklist.vanillaMusicBoxTypes.Contains(type) || BossChecklist.itemToMusicReference.ContainsKey(type)) setup.Add(CollectionType.MusicBox);
				else if (temp.createTile > 0) {
					TileObjectData data = TileObjectData.GetTileData(temp.createTile, temp.placeStyle);
					if (data.AnchorWall == TileObjectData.Style3x3Wall.AnchorWall && data.Width == 3 && data.Height == 3) {
						setup.Add(CollectionType.Trophy);
					}
					else setup.Add(CollectionType.Generic);
				}
				else setup.Add(CollectionType.Generic);
			}
			return setup;
		}

		public override string ToString() => $"{progression} {name} {modSource}";
	}

	internal class OrphanInfo
	{
		internal OrphanType type;
		internal string modSource;
		internal string internalName;
		internal string Key => modSource + " " + internalName;
		internal List<int> values;

		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : ModLoader.GetMod(modSource).DisplayName;

		internal OrphanInfo(OrphanType type, string modSource, string internalName, List<int> values) {
			this.type = type;
			this.modSource = modSource;
			this.internalName = internalName;
			this.values = values;
		}
	}
}
