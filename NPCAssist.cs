using System;
using System.Linq;
using Terraria;
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
			if (Main.netMode == NetmodeID.MultiplayerClient || BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // Only single player and server should be starting the record tracking process

			WorldAssist.ActiveNPCEntryFlags[npc.whoAmI] = entry.GetIndex;

			if (Main.netMode is NetmodeID.SinglePlayer) {
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].StartTracking(); // start tracking for active players
			}
			else if (Main.netMode is NetmodeID.Server) {
				foreach (Player player in Main.player.Where(x => x.active)) {
					BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StartTracking_Server(player.whoAmI);
				}
			}
		}

		// Special case for moon lord. The hands and head do not 'die' when the messages need to be triggered
		public override void HitEffect(NPC npc, NPC.HitInfo hit) {
			if ((npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead) && npc.life <= 0) {
				if (BossChecklist.bossTracker.IsEntryLimb(npc.type, out EntryInfo limbEntry) && limbEntry.GetLimbMessage(npc) is LocalizedText message) {
					if (Main.netMode != NetmodeID.Server)
						Main.NewText(message.Format(npc.FullName), Colors.RarityGreen);
				}
			}
		}

		// When an NPC is killed and fully inactive the fight has ended, so stop all record trackers
		public override void OnKill(NPC npc) {
			HandleDownedNPCs(npc.type); // Custom downed bool code
			WorldAssist.ActiveNPCEntryFlags[npc.whoAmI] = -1; // NPC is killed, unflag their active status

			// Display a message for Limbs/Towers if config is enabled, which should be checked after the active flags update
			if (BossChecklist.bossTracker.IsEntryLimb(npc.type, out EntryInfo limbEntry) && limbEntry.GetLimbMessage(npc) is LocalizedText message) {
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message.Format(npc.FullName), Colors.RarityPurple);
				}
				else if (Main.netMode == NetmodeID.Server) {
					// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
					foreach (Player player in Main.player.Where(p => p.active)) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.SendClientConfigMessage);
						packet.Write((byte)ClientMessageType.Limb);
						packet.Write(npc.whoAmI);
						packet.Send(player.whoAmI); // Server --> Multiplayer client
					}
				}
			}

			if (BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // make sure NPC has a valid entry and that no other NPCs exist with that entry index

			if (WorldAssist.ActiveNPCEntryFlags.Any(x => x == entry.GetIndex))
				return;

			bool newPersonalBestOnServer = false;
			// stop tracking and record stats for those who had interactions with the boss

			if (Main.netMode is NetmodeID.SinglePlayer) {
				bool interaction = npc.playerInteraction[Main.LocalPlayer.whoAmI];
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].StopTracking(interaction && BossChecklist.FeatureConfig.AllowNewRecords, interaction);
			}
			else if (Main.netMode is NetmodeID.Server) {
				foreach (Player player in Main.player.Where(x => x.active)) {
					bool interaction = npc.playerInteraction[player.whoAmI];
					if (BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StopTracking_Server(player.whoAmI, interaction && BossChecklist.Server_AllowNewRecords[player.whoAmI], interaction))
						newPersonalBestOnServer = true; // if any player gets a new persoanl best on the server...
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
		/// <returns>If the corresponding flag was flipped.</returns>
		internal static bool HandleDownedNPCs(int npcType) {
			return npcType switch {
				NPCID.DD2DarkMageT1 => Networking.DownedEntryCheck(ref WorldAssist.downedDarkMage),
				NPCID.DD2DarkMageT3 => Networking.DownedEntryCheck(ref WorldAssist.downedDarkMage),
				NPCID.DD2OgreT2 => Networking.DownedEntryCheck(ref WorldAssist.downedOgre),
				NPCID.DD2OgreT3 => Networking.DownedEntryCheck(ref WorldAssist.downedOgre),
				NPCID.PirateShip => Networking.DownedEntryCheck(ref WorldAssist.downedFlyingDutchman),
				NPCID.MartianSaucerCore => Networking.DownedEntryCheck(ref WorldAssist.downedMartianSaucer),
				NPCID.LunarTowerVortex => Networking.DownedEntryCheck(ref NPC.downedTowerVortex),
				NPCID.LunarTowerStardust => Networking.DownedEntryCheck(ref NPC.downedTowerStardust),
				NPCID.LunarTowerNebula => Networking.DownedEntryCheck(ref NPC.downedTowerNebula),
				NPCID.LunarTowerSolar => Networking.DownedEntryCheck(ref NPC.downedTowerSolar),
				_ => false
			};
		}
	}
}
