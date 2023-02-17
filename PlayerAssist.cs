using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
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
		public List<ItemDefinition> BossItemsCollected;

		// The 'in progress' values for records. This is what is updated during boss fights.
		public int[] Tracker_Duration;
		public int[] Tracker_HitsTaken;
		public bool[] Tracker_Deaths;
		public bool[] hasNewRecord;

		public override void Initialize() {
			hasOpenedTheBossLog = false;
			enteredWorldReset = false;

			AllStoredRecords = new Dictionary<string, List<BossRecord>>();
			RecordsForWorld = new List<BossRecord>();
			BossItemsCollected = new List<ItemDefinition>();

			// For being able to complete records in Multiplayer
			Tracker_Duration = Array.Empty<int>();
			Tracker_Deaths = Array.Empty<bool>();
			Tracker_HitsTaken = Array.Empty<int>();
			hasNewRecord = Array.Empty<bool>();
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

			tag["BossLogPrompt"] = hasOpenedTheBossLog;
			tag["StoredRecords"] = TempRecords;
			tag["BossLootObtained"] = BossItemsCollected;
		}

		public override void LoadData(TagCompound tag) {
			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt");

			// Grab the player's record data so we can grab what we need in OnEnterWorld().
			TagCompound SavedStoredRecords = tag.Get<TagCompound>("StoredRecords");
			AllStoredRecords.Clear(); // Clear the list so we can convert our TagCompund back to a Dictionary
			foreach (KeyValuePair<string, object> bossRecords in SavedStoredRecords) {
				AllStoredRecords.Add(bossRecords.Key, SavedStoredRecords.GetList<BossRecord>(bossRecords.Key).ToList());
			}

			// Prepare the collections for the player. Putting unloaded bosses in the back and new/existing ones up front
			BossItemsCollected = tag.GetList<ItemDefinition>("BossLootObtained").ToList();

			// Recover old data to convert to new data // TODO: Remove in future update
			TagCompound oldData = tag.Get<TagCompound>("BossItemsCollected");
			if (oldData.Count != 0) {
				foreach (KeyValuePair<string, object> bossRecords in oldData) {
					BossItemsCollected.AddRange(oldData.GetList<ItemDefinition>(bossRecords.Key).ToList());
				}
			}
		}

		public override void OnEnterWorld(Player player) {
			// PageNum starts out with an invalid number so jumping between worlds will always reset the BossLog when toggled
			enteredWorldReset = true;

			// Upon entering a world, determine if records already exist for a player and copy them into 'RecordsForWorld'
			RecordsForWorld.Clear(); // The list must be cleared first, otherwise the list will contain items from previously entered worlds
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.TryGetValue(WorldID, out List<BossRecord> tempRecords)) {
				for (int i = 0; i < BossChecklist.bossTracker.BossRecordKeys.Count; i++) {
					// If the index was found, copy it to our list
					// Otherwise the index must be invalid so a new entry must be created
					int grabIndex = tempRecords.FindIndex(x => x.bossKey == BossChecklist.bossTracker.BossRecordKeys[i]);
					if (grabIndex != -1) {
						RecordsForWorld.Add(tempRecords[grabIndex]);
					}
					else {
						RecordsForWorld.Add(new BossRecord(BossChecklist.bossTracker.BossRecordKeys[i]));
					}
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
			Tracker_Duration = new int[BossChecklist.bossTracker.BossRecordKeys.Count];
			Tracker_Deaths = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];
			Tracker_HitsTaken = new int[BossChecklist.bossTracker.BossRecordKeys.Count];
			hasNewRecord = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];

			// Send this info to the server to populate the arrays server-sided
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.ResetTrackers);
				packet.Write(-1);
				packet.Write(Player.whoAmI);
				packet.Send(); // Multiplayer client --> Server
			}

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
					// The only records that we need to compare between other players are previous and best records
					// The statisctics and first records are strictly for the assigned client
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
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE || Player.whoAmI == 255)
				return;

			if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
				for (int recordIndex = 0; recordIndex < BossChecklist.bossTracker.BossRecordKeys.Count; recordIndex++) {
					// If a boss is marked active and this player is a 'starting player'
					if (WorldAssist.Tracker_ActiveEntry[recordIndex] && WorldAssist.Tracker_StartingPlayers[recordIndex, Player.whoAmI]) {
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
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE || Player.whoAmI == 255 || player.whoAmI != Player.whoAmI)
				return;

			if (!BossChecklist.DebugConfig.RecordTrackingDisabled) {
				for (int recordIndex = 0; recordIndex < Tracker_Deaths.Length; recordIndex++) {
					if (Tracker_Deaths[recordIndex]) {
						Tracker_Deaths[recordIndex] = false;
						RecordsForWorld[recordIndex].stats.deaths++;
						WorldAssist.worldRecords[recordIndex].stats.totalDeaths++;
					}
				}
			}
		}

		// Whenever the player is hurt, add to the HitsTaken tracker
		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit, int cooldownCounter) {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE || Player.whoAmI == 255)
				return;

			if (!BossChecklist.DebugConfig.RecordTrackingDisabled && damage > 0) {
				for (int recordIndex = 0; recordIndex < BossChecklist.bossTracker.BossRecordKeys.Count; recordIndex++) {
					if (WorldAssist.Tracker_ActiveEntry[recordIndex] && WorldAssist.Tracker_StartingPlayers[recordIndex, Player.whoAmI]) {
						Tracker_HitsTaken[recordIndex]++;
					}
				}
			}
		}

		public override void UpdateDead() {
			if (Main.netMode == NetmodeID.Server || Main.myPlayer != Player.whoAmI)
				return;

			// Timer sounds when a player is about to respawn
			if (BossChecklist.ClientConfig.TimerSounds) {
				if (Player.respawnTimer > 0 && Player.respawnTimer <= 180 && Player.respawnTimer % 60 == 0) {
					SoundEngine.PlaySound(SoundID.MaxMana);
				}
			}
		}
	}
}
