﻿using System;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BossChecklist
{
	class NPCAssist : GlobalNPC {
		public const string LangChat = "Mods.BossChecklist.ChatMessages";

		// When an entry NPC spawns, setup the world and player trackers for the upcoming fight
		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (Main.netMode == NetmodeID.MultiplayerClient || BossChecklist.bossTracker.FindEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // Only single player and server should be starting the record tracking process

			WorldAssist.ActiveNPCEntryFlags[npc.whoAmI] = entry.GetIndex;

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			foreach (Player player in Main.player) {
				if (player.active) {
					if (Main.netMode == NetmodeID.Server) {
						PersonalRecords serverRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex];
						serverRecords.StartTracking_Server(player.whoAmI);
					}
					else {
						PersonalRecords bossrecord = player.GetModPlayer<PlayerAssist>().RecordsForWorld[recordIndex];
						bossrecord.StartTracking(); // start tracking for active players
					}
				}
			}
		}

		// Special case for moon lord. The hands and head do not 'die' when the messages need to be triggered
		public override void HitEffect(NPC npc, NPC.HitInfo hit) {
			if (npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead) {
				if (npc.life <= 0)
					SendEntryMessage(npc);
			}
		}

		// When an NPC is killed and fully inactive the fight has ended, so stop all record trackers
		public override void OnKill(NPC npc) {
			HandleDownedNPCs(npc.type); // Custom downed bool code
			WorldAssist.ActiveNPCEntryFlags[npc.whoAmI] = -1; // NPC is killed, unflag their active status
			SendEntryMessage(npc); // Display a message for Limbs/Towers if config is enabled, which should be checked after the active flags update

			if (BossChecklist.bossTracker.FindEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // make sure NPC has a valid entry and that no other NPCs exist with that entry index

			if (WorldAssist.ActiveNPCEntryFlags.Any(x => x == entry.GetIndex))
				return;

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			bool newPersonalBestOnServer = false;
			// stop tracking and record stats for those who had interactions with the boss
			foreach (Player player in Main.player) {
				if (!player.active)
					continue;

				bool interaction = npc.playerInteraction[player.whoAmI];
				if (Main.netMode == NetmodeID.Server) {
					PersonalRecords serverRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex];
					if (serverRecords.StopTracking_Server(player.whoAmI, interaction, interaction))
						newPersonalBestOnServer = true; // if any player gets a new persoanl best on the server...
				}
				else {
					PersonalRecords bossrecord = player.GetModPlayer<PlayerAssist>().RecordsForWorld[recordIndex];
					bossrecord.StopTracking(interaction && BossChecklist.ClientConfig.AllowNewRecords, interaction);
				}
			}

			// ... check to see if it is a world record and update every player's logs if so
			if (newPersonalBestOnServer) {
				Console.WriteLine($"A Personal Best was beaten! Comparing against world records...");
				WorldAssist.WorldRecordsForWorld[recordIndex].CheckForWorldRecords_Server(npc.playerInteraction.GetTrueIndexes());
			}
		}

		/// <summary>
		/// Handles all of BossChecklist's custom downed variables, makring them as defeated and updating all clients when needed.
		/// </summary>
		public void HandleDownedNPCs(int npcType) {
			if (!WorldAssist.TrackingDowns)
				return;

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
			if (!BossChecklist.ClientConfig.LimbMessages || !BossChecklist.bossTracker.IsEntryLimb(npc.type, out EntryInfo entry))
				return; // limb messages must be on and the npc must be a limb
			
			if (entry.type == EntryType.Boss && !WorldAssist.ActiveNPCEntryFlags.Contains(entry.GetIndex))
				return; // stops messages when the main boss part is not active

			if (Main.player.All(plr => !plr.active || plr.dead))
				return; // stops messages from appearing when all players are dead (some limb NPCs are killed to despawn)

			if (Main.netMode == NetmodeID.SinglePlayer) {
				Main.NewText(entry.npcLimbs[npc.type].Format(npc.FullName), Colors.RarityGreen);
			}
			else {
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(entry.npcLimbs[npc.type].Format(npc.FullName)), Colors.RarityGreen);
			}
		}
	}
}
