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

			// Has to contain all entries, even if they arent a boss //TODO: maybe look into again at some point, for now its fine.
			hasNewRecord = new List<bool>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				hasNewRecord.Add(false);
			}
		}

		public override void SaveData(TagCompound tag) {
			// Every time player data is saved, the RecordsForWorld list should be resubmitted into AllStoredRecords to properly update them
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.ContainsKey(WorldID)) {
				AllStoredRecords[WorldID] = RecordsForWorld;
			}

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

			// Add any new bosses missing inside of BossItemsCollected
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (!BossItemsCollected.TryGetValue(boss.Key, out List<ItemDefinition> value)) {
					BossItemsCollected.Add(boss.Key, new List<ItemDefinition>());
				}
			}

			// Upon entering a world, determine if records already exist for a player and copy them into 'RecordsForWorld'
			RecordsForWorld.Clear(); // The list must be cleared first, otherwise the list will contain items from previously entered worlds
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.TryGetValue(WorldID, out List<BossRecord> tempRecords)) {
				for (int i = 0; i < BossChecklist.bossTracker.BossRecordKeys.Count; i++) {
					// If the index was found, copy it to our list
					// Otherwise the index must be invalid so a new entry must be created
					int grabIndex = tempRecords.FindIndex(x => x.bossKey == BossChecklist.bossTracker.BossRecordKeys[i]);
					if (grabIndex != -1)
						RecordsForWorld.Add(tempRecords[grabIndex]);
					else
						RecordsForWorld.Add(new BossRecord(BossChecklist.bossTracker.BossRecordKeys[i]));
				}
			}
			else {
				// If personal records do not exist for this world, create a new entry for the player to use
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					RecordsForWorld.Add(new BossRecord(key));
				}
				// A new entry will be added to AllStoredRecords so that it can be saved when needed
				AllStoredRecords.Add(WorldID, RecordsForWorld);
			}

			// Reset record tracker numbers. Has to be reset after entering a world.
			// Add values to all record trackers after RecordsForWorld are determined
			Tracker_Duration = new List<int>();
			Tracker_Deaths = new List<bool>();
			Tracker_HitsTaken = new List<int>();

			for (int i = 0; i < BossChecklist.bossTracker.BossRecordKeys.Count; i++) {
				Tracker_Duration.Add(0);
				Tracker_Deaths.Add(false);
				Tracker_HitsTaken.Add(0);
			}

			// If the player has not been in this world before, create an entry for this world
			if (!AllStoredForceDowns.ContainsKey(WorldID)) {
				AllStoredForceDowns.Add(WorldID, new List<string>());
			}
			// Then make ListedChecks the list needed for the designated world
			AllStoredForceDowns.TryGetValue(WorldID, out ForceDownsForWorld);

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}

			// When a player joins a world, their Records will need to be sent to the server
			// The server doesn't need player records from every world, just the current one
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.SendRecordsToServer);
				packet.Write(RecordsForWorld.Count);
				for (int i = 0; i < RecordsForWorld.Count; i++) {
					// Only records that we need to compare between other players are needed
					// The statisctics and First recrods are strictly personal with no comparing involved
					PersonalStats stat = RecordsForWorld[i].stats;
					packet.Write(RecordsForWorld[i].bossKey);
					packet.Write(stat.durationBest);
					packet.Write(stat.durationPrev);
					packet.Write(stat.hitsTakenBest);
					packet.Write(stat.hitsTakenPrev);
				}
				packet.Send(); // Multiplayer client --> Server
			}
		}

		// Continually track the duration of boss fights while boss NPCs are active
		// If a player dies at any point while a boss is active, add to the death tracker for later
		public override void PreUpdate() {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && Main.netMode != NetmodeID.Server) {
				for (int recordIndex = 0; recordIndex < BossChecklist.bossTracker.BossRecordKeys.Count; recordIndex++) {
					// If a boss is marked active and this player is a 'starting player'
					if (WorldAssist.Tracker_ActiveEntry[recordIndex] && WorldAssist.Tracker_StartingPlayers[recordIndex][Main.myPlayer]) {
						if (Player.dead) {
							Tracker_Deaths[recordIndex] = true;
						}
						Tracker_Duration[recordIndex]++;
					}
				}
			}
		}

		// When a player is dead they are marked as such in the Death tracker
		// On respawn, add to the total deaths towards marked bosses
		// ActiveBossesList and StartingPlayers doesn't need to be checked since it was checked when setting the tracker bool to true
		public override void OnRespawn(Player player) {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
				for (int recordIndex = 0; recordIndex < Tracker_Deaths.Count; recordIndex++) {
					if (Tracker_Deaths[recordIndex]) {
						Tracker_Deaths[recordIndex] = false;
						RecordsForWorld[recordIndex].stats.deaths++;
						WorldAssist.worldRecords[recordIndex].stats.totalDeaths++;
					}
				}
			}
		}

		// Whenever the player is hurt, add to the HitsTaken tracker
		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit) {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int recordIndex = 0; recordIndex < BossChecklist.bossTracker.BossRecordKeys.Count; recordIndex++) {
					if (WorldAssist.Tracker_ActiveEntry[recordIndex] && WorldAssist.Tracker_StartingPlayers[recordIndex][Main.myPlayer]) {
						Tracker_HitsTaken[recordIndex]++;
					}
				}
			}
		}
	}
}
