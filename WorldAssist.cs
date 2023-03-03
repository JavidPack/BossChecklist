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
		// Since only 1 set of records is saved per boss, there is no need to put it into a dictionary
		// A separate list of World Records is needed to hold information about unloaded entries
		public static WorldRecord[] worldRecords;
		public static List<WorldRecord> unloadedWorldRecords;

		// Bosses will be set to true when they spawn and will only be set back to false when the boss despawns or dies
		public static bool[] Tracker_ActiveEntry;

		// Players that are in the server when a boss fight starts
		// Prevents players that join a server mid bossfight from messing up records
		public static bool[,] Tracker_StartingPlayers;

		public static bool[] CheckedRecordIndexes;

		public static HashSet<string> HiddenBosses = new HashSet<string>();
		public static HashSet<string> ForcedMarkedEntries = new HashSet<string>();

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

		bool Tracker_BloodMoon = false;
		bool Tracker_PumpkinMoon = false;
		bool Tracker_FrostMoon = false;
		bool Tracker_SolarEclipse = false;

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

		private void ClearDownedBools() {
			// Events
			downedBloodMoon = false;
			downedFrostMoon = false;
			downedPumpkinMoon = false;
			downedSolarEclipse = false;

			// Event trackers
			Tracker_BloodMoon = false;
			Tracker_FrostMoon = false;
			Tracker_PumpkinMoon = false;
			Tracker_SolarEclipse = false;

			// MiniBosses
			downedDarkMage = false;
			downedOgre = false;
			downedFlyingDutchman = false;
			downedMartianSaucer = false;

			// Vanilla additions
			downedInvasionT2Ours = false;
			downedInvasionT3Ours = false;
			downedTorchGod = false;
		}

		public override void OnWorldLoad() {
			HiddenBosses.Clear();
			ForcedMarkedEntries.Clear();

			ClearDownedBools();

			// Record related lists that should be the same count of record tracking entries
			worldRecords = new WorldRecord[BossChecklist.bossTracker.BossRecordKeys.Count];
			unloadedWorldRecords = new List<WorldRecord>();
			CheckedRecordIndexes = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];
			Tracker_ActiveEntry = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];
			Tracker_StartingPlayers = new bool[BossChecklist.bossTracker.BossRecordKeys.Count, Main.maxPlayers];

			// Populate world records list
			foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
				worldRecords[BossChecklist.bossTracker.SortedBosses[BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == key)].GetRecordIndex] = new WorldRecord(key);
			}
		}

		public override void OnWorldUnload() {
			ClearDownedBools(); // Reset downs and trackers to prevent "defeation" of an entry
		}

		public override void PreWorldGen() {
			ClearDownedBools(); // Reset downs and trackers back to false if creating a new world
		}

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenBosses);
			var ForcedMarkedList = new List<string>(ForcedMarkedEntries);

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
			tag["downed_Forced"] = ForcedMarkedList;

			if (worldRecords != null) {
				tag["WorldRecords"] = worldRecords.Concat(unloadedWorldRecords).ToList(); // Combine loaded and unloaded data to prevent lost world record data
			}
		}

		public override void LoadWorldData(TagCompound tag) {
			unloadedWorldRecords.Clear();
			List<WorldRecord> SavedWorldRecords = tag.Get<List<WorldRecord>>("WorldRecords").ToList();
			foreach (WorldRecord record in SavedWorldRecords) {
				int sortedIndex = BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == record.bossKey);
				if (sortedIndex == -1) {
					unloadedWorldRecords.Add(record); // Add any unloaded entries to this list
					continue; // Entry is not loaded
				}
				else if (BossChecklist.bossTracker.SortedBosses[sortedIndex].type != EntryType.Boss)
					continue; // Loaded entry is not a boss

				// Set record data to list based on record index
				// Data here can't be null as the key is checked beforehand
				worldRecords[BossChecklist.bossTracker.SortedBosses[sortedIndex].GetRecordIndex] = record;
			}

			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenBosses.Add(bossKey);
			}

			var ForcedMarkedList = tag.GetList<string>("downed_Forced");
			foreach (var bossKey in ForcedMarkedList) {
				ForcedMarkedEntries.Add(bossKey);
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

			writer.Write(ForcedMarkedEntries.Count);
			foreach (var bossKey in ForcedMarkedEntries) {
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

			ForcedMarkedEntries.Clear();
			count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				ForcedMarkedEntries.Add(reader.ReadString());
			}

			// Update checklist to match Hidden and Forced Downed entries
			BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
			if (BossChecklist.BossLogConfig.HideUnavailable && BossUISystem.Instance.BossLog.PageNum == -1) {
				BossUISystem.Instance.BossLog.UpdateSelectedPage(BossLogUI.Page_TableOfContents);
			}
		}

		public override void PreUpdateWorld() {
			HandleMoonDowns();
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			foreach (NPC npc in Main.npc) {
				BossInfo entry = NPCAssist.GetBossInfo(npc.type);
				if (entry == null)
					continue;

				int recordIndex = entry.GetRecordIndex;
				if (recordIndex == -1 || CheckedRecordIndexes[recordIndex])
					continue; // If the NPC's record index is invalid OR was already handled, move on to the next NPC

				CheckedRecordIndexes[recordIndex] = true; // record index will be handled, so no need to check it again

				// If marked as active...
				if (Tracker_ActiveEntry[recordIndex]) {
					// ...remove any players that become inactive during the fight
					for (int i = 0; i < Main.maxPlayers; i++) {
						if (!Main.player[i].active) {
							Tracker_StartingPlayers[recordIndex, i] = false;
						}
					}

					// ...check if the npc is actually still active or not and display a despawn message if they are no longer active (but not killed!)
					if (NPCAssist.FullyInactive(npc, entry.GetIndex)) {
						Tracker_ActiveEntry[recordIndex] = false; // No longer an active boss (only other time this is set to false is NPC.OnKill)
						string message = NPCAssist.GetDespawnMessage(npc, entry.GetIndex);
						if (!string.IsNullOrEmpty(message)) {
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

			for (int i = 0; i < CheckedRecordIndexes.Length; i++) {
				CheckedRecordIndexes[i] = false; // reset all handled record indexes to false after iterating through all NPCs
			}
		}

		public void AnnounceEventEnd(string eventType) {
			// TODO: Custom/Generic announcements
			NetworkText message = NetworkText.FromKey($"Mods.BossChecklist.EventEnd.{eventType}");
			if (Main.netMode == NetmodeID.SinglePlayer) {
				Main.NewText(message.ToString(), Colors.RarityGreen);
			}
			else {
				ChatHelper.BroadcastChatMessage(message, Colors.RarityGreen);
			}
		}

		public void HandleMoonDowns() {
			// Blood Moon
			if (Main.bloodMoon) {
				Tracker_BloodMoon = true;
			}
			else if (Tracker_BloodMoon) {
				Tracker_BloodMoon = false;
				AnnounceEventEnd("BloodMoon"); // Sends a message to all players that the moon event has ended
				if (!downedBloodMoon) {
					downedBloodMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Frost Moon
			if (Main.snowMoon) {
				Tracker_FrostMoon = true;
			}
			else if (Tracker_FrostMoon) {
				Tracker_FrostMoon = false;
				AnnounceEventEnd("FrostMoon");
				if (!downedFrostMoon) {
					downedFrostMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Pumpkin Moon
			if (Main.pumpkinMoon) {
				Tracker_PumpkinMoon = true;
			}
			else if (Tracker_PumpkinMoon) {
				Tracker_PumpkinMoon = false;
				AnnounceEventEnd("PumpkinMoon");
				if (!downedPumpkinMoon) {
					downedPumpkinMoon = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}

			// Solar Eclipse
			if (Main.eclipse) {
				Tracker_SolarEclipse = true;
			}
			else if (Tracker_SolarEclipse) {
				Tracker_SolarEclipse = false;
				AnnounceEventEnd("SolarEclipse");
				if (!downedSolarEclipse) {
					downedSolarEclipse = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}
		}
	}
}
