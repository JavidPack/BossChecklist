using System;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.UI;
using Terraria.DataStructures;
using BossChecklist.UI;
using Microsoft.Xna.Framework;
using Terraria.UI.Chat;
using System.Linq;
using Terraria.GameContent.UI.Chat;
using System.IO;
using Terraria.ID;

// TODO: Kill all npc checklist
// TODO: Currently have all town npc checklist
namespace BossChecklist
{
	public class BossChecklist : Mod
	{
		static internal BossChecklist instance;
		internal static BossTracker bossTracker;
		internal static ModHotKey ToggleChecklistHotKey;
		internal static UserInterface bossChecklistInterface;
		internal BossChecklistUI bossChecklistUI;

		// Mods that have been added manually
		internal bool vanillaLoaded = true;
		//internal bool thoriumLoaded;

		// Mods with bosses that could use suppory, but need fixes in the tmod files.
		//internal bool sacredToolsLoaded;
		//internal bool crystiliumLoaded;

		// Mods that have been added natively, no longer need code here.
		internal static bool tremorLoaded;
		//internal bool bluemagicLoaded;
		//internal bool joostLoaded;
		//internal bool calamityLoaded;
		//internal bool pumpkingLoaded;

		public BossChecklist()
		{
		}

		public override void Load()
		{
			// Too many people are downloading 0.10 versions on 0.9....
			if (ModLoader.version < new Version(0, 10))
			{
				throw new Exception("\nThis mod uses functionality only present in the latest tModLoader. Please update tModLoader to use this mod\n\n");
			}
			instance = this;
			ToggleChecklistHotKey = RegisterHotKey("Toggle Boss Checklist", "P");

			tremorLoaded = ModLoader.GetMod("Tremor") != null;

			bossTracker = new BossTracker();

			if (!Main.dedServ)
			{
				bossChecklistUI = new BossChecklistUI();
				bossChecklistUI.Activate();
				bossChecklistInterface = new UserInterface();
				bossChecklistInterface.SetState(bossChecklistUI);

				UICheckbox.checkboxTexture = GetTexture("checkBox");
				UICheckbox.checkmarkTexture = GetTexture("checkMark");
			}
		}

		public override void Unload()
		{
			instance = null;
			ToggleChecklistHotKey = null;
			bossChecklistInterface = null;
			bossTracker = null;

			UICheckbox.checkboxTexture = null;
			UICheckbox.checkmarkTexture = null;
		}

		public override void UpdateUI(GameTime gameTime)
		{
			bossChecklistInterface?.Update(gameTime);
		}

		int lastSeenScreenWidth;
		int lastSeenScreenHeight;
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			//if (BossChecklistUI.visible)
			//{
			//	layers.RemoveAll(x => x.Name == "Vanilla: Resource Bars" || x.Name == "Vanilla: Map / Minimap");
			//}

			int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (MouseTextIndex != -1)
			{
				layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
					"BossChecklist: Boss Checklist",
					delegate
					{
						if (BossChecklistUI.visible)
						{
							if (lastSeenScreenWidth != Main.screenWidth || lastSeenScreenHeight != Main.screenHeight)
							{
								bossChecklistInterface.Recalculate();
								lastSeenScreenWidth = Main.screenWidth;
								lastSeenScreenHeight = Main.screenHeight;
							}

							bossChecklistUI.Draw(Main.spriteBatch);

							if (BossChecklistUI.hoverText != "")
							{
								float x = Main.fontMouseText.MeasureString(BossChecklistUI.hoverText).X;
								Vector2 vector = new Vector2((float)Main.mouseX, (float)Main.mouseY) + new Vector2(16f, 16f);
								if (vector.Y > (float)(Main.screenHeight - 30))
								{
									vector.Y = (float)(Main.screenHeight - 30);
								}
								if (vector.X > (float)(Main.screenWidth - x - 30))
								{
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

								if (hoveredSnippet > -1)
								{
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
		}

		// An alternative approach to the weak reference approach is to do the following in YOUR mod in PostSetupContent
		//Mod bossChecklist = ModLoader.GetMod("BossChecklist");
		//if (bossChecklist != null)
		//{
		//	bossChecklist.Call("AddBoss", "My BossesName", 2.3f, (Func<bool>)(() => MyMod.MyModWorld.downedMyBoss));
		//}
		public override void PostSetupContent()
		{
			try
			{
				//thoriumLoaded = ModLoader.GetMod("ThoriumMod") != null;
				//bluemagicLoaded = ModLoader.GetMod("Bluemagic") != null;
				//calamityLoaded = ModLoader.GetMod("CalamityMod") != null;
				//joostLoaded = ModLoader.GetMod("JoostMod") != null;
				//crystiliumLoaded = ModLoader.GetMod("CrystiliumMod") != null;
				//sacredToolsLoaded = ModLoader.GetMod("SacredTools") != null;
				//pumpkingLoaded = ModLoader.GetMod("Pumpking") != null;
			}
			catch (Exception e)
			{
				ErrorLogger.Log("BossChecklist PostSetupContent Error: " + e.StackTrace + e.Message);
			}
		}

		// Messages:
		// string:"AddBoss" - string:Bossname - float:bossvalue - Func<bool>:BossDowned
		public override object Call(params object[] args)
		{
			try
			{
				string message = args[0] as string;
				if (message == "AddBoss")
				{
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					bossTracker.AddBoss(bossname, bossValue, bossDowned);
					return "Success";
				}
				else if (message == "AddBossWithInfo")
				{
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					bossTracker.AddBoss(bossname, bossValue, bossDowned, bossInfo);
					return "Success";
				}
				else if (message == "AddMiniBossWithInfo")
				{
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					bossTracker.AddMiniBoss(bossname, bossValue, bossDowned, bossInfo);
					return "Success";
				}
				else if (message == "AddEventWithInfo")
				{
					string bossname = args[1] as string;
					float bossValue = Convert.ToSingle(args[2]);
					Func<bool> bossDowned = args[3] as Func<bool>;
					string bossInfo = args[4] as string;
					bossTracker.AddEvent(bossname, bossValue, bossDowned, bossInfo);
					return "Success";
				}
				// TODO
				//else if (message == "GetCurrentBossStates")
				//{
				//	// Returns List<Tuple<string, float, int, bool>>: Name, value, bosstype(boss, miniboss, event), downed.
				//	return bossTracker.allBosses.Select(x => new Tuple<string, float, int, bool>(x.name, x.progression, (int)x.type, x.downed())).ToList();
				//}
				else
				{
					ErrorLogger.Log("BossChecklist Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e)
			{
				ErrorLogger.Log("BossChecklist Call Error: " + e.StackTrace + e.Message);
			}
			return "Failure";
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			BossChecklistMessageType msgType = (BossChecklistMessageType)reader.ReadByte();
			switch (msgType)
			{
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
				default:
					ErrorLogger.Log("BossChecklist: Unknown Message type: " + msgType);
					break;
			}
		}
	}

	enum BossChecklistMessageType : byte
	{
		RequestHideBoss,
		RequestClearHidden,
	}
}

