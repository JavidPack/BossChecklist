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
			if (!Main.dedServ && Main.gameMenu) {
				return;
			}
			string partName = npc.GetFullNetName().ToString();
			if (BossChecklist.ClientConfig.PillarMessages) {
				int[] LunarTowers = new int[] {
					NPCID.LunarTowerSolar,
					NPCID.LunarTowerVortex,
					NPCID.LunarTowerNebula,
					NPCID.LunarTowerStardust
				};
				
				if (LunarTowers.Contains(npc.type)) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						string key = "Mods.BossChecklist.BossDefeated.Tower";
						Main.NewText(Language.GetTextValue(key, npc.GetFullNetName().ToString()), Colors.RarityPurple);
					}
					else {
						string key = "Mods.BossChecklist.BossDefeated.Tower";
						string netName = npc.GetFullNetName().ToString();
						NetMessage.BroadcastChatMessage(NetworkText.FromKey(key, netName), Colors.RarityPurple);
					}
				}
			}
			if (NPCisLimb(npc) && BossChecklist.ClientConfig.LimbMessages) {
				if (npc.type == NPCID.SkeletronHand) {
					partName = "Skeletron Hand";
				}
				if (Main.netMode == NetmodeID.SinglePlayer) {
					string key = "Mods.BossChecklist.BossDefeated.Limb";
					Main.NewText(Language.GetTextValue(key, partName), Colors.RarityGreen);
				}
				else {
					string key = "Mods.BossChecklist.BossDefeated.Limb";
					NetMessage.BroadcastChatMessage(NetworkText.FromKey(key, partName), Colors.RarityGreen);
				}
			}

			// Setting a record for fastest boss kill, and counting boss kills
			// Twins check makes sure the other is not around before counting towards the record
			int index = ListedBossNum(npc);
			if (index != -1) {
				DebugConfiguration debug = BossChecklist.DebugConfig;
				if (!debug.NewRecordsDisabled && !debug.RecordTrackingDisabled && TruelyDead(npc)) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						CheckRecords(npc, index);
					}
					else if (Main.netMode == NetmodeID.Server) {
						CheckRecordsMultiplayer(npc, index);
					}
				}
				if (BossChecklist.DebugConfig.ShowTDC) {
					Main.NewText(npc.FullName + ": " + TruelyDead(npc));
				}
			}
		}

		public override bool InstancePerEntity => true;

		public override bool PreAI(NPC npc) {
			if (npc.realLife != -1 && npc.realLife != npc.whoAmI) {
				return true; // Checks for multi-segmented bosses?
			}
			int listNum = ListedBossNum(npc);
			if (listNum != -1) {
				if (!WorldAssist.ActiveBossesList[listNum]) {
					for (int j = 0; j < Main.maxPlayers; j++) {
						if (!Main.player[j].active) {
							continue;
						}
						PlayerAssist modPlayer = Main.player[j].GetModPlayer<PlayerAssist>();
						modPlayer.Trackers_Duration[listNum] = 0;
					}
				}
			}
			return true;
		}

		public void CheckRecords(NPC npc, int recordIndex) {
			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
			if (!npc.playerInteraction[Main.myPlayer]) {
				return; // Player must have contributed to the boss fight
			}

			bool newRecordSet = false;
			BossStats bossStats = modPlayer.RecordsForWorld[recordIndex].stat;

			int duration_Prev = modPlayer.Trackers_Duration[recordIndex];
			int duration_First = bossStats.durationFirs;
			int duration_Best = bossStats.durationBest;
			
			int hitsTaken_Prev = modPlayer.Tracker_HitsTaken[recordIndex];
			int hitsTaken_First = bossStats.hitsTakenFirs;
			int hitsTaken_Best = bossStats.hitsTakenBest;

			// 1.) Setup player's last fight attempt numbers. These will be the numbers we are comparing to our best record.
			// We also mark them for modplayer so we can calculate differences! //TODO ?? Is this necessary ??
			bossStats.durationPrev = duration_Prev;
			bossStats.hitsTakenPrev = hitsTaken_Prev;

			modPlayer.durationLastFight = duration_Prev;
			modPlayer.hitsTakenLastFight = hitsTaken_Prev;

			// 2.) Kills always go up, since this section of code only occurs if boss was defeated
			bossStats.kills++; 
			
			// 3.) Check if this is the first record attempt. If it is mark the best record too since it must be empty.
			if (duration_First == -1 && duration_Best == -1) {
				bossStats.durationFirs = duration_Prev;
				bossStats.durationBest = duration_Prev;
			}
			// 4.) If a first attempt was made, check to see if it is a new best record.
			else if (duration_Prev < duration_Best || duration_Best == -1) {
				newRecordSet = true;
				bossStats.durationBest = duration_Prev;
			}

			/// repeated for other records...
			if (hitsTaken_First == -1 && hitsTaken_Best == -1) {
				bossStats.hitsTakenFirs = hitsTaken_Prev;
				bossStats.hitsTakenBest = hitsTaken_Prev;
			}
			else if (hitsTaken_Prev < hitsTaken_Best || hitsTaken_Best == -1) {
				newRecordSet = true;
				bossStats.hitsTakenBest = duration_Prev;
			}
			
			// 5.) If a new record was made, notify the player. Note: The player isn't notified if it was their first attempt.
			// If thw world records are set, we can check for new world records as well
			if (newRecordSet) {
				modPlayer.hasNewRecord[recordIndex] = true;
				CombatText.NewText(player.getRect(), Color.LightYellow, "Personal Best!", true);
			}
		}

		public void CheckRecordsMultiplayer(NPC npc, int recordIndex) {
			bool hasNewRecord = false;
			for (int i = 0; i < 255; i++) {
				Player player = Main.player[i];

				// Players must be active AND have interacted with the boss AND cannot have recordingstats disabled
				if (!player.active || !npc.playerInteraction[i]) {
					continue;
				}

				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				List<BossStats> serverCollectedPlayerRecords = BossChecklist.ServerCollectedRecords[i];
				BossStats playerRecord = serverCollectedPlayerRecords[recordIndex];

				int duration_Prev = modPlayer.Trackers_Duration[recordIndex];
				int hitsTaken_Prev = modPlayer.Tracker_HitsTaken[recordIndex];

				// 1.) Setup this player's new boss records for the server to recollect.
				// We also mark them for modplayer so we can calculate differences! //TODO ?? Is this necessary ??
				BossStats newRecord = new BossStats {
					durationPrev = duration_Prev,
					hitsTakenPrev = hitsTaken_Prev
				};

				modPlayer.durationLastFight = duration_Prev;
				modPlayer.hitsTakenLastFight = hitsTaken_Prev;

				// 2.) Kills always go up, since this section of code only occurs if boss was defeated
				// TODO: Do I need to account for deaths to update to the server too?? Or is that happen each instance already.
				newRecord.kills++;

				// 3.) Setup the flag system for server collection of new records
				RecordID specificRecord = RecordID.None;

				// 4.) ?? Check if this is the first record attempt. If it is mark the best record too since it must be empty.
				// If the record is beaten, we add a flag to specificRecord to allow an override of the current record
				if (playerRecord.durationFirs == -1 && playerRecord.durationBest == -1) {
					specificRecord |= RecordID.Duration;
					newRecord.durationFirs = duration_Prev;
					newRecord.durationBest = duration_Prev;
					hasNewRecord = true;
				}
				else if (duration_Prev < playerRecord.durationBest || playerRecord.durationBest == -1) {
					specificRecord |= RecordID.Duration;
					newRecord.durationBest = duration_Prev;
					hasNewRecord = true;
				}

				/// repeat...
				if (playerRecord.hitsTakenFirs == -1 && playerRecord.hitsTakenBest == -1) {
					specificRecord |= RecordID.HitsTaken;
					newRecord.hitsTakenFirs = hitsTaken_Prev;
					newRecord.hitsTakenBest = hitsTaken_Prev;
					hasNewRecord = true;
				}
				else if (hitsTaken_Prev < playerRecord.hitsTakenBest || playerRecord.hitsTakenBest == -1) {
					specificRecord |= RecordID.HitsTaken;
					newRecord.hitsTakenBest = hitsTaken_Prev;
					hasNewRecord = true;
				}

				// 5.) Make and send the packet
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.RecordUpdate);
				packet.Write((int)recordIndex); // Which boss record are we changing?
				newRecord.NetSend(packet, specificRecord); // Writes all the variables needed
				packet.Send(toClient: i); // We send to the player. Only they need to see their own records
			}
			if (hasNewRecord) {
				CheckForAWorldRecord();
			}




			if (worldRecordHolders.Any(x => x != "")) {
				WorldStats worldStats = WorldAssist.worldRecords[recordIndex].stat;
				RecordID specificRecord = RecordID.None;
				if (worldRecordHolders[0] != "") {
					specificRecord |= RecordID.Duration;
					worldStats.durationHolder = worldRecordHolders[0];
					worldStats.durationWorld = newWorldRecords[0];
				}
				if (worldRecordHolders[1] != "") {
					specificRecord |= RecordID.HitsTaken;
					worldStats.hitsTakenHolder = worldRecordHolders[1];
					worldStats.hitsTakenWorld = newWorldRecords[1];
				}
				
				ModPacket packet = mod.GetPacket();
				packet.Write((byte)PacketMessageType.WorldRecordUpdate);
				packet.Write((int)recordIndex); // Which boss record are we changing?
				worldStats.NetSend(packet, specificRecord);
				packet.Send(); // To server (world data for everyone)
			}
		}
		
		public void CheckForAWorldRecord(int recordIndex, int whoAmI) {
			Player player = Main.player[whoAmI];
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();

			BossStats playerRecord = modPlayer.RecordsForWorld[recordIndex].stat;
			int duration_PlrBest = playerRecord.durationBest;
			int hitsTaken_PlrBest = playerRecord.hitsTakenBest;

			WorldStats worldRecord = WorldAssist.worldRecords[recordIndex].stat;
			int duration_WldBest = worldRecord.durationWorld;
			int hitsTaken_WldBest = worldRecord.hitsTakenWorld;

			bool newRecord_duration = false;
			bool newRecord_hitsTaken = false;
			// Line below is useful if we dont want the message to appear if the world record holder is already the player
			//bool holderIsPlayerOrEmpty = worldRecord.durationHolder == player.name || worldRecord.durationHolder == "";

			// We only want the message to appear if the player is in a multiplayer server
			if (duration_PlrBest < duration_WldBest || worldRecord.durationWorld == -1) {
				// TODO: BELOW NEVER OCCURS CAUSE WE CHECK FOR SINGLEPLAYER CLIENT AT LINE #83
				newRecord_duration = true;
				worldRecord.durationWorld = playerRecord.durationBest;
				worldRecord.durationHolder = player.name;
			}
			if (playerRecord.hitsTakenBest < worldRecord.hitsTakenWorld || worldRecord.hitsTakenWorld == -1) {
				newRecord_hitsTaken = true;
				worldRecord.hitsTakenWorld = playerRecord.hitsTakenBest;
				worldRecord.hitsTakenHolder = player.name;
			}
			return; // newRecord_duration || newRecord_hitsTaken;
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

		// Skipcheck incase we need it to account for events
		public static int ListedBossNum(NPC boss, bool skipEventCheck = true) {
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
				BossInfo bossInfo = BossChecklist.bossTracker.SortedBosses[index];
				for (int i = 0; i < bossInfo.npcIDs.Count; i++) {
					if (Main.npc.Any(x => x != npc && x.type == bossInfo.npcIDs[i] && x.active)) {
						return false;
					}
				}
			}
			return true;
		}
	}
}
