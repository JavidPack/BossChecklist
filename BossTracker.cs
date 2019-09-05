using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BossChecklist
{
	internal class BossTracker
	{
		public const float SlimeKing = 1f;
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

		// TODO: OrphanBosses: Boss info added to other bosses when those bosses aren't loaded yet. Log remaining orphans maybe after load.

		public BossTracker() {
			BossChecklist.bossTracker = this;
			InitializeVanillaBosses();
		}

		private void InitializeVanillaBosses() {
			SortedBosses = new List<BossInfo> {
			// Bosses -- Vanilla
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, SlimeKing, "King Slime", new List<int>() { NPCID.KingSlime },
			 () => NPC.downedSlimeKing, new List<int>() { ItemID.SlimeCrown }, $"Use [i:{ItemID.SlimeCrown}], randomly in outer 3rds of map, or kill 150 slimes during slime rain."),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EyeOfCthulhu, "Eye of Cthulhu", new List<int>() { NPCID.EyeofCthulhu }, 
			 () => NPC.downedBoss1, new List<int>() { ItemID.SuspiciousLookingEye }, $"Use [i:{ItemID.SuspiciousLookingEye}] at night, or 1/3 chance nightly if over 200 HP\nAchievement : [a:EYE_ON_YOU]"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EaterOfWorlds,"Eater of Worlds",new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail},
			 () => NPC.downedBoss2,new List<int>() { ItemID.WormFood }, $"Use [i:{ItemID.WormFood}] or [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts or Shadow Orbs"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, EaterOfWorlds, "Brain of Cthulhu",new List<int>() { NPCID.BrainofCthulhu },
			 () => NPC.downedBoss2, new List<int>() { ItemID.BloodySpine }, $"Use [i:{ItemID.WormFood}] or [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts or Shadow Orbs"), // FIX
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, QueenBee, "Queen Bee", new List<int>() { NPCID.QueenBee },
			 () => NPC.downedQueenBee, new List<int>() { ItemID.Abeemination }, $"Use [i:{ItemID.Abeemination}] or break Larva in Jungle"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Skeletron, "Skeletron", new List<int>() { NPCID.SkeletronHead },
			 () => NPC.downedBoss3, new List<int>() { ItemID.ClothierVoodooDoll }, $"Visit dungeon or use [i:{ItemID.ClothierVoodooDoll}] at night"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, WallOfFlesh, "Wall of Flesh", new List<int>() { NPCID.WallofFlesh },
			 () => Main.hardMode  , new List<int>() { ItemID.GuideVoodooDoll }, $"Spawn by throwing [i:{ItemID.GuideVoodooDoll}] in lava in the Underworld. [c/FF0000:Starts Hardmode!]"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, TheTwins, "The Twins", new List<int>() { NPCID.Retinazer, NPCID.Spazmatism },
			 () => NPC.downedMechBoss2,new List<int>() { ItemID.MechanicalEye },  $"Use [i:{ItemID.MechanicalEye}] at night to spawn"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, TheDestroyer, "The Destroyer",new List<int>() { NPCID.TheDestroyer },
			 () => NPC.downedMechBoss1,  new List<int>() { ItemID.MechanicalWorm }, $"Use [i:{ItemID.MechanicalWorm}] at night to spawn"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, SkeletronPrime, "Skeletron Prime", new List<int>() { NPCID.SkeletronPrime },
			 () => NPC.downedMechBoss3,new List<int>() { ItemID.MechanicalSkull }, $"Use [i:{ItemID.MechanicalSkull}] at night to spawn"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Plantera, "Plantera", new List<int>() { NPCID.Plantera },
			 () => NPC.downedPlantBoss, new List<int>() { }, $"Break a Plantera's Bulb in jungle after 3 Mechanical bosses have been defeated"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Golem, "Golem", new List<int>() { NPCID.Golem, NPCID.GolemHead },
			 () => NPC.downedGolemBoss,  new List<int>() { ItemID.LihzahrdPowerCell }, $"Use [i:{ItemID.LihzahrdPowerCell}] on Lihzahrd Altar"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Golem + 0.5f, "Betsy", new List<int>() { NPCID.DD2Betsy },
			 () => WorldAssist.downedBetsy, new List<int>() { ItemID.DD2ElderCrystal }, "Fight during Old One's Army Tier 3"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, DukeFishron, "Duke Fishron", new List<int>() { NPCID.DukeFishron },
			 () => NPC.downedFishron, new List<int>() { ItemID.TruffleWorm }, $"Fish in ocean using the [i:{ItemID.TruffleWorm}] bait"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, LunaticCultist ,"Lunatic Cultist", new List<int>() { NPCID.CultistBoss },
			 () => NPC.downedAncientCultist, new List<int>() { }, $"Kill the cultists outside the dungeon post-Golem"),
			BossInfo.MakeVanillaBoss(BossChecklistType.Boss, Moonlord, "Moonlord", new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand },
			 () => NPC.downedMoonlord, new List<int>() { ItemID.CelestialSigil }, $"Use [i:{ItemID.CelestialSigil}] or defeat all {(BossChecklist.tremorLoaded ? 5 : 4)} pillars. {(BossChecklist.tremorLoaded ? "[c/FF0000:Starts Tremode!]" : "")}"),
			// Mini Bosses -- Vanilla
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.3f,"Pumpking", new List<int>() {NPCID.Pumpking },
			 () => NPC.downedHalloweenKing, new List<int>() { }, $"Spawns during Wave 7 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.6f,"Mourning Wood", new List<int>() {NPCID.MourningWood },
			 () => NPC.downedHalloweenTree, new List<int>() { }, $"Spawns during Wave 4 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
				// BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,WallOfFlesh + 0.1f, "Clown", new List<int>() { NPCID.Clown}, () => NPC.downedClown, new List<int>() { }, $"Spawns during Hardmode Bloodmoon"),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.9f, "Ice Queen", new List<int>() {NPCID.IceQueen },
			 () => NPC.downedChristmasIceQueen, new List<int>() { }, $"Spawns during Wave 11 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss, Plantera + 0.6f,"Santa-NK1", new List<int>() { NPCID.SantaNK1},
			 () => NPC.downedChristmasSantank, new List<int>() { }, $"Spawns during Wave 7 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			BossInfo.MakeVanillaBoss(BossChecklistType.MiniBoss,  Plantera + 0.3f,"Everscream", new List<int>() {NPCID.Everscream },
			 () => NPC.downedChristmasTree, new List<int>() { }, $"Spawns during Wave 4 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			// Events -- Vanilla
			BossInfo.MakeVanillaEvent(LunaticCultist + 0.03f, "Nebula Pillar",
			 () => NPC.downedTowerNebula, SetupEventNPCList("Nebula Pillar"), new List<int>() { }, SetupEventLoot("Lunar Pillar"), $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			BossInfo.MakeVanillaEvent(LunaticCultist + 0.02f, "Vortex Pillar",
			 () => NPC.downedTowerVortex, SetupEventNPCList("Vortex Pillar"), new List<int>() { }, SetupEventLoot("Lunar Event"), $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			BossInfo.MakeVanillaEvent( LunaticCultist + 0.01f,"Solar Pillar",
			 () => NPC.downedTowerSolar, SetupEventNPCList("Solar Pillar"), new List<int>() { }, SetupEventLoot("Lunar Event"), $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			BossInfo.MakeVanillaEvent(LunaticCultist + 0.04f, "Stardust Pillar",
			 () => NPC.downedTowerStardust, SetupEventNPCList("Stardust Pillar"), new List<int>() { }, SetupEventLoot("Lunar Event"), $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
				// TODO, all other event bosses...Maybe all pillars as 1?
			BossInfo.MakeVanillaEvent(EyeOfCthulhu + 0.5f,"Goblin Army", 
			 () => NPC.downedGoblins, SetupEventNPCList("Goblin Army"), new List<int>() {ItemID.GoblinBattleStandard }, SetupEventLoot("Goblin Army"), $"Occurs randomly at dawn once a Shadow Orb or Crimson Heart has been destroyed. Alternatively, spawn with [i:{ItemID.GoblinBattleStandard}]", "BossChecklist/Resources/BossTextures/EventGoblinArmy"),
			BossInfo.MakeVanillaEvent( WallOfFlesh + 0.6f,"Frost Legion",
			 () => NPC.downedFrost, SetupEventNPCList("Frost Legion"), new List<int>() { ItemID.SnowGlobe}, SetupEventLoot("Frost Legion"), $"Use [i:{ItemID.SnowGlobe}] to start. Find [i:{ItemID.SnowGlobe}] by opening [i:{ItemID.Present}] while in Hardmode during Christmas season."),
			BossInfo.MakeVanillaEvent( Golem + 0.4f,"Martian Madness",
			 () => NPC.downedMartians, SetupEventNPCList("Martian Madness"), new List<int>() { }, SetupEventLoot("Martian Madness"), $"After defeating Golem, find a Martian Probe above ground and let it escape."),
			BossInfo.MakeVanillaEvent( WallOfFlesh + 0.7f,"Pirate Invasion",
			 () => NPC.downedPirates, SetupEventNPCList("Pirate Invasion"), new List<int>() { ItemID.PirateMap }, SetupEventLoot("Pirate Invasion"), $"Occurs randomly in Hardmode after an Altar has been destroyed. Alternatively, spawn with [i:{ItemID.PirateMap}]"),
			BossInfo.MakeVanillaEvent( EaterOfWorlds + 0.5f,"Old One's Army",
			 () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1 || Terraria.GameContent.Events.DD2Event.DownedInvasionT2 || Terraria.GameContent.Events.DD2Event.DownedInvasionT3, SetupEventNPCList("Old One's Army"), new List<int>() {ItemID.DD2ElderCrystal, ItemID.DD2ElderCrystalStand }, SetupEventLoot("Old One's Army"), $"After finding the Tavernkeep, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]", "BossChecklist/Resources/BossTextures/EventDD2"), // () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty
				// TODO: track bugged DownedInvasionT1 event separately from vanilla somehow.
			//new BossInfo(BossChecklistType.Event, "Old One's Army 1", EaterOfWorlds + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1,  $"After finding the Tavernkeep, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			//new BossInfo(BossChecklistType.Event, "Old One's Army 2", TheTwins + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT2,  $"After defeating a mechanical boss, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			//new BossInfo(BossChecklistType.Event, "Old One's Army 3", Golem + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT3,  $"After defeating Golem, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			BossInfo.MakeVanillaEvent( EyeOfCthulhu + 0.2f,"Blood Moon",
			 () => true, SetupEventNPCList("Blood Moon"), new List<int>() {0}, SetupEventLoot("Blood Moon"), $"Randomly occurs on at night, where the moon is not a new moon", "BossChecklist/Resources/BossTextures/EventBloodMoon"),
			BossInfo.MakeVanillaEvent( Plantera + 0.1f,"Pumpkin Moon",
			 () => true, SetupEventNPCList("Pumpkin Moon"), new List<int>() {ItemID.PumpkinMoonMedallion}, SetupEventLoot("Pumpkin Moon"), $"Pumpkin medalion, that is all", "BossChecklist/Resources/BossTextures/EventPumpkinMoon"),
			BossInfo.MakeVanillaEvent( Plantera + 0.1f,"Frost Moon",
			 () => true, SetupEventNPCList("Frost Moon"), new List<int>() {ItemID.NaughtyPresent}, SetupEventLoot("Frost Moon"), $"Something medalion, that is all", "BossChecklist/Resources/BossTextures/EventFrostMoon"),
			};
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
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
			if (eventName == "Pumpking Moon") {
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
			if (eventName == "Solar Pillar") {
				return new List<int>()
				{
					NPCID.SolarSolenian,
					NPCID.SolarSpearman,
					NPCID.SolarCorite,
					NPCID.SolarSroller,
					NPCID.SolarCrawltipedeHead,
					NPCID.SolarDrakomire,
					NPCID.SolarDrakomireRider,
				};
			}
			if (eventName == "Vortex Pillar") {
				return new List<int>()
				{
					NPCID.VortexHornet,
					NPCID.VortexHornetQueen,
					NPCID.VortexLarva,
					NPCID.VortexRifleman,
					NPCID.VortexSoldier,
				};
			}
			if (eventName == "Nebula Pillar") {
				return new List<int>()
				{
					NPCID.NebulaBeast,
					NPCID.NebulaBrain,
					NPCID.NebulaHeadcrab,
					NPCID.NebulaSoldier,
				};
			}
			if (eventName == "Stardust Pillar") {
				return new List<int>()
				{
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
					ItemID.BetsyWings,
					ItemID.DD2BetsyBow, // Aerial Bane
                    ItemID.MonkStaffT3, // Sky Dragon's Fury
                    ItemID.ApprenticeStaffT3, // Betsy's Wrath
                    ItemID.DD2SquireBetsySword, // Flying Dragon
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

		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null) {
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, bossValue, "Unknown", bossname, new List<int>(), bossDowned, available, new List<int>(), new List<int>(), new List<int>(), null, bossInfo));
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
		}

		// New system is better
		internal void AddBoss(float val, List<int> id, string source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string texture) {
			if (!ModContent.TextureExists(texture)) texture = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			SortedBosses.Add(new BossInfo(BossChecklistType.Boss, val, source, name, id, down, null, spawn, SortCollectibles(collect), loot, texture, "No info provided"));
			SortedBosses.Sort((x, y) => x.progression.CompareTo(y.progression));
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
		/*
	Didnt work out as I had hoped, not completely sure why, but probably not even necessary

internal List<int> BagFirst(List<int> loot)
{
	int reserve = 0;
	foreach (int item in loot)
	{
		Item i = new Item();
		i.SetDefaults(item);
		if (i.expert || i.Name.Contains("Treasure Bag") || (i.type > ItemID.Count && i.modItem.Name.Contains("Treasure Bag")))
		{
			reserve = item;
			break;
		}
	}
	if (reserve != 0)
	{
		loot.Remove(reserve);
		loot.Insert(reserve, 0);
	}

	return loot;
}
*/

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
		/*

		internal void AddSpawnItem(int bType, string bSource, List<int> bLoot)
		{
			int index = SortedBosses.FindIndex(x => x.id == bType && x.source == bSource);
			if (index != -1)
			{
				foreach (int item in bLoot)
				{
					SortedBosses[index].loot.Add(item);
				}
			}
		}

		internal void AddToLootTable(int bType, string bSource, List<int> bLoot)
        {
            int index = SortedBosses.FindIndex(x => x.id == bType && x.source == bSource);
            if (index != -1)
            {
                foreach (int item in bLoot)
                {
                    SortedBosses[index].loot.Add(item);
                }
            }
        }

        internal void AddToCollection(int bType, string bSource, List<int> bCollect)
        {
            int index = SortedBosses.FindIndex(x => x.id == bType && x.source == bSource);
            if (index != -1)
            {
                foreach (int item in bCollect)
                {
                    SortedBosses[index].collection.Add(item);
                }
            }
        }
		*/
	}
}
