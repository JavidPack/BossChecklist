using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class WorldAssist : ModSystem {
		public static List<WorldRecord> WorldRecordsForWorld = new List<WorldRecord>(); // A list of all world records for each boss, saved to each world individually
		public static List<WorldRecord> WorldRecordsForWorld_Unloaded = new List<WorldRecord>(); // A list of all the world records for unloadeded bosses
		public static int[] ActiveNPCEntryFlags; // Used for despawn messages, which will occur when the npc is unflagged

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

			TrackingDowns = startTrackingDowns;
		}

		public override void ClearWorld() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
			ClearDownedBools(false);
			WorldRecordsForWorld.Clear();
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();

			ClearDownedBools(true);
			WorldRecordsForWorld.Clear();

			// Record related lists that should be the same count of record tracking entries
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
			for (int i = 0; i < Main.maxNPCs; i++) {
				ActiveNPCEntryFlags[i] = -1;
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

			tag["downed"] = downed;
			tag["HiddenBossesList"] = HiddenBossesList;
			tag["downed_Forced"] = MarkedAsDownedList;

			// All world record data, loaded or not, needs to be serialized and saved
			TagCompound WorldRecordTag = new TagCompound();
			foreach (WorldRecord record in WorldRecordsForWorld) {
				if (record.CanBeSaved)
					WorldRecordTag.Add(record.BossKey, record.SerializeData());
			}

			WorldRecordsForWorld_Unloaded.ForEach(record => WorldRecordTag.Add(record.BossKey, record.SerializeData()));

			tag["World_Record_Data"] = WorldRecordTag;
		}

		public override void LoadWorldData(TagCompound tag) {
			if (tag.TryGet("World_Record_Data", out TagCompound savedData)) {
				List<WorldRecord> SavedWorldRecords = new List<WorldRecord>();

				foreach (KeyValuePair<string, object> data in savedData) {
					SavedWorldRecords.Add(WorldRecord.DESERIALIZER(data.Value as TagCompound)); // deserialize the saved world record data
				}

				// Iterate through the saved data and store any records that are not loaded/active with the current mods
				foreach (WorldRecord record in SavedWorldRecords) {
					if (!BossChecklist.bossTracker.BossRecordKeys.Contains(record.BossKey))
						WorldRecordsForWorld_Unloaded.Add(record); // any saved records from an unloaded boss must be perserved
				}

				// Iterate through the boss record keys to assign each record to where itshould be placed
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					int index = SavedWorldRecords.FindIndex(x => x.BossKey == key);
					WorldRecordsForWorld.Add(index == -1 ? new WorldRecord(key) : SavedWorldRecords[index]); // create a new entry if not in the list, otherwise use the saved data
				}
			}
			else {
				BossChecklist.bossTracker.BossRecordKeys.ForEach(key => WorldRecordsForWorld.Add(new WorldRecord(key))); // create a new entry if no saved data was found
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
		}

		public static string DetermineMoonAnnoucement(string eventType) {
			if (BossChecklist.FeatureConfig.MoonMessages == "Generic") {
				string eventTypeLocal = Language.Exists($"Bestiary_Events.{eventType}") ? Language.GetTextValue($"Bestiary_Events.{eventType}") : Language.GetTextValue($"Bestiary_Invasions.{eventType}");
				if (eventType == "Eclipse")
					eventTypeLocal = eventTypeLocal.ToLower();
				return Language.GetText($"{NPCAssist.LangChat}.EventEnd.Generic").Format(eventTypeLocal);
			}
			else if (BossChecklist.FeatureConfig.MoonMessages == "Unique") {
				return Language.GetTextValue($"{NPCAssist.LangChat}.EventEnd.{eventType}");
			}

			return null;
		}

		public void AnnounceEventEnd(string eventType) {
			if (Main.netMode == NetmodeID.SinglePlayer && DetermineMoonAnnoucement(eventType) is string message) {
				Main.NewText(message, new Color(50, 255, 130));
			}
			else if (Main.netMode == NetmodeID.Server) {
				// Send a packet to all multiplayer clients. Moon messages are client based, so they will need to read their own configs to determine the message.
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.SendClientConfigMessage);
				packet.Write((byte)ClientMessageType.Moon);
				packet.Write(eventType);
				packet.Send();
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
				AnnounceEventEnd("Eclipse");
				if (!downedSolarEclipse) {
					downedSolarEclipse = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}
		}

		/// <summary>
		/// Loops through all NPCs to check their active status.
		/// Once inactive, the entry is unflagged and will have its despawn message displayed in chat.
		/// Any record trackers currently active will stop if all instances of the entry's NPCs are no longer active.
		/// </summary>
		public void HandleDespawnFlags() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				if (ActiveNPCEntryFlags[i] == -1)
					continue; // skip invalid entries

				NPC npc = Main.npc[i];
				if (npc.active)
					continue; // Don't trigger despawn message or stop trackers if the npc is still active

				EntryInfo selectedEntry = BossChecklist.bossTracker.SortedEntries[ActiveNPCEntryFlags[i]];
				ActiveNPCEntryFlags[i] = -1; // if the npc tracked is inactive, remove entry value

				if (ActiveNPCEntryFlags.Any(x => x == selectedEntry.GetIndex))
					continue; // do nothing if any other npcs are apart of the entry and are still active

				// Now that the entry no longer exists within ActiveNPCEntryFlags, it is determined to have despawned
				if (selectedEntry.GetDespawnMessage(npc) is LocalizedText message) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						Main.NewText(message.Format(npc.FullName), Colors.RarityPurple);
					}
					else if (Main.netMode == NetmodeID.Server) {
						//ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message.Format(npc.FullName)), Colors.RarityPurple);
						// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.SendClientConfigMessage);
						packet.Write((byte)ClientMessageType.Despawn);
						packet.Write(npc.whoAmI);
						packet.Send();
					}
				}

				// When a boss despawns, stop tracking it for all players
				selectedEntry.IsRecordIndexed(out int recordIndex);
				foreach (Player player in Main.player) {
					if (!player.active)
						continue;

					if (Main.netMode == NetmodeID.Server) {
						PersonalRecords serverRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex];
						serverRecords.StopTracking_Server(player.whoAmI, false, npc.playerInteraction[player.whoAmI]);
					}
					else {
						player.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].StopTracking(false, npc.playerInteraction[player.whoAmI]);
					}
				}

				if (Main.netMode == NetmodeID.Server)
					WorldRecordsForWorld[recordIndex].UpdateGlobalDeaths(npc.playerInteraction.GetTrueIndexes());
			}
		}
	}
}
