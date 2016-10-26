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
			}
			catch (Exception e)
			{
				ErrorLogger.Log("BossChecklist PostSetupContent Error: " + e.StackTrace + e.Message);
			}
		}
	}
}

