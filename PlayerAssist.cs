using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class PlayerAssist : ModPlayer
	{
		public bool hasOpenedTheBossLog;
		public List<bool> hasNewRecord;

		public List<BossRecord> AllBossRecords;
		public List<BossCollection> BossTrophies;

		public int durationLastFight;
		public int hitsTakenLastFight;
		public int healthLossLastFight;

		public List<int> RecordTimers;
		public List<int> BrinkChecker;
		public List<int> MaxHealth;
		public List<bool> DeathTracker;
		public List<int> DodgeTimer;
		public List<int> AttackCounter;

		public override void Initialize() {
			hasOpenedTheBossLog = false;

			AllBossRecords = new List<BossRecord>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				AllBossRecords.Add(new BossRecord(boss.Key));
			}

			// Make a new list of collections
			BossTrophies = new List<BossCollection>();
			// For each boss added...
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				// 1.) Add a collection for the boss
				BossTrophies.Add(new BossCollection(boss.Key));
				// 2.) setup the item list and check off list for the boss
				int index = BossTrophies.FindIndex(x => x.bossName == boss.Key);
				BossTrophies[index].loot = new List<ItemDefinition>();
				BossTrophies[index].collectibles = new List<ItemDefinition>();
			}

			// This will be the attempt records of the players last fight (Not saved!)
			// This is only used for the UI, to determine whether the PrevRecord is a "last attempt" or a "beaten record"
			durationLastFight = hitsTakenLastFight = healthLossLastFight = -1;

			// For being able to complete records in Multiplayer
			RecordTimers = new List<int>();
			BrinkChecker = new List<int>();
			MaxHealth = new List<int>();
			DeathTracker = new List<bool>();
			DodgeTimer = new List<int>();
			AttackCounter = new List<int>();
			hasNewRecord = new List<bool>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				RecordTimers.Add(0);
				BrinkChecker.Add(0);
				MaxHealth.Add(0);
				DeathTracker.Add(false);
				DodgeTimer.Add(0);
				AttackCounter.Add(0);
				hasNewRecord.Add(false);
			}
		}

		public override TagCompound Save() {
			TagCompound saveData = new TagCompound
			{
				{ "BossLogPrompt", hasOpenedTheBossLog },
				{ "Records", AllBossRecords },
				{ "Collection", BossTrophies }
			};
			return saveData;
		}

		public override void Load(TagCompound tag) {
			// Add new bosses to the list, and place existing ones accordingly
			List<BossRecord> TempRecordStorage = tag.Get<List<BossRecord>>("Records");
			foreach (BossRecord record in TempRecordStorage) {
				int index = AllBossRecords.FindIndex(x => x.bossName == record.bossName);
				if (index == -1) AllBossRecords.Add(record);
				else AllBossRecords[index] = record;
			}

			// Prepare the collections for the player. Putting unloaded bosses in the back and new/existing ones up front
			List<BossCollection> TempCollectionStorage = tag.Get<List<BossCollection>>("Collection");

			List<BossCollection> AddedCollections = new List<BossCollection>();
			foreach (BossCollection collection in TempCollectionStorage) {
				int index = BossTrophies.FindIndex(x => x.bossName == collection.bossName);
				if (index == -1) BossTrophies.Add(collection);
				else BossTrophies[index] = collection;
			}

			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt");
		}

		public override void clientClone(ModPlayer clientClone) {
			PlayerAssist clone = clientClone as PlayerAssist;
			clone.hasOpenedTheBossLog = hasOpenedTheBossLog;
			clone.BossTrophies = BossTrophies;
			clone.AllBossRecords = AllBossRecords;
		}

		public override void OnEnterWorld(Player player) {
			BossLogUI.PageNum = -3;
			durationLastFight = hitsTakenLastFight = healthLossLastFight = -1;
			RecordTimers = new List<int>();
			BrinkChecker = new List<int>();
			MaxHealth = new List<int>();
			DeathTracker = new List<bool>();
			DodgeTimer = new List<int>();
			AttackCounter = new List<int>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				RecordTimers.Add(0);
				BrinkChecker.Add(0);
				MaxHealth.Add(0);
				DeathTracker.Add(false);
				DodgeTimer.Add(0);
				AttackCounter.Add(0);
			}

			int bossCount = BossChecklist.bossTracker.SortedBosses.Count;
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Essentially to get "BossAssist.ServerCollectedRecords[player.whoAmI] = AllBossRecords;"
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.SendRecordsToServer);
				for (int i = 0; i < bossCount; i++) {
					BossStats stat = AllBossRecords[i].stat;
					packet.Write(stat.kills);
					packet.Write(stat.deaths);
					packet.Write(stat.durationBest);
					packet.Write(stat.durationPrev);
					packet.Write(stat.hitsTakenBest);
					packet.Write(stat.hitsTakenPrev);
					packet.Write(stat.dodgeTimeBest);
					packet.Write(stat.healthLossBest);
					packet.Write(stat.healthLossPrev);
				}
				packet.Send(); // To server
			}
		}

		public override void OnRespawn(Player player) {
			// This works both in SP and MP because it records within ModPlayer
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
				for (int i = 0; i < DeathTracker.Count; i++) {
					if (DeathTracker[i]) AllBossRecords[i].stat.deaths++;
					DeathTracker[i] = false;
				}
			}
		}

		public override void OnHitByNPC(NPC npc, int damage, bool crit) {
			/*
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) continue;
					AttackCounter[NPCAssist.ListedBossNum(Main.npc[i])]++;
					DodgeTimer[NPCAssist.ListedBossNum(Main.npc[i])] = 0;
				}
			}
			*/
		}

		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) continue;
					int listNum = NPCAssist.ListedBossNum(Main.npc[i]);
					if (BrinkChecker[listNum] == 0) BrinkChecker[listNum] = player.statLife;
					AttackCounter[listNum]++;
					DodgeTimer[listNum] = 0;
				}
			}
		}

		public override void OnHitByProjectile(Projectile proj, int damage, bool crit) {
			/*
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) continue;
					AttackCounter[NPCAssist.ListedBossNum(Main.npc[i])]++;
					DodgeTimer[NPCAssist.ListedBossNum(Main.npc[i])] = 0;
				}
			}
			*/
		}

		public override void PreUpdate() {
			/* Previous bug? debug stuff
			for (int listNum = 0; listNum < BossChecklist.bossTracker.SortedBosses.Count; listNum++) {
				if (AllBossRecords[listNum].stat.healthLossBest == 0) {
					AllBossRecords[listNum].stat.healthLossBest = -1;
					AllBossRecords[listNum].stat.healthAtStart = -1;
					AllBossRecords[listNum].stat.healthLossPrev = -1;
					AllBossRecords[listNum].stat.healthAtStartPrev = -1;
				}
			}
			*/
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && Main.netMode != NetmodeID.Server) {
				for (int listNum = 0; listNum < BossChecklist.bossTracker.SortedBosses.Count; listNum++) {
					if (WorldAssist.ActiveBossesList.Count == 0 || !WorldAssist.ActiveBossesList[listNum]) continue;
					else if (WorldAssist.StartingPlayers[listNum][Main.myPlayer]) {
						if (player.dead) {
							DeathTracker[listNum] = true;
							DodgeTimer[listNum] = 0;
							BrinkChecker[listNum] = MaxHealth[listNum];
						}
						RecordTimers[listNum]++;
						if (!player.dead) DodgeTimer[listNum]++;
						if (player.statLife < BrinkChecker[listNum] && player.statLife > 0) {
							BrinkChecker[listNum] = player.statLife;
						}
					}
				}
			}

			
		}
	}
}
 