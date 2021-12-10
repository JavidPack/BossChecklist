using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class WorldAssist : ModSystem
	{
		public static List<WorldRecord> worldRecords;
		public static HashSet<string> HiddenBosses = new HashSet<string>();

		public static bool downedBloodMoon;
		public static bool downedFrostMoon;
		public static bool downedPumpkinMoon;
		public static bool downedSolarEclipse;

		public static bool downedDarkMage;
		public static bool downedOgre;
		public static bool downedFlyingDutchman;
		public static bool downedMartianSaucer;

		public static bool downedInvasionT2Ours;
		public static bool downedInvasionT3Ours;

		public static List<bool> ActiveBossesList;
		public static List<bool[]> StartingPlayers;
		public static List<int> DayDespawners;

		string EventKey = "";
		bool isBloodMoon = false;
		bool isPumpkinMoon = false;
		bool isFrostMoon = false;
		bool isEclipse = false;

		public override void Load() {
			On.Terraria.GameContent.Events.DD2Event.WinInvasionInternal += DD2Event_WinInvasionInternal;
		}

		private void DD2Event_WinInvasionInternal(On.Terraria.GameContent.Events.DD2Event.orig_WinInvasionInternal orig) {
			orig();
			if (DD2Event.OngoingDifficulty == 2)
				downedInvasionT2Ours = true;
			if (DD2Event.OngoingDifficulty == 3)
				downedInvasionT3Ours = true;
		}

		public override void OnWorldLoad() {
			worldRecords = new List<WorldRecord>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				worldRecords.Add(new WorldRecord(boss.Key));
			}

			HiddenBosses.Clear();

			downedBloodMoon = false;
			downedFrostMoon = false;
			downedPumpkinMoon = false;
			downedSolarEclipse = false;

			downedDarkMage = false;
			downedOgre = false;
			downedFlyingDutchman = false;
			downedMartianSaucer = false;

			downedInvasionT2Ours = false;
			downedInvasionT3Ours = false;

			DayDespawners = new List<int>() { //Skeletron and Skeletron Prime are not added because they kill the player before despawning
				NPCID.EyeofCthulhu,
				NPCID.Retinazer,
				NPCID.Spazmatism,
				NPCID.TheDestroyer,
			};

			List<BossInfo> BL = BossChecklist.bossTracker.SortedBosses;
			ActiveBossesList = new List<bool>();
			StartingPlayers = new List<bool[]>();
			for (int i = 0; i < BL.Count; i++) { // Includes events, even though they wont be accounted for
				ActiveBossesList.Add(false);
				StartingPlayers.Add(new bool[Main.maxPlayers]);
			}
		}

		public override void PreUpdateWorld() {
			for (int n = 0; n < Main.maxNPCs; n++) {
				NPC b = Main.npc[n];
				int listNum = NPCAssist.ListedBossNum(b);
				if (listNum != -1) {
					if (b.active) {
						for (int i = 0; i < Main.maxPlayers; i++) {
							if (!ActiveBossesList[listNum]) StartingPlayers[listNum][i] = Main.player[i].active;
							else if (!Main.player[i].active) StartingPlayers[listNum][i] = false;
						}
						ActiveBossesList[listNum] = true;
						// if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
					}
					else if (ActiveBossesList[listNum]) {
						if (NPCAssist.TrulyDead(b)) {
							string message = GetDespawnMessage(b, listNum);
							if (message != "") {
								if (Main.netMode == NetmodeID.SinglePlayer) {
									if (BossChecklist.ClientConfig.DespawnMessageType != "Disabled") {
										Main.NewText(Language.GetTextValue(message, b.FullName), Colors.RarityPurple);
									}
								}
								else ChatHelper.BroadcastChatMessage(NetworkText.FromKey(message, b.FullName), Colors.RarityPurple);
							}
							ActiveBossesList[listNum] = false;
							// if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
						}
					}
				}
			}
		}

		public override void PostUpdateWorld() {
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
				else ChatHelper.BroadcastChatMessage(message, Colors.RarityGreen);
				EventKey = "";
			}
		}

		public string GetDespawnMessage(NPC boss, int listnum) {
			if (Main.player.Any(playerCheck => playerCheck.active && !playerCheck.dead)) { // If any player is active and alive
				if (Main.npc.Any(x => x.life > 0 && BossChecklist.bossTracker.SortedBosses[listnum].npcIDs.IndexOf(x.type) != -1)) { // If boss is still active
					if (Main.dayTime && DayDespawners.Contains(boss.type)) return "Mods.BossChecklist.BossDespawn.Day";
					else if (boss.type == NPCID.WallofFlesh) return "Mods.BossChecklist.BossVictory.WallofFlesh";
					else return "Mods.BossChecklist.BossDespawn.Generic";
				}
				else return "";
			}
			else if (BossChecklist.ClientConfig.DespawnMessageType == "Unique") {
				// Check already accounted for to get to this point
				return BossChecklist.bossTracker.SortedBosses[NPCAssist.ListedBossNum(boss)].despawnMessage;
			}
			else return "Mods.BossChecklist.BossVictory.Generic";
		}

		public bool CheckRealLife(int realNPC) => realNPC == -1 || Main.npc[realNPC].life >= 0;

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenBosses);
			var downed = new List<string>();
			if (downedBloodMoon) downed.Add("bloodmoon");
			if (downedFrostMoon) downed.Add("frostmoon");
			if (downedPumpkinMoon) downed.Add("pumpkinmoon");
			if (downedSolarEclipse) downed.Add("solareclipse");
			if (downedDarkMage) downed.Add("darkmage");
			if (downedOgre) downed.Add("ogre");
			if (downedFlyingDutchman) downed.Add("flyingdutchman");
			if (downedMartianSaucer) downed.Add("martiansaucer");
			if (downedInvasionT2Ours) downed.Add("invasionT2Ours");
			if (downedInvasionT3Ours) downed.Add("invasionT3Ours");

			tag["downed"] = downed;
			tag["HiddenBossesList"] = HiddenBossesList;
			if (worldRecords == null) {
				tag["Records"] = new List<WorldRecord>();
			}
			else {
				tag["Records"] = new List<WorldRecord>(worldRecords);
			}
		}

		public override void LoadWorldData(TagCompound tag) {
			List<WorldRecord> TempRecordStorage = tag.Get<List<WorldRecord>>("Records");
			foreach (WorldRecord record in TempRecordStorage) {
				int index = worldRecords.FindIndex(x => x.bossName == record.bossName);
				if (index == -1) {
					worldRecords.Add(record);
				}
				else {
					worldRecords[index] = record;
				}
			}

			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenBosses.Add(bossKey);
			}

			var downed = tag.GetList<string>("downed");
			downedBloodMoon = downed.Contains("bloodmoon");
			downedFrostMoon = downed.Contains("frostmoon");
			downedPumpkinMoon = downed.Contains("pumpkinmoon");
			downedSolarEclipse = downed.Contains("solareclipse");
			downedDarkMage = downed.Contains("darkmage");
			downedOgre = downed.Contains("ogre");
			downedFlyingDutchman = downed.Contains("flyingdutchman");
			downedMartianSaucer = downed.Contains("martiansaucer");
			downedInvasionT2Ours = downed.Contains("invasionT2Ours");
			downedInvasionT3Ours = downed.Contains("invasionT3Ours");
		}

		public override void NetSend(BinaryWriter writer) {
			// BitBytes can have up to 8 values.
			// BitsByte flags2 = reader.ReadByte();
			BitsByte flags = new BitsByte {
				[0] = downedBloodMoon,
				[1] = downedFrostMoon,
				[2] = downedPumpkinMoon,
				[3] = downedSolarEclipse,
				[4] = downedDarkMage,
				[5] = downedOgre,
				[6] = downedFlyingDutchman,
				[7] = downedMartianSaucer
			};
			writer.Write(flags);

			// Vanilla doesn't sync these values, so we will.
			flags = new BitsByte {
				[0] = NPC.downedTowerSolar,
				[1] = NPC.downedTowerVortex,
				[2] = NPC.downedTowerNebula,
				[3] = NPC.downedTowerStardust,
				[4] = downedInvasionT2Ours,
				[5] = downedInvasionT3Ours
			};
			writer.Write(flags);

			writer.Write(HiddenBosses.Count);
			foreach (var bossKey in HiddenBosses) {
				writer.Write(bossKey);
			}
		}

		public override void NetReceive(BinaryReader reader) {
			BitsByte flags = reader.ReadByte();
			downedBloodMoon = flags[0];
			downedFrostMoon = flags[1];
			downedPumpkinMoon = flags[2];
			downedSolarEclipse = flags[3];
			downedDarkMage = flags[4];
			downedOgre = flags[5];
			downedFlyingDutchman = flags[6];
			downedMartianSaucer = flags[7];

			flags = reader.ReadByte();
			NPC.downedTowerSolar = flags[0];
			NPC.downedTowerVortex = flags[1];
			NPC.downedTowerNebula = flags[2];
			NPC.downedTowerStardust = flags[3];
			downedInvasionT2Ours = flags[4];
			downedInvasionT3Ours = flags[5];

			HiddenBosses.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				HiddenBosses.Add(reader.ReadString());
			}
			BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
			if (BossChecklist.BossLogConfig.HideUnavailable && BossLogUI.PageNum == -1) {
				BossUISystem.Instance.BossLog.UpdateTableofContents();
			}
		}
	}
}