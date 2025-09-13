using BossChecklist.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
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

		public override void ClearWorld() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();
			
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();

			// Record related lists that should be the same count of record tracking entries
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
			for (int i = 0; i < Main.maxNPCs; i++) {
				ActiveNPCEntryFlags[i] = -1;
			}
		}

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenEntries);
			var MarkedAsDownedList = new List<string>(MarkedEntries);

			tag["HiddenBossesList"] = HiddenBossesList;
			tag["downed_Forced"] = MarkedAsDownedList;

			// All world record data, loaded or not, needs to be serialized and saved
			TagCompound WorldRecordTag = new TagCompound();
			foreach (WorldRecord record in WorldRecordsForWorld) {
				if (record.CanBeSaved)
					WorldRecordTag.Add(record.BossKey, record.SerializeData());
			}

			foreach (WorldRecord record in WorldRecordsForWorld_Unloaded) {
				if (!WorldRecordTag.ContainsKey(record.BossKey))
					WorldRecordTag.Add(record.BossKey, record.SerializeData());
			}

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
		}

		public override void NetSend(BinaryWriter writer) {
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
			if (BossChecklistUI.Visible)
				BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();

			if (BossUISystem.Instance.BossLog.BossLogVisible && BossUISystem.Instance.BossLog.PageNum == -1) {
				BossUISystem.Instance.BossLog.RefreshPageContent();
			}
		}

		public override void PreUpdateWorld() {
			HandleDespawnFlags();
		}

		/// <summary>
		/// Loops through all NPCs to check their active status.
		/// Once inactive, the entry is unflagged and will have its despawn message displayed in chat.
		/// Any record trackers currently active will stop if all instances of the entry's NPCs are no longer active.
		/// </summary>
		public void HandleDespawnFlags() {
			foreach (NPC npc in Main.npc) {
				if (npc.whoAmI >= Main.maxNPCs || ActiveNPCEntryFlags[npc.whoAmI] == -1 || npc.active)
					continue; // skip unflagged entries. If flagged, don't trigger despawn message or stop trackers if the npc is still active

				EntryInfo selectedEntry = BossChecklist.bossTracker.SortedEntries[ActiveNPCEntryFlags[npc.whoAmI]];
				ActiveNPCEntryFlags[npc.whoAmI] = -1; // if the npc tracked is inactive, remove entry value

				if (ActiveNPCEntryFlags.Contains(selectedEntry.GetIndex))
					continue; // do nothing if any other npcs are apart of the entry and are still active

				// Now that the entry no longer exists within ActiveNPCEntryFlags, it is determined to have despawned
				// The Moon Lord has a special case, since it technically despawns when its killed
				if (selectedEntry.GetDespawnMessage(npc) is LocalizedText message && (selectedEntry.Key != "Terraria MoonLord" || npc.life > 0)) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						Main.NewText(message.Format(npc.FullName), Colors.RarityPurple);
					}
					else if (Main.netMode == NetmodeID.Server) {
						//ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message.Format(npc.FullName)), Colors.RarityPurple);
						// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
						foreach (Player player in Main.ActivePlayers) {
							ModPacket packet = BossChecklist.instance.GetPacket();
							packet.Write((byte)PacketMessageType.SendClientConfigMessage);
							packet.Write((byte)ClientMessageType.Despawn);
							packet.Write(npc.whoAmI);
							packet.Send(player.whoAmI); // Server --> Multiplayer client
						}
					}
				}

				// When a boss despawns, stop tracking it for all players
				if (!selectedEntry.IsRecordIndexed(out int recordIndex))
					continue;

				if (Main.netMode is NetmodeID.SinglePlayer) {
					Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].StopTracking(false, npc.playerInteraction[Main.LocalPlayer.whoAmI]);
				}
				else if (Main.netMode is NetmodeID.Server) {
					foreach (Player player in Main.ActivePlayers) {
						BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StopTracking_Server(player.whoAmI, false, npc.playerInteraction[player.whoAmI]);
					}
					WorldRecordsForWorld[recordIndex].UpdateGlobalDeaths(npc.playerInteraction.GetTrueIndexes());
				}
			}
		}
	}
}
