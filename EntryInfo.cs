using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace BossChecklist
{
	internal class EntryInfo // Inheritance for Event instead?
	{
		// This localization-ignoring string is used for cross mod queries and networking. Each key is completely unique.
		internal string Key => modSource + " " + internalName;

		internal EntryType type;
		internal string modSource;
		internal string internalName; // This should be unique per mod.
		internal string name; // This should not be used for displaying purposes. Use 'EntryInfo.GetDisplayName' instead.
		internal List<int> npcIDs;
		internal float progression;
		internal Func<bool> downed;
		internal Func<bool> available;
		internal bool hidden;
		internal Func<NPC, string> customDespawnMessages;

		internal List<string> relatedEntries;

		internal List<int> spawnItem;
		internal string spawnInfo;

		internal int treasureBag = 0;
		internal List<int> collection;
		internal Dictionary<int, CollectionType> collectType;
		internal List<DropRateInfo> loot;
		internal List<int> lootItemTypes;

		internal Asset<Texture2D> portraitTexture;
		internal Action<SpriteBatch, Rectangle, Color> customDrawing;
		internal List<Asset<Texture2D>> headIconTextures;

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

		internal Dictionary<string, object> ConvertToDictionary(Version GetEntryInfoAPIVersion) {
			// We may want to allow different returns based on api version.
			//if (GetEntryInfoAPIVersion == new Version(1, 1)) {
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
				{ "treasureBag", treasureBag },
				{ "loot", new List<DropRateInfo>(loot) },
				{ "collection", new List<int>(collection) }
			};

			return dict;
		}

		string GetTextFromPossibleTranslationKey(string input) => Language.GetTextValue(input.Substring(input.StartsWith("$") == true ? 1 : 0));

		internal string DisplayName => GetTextFromPossibleTranslationKey(this.name);

		internal string DisplaySpawnInfo => GetTextFromPossibleTranslationKey(this.spawnInfo);
		
		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : SourceDisplayNameWithoutChatTags(ModLoader.GetMod(modSource).DisplayName);

		internal bool ForceDowned => WorldAssist.ForcedMarkedEntries.Contains(this.Key);

		internal bool IsDownedOrForced => downed() || ForceDowned;

		internal int GetIndex => BossChecklist.bossTracker.SortedEntries.IndexOf(this);

		internal int GetRecordIndex => BossChecklist.bossTracker.BossRecordKeys.IndexOf(this.Key);

		internal static string SourceDisplayNameWithoutChatTags(string modSource) {
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

		/// <summary>
		/// Determines whether or not the entry should be visible on the Table of Contents, 
		/// based on configurations and filter status.
		/// </summary>
		/// <returns>If the entry should be visible</returns>
		internal bool VisibleOnChecklist() {
			bool HideUnsupported = modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported; // entries not using the new mod calls for the Boss Log
			bool HideUnavailable = !available() && BossChecklist.BossLogConfig.HideUnavailable && !BossUISystem.Instance.BossLog.showHidden && !IsDownedOrForced; // entries that are labeled as not available
			bool HideHidden = hidden && !BossUISystem.Instance.BossLog.showHidden; // entries that are labeled as hidden
			bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && type != EntryType.Boss; // if the user has the config to only show bosses and the entry is not a boss
			if (HideUnavailable || HideHidden || SkipNonBosses || HideUnsupported) {
				return false;
			}

			// Make sure the filters allow the entry to be visible
			string bFilter = BossChecklist.BossLogConfig.FilterBosses;
			string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
			string eFilter = BossChecklist.BossLogConfig.FilterEvents;

			bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && IsDownedOrForced;
			bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && IsDownedOrForced));
			bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && IsDownedOrForced));
			if (FilterBoss || FilterMiniBoss || FilterEvent) {
				return false;
			}

			return true; // if it passes all the checks, it should be shown
		}

		internal EntryInfo(EntryType type, string modSource, string name, List<int> npcIDs, float progression, Func<bool> downed, Func<bool> available, List<int> collection, List<int> spawnItem, string info, Func<NPC, string> despawnMessages = null, Action<SpriteBatch, Rectangle, Color> customDrawing = null, List<string> overrideHeadTextures = null) {
			this.type = type;
			this.modSource = modSource;
			this.internalName = name.StartsWith("$") ? name.Substring(name.LastIndexOf('.') + 1) : name;
			this.name = name;
			this.npcIDs = npcIDs;
			this.progression = progression;
			this.downed = downed;
			this.available = available ?? (() => true);
			this.hidden = false;

			// Despawn messages for events are currently unsupported
			if (type != EntryType.Event) {
				this.customDespawnMessages = despawnMessages;
			}
			else {
				this.customDespawnMessages = null;
			}

			relatedEntries = new List<string>();

			this.spawnItem = spawnItem ?? new List<int>();
			this.spawnInfo = info ?? "";
			if (this.spawnInfo == "") {
				this.spawnInfo = $"{BossLogUI.LangLog}.SpawnInfo.NoInfo";
			}

			this.loot = new List<DropRateInfo>();
			this.lootItemTypes = new List<int>();
			this.collection = collection ?? new List<int>();
			this.collectType = new Dictionary<int, CollectionType>(); // This will be set up after all orphan data is submitted in Mod.AddRecipes

			this.portraitTexture = null;
			this.customDrawing = customDrawing;
			this.headIconTextures = new List<Asset<Texture2D>>();
			if (overrideHeadTextures == null) {
				foreach (int npc in npcIDs) {
					// No need to check for events, as events must have a custom icon to begin with.
					if (type == EntryType.Boss || type == EntryType.MiniBoss) {
						if (NPCID.Sets.BossHeadTextures[npc] != -1) {
							headIconTextures.Add(TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[npc]]);
						}
					}
				}
			}
			else {
				foreach (string texturePath in overrideHeadTextures) {
					headIconTextures.Add(ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad));
				}
			}
			if (headIconTextures.Count == 0) {
				headIconTextures.Add(TextureAssets.NpcHead[0]);
			}

			// Add the mod source to the opted mods list of the credits page if its not already and add the entry type
			if (modSource != "Terraria" && modSource != "Unknown") {
				BossUISystem.Instance.RegisteredMods.TryAdd(modSource, new int[3]);
				BossUISystem.Instance.RegisteredMods[modSource][(int)type]++;
			}
		}

		// Workaround for vanilla events with illogical translation keys.
		internal EntryInfo WithCustomTranslationKey(string translationKey) {
			// EntryInfo.name should remain as a translation key.
			this.name = translationKey;
			// Replace internal name (which would originally be illogicgal) with the printed name
			this.internalName = Language.GetTextValue(translationKey.Substring(1)).Replace(" ", "").Replace("'", "");
			return this;
		}

		internal EntryInfo WithCustomAvailability(Func<bool> funcBool) {
			this.available = funcBool;
			return this;
		}

		internal EntryInfo WithCustomPortrait(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.portraitTexture = ModContent.Request<Texture2D>(texturePath);
			}
			return this;
		}

		internal EntryInfo WithCustomHeadIcon(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.headIconTextures = new List<Asset<Texture2D>>() { ModContent.Request<Texture2D>(texturePath) };
			}
			else {
				this.headIconTextures = new List<Asset<Texture2D>>() { TextureAssets.NpcHead[0] };
			}
			return this;
		}

		internal EntryInfo WithCustomHeadIcon(List<string> texturePaths) {
			this.headIconTextures = new List<Asset<Texture2D>>();
			foreach (string path in texturePaths) {
				if (ModContent.HasAsset(path)) {
					this.headIconTextures.Add(ModContent.Request<Texture2D>(path));
				}
			}
			if (headIconTextures.Count == 0) {
				this.headIconTextures = new List<Asset<Texture2D>>() { TextureAssets.NpcHead[0] };
			}
			return this;
		}

		internal static EntryInfo MakeVanillaBoss(EntryType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.Substring(name.LastIndexOf(".") + 1);
			string tremor = name == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";

			List<int> DayDespawners = new List<int>() {
				NPCID.EyeofCthulhu,
				NPCID.Retinazer,
				NPCID.Spazmatism,
				NPCID.TheDestroyer,
			};

			Func<bool> isDay = () => Main.dayTime;
			Func<bool> AllPlayersAreDead = () => Main.player.All(plr => !plr.active || plr.dead);

			string bossCustomKillMessage = $"{NPCAssist.LangChat}.Loss.{nameKey}";
			if (!Language.Exists(bossCustomKillMessage)) {
				bossCustomKillMessage = $"{NPCAssist.LangChat}.Loss.Generic"; // If the provided key wasn't found, default to the generic key
			}

			Func<NPC, string> customMessages = npc => AllPlayersAreDead() ? bossCustomKillMessage : DayDespawners.Contains(npc.type) && isDay() ? $"{NPCAssist.LangChat}.Despawn.Day" : $"{NPCAssist.LangChat}.Despawn.Generic";

			return new EntryInfo(
				type,
				"Terraria",
				name,
				ids,
				progression,
				downed,
				() => true,
				BossChecklist.bossTracker.BossCollections.GetValueOrDefault($"Terraria {nameKey}"),
				spawnItem,
				$"Mods.BossChecklist.BossSpawnInfo.{nameKey}{tremor}",
				customMessages
			);
		}

		internal static EntryInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.StartsWith("$") ? name.Substring(name.LastIndexOf(".") + 1) : name.Replace(" ", "").Replace("'", "");
			return new EntryInfo(
				EntryType.Event,
				"Terraria",
				name,
				BossChecklist.bossTracker.EventNPCs.GetValueOrDefault($"Terraria {nameKey}"),
				progression,
				downed,
				() => true,
				BossChecklist.bossTracker.BossCollections.GetValueOrDefault($"Terraria {nameKey}"),
				spawnItem,
				$"Mods.BossChecklist.BossSpawnInfo.{nameKey}"
			);
		}

		public override string ToString() => $"{progression} {name} {modSource}";
	}

	internal class OrphanInfo
	{
		internal OrphanType type;
		internal string Key;
		internal string modSource;
		internal string bossName;

		internal List<int> values;
		// Use cases for values...
		/// Adding Spawn Item IDs to a boss
		/// Adding Collectible item IDs to a boss
		/// Adding NPC IDs to an event

		internal OrphanInfo(OrphanType type, string bossKey, List<int> values) {
			this.type = type;
			this.Key = bossKey;
			this.values = values;

			List<EntryInfo> bosses = BossChecklist.bossTracker.SortedEntries;
			int index = bosses.FindIndex(x => x.Key == this.Key);
			if (index != -1) {
				modSource = bosses[index].SourceDisplayName;
				bossName = bosses[index].DisplayName;
			}
			else {
				modSource = "Unknown";
				bossName = "Unknown";
			}
		}
	}
}
