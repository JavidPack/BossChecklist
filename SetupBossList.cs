using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BossChecklist
{
    public class SetupBossList
    {
        internal List<BossInfo> SortedBosses;

        public SetupBossList()
        {
            InitList();
        }

        private void InitList()
        {
            SortedBosses = new List<BossInfo>
            {
                new BossInfo(1f, new List<int>() { NPCID.KingSlime }, "Vanilla", "King Slime", (() => NPC.downedSlimeKing), new List<int>() { ItemID.SlimeCrown }, SetupCollect(50), SetupLoot(50), "BossChecklist/Resources/BossTextures/Boss50"),
                new BossInfo(2f, new List<int>() { NPCID.EyeofCthulhu }, "Vanilla", "Eye of Cthulhu", (() => NPC.downedBoss1),  new List<int>() { ItemID.SuspiciousLookingEye }, SetupCollect(4), SetupLoot(4), "BossChecklist/Resources/BossTextures/Boss4"),
                new BossInfo(3f, new List<int>() { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail}, "Vanilla", "Eater of Worlds", (() => NPC.downedBoss2), new List<int>() { ItemID.WormFood }, SetupCollect(13), SetupLoot(13), "BossChecklist/Resources/BossTextures/Boss13"),
                new BossInfo(3f, new List<int>() { NPCID.BrainofCthulhu }, "Vanilla", "Brain of Cthulhu", (() => NPC.downedBoss2), new List<int>() { ItemID.BloodySpine }, SetupCollect(266), SetupLoot(266), "BossChecklist/Resources/BossTextures/Boss266"),
                new BossInfo(4f, new List<int>() { NPCID.QueenBee }, "Vanilla", "Queen Bee", (() => NPC.downedQueenBee), new List<int>() { ItemID.Abeemination }, SetupCollect(222), SetupLoot(222), "BossChecklist/Resources/BossTextures/Boss222"),
                new BossInfo(5f, new List<int>() { NPCID.SkeletronHead }, "Vanilla", "Skeletron", (() => NPC.downedBoss3), new List<int>() { ItemID.ClothierVoodooDoll }, SetupCollect(35), SetupLoot(35), "BossChecklist/Resources/BossTextures/Boss35"),
                new BossInfo(6f, new List<int>() { NPCID.WallofFlesh }, "Vanilla", "Wall of Flesh", (() => Main.hardMode), new List<int>() { ItemID.GuideVoodooDoll }, SetupCollect(113), SetupLoot(113), "BossChecklist/Resources/BossTextures/Boss113"),
                new BossInfo(7f, new List<int>() { NPCID.Retinazer, NPCID.Spazmatism }, "Vanilla", "The Twins", (() => NPC.downedMechBoss2), new List<int>() { ItemID.MechanicalEye }, SetupCollect(125), SetupLoot(125), "BossChecklist/Resources/BossTextures/Boss125"),
                new BossInfo(8f, new List<int>() { NPCID.TheDestroyer }, "Vanilla", "The Destroyer", (() => NPC.downedMechBoss1), new List<int>() { ItemID.MechanicalWorm }, SetupCollect(134), SetupLoot(134), "BossChecklist/Resources/BossTextures/Boss134"),
                new BossInfo(9f, new List<int>() { NPCID.SkeletronPrime }, "Vanilla", "Skeletron Prime", (() => NPC.downedMechBoss3), new List<int>() { ItemID.MechanicalSkull }, SetupCollect(127), SetupLoot(127), "BossChecklist/Resources/BossTextures/Boss127"),
                new BossInfo(10f, new List<int>() { NPCID.Plantera }, "Vanilla", "Plantera", (() => NPC.downedPlantBoss), new List<int>() { }, SetupCollect(262), SetupLoot(262), "BossChecklist/Resources/BossTextures/Boss262"),
                new BossInfo(11f, new List<int>() { NPCID.GolemHead, NPCID.Golem }, "Vanilla", "Golem", (() => NPC.downedGolemBoss), new List<int>() { ItemID.LihzahrdPowerCell },  SetupCollect(245), SetupLoot(245), "BossChecklist/Resources/BossTextures/Boss245"),
                new BossInfo(11.5f, new List<int>() { NPCID.DD2Betsy }, "Vanilla", "Betsy", (() => WorldAssist.downedBetsy), new List<int>() { ItemID.DD2ElderCrystal }, SetupCollect(551), SetupLoot(551), "BossChecklist/Resources/BossTextures/Boss551"),
                new BossInfo(12f, new List<int>() { NPCID.DukeFishron }, "Vanilla", "Duke Fishron", (() => NPC.downedFishron), new List<int>() { ItemID.TruffleWorm }, SetupCollect(370), SetupLoot(370), "BossChecklist/Resources/BossTextures/Boss370"),
                new BossInfo(13f, new List<int>() { NPCID.CultistBoss }, "Vanilla", "Lunatic Cultist", (() => NPC.downedAncientCultist), new List<int>() { }, SetupCollect(439), SetupLoot(439), "BossChecklist/Resources/BossTextures/Boss439"),
                new BossInfo(14f, new List<int>() { NPCID.MoonLordHead, NPCID.MoonLordCore, NPCID.MoonLordHand }, "Vanilla", "Moon Lord", (() => NPC.downedMoonlord), new List<int>() { ItemID.CelestialSigil }, SetupCollect(396), SetupLoot(396), "BossChecklist/Resources/BossTextures/Boss396")
            };
        }
        
		// New system is better
		internal void AddBoss(float val, List<int> id, string source, string name, Func<bool> down, List<int> spawn, List<int> collect, List<int> loot, string texture)
		{
			if (!ModContent.TextureExists(texture)) texture = "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina";
			SortedBosses.Add(new BossInfo(val, id, source, name, down, spawn, SortCollectibles(collect), loot, texture));
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
			if (BossChecklist.ServerCollectedRecords != null)
			{
				for (int i = 0; i < 255; i++)
				{
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

		internal List<int> SortCollectibles(List<int> collection)
		{
			// Sorts the Main 3 Collectibles
			List<int> SortedCollectibles = new List<int>();

			foreach (int item in collection)
			{
				Item i = new Item();
				i.SetDefaults(item);
				if (i.createTile > 0 && (i.Name.Contains("Trophy")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Trophy")))
				{
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 0) SortedCollectibles.Add(-1); // No Trophy
			foreach (int item in collection)
			{
				Item i = new Item();
				i.SetDefaults(item);
				if (i.vanity && (i.Name.Contains("Mask")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Mask")))
				{
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 1) SortedCollectibles.Add(-1); // No Mask
			foreach (int item in collection)
			{
				Item i = new Item();
				i.SetDefaults(item);
				if (i.createTile > 0 && (i.Name.Contains("Music Box")) || (i.type > ItemID.Count && i.modItem.Name.Contains("Music Box")))
				{
					SortedCollectibles.Add(item);
					break;
				}
			}
			if (SortedCollectibles.Count == 2) SortedCollectibles.Add(-1); // No Music Box
			foreach (int item in collection)
			{
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

		internal protected List<int> SetupLoot(int bossNum)
        {
            if (bossNum == NPCID.KingSlime)
            {
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
            if (bossNum == NPCID.EyeofCthulhu)
            {
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
            if (bossNum == NPCID.EaterofWorldsHead)
            {
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
            if (bossNum == NPCID.BrainofCthulhu)
            {
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
            if (bossNum == NPCID.QueenBee)
            {
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
            if (bossNum == NPCID.SkeletronHead)
            {
                return new List<int>()
                {
                    ItemID.SkeletronBossBag,
                    ItemID.BoneGlove,
                    ItemID.SkeletronHand,
                    ItemID.BookofSkulls,
                    ItemID.LesserHealingPotion
                };
            }
            if (bossNum == NPCID.WallofFlesh)
            {
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
            if (bossNum == NPCID.Retinazer)
            {
                return new List<int>()
                {
                    ItemID.TwinsBossBag,
                    ItemID.MechanicalWheelPiece,
                    ItemID.SoulofSight,
                    ItemID.HallowedBar,
                    ItemID.GreaterHealingPotion
                };
            }
            if (bossNum == NPCID.TheDestroyer)
            {
                return new List<int>()
                {
                    ItemID.DestroyerBossBag,
                    ItemID.MechanicalWagonPiece,
                    ItemID.SoulofMight,
                    ItemID.HallowedBar,
                    ItemID.GreaterHealingPotion
                };
            }
            if (bossNum == NPCID.SkeletronPrime)
            {
                return new List<int>()
                {
                    ItemID.SkeletronPrimeBossBag,
                    ItemID.MechanicalBatteryPiece,
                    ItemID.SoulofFright,
                    ItemID.HallowedBar,
                    ItemID.GreaterHealingPotion
                };
            }
            if (bossNum == NPCID.Plantera)
            {
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
            if (bossNum == NPCID.Golem)
            {
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
            if (bossNum == NPCID.DD2Betsy)
            {
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
            if (bossNum == NPCID.DukeFishron)
            {
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
            if (bossNum == NPCID.CultistBoss)
            {
                return new List<int>()
                {
                    ItemID.CultistBossBag,
                    ItemID.LunarCraftingStation,
                    ItemID.GreaterHealingPotion
                };
            }
            if (bossNum == NPCID.MoonLordHead)
            {
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

        internal protected List<int> SetupCollect(int bossNum)
        {
            if (bossNum == NPCID.KingSlime)
            {
                return new List<int>()
                {
                    ItemID.KingSlimeTrophy,
                    ItemID.KingSlimeMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.EyeofCthulhu)
            {
                return new List<int>()
                {
                    ItemID.EyeofCthulhuTrophy,
                    ItemID.EyeMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.EaterofWorldsHead)
            {
                return new List<int>()
                {
                    ItemID.EaterofWorldsTrophy,
                    ItemID.EaterMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.BrainofCthulhu)
            {
                return new List<int>()
                {
                    ItemID.BrainofCthulhuTrophy,
                    ItemID.BrainMask,
                    ItemID.MusicBoxBoss3
                };
            }
            if (bossNum == NPCID.QueenBee)
            {
                return new List<int>()
                {
                    ItemID.QueenBeeTrophy,
                    ItemID.BeeMask,
                    ItemID.MusicBoxBoss4
                };
            }
            if (bossNum == NPCID.SkeletronHead)
            {
                return new List<int>()
                {
                    ItemID.SkeletronTrophy,
                    ItemID.SkeletronMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.WallofFlesh)
            {
                return new List<int>()
                {
                    ItemID.WallofFleshTrophy,
                    ItemID.FleshMask,
                    ItemID.MusicBoxBoss2
                };
            }
            if (bossNum == NPCID.Retinazer)
            {
                return new List<int>()
                {
                    ItemID.RetinazerTrophy,
                    ItemID.SpazmatismTrophy,
                    ItemID.TwinMask,
                    ItemID.MusicBoxBoss2
                };
            }
            if (bossNum == NPCID.TheDestroyer)
            {
                return new List<int>()
                {
                    ItemID.DestroyerTrophy,
                    ItemID.DestroyerMask,
                    ItemID.MusicBoxBoss3
                };
            }
            if (bossNum == NPCID.SkeletronPrime)
            {
                return new List<int>()
                {
                    ItemID.SkeletronPrimeTrophy,
                    ItemID.SkeletronPrimeMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.Plantera)
            {
                return new List<int>()
                {
                    ItemID.PlanteraTrophy,
                    ItemID.PlanteraMask,
                    ItemID.MusicBoxPlantera
                };
            }
            if (bossNum == NPCID.Golem)
            {
                return new List<int>()
                {
                    ItemID.GolemTrophy,
                    ItemID.GolemMask,
                    ItemID.MusicBoxBoss5
                };
            }
            if (bossNum == NPCID.DD2Betsy)
            {
                return new List<int>()
                {
                    ItemID.BossTrophyBetsy,
                    ItemID.BossMaskBetsy,
                    ItemID.MusicBoxDD2
                };
            }
            if (bossNum == NPCID.DukeFishron)
            {
                return new List<int>()
                {
                    ItemID.DukeFishronTrophy,
                    ItemID.DukeFishronMask,
                    ItemID.MusicBoxBoss1
                };
            }
            if (bossNum == NPCID.CultistBoss)
            {
                return new List<int>()
                {
                    ItemID.AncientCultistTrophy,
                    ItemID.BossMaskCultist,
                    ItemID.MusicBoxBoss5
                };
            }
            if (bossNum == NPCID.MoonLordHead)
            {
                return new List<int>()
                {
                    ItemID.MoonLordTrophy,
                    ItemID.BossMaskMoonlord,
                    ItemID.MusicBoxLunarBoss
                };
            }
            return new List<int>();
        }
    }

    public class BossInfo
    {
        internal float progression;
		internal List<int> ids;
        internal string source;
        internal string name;
        internal Func<bool> downed;

        internal List<int> spawnItem;
        internal List<int> loot;
        internal List<int> collection;

        internal string pageTexture;
		
		internal BossInfo(float progression, List<int> ids, string source, string name, Func<bool> downed, List<int> spawnItem, List<int> collection, List<int> loot, string pageTexture = null)
		{
			this.progression = progression;
			this.ids = ids;
			this.source = source;
			this.name = name;
			this.downed = downed;
			this.spawnItem = spawnItem;
			this.collection = collection;
			this.loot = loot;
			this.pageTexture = pageTexture;
		}
	}
}