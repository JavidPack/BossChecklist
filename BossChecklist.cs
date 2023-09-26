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
		private string LastVanillaProgressionRevision = "v1.4.0"; // This should be updated whenever a vanilla progression value is changed, or if another vanilla boss is added.

		public static Dictionary<int, int> itemToMusicReference;

		internal static ClientConfiguration ClientConfig;
		internal static DebugConfiguration DebugConfig;
		internal static BossLogConfiguration BossLogConfig;
		public static List<BossRecord>[] ServerCollectedRecords;

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
			MapAssist.FullMapInitialize();

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

			Logger.Info($"Progression values for vanilla entries have been last updated on BossChecklist {LastVanillaProgressionRevision}");
			if (!DebugConfig.ModCallLogVerbose)
				Logger.Info("Boss Log integration messages will not be logged.");
		}

		public override void Unload() {
			instance = null;
			ToggleChecklistHotKey = null;
			bossTracker = null;
			ToggleBossLog = null;
			ServerCollectedRecords = null;
			ClientConfig = null;
			DebugConfig = null;
			BossLogConfig = null;
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

					if (!bossTracker.EntriesFinalized) {
						Logger.Warn($"Call Warning: The attempted message, \"{message}\", was sent too early. Expect the Call message to return incomplete data. For best results, call in PostAddRecipes.");
					}
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
						Logger.Warn($"Invalid mod instance passed ({args[1] as string}). Your call must contain a Mod instance to generate an entry key.");
						return "Failure";
					}

					string internalName = args[2] as string;
					if (!internalName.All(char.IsLetterOrDigit)) {
						Logger.Warn($"Invalid internal name passed ({internalName}). Your call must contain a string comprised of letters and/or digits without whitespace characters in order to generate an entry key.");
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
							"SubmitEntryLoot" => OrphanType.Loot,
							"SubmitEntryCollectibles" => OrphanType.Collectibles,
							"SubmitEntrySpawnItems" => OrphanType.SpawnItems,
							"SubmitEventNPCs" => OrphanType.EventNPC,
							_ => null
						};
					}

					if (DetermineOrphanType() == null) {
						Logger.Error($"Call Error: Unknown Message: {message}");
						return "Failue";
					}

					if (args[1] is not Mod submittedMod) {
						Logger.Error($"Invalid mod instance passed ({args[1] as string}). Your call must contain a Mod instance for logging purposes.");
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

							if (!DebugConfig.DisableAutoLocalization) {
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
			Func<NPC, string> InterpretObjectAsStringFunction(object data) => data is Func<NPC, string> ? data as Func<NPC, string> : (data is string ? npc => data as string : null);
			List<string> InterpretObjectAsListOfStrings(object data) => data is List<string> ? data as List<string> : (data is string ? new List<string>() { data as string } : null);

			void AddToOldCalls(string message, string name) {
				// TODO: maybe spam the log if ModCompile.activelyModding (needs reflection)
				if (!bossTracker.OldCalls.TryGetValue(message, out List<string> oldCallsList))
					bossTracker.OldCalls.Add(message, oldCallsList = new List<string>());
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
			PlayerAssist modPlayer;
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
				case PacketMessageType.SendAllRecordsFromPlayerToServer:
					// When sending records to the server, it should always be sent from a player client, meaning whoAmI can be used to determine the player
					int totalCount = reader.ReadInt32();
					int invalidConflicts = 0;
					for (int i = 0; i < totalCount; i++) {
						// Read the bossKey and attempt to locate its position within the server's collection of records
						// If index is invalid (which it shouldn't be), send a relay message and continue the process
						string key = reader.ReadString();
						int index = ServerCollectedRecords[whoAmI].FindIndex(x => x.bossKey == key);
						if (index == -1) {
							invalidConflicts++;
							continue;
						}

						// Read the stats sent to the server and update them
						PersonalStats bossStats = ServerCollectedRecords[whoAmI][index].stats;
						bossStats.durationPrev = reader.ReadInt32();
						bossStats.durationBest = reader.ReadInt32();
						bossStats.hitsTakenPrev = reader.ReadInt32();
						bossStats.hitsTakenBest = reader.ReadInt32();
					}

					if (invalidConflicts > 0) {
						Console.ForegroundColor = ConsoleColor.DarkRed;
						Console.WriteLine($"Personal records for player '{Main.player[whoAmI].name}' has been retrieved with {invalidConflicts} conflicts");
					}
					else {
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine($"Personal records for player '{Main.player[whoAmI].name}' has successfully been retrieved!");
					}
					Console.ResetColor();
					break;
				case PacketMessageType.UpdateRecordsFromServerToPlayer:
					// The server just sent updated information for a player's records and it will be used to update the records for the client as well
					// Since the packet is being sent with 'toClient: i', LocalPlayer can be used here
					int recordIndex = reader.ReadInt32();
					Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld[recordIndex].stats.NetRecieve(reader);
					break;
				case PacketMessageType.WorldRecordUpdate:
					// World Records should be shared to all clients
					recordIndex = reader.ReadInt32();
					WorldAssist.worldRecords[recordIndex].stats.NetRecieve(reader);
					break;
				case PacketMessageType.PlayTimeRecordUpdate:
					recordIndex = reader.ReadInt32();
					long playTime = reader.ReadInt64();
					ServerCollectedRecords[whoAmI][recordIndex].stats.playTimeFirst = playTime;
					break;
				case PacketMessageType.ResetTrackers:
					recordIndex = reader.ReadInt32();
					int plrIndex = reader.ReadInt32();
					modPlayer = Main.player[plrIndex].GetModPlayer<PlayerAssist>();
					modPlayer.RecordsForWorld[recordIndex].stats.StartTracking();
					break;
				default:
					Logger.Error($"Unknown Message type: {msgType}");
					break;
			}
		}
	}
}
