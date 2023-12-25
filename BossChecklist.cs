using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	internal class BossChecklist : Mod {
		internal static BossChecklist instance;
		internal static BossTracker bossTracker;
		internal static ModKeybind ToggleChecklistHotKey;
		public static ModKeybind ToggleBossLog;
		private readonly string LastModCallUpdate = "v2.0.0"; // This should be updated whenever there are changes or additions to the ModCalls.
		private readonly string LastVanillaProgressionRevision = "v1.4.0"; // This should be updated whenever a vanilla progression value is changed, or if another vanilla boss is added.

		public static Dictionary<int, int> itemToMusicReference;

		internal static BossLogConfiguration BossLogConfig;
		internal static FeatureConfiguration FeatureConfig;
		public static List<PersonalRecords>[] ServerCollectedRecords;
		public static bool[] Server_AllowTracking;
		public static bool[] Server_AllowNewRecords;

		public BossChecklist() {
		}

		public override void Load() {
			instance = this;
			ToggleChecklistHotKey = KeybindLoader.RegisterKeybind(this, "ToggleChecklist", "P");
			ToggleBossLog = KeybindLoader.RegisterKeybind(this, "ToggleLog", "L");

			FieldInfo itemToMusicField = typeof(MusicLoader).GetField("itemToMusic", BindingFlags.Static | BindingFlags.NonPublic);
			itemToMusicReference = (Dictionary<int, int>)itemToMusicField.GetValue(null);

			bossTracker = new BossTracker();

			On_Player.ApplyMusicBox += Player_ApplyMusicBox;

			Server_AllowTracking = new bool[Main.maxPlayers];
			Server_AllowNewRecords = new bool[Main.maxPlayers];

			/*
			// Fix some translation keys automatically -- TODO
			FieldInfo translationsField = typeof(Mod).GetField("translations", BindingFlags.Instance | BindingFlags.NonPublic);
			var translations = (Dictionary<string, ModTranslation>)translationsField?.GetValue(this);
			if (translations != null) {
				foreach (var translation in translations) {
					if (translation.Value.GetDefault().Contains("ItemID.")) {
						ItemID.Search.GetId()
					}
				}
			}
			*/
		}

		public override void Unload() {
			instance = null;
			ToggleChecklistHotKey = null;
			bossTracker = null;
			ToggleBossLog = null;
			ServerCollectedRecords = null;
			Server_AllowTracking = null;
			Server_AllowNewRecords = null;
			FeatureConfig = null;
			BossLogConfig = null;
		}

		internal void LoggingInitialization() {
			string LangLogMessage = "Mods.BossChecklist.LogMessage.";
			Logger.Info(Language.GetText(LangLogMessage + "LastUpdated_ModCall").Format(LastModCallUpdate) + " " + Language.GetTextValue(LangLogMessage + "ModCallDocumentation"));
			Logger.Info(Language.GetText(LangLogMessage + "LastUpdated_Progression").Format(LastVanillaProgressionRevision));
			if (!BossLogConfig.Debug.ModCallLogVerbose)
				Logger.Info(Language.GetTextValue("NoLogging"));
		}

		internal void LogModCallInfo(string key, params object[] args) {
			if (!BossLogConfig.Debug.ModCallLogVerbose)
				return;

			LocalizedText text = Language.GetText("Mods.BossChecklist.LogMessage." + key);
			Logger.Info(text.Format(args));
		}

		internal void LogWarning(string key, bool requiresConfig, params object[] args) {
			if (requiresConfig && !BossLogConfig.Debug.ModCallLogVerbose)
				return;

			LocalizedText text = Language.GetText("Mods.BossChecklist.LogMessage." + key);
			Logger.Warn(text.Format(args));
		}

		internal static void SaveConfig(BossLogConfiguration bossLogConfig) {
			// in-game ModConfig saving from mod code is not supported yet in tmodloader, and subject to change, so we need to be extra careful.
			// This code only supports client configs, and doesn't call onchanged. It also doesn't support ReloadRequired or anything else.
			MethodInfo saveMethodInfo = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic);
			if (saveMethodInfo != null)
				saveMethodInfo.Invoke(null, new object[] { bossLogConfig });
			else
				BossChecklist.instance.Logger.Warn("In-game SaveConfig failed, code update required");
		}

		private void Player_ApplyMusicBox(On_Player.orig_ApplyMusicBox orig, Player player, Item item) {
			PlayerAssist modplayer = player.GetModPlayer<PlayerAssist>();
			if (bossTracker.IsRegisteredMusicBox(item.type) && !modplayer.BossItemsCollected.Contains(new ItemDefinition(item.type)))
				modplayer.BossItemsCollected.Add(new ItemDefinition(item.type));

			orig(player, item);
		}

		// An alternative approach to the weak reference approach is to do the following in YOUR mod in PostSetupContent
		//Mod bossChecklist = ModLoader.GetMod("BossChecklist");
		//if (bossChecklist != null)
		//{
		//	bossChecklist.Call("AddBoss", "My BossesName", 2.3f, (Func<bool>)(() => MyMod.MyModWorld.downedMyBoss));
		//}
		public override void PostSetupContent() {
			try {
				//thoriumLoaded = ModLoader.GetMod("ThoriumMod") != null;
				//bluemagicLoaded = ModLoader.GetMod("Bluemagic") != null;
				//calamityLoaded = ModLoader.GetMod("CalamityMod") != null;
				//joostLoaded = ModLoader.GetMod("JoostMod") != null;
				//crystiliumLoaded = ModLoader.GetMod("CrystiliumMod") != null;
				//sacredToolsLoaded = ModLoader.GetMod("SacredTools") != null;
				//pumpkingLoaded = ModLoader.GetMod("Pumpking") != null;
			}
			catch (Exception e) {
				Logger.Error($"PostSetupContent Error: {e.StackTrace} {e.Message}");
			}
		}

		// Messages:
		// string:"AddBoss" - string:Bossname - float:bossvalue - Func<bool>:BossDowned
		// 0.2: added 6th parameter to AddBossWithInfo/AddMiniBossWithInfo/AddEventWithInfo: Func<bool> available
		// Merge Notes: AddStatPage added, new AddBoss needed.
		// 1.1: added: string:GetBossInfoDictionary - Mod:mod - string:apiversion
		public override object Call(params object[] args) {
			// Logs messages when a mod is not using an updated call for the boss log, urging them to update.
			int argsLength = args.Length; // Simplify code by resizing args.
			Array.Resize(ref args, 15);
			try {
				string message = args[0] as string;
				// TODO if requested: GetBossInfoDirect for returning a clone of BossInfo directly for strong reference. GetBossInfoExpando if convenient. BossInfoAPI public static class for strong dependencies.
				if (message == "GetBossInfoDictionary") {
					if (args[1] is not Mod mod) {
						throw new Exception($"Call Error: The Mod argument for the attempted message, \"{message}\" has returned null.");
					}
					var apiVersion = args[2] is string ? new Version(args[2] as string) : Version; // Future-proofing. Allowing new info to be returned while maintaining backwards compat if necessary.

					Logger.Info($"{(mod.DisplayName ?? "A mod")} has registered for GetBossInfoDictionary");
					if (!bossTracker.EntriesFinalized)
						LogWarning("LateCall", requiresConfig: false, message);

					//if (message == "GetBossInfoExpando") {
					//	return bossTracker.SortedBosses.ToDictionary(boss => boss.Key, boss => boss.ConvertToExpandoObject());
					//}
					if (message == "GetBossInfoDictionary") {
						return bossTracker.SortedEntries.ToDictionary(boss => boss.Key, boss => boss.ConvertToDictionary(apiVersion));
					}
					return "Failure";
				}

				if (bossTracker.EntriesFinalized)
					throw new Exception($"Call Error: The attempted message, \"{message}\", was sent too late. BossChecklist expects Call messages up until before AddRecipes.");
				
				if (message == "LogBoss" || message == "LogMiniBoss" || message == "LogEvent") {
					if (args[1] is not Mod submittedMod) {
						LogWarning("MustContainMod", requiresConfig: false, args[1] as string);
						return "Failure";
					}

					string internalName = args[2] as string;
					if (!internalName.All(char.IsLetterOrDigit)) {
						LogWarning("MustContainName", requiresConfig: false, internalName);
						return "Failure";
					}

					bossTracker.AddEntry(
						message == "LogBoss" ? EntryType.Boss : message == "LogMiniBoss" ? EntryType.MiniBoss : EntryType.Event,
						submittedMod, // Mod
						internalName, // Internal Name
						Convert.ToSingle(args[3]), // Prog
						args[4] as Func<bool>, // Downed
						InterpretObjectAsListOfInt(args[5]), // NPC IDs
						args[6] as Dictionary<string, object>
					);
					return "Success";
				}
				else if (message.StartsWith("Submit")) {
					OrphanType? DetermineOrphanType() {
						return message switch {
							"SubmitEntryLoot" => OrphanType.SubmitEntryLoot,
							"SubmitEntryCollectibles" => OrphanType.SubmitEntryCollectibles,
							"SubmitEntrySpawnItems" => OrphanType.SubmitEntrySpawnItems,
							"SubmitEventNPCs" => OrphanType.SubmitEventNPCs,
							_ => null
						};
					}

					if (DetermineOrphanType() == null) {
						Logger.Error($"Call Error: Unknown Message: {message}");
						return "Failue";
					}

					if (args[1] is not Mod submittedMod) {
						LogWarning("MustContainMod_Orphan", requiresConfig: false, args[1] as string);
						return "Failure";
					}

					bossTracker.AddOrphanData(
						DetermineOrphanType().Value, // OrphanType
						submittedMod,
						args[2] as Dictionary<string, object> // ID List
					);

					return "Success";
				}
				// TODO
				//else if (message == "GetCurrentBossStates")
				//{
				//	// Returns List<Tuple<string, float, int, bool>>: Name, value, bosstype(boss, miniboss, event), downed.
				//	return bossTracker.allBosses.Select(x => new Tuple<string, float, int, bool>(x.name, x.progression, (int)x.type, x.downed())).ToList();
				//}
				else {
					Logger.Error($"Call Error: Unknown Message: {message}");

					// Track old mod calls to later inform mod developers to update their mod calls.
					if (message.Contains("AddBoss") || message.Contains("AddMiniBoss") || message.Contains("AddEvent")) {
						string entryNameValue = "unknown";
						if (args[1] is Mod mod) {
							string submittedName = args[2] as string;
							string keyOrValue = submittedName.StartsWith("$") ? submittedName.Substring(1) : submittedName;
							entryNameValue = Language.GetTextValue(keyOrValue);

							if (!BossLogConfig.Debug.DisableAutoLocalization) {
								if (message.Contains("Event")) {
									SetupLocalizationForEvent(mod.Name, Language.GetTextValue(entryNameValue.Replace(" ", "")), submittedName, args[9] as string, args[10]);
								}
								else {
									List<int> npcs = InterpretObjectAsListOfInt(args[3]);
									if (npcs.Count > 0)
										SetupLocalizationForNPC(npcs[0], submittedName, args[9] as string, args[10]);
								}
							}							
						}
						else if (args[1] is string submittedName) {
							entryNameValue = submittedName;
						}

						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, entryNameValue);
					}
					else if (message == "AddToBossLoot" || message == "AddToBossCollection" || message == "AddToBossSpawnItems" || message == "AddToEventNPCs") {
						AddToOldCalls(message, args[1] as string);
					}
				}
			}
			catch (Exception e) {
				Logger.Error($"Call Error: {e.StackTrace} {e.Message}");
			}
			return "Failure";

			// Local functions.
			List<int> InterpretObjectAsListOfInt(object data) => data is List<int> ? data as List<int> : (data is int ? new List<int>() { Convert.ToInt32(data) } : null);

			void AddToOldCalls(string message, string name) {
				// TODO: maybe spam the log if ModCompile.activelyModding (needs reflection)
				if (!bossTracker.OldCalls.TryGetValue(message, out List<string> oldCallsList))
					bossTracker.OldCalls.TryAdd(message, oldCallsList = new List<string>());
				oldCallsList.Add(name);
			}

			void SetupLocalizationForNPC(int npcType, string entryName, string spawnInfo, object despawnMessage) {
				ModNPC modNPC = ModContent.GetModNPC(npcType);
				if(modNPC.DisplayName.Value != Language.GetTextValue(entryName)) // No need to register localization key if equal to displayname. Updated Call code will also assume similar logic.
					modNPC.GetLocalization("BossChecklistIntegration.EntryName", () => GetLocalizationEntryValueFromObsoleteSubmission(entryName));
				if(spawnInfo != null) // Required in 1.4.4, so register even if null.
					modNPC.GetLocalization("BossChecklistIntegration.SpawnInfo", () => GetLocalizationEntryValueFromObsoleteSubmission(spawnInfo));
				else
					modNPC.GetLocalization("BossChecklistIntegration.SpawnInfo", () => "Spawn conditions unknown");
				if (despawnMessage != null) { // optional, don't register unless provided
					if (despawnMessage is string)
						modNPC.GetLocalization("BossChecklistIntegration.DespawnMessage", () => GetLocalizationEntryValueFromObsoleteSubmission(despawnMessage as string));
					else if (despawnMessage is Func<NPC, string>)
						modNPC.GetLocalization("BossChecklistIntegration.DespawnMessage", () => "{0} is no longer after you...");
				}
			}

			void SetupLocalizationForEvent(string modName, string internalName, string entryName, string spawnInfo, object despawnMessage) {
				string RegisterKey = $"Mods.{modName}.BossChecklistIntegration.{internalName}";
				Language.GetOrRegister(RegisterKey + ".EntryName", () => GetLocalizationEntryValueFromObsoleteSubmission(entryName));
				if (spawnInfo != null)
					Language.GetOrRegister(RegisterKey + ".SpawnInfo", () => GetLocalizationEntryValueFromObsoleteSubmission(spawnInfo));
				else
					Language.GetOrRegister(RegisterKey + ".SpawnInfo", () => "Spawn conditions unknown");
				if (despawnMessage != null) {
					if (despawnMessage is string)
						Language.GetOrRegister(RegisterKey + ".DespawnMessage", () => GetLocalizationEntryValueFromObsoleteSubmission(despawnMessage as string));
					else if (despawnMessage is Func<NPC, string>)
						Language.GetOrRegister(RegisterKey + ".DespawnMessage", () => "");
				}
			}

			string GetLocalizationEntryValueFromObsoleteSubmission(string input) {
				return input.StartsWith("$") ? $"{{{input}}}" : input;
			}
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			PacketMessageType msgType = (PacketMessageType)reader.ReadByte();
			switch (msgType) {
				// Sent from Client to Server
				case PacketMessageType.RequestHideBoss:
					//if (Main.netMode == NetmodeID.MultiplayerClient)
					//{
					//	Main.NewText("Huh? RequestHideBoss on client?");
					//}
					string bossKey = reader.ReadString();
					bool hide = reader.ReadBoolean();
					if (hide)
						WorldAssist.HiddenEntries.Add(bossKey);
					else
						WorldAssist.HiddenEntries.Remove(bossKey);
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.WorldData);
					//else
					//	ErrorLogger.Log("BossChecklist: Why is RequestHideBoss on Client/SP?");
					break;
				case PacketMessageType.RequestClearHidden:
					//if (Main.netMode == NetmodeID.MultiplayerClient)
					//{
					//	Main.NewText("Huh? RequestClearHidden on client?");
					//}
					WorldAssist.HiddenEntries.Clear();
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.WorldData);
					//else
					//	ErrorLogger.Log("BossChecklist: Why is RequestHideBoss on Client/SP?");
					break;
				case PacketMessageType.RequestMarkedDownEntry:
					bossKey = reader.ReadString();
					bool mark = reader.ReadBoolean();
					if (mark) {
						WorldAssist.MarkedEntries.Add(bossKey);
					}
					else {
						WorldAssist.MarkedEntries.Remove(bossKey);
					}

					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
					break;
				case PacketMessageType.RequestClearMarkedDowns:
					WorldAssist.MarkedEntries.Clear();
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
					break;
				case PacketMessageType.SendClientConfigMessage:
					ClientMessageType messageType = (ClientMessageType)reader.ReadByte();
					if (messageType == ClientMessageType.Despawn) {
						NPC despawnedNPC = Main.npc[reader.ReadInt32()];
						EntryInfo despawnEntry = bossTracker.FindBossEntryByNPC(despawnedNPC.type, out _);
						if (despawnEntry.GetDespawnMessage(despawnedNPC) is LocalizedText despawnMessage)
							Main.NewText(despawnMessage.Format(despawnedNPC.FullName), Colors.RarityPurple);
					}
					else if (messageType == ClientMessageType.Limb) {
						NPC limbNPC = Main.npc[reader.ReadInt32()];
						bossTracker.IsEntryLimb(limbNPC.type, out EntryInfo limbEntry);
						if (limbEntry is not null && limbEntry.GetLimbMessage(limbNPC) is LocalizedText limbMessage)
							Main.NewText(limbMessage.Format(limbNPC.FullName), Colors.RarityPurple);
					}
					else if (messageType == ClientMessageType.Moon) {
						if (WorldAssist.DetermineMoonAnnoucement(reader.ReadString()) is string message)
							Main.NewText(message, new Color(50, 255, 130));
					}
					break;
				case PacketMessageType.UpdateAllowTracking:
					Server_AllowTracking[whoAmI] = reader.ReadBoolean();
					Server_AllowNewRecords[whoAmI] = reader.ReadBoolean(); 
					break;
				case PacketMessageType.SendPersonalBestRecordsToServer:
					// Multiplayer client --> Server (always)
					// When sending records to the server, it should always be sent from a player client, meaning whoAmI can be used to determine the player
					// ServerCollectedRecords is already sorted properly using BossTracker keys and can recieve data the order it is sent in
					foreach (PersonalRecords serverRecord in ServerCollectedRecords[whoAmI]) {
						serverRecord.kills = reader.ReadInt32();
						if (serverRecord.kills > 0)
							serverRecord.playTimeFirst = 1; // if had killed before set play time (value does not matter, as long as its >0)
						serverRecord.durationBest = reader.ReadInt32();
						serverRecord.hitsTakenBest = reader.ReadInt32();
					}
					break;
				case PacketMessageType.UpdateRecordsFromServerToPlayer:
					// Server --> Multiplayer client (always)
					int recordIndex = reader.ReadInt32();
					Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].NetReceiveRecords(reader);
					break;
				case PacketMessageType.RequestWorldRecords:
					// Multiplayer client --> Server
					ModPacket packet = GetPacket();
					packet.Write((byte)PacketMessageType.SendWorldRecordsFromServerToPlayer);
					foreach (string key in bossTracker.BossRecordKeys) {
						int index = WorldAssist.WorldRecordsForWorld.FindIndex(x => x.BossKey == key);
						if (index != -1) {
							packet.Write(key);
							WorldAssist.WorldRecordsForWorld[index].NetSend(packet);
						}
					}
					packet.Send(whoAmI); // Server --> Multiplayer client
					break;
				case PacketMessageType.SendWorldRecordsFromServerToPlayer:
					// Server --> Multiplayer client
					WorldAssist.WorldRecordsForWorld = new List<WorldRecord>();
					foreach (string key in bossTracker.BossRecordKeys) {
						WorldAssist.WorldRecordsForWorld.Add(new WorldRecord(key));
						EntryInfo entry = bossTracker.FindEntryFromKey(reader.ReadString());
						entry.IsRecordIndexed(out recordIndex);
						WorldAssist.WorldRecordsForWorld[recordIndex].NetRecieve(reader);
					}
					break;
				case PacketMessageType.UpdateWorldRecordsToAllPlayers:
					// Server --> Multiplayer client
					recordIndex = reader.ReadInt32();
					WorldAssist.WorldRecordsForWorld[recordIndex].NetReceiveWorldRecords(reader);
					break;
				case PacketMessageType.ResetPlayerRecordForServer:
					// Multiplayer client --> Server
					recordIndex = reader.ReadInt32();
					NetRecordID resetType = (NetRecordID)reader.ReadInt32();
					ServerCollectedRecords[whoAmI][recordIndex].ResetStats_Server(resetType);
					break;
				case PacketMessageType.ResetTrackers:
					// Server --> Multiplayer client (always)
					recordIndex = reader.ReadInt32();
					Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld?[recordIndex].StartTracking();
					break;
				default:
					Logger.Error($"Unknown Message type: {msgType}");
					break;
			}
		}
	}
}
