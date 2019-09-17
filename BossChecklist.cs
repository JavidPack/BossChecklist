using BossChecklist.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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

				UICheckbox.checkboxTexture = GetTexture("checkBox");
				UICheckbox.checkmarkTexture = GetTexture("checkMark");
			}

			if (Main.netMode == NetmodeID.Server) {
				ServerCollectedRecords = new List<BossStats>[255];
				for (int i = 0; i < 255; i++) {
					ServerCollectedRecords[i] = new List<BossStats>();
					for (int j = 0; j < bossTracker.SortedBosses.Count; j++) {
						ServerCollectedRecords[i].Add(new BossStats());
					}
				}
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
							foreach (int item in orphan.itemValues) {
								bossTracker.SortedBosses[index].loot.Add(item);
							}
							break;
						case OrphanType.Collection:
							foreach (int item in orphan.itemValues) {
								bossTracker.SortedBosses[index].collection.Add(item);
							}
							break;
						case OrphanType.SpawnItem:
							foreach (int item in orphan.itemValues) {
								bossTracker.SortedBosses[index].spawnItem.Add(item);
							}
							break;
					}
				}
				else {
					Logger.Error("BossChecklist Call Error: Could not find " + orphan.name + " from " + orphan.modSource + " to add OrphanInfo to.");
				}
			}
		}

		// Messages:
		// string:"AddBoss" - string:Bossname - float:bossvalue - Func<bool>:BossDowned
		// 0.2: added 6th parameter to AddBossWithInfo/AddMiniBossWithInfo/AddEventWithInfo: Func<bool> available
		// Merge Notes: AddStatPage added, new AddBoss needed.
		public override object Call(params object[] args) {
			try {
				string message = args[0] as string;
				if (message == "AddBoss") {
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					bossTracker.AddBoss(bossname, bossValue, bossDowned);
					return "Success";
				}
				else if (message == "AddBossWithInfo") {
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					Func<bool> available = args.Length == 6 ? args[5] as Func<bool> : null;
					// possible? var assembly = Assembly.GetCallingAssembly();
					bossTracker.AddBoss(bossname, bossValue, bossDowned, bossInfo, available);
					return "Success";
				}
				else if (message == "AddMiniBossWithInfo") {
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					Func<bool> available = args.Length == 6 ? args[5] as Func<bool> : null;
					bossTracker.AddMiniBoss(bossname, bossValue, bossDowned, bossInfo, available);
					return "Success";
				}
				else if (message == "AddEventWithInfo") {
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					Func<bool> available = args.Length == 6 ? args[5] as Func<bool> : null;
					bossTracker.AddEvent(bossname, bossValue, bossDowned, bossInfo, available);
					return "Success";
				}
				// TODO
				//else if (message == "GetCurrentBossStates")
				//{
				//	// Returns List<Tuple<string, float, int, bool>>: Name, value, bosstype(boss, miniboss, event), downed.
				//	return bossTracker.allBosses.Select(x => new Tuple<string, float, int, bool>(x.name, x.progression, (int)x.type, x.downed())).ToList();
				//}
				else if (message == "AddDespawnMessage") {
					int bossID = Convert.ToInt32(args[1]);
					string bossMessage = args[2] as string;

					WorldAssist.ModBossTypes.Add(bossID);
					WorldAssist.ModBossMessages.Add(bossMessage);
					return "Success";
				}
				else if (message == "AddStatPage") {
					float BossValue = Convert.ToSingle(args[1]);

					List<int> BossID;
					if (args[2] is List<int>) BossID = args[2] as List<int>;
					else BossID = new List<int>() { Convert.ToInt32(args[2]) };

					string ModName = args[3].ToString();
					string BossName = args[4].ToString();
					Func<bool> BossDowned = args[5] as Func<bool>;

					List<int> BossSpawn;
					if (args[6] is List<int>) BossSpawn = args[6] as List<int>;
					else BossSpawn = new List<int>() { Convert.ToInt32(args[6]) };

					List<int> BossCollect = args[7] as List<int>;
					List<int> BossLoot = args[8] as List<int>;
					string BossTexture = "";
					if (args.Length > 9) BossTexture = args[9].ToString();

					bossTracker.AddBoss(BossValue, BossID, ModName, BossName, BossDowned, BossSpawn, BossCollect, BossLoot, BossTexture);
					return "Success";
				}
				else if (message == "AddToBossLoot") {
					string modName = args[1].ToString();
					string bossName = args[2].ToString();
					List<int> newLoot = args[3] as List<int>;

					bossTracker.AddToBossLoot(modName, bossName, newLoot);
				}
				else if (message == "AddToBossCollection") {
					string modName = args[1].ToString();
					string bossName = args[2].ToString();
					List<int> newLoot = args[3] as List<int>;

					bossTracker.AddToBossCollection(modName, bossName, newLoot);
				}
				else if (message == "AddToBossSpawnItems") {
					string modName = args[1].ToString();
					string bossName = args[2].ToString();
					List<int> newLoot = args[3] as List<int>;

					bossTracker.AddToBossSpawnItems(modName, bossName, newLoot);
				}
				/*
                // Will be added in later once some fixes are made and features are introduced

				//
                else if (AddType == "AddLoot")
                {
                    string ModName = args[1].ToString();
                    int BossID = Convert.ToInt32(args[2]);
                    List<int> BossLoot = args[3] as List<int>;
                    // This list is for adding on to existing bosses loot drops
                    setup.AddToLootTable(BossID, ModName, BossLoot);
                }
                else if (AddType == "AddCollectibles")
                {
                    string ModName = args[1].ToString();
                    int BossID = Convert.ToInt32(args[2]);
                    List<int> BossCollect = args[3] as List<int>;
                    // This list is for adding on to existing bosses loot drops
                    setup.AddToCollection(BossID, ModName, BossCollect);
                }
				//
                else
                {

                }
				*/
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

