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
		internal UserInterface BossLogInterface;
		internal BossLogUI BossLog;
		internal static UserInterface BossRadarUIInterface;
		internal BossRadarUI BossRadarUI;

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

			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

		public override void UpdateUI(GameTime gameTime) {
			bossChecklistInterface?.Update(gameTime);
			BossLogInterface?.Update(gameTime);
			BossRadarUI?.Update(gameTime);
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
				Logger.Error("BossChecklist PostSetupContent Error: " + e.StackTrace + e.Message);
			}
		}

		public override void AddRecipes() {
			foreach (OrphanInfo orphan in bossTracker.ExtraData) {
				int index = bossTracker.SortedBosses.FindIndex(boss => boss.Key == orphan.Key);
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
							if (bossTracker.SortedBosses[index].type == EntryType.Event) {
								foreach (int npcid in orphan.values) {
									bossTracker.SortedBosses[index].npcIDs.Add(npcid);
								}
							}
							break;
					}
				}
				else {
					Logger.Error("BossChecklist Call Error: Could not find " + orphan.internalName + " from " + orphan.modSource + " to add OrphanInfo to.");
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
			int argsLength = args.Length; // Simplify code by resizing args.
			Array.Resize(ref args, 15);
			try {
				string message = args[0] as string;
				if (bossTracker.BossesFinalized)
					throw new Exception($"BossChecklist Call Error: The attempted message, \"{message}\", was sent too late. BossChecklist expects Call messages un until before AddRecipes.");
				if (message == "AddBoss" || message == "AddBossWithInfo") { // For compatability reasons
					if (argsLength < 7) {
						bossTracker.AddBoss(
							args[1] as string, // Boss Name
							Convert.ToSingle(args[2]), // Prog
							args[3] as Func<bool>, // Downed
							args[4] as string, // Info
							args[5] as Func<bool> // Available
						);
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddBoss(
							Convert.ToSingle(args[1]), // Prog
							args[2] is List<int> ? args[2] as List<int> : (args[2] is int ? new List<int>() { Convert.ToInt32(args[2]) } : null), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // Boss Name
							args[5] as Func<bool>, // Downed
							args[6] is List<int> ? args[6] as List<int> : (args[6] is int ? new List<int>() { Convert.ToInt32(args[6]) } : null), // Spawn Items
							args[7] is List<int> ? args[7] as List<int> : (args[7] is int ? new List<int>() { Convert.ToInt32(args[7]) } : null), // Collection
							args[8] is List<int> ? args[8] as List<int> : (args[8] is int ? new List<int>() { Convert.ToInt32(args[8]) } : null), // Loot
							args[9] as string, // Texture
							argsLength > 10 ? args[10] as string : "No info provided", // Info
							argsLength > 11 ? args[11] as string : "", // Despawn Message
							args[12] as Func<bool>, // Available
							argsLength > 13 ? args[13] as string : "" // Override Icon Texture
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
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddMiniBoss(
							Convert.ToSingle(args[1]), // Prog
							args[2] is List<int> ? args[2] as List<int> : (args[2] is int ? new List<int>() { Convert.ToInt32(args[2]) } : null), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // MiniBoss Name
							args[5] as Func<bool>, // Downed
							args[6] is List<int> ? args[6] as List<int> : (args[6] is int ? new List<int>() { Convert.ToInt32(args[6]) } : null), // Spawn Items
							args[7] is List<int> ? args[7] as List<int> : (args[7] is int ? new List<int>() { Convert.ToInt32(args[7]) } : null), // Collection
							args[8] is List<int> ? args[8] as List<int> : (args[8] is int ? new List<int>() { Convert.ToInt32(args[8]) } : null), // Loot
							args[9] as string, // Texture
							argsLength > 10 ? args[10] as string : "No info provided", // Info
							argsLength > 11 ? args[11] as string : "", // Despawn Message
							args[12] as Func<bool>, // Available
							argsLength > 13 ? args[13] as string : "" // Override Icon Texture
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
						Logger.Warn(message + " call for " + args[1] as string + " is not utilizing Boss Log features. Update mod call with proper information.");
					}
					else {
						bossTracker.AddEvent(
							Convert.ToSingle(args[1]), // Prog
							args[2] is List<int> ? args[2] as List<int> : (args[2] is int ? new List<int>() { Convert.ToInt32(args[2]) } : null), // IDs
							args[3] as Mod, // Mod
							args[4] as string, // Event Name
							args[5] as Func<bool>, // Downed
							args[6] is List<int> ? args[6] as List<int> : (args[6] is int ? new List<int>() { Convert.ToInt32(args[6]) } : null), // Spawn Items
							args[7] is List<int> ? args[7] as List<int> : (args[7] is int ? new List<int>() { Convert.ToInt32(args[7]) } : null), // Collection
							args[8] is List<int> ? args[8] as List<int> : (args[8] is int ? new List<int>() { Convert.ToInt32(args[8]) } : null), // Loot
							args[9] as string, // Texture
							argsLength > 10 ? args[10] as string : "No info provided", // Info
							argsLength > 11 ? args[11] as string : "", // Despawn Message
							args[12] as Func<bool>, // Available
							argsLength > 13 ? args[13] as string : "" // Override Icon Texture
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
						args[3] as List<int> // ID List
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
						BossChecklistWorld.HiddenBosses.Add(bossKey);
					else
						BossChecklistWorld.HiddenBosses.Remove(bossKey);
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
					BossChecklistWorld.HiddenBosses.Clear();
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
						bossStats.durationWorst = reader.ReadInt32();
						bossStats.healthLossBest = reader.ReadInt32();
						bossStats.healthLossWorst = reader.ReadInt32();
						bossStats.hitsTakenBest = reader.ReadInt32();
						bossStats.hitsTakenWorst = reader.ReadInt32();
						bossStats.dodgeTimeBest = reader.ReadInt32();

						Console.WriteLine($"Establishing {player.name}'s records for {bossTracker.SortedBosses[i].name} to the server");
					}
					Console.WriteLine($"Record data established for player {player.name}.");
					break;
				case PacketMessageType.RecordUpdate:
					player = Main.LocalPlayer;
					modPlayer = player.GetModPlayer<PlayerAssist>();
					//Server just sent us information about what boss just got killed and its records shall be updated
					//Since we did packet.Send(toClient: i);, you can use LocalPlayer here
					int npcPos = reader.ReadInt32();

					BossStats specificRecord = modPlayer.AllBossRecords[npcPos].stat;
					specificRecord.NetRecieve(reader, modPlayer.hasNewRecord);
					if (modPlayer.hasNewRecord) {
						CombatText.NewText(player.getRect(), Color.LightYellow, "New Record!", true);
					}

					//Update the serverrecords too so they can be used later

					ModPacket packet = GetPacket();
					packet.Write((byte)PacketMessageType.SendRecordsToServer);
					for (int i = 0; i < bossTracker.SortedBosses.Count; i++) {
						BossStats stat = modPlayer.AllBossRecords[i].stat;
						packet.Write(stat.kills);
						packet.Write(stat.deaths);
						packet.Write(stat.durationBest);
						packet.Write(stat.durationWorst);
						packet.Write(stat.healthLossBest);
						packet.Write(stat.healthLossWorst);
						packet.Write(stat.hitsTakenBest);
						packet.Write(stat.hitsTakenWorst);
						packet.Write(stat.dodgeTimeBest);
					}
					packet.Send(); // To server

					// ORDER MATTERS FOR reader
					break;
				default:
					Logger.Error("BossChecklist: Unknown Message type: " + msgType);
					break;
			}
		}
	}
}
