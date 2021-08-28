using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BossChecklist
{
	class NPCAssist : GlobalNPC
	{
		public override void NPCLoot(NPC npc) {
			if ((npc.type == NPCID.DD2DarkMageT1 || npc.type == NPCID.DD2DarkMageT3) && !WorldAssist.downedDarkMage) {
				WorldAssist.downedDarkMage = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			if ((npc.type == NPCID.DD2OgreT2 || npc.type == NPCID.DD2OgreT3) && !WorldAssist.downedOgre) {
				WorldAssist.downedOgre = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			if (npc.type == NPCID.PirateShip && !WorldAssist.downedFlyingDutchman) {
				WorldAssist.downedFlyingDutchman = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			if (npc.type == NPCID.MartianSaucerCore && !WorldAssist.downedMartianSaucer) {
				WorldAssist.downedMartianSaucer = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			if (!Main.dedServ && Main.gameMenu) return;
			string partName = npc.GetFullNetName().ToString();
			if (BossChecklist.ClientConfig.PillarMessages) {
				if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust) {
					if (Main.netMode == 0) Main.NewText(Language.GetTextValue("Mods.BossChecklist.BossDefeated.Tower", npc.GetFullNetName().ToString()), Colors.RarityPurple);
					else NetMessage.BroadcastChatMessage(NetworkText.FromKey("Mods.BossChecklist.BossDefeated.Tower", npc.GetFullNetName().ToString()), Colors.RarityPurple);
				}
			}
			if (NPCisLimb(npc) && BossChecklist.ClientConfig.LimbMessages) {
				if (npc.type == NPCID.SkeletronHand) partName = "Skeletron Hand";
				if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(Language.GetTextValue("Mods.BossChecklist.BossDefeated.Limb", partName), Colors.RarityGreen);
				else NetMessage.BroadcastChatMessage(NetworkText.FromKey("Mods.BossChecklist.BossDefeated.Limb", partName), Colors.RarityGreen);
			}

			// Setting a record for fastest boss kill, and counting boss kills
			// Twins check makes sure the other is not around before counting towards the record
			int index = ListedBossNum(npc);
			if (index != -1) {
				if (!BossChecklist.DebugConfig.NewRecordsDisabled && !BossChecklist.DebugConfig.RecordTrackingDisabled && TruelyDead(npc)) {
					if (Main.netMode == NetmodeID.SinglePlayer) CheckRecords(npc, index);
					else if (Main.netMode == NetmodeID.Server) CheckRecordsMultiplayer(npc, index);
				}
				if (BossChecklist.DebugConfig.ShowTDC) Main.NewText(npc.FullName + ": " + TruelyDead(npc));
			}
		}

		public override bool InstancePerEntity => true;

		public override bool PreAI(NPC npc) {
			if (npc.realLife != -1 && npc.realLife != npc.whoAmI) return true; // Checks for multi-segmented bosses?
			int listNum = ListedBossNum(npc);
			if (listNum != -1) {
				if (!WorldAssist.ActiveBossesList[listNum]) {
					for (int j = 0; j < Main.maxPlayers; j++) {
						if (!Main.player[j].active) continue;
						PlayerAssist modPlayer = Main.player[j].GetModPlayer<PlayerAssist>();
						modPlayer.RecordTimers[listNum] = 0;
					}
				}
			}
			return true;
		}

		public void CheckRecords(NPC npc, int recordIndex) {
			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
			if (!npc.playerInteraction[Main.myPlayer]) return; // Player must have contributed to the boss fight

			bool newRecordSet = false;
			BossStats bossStats = modPlayer.AllBossRecords[recordIndex].stat;

			int durationAttempt = modPlayer.RecordTimers[recordIndex];
			int currentBestDuration = bossStats.durationBest;
			
			int hitsTakenAttempt = modPlayer.AttackCounter[recordIndex];
			int currentBestHitsTaken = bossStats.hitsTakenBest;

			// Setup player's last fight attempt numbers
			modPlayer.durationLastFight = durationAttempt;
			modPlayer.hitsTakenLastFight = hitsTakenAttempt;

			bossStats.kills++; // Kills always go up, since comparing only occurs if boss was defeated
			
			// If the player has beaten their best record, we change BEST to PREV and make the current attempt the new BEST
			// Otherwise, just overwrite PREV with the current attempt
			if (durationAttempt < currentBestDuration || currentBestDuration == -1) {
				if (bossStats.durationBest == -1) newRecordSet = true; // New Record should not appear on first boss kill
				bossStats.durationPrev = currentBestDuration;
				bossStats.durationBest = durationAttempt;
			}
			else bossStats.durationPrev = durationAttempt;
			
			// Empty check should be less than 0 because 0 is achievable (No Hit)
			if (hitsTakenAttempt < currentBestHitsTaken || currentBestHitsTaken == -1) {
				if (bossStats.hitsTakenBest  == -1) newRecordSet = true;
				bossStats.hitsTakenPrev = currentBestHitsTaken;
				bossStats.hitsTakenBest = hitsTakenAttempt;
			}
			else bossStats.hitsTakenPrev = hitsTakenAttempt;
			
			// If a new record was made, notify the player
			// This will not show for newly set records
			if (newRecordSet) {
				modPlayer.hasNewRecord[recordIndex] = true;
				// Compare records to World Records. Logically, you can only beat the world records if you have beaten your own record
				// TODO: Move World Record texts to Multiplayer exclusively. Check should still happen.
				string message = CheckWorldRecords(recordIndex) ? "World Record!" : "New Record!";
				CombatText.NewText(player.getRect(), Color.LightYellow, message, true);
			}
		}

		public void CheckRecordsMultiplayer(NPC npc, int recordIndex) {
			string[] newRecordHolders = new string[] { "", "", "" };
			int[] newWorldRecords = new int[]{
				WorldAssist.worldRecords[recordIndex].stat.durationWorld,
				WorldAssist.worldRecords[recordIndex].stat.hitsTakenWorld,
			};
			for (int i = 0; i < 255; i++) {
				Player player = Main.player[i];

				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[i]) continue;
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				List<BossStats> list = BossChecklist.ServerCollectedRecords[i];
				BossStats oldRecord = list[recordIndex];

				// Establish the new records for comparing
				BossStats newRecord = new BossStats() {
					durationPrev = modPlayer.RecordTimers[recordIndex],
					hitsTakenPrev = modPlayer.AttackCounter[recordIndex],
				};

				// Setup player's last fight attempt numbers
				modPlayer.durationLastFight = newRecord.durationPrev;
				modPlayer.hitsTakenLastFight = newRecord.hitsTakenPrev;

				RecordID specificRecord = RecordID.None;
				// For each record type we check if its beats the current record or if it is not set already
				// If it is beaten, we add a flag to specificRecord to allow newRecord's numbers to override the current record
				if (newRecord.durationPrev < oldRecord.durationBest || oldRecord.durationBest <= 0) {
					Console.WriteLine($"{player.name} set a new record for DURATION: {newRecord.durationPrev} (Previous Record: {oldRecord.durationBest})");
					specificRecord |= RecordID.ShortestFightTime;
					oldRecord.durationPrev = oldRecord.durationBest;
					oldRecord.durationBest = newRecord.durationPrev;
				}
				else oldRecord.durationPrev = newRecord.durationPrev;
				
				if (newRecord.hitsTakenPrev < oldRecord.hitsTakenBest || oldRecord.hitsTakenBest < 0) {
					Console.WriteLine($"{player.name} set a new record for HITS TAKEN: {newRecord.hitsTakenPrev} (Previous Record: {oldRecord.hitsTakenBest})");
					specificRecord |= RecordID.LeastHits;
					oldRecord.hitsTakenPrev = oldRecord.hitsTakenBest;
					oldRecord.hitsTakenBest = newRecord.hitsTakenPrev;
				}
				else oldRecord.hitsTakenPrev = newRecord.hitsTakenPrev;
				
				// Make and send the packet
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write((int)recordIndex); // Which boss record are we changing?
				newRecord.NetSend(packet, specificRecord); // Writes all the variables needed
				packet.Send(toClient: i); // We send to the player. Only they need to see their own records
			}
			if (newRecordHolders.Any(x => x != "")) {
				WorldStats worldStats = WorldAssist.worldRecords[recordIndex].stat;
				RecordID specificRecord = RecordID.None;
				if (newRecordHolders[0] != "") {
					specificRecord |= RecordID.ShortestFightTime;
					worldStats.durationHolder = newRecordHolders[0];
					worldStats.durationWorld = newWorldRecords[0];
				}
				if (newRecordHolders[1] != "") {
					specificRecord |= RecordID.LeastHits;
					worldStats.hitsTakenHolder = newRecordHolders[1];
					worldStats.hitsTakenWorld = newWorldRecords[1];
				}
				
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.WorldRecordUpdate);
				packet.Write((int)recordIndex); // Which boss record are we changing?
				worldStats.NetSend(packet, specificRecord);
				packet.Send(); // To server (world data for everyone)
			}
		}

		public bool CheckWorldRecords(int recordIndex) { // Returns whether or not to stop the New Record! text from appearing to show World Record! instead
			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
			BossStats playerRecord = modPlayer.AllBossRecords[recordIndex].stat;
			WorldStats worldRecord = WorldAssist.worldRecords[recordIndex].stat;
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
			return npcType.type == NPCID.PrimeSaw
				|| npcType.type == NPCID.PrimeLaser
				|| npcType.type == NPCID.PrimeCannon
				|| npcType.type == NPCID.PrimeVice
				|| npcType.type == NPCID.SkeletronHand
				|| npcType.type == NPCID.GolemFistLeft
				|| npcType.type == NPCID.GolemFistRight
				|| npcType.type == NPCID.GolemHead
				|| (npcType.type == NPCID.Retinazer && Main.npc.Any(otherBoss => otherBoss.type == NPCID.Spazmatism && otherBoss.active))
				|| (npcType.type == NPCID.Spazmatism && Main.npc.Any(otherBoss => otherBoss.type == NPCID.Retinazer && otherBoss.active));
		}

		public static int ListedBossNum(NPC boss, bool skipEventCheck = true) { // Skipcheck incase we need it to account for events
			if (!BossChecklist.bossTracker.BossCache[boss.type]) return -1;
			
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			if (boss.type < NPCID.Count) {
				int index = BL.FindIndex(x => x.npcIDs.Any(y => y == boss.type));
				if (index != -1 && skipEventCheck && BL[index].type == EntryType.Event) return -1;
				return index;
			}
			else {
				int index = BL.FindIndex(x => x.modSource == boss.modNPC.mod.Name && x.npcIDs.Any(y => y == boss.type));
				if (index != -1 && skipEventCheck && BL[index].type == EntryType.Event) return -1;
				return index;
			}
		}

		public static int ListedBossNum(int type, string modSource) {
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			if (type < NPCID.Count) return BL.FindIndex(x => x.npcIDs.Any(y => y == type));
			else return BL.FindIndex(x => x.modSource == modSource && x.npcIDs.Any(y => y == type));
		}

		public static bool TruelyDead(NPC npc) {
			// Check all multibosses
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			int index = ListedBossNum(npc);
			if (index != -1) {
				for (int i = 0; i < BossChecklist.bossTracker.SortedBosses[index].npcIDs.Count; i++) {
					if (Main.npc.Any(x => x != npc && x.type == BossChecklist.bossTracker.SortedBosses[index].npcIDs[i] && x.active)) return false;
				}
			}
			return true;
		}
	}
}
