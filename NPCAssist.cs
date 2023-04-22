using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BossChecklist
{
	class NPCAssist : GlobalNPC
	{
		// When an entry NPC spawns, setup the world and player trackers for the upcoming fight
		public override void OnSpawn(NPC npc, IEntitySource source) {
			// Only single player and server should be starting the record tracking process
			if (Main.netMode == NetmodeID.MultiplayerClient || BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			EntryInfo entry = GetEntryInfo(npc.type);
			if (entry == null)
				return; // Make sure the npc is an entry

			// If not marked active, set to active and reset trackers for all players to start tracking records for this fight
			int recordIndex = entry.GetRecordIndex;
			if (!WorldAssist.Tracker_ActiveEntry[recordIndex]) {
				WorldAssist.Tracker_ActiveEntry[recordIndex] = true;

				if (Main.netMode == NetmodeID.SinglePlayer) {
					WorldAssist.Tracker_StartingPlayers[recordIndex, Main.LocalPlayer.whoAmI] = true; // Active players when the boss spawns will be counted
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					modPlayer.Tracker_Duration[recordIndex] = 0;
					modPlayer.Tracker_HitsTaken[recordIndex] = 0;
				}
				else if (Main.netMode == NetmodeID.Server) {
					for (int j = 0; j < Main.maxPlayers; j++) {
						if (!Main.player[j].active)
							continue; // skip any inactive players

						WorldAssist.Tracker_StartingPlayers[recordIndex, j] = true; // Active players when the boss spawns will be counted

						// This is updated serverside
						PlayerAssist modPlayer = Main.player[j].GetModPlayer<PlayerAssist>();
						modPlayer.Tracker_Duration[recordIndex] = 0;
						modPlayer.Tracker_HitsTaken[recordIndex] = 0;

						// Needs to be updated client side as well
						// Send packets from the server to all participating players to reset their trackers for the recordIndex provided
						ModPacket packet = Mod.GetPacket();
						packet.Write((byte)PacketMessageType.ResetTrackers);
						packet.Write(recordIndex);
						packet.Write(Main.player[j].whoAmI);
						packet.Send(toClient: Main.player[j].whoAmI); // Server --> Multiplayer client
					}
				}
			}
		}

		// When an NPC is killed and fully inactive the fight has ended, so stop all record trackers
		public override void OnKill(NPC npc) {
			HandleDownedNPCs(npc.type); // Custom downed bool code
			SendEntryMessage(npc); // Display a message for Limbs/Towers if config is enabled

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			// Stop record trackers and record them to the player while also checking for records and world records
			
			EntryInfo entry = GetEntryInfo(npc.type);
			if (entry != null) {
				int index = entry.GetIndex;
				int recordIndex = entry.GetRecordIndex;
				if (FullyInactive(npc, index, true)) {
					if (!BossChecklist.DebugConfig.NewRecordsDisabled && !BossChecklist.DebugConfig.RecordTrackingDisabled) {
						if (Main.netMode == NetmodeID.Server) {
							CheckRecordsForServer(npc, recordIndex);
						}
						else if (Main.netMode == NetmodeID.SinglePlayer) {
							CheckRecords(npc, recordIndex);
						}
						else if (Main.netMode == NetmodeID.MultiplayerClient) {
							SubmitPlayTimeFirstStat(npc, recordIndex);
						}
					}

					if (BossChecklist.DebugConfig.ShowInactiveBossCheck)
						Main.NewText(npc.FullName + ": " + FullyInactive(npc, index));

					WorldAssist.worldRecords[recordIndex].stats.totalKills++;

					// Reset world variables after record checking takes place
					WorldAssist.Tracker_ActiveEntry[recordIndex] = false;
					for (int i = 0; i < Main.maxPlayers; i++) {
						WorldAssist.Tracker_StartingPlayers[recordIndex, i] = false;
					}
				}
			}
		}

		/// <summary>
		/// Loops through all entries in BossTracker.SortedBosses to find EntryInfo that contains the specified npc type.
		/// This method is mainly used for boss record purposes.
		/// </summary>
		/// <returns>A valid EntryInfo entry within the registered entries. Returns null if no entry can be found.</returns>
		public static EntryInfo GetEntryInfo(int npcType) {
			if (!BossChecklist.bossTracker.EntryCache[npcType])
				return null; // the entry hasn't been registered

			List<EntryInfo> entries = BossChecklist.bossTracker.SortedEntries;
			for (int index = 0; index < entries.Count; index++) {
				if (entries[index].type != EntryType.Boss)
					continue; // skip non-boss

				if (entries[index].npcIDs.Contains(npcType))
					return entries[index]; // if the npc pool contains the npc type, return the current the index
			}

			return null; // no entry found
		}

		/// <summary>
		/// Searches for all npc types of a given SortedBosses index and checks their active status within Main.npc. Does not include the passed NPC itself unless specified.
		/// </summary>
		/// <param name="excludePassedNPC">Whether or not this method should also check if the NPC passed is inactive.</param>
		/// <returns>Whether or not an npc listed in the specified entry's npc pool is active or not.</returns>
		public static bool FullyInactive(NPC npc, int index, bool excludePassedNPC = false) {
			if (index == -1)
				return !npc.active; // index should already be valid before submitting, but just in case return the NPC's active status

			foreach (int npcType in BossChecklist.bossTracker.SortedEntries[index].npcIDs) {
				if (Main.npc.Any(nPC => nPC != npc && nPC.active && nPC.type == npcType))
					return false; // if another npc within the same npc pool exists, the entry isn't truly inactive. Reminder: Boss minions should not submitted into NPC pools.
			}

			if (excludePassedNPC)
				return true; // If excluding the passed NPC from the active check, this should return true

			return !npc.active; // otherwise, return the NPC's active status
		}

		/// <summary>
		/// Determines what despawn message should be used based on client configuration and submitted entry info.
		/// </summary>
		/// <returns>A string or key of the despawn message of the passed npc. Returns null if no message can be found.</returns>
		public static string GetDespawnMessage(NPC npc, int index) {
			if (npc.life <= 0)
				return null; // If the boss was killed, don't display a despawn message

			string messageType = BossChecklist.ClientConfig.DespawnMessageType;
			if (messageType == "Unique") {
				// When unique despawn messages are enabled, pass the NPC for the custom message function provided by the entry
				string customMessage = BossChecklist.bossTracker.SortedEntries[index].customDespawnMessages(npc);
				if (!string.IsNullOrEmpty(customMessage))
					return customMessage; // this will only return a unique message if the custom message function properly assigns one
			}

			if (messageType != "Disabled") {
				// If the Unique message was empty/null or the player is using Generic despawn messages, try to find an appropriate despawn message to send
				// Return a generic despawn message if any player is left alive or return a boss victory despawn message if all player's were killed
				return Main.player.Any(plr => plr.active && !plr.dead) ? "Mods.BossChecklist.ChatMessages.Despawn.Generic" : "Mods.BossChecklist.ChatMessages.Loss.Generic";
			}
			// The despawn message feature was disabled. Return an empty message.
			return null;
		}

		/// <summary>
		/// Takes the data from record trackers and updates the player's saved records accordingly.
		/// <para>Only runs in the Singleplayer netmode.</para>
		/// </summary>
		private void CheckRecords(NPC npc, int recordIndex) {
			if (Main.netMode != NetmodeID.SinglePlayer)
				return;

			// Player must have contributed to the boss fight
			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex, Main.myPlayer])
				return;

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			PersonalStats statistics = modPlayer.RecordsForWorld[recordIndex].stats;
			int trackedDuration = modPlayer.Tracker_Duration[recordIndex];
			int trackedHitsTaken = modPlayer.Tracker_HitsTaken[recordIndex];
			bool newRecordSet = false;

			statistics.kills++; // Kills always go up, since record checking only occurs if boss was defeated

			// If this was the first record made for the boss, set add them to the recordType
			if (statistics.durationFirst == -1 && statistics.hitsTakenFirst == -1) {
				statistics.durationFirst = trackedDuration;
				statistics.hitsTakenFirst = trackedHitsTaken;
				statistics.playTimeFirst = Main.ActivePlayerFileData.GetPlayTime().Ticks;
			}

			// Check if the tracked duration was better than the current best OR if the current best has not yet been achieved
			// Overwrite PrevBest records with the 'current' one first
			// If the current Best is -1, it was a first record, which means there was no prevBest (logic still works!)
			statistics.durationPrev = trackedDuration;
			if (trackedDuration < statistics.durationBest || statistics.durationBest == -1) {
				statistics.durationPrevBest = statistics.durationBest;
				statistics.durationBest = trackedDuration;
				if (statistics.durationBest != -1) {
					newRecordSet = true; // New Record should not appear above the player on the first record achieved
				}
			}

			// Repeat the same logic with the Hits Taken record
			statistics.hitsTakenPrev = trackedHitsTaken;
			if (trackedHitsTaken < statistics.hitsTakenBest || statistics.hitsTakenBest == -1) {
				statistics.hitsTakenPrevBest = statistics.hitsTakenBest;
				statistics.hitsTakenBest = trackedHitsTaken;
				if (statistics.hitsTakenBest != -1) {
					newRecordSet = true;
				}
			}

			// If a new record was made, notify the player. Again, this will not show for newly set records
			if (newRecordSet) {
				modPlayer.hasNewRecord[recordIndex] = true;
				// Compare records to World Records. Players must have beaten their own records to beat a world record
				string recordSet = CheckWorldRecords(recordIndex) ? "NewWorldRecord" : "NewRecord";
				string message = Language.GetTextValue($"{BossLogUI.LogPath}.Records.{recordSet}");
				CombatText.NewText(Main.LocalPlayer.getRect(), Color.LightYellow, message, true);
			}
		}

		/// <summary>
		/// Compares the record tracker data from all players and updates all records accordingly to the server.
		/// A ModPacket will be sent to update the personal record data for player clients afterwards.
		/// World Records will be checked and updated once all records are finalized and sent.
		/// <para>Only runs in the Server netmode.</para>
		/// </summary>
		private void CheckRecordsForServer(NPC npc, int recordIndex) {
			if (Main.netMode != NetmodeID.Server)
				return;

			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			bool newRecordSet = false;
			List<int> dHolders = new List<int>();
			List<int> htHolders = new List<int>();
			int dCompareValue = -1;
			int htCompareValue = -1;

			foreach (Player player in Main.player) {
				if (!player.active || !npc.playerInteraction[player.whoAmI] || !WorldAssist.Tracker_StartingPlayers[recordIndex, player.whoAmI])
					continue; // Players must be active AND have interacted with the boss AND cannot have recordingstats disabled

				PersonalStats serverStatistics = BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].stats;
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				int trackedDuration = modPlayer.Tracker_Duration[recordIndex];
				int trackedHitsTaken = modPlayer.Tracker_HitsTaken[recordIndex];

				// For each record type we check if its beats the current record or if it is not set already
				// If it is beaten, we add a flag to recordType to allow the record tracker numbers to override the current record
				NetRecordID recordType = NetRecordID.None;

				serverStatistics.kills++;

				// If this was the first record made for the boss, set add them to the recordType
				if (serverStatistics.durationFirst == -1 && serverStatistics.hitsTakenFirst == -1) {
					recordType |= NetRecordID.FirstRecord;
					serverStatistics.durationFirst = trackedDuration;
					serverStatistics.hitsTakenFirst = trackedHitsTaken;
				}

				// Check for best records as well (This would apply on first records as well)
				// Overwrite PrevBest records with the 'current' one first
				// If the current Best is -1, it was a first record, which means there was no prevBest (logic still works!)
				serverStatistics.durationPrev = trackedDuration;
				if (trackedDuration < serverStatistics.durationBest || serverStatistics.durationBest == -1) {
					recordType |= NetRecordID.Duration_Best;
					serverStatistics.durationPrevBest = serverStatistics.durationBest;
					serverStatistics.durationBest = trackedDuration;
					if (serverStatistics.durationBest != -1) {
						newRecordSet = true;
					}
				}

				serverStatistics.hitsTakenPrev = trackedHitsTaken;
				if (trackedHitsTaken < serverStatistics.hitsTakenBest || serverStatistics.hitsTakenBest == -1) {
					recordType |= NetRecordID.HitsTaken_Best;
					serverStatistics.hitsTakenPrevBest = serverStatistics.hitsTakenBest;
					serverStatistics.hitsTakenBest = trackedHitsTaken;
					if (serverStatistics.durationBest != -1) {
						newRecordSet = true;
					}
				}

				// Make and send the packet
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write(recordIndex); // Which boss record are we changing?
				serverStatistics.NetSend(packet, recordType);
				packet.Send(toClient: player.whoAmI); // Server --> Multiplayer client // We send to the player as only they need to see their own records


				if (BossChecklist.DebugConfig.DisableWorldRecords)
					continue;

				if (worldRecords.durationWorld == -1 || trackedDuration <= worldRecords.durationWorld) {
					dHolders.Add(player.whoAmI); // Duration is the same for every player, since this code is ran on GlobalNPC.OnKill
					dCompareValue = trackedDuration;
				}

				// Check for world records
				int hitsTakenValue = htCompareValue == -1 ? worldRecords.hitsTakenWorld : htCompareValue;
				if (hitsTakenValue == -1 || trackedHitsTaken <= hitsTakenValue) {
					htCompareValue = trackedHitsTaken;
					if (trackedHitsTaken < hitsTakenValue) {
						htHolders.Clear();
					}
					htHolders.Add(player.whoAmI);
				}
			}

			if (BossChecklist.DebugConfig.DisableWorldRecords)
				return;

			if (dCompareValue == -1 && htCompareValue == -1)
				return; // If no world records were made, skip the update process

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"World records have been updated!");
			Console.ResetColor();

			NetRecordID worldRecordType = NetRecordID.None;

			// Apply new world record values and send them through packets for player clients
			if (dCompareValue != -1 && dHolders.Count > 0) {
				worldRecordType |= NetRecordID.Duration_Best;

				// If the compare value is better than the original record, clear the holders list
				if (dCompareValue < worldRecords.durationWorld) {
					worldRecords.durationHolder.Clear();
				}

				// New names should just be added to the list and this will need to be communicated within PacketHandling
				foreach (int playerNum in dHolders) {
					if (!worldRecords.durationHolder.Contains(Main.player[playerNum].name)) {
						worldRecords.durationHolder.Add(Main.player[playerNum].name);
					}
				}
				worldRecords.durationWorld = dCompareValue;
			}

			if (htCompareValue != -1 && htHolders.Count > 0) {
				worldRecordType |= NetRecordID.HitsTaken_Best;

				if (htCompareValue < worldRecords.hitsTakenWorld) {
					worldRecords.hitsTakenHolder.Clear();
				}
				foreach (int playerNum in dHolders) {
					if (!worldRecords.hitsTakenHolder.Contains(Main.player[playerNum].name)) {
						worldRecords.hitsTakenHolder.Add(Main.player[playerNum].name);
					}
				}
				worldRecords.hitsTakenWorld = htCompareValue;
			}

			// World Record data has to be sent to ALL active players
			for (int i = 0; i < Main.maxPlayers; i++) {
				if (!Main.player[i].active)
					continue;

				bool setWorldRecord = dHolders.Contains(Main.player[i].whoAmI) || htHolders.Contains(Main.player[i].whoAmI);

				ModPacket packet = Mod.GetPacket();
				packet.Write((int)PacketMessageType.WorldRecordUpdate);
				packet.Write(recordIndex);
				worldRecords.NetSend(packet, worldRecordType);
				//packet.Write(newRecordSet);
				packet.Write(setWorldRecord);
				packet.Send(toClient: Main.player[i].whoAmI); // Server --> Multiplayer client
			}
		}

		/// <summary>
		/// Compares the updated player record data with the current world records and updating them if the records were beaten.
		/// <para>Only runs in the Singleplayer netmode.</para>
		/// </summary>
		/// <returns>Whether or not a world record was beaten or matched</returns>
		private bool CheckWorldRecords(int recordIndex) {
			if (BossChecklist.DebugConfig.DisableWorldRecords)
				return false;

			Player player = Main.LocalPlayer;
			PersonalStats playerRecord = player.GetModPlayer<PlayerAssist>().RecordsForWorld[recordIndex].stats;
			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			bool newWorldRecord = false;

			// World records should NOT update if the world record is empty and the user is in SinglePlayer
			if (!worldRecords.DurationEmpty && playerRecord.durationBest <= worldRecords.durationWorld) {
				// If the world record was beaten, clear the list entirely
				if (playerRecord.durationBest < worldRecords.durationWorld) {
					worldRecords.durationHolder.Clear(); 
				}
				// Add the player name if the list does not contain it
				if (!worldRecords.durationHolder.Contains(player.name)) {
					worldRecords.durationHolder.Add(player.name);
				}
				worldRecords.durationWorld = playerRecord.durationBest;
				newWorldRecord = true;
			}
			if (!worldRecords.HitsTakenEmpty && playerRecord.hitsTakenBest <= worldRecords.hitsTakenWorld) {
				// If the world record was beaten, clear the list entirely
				if (playerRecord.hitsTakenBest < worldRecords.hitsTakenWorld) {
					worldRecords.hitsTakenHolder.Clear();
				}
				// Add the player name if the list does not contain it
				if (!worldRecords.hitsTakenHolder.Contains(player.name)) {
					worldRecords.hitsTakenHolder.Add(player.name);
				}
				worldRecords.hitsTakenWorld = playerRecord.hitsTakenBest;
				newWorldRecord = true;
			}
			return newWorldRecord; // Will be used to display CombatTexts of "New Record!" or "New World Record!"
		}

		/// <summary>
		/// Allows the player's playtime stat to be updated in multiplayer.
		/// </summary>
		public void SubmitPlayTimeFirstStat(NPC npc, int recordIndex) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex, Main.myPlayer])
				return; // Player must have contributed to the boss fight

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			PersonalStats statistics = modPlayer.RecordsForWorld[recordIndex].stats;
			if (statistics.playTimeFirst == -1) {
				statistics.playTimeFirst = Main.ActivePlayerFileData.GetPlayTime().Ticks;

				// Send the data to the server
				ModPacket packet = Mod.GetPacket();
				packet.Write((int)PacketMessageType.PlayTimeRecordUpdate);
				packet.Write(recordIndex);
				packet.Write(statistics.playTimeFirst);
				packet.Send(); // Multiplayer client --> Server
			}
		}

		/// <summary>
		/// Handles all of BossChecklist's custom downed variables, makring them as defeated and updating all clients when needed.
		/// </summary>
		public void HandleDownedNPCs(int npcType) {
			if ((npcType == NPCID.DD2DarkMageT1 || npcType == NPCID.DD2DarkMageT3) && !WorldAssist.downedDarkMage) {
				WorldAssist.downedDarkMage = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if ((npcType == NPCID.DD2OgreT2 || npcType == NPCID.DD2OgreT3) && !WorldAssist.downedOgre) {
				WorldAssist.downedOgre = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (npcType == NPCID.PirateShip && !WorldAssist.downedFlyingDutchman) {
				WorldAssist.downedFlyingDutchman = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (npcType == NPCID.MartianSaucerCore && !WorldAssist.downedMartianSaucer) {
				WorldAssist.downedMartianSaucer = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
		}

		/// <summary>
		/// Handles the extra npc defeation messages related to boss limbs and towers.
		/// These messages will not appear if the related configs are disabled.
		/// </summary>
		public void SendEntryMessage(NPC npc) {
			if (NPCisLimb(npc)) {
				if (!BossChecklist.ClientConfig.LimbMessages)
					return;

				// Skeletron's hands just use Skeletron's name instead of their own, so a custom name is needed
				string partName = npc.type == NPCID.SkeletronHand ? Lang.GetItemNameValue(ItemID.SkeletronHand) : npc.GetFullNetName().ToString();
				string defeatedLimb = "Mods.BossChecklist.ChatMessages.Defeated.Limb";
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(Language.GetTextValue(defeatedLimb, partName), Colors.RarityGreen);
				}
				else {
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey(defeatedLimb, partName), Colors.RarityGreen);
				}
			}
			else if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust) {
				if (!BossChecklist.ClientConfig.PillarMessages)
					return;

				string defeatedTower = "Mods.BossChecklist.ChatMessages.Defeated.Tower";
				string npcName = npc.GetFullNetName().ToString();
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(Language.GetTextValue(defeatedTower, npcName), Colors.RarityPurple);
				}
				else {
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey(defeatedTower, npcName), Colors.RarityPurple);
				}
			}
		}

		// This feature will not be extended to modded entries as those mods can handle limb messages themselves with ease, if desired.
		/// <summary>
		/// A 'limb' NPC is a part of a boss that is an extension of the boss, such as Skeletron's hands.
		/// This also considers boss's that are multiple entities, such as the Twins consisting of Retinazer and Spazmatism.
		/// </summary>
		/// <returns>Whether or not the npc is considered a 'limb'.</returns>
		public bool NPCisLimb(NPC npc) {
			int[] limbNPCs = new int[] {
				NPCID.PrimeSaw,
				NPCID.PrimeLaser,
				NPCID.PrimeCannon,
				NPCID.PrimeVice,
				NPCID.SkeletronHand,
				NPCID.GolemFistLeft,
				NPCID.GolemFistRight,
				NPCID.GolemHead
			};

			bool isTwinsRet = npc.type == NPCID.Retinazer && Main.npc.Any(x => x.type == NPCID.Spazmatism && x.active);
			bool isTwinsSpaz = npc.type == NPCID.Spazmatism && Main.npc.Any(x => x.type == NPCID.Retinazer && x.active);

			return limbNPCs.Contains(npc.type) || isTwinsRet || isTwinsSpaz;
		}
	}
}
