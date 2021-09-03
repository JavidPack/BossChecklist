using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class PlayerAssist : ModPlayer
	{
		public bool hasOpenedTheBossLog; // For the 'never opened' button glow for players who haven't noticed the new feature yet.
		
		public Dictionary<string, List<BossRecord>> AllStoredRecords; // string = Main.ActiveWorldFileData.UniqueId.ToString()
		public List<BossRecord> RecordsForWorld; // Records will now be tied to character AND world.
		public List<BossCollection> BossTrophies; // Unlike records, loot and collectibles are bound to player only.
		
		public int durationLastFight;
		public int hitsTakenLastFight;

		public List<int> Trackers_Duration;
		public List<int> Tracker_HitsTaken;
		public List<bool> Tracker_Deaths;
		public List<bool> hasNewRecord;

		public override void Initialize() {
			hasOpenedTheBossLog = false;

			AllStoredRecords = new Dictionary<string, List<BossRecord>>();
			RecordsForWorld = new List<BossRecord>();
			BossTrophies = new List<BossCollection>();

			// Create new lists for each boss's loot and collections so we can apply the saved data to them
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				BossTrophies.Add(new BossCollection(boss.Key));
				int index = BossTrophies.FindIndex(x => x.bossKey == boss.Key);
				BossTrophies[index].loot = new List<ItemDefinition>();
				BossTrophies[index].collectibles = new List<ItemDefinition>();
			}

			// TODO: Reimplement how this works. Best record becomes Prev Best on new record until another fight overwrites it with a new previous attempt?
			// This will be the attempt records of the players last fight (Not saved!)
			// This is only used for the UI, to determine whether the PrevRecord is a "last attempt" or a "beaten record"
			durationLastFight = hitsTakenLastFight = -1;

			// For being able to complete records in Multiplayer
			Trackers_Duration = new List<int>();
			Tracker_Deaths = new List<bool>();
			Tracker_HitsTaken = new List<int>();
			hasNewRecord = new List<bool>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				Trackers_Duration.Add(0);
				Tracker_Deaths.Add(false);
				Tracker_HitsTaken.Add(0);
				hasNewRecord.Add(false);
			}
		}

		public override TagCompound Save() {
			// We cannot save dictionaries, so we'll convert it to a TagoCompund instead
			TagCompound TempStoredRecords = new TagCompound();
			foreach (KeyValuePair<string, List<BossRecord>> bossRecord in AllStoredRecords) {
				TempStoredRecords.Add(bossRecord.Key, bossRecord.Value);
			}

			TagCompound saveData = new TagCompound {
				{ "BossLogPrompt", hasOpenedTheBossLog },
				{ "StoredRecords", TempStoredRecords },
				{ "Collection", BossTrophies }
			};
			return saveData;
		}

		public override void Load(TagCompound tag) {
			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt");

			// Grab the player's record data so we can grab what we need in OnEnterWorld().
			TagCompound TempStoredRecords = tag.Get<TagCompound>("StoredRecords");
			// Clear the list so we can convert our TagCompund back to a Dictionary
			AllStoredRecords.Clear();
			foreach (KeyValuePair<string, object> bossRecords in TempStoredRecords) {
				AllStoredRecords.Add(bossRecords.Key, TempStoredRecords.GetList<BossRecord>(bossRecords.Key).ToList());
			}

			// Prepare the collections for the player. Putting unloaded bosses in the back and new/existing ones up front
			List<BossCollection> SavedCollections = tag.Get<List<BossCollection>>("Collection");
			foreach (BossCollection collection in SavedCollections) {
				int index = BossTrophies.FindIndex(x => x.bossKey == collection.bossKey);
				if (index == -1) {
					BossTrophies.Add(collection);
				}
				else {
					BossTrophies[index] = collection;
				}
			}
		}

		public override void clientClone(ModPlayer clientClone) {
			PlayerAssist clone = clientClone as PlayerAssist;
			clone.hasOpenedTheBossLog = hasOpenedTheBossLog;
			clone.BossTrophies = BossTrophies;
			clone.RecordsForWorld = RecordsForWorld;
		}

		public override void OnEnterWorld(Player player) {
			// PageNum starts out with an invalid number so jumping between worlds will always reset the BossLog when toggled
			BossLogUI.PageNum = -3; 
			
			// Reset record tracker numbers
			durationLastFight = -1; //TODO: find out why hitstaken isnt reset and why its set to -1
			Trackers_Duration = new List<int>();
			Tracker_Deaths = new List<bool>();
			Tracker_HitsTaken = new List<int>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				Trackers_Duration.Add(0);
				Tracker_Deaths.Add(false);
				Tracker_HitsTaken.Add(0);
			}
			
			// Upon entering a world, determine if records already exist for a player and copy them into a variable.
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.ContainsKey(WorldID) && AllStoredRecords.TryGetValue(WorldID, out RecordsForWorld)) {
				foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
					// If we added mod bosses since we last generated this, be sure to add them
					if (!RecordsForWorld.Exists(x => x.bossKey == boss.Key)) {
						RecordsForWorld.Add(new BossRecord(boss.Key));
					}
				}
			}
			else {
				// If records dont exist (player is new to the world) create a new entry for the player to use.
				RecordsForWorld = new List<BossRecord>();
				foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
					RecordsForWorld.Add(new BossRecord(boss.Key));
				}
				AllStoredRecords.Add(WorldID, RecordsForWorld);
			}
			
			// Send the player's world-bound records to the server. The server doesn't need player records from every world.
			int bossCount = BossChecklist.bossTracker.SortedBosses.Count;
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Essentially to get "BossAssist.ServerCollectedRecords[player.whoAmI] = AllBossRecords;"
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.SendRecordsToServer);
				for (int i = 0; i < bossCount; i++) {
					BossStats stat = RecordsForWorld[i].stat;
					packet.Write(stat.kills);
					packet.Write(stat.deaths);
					packet.Write(stat.durationBest);
					packet.Write(stat.durationPrev);
					packet.Write(stat.hitsTakenBest);
					packet.Write(stat.hitsTakenPrev);
				}
				packet.Send(); // To server
			}
		}

		// Continually track the duration of boss fights while boss NPCs are active.
		// If a player dies at any point while a boss is active, add to the death tracker for later.
		public override void PreUpdate() {
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && Main.netMode != NetmodeID.Server) {
				for (int listNum = 0; listNum < BossChecklist.bossTracker.SortedBosses.Count; listNum++) {
					if (WorldAssist.ActiveBossesList.Count == 0 || !WorldAssist.ActiveBossesList[listNum]) {
						continue;
					}
					else if (WorldAssist.StartingPlayers[listNum][Main.myPlayer]) {
						if (player.dead) {
							Tracker_Deaths[listNum] = true;
						}
						Trackers_Duration[listNum]++;
					}
				}
			}
		}

		// When a player is dead they are marked as such in the Death tracker.
		// On respawn, add to the total deaths towards marked bosses.
		public override void OnRespawn(Player player) {
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
				for (int i = 0; i < Tracker_Deaths.Count; i++) {
					if (Tracker_Deaths[i]) {
						RecordsForWorld[i].stat.deaths++;
					}
					Tracker_Deaths[i] = false;
				}
			}
		}

		// Whenever the player is hurt, add to the HitsTaken tracker.
		// TODO: Test debuff damage. Would rather that not count.
		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					if (!Main.npc[i].active || NPCAssist.ListedBossNum(Main.npc[i]) == -1) {
						continue;
					}
					int listNum = NPCAssist.ListedBossNum(Main.npc[i]);
					Tracker_HitsTaken[listNum]++;
				}
			}
		}
	}
}
 