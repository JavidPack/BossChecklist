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
					WorldAssist.Tracker_StartingPlayers[recordIndex][Main.LocalPlayer.whoAmI] = true; // Active players when the boss spawns will be counted
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					modPlayer.Tracker_Duration[recordIndex] = 0;
					modPlayer.Tracker_HitsTaken[recordIndex] = 0;
				}
				else if (Main.netMode == NetmodeID.Server) {
					for (int j = 0; j < Main.maxPlayers; j++) {
						if (!Main.player[j].active)
							continue; // skip any inactive players

						WorldAssist.Tracker_StartingPlayers[recordIndex][j] = true; // Active players when the boss spawns will be counted

						// This is updated serverside
						PlayerAssist modPlayer = Main.player[j].GetModPlayer<PlayerAssist>();
						modPlayer.Tracker_Duration[recordIndex] = 0;
						modPlayer.Tracker_HitsTaken[recordIndex] = 0;

						// Needs to be updated client side as well
						// Send packets from the server to all participating players to reset their trackers for the recordIndex provided
						ModPacket packet = Mod.GetPacket();
						packet.Write((byte)PacketMessageType.ResetTrackers);
						packet.Write(recordIndex);
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
					}

					if (BossChecklist.DebugConfig.ShowInactiveBossCheck)
						Main.NewText(npc.FullName + ": " + FullyInactive(npc, index));

					int recordIndex = BossChecklist.bossTracker.SortedBosses[index].GetRecordIndex;
					WorldAssist.worldRecords[recordIndex].stats.totalKills++;

					// Reset world variables after record checking takes place
					WorldAssist.Tracker_ActiveEntry[recordIndex] = false;
					WorldAssist.Tracker_StartingPlayers[recordIndex] = new bool[Main.maxPlayers];
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
		/// <para>Only runs in the Singleplayer and Multiplayer netmodes.</para>
		/// </summary>
		public void CheckRecords(NPC npc, int bossIndex) {
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;

			// Player must have contributed to the boss fight
			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex][Main.myPlayer]) {
				return;
			}

			bool newRecordSet = false;
			ref PersonalStats statistics = ref modPlayer.RecordsForWorld[recordIndex].stats; // Use a ref to properly update records
			int trackedDuration = modPlayer.Tracker_Duration[recordIndex];
			int trackedhitsTaken = modPlayer.Tracker_HitsTaken[recordIndex];

			statistics.kills++; // Kills always go up, since record checking only occurs if boss was defeated

			// Check if the tracked duration was better than the current best OR if the current best has not yet been achieved
			statistics.durationPrev = trackedDuration;
			if (trackedDuration < statistics.durationBest || statistics.durationBest == -1) {
				if (statistics.durationBest != -1) {
					newRecordSet = true; // New Record should not appear above the player on the first record achieved
				}
				statistics.durationBest = trackedDuration;
			}

			// Repeat the same logic with the Hits Taken record
			statistics.hitsTakenPrev = trackedhitsTaken;
			if (trackedhitsTaken < statistics.hitsTakenBest || statistics.hitsTakenBest == -1) {
				if (statistics.hitsTakenBest != -1) {
					newRecordSet = true;
				}
				statistics.hitsTakenBest = trackedhitsTaken;
			}

			// If a new record was made, notify the player. Again, this will not show for newly set records
			if (newRecordSet) {
				modPlayer.hasNewRecord[bossIndex] = true;
				// Compare records to World Records. Players must have beaten their own records to beat a world record
				string recordSet = CheckWorldRecords(recordIndex) ? "NewWorldRecord" : "NewRecord";
				string message = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms." + recordSet);
				CombatText.NewText(Main.LocalPlayer.getRect(), Color.LightYellow, message, true);
			}
		}

		// TODO: update method to improve what it needs to do
		/// <summary>
		/// Takes the record data from all players and updates them where needed.
		/// <para>Only runs in the Server netmode.</para>
		/// </summary>
		public void CheckRecordsForServer(NPC npc, int bossIndex) {
			int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;
			foreach (Player player in Main.player) {
				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[player.whoAmI] || !WorldAssist.Tracker_StartingPlayers[recordIndex][player.whoAmI]) {
					continue;
				}

				ref PersonalStats serverStatistics = ref BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].stats;
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				int trackedDuration = modPlayer.Tracker_Duration[recordIndex];
				int trackedHitsTaken = modPlayer.Tracker_HitsTaken[recordIndex];

				// For each record type we check if its beats the current record or if it is not set already
				// If it is beaten, we add a flag to recordType to allow the record tracker numbers to override the current record
				NetRecordID recordType = NetRecordID.None;

				serverStatistics.kills++;

				serverStatistics.durationPrev = trackedDuration;
				if (trackedDuration < serverStatistics.durationBest || serverStatistics.durationBest <= 0) {
					//Console.WriteLine($"{player.name} set a new record for DURATION: {trackedDuration} (Previous Record: {serverStatistics.durationBest})");
					recordType |= NetRecordID.Duration_Best;
					serverStatistics.durationBest = trackedDuration;
				}

				serverStatistics.hitsTakenPrev = trackedHitsTaken;
				if (trackedHitsTaken < serverStatistics.hitsTakenBest || serverStatistics.hitsTakenBest < 0) {
					//Console.WriteLine($"{player.name} set a new record for HITS TAKEN: {trackedHitsTaken} (Previous Record: {serverStatistics.hitsTakenBest})");
					recordType |= NetRecordID.HitsTaken_Best;
					serverStatistics.hitsTakenBest = trackedHitsTaken;
				}
				
				// Make and send the packet
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write(recordIndex); // Which boss record are we changing?
				modPlayer.RecordsForWorld[recordIndex].stats.NetSend(packet, recordType); // Writes all the variables needed
				packet.Send(toClient: player.whoAmI); // Server --> Multiplayer client // We send to the player as only they need to see their own records
			}
			/*
			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			string[] newRecordHolders = new string[] { "", "" };
			int[] newWorldRecords = new int[]{
				worldRecords.durationWorld,
				worldRecords.hitsTakenWorld
			};
			
			if (newRecordHolders.Any(x => x != "")) {
				RecordID specificRecord = RecordID.None;
				if (newRecordHolders[0] != "") {
					specificRecord |= RecordID.Duration;
					worldRecords.durationHolder = newRecordHolders[0];
					worldRecords.durationWorld = newWorldRecords[0];
				}
				if (newRecordHolders[1] != "") {
					specificRecord |= RecordID.HitsTaken;
					worldRecords.hitsTakenHolder = newRecordHolders[1];
					worldRecords.hitsTakenWorld = newWorldRecords[1];
				}
				
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.WorldRecordUpdate);
				packet.Write(recordIndex); // Which boss record are we changing?
				worldRecords.NetSend(packet, specificRecord);
				packet.Send(); // Server --> Server (world data for everyone)
			}
			*/
		}

		// TODO: update method to be compatible with both CheckRecords and CheckRecordsMultiplayer
		/// <summary>
		/// Takes the world records with the updated player record data, updating the world records if they were beaten.
		/// </summary>
		/// <returns>Whether or not the world record was beaten</returns>
		public bool CheckWorldRecords(int recordIndex) { // Returns whether or not to stop the New Record! text from appearing to show World Record! instead
			Player player = Main.LocalPlayer;
			PersonalStats playerRecord = player.GetModPlayer<PlayerAssist>().RecordsForWorld[recordIndex].stats;
			WorldStats worldRecord = WorldAssist.worldRecords[recordIndex].stats;
			bool newRecord = false;

			if (playerRecord.durationBest < worldRecord.durationWorld || worldRecord.durationWorld <= 0) {
				// only say World Record if you the player is on a server OR if the player wasn't holding the previoes record
				newRecord = (worldRecord.durationHolder != player.name && worldRecord.durationHolder != "") || Main.netMode == NetmodeID.MultiplayerClient;
				worldRecord.durationWorld = playerRecord.durationBest;
				worldRecord.durationHolder = player.name;
			}
			if (playerRecord.hitsTakenBest < worldRecord.hitsTakenWorld || worldRecord.hitsTakenWorld < 0) {
				newRecord = (worldRecord.hitsTakenHolder != player.name && worldRecord.hitsTakenHolder != "") || Main.netMode == NetmodeID.MultiplayerClient;
				worldRecord.hitsTakenWorld = playerRecord.hitsTakenBest;
				worldRecord.hitsTakenHolder = player.name;
			}
			return newRecord;
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
					partName = "Skeletron Hand"; // TODO: Localization needed
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
