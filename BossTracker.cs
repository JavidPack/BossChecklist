using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
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

		// TODO: OrphanBosses: Boss info added to other bosses when those bosses aren't loaded yet. Log remaining orphans maybe after load.

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaBosses();
			ExtraData = new List<OrphanInfo>();
		}

		private void InitializeVanillaBosses() {
			SortedBosses = new List<BossInfo> {
			// Bosses -- Vanilla
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, KingSlime, "King Slime", new List<int>() { NPCID.KingSlime }, () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EyeOfCthulhu, "Eye of Cthulhu", new List<int>() { NPCID.EyeofCthulhu }, () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EaterOfWorlds,"Eater of Worlds", new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail}, () => NPC.downedBoss2,new List<int>() { ItemID.WormFood }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EaterOfWorlds, "Brain of Cthulhu", new List<int>() { NPCID.BrainofCthulhu }, () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, QueenBee, "Queen Bee", new List<int>() { NPCID.QueenBee }, () => NPC.downedQueenBee, new List<int>() { ItemID.Abeemination }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Skeletron, "Skeletron", new List<int>() { NPCID.SkeletronHead }, () => NPC.downedBoss3, new List<int>() { ItemID.ClothierVoodooDoll }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, WallOfFlesh, "Wall of Flesh", new List<int>() { NPCID.WallofFlesh }, () => Main.hardMode  , new List<int>() { ItemID.GuideVoodooDoll }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, TheTwins, "The Twins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, () => NPC.downedMechBoss2,new List<int>() { ItemID.MechanicalEye }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, TheDestroyer, "The Destroyer", new List<int>() { NPCID.TheDestroyer }, () => NPC.downedMechBoss1,  new List<int>() { ItemID.MechanicalWorm }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, SkeletronPrime, "Skeletron Prime", new List<int>() { NPCID.SkeletronPrime }, () => NPC.downedMechBoss3,new List<int>() { ItemID.MechanicalSkull }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Plantera, "Plantera", new List<int>() { NPCID.Plantera }, () => NPC.downedPlantBoss, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Golem, "Golem", new List<int>() { NPCID.Golem, NPCID.GolemHead }, () => NPC.downedGolemBoss,  new List<int>() { ItemID.LihzahrdPowerCell }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Golem + 0.5f, "Betsy", new List<int>() { NPCID.DD2Betsy }, () => WorldAssist.downedBetsy, new List<int>() { ItemID.DD2ElderCrystal }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, DukeFishron, "Duke Fishron", new List<int>() { NPCID.DukeFishron }, () => NPC.downedFishron, new List<int>() { ItemID.TruffleWorm }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, LunaticCultist, "Lunatic Cultist", new List<int>() { NPCID.CultistBoss }, () => NPC.downedAncientCultist, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Moonlord, "Moonlord", new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand }, () => NPC.downedMoonlord, new List<int>() { ItemID.CelestialSigil }),
			// Mini Bosses -- Vanilla
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.12f, "Pumpking", new List<int>() { NPCID.Pumpking }, () => NPC.downedHalloweenKing, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.11f, "Mourning Wood", new List<int>() { NPCID.MourningWood }, () => NPC.downedHalloweenTree, new List<int>() { }),
				// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.16f, "Ice Queen", new List<int>() { NPCID.IceQueen }, () => NPC.downedChristmasIceQueen, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.15f, "Santa-NK1", new List<int>() { NPCID.SantaNK1 }, () => NPC.downedChristmasSantank, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.14f, "Everscream", new List<int>() { NPCID.Everscream }, () => NPC.downedChristmasTree, new List<int>() { }),
				// Mothron??
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, WallOfFlesh + 0.71f, "Flying Dutchman", new List<int>() { NPCID.PirateShip }, () => true, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, SkeletronPrime + 0.5f, "Ogre", new List<int>() { NPCID.DD2OgreT3 }, () => true, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, EaterOfWorlds + 0.51f, "Dark Mage", new List<int>() { NPCID.DD2DarkMageT3 }, () => true, new List<int>() { }),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Golem + 0.11f, "Martian Saucer", new List<int>() { NPCID.MartianSaucer, NPCID.MartianSaucerCore }, () => true, new List<int>() { }),
			
			// Events -- Vanilla
			BossInfo.MakeVanillaEvent(LunaticCultist + 0.01f, "Lunar Event", () => NPC.downedTowerNebula && NPC.downedTowerVortex && NPC.downedTowerSolar && NPC.downedTowerStardust, new List<int>() { }),
			BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.5f, "Goblin Army", () => NPC.downedGoblins, new List<int>() {ItemID.GoblinBattleStandard }, "BossChecklist/Resources/BossTextures/EventGoblinArmy"),
			BossInfo.MakeVanillaEvent(WallOfFlesh + 0.6f, "Frost Legion", () => NPC.downedFrost, new List<int>() { ItemID.SnowGlobe }),
			BossInfo.MakeVanillaEvent(Golem + 0.1f, "Martian Madness", () => NPC.downedMartians, new List<int>() { }),
			BossInfo.MakeVanillaEvent(WallOfFlesh + 0.7f, "Pirate Invasion", () => NPC.downedPirates, new List<int>() { ItemID.PirateMap }),
			BossInfo.MakeVanillaEvent(EaterOfWorlds + 0.5f, "Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty, new List<int>() { ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand }, "BossChecklist/Resources/BossTextures/EventDD2"),
			BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.2f, "Blood Moon", () => true, new List<int>() { }, "BossChecklist/Resources/BossTextures/EventBloodMoon"),
			BossInfo.MakeVanillaEvent(SkeletronPrime + 0.2f, "Solar Eclipse", () => true, new List<int>() { ItemID.SolarTablet }, "BossChecklist/Resources/BossTextures/EventSolarEclipse"),
			BossInfo.MakeVanillaEvent(Plantera + 0.1f, "Pumpkin Moon", () => true, new List<int>() { ItemID.PumpkinMoonMedallion }, "BossChecklist/Resources/BossTextures/EventPumpkinMoon"),
			BossInfo.MakeVanillaEvent(Plantera + 0.13f, "Frost Moon", () => true, new List<int>() { ItemID.NaughtyPresent }, "BossChecklist/Resources/BossTextures/EventFrostMoon"),
			};
		}

		internal void FinalizeBossData() {
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
			BossesFinalized = true;
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
					ItemID.UnholyArrow,
					ItemID.CorruptSeeds,
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
			if (bossNum == NPCID.Plantera) {
				return new List<int>()
				{
					ItemID.PlanteraTrophy,
					ItemID.PlanteraMask,
					ItemID.MusicBoxPlantera
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
			if (eventName == "Goblin Army") {
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
			if (npcID == NPCID.EyeofCthulhu) return $"Use [i:{ItemID.SuspiciousLookingEye}] at night, or 1/3 chance nightly if over 200 HP\nAchievement : [a:EYE_ON_YOU]";
			if (npcID == NPCID.BrainofCthulhu) return $"Use [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts";
			if (npcID == NPCID.EaterofWorldsHead) return $"Use [i:{ItemID.WormFood}] or break 3 Shadow Orbs";
			if (npcID == NPCID.QueenBee) return $"Use [i:{ItemID.Abeemination}] or break Larva in Jungle";
			if (npcID == NPCID.SkeletronHead) return $"Visit dungeon or use [i:{ItemID.ClothierVoodooDoll}] at night";
			if (npcID == NPCID.WallofFlesh) return $"Spawn by throwing [i:{ItemID.GuideVoodooDoll}] in lava in the Underworld. [c/FF0000:Starts Hardmode!]";
			if (npcID == NPCID.Retinazer) return $"Use [i:{ItemID.MechanicalEye}] at night to spawn";
			if (npcID == NPCID.TheDestroyer) return $"Use [i:{ItemID.MechanicalWorm}] at night to spawn";
			if (npcID == NPCID.SkeletronPrime) return $"Use [i:{ItemID.MechanicalSkull}] at night to spawn";
			if (npcID == NPCID.Plantera) return $"Break a Plantera's Bulb in jungle after 3 Mechanical bosses have been defeated";
			if (npcID == NPCID.Golem) return $"Use [i:{ItemID.LihzahrdPowerCell}] on Lihzahrd Altar";
			if (npcID == NPCID.DD2Betsy) return "Fight during Old One's Army Tier 3";
			if (npcID == NPCID.DukeFishron) return $"Fish in ocean using the [i:{ItemID.TruffleWorm}] bait";
			if (npcID == NPCID.CultistBoss) return $"Kill the cultists outside the dungeon post-Golem";
			if (npcID == NPCID.MoonLordHead) return $"Use [i:{ItemID.CelestialSigil}] or defeat all {(BossChecklist.tremorLoaded ? 5 : 4)} pillars. {(BossChecklist.tremorLoaded ? "[c/FF0000:Starts Tremode!]" : "")}";

			if (npcID == NPCID.Pumpking) return $"Spawns during Wave 7 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]";
			if (npcID == NPCID.MourningWood) return $"Spawns during Wave 4 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]";
			if (npcID == NPCID.IceQueen) return $"Spawns during Wave 11 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";
			if (npcID == NPCID.SantaNK1) return $"Spawns during Wave 7 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";
			if (npcID == NPCID.Everscream) return $"Spawns during Wave 4 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]";

			return "";
		}

		internal string SetupEventSpawnDesc(string eventName) {
			if (eventName == "Blood Moon") return $"Occurs randomly on at night. Can start when any player in the world has more than 100 health and the current moon phase is NOT a new moon.";
			if (eventName == "Goblin Army") return $"Has a 1 in 3 chance of occurring every dawn if at least one Shadow Orb/Crimson Heart has been destroyed, at least one player has 200 health or more, and there is not a Goblin Army already in progress. It can also be summoned manually using a [i:{ItemID.GoblinBattleStandard}]";
			if (eventName == "Old One's Army") return $"After finding the Tavernkeep, purchase and activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]";
			if (eventName == "Frost Legion") return $"Use a [i:{ItemID.SnowGlobe}], which can be found by opening [i:{ItemID.Present}], during the Christmas season.";
			if (eventName == "Solar Eclipse") return $"Has a 1/20 chance to occur each day upon dawn, as soon as any Mechanical Boss has been defeated. Alternatively, summon with a [i:{ItemID.SolarTablet}]";
			if (eventName == "Pirate Invasion") return $"Occurs randomly once at least one alter has been destroyed. Can be summoned with a [i:{ItemID.PirateMap}], which can be obtained from killing any enemy in the Ocean biome during Hardmode. ";
			if (eventName == "Pumpkin Moon") return $"Use a [i:{ItemID.PumpkinMoonMedallion}] at night";
			if (eventName == "Frost Moon") return $"Use a [i:{ItemID.NaughtyPresent}] at night";
			if (eventName == "Martian Madness") return $"After defeating Golem, find a Martian Probe above ground and allow it escape.";
			if (eventName == "Lunar Event") return $"To summon, kill the Lunatic Cultist outside the dungeon post-Golem";
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
				};
			}
			if (eventName == "Pumpkin Moon") {
				return new List<int>() {
					ItemID.MusicBoxPumpkinMoon,
				};
			}
			if (eventName == "Frost Moon") {
				return new List<int>() {
					ItemID.MusicBoxFrostMoon,
				};
			}
			if (eventName == "Martian Madness") {
				return new List<int>() {
					ItemID.MusicBoxMartians,
				};
			}
			if (eventName == "Lunar Event") {
				return new List<int>() {
					ItemID.MusicBoxTowers,
				};
			}
			return new List<int>();
		}

		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
		}

		// New system is better
		internal void AddBoss(float val, List<int> id, string source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string texture) {
			if (!ModContent.TextureExists(texture)) texture = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, val, source, name, id, down, null, spawn, SortCollectibles(collect), loot, texture, "No info provided"));
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("<<Boss Assist>> ");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write(source + " has added ");
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.Write(name);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write(" to the boss list!");
			Console.WriteLine();
			Console.ResetColor();
			if (BossChecklist.ServerCollectedRecords != null) {
				for (int i = 0; i < 255; i++) {
					BossChecklist.ServerCollectedRecords[i].Add(new BossStats());
				}
				// Adding a boss to each player
			}
		}

		internal void AddToBossLoot(string modName, string bossName, List<int> lootList) {
			ExtraData.Add(new OrphanInfo(OrphanType.Loot, modName, bossName, lootList));
		}

		internal void AddToBossCollection(string modName, string bossName, List<int> collectionList) {
			ExtraData.Add(new OrphanInfo(OrphanType.Collection, modName, bossName, collectionList));
		}

		internal void AddToBossSpawnItems(string modName, string bossName, List<int> spawnItems) {
			ExtraData.Add(new OrphanInfo(OrphanType.SpawnItem, modName, bossName, spawnItems));
		}

		internal List<int> SortCollectibles(List<int> collection) {
			// Sorts the Main 3 Collectibles
			List<int> SortedCollectibles = new List<int>();

			foreach (int item in collection) {
				Item i = new Item();
				i.SetDefaults(item);
				if (i.createTile > 0 && (i.Name.Contains("Trophy")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Trophy"))) {
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 0) SortedCollectibles.Add(-1); // No Trophy
			foreach (int item in collection) {
				Item i = new Item();
				i.SetDefaults(item);
				if (i.vanity && (i.Name.Contains("Mask")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Mask"))) {
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 1) SortedCollectibles.Add(-1); // No Mask
			foreach (int item in collection) {
				Item i = new Item();
				i.SetDefaults(item);
				if (i.createTile > 0 && (i.Name.Contains("Music Box")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Music Box"))) {
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 2) SortedCollectibles.Add(-1); // No Music Box
			foreach (int item in collection) {
				if (!SortedCollectibles.Contains(item)) SortedCollectibles.Add(item);
			}

			return SortedCollectibles;
		}
	}
}
