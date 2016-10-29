using System;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.UI;
using Terraria.DataStructures;
using BossChecklist.UI;

namespace BossChecklist
{
	public class BossChecklist : Mod
	{
		static internal BossChecklist instance;
		private UserInterface bossChecklistInterface;
		internal BossChecklistUI bossChecklistUI;
		private double pressedRandomBuffHotKeyTime;

		// Mods that have been added
		internal bool vanillaLoaded = true;
		internal bool thoriumLoaded;
		internal bool bluemagicLoaded;
		internal bool crystiliumLoaded;
		internal bool calamityLoaded;
		internal bool sacredToolsLoaded;
		internal bool tremorLoaded;
		internal bool pumpkingLoaded;

		public BossChecklist()
		{
		}

		public override void Load()
		{
			instance = this;
			RegisterHotKey("Toggle Boss Checklist", "P");
			if (!Main.dedServ)
			{
				bossChecklistUI = new BossChecklistUI();
				bossChecklistUI.Activate();
				bossChecklistInterface = new UserInterface();
				bossChecklistInterface.SetState(bossChecklistUI);
			}
		}


		public override void HotKeyPressed(string name)
		{
			if (name == "Toggle Boss Checklist")
			{
				if (Math.Abs(Main.time - pressedRandomBuffHotKeyTime) > 60)
				{
					pressedRandomBuffHotKeyTime = Main.time;

					if (!BossChecklistUI.visible)
					{
						bossChecklistUI.UpdateCheckboxes();
					}
					BossChecklistUI.visible = !BossChecklistUI.visible;

					//BossChecklistUI
				}
			}
		}

		public override void ModifyInterfaceLayers(List<MethodSequenceListItem> layers)
		{
			int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (MouseTextIndex != -1)
			{
				layers.Insert(MouseTextIndex, new MethodSequenceListItem(
					"BossChecklist: Boss Checklist",
					delegate
					{
						if (BossChecklistUI.visible)
						{
							bossChecklistInterface.Update(Main._drawInterfaceGameTime);
							bossChecklistUI.Draw(Main.spriteBatch);
						}
						return true;
					},
					null)
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
				thoriumLoaded = ModLoader.GetMod("ThoriumMod") != null;
				bluemagicLoaded = ModLoader.GetMod("Bluemagic") != null;
				calamityLoaded = ModLoader.GetMod("CalamityMod") != null;
				crystiliumLoaded = ModLoader.GetMod("CrystiliumMod") != null;
				sacredToolsLoaded = ModLoader.GetMod("SacredTools") != null;
				tremorLoaded = ModLoader.GetMod("Tremor") != null;
				pumpkingLoaded = ModLoader.GetMod("Pumpking") != null;
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
					bossChecklistUI.AddBoss(bossname, bossValue, bossDowned);
					//RegisterButton(args[1] as Texture2D, args[2] as Action, args[3] as Func<string>);
				}
				else
				{
					ErrorLogger.Log("BossChecklist Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e)
			{
				ErrorLogger.Log("BossChecklist Call Error: " + e.StackTrace + e.Message);
			}
			return null;
		}
	}
}

