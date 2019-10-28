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

		public static List<bool> ActiveBossesList;
		public static List<List<Player>> StartingPlayers;
		public static List<int> ModBossTypes;
		public static List<string> ModBossMessages;

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

			ModBossTypes = new List<int>();
			ModBossMessages = new List<string>();

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
								if (Main.netMode == NetmodeID.SinglePlayer && BossChecklist.ClientConfig.DespawnMessageType != "Disabled") Main.NewText(GetDespawnMessage(b), Colors.RarityPurple);
								else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(GetDespawnMessage(b)), Colors.RarityPurple);
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
				// Loot Collections
				for (int i = 0; i < BossChecklist.bossTracker.SortedBosses.Count; i++) {
					for (int j = 0; j < BossChecklist.bossTracker.SortedBosses[i].loot.Count; j++) {
						int item = BossChecklist.bossTracker.SortedBosses[i].loot[j];
						if (Main.LocalPlayer.HasItem(item)) {
							int BossIndex = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies.FindIndex(boss => boss.bossName == BossChecklist.bossTracker.SortedBosses[i].name && boss.modName == BossChecklist.bossTracker.SortedBosses[i].modSource);
							if (BossIndex == -1) continue;
							if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].loot.FindIndex(x => x.Type == item) == -1) {
								Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].loot.Add(new ItemDefinition(item));
							}
						}
					}
				}

				// Boss Collections
				for (int i = 0; i < BossChecklist.bossTracker.SortedBosses.Count; i++) {
					for (int j = 0; j < BossChecklist.bossTracker.SortedBosses[i].collection.Count; j++) {
						int item = BossChecklist.bossTracker.SortedBosses[i].collection[j];
						if (Main.LocalPlayer.HasItem(item)) {
							int BossIndex = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies.FindIndex(boss => boss.bossName == BossChecklist.bossTracker.SortedBosses[i].name && boss.modName == BossChecklist.bossTracker.SortedBosses[i].modSource);
							if (BossIndex == -1) continue;
							if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].collectibles.FindIndex(x => x.Type == item) == -1) {
								Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].collectibles.Add(new ItemDefinition(item));
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
				EventKey = "The Blood Moon falls past the horizon...";
				if (!downedBloodMoon) {
					downedBloodMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.snowMoon && isFrostMoon) {
				isFrostMoon = false;
				EventKey = "The Frost Moon melts as the sun rises...";
				if (!downedFrostMoon) {
					downedFrostMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.pumpkinMoon && isPumpkinMoon) {
				isPumpkinMoon = false;
				EventKey = "The Pumpkin Moon ends its harvest...";
				if (!downedPumpkinMoon) {
					downedPumpkinMoon = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
			}
			else if (!Main.eclipse && isEclipse) {
				isEclipse = false;
				EventKey = "The solar eclipse has ended... until next time...";
				if (!downedSolarEclipse) {
					downedSolarEclipse = true;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
				}
				
			}

			if (EventKey != "") {
				if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(EventKey, Colors.RarityGreen);
				else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(EventKey), Colors.RarityGreen);
				EventKey = "";
			}
		}

		public string GetDespawnMessage(NPC boss) {
			bool moonLordCheck = (boss.type == NPCID.MoonLordHead || boss.type == NPCID.MoonLordCore);
			if (Main.player.Any(playerCheck => playerCheck.active && !playerCheck.dead) && !moonLordCheck) // If any player is active and alive
			{
				if (Main.dayTime && (boss.type == NPCID.EyeofCthulhu || boss.type == NPCID.TheDestroyer || boss.type == NPCID.Retinazer || boss.type == NPCID.Spazmatism)) {
					return boss.FullName + " flees as the sun rises...";
				}
				else if (boss.type == NPCID.WallofFlesh) return "Wall of Flesh has managed to cross the underworld...";
				else if (boss.type == NPCID.Retinazer) return "The Twins are no longer after you...";
				else return boss.FullName + " is no longer after you...";
			}
			else if (BossChecklist.ClientConfig.DespawnMessageType == "Custom") {
				if (boss.type == NPCID.KingSlime) return "King Slime leaves in triumph...";
				else if (boss.type == NPCID.EyeofCthulhu) return "Eye of Cthulhu has disappeared into the night...";
				else if (boss.type == NPCID.EaterofWorldsHead) return "Eater of Worlds burrows back underground...";
				else if (boss.type == NPCID.BrainofCthulhu) return "Brain of Cthulhu vanishes into the pits of the crimson...";
				else if (boss.type == NPCID.QueenBee) return "Queen Bee returns to her colony's nest...";
				else if (boss.type == NPCID.SkeletronHead) return "Skeletron continues to torture the Old Man...";
				else if (boss.type == NPCID.WallofFlesh) return "Wall of Flesh has managed to cross the underworld...";
				else if (boss.type == NPCID.Retinazer) return "Retinazer continues its observations...";
				else if (boss.type == NPCID.Spazmatism) return "Spazmatism continues its observations...";
				else if (boss.type == NPCID.TheDestroyer) return "The Destroyer seeks for another world to devour...";
				else if (boss.type == NPCID.SkeletronPrime) return "Skeletron Prime begins searching for a new victim...";
				else if (boss.type == NPCID.Plantera) return "Plantera continues its rest within the jungle...";
				else if (boss.type == NPCID.Golem) return "Golem deactivates in the bowels of the temple...";
				else if (boss.type == NPCID.DukeFishron) return "Duke Fishron returns to the ocean depths...";
				else if (boss.type == NPCID.CultistBoss) return "Lunatic Cultist goes back to its devoted worship...";
				else if (boss.type == NPCID.MoonLordCore) return "Moon Lord has left this realm...";
				else {
					for (int i = 0; i < ModBossTypes.Count; i++) {
						if (boss.type == ModBossTypes[i]) return ModBossMessages[i];
						// If a mod has submitted a custom despawn message, it will display here
					}
					return boss.FullName + " has killed every player!"; // Otherwise it defaults to this
				}
			}
			else return boss.FullName + " has killed every player!";
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
			BitsByte flags = new BitsByte();
			flags[0] = downedBloodMoon;
			flags[1] = downedFrostMoon;
			flags[2] = downedPumpkinMoon;
			flags[3] = downedSolarEclipse;
			writer.Write(flags);
			// BitBytes can have up to 8 values.
			// BitsByte flags2 = reader.ReadByte();
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