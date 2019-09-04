using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

// Bug: ItemLists for loot and collections added in with add loot/collect calls do not get added to saved data

namespace BossChecklist
{
	public class PlayerAssist : ModPlayer
	{
		public bool isNewPlayer = true;

		public List<BossRecord> AllBossRecords;
		public List<BossCollection> BossTrophies;

		public List<int> RecordTimers;
		public List<int> BrinkChecker;
		public List<int> MaxHealth;
		public List<int> DeathTracker;
		public List<int> DodgeTimer; // Track the time in-between hits
		public List<int> AttackCounter;

		public override void Initialize() {
			AllBossRecords = new List<BossRecord>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				AllBossRecords.Add(new BossRecord(boss.source, boss.name));
			}

			// Make a new list of collections
			BossTrophies = new List<BossCollection>();
			// For each boss added...
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				// 1.) Add a collection for the boss
				BossTrophies.Add(new BossCollection(boss.source, boss.name));
				// 2.) setup the item list and check off list for the boss
				int index = BossTrophies.FindIndex(x => x.modName == boss.source && x.bossName == boss.name);
				BossTrophies[index].loot = new List<Item>();
				BossTrophies[index].collectibles = new List<Item>();
			}

			// For being able to complete records in Multiplayer
			RecordTimers = new List<int>();
			int bossCount = BossChecklist.bossTracker.SortedBosses.Count;
			BrinkChecker = new List<int>(bossCount);
			MaxHealth = new List<int>(bossCount);
			DeathTracker = new List<int>(bossCount);
			DodgeTimer = new List<int>(bossCount);
			AttackCounter = new List<int>(bossCount);

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				RecordTimers.Add(0);
				BrinkChecker.Add(0);
				MaxHealth.Add(0);
				DeathTracker.Add(0);
				DodgeTimer.Add(0);
				AttackCounter.Add(0);
			}
		}

		public override TagCompound Save() {
			TagCompound saveData = new TagCompound
			{
				{ "Records", AllBossRecords },
				{ "Collection", BossTrophies },
				{ "NewPlayer", isNewPlayer }
			};
			return saveData;
		}

		public override void Load(TagCompound tag) {
			// Add new bosses to the list, and place existing ones accordingly
			List<BossRecord> TempRecordStorage = tag.Get<List<BossRecord>>("Records");
			foreach (BossRecord record in TempRecordStorage) {
				int index = AllBossRecords.FindIndex(x => x.modName == record.modName && x.bossName == record.bossName);
				if (index == -1) AllBossRecords.Add(record);
				else AllBossRecords[index] = record;
			}

			// Prepare the collections for the player. Putting unloaded bosses in the back and new/existing ones up front
			List<BossCollection> TempCollectionStorage = tag.Get<List<BossCollection>>("Collection");

			List<BossCollection> AddedCollections = new List<BossCollection>();
			foreach (BossCollection collection in TempCollectionStorage) {
				int index = BossTrophies.FindIndex(x => x.modName == collection.modName && x.bossName == collection.bossName);
				if (index == -1) BossTrophies.Add(collection);
				else BossTrophies[index] = collection;
			}
			isNewPlayer = tag.Get<bool>("NewPlayer");
		}

		public override void clientClone(ModPlayer clientClone) {
			PlayerAssist clone = clientClone as PlayerAssist;
			clone.BossTrophies = BossTrophies;
			clone.AllBossRecords = AllBossRecords;
		}

		public static PlayerAssist Get(Player player, Mod mod) {
			return player.GetModPlayer<PlayerAssist>(mod);
		}

		public override void OnRespawn(Player player) {
			// This works both in SP and MP because it records within ModPlayer
			for (int i = 0; i < DeathTracker.Count; i++) {
				if (DeathTracker[i] == 1) AllBossRecords[i].stat.deaths++;
				DeathTracker[i] = 0;
			}
		}

		public override void OnEnterWorld(Player player) {
			int bossCount = BossChecklist.bossTracker.SortedBosses.Count;
			RecordTimers = new List<int>(bossCount);
			BrinkChecker = new List<int>(bossCount);
			MaxHealth = new List<int>(bossCount);
			DeathTracker = new List<int>(bossCount);
			DodgeTimer = new List<int>(bossCount);
			AttackCounter = new List<int>(bossCount);

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				RecordTimers.Add(0);
				BrinkChecker.Add(0);
				MaxHealth.Add(0);
				DeathTracker.Add(0);
				DodgeTimer.Add(0);
				AttackCounter.Add(0);
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Essentially to get "BossAssist.ServerCollectedRecords[player.whoAmI] = AllBossRecords;"
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)BossChecklistMessageType.SendRecordsToServer);
				for (int i = 0; i < bossCount; i++) {
					BossStats stat = AllBossRecords[i].stat;
					packet.Write(stat.kills);
					packet.Write(stat.deaths);
					packet.Write(stat.fightTime);
					packet.Write(stat.fightTime2);
					packet.Write(stat.brink2);
					packet.Write(stat.brink);
					packet.Write(stat.totalDodges);
					packet.Write(stat.totalDodges2);
					packet.Write(stat.dodgeTime);
				}
				packet.Send(); // To server
			}
			/*
            if (isNewPlayer)
            {
                // This wont work in MP, but ill fix that later
                CombatText.NewText(player.getRect(), Color.LightGreen, "Thanks for playing with Shepherd's mods!!", true);
                isNewPlayer = false;
            }
			*/
			MapAssist.shouldDraw = false;
			MapAssist.tilePos = new Vector2(0, 0);
		}

		public override void OnHitByNPC(NPC npc, int damage, bool crit) {
			for (int i = 0; i < Main.maxNPCs; i++) {
				if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) continue;
				AttackCounter[NPCAssist.ListedBossNum(Main.npc[i])]++;
				DodgeTimer[NPCAssist.ListedBossNum(Main.npc[i])] = 0;
			}
		}

		public override void OnHitByProjectile(Projectile proj, int damage, bool crit) {
			for (int i = 0; i < Main.maxNPCs; i++) {
				if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) continue;
				AttackCounter[NPCAssist.ListedBossNum(Main.npc[i])]++;
				DodgeTimer[NPCAssist.ListedBossNum(Main.npc[i])] = 0;
			}
		}
	}
}