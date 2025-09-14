using BossChecklist.Systems;
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
		// Special case for moon lord. The hands and head do not 'die' when the messages need to be triggered
		public override void HitEffect(NPC npc, NPC.HitInfo hit) {
			if ((npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead) && npc.life <= 0) {
				if (Main.npc.Any(x => x.type == NPCID.MoonLordCore && x.life <= 0))
					return; // Messages shouldn't be sent if the Moon Lord's core is defeated as that would mean the entire boss is defeated

				if (BossChecklist.bossTracker.IsEntryLimb(npc.type, out EntryInfo limbEntry) && limbEntry.GetLimbMessage(npc) is LocalizedText message) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						Main.NewText(message.Format(npc.FullName), Colors.RarityGreen);
					}
					else if (Main.netMode == NetmodeID.Server) {
						// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
						foreach (Player player in Main.ActivePlayers) {
							ModPacket packet = BossChecklist.instance.GetPacket();
							packet.Write((byte)PacketMessageType.SendClientConfigMessage);
							packet.Write((byte)ClientMessageType.Limb);
							packet.Write(npc.whoAmI);
							packet.Send(player.whoAmI); // Server --> Multiplayer client
						}
					}
				}
			}
		}

		public override void OnKill(NPC npc) {
			// Handles all of BossChecklist's custom downed variables, makring them as defeated and updating all clients when needed.
			switch (npc.type) {
				case NPCID.DD2DarkMageT1:
				case NPCID.DD2DarkMageT3:
					NPC.SetEventFlagCleared(ref DownedSystem.downedDarkMage, -1);
					break;
				case NPCID.DD2OgreT2:
				case NPCID.DD2OgreT3:
					NPC.SetEventFlagCleared(ref DownedSystem.downedOgre, -1);
					break;
				case NPCID.PirateShip: NPC.SetEventFlagCleared(ref DownedSystem.downedFlyingDutchman, -1); break;
				case NPCID.MartianSaucerCore: NPC.SetEventFlagCleared(ref DownedSystem.downedMartianSaucer, -1); break;
				case NPCID.LunarTowerVortex: NPC.SetEventFlagCleared(ref NPC.downedTowerVortex, -1); break;
				case NPCID.LunarTowerStardust: NPC.SetEventFlagCleared(ref NPC.downedTowerStardust, -1); break;
				case NPCID.LunarTowerNebula: NPC.SetEventFlagCleared(ref NPC.downedTowerNebula, -1); break;
				case NPCID.LunarTowerSolar: NPC.SetEventFlagCleared(ref NPC.downedTowerSolar, -1); break;
				default: break;
			};

			// Display a message for Limbs/Towers if config is enabled, which should be checked after the active flags update
			if (BossChecklist.bossTracker.IsEntryLimb(npc.type, out EntryInfo limbEntry) && limbEntry.GetLimbMessage(npc) is LocalizedText message) {
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message.Format(npc.FullName), Colors.RarityGreen);
				}
				else if (Main.netMode == NetmodeID.Server) {
					// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
					foreach (Player player in Main.ActivePlayers) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.SendClientConfigMessage);
						packet.Write((byte)ClientMessageType.Limb);
						packet.Write(npc.whoAmI);
						packet.Send(player.whoAmI); // Server --> Multiplayer client
					}
				}
			}
		}
	}
}
