using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

// Dodge counter not working properly

namespace BossChecklist
{
	class NPCAssist : GlobalNPC
	{
		public override void NPCLoot(NPC npc) {
			if (!Main.dedServ && Main.gameMenu) return;
			string partName = npc.GetFullNetName().ToString();
			if (BossChecklist.ClientConfig.PillarMessages) {
				if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust) {
					if (Main.netMode == 0) Main.NewText("The " + npc.GetFullNetName().ToString() + " has been destroyed", Colors.RarityPurple);
					else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The " + npc.GetFullNetName().ToString() + " has been destroyed"), Colors.RarityPurple);
				}
			}
			if (NPCisLimb(npc) && BossChecklist.ClientConfig.LimbMessages) {
				if (npc.type == NPCID.SkeletronHand) partName = "Skeletron Hand";
				if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText("The " + partName + " is down!", Colors.RarityGreen);
				else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The " + partName + " is down!"), Colors.RarityGreen);
			}

			// Setting a record for fastest boss kill, and counting boss kills
			// Twins check makes sure the other is not around before counting towards the record
			if (ListedBossNum(npc) != -1) {
				if (TruelyDead(npc)) {
					if (Main.netMode == NetmodeID.SinglePlayer) CheckRecords(npc, ListedBossNum(npc));
					else if (Main.netMode == NetmodeID.Server) CheckRecordsMultiplayer(npc, ListedBossNum(npc));
				}
				if (BossChecklist.DebugConfig.ShowTDC) Main.NewText(npc.FullName + ": " + TruelyDead(npc));
			}
		}

		public override bool InstancePerEntity => true;
		
		List<Player> StartingPlayers = new List<Player>(); // Created for each NPC

