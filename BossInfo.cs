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
	internal class BossInfo // Inheritance for Event instead?
	{
		// This localization-ignoring string is used for cross mod queries and networking. Each key is completely unique.
		internal string Key => modSource + " " + internalName;

		internal EntryType type;
		internal string modSource;
		internal string internalName; // This should be unique per mod.
		internal string name; // This should not be used for displaying purposes. Use 'BossInfo.GetDisplayName' instead.
		internal List<int> npcIDs;
		internal float progression;
		internal Func<bool> downed;
		internal Func<bool> available;
		internal bool hidden;
		internal Func<NPC, string> customDespawnMessages;

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
				{ "treasureBag", treasureBag },
				{ "loot", new List<DropRateInfo>(loot) },
				{ "collection", new List<int>(collection) }
			};

			return dict;
		}

		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : SourceDisplayNameWithoutChatTags(ModLoader.GetMod(modSource).DisplayName);

		internal bool ForceDownedByPlayer(Player player) => player.GetModPlayer<PlayerAssist>().ForceDownsForWorld.Contains(Key);

		internal bool IsDownedOrForced => downed() || ForceDownedByPlayer(Main.LocalPlayer);

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

		internal BossInfo(EntryType type, string modSource, string name, List<int> npcIDs, float progression, Func<bool> downed, Func<bool> available, List<int> collection, List<int> spawnItem, string info, Func<NPC, string> despawnMessages, Action<SpriteBatch, Rectangle, Color> customDrawing) {
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

			this.spawnItem = spawnItem ?? new List<int>();
			this.spawnInfo = info ?? "";
			if (this.spawnInfo == "") {
				this.spawnInfo = "Mods.BossChecklist.BossLog.DrawnText.NoInfo";
			}

			this.loot = new List<DropRateInfo>();
			this.lootItemTypes = new List<int>();
			this.collection = collection ?? new List<int>();
			this.collectType = new Dictionary<int, CollectionType>(); // This will be set up after all orphan data is submitted in Mod.AddRecipes

			// Loot is easily found through the item drop database.
			foreach (int npc in npcIDs) {
				List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npc, false);
				List<DropRateInfo> itemDropInfo = new List<DropRateInfo>();
				DropRateInfoChainFeed ratesInfo = new DropRateInfoChainFeed(1f);
				foreach (IItemDropRule item in dropRules) {
					item.ReportDroprates(itemDropInfo, ratesInfo);
				}
				this.loot.AddRange(itemDropInfo);

				List<int> itemIds = new List<int>();
				foreach (DropRateInfo dropRate in itemDropInfo) {
					itemIds.Add(dropRate.itemId);
				}
				this.lootItemTypes.AddRange(itemIds);
			}

			// Assign this boss's treasure bag, looking through the loot list provided
			if (!BossTracker.vanillaBossBags.TryGetValue(npcIDs[0], out this.treasureBag)) {
				foreach (int itemType in this.lootItemTypes) {
					if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item item)) {
						if (item.ModItem != null && item.ModItem.BossBagNPC > 0) {
							this.treasureBag = itemType;
							break;
						}
					}
				}
			}

			this.portraitTexture = null;
			this.customDrawing = customDrawing;
			this.headIconTextures = new List<Asset<Texture2D>>();
			foreach (int npc in npcIDs) {
				if (type == EntryType.Boss || type == EntryType.MiniBoss) {
					if (NPCID.Sets.BossHeadTextures[npc] != -1) {
						headIconTextures.Add(TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[npc]]);
					}
				}
				// No need to check for events, as events must have a custom icon to begin with.
				// Also, any minibosses apart of the event should not count in this list.
			}
			if (headIconTextures.Count == 0) {
				headIconTextures.Add(TextureAssets.NpcHead[0]);
			}

			// Add the mod source to the opted mods list of the credits page if its not already.
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

		internal BossInfo WithCustomPortrait(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.portraitTexture = ModContent.Request<Texture2D>(texturePath);
			}
			return this;
		}

		internal BossInfo WithCustomHeadIcon(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.headIconTextures = new List<Asset<Texture2D>>() { ModContent.Request<Texture2D>(texturePath) };
			}
			else {
				this.headIconTextures = new List<Asset<Texture2D>>() { TextureAssets.NpcHead[0] };
			}
			return this;
		}

		internal BossInfo WithCustomHeadIcon(List<string> texturePaths) {
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

		internal static BossInfo MakeVanillaBoss(EntryType type, float progression, string name, List<int> ids, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.Substring(name.LastIndexOf("."));
			string tremor = name == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";

			List<int> DayDespawners = new List<int>() {
				NPCID.EyeofCthulhu,
				NPCID.Retinazer,
				NPCID.Spazmatism,
				NPCID.TheDestroyer,
			};

			Func<bool> isDay = () => Main.dayTime;
			Func<bool> AllPlayersAreDead = () => Main.player.All(plr => !plr.active || plr.dead);

			string bossCustomKillMessage = $"Mods.BossChecklist.BossVictory{nameKey}";
			if (Language.GetTextValue(bossCustomKillMessage) == bossCustomKillMessage) {
				// If the provided key wasn't found, default to the generic key
				bossCustomKillMessage = $"Mods.BossChecklist.BossVictory.Generic";
			}

			Func<NPC, string> customMessages = npc => AllPlayersAreDead() ? bossCustomKillMessage : DayDespawners.Contains(npc.type) && isDay() ? "Mods.BossChecklist.BossDespawn.Day" : "Mods.BossChecklist.BossDespawn.Generic";

			return new BossInfo(
				type,
				"Terraria",
				name,
				ids,
				progression,
				downed,
				() => true,
				BossChecklist.bossTracker.SetupCollect(ids[0]),
				spawnItem,
				$"$Mods.BossChecklist.BossSpawnInfo{nameKey}{tremor}",
				customMessages,
				null
			);
		}

		internal static BossInfo MakeVanillaEvent(float progression, string name, Func<bool> downed, List<int> spawnItem) {
			string nameKey = name.Replace(" ", "").Replace("'", "");
			return new BossInfo(
				EntryType.Event,
				"Terraria",
				name,
				BossChecklist.bossTracker.SetupEventNPCList(name),
				progression,
				downed,
				() => true,
				BossChecklist.bossTracker.SetupEventCollectibles(name),
				spawnItem,
				$"$Mods.BossChecklist.BossSpawnInfo.{nameKey}",
				null,
				null
			);
		}

		public override string ToString() => $"{progression} {name} {modSource}";

		public string GetDisplayName() => GetTextFromPossibleTranslationKey(this.name);

		public string GetDisplaySpawnInfo() => GetTextFromPossibleTranslationKey(this.spawnInfo);

		string GetTextFromPossibleTranslationKey(string input) => input?.StartsWith("$") == true ? Language.GetTextValue(input.Substring(1)) : input;
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
		/// Adding Loot or Collectible item IDs to a boss
		/// Adding NPC IDs to an event

		internal OrphanInfo(OrphanType type, string bossKey, List<int> values) {
			this.type = type;
			this.Key = bossKey;
			modSource = bossKey.Substring(0, bossKey.IndexOf(" "));
			bossName = bossKey.Substring(bossKey.IndexOf(" ") + 1);
			this.values = values;
		}
	}
}
