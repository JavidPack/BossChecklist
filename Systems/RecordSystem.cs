using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist.Systems
{
	public class RecordSystem : ModSystem {
		public static List<WorldRecord> WorldRecordsForWorld = new List<WorldRecord>(); // A list of all world records for each boss, saved to each world individually
		public static List<WorldRecord> WorldRecordsForWorld_Unloaded = new List<WorldRecord>(); // A list of all the world records for unloadeded bosses
		public static int[] ActiveNPCEntryFlags; // Used for despawn messages, which will occur when the npc is unflagged

		public override void ClearWorld() {
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();

			ActiveNPCEntryFlags = new int[Main.maxNPCs];
		}

		public override void OnWorldLoad() {
			WorldRecordsForWorld.Clear();
			WorldRecordsForWorld_Unloaded.Clear();

			// Record related lists that should be the same count of record tracking entries
			ActiveNPCEntryFlags = new int[Main.maxNPCs];
			for (int i = 0; i < Main.maxNPCs; i++) {
				ActiveNPCEntryFlags[i] = -1;
			}
		}

		public override void SaveWorldData(TagCompound tag) {
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
					Main.LocalPlayer.GetModPlayer<RecordModPlayer>().RecordsForWorld?[recordIndex].StopTracking(false, npc.playerInteraction[Main.LocalPlayer.whoAmI]);
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

	class RecordBossNPC : GlobalNPC {
		// When an entry NPC spawns, setup the world and player trackers for the upcoming fight
		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (Main.netMode == NetmodeID.MultiplayerClient || BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // Only single player and server should be starting the record tracking process

			RecordSystem.ActiveNPCEntryFlags[npc.whoAmI] = entry.GetIndex;

			if (Main.netMode is NetmodeID.SinglePlayer) {
				Main.LocalPlayer.GetModPlayer<RecordModPlayer>().RecordsForWorld?[recordIndex].StartTracking(); // start tracking for active players
			}
			else if (Main.netMode is NetmodeID.Server) {
				foreach (Player player in Main.ActivePlayers) {
					BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StartTracking_Server(player.whoAmI);
				}
			}
		}

		// When an NPC is killed and fully inactive the fight has ended, so stop all record trackers
		public override void OnKill(NPC npc) {
			HandleDownedNPCs(npc.type); // Custom downed bool code
			RecordSystem.ActiveNPCEntryFlags[npc.whoAmI] = -1; // NPC is killed, unflag their active status

			if (BossChecklist.bossTracker.FindBossEntryByNPC(npc.type, out int recordIndex) is not EntryInfo entry)
				return; // make sure NPC has a valid entry and that no other NPCs exist with that entry index

			if (RecordSystem.ActiveNPCEntryFlags.Any(x => x == entry.GetIndex))
				return;

			bool newPersonalBestOnServer = false;
			// stop tracking and record stats for those who had interactions with the boss

			if (Main.netMode is NetmodeID.SinglePlayer) {
				bool interaction = npc.playerInteraction[Main.LocalPlayer.whoAmI];
				Main.LocalPlayer.GetModPlayer<RecordModPlayer>().RecordsForWorld?[recordIndex].StopTracking(interaction && BossChecklist.FeatureConfig.AllowNewRecords, interaction);
			}
			else if (Main.netMode is NetmodeID.Server) {
				foreach (Player player in Main.ActivePlayers) {
					bool interaction = npc.playerInteraction[player.whoAmI];
					if (BossChecklist.ServerCollectedRecords[player.whoAmI][recordIndex].StopTracking_Server(player.whoAmI, interaction && BossChecklist.Server_AllowNewRecords[player.whoAmI], interaction))
						newPersonalBestOnServer = true; // if any player gets a new persoanl best on the server...
				}
			}

			// ... check to see if it is a world record and update every player's logs if so
			if (newPersonalBestOnServer) {
				// at this point recordIndex should never be -1, so just ensure the world records collection is properly populated
				if (recordIndex < RecordSystem.WorldRecordsForWorld.Count) {
					Console.WriteLine($"A Personal Best was beaten! Comparing against world records...");
					RecordSystem.WorldRecordsForWorld[recordIndex].CheckForWorldRecords_Server(npc.playerInteraction.GetTrueIndexes());
				}
				else {
					BossChecklist.instance.Logger.Warn(
						$"A Personal Best was beaten, but something went wrong when comparing with world records. " +
						$"World Records count is {RecordSystem.WorldRecordsForWorld.Count}. " +
						$"Record Index is {recordIndex}."); // change to a key? might not need if I can fix this later
				}
			}
		}

		/// <summary>
		/// Handles all of BossChecklist's custom downed variables, makring them as defeated and updating all clients when needed.
		/// </summary>
		/// <returns>If the corresponding flag was flipped.</returns>
		internal static void HandleDownedNPCs(int npcType) {
			switch (npcType) {
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
		}
	}

	public class RecordModPlayer : ModPlayer {
		// Records are bound to characters, but records are independent between worlds as well.
		// AllStored records contains every player record from every world
		// RecordsForWorld is a reference to the specfic player records of the current world
		// We split up AllStoredRecords with 'Main.ActiveWorldFileData.UniqueId.ToString()' as keys
		public Dictionary<string, List<PersonalRecords>> AllStoredRecords;
		public bool PlayerRecordsInitialized;

		/// <summary>
		/// Fetches the list of records assigned to the current world from the list of all stored records by the player.
		/// </summary>
		public List<PersonalRecords> RecordsForWorld => AllStoredRecords.TryGetValue(Main.ActiveWorldFileData.UniqueId.ToString(), out List<PersonalRecords> ValidKey) ? ValidKey : null;
		public Dictionary<string, int> MiniBossKills;

		public const int RecordState_NoRecord = 0;
		public const int RecordState_PersonalBest = 1;
		public const int RecordState_WorldRecord = 2;
		public int NewRecordState = 0;
		public bool[] hasNewRecord;

		public void SubmitCombatText(int recordIndex) {
			if (NewRecordState == RecordState_PersonalBest)
				CombatText.NewText(Player.getRect(), Color.LightYellow, Language.GetTextValue($"{BossLogUI.LangLog}.Records.NewRecord"), true);
			else if (NewRecordState == RecordState_WorldRecord)
				CombatText.NewText(Player.getRect(), Color.LightYellow, Language.GetTextValue($"{BossLogUI.LangLog}.Records.NewWorldRecord"), true);

			if (NewRecordState != RecordState_NoRecord)
				hasNewRecord[recordIndex] = true;

			NewRecordState = RecordState_NoRecord;
		}

		public override void Initialize() {
			PlayerRecordsInitialized = false;

			AllStoredRecords = new Dictionary<string, List<PersonalRecords>>();
			MiniBossKills = new Dictionary<string, int>();

			hasNewRecord = Array.Empty<bool>();
		}

		public override void SaveData(TagCompound tag) {
			// We cannot save dictionaries, so we'll convert it to a TagCompound instead
			TagCompound Record_Data = new TagCompound();
			foreach (KeyValuePair<string, List<PersonalRecords>> data in AllStoredRecords) {
				TagCompound Record_PerWorld = new TagCompound(); // new list of records for each world
				foreach (PersonalRecords record in data.Value) {
					if (record.CanBeSaved)
						Record_PerWorld.Add(record.BossKey, record.SerializeData()); // serialize the boss key and records (for each world)
				}
				Record_Data.Add(data.Key, Record_PerWorld);
			}

			TagCompound MiniBossKillData = new TagCompound();
			foreach (KeyValuePair<string, int> pair in MiniBossKills) {
				MiniBossKillData.Add(pair.Key, pair.Value);
			}

			tag["Record_Data"] = Record_Data;
			tag["MiniBossKillCount"] = MiniBossKillData;
		}

		public override void LoadData(TagCompound tag) {
			if (tag.TryGet("Record_Data", out TagCompound savedData)) {
				AllStoredRecords.Clear();
				// foreach unique world key
				foreach (KeyValuePair<string, object> data in savedData) {
					List<PersonalRecords> RecordsByWorldKey = new List<PersonalRecords>();
					// foreach 
					foreach (KeyValuePair<string, object> listofrecords in data.Value as TagCompound) {
						RecordsByWorldKey.Add(PersonalRecords.DESERIALIZER(listofrecords.Value as TagCompound)); // deserialize the saved record data
					}
					AllStoredRecords.TryAdd(data.Key, RecordsByWorldKey); // add each world key to all stored records
				}
			}

			if (tag.TryGet("MiniBossKillCount", out TagCompound savedKills)) {
				MiniBossKills.Clear();
				// foreach unique world key
				foreach (KeyValuePair<string, object> data in savedKills) {
					MiniBossKills.TryAdd(data.Key, (int)data.Value);
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (!BossChecklist.bossTracker.EntryCache[target.type] || target.life > 0)
				return;

			foreach (EntryInfo entry in BossChecklist.bossTracker.SortedEntries.Where(x => x.type == EntryType.MiniBoss)) {
				if (entry.npcIDs.Contains(target.type)) {
					if (MiniBossKills.ContainsKey(entry.Key)) {
						MiniBossKills[entry.Key]++;
					}
					else {
						MiniBossKills.TryAdd(entry.Key, 1);
					}
				}
			}
		}

		public override void OnEnterWorld() {
			// Upon entering a world, determine if records already exist for a player and copy them into 'RecordsForWorld'
			// If personal records do not exist for this world, create a new entry for the player to use
			string WorldID = Main.ActiveWorldFileData.UniqueId.ToString();
			if (AllStoredRecords.TryGetValue(WorldID, out List<PersonalRecords> tempRecords)) {
				List<PersonalRecords> unloadedRecords = new List<PersonalRecords>();
				List<PersonalRecords> sortedRecords = new List<PersonalRecords>();
				foreach (PersonalRecords record in tempRecords) {
					if (!BossChecklist.bossTracker.BossRecordKeys.Contains(record.BossKey))
						unloadedRecords.Add(record); // any saved records from an unloaded boss must be perserved
				}

				// iterate through the record keys to keep the data in order
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					int index = tempRecords.FindIndex(x => x.BossKey == key);
					sortedRecords.Add(index == -1 ? new PersonalRecords(key) : tempRecords[index]);
				}

				AllStoredRecords[WorldID] = sortedRecords.Concat(unloadedRecords).ToList();
			}
			else {
				List<PersonalRecords> NewRecordListForWorld = new List<PersonalRecords>();
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					NewRecordListForWorld.Add(new PersonalRecords(key));
				}
				AllStoredRecords.TryAdd(WorldID, NewRecordListForWorld); // A new entry will be added to AllStoredRecords so that it can be saved when needed
			}
			PlayerRecordsInitialized = true;

			hasNewRecord = new bool[BossChecklist.bossTracker.BossRecordKeys.Count];

			BossChecklist.Server_AllowTracking[Player.whoAmI] = BossChecklist.FeatureConfig.RecordTrackingEnabled;
			BossChecklist.Server_AllowNewRecords[Player.whoAmI] = BossChecklist.FeatureConfig.AllowNewRecords;

			// When a player joins a world, their Personal Best records will need to be sent to the server for new Personal Best comparing
			// The server doesn't need player records from every world, just the current one
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.SendPersonalBestRecordsToServer);
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					int index = RecordsForWorld.FindIndex(x => x.BossKey == key);
					if (index != -1) {
						packet.Write(RecordsForWorld[index].kills);
						packet.Write(RecordsForWorld[index].durationBest);
						packet.Write(RecordsForWorld[index].hitsTakenBest);
					}
				}
				packet.Send(); // Multiplayer client --> Server

				packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.RequestWorldRecords);
				packet.Write(BossChecklist.bossTracker.BossRecordKeys.Count);
				foreach (string key in BossChecklist.bossTracker.BossRecordKeys) {
					packet.Write(key);
				}
				packet.Send(); // Multiplayer client --> Server

				packet = Mod.GetPacket();
				packet.Write((byte)PacketMessageType.UpdateAllowTracking);
				packet.Write(BossChecklist.FeatureConfig.RecordTrackingEnabled);
				packet.Write(BossChecklist.FeatureConfig.AllowNewRecords);
				packet.Send(); // Multiplayer client --> Server
			}
		}

		// Track each tick that passes during boss fights.
		public override void PreUpdate() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.whoAmI == 255)
				return;
			/* Debug tool for opening the Progression Mode prompt
			if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
				hasOpenedTheBossLog = false;
			*/
			List<PersonalRecords> EntryRecords = Main.netMode == NetmodeID.Server ? BossChecklist.ServerCollectedRecords[Player.whoAmI] : RecordsForWorld;
			if (EntryRecords is null)
				return;

			foreach (PersonalRecords record in EntryRecords) {
				if (record.IsCurrentlyBeingTracked)
					record.Tracker_Duration++;
			}
		}

		// Track amount of times damage was taken during a boss fight. Source of damage does not matter.
		public override void OnHurt(Player.HurtInfo info) {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.whoAmI == 255)
				return;

			List<PersonalRecords> EntryRecords = Main.netMode == NetmodeID.Server ? BossChecklist.ServerCollectedRecords[Player.whoAmI] : RecordsForWorld;
			if (EntryRecords is null)
				return;

			foreach (PersonalRecords record in EntryRecords) {
				if (record.IsCurrentlyBeingTracked)
					record.Tracker_HitsTaken++;
			}
		}

		// Track player deaths during boss fights.
		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.whoAmI == 255)
				return;

			List<PersonalRecords> EntryRecords = Main.netMode == NetmodeID.Server ? BossChecklist.ServerCollectedRecords[Player.whoAmI] : RecordsForWorld;
			if (EntryRecords is null)
				return;

			foreach (PersonalRecords record in EntryRecords) {
				if (record.IsCurrentlyBeingTracked)
					record.Tracker_Deaths++;
			}
		}

		// Record tracking should stop if the player disconnects from the world.
		public override void PlayerDisconnect() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.whoAmI == 255)
				return;

			if (Main.netMode == NetmodeID.Server) {
				BossChecklist.ServerCollectedRecords[Player.whoAmI].ForEach(record => record.StopTracking_Server(Player.whoAmI, false, false));
			}
			else {
				RecordsForWorld?.ForEach(record => record.StopTracking(false, false)); // Note: Disconnecting still tracks attempts and deaths. Does not save last attempt data.
			}
		}
	}
}
