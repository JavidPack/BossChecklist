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
		// For the 'never opened' button glow for players who haven't noticed the new feature yet.
		public bool hasOpenedTheBossLog;
		// When players jon a different world, the boss log PageNum should reset back to its original state
		public bool enteredWorldReset;

		// Records are bound to characters, but records are independent between worlds as well.
		// AllStored records contains every player record from every world
		// RecordsForWorld is a reference to the specfic player records of the current world
		// We split up AllStoredRecords with 'Main.ActiveWorldFileData.UniqueId.ToString()' as keys
		public Dictionary<string, List<BossRecord>> AllStoredRecords;
		public List<BossRecord> RecordsForWorld;

		public Dictionary<string, List<ItemDefinition>> BossItemsCollected;

		// Key represents worldID, Value represents BossKeys
		public Dictionary<string, List<string>> AllStoredForceDowns;
		public List<string> ForceDownsForWorld;

		// The 'in progress' values for records. This is what is updated during boss fights.
		public List<int> Tracker_Duration;
		public List<int> Tracker_HitsTaken;
		public List<bool> Tracker_Deaths;
		public List<bool> hasNewRecord;

		public override void Initialize() {
			hasOpenedTheBossLog = false;
			enteredWorldReset = false;

			AllStoredRecords = new Dictionary<string, List<BossRecord>>();
			RecordsForWorld = new List<BossRecord>();
			BossItemsCollected = new Dictionary<string, List<ItemDefinition>>();
			AllStoredForceDowns = new Dictionary<string, List<string>>();
			ForceDownsForWorld = new List<string>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				BossItemsCollected.Add(boss.Key, new List<ItemDefinition>());
			}

			// For being able to complete records in Multiplayer
			Tracker_Duration = new List<int>();
			Tracker_Deaths = new List<bool>();
			Tracker_HitsTaken = new List<int>();
			hasNewRecord = new List<bool>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				Tracker_Duration.Add(0);
				Tracker_Deaths.Add(false);
				Tracker_HitsTaken.Add(0);
				hasNewRecord.Add(false);
			}
		}

		public override void SaveData(TagCompound tag) {
			// We cannot save dictionaries, so we'll convert it to a TagCompound instead
			TagCompound TempRecords = new TagCompound();
			foreach (KeyValuePair<string, List<BossRecord>> bossRecord in AllStoredRecords) {
				TempRecords.Add(bossRecord.Key, bossRecord.Value);
			}

			TagCompound TempChecks = new TagCompound();
			foreach(KeyValuePair<string, List<string>> entry in AllStoredForceDowns) {
				TempChecks.Add(entry.Key, entry.Value);
			}

			TagCompound TempItemsCollected = new TagCompound();
			foreach (KeyValuePair<string, List<ItemDefinition>> entry in BossItemsCollected) {
				TempItemsCollected.Add(entry.Key, entry.Value);
			}

			tag["BossLogPrompt"] = hasOpenedTheBossLog;
			tag["StoredRecords"] = TempRecords;
			tag["BossItemsCollected"] = TempItemsCollected;
			tag["ForcedChecks"] = TempChecks;
		}

		public override void LoadData(TagCompound tag) {
			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt");

			// Grab the player's record data so we can grab what we need in OnEnterWorld().
			TagCompound SavedStoredRecords = tag.Get<TagCompound>("StoredRecords");
			// Clear the list so we can convert our TagCompund back to a Dictionary
			AllStoredRecords.Clear();
			foreach (KeyValuePair<string, object> bossRecords in SavedStoredRecords) {
				AllStoredRecords.Add(bossRecords.Key, SavedStoredRecords.GetList<BossRecord>(bossRecords.Key).ToList());
			}

			// Do the same for any checkmarks the user wants to force
			TagCompound SavedChecks = tag.Get<TagCompound>("ForcedChecks");
			AllStoredForceDowns.Clear();
			foreach (KeyValuePair<string, object> entry in SavedChecks) {
				AllStoredForceDowns.Add(entry.Key, SavedChecks.GetList<string>(entry.Key).ToList());
			}

			// Prepare the collections for the player. Putting unloaded bosses in the back and new/existing ones up front
			TagCompound SavedItemsCollected = tag.Get<TagCompound>("BossItemsCollected");
			BossItemsCollected.Clear();
			foreach (KeyValuePair<string, object> entry in SavedItemsCollected) {
				BossItemsCollected.Add(entry.Key, SavedItemsCollected.GetList<ItemDefinition>(entry.Key).ToList());
			}
		}

		public override void OnEnterWorld(Player player) {
			// If the boss log has been fully opened before or the prompt is disabled, set the pagenum to the Table of Contents instead of the prompt.
			BossLogUI.PageNum = hasOpenedTheBossLog || BossChecklist.BossLogConfig.PromptDisabled ? -1 : -3;

			// PageNum starts out with an invalid number so jumping between worlds will always reset the BossLog when toggled
			enteredWorldReset = true;

			// Reset record tracker numbers
			Tracker_Duration = new List<int>();
			Tracker_Deaths = new List<bool>();
			Tracker_HitsTaken = new List<int>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				// Add values to all record trackers
				Tracker_Duration.Add(0);
				Tracker_Deaths.Add(false);
				Tracker_HitsTaken.Add(0);

				// Add any new bosses missing inside of BossItemsCollected
				if (!BossItemsCollected.TryGetValue(boss.Key, out List<ItemDefinition> value)) {
					BossItemsCollected.Add(boss.Key, new List<ItemDefinition>());
				}
			}

			// Upon entering a world, determine if records already exist for a player and copy them into a variable.
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.ContainsKey(WorldID) && AllStoredRecords.TryGetValue(WorldID, out RecordsForWorld)) {
				foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
					// If we added mod bosses since we last generated this, be sure to add them
					if (!RecordsForWorld.Exists(x => x.bossKey == boss.Key) && boss.type == EntryType.Boss) {
						RecordsForWorld.Add(new BossRecord(boss.Key));
					}
				}
			}
			else {
				// If records dont exist (player is new to the world) create a new entry for the player to use.
				RecordsForWorld = new List<BossRecord>();
				foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
					if (boss.type == EntryType.Boss) {
						RecordsForWorld.Add(new BossRecord(boss.Key));
					}
				}
				AllStoredRecords.Add(WorldID, RecordsForWorld);
			}

			// If the player has not been in this world before, create an entry for this world
			if (!AllStoredForceDowns.ContainsKey(WorldID)) {
				AllStoredForceDowns.Add(WorldID, new List<string>());
			}
			// Then make ListedChecks the list needed for the designated world
			AllStoredForceDowns.TryGetValue(WorldID, out ForceDownsForWorld);

			// Send the player's world-bound records to the server. The server doesn't need player records from every world.
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Essentially to get "BossAssist.ServerCollectedRecords[player.whoAmI] = AllBossRecords;"
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.SendRecordsToServer);
				for (int i = 0; i < RecordsForWorld.Count; i++) {
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
						if (Player.dead) {
							Tracker_Deaths[listNum] = true;
						}
						Tracker_Duration[listNum]++;
					}
				}
			}
		}

		// When a player is dead they are marked as such in the Death tracker.
		// On respawn, add to the total deaths towards marked bosses.
		public override void OnRespawn(Player player) {
			for (int i = 0; i < Tracker_Deaths.Count; i++) {
				if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
					if (Tracker_Deaths[i]) {
						RecordsForWorld[i].stat.deaths++;
					}
					Tracker_Deaths[i] = false;
				}
				WorldAssist.worldRecords[i].stat.totalDeaths++;
			}
		}

		// Whenever the player is hurt, add to the HitsTaken tracker.
		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					if (!Main.npc[i].active || NPCAssist.GetBossInfoIndex(Main.npc[i]) == -1) {
						continue;
					}
					int listNum = NPCAssist.GetBossInfoIndex(Main.npc[i]);
					Tracker_HitsTaken[listNum]++;
				}
			}
		}
	}
}
