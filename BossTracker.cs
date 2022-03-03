using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

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
				BossInfo.MakeVanillaBoss(EntryType.Boss, KingSlime, "$NPCName.KingSlime", new List<int>() { NPCID.KingSlime }, () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EyeOfCthulhu, "$NPCName.EyeofCthulhu", new List<int>() { NPCID.EyeofCthulhu }, () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.EaterofWorldsHead", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, () => NPC.downedBoss2, new List<int>() { ItemID.WormFood }).WithCustomAvailability(() => !WorldGen.crimson || ModLoader.TryGetMod("BothEvils", out Mod mod)),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.BrainofCthulhu", new List<int>() { NPCID.BrainofCthulhu }, () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine }).WithCustomAvailability(() => WorldGen.crimson || ModLoader.TryGetMod("BothEvils", out Mod mod)),
				BossInfo.MakeVanillaBoss(EntryType.Boss, QueenBee, "$NPCName.QueenBee", new List<int>() { NPCID.QueenBee }, () => NPC.downedQueenBee, new List<int>() { ItemID.Abeemination }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Skeletron, "$NPCName.SkeletronHead", new List<int>() { NPCID.SkeletronHead }, () => NPC.downedBoss3, new List<int>() { ItemID.ClothierVoodooDoll }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, DeerClops, "$NPCName.Deerclops", new List<int>() { NPCID.Deerclops }, () => NPC.downedDeerclops, new List<int>() { ItemID.DeerThing }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, WallOfFlesh, "$NPCName.WallofFlesh", new List<int>() { NPCID.WallofFlesh }, () => Main.hardMode, new List<int>() { ItemID.GuideVoodooDoll }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, QueenSlime, "$NPCName.QueenSlimeBoss", new List<int>() { NPCID.QueenSlimeBoss }, () => NPC.downedQueenSlime, new List<int>() { ItemID.QueenSlimeCrystal }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheTwins, "$Enemies.TheTwins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, () => NPC.downedMechBoss2, new List<int>() { ItemID.MechanicalEye }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheDestroyer, "$NPCName.TheDestroyer", new List<int>() { NPCID.TheDestroyer }, () => NPC.downedMechBoss1, new List<int>() { ItemID.MechanicalWorm }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, SkeletronPrime, "$NPCName.SkeletronPrime", new List<int>() { NPCID.SkeletronPrime }, () => NPC.downedMechBoss3, new List<int>() { ItemID.MechanicalSkull }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Plantera, "$NPCName.Plantera", new List<int>() { NPCID.Plantera }, () => NPC.downedPlantBoss, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem, "$NPCName.Golem", new List<int>() { NPCID.Golem, NPCID.GolemHead }, () => NPC.downedGolemBoss, new List<int>() { ItemID.LihzahrdPowerCell, ItemID.LihzahrdAltar }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem + 0.5f, "$NPCName.DD2Betsy", new List<int>() { NPCID.DD2Betsy }, () => WorldAssist.downedInvasionT3Ours, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand }), // No despawn message due to being in an event
				BossInfo.MakeVanillaBoss(EntryType.Boss, EmpressOfLight, "$NPCName.HallowBoss", new List<int>() { NPCID.HallowBoss }, () => NPC.downedEmpressOfLight, new List<int>() { ItemID.EmpressButterfly }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, DukeFishron, "$NPCName.DukeFishron", new List<int>() { NPCID.DukeFishron }, () => NPC.downedFishron, new List<int>() { ItemID.TruffleWorm }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, LunaticCultist, "$NPCName.CultistBoss", new List<int>() { NPCID.CultistBoss }, () => NPC.downedAncientCultist, new List<int>() { }),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Moonlord, "$Enemies.MoonLord", new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand }, () => NPC.downedMoonlord, new List<int>() { ItemID.CelestialSigil }),
				
				// Minibosses and Events -- Vanilla
				BossInfo.MakeVanillaEvent(KingSlime + 0.2f, "The Torch God", () => Main.LocalPlayer.unlockedBiomeTorches, new List<int>() { ItemID.Torch }).WithCustomTranslationKey("$NPCName.TorchGod"),
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.2f, "Blood Moon", () => WorldAssist.downedBloodMoon, new List<int>() { ItemID.BloodMoonStarter }),
					// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.5f, "Goblin Army", () => NPC.downedGoblins, new List<int>() { ItemID.GoblinBattleStandard }).WithCustomTranslationKey("$LegacyInterface.88"),
				BossInfo.MakeVanillaEvent(EaterOfWorlds + 0.5f, "Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand }).WithCustomTranslationKey("$DungeonDefenders2.InvasionProgressTitle"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, EaterOfWorlds + 0.51f, "$NPCName.DD2DarkMageT3", new List<int>() { NPCID.DD2DarkMageT3 }, () => WorldAssist.downedDarkMage, new List<int>() { }),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, SkeletronPrime + 0.5f, "$NPCName.DD2OgreT3", new List<int>() { NPCID.DD2OgreT3 }, () => WorldAssist.downedOgre, new List<int>() { }),
				BossInfo.MakeVanillaEvent(WallOfFlesh + 0.6f, "Frost Legion", () => NPC.downedFrost, new List<int>() { ItemID.SnowGlobe }).WithCustomTranslationKey("$LegacyInterface.87").WithCustomAvailability(() => Main.xMas),
				BossInfo.MakeVanillaEvent(WallOfFlesh + 0.7f, "Pirate Invasion", () => NPC.downedPirates, new List<int>() { ItemID.PirateMap }).WithCustomTranslationKey("$LegacyInterface.86"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, WallOfFlesh + 0.71f, "$NPCName.PirateShip", new List<int>() { NPCID.PirateShip }, () => WorldAssist.downedFlyingDutchman, new List<int>() { }),
				BossInfo.MakeVanillaEvent(SkeletronPrime + 0.2f, "Solar Eclipse", () => WorldAssist.downedSolarEclipse, new List<int>() { ItemID.SolarTablet }),
					// BossInfo for Mothron?? If so, a boss head icon needs to be created
				BossInfo.MakeVanillaEvent(Plantera + 0.1f, "Pumpkin Moon", () => WorldAssist.downedPumpkinMoon, new List<int>() { ItemID.PumpkinMoonMedallion }).WithCustomTranslationKey("$LegacyInterface.84"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.11f, "$NPCName.MourningWood", new List<int>() { NPCID.MourningWood }, () => NPC.downedHalloweenTree, new List<int>() { }),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.12f, "$NPCName.Pumpking", new List<int>() { NPCID.Pumpking }, () => NPC.downedHalloweenKing, new List<int>() { }),
				BossInfo.MakeVanillaEvent(Plantera + 0.13f, "Frost Moon", () => WorldAssist.downedFrostMoon, new List<int>() { ItemID.NaughtyPresent }).WithCustomTranslationKey("$LegacyInterface.83"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.14f, "$NPCName.Everscream", new List<int>() { NPCID.Everscream }, () => NPC.downedChristmasTree, new List<int>() { }),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.15f, "$NPCName.SantaNK1", new List<int>() { NPCID.SantaNK1 }, () => NPC.downedChristmasSantank, new List<int>() { }),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Plantera + 0.16f, "$NPCName.IceQueen", new List<int>() { NPCID.IceQueen }, () => NPC.downedChristmasIceQueen, new List<int>() { }),
				BossInfo.MakeVanillaEvent(Golem + 0.1f, "Martian Madness", () => NPC.downedMartians, new List<int>() { }).WithCustomTranslationKey("$LegacyInterface.85"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, Golem + 0.11f, "$NPCName.MartianSaucer", new List<int>() { NPCID.MartianSaucer, NPCID.MartianSaucerCore }, () => WorldAssist.downedMartianSaucer, new List<int>() { }),
				BossInfo.MakeVanillaEvent(LunaticCultist + 0.01f, "Lunar Event", () => NPC.downedTowerNebula && NPC.downedTowerVortex && NPC.downedTowerSolar && NPC.downedTowerStardust, new List<int>() { }),
			};
		}

		internal void FinalizeLocalization() {
			// Modded Localization keys are initialized before AddRecipes, so we need to do this late.
			foreach (var boss in SortedBosses) {
				boss.name = GetTextFromPossibleTranslationKey(boss.name);
				boss.info = GetTextFromPossibleTranslationKey(boss.info);
			}

			// Local Functions
			string GetTextFromPossibleTranslationKey(string input) => input?.StartsWith("$") == true ? Language.GetTextValue(input.Substring(1)) : input;
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

			// Adding modded treasure bags preemptively to remove Mod Item checks in other parts of code
			BossCache = new bool[NPCLoader.NPCCount];
			foreach (var boss in SortedBosses) {
				if (!Main.dedServ) {
					//TODO: commented out until boss bag tag reintroduced in alpha
					/*
					foreach (int npc in boss.npcIDs) {
						if (npc < NPCID.Count) {
							// If the id is vanilla continue in case the boss uses a servant or something
							// The first NPC should have the boss that drops the bag, but backups are needed anyways
							continue; 
						}
						int bagType = NPCLoader.GetNPC(npc).BossBag;
						if (bagType > 0) {
							if (!BossChecklist.registeredBossBagTypes.Contains(bagType)) {
								BossChecklist.registeredBossBagTypes.Add(bagType);
								break; // We found the boss bag for this modded boss, skip the other NPC IDs and go to the next boss
							}
						}
					}
					*/
				}
				boss.npcIDs.ForEach(x => BossCache[x] = true);
			}
			// This code should do the same as above, since ModNPC.BossBag no longer exists.
			foreach (KeyValuePair<int, Item> item in ContentSamples.ItemsByType) {
				if (item.Key < ItemID.Count)
					continue;
				if (item.Value.ModItem is ModItem modItem) {
					if (modItem.BossBagNPC > 0) {
						if (!BossChecklist.registeredBossBagTypes.Contains(item.Key)) {
							BossChecklist.registeredBossBagTypes.Add(item.Key);
						}
					}
				}
			}
		}

		internal protected List<int> SetupLoot(int bossNum) {
			#region Boss Loot
			if (bossNum == NPCID.KingSlime) {
				return new List<int>() {
					ItemID.KingSlimeBossBag,
					ItemID.KingSlimeMasterTrophy,
					ItemID.KingSlimePetItem,
					ItemID.RoyalGel,
					ItemID.KingSlimeMask,
					ItemID.KingSlimeTrophy,

					ItemID.SlimySaddle,
					ItemID.NinjaHood,
					ItemID.NinjaShirt,
					ItemID.NinjaPants,
					ItemID.SlimeGun,
					ItemID.SlimeHook,
					ItemID.Solidifier,
					ItemID.LesserHealingPotion
				};
			}
			else if (bossNum == NPCID.EyeofCthulhu) {
				return new List<int>() {
					ItemID.EyeOfCthulhuBossBag,
					ItemID.EyeofCthulhuMasterTrophy,
					ItemID.EyeOfCthulhuPetItem,
					ItemID.EoCShield,
					ItemID.EyeofCthulhuTrophy,
					ItemID.EyeMask,

					ItemID.BadgersHat,
					ItemID.Binoculars,
					ItemID.DemoniteOre,
					ItemID.CrimtaneOre,
					ItemID.UnholyArrow,
					ItemID.CorruptSeeds,
					ItemID.CrimsonSeeds,
					ItemID.AviatorSunglasses,
					ItemID.LesserHealingPotion
				};
			}
			else if (bossNum == NPCID.EaterofWorldsHead) {
				return new List<int>() {
					ItemID.EaterOfWorldsBossBag,
					ItemID.EaterofWorldsMasterTrophy,
					ItemID.EaterOfWorldsPetItem,
					ItemID.WormScarf,
					ItemID.EaterofWorldsTrophy,
					ItemID.EaterMask,

					ItemID.DemoniteOre,
					ItemID.ShadowScale,
					ItemID.EatersBone,
					ItemID.LesserHealingPotion
				};
			}
			else if (bossNum == NPCID.BrainofCthulhu) {
				return new List<int>() {
					ItemID.BrainOfCthulhuBossBag,
					ItemID.BrainofCthulhuMasterTrophy,
					ItemID.BrainOfCthulhuPetItem,
					ItemID.BrainOfConfusion,
					ItemID.BrainofCthulhuTrophy,
					ItemID.BrainMask,

					ItemID.CrimtaneOre,
					ItemID.TissueSample,
					ItemID.BoneRattle,
					ItemID.LesserHealingPotion
				};
			}
			else if (bossNum == NPCID.QueenBee) {
				return new List<int>() {
					ItemID.QueenBeeBossBag,
					ItemID.QueenBeeMasterTrophy,
					ItemID.QueenBeePetItem,
					ItemID.HiveBackpack,
					ItemID.QueenBeeTrophy,
					ItemID.BeeMask,

					ItemID.BeeGun,
					ItemID.BeeKeeper,
					ItemID.BeesKnees,
					ItemID.HoneyComb,
					ItemID.Nectar,
					ItemID.HoneyedGoggles,
					ItemID.HiveWand,
					ItemID.BeeHat,
					ItemID.BeeShirt,
					ItemID.BeePants,
					ItemID.Beenade,
					ItemID.BeeWax,
					ItemID.BottledHoney
				};
			}
			else if (bossNum == NPCID.SkeletronHead) {
				return new List<int>() {
					ItemID.SkeletronBossBag,
					ItemID.SkeletronMasterTrophy,
					ItemID.SkeletronPetItem,
					ItemID.BoneGlove,
					ItemID.SkeletronTrophy,
					ItemID.SkeletronMask,

					ItemID.ChippysCouch,
					ItemID.SkeletronHand,
					ItemID.BookofSkulls,
					ItemID.LesserHealingPotion
				};
			}
			else if (bossNum == NPCID.Deerclops) {
				return new List<int>() {
					ItemID.DeerclopsBossBag,
					ItemID.DeerclopsMasterTrophy,
					ItemID.DeerclopsPetItem,
					ItemID.BoneHelm,
					ItemID.DeerclopsTrophy,
					ItemID.DeerclopsMask,

					ItemID.ChesterPetItem,
					ItemID.Eyebrella,
					ItemID.DontStarveShaderItem,
					ItemID.PewMaticHorn,
					ItemID.WeatherPain,
					ItemID.HoundiusShootius,
					ItemID.LucyTheAxe
				};
			}
			else if (bossNum == NPCID.WallofFlesh) {
				return new List<int>() {
					ItemID.WallOfFleshBossBag,
					ItemID.WallofFleshMasterTrophy,
					ItemID.WallOfFleshGoatMountItem,
					ItemID.DemonHeart,
					ItemID.WallofFleshTrophy,
					ItemID.FleshMask,

					ItemID.BadgersHat,
					ItemID.Pwnhammer,
					ItemID.WarriorEmblem,
					ItemID.SorcererEmblem,
					ItemID.RangerEmblem,
					ItemID.SummonerEmblem,
					ItemID.BreakerBlade,
					ItemID.ClockworkAssaultRifle,
					ItemID.LaserRifle,
					ItemID.FireWhip,
					ItemID.HealingPotion
				};
			}
			else if (bossNum == NPCID.QueenSlimeBoss) {
				return new List<int>() {
					ItemID.QueenSlimeBossBag,
					ItemID.QueenSlimeMasterTrophy,
					ItemID.QueenSlimePetItem,
					ItemID.VolatileGelatin,
					ItemID.QueenSlimeTrophy,
					ItemID.QueenSlimeMask,

					ItemID.Smolstar,
					ItemID.QueenSlimeMountSaddle,
					ItemID.CrystalNinjaHelmet,
					ItemID.CrystalNinjaChestplate,
					ItemID.CrystalNinjaLeggings,
					ItemID.QueenSlimeHook,
					ItemID.GelBalloon,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.Retinazer) {
				return new List<int>() {
					ItemID.TwinsBossBag,
					ItemID.TwinsMasterTrophy,
					ItemID.TwinsPetItem,
					ItemID.MechanicalWheelPiece,
					ItemID.RetinazerTrophy,
					ItemID.SpazmatismTrophy,
					ItemID.TwinMask,

					ItemID.SoulofSight,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.TheDestroyer) {
				return new List<int>() {
					ItemID.DestroyerBossBag,
					ItemID.DestroyerMasterTrophy,
					ItemID.DestroyerPetItem,
					ItemID.MechanicalWagonPiece,
					ItemID.DestroyerTrophy,
					ItemID.DestroyerMask,

					ItemID.SoulofMight,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.SkeletronPrime) {
				return new List<int>() {
					ItemID.SkeletronPrimeBossBag,
					ItemID.SkeletronPrimeMasterTrophy,
					ItemID.SkeletronPrimePetItem,
					ItemID.MechanicalBatteryPiece,
					ItemID.SkeletronPrimeTrophy,
					ItemID.SkeletronPrimeMask,

					ItemID.SoulofFright,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.Plantera) {
				return new List<int>() {
					ItemID.PlanteraBossBag,
					ItemID.PlanteraMasterTrophy,
					ItemID.PlanteraPetItem,
					ItemID.SporeSac,
					ItemID.PlanteraTrophy,
					ItemID.PlanteraMask,

					ItemID.TempleKey,
					ItemID.GrenadeLauncher,
					ItemID.VenusMagnum,
					ItemID.NettleBurst,
					ItemID.LeafBlower,
					ItemID.FlowerPow,
					ItemID.WaspGun,
					ItemID.Seedler,
					ItemID.Seedling,
					ItemID.TheAxe,
					ItemID.PygmyStaff,
					ItemID.ThornHook,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.Golem) {
				return new List<int>() {
					ItemID.GolemBossBag,
					ItemID.GolemMasterTrophy,
					ItemID.GolemPetItem,
					ItemID.ShinyStone,
					ItemID.GolemTrophy,
					ItemID.GolemMask,

					ItemID.Picksaw,
					ItemID.Stynger,
					ItemID.StyngerBolt,
					ItemID.PossessedHatchet,
					ItemID.SunStone,
					ItemID.EyeoftheGolem,
					ItemID.HeatRay,
					ItemID.StaffofEarth,
					ItemID.GolemFist,
					ItemID.BeetleHusk,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.HallowBoss) {
				return new List<int>() {
					ItemID.FairyQueenBossBag,
					ItemID.FairyQueenMasterTrophy,
					ItemID.FairyQueenPetItem,
					ItemID.EmpressFlightBooster,
					ItemID.FairyQueenTrophy,
					ItemID.FairyQueenMask,

					ItemID.FairyQueenMagicItem,
					ItemID.PiercingStarlight,
					ItemID.RainbowWhip,
					ItemID.FairyQueenRangedItem,
					ItemID.RainbowWings,
					ItemID.SparkleGuitar,
					ItemID.RainbowCursor,
					ItemID.HallowBossDye,
					ItemID.EmpressBlade,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.DD2Betsy) {
				return new List<int>() {
					ItemID.BossBagBetsy,
					ItemID.BetsyMasterTrophy,
					ItemID.DD2BetsyPetItem,
					ItemID.BossTrophyBetsy,
					ItemID.BossMaskBetsy,

					ItemID.BetsyWings,
					ItemID.DD2BetsyBow, // Aerial Bane
					ItemID.MonkStaffT3, // Sky Dragon's Fury
					ItemID.ApprenticeStaffT3, // Betsy's Wrath
					ItemID.DD2SquireBetsySword, // Flying Dragon
					ItemID.DefenderMedal
				};
			}
			else if (bossNum == NPCID.DukeFishron) {
				return new List<int>() {
					ItemID.FishronBossBag,
					ItemID.DukeFishronMasterTrophy,
					ItemID.DukeFishronPetItem,
					ItemID.ShrimpyTruffle,
					ItemID.DukeFishronTrophy,
					ItemID.DukeFishronMask,

					ItemID.BubbleGun,
					ItemID.Flairon,
					ItemID.RazorbladeTyphoon,
					ItemID.TempestStaff,
					ItemID.Tsunami,
					ItemID.FishronWings,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.CultistBoss) {
				return new List<int>() {
					ItemID.CultistBossBag,
					ItemID.LunaticCultistMasterTrophy,
					ItemID.LunaticCultistPetItem,
					ItemID.AncientCultistTrophy,
					ItemID.BossMaskCultist,

					ItemID.LunarCraftingStation,
					ItemID.GreaterHealingPotion
				};
			}
			else if (bossNum == NPCID.MoonLordHead) {
				return new List<int>() {
					ItemID.MoonLordBossBag,
					ItemID.MoonLordMasterTrophy,
					ItemID.MoonLordPetItem,
					ItemID.GravityGlobe,
					ItemID.MoonLordTrophy,
					ItemID.BossMaskMoonlord,

					ItemID.SuspiciousLookingTentacle,
					ItemID.LongRainbowTrailWings,
					ItemID.Meowmere,
					ItemID.Terrarian,
					ItemID.StarWrath,
					ItemID.SDMG,
					ItemID.LastPrism,
					ItemID.LunarFlareBook,
					ItemID.RainbowCrystalStaff,
					ItemID.MoonlordTurretStaff, // Lunar Portal Staff
					ItemID.Celeb2,
					ItemID.PortalGun,
					ItemID.LunarOre,
					ItemID.MeowmereMinecart,
					ItemID.SuperHealingPotion
				};
			}
			#endregion
			#region Mini-boss Loot
			// MiniBosses
			else if (bossNum == NPCID.DD2DarkMageT3) {
				return new List<int>() {
					ItemID.DarkMageMasterTrophy,
					ItemID.DarkMageBookMountItem,
					ItemID.BossTrophyDarkmage,
					ItemID.BossMaskDarkMage,

					ItemID.WarTable,
					ItemID.WarTableBanner,
					ItemID.DD2PetDragon,
					ItemID.DD2PetGato,
				};
			}
			else if (bossNum == NPCID.PirateShip) {
				return new List<int>() {
					ItemID.FlyingDutchmanMasterTrophy,
					ItemID.PirateShipMountItem,
					ItemID.FlyingDutchmanTrophy,

					ItemID.CoinGun,
					ItemID.LuckyCoin,
					ItemID.DiscountCard,
					ItemID.PirateStaff,
					ItemID.GoldRing,
					ItemID.PirateMinecart,
					ItemID.Cutlass,
				};
			}
			else if (bossNum == NPCID.DD2OgreT3) {
				return new List<int>() {
					ItemID.OgreMasterTrophy,
					ItemID.DD2OgrePetItem,
					ItemID.BossTrophyOgre,
					ItemID.BossMaskOgre,

					ItemID.ApprenticeScarf,
					ItemID.SquireShield,
					ItemID.HuntressBuckler,
					ItemID.MonkBelt,
					ItemID.BookStaff,
					ItemID.DD2PhoenixBow,
					ItemID.DD2SquireDemonSword,
					ItemID.MonkStaffT1,
					ItemID.MonkStaffT2,
					ItemID.DD2PetGhost,
				};
			}
			else if (bossNum == NPCID.MourningWood) {
				return new List<int>() {
					ItemID.MourningWoodMasterTrophy,
					ItemID.SpookyWoodMountItem,
					ItemID.WitchBroom,
					ItemID.MourningWoodTrophy,

					ItemID.SpookyWood,
					ItemID.SpookyHook,
					ItemID.SpookyTwig,
					ItemID.StakeLauncher,
					ItemID.Stake,
					ItemID.CursedSapling,
					ItemID.NecromanticScroll,
				};
			}
			else if (bossNum == NPCID.Pumpking) {
				return new List<int>() {
					ItemID.PumpkingMasterTrophy,
					ItemID.PumpkingPetItem,
					ItemID.PumpkingTrophy,

					ItemID.TheHorsemansBlade,
					ItemID.BatScepter,
					ItemID.BlackFairyDust,
					ItemID.SpiderEgg,
					ItemID.RavenStaff,
					ItemID.CandyCornRifle,
					ItemID.CandyCorn,
					ItemID.JackOLanternLauncher,
					ItemID.ExplosiveJackOLantern,
					ItemID.ScytheWhip
				};
			}
			else if (bossNum == NPCID.Everscream) {
				return new List<int>() {
					ItemID.EverscreamMasterTrophy,
					ItemID.EverscreamPetItem,
					ItemID.EverscreamTrophy,

					ItemID.ChristmasTreeSword,
					ItemID.ChristmasHook,
					ItemID.Razorpine,
					ItemID.FestiveWings,
				};
			}
			else if (bossNum == NPCID.SantaNK1) {
				return new List<int>() {
					ItemID.SantankMasterTrophy,
					ItemID.SantankMountItem,
					ItemID.SantaNK1Trophy,

					ItemID.EldMelter,
					ItemID.ChainGun,
				};
			}
			else if (bossNum == NPCID.IceQueen) {
				return new List<int>() {
					ItemID.IceQueenMasterTrophy,
					ItemID.IceQueenPetItem,
					ItemID.IceQueenTrophy,

					ItemID.BlizzardStaff,
					ItemID.SnowmanCannon,
					ItemID.NorthPole,
					ItemID.BabyGrinchMischiefWhistle,
					ItemID.ReindeerBells,
				};
			}
			else if (bossNum == NPCID.MartianSaucer) {
				return new List<int>() {
					ItemID.UFOMasterTrophy,
					ItemID.MartianPetItem,
					ItemID.MartianSaucerTrophy,

					ItemID.Xenopopper,
					ItemID.XenoStaff,
					ItemID.LaserMachinegun,
					ItemID.ElectrosphereLauncher,
					ItemID.InfluxWaver,
					ItemID.CosmicCarKey,
					ItemID.AntiGravityHook,
					ItemID.LaserDrill,
					ItemID.ChargedBlasterCannon,
					ItemID.GreaterHealingPotion,
				};
			}
			#endregion
			return new List<int>();
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
			return new List<int>();
		}

		internal List<int> SetupEventLoot(string eventName) {
			if (eventName == "The Torch God") {
				return new List<int>() {
					ItemID.TorchGodsFavor,
				};
			}
			else if (eventName == "Blood Moon") {
				return new List<int>() {
					ItemID.BunnyHood,
					ItemID.TopHat,
					ItemID.TheBrideHat,
					ItemID.TheBrideDress,
					ItemID.MoneyTrough,
					ItemID.SharkToothNecklace,
					ItemID.Bananarang,
					ItemID.BloodMoonStarter,
					ItemID.ChumBucket,
					ItemID.KiteBunnyCorrupt,
					ItemID.KiteBunnyCrimson,
					ItemID.KOCannon,
					ItemID.SanguineStaff,
					ItemID.BloodMoonMonolith,
					ItemID.DripplerFlail,
					ItemID.SharpTears,
					ItemID.BloodHamaxe,
					ItemID.BloodRainBow,
					ItemID.VampireFrogStaff,
					ItemID.BloodFishingRod,
					ItemID.CombatBook,
					ItemID.PedguinHat,
					ItemID.PedguinShirt,
					ItemID.PedguinPants,
				};
			}
			else if (eventName == "Goblin Army") {
				return new List<int>() {
					ItemID.Harpoon,
					ItemID.SpikyBall,
					ItemID.ShadowFlameHexDoll,
					ItemID.ShadowFlameKnife,
					ItemID.ShadowFlameBow,
				};
			}
			else if (eventName == "Frost Legion") {
				return new List<int>() {
					ItemID.SnowBlock,
				};
			}
			else if (eventName == "Pirate Invasion") {
				return new List<int>() {
					ItemID.CoinGun,
					ItemID.Cutlass,
					ItemID.DiscountCard,
					ItemID.GoldRing,
					ItemID.LuckyCoin,
					ItemID.PirateStaff,
					ItemID.EyePatch,
					ItemID.SailorHat,
					ItemID.SailorShirt,
					ItemID.SailorPants,
					ItemID.BuccaneerBandana,
					ItemID.BuccaneerShirt,
					ItemID.BuccaneerPants,
					ItemID.GoldenBathtub,
					ItemID.GoldenBed,
					ItemID.GoldenBookcase,
					ItemID.GoldenCandelabra,
					ItemID.GoldenCandle,
					ItemID.GoldenChair,
					ItemID.GoldenChest,
					ItemID.GoldenChandelier,
					ItemID.GoldenDoor,
					ItemID.GoldenDresser,
					ItemID.GoldenClock,
					ItemID.GoldenLamp,
					ItemID.GoldenLantern,
					ItemID.GoldenPiano,
					ItemID.GoldenPlatform,
					ItemID.GoldenSink,
					ItemID.GoldenSofa,
					ItemID.GoldenTable,
					ItemID.GoldenToilet,
					ItemID.GoldenWorkbench,
				};
			}
			else if (eventName == "Solar Eclipse") {
				return new List<int>() {
					ItemID.EyeSpring,
					ItemID.BrokenBatWing,
					ItemID.MoonStone,
					ItemID.NeptunesShell,
					ItemID.Steak,
					ItemID.DeathSickle,
					ItemID.ButchersChainsaw,
					ItemID.ButcherMask,
					ItemID.ButcherApron,
					ItemID.ButcherPants,
					ItemID.DeadlySphereStaff,
					ItemID.ToxicFlask,
					ItemID.DrManFlyMask,
					ItemID.DrManFlyLabCoat,
					ItemID.NailGun,
					ItemID.Nail,
					ItemID.PsychoKnife,
					ItemID.BrokenHeroSword,
					ItemID.MothronWings,
					ItemID.TheEyeOfCthulhu,
				};
			}
			else if (eventName == "Pumpkin Moon") {
				return new List<int>() {
					ItemID.ScarecrowHat,
					ItemID.ScarecrowShirt,
					ItemID.ScarecrowPants,
					ItemID.SpookyWood,
					ItemID.JackOLanternMask,
				};
			}
			else if (eventName == "Frost Moon") {
				return new List<int>() {
					ItemID.ElfHat,
					ItemID.ElfShirt,
					ItemID.ElfPants,
				};
			}
			else if (eventName == "Martian Madness") {
				return new List<int>() {
					ItemID.MartianConduitPlating,
					ItemID.LaserDrill,
					ItemID.ChargedBlasterCannon,
					ItemID.AntiGravityHook,
					ItemID.MartianCostumeMask,
					ItemID.MartianCostumeShirt,
					ItemID.MartianCostumePants,
					ItemID.MartianUniformHelmet,
					ItemID.MartianUniformTorso,
					ItemID.MartianUniformPants,
					ItemID.BrainScrambler,
				};
			}
			else if (eventName == "Old One's Army") {
				return new List<int>() {
					ItemID.DD2EnergyCrystal,
					ItemID.DefenderMedal
				};
			}
			else if (eventName == "Lunar Event") {
				return new List<int>() {
					ItemID.FragmentSolar,
					ItemID.FragmentNebula,
					ItemID.FragmentStardust,
					ItemID.FragmentVortex,
				};
			}
			return new List<int>();
		}

		internal List<int> SetupEventCollectibles(string eventName) {
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
			return new List<int>();
		}

		// Old version compatibility methods
		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(EntryType.Event, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		// New system is better
		internal void AddBoss(float val, List<int> id, Mod source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string info, string despawnMessage, string texture, string iconTexture, Func<bool> available) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Boss, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddMiniBoss(float val, List<int> id, Mod source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string info, string despawnMessage, string texture, string iconTexture, Func<bool> available) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddEvent(float val, List<int> id, Mod source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string info, string despawnMessage, string texture, string iconTexture, Func<bool> available) {
			EnsureBossIsNotDuplicate(source?.Name ?? "Unknown", name);
			SortedBosses.Add(new BossInfo(EntryType.Event, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
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

		internal void AddOrphanData(string type, string modName, string bossName, List<int> ids) {
			OrphanType orphanType = OrphanType.Loot;
			if (type == "AddToBossCollection") {
				orphanType = OrphanType.Collection;
			}
			else if (type == "AddToBossSpawnItems") {
				orphanType = OrphanType.SpawnItem;
			}
			else if (type == "AddToEventNPCs") {
				orphanType = OrphanType.EventNPC;
			}
			ExtraData.Add(new OrphanInfo(orphanType, modName, bossName, ids));
		}
	}
}
