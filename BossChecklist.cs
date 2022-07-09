using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	internal class BossChecklist : Mod
	{
		internal static BossChecklist instance;
		internal static BossTracker bossTracker;
		internal static ModKeybind ToggleChecklistHotKey;
		public static ModKeybind ToggleBossLog;

		public static Dictionary<int, int> itemToMusicReference;

		// Mods that have been added manually
		internal bool vanillaLoaded = true;
		//internal bool thoriumLoaded;

		// Mods that have been added natively, no longer need code here.
		internal static bool tremorLoaded;
		//internal bool bluemagicLoaded;
		//internal bool joostLoaded;
		//internal bool calamityLoaded;
		//internal bool pumpkingLoaded;

		internal static ClientConfiguration ClientConfig;
		internal static DebugConfiguration DebugConfig;
		internal static BossLogConfiguration BossLogConfig;
		public static List<BossRecord>[] ServerCollectedRecords;

		public BossChecklist() {
		}

		public override void Load() {
			instance = this;
			ToggleChecklistHotKey = KeybindLoader.RegisterKeybind(this, "Toggle Boss Checklist", "P");
			ToggleBossLog = KeybindLoader.RegisterKeybind(this, "Toggle Boss Log", "L");

			tremorLoaded = ModLoader.TryGetMod("Tremor", out Mod mod);

			FieldInfo itemToMusicField = typeof(MusicLoader).GetField("itemToMusic", BindingFlags.Static | BindingFlags.NonPublic);
			itemToMusicReference = (Dictionary<int, int>)itemToMusicField.GetValue(null);

			bossTracker = new BossTracker();

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

			if(!DebugConfig.ModCallLogVerbose)
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

		public override void AddRecipes() {
			//bossTracker.FinalizeLocalization();
			bossTracker.FinalizeOrphanData(); // Add any remaining boss data, including added NPCs, loot, collectibles and spawn items.
			bossTracker.FinalizeBossLootTables(); // Generate boss loot data. Treasurebag is also determined in this.
			bossTracker.FinalizeCollectionTypes(); // Collectible types have to be determined AFTER all items in orphan data has been added.
			bossTracker.FinalizeBossData(); // Finalize all boss data. Entries cannot be further edited beyond this point.
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

					if (!bossTracker.BossesFinalized) {
						Logger.Warn($"Call Warning: The attempted message, \"{message}\", was sent too early. Expect the Call message to return incomplete data. For best results, call in PostAddRecipes.");
					}
					//if (message == "GetBossInfoExpando") {
					//	return bossTracker.SortedBosses.ToDictionary(boss => boss.Key, boss => boss.ConvertToExpandoObject());
					//}
					if (message == "GetBossInfoDictionary") {
						return bossTracker.SortedBosses.ToDictionary(boss => boss.Key, boss => boss.ConvertToDictionary(apiVersion));
					}
					return "Failure";
				}
				if (bossTracker.BossesFinalized)
					throw new Exception($"Call Error: The attempted message, \"{message}\", was sent too late. BossChecklist expects Call messages up until before AddRecipes.");
				if (message == "AddBoss" || message == "AddBossWithInfo") { // For compatability reasons
					if (argsLength < 7) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
						bossTracker.AddBoss(
							args[1] as string, // Boss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
					}
					else if (args[1] as Mod == null) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[4] as string);
						bossTracker.AddBoss(
							args[3] as Mod, // Mod
							args[4] as string, // Boss Name
							InterpretObjectAsListOfInt(args[2]), // IDs
							Convert.ToSingle(args[1]), // Prog
							args[5] as Func<bool>, // Downed
							args[13] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							args[9] as string // Spawn Info
						);
					}
					else {
						bossTracker.AddBoss(
							args[1] as Mod, // Mod
							args[2] as string, // Boss Name
							InterpretObjectAsListOfInt(args[3]), // IDs
							Convert.ToSingle(args[4]), // Prog
							args[5] as Func<bool>, // Downed
							args[6] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Spawn Items
							args[9] as string, // Spawn Info
							InterpretObjectAsStringFunction(args[10]), // Despawn message
							args[11] as Action<SpriteBatch, Rectangle, Color>, // Custom Drawing
							InterpretObjectAsListOfStrings(args[12])
						);
					}
					return "Success";
				}
				else if (message == "AddMiniBoss" || message == "AddMiniBossWithInfo") {
					if (argsLength < 7) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
						bossTracker.AddMiniBoss(
							args[1] as string, // MiniBoss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
					}
					else if (args[1] as Mod == null) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[4] as string);
						bossTracker.AddMiniBoss(
							args[3] as Mod, // Mod
							args[4] as string, // Boss Name
							InterpretObjectAsListOfInt(args[2]), // IDs
							Convert.ToSingle(args[1]), // Prog
							args[5] as Func<bool>, // Downed
							args[13] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							args[9] as string // Spawn Info
						);
					}
					else {
						bossTracker.AddMiniBoss(
							args[1] as Mod, // Mod
							args[2] as string, // Boss Name
							InterpretObjectAsListOfInt(args[3]), // IDs
							Convert.ToSingle(args[4]), // Prog
							args[5] as Func<bool>, // Downed
							args[6] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Spawn Items
							args[9] as string, // Spawn Info
							InterpretObjectAsStringFunction(args[10]), // Despawn message
							args[11] as Action<SpriteBatch, Rectangle, Color>, // Custom Drawing
							InterpretObjectAsListOfStrings(args[12])
						);
					}
					return "Success";
				}
				else if (message == "AddEvent" || message == "AddEventWithInfo") {
					if (argsLength < 7) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
						bossTracker.AddEvent(
							args[1] as string, // Event Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
					}
					else if (args[1] as Mod == null) {
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[4] as string);
						bossTracker.AddEvent(
							args[3] as Mod, // Mod
							args[4] as string, // Boss Name
							InterpretObjectAsListOfInt(args[2]), // IDs
							Convert.ToSingle(args[1]), // Prog
							args[5] as Func<bool>, // Downed
							args[13] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							args[9] as string // Spawn Info
						);
					}
					else {
						bossTracker.AddEvent(
							args[1] as Mod, // Mod
							args[2] as string, // Boss Name
							InterpretObjectAsListOfInt(args[3]), // IDs
							Convert.ToSingle(args[4]), // Prog
							args[5] as Func<bool>, // Downed
							args[6] as Func<bool>, // Available
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Spawn Items
							args[9] as string, // Spawn Info
							args[10] as Action<SpriteBatch, Rectangle, Color>, // Custom Drawing
							InterpretObjectAsListOfStrings(args[11])
						);
					}
					return "Success";
				}
				// TODO
				//else if (message == "GetCurrentBossStates")
				//{
				//	// Returns List<Tuple<string, float, int, bool>>: Name, value, bosstype(boss, miniboss, event), downed.
				//	return bossTracker.allBosses.Select(x => new Tuple<string, float, int, bool>(x.name, x.progression, (int)x.type, x.downed())).ToList();
				//}
				else if (message == "AddToBossLoot" || message == "AddToBossCollection" || message == "AddToBossSpawnItems" || message == "AddToEventNPCs") {
					bossTracker.AddOrphanData(
						message, // OrphanType
						args[1] as string, // Boss Key (obtainable via the BossLog, when display config is enabled)
						InterpretObjectAsListOfInt(args[2]) // ID List
					);
					return "Success";
				}
				else {
					Logger.Error($"Call Error: Unknown Message: {message}");
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
						WorldAssist.HiddenBosses.Add(bossKey);
					else
						WorldAssist.HiddenBosses.Remove(bossKey);
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
					WorldAssist.HiddenBosses.Clear();
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.WorldData);
					//else
					//	ErrorLogger.Log("BossChecklist: Why is RequestHideBoss on Client/SP?");
					break;
				case PacketMessageType.SendRecordsToServer:
					// When sending records to the server, it should always be sent from a player client, meaning whoAmI can be used to determine the player
					// If total count is equal to 1, a record update must be taking place
					// If total count is greater than 1, the packet must have been sent from a newly connected player
					int totalCount = reader.ReadInt32();
					if (totalCount > 1) {
						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine($"Attempting to receive personal records from the connected player '{Main.player[whoAmI].name}'...");
					}

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

					if (totalCount > 1) {
						if (invalidConflicts > 0) {
							Console.ForegroundColor = ConsoleColor.DarkRed;
							Console.WriteLine($"Personal records for player '{Main.player[whoAmI].name}' has been retrieved with {invalidConflicts} conflicts");
						}
						else {
							Console.ForegroundColor = ConsoleColor.Green;
							Console.WriteLine($"Personal records for player '{Main.player[whoAmI].name}' has successfully been retrieved!");
						}
						Console.ResetColor();
					}
					break;
				case PacketMessageType.RecordUpdate:
					// Server just sent us information about what boss just got killed and its records should be updated
					// Grab the player's records and update them through the reader (player and npcPos needed for new record)
					// Since the packet is being sent with 'toClient: i', LocalPlayer can be used here
					int npcPos = reader.ReadInt32();
					modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					BossRecord record = modPlayer.RecordsForWorld[npcPos];
					record.stats.NetRecieve(reader, Main.LocalPlayer, npcPos);

					Console.ForegroundColor = ConsoleColor.DarkCyan;
					Console.WriteLine($"Updated a boss record for player '{Main.LocalPlayer.name}'");
					Console.ResetColor();

					// Whenever records are updated, the server will need to have this updated information as well
					ModPacket packet = GetPacket();
					packet.Write((byte)PacketMessageType.SendRecordsToServer);
					packet.Write(1); // Since RecordUpdate only updates one record at a time, the count is set to 1
					packet.Write(record.bossKey);
					packet.Write(record.stats.durationPrev);
					packet.Write(record.stats.durationBest);
					packet.Write(record.stats.hitsTakenPrev);
					packet.Write(record.stats.hitsTakenBest);
					packet.Send(); // Multiplayer client --> Server
					break;
				case PacketMessageType.WorldRecordUpdate:
					// World Record updates are sent from the server to the server so its data can be sent to all players
					// Grab the world records of the selected boss and update them through the reader
					npcPos = reader.ReadInt32();
					WorldStats worldRecords = WorldAssist.worldRecords[npcPos].stats;
					worldRecords.NetRecieve(reader);

					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine($"World records have been updated!");
					Console.ResetColor();
					break;
				default:
					Logger.Error($"Unknown Message type: {msgType}");
					break;
			}
		}
	}
}
