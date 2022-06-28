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
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	class NPCAssist : GlobalNPC
	{
		public override void OnKill(NPC npc) {
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

			if (!Main.dedServ && Main.gameMenu) {
				return;
			}

			if (BossChecklist.ClientConfig.PillarMessages) {
				if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust) {
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

			if (NPCisLimb(npc) && BossChecklist.ClientConfig.LimbMessages) {
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

			// Setting a record for fastest boss kill, and counting boss kills
			// Twins check makes sure the other is not around before counting towards the record
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			int index = GetBossInfoIndex(npc);
			if (index != -1) {
				if (FullyInactive(npc, index)) {
					if (!BossChecklist.DebugConfig.NewRecordsDisabled && !BossChecklist.DebugConfig.RecordTrackingDisabled) {
						if (Main.netMode == NetmodeID.SinglePlayer) {
							CheckRecords(npc, index);
						}
						else if (Main.netMode == NetmodeID.Server) {
							CheckRecordsMultiplayer(npc, index);
						}
					}
					if (BossChecklist.DebugConfig.ShowInactiveBossCheck) {
						Main.NewText(npc.FullName + ": " + FullyInactive(npc, index));
					}
					WorldAssist.worldRecords[BossLogUI.PageNumToRecordIndex(WorldAssist.worldRecords, index)].stats.totalKills++;

					// Reset world variables after record checking takes place
					WorldAssist.ActiveBossesList[index] = false;
					WorldAssist.StartingPlayers[index] = new bool[Main.maxPlayers];
				}
			}
		}

		public override bool InstancePerEntity => true;

		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			if (npc.realLife != -1 && npc.realLife != npc.whoAmI) {
				return; // Checks for multi-segmented bosses?
			}

			int index = GetBossInfoIndex(npc);
			if (index == -1) {
				return; // Make sure the npc is an entry
			}

			// If not marked active, set to active and reset trackers for all players to start tracking records for this fight
			if (!WorldAssist.ActiveBossesList[index]) {
				WorldAssist.ActiveBossesList[index] = true;
				for (int j = 0; j < Main.maxPlayers; j++) {
					if (!Main.player[j].active) {
						continue; // skip any inactive players
					}
					else {
						WorldAssist.StartingPlayers[index][j] = true; // Active players when the boss spawns will be counted
					}
					// Reset Timers and counters so we can start recording the next fight
					PlayerAssist modPlayer = Main.player[j].GetModPlayer<PlayerAssist>();
					int recordIndex = BossLogUI.PageNumToRecordIndex(modPlayer.RecordsForWorld, index);
					modPlayer.Tracker_Duration[recordIndex] = 0;
					modPlayer.Tracker_HitsTaken[recordIndex] = 0;
				}
			}
		}

		public void CheckRecords(NPC npc, int bossIndex) {
			// Player must have contributed to the boss fight
			if (!npc.playerInteraction[Main.myPlayer]) {
				return;
			}

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			bool newRecordSet = false;
			int recordIndex = BossLogUI.PageNumToRecordIndex(modPlayer.RecordsForWorld, bossIndex);
			PersonalStats bossStats = modPlayer.RecordsForWorld[recordIndex].stats;

			int durationAttempt = modPlayer.Tracker_Duration[recordIndex];
			int currentBestDuration = bossStats.durationBest;
			
			int hitsTakenAttempt = modPlayer.Tracker_HitsTaken[recordIndex];
			int currentBestHitsTaken = bossStats.hitsTakenBest;

			bossStats.kills++; // Kills always go up, since comparing only occurs if boss was defeated

			// If the player has beaten their best record, we change BEST to PREV and make the current attempt the new BEST
			// Otherwise, just overwrite PREV with the current attempt
			if (durationAttempt < currentBestDuration || currentBestDuration == -1) {
				// New Record should not appear on first boss kill, which would appear as -1
				if (bossStats.durationBest != -1) {
					newRecordSet = true;
				}
				bossStats.durationPrev = currentBestDuration;
				bossStats.durationBest = durationAttempt;
			}
			else {
				bossStats.durationPrev = durationAttempt;
			}

			// Empty check should be less than 0 because 0 is achievable (No Hit)
			if (hitsTakenAttempt < currentBestHitsTaken || currentBestHitsTaken == -1) {
				if (bossStats.hitsTakenBest != -1) {
					newRecordSet = true;
				}
				bossStats.hitsTakenPrev = currentBestHitsTaken;
				bossStats.hitsTakenBest = hitsTakenAttempt;
			}
			else {
				bossStats.hitsTakenPrev = hitsTakenAttempt;
			}

			// If a new record was made, notify the player
			// This will not show for newly set records
			if (newRecordSet) {
				modPlayer.hasNewRecord[bossIndex] = true;
				// Compare records to World Records. Logically, you can only beat the world records if you have beaten your own record
				// TODO: Move World Record texts to Multiplayer exclusively. Check should still happen.
				string recordType = "Mods.BossChecklist.BossLog.Terms.";
				recordType += CheckWorldRecords(recordIndex) ? "NewWorldRecord" : "NewRecord";
				string message = Language.GetTextValue(recordType);
				CombatText.NewText(Main.LocalPlayer.getRect(), Color.LightYellow, message, true);
			}
		}

		public void CheckRecordsMultiplayer(NPC npc, int bossIndex) {
			int recordIndex = BossLogUI.PageNumToRecordIndex(WorldAssist.worldRecords, bossIndex);
			WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
			string[] newRecordHolders = new string[] { "", "" };
			int[] newWorldRecords = new int[]{
				worldRecords.durationWorld,
				worldRecords.hitsTakenWorld
			};
			for (int i = 0; i < 255; i++) {
				Player player = Main.player[i];

				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[i]) {
					continue;
				}
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				List<PersonalStats> serverRecords = BossChecklist.ServerCollectedRecords[i];
				PersonalStats oldRecord = serverRecords[recordIndex];

				// Establish the new records for comparing
				PersonalStats newRecord = new PersonalStats() {
					durationPrev = modPlayer.Tracker_Duration[recordIndex],
					hitsTakenPrev = modPlayer.Tracker_HitsTaken[recordIndex]
				};

				RecordID specificRecord = RecordID.None;
				// For each record type we check if its beats the current record or if it is not set already
				// If it is beaten, we add a flag to specificRecord to allow newRecord's numbers to override the current record
				if (newRecord.durationPrev < oldRecord.durationBest || oldRecord.durationBest <= 0) {
					Console.WriteLine($"{player.name} set a new record for DURATION: {newRecord.durationPrev} (Previous Record: {oldRecord.durationBest})");
					specificRecord |= RecordID.Duration;
					oldRecord.durationPrev = oldRecord.durationBest;
					oldRecord.durationBest = newRecord.durationPrev;
				}
				else {
					oldRecord.durationPrev = newRecord.durationPrev;
				}

				if (newRecord.hitsTakenPrev < oldRecord.hitsTakenBest || oldRecord.hitsTakenBest < 0) {
					Console.WriteLine($"{player.name} set a new record for HITS TAKEN: {newRecord.hitsTakenPrev} (Previous Record: {oldRecord.hitsTakenBest})");
					specificRecord |= RecordID.HitsTaken;
					oldRecord.hitsTakenPrev = oldRecord.hitsTakenBest;
					oldRecord.hitsTakenBest = newRecord.hitsTakenPrev;
				}
				else {
					oldRecord.hitsTakenPrev = newRecord.hitsTakenPrev;
				}
				
				// Make and send the packet
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write((int)recordIndex); // Which boss record are we changing?
				packet.Write((int)player.whoAmI); // Player index
				newRecord.NetSend(packet, specificRecord); // Writes all the variables needed
				packet.Send(toClient: i); // We send to the player. Only they need to see their own records
			}
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
				packet.Write((int)recordIndex); // Which boss record are we changing?
				worldRecords.NetSend(packet, specificRecord);
				packet.Send(); // To server (world data for everyone)
			}
		}

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

		public static int GetBossInfoIndex(NPC boss, bool skipEventCheck = true) { // Skipcheck incase we need it to account for events
			if (!BossChecklist.bossTracker.BossCache[boss.type]) {
				return -1;
			}

			List<BossInfo> BossInfoList = BossChecklist.bossTracker.SortedBosses;
			for (int index = 0; index < BossInfoList.Count; index++) {
				if (BossInfoList[index].type == EntryType.Event && skipEventCheck) {
					continue;
				}
				if (BossInfoList[index].npcIDs.Contains(boss.type)) {
					return index;
				}
			}
			return -1;
		}

		public static int GetBossInfoIndex(NPCDefinition npc) {
			if (npc.IsUnloaded || npc.Type == -1 || npc.Type == NPCID.None) {
				return -1;
			}
			if (!BossChecklist.bossTracker.BossCache[npc.Type]) {
				return -1;
			}

			List<BossInfo> BossInfoList = BossChecklist.bossTracker.SortedBosses;
			for (int index = 0; index < BossInfoList.Count; index++) {
				if (BossInfoList[index].type == EntryType.Event) {
					continue;
				}
				if (BossInfoList[index].npcIDs.Contains(npc.Type)) {
					return index;
				}
			}
			return -1;
		}

		public static bool FullyInactive(NPC npc, int index) {
			// Check all multibosses to see if the NPC is truly dead
			// index should be checked for a value of -1 before submitting, but just in case...
			if (index == -1) {
				return !npc.active;
			}

			foreach (int bossType in BossChecklist.bossTracker.SortedBosses[index].npcIDs) {
				if (Main.npc.Any(nPC => nPC != npc && nPC.active && nPC.type == bossType)) {
					return false;
				}
			}
			return true;
		}
	}
}
