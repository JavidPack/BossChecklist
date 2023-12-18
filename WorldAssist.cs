using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
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

		public static bool downedBloodMoon;
		public static bool downedFrostMoon;
		public static bool downedPumpkinMoon;
		public static bool downedSolarEclipse;

		public static bool downedDarkMage;
		public static bool downedOgre;
		public static bool downedFlyingDutchman;
		public static bool downedMartianSaucer;

		private static bool TrackingMoons = false; // This will help to prevent moon down checks from leaking into toher worlds

		public override void Load() {
			On_Main.UpdateTime_StartDay += OnStartDay_CheckMoonEvents;
			On_Main.UpdateTime_StartNight += OnStartNight_CheckEclipseDown;
		}

		/// <summary>
		/// Before varibles are change for day (dawn), check for any moon events and mark as defeated it so.
		/// </summary>
		internal static void OnStartDay_CheckMoonEvents(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
			if (TrackingMoons) {
				if (Main.bloodMoon) {
					AnnounceEventEnd("BloodMoon"); // Sends a message to all players that the moon event has ended
					Networking.DownedEntryCheck(ref downedBloodMoon);
				}
				else if (Main.snowMoon) {
					AnnounceEventEnd("FrostMoon");
					Networking.DownedEntryCheck(ref downedFrostMoon);
				}
				else if (Main.pumpkinMoon) {
					AnnounceEventEnd("PumpkinMoon");
					Networking.DownedEntryCheck(ref downedPumpkinMoon);
				}
			}
			orig(ref stopEvents);
		}

		/// <summary>
		/// Before varibles are change for night (dusk), check for the eclipse event and mark as defeated it so.
		/// </summary>
		internal static void OnStartNight_CheckEclipseDown(On_Main.orig_UpdateTime_StartNight orig, ref bool stopEvents) {
			if (Main.eclipse) {
				AnnounceEventEnd("Eclipse");
				Networking.DownedEntryCheck(ref downedSolarEclipse);
			}
			orig(ref stopEvents);
		}

		public override void ClearWorld() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();
			TrackingMoons = false; // turn tracker off
			downedBloodMoon = downedFrostMoon = downedPumpkinMoon = downedSolarEclipse = false; // clear moon downs
			downedDarkMage = downedOgre = downedFlyingDutchman = downedMartianSaucer = false; // clear mini-boss downs
			
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();
			TrackingMoons = true; // ensure trackers are started again once the world is loaded

			// Record related lists that should be the same count of record tracking entries
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
			for (int i = 0; i < Main.maxNPCs; i++) {
				ActiveNPCEntryFlags[i] = -1;
			}
		}

		public override void PreWorldGen() {
			TrackingMoons = false; // World Generation should start with the tracker false before anything (prevents moon defeations leaking into other worlds)
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
				WorldRecordsForWorld.Clear();
				WorldRecordsForWorld_Unloaded.Clear();

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
		}

		public override void NetSend(BinaryWriter writer) {
			// BitBytes can have up to 8 values.
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

			/* 8 flags added, do not add more, move onto next BitsByte
			flags = new BitsByte {

			};
			writer.Write(flags); */

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

			//flags = reader.ReadByte();

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

		public static void AnnounceEventEnd(string eventType) {
			if (Main.netMode == NetmodeID.SinglePlayer && DetermineMoonAnnoucement(eventType) is string message) {
				Main.NewText(message, new Color(50, 255, 130));
			}
			else if (Main.netMode == NetmodeID.Server) {
				// Send a packet to all multiplayer clients. Moon messages are client based, so they will need to read their own configs to determine the message.
				foreach (Player player in Main.player.Where(p => p.active)) {
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.SendClientConfigMessage);
					packet.Write((byte)ClientMessageType.Moon);
					packet.Write(eventType);
					packet.Send(player.whoAmI); // Server --> Multiplayer client
				}
			}
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
				if (selectedEntry.GetDespawnMessage(npc) is LocalizedText message) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						Main.NewText(message.Format(npc.FullName), Colors.RarityPurple);
					}
					else if (Main.netMode == NetmodeID.Server) {
						//ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message.Format(npc.FullName)), Colors.RarityPurple);
						// Send a packet to all multiplayer clients. Limb messages are client based, so they will need to read their own configs to determine the message.
						foreach (Player player in Main.player.Where(p => p.active)) {
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
					foreach (Player player in Main.player.Where(x => x.active)) {
						BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StopTracking_Server(player.whoAmI, false, npc.playerInteraction[player.whoAmI]);
					}
					WorldRecordsForWorld[recordIndex].UpdateGlobalDeaths(npc.playerInteraction.GetTrueIndexes());
				}
			}
		}
	}
}
