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
using System.Text.RegularExpressions;

namespace BossChecklist
{
	internal enum EntryType {
		Boss,
		MiniBoss,
		Event
	}

	internal class EntryInfo // Inheritance for Event instead?
	{
		// This localization-ignoring string is used for cross mod queries and networking. Each key is completely unique.
		internal string Key { get; init; }

		internal EntryType type;
		internal string modSource;
		internal LocalizedText name; // This should not be used for displaying purposes. Use 'EntryInfo.GetDisplayName' instead.
		internal List<int> npcIDs;
		internal float progression;
		internal Func<bool> downed;
		internal Func<bool> available;
		internal bool hidden;
		internal Func<NPC, LocalizedText> customDespawnMessages;

		internal List<string> relatedEntries;

		internal List<int> spawnItem;
		internal Func<LocalizedText> spawnInfo;

		internal int treasureBag = 0;
		internal List<int> collectibles;
		internal Dictionary<int, CollectibleType> collectibleType;
		internal List<DropRateInfo> loot;
		internal List<int> lootItemTypes;

		internal Asset<Texture2D> portraitTexture; // used for vanilla entry portrait drawing
		internal Action<SpriteBatch, Rectangle, Color> customDrawing; // used for modded entry portrait drawing
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
			expando.collectibles = new List<int>(collectibles);

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

				{ "progression", progression },
				{ "downed", new Func<bool>(downed) },

				{ "isBoss", type.Equals(EntryType.Boss) },
				{ "isMiniboss", type.Equals(EntryType.MiniBoss) },
				{ "isEvent", type.Equals(EntryType.Event) },

				{ "npcIDs", new List<int>(npcIDs) },
				{ "spawnItems", new List<int>(spawnItem) },
				{ "treasureBag", treasureBag },
				{ "loot", new List<DropRateInfo>(loot) },
				{ "collectibles", new List<int>(collectibles) }
			};

