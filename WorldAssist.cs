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
		// Since only 1 set of records is saved per boss, there is no need to put it into a dictionary.
		public static List<WorldRecord> worldRecords;

		// Bosses will be set to true when they spawn and will only be set back to false when the boss despawns or dies
		public static List<bool> ActiveBossesList;

		// Players that are in the server when a boss fight starts
		// Prevents players that join a server mid bossfight from messing up records
		public static List<bool[]> StartingPlayers;

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
		public static bool downedTorchGod;

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
			HiddenBosses.Clear();

			downedBloodMoon = false;
			downedFrostMoon = false;
			downedPumpkinMoon = false;
			downedSolarEclipse = false;

			isBloodMoon = false;
			isFrostMoon = false;
			isPumpkinMoon = false;
			isEclipse = false;

			downedDarkMage = false;
			downedOgre = false;
			downedFlyingDutchman = false;
			downedMartianSaucer = false;

			downedInvasionT2Ours = false;
			downedInvasionT3Ours = false;
			downedTorchGod = false;

			// Record related lists that should be the same count of record tracking entries
			worldRecords = new List<WorldRecord>();
			ActiveBossesList = new List<bool>();
			StartingPlayers = new List<bool[]>();

			for (int i = 0; i < BossChecklist.bossTracker.BossRecordKeys.Count; i++) {
				worldRecords.Add(new WorldRecord(BossChecklist.bossTracker.BossRecordKeys[i]));
				ActiveBossesList.Add(false);
				StartingPlayers.Add(new bool[Main.maxPlayers]);
			}
		}

		public override void PreUpdateWorld() {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
				return;
			}
			for (int n = 0; n < Main.maxNPCs; n++) {
				NPC npc = Main.npc[n];
				int bossIndex = NPCAssist.GetBossInfoIndex(npc.type, true);
				int recordIndex = BossChecklist.bossTracker.SortedBosses[bossIndex].GetRecordIndex;
				if (bossIndex == -1 || recordIndex == -1) {
					continue;
				}

				if (ActiveBossesList[recordIndex]) {
					// If any players become inactive during the fight, remove them from the list
					for (int i = 0; i < Main.maxPlayers; i++) {
						if (!Main.player[i].active) {
							StartingPlayers[recordIndex][i] = false;
						}
					}

					// If the boss is marked active, but is no longer active, check for other potential npcs assigned to the boss
					// If the boss is fully inactive, but not killed, display a despawn message
					if (!npc.active && NPCAssist.FullyInactive(npc, bossIndex)) {
						ActiveBossesList[recordIndex] = false; // No longer an active boss (only other time this is set to false is NPC.OnKill)
						string message = GetDespawnMessage(npc, bossIndex);
						if (message != "") {
							if (Main.netMode == NetmodeID.SinglePlayer) {
								Main.NewText(Language.GetTextValue(message, npc.FullName), Colors.RarityPurple);
							}
							else {
								ChatHelper.BroadcastChatMessage(NetworkText.FromKey(message, npc.FullName), Colors.RarityPurple);
							}
						}
					}
				}
			}
		}

		public override void PostUpdateWorld() {
			string EventKey = "";

			// Blood Moon
			if (Main.bloodMoon) {
				isBloodMoon = true;
			}
			else if (isBloodMoon) {
				isBloodMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.BloodMoon";
				if (!downedBloodMoon) {
					downedBloodMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Frost Moon
			if (Main.snowMoon) {
				isFrostMoon = true;
			}
			else if (isFrostMoon) {
				isFrostMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.FrostMoon";
				if (!downedFrostMoon) {
					downedFrostMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Pumpkin Moon
			if (Main.pumpkinMoon) {
				isPumpkinMoon = true;
			}
			else if (isPumpkinMoon) {
				isPumpkinMoon = false;
				EventKey = "Mods.BossChecklist.EventEnd.PumpkinMoon";
				if (!downedPumpkinMoon) {
					downedPumpkinMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Solar Eclipse
			if (Main.eclipse) {
				isEclipse = true;
			}
			else if (isEclipse) {
				isEclipse = false;
				EventKey = "Mods.BossChecklist.EventEnd.SolarEclipse";
				if (!downedSolarEclipse) {
					downedSolarEclipse = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Event Ending Messages
			if (EventKey != "") {
				NetworkText message = NetworkText.FromKey(EventKey);
				if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(message.ToString(), Colors.RarityGreen);
				}
				else {
					ChatHelper.BroadcastChatMessage(message, Colors.RarityGreen);
				}
			}
		}

		public string GetDespawnMessage(NPC npc, int index) {
			if (npc.life <= 0) {
				return ""; // If the boss was killed, don't display a despawn message
			}

			List<BossInfo> bosses = BossChecklist.bossTracker.SortedBosses;
			string messageType = BossChecklist.ClientConfig.DespawnMessageType;

			if (messageType == "Unique") {
				// Provide the npc for the custom message
				// If null or empty, give a generic message instead of a custom one
				string customMessage = bosses[index].customDespawnMessages(npc);
				if (!string.IsNullOrEmpty(customMessage)) {
					return customMessage;
				}
			}
			if (messageType != "Disabled") {
				// If any player is still alive, use the generic despawn message.
				// If all players are dead, use the boss victory despawn message.
				return Main.player.Any(plr => plr.active && !plr.dead) ? "Mods.BossChecklist.BossDespawn.Generic" : "Mods.BossChecklist.BossVictory.Generic";
			}
			// The despawn message feature was disabled. Return an empty message.
			return "";
		}

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenBosses);

			var downed = new List<string>();
			if (downedBloodMoon)
				downed.Add("bloodmoon");
			if (downedFrostMoon)
				downed.Add("frostmoon");
			if (downedPumpkinMoon)
				downed.Add("pumpkinmoon");
			if (downedSolarEclipse)
				downed.Add("solareclipse");
			if (downedDarkMage)
				downed.Add("darkmage");
			if (downedOgre)
				downed.Add("ogre");
			if (downedFlyingDutchman)
				downed.Add("flyingdutchman");
			if (downedMartianSaucer)
				downed.Add("martiansaucer");
			if (downedInvasionT2Ours)
				downed.Add("invasionT2Ours");
			if (downedInvasionT3Ours)
				downed.Add("invasionT3Ours");
			if (downedTorchGod)
				downed.Add("torchgod");

			tag["downed"] = downed;
			tag["HiddenBossesList"] = HiddenBossesList;
			// TODO: unloaded entry data must be preserved
			if (worldRecords == null) {
				tag["Records"] = new List<WorldRecord>();
			}
			else {
				tag["Records"] = new List<WorldRecord>(worldRecords);
			}
		}

		public override void LoadWorldData(TagCompound tag) {
			List<WorldRecord> SavedWorldRecords = tag.Get<List<WorldRecord>>("Records").ToList();
			foreach (WorldRecord record in SavedWorldRecords) {
				// Check to see if the boss is assigned within the SotredBoss list
				// If we know an entry was added that isn't a boss (old player data) skip adding this entry, effectively removing it when next saved.
				int sortedIndex = BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == record.bossKey);
				if (sortedIndex != -1 && BossChecklist.bossTracker.SortedBosses[sortedIndex].type != EntryType.Boss)
					continue;

				int index = worldRecords.FindIndex(x => x.bossKey == record.bossKey);
				if (index == -1)
					worldRecords.Add(record);
				else
					worldRecords[index] = record;
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
			downedTorchGod = downed.Contains("torchgod");
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
				[5] = downedInvasionT3Ours,
				[6] = downedTorchGod
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
			downedTorchGod = flags[6];

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
