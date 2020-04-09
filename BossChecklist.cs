using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist
{
	internal class BossChecklist : Mod
	{
		internal static BossChecklist instance;
		internal static BossTracker bossTracker;
		internal static ModHotKey ToggleChecklistHotKey;
		public static ModHotKey ToggleBossLog;

		internal static UserInterface bossChecklistInterface;
		internal BossChecklistUI bossChecklistUI;
		internal UserInterface BossLogInterface;
		internal BossLogUI BossLog;
		internal static UserInterface BossRadarUIInterface;
		internal BossRadarUI BossRadarUI;

		public readonly static List<int> vanillaMusicBoxTypes = new List<int>() {
			ItemID.MusicBoxOverworldDay,
			ItemID.MusicBoxAltOverworldDay,
			ItemID.MusicBoxNight,
			ItemID.MusicBoxRain,
			ItemID.MusicBoxSnow,
			ItemID.MusicBoxIce,
			ItemID.MusicBoxDesert,
			ItemID.MusicBoxOcean,
			ItemID.MusicBoxSpace,
			ItemID.MusicBoxUnderground,
			ItemID.MusicBoxAltUnderground,
			ItemID.MusicBoxMushrooms,
			ItemID.MusicBoxJungle,
			ItemID.MusicBoxCorruption,
			ItemID.MusicBoxUndergroundCorruption,
			ItemID.MusicBoxCrimson,
			ItemID.MusicBoxUndergroundCrimson,
			ItemID.MusicBoxTheHallow,
			ItemID.MusicBoxUndergroundHallow,
			ItemID.MusicBoxHell,
			ItemID.MusicBoxDungeon,
			ItemID.MusicBoxTemple,
			ItemID.MusicBoxBoss1,
			ItemID.MusicBoxBoss2,
			ItemID.MusicBoxBoss3,
			ItemID.MusicBoxBoss4,
			ItemID.MusicBoxBoss5,
			ItemID.MusicBoxPlantera,
			ItemID.MusicBoxEerie,
			ItemID.MusicBoxEclipse,
			ItemID.MusicBoxGoblins,
			ItemID.MusicBoxPirates,
			ItemID.MusicBoxMartians,
			ItemID.MusicBoxPumpkinMoon,
			ItemID.MusicBoxFrostMoon,
			ItemID.MusicBoxTowers,
			ItemID.MusicBoxLunarBoss,
			ItemID.MusicBoxSandstorm,
			ItemID.MusicBoxDD2,
			ItemID.MusicBoxTitle
		};
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
		public static List<BossStats>[] ServerCollectedRecords;
		//Zoom level, (for UIs)
		public static Vector2 ZoomFactor; //0f == fully zoomed out, 1f == fully zoomed in

		public BossChecklist() {
		}

		public override void Load() {
			instance = this;
			ToggleChecklistHotKey = RegisterHotKey("Toggle Boss Checklist", "P");
			ToggleBossLog = RegisterHotKey("Toggle Boss Log", "L");

			tremorLoaded = ModLoader.GetMod("Tremor") != null;

			FieldInfo itemToMusicField = typeof(SoundLoader).GetField("itemToMusic", BindingFlags.Static | BindingFlags.NonPublic);
			itemToMusicReference = (Dictionary<int, int>)itemToMusicField.GetValue(null);

			bossTracker = new BossTracker();

			MapAssist.FullMapInitialize();

			if (!Main.dedServ) {
				bossChecklistUI = new BossChecklistUI();
				bossChecklistUI.Activate();
				bossChecklistInterface = new UserInterface();

				UICheckbox.checkboxTexture = GetTexture("UIElements/checkBox");
				UICheckbox.checkmarkTexture = GetTexture("UIElements/checkMark");

				BossLog = new BossLogUI();
				BossLog.Activate();
				BossLogInterface = new UserInterface();
				BossLogInterface.SetState(BossLog);

				//important, after setup has been initialized
				BossRadarUI = new BossRadarUI();
				BossRadarUI.Activate();
				BossRadarUIInterface = new UserInterface();
				BossRadarUIInterface.SetState(BossRadarUI);
			}

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
			bossChecklistInterface = null;
			bossTracker = null;
			ToggleBossLog = null;
			ServerCollectedRecords = null;
			BossRadarUIInterface = null;
			BossRadarUI.arrowTexture = null;
			ClientConfig = null;
			DebugConfig = null;
			BossLogConfig = null;
			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

		public override void UpdateUI(GameTime gameTime) {
			bossChecklistInterface?.Update(gameTime);
			BossLogInterface?.Update(gameTime);
			BossRadarUI?.Update(gameTime);
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

		public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) {
			//this is needed for Boss Radar, so it takes the range at which to draw the icon properly
			ZoomFactor = Transform.Zoom - (Vector2.UnitX + Vector2.UnitY);
		}

		public override void PostDrawFullscreenMap(ref string mouseText) {
			MapAssist.DrawFullscreenMap();
		}

		private string[] LayersToHideWhenChecklistVisible = new string[] {
			"Vanilla: Map / Minimap", "Vanilla: Resource Bars"
		};

		//int lastSeenScreenWidth;
		//int lastSeenScreenHeight;
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			//if (BossChecklistUI.visible)
			//{
			//	layers.RemoveAll(x => x.Name == "Vanilla: Resource Bars" || x.Name == "Vanilla: Map / Minimap");
			//}

			int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (MouseTextIndex != -1) {
				layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
					"BossChecklist: Boss Checklist",
					delegate {
						if (BossChecklistUI.Visible) {
							bossChecklistInterface?.Draw(Main.spriteBatch, new GameTime());

							if (BossChecklistUI.hoverText != "") {
								float x = Main.fontMouseText.MeasureString(BossChecklistUI.hoverText).X;
								Vector2 vector = new Vector2((float)Main.mouseX, (float)Main.mouseY) + new Vector2(16f, 16f);
								if (vector.Y > (float)(Main.screenHeight - 30)) {
									vector.Y = (float)(Main.screenHeight - 30);
								}
								if (vector.X > (float)(Main.screenWidth - x - 30)) {
									vector.X = (float)(Main.screenWidth - x - 30);
								}
								//Utils.DrawBorderStringFourWay(Main.spriteBatch, Main.fontMouseText, BossChecklistUI.hoverText,
								//	vector.X, vector.Y, new Color((int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor, (int)Main.mouseTextColor), Color.Black, Vector2.Zero, 1f);
								//	Utils.draw

								//ItemTagHandler.GenerateTag(item)
								int hoveredSnippet = -1;
								TextSnippet[] array = ChatManager.ParseMessage(BossChecklistUI.hoverText, Color.White).ToArray();
								ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, Main.fontMouseText, array,
									vector, 0f, Vector2.Zero, Vector2.One, out hoveredSnippet/*, -1f, 2f*/);

								if (hoveredSnippet > -1) {
									array[hoveredSnippet].OnHover();
									//if (Main.mouseLeft && Main.mouseLeftRelease)
									//{
									//	array[hoveredSnippet].OnClick();
									//}
								}
							}
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			// This doesn't work perfectly.
			//if (BossChecklistUI.Visible) {
			//	layers.RemoveAll(x => LayersToHideWhenChecklistVisible.Contains(x.Name));
			//}
			if (MouseTextIndex != -1) {
				layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Log",
					delegate {
						BossLogInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
				layers.Insert(++MouseTextIndex, new LegacyGameInterfaceLayer("BossChecklist: Boss Radar",
					delegate {
						BossRadarUIInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			if (ClientConfig.RespawnTimerEnabled) {
				int InventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Death Text"));
				if (InventoryIndex != -1) {
					layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer("BossChecklist: Respawn Timer",
						delegate {
							if (Main.LocalPlayer.dead && Main.LocalPlayer.difficulty != 2) {
								if (ClientConfig.TimerSounds) {
									if (Main.LocalPlayer.respawnTimer % 60 == 0 && Main.LocalPlayer.respawnTimer / 60 <= 3) Main.PlaySound(25);
								}
								string timer = (Main.LocalPlayer.respawnTimer / 60 + 1).ToString();
								Vector2 screenPos = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 - 75);
								Color deathColor = Main.player[Main.myPlayer].GetDeathAlpha(Color.Transparent);
								DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontDeathText, timer, screenPos, deathColor);
							}
							return true;
						},
						InterfaceScaleType.UI)
					);
				}
			}
			#region DEBUG
			int PlayerChatIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Player Chat"));
			if (PlayerChatIndex != -1) {
				layers.Insert(PlayerChatIndex, new LegacyGameInterfaceLayer("BossChecklist: Debug Timers and Counters",
					delegate {
						PlayerAssist playerAssist = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						int ConfigIndex = NPCAssist.ListedBossNum(DebugConfig.ShowTimerOrCounter.Type, DebugConfig.ShowTimerOrCounter.mod);
						if (ConfigIndex != -1) {
							string textKingSlime = $"{bossTracker.SortedBosses[ConfigIndex].name} (#{ConfigIndex + 1})" +
												$"\nTime: {playerAssist.RecordTimers[ConfigIndex]}" +
												$"\nDodge Timer: {playerAssist.DodgeTimer[ConfigIndex]}" +
												$"\nTimes Hit: {playerAssist.AttackCounter[ConfigIndex]}" +
												$"\nLowest Health: {playerAssist.BrinkChecker[ConfigIndex]} / {playerAssist.MaxHealth[ConfigIndex]}" +
												$"\nDeaths: {playerAssist.DeathTracker[ConfigIndex]}";
							DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, textKingSlime, new Vector2(20, Main.screenHeight - 175), new Color(1f, 0.388f, 0.278f), 0f, default(Vector2), 1, SpriteEffects.None, 0f);
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#endregion
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
				Logger.Error("PostSetupContent Error: " + e.StackTrace + e.Message);
			}
		}

		public override void AddRecipes() {
			bossTracker.FinalizeLocalization();
			foreach (OrphanInfo orphan in bossTracker.ExtraData) {
				BossInfo bossInfo = bossTracker.SortedBosses.Find(boss => boss.Key == orphan.Key);
				if (bossInfo != null && orphan.values != null) {
					switch (orphan.type) {
						case OrphanType.Loot:
							bossInfo.loot.AddRange(orphan.values);
							break;
						case OrphanType.Collection:
							bossInfo.collection.AddRange(orphan.values);
							break;
						case OrphanType.SpawnItem:
							bossInfo.spawnItem.AddRange(orphan.values);
							break;
						case OrphanType.EventNPC:
							if (bossInfo.type == EntryType.Event) {
								bossInfo.npcIDs.AddRange(orphan.values);
							}
							break;
					}
				}
				else {
					if(BossChecklist.DebugConfig.ModCallLogVerbose)
						Logger.Info("Could not find " + orphan.internalName + " from " + orphan.modSource + " to add OrphanInfo to.");
				}
			}
			foreach (BossInfo boss in bossTracker.SortedBosses) {
				boss.collectType = BossInfo.SetupCollectionTypes(boss.collection);
			}
			bossTracker.FinalizeBossData();
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
				// TODO if requested: GetBossInfoDirect for returning a clone of BossInfo directly for strong reference. GetBossInfoExpando if convinient. BossInfoAPI public static class for strong dependencies.
				if (message == "GetBossInfoDictionary") {
					var mod = args[1] as Mod;
					var apiVersion = args[2] is string ? new Version(args[2] as string) : Version; // Future-proofing. Allowing new info to be returned while maintaining backwards compat if necessary.

					Logger.Info($"{mod.DisplayName} has registered for GetBossInfoDictionary");

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
						bossTracker.AddBoss(
							args[1] as string, // Boss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
					}
					else {
						bossTracker.AddBoss(
							Convert.ToSingle(args[1]), // Prog
							InterpretObjectAsListOfInt(args[2]), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // Boss Name
							args[5] as Func<bool>, // Downed
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Loot
							args[9] as string, // Info
							args[10] as string, // Despawn Message
							args[11] as string, // Texture
							args[12] as string, // Override Icon Texture
							args[13] as Func<bool> // Available
						);
					}
					return "Success";
				}
				else if (message == "AddMiniBoss" || message == "AddMiniBossWithInfo") {
					if (argsLength < 7) {
						bossTracker.AddMiniBoss(
							args[1] as string, // MiniBoss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
					}
					else {
						bossTracker.AddMiniBoss(
							Convert.ToSingle(args[1]), // Prog
							InterpretObjectAsListOfInt(args[2]), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // MiniBoss Name
							args[5] as Func<bool>, // Downed
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Loot
							args[9] as string, // Info
							args[10] as string, // Despawn Message
							args[11] as string, // Texture
							args[12] as string, // Override Icon Texture
							args[13] as Func<bool> // Available
						);
					}
					return "Success";
				}
				else if (message == "AddEvent" || message == "AddEventWithInfo") {
					if (argsLength < 7) {
						bossTracker.AddEvent(
							args[1] as string, // Event Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
						bossTracker.AnyModHasOldCall = true;
						AddToOldCalls(message, args[1] as string);
					}
					else {
						bossTracker.AddEvent(
							Convert.ToSingle(args[1]), // Prog
							InterpretObjectAsListOfInt(args[2]), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // Event Name
							args[5] as Func<bool>, // Downed
							InterpretObjectAsListOfInt(args[6]), // Spawn Items
							InterpretObjectAsListOfInt(args[7]), // Collection
							InterpretObjectAsListOfInt(args[8]), // Loot
							args[9] as string, // Info
							args[10] as string, // Despawn Message
							args[11] as string, // Texture
							args[12] as string, // Override Icon Texture
							args[13] as Func<bool> // Available
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
						args[1] as string, // Mod Name
						args[2] as string, // Boss Name
						InterpretObjectAsListOfInt(args[3]) // ID List
					);
				}
				else {
					Logger.Error("Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e) {
				Logger.Error("Call Error: " + e.StackTrace + e.Message);
			}
			return "Failure";

			// Local functions.
			List<int> InterpretObjectAsListOfInt(object data) => data is List<int> ? data as List<int> : (data is int ? new List<int>() { Convert.ToInt32(data) } : null);

			void AddToOldCalls(string message, string name) {
				// TODO: maybe spam the log if ModCompile.activelyModding (needs reflection)
				List<string> oldCallsList;
				if (!bossTracker.OldCalls.TryGetValue(message, out oldCallsList))
					bossTracker.OldCalls.Add(message, oldCallsList = new List<string>());
				oldCallsList.Add(name);
			}
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			PacketMessageType msgType = (PacketMessageType)reader.ReadByte();
			Player player;
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
					player = Main.player[whoAmI];
					Console.WriteLine($"Receiving boss records from the joined player {player.name}!");
					for (int i = 0; i < bossTracker.SortedBosses.Count; i++) {
						BossStats bossStats = ServerCollectedRecords[whoAmI][i];
						bossStats.kills = reader.ReadInt32();
						bossStats.deaths = reader.ReadInt32();
						bossStats.durationBest = reader.ReadInt32();
						bossStats.durationPrev = reader.ReadInt32();
						bossStats.healthLossBest = reader.ReadInt32();
						bossStats.healthLossPrev = reader.ReadInt32();
						bossStats.hitsTakenBest = reader.ReadInt32();
						bossStats.hitsTakenPrev = reader.ReadInt32();
						bossStats.dodgeTimeBest = reader.ReadInt32();

						//Console.WriteLine($"Establishing {player.name}'s records for {bossTracker.SortedBosses[i].name} to the server");
					}
					break;
				case PacketMessageType.RecordUpdate:
					player = Main.LocalPlayer;
					modPlayer = player.GetModPlayer<PlayerAssist>();
					//Server just sent us information about what boss just got killed and its records shall be updated
					//Since we did packet.Send(toClient: i);, you can use LocalPlayer here
					int npcPos = reader.ReadInt32();

					BossStats specificRecord = modPlayer.AllBossRecords[npcPos].stat; // Get the Player's records
					specificRecord.NetRecieve(reader, player, npcPos); // The records will be updated through the reader (player and npcPos needed for new record)

					//Update the serverrecords too so they can be used later
					// TODO? send it as a single entry?
					ModPacket packet = GetPacket();
					packet.Write((byte)PacketMessageType.SendRecordsToServer);
					for (int i = 0; i < bossTracker.SortedBosses.Count; i++) {
						BossStats stat = modPlayer.AllBossRecords[i].stat;
						packet.Write(stat.kills);
						packet.Write(stat.deaths);
						packet.Write(stat.durationBest);
						packet.Write(stat.durationPrev);
						packet.Write(stat.hitsTakenBest);
						packet.Write(stat.hitsTakenPrev);
						packet.Write(stat.dodgeTimeBest);
						packet.Write(stat.healthLossBest);
						packet.Write(stat.healthLossPrev);
					}
					packet.Send(); // To server (ORDER MATTERS FOR reader)
					break;
				case (PacketMessageType.WorldRecordUpdate):
					npcPos = reader.ReadInt32();
					WorldStats worldRecords = WorldAssist.worldRecords[npcPos].stat; // Get the Player's records
					worldRecords.NetRecieve(reader); // The records will be updated through the reader (player and npcPos needed for new record)
					break;
				default:
					Logger.Error("Unknown Message type: " + msgType);
					break;
			}
		}
	}
}
