using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace BossChecklist.UI
{
	internal enum BossChecklistType
	{
		Boss,
		MiniBoss,
		Event
	}

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

		internal List<BossInfo> allBosses;

		public BossTracker()
		{
			InitializeVanillaBosses();
		}

		private void InitializeVanillaBosses()
		{
			allBosses = new List<BossInfo> {
			// Bosses -- Vanilla
			new BossInfo(BossChecklistType.Boss, "King Slime", SlimeKing, () => true, () => NPC.downedSlimeKing, $"Use [i:{ItemID.SlimeCrown}], randomly in outer 3rds of map, or kill 150 slimes during slime rain."),
			new BossInfo(BossChecklistType.Boss, "Eye of Cthulhu", EyeOfCthulhu, () => true, () => NPC.downedBoss1,  $"Use [i:{ItemID.SuspiciousLookingEye}] at night, or 1/3 chance nightly if over 200 HP\nAchievement : [a:EYE_ON_YOU]"),
			new BossInfo(BossChecklistType.Boss, "Eater of Worlds / Brain of Cthulhu", EaterOfWorlds, () => true, () => NPC.downedBoss2,  $"Use [i:{ItemID.WormFood}] or [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts or Shadow Orbs"),
			new BossInfo(BossChecklistType.Boss, "Queen Bee", QueenBee, () => true, () => NPC.downedQueenBee,  $"Use [i:{ItemID.Abeemination}] or break Larva in Jungle"),
			new BossInfo(BossChecklistType.Boss, "Skeletron", Skeletron, () => true, () => NPC.downedBoss3,  $"Visit dungeon or use [i:{ItemID.ClothierVoodooDoll}] at night"),
			new BossInfo(BossChecklistType.Boss, "Wall of Flesh", WallOfFlesh, () => true, () => Main.hardMode  ,  $"Spawn by throwing [i:{ItemID.GuideVoodooDoll}] in lava in the Underworld. [c/FF0000:Starts Hardmode!]"),
			new BossInfo(BossChecklistType.Boss, "The Twins", TheTwins, () => true, () => NPC.downedMechBoss2,  $"Use [i:{ItemID.MechanicalEye}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "The Destroyer",TheDestroyer, () => true, () => NPC.downedMechBoss1,  $"Use [i:{ItemID.MechanicalWorm}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "Skeletron Prime", SkeletronPrime, () => true, () => NPC.downedMechBoss3,  $"Use [i:{ItemID.MechanicalSkull}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "Plantera", Plantera, () => true, () => NPC.downedPlantBoss,  $"Break a Plantera's Bulb in jungle after 3 Mechanical bosses have been defeated"),
			new BossInfo(BossChecklistType.Boss, "Golem", Golem, () => true, () => NPC.downedGolemBoss,  $"Use [i:{ItemID.LihzahrdPowerCell}] on Lihzahrd Altar"),
			new BossInfo(BossChecklistType.Boss, "Duke Fishron", DukeFishron, () => true, () => NPC.downedFishron,  $"Fish in ocean using the [i:{ItemID.TruffleWorm}] bait"),
			new BossInfo(BossChecklistType.Boss, "Lunatic Cultist", LunaticCultist, () => true, () => NPC.downedAncientCultist,  $"Kill the cultists outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Boss, "Moonlord", Moonlord, () => true, () => NPC.downedMoonlord,  $"Use [i:{ItemID.CelestialSigil}] or defeat all {(BossChecklist.tremorLoaded ? 5 : 4)} pillars. {(BossChecklist.tremorLoaded ? "[c/FF0000:Starts Tremode!]" : "")}"),
			// Event Bosses -- Vanilla
			new BossInfo(BossChecklistType.Event, "Nebula Pillar", LunaticCultist + .1f, () => true, () => NPC.downedTowerNebula,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Vortex Pillar", LunaticCultist + .2f, () => true, () => NPC.downedTowerVortex,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Solar Pillar", LunaticCultist +.3f, () => true, () => NPC.downedTowerSolar,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Stardust Pillar", LunaticCultist + .4f, () => true, () => NPC.downedTowerStardust,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			// TODO, all other event bosses...Maybe all pillars as 1?
			new BossInfo(BossChecklistType.MiniBoss, "Clown", WallOfFlesh + 0.1f, () => true, () => NPC.downedClown,  $"Spawns during Hardmode Bloodmoon"),
			new BossInfo(BossChecklistType.Event, "Goblin Army", EyeOfCthulhu + 0.5f, () => true, () => NPC.downedGoblins,  $"Occurs randomly at dawn once a Shadow Orb or Crimson Heart has been destroyed. Alternatively, spawn with [i:{ItemID.GoblinBattleStandard}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Ice Queen", Plantera + 0.9f, () => true, () => NPC.downedChristmasIceQueen,  $"Spawns during Wave 11 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Santa-NK1", Plantera + 0.6f, () => true, () => NPC.downedChristmasSantank,  $"Spawns during Wave 7 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Everscream", Plantera + 0.3f, () => true, () => NPC.downedChristmasTree,  $"Spawns during Wave 4 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.Event, "Frost Legion", WallOfFlesh + 0.6f, () => true, () => NPC.downedFrost,  $"Use [i:{ItemID.SnowGlobe}] to start. Find [i:{ItemID.SnowGlobe}] by opening [i:{ItemID.Present}] while in Hardmode during Christmas season."),
			new BossInfo(BossChecklistType.MiniBoss, "Pumpking", Plantera + 0.3f, () => true, () => NPC.downedHalloweenKing,  $"Spawns during Wave 7 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Mourning Wood", Plantera + 0.6f, () => true, () => NPC.downedHalloweenTree,  $"Spawns during Wave 4 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
			new BossInfo(BossChecklistType.Event, "Martian Madness", Golem + 0.4f, () => true, () => NPC.downedMartians,  $"After defeating Golem, find a Martian Probe above ground and let it escape."),
			new BossInfo(BossChecklistType.Event, "Pirate Invasion", WallOfFlesh + 0.7f, () => true, () => NPC.downedPirates,  $"Occurs randomly in Hardmode after an Altar has been destroyed. Alternatively, spawn with [i:{ItemID.PirateMap}]"),
			new BossInfo(BossChecklistType.Event, "Old One's Army Any Tier", EaterOfWorlds + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionAnyDifficulty,  $"After finding the Tavernkeep, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			//new BossInfo(BossChecklistType.Event, "Old One's Army 1", EaterOfWorlds + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1,  $"After finding the Tavernkeep, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			//new BossInfo(BossChecklistType.Event, "Old One's Army 2", TheTwins + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT2,  $"After defeating a mechanical boss, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			//new BossInfo(BossChecklistType.Event, "Old One's Army 3", Golem + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT3,  $"After defeating Golem, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			};
		}

		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.Boss, bossname, bossValue, available, bossDowned, bossInfo));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.MiniBoss, bossname, bossValue, available, bossDowned, bossInfo));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null, Func<bool> available = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.Event, bossname, bossValue, available, bossDowned, bossInfo));
		}
	}

	internal class BossInfo
	{
		internal Func<bool> available;
		internal bool hidden;
		internal Func<bool> downed;
		internal string name;
		internal float progression;
		//internal int spawnItemID;
		internal string info;
		internal BossChecklistType type;

		internal BossInfo(BossChecklistType type, string name, float progression, Func<bool> available, Func<bool> downed, string info = null)
		{
			this.type = type;
			this.name = name;
			this.progression = progression;
			this.available = available ?? (() => true);
			this.downed = downed;
			this.info = info;
			this.hidden = false;
		}
	}
}
