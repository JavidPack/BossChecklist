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

			int index = GetBossInfoIndex(npc.type, true);
			if (index == -1)
				return; // Make sure the npc is an entry

			// If not marked active, set to active and reset trackers for all players to start tracking records for this fight
			int recordIndex = BossChecklist.bossTracker.SortedBosses[index].GetRecordIndex;
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

		// When an NPC is killed and fully inactive, the fight has ended, stopping record trackers
		public override void OnKill(NPC npc) {
			HandleDownedNPCs(npc); // Custom downed bool code
			SendEntryMessage(npc); // Display a message for Limbs/Towers if config is enabled

			// Stop record trackers and record them to the player while also checking for records and world records
			
			int index = GetBossInfoIndex(npc.type, true);
			if (index != -1) {
				if (FullyInactive(npc, index)) {
					if (!BossChecklist.DebugConfig.NewRecordsDisabled && !BossChecklist.DebugConfig.RecordTrackingDisabled) {
						if (Main.netMode == NetmodeID.Server) {
							CheckRecordsForServer(npc, index);
						}
						else if (Main.netMode == NetmodeID.SinglePlayer) {
							CheckRecords(npc, index);
						}
						else if (Main.netMode == NetmodeID.MultiplayerClient) {
							SubmitPlayTimeFirstStat(npc, index);
						}
					}

					if (BossChecklist.DebugConfig.ShowInactiveBossCheck)
						Main.NewText(npc.FullName + ": " + FullyInactive(npc, index));

					int recordIndex = BossChecklist.bossTracker.SortedBosses[index].GetRecordIndex;
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
		/// Loops through all entries in BossTracker.SortedBosses to find BossInfo that contains the specified npc type.
		/// </summary>
		/// <param name="bossesOnly">Leave false to use this for any entry. Set to true while using this for boss record purposes.</param>
		/// <returns>The index within BossTracker.SortedBosses. Returns -1 if searching for an invalid npc type.</returns>
		public static int GetBossInfoIndex(int npcType, bool bossesOnly = false) {
			if (!BossChecklist.bossTracker.BossCache[npcType])
				return -1;

			List<BossInfo> BossInfoList = BossChecklist.bossTracker.SortedBosses;
			for (int index = 0; index < BossInfoList.Count; index++) {
				if (bossesOnly && BossInfoList[index].type != EntryType.Boss) {
					continue;
				}
				if (BossInfoList[index].npcIDs.Contains(npcType)) {
					return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Searches for all npc types of a given SortedBosses index and checks their active status within Main.npc. Does not include the passed NPC itself unless specified.
		/// </summary>
		/// <returns>Whether or not an npc listed in the specified entry's npc pool is active or not.</returns>
		public static bool FullyInactive(NPC npc, int index, bool includePassedNPC = false) {
			// Check all multibosses to see if the NPC is truly dead
			// index should be checked for a value of -1 before submitting, but just in case...
			if (index == -1)
				return !npc.active;

			// Loop through the npc types of the given index and check if any are currently an active npc
			foreach (int bossType in BossChecklist.bossTracker.SortedBosses[index].npcIDs) {
				if (Main.npc.Any(nPC => nPC != npc && nPC.active && nPC.type == bossType))
					return false;
			}

			// If none of the npc types are active, return the NPC's own active state
			return !includePassedNPC || !npc.active;
		}

		/// <summary>
		/// Uses an entry's custom despawn message logic to determine what string or localization key should be sent.
		/// </summary>
		/// <returns>The despawn message of the provided npc.</returns>
		public static string GetDespawnMessage(NPC npc, int index) {
			if (npc.life <= 0) {
				return ""; // If the boss was killed, don't display a despawn message
			}

			string messageType = BossChecklist.ClientConfig.DespawnMessageType;

			// Provide the npc for the custom message
			// If null or empty, give a generic message instead of a custom one
			if (messageType == "Unique") {
				string customMessage = BossChecklist.bossTracker.SortedBosses[index].customDespawnMessages(npc);
				if (!string.IsNullOrEmpty(customMessage)) {
					return customMessage;
				}
			}

			// If the Unique message was empty or the player is using Generic despawn messages, try to find an appropriate despawn message to send
			// Return a generic despawn message is any player is left alive or return a boss victory despawn message if all player's were killed
			if (messageType != "Disabled") {
				return Main.player.Any(plr => plr.active && !plr.dead) ? "Mods.BossChecklist.BossDespawn.Generic" : "Mods.BossChecklist.BossVictory.Generic";
			}
			// The despawn message feature was disabled. Return an empty message.
			return "";
		}

		/// <summary>
		/// Takes the data from record trackers and updates the player's saved records accordingly.
		/// <para>Only runs in the Singleplayer netmode.</para>
		/// </summary>
		private void CheckRecords(NPC npc, int bossIndex) {
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;

			// Player must have contributed to the boss fight
			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex, Main.myPlayer]) {
				return;
			}

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
				if (statistics.durationBest != -1)
					newRecordSet = true; // New Record should not appear above the player on the first record achieved
			}

			// Repeat the same logic with the Hits Taken record
			statistics.hitsTakenPrev = trackedHitsTaken;
			if (trackedHitsTaken < statistics.hitsTakenBest || statistics.hitsTakenBest == -1) {
				statistics.hitsTakenPrevBest = statistics.hitsTakenBest;
				statistics.hitsTakenBest = trackedHitsTaken;
				if (statistics.hitsTakenBest != -1)
					newRecordSet = true;
			}

			// If a new record was made, notify the player. Again, this will not show for newly set records
			if (newRecordSet) {
				modPlayer.hasNewRecord[recordIndex] = true;
				// Compare records to World Records. Players must have beaten their own records to beat a world record
				string recordSet = CheckWorldRecords(recordIndex) ? "NewWorldRecord" : "NewRecord";
				string message = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms." + recordSet);
				CombatText.NewText(Main.LocalPlayer.getRect(), Color.LightYellow, message, true);
			}
		}

		/// <summary>
		/// Compares the record tracker data from all players and updates all records accordingly to the server.
		/// A ModPacket will be sent to update the personal record data for player clients afterwards.
		/// World Records will be checked and updated once all records are finalized and sent.
		/// <para>Only runs in the Server netmode.</para>
		/// </summary>
		private void CheckRecordsForServer(NPC npc, int bossIndex) {
			int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;

			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			bool newRecordSet = false;
			List<int> dHolders = new List<int>();
			List<int> htHolders = new List<int>();
			int dCompareValue = -1;
			int htCompareValue = -1;

			foreach (Player player in Main.player) {
				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[player.whoAmI] || !WorldAssist.Tracker_StartingPlayers[recordIndex, player.whoAmI]) {
					continue;
				}

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
					if (serverStatistics.durationBest != -1)
						newRecordSet = true;
				}

				serverStatistics.hitsTakenPrev = trackedHitsTaken;
				if (trackedHitsTaken < serverStatistics.hitsTakenBest || serverStatistics.hitsTakenBest == -1) {
					recordType |= NetRecordID.HitsTaken_Best;
					serverStatistics.hitsTakenPrevBest = serverStatistics.hitsTakenBest;
					serverStatistics.hitsTakenBest = trackedHitsTaken;
					if (serverStatistics.durationBest != -1)
						newRecordSet = true;
				}

				// Make and send the packet
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write(recordIndex); // Which boss record are we changing?
				serverStatistics.NetSend(packet, recordType);
				packet.Send(toClient: player.whoAmI); // Server --> Multiplayer client // We send to the player as only they need to see their own records


				if (BossChecklist.DebugConfig.DisableWorldRecords)
					continue;

				// Check for world records
				int hitsTakenValue = htCompareValue == -1 ? worldRecords.hitsTakenWorld : htCompareValue;

				if (worldRecords.durationWorld == -1 || trackedDuration <= worldRecords.durationWorld) {
					dHolders.Add(player.whoAmI); // Duration is the same for every player, since this code is ran on GlobalNPC.OnKill
					dCompareValue = trackedDuration;
				}
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

			// If no world records were made, skip the update process
			if (dCompareValue == -1 && htCompareValue == -1)
				return;

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"World records have been updated!");
			Console.ResetColor();

			NetRecordID worldRecordType = NetRecordID.None;
			bool durationBeaten = false;
			bool hitsTakenBeaten = false;

			// Apply new world record values and send them through packets for player clients
			if (dCompareValue == -1 && dHolders.Count > 0) {
				worldRecordType |= NetRecordID.Duration_Best;

				// If the compare value is better than the original record, clear the holders list
				// New names should just be added without clearing the list and this will need to be communicated within PcketHandling
				if (dCompareValue < worldRecords.durationWorld) {
					worldRecords.durationHolder.Clear();
					durationBeaten = true;
				}
				for (int i = 0; i < dHolders.Count; i++) {
					if (!worldRecords.durationHolder.Contains(Main.player[dHolders[i]].name))
						worldRecords.durationHolder.Add(Main.player[dHolders[i]].name);
				}
				worldRecords.durationWorld = dCompareValue;
				//CheckWorldRecordsForServer(recordIndex, Main.pla.whoAmI); ?????????????????????????????????????????????????????????????????????????????????????????
			}
			if (htCompareValue == -1 && htHolders.Count > 0) {
				worldRecordType |= NetRecordID.HitsTaken_Best;

				if (htCompareValue < worldRecords.hitsTakenWorld) {
					worldRecords.hitsTakenHolder.Clear();
					hitsTakenBeaten = true;
				}
				for (int i = 0; i < htHolders.Count; i++) {
					if (!worldRecords.hitsTakenHolder.Contains(Main.player[htHolders[i]].name))
						worldRecords.hitsTakenHolder.Add(Main.player[htHolders[i]].name);
				}
				worldRecords.hitsTakenWorld = htCompareValue;
			}

			foreach (Player player in Main.player) {
				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[player.whoAmI] || !WorldAssist.Tracker_StartingPlayers[recordIndex, player.whoAmI]) {
					continue;
				}

				ModPacket packet2 = Mod.GetPacket();
				packet2.Write((int)PacketMessageType.WorldRecordUpdate);
				packet2.Write(recordIndex);
				packet2.Write(durationBeaten);
				packet2.Write(hitsTakenBeaten);
				worldRecords.NetSend(packet2, worldRecordType);
				packet2.Write(newRecordSet);
				packet2.Write(dHolders.Contains(player.whoAmI) || htHolders.Contains(player.whoAmI));
				packet2.Send(toClient: player.whoAmI); // Server --> Multiplayer client
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

		public void SubmitPlayTimeFirstStat(NPC npc, int bossIndex) {
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;

			// Player must have contributed to the boss fight
			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex, Main.myPlayer]) {
				return;
			}

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

		// All of BossChecklist's custom downed variables will be handled here
		public void HandleDownedNPCs(NPC npc) {
			if ((npc.type == NPCID.DD2DarkMageT1 || npc.type == NPCID.DD2DarkMageT3) && !WorldAssist.downedDarkMage) {
				WorldAssist.downedDarkMage = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if ((npc.type == NPCID.DD2OgreT2 || npc.type == NPCID.DD2OgreT3) && !WorldAssist.downedOgre) {
				WorldAssist.downedOgre = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (npc.type == NPCID.PirateShip && !WorldAssist.downedFlyingDutchman) {
				WorldAssist.downedFlyingDutchman = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (npc.type == NPCID.MartianSaucerCore && !WorldAssist.downedMartianSaucer) {
				WorldAssist.downedMartianSaucer = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
		}

		// Depending on what configs are enabled, this will send messages in chat displaying what NPC has been defeated
		public void SendEntryMessage(NPC npc) {
			if (NPCisLimb(npc)) {
				if (!BossChecklist.ClientConfig.LimbMessages)
					return;

				string partName = npc.GetFullNetName().ToString();
				if (npc.type == NPCID.SkeletronHand) {
					partName = Lang.GetItemNameValue(ItemID.SkeletronHand);
				}
				string defeatedLimb = "Mods.BossChecklist.BossDefeated.Limb";
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
				string defeatedTower = "Mods.BossChecklist.BossDefeated.Tower";
				string npcName = npc.GetFullNetName().ToString();
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(Language.GetTextValue(defeatedTower, npcName), Colors.RarityPurple);
				}
				else {
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey(defeatedTower, npcName), Colors.RarityPurple);
				}
			}
		}

		// TODO: Expand on this idea for modded entries?
		public bool NPCisLimb(NPC npcType) {
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

			bool isTwinsRet = npcType.type == NPCID.Retinazer && Main.npc.Any(x => x.type == NPCID.Spazmatism && x.active);
			bool isTwinsSpaz = npcType.type == NPCID.Spazmatism && Main.npc.Any(x => x.type == NPCID.Retinazer && x.active);

			return limbNPCs.Contains(npcType.type) || isTwinsRet || isTwinsSpaz;
		}
	}
}
