using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BossChecklist
{
	internal class BossTracker
	{
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
		public const float Betsy = 14f;
		public const float EmpressOfLight = 15f;
		public const float DukeFishron = 16f;
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
		public const float PumpkinMoon = 12.33f;
		public const float MourningWood = PumpkinMoon + 0.01f;
		public const float Pumpking = PumpkinMoon + 0.02f;
		public const float FrostMoon = 12.66f;
		public const float Everscream = FrostMoon + 0.01f;
		public const float SantaNK1 = FrostMoon + 0.02f;
		public const float IceQueen = FrostMoon + 0.03f;
		public const float MartianMadness = 13.5f;
		public const float MartianSaucer = MartianMadness + 0.01f;
		public const float LunarEvent = LunaticCultist + 0.01f; // Happens immediately after the defeation of the Lunatic Cultist

		/// <summary>
		/// All currently loaded bosses/minibosses/events sorted in progression order.
		/// </summary>
		internal List<BossInfo> SortedBosses;
		internal bool[] BossCache; 
		internal List<OrphanInfo> ExtraData;
		internal bool BossesFinalized = false;
		internal bool AnyModHasOldCall = false;
		internal Dictionary<string, List<string>> OldCalls = new();
		internal List<string> BossRecordKeys;

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaBosses();
			ExtraData = new List<OrphanInfo>();
			BossRecordKeys = new List<string>();
		}

		private void InitializeVanillaBosses() {
			SortedBosses = new List<BossInfo> {
				// Bosses -- Vanilla
				BossInfo.MakeVanillaBoss(EntryType.Boss, KingSlime, "$NPCName.KingSlime", new List<int>() { NPCID.KingSlime }, () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.KingSlime}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EyeOfCthulhu, "$NPCName.EyeofCthulhu", new List<int>() { NPCID.EyeofCthulhu }, () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.EaterofWorldsHead", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, () => NPC.downedBoss2, new List<int>() { ItemID.WormFood })
					.WithCustomAvailability(() => !WorldGen.crimson || Main.drunkWorld || ModLoader.TryGetMod("BothEvils", out Mod mod))
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.EaterofWorldsHead}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.BrainofCthulhu", new List<int>() { NPCID.BrainofCthulhu }, () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine })
					.WithCustomAvailability(() => WorldGen.crimson || Main.drunkWorld || ModLoader.TryGetMod("BothEvils", out Mod mod)),
				BossInfo.MakeVanillaBoss(EntryType.Boss, QueenBee, "$NPCName.QueenBee", new List<int>() { NPCID.QueenBee }, () => NPC.downedQueenBee, new List<int>() { ItemID.Abeemination }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Skeletron, "$NPCName.SkeletronHead", new List<int>() { NPCID.SkeletronHead, NPCID.SkeletronHand }, () => NPC.downedBoss3, new List<int>() { ItemID.ClothierVoodooDoll })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.SkeletronHead}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, DeerClops, "$NPCName.Deerclops", new List<int>() { NPCID.Deerclops }, () => NPC.downedDeerclops, new List<int>() { ItemID.DeerThing })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Deerclops}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, WallOfFlesh, "$NPCName.WallofFlesh", new List<int>() { NPCID.WallofFlesh, NPCID.WallofFleshEye }, () => Main.hardMode, new List<int>() { ItemID.GuideVoodooDoll })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.WallofFlesh}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, QueenSlime, "$NPCName.QueenSlimeBoss", new List<int>() { NPCID.QueenSlimeBoss }, () => NPC.downedQueenSlime, new List<int>() { ItemID.QueenSlimeCrystal })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.QueenSlimeBoss}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheTwins, "$Enemies.TheTwins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, () => NPC.downedMechBoss2, new List<int>() { ItemID.MechanicalEye })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Retinazer}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheDestroyer, "$NPCName.TheDestroyer", new List<int>() { NPCID.TheDestroyer }, () => NPC.downedMechBoss1, new List<int>() { ItemID.MechanicalWorm })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.TheDestroyer}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, SkeletronPrime, "$NPCName.SkeletronPrime", new List<int>() { NPCID.SkeletronPrime }, () => NPC.downedMechBoss3, new List<int>() { ItemID.MechanicalSkull })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.SkeletronPrime}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Plantera, "$NPCName.Plantera", new List<int>() { NPCID.Plantera }, () => NPC.downedPlantBoss, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem, "$NPCName.Golem", new List<int>() { NPCID.Golem, NPCID.GolemHead }, () => NPC.downedGolemBoss, new List<int>() { ItemID.LihzahrdPowerCell, ItemID.LihzahrdAltar })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Golem}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Betsy, "$NPCName.DD2Betsy", new List<int>() { NPCID.DD2Betsy }, () => WorldAssist.downedInvasionT3Ours, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2Betsy}"),
					// No despawn message due to being in an event
				BossInfo.MakeVanillaBoss(EntryType.Boss, EmpressOfLight, "$NPCName.HallowBoss", new List<int>() { NPCID.HallowBoss }, () => NPC.downedEmpressOfLight, new List<int>() { ItemID.EmpressButterfly })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.HallowBoss}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, DukeFishron, "$NPCName.DukeFishron", new List<int>() { NPCID.DukeFishron }, () => NPC.downedFishron, new List<int>() { ItemID.TruffleWorm }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, LunaticCultist, "$NPCName.CultistBoss", new List<int>() { NPCID.CultistBoss }, () => NPC.downedAncientCultist, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.CultistBoss}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Moonlord, "$Enemies.MoonLord", new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand }, () => NPC.downedMoonlord, new List<int>() { ItemID.CelestialSigil })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.MoonLordHead}"),

				// Minibosses and Events -- Vanilla
				BossInfo.MakeVanillaEvent(TorchGod, "$NPCName.TorchGod", () => WorldAssist.downedTorchGod, new List<int>() { ItemID.Torch })
					.WithCustomHeadIcon($"Terraria/Images/Item_{ItemID.TorchGodsFavor}"),
				BossInfo.MakeVanillaEvent(BloodMoon, "$Bestiary_Events.BloodMoon", () => WorldAssist.downedBloodMoon, new List<int>() { ItemID.BloodMoonStarter })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventBloodMoon")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventBloodMoon_Head"),
				// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
				BossInfo.MakeVanillaEvent(GoblinArmy, "Goblin Army", () => NPC.downedGoblins, new List<int>() { ItemID.GoblinBattleStandard })
					.WithCustomTranslationKey("$LegacyInterface.88")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventGoblinArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_9"),
				BossInfo.MakeVanillaEvent(OldOnesArmy, "Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand })
					.WithCustomTranslationKey("$DungeonDefenders2.InvasionProgressTitle")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventOldOnesArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_79"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, DarkMage, "$NPCName.DD2DarkMageT3", new List<int>() { NPCID.DD2DarkMageT3 }, () => WorldAssist.downedDarkMage, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2DarkMageT3}"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Ogre, "$NPCName.DD2OgreT3", new List<int>() { NPCID.DD2OgreT3 }, () => WorldAssist.downedOgre, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2OgreT3}"),
				BossInfo.MakeVanillaEvent(FrostLegion, "Frost Legion", () => NPC.downedFrost, new List<int>() { ItemID.SnowGlobe })
					.WithCustomTranslationKey("$LegacyInterface.87")
					.WithCustomAvailability(() => Main.xMas)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostLegion")
					.WithCustomHeadIcon("Terraria/Images/Extra_7"),
				BossInfo.MakeVanillaEvent(PirateInvasion, "Pirate Invasion", () => NPC.downedPirates, new List<int>() { ItemID.PirateMap })
					.WithCustomTranslationKey("$LegacyInterface.86")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPirateInvasion")
					.WithCustomHeadIcon("Terraria/Images/Extra_11"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, PirateShip, "$NPCName.PirateShip", new List<int>() { NPCID.PirateShip }, () => WorldAssist.downedFlyingDutchman, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.PirateShip}"),
				BossInfo.MakeVanillaEvent(SolarEclipse, "$Bestiary_Events.Eclipse", () => WorldAssist.downedSolarEclipse, new List<int>() { ItemID.SolarTablet })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventSolarEclipse")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventSolarEclipse_Head"),
				BossInfo.MakeVanillaEvent(PumpkinMoon, "Pumpkin Moon", () => WorldAssist.downedPumpkinMoon, new List<int>() { ItemID.PumpkinMoonMedallion })
					.WithCustomTranslationKey("$LegacyInterface.84")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPumpkinMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_12"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, MourningWood, "$NPCName.MourningWood", new List<int>() { NPCID.MourningWood }, () => NPC.downedHalloweenTree, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Pumpking, "$NPCName.Pumpking", new List<int>() { NPCID.Pumpking }, () => NPC.downedHalloweenKing, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Pumpking}"),
				BossInfo.MakeVanillaEvent(FrostMoon, "Frost Moon", () => WorldAssist.downedFrostMoon, new List<int>() { ItemID.NaughtyPresent })
					.WithCustomTranslationKey("$LegacyInterface.83")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_8"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Everscream, "$NPCName.Everscream", new List<int>() { NPCID.Everscream }, () => NPC.downedChristmasTree, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, SantaNK1, "$NPCName.SantaNK1", new List<int>() { NPCID.SantaNK1 }, () => NPC.downedChristmasSantank, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, IceQueen, "$NPCName.IceQueen", new List<int>() { NPCID.IceQueen }, () => NPC.downedChristmasIceQueen, new List<int>() { }),
				BossInfo.MakeVanillaEvent(MartianMadness, "Martian Madness", () => NPC.downedMartians, new List<int>() { })
					.WithCustomTranslationKey("$LegacyInterface.85")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventMartianMadness")
					.WithCustomHeadIcon($"Terraria/Images/Extra_10"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, MartianSaucer, "$NPCName.MartianSaucer", new List<int>() { NPCID.MartianSaucer, NPCID.MartianSaucerCore }, () => WorldAssist.downedMartianSaucer, new List<int>() { }),
				BossInfo.MakeVanillaEvent(LunarEvent, "Lunar Event", () => NPC.downedTowerNebula && NPC.downedTowerVortex && NPC.downedTowerSolar && NPC.downedTowerStardust, new List<int>() { })
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

		internal void FinalizeOrphanData() {
			foreach (OrphanInfo orphan in ExtraData) {
				BossInfo bossInfo = SortedBosses.Find(boss => boss.Key == orphan.Key);
				if (bossInfo != null && orphan.values != null) {
					switch (orphan.type) {
						case OrphanType.Loot:
							bossInfo.lootItemTypes.AddRange(orphan.values);
							break;
						case OrphanType.Collection:
							bossInfo.collection.AddRange(orphan.values);
							break;
						case OrphanType.SpawnItem:
							bossInfo.spawnItem.AddRange(orphan.values);
							break;
						case OrphanType.EventNPC:
							if (bossInfo.type == EntryType.Event) {
								bossInfo.npcIDs.AddRange(orphan.values);
							}
							else {
								BossChecklist.instance.Logger.Info($"{orphan.bossName} from {orphan.modSource} is not an Event entry. AddToEventNPCs must be added to Events.");
							}
							break;
					}
				}
				else if (BossChecklist.DebugConfig.ModCallLogVerbose) {
					string nullBossInfo = $"Could not find {orphan.bossName} from {orphan.modSource} to add OrphanInfo to.";
					string emptyValues = $"Orphan values for {orphan.bossName} from {orphan.modSource} found to be empty.";
					BossChecklist.instance.Logger.Warn((bossInfo == null ? nullBossInfo : "") + (orphan.values == null ? emptyValues : ""));
				}
			}
		}

		internal void FinalizeCollectionTypes() {
			foreach (BossInfo boss in SortedBosses) {
				foreach (int type in boss.collection) {
					if (!ContentSamples.ItemsByType.TryGetValue(type, out Item temp)) {
						continue;
					}
					if (temp.headSlot > 0 && temp.vanity) {
						boss.collectType.Add(type, CollectionType.Mask);
					}
					else if (vanillaMusicBoxTypes.Contains(type) || otherWorldMusicBoxTypes.Contains(type) || BossChecklist.itemToMusicReference.ContainsKey(type)) {
						boss.collectType.Add(type, CollectionType.MusicBox);
					}
					else if (temp.master && temp.shoot > ProjectileID.None && temp.buffType > 0) {
						boss.collectType.Add(type, CollectionType.Pet);
					}
					else if (temp.master && temp.mountType > MountID.None) {
						boss.collectType.Add(type, CollectionType.Mount);
					}
					else if (temp.createTile > TileID.Dirt) {
						TileObjectData data = TileObjectData.GetTileData(temp.createTile, temp.placeStyle);
						if (data.AnchorWall == TileObjectData.Style3x3Wall.AnchorWall && data.Width == 3 && data.Height == 3) {
							boss.collectType.Add(type, CollectionType.Trophy);
						}
						else if (temp.master && data.Width == 3 && data.Height == 4) {
							boss.collectType.Add(type, CollectionType.Relic);
						}
						else {
							boss.collectType.Add(type, CollectionType.Generic);
						}
					}
					else {
						boss.collectType.Add(type, CollectionType.Generic);
					}
				}
			}
		}

		internal void FinalizeBossData() {
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
			// 
			for (int i = 0; i < SortedBosses.Count; i++) {
				if (SortedBosses[i].type == EntryType.Boss)
					BossRecordKeys.Add(SortedBosses[i].Key);
			}

			// Bosses are now finalized. Entries can no longer be added or edited through Mod Calls.
			BossesFinalized = true;
			if (AnyModHasOldCall) {
				foreach (var oldCall in OldCalls) {
					BossChecklist.instance.Logger.Info($"{oldCall.Key} calls for the following either not utilizing Boss Log features or is using an old call method for it. Mod developers should update mod calls with proper information to improve user experience. {oldCall.Key} entries include: [{string.Join(", ", oldCall.Value)}]");
				}
				OldCalls.Clear();
				BossChecklist.instance.Logger.Info("Updated Mod.Call documentation for BossChecklist can be found here: https://github.com/JavidPack/BossChecklist/wiki/%5B1.4-alpha%5D-Mod-Call-Structure");
			}

			// The server must populate for collected records after all bosses have been counted and sorted.
			if (Main.netMode == NetmodeID.Server) {
				BossChecklist.ServerCollectedRecords = new List<BossRecord>[Main.maxPlayers];
				for (int i = 0; i < Main.maxPlayers; i++) {
					BossChecklist.ServerCollectedRecords[i] = new List<BossRecord>();
					foreach (BossInfo info in BossChecklist.bossTracker.SortedBosses) {
						// Be sure to only populate with Boss type entries as they are the only entries that can have records to begin with
						if (info.type == EntryType.Boss) {
							BossChecklist.ServerCollectedRecords[i].Add(new BossRecord(info.Key));
						}
					}
				}
			}

			BossCache = new bool[NPCLoader.NPCCount];
			foreach (var boss in SortedBosses) {
				boss.npcIDs.ForEach(x => BossCache[x] = true);
			}
		}

		internal void FinalizeBossLootTables() {
			foreach (BossInfo boss in SortedBosses) {
				// Loot is easily found through the item drop database.
				foreach (int npc in boss.npcIDs) {
					List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npc, false);
					List<DropRateInfo> itemDropInfo = new List<DropRateInfo>();
					DropRateInfoChainFeed ratesInfo = new DropRateInfoChainFeed(1f);
					foreach (IItemDropRule item in dropRules) {
						item.ReportDroprates(itemDropInfo, ratesInfo);
					}
					boss.loot.AddRange(itemDropInfo);

					foreach (DropRateInfo dropRate in itemDropInfo) {
						if (!boss.lootItemTypes.Contains(dropRate.itemId)) {
							boss.lootItemTypes.Add(dropRate.itemId);
						}
					}

					// Add Torch God's Favor since its not technically an NPC drop.
					// The rest of added items are unobtainable vanilla boss bags, and are added only for visual purposes
					if (boss.Key == "Terraria TorchGod") {
						boss.lootItemTypes.Add(ItemID.TorchGodsFavor);
					}
					else if (boss.Key == "Terraria BrainofCthulhu") {
						boss.lootItemTypes.Add(ItemID.TissueSample);
					}
					else if (boss.Key == "Terraria DD2DarkMageT3") {
						boss.lootItemTypes.Add(ItemID.BossBagDarkMage);
					}
					else if (boss.Key == "Terraria DD2OgreT3") {
						boss.lootItemTypes.Add(ItemID.BossBagOgre);
					}
					else if (boss.Key == "Terraria CultistBoss") {
						boss.lootItemTypes.Add(ItemID.CultistBossBag);
					}
				}

				// Assign this boss's treasure bag, looking through the loot list provided
				if (!vanillaBossBags.TryGetValue(boss.Key, out boss.treasureBag)) {
					foreach (int itemType in boss.lootItemTypes) {
						if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item item)) {
							if (item.ModItem != null && item.ModItem.BossBagNPC > 0) {
								boss.treasureBag = itemType;
								break;
							}
						}
					}
				}
			}
		}

		internal readonly Dictionary<string, List<int>> BossCollections = new Dictionary<string, List<int>>() {
			#region Boss Collectibles
			{ "Terraria KingSlime",
				new List<int>(){
					ItemID.KingSlimeMasterTrophy,
					ItemID.KingSlimePetItem,
					ItemID.KingSlimeTrophy,
					ItemID.KingSlimeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1
				}
			},
			{ "Terraria EyeofCthulhu",
				new List<int>(){
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
			{ "Terraria EaterofWorldsHead",
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
			{ "Terraria SkeletronHead",
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

		internal readonly Dictionary<string, List<int>> EventNPCs = new Dictionary<string, List<int>>() {
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
			{ "Terraria GoblinArmy",
					new List<int>() {
					NPCID.GoblinScout,
					NPCID.GoblinPeon,
					NPCID.GoblinSorcerer,
					NPCID.GoblinThief,
					NPCID.GoblinWarrior,
					NPCID.GoblinArcher,
					NPCID.GoblinSummoner,
				}
			},
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
			{ "Terraria FrostLegion",
				new List<int>() {
					NPCID.MisterStabby,
					NPCID.SnowmanGangsta,
					NPCID.SnowBalla,
				}
			},
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
			{ "Terraria PirateInvasion",
				new List<int>() {
					NPCID.PirateDeckhand,
					NPCID.PirateDeadeye,
					NPCID.PirateCorsair,
					NPCID.PirateCrossbower,
					NPCID.PirateCaptain,
					NPCID.Parrot,
					NPCID.PirateShip,
				}
			},
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
			{ "Terraria MartianMadness",
				new List<int>() {
					NPCID.MartianSaucerCore,
					NPCID.Scutlix,
					NPCID.ScutlixRider,
					NPCID.MartianWalker,
					NPCID.MartianDrone,
					NPCID.MartianTurret,
					NPCID.GigaZapper,
					NPCID.MartianEngineer,
					NPCID.MartianOfficer,
					NPCID.RayGunner,
					NPCID.GrayGrunt,
					NPCID.BrainScrambler
				}
			},
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
			{ "Terraria SkeletronHead", ItemID.SkeletronBossBag },
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

		// Vanilla and Other World music boxes are in order given by the official Terraria wiki
		public readonly static List<int> vanillaMusicBoxTypes = new List<int>() {
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

		public readonly static List<int> otherWorldMusicBoxTypes = new List<int>() {
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

		// Old version compatibility methods
		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Boss, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), bossInfo, null, null));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), bossInfo, null, null));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Event, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), bossInfo, null, null));
		}

		// New system
		internal void AddBoss(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, Func<NPC, string> despawn = null, Action<SpriteBatch, Rectangle, Color> drawing = null, List<string> headTextures = null) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Boss, source?.Name ?? "Unknown", name, id, val, down, available, collect, spawn, info, despawn, drawing, headTextures));
			LogNewBoss(source?.Name ?? "Unknown", Language.GetTextValue(name.StartsWith("?") ? name.Substring(1) : name));
		}

		internal void AddMiniBoss(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, Func<NPC, string> despawn = null, Action<SpriteBatch, Rectangle, Color> drawing = null, List<string> headTextures = null) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, source?.Name ?? "Unknown", name, id, val, down, available, collect, spawn, info, despawn, drawing, headTextures));
			LogNewBoss(source?.Name ?? "Unknown", Language.GetTextValue(name.StartsWith("?") ? name.Substring(1) : name));
		}

		internal void AddEvent(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, Action<SpriteBatch, Rectangle, Color> drawing = null, List<string> headTextures = null) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Event, source?.Name ?? "Unknown", name, id, val, down, available, collect, spawn, info, null, drawing, headTextures));
			LogNewBoss(source?.Name ?? "Unknown", Language.GetTextValue(name.StartsWith("?") ? name.Substring(1) : name));
		}

		internal void EnsureBossIsNotDuplicate(string mod, string bossname) {
			if (SortedBosses.Any(x=> x.Key == $"{mod} {bossname}"))
				throw new Exception($"The boss '{bossname}' from the mod '{mod}' has already been added. Check your code for duplicate entries or typos.");
		}

		internal void LogNewBoss(string mod, string name) {
			if (!BossChecklist.DebugConfig.ModCallLogVerbose)
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
			if (OldCalls.Values.Any(x => x.Contains(name))) {
				BossChecklist.instance.Logger.Info($"[Outdated Mod Call] Boss Log entry added: [{mod}] {name}");
			}
			else {
				BossChecklist.instance.Logger.Info($"Boss Log entry successfully added: [{mod}] {name}");
			}
		}

		internal void AddOrphanData(string type, string bossKey, object values) {
			OrphanType orphanType;
			if (type == "AddToBossLoot") {
				orphanType = OrphanType.Loot;
			}
			else if (type == "AddToBossCollection") {
				orphanType = OrphanType.Collection;
			}
			else if (type == "AddToBossSpawnItems") {
				orphanType = OrphanType.SpawnItem;
			}
			else if (type == "AddToEventNPCs") {
				orphanType = OrphanType.EventNPC;
			}
			else {
				BossChecklist.instance.Logger.Warn($"Invalid orphan data found. ({type} for {bossKey})");
				return;
			}

			ExtraData.Add(new OrphanInfo(orphanType, bossKey, values as List<int>));
		}
	}
}
