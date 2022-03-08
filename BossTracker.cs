using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BossChecklist
{
	internal class BossTracker
	{
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
		public const float EmpressOfLight = 14f;
		public const float DukeFishron = 15f;
		public const float LunaticCultist = 16f;
		public const float Moonlord = 17f;

		/// <summary>
		/// All currently loaded bosses/minibosses/events sorted in progression order.
		/// </summary>
		internal List<BossInfo> SortedBosses;
		internal bool[] BossCache; 
		internal List<OrphanInfo> ExtraData;
		internal bool BossesFinalized = false;
		internal bool AnyModHasOldCall = false;
		internal Dictionary<string, List<string>> OldCalls = new();

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaBosses();
			ExtraData = new List<OrphanInfo>();
		}

		private void InitializeVanillaBosses() {
			SortedBosses = new List<BossInfo> {
				// Bosses -- Vanilla
				BossInfo.MakeVanillaBoss(EntryType.Boss, KingSlime, "$NPCName.KingSlime", new List<int>() { NPCID.KingSlime }, () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.KingSlime}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EyeOfCthulhu, "$NPCName.EyeofCthulhu", new List<int>() { NPCID.EyeofCthulhu }, () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.EaterofWorldsHead", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, () => NPC.downedBoss2, new List<int>() { ItemID.WormFood })
					.WithCustomAvailability(() => !WorldGen.crimson || ModLoader.TryGetMod("BothEvils", out Mod mod))
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.EaterofWorldsHead}"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.BrainofCthulhu", new List<int>() { NPCID.BrainofCthulhu }, () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine })
					.WithCustomAvailability(() => WorldGen.crimson || ModLoader.TryGetMod("BothEvils", out Mod mod)),
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
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem + 0.5f, "$NPCName.DD2Betsy", new List<int>() { NPCID.DD2Betsy }, () => WorldAssist.downedInvasionT3Ours, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand })
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
				BossInfo.MakeVanillaEvent(KingSlime + 0.2f, "The Torch God", () => Main.LocalPlayer.unlockedBiomeTorches, new List<int>() { ItemID.Torch })
					.WithCustomTranslationKey("$NPCName.TorchGod")
					.WithCustomHeadIcon($"Terraria/Images/Item_{ItemID.TorchGodsFavor}"),
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.2f, "Blood Moon", () => WorldAssist.downedBloodMoon, new List<int>() { ItemID.BloodMoonStarter })
					.WithCustomTranslationKey("$Bestiary_Events.BloodMoon")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventBloodMoon")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventBloodMoon_Head"),
				// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.5f, "Goblin Army", () => NPC.downedGoblins, new List<int>() { ItemID.GoblinBattleStandard })
					.WithCustomTranslationKey("$LegacyInterface.88")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventGoblinArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_9"),
				BossInfo.MakeVanillaEvent(EaterOfWorlds + 0.5f, "Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand })
					.WithCustomTranslationKey("$DungeonDefenders2.InvasionProgressTitle")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventOldOnesArmy")
					.WithCustomHeadIcon("Terraria/Images/Extra_79"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, EaterOfWorlds + 0.51f, "$NPCName.DD2DarkMageT3", new List<int>() { NPCID.DD2DarkMageT3 }, () => WorldAssist.downedDarkMage, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2DarkMageT3}"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, SkeletronPrime + 0.5f, "$NPCName.DD2OgreT3", new List<int>() { NPCID.DD2OgreT3 }, () => WorldAssist.downedOgre, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.DD2OgreT3}"),
				BossInfo.MakeVanillaEvent(WallOfFlesh + 0.6f, "Frost Legion", () => NPC.downedFrost, new List<int>() { ItemID.SnowGlobe })
					.WithCustomTranslationKey("$LegacyInterface.87")
					.WithCustomAvailability(() => Main.xMas)
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostLegion")
					.WithCustomHeadIcon("Terraria/Images/Extra_7"),
				BossInfo.MakeVanillaEvent(WallOfFlesh + 0.7f, "Pirate Invasion", () => NPC.downedPirates, new List<int>() { ItemID.PirateMap })
					.WithCustomTranslationKey("$LegacyInterface.86")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPirateInvasion")
					.WithCustomHeadIcon("Terraria/Images/Extra_11"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, WallOfFlesh + 0.71f, "$NPCName.PirateShip", new List<int>() { NPCID.PirateShip }, () => WorldAssist.downedFlyingDutchman, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.PirateShip}"),
				BossInfo.MakeVanillaEvent(SkeletronPrime + 0.2f, "Solar Eclipse", () => WorldAssist.downedSolarEclipse, new List<int>() { ItemID.SolarTablet })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventSolarEclipse")
					.WithCustomHeadIcon($"BossChecklist/Resources/BossTextures/EventSolarEclipse_Head"),
				BossInfo.MakeVanillaEvent(Plantera + 0.1f, "Pumpkin Moon", () => WorldAssist.downedPumpkinMoon, new List<int>() { ItemID.PumpkinMoonMedallion })
					.WithCustomTranslationKey("$LegacyInterface.84")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventPumpkinMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_12"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.11f, "$NPCName.MourningWood", new List<int>() { NPCID.MourningWood }, () => NPC.downedHalloweenTree, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.12f, "$NPCName.Pumpking", new List<int>() { NPCID.Pumpking }, () => NPC.downedHalloweenKing, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/Boss{NPCID.Pumpking}"),
				BossInfo.MakeVanillaEvent(Plantera + 0.13f, "Frost Moon", () => WorldAssist.downedFrostMoon, new List<int>() { ItemID.NaughtyPresent })
					.WithCustomTranslationKey("$LegacyInterface.83")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventFrostMoon")
					.WithCustomHeadIcon($"Terraria/Images/Extra_8"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.14f, "$NPCName.Everscream", new List<int>() { NPCID.Everscream }, () => NPC.downedChristmasTree, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.15f, "$NPCName.SantaNK1", new List<int>() { NPCID.SantaNK1 }, () => NPC.downedChristmasSantank, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.16f, "$NPCName.IceQueen", new List<int>() { NPCID.IceQueen }, () => NPC.downedChristmasIceQueen, new List<int>() { }),
				BossInfo.MakeVanillaEvent(Golem + 0.1f, "Martian Madness", () => NPC.downedMartians, new List<int>() { })
					.WithCustomTranslationKey("$LegacyInterface.85")
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventMartianMadness")
					.WithCustomHeadIcon($"Terraria/Images/Extra_10"),
				BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Golem + 0.11f, "$NPCName.MartianSaucer", new List<int>() { NPCID.MartianSaucer, NPCID.MartianSaucerCore }, () => WorldAssist.downedMartianSaucer, new List<int>() { }),
				BossInfo.MakeVanillaEvent(LunaticCultist + 0.01f, "Lunar Event", () => NPC.downedTowerNebula && NPC.downedTowerVortex && NPC.downedTowerSolar && NPC.downedTowerStardust, new List<int>() { })
					.WithCustomPortrait($"BossChecklist/Resources/BossTextures/EventLunarEvent")
					.WithCustomHeadIcon(new List<string>() {
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerNebula]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerVortex]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerSolar]}",
						$"Terraria/Images/NPC_Head_Boss_{NPCID.Sets.BossHeadTextures[NPCID.LunarTowerStardust]}"}
					),
			};
		}

		internal void FinalizeLocalization() {
			// Modded Localization keys are initialized before AddRecipes, so we need to do this late.
			foreach (var boss in SortedBosses) {
				boss.name = GetTextFromPossibleTranslationKey(boss.name);
				boss.spawnInfo = GetTextFromPossibleTranslationKey(boss.spawnInfo);
			}

			// Local Functions
			string GetTextFromPossibleTranslationKey(string input) => input?.StartsWith("$") == true ? Language.GetTextValue(input.Substring(1)) : input;
		}

		internal void FinalizeOrphanData() {
			foreach (OrphanInfo orphan in ExtraData) {
				BossInfo bossInfo = SortedBosses.Find(boss => boss.Key == orphan.Key);
				if (bossInfo != null && orphan.values != null) {
					switch (orphan.type) {
						/* TODO: Revisit adding loot via Orphan data
						case OrphanType.Loot:
							bossInfo.loot.AddRange(orphan.values);
							break;
						*/
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
							break;
					}
				}
				else if (BossChecklist.DebugConfig.ModCallLogVerbose) {
					if (bossInfo == null) {
						BossChecklist.instance.Logger.Info($"Could not find {orphan.bossName} from {orphan.modSource} to add OrphanInfo to.");
					}
					if (orphan.values == null) {
						BossChecklist.instance.Logger.Info($"Orphan values for {orphan.bossName} from {orphan.modSource} found to be empty.");
					}
				}
			}
		}

		internal void FinalizeCollectionTypes() {
			foreach (BossInfo boss in SortedBosses) {
				foreach (int type in boss.collection) {
					Item temp = new Item(type);
					if (temp.headSlot > 0 && temp.vanity) {
						boss.collectType.Add(type, CollectionType.Mask);
					}
					else if (BossChecklist.vanillaMusicBoxTypes.Contains(type) || BossChecklist.otherWorldMusicBoxTypes.Contains(type) || BossChecklist.itemToMusicReference.ContainsKey(type)) {
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
			BossesFinalized = true;
			if (AnyModHasOldCall) {
				foreach (var oldCall in OldCalls) {
					BossChecklist.instance.Logger.Info($"{oldCall.Key} calls for the following are not utilizing Boss Log features. Mod developers should update mod calls with proper information to improve user experience: {string.Join(", ", oldCall.Value)}");
				}
				OldCalls.Clear();
				BossChecklist.instance.Logger.Info("Updated Mod.Call documentation for BossChecklist: https://github.com/JavidPack/BossChecklist/wiki/Support-using-Mod-Call#modcalls");
			}
			
			if (Main.netMode == NetmodeID.Server) {
				BossChecklist.ServerCollectedRecords = new List<BossStats>[255];
				for (int i = 0; i < 255; i++) {
					BossChecklist.ServerCollectedRecords[i] = new List<BossStats>();
					for (int j = 0; j < BossChecklist.bossTracker.SortedBosses.Count; j++) {
						BossChecklist.ServerCollectedRecords[i].Add(new BossStats());
					}
				}
			}

			BossCache = new bool[NPCLoader.NPCCount];
			foreach (var boss in SortedBosses) {
				boss.npcIDs.ForEach(x => BossCache[x] = true);
			}
		}

		internal protected List<int> SetupCollect(int bossNum) {
			#region Boss Collectibles
			if (bossNum == NPCID.KingSlime) {
				return new List<int>() {
					ItemID.KingSlimeMasterTrophy,
					ItemID.KingSlimePetItem,
					ItemID.KingSlimeTrophy,
					ItemID.KingSlimeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1
				};
			}
			else if (bossNum == NPCID.EyeofCthulhu) {
				return new List<int>() {
					ItemID.EyeofCthulhuMasterTrophy,
					ItemID.EyeOfCthulhuPetItem,
					ItemID.EyeofCthulhuTrophy,
					ItemID.EyeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.AviatorSunglasses,
					ItemID.BadgersHat
				};
			}
			else if (bossNum == NPCID.EaterofWorldsHead) {
				return new List<int>() {
					ItemID.EaterofWorldsMasterTrophy,
					ItemID.EaterOfWorldsPetItem,
					ItemID.EaterofWorldsTrophy,
					ItemID.EaterMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.EatersBone
				};
			}
			else if (bossNum == NPCID.BrainofCthulhu) {
				return new List<int>() {
					ItemID.BrainofCthulhuMasterTrophy,
					ItemID.BrainOfCthulhuPetItem,
					ItemID.BrainofCthulhuTrophy,
					ItemID.BrainMask,
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWBoss1,
					ItemID.BoneRattle
				};
			}
			else if (bossNum == NPCID.QueenBee) {
				return new List<int>() {
					ItemID.QueenBeeMasterTrophy, 
					ItemID.QueenBeePetItem,
					ItemID.QueenBeeTrophy,
					ItemID.BeeMask,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss1,
					ItemID.Nectar,
				};
			}
			else if (bossNum == NPCID.SkeletronHead) {
				return new List<int>() {
					ItemID.SkeletronMasterTrophy,
					ItemID.SkeletronPetItem,
					ItemID.SkeletronTrophy,
					ItemID.SkeletronMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss1,
					ItemID.ChippysCouch
				};
			}
			else if (bossNum == NPCID.Deerclops) {
				return new List<int>() {
					ItemID.DeerclopsMasterTrophy,
					ItemID.DeerclopsPetItem,
					ItemID.DeerclopsTrophy,
					ItemID.DeerclopsMask,
					ItemID.MusicBoxDeerclops,
					ItemID.MusicBoxOWBoss1
				};
			}
			else if (bossNum == NPCID.WallofFlesh) {
				return new List<int>() {
					ItemID.WallofFleshMasterTrophy,
					ItemID.WallOfFleshGoatMountItem,
					ItemID.WallofFleshTrophy,
					ItemID.FleshMask,
					ItemID.MusicBoxBoss2,
					ItemID.MusicBoxOWWallOfFlesh,
					ItemID.BadgersHat
				};
			}
			else if (bossNum == NPCID.QueenSlimeBoss) {
				return new List<int>() {
					ItemID.QueenSlimeMasterTrophy,
					ItemID.QueenSlimePetItem,
					ItemID.QueenSlimeTrophy,
					ItemID.QueenSlimeMask,
					ItemID.MusicBoxQueenSlime,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.Retinazer) {
				return new List<int>() {
					ItemID.TwinsMasterTrophy,
					ItemID.TwinsPetItem,
					ItemID.RetinazerTrophy,
					ItemID.SpazmatismTrophy,
					ItemID.TwinMask,
					ItemID.MusicBoxBoss2,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.TheDestroyer) {
				return new List<int>() {
					ItemID.DestroyerMasterTrophy,
					ItemID.DestroyerPetItem,
					ItemID.DestroyerTrophy,
					ItemID.DestroyerMask,
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.SkeletronPrime) {
				return new List<int>() {
					ItemID.SkeletronPrimeMasterTrophy,
					ItemID.SkeletronPrimePetItem,
					ItemID.SkeletronPrimeTrophy,
					ItemID.SkeletronPrimeMask,
					ItemID.MusicBoxBoss1,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.Plantera) {
				return new List<int>() {
					ItemID.PlanteraMasterTrophy,
					ItemID.PlanteraPetItem,
					ItemID.PlanteraTrophy,
					ItemID.PlanteraMask,
					ItemID.MusicBoxPlantera,
					ItemID.MusicBoxOWPlantera,
					ItemID.Seedling
				};
			}
			else if (bossNum == NPCID.Golem) {
				return new List<int>() {
					ItemID.GolemMasterTrophy,
					ItemID.GolemPetItem,
					ItemID.GolemTrophy,
					ItemID.GolemMask,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.HallowBoss) {
				return new List<int>() {
					ItemID.FairyQueenMasterTrophy,
					ItemID.FairyQueenPetItem,
					ItemID.FairyQueenTrophy,
					ItemID.FairyQueenMask,
					ItemID.MusicBoxEmpressOfLight,
					ItemID.MusicBoxOWBoss2,
					ItemID.HallowBossDye,
					ItemID.RainbowCursor,
				};
			}
			else if (bossNum == NPCID.DD2Betsy) {
				return new List<int>() {
					ItemID.BetsyMasterTrophy,
					ItemID.DD2BetsyPetItem,
					ItemID.BossTrophyBetsy,
					ItemID.BossMaskBetsy,
					ItemID.MusicBoxDD2,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (bossNum == NPCID.DukeFishron) {
				return new List<int>() {
					ItemID.DukeFishronMasterTrophy,
					ItemID.DukeFishronPetItem,
					ItemID.DukeFishronTrophy,
					ItemID.DukeFishronMask,
					ItemID.MusicBoxDukeFishron,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.CultistBoss) {
				return new List<int>() {
					ItemID.LunaticCultistMasterTrophy,
					ItemID.LunaticCultistPetItem,
					ItemID.AncientCultistTrophy,
					ItemID.BossMaskCultist,
					ItemID.MusicBoxBoss5,
					ItemID.MusicBoxOWBoss2
				};
			}
			else if (bossNum == NPCID.MoonLordHead) {
				return new List<int>() {
					ItemID.MoonLordMasterTrophy,
					ItemID.MoonLordPetItem,
					ItemID.MoonLordTrophy,
					ItemID.BossMaskMoonlord,
					ItemID.MusicBoxLunarBoss,
					ItemID.MusicBoxOWMoonLord
				};
			}
			#endregion
			#region Mini-boss Collectibles
			else if (bossNum == NPCID.DD2DarkMageT3) {
				return new List<int>() {
					ItemID.DarkMageMasterTrophy,
					ItemID.DarkMageBookMountItem,
					ItemID.BossTrophyDarkmage,
					ItemID.BossMaskDarkMage,
					ItemID.MusicBoxDD2,
					ItemID.DD2PetDragon,
					ItemID.DD2PetGato
				};
			}
			else if (bossNum == NPCID.PirateShip) {
				return new List<int>() {
					ItemID.FlyingDutchmanMasterTrophy,
					ItemID.PirateShipMountItem,
					ItemID.FlyingDutchmanTrophy,
					ItemID.MusicBoxPirates
				};
			}
			else if (bossNum == NPCID.DD2OgreT3) {
				return new List<int>() {
					ItemID.OgreMasterTrophy,
					ItemID.DD2OgrePetItem,
					ItemID.BossTrophyOgre,
					ItemID.BossMaskOgre,
					ItemID.MusicBoxDD2,
					ItemID.DD2PetGhost
				};
			}
			else if (bossNum == NPCID.MourningWood) {
				return new List<int>() {
					ItemID.MourningWoodMasterTrophy,
					ItemID.SpookyWoodMountItem,
					ItemID.MourningWoodTrophy,
					ItemID.MusicBoxPumpkinMoon,
					ItemID.CursedSapling
				};
			}
			else if (bossNum == NPCID.Pumpking) {
				return new List<int>() {
					ItemID.PumpkingMasterTrophy,
					ItemID.PumpkingPetItem,
					ItemID.PumpkingTrophy,
					ItemID.MusicBoxPumpkinMoon,
					ItemID.SpiderEgg
				};
			}
			else if (bossNum == NPCID.Everscream) {
				return new List<int>() {
					ItemID.EverscreamMasterTrophy,
					ItemID.EverscreamPetItem,
					ItemID.EverscreamTrophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			else if (bossNum == NPCID.SantaNK1) {
				return new List<int>() {
					ItemID.SantankMasterTrophy,
					ItemID.SantankMountItem,
					ItemID.SantaNK1Trophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			else if (bossNum == NPCID.IceQueen) {
				return new List<int>() {
					ItemID.IceQueenMasterTrophy,
					ItemID.IceQueenPetItem,
					ItemID.IceQueenTrophy,
					ItemID.MusicBoxFrostMoon,
					ItemID.BabyGrinchMischiefWhistle
				};
			}
			else if (bossNum == NPCID.MartianSaucer) {
				return new List<int>() {
					ItemID.UFOMasterTrophy,
					ItemID.MartianPetItem,
					ItemID.MartianSaucerTrophy,
					ItemID.MusicBoxMartians
				};
			}
			#endregion
			return new List<int>();
		}

		internal List<int> SetupEventNPCList(string eventName) {
			#region Event NPC List
			if (eventName == "The Torch God") {
				return new List<int>() {
					NPCID.TorchGod,
				};
			}
			else if (eventName == "Blood Moon") {
				return new List<int>() {
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
				};
			}
			else if (eventName == "Goblin Army") {
				return new List<int>() {
					NPCID.GoblinScout,
					NPCID.GoblinPeon,
					NPCID.GoblinSorcerer,
					NPCID.GoblinThief,
					NPCID.GoblinWarrior,
					NPCID.GoblinArcher,
					NPCID.GoblinSummoner,
				};
			}
			else if (eventName == "Old One's Army") {
				return new List<int>() {
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
				};
			}
			else if (eventName == "Frost Legion") {
				return new List<int>() {
					NPCID.MisterStabby,
					NPCID.SnowmanGangsta,
					NPCID.SnowBalla,
				};
			}
			else if (eventName == "Solar Eclipse") {
				return new List<int>() {
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
				};
			}
			else if (eventName == "Pirate Invasion") {
				return new List<int>() {
					NPCID.PirateDeckhand,
					NPCID.PirateDeadeye,
					NPCID.PirateCorsair,
					NPCID.PirateCrossbower,
					NPCID.PirateCaptain,
					NPCID.Parrot,
					NPCID.PirateShip,
				};
			}
			else if (eventName == "Pumpkin Moon") {
				return new List<int>() {
					NPCID.Scarecrow1,
					NPCID.Splinterling,
					NPCID.Hellhound,
					NPCID.Poltergeist,
					NPCID.HeadlessHorseman,
					NPCID.MourningWood,
					NPCID.Pumpking,
				};
			}
			else if (eventName == "Frost Moon") {
				return new List<int>() {
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
				};
			}
			else if (eventName == "Martian Madness") {
				return new List<int>() {
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
				};
			}
			else if (eventName == "Lunar Event") {
				return new List<int>() {
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
				};
			}
			#endregion
			return new List<int>();
		}

		internal List<int> SetupEventCollectibles(string eventName) {
			#region Event Collectibles
			if (eventName == "The Torch God") {
				return new List<int>() {
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWWallOfFlesh
				};
			}
			else if (eventName == "Blood Moon") {
				return new List<int>() {
					ItemID.MusicBoxEerie,
					ItemID.MusicBoxOWBloodMoon
				};
			}
			else if (eventName == "Goblin Army") {
				return new List<int>() {
					ItemID.MusicBoxGoblins,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Old One's Army") {
				return new List<int>() {
					ItemID.MusicBoxDD2,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Frost Legion") {
				return new List<int>() {
					ItemID.MusicBoxBoss3,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Solar Eclipse") {
				return new List<int>() {
					ItemID.MusicBoxEclipse,
					ItemID.MusicBoxOWBloodMoon
				};
			}
			else if (eventName == "Pirate Invasion") {
				return new List<int>() {
					ItemID.MusicBoxPirates,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Pumpkin Moon") {
				return new List<int>() {
					ItemID.MusicBoxPumpkinMoon,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Frost Moon") {
				return new List<int>() {
					ItemID.MusicBoxFrostMoon,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Martian Madness") {
				return new List<int>() {
					ItemID.MusicBoxMartians,
					ItemID.MusicBoxOWInvasion
				};
			}
			else if (eventName == "Lunar Event") {
				return new List<int>() {
					ItemID.MusicBoxTowers,
					ItemID.MusicBoxOWTowers
				};
			}
			#endregion
			return new List<int>();
		}

		internal readonly static Dictionary<int, int> vanillaBossBags = new Dictionary<int, int>() {
			{ NPCID.KingSlime, ItemID.KingSlimeBossBag },
			{ NPCID.EyeofCthulhu, ItemID.EyeOfCthulhuBossBag },
			{ NPCID.EaterofWorldsHead, ItemID.EaterOfWorldsBossBag },
			{ NPCID.BrainofCthulhu, ItemID.BrainOfCthulhuBossBag },
			{ NPCID.QueenBee, ItemID.QueenBeeBossBag },
			{ NPCID.SkeletronHead, ItemID.SkeletronBossBag },
			{ NPCID.WallofFlesh, ItemID.WallOfFleshBossBag },
			{ NPCID.Retinazer, ItemID.TwinsBossBag },
			{ NPCID.TheDestroyer, ItemID.DestroyerBossBag },
			{ NPCID.SkeletronPrime, ItemID.SkeletronPrimeBossBag },
			{ NPCID.Plantera, ItemID.PlanteraBossBag },
			{ NPCID.Golem, ItemID.GolemBossBag },
			{ NPCID.DukeFishron, ItemID.FishronBossBag },
			{ NPCID.MoonLordHead, ItemID.MoonLordBossBag },
			{ NPCID.DD2Betsy, ItemID.BossBagBetsy },
			{ NPCID.QueenSlimeBoss, ItemID.QueenSlimeBossBag },
			{ NPCID.HallowBoss, ItemID.FairyQueenBossBag },
			{ NPCID.Deerclops, ItemID.DeerclopsBossBag }
		};

		// Old version compatibility methods
		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Boss, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Event, "Unknown", bossname, new List<int>(), bossValue, bossDowned, available, new List<int>(), new List<int>(), null, bossInfo));
		}

		// New system is better
		internal void AddBoss(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, string despawnMessage) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Boss, source?.Name ?? "Unknown", name, id, val, down, available, spawn, collect, info, despawnMessage));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddMiniBoss(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, string despawnMessage) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, source?.Name ?? "Unknown", name, id, val, down, available, spawn, collect, info, despawnMessage));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddEvent(Mod source, string name, List<int> id, float val, Func<bool> down, Func<bool> available, List<int> collect, List<int> spawn, string info, string despawnMessage) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Event, source?.Name ?? "Unknown", name, id, val, down, available, spawn, collect, info, despawnMessage));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void EnsureBossIsNotDuplicate(string mod, string bossname) {
			if (SortedBosses.Any(x=> x.Key == $"{mod} {bossname}"))
				throw new Exception($"The boss '{bossname}' from the mod '{mod}' has already been added. Check your code for duplicate entries or typos.");
		}

		internal void LogNewBoss(string mod, string name) {
			if (!BossChecklist.DebugConfig.ModCallLogVerbose)
				return;
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("<<Boss Checklist>> ");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write(mod + " has added ");
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.Write(name);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write(" to the boss log!");
			Console.WriteLine();
			Console.ResetColor();
			BossChecklist.instance.Logger.Info($"{name} has been added to the Boss Log!");
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