		public override bool PreAI(NPC npc) {
			if (npc.realLife != -1 && npc.realLife != npc.whoAmI) return true; // Checks for multi-segmented bosses?
			int listNum = ListedBossNum(npc);
			if (listNum != -1 && StartingPlayers.Count == 0) {
				StartingPlayers = new List<Player>();
				foreach (Player player in Main.player) {
					if (player.active) StartingPlayers.Add(player);
				}
				//TODO: Fix ghost bosses from the following code below? (Index out of bounds range warning while in MP?)
				if (!WorldAssist.ActiveBossesList[listNum]) {
					//WorldAssist.ActiveBossesList[listNum] = true; (New system introduced, but might be better to have this)
					foreach (Player player in StartingPlayers) {
						PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
						modPlayer.MaxHealth[listNum] = player.statLifeMax;
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
			PlayerAssist modplayer = player.GetModPlayer<PlayerAssist>();
			if (!modplayer.RecordingStats || !npc.playerInteraction[Main.myPlayer]) return; // RecordingStats must be enabled!

			bool newRecordSet = false;

			int recordAttempt = modplayer.RecordTimers[recordIndex]; // Trying to set a new record
			BossStats bossStats = modplayer.AllBossRecords[recordIndex].stat;
			int currentRecord = bossStats.durationBest;
			int worstRecord = bossStats.durationWorst;

			bossStats.durationLast = recordAttempt;

			int brinkAttempt = modplayer.BrinkChecker[recordIndex]; // Trying to set a new record
			int MaxLife = modplayer.MaxHealth[recordIndex];
			int currentBrink = bossStats.healthLossBest;
			int worstBrink = bossStats.healthLossWorst;

			bossStats.healthLossLast = brinkAttempt;
			double lastHealth = (double)brinkAttempt / (double)MaxLife;
			bossStats.healthLossLastPercent = (int)(lastHealth * 100);

			int dodgeTimeAttempt = modplayer.DodgeTimer[recordIndex];
			int currentDodgeTime = bossStats.dodgeTimeBest;
			int dodgeAttempt = modplayer.AttackCounter[recordIndex];
			int currentDodges = bossStats.hitsTakenBest;
			int worstDodges = bossStats.hitsTakenWorst;

			bossStats.dodgeTimeLast = dodgeTimeAttempt;
			bossStats.hitsTakenLast = dodgeAttempt;

			// Increase kill count
			bossStats.kills++;

			if (recordAttempt < currentRecord && currentRecord != 0 && worstRecord <= 0) {
				// First make the current record the worst record if no worst record has been made and a new record was made
				bossStats.durationWorst = currentRecord;
			}
			if (recordAttempt < currentRecord || currentRecord <= 0) {
				//The player has beaten their best record, so we have to overwrite the old record with the new one
				bossStats.durationBest = recordAttempt;
				newRecordSet = true;
			}
			else if (recordAttempt > worstRecord || worstRecord <= 0) {
				//The player has beaten their worst record, so we have to overwrite the old record with the new one
				bossStats.durationWorst = recordAttempt;
			}

			if (brinkAttempt > currentBrink && currentBrink != 0 && worstBrink <= 0) {
				bossStats.healthLossWorst = currentBrink;
			}
			if (brinkAttempt > currentBrink || currentBrink <= 0) {
				bossStats.healthLossBest = brinkAttempt;
				double newHealth = (double)brinkAttempt / (double)MaxLife; // Casts may be redundant, but this setup doesn't work without them.
				bossStats.healthLossBestPercent = (int)(newHealth * 100);
				newRecordSet = true;
			}
			else if (brinkAttempt < worstBrink || worstBrink <= 0) {
				bossStats.healthLossWorst = brinkAttempt;
				double newHealth = (double)brinkAttempt / (double)MaxLife; // Casts may be redundant, but this setup doesn't work without them.
				bossStats.healthLossWorstPercent = (int)(newHealth * 100);
			}

			if (dodgeTimeAttempt > currentDodgeTime || currentDodgeTime < 0) {
				// There is no "worse record" for this one so just overwrite any better records made
				bossStats.dodgeTimeBest = dodgeTimeAttempt;
			}

			if (dodgeAttempt < currentDodges || currentDodges <= 0) {
				bossStats.hitsTakenBest = dodgeAttempt;
				if (worstDodges == 0) bossStats.hitsTakenWorst = currentDodges;
				newRecordSet = true;
			}
			else if (dodgeAttempt > worstDodges || worstDodges < 0) {
				bossStats.hitsTakenWorst = dodgeAttempt;
			}

			// If a new record was made, notify the player
			if (newRecordSet) {
				CombatText.NewText(player.getRect(), Color.LightYellow, "New Record!", true);
				modplayer.hasNewRecord = true;
			}
		}

		public void CheckRecordsMultiplayer(NPC npc, int recordIndex) {
			for (int i = 0; i < 255; i++) {
				Player player = Main.player[i];

				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[i] || !player.GetModPlayer<PlayerAssist>().RecordingStats) continue;
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				List<BossStats> list = BossChecklist.ServerCollectedRecords[i]; //TODO: pretty sure I have to update this
				BossStats oldRecord = list[recordIndex];

				// Establish the new records for comparing

				BossStats newRecord = new BossStats() {
					durationLast = modPlayer.RecordTimers[recordIndex],
					hitsTakenLast = modPlayer.AttackCounter[recordIndex],
					dodgeTimeLast = modPlayer.DodgeTimer[recordIndex],
					healthLossLast = modPlayer.BrinkChecker[recordIndex],

					healthLossLastPercent = (int)(((double)modPlayer.BrinkChecker[recordIndex] / modPlayer.MaxHealth[recordIndex]) * 100),
				};

				RecordID specificRecord = RecordID.None;

				Console.WriteLine(newRecord.durationLast + " vs " + oldRecord.durationBest);
				if ((newRecord.durationLast < oldRecord.durationBest && newRecord.durationLast > 0) || oldRecord.durationBest <= 0) {
					Console.WriteLine("This Fight (" + newRecord.durationLast + ") was better than your current best (" + oldRecord.durationBest + ")");
					specificRecord |= RecordID.ShortestFightTime;
					oldRecord.durationBest = newRecord.durationLast;
				}
				if (newRecord.durationLast > oldRecord.durationWorst && newRecord.durationLast > 0) {
					Console.WriteLine("This Fight (" + newRecord.durationLast + ") was worse than your current worst (" + oldRecord.durationWorst + ")");
					specificRecord |= RecordID.LongestFightTime;
					oldRecord.durationWorst = newRecord.durationLast;
				}
				oldRecord.durationLast = newRecord.durationLast;

				if (newRecord.healthLossLast > oldRecord.healthLossBest && newRecord.healthLossLast > 0) {
					Console.WriteLine("This Fight (" + newRecord.healthLossBest + ") was better than your current best (" + oldRecord.healthLossBest + ")");
					specificRecord |= RecordID.BestBrink;
					oldRecord.healthLossBest = newRecord.healthLossLast;
					oldRecord.healthLossBestPercent = newRecord.healthLossLastPercent;
				}
				if (newRecord.healthLossLast < oldRecord.healthLossWorst && newRecord.healthLossLast > 0) {
					Console.WriteLine("This Fight (" + newRecord.healthLossWorst + ") was worse than your current best (" + oldRecord.healthLossWorst + ")");
					specificRecord |= RecordID.WorstBrink;
					oldRecord.healthLossWorst = newRecord.healthLossLast;
					oldRecord.healthLossWorstPercent = newRecord.healthLossLastPercent;
				}
				oldRecord.healthLossLast = newRecord.healthLossLast;
				oldRecord.healthLossLastPercent = newRecord.healthLossLastPercent;

				if (newRecord.hitsTakenLast < oldRecord.hitsTakenBest && newRecord.hitsTakenLast > -1) {
					Console.WriteLine("This Fight (" + newRecord.hitsTakenLast + ") was better than your current best (" + oldRecord.hitsTakenBest + ")");
					specificRecord |= RecordID.LeastHits;
					oldRecord.hitsTakenBest = newRecord.hitsTakenLast;
				}
				if (newRecord.hitsTakenLast > oldRecord.hitsTakenWorst && oldRecord.hitsTakenLast > -1) {
					Console.WriteLine("This Fight (" + newRecord.hitsTakenLast + ") was worst than your current best (" + oldRecord.hitsTakenWorst + ")");
					specificRecord |= RecordID.MostHits;
					oldRecord.hitsTakenWorst = newRecord.hitsTakenLast;
				}
				oldRecord.hitsTakenLast = newRecord.hitsTakenLast;

				if (newRecord.dodgeTimeLast > oldRecord.dodgeTimeBest && oldRecord.dodgeTimeLast > 0) {
					Console.WriteLine("This Fight (" + newRecord.dodgeTimeLast + ") was better than your current best (" + oldRecord.dodgeTimeBest + ")");
					specificRecord |= RecordID.DodgeTime;
					oldRecord.dodgeTimeBest = newRecord.dodgeTimeLast;
				}
				oldRecord.dodgeTimeLast = newRecord.dodgeTimeLast;

				// Make the packet
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write((int)recordIndex);
				newRecord.NetSend(packet, specificRecord);

				// ORDER MATTERS
				packet.Send(toClient: i);
			}
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

		public static int ListedBossNum(NPC boss) {
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			if (boss.type < NPCID.Count) return BL.FindIndex(x => x.npcIDs.Any(y => y == boss.type));
			else return BL.FindIndex(x => x.name == boss.FullName && x.modSource == boss.modNPC.mod.Name && x.npcIDs.Any(y => y == boss.type));
		}

		public static int ListedBossNum(int type, string modSource) {
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			if (type < NPCID.Count) return BL.FindIndex(x => x.npcIDs.Any(y => y == type));
			else return BL.FindIndex(x => x.modSource == modSource && x.npcIDs.Any(y => y == type));
		}

		public static bool TruelyDead(NPC npc) {
			// Check all multibosses
			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			if (ListedBossNum(npc) != -1) {
				for (int i = 0; i < BossChecklist.bossTracker.SortedBosses[ListedBossNum(npc)].npcIDs.Count; i++) {
					if (Main.npc.Any(x => x != npc && x.type == BossChecklist.bossTracker.SortedBosses[ListedBossNum(npc)].npcIDs[i] && x.active)) return false;
				}
			}
			return true;
		}
	}
}
