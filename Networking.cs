using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BossChecklist
{
	enum PacketMessageType : byte {
		RequestHideBoss,
		RequestClearHidden,
		RequestMarkedDownEntry,
		RequestClearMarkedDowns,
		SendRecordsToServer,
		RecordUpdate,
		WorldRecordUpdate,
		ResetTrackers,
		PlayTimeRecordUpdate
	}

	internal class Networking {

		/// <summary>
		/// Send a packet to the server to add, remove, or clear entries from the hidden entries list.
		/// </summary>
		/// <param name="Key">Provide an entry key to add/remove the entry. Leave blank to clear the entire hidden list.</param>
		public static void RequestHiddenEntryUpdate(string Key = null, bool hide = true) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write(string.IsNullOrEmpty(Key) ? (byte)PacketMessageType.RequestClearHidden : (byte)PacketMessageType.RequestHideBoss);
			if (!string.IsNullOrEmpty(Key)) {
				packet.Write(Key);
				packet.Write(hide);
			}
			packet.Send(); // Multiplayer --> Server
		}

		/// <summary>
		/// Send a packet to the server to add, remove, or clear entries from the marked entries list.
		/// </summary>
		/// <param name="Key">Provide an entry key to add/remove the entry. Leave blank to clear the entire marked list.</param>
		public static void RequestMarkedEntryUpdate(string Key = null, bool mark = true) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write(string.IsNullOrEmpty(Key) ? (byte)PacketMessageType.RequestClearMarkedDowns : (byte)PacketMessageType.RequestMarkedDownEntry);
			if (!string.IsNullOrEmpty(Key)) {
				packet.Write(Key);
				packet.Write(mark);
			}
			packet.Send(); // Multiplayer --> Server
		}


		/// <summary>
		/// Updates the player's first victory play time record and sends it to the server.
		/// <para>Only runs on a Multiplayer client.</para>
		/// </summary>
		public static void SubmitPlayTimeToServer(NPC npc, int recordIndex) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (!npc.playerInteraction[Main.myPlayer] || !WorldAssist.Tracker_StartingPlayers[recordIndex, Main.myPlayer])
				return; // Player must have contributed to the boss fight

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			PersonalStats statistics = modPlayer.RecordsForWorld[recordIndex].stats;
			if (statistics.playTimeFirst == -1)
				return;

			statistics.playTimeFirst = Main.ActivePlayerFileData.GetPlayTime().Ticks; // update player's records

			// Send the data to the server
			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write((byte)PacketMessageType.PlayTimeRecordUpdate);
			packet.Write(recordIndex);
			packet.Write(statistics.playTimeFirst);
			packet.Send(); // Multiplayer client --> Server
		}

		/// <summary>
		/// Compares the record tracker data from all players against the server's saved records.
		/// World records will be checked and updated during this process.
		/// A ModPacket will be sent to update the personal record data for player clients afterwards.
		/// <para>Only runs on the Server client.</para>
		/// </summary>
		public static void UpdateServerRecords(NPC npc, int recordIndex) {
			if (Main.netMode != NetmodeID.Server || BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			Dictionary<int, NetRecordID> netRecords = new Dictionary<int, NetRecordID>();

			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			int Current_Duration_Wr = worldRecords.durationWorld;
			int Current_HitsTaken_Wr = worldRecords.durationWorld;
			List<int> WR_Duration_Holders = new List<int>();
			List<int> WR_HitsTaken_Holders = new List<int>();

			foreach (Player player in Main.player) {
				if (!player.active || !npc.playerInteraction[player.whoAmI] || !WorldAssist.Tracker_StartingPlayers[recordIndex, player.whoAmI])
					continue; // Players must be active AND have interacted with the boss AND cannot have recordingstats disabled

				PersonalStats serverRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].stats;
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				int trackedDuration = modPlayer.Tracker_Duration[recordIndex];
				int trackedHitsTaken = modPlayer.Tracker_HitsTaken[recordIndex];

				// The trackers will be compared against all record category types in an attempt to update them
				// If a record is beaten by any trackers, the record will be overwritten using a NetRecordID flag
				// If the tracked records do NOT override any other records, only the Previous Attempt record category will be updated
				NetRecordID recordType = NetRecordID.PreviousAttemptOnly;
				serverRecords.kills++;
				serverRecords.durationPrev = trackedDuration;
				serverRecords.hitsTakenPrev = trackedHitsTaken;

				if (BossChecklist.DebugConfig.NewRecordsDisabled) {
					// If this was the first down for the boss (where play time value is -1), a first record has been made
					if (serverRecords.playTimeFirst == -1) {
						recordType |= NetRecordID.FirstVictory;
						serverRecords.durationFirst = trackedDuration;
						serverRecords.hitsTakenFirst = trackedHitsTaken;
					}

					// Check for best records as well (This would apply on first records as well, as the best records would be -1)
					// Overwrite PrevBest records with the 'current' one first. If overwritten with -1, nothing changes.
					if (trackedDuration < serverRecords.durationBest || serverRecords.durationBest == -1) {
						recordType |= NetRecordID.PersonalBest_Duration;
						serverRecords.durationPrevBest = serverRecords.durationBest;
						serverRecords.durationBest = trackedDuration;

						// If a new record has been made, also check if it is a world record
						// If the tracked record matches the world record, simply add the player to the list of holders
						// If the tracked record is better than the world record, create a new list and update the world record value
						if (!BossChecklist.DebugConfig.DisableWorldRecords) {
							if (worldRecords.durationWorld == trackedDuration) {
								WR_Duration_Holders.Add(player.whoAmI);
							}
							else if (worldRecords.durationWorld > trackedDuration) {
								worldRecords.durationWorld = trackedDuration;
								WR_Duration_Holders = new List<int>() { player.whoAmI };
							}
						}
					}

					if (trackedHitsTaken < serverRecords.hitsTakenBest || serverRecords.hitsTakenBest == -1) {
						recordType |= NetRecordID.PersonalBest_HitsTaken;
						serverRecords.hitsTakenPrevBest = serverRecords.hitsTakenBest;
						serverRecords.hitsTakenBest = trackedHitsTaken;

						// In this world record scenario, players can have varying amounts of hits taken, where as duration would always be the same
						// This check will also serve to see if the current player has a better record than any previously checked players
						if (!BossChecklist.DebugConfig.DisableWorldRecords) {
							if (worldRecords.hitsTakenWorld == trackedHitsTaken) {
								WR_HitsTaken_Holders.Add(player.whoAmI);
							}
							else if (worldRecords.hitsTakenWorld > trackedHitsTaken) {
								worldRecords.hitsTakenWorld = trackedHitsTaken;
								WR_HitsTaken_Holders = new List<int>() { player.whoAmI };
							}
						}
					}
				}

				netRecords.TryAdd(player.whoAmI, recordType); // add each player's recordType to a dictionary to look for world records before making a packet
			}

			// If the players' new world record is better than the old world record, clear the holder list
			if (Current_Duration_Wr != worldRecords.durationWorld)
				worldRecords.durationHolder.Clear();

			if (Current_HitsTaken_Wr != worldRecords.hitsTakenWorld)
				worldRecords.hitsTakenHolder.Clear();

			NetRecordID worldNetRecord = NetRecordID.None; // prepare world record record type for packet

			foreach (KeyValuePair<int, NetRecordID> plrRecord in netRecords) {
				// First, loop through the actual world record breakers and apply the flag necessary to them
				if (!BossChecklist.DebugConfig.DisableWorldRecords) {
					if (WR_Duration_Holders.Contains(plrRecord.Key)) {
						netRecords[plrRecord.Key] |= NetRecordID.WorldRecord_Duration;
						worldRecords.durationHolder.Add(Main.player[plrRecord.Key].name);
						worldNetRecord |= NetRecordID.WorldRecord_Duration;
					}

					if (WR_HitsTaken_Holders.Contains(plrRecord.Key)) {
						netRecords[plrRecord.Key] |= NetRecordID.WorldRecord_HitsTaken;
						worldRecords.hitsTakenHolder.Add(Main.player[plrRecord.Key].name);
						worldNetRecord |= NetRecordID.WorldRecord_HitsTaken;
					}
				}
				
				// Then send the mod packet to the client
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write(recordIndex);
				BossChecklist.ServerCollectedRecords[plrRecord.Key][recordIndex].stats.NetSend(packet, plrRecord.Value);
				packet.Send(toClient: plrRecord.Key); // Server --> Multiplayer client (Player's only need to see their own records)
			}

			if (!BossChecklist.DebugConfig.DisableWorldRecords) {
				// Finally update all clients with the updated world records
				foreach (Player player in Main.player) {
					if (!player.active)
						continue;

					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.WorldRecordUpdate);
					packet.Write(recordIndex);
					worldRecords.NetSend(packet, worldNetRecord);
					packet.Send(toClient: player.whoAmI); // Server --> Multiplayer client
				}
			}
		}
	}
}
