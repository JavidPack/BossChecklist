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
						modPlayer.MaxHealth[listNum] = Main.player[j].statLifeMax;
						modPlayer.RecordTimers[listNum] = 0;
						modPlayer.BrinkChecker[listNum] = 0;
						modPlayer.DodgeTimer[listNum] = 0;
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
			
			int dodgeTimeAttempt = modPlayer.DodgeTimer[recordIndex];
			int currentBestDodgeTime = bossStats.dodgeTimeBest;

			int brinkAttempt = modPlayer.BrinkChecker[recordIndex];
			int maxLifeAttempt = modPlayer.MaxHealth[recordIndex];
			int currentBestBrink = bossStats.healthLossBest;
			int currentBestMaxLife = bossStats.healthAtStart;

			// Setup player's last fight attempt numbers
			modPlayer.durationLastFight = durationAttempt;
			modPlayer.hitsTakenLastFight = hitsTakenAttempt;
			modPlayer.healthLossLastFight = brinkAttempt;

			bossStats.kills++; // Kills always go up, since comparing only occurs if boss was defeated
			
			// If the player has beaten their best record, we change BEST to PREV and make the current attempt the new BEST
			// Otherwise, just overwrite PREV with the current attempt
			if (durationAttempt < currentBestDuration || currentBestDuration <= 0) {
				bossStats.durationPrev = currentBestDuration;
				bossStats.durationBest = durationAttempt;
				newRecordSet = true;
			}
			else bossStats.durationPrev = durationAttempt;
			
			// Empty check should be less than 0 because 0 is achievable (No Hit)
			if (hitsTakenAttempt < currentBestHitsTaken || currentBestHitsTaken < 0) {
				bossStats.hitsTakenPrev = currentBestHitsTaken;
				bossStats.hitsTakenBest = hitsTakenAttempt;
				newRecordSet = true;
			}
			else bossStats.hitsTakenPrev = hitsTakenAttempt;

			// This is an extra record based on Hits Taken. Only overwrite if time is higher than previous.
			if (dodgeTimeAttempt > currentBestDodgeTime || currentBestDodgeTime <= 0) bossStats.dodgeTimeBest = dodgeTimeAttempt;
			
			if (brinkAttempt < currentBestBrink || currentBestBrink <= 0) {
				bossStats.healthLossPrev = currentBestBrink;
				bossStats.healthLossBest = brinkAttempt;
				bossStats.healthAtStartPrev = currentBestMaxLife;
				bossStats.healthAtStart = maxLifeAttempt;
				newRecordSet = true;
			}
			else {
				bossStats.healthLossPrev = brinkAttempt;
				bossStats.healthAtStartPrev = maxLifeAttempt;
			}

			// If a new record was made, notify the player
			if (newRecordSet) {
				modPlayer.hasNewRecord[recordIndex] = true;
				// Compare records to World Records. Logically, you can only beat the world records if you have beaten your own record
				// TODO: Move World Record texts to Multiplayer exclusively. Check should still happen.
				string message = CheckWorldRecords(recordIndex) ? "World Record!" : "New Record!";
				CombatText.NewText(player.getRect(), Color.LightYellow, message, true);
			}
		}

		public void CheckRecordsMultiplayer(NPC npc, int recordIndex) {
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
					dodgeTimePrev = modPlayer.DodgeTimer[recordIndex],
					healthLossPrev = modPlayer.BrinkChecker[recordIndex],
					healthAtStartPrev = modPlayer.MaxHealth[recordIndex]
				};

				// Setup player's last fight attempt numbers
				modPlayer.durationLastFight = newRecord.durationPrev;
				modPlayer.hitsTakenLastFight = newRecord.hitsTakenPrev;
				modPlayer.healthLossLastFight = newRecord.healthLossPrev;

				RecordID specificRecord = RecordID.None;
				
				if (newRecord.durationPrev < oldRecord.durationBest || oldRecord.durationBest <= 0) {
					Console.WriteLine($"{player.name} set a new record for DURATION: {newRecord.durationPrev} (Previous Record: {oldRecord.durationBest})");
					specificRecord |= RecordID.ShortestFightTime;
					oldRecord.durationPrev = oldRecord.durationBest;
					oldRecord.durationBest = newRecord.durationPrev;
				}
				else oldRecord.durationPrev = newRecord.durationPrev;
				specificRecord |= RecordID.PreviousFightTime;
				
				if (newRecord.hitsTakenPrev < oldRecord.hitsTakenBest || oldRecord.hitsTakenBest < 0) {
					Console.WriteLine($"{player.name} set a new record for HITS TAKEN: {newRecord.hitsTakenPrev} (Previous Record: {oldRecord.hitsTakenBest})");
					specificRecord |= RecordID.LeastHits;
					oldRecord.hitsTakenPrev = oldRecord.hitsTakenBest;
					oldRecord.hitsTakenBest = newRecord.hitsTakenPrev;
				}
				else oldRecord.hitsTakenPrev = newRecord.hitsTakenPrev;
				specificRecord |= RecordID.PreviousHits;

				if (newRecord.dodgeTimePrev > oldRecord.dodgeTimeBest || oldRecord.dodgeTimeBest <= 0) {
					Console.WriteLine($"{player.name} set a new record for BEST DODGE TIME: {newRecord.dodgeTimePrev} (Previous Record: {oldRecord.dodgeTimeBest})");
					specificRecord |= RecordID.DodgeTime;
					oldRecord.dodgeTimeBest = newRecord.dodgeTimePrev;
				}

				if (newRecord.healthLossPrev > oldRecord.healthLossBest || oldRecord.healthLossBest <= 0) {
					Console.WriteLine($"{player.name} set a new record for BEST HEALTH: {newRecord.healthLossPrev} (Previous Record: {oldRecord.healthLossBest})");
					specificRecord |= RecordID.BestBrink;
					oldRecord.healthLossPrev = oldRecord.healthLossBest;
					oldRecord.healthLossBest = newRecord.healthLossPrev;
				}
				else oldRecord.healthLossPrev = newRecord.healthLossPrev;
				specificRecord |= RecordID.PreviousBrink;
				
				// Make and send the packet
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write((int)recordIndex);
				newRecord.NetSend(packet, specificRecord);
				packet.Send(toClient: i);
			}
		}

		public bool CheckWorldRecords(int recordIndex) { // Returns whether or not to stop the New Record! text from apparing to show World Record! instead
			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
			BossStats playerRecord = modPlayer.AllBossRecords[recordIndex].stat;
			WorldStats worldRecord = WorldAssist.worldRecords[recordIndex].stat;
			bool newRecord = false;

			if (playerRecord.durationBest < worldRecord.durationWorld) {
				worldRecord.durationWorld = playerRecord.durationBest;
				worldRecord.durationHolder = player.name;
				newRecord = true;
			}
			if (playerRecord.hitsTakenBest < worldRecord.hitsTakenWorld) {
				worldRecord.hitsTakenWorld = playerRecord.hitsTakenBest;
				worldRecord.dodgeTimeWorld = playerRecord.dodgeTimeBest;
				worldRecord.hitsTakenHolder = player.name;
				newRecord = true;
			}
			if (playerRecord.healthLossBest < worldRecord.healthLossWorld) {
				worldRecord.healthLossWorld = playerRecord.healthLossBest;
				worldRecord.healthAtStartWorld = playerRecord.healthAtStart;
				worldRecord.hitsTakenHolder = player.name;
				newRecord = true;
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
