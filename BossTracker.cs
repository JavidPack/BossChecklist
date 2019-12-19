using System;
using System.Collections.Generic;
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
		public const float WallOfFlesh = 6f;
		public const float TheTwins = 7f;
		public const float TheDestroyer = 8f;
		public const float SkeletronPrime = 9f;
		public const float Plantera = 10f;
		public const float Golem = 11f;
		public const float DukeFishron = 12f;
		public const float LunaticCultist = 13f;
		public const float Moonlord = 14f;

		/// <summary>
		/// All currently loaded bosses/minibosses/events sorted in progression order.
		/// </summary>
		internal List<BossInfo> SortedBosses;
		internal List<OrphanInfo> ExtraData;
		internal bool BossesFinalized = false;
		internal bool AnyModHasOldCall = false;

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaBosses();
			ExtraData = new List<OrphanInfo>();
		}

		private void InitializeVanillaBosses() {
			SortedBosses = new List<BossInfo> {
				// Bosses -- Vanilla
				BossInfo.MakeVanillaBoss(EntryType.Boss, KingSlime, "$NPCName.KingSlime", new List<int>() { NPCID.KingSlime }, () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown }, "Mods.BossChecklist.BossVictory.KingSlime"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EyeOfCthulhu, "$NPCName.EyeofCthulhu", new List<int>() { NPCID.EyeofCthulhu }, () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }, "Mods.BossChecklist.BossVictory.EyeofCthulhu"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.EaterofWorldsHead", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail }, () => NPC.downedBoss2, new List<int>() { ItemID.WormFood }, "Mods.BossChecklist.BossVictory.EaterofWorlds"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, EaterOfWorlds, "$NPCName.BrainofCthulhu", new List<int>() { NPCID.BrainofCthulhu }, () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine }, "Mods.BossChecklist.BossVictory.BrainofCthulhu"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, QueenBee, "$NPCName.QueenBee", new List<int>() { NPCID.QueenBee }, () => NPC.downedQueenBee, new List<int>() { ItemID.Abeemination }, "Mods.BossChecklist.BossVictory.QueenBee"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Skeletron, "$NPCName.SkeletronHead", new List<int>() { NPCID.SkeletronHead }, () => NPC.downedBoss3, new List<int>() { ItemID.ClothierVoodooDoll }, "Mods.BossChecklist.BossVictory.Skeletron"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, WallOfFlesh, "$NPCName.WallofFlesh", new List<int>() { NPCID.WallofFlesh }, () => Main.hardMode  , new List<int>() { ItemID.GuideVoodooDoll }, "Mods.BossChecklist.BossVictory.WallofFlesh"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheTwins, "$Enemies.TheTwins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, () => NPC.downedMechBoss2, new List<int>() { ItemID.MechanicalEye }, "Mods.BossChecklist.BossVictory.Twins"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, TheDestroyer, "$NPCName.TheDestroyer", new List<int>() { NPCID.TheDestroyer }, () => NPC.downedMechBoss1, new List<int>() { ItemID.MechanicalWorm }, "Mods.BossChecklist.BossVictory.TheDestroyer"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, SkeletronPrime, "$NPCName.SkeletronPrime", new List<int>() { NPCID.SkeletronPrime }, () => NPC.downedMechBoss3, new List<int>() { ItemID.MechanicalSkull }, "Mods.BossChecklist.BossVictory.SkeletronPrime"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Plantera, "$NPCName.Plantera", new List<int>() { NPCID.Plantera }, () => NPC.downedPlantBoss, new List<int>() { }, "Mods.BossChecklist.BossVictory.Plantera"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem, "$NPCName.Golem", new List<int>() { NPCID.Golem, NPCID.GolemHead }, () => NPC.downedGolemBoss, new List<int>() { ItemID.LihzahrdPowerCell }, "Mods.BossChecklist.BossVictory.Golem"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Golem + 0.5f, "$NPCName.DD2Betsy", new List<int>() { NPCID.DD2Betsy }, () => WorldAssist.downedInvasionT3Ours, new List<int>() { ItemID.DD2ElderCrystal }), // No despawn message due to being in an event
				BossInfo.MakeVanillaBoss(EntryType.Boss, DukeFishron, "$NPCName.DukeFishron", new List<int>() { NPCID.DukeFishron }, () => NPC.downedFishron, new List<int>() { ItemID.TruffleWorm }, "Mods.BossChecklist.BossVictory.DukeFishron"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, LunaticCultist, "$NPCName.CultistBoss", new List<int>() { NPCID.CultistBoss }, () => NPC.downedAncientCultist, new List<int>() { }, "Mods.BossChecklist.BossVictory.LunaticCultist"),
				BossInfo.MakeVanillaBoss(EntryType.Boss, Moonlord, "$Enemies.MoonLord", new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand }, () => NPC.downedMoonlord, new List<int>() { ItemID.CelestialSigil }, "Mods.BossChecklist.BossVictory.MoonLord"),
				
				// Minibosses and Events -- Vanilla
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.2f, "Blood Moon", () => WorldAssist.downedBloodMoon, new List<int>() { }),
					// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
				BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.5f, "Goblin Army", () => NPC.downedGoblins, new List<int>() { ItemID.GoblinBattleStandard }).WithCustomTranslationKey("$LegacyInterface.88"),
				BossInfo.MakeVanillaEvent(EaterOfWorlds + 0.5f, "Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand }).WithCustomTranslationKey("$DungeonDefenders2.InvasionProgressTitle"),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, EaterOfWorlds + 0.51f, "$NPCName.DD2DarkMageT3", new List<int>() { NPCID.DD2DarkMageT3 }, () => WorldAssist.downedDarkMage, new List<int>() { }),
					BossInfo.MakeVanillaBoss(EntryType.MiniBoss, SkeletronPrime + 0.5f, "$NPCName.DD2OgreT3", new List<int>() { NPCID.DD2OgreT3 }, () => WorldAssist.downedOgre, new List<int>() { }),
				BossInfo.MakeVanillaEvent(WallOfFlesh + 0.6f, "Frost Legion", () => NPC.downedFrost, new List<int>() { ItemID.SnowGlobe }).WithCustomTranslationKey("$LegacyInterface.87"),
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
			if(AnyModHasOldCall)
				BossChecklist.instance.Logger.Info("Updated Mod.Call documentation for BossChecklist: https://github.com/JavidPack/BossChecklist/wiki/Support-using-Mod-Call#modcalls");
			
			if (Main.netMode == NetmodeID.Server) {
				BossChecklist.ServerCollectedRecords = new List<BossStats>[255];
				for (int i = 0; i < 255; i++) {
					BossChecklist.ServerCollectedRecords[i] = new List<BossStats>();
					for (int j = 0; j < BossChecklist.bossTracker.SortedBosses.Count; j++) {
						BossChecklist.ServerCollectedRecords[i].Add(new BossStats());
					}
				}
			}
		}

		internal protected List<int> SetupLoot(int bossNum) {
			if (bossNum == NPCID.KingSlime) {
				return new List<int>()
				{
					ItemID.KingSlimeBossBag,
					ItemID.RoyalGel,
					ItemID.Solidifier,
					ItemID.SlimySaddle,
					ItemID.NinjaHood,
					ItemID.NinjaShirt,
					ItemID.NinjaPants,
					ItemID.SlimeHook,
					ItemID.SlimeGun,
					ItemID.LesserHealingPotion
				};
			}
			if (bossNum == NPCID.EyeofCthulhu) {
				return new List<int>()
				{
					ItemID.EyeOfCthulhuBossBag,
					ItemID.EoCShield,
					ItemID.DemoniteOre,
					ItemID.CrimtaneOre,
					ItemID.UnholyArrow,
					ItemID.CorruptSeeds,
					ItemID.CrimsonSeeds,
					ItemID.Binoculars,
					ItemID.LesserHealingPotion
				};
			}
			if (bossNum == NPCID.EaterofWorldsHead) {
				return new List<int>()
				{
					ItemID.EaterOfWorldsBossBag,
					ItemID.WormScarf,
					ItemID.ShadowScale,
					ItemID.DemoniteOre,
					ItemID.EatersBone,
					ItemID.LesserHealingPotion
				};
			}
			if (bossNum == NPCID.BrainofCthulhu) {
				return new List<int>()
				{
					ItemID.BrainOfCthulhuBossBag,
					ItemID.BrainOfConfusion,
					ItemID.CrimtaneOre,
					ItemID.TissueSample,
					ItemID.BoneRattle,
					ItemID.LesserHealingPotion
				};
			}
			if (bossNum == NPCID.DD2DarkMageT3) {
				return new List<int>()
				{
					ItemID.WarTable,
					ItemID.WarTableBanner,
					ItemID.DD2PetDragon,
					ItemID.DD2PetGato,
				};
			}
			if (bossNum == NPCID.QueenBee) {
				return new List<int>()
				{
					ItemID.QueenBeeBossBag,
					ItemID.HiveBackpack,
					ItemID.BeeGun,
					ItemID.BeeKeeper,
					ItemID.BeesKnees,
					ItemID.HiveWand,
					ItemID.BeeHat,
					ItemID.BeeShirt,
					ItemID.BeePants,
					ItemID.HoneyComb,
					ItemID.Nectar,
					ItemID.HoneyedGoggles,
					ItemID.Beenade,
					ItemID.BottledHoney
				};
			}
			if (bossNum == NPCID.SkeletronHead) {
				return new List<int>()
				{
					ItemID.SkeletronBossBag,
					ItemID.BoneGlove,
					ItemID.SkeletronHand,
					ItemID.BookofSkulls,
					ItemID.LesserHealingPotion
				};
			}
			if (bossNum == NPCID.WallofFlesh) {
				return new List<int>()
				{
					ItemID.WallOfFleshBossBag,
					ItemID.DemonHeart,
					ItemID.Pwnhammer,
					ItemID.BreakerBlade,
					ItemID.ClockworkAssaultRifle,
					ItemID.LaserRifle,
					ItemID.WarriorEmblem,
					ItemID.SorcererEmblem,
					ItemID.RangerEmblem,
					ItemID.SummonerEmblem,
					ItemID.HealingPotion
				};
			}
			if (bossNum == NPCID.PirateShip) {
				return new List<int>()
				{
					ItemID.CoinGun,
					ItemID.LuckyCoin,
					ItemID.DiscountCard,
					ItemID.PirateStaff,
					ItemID.GoldRing,
					ItemID.Cutlass,
				};
			}
			if (bossNum == NPCID.Retinazer) {
				return new List<int>()
				{
					ItemID.TwinsBossBag,
					ItemID.MechanicalWheelPiece,
					ItemID.SoulofSight,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.TheDestroyer) {
				return new List<int>()
				{
					ItemID.DestroyerBossBag,
					ItemID.MechanicalWagonPiece,
					ItemID.SoulofMight,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.SkeletronPrime) {
				return new List<int>()
				{
					ItemID.SkeletronPrimeBossBag,
					ItemID.MechanicalBatteryPiece,
					ItemID.SoulofFright,
					ItemID.HallowedBar,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.DD2OgreT3) {
				return new List<int>()
				{
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
			if (bossNum == NPCID.Plantera) {
				return new List<int>()
				{
					ItemID.PlanteraBossBag,
					ItemID.SporeSac,
					ItemID.TempleKey,
					ItemID.Seedling,
					ItemID.TheAxe,
					ItemID.PygmyStaff,
					ItemID.GrenadeLauncher,
					ItemID.VenusMagnum,
					ItemID.NettleBurst,
					ItemID.LeafBlower,
					ItemID.FlowerPow,
					ItemID.WaspGun,
					ItemID.Seedler,
					ItemID.ThornHook,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.MourningWood) {
				return new List<int>()
				{
					ItemID.SpookyWood,
					ItemID.CursedSapling,
					ItemID.SpookyTwig,
					ItemID.SpookyHook,
					ItemID.NecromanticScroll,
					ItemID.StakeLauncher,
				};
			}
			if (bossNum == NPCID.Pumpking) {
				return new List<int>()
				{
					ItemID.TheHorsemansBlade,
					ItemID.BatScepter,
					ItemID.BlackFairyDust,
					ItemID.SpiderEgg,
					ItemID.RavenStaff,
					ItemID.CandyCornRifle,
					ItemID.JackOLanternLauncher,
				};
			}
			if (bossNum == NPCID.Everscream) {
				return new List<int>()
				{
					ItemID.ChristmasTreeSword,
					ItemID.ChristmasHook,
					ItemID.Razorpine,
					ItemID.FestiveWings,
				};
			}
			if (bossNum == NPCID.SantaNK1) {
				return new List<int>()
				{
					ItemID.EldMelter,
					ItemID.ChainGun,
				};
			}
			if (bossNum == NPCID.IceQueen) {
				return new List<int>()
				{
					ItemID.SnowmanCannon,
					ItemID.NorthPole,
					ItemID.BlizzardStaff,
					ItemID.BabyGrinchMischiefWhistle,
					ItemID.ReindeerBells,
				};
			}
			if (bossNum == NPCID.Golem) {
				return new List<int>()
				{
					ItemID.GolemBossBag,
					ItemID.ShinyStone,
					ItemID.Stynger,
					ItemID.PossessedHatchet,
					ItemID.SunStone,
					ItemID.EyeoftheGolem,
					ItemID.Picksaw,
					ItemID.HeatRay,
					ItemID.StaffofEarth,
					ItemID.GolemFist,
					ItemID.BeetleHusk,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.MartianSaucer) {
				return new List<int>()
				{
					ItemID.Xenopopper,
					ItemID.XenoStaff,
					ItemID.LaserMachinegun,
					ItemID.LaserDrill,
					ItemID.ElectrosphereLauncher,
					ItemID.ChargedBlasterCannon,
					ItemID.InfluxWaver,
					ItemID.CosmicCarKey,
					ItemID.AntiGravityHook,
					ItemID.GreaterHealingPotion,
				};
			}
			if (bossNum == NPCID.DD2Betsy) {
				return new List<int>()
				{
					ItemID.BossBagBetsy,
					ItemID.BetsyWings,
					ItemID.DD2BetsyBow, // Aerial Bane
                    ItemID.MonkStaffT3, // Sky Dragon's Fury
                    ItemID.ApprenticeStaffT3, // Betsy's Wrath
                    ItemID.DD2SquireBetsySword // Flying Dragon
                };
			}
			if (bossNum == NPCID.DukeFishron) {
				return new List<int>()
				{
					ItemID.FishronBossBag,
					ItemID.ShrimpyTruffle,
					ItemID.FishronWings,
					ItemID.BubbleGun,
					ItemID.Flairon,
					ItemID.RazorbladeTyphoon,
					ItemID.TempestStaff,
					ItemID.Tsunami,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.CultistBoss) {
				return new List<int>()
				{
					ItemID.CultistBossBag,
					ItemID.LunarCraftingStation,
					ItemID.GreaterHealingPotion
				};
			}
			if (bossNum == NPCID.MoonLordHead) {
				return new List<int>()
				{
					ItemID.MoonLordBossBag,
					ItemID.GravityGlobe,
					ItemID.PortalGun,
					ItemID.LunarOre,
					ItemID.Meowmere,
					ItemID.Terrarian,
					ItemID.StarWrath,
					ItemID.SDMG,
					ItemID.FireworksLauncher, // The Celebration
                    ItemID.LastPrism,
					ItemID.LunarFlareBook,
					ItemID.RainbowCrystalStaff,
					ItemID.MoonlordTurretStaff, // Lunar Portal Staff
                    ItemID.SuspiciousLookingTentacle,
					ItemID.GreaterHealingPotion
				};
			}
			return new List<int>();
		}

		internal protected List<int> SetupCollect(int bossNum) {
			if (bossNum == NPCID.KingSlime) {
				return new List<int>()
				{
					ItemID.KingSlimeTrophy,
					ItemID.KingSlimeMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.EyeofCthulhu) {
				return new List<int>()
				{
					ItemID.EyeofCthulhuTrophy,
					ItemID.EyeMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.EaterofWorldsHead) {
				return new List<int>()
				{
					ItemID.EaterofWorldsTrophy,
					ItemID.EaterMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.BrainofCthulhu) {
				return new List<int>()
				{
					ItemID.BrainofCthulhuTrophy,
					ItemID.BrainMask,
					ItemID.MusicBoxBoss3
				};
			}
			if (bossNum == NPCID.BrainofCthulhu) {
				return new List<int>()
				{
					ItemID.BossMaskDarkMage,
					ItemID.BossTrophyDarkmage,
					ItemID.MusicBoxDD2
				};
			}
			if (bossNum == NPCID.DD2DarkMageT3) {
				return new List<int>()
				{
					ItemID.BossMaskDarkMage,
					ItemID.BossTrophyDarkmage,
					ItemID.MusicBoxDD2
				};
			}
			if (bossNum == NPCID.QueenBee) {
				return new List<int>()
				{
					ItemID.QueenBeeTrophy,
					ItemID.BeeMask,
					ItemID.MusicBoxBoss4
				};
			}
			if (bossNum == NPCID.SkeletronHead) {
				return new List<int>()
				{
					ItemID.SkeletronTrophy,
					ItemID.SkeletronMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.WallofFlesh) {
				return new List<int>()
				{
					ItemID.WallofFleshTrophy,
					ItemID.FleshMask,
					ItemID.MusicBoxBoss2
				};
			}
			if (bossNum == NPCID.PirateShip) {
				return new List<int>()
				{
					ItemID.FlyingDutchmanTrophy,
					ItemID.MusicBoxPirates
				};
			}
			if (bossNum == NPCID.Retinazer) {
				return new List<int>()
				{
					ItemID.RetinazerTrophy,
					ItemID.SpazmatismTrophy,
					ItemID.TwinMask,
					ItemID.MusicBoxBoss2
				};
			}
			if (bossNum == NPCID.TheDestroyer) {
				return new List<int>()
				{
					ItemID.DestroyerTrophy,
					ItemID.DestroyerMask,
					ItemID.MusicBoxBoss3
				};
			}
			if (bossNum == NPCID.SkeletronPrime) {
				return new List<int>()
				{
					ItemID.SkeletronPrimeTrophy,
					ItemID.SkeletronPrimeMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.DD2OgreT3) {
				return new List<int>()
				{
					ItemID.BossMaskOgre,
					ItemID.BossTrophyOgre,
					ItemID.MusicBoxDD2
				};
			}
			if (bossNum == NPCID.Plantera) {
				return new List<int>()
				{
					ItemID.PlanteraTrophy,
					ItemID.PlanteraMask,
					ItemID.MusicBoxPlantera
				};
			}
			if (bossNum == NPCID.MourningWood) {
				return new List<int>()
				{
					ItemID.MourningWoodTrophy,
					ItemID.MusicBoxPumpkinMoon
				};
			}
			if (bossNum == NPCID.Pumpking) {
				return new List<int>()
				{
					ItemID.PumpkingTrophy,
					ItemID.MusicBoxPumpkinMoon
				};
			}
			if (bossNum == NPCID.Everscream) {
				return new List<int>()
				{
					ItemID.EverscreamTrophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			if (bossNum == NPCID.SantaNK1) {
				return new List<int>()
				{
					ItemID.SantaNK1Trophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			if (bossNum == NPCID.IceQueen) {
				return new List<int>()
				{
					ItemID.IceQueenTrophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			if (bossNum == NPCID.Golem) {
				return new List<int>()
				{
					ItemID.GolemTrophy,
					ItemID.GolemMask,
					ItemID.MusicBoxBoss5
				};
			}
			if (bossNum == NPCID.MartianSaucer) {
				return new List<int>()
				{
					ItemID.MartianSaucerTrophy,
					ItemID.MusicBoxMartians
				};
			}
			if (bossNum == NPCID.DD2Betsy) {
				return new List<int>()
				{
					ItemID.BossTrophyBetsy,
					ItemID.BossMaskBetsy,
					ItemID.MusicBoxDD2
				};
			}
			if (bossNum == NPCID.DukeFishron) {
				return new List<int>()
				{
					ItemID.DukeFishronTrophy,
					ItemID.DukeFishronMask,
					ItemID.MusicBoxBoss1
				};
			}
			if (bossNum == NPCID.CultistBoss) {
				return new List<int>()
				{
					ItemID.AncientCultistTrophy,
					ItemID.BossMaskCultist,
					ItemID.MusicBoxBoss5
				};
			}
			if (bossNum == NPCID.MoonLordHead) {
				return new List<int>()
				{
					ItemID.MoonLordTrophy,
					ItemID.BossMaskMoonlord,
					ItemID.MusicBoxLunarBoss
				};
			}
			return new List<int>();
		}

		internal List<int> SetupEventNPCList(string eventName) {
			if (eventName == "Blood Moon") {
				return new List<int>()
				{
					NPCID.BloodZombie,
					NPCID.Drippler,
					NPCID.TheGroom,
					NPCID.TheBride,
					NPCID.Clown,
					NPCID.CorruptBunny,
					NPCID.CorruptGoldfish,
					NPCID.CorruptPenguin,
					NPCID.CrimsonBunny,
					NPCID.CrimsonGoldfish,
					NPCID.CrimsonPenguin
				};
			}
			if (eventName == "Goblin Army") {
				return new List<int>()
				{
					NPCID.GoblinScout,
					NPCID.GoblinPeon,
					NPCID.GoblinSorcerer,
					NPCID.GoblinThief,
					NPCID.GoblinWarrior,
					NPCID.GoblinArcher,
					NPCID.GoblinSummoner,
				};
			}
			if (eventName == "Old One's Army") {
				return new List<int>()
				{
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
					NPCID.DD2OgreT3
				};
			}
			if (eventName == "Frost Legion") {
				return new List<int>()
				{
					NPCID.MisterStabby,
					NPCID.SnowmanGangsta,
					NPCID.SnowBalla,
				};
			}
			if (eventName == "Solar Eclipse") {
				return new List<int>()
				{
					NPCID.SwampThing,
					NPCID.Frankenstein,
					NPCID.Eyezor,
					NPCID.Vampire,
					NPCID.ThePossessed,
					NPCID.Fritz,
					NPCID.CreatureFromTheDeep,
					NPCID.Reaper,
					NPCID.Mothron,
					NPCID.Butcher,
					NPCID.Nailhead,
					NPCID.DeadlySphere,
					NPCID.Psycho,
					NPCID.DrManFly,
				};
			}
			if (eventName == "Pirate Invasion") {
				return new List<int>()
				{
					NPCID.PirateDeckhand,
					NPCID.PirateDeadeye,
					NPCID.PirateCorsair,
					NPCID.PirateCrossbower,
					NPCID.PirateCaptain,
					NPCID.Parrot,
					NPCID.PirateShip,
				};
			}
			if (eventName == "Pumpkin Moon") {
				return new List<int>()
				{
					NPCID.Scarecrow1,
					NPCID.Splinterling,
					NPCID.Hellhound,
					NPCID.Poltergeist,
					NPCID.HeadlessHorseman,
					NPCID.MourningWood,
					NPCID.Pumpking,
				};
			}
			if (eventName == "Frost Moon") {
				return new List<int>()
				{
					NPCID.GingerbreadMan,
					NPCID.ZombieElf,
					NPCID.ElfArcher,
					NPCID.Nutcracker,
					NPCID.Yeti,
					NPCID.ElfCopter,
					NPCID.Krampus,
					NPCID.Flocko,
					NPCID.Everscream,
					NPCID.SantaNK1,
					NPCID.IceQueen
				};
			}
			if (eventName == "Martian Madness") {
				return new List<int>()
				{
					NPCID.MartianSaucer,
					NPCID.Scutlix,
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
			if (eventName == "Lunar Event") {
				return new List<int>()
				{
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
			if (eventName == "Blood Moon") {
				return new List<int>()
				{
					ItemID.TopHat,
					ItemID.TheBrideHat,
					ItemID.TheBrideDress,
					ItemID.MoneyTrough,
					ItemID.SharkToothNecklace,
					ItemID.Bananarang,
				};
			}
			if (eventName == "Goblin Army") {
				return new List<int>()
				{
					ItemID.Harpoon,
					ItemID.SpikyBall,
					ItemID.ShadowFlameHexDoll,
					ItemID.ShadowFlameKnife,
					ItemID.ShadowFlameBow,
				};
			}
			if (eventName == "Frost Legion") {
				return new List<int>()
				{
					ItemID.SnowBlock,
				};
			}
			if (eventName == "Pirate Invasion") {
				return new List<int>()
				{
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
			if (eventName == "Solar Eclipse") {
				return new List<int>()
				{
					ItemID.EyeSpring,
					ItemID.DeathSickle,
					ItemID.BrokenBatWing,
					ItemID.MoonStone,
					ItemID.ButchersChainsaw,
					ItemID.NeptunesShell,
					ItemID.DeadlySphereStaff,
					ItemID.ToxicFlask,
					ItemID.BrokenHeroSword,
					ItemID.MothronWings,
					ItemID.TheEyeOfCthulhu,
					ItemID.NailGun,
					ItemID.Nail,
					ItemID.PsychoKnife,
				};
			}
			if (eventName == "Pumpkin Moon") {
				return new List<int>()
				{
					ItemID.ScarecrowHat,
					ItemID.ScarecrowShirt,
					ItemID.ScarecrowPants,
					ItemID.SpookyWood,
					ItemID.StakeLauncher,
					ItemID.Stake,
					ItemID.NecromanticScroll,
					ItemID.SpookyHook,
					ItemID.SpookyTwig,
					ItemID.CursedSapling,
					ItemID.JackOLanternMask,
					ItemID.TheHorsemansBlade,
					ItemID.RavenStaff,
					ItemID.BatScepter,
					ItemID.CandyCornRifle,
					ItemID.JackOLanternLauncher,
					ItemID.BlackFairyDust,
					ItemID.SpiderEgg,
				};
			}
			if (eventName == "Frost Moon") {
				return new List<int>()
				{
					ItemID.ElfHat,
					ItemID.ElfShirt,
					ItemID.ElfPants,
					ItemID.ChristmasTreeSword,
					ItemID.Razorpine,
					ItemID.FestiveWings,
					ItemID.ChristmasHook,
					ItemID.EldMelter,
					ItemID.ChainGun,
					ItemID.BlizzardStaff,
					ItemID.NorthPole,
					ItemID.SnowmanCannon,
					ItemID.BabyGrinchMischiefWhistle,
					ItemID.ReindeerBells,
				};
			}
			if (eventName == "Martian Madness") {
				return new List<int>()
				{
					ItemID.MartianCostumeMask,
					ItemID.MartianCostumeShirt,
					ItemID.MartianCostumePants,
					ItemID.MartianUniformHelmet,
					ItemID.MartianUniformTorso,
					ItemID.MartianUniformPants,
					ItemID.BrainScrambler,
					ItemID.LaserMachinegun,
					ItemID.Xenopopper,
					ItemID.XenoStaff,
					ItemID.CosmicCarKey,
					ItemID.LaserDrill,
					ItemID.ElectrosphereLauncher,
					ItemID.ChargedBlasterCannon,
					ItemID.InfluxWaver,
					ItemID.AntiGravityHook,
				};
			}
			if (eventName == "Old One's Army") {
				return new List<int>()
				{
					ItemID.DefenderMedal,
					ItemID.WarTable,
					ItemID.WarTableBanner,
					ItemID.DD2PetDragon,
					ItemID.DD2PetGato,
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
			if (eventName == "Lunar Event") {
				return new List<int>()
				{
					ItemID.FragmentSolar,
					ItemID.FragmentNebula,
					ItemID.FragmentStardust,
					ItemID.FragmentVortex,
				};
			}
			return new List<int>();
		}

		internal string SetupSpawnDesc(int npcID) {
			if (npcID == NPCID.KingSlime) return $"Use [i:{ItemID.SlimeCrown}], randomly in outer 3rds of map, or kill 150 slimes during slime rain.";
			if (npcID == NPCID.EyeofCthulhu) return $"Use [i:{ItemID.SuspiciousLookingEye}] at night, or 1/3 chance nightly if over 200 HP.";
			if (npcID == NPCID.BrainofCthulhu) return $"Use [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts in a crimson chasm.";
			if (npcID == NPCID.EaterofWorldsHead) return $"Use [i:{ItemID.WormFood}] or break 3 Shadow Orbs in a corruption chasm.";
			if (npcID == NPCID.QueenBee) return $"Use [i:{ItemID.Abeemination}] or break a Bee Hive's Larva found in the jungle.";
			if (npcID == NPCID.SkeletronHead) return $"Visit the dungeon and talk to the old man at night or kill the Clothier with [i:{ItemID.ClothierVoodooDoll}] at night.";
			if (npcID == NPCID.WallofFlesh) return $"Spawn by throwing [i:{ItemID.GuideVoodooDoll}] into lava in the Underworld. [c/FF0000:Starts Hardmode!]";
			if (npcID == NPCID.Retinazer) return $"10% chance to randomly spawn each night after the first altar is destroyed, until one mech boss is defeated. Alternatively, Use [i:{ItemID.MechanicalEye}] at night to spawn.";
			if (npcID == NPCID.TheDestroyer) return $"10% chance to randomly spawn each night after the first altar is destroyed, until one mech boss is defeated. Use [i:{ItemID.MechanicalWorm}] at night to spawn.";
			if (npcID == NPCID.SkeletronPrime) return $"10% chance to randomly spawn each night after the first altar is destroyed, until one mech boss is defeated. Use [i:{ItemID.MechanicalSkull}] at night to spawn.";
			if (npcID == NPCID.Plantera) return $"Break a Plantera's Bulb in jungle after 3 Mechanical bosses have been defeated.";
			if (npcID == NPCID.Golem) return $"Use [i:{ItemID.LihzahrdPowerCell}] on Lihzahrd Altar, found deep in the Jungle Temple.";
			if (npcID == NPCID.DD2Betsy) return "Spawns on Wave 7, the final wave, of Old One's Army (Tier 3).";
			if (npcID == NPCID.DukeFishron) return $"Fish in ocean using the [i:{ItemID.TruffleWorm}] as bait.";
			if (npcID == NPCID.CultistBoss) return $"Kill the 4 cultists outside the dungeon after defeating Golem.";
			if (npcID == NPCID.MoonLordHead) return $"Use [i:{ItemID.CelestialSigil}] or defeat all {(BossChecklist.tremorLoaded ? 5 : 4)} pillars. {(BossChecklist.tremorLoaded ? "[c/FF0000:Starts Tremode!]" : "")}";

			if (npcID == NPCID.Pumpking) return $"Spawns during Wave 7 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]";
			if (npcID == NPCID.MourningWood) return $"Spawns during Wave 4 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]";
			if (npcID == NPCID.IceQueen) return $"Spawns during Wave 11 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";
			if (npcID == NPCID.SantaNK1) return $"Spawns during Wave 7 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";
			if (npcID == NPCID.Everscream) return $"Spawns during Wave 4 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";

			if (npcID == NPCID.PirateShip) return $"Occasionally appears during a Pirate Invasion! Start Pirate Invasion with [i:{ItemID.PirateMap}]";
			if (npcID == NPCID.DD2OgreT3) return $"Appears in wave 7 (Tier 2) and waves 4 & 6 (Tier 3) during the Old One's Army event! Start Old One's Army with [i:{ItemID.DD2ElderCrystalStand}] and [i:{ItemID.DD2ElderCrystal}]";
			if (npcID == NPCID.DD2DarkMageT3) return $"Appears in wave 5 (Tier 1) and waves 5 & 6 (Tier 3) during the Old One's Army event! Start Old One's Army with [i:{ItemID.DD2ElderCrystalStand}] and [i:{ItemID.DD2ElderCrystal}]";
			if (npcID == NPCID.MartianSaucer) return "Occasionally appears during Martian Madness! Start Martian Madness by being detected by an alien probe.";

			return "";
		}

		internal string SetupEventSpawnDesc(string eventName) {
			if (eventName == "Blood Moon") return $"Occurs randomly at the start of night. Can start when any player in the world has more than 100 health and the current moon phase is NOT a new moon. Lasts until sunrise.";
			if (eventName == "Goblin Army") return $"Has a 1 in 3 chance of occurring every dawn if at least one Shadow Orb/Crimson Heart has been destroyed, at least one player has 200 health or more, and there is not a Goblin Army already in progress. It can also be summoned manually using a [i:{ItemID.GoblinBattleStandard}]. Lasts until enough enemies are defeated.";
			if (eventName == "Old One's Army") return $"After finding the Tavernkeep, purchase and activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]. Lasts until all waves are cleared.";
			if (eventName == "Frost Legion") return $"Use a [i:{ItemID.SnowGlobe}], which can be found by opening [i:{ItemID.Present}], during the Christmas season. Lasts until enough enemies are defeated.";
			if (eventName == "Solar Eclipse") return $"Has a 1/20 chance to occur each day upon dawn, as soon as any Mechanical Boss has been defeated. Alternatively, summon with a [i:{ItemID.SolarTablet}]. Lasts until nightfall.";
			if (eventName == "Pirate Invasion") return $"Occurs randomly once at least one altar has been destroyed. Can be summoned with a [i:{ItemID.PirateMap}], which can be obtained from killing any enemy in the Ocean biome during Hardmode. Lasts until enough enemies are defeated.";
			if (eventName == "Pumpkin Moon") return $"Summoned with a [i:{ItemID.PumpkinMoonMedallion}] at night. Lasts until sunrise.";
			if (eventName == "Frost Moon") return $"Summoned with a [i:{ItemID.NaughtyPresent}] at night. Lasts until sunrise.";
			if (eventName == "Martian Madness") return $"After defeating Golem, find a Martian Probe above ground and allow it to find you and escape. Lasts until enough enemies are defeated.";
			if (eventName == "Lunar Event") return $"Defeat the Lunatic Cultist found outside the dungeon. Each pillar spawns in its own area within the world. Lasts until all 4 pillars are destroyed.";
			return "";
		}

		internal List<int> SetupEventCollectibles(string eventName) {
			if (eventName == "Blood Moon") {
				return new List<int>() {
					ItemID.MusicBoxEerie,
				};
			}
			if (eventName == "Goblin Army") {
				return new List<int>() {
					ItemID.MusicBoxGoblins,
				};
			}

			if (eventName == "Old One's Army") {
				return new List<int>() {
					ItemID.MusicBoxDD2,
					//Betsy
					ItemID.BossTrophyBetsy,
					ItemID.BossMaskBetsy,
					//Ogre
					ItemID.BossMaskOgre,
					ItemID.BossTrophyOgre,
					//Dark Mage
					ItemID.BossMaskDarkMage,
					ItemID.BossTrophyDarkmage,
				};
			}
			if (eventName == "Frost Legion") {
				return new List<int>() {
					ItemID.MusicBoxBoss3,
				};
			}
			if (eventName == "Solar Eclipse") {
				return new List<int>() {
					ItemID.MusicBoxEclipse,
				};
			}
			if (eventName == "Pirate Invasion") {
				return new List<int>() {
					ItemID.MusicBoxPirates,
					ItemID.FlyingDutchmanTrophy
				};
			}
			if (eventName == "Pumpkin Moon") {
				return new List<int>() {
					ItemID.MourningWoodTrophy,
					ItemID.PumpkingTrophy,
					ItemID.MusicBoxPumpkinMoon
				};
			}
			if (eventName == "Frost Moon") {
				return new List<int>() {
					ItemID.EverscreamTrophy,
					ItemID.SantaNK1Trophy,
					ItemID.IceQueenTrophy,
					ItemID.MusicBoxFrostMoon
				};
			}
			if (eventName == "Martian Madness") {
				return new List<int>() {
					ItemID.MusicBoxMartians,
					ItemID.MartianSaucerTrophy
				};
			}
			if (eventName == "Lunar Event") {
				return new List<int>() {
					ItemID.MusicBoxTowers,
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
			SortedBosses.Add(new BossInfo(EntryType.Boss, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddMiniBoss(float val, List<int> id, Mod source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string info, string despawnMessage, string texture, string iconTexture, Func<bool> available) {
			SortedBosses.Add(new BossInfo(EntryType.MiniBoss, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void AddEvent(float val, List<int> id, Mod source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string info, string despawnMessage, string texture, string iconTexture, Func<bool> available) {
			SortedBosses.Add(new BossInfo(EntryType.Event, val, source?.Name ?? "Unknown", name, id, down, available, spawn, collect, loot, texture, info, despawnMessage, iconTexture));
			LogNewBoss(source?.Name ?? "Unknown", name);
		}

		internal void LogNewBoss(string mod, string name) {
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
			BossChecklist.instance.Logger.Info(name + " has been added to the Boss Log!");
		}

		internal void AddOrphanData(string type, string modName, string bossName, List<int> ids) {
			OrphanType orphanType = OrphanType.Loot;
			if (type == "AddToBossCollection") orphanType = OrphanType.Collection;
			else if (type == "AddToBossSpawnItems") orphanType = OrphanType.SpawnItem;
			else if (type == "AddToEventNPCs") orphanType = OrphanType.EventNPC;
			ExtraData.Add(new OrphanInfo(orphanType, modName, bossName, ids));
		}
	}
}
