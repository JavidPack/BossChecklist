using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
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
        public override void NPCLoot(NPC npc)
		{
			if (npc.type == NPCID.DD2Betsy)
            {
                WorldAssist.downedBetsy = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData); // Immediately inform clients of new world state.
                }
            }

            string partName = npc.GetFullNetName().ToString();
			if (BossChecklist.ClientConfig.PillarMessages)
			{
				if (npc.type == NPCID.LunarTowerSolar || npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerStardust)
				{
					if (Main.netMode == 0) Main.NewText("The " + npc.GetFullNetName().ToString() + " has been destroyed", Colors.RarityPurple);
					else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The " + npc.GetFullNetName().ToString() + " has been destroyed"), Colors.RarityPurple);
				}
			}
            if (NPCisLimb(npc) && BossChecklist.ClientConfig.LimbMessages)
            {
                if (npc.type == NPCID.SkeletronHand) partName = "Skeletron Hand";
                if (Main.netMode == 0) Main.NewText("The " + partName + " is down!", Colors.RarityGreen);
                else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The " + partName + " is down!"), Colors.RarityGreen);
            }
            
            // Setting a record for fastest boss kill, and counting boss kills
            // Twins check makes sure the other is not around before counting towards the record
            if (ListedBossNum(npc) != -1)
			{
				if (TruelyDead(npc))
                {
					if (Main.netMode == NetmodeID.SinglePlayer)
					{
						if (npc.playerInteraction[Main.myPlayer])
						{
							Player player = Main.player[Main.myPlayer];
							CheckRecords(npc, player, PlayerAssist.Get(player, mod));
						}
					}
					else
					{
						CheckRecordsMultiplayer(npc);
					}
                }
				if (BossChecklist.DebugConfig.ShowTDC) Main.NewText(TruelyDead(npc));
            }
        }

		public override bool InstancePerEntity => true;

		List<Player> StartingPlayers;
		
		public override bool PreAI(NPC npc)
		{
			if (ListedBossNum(npc) != -1)
			{
				int listNum = ListedBossNum(npc);
				if (StartingPlayers == null)
				{
					// I'll need to have a check in here for Worm bosses
					StartingPlayers = new List<Player>();
					foreach (Player player in Main.player)
					{
						if (player.active) StartingPlayers.Add(player);
					}
					
					foreach (Player player in StartingPlayers)
					{
						PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
						modPlayer.MaxHealth[listNum] = 0;
						modPlayer.RecordTimers[listNum] = 0;
						modPlayer.BrinkChecker[listNum] = 0;
						modPlayer.DodgeTimer[listNum] = 0;
					}
				}

				if (npc.active)
				{
					foreach (Player player in StartingPlayers)
					{
						PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
						if (!player.active)
						{
							StartingPlayers.Remove(player);
							continue;
						}
						if (player.dead) modPlayer.DeathTracker[ListedBossNum(npc)] = 1;
						modPlayer.RecordTimers[listNum]++;
						modPlayer.DodgeTimer[listNum]++;
						if (modPlayer.MaxHealth[listNum] == 0) modPlayer.MaxHealth[listNum] = player.statLifeMax2;
						if (modPlayer.BrinkChecker[listNum] == 0 || (player.statLife < modPlayer.BrinkChecker[listNum] && player.statLife > 0))
						{
							modPlayer.BrinkChecker[listNum] = player.statLife;
						}
					}
				}
			}

			return true;
		}

		public void CheckRecords(NPC npc, Player player, PlayerAssist modplayer)
		{
			int recordIndex = ListedBossNum(npc);
			int recordAttempt = modplayer.RecordTimers[recordIndex]; // Trying to set a new record
			int currentRecord = modplayer.AllBossRecords[recordIndex].stat.fightTime;
			int worstRecord = modplayer.AllBossRecords[recordIndex].stat.fightTime2;

			modplayer.AllBossRecords[recordIndex].stat.fightTimeL = recordAttempt;

			int brinkAttempt = modplayer.BrinkChecker[recordIndex]; // Trying to set a new record
			int MaxLife = modplayer.MaxHealth[recordIndex];
			int currentBrink = modplayer.AllBossRecords[recordIndex].stat.brink2;
			int worstBrink = modplayer.AllBossRecords[recordIndex].stat.brink;

			modplayer.AllBossRecords[recordIndex].stat.brinkL = brinkAttempt;
			double lastHealth = (double)brinkAttempt / (double)MaxLife;
			modplayer.AllBossRecords[recordIndex].stat.brinkPercentL = (int)(lastHealth * 100);

			int dodgeTimeAttempt = modplayer.DodgeTimer[recordIndex];
			int currentDodgeTime = modplayer.AllBossRecords[recordIndex].stat.dodgeTime;
			int dodgeAttempt = modplayer.AttackCounter[recordIndex];
			int currentDodges = modplayer.AllBossRecords[recordIndex].stat.totalDodges;
			int worstDodges = modplayer.AllBossRecords[recordIndex].stat.totalDodges2;

			modplayer.AllBossRecords[recordIndex].stat.dodgeTimeL = dodgeTimeAttempt;
			modplayer.AllBossRecords[recordIndex].stat.totalDodgesL = dodgeAttempt;

			// Increase kill count
			modplayer.AllBossRecords[recordIndex].stat.kills++;

			if (recordAttempt < currentRecord && currentRecord != 0 && worstRecord <= 0)
			{
				// First make the current record the worst record if no worst record has been made and a new record was made
				modplayer.AllBossRecords[recordIndex].stat.fightTime2 = currentRecord;
			}
			if (recordAttempt < currentRecord || currentRecord <= 0)
			{
				//The player has beaten their best record, so we have to overwrite the old record with the new one
				modplayer.AllBossRecords[recordIndex].stat.fightTime = recordAttempt;
			}
			else if (recordAttempt > worstRecord || worstRecord <= 0)
			{
				//The player has beaten their worst record, so we have to overwrite the old record with the new one
				modplayer.AllBossRecords[recordIndex].stat.fightTime2 = recordAttempt;
			}

			if (brinkAttempt > currentBrink && currentBrink != 0 && worstBrink <= 0)
			{
				modplayer.AllBossRecords[recordIndex].stat.brink = currentBrink;
			}
			if (brinkAttempt > currentBrink || currentBrink <= 0)
			{
				modplayer.AllBossRecords[recordIndex].stat.brink2 = brinkAttempt;
				double newHealth = (double)brinkAttempt / (double)MaxLife; // Casts may be redundant, but this setup doesn't work without them.
				modplayer.AllBossRecords[recordIndex].stat.brinkPercent2 = (int)(newHealth * 100);
			}
			else if (brinkAttempt < worstBrink || worstBrink <= 0)
			{
				modplayer.AllBossRecords[recordIndex].stat.brink = brinkAttempt;
				double newHealth = (double)brinkAttempt / (double)MaxLife; // Casts may be redundant, but this setup doesn't work without them.
				modplayer.AllBossRecords[recordIndex].stat.brinkPercent = (int)(newHealth * 100);
			}

			if (dodgeTimeAttempt > currentDodgeTime || currentDodgeTime < 0)
			{
				// There is no "worse record" for this one so just overwrite any better records made
				modplayer.AllBossRecords[recordIndex].stat.dodgeTime = dodgeTimeAttempt;
			}

			if (dodgeAttempt < currentDodges || currentDodges < 0)
			{
				modplayer.AllBossRecords[recordIndex].stat.totalDodges = dodgeAttempt;
				if (worstDodges == 0) modplayer.AllBossRecords[recordIndex].stat.totalDodges2 = currentDodges;
			}
			else if (dodgeAttempt > worstDodges || worstDodges < 0)
			{
				modplayer.AllBossRecords[recordIndex].stat.totalDodges2 = dodgeAttempt;
			}

			modplayer.DodgeTimer[recordIndex] = 0;
			modplayer.AttackCounter[recordIndex] = 0;

			// If a new record was made, notify the player
			if ((recordAttempt < currentRecord || currentRecord <= 0) || (brinkAttempt > currentBrink || currentBrink <= 0) || (dodgeAttempt < currentDodges || dodgeAttempt <= 0))
			{
				CombatText.NewText(player.getRect(), Color.LightYellow, "New Record!", true);
			}
		}

		public void CheckRecordsMultiplayer(NPC npc)
		{
			int recordIndex = ListedBossNum(npc);
			for (int i = 0; i < 255; i++)
			{
				Player player = Main.player[i];

				if (!player.active || !npc.playerInteraction[i]) continue; // Players must be active AND have interacted with the boss
				PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
				if (Main.netMode == NetmodeID.Server)
				{
					List<BossStats> list = BossChecklist.ServerCollectedRecords[i];
					BossStats oldRecord = list[recordIndex];

					// Establish the new records for comparing

					BossStats newRecord = new BossStats()
					{
						fightTimeL = modPlayer.RecordTimers[recordIndex],
						totalDodgesL = modPlayer.AttackCounter[recordIndex],
						dodgeTimeL = modPlayer.DodgeTimer[recordIndex],
						brinkL = modPlayer.BrinkChecker[recordIndex],

						brinkPercentL = (int)(((double)modPlayer.BrinkChecker[recordIndex] / modPlayer.MaxHealth[recordIndex]) * 100),
					};
					
					// Compare the records

					//
					//
					// TO DO: Make sure to check if the old and new records are -1/0 or "undefined"
					//
					//

					RecordID specificRecord = RecordID.None;

					Console.WriteLine(newRecord.fightTimeL + " vs " + oldRecord.fightTime);
					if ((newRecord.fightTimeL < oldRecord.fightTime && newRecord.fightTimeL > 0) || oldRecord.fightTime <= 0)
					{
						Console.WriteLine("This Fight (" + newRecord.fightTimeL + ") was better than your current best (" + oldRecord.fightTime + ")");
						specificRecord |= RecordID.ShortestFightTime;
						BossChecklist.ServerCollectedRecords[i][recordIndex].fightTime = newRecord.fightTimeL;
					}
					if (newRecord.fightTimeL > oldRecord.fightTime2 && newRecord.fightTimeL > 0)
					{
						Console.WriteLine("This Fight (" + newRecord.fightTimeL + ") was worse than your current worst (" + oldRecord.fightTime2 + ")");
						specificRecord |= RecordID.LongestFightTime;
						BossChecklist.ServerCollectedRecords[i][recordIndex].fightTime2 = newRecord.fightTimeL;
					}
					BossChecklist.ServerCollectedRecords[i][recordIndex].fightTimeL = newRecord.fightTimeL;

					if (newRecord.brinkL > oldRecord.brink2 && newRecord.brinkL > 0)
					{
						Console.WriteLine("This Fight (" + newRecord.brink2 + ") was better than your current best (" + oldRecord.brink2 + ")");
						specificRecord |= RecordID.BestBrink;
						BossChecklist.ServerCollectedRecords[i][recordIndex].brink2 = newRecord.brinkL;
						BossChecklist.ServerCollectedRecords[i][recordIndex].brinkPercent2 = newRecord.brinkPercentL;
					}
					if (newRecord.brinkL < oldRecord.brink && newRecord.brinkL > 0)
					{
						Console.WriteLine("This Fight (" + newRecord.brink + ") was better than your current best (" + oldRecord.brink + ")");
						specificRecord |= RecordID.WorstBrink;
						BossChecklist.ServerCollectedRecords[i][recordIndex].brink = newRecord.brinkL;
						BossChecklist.ServerCollectedRecords[i][recordIndex].brinkPercent = newRecord.brinkPercentL;
					}
					BossChecklist.ServerCollectedRecords[i][recordIndex].brinkL = newRecord.brinkL;
					BossChecklist.ServerCollectedRecords[i][recordIndex].brinkPercentL = newRecord.brinkPercentL;

					if (newRecord.totalDodgesL < oldRecord.totalDodges && newRecord.totalDodgesL > -1)
					{
						Console.WriteLine("This Fight (" + newRecord.totalDodgesL + ") was better than your current best (" + oldRecord.totalDodges + ")");
						specificRecord |= RecordID.LeastHits;
						BossChecklist.ServerCollectedRecords[i][recordIndex].totalDodges = newRecord.totalDodgesL;
					}
					if (newRecord.totalDodgesL > oldRecord.totalDodges2 && oldRecord.totalDodgesL > -1)
					{
						Console.WriteLine("This Fight (" + newRecord.totalDodgesL + ") was better than your current best (" + oldRecord.totalDodges2 + ")");
						specificRecord |= RecordID.MostHits;
						BossChecklist.ServerCollectedRecords[i][recordIndex].totalDodges2 = newRecord.totalDodgesL;
					}
					BossChecklist.ServerCollectedRecords[i][recordIndex].totalDodgesL = newRecord.totalDodgesL;

					if (newRecord.dodgeTimeL > oldRecord.dodgeTime && oldRecord.dodgeTimeL > 0)
					{
						Console.WriteLine("This Fight (" + newRecord.dodgeTimeL + ") was better than your current best (" + oldRecord.dodgeTime + ")");
						specificRecord |= RecordID.DodgeTime;
						BossChecklist.ServerCollectedRecords[i][recordIndex].dodgeTime = newRecord.dodgeTimeL;
					}
					BossChecklist.ServerCollectedRecords[i][recordIndex].dodgeTimeL = newRecord.dodgeTimeL;
					
					// Make the packet

					ModPacket packet = mod.GetPacket();
					packet.Write((byte)BossChecklistMessageType.RecordUpdate);

					packet.Write((int)specificRecord);
					packet.Write((int)recordIndex);
					// Kills update by 1 automatically
					// Deaths have to be sent elsewhere (NPCLoot wont run if the player dies)

					if (specificRecord.HasFlag(RecordID.ShortestFightTime)) packet.Write(newRecord.fightTimeL);
					if (specificRecord.HasFlag(RecordID.LongestFightTime)) packet.Write(newRecord.fightTimeL);
					packet.Write(newRecord.fightTimeL);

					if (specificRecord.HasFlag(RecordID.BestBrink))
					{
						packet.Write(newRecord.brinkL);
						packet.Write(newRecord.brinkPercentL);
					}
					if (specificRecord.HasFlag(RecordID.WorstBrink))
					{
						packet.Write(newRecord.brinkL);
						packet.Write(newRecord.brinkPercentL);
					}
					packet.Write(newRecord.brinkL);
					packet.Write(newRecord.brinkPercentL);

					if (specificRecord.HasFlag(RecordID.LeastHits)) packet.Write(newRecord.totalDodgesL);
					if (specificRecord.HasFlag(RecordID.MostHits)) packet.Write(newRecord.totalDodgesL);
					packet.Write(newRecord.totalDodgesL);
					if (specificRecord.HasFlag(RecordID.DodgeTime)) packet.Write(newRecord.dodgeTimeL);
					packet.Write(newRecord.dodgeTimeL);

					// ORDER MATTERS
					packet.Send(toClient: i);
				}
			}
		}

        public bool NPCisLimb(NPC npcType)
        {
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

        public static int ListedBossNum(NPC boss)
        {
            List<BossInfo> BL = BossChecklist.instance.setup.SortedBosses;
			if (boss.type < Main.maxNPCTypes) return BL.FindIndex(x => x.ids.Any(y => y == boss.type));
			else return BL.FindIndex(x => x.name == boss.FullName && x.source == boss.modNPC.mod.Name && x.ids.Any(y => y == boss.type));
        }

        public bool TruelyDead(NPC npc)
        {
			// Check all multibosses
			List<BossInfo> BL = BossChecklist.instance.setup.SortedBosses;
			if (ListedBossNum(npc) != -1)
			{
				for (int i = 0; i < BossChecklist.instance.setup.SortedBosses[ListedBossNum(npc)].ids.Count; i++)
				{
					if (Main.npc.Any(x => x != npc && x.type == BossChecklist.instance.setup.SortedBosses[ListedBossNum(npc)].ids[i] && x.active)) return false;
				}
			}
			return true;
        }
		
		public override void OnChatButtonClicked(NPC npc, bool firstButton)
		{
			if (npc.type == NPCID.Dryad && !firstButton)
			{
				MapAssist.LocateNearestEvil();
			}
		}
	}
}
