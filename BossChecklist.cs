using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
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
		internal UserInterface BossLogInterface;
		internal BossLogUI BossLog;
		//Zoom level, (for UIs)
		public static Vector2 ZoomFactor; //0f == fully zoomed out, 1f == fully zoomed in

		internal static UserInterface BossRadarUIInterface;
		internal static BossRadarUI BossRadarUI;

		public BossChecklist() {
		}

		public override void Load() {
			instance = this;
			ToggleChecklistHotKey = RegisterHotKey("Toggle Boss Checklist", "P");
			ToggleBossLog = RegisterHotKey("Toggle Boss Log", "L");

			tremorLoaded = ModLoader.GetMod("Tremor") != null;

			bossTracker = new BossTracker();

			MapAssist.FullMapInitialize();

			if (!Main.dedServ) {
				bossChecklistUI = new BossChecklistUI();
				bossChecklistUI.Activate();
				bossChecklistInterface = new UserInterface();

				UICheckbox.checkboxTexture = GetTexture("UIElements/checkBox");
				UICheckbox.checkmarkTexture = GetTexture("UIElements/checkMark");
			}

			if (!Main.dedServ) {
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
		}

		public override void Unload() {
			instance = null;
			ToggleChecklistHotKey = null;
			bossChecklistInterface = null;
			bossTracker = null;
			ToggleBossLog = null;
			ServerCollectedRecords = null;
			BossRadarUI.arrowTexture = null;

			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

		public override void UpdateUI(GameTime gameTime) {
			bossChecklistInterface?.Update(gameTime);

			if (BossLogInterface != null) BossLogInterface.Update(gameTime);
			BossRadarUI.Update(gameTime);
		}

		public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) {
			//this is needed for Boss Radar, so it takes the range at which to draw the icon properly
			ZoomFactor = Transform.Zoom - (Vector2.UnitX + Vector2.UnitY);
		}

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
			int InventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Death Text"));
			if (InventoryIndex != -1) {
				layers.Insert(InventoryIndex, new LegacyGameInterfaceLayer("BossChecklist: Respawn Timer",
					delegate {
						if (Main.LocalPlayer.dead && Main.LocalPlayer.difficulty != 2) {
							if (Main.LocalPlayer.respawnTimer % 60 == 0 && Main.LocalPlayer.respawnTimer / 60 <= 3) Main.PlaySound(25);
							string timer = (Main.LocalPlayer.respawnTimer / 60 + 1).ToString();
							DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontDeathText, timer, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 - 75), new Color(1f, 0.388f, 0.278f), 0f, default(Vector2), 1, SpriteEffects.None, 0f);
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#region DEBUG
			int PlayerChatIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Player Chat"));
			if (PlayerChatIndex != -1) {
				layers.Insert(PlayerChatIndex, new LegacyGameInterfaceLayer("BossChecklist: Debug Timers and Counters",
					delegate {
						if (Main.LocalPlayer.difficulty != 2) {
							string calc = "";
							List<int> list;
							PlayerAssist playerAssist = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
							if (DebugConfig.ShowTimerOrCounter == "RecordTimers") list = playerAssist.RecordTimers;
							else if (DebugConfig.ShowTimerOrCounter == "BrinkChecker") list = playerAssist.BrinkChecker;
							else if (DebugConfig.ShowTimerOrCounter == "MaxHealth") list = playerAssist.MaxHealth;
							else if (DebugConfig.ShowTimerOrCounter == "DeathTracker") list = playerAssist.DeathTracker;
							else if (DebugConfig.ShowTimerOrCounter == "DodgeTimer") list = playerAssist.DodgeTimer;
							else if (DebugConfig.ShowTimerOrCounter == "AttackCounter") list = playerAssist.AttackCounter;
							if (DebugConfig.ShowTimerOrCounter != "None") {
								foreach (int timer in playerAssist.RecordTimers) {
									calc += timer + ", ";
								}
								DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, DebugConfig.ShowTimerOrCounter, new Vector2(20, Main.screenHeight - 50), new Color(1f, 0.388f, 0.278f), 0f, default(Vector2), 1, SpriteEffects.None, 0f);
								DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, calc, new Vector2(20, Main.screenHeight - 25), new Color(1f, 0.388f, 0.278f), 0f, default(Vector2), 1, SpriteEffects.None, 0f);
							}
						}
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
			#endregion
		}

		public override void PostDrawFullscreenMap(ref string mouseText) {
			MapAssist.DrawFullscreenMap();
			if (MapAssist.shouldDraw) {
				MapAssist.DrawNearestEvil(MapAssist.tilePos);
			}
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
				Logger.Error("BossChecklist PostSetupContent Error: " + e.StackTrace + e.Message);
			}
		}

		public override void AddRecipes() {
			foreach (OrphanInfo orphan in bossTracker.ExtraData) {
				int index = bossTracker.SortedBosses.FindIndex(boss => boss.modSource == orphan.modSource && boss.name == orphan.name);
				if (index != -1) {
					switch (orphan.type) {
						case OrphanType.Loot:
							foreach (int item in orphan.values) {
								bossTracker.SortedBosses[index].loot.Add(item);
							}
							break;
						case OrphanType.Collection:
							foreach (int item in orphan.values) {
								bossTracker.SortedBosses[index].collection.Add(item);
							}
							break;
						case OrphanType.SpawnItem:
							foreach (int item in orphan.values) {
								bossTracker.SortedBosses[index].spawnItem.Add(item);
							}
							break;
						case OrphanType.EventNPC:
							if (bossTracker.SortedBosses[index].type == BossChecklistType.Event) {
								foreach (int npcid in orphan.values) {
									bossTracker.SortedBosses[index].npcIDs.Add(npcid);
								}
							}
							break;
					}
				}
				else {
					Logger.Error("BossChecklist Call Error: Could not find " + orphan.name + " from " + orphan.modSource + " to add OrphanInfo to.");
				}
			}
			bossTracker.FinalizeBossData();
		}

		// Messages:
		// string:"AddBoss" - string:Bossname - float:bossvalue - Func<bool>:BossDowned
		// 0.2: added 6th parameter to AddBossWithInfo/AddMiniBossWithInfo/AddEventWithInfo: Func<bool> available
		// Merge Notes: AddStatPage added, new AddBoss needed.
		public override object Call(params object[] args) {
			// Logs messages when a mod is not using an updated call for the boss log, urging them to update.
			try {
				string message = args[0] as string;
				if (bossTracker.BossesFinalized)
					throw new Exception($"BossChecklist Call Error: The attempted message, \"{message}\", was sent too late. BossChecklist expects Call messages un until before AddRecipes.");
				if (message == "AddBoss" || message == "AddBossWithInfo") { // For compatability reasons
					if (args.Length < 7) {
						bossTracker.AddBoss(
							args[1] as string, // Boss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args.Length > 4 ? args[4] as string : null, // Info
							args.Length > 5 ? args[5] as Func<bool> : null // Available
						);
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddBoss(
							Convert.ToSingle(args[1]), // Prog
							args[2] as List<int>, // IDs
							args[3] as string, // Mod Name
							args[4] as string, // Boss Name
							args[5] as Func<bool>, // Downed
							args[6] as List<int>, // Spawn Items
							args[7] as List<int>, // Collection
							args[8] as List<int>, // Loot
							args.Length > 9 ? args[9] as string : null, // Texture
							args.Length > 10 ? args[10] as string : "No info provided", // Info
							args.Length > 11 ? args[11] as Func<bool> : null, // Available
							args.Length > 12 ? args[12] as string : "" // Override Icon Texture
						);
					}
					return "Success";
				}
				else if (message == "AddMiniBoss" || message == "AddMiniBossWithInfo") {
					if (args.Length < 7) {
						bossTracker.AddMiniBoss(
							args[1] as string, // MiniBoss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args.Length > 4 ? args[4] as string : null, // Info
							args.Length > 5 ? args[5] as Func<bool> : null // Available
						);
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddMiniBoss(
							Convert.ToSingle(args[1]), // Prog
							args[2] as List<int>, // IDs
							args[3] as string, // Mod Name
							args[4] as string, // MiniBoss Name
							args[5] as Func<bool>, // Downed
							args[6] as List<int>, // Spawn Items
							args[7] as List<int>, // Collection
							args[8] as List<int>, // Loot
							args.Length > 9 ? args[9] as string : null, // Texture
							args.Length > 10 ? args[10] as string : "No info provided", // Info
							args.Length > 11 ? args[11] as Func<bool> : null, // Available
							args.Length > 12 ? args[12] as string : "" // Override Icon Texture
						);
					}
					return "Success";
				}
				else if (message == "AddEvent" || message == "AddEventWithInfo") {
					if (args.Length < 7) {
						bossTracker.AddEvent(
							args[1] as string, // Event Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args.Length > 4 ? args[4] as string : null, // Info
							args.Length > 5 ? args[5] as Func<bool> : null // Available
						);
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddEvent(
							Convert.ToSingle(args[1]), // Prog
							args[2] as List<int>, // IDs
							args[3] as string, // Mod Name
							args[4] as string, // Event Name
							args[5] as Func<bool>, // Downed
							args[6] as List<int>, // Spawn Items
							args[7] as List<int>, // Collection
							args[8] as List<int>, // Loot
							args.Length > 9 ? args[9] as string : null, // Texture
							args.Length > 10 ? args[10] as string : "No info provided", // Info
							args.Length > 11 ? args[11] as Func<bool> : null, // Available
							args.Length > 12 ? args[12] as string : "" // Override Icon Texture
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
				else if (message == "AddDespawnMessage") {
					WorldAssist.ModBossTypes.Add(Convert.ToInt32(args[1]));
					WorldAssist.ModBossMessages.Add(args[2] as string);
					return "Success";
				}
				else if (message == "AddToBossLoot") {
					bossTracker.AddToBossCollection(
						args[1].ToString(), // Mod Name
						args[2].ToString(), // Boss Name
						args[3] as List<int> // Loot Items List
					);
				}
				else if (message == "AddToBossCollection") {
					bossTracker.AddToBossCollection(
						args[1].ToString(), // Mod Name
						args[2].ToString(), // Boss Name
						args[3] as List<int> // Collection Items List
					);
				}
				else if (message == "AddToBossSpawnItems") {
					bossTracker.AddToBossSpawnItems(
						args[1].ToString(), // Mod Name
						args[2].ToString(), // Boss Name
						args[3] as List<int> // Spawn Items List
					);
				}
				else {
					Logger.Error("BossChecklist Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e) {
				Logger.Error("BossChecklist Call Error: " + e.StackTrace + e.Message);
			}
			return "Failure";
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			BossChecklistMessageType msgType = (BossChecklistMessageType)reader.ReadByte();
			Player player;
			PlayerAssist modPlayer;
			switch (msgType) {
				// Sent from Client to Server
				case BossChecklistMessageType.RequestHideBoss:
					//if (Main.netMode == NetmodeID.MultiplayerClient)
					//{
					//	Main.NewText("Huh? RequestHideBoss on client?");
					//}
					string bossName = reader.ReadString();
					bool hide = reader.ReadBoolean();
					if (hide)
						BossChecklistWorld.HiddenBosses.Add(bossName);
					else
						BossChecklistWorld.HiddenBosses.Remove(bossName);
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.WorldData);
					//else
					//	ErrorLogger.Log("BossChecklist: Why is RequestHideBoss on Client/SP?");
					break;
				case BossChecklistMessageType.RequestClearHidden:
					//if (Main.netMode == NetmodeID.MultiplayerClient)
					//{
					//	Main.NewText("Huh? RequestClearHidden on client?");
					//}
					BossChecklistWorld.HiddenBosses.Clear();
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.WorldData);
					//else
					//	ErrorLogger.Log("BossChecklist: Why is RequestHideBoss on Client/SP?");
					break;
				case BossChecklistMessageType.SendRecordsToServer:
					player = Main.player[whoAmI];
					Console.WriteLine("Receiving boss records from the joined player + " + player.name + "!");
					for (int i = 0; i < bossTracker.SortedBosses.Count; i++) {
						BossStats bossStats = ServerCollectedRecords[whoAmI][i];
						bossStats.kills = reader.ReadInt32();
						bossStats.deaths = reader.ReadInt32();
						bossStats.durationBest = reader.ReadInt32();
						bossStats.durationWorst = reader.ReadInt32();
						bossStats.healthLossBest = reader.ReadInt32();
						bossStats.healthLossWorst = reader.ReadInt32();
						bossStats.hitsTakenBest = reader.ReadInt32();
						bossStats.hitsTakenWorst = reader.ReadInt32();
						bossStats.dodgeTimeBest = reader.ReadInt32();

						Console.WriteLine("Establishing " + player.name + "'s records for " + bossTracker.SortedBosses[i].name + " to the server");
					}
					break;
				case BossChecklistMessageType.RecordUpdate:
					player = Main.LocalPlayer;
					modPlayer = player.GetModPlayer<PlayerAssist>();
					//Server just sent us information about what boss just got killed and its records shall be updated
					//Since we did packet.Send(toClient: i);, you can use LocalPlayer here
					int npcPos = reader.ReadInt32();

					BossStats specificRecord = modPlayer.AllBossRecords[npcPos].stat;
					specificRecord.NetRecieve(reader);

					//Ill need to update the serverrecords too so they can be used later

					//Main.NewText(ServerCollectedRecords[Main.myPlayer][0].kills + " / " + ServerCollectedRecords[Main.myPlayer][0].deaths);
					//Main.NewText(ServerCollectedRecords[Main.myPlayer][0].fightTime.ToString());
					//Main.NewText(ServerCollectedRecords[Main.myPlayer][0].brink + "(" + ServerCollectedRecords[Main.myPlayer][0].brinkPercent + ")");
					//Main.NewText(ServerCollectedRecords[Main.myPlayer][0].totalDodges + "(" + ServerCollectedRecords[Main.myPlayer][0].dodgeTime + ")");

					// ORDER MATTERS FOR reader
					break;
				default:
					Logger.Error("BossChecklist: Unknown Message type: " + msgType);
					break;
			}
		}
	}
}