			return dict;
		}

		internal string DisplayName => name.Value;

		internal string DisplaySpawnInfo => spawnInfo().Value;
		
		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : SourceDisplayNameWithoutChatTags(ModLoader.GetMod(modSource).DisplayName);

		internal bool MarkedAsDowned => WorldAssist.MarkedEntries.Contains(this.Key);

		internal bool IsDownedOrMarked => downed() || MarkedAsDowned;

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
		/// Determines what despawn message should be used based on client configuration and submitted entry data.
		/// </summary>
		/// <returns>A LocalizedText of the despawn message of the passed npc. Returns null if no message can be found.</returns>
		internal LocalizedText GetDespawnMessage(NPC npc) {
			if (npc.life <= 0)
				return null; // If the boss was killed, don't display a despawn message

			// When unique despawn messages are enabled, pass the NPC for the custom message function provided by the entry
			if (BossChecklist.ClientConfig.DespawnMessageType == "Unique" && customDespawnMessages(npc) is LocalizedText message)
				return message; // this will only return a unique message if the custom message function properly assigns one

			// If the Unique message was empty/null or the player is using Generic despawn messages, try to find an appropriate despawn message to send
			// Return a generic despawn message if any player is left alive or return a boss victory despawn message if all player's were killed
			if (BossChecklist.ClientConfig.DespawnMessageType != "Disabled")
				return Language.GetText(Main.player.Any(plr => plr.active && !plr.dead) ? $"{NPCAssist.LangChat}.Despawn.Generic" : $"{NPCAssist.LangChat}.Loss.Generic");

			return null; // The despawn message feature was disabled. Return an empty message.
		}

		/// <summary>
		/// Determines whether or not the entry should be visible on the Table of Contents, 
		/// based on configurations and filter status.
		/// </summary>
		/// <returns>If the entry should be visible</returns>
		internal bool VisibleOnChecklist() {
			bool HideUnsupported = modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported; // entries not using the new mod calls for the Boss Log
			bool HideUnavailable = !available() && BossChecklist.BossLogConfig.HideUnavailable && !BossUISystem.Instance.BossLog.showHidden && !IsDownedOrMarked; // entries that are labeled as not available
			bool HideHidden = hidden && !BossUISystem.Instance.BossLog.showHidden; // entries that are labeled as hidden
			bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && type != EntryType.Boss; // if the user has the config to only show bosses and the entry is not a boss
			if (HideUnavailable || HideHidden || SkipNonBosses || HideUnsupported) {
				return false;
			}

			// Make sure the filters allow the entry to be visible
			string bFilter = BossChecklist.BossLogConfig.FilterBosses;
			string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
			string eFilter = BossChecklist.BossLogConfig.FilterEvents;

			bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && IsDownedOrMarked;
			bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && IsDownedOrMarked));
			bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && IsDownedOrMarked));
			if (FilterBoss || FilterMiniBoss || FilterEvent) {
				return false;
			}

			return true; // if it passes all the checks, it should be shown
		}

		internal EntryInfo(EntryType entryType, string modSource, string internalName, float progression, Func<bool> downed, List<int> npcIDs, Dictionary<string, object> extraData = null) {
			// Add the mod source to the opted mods list of the credits page if its not already and add the entry type
			if (modSource != "Terraria" && modSource != "Unknown") {
				BossUISystem.Instance.RegisteredMods.TryAdd(modSource, new int[3]);
				BossUISystem.Instance.RegisteredMods[modSource][(int)entryType]++;
			}

			// required entry data
			this.Key = modSource + " " + internalName;
			this.type = entryType;
			this.modSource = modSource;
			this.progression = progression;
			this.downed = downed;
			this.npcIDs = npcIDs ?? new List<int>();

			// Localization checks
			LocalizedText name = extraData?.ContainsKey("displayName") == true ? extraData["displayName"] as LocalizedText : null;
			Func<LocalizedText> spawnInfo = null;
			if (extraData?.ContainsKey("spawnInfo") == true) {
				if (extraData["spawnInfo"] is Func<LocalizedText>) {
					spawnInfo = extraData["spawnInfo"] as Func<LocalizedText>;
				}
				else if (extraData["spawnInfo"] is LocalizedText) {
					spawnInfo = () => extraData["spawnInfo"] as LocalizedText;
				}
			}
			
			if (name == null || spawnInfo == null) {
				// Modded. Ensure that all nulls passed in autoregister a localization key.
				if (type == EntryType.Event) {
					name ??= Language.GetOrRegister($"Mods.{modSource}.BossChecklistIntegration.{internalName}.EntryName", () => Regex.Replace(internalName, "([A-Z])", " $1").Trim()); // Add spaces before each capital letter.
					spawnInfo ??= () => Language.GetOrRegister($"Mods.{modSource}.BossChecklistIntegration.{internalName}.SpawnInfo", () => "Spawn conditions unknown");
				}
				else {
					int primaryNPCID = npcIDs?.Count > 0 ? npcIDs[0] : 0;
					if (ModContent.GetModNPC(primaryNPCID) is ModNPC modNPC) {
						string prefix = modNPC.GetLocalizationKey("BossChecklistIntegration");
						// For single NPC bosses, assume EntryName is DisplayName rather than registering a localization key.
						if (/*internalName == modNPC.Name &&*/ npcIDs.Count == 1 && !Language.Exists($"{prefix}.EntryName"))
							name ??= modNPC.DisplayName;
						name ??= Language.GetOrRegister($"{prefix}.EntryName", () => Regex.Replace(internalName, "([A-Z])", " $1").Trim());
						spawnInfo ??= () => Language.GetOrRegister($"{prefix}.SpawnInfo", () => "Spawn conditions unknown"); // Register English/default, not localized.
					}
					else {
						// Mod registered boss for vanilla npc or no npcids?
						name ??= Language.GetText("Mods.BossChecklist.BossSpawnInfo.Unknown");
						spawnInfo ??= () => Language.GetText("Mods.BossChecklist.BossSpawnInfo.Unknown");
					}
				}
			}

			this.name = name;
			this.spawnInfo = spawnInfo;

			// self-initializing data
			this.hidden = false; // defaults to false, hidden status can be toggled per world
			this.relatedEntries = new List<string>(); /// Setup in <see cref="BossTracker.SetupEntryRelations"/>
			this.loot = new List<DropRateInfo>(); /// Setup in <see cref="BossTracker.FinalizeEntryLootTables"/>
			this.lootItemTypes = new List<int>(); /// Setup in <see cref="BossTracker.FinalizeEntryLootTables"/>
			this.collectibleType = new Dictionary<int, CollectibleType>(); /// Setup in <see cref="BossTracker.FinalizeCollectibleTypes"/>

			// optional extra data
			List<int> InterpretObjectAsListOfInt(object data) => data is List<int> ? data as List<int> : (data is int ? new List<int>() { Convert.ToInt32(data) } : new List<int>());
			List<string> InterpretObjectAsListOfStrings(object data) => data is List<string> ? data as List<string> : (data is string ? new List<string>() { data as string } : null);

			this.available = extraData?.ContainsKey("availability") == true ? extraData["availability"] as Func<bool> : () => true;
			this.spawnItem = extraData?.ContainsKey("spawnItems") == true ? InterpretObjectAsListOfInt(extraData["spawnItems"]) : new List<int>();
			this.collectibles = extraData?.ContainsKey("collectibles") == true ? InterpretObjectAsListOfInt(extraData["collectibles"]) : new List<int>();
			this.customDrawing = extraData?.ContainsKey("customPortrait") == true ? extraData["customPortrait"] as Action<SpriteBatch, Rectangle, Color> : null;
			if (extraData?.ContainsKey("despawnMessage") == true) {
				if (extraData["despawnMessage"] is Func<NPC, LocalizedText> multiMessage) {
					this.customDespawnMessages = multiMessage;
				}
				else if (extraData["despawnMessage"] is LocalizedText singleMessage) {
					this.customDespawnMessages = (NPC npc) => singleMessage;
				}
				else {
					this.customDespawnMessages = null;
				}
			}

			headIconTextures = new List<Asset<Texture2D>>();
			if (extraData?.ContainsKey("overrideHeadTextures") == true) {
				foreach (string texturePath in InterpretObjectAsListOfStrings(extraData["overrideHeadTextures"])) {
					headIconTextures.Add(ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad));
				}
			}
			else {
				foreach (int npc in npcIDs) {
					if (entryType != EntryType.Event && NPCID.Sets.BossHeadTextures[npc] != -1)
						headIconTextures.Add(TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[npc]]); // Skip events. Events must use a custom icon to display.
				}
			}

			if (headIconTextures.Count == 0)
				headIconTextures.Add(TextureAssets.NpcHead[0]); // If the head textures is empty, fill it with the '?' head icon so modder's see something is wrong
		}

		// Workaround for vanilla events with illogical translation keys.
		internal EntryInfo WithCustomTranslationKey(string translationKey) {
			// EntryInfo.name should remain as a translation key.
			this.name = Language.GetText(translationKey);
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

		internal static EntryInfo MakeVanillaBoss(EntryType type, float val, string key, int npcID, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1);

			Func<NPC, LocalizedText> customMessages = null;
			if (type == EntryType.Boss) { // BossChecklist only has despawn messages for vanilla Bosses
				List<int> DayDespawners = new List<int>() {
					NPCID.EyeofCthulhu,
					NPCID.Retinazer,
					NPCID.Spazmatism,
					NPCID.TheDestroyer,
				};

				customMessages = delegate (NPC npc) {
					if (Main.player.All(plr => !plr.active || plr.dead)) {
						return Language.GetText($"{NPCAssist.LangChat}.Loss.{nameKey}"); // Despawn message when all players are dead
					}
					else if (Main.dayTime && DayDespawners.Contains(npc.type)) {
						return Language.GetText($"{NPCAssist.LangChat}.Despawn.Day"); // Despawn message when it turns to day
					}

					// unique despawn messages should default to the generic message when no conditions are met
					return Language.GetText($"{NPCAssist.LangChat}.Despawn.Generic");
				};
			}

			return new EntryInfo(
				entryType: type,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				downed: downed,
				npcIDs: new List<int>() { npcID },
				extraData: new Dictionary<string, object>() {
					{ "displayName", Language.GetText(key) },
					{ "spawnInfo", Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}") },
					{ "spawnItems", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollectibles.GetValueOrDefault($"Terraria {nameKey}") },
					{ "despawnMessage", customMessages },
				}
			);
		}

		internal static EntryInfo MakeVanillaBoss(EntryType type, float val, string key, List<int> ids, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1).Replace(" ", "").Replace("'", "");

			Func<NPC, LocalizedText> customMessages = null;
			if (type == EntryType.Boss) { // BossChecklist only has despawn messages for vanilla Bosses
				List<int> DayDespawners = new List<int>() {
					NPCID.EyeofCthulhu,
					NPCID.Retinazer,
					NPCID.Spazmatism,
					NPCID.TheDestroyer,
				};

				customMessages = delegate (NPC npc) {
					if (Main.player.All(plr => !plr.active || plr.dead)) {
						return Language.GetText($"{NPCAssist.LangChat}.Loss.{nameKey}"); // Despawn message when all players are dead
					}
					else if (Main.dayTime && DayDespawners.Contains(npc.type)) {
						return Language.GetText($"{NPCAssist.LangChat}.Despawn.Day"); // Despawn message when it turns to day
					}

					// unique despawn messages should default to the generic message when no conditions are met
					return Language.GetText($"{NPCAssist.LangChat}.Despawn.Generic");
				};
			}

			return new EntryInfo(
				entryType: type,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				downed: downed,
				npcIDs: ids,
				extraData: new Dictionary<string, object>() {
					{ "displayName", Language.GetText(key) },
					{ "spawnInfo", Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}") },
					{ "spawnItems", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollectibles.GetValueOrDefault($"Terraria {nameKey}") },
					{ "despawnMessage", customMessages },
				}
			);
		}

		internal static EntryInfo MakeVanillaEvent(float val, string key, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1).Replace(" ", "").Replace("'", "");
			return new EntryInfo(
				entryType: EntryType.Event,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				downed: downed,
				npcIDs: BossChecklist.bossTracker.EventNPCs.GetValueOrDefault($"Terraria {nameKey}"),
				extraData: new Dictionary<string, object>() {
					{ "displayName", Language.GetText(key) },
					{ "spawnInfo", Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}") },
					{ "spawnItems", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollectibles.GetValueOrDefault($"Terraria {nameKey}") },
				}
			);
		}

		public override string ToString() => $"{progression} {Key}";
	}

	internal enum OrphanType {
		Loot,
		Collectibles,
		SpawnItems,
		EventNPC
	}

	internal class OrphanInfo
	{
		internal OrphanType type;
		internal string modSource;
		internal Dictionary<string, object> values;

		internal OrphanInfo(OrphanType type, string modSource, Dictionary<string, object> values) {
			this.type = type;
			this.modSource = modSource;

			// Sort through the data submissions to remove any invalid data
			foreach (string Key in values.Keys) {
				if (!Key.Contains(' ')) {
					values.Remove(Key); // remove submissions with invalid keys (no space between modSource and internalName)
					BossChecklist.instance.Logger.Warn($"A {type} call from {modSource} contains an invalid key ({Key}) and has been removed.");
				}
				else if (!Key.StartsWith("Terraria ") && !ModLoader.TryGetMod(Key.Substring(0, Key.IndexOf(" ")), out Mod mod)) {
					values.Remove(Key); // remove submissions that use an entry key from an unloaded mod (no need to log removed entries for unloaded mods)
				}
			}
			this.values = values;
		}
	}
}
