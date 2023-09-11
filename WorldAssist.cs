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
	public class WorldAssist : ModSystem {
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

		public static int[] ActiveNPCEntryFlags;

		public static HashSet<string> HiddenEntries = new HashSet<string>();
		public static HashSet<string> MarkedEntries = new HashSet<string>();

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
		public static bool TrackingDowns = false;

		public override void Load() {
			On_DD2Event.WinInvasionInternal += DD2Event_WinInvasionInternal;
		}

		private void DD2Event_WinInvasionInternal(On_DD2Event.orig_WinInvasionInternal orig) {
			orig();
			if (DD2Event.OngoingDifficulty == 2)
				downedInvasionT2Ours = true;
			if (DD2Event.OngoingDifficulty == 3)
				downedInvasionT3Ours = true;
		}

		private void ClearDownedBools(bool startTrackingDowns = false) {
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

			TrackingDowns = startTrackingDowns;
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();

			ClearDownedBools(true);

			// Record related lists that should be the same count of record tracking entries
			worldRecords = new WorldRecord[BossChecklist.bossTracker.BossRecordKeys.Count];
			unloadedWorldRecords = new List<WorldRecord>();
			CheckedRecordIndexes = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
			for (int i = 0; i < Main.maxNPCs; i++) {
				ActiveNPCEntryFlags[i] = -1;
			}
			Tracker_ActiveEntry = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];
			Tracker_StartingPlayers = new bool[BossChecklist.bossTracker.BossRecordKeys.Count, Main.maxPlayers];

			// Populate world records list
			foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
				if (BossChecklist.bossTracker.SortedEntries.Find(x => x.Key == key) is EntryInfo entry && entry.IsRecordIndexed(out int recordIndex))
					worldRecords[recordIndex] = new WorldRecord(key);
			}
		}

		public override void OnWorldUnload() {
			ClearDownedBools(); // Reset downs and trackers to prevent "defeation" of an entry
		}

		public override void PreWorldGen() {
			ClearDownedBools(); // Reset downs and trackers back to false if creating a new world
		}

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenEntries);
			var MarkedAsDownedList = new List<string>(MarkedEntries);

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
			tag["downed_Forced"] = MarkedAsDownedList;

			if (worldRecords != null) {
				tag["WorldRecords"] = worldRecords.Concat(unloadedWorldRecords).ToList(); // Combine loaded and unloaded data to prevent lost world record data
			}
		}

		public override void LoadWorldData(TagCompound tag) {
			unloadedWorldRecords.Clear();
			List<WorldRecord> SavedWorldRecords = tag.Get<List<WorldRecord>>("WorldRecords").ToList();
			foreach (WorldRecord record in SavedWorldRecords) {
				if (BossChecklist.bossTracker.SortedEntries.Find(x => x.Key == record.bossKey) is not EntryInfo entry) {
					unloadedWorldRecords.Add(record); // Add any unloaded entries to this list
					continue; // Entry is not loaded
				}
				else if (entry.IsRecordIndexed(out int recordIndex)) {
					// Set record data to list based on record index
					// if there is no record value, skip the entry as it is not a boss
					worldRecords[recordIndex] = record;
				}
			}

			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenEntries.Add(bossKey);
			}

			var MarkedAsDownedList = tag.GetList<string>("downed_Forced");
			foreach (var bossKey in MarkedAsDownedList) {
				MarkedEntries.Add(bossKey);
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

			writer.Write(HiddenEntries.Count);
			foreach (var bossKey in HiddenEntries) {
				writer.Write(bossKey);
			}

			writer.Write(MarkedEntries.Count);
			foreach (var bossKey in MarkedEntries) {
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

			HiddenEntries.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				HiddenEntries.Add(reader.ReadString());
			}

			MarkedEntries.Clear();
			count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				MarkedEntries.Add(reader.ReadString());
			}

			// Update checklist to match Hidden and Marked Downed entries
			BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
			if (BossUISystem.Instance.BossLog.BossLogVisible && BossUISystem.Instance.BossLog.PageNum == -1) {
				BossUISystem.Instance.BossLog.RefreshPageContent();
			}
		}

		public override void PreUpdateWorld() {
			HandleMoonDowns();
			HandleDespawnFlags();

			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			foreach (NPC npc in Main.npc) {
				if (NPCAssist.GetEntryInfo(npc.type, out int recordIndex) is not EntryInfo entry || CheckedRecordIndexes[recordIndex])
					continue; // If the NPC's record index is invalid OR was already handled, move on to the next NPC

				CheckedRecordIndexes[recordIndex] = true; // record index will be handled, so no need to check it again

				// If marked as active...
				if (Tracker_ActiveEntry[recordIndex]) {
					// ...remove any players that become inactive during the fight
					foreach (Player player in Main.player) {
						if (!player.active)
							Tracker_StartingPlayers[recordIndex, player.whoAmI] = false;
					}

					// ...check if the npc is actually still active or not and display a despawn message if they are no longer active (but not killed!)
					if (NPCAssist.FullyInactive(npc, entry.GetIndex))
						Tracker_ActiveEntry[recordIndex] = false; // No longer an active boss (only other time this is set to false is NPC.OnKill)
				}
			}

			for (int i = 0; i < CheckedRecordIndexes.Length; i++) {
				CheckedRecordIndexes[i] = false; // reset all handled record indexes to false after iterating through all NPCs
			}
		}

		public void AnnounceEventEnd(string eventType) {
			// TODO: Custom/Generic announcements
			NetworkText message = NetworkText.FromKey($"{NPCAssist.LangChat}.EventEnd.{eventType}");
			if (Main.netMode == NetmodeID.SinglePlayer) {
				Main.NewText(message.ToString(), Colors.RarityGreen);
			}
			else {
				ChatHelper.BroadcastChatMessage(message, Colors.RarityGreen);
			}
		}

		public void HandleMoonDowns() {
			if (!TrackingDowns)
				return; // Do not track moon phase when it shouldn't. Should help with data leaking into other worlds.

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

		public void HandleDespawnFlags() {
			int entryValue = -1;
			for (int i = 0; i < ActiveNPCEntryFlags.Length - 1; i++) {
				entryValue = ActiveNPCEntryFlags[i]; // keep track of the active entry
				if (entryValue == -1)
					continue; // skip non-boss entries

				NPC npc = Main.npc[i];
				if (!npc.active) {
					ActiveNPCEntryFlags[i] = -1; // if the npc tracked is inactive, remove entry value

					if (ActiveNPCEntryFlags.Any(x => x == entryValue))
						continue; // if the entry value no longer exists in the array, display the message. Otherwise, do nothing until all respective npcs are inactive.

					if (BossChecklist.bossTracker.SortedEntries[entryValue].GetDespawnMessage(npc) is LocalizedText message) {
						if (Main.netMode == NetmodeID.SinglePlayer) {
							Main.NewText(message.Format(npc.FullName), Colors.RarityPurple);
						}
						else {
							ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message.Format(npc.FullName)), Colors.RarityPurple);
						}
					}
				}
			}
		}
	}
}
