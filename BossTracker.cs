using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BossChecklist
{
	internal class BossTracker {
		// Bosses
		public const float KingSlime = 1f;
		public const float EyeOfCthulhu = 2f;
		public const float EaterOfWorlds = 3f;
		public const float QueenBee = 4f;
		public const float Skeletron = 5f;
		public const float DeerClops = 6f;
		public const float WallOfFlesh = 7f;
		public const float QueenSlime = 8f;
		public const float TheTwins = 9f;
		public const float TheDestroyer = 10f;
		public const float SkeletronPrime = 11f;
		public const float Plantera = 12f;
		public const float Golem = 13f;
		public const float DukeFishron = 14f;
		public const float EmpressOfLight = 15f;
		public const float Betsy = 16f;
		public const float LunaticCultist = 17f;
		public const float Moonlord = 18f;

		// Mini-bosses and Events
		public const float TorchGod = 1.5f;
		public const float BloodMoon = 2.5f;
		public const float GoblinArmy = 3.33f;
		public const float OldOnesArmy = 3.66f;
		public const float DarkMage = OldOnesArmy + 0.01f;
		public const float Ogre = SkeletronPrime + 0.01f; // Unlocked once a mechanical boss has been defeated
		public const float FrostLegion = 7.33f;
		public const float PirateInvasion = 7.66f;
		public const float PirateShip = PirateInvasion + 0.01f;
		public const float SolarEclipse = 11.5f;
		public const float PumpkinMoon = 13.25f;
		public const float MourningWood = PumpkinMoon + 0.01f;
		public const float Pumpking = PumpkinMoon + 0.02f;
		public const float FrostMoon = 13.5f;
		public const float Everscream = FrostMoon + 0.01f;
		public const float SantaNK1 = FrostMoon + 0.02f;
		public const float IceQueen = FrostMoon + 0.03f;
		public const float MartianMadness = 13.75f;
		public const float MartianSaucer = MartianMadness + 0.01f;
		public const float LunarEvent = LunaticCultist + 0.01f; // Happens immediately after the defeation of the Lunatic Cultist

		/// <summary>
		/// All currently loaded bosses/minibosses/events sorted by progression.
		/// When updating vanilla progression values, please also update the version number of <see cref="BossChecklist.LastVanillaProgressionRevision"/>.
		/// </summary>
		internal List<EntryInfo> SortedEntries;
		internal EntryInfo FindEntryFromKey(string lookupKey) => SortedEntries.Find(entry => entry.Key == lookupKey);

		/// <summary>
		/// Loops through all entries in BossTracker.SortedEntries to find EntryInfo that contains the specified npc type.
		/// Only returns with an entry if the entry has a record index (is a boss).
		/// </summary>
		/// <returns>Returns null if no valid entry can be found.</returns>
		public EntryInfo FindBossEntryByNPC(int npcType, out int recordIndex) {
			recordIndex = -1;
			if (!EntryCache[npcType])
				return null; // the entry hasn't been registered

			foreach (EntryInfo entry in SortedEntries) {
				if (entry.IsRecordIndexed(out recordIndex) && entry.npcIDs.Contains(npcType))
					return entry; // if the npc pool contains the npc type, return the current the index
			}

			return null; // no valid entry found (may be an entry, but is not record indexed.
		}

		public bool IsEntryLimb(int npcType, out EntryInfo limbEntry) {
			limbEntry = null;
			foreach (EntryInfo entry in SortedEntries) {
				if (entry.npcLimbs.ContainsKey(npcType)) {
					limbEntry = entry;
					return true;
				}
			}
			return false;
		}

		internal Dictionary<string, int[]> RegisteredMods; // Key: mod internal name, Value: Entries registered by type]
		internal bool[] EntryCache;
		internal bool[] EntryLootCache;
		internal List<OrphanInfo> ExtraData;
		internal bool EntriesFinalized = false;
		internal bool AnyModHasOldCall = false;
		internal Dictionary<string, List<string>> OldCalls = new();
		internal List<string> BossRecordKeys;

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaEntries();
			ExtraData = new List<OrphanInfo>();
			BossRecordKeys = new List<string>();
			RegisteredMods = new Dictionary<string, int[]>();
		}

		private void InitializeVanillaEntries() {
			SortedEntries = new List<EntryInfo> {
				// Bosses -- Vanilla
				EntryInfo.MakeVanillaBoss(EntryType.Boss, KingSlime, "NPCName.KingSlime", NPCID.KingSlime, () => NPC.downedSlimeKing)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.KingSlime}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, EyeOfCthulhu, "NPCName.EyeofCthulhu", NPCID.EyeofCthulhu, () => NPC.downedBoss1),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "NPCName.EaterofWorldsHead", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, () => NPC.downedBoss2)
					.WithCustomAvailability(() => !WorldGen.crimson || Main.drunkWorld || ModLoader.TryGetMod("BothEvils", out Mod mod))
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.EaterofWorldsHead}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "NPCName.BrainofCthulhu", NPCID.BrainofCthulhu, () => NPC.downedBoss2)
					.WithCustomAvailability(() => WorldGen.crimson || Main.drunkWorld || ModLoader.TryGetMod("BothEvils", out Mod mod)),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, QueenBee, "NPCName.QueenBee", NPCID.QueenBee, () => NPC.downedQueenBee),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, Skeletron, "NPCName.SkeletronHead", new List<int>() { NPCID.SkeletronHead }, () => NPC.downedBoss3)
					.WithCustomLimbs(new List<int>() { NPCID.SkeletronHand })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.SkeletronHead}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, DeerClops, "NPCName.Deerclops", NPCID.Deerclops, () => NPC.downedDeerclops)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Deerclops}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, WallOfFlesh, "NPCName.WallofFlesh", new List<int>() { NPCID.WallofFlesh }, () => Main.hardMode)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.WallofFlesh}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, QueenSlime, "NPCName.QueenSlimeBoss", NPCID.QueenSlimeBoss, () => NPC.downedQueenSlime)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.QueenSlimeBoss}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, TheTwins, "Enemies.TheTwins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, () => NPC.downedMechBoss2)
					.WithCustomLimbs(new List<int>() { NPCID.Retinazer, NPCID.Spazmatism })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Retinazer}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, TheDestroyer, "NPCName.TheDestroyer", NPCID.TheDestroyer, () => NPC.downedMechBoss1)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.TheDestroyer}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, SkeletronPrime, "NPCName.SkeletronPrime", NPCID.SkeletronPrime, () => NPC.downedMechBoss3)
					.WithCustomLimbs(new List<int>() { NPCID.PrimeCannon, NPCID.PrimeSaw, NPCID.PrimeVice, NPCID.PrimeLaser })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.SkeletronPrime}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, Plantera, "NPCName.Plantera", NPCID.Plantera, () => NPC.downedPlantBoss),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, Golem, "NPCName.Golem", new List<int>() { NPCID.Golem }, () => NPC.downedGolemBoss)
					.WithCustomLimbs(new List<int>() { NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHead })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Golem}")
					.WithCustomHeadIcon($"Terraria/Images/NPC_Head_Boss_5"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, Betsy, "NPCName.DD2Betsy", NPCID.DD2Betsy, () => DD2Event.DownedInvasionT3)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2Betsy}"),
					// No despawn message due to being in an event
				EntryInfo.MakeVanillaBoss(EntryType.Boss, EmpressOfLight, "NPCName.HallowBoss", NPCID.HallowBoss, () => NPC.downedEmpressOfLight)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.HallowBoss}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, DukeFishron, "NPCName.DukeFishron", NPCID.DukeFishron, () => NPC.downedFishron),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, LunaticCultist, "NPCName.CultistBoss", NPCID.CultistBoss, () => NPC.downedAncientCultist)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.CultistBoss}"),
				EntryInfo.MakeVanillaBoss(EntryType.Boss, Moonlord, "Enemies.MoonLord", new List<int>() { NPCID.MoonLordCore }, () => NPC.downedMoonlord)
					.WithCustomLimbs(new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordHand })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.MoonLordHead}"),

				// Minibosses and Events -- Vanilla
				EntryInfo.MakeVanillaEvent(TorchGod, "NPCName.TorchGod", () => Main.LocalPlayer.unlockedBiomeTorches)
					.WithCustomHeadIcon($"Terraria/Images/Item_{ItemID.TorchGodsFavor}"),
				EntryInfo.MakeVanillaEvent(BloodMoon, "Bestiary_Events.BloodMoon", () => WorldAssist.downedBloodMoon)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventBloodMoon")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventBloodMoon_Head"),
				// EntryInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
				EntryInfo.MakeVanillaEvent(GoblinArmy, "Goblin Army", () => NPC.downedGoblins)
					.WithCustomTranslationKey("LegacyInterface.88")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventGoblinArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_9"),
				EntryInfo.MakeVanillaEvent(OldOnesArmy, "Old One's Army", () => DD2Event.DownedInvasionAnyDifficulty)
					.WithCustomTranslationKey("DungeonDefenders2.InvasionProgressTitle")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventOldOnesArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_79"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, DarkMage, "NPCName.DD2DarkMageT3", new List<int>() { NPCID.DD2DarkMageT3, NPCID.DD2DarkMageT1 }, () => WorldAssist.downedDarkMage)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2DarkMageT3}"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, Ogre, "NPCName.DD2OgreT3", new List<int>() { NPCID.DD2OgreT3, NPCID.DD2OgreT2 }, () => WorldAssist.downedOgre)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2OgreT3}"),
				EntryInfo.MakeVanillaEvent(FrostLegion, "Frost Legion", () => NPC.downedFrost)
					.WithCustomTranslationKey("LegacyInterface.87")
					.WithCustomAvailability(() => Main.xMas)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostLegion")
					.WithCustomHeadIcon("Terraria/Images/Extra_7"),
				EntryInfo.MakeVanillaEvent(PirateInvasion, "Pirate Invasion", () => NPC.downedPirates)
					.WithCustomTranslationKey("LegacyInterface.86")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPirateInvasion")
					.WithCustomHeadIcon("Terraria/Images/Extra_11"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, PirateShip, "NPCName.PirateShip", new List<int>() { NPCID.PirateShip }, () => WorldAssist.downedFlyingDutchman)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.PirateShip}"),
				EntryInfo.MakeVanillaEvent(SolarEclipse, "Bestiary_Events.Eclipse", () => WorldAssist.downedSolarEclipse)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventSolarEclipse")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventSolarEclipse_Head"),
				EntryInfo.MakeVanillaEvent(PumpkinMoon, "Pumpkin Moon", () => WorldAssist.downedPumpkinMoon)
					.WithCustomTranslationKey("LegacyInterface.84")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPumpkinMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_12"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, MourningWood, "NPCName.MourningWood", new List<int>() { NPCID.MourningWood }, () => NPC.downedHalloweenTree),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, Pumpking, "NPCName.Pumpking", new List<int>() { NPCID.Pumpking }, () => NPC.downedHalloweenKing)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Pumpking}"),
				EntryInfo.MakeVanillaEvent(FrostMoon, "Frost Moon", () => WorldAssist.downedFrostMoon)
					.WithCustomTranslationKey("LegacyInterface.83")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_8"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, Everscream, "NPCName.Everscream", new List<int>() { NPCID.Everscream }, () => NPC.downedChristmasTree),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, SantaNK1, "NPCName.SantaNK1", new List<int>() { NPCID.SantaNK1 }, () => NPC.downedChristmasSantank),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, IceQueen, "NPCName.IceQueen", new List<int>() { NPCID.IceQueen }, () => NPC.downedChristmasIceQueen),
				EntryInfo.MakeVanillaEvent(MartianMadness, "Martian Madness", () => NPC.downedMartians)
					.WithCustomTranslationKey("LegacyInterface.85")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventMartianMadness")
					.WithCustomHeadIcon($"Terraria/Images/Extra_10"),
				EntryInfo.MakeVanillaBoss(EntryType.MiniBoss, MartianSaucer, "NPCName.MartianSaucer", new List<int>() { NPCID.MartianSaucer, NPCID.MartianSaucerCore }, () => WorldAssist.downedMartianSaucer),
				EntryInfo.MakeVanillaEvent(LunarEvent, "Lunar Event", () => NPC.downedTowers)
					.WithCustomLimbs(new List<int>() { NPCID.LunarTowerVortex, NPCID.LunarTowerStardust, NPCID.LunarTowerNebula, NPCID.LunarTowerSolar })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventLunarEvent")
					.WithCustomHeadIcon(new List<string>() {
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerNebula]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerVortex]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerSolar]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerStardust]}"}
					),
			};
		}

		/*
		internal void FinalizeLocalization() {
			// Modded Localization keys are initialized before AddRecipes, so we need to do this late.
			foreach (var boss in SortedBosses) {
				boss.name = GetTextFromPossibleTranslationKey(boss.name);
				boss.spawnInfo = GetTextFromPossibleTranslationKey(boss.spawnInfo);
			}

			// Local Functions
			string GetTextFromPossibleTranslationKey(string input) => input?.StartsWith("$") == true ? Language.GetTextValue(input.Substring(1)) : input;
		}
		*/

		internal void FinalizeEventNPCPools() {
			foreach (string key in EventKeysWhoHaveBelongToInvasionSets) {
				FindEntryFromKey(key).npcIDs = GetBelongsToInvasionSet(key).GetTrueIndexes();
			}
		}

		internal void FinalizeOrphanData() {
			foreach (OrphanInfo orphan in ExtraData) {
				int typeCount = 0;
				foreach (KeyValuePair<string, object> submission in orphan.values) {
					if (FindEntryFromKey(submission.Key) is not EntryInfo entry) {
						BossChecklist.instance.LogWarning("InvalidOrphanKey", requiresConfig: false, orphan.type, orphan.modCallerDisplayName, submission.Key);
						continue;
					}

					object data = submission.Value;
					List<int> InterpretDataAsListOfInt = data is List<int> ? data as List<int> : (data is int ? new List<int>() { Convert.ToInt32(data) } : new List<int>());
					typeCount += InterpretDataAsListOfInt.Count;

					if (orphan.type == OrphanType.SubmitEntryLoot) {
						entry.lootItemTypes.AddRange(InterpretDataAsListOfInt);
					}
					else if (orphan.type == OrphanType.SubmitEntryCollectibles) {
						InterpretDataAsListOfInt.ForEach(item => entry.collectibles.TryAdd(item, CollectibleType.Generic));
					}
					else if (orphan.type == OrphanType.SubmitEntrySpawnItems) {
						entry.spawnItem.AddRange(InterpretDataAsListOfInt);
					}
					else if (orphan.type == OrphanType.SubmitEventNPCs) {
						if (entry.type == EntryType.Event) {
							entry.npcIDs.AddRange(InterpretDataAsListOfInt);
							if (EventKeysWhoHaveBelongToInvasionSets.Contains(submission.Key) && BossChecklist.BossLogConfig.Debug.ModCallLogVerbose)
								BossChecklist.instance.LogWarning("BelongsToInvasion", requiresConfig: true, orphan.modCallerDisplayName, submission.Key);
						}
						else {
							BossChecklist.instance.LogWarning("InvalidEventEntry", requiresConfig: false, entry.Key, OrphanType.SubmitEventNPCs);
						}
					}
				}

				BossChecklist.instance.LogModCallInfo("SuccessfulOrphanData", orphan.modCallerDisplayName, typeCount, orphan.type);
			}
		}

		internal void FinalizeCollectibleTypes() {
			foreach (EntryInfo entry in SortedEntries) {
				foreach (int item in entry.collectibles.Keys.ToList()) {
					if (!ContentSamples.ItemsByType.TryGetValue(item, out Item temp))
						continue;

					if (temp.headSlot > 0 && temp.vanity) {
						entry.collectibles[item] = CollectibleType.Mask;
					}
					else if (IsRegisteredMusicBox(item)) {
						entry.collectibles[item] = CollectibleType.Music;
					}
					else if ((Main.projPet[temp.shoot] && Main.vanityPet[temp.buffType]) || (ProjectileID.Sets.LightPet[temp.shoot] && Main.lightPet[temp.buffType])) {
						entry.collectibles[item] = CollectibleType.Pet;
					}
					else if (temp.master && temp.mountType > MountID.None) {
						entry.collectibles[item] = CollectibleType.Mount;
					}
					else if (temp.createTile > TileID.Dirt && TileObjectData.GetTileData(temp.createTile, temp.placeStyle) is TileObjectData data) {
						if (data.AnchorWall == TileObjectData.Style3x3Wall.AnchorWall && data.Width == 3 && data.Height == 3) {
							entry.collectibles[item] = CollectibleType.Trophy;
						}
						else if (temp.master && data.Width == 3 && data.Height == 4) {
							entry.collectibles[item] = CollectibleType.Relic;
						}
						else {
							entry.collectibles[item] = CollectibleType.Generic;
						}
					}
					else {
						entry.collectibles[item] = CollectibleType.Generic;
					}
				}
			}
		}

		internal void FinalizeEntryData() {
			SortedEntries.Sort((x, y) => x.progression.CompareTo(y.progression));
			SetupEntryRelations(); // must be done after sorting entries

			EntryCache = new bool[NPCLoader.NPCCount];
			EntryLootCache = new bool[ItemLoader.ItemCount];
			foreach (EntryInfo entry in SortedEntries) {
				if (entry.type == EntryType.Boss) {
					BossRecordKeys.Add(entry.Key); // Add all Boss Type entries to a list of keys for Boss Records
				}
				entry.npcIDs.ForEach(x => EntryCache[x] = true); // Mark all NPCs as an entry NPC for verifying purposes
				entry.lootItemTypes.ForEach(x => EntryLootCache[x] = true); // Mark loot items to be "obtainable" for loot checklist
				entry.collectibles.Keys.ToList().ForEach(x => EntryLootCache[x] = true); // Mark collectibles items to be "obtainable" for loot checklist
			}

			// Entries are now finalized. Entries can no longer be added or edited through Mod Calls.
			EntriesFinalized = true;

			foreach (KeyValuePair<string, int[]> value in RegisteredMods) {
				List<string> bossKeys = new List<string>();
				List<string> minibossKeys = new List<string>();
				List<string> eventKeys = new List<string>();
				foreach (EntryInfo entry in BossChecklist.bossTracker.SortedEntries.Where(x => x.modSource == value.Key)) {
					if (entry.type == EntryType.Event) {
						eventKeys.Add(entry.Key.Substring(entry.modSource.Length + 1));
					}
					else if (entry.type == EntryType.MiniBoss) {
						minibossKeys.Add(entry.Key.Substring(entry.modSource.Length + 1));
					}
					else {
						bossKeys.Add(entry.Key.Substring(entry.modSource.Length + 1));
					}
				}

				string modName = ModLoader.TryGetMod(value.Key, out Mod mod) ? mod.DisplayName : value.Key;

				if (bossKeys.Count > 0)
					BossChecklist.instance.LogModCallInfo("RegisteredBosses", modName, value.Value[0], "[" + string.Join(", ", bossKeys) + "]");

				if (minibossKeys.Count > 0)
					BossChecklist.instance.LogModCallInfo("RegisteredMiniBosses", modName, value.Value[1], "[" + string.Join(", ", minibossKeys) + "]");

				if (eventKeys.Count > 0)
					BossChecklist.instance.LogModCallInfo("RegisteredEvents", modName, value.Value[2], "[" + string.Join(", ", eventKeys) + "]");
			}

			if (AnyModHasOldCall) {
				string OldToNewCall(string message) {
					return message switch {
						"AddBoss" => "LogBoss",
						"AddBossWithInfo" => "LogBoss",
						"AddMiniBoss" => "LogMiniBoss",
						"AddMiniBossWithInfo" => "LogMiniBoss",
						"AddEvent" => "LogEvent",
						"AddEventWithInfo" => "LogEvent",
						"AddToBossLoot" => "SubmitEntryLoot",
						"AddToBossCollection" => "SubmitEntryCollectibles",
						"AddToBossSpawnItems" => "SubmitEntrySpawnItems",
						"AddToEventNPCs" => "SubmitEventNPCs",
						_ => "invalid mod call detected"
					};
				}

				foreach (var oldCall in OldCalls) {
					BossChecklist.instance.LogWarning("OldCall", requiresConfig: true, oldCall.Key, OldToNewCall(oldCall.Key), oldCall.Key, string.Join(", ", oldCall.Value));
				}
				OldCalls.Clear();
				BossChecklist.instance.LogModCallInfo("ModCallDocumentation");
			}

			// The server must populate for collected records after all entries have been counted and sorted.
			if (Main.netMode == NetmodeID.Server) {
				BossChecklist.ServerCollectedRecords = new List<PersonalRecords>[Main.maxPlayers];
				for (int i = 0; i < Main.maxPlayers; i++) {
					BossChecklist.ServerCollectedRecords[i] = new List<PersonalRecords>();
					foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
						BossChecklist.ServerCollectedRecords[i].Add(new PersonalRecords(key));
					}
				}
			}
		}

		internal void FinalizeEntryLootTables() {
			foreach (EntryInfo entry in SortedEntries) {
				// Loot is easily found through the item drop database.
				foreach (int npc in entry.npcIDs) {
					List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npc, false);
					List<DropRateInfo> itemDropInfo = new List<DropRateInfo>();
					foreach (IItemDropRule item in dropRules) {
						item.ReportDroprates(itemDropInfo, new DropRateInfoChainFeed(1f));
					}
					entry.loot.AddRange(itemDropInfo);

					foreach (DropRateInfo dropRate in itemDropInfo) {
						if (dropRate.itemId <= 0)
							continue;

						if (!entry.lootItemTypes.Contains(dropRate.itemId))
							entry.lootItemTypes.Add(dropRate.itemId);
					}

					if (entry.Key == "Terraria TorchGod") {
						entry.lootItemTypes.Add(ItemID.TorchGodsFavor); // not dropped by NPC, but rather placed in the inventory
					}
					else if (entry.Key == "Terraria BrainofCthulhu") {
						entry.lootItemTypes.Add(ItemID.TissueSample); // tissue samples are dropped by the minions
					}
				}

				// Assign this boss's treasure bag, looking through the loot found by the bestiary
				if (!vanillaBossBags.TryGetValue(entry.Key, out int bag) && entry.type != EntryType.Event) {
					foreach (int itemType in entry.lootItemTypes) {
						if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item item) && ItemID.Sets.BossBag[item.type]) {
							if (entry.collectibles.TryAdd(itemType, CollectibleType.TreasureBag) is false) {
								entry.collectibles[itemType] = CollectibleType.TreasureBag;
							}
							break;
						}
					}
				}
				else {
					if (entry.collectibles.TryAdd(bag, CollectibleType.TreasureBag) is false) {
						entry.collectibles[bag] = CollectibleType.TreasureBag;
					}
				}

				// If the treasure bag is assigned, look through its loot table for expert exclusive items
				if (entry.TreasureBag != 0) {
					List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForItemID(entry.TreasureBag);
					List<DropRateInfo> itemDropInfo = new List<DropRateInfo>();
					foreach (IItemDropRule item in dropRules) {
						item.ReportDroprates(itemDropInfo, new DropRateInfoChainFeed(1f));
					}
					entry.loot.AddRange(itemDropInfo);

					foreach (DropRateInfo dropRate in itemDropInfo) {
						if (dropRate.itemId <= 0)
							continue;

						Item item = ContentSamples.ItemsByType[dropRate.itemId];
						if (item.expert || item.expertOnly || item.master || item.masterOnly) {
							if (!entry.lootItemTypes.Contains(dropRate.itemId))
								entry.lootItemTypes.Add(dropRate.itemId);
						}
					}
				}


				// Sorts Master Mode items first, Expert Mode items second, and leaves the remaining items last
				List<int> masterItems = new List<int>();
				List<int> expertItems = new List<int>();
				List<int> normalItems = new List<int>();
				foreach (int item in entry.lootItemTypes) {
					Item refItem = ContentSamples.ItemsByType[item];
					if (refItem.master || refItem.masterOnly) {
						masterItems.Add(item);
					}
					else if (refItem.expert || refItem.expertOnly) {
						expertItems.Add(item);
					}
					else {
						normalItems.Add(item);
					}
				}
				normalItems.Sort();
				entry.lootItemTypes = masterItems.Concat(expertItems).Concat(normalItems).ToList();
			}
		}

		internal void SetupEntryRelations() {
			foreach (EntryInfo entry in SortedEntries) {
				foreach (EntryInfo distinctEntry in SortedEntries) {
					if (entry == distinctEntry || entry.type == distinctEntry.type)
						continue;

					if (entry.npcIDs.Intersect(distinctEntry.npcIDs).Any()) {
						if (!entry.relatedEntries.Contains(distinctEntry.Key)) {
							entry.relatedEntries.Add(distinctEntry.Key);
						}
						if (!distinctEntry.relatedEntries.Contains(entry.Key)) {
							distinctEntry.relatedEntries.Add(entry.Key);
						}
					}
				}
			}
		}

		internal readonly static Dictionary<string, List<int>> EntrySpawnItems = new Dictionary<string, List<int>>() {
			#region Boss SpawnItems
			{ "Terraria KingSlime", new List<int>() { ItemID.SlimeCrown } },
			{ "Terraria EyeofCthulhu", new List<int>() { ItemID.SuspiciousLookingEye } },
			{ "Terraria EaterofWorlds", new List<int>() { ItemID.WormFood } },
			{ "Terraria BrainofCthulhu", new List<int>() { ItemID.BloodySpine } },
			{ "Terraria QueenBee", new List<int>() { ItemID.Abeemination } },
			{ "Terraria Skeletron", new List<int>() { ItemID.ClothierVoodooDoll } },
			{ "Terraria Deerclops", new List<int>() { ItemID.DeerThing } },
			{ "Terraria WallofFlesh", new List<int>() { ItemID.GuideVoodooDoll } },
			{ "Terraria QueenSlimeBoss", new List<int>() { ItemID.QueenSlimeCrystal } },
			{ "Terraria TheTwins", new List<int>() { ItemID.MechanicalEye } },
			{ "Terraria TheDestroyer", new List<int>() { ItemID.MechanicalWorm } },
			{ "Terraria SkeletronPrime", new List<int>() { ItemID.MechanicalSkull } },
			// Terraria Plantera: none
			{ "Terraria Golem", new List<int>() { ItemID.LihzahrdAltar, ItemID.LihzahrdPowerCell } },
			{ "Terraria HallowBoss", new List<int>() { ItemID.EmpressButterfly } },
			{ "Terraria DD2Betsy", new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand } },
			{ "Terraria DukeFishron", new List<int>() { ItemID.TruffleWorm } },
			// Terraria CultistBoss : none
			{ "Terraria MoonLord", new List<int>() { ItemID.CelestialSigil } },
			#endregion
			// Mini-bosses tied to events will not display spawn items
			#region Event Collectibles
			{ "Terraria TorchGod", new List<int>() { ItemID.Torch } },
			{ "Terraria BloodMoon", new List<int>() { ItemID.BloodMoonStarter } },
			{ "Terraria GoblinArmy", new List<int>() { ItemID.GoblinBattleStandard } },
			{ "Terraria OldOnesArmy", new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand } },
			{ "Terraria FrostLegion", new List<int>() { ItemID.SnowGlobe } },
			{ "Terraria Eclipse", new List<int>() { ItemID.SolarTablet } },
			{ "Terraria PirateInvasion", new List<int>() { ItemID.PirateMap } },
			{ "Terraria PumpkinMoon", new List<int>() { ItemID.PumpkinMoonMedallion } },
			{ "Terraria FrostMoon", new List<int>() { ItemID.NaughtyPresent } },
			// Terraria MartianMadness: none
			// Terraria LunarEvent: none
			#endregion
		};

		internal readonly static Dictionary<string, List<int>> EntryCollectibles = new Dictionary<string, List<int>>() {
			#region Boss Collectibles
			{ "Terraria KingSlime",
				new List<int>() {
					ItemID.KingSlimeMasterTrophy,
					ItemID.KingSlimePetItem,
					ItemID.KingSlimeTrophy,
					ItemID.KingSlimeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1
				}
			},
			{ "Terraria EyeofCthulhu",
				new List<int>() {
					ItemID.EyeofCthulhuMasterTrophy,
					ItemID.EyeOfCthulhuPetItem,
					ItemID.EyeofCthulhuTrophy,
					ItemID.EyeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.AviatorSunglasses,
					ItemID.BadgersHat
				}
			},
			{ "Terraria EaterofWorlds",
				new List<int>() {
					ItemID.EaterofWorldsMasterTrophy,
					ItemID.EaterOfWorldsPetItem,
					ItemID.EaterofWorldsTrophy,
					ItemID.EaterMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.EatersBone
				}
			},
			{ "Terraria BrainofCthulhu",
				new List<int>() {
					ItemID.BrainofCthulhuMasterTrophy,
					ItemID.BrainOfCthulhuPetItem,
					ItemID.BrainofCthulhuTrophy,
					ItemID.BrainMask,
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWBoss1,
					ItemID.BoneRattle
				}
			},
			{ "Terraria QueenBee",
				new List<int>() {
					ItemID.QueenBeeMasterTrophy,
					ItemID.QueenBeePetItem,
					ItemID.QueenBeeTrophy,
					ItemID.BeeMask,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss1,
					ItemID.Nectar,
				}
			},
			{ "Terraria Skeletron",
				new List<int>() {
					ItemID.SkeletronMasterTrophy,
					ItemID.SkeletronPetItem,
					ItemID.SkeletronTrophy,
					ItemID.SkeletronMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.ChippysCouch
				}
			},
			{ "Terraria Deerclops",
				new List<int>() {
					ItemID.DeerclopsMasterTrophy,
					ItemID.DeerclopsPetItem,
					ItemID.DeerclopsTrophy,
					ItemID.DeerclopsMask,
					ItemID.MusicBoxDeerclops,
					ItemID.MusicBoxOWBoss1
				}
			},
			{ "Terraria WallofFlesh",
				new List<int>() {
					ItemID.WallofFleshMasterTrophy,
					ItemID.WallOfFleshGoatMountItem,
					ItemID.WallofFleshTrophy,
					ItemID.FleshMask,
					ItemID.MusicBoxBoss2,
					ItemID.MusicBoxOWWallOfFlesh,
					ItemID.BadgersHat
				}
			},
			{ "Terraria QueenSlimeBoss",
				new List<int>() {
					ItemID.QueenSlimeMasterTrophy,
					ItemID.QueenSlimePetItem,
					ItemID.QueenSlimeTrophy,
					ItemID.QueenSlimeMask,
					ItemID.MusicBoxQueenSlime,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria TheTwins",
				new List<int>() {
					ItemID.TwinsMasterTrophy,
					ItemID.TwinsPetItem,
					ItemID.RetinazerTrophy,
					ItemID.SpazmatismTrophy,
					ItemID.TwinMask,
					ItemID.MusicBoxBoss2,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria TheDestroyer",
				new List<int>() {
					ItemID.DestroyerMasterTrophy,
					ItemID.DestroyerPetItem,
					ItemID.DestroyerTrophy,
					ItemID.DestroyerMask,
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria SkeletronPrime",
				new List<int>() {
					ItemID.SkeletronPrimeMasterTrophy,
					ItemID.SkeletronPrimePetItem,
					ItemID.SkeletronPrimeTrophy,
					ItemID.SkeletronPrimeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria Plantera",
				new List<int>() {
					ItemID.PlanteraMasterTrophy,
					ItemID.PlanteraPetItem,
					ItemID.PlanteraTrophy,
					ItemID.PlanteraMask,
					ItemID.MusicBoxPlantera,
					ItemID.MusicBoxOWPlantera,
					ItemID.Seedling
				}
			},
			{ "Terraria Golem",
				new List<int>() {
					ItemID.GolemMasterTrophy,
					ItemID.GolemPetItem,
					ItemID.GolemTrophy,
					ItemID.GolemMask,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria HallowBoss",
				new List<int>() {
					ItemID.FairyQueenMasterTrophy,
					ItemID.FairyQueenPetItem,
					ItemID.FairyQueenTrophy,
					ItemID.FairyQueenMask,
					ItemID.MusicBoxEmpressOfLight,
					ItemID.MusicBoxOWBoss2,
					ItemID.HallowBossDye,
					ItemID.RainbowCursor,
				}
			},
			{ "Terraria DD2Betsy",
				new List<int>() {
					ItemID.BetsyMasterTrophy,
					ItemID.DD2BetsyPetItem,
					ItemID.BossTrophyBetsy,
					ItemID.BossMaskBetsy,
					ItemID.MusicBoxDD2,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria DukeFishron",
				new List<int>() {
					ItemID.DukeFishronMasterTrophy,
					ItemID.DukeFishronPetItem,
					ItemID.DukeFishronTrophy,
					ItemID.DukeFishronMask,
					ItemID.MusicBoxDukeFishron,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria CultistBoss",
				new List<int>() {
					ItemID.LunaticCultistMasterTrophy,
					ItemID.LunaticCultistPetItem,
					ItemID.AncientCultistTrophy,
					ItemID.BossMaskCultist,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss2
				}
			},
			{ "Terraria MoonLord",
				new List<int>() {
					ItemID.MoonLordMasterTrophy,
					ItemID.MoonLordPetItem,
					ItemID.MoonLordTrophy,
					ItemID.BossMaskMoonlord,
					ItemID.MusicBoxLunarBoss,
					ItemID.MusicBoxOWMoonLord
				}
			},
			#endregion
			#region Mini-boss Collectibles
			{ "Terraria DD2DarkMageT3",
				new List<int>() {
					ItemID.DarkMageMasterTrophy,
					ItemID.DarkMageBookMountItem,
					ItemID.BossTrophyDarkmage,
					ItemID.BossMaskDarkMage,
					ItemID.DD2PetDragon,
					ItemID.DD2PetGato
				}
			},
			{ "Terraria PirateShip",
				new List<int>() {
					ItemID.FlyingDutchmanMasterTrophy,
					ItemID.PirateShipMountItem,
					ItemID.FlyingDutchmanTrophy
				}
			},
			{ "Terraria DD2OgreT3",
				new List<int>() {
					ItemID.OgreMasterTrophy,
					ItemID.DD2OgrePetItem,
					ItemID.BossTrophyOgre,
					ItemID.BossMaskOgre,
					ItemID.DD2PetGhost
				}
			},
			{ "Terraria MourningWood",
				new List<int>() {
					ItemID.MourningWoodMasterTrophy,
					ItemID.SpookyWoodMountItem,
					ItemID.MourningWoodTrophy,
					ItemID.CursedSapling
				}
			},
			{ "Terraria Pumpking",
				new List<int>() {
					ItemID.PumpkingMasterTrophy,
					ItemID.PumpkingPetItem,
					ItemID.PumpkingTrophy,
					ItemID.SpiderEgg
				}
			},
			{ "Terraria Everscream",
				new List<int>() {
					ItemID.EverscreamMasterTrophy,
					ItemID.EverscreamPetItem,
					ItemID.EverscreamTrophy
				}
			},
			{ "Terraria SantaNK1",
				new List<int>() {
					ItemID.SantankMasterTrophy,
					ItemID.SantankMountItem,
					ItemID.SantaNK1Trophy
				}
			},
			{ "Terraria IceQueen",
				new List<int>() {
					ItemID.IceQueenMasterTrophy,
					ItemID.IceQueenPetItem,
					ItemID.IceQueenTrophy,
					ItemID.BabyGrinchMischiefWhistle
				}
			},
			{ "Terraria MartianSaucer",
				new List<int>() {
					ItemID.UFOMasterTrophy,
					ItemID.MartianPetItem,
					ItemID.MartianSaucerTrophy
				}
			},
			#endregion
			#region Event Collectibles
			{ "Terraria TorchGod",
				new List<int>() {
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWWallOfFlesh
				}
			},
			{ "Terraria BloodMoon",
				new List<int>() {
					ItemID.MusicBoxEerie,
					ItemID.MusicBoxOWBloodMoon
				}
			},
			{ "Terraria GoblinArmy",
				new List<int>() {
					ItemID.MusicBoxGoblins,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria OldOnesArmy",
				new List<int>() {
					ItemID.MusicBoxDD2,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria FrostLegion",
				new List<int>() {
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria Eclipse",
				new List<int>() {
					ItemID.MusicBoxEclipse,
					ItemID.MusicBoxOWBloodMoon
				}
			},
			{ "Terraria PirateInvasion",
				new List<int>() {
					ItemID.MusicBoxPirates,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria PumpkinMoon",
				new List<int>() {
					ItemID.MusicBoxPumpkinMoon,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria FrostMoon",
				new List<int>() {
					ItemID.MusicBoxFrostMoon,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria MartianMadness",
				new List<int>() {
					ItemID.MusicBoxMartians,
					ItemID.MusicBoxOWInvasion
				}
			},
			{ "Terraria LunarEvent",
				new List<int>() {
					ItemID.MusicBoxTowers,
					ItemID.MusicBoxOWTowers
				}
			}
			#endregion
		};

		internal readonly static List<string> EventKeysWhoHaveBelongToInvasionSets = new List<string>() {
			"Terraria GoblinArmy",
			"Terraria OldOnesArmy",
			"Terraria FrostLegion",
			"Terraria PirateInvasion",
			"Terraria MartianMadness",
		};

		internal static bool[] GetBelongsToInvasionSet(string Key) {
			return Key switch {
				"Terraria GoblinArmy" => NPCID.Sets.BelongsToInvasionGoblinArmy,
				"Terraria OldOnesArmy" => NPCID.Sets.BelongsToInvasionOldOnesArmy,
				"Terraria FrostLegion" => NPCID.Sets.BelongsToInvasionFrostLegion,
				"Terraria PirateInvasion" => NPCID.Sets.BelongsToInvasionPirate,
				"Terraria MartianMadness" => NPCID.Sets.BelongsToInvasionMartianMadness,
				_ => null
			};
		}

		internal readonly static Dictionary<string, List<int>> EventNPCs = new Dictionary<string, List<int>>() {
			{ "Terraria TorchGod",
				new List<int>() {
					NPCID.TorchGod,
				}
			},
			{ "Terraria BloodMoon",
				new List<int>() {
					NPCID.BloodZombie,
					NPCID.Drippler,
					NPCID.TheGroom,
					NPCID.TheBride,
					NPCID.CorruptBunny,
					NPCID.CrimsonBunny,
					NPCID.CorruptGoldfish,
					NPCID.CrimsonGoldfish,
					NPCID.CorruptPenguin,
					NPCID.CrimsonPenguin,
					NPCID.Clown,
					NPCID.ChatteringTeethBomb,
					NPCID.EyeballFlyingFish,
					NPCID.ZombieMerman,
					NPCID.GoblinShark,
					NPCID.BloodEelHead,
					NPCID.BloodSquid,
					NPCID.BloodNautilus,
				}
			},
			
			// Goblin Army uses BelongsToInvasion set

			{ "Terraria OldOnesArmy",
				new List<int>() {
					NPCID.DD2GoblinT3,
					NPCID.DD2GoblinBomberT3,
					NPCID.DD2JavelinstT3,
					NPCID.DD2KoboldWalkerT3,
					NPCID.DD2KoboldFlyerT3,
					NPCID.DD2WyvernT3,
					NPCID.DD2DrakinT3,
					NPCID.DD2LightningBugT3,
					NPCID.DD2SkeletonT3,
					NPCID.DD2DarkMageT3,
					NPCID.DD2OgreT3,
					NPCID.DD2Betsy
				}
			},
			
			// Frost Legion uses BelongsToInvasion set

			{ "Terraria Eclipse",
				new List<int>() {
					NPCID.Eyezor,
					NPCID.Frankenstein,
					NPCID.SwampThing,
					NPCID.Vampire,
					NPCID.CreatureFromTheDeep,
					NPCID.Fritz,
					NPCID.ThePossessed,
					NPCID.Reaper,
					NPCID.Butcher,
					NPCID.DeadlySphere,
					NPCID.DrManFly,
					NPCID.Nailhead,
					NPCID.Psycho,
					NPCID.Mothron,
					NPCID.MothronSpawn,
				}
			},
			
			// Pirate Invasion uses BelongsToInvasion set

			{ "Terraria PumpkinMoon",
				new List<int>() {
					NPCID.Scarecrow1,
					NPCID.Splinterling,
					NPCID.Hellhound,
					NPCID.Poltergeist,
					NPCID.HeadlessHorseman,
					NPCID.MourningWood,
					NPCID.Pumpking,
				}
			},
			{ "Terraria FrostMoon",
				new List<int>() {
					NPCID.PresentMimic,
					NPCID.Flocko,
					NPCID.GingerbreadMan,
					NPCID.ZombieElf,
					NPCID.ElfArcher,
					NPCID.Nutcracker,
					NPCID.Yeti,
					NPCID.ElfCopter,
					NPCID.Krampus,
					NPCID.Everscream,
					NPCID.SantaNK1,
					NPCID.IceQueen
				}
			},
			
			// Martian Madness uses BelongsToInvasion set

			{ "Terraria LunarEvent",
				new List<int>() {
					NPCID.LunarTowerSolar,
					NPCID.SolarSolenian,
					NPCID.SolarSpearman,
					NPCID.SolarCorite,
					NPCID.SolarSroller,
					NPCID.SolarCrawltipedeHead,
					NPCID.SolarDrakomire,
					NPCID.SolarDrakomireRider,

					NPCID.LunarTowerVortex,
					NPCID.VortexHornet,
					NPCID.VortexHornetQueen,
					NPCID.VortexLarva,
					NPCID.VortexRifleman,
					NPCID.VortexSoldier,

					NPCID.LunarTowerNebula,
					NPCID.NebulaBeast,
					NPCID.NebulaBrain,
					NPCID.NebulaHeadcrab,
					NPCID.NebulaSoldier,

					NPCID.LunarTowerStardust,
					NPCID.StardustCellBig,
					NPCID.StardustJellyfishBig,
					NPCID.StardustSoldier,
					NPCID.StardustSpiderBig,
					NPCID.StardustWormHead,
				}
			}
		};

		internal readonly static Dictionary<string, int> vanillaBossBags = new Dictionary<string, int>() {
			{ "Terraria KingSlime", ItemID.KingSlimeBossBag },
			{ "Terraria EyeofCthulhu", ItemID.EyeOfCthulhuBossBag },
			{ "Terraria EaterofWorlds", ItemID.EaterOfWorldsBossBag },
			{ "Terraria BrainofCthulhu", ItemID.BrainOfCthulhuBossBag },
			{ "Terraria QueenBee", ItemID.QueenBeeBossBag },
			{ "Terraria Skeletron", ItemID.SkeletronBossBag },
			{ "Terraria WallofFlesh", ItemID.WallOfFleshBossBag },
			{ "Terraria TheTwins", ItemID.TwinsBossBag },
			{ "Terraria TheDestroyer", ItemID.DestroyerBossBag },
			{ "Terraria SkeletronPrime", ItemID.SkeletronPrimeBossBag },
			{ "Terraria Plantera", ItemID.PlanteraBossBag },
			{ "Terraria Golem", ItemID.GolemBossBag },
			{ "Terraria DukeFishron", ItemID.FishronBossBag },
			{ "Terraria MoonLord", ItemID.MoonLordBossBag },
			{ "Terraria DD2Betsy", ItemID.BossBagBetsy },
			{ "Terraria QueenSlimeBoss", ItemID.QueenSlimeBossBag },
			{ "Terraria HallowBoss", ItemID.FairyQueenBossBag },
			{ "Terraria Deerclops", ItemID.DeerclopsBossBag },
			// Unobtainable treasure bages...
			{ "Terraria DD2DarkMageT3", ItemID.BossBagDarkMage },
			{ "Terraria DD2OgreT3", ItemID.BossBagOgre },
			{ "Terraria CultistBoss", ItemID.CultistBossBag }
		};

		public bool IsRegisteredMusicBox(int type) => vanillaMusicBoxTypes.Contains(type) || otherWorldMusicBoxTypes.Contains(type) || BossChecklist.itemToMusicReference.ContainsKey(type);

		// Vanilla and Other World music boxes are in order given by the official Terraria wiki
		public readonly List<int> vanillaMusicBoxTypes = new List<int>() {
			ItemID.MusicBoxOverworldDay,
			ItemID.MusicBoxAltOverworldDay,
			ItemID.MusicBoxNight,
			ItemID.MusicBoxRain,
			ItemID.MusicBoxSnow,
			ItemID.MusicBoxIce,
			ItemID.MusicBoxDesert,
			ItemID.MusicBoxOcean,
			ItemID.MusicBoxOceanAlt,
			ItemID.MusicBoxSpace,
			ItemID.MusicBoxSpaceAlt,
			ItemID.MusicBoxUnderground,
			ItemID.MusicBoxAltUnderground,
			ItemID.MusicBoxMushrooms,
			ItemID.MusicBoxJungle,
			ItemID.MusicBoxCorruption,
			ItemID.MusicBoxUndergroundCorruption,
			ItemID.MusicBoxCrimson,
			ItemID.MusicBoxUndergroundCrimson,
			ItemID.MusicBoxTheHallow,
			ItemID.MusicBoxUndergroundHallow,
			ItemID.MusicBoxHell,
			ItemID.MusicBoxDungeon,
			ItemID.MusicBoxTemple,
			ItemID.MusicBoxBoss1,
			ItemID.MusicBoxBoss2,
			ItemID.MusicBoxBoss3,
			ItemID.MusicBoxBoss4,
			ItemID.MusicBoxBoss5,
			ItemID.MusicBoxDeerclops,
			ItemID.MusicBoxQueenSlime,
			ItemID.MusicBoxPlantera,
			ItemID.MusicBoxEmpressOfLight,
			ItemID.MusicBoxDukeFishron,
			ItemID.MusicBoxEerie,
			ItemID.MusicBoxEclipse,
			ItemID.MusicBoxGoblins,
			ItemID.MusicBoxPirates,
			ItemID.MusicBoxMartians,
			ItemID.MusicBoxPumpkinMoon,
			ItemID.MusicBoxFrostMoon,
			ItemID.MusicBoxTowers,
			ItemID.MusicBoxLunarBoss,
			ItemID.MusicBoxSandstorm,
			ItemID.MusicBoxDD2,
			ItemID.MusicBoxSlimeRain,
			ItemID.MusicBoxTownDay,
			ItemID.MusicBoxTownNight,
			ItemID.MusicBoxWindyDay,
			ItemID.MusicBoxDayRemix,
			ItemID.MusicBoxTitleAlt, // Journey's Beginning
			ItemID.MusicBoxStorm,
			ItemID.MusicBoxGraveyard,
			ItemID.MusicBoxUndergroundJungle,
			ItemID.MusicBoxJungleNight,
			ItemID.MusicBoxMorningRain,
			ItemID.MusicBoxConsoleTitle,
			ItemID.MusicBoxUndergroundDesert,
			ItemID.MusicBoxCredits, // Journey's End
			ItemID.MusicBoxTitle,
		};

		public readonly List<int> otherWorldMusicBoxTypes = new List<int>() {
			ItemID.MusicBoxOWRain,
			ItemID.MusicBoxOWDay,
			ItemID.MusicBoxOWNight,
			ItemID.MusicBoxOWUnderground,
			ItemID.MusicBoxOWDesert,
			ItemID.MusicBoxOWOcean,
			ItemID.MusicBoxOWMushroom,
			ItemID.MusicBoxOWDungeon,
			ItemID.MusicBoxOWSpace,
			ItemID.MusicBoxOWUnderworld,
			ItemID.MusicBoxOWSnow,
			ItemID.MusicBoxOWCorruption,
			ItemID.MusicBoxOWUndergroundCorruption,
			ItemID.MusicBoxOWCrimson,
			ItemID.MusicBoxOWUndergroundCrimson,
			ItemID.MusicBoxOWUndergroundSnow, // Ice
			ItemID.MusicBoxOWUndergroundHallow,
			ItemID.MusicBoxOWBloodMoon, // Eerie
			ItemID.MusicBoxOWBoss2,
			ItemID.MusicBoxOWBoss1,
			ItemID.MusicBoxOWInvasion,
			ItemID.MusicBoxOWTowers,
			ItemID.MusicBoxOWMoonLord,
			ItemID.MusicBoxOWPlantera,
			ItemID.MusicBoxOWJungle,
			ItemID.MusicBoxOWWallOfFlesh,
			ItemID.MusicBoxOWHallow,
		};

		internal void AddEntry(EntryType type, Mod mod, string iName, float val, Func<bool> down, List<int> id, Dictionary<string, object> extra = null) {
			EnsureBossIsNotDuplicate(mod?.Name ?? "Unknown", iName);
			SortedEntries.Add(new EntryInfo(type, mod?.Name ?? "Unknown", iName, val, down, id, extra));
			LogNewBoss(mod?.DisplayName ?? "Unknown", iName);
		}

		internal void AddOrphanData(OrphanType type, Mod mod, Dictionary<string, object> values) {
			if (values is null) {
				BossChecklist.instance.LogWarning("OldCall_Orphan", requiresConfig: false, type, mod.Name);
			}
			else {
				ExtraData.Add(new OrphanInfo(type, mod.DisplayName, values)); // Mod instance is checked to be valid before created within ModCall
			}
		}

		internal void EnsureBossIsNotDuplicate(string mod, string internalName) {
			if (SortedEntries.Any(x=> x.Key == $"{mod} {internalName}"))
				throw new Exception(Language.GetText("DuplicateEntry").Format(mod, internalName));
		}

		internal void LogNewBoss(string mod, string name) {
			if (!BossChecklist.BossLogConfig.Debug.ModCallLogVerbose)
				return;

			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("[Boss Checklist] ");
			Console.ResetColor();
			Console.Write("Boss Log entry added: ");
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.Write("[" + mod + "] ");
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write(name);
			Console.WriteLine();
			Console.ResetColor();

			/*
			if (OldCalls.Values.Any(x => x.Contains(name))) {
				BossChecklist.instance.Logger.Warn($"Entry successfully registered to the Boss Log: [{mod} {name}] (outdated mod call)");
			}
			else {
				BossChecklist.instance.Logger.Info($"Entry successfully registered to the Boss Log: [{mod} {name}]");
			}
			*/
		}
	}
}
