using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.ID;

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

		/*
		internal ExpandoObject ConvertToExpandoObject() {
			dynamic expando = new ExpandoObject();

			expando.key = Key;
			expando.modSource = modSource;
			expando.internalName = internalName;
			expando.displayName = name;

			expando.progression = progression;
			expando.downed = new Func<bool>(downed);

			expando.isBoss = type.Equals(EntryType.Boss);
			expando.isMiniboss = type.Equals(EntryType.MiniBoss);
			expando.isEvent = type.Equals(EntryType.Event);

			expando.npcIDs = new List<int>(npcIDs);
			expando.spawnItem = new List<int>(spawnItem);
			expando.loot = new List<int>(loot);
			expando.collection = new List<int>(collection);

			return expando;
		}
		*/

		internal Dictionary<string, object> ConvertToDictionary(Version GetBossInfoAPIVersion) {
			// We may want to allow different returns based on api version.
			//if (GetBossInfoAPIVersion == new Version(1, 1)) {
			var dict = new Dictionary<string, object> {
				{ "key", Key },
				{ "modSource", modSource },
				{ "displayName", name },
				{ "internalName", internalName },

				{ "progression", progression },
				{ "downed", new Func<bool>(downed) },

				{ "isBoss", type.Equals(EntryType.Boss) },
				{ "isMiniboss", type.Equals(EntryType.MiniBoss) },
				{ "isEvent", type.Equals(EntryType.Event) },

				{ "npcIDs", new List<int>(npcIDs) },
				{ "spawnItem", new List<int>(spawnItem) },
				{ "loot", new List<int>(loot) },
				{ "collection", new List<int>(collection) }
			};

			return dict;
		}

		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : SourceDisplayNameWithoutChatTags(ModLoader.GetMod(modSource).DisplayName);
		
		internal string SourceDisplayNameWithoutChatTags(string modSource) {
			string editedName = "";

			for (int c = 0; c < modSource.Length; c++) {
				// Add each character one by one to find chattags in order
				// Chat tags cannot be contained inside other chat tags so no need to worry about overlap
				editedName += modSource[c];
				if (editedName.Contains("[i:") && editedName.EndsWith("]")) {
					// Update return name if a complete item chat tag is found
					editedName = editedName.Substring(0, editedName.IndexOf("[i:"));
					continue;
				}
				if (editedName.Contains("[i/") && editedName.EndsWith("]")) {
					// Update return name if a complete item chat tag is found
					editedName = editedName.Substring(0, editedName.IndexOf("[i/"));
					continue;
				}
				if (editedName.Contains("[c/") && editedName.Contains(":") && editedName.EndsWith("]")) {
					// Color chat tags are edited differently as we want to keep the text that's nested inside them
					string part1 = editedName.Substring(0, editedName.IndexOf("[c/"));
					string part2 = editedName.Substring(editedName.IndexOf(":") + 1);
					part2 = part2.Substring(0, part2.Length - 1);
					editedName = part1 + part2;
					continue;
				}
			}
			return editedName;
		}

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
			//this.collectType = SetupCollectionTypes(this.collection); Do this during the BossFinalization for orphan data
			this.loot = loot ?? new List<int>();
			this.info = info ?? "";
			if (this.info == "") {
				this.info = "Mods.BossChecklist.BossLog.DrawnText.NoInfo";
			}
			this.despawnMessage = despawnMessage?.StartsWith("$") == true ? despawnMessage.Substring(1) : despawnMessage;
			if (string.IsNullOrEmpty(this.despawnMessage) && type == EntryType.Boss) {
				this.despawnMessage = "Mods.BossChecklist.BossVictory.Generic";
			}

			this.pageTexture = pageTexture ?? $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			if (!Main.dedServ && !ModContent.HasAsset(this.pageTexture) && this.pageTexture != "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
				if (SourceDisplayName != "Terraria" && SourceDisplayName != "Unknown") {
					BossChecklist.instance.Logger.Warn($"Boss Display Texture for {SourceDisplayName} {this.name} named {this.pageTexture} is missing");
				}
				this.pageTexture = $"BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			}
			this.overrideIconTexture = overrideIconTexture ?? "";
			if (!Main.dedServ && !ModContent.HasAsset(this.overrideIconTexture) && this.overrideIconTexture != "") {
				// If unused, no overriding is needed. If used, we attempt to override the texture used for the boss head icon in the Boss Log.
				if (SourceDisplayName != "Terraria" && SourceDisplayName != "Unknown") {
					BossChecklist.instance.Logger.Warn($"Boss Head Icon Texture for {SourceDisplayName} {this.name} named {this.overrideIconTexture} is missing");
				}
				this.overrideIconTexture = "Terraria/Images/NPC_Head_0";
			}
			this.available = available ?? (() => true);
			this.hidden = false;

			// Add to Opted Mods if a new mod
			if (modSource != "Unknown" && modSource != "Terraria") {
				if (!BossUISystem.Instance.OptedModNames.Contains(SourceDisplayNameWithoutChatTags(modSource))) {
					BossUISystem.Instance.OptedModNames.Add(SourceDisplayNameWithoutChatTags(modSource));
				}
			}
		}

		// Workaround for vanilla events with illogical translation keys.
		internal BossInfo WithCustomTranslationKey(string translationKey) {
			this.name = Language.GetTextValue(translationKey.Substring(1));
			return this;
		}

		internal BossInfo WithCustomAvailability(Func<bool> funcBool) {
			this.available = funcBool;
			return this;
		}

		internal static BossInfo MakeVanillaBoss(EntryType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.Substring(name.LastIndexOf("."));
			string tremor = name == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";
			return new BossInfo(
				type,
				progression,
				"Terraria",
				name,
				ids,
				downed,
				() => true,
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

		internal static List<CollectionType> SetupCollectionTypes(List<int> collection) {
			List<CollectionType> setup = new List<CollectionType>();
			foreach (int type in collection) {
				Item temp = new Item();
				temp.SetDefaults(type);
				if (temp.headSlot > 0 && temp.vanity) {
					setup.Add(CollectionType.Mask);
				}
				else if (BossChecklist.vanillaMusicBoxTypes.Contains(type) ||  BossChecklist.otherWorldMusicBoxTypes.Contains(type) || BossChecklist.itemToMusicReference.ContainsKey(type)) {
					setup.Add(CollectionType.MusicBox);
				}
				else if (temp.master && temp.shoot > ProjectileID.None && temp.buffType > 0) {
					setup.Add(CollectionType.Pet);
				}
				else if (temp.master && temp.mountType > MountID.None) {
					setup.Add(CollectionType.Mount);
				}
				else if (temp.createTile > TileID.Dirt) {
					TileObjectData data = TileObjectData.GetTileData(temp.createTile, temp.placeStyle);
					if (data.AnchorWall == TileObjectData.Style3x3Wall.AnchorWall && data.Width == 3 && data.Height == 3) {
						setup.Add(CollectionType.Trophy);
					}
					else if (temp.master && data.Width == 3 && data.Height == 4) {
						setup.Add(CollectionType.Relic);
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
