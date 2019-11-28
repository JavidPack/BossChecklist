using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class WorldAssist : ModWorld
	{
		public static bool downedBloodMoon;
		public static bool downedFrostMoon;
		public static bool downedPumpkinMoon;
		public static bool downedSolarEclipse;

		public static bool downedDarkMage;
		public static bool downedOgre;
		public static bool downedFlyingDutchman;
		public static bool downedMartianSaucer;
		
		public static List<bool> ActiveBossesList;
		public static List<List<Player>> StartingPlayers;
		public static List<int> DayDespawners;

		string EventKey = "";
		bool isBloodMoon = false;
		bool isPumpkinMoon = false;
		bool isFrostMoon = false;
		bool isEclipse = false;
		
		public override void Initialize() {
			downedBloodMoon = false;
			downedFrostMoon = false;
			downedPumpkinMoon = false;
			downedSolarEclipse = false;

			downedDarkMage = false;
			downedOgre = false;
			downedFlyingDutchman = false;
			downedMartianSaucer = false;

			DayDespawners = new List<int>() { //Skeletron and Skeletron Prime are not added because they kill the player before despawning
				NPCID.EyeofCthulhu,
				NPCID.Retinazer,
				NPCID.Spazmatism,
				NPCID.TheDestroyer,
			};

			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			ActiveBossesList = new List<bool>();
			StartingPlayers = new List<List<Player>>();
			for (int i = 0; i < BL.Count; i++) {
				ActiveBossesList.Add(false);
				StartingPlayers.Add(new List<Player>());
			}
		}

		public override void PreUpdate() {
			for (int n = 0; n < Main.maxNPCs; n++) {
				NPC b = Main.npc[n];
				int listNum = NPCAssist.ListedBossNum(b);
				if (listNum != -1) {
					if (b.active) {
						ActiveBossesList[listNum] = true;
						if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
					}
					else if (ActiveBossesList[listNum]) {
						if (NPCAssist.TruelyDead(b)) {
							bool moonLordCheck = (b.type == NPCID.MoonLordHead || b.type == NPCID.MoonLordCore);
							if ((!moonLordCheck && b.life >= 0 && CheckRealLife(b.realLife)) || (moonLordCheck && b.life <= 0)) {
								if (Main.netMode == NetmodeID.SinglePlayer) {
									if (BossChecklist.ClientConfig.DespawnMessageType != "Disabled") {
										Main.NewText(NetworkText.FromKey(GetDespawnMessage(b), b.FullName), Colors.RarityPurple);
									}
								}
								else NetMessage.BroadcastChatMessage(NetworkText.FromKey(GetDespawnMessage(b), b.FullName), Colors.RarityPurple);
							}
							ActiveBossesList[listNum] = false;
							if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
						}
					}
				}
			}
			
			for (int listNum = 0; listNum < ActiveBossesList.Count; listNum++) {
				if (!ActiveBossesList[listNum]) {
					foreach (Player player in Main.player) {
						if (player.active) StartingPlayers[listNum].Add(player);
					}
				}
				else {
					foreach (Player player in StartingPlayers[listNum]) {
						if (!player.active) StartingPlayers[listNum].Remove(player);
					}
				}
			}
		}

		public override void PostUpdate() {
			if (Main.netMode != NetmodeID.Server) {
				List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				// Loot Collections
				for (int i = 0; i < BossList.Count; i++) {
					for (int j = 0; j < BossList[i].loot.Count; j++) {
						int item = BossList[i].loot[j];
						if (Main.LocalPlayer.HasItem(item)) {
							int BossIndex = modPlayer.BossTrophies.FindIndex(boss => boss.bossName == BossList[i].name && boss.modName == BossList[i].modSource);
							if (BossIndex == -1) continue;
							if (modPlayer.BossTrophies[i].loot.FindIndex(x => x.Type == item) == -1) {
								modPlayer.BossTrophies[i].loot.Add(new ItemDefinition(item));
							}
						}
					}
				}

				// Boss Collections
				for (int i = 0; i < BossList.Count; i++) {
					for (int j = 0; j < BossList[i].collection.Count; j++) {
						int item = BossList[i].collection[j];
						if (Main.LocalPlayer.HasItem(item)) {
							int BossIndex = modPlayer.BossTrophies.FindIndex(boss => boss.bossName == BossList[i].name && boss.modName == BossList[i].modSource);
							if (BossIndex == -1) continue;
							if (modPlayer.BossTrophies[i].collectibles.FindIndex(x => x.Type == item) == -1) {
								modPlayer.BossTrophies[i].collectibles.Add(new ItemDefinition(item));
							}
						}
					}
				}
			}

			// Event Ending Messages
			if (Main.bloodMoon) isBloodMoon = true;
			if (Main.snowMoon) isFrostMoon = true;
			if (Main.pumpkinMoon) isPumpkinMoon = true;
			if (Main.eclipse) isEclipse = true;

			if (!Main.bloodMoon && isBloodMoon) {
				isBloodMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.BloodMoon";
				if (!downedBloodMoon) {
					downedBloodMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.snowMoon && isFrostMoon) {
				isFrostMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.FrostMoon";
				if (!downedFrostMoon) {
					downedFrostMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.pumpkinMoon && isPumpkinMoon) {
				isPumpkinMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.PumpkinMoon";
				if (!downedPumpkinMoon) {
					downedPumpkinMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.eclipse && isEclipse) {
				isEclipse = false;
				EventKey = "Mods.BossChecklist.EventEnd.SolarEclipse";
				if (!downedSolarEclipse) {
					downedSolarEclipse = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}

			if (EventKey != "") {
				NetworkText message = NetworkText.FromKey(EventKey);
				if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(message.ToString(), Colors.RarityGreen);
				else NetMessage.BroadcastChatMessage(message, Colors.RarityGreen);
				EventKey = "";
			}
		}

		public string GetDespawnMessage(NPC boss) {
			bool moonLordCheck = (boss.type == NPCID.MoonLordHead || boss.type == NPCID.MoonLordCore);
			if (Main.player.Any(playerCheck => playerCheck.active && !playerCheck.dead) && !moonLordCheck) { // If any player is active and alive
				if (Main.dayTime && DayDespawners.Contains(boss.type)) return "Mods.BossChecklist.BossDespawn.Day";
				else if (boss.type == NPCID.WallofFlesh) return "Mods.BossChecklist.BossVictory.WallofFlesh";
				else return "Mods.BossChecklist.BossDespawn.Generic";
			}
			else if (BossChecklist.ClientConfig.DespawnMessageType == "Custom") {
				// Check already accounted for to get to this point
				return BossChecklist.bossTracker.SortedBosses[NPCAssist.ListedBossNum(boss)].despawnMessage;
			}
			else return "Mods.BossChecklist.BossVictory.Generic";
		}

		public bool CheckRealLife(int realNPC) {
			if (realNPC == -1) return true;
			if (Main.npc[realNPC].life >= 0) return true;
			else return false;
		}

		public override TagCompound Save() {
			var downed = new List<string>();
			if (downedBloodMoon) downed.Add("bloodmoon");
			if (downedFrostMoon) downed.Add("frostmoon");
			if (downedPumpkinMoon) downed.Add("pumpkinmoon");
			if (downedSolarEclipse) downed.Add("solareclipse");

			return new TagCompound {
				["downed"] = downed,
			};
		}

		public override void Load(TagCompound tag) {
			var downed = tag.GetList<string>("downed");
			downedBloodMoon = downed.Contains("bloodmoon");
			downedFrostMoon = downed.Contains("frostmoon");
			downedPumpkinMoon = downed.Contains("pumpkinmoon");
			downedSolarEclipse = downed.Contains("solareclipse");
		}

		public override void LoadLegacy(BinaryReader reader) {
			int loadVersion = reader.ReadInt32();
			if (loadVersion == 0) {
				BitsByte flags = reader.ReadByte();
				downedBloodMoon = flags[0];
				downedFrostMoon = flags[1];
				downedPumpkinMoon = flags[2];
				downedSolarEclipse = flags[3];
			}
			else {
				mod.Logger.WarnFormat($"BossChecklist: Unknown loadVersion: {loadVersion}");
			}
		}

		public override void NetSend(BinaryWriter writer) {
			// BitBytes can have up to 8 values.
			// BitsByte flags2 = reader.ReadByte();
			BitsByte flags = new BitsByte();
			flags[0] = downedBloodMoon;
			flags[1] = downedFrostMoon;
			flags[2] = downedPumpkinMoon;
			flags[3] = downedSolarEclipse;
			writer.Write(flags);

			BitsByte flags2 = new BitsByte();
			flags2[0] = downedDarkMage;
			flags2[1] = downedOgre;
			flags2[2] = downedFlyingDutchman;
			flags2[3] = downedMartianSaucer;
			writer.Write(flags2);
		}

		public override void NetReceive(BinaryReader reader) {
			BitsByte flags = reader.ReadByte();
			downedBloodMoon = flags[0];
			downedFrostMoon = flags[1];
			downedPumpkinMoon = flags[2];
			downedSolarEclipse = flags[3];
			// BitBytes can have up to 8 values.
		}
	}
}