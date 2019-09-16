using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

// ✓ Kills
// ✓ Deaths
// ✓ Fight Duration
// ✓ Brink/Vitality
// ✗ Dodge Counter // Counters work, but the recording does not?
// ✗ Dodge Timer

// Boss victory messages MAY HAVE TO BE serverside configs
// Rewrite Collection display to show multiple items, and make it automatically find which ones
// Eater of Worlds STILL not working properly (has to do with InstancePerEntity)
// Possibly merged all the checkmarks/boxes into one sprite and same for boss log buttons? Just for less clutter within resources

/* Patch Notes:
 *   + Added hidden mask feature. Bosses dont show what they look like until defeated
 *   + Upgraded the spawn item tab to contain multiple items and all their recipes (You do not have to change your call, it still works with a singular int)
 *   + Added the ability to display records in chat <<<<<<<<<<<<<<<<<<< FINISH THIS <<<<<<<<<<<<<<<<<<<
 *   + Records now work in Multiplayer!
 *   + Fixed limb messages giving the wrong name in MultiPlayer
 *   + Boss Log now hides itself when if you are dead. Thats where the respawn timer should be.
 *   + Boss Log Pages now supports multiple boss types and now checks for all boss types to be inactive for records
 *   + Bosses with multiple IDs now show all corresponding head icons within the Boss Log
 *   + Boss Collection display now shows all possible trophies/masks/music boxes for ALL bosses now
 */

namespace BossChecklist
{
	// "Open UI" buttons
	internal class BossAssistButton : UIImageButton
	{
		internal string buttonType;
		internal Texture2D texture;

		public BossAssistButton(Texture2D texture, string type) : base(texture) {
			buttonType = type;
			this.texture = texture;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 stringAdjust = Main.fontMouseText.MeasureString(buttonType);
			Vector2 pos = new Vector2(innerDimensions.X - (stringAdjust.X / 3), innerDimensions.Y - 24);

			base.DrawSelf(spriteBatch);

			// Draw the Boss Log Color
			if (Id == "OpenUI") {
				Texture2D bookCover = BossChecklist.instance.GetTexture("Resources/LogUI_Button");
				Rectangle source = new Rectangle(36 * 3, 0, 34, 38);
				Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
				if (!IsMouseHovering) {
					source = new Rectangle(36 * 2, 0, 34, 38);
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
				}
				spriteBatch.Draw(bookCover, innerDimensions.ToRectangle(), source, coverColor);
			}

			if (IsMouseHovering) {
				BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
				if (!Id.Contains("CycleItem")) DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, buttonType, pos, Color.White);
				else {
					pos = new Vector2(innerDimensions.X - stringAdjust.X + 20, innerDimensions.Y + 36);
					Utils.DrawBorderString(spriteBatch, buttonType, pos, Color.White);
				}
			}
			if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) Main.player[Main.myPlayer].mouseInterface = true;
		}
	}

	internal class LogItemSlot : UIElement
	{
		internal string hoverText;
		internal Item item;
		private readonly int context;
		private readonly float scale;
		internal bool hasItem;

		public LogItemSlot(Item item, bool hasItem, string hoverText = "", int context = ItemSlot.Context.TrashItem, float scale = 1f) {
			this.context = context;
			this.scale = scale;
			this.item = item;
			this.hoverText = hoverText;
			this.hasItem = hasItem;

			Width.Set(Main.inventoryBack9Texture.Width * scale, 0f);
			Height.Set(Main.inventoryBack9Texture.Height * scale, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = scale;
			Rectangle rectangle = GetDimensions().ToRectangle();
			var backup = Main.inventoryBack6Texture;
			var backup2 = Main.inventoryBack7Texture;

			if (Main.expertMode) Main.inventoryBack6Texture = Main.inventoryBack15Texture;
			else Main.inventoryBack6Texture = BossChecklist.instance.GetTexture("Resources/ExpertOnly");

			BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];

			if (Id.Contains("loot_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
				Main.inventoryBack6Texture = BossChecklist.instance.GetTexture("Resources/ExpertCollected");
			}

			if (Id.Contains("collect_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
			}

			// Prevents empty collectible slots from being drawn
			if (!Id.Contains("collect_") || item.type != 0) {
				ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
			}

			Main.inventoryBack6Texture = backup;
			Main.inventoryBack7Texture = backup2;

			Texture2D checkMark = BossChecklist.instance.GetTexture("Resources/LogUI_Checks");
			if (hasItem && item.type != 0 && (Id.Contains("loot_") || Id.Contains("collect_"))) {
				Rectangle rect = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 22, 20);
				Rectangle source = new Rectangle(0, 0, 22, 20);
				spriteBatch.Draw(checkMark, rect, source, Color.White);
			}

			if (IsMouseHovering) {
				if (hoverText != "By Hand") {
					if (item.type != 0 || hoverText != "") {
						Color newcolor = ItemRarity.GetColor(item.rare);
						float num3 = (float)(int)Main.mouseTextColor / 255f;
						if (item.expert || item.expertOnly) {
							newcolor = new Color((byte)(Main.DiscoR * num3), (byte)(Main.DiscoG * num3), (byte)(Main.DiscoB * num3), Main.mouseTextColor);
						}
						Main.HoverItem = item;
						Main.hoverItemName = "[c/" + newcolor.Hex3() + ":" + hoverText + "]";
					}
				}
				else Main.hoverItemName = hoverText;
			}
			Main.inventoryScale = oldScale;
		}
	}

	internal class LootRow : UIElement
	{
		// Had to put the itemslots in a row in order to be put in a UIList with scroll functionality
		int order;

		public LootRow(int order) {
			this.order = order;
			Height.Pixels = 50;
			Width.Pixels = 800;
		}

		public override int CompareTo(object obj) {
			LootRow other = obj as LootRow;
			return order.CompareTo(other.order);
		}
	}

	internal class BossLogPanel : UIElement
	{
		public static bool visible = false;
		public static int itemTimer = 300;
		public static int[] itemShown;
		public static List<int>[] validItems;
		public static int headNum = -1;

		public override void Draw(SpriteBatch spriteBatch) {
			if (!visible) return;
			BossInfo selectedBoss;
			if (BossLogUI.PageNum >= 0) selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
			else selectedBoss = BossChecklist.bossTracker.SortedBosses[0];

			if (BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2 && BossLogUI.AltPage[BossLogUI.SubPageNum] && Id == "PageTwo") // PageTwo check to prevent the timer from counting down twice (once for each page)
			{
				if (validItems == null) {
					validItems = new List<int>[]
					{
						new List<int>(),
						new List<int>(),
						new List<int>()
					};
					foreach (int type in selectedBoss.collection) {
						if (type != -1) {
							Item newItem = new Item();
							newItem.SetDefaults(type);
							if (newItem.Name.Contains("Trophy") && newItem.createTile > 0) validItems[0].Add(type);
							if (newItem.Name.Contains("Mask") && newItem.vanity) validItems[1].Add(type);
							if (newItem.Name.Contains("Music Box") && newItem.createTile > 0) validItems[2].Add(type);
						}
					}
					if (validItems[0].Count == 0) validItems[0].Add(0);
					if (validItems[1].Count == 0) validItems[1].Add(0);
					if (validItems[2].Count == 0) validItems[2].Add(0);
					itemShown = new int[]
					{
						0,
						0,
						0
					};
				}

				if (itemTimer <= 0) {
					itemTimer = 300;
					for (int i = 0; i < itemShown.Length; i++) {
						if (itemShown[i] == validItems[i].Count - 1) itemShown[i] = 0;
						else itemShown[i]++;
					}
				}
				else itemTimer--;
			}

			base.Draw(spriteBatch);
			Rectangle pageRect = GetInnerDimensions().ToRectangle();
			
			if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
				// Needed to remove mousetext from outside sources when using the Boss Log
				Main.player[Main.myPlayer].mouseInterface = true;
				Main.mouseText = true;

				// Item icons such as hovering over a bed will not appear
				Main.LocalPlayer.showItemIcon = false;
				Main.LocalPlayer.showItemIcon2 = -1;
				Main.ItemIconCacheUpdate(0);
			}

			if (BossLogUI.PageNum == -1) {
				Vector2 pos = new Vector2(GetInnerDimensions().X + 19, GetInnerDimensions().Y + 15);
				if (Id == "PageOne") Utils.DrawBorderStringBig(spriteBatch, "Pre-Hardmode", pos, Colors.RarityAmber, 0.6f);
				else if (Id == "PageTwo") Utils.DrawBorderStringBig(spriteBatch, "Hardmode", pos, Colors.RarityAmber, 0.6f);

				if (!IsMouseHovering) headNum = -1;

				if (headNum != -1) {
					BossInfo headBoss = BossChecklist.bossTracker.SortedBosses[headNum];
					if (headBoss.type != BossChecklistType.Event) { 

						int adjustment = 0;
						for (int h = 0; h < headBoss.npcIDs.Count; h++) {
							Color maskedHead;
							if (!headBoss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) maskedHead = Color.Black;
							else maskedHead = Color.White;

							if (BossLogUI.GetBossHead(headBoss.npcIDs[h]) != Main.npcHeadTexture[0]) {
								Texture2D head = BossLogUI.GetBossHead(headBoss.npcIDs[h]);
								spriteBatch.Draw(head, new Rectangle(Main.mouseX + 15 + ((head.Width + 2) * adjustment), Main.mouseY + 15, head.Width, head.Height), maskedHead);
								adjustment++;
							}
						}
					}
					else {
						Texture2D invasionIcon = Main.npcHeadTexture[0];
						Color maskedHead;
						if (!headBoss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) maskedHead = Color.Black;
						else maskedHead = Color.White;

						if (headBoss.name == "Frost Legion") invasionIcon = ModContent.GetTexture("Terraria/Extra_7");
						if (headBoss.name == "Frost Moon") invasionIcon = ModContent.GetTexture("Terraria/Extra_8");
						if (headBoss.name == "Goblin Army") invasionIcon = ModContent.GetTexture("Terraria/Extra_9");
						if (headBoss.name == "Martian Madness") invasionIcon = ModContent.GetTexture("Terraria/Extra_10");
						if (headBoss.name == "Pirate Invasion") invasionIcon = ModContent.GetTexture("Terraria/Extra_11");
						if (headBoss.name == "Pumpkin Moon") invasionIcon = ModContent.GetTexture("Terraria/Extra_12");
						if (headBoss.name == "Old One's Army") invasionIcon = BossLogUI.GetBossHead(NPCID.DD2LanePortal);
						if (headBoss.name == "Solar Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerSolar);
						if (headBoss.name == "Vortex Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerVortex);
						if (headBoss.name == "Nebula Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerNebula);
						if (headBoss.name == "Stardust Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerStardust);
						if (headBoss.name == "Blood Moon") invasionIcon = BossChecklist.instance.GetTexture("Resources/BossTextures/EventBloodMoon_Head");
						if (headBoss.name == "Solar Eclipse") invasionIcon = BossChecklist.instance.GetTexture("Resources/BossTextures/EventSolarEclipse_Head");


						Rectangle iconpos = new Rectangle(Main.mouseX + 15, Main.mouseY + 15, invasionIcon.Width, invasionIcon.Height);
						if (invasionIcon != Main.npcHeadTexture[0]) spriteBatch.Draw(invasionIcon, iconpos, maskedHead);
					}
				}
			}

			if (Id == "PageOne" && BossLogUI.PageNum >= 0) {
				if (selectedBoss.pageTexture != "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
					Texture2D bossTexture = ModContent.GetTexture(selectedBoss.pageTexture);
					Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (bossTexture.Width / 2), pageRect.Y + (pageRect.Height / 2) - (bossTexture.Height / 2), bossTexture.Width, bossTexture.Height);
					Rectangle cutRect = new Rectangle(0, 0, bossTexture.Width, bossTexture.Height);
					Color masked;
					if (!selectedBoss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) masked = Color.Black;
					else masked = Color.White;
					spriteBatch.Draw(bossTexture, posRect, cutRect, masked);
				}
				else if (selectedBoss.npcIDs.Count > 0) {
					Main.instance.LoadNPC(selectedBoss.npcIDs[0]);
					Texture2D NPCTexture = Main.npcTexture[selectedBoss.npcIDs[0]];
					Rectangle snippet = new Rectangle(0, 0, NPCTexture.Width, NPCTexture.Height / Main.npcFrameCount[selectedBoss.npcIDs[0]]);
					Vector2 bossPos = new Vector2(pageRect.X + (int)((Width.Pixels / 2) - (snippet.Width / 2)), pageRect.Y + (int)((Height.Pixels / 2) - (snippet.Height / 2)));
					spriteBatch.Draw(NPCTexture, bossPos, snippet, Color.White);
				}

				if (selectedBoss.type != BossChecklistType.Event) {

					int adjustment = 0;
					for (int h = 0; h < selectedBoss.npcIDs.Count; h++) {
						Color maskedHead;
						if (!selectedBoss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) maskedHead = Color.Black;
						else maskedHead = Color.White;

						if (BossLogUI.GetBossHead(selectedBoss.npcIDs[h]) != Main.npcHeadTexture[0]) {
							Texture2D head = BossLogUI.GetBossHead(selectedBoss.npcIDs[h]);
							Rectangle headPos = new Rectangle(pageRect.X + pageRect.Width - head.Width - 10 - ((head.Width + 2) * adjustment), pageRect.Y + 5, head.Width, head.Height);
							spriteBatch.Draw(head, headPos, maskedHead);
							adjustment++;
						}
					}
				}
				else {
					Texture2D invasionIcon = Main.npcHeadTexture[0];
					Color maskedHead;
					if (!selectedBoss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) maskedHead = Color.Black;
					else maskedHead = Color.White;
					
					if (selectedBoss.name == "Frost Legion") invasionIcon = ModContent.GetTexture("Terraria/Extra_7");
					if (selectedBoss.name == "Frost Moon") invasionIcon = ModContent.GetTexture("Terraria/Extra_8");
					if (selectedBoss.name == "Goblin Army") invasionIcon = ModContent.GetTexture("Terraria/Extra_9");
					if (selectedBoss.name == "Martian Madness") invasionIcon = ModContent.GetTexture("Terraria/Extra_10");
					if (selectedBoss.name == "Pirate Invasion") invasionIcon = ModContent.GetTexture("Terraria/Extra_11");
					if (selectedBoss.name == "Pumpkin Moon") invasionIcon = ModContent.GetTexture("Terraria/Extra_12");
					if (selectedBoss.name == "Old One's Army") invasionIcon = BossLogUI.GetBossHead(NPCID.DD2LanePortal);
					if (selectedBoss.name == "Solar Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerSolar);
					if (selectedBoss.name == "Vortex Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerVortex);
					if (selectedBoss.name == "Nebula Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerNebula);
					if (selectedBoss.name == "Stardust Pillar") invasionIcon = BossLogUI.GetBossHead(NPCID.LunarTowerStardust);
					if (selectedBoss.name == "Blood Moon") invasionIcon = BossChecklist.instance.GetTexture("Resources/BossTextures/EventBloodMoon_Head");
					if (selectedBoss.name == "Solar Eclipse") invasionIcon = BossChecklist.instance.GetTexture("Resources/BossTextures/EventSolarEclipse_Head");

					Rectangle iconpos = new Rectangle(pageRect.X + pageRect.Width - invasionIcon.Width - 10, pageRect.Y + 5, invasionIcon.Width, invasionIcon.Height);
					if (invasionIcon != Main.npcHeadTexture[0]) spriteBatch.Draw(invasionIcon, iconpos, maskedHead);
				}

				string sourceDisplayName = $"[c/9696ff:{selectedBoss.SourceDisplayName}]";
				string isDefeated = "";

				if (selectedBoss.downed()) isDefeated = "[c/d3ffb5:Defeated in " + Main.worldName + "]";
				else isDefeated = "[c/ffccc8:Undefeated in " + Main.worldName + "]";

				Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
				Utils.DrawBorderString(spriteBatch, selectedBoss.name, pos, Color.Goldenrod);

				pos = new Vector2(pageRect.X + 5, pageRect.Y + 30);
				Utils.DrawBorderString(spriteBatch, isDefeated, pos, Color.White);

				pos = new Vector2(pageRect.X + 5, pageRect.Y + 55);
				Utils.DrawBorderString(spriteBatch, sourceDisplayName, pos, Color.White);
			}

			if (Id == "PageOne" && BossLogUI.PageNum == -2) {
				// Credits Page
				Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
				Utils.DrawBorderString(spriteBatch, "Special thanks to:", pos, Color.IndianRed);

				Texture2D users = BossChecklist.instance.GetTexture("Resources/CreditUsers");
				float textScaling = 0.75f;

				// Jopojelly
				Vector2 userpos = new Vector2(pageRect.X + 20, pageRect.Y + 40);
				Rectangle userselected = new Rectangle(0 + (59 * 1), 0, 59, 58);
				spriteBatch.Draw(users, userpos, userselected, Color.White);

				pos = new Vector2(pageRect.X + 85, pageRect.Y + 50);
				Utils.DrawBorderString(spriteBatch, "Jopojelly\nOriginal creator of Boss Checklist!", pos, Color.CornflowerBlue, textScaling);

				// SheepishShepherd
				userpos = new Vector2(pageRect.X + 20, pageRect.Y + 110);
				userselected = new Rectangle(0 + (59 * 0), 0, 59, 58);
				spriteBatch.Draw(users, userpos, userselected, Color.White);

				pos = new Vector2(pageRect.X + 85, pageRect.Y + 120);
				Utils.DrawBorderString(spriteBatch, "Sheepish Shepherd\nBoss log UI and other boss features code", pos, Color.Goldenrod, textScaling);

				// direwolf420
				userpos = new Vector2(pageRect.X + 20, pageRect.Y + 180);
				userselected = new Rectangle(0 + (59 * 3), 0, 59, 58);
				spriteBatch.Draw(users, userpos, userselected, Color.White);

				pos = new Vector2(pageRect.X + 85, pageRect.Y + 190);
				Utils.DrawBorderString(spriteBatch, "direwolf420\nBoss radar and multiplayer compatability code", pos, Color.Tomato, textScaling);

				// Orian
				userpos = new Vector2(pageRect.X + 20, pageRect.Y + 250);
				userselected = new Rectangle(0 + (59 * 2), 0, 59, 58);
				spriteBatch.Draw(users, userpos, userselected, Color.White);

				pos = new Vector2(pageRect.X + 85, pageRect.Y + 260);
				Utils.DrawBorderString(spriteBatch, "Orian34\nSingleplayer beta testing", pos, new Color(49, 210, 162), textScaling);

				// Panini
				userpos = new Vector2(pageRect.X + 11, pageRect.Y + 320);
				userselected = new Rectangle(0 + (59 * 4), 0, 68, 58);
				spriteBatch.Draw(users, userpos, userselected, Color.White);

				pos = new Vector2(pageRect.X + 85, pageRect.Y + 330);
				Utils.DrawBorderString(spriteBatch, "Panini\nMultiplayer/Server beta testing", pos, Color.HotPink, textScaling);

				// "Spriters"
				pos = new Vector2(pageRect.X + 20, pageRect.Y + 390);
				Utils.DrawBorderString(spriteBatch, "...and thank you RiverOaken for an amazing book sprite!", pos, Color.MediumPurple, textScaling);

				/*
                pos = new Vector2(pageRect.X + 5, pageRect.Y + 270);
                Utils.DrawBorderString(spriteBatch, "To add your own bosses to the boss log, \nfollow the instructions on the homepage.\nAdvise other modders to do the same. \nThe more this mod expands the better!!", pos, Color.LightCoral);
				*/
			}

			if (Id == "PageTwo" && BossLogUI.PageNum == -2) {
				// Credits Page

				List<string> optedMods = new List<string>();
				foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
					if (boss.modSource != "Vanilla" && boss.modSource != "Unknown") {
						string sourceDisplayName = boss.SourceDisplayName;
						if (!optedMods.Contains(sourceDisplayName)) {
							optedMods.Add(sourceDisplayName);
						}
					}
				}

				int adjustment = 0;

				if (optedMods.Count != 0) {
					Vector2 pos = new Vector2(GetInnerDimensions().X + 5, GetInnerDimensions().Y + 5);
					Utils.DrawBorderString(spriteBatch, "Thanks to all the mods who opted in!", pos, Color.LightSkyBlue); adjustment += 35;

					pos = new Vector2(GetInnerDimensions().X + 5, GetInnerDimensions().Y + adjustment);
					Utils.DrawBorderString(spriteBatch, "[This list only contains loaded mods]", pos, Color.LightBlue);
				}
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 0 && selectedBoss.modSource != "Unknown") {
				if (selectedBoss.type != BossChecklistType.Event) {
					// Boss Records Subpage
					Texture2D achievements = ModContent.GetTexture("Terraria/UI/Achievements");
					BossStats record = Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[BossLogUI.PageNum].stat;

					string recordType = "";
					string recordNumbers = "";
					int achX = 0;
					int achY = 0;

					for (int i = 0; i < 4; i++) // 4 Records total
					{
						if (i == 0) {
							recordType = "Kill Death Ratio";

							int killTimes = record.kills;
							int deathTimes = record.deaths;

							if (killTimes >= deathTimes) {
								achX = 4;
								achY = 10;
							}
							else {
								achX = 4;
								achY = 8;
							}

							if (killTimes == 0 && deathTimes == 0) recordNumbers = "Unchallenged!";
							else recordNumbers = killTimes + " kills / " + deathTimes + " deaths";

							SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;
							SubpageButton.displayLastArray[i] = "Victories: " + killTimes;
						}
						else if (i == 1) {
							recordType = "Quickest Victory";
							if (BossLogUI.AltPage[BossLogUI.SubPageNum]) recordType = "Slowest Victory";
							string finalResult = "";

							int BestRecord = record.fightTime;
							int WorstRecord = record.fightTime2;
							int LastRecord = record.fightTimeL;

							if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
								achX = 4;
								achY = 9;

								if (LastRecord == BestRecord && LastRecord != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 232, (int)GetInnerDimensions().Y + 180, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								if (BestRecord > 0) {
									double recordOrg = (double)BestRecord / 60;
									int recordMin = (int)recordOrg / 60;
									int recordSec = (int)recordOrg % 60;

									double record2 = (double)LastRecord / 60;
									int recordMin2 = (int)record2 / 60;
									int recordSec2 = (int)record2 % 60;

									string rec1 = recordOrg.ToString("0.##");
									string recSec1 = recordSec.ToString("0.##");
									string rec2 = record2.ToString("0.##");
									string recSec2 = recordSec2.ToString("0.##");

									if (rec1.Length > 2 && rec1.Substring(rec1.Length - 2).Contains(".")) rec1 += "0";
									if (recSec1.Length > 2 && recSec1.Substring(recSec1.Length - 2).Contains(".")) recSec1 += "0";
									if (rec2.Length > 2 && rec2.Substring(rec2.Length - 2).Contains(".")) rec2 += "0";
									if (recSec2.Length > 2 && recSec2.Substring(recSec2.Length - 2).Contains(".")) recSec2 += "0";

									if (recordMin > 0) finalResult += recordMin + "m " + recSec1 + "s";
									else finalResult += rec1 + "s";

									SubpageButton.displayArray[i] = recordType + ": " + finalResult;

									if (LastRecord == BestRecord) {
										string lastFight = "";
										if (recordMin2 > 0) lastFight += recordMin2 + "m " + recSec2 + "s";
										else lastFight += rec2 + "s";
										SubpageButton.displayLastArray[i] = "Fight Time: " + lastFight;

										finalResult += " [" + lastFight + "]";
									}
									else {
										SubpageButton.displayLastArray[i] = "";
									}

									recordNumbers = finalResult;
								}
								else {
									recordNumbers = "No record!";
									SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;
								}
							}
							else {
								achX = 7;
								achY = 5;

								if (LastRecord == WorstRecord && LastRecord != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 232, (int)GetInnerDimensions().Y + 180, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								if (WorstRecord > 0) {
									double recordOrg = (double)WorstRecord / 60;
									int recordMin = (int)recordOrg / 60;
									int recordSec = (int)recordOrg % 60;

									double record2 = (double)LastRecord / 60;
									int recordMin2 = (int)record2 / 60;
									int recordSec2 = (int)record2 % 60;

									string rec1 = recordOrg.ToString("0.##");
									string recSec1 = recordSec.ToString("0.##");
									string rec2 = record2.ToString("0.##");
									string recSec2 = recordSec2.ToString("0.##");

									if (rec1.Length > 2 && rec1.Substring(rec1.Length - 2).Contains(".")) rec1 += "0";
									if (recSec1.Length > 2 && recSec1.Substring(recSec1.Length - 2).Contains(".")) recSec1 += "0";
									if (rec2.Length > 2 && rec2.Substring(rec2.Length - 2).Contains(".")) rec2 += "0";
									if (recSec2.Length > 2 && recSec2.Substring(recSec2.Length - 2).Contains(".")) recSec2 += "0";

									if (recordMin > 0) finalResult += recordMin + "m " + recSec1 + "s";
									else finalResult += rec1 + "s";

									SubpageButton.displayArray[i] = recordType + ": " + finalResult;

									string lastFight = "";

									if (LastRecord != -1) {
										if (recordMin2 > 0) {
											lastFight = "Fight Time: " + recordMin2 + "m " + recSec2 + "s";
											SubpageButton.displayLastArray[i] = "";
											if (LastRecord != WorstRecord) finalResult += " [" + recordMin2 + "m " + recSec2 + "s]";
										}
										else {
											lastFight = "Fight Time: " + rec2 + "s";
											if (LastRecord != WorstRecord) finalResult += " [" + rec2 + "s]";
										}
									}

									recordNumbers = finalResult;
								}
								else {
									recordNumbers = "No record!";
									SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;
								}
							}
						}
						else if (i == 2) {
							recordType = "Vitality";
							if (BossLogUI.AltPage[BossLogUI.SubPageNum]) recordType = "Brink of Death";

							int BestRecord = record.brink2;
							int BestPercent = record.brinkPercent2;
							int WorstRecord = record.brink;
							int WorstPercent = record.brinkPercent;
							int LastRecord = record.brinkL;
							int LastPercent = record.brinkPercentL;

							if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
								achX = 3;
								achY = 0;
								if (LastRecord == BestRecord && LastRecord != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 182, (int)GetInnerDimensions().Y + 255, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								string finalResult = "";
								string lastFight = "";
								if (BestRecord > 0) {
									finalResult += BestRecord + " (" + BestPercent + "%)";
									SubpageButton.displayArray[i] = recordType + ": " + finalResult;
									if (LastRecord != BestRecord && LastRecord != -1) {
										lastFight = "Lowest Health: " + LastRecord + " (" + LastPercent + "%)";
										if (LastRecord != BestRecord) finalResult += " [" + LastRecord + " (" + LastPercent + "%)]";
									}
									recordNumbers = finalResult;
								}
								else {
									recordNumbers = "No record!";
									SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;
								}

								SubpageButton.displayLastArray[i] = lastFight;
							}
							else {
								achX = 6;
								achY = 7;

								if (LastRecord == WorstRecord && LastRecord != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 182, (int)GetInnerDimensions().Y + 255, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								string finalResult = "";
								string lastFight = "";
								if (WorstRecord > 0) {
									finalResult += WorstRecord + " (" + WorstPercent + "%)";
									SubpageButton.displayArray[i] = recordType + ": " + finalResult;
									if (LastRecord != -1) {
										lastFight = "Lowest Health: " + LastRecord + " (" + LastPercent + "%)";
										if (LastRecord != WorstRecord) finalResult += " [" + LastRecord + " (" + LastPercent + "%)" + "]";
									}
									recordNumbers = finalResult;
								}
								else {
									recordNumbers = "No record!";
									SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;
								}

								SubpageButton.displayLastArray[i] = lastFight;
							}
						}
						else if (i == 3) {
							recordType = "Ninja Reflexes";
							if (BossLogUI.AltPage[BossLogUI.SubPageNum]) recordType = "Clumsy Fool";

							int timer = record.dodgeTime;
							int low = record.totalDodges;
							int high = record.totalDodges2;
							int last = record.totalDodgesL;

							double timer2 = (double)record.dodgeTime / 60;
							string timerOutput = timer2.ToString("0.##");

							if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
								achX = 0;
								achY = 7;

								if (last == low && last != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 225, (int)GetInnerDimensions().Y + 332, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								if (timer <= 0 || low < 0) recordNumbers = "No record!";
								else recordNumbers = low + " (" + timerOutput + "s)";
								SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;

								if (last != -1) {
									if (low != last) recordNumbers += " [" + last + "]";
									SubpageButton.displayLastArray[i] = "Times Hit: " + last;
								}
								else SubpageButton.displayLastArray[i] = "";
							}
							else {
								achX = 4;
								achY = 2;

								if (last == high && last != -1) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle((int)GetInnerDimensions().X + 225, (int)GetInnerDimensions().Y + 332, text.Width, text.Height);
									spriteBatch.Draw(text, exclam, Color.White);
								}

								if (high < 0) recordNumbers = "No record!";
								else recordNumbers = high + " (" + timerOutput + "s)";
								SubpageButton.displayArray[i] = recordType + ": " + recordNumbers;

								if (last != -1) {
									if (high != last) recordNumbers = high + " (" + timerOutput + "s)" + " [" + last + "]";
									SubpageButton.displayLastArray[i] = "Times Hit: " + last;
								}
								else SubpageButton.displayLastArray[i] = "";
							}
						}

						Rectangle posRect = new Rectangle(pageRect.X, pageRect.Y + 100 + (75 * i), 64, 64);
						Rectangle cutRect = new Rectangle(66 * achX, 66 * achY, 64, 64);
						spriteBatch.Draw(achievements, posRect, cutRect, Color.White);

						Vector2 stringAdjust = Main.fontMouseText.MeasureString(recordType);
						Vector2 pos = new Vector2(GetInnerDimensions().X + (GetInnerDimensions().Width / 2 - 45) - (stringAdjust.X / 3), GetInnerDimensions().Y + 110 + i * 75);
						Utils.DrawBorderString(spriteBatch, recordType, pos, Color.Goldenrod);

						stringAdjust = Main.fontMouseText.MeasureString(recordNumbers);
						pos = new Vector2(GetInnerDimensions().X + (GetInnerDimensions().Width / 2 - 45) - (stringAdjust.X / 3), GetInnerDimensions().Y + 135 + i * 75);
						Utils.DrawBorderString(spriteBatch, recordNumbers, pos, Color.White);
					}
				}
				else {
					// TODO: Make boxes for event NPC list. Next to the box, a number appears for how many the player/world has killed (banner count)
					int offset = 0;
					for (int i = 0; i < selectedBoss.npcIDs.Count; i++) {
						int npcID = selectedBoss.npcIDs[i];
						Main.instance.LoadNPC(npcID);
						Texture2D NPCTexture = Main.npcTexture[npcID];
						Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X, GetInnerDimensions().ToRectangle().Y + offset);
						Rectangle snippet = new Rectangle(0, 0, NPCTexture.Width, NPCTexture.Height / Main.npcFrameCount[npcID]);
						spriteBatch.Draw(NPCTexture, pos, snippet, Color.White);
						offset += NPCTexture.Height / Main.npcFrameCount[npcID];
					}
				}
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 1) {
				// Spawn Item Subpage
				/// Should I implement a text based spawn page?
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2) {
				if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
					// Loot Table Subpage
					Main.instance.LoadTiles(237);
					Texture2D bag = ModContent.GetTexture("BossChecklist/Resources/treasureBag");
					for (int i = 0; i < selectedBoss.loot.Count; i++) {
						Item bagItem = new Item();
						bagItem.SetDefaults(selectedBoss.loot[i]);
						if (bagItem.expert && bagItem.Name.Contains("Treasure Bag")) {
							if (bagItem.type < ItemID.Count) {
								bag = ModContent.GetTexture("Terraria/Item_" + bagItem.type);
							}
							else {
								bag = ModContent.GetTexture(ItemLoader.GetItem(bagItem.type).Texture);
								break;
							}
						}
					}

					for (int i = 0; i < 7; i++) {
						Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - 20 - (bag.Width / 2), pageRect.Y + 88, bag.Width, bag.Height);
						spriteBatch.Draw(bag, posRect, Color.White);
					}
				}
				else {
					// Collectibles Subpage
					BossCollection Collections = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];

					// PageNum already corresponds with the index of the saved player data

					Texture2D template = ModContent.GetTexture("BossChecklist/Resources/CollectionTemplate");
					if (validItems[2][itemShown[2]] > 0 && Collections.collectibles.Any(x => x.type == validItems[2][itemShown[2]])) {
						template = ModContent.GetTexture("BossChecklist/Resources/CollectionTemplate_NoMusicBox");
					}

					spriteBatch.Draw(template, new Rectangle(pageRect.X + (pageRect.Width / 2) - (template.Width / 2) - 20, pageRect.Y + 84, template.Width, template.Height), Color.White);

					// Draw Mask
					if (validItems[1][itemShown[1]] > 0 && Collections.collectibles.Any(x => x.type == validItems[1][itemShown[1]])) {
						Texture2D mask;
						if (validItems[1][itemShown[1]] < ItemID.Count) {
							Item newItem = new Item();
							newItem.SetDefaults(validItems[1][itemShown[1]]);
							mask = ModContent.GetTexture("Terraria/Armor_Head_" + newItem.headSlot);
						}
						else mask = ModContent.GetTexture(ItemLoader.GetItem(validItems[1][itemShown[1]]).Texture + "_Head");

						int frameCut = mask.Height / 24;
						Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (mask.Width / 2) - 8, pageRect.Y + (pageRect.Height / 2) - (frameCut / 2) - 86, mask.Width, frameCut);
						Rectangle cutRect = new Rectangle(0, 0, mask.Width, frameCut);
						spriteBatch.Draw(mask, posRect, cutRect, Color.White);
					}

					if (validItems[0][itemShown[0]] > 0 && Collections.collectibles.Any(x => x.type == validItems[0][itemShown[0]])) {
						int offsetX = 0;
						int offsetY = 0;
						Main.instance.LoadTiles(240);
						Texture2D trophy = Main.tileTexture[240];

						if (validItems[0][itemShown[0]] < ItemID.Count) {
							offsetX = BossLogUI.GetVanillaBossTrophyPos(validItems[0][itemShown[0]])[0];
							offsetY = BossLogUI.GetVanillaBossTrophyPos(validItems[0][itemShown[0]])[1];

							int backupX = offsetX;
							int backupY = offsetY;

							for (int i = 0; i < 9; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 98 + (offsetX * 16) - (backupX * 16), pageRect.Y + 126 + (offsetY * 16) - (backupY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, offsetY * 18, 16, 16);

								spriteBatch.Draw(trophy, posRect, cutRect, Color.White);

								offsetX++;
								if (i == 2 || i == 5) {
									offsetX = backupX;
									offsetY++;
								}
							}
						}
						else {
							Main.instance.LoadTiles(ItemLoader.GetItem(validItems[0][itemShown[0]]).item.createTile);
							trophy = Main.tileTexture[ItemLoader.GetItem(validItems[0][itemShown[0]]).item.createTile];

							offsetX = 0;
							offsetY = 0;

							for (int i = 0; i < 9; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 98 + (offsetX * 16), pageRect.Y + 126 + (offsetY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, offsetY * 18, 16, 16);

								spriteBatch.Draw(trophy, posRect, cutRect, Color.White);

								offsetX++;
								if (i == 2 || i == 5) {
									offsetX = 0;
									offsetY++;
								}
							}
						}
					}

					// Draw Music Box
					if (validItems[2][itemShown[2]] > 0 && Collections.collectibles.Any(x => x.type == validItems[2][itemShown[2]])) {
						int offsetX = 0;
						int offsetY = 0;
						Main.instance.LoadTiles(139);
						Texture2D musicBox = Main.tileTexture[139];

						if (selectedBoss.collection[2] < ItemID.Count) {
							if (selectedBoss.collection[2] == ItemID.MusicBoxBoss1) {
								if (Main.music[MusicID.Boss1].IsPlaying) offsetX = 2;
								offsetY = 10;
							}
							else if (selectedBoss.collection.Any(x => x == ItemID.MusicBoxBoss2)) {
								if (Main.music[MusicID.Boss2].IsPlaying) offsetX = 2;
								offsetY = 20;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxBoss3) {
								if (Main.music[MusicID.Boss3].IsPlaying) offsetX = 2;
								offsetY = 24;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxBoss4) {
								if (Main.music[MusicID.Boss4].IsPlaying) offsetX = 2;
								offsetY = 32;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxBoss5) {
								if (Main.music[MusicID.Boss5].IsPlaying) offsetX = 2;
								offsetY = 48;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxPlantera) {
								if (Main.music[MusicID.Plantera].IsPlaying) offsetX = 2;
								offsetY = 46;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxDD2) {
								if (Main.music[MusicID.OldOnesArmy].IsPlaying) offsetX = 2;
								offsetY = 78;
							}
							else if (selectedBoss.collection[2] == ItemID.MusicBoxLunarBoss) {
								if (Main.music[MusicID.LunarBoss].IsPlaying) offsetX = 2;
								offsetY = 64;
							}

							int backupX = offsetX;
							int backupY = offsetY;

							for (int i = 0; i < 4; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 210 + (offsetX * 16) - (backupX * 16), pageRect.Y + 158 + (offsetY * 16) - (backupY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, (offsetY * 18) - 2, 16, 16);

								spriteBatch.Draw(musicBox, posRect, cutRect, Color.White);

								offsetX++;
								if (i == 1) {
									offsetX = backupX;
									offsetY++;
								}
							}
						}
						else {
							Main.instance.LoadTiles(ItemLoader.GetItem(selectedBoss.collection[2]).item.createTile);
							musicBox = Main.tileTexture[ItemLoader.GetItem(selectedBoss.collection[2]).item.createTile];

							for (int i = 0; i < 4; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 210 + (offsetX * 16), pageRect.Y + 158 + (offsetY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, (offsetY * 18) - 2, 16, 16);

								spriteBatch.Draw(musicBox, posRect, cutRect, Color.White);

								offsetX++;
								if (i == 1) {
									offsetX = 0;
									offsetY++;
								}
							}
						}
					}
				}
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 3 && validItems != null) {

			}
		}
	}

	internal class FixedUIScrollbar : UIScrollbar
	{
		protected override void DrawSelf(SpriteBatch spriteBatch) {
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.instance.BossLogInterface;
			base.DrawSelf(spriteBatch);
			UserInterface.ActiveInstance = temp;
		}

		public override void MouseDown(UIMouseEvent evt) {
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.instance.BossLogInterface;
			base.MouseDown(evt);
			UserInterface.ActiveInstance = temp;
		}

		public override void Click(UIMouseEvent evt) {
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.instance.BossLogInterface;
			base.MouseDown(evt);
			UserInterface.ActiveInstance = temp;
		}
	}

	internal class BookUI : UIImage
	{
		public static bool visible = false;
		Texture2D book;

		public BookUI(Texture2D texture) : base(texture) {
			book = texture;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			if (!visible || Main.LocalPlayer.dead) return;

			if (BossLogUI.PageNum != -1 && (Id == "filterPanel" || Id == "Filter_Tab")) return;

			if (Id == "TableOfContents_Tab") {
				Texture2D pages = BossChecklist.instance.GetTexture("Resources/LogUI_Back");
				Vector2 pagePos = new Vector2((Main.screenWidth / 2) - 400, (Main.screenHeight / 2) - 250);
				spriteBatch.Draw(pages, pagePos, BossChecklist.BossLogConfig.BossLogColor);
			}
			if (!Id.Contains("_Tab")) base.DrawSelf(spriteBatch);
			else {
				// Tab drawing
				SpriteEffects effect = SpriteEffects.FlipHorizontally;
				if (Left.Pixels <= (Main.screenWidth / 2) - 400 - 16) effect = SpriteEffects.None;

				Color color = new Color(153, 199, 255);
				if (Id == "Bosses_Tab") color = new Color(255, 168, 168);
				else if (Id == "MiniBosses_Tab") color = new Color(153, 253, 119);
				else if (Id == "Events_Tab") color = new Color(196, 171, 254);
				else if (Id == "Credits_Tab") color = new Color(218, 175, 133);
				color = Color.Tan;

				spriteBatch.Draw(book, GetDimensions().ToRectangle(), new Rectangle(0, 0, book.Width, book.Height), color, 0f, Vector2.Zero, effect, 0f);
			}
			if (Id == "Events_Tab") {
				// Paper Drawing
				Texture2D pages = BossChecklist.instance.GetTexture("Resources/LogUI_Paper");
				Vector2 pagePos = new Vector2((Main.screenWidth / 2) - 400, (Main.screenHeight / 2) - 250);
				spriteBatch.Draw(pages, pagePos, Color.White);
			}
			Main.playerInventory = false;

			if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
				// Needed to remove mousetext from outside sources when using the Boss Log
				Main.player[Main.myPlayer].mouseInterface = true;
				Main.mouseText = true;

				// Item icons such as hovering over a bed will not appear
				Main.LocalPlayer.showItemIcon = false;
				Main.LocalPlayer.showItemIcon2 = -1;
				Main.ItemIconCacheUpdate(0);
			}

			if (Id.Contains("_Tab")) {
				// Tab Icon
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Texture2D texture = BossChecklist.instance.GetTexture("Resources/LogUI_Nav");
				Vector2 pos = new Vector2(inner.X + Width.Pixels / 2 - 11, inner.Y + Height.Pixels / 2 - 11);
				Rectangle cut = new Rectangle(2 * 24, 0 * 24, 22, 22);
				if (Id == "Bosses_Tab") cut = new Rectangle(0 * 24, 1 * 24, 22, 22);
				else if (Id == "MiniBosses_Tab") cut = new Rectangle(1 * 24, 1 * 24, 22, 22);
				else if (Id == "Events_Tab") cut = new Rectangle(2 * 24, 1 * 24, 22, 22);
				else if (Id == "Credits_Tab") cut = new Rectangle(3 * 24, 0 * 24, 22, 22);
				else if (Id == "Filter_Tab") cut = new Rectangle(3 * 24, 1 * 24, 22, 22);
				spriteBatch.Draw(texture, pos, cut, Color.White);
			}

			if (Id.Contains("C_") && IsMouseHovering) {
				if (Id == "C_0") Main.hoverItemName = BossChecklist.BossLogConfig.FilterBosses;
				if (Id == "C_1") Main.hoverItemName = BossChecklist.BossLogConfig.FilterMiniBosses;
				if (Id == "C_2") Main.hoverItemName = BossChecklist.BossLogConfig.FilterEvents;
			}
		}
	}

	internal class TableOfContents : UIText
	{
		float order = 0;
		bool nextCheck;
		string text;

		public TableOfContents(float order, string text, bool nextCheck, float textScale = 1, bool large = false) : base(text, textScale, large) {
			this.order = order;
			this.nextCheck = nextCheck;
			Recalculate();
			this.text = text;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);

			Texture2D checkGrid = BossChecklist.instance.GetTexture("Resources/LogUI_Checks");
			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 pos = new Vector2(innerDimensions.X - 20, innerDimensions.Y - 5);
			Rectangle source = new Rectangle(72, 0, 22, 20);
			spriteBatch.Draw(checkGrid, pos, source, Color.White);

			Vector2 pos2 = new Vector2(innerDimensions.X + Main.fontMouseText.MeasureString(text).X + 6, innerDimensions.Y - 2);
			List<BossInfo> sortedBosses = BossChecklist.bossTracker.SortedBosses;
			int index = sortedBosses.FindIndex(x => x.progression == order);

			bool allLoot = false;
			bool allCollect = false;

			foreach (int loot in sortedBosses[index].loot) {
				if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[index].loot.Any(x => x.type == loot)) {
					allLoot = true;
				}
				else if (loot != sortedBosses[index].loot[0]) {
					allLoot = false;
					break;
				}
			}
			foreach (int collectible in sortedBosses[index].collection) {
				if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[index].collectibles.Any(x => x.type == collectible)) {
					allCollect = true;
				}
				else if (collectible != -1 && collectible != 0) {
					allCollect = false;
					break;
				}
			}

			if (allLoot && allCollect) {
				Rectangle iconType = new Rectangle(72, 22, 22, 20);
				spriteBatch.Draw(checkGrid, pos2, iconType, Color.White);
			}
			else {
				if (allLoot) {
					Rectangle iconType = new Rectangle(24, 22, 22, 20);
					spriteBatch.Draw(checkGrid, pos2, iconType, Color.White);
				}
				if (allCollect) {
					Rectangle iconType = new Rectangle(48, 22, 22, 20);
					spriteBatch.Draw(checkGrid, pos2, iconType, Color.White);
				}
			}

			if (order != -1f) {
				BossChecklist BA = BossChecklist.instance;

				Rectangle checkType = new Rectangle(0, 0, 22, 20);
				Rectangle exType = new Rectangle(0, 0, 22, 20);
				
				if (sortedBosses[Convert.ToInt32(Id)].downed()) {
					if (BossChecklist.BossLogConfig.SelectedCheckmarkType == "X and  ☐") checkType = new Rectangle(24, 0, 22, 20);
					else checkType = new Rectangle(0, 0, 22, 20);
				}
				else {
					if (BossChecklist.BossLogConfig.SelectedCheckmarkType == "✓ and  X") checkType = new Rectangle(24, 0, 22, 20);
					else checkType = new Rectangle(72, 0, 22, 20);
					if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) checkType = new Rectangle(48, 0, 22, 20);
				}

				spriteBatch.Draw(checkGrid, pos, checkType, Color.White);

				if (BossChecklist.BossLogConfig.ColoredBossText) {
					if (IsMouseHovering && sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.DarkSeaGreen;
					else if (IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.IndianRed;
					else if (!IsMouseHovering && sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Colors.RarityGreen;
					else if (!IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Colors.RarityRed;
					if (IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed() && nextCheck && BossChecklist.BossLogConfig.DrawNextMark) TextColor = new Color(189, 180, 64);
					else if (!IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed() && nextCheck && BossChecklist.BossLogConfig.DrawNextMark) TextColor = new Color(248, 235, 91);
				}
				else {
					if (IsMouseHovering) TextColor = new Color(80, 85, 100);
					else TextColor = new Color(140, 145, 160);
				}

				if ((WorldGen.crimson && sortedBosses[Convert.ToInt32(Id)].name == "Eater of Worlds")
				|| (!WorldGen.crimson && sortedBosses[Convert.ToInt32(Id)].name == "Brain of Cthulhu")
				|| !sortedBosses[Convert.ToInt32(Id)].available()) {
					if (sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = new Color(105, 125, 105);
					else TextColor = new Color(125, 105, 105);
				}

				if (IsMouseHovering) BossLogPanel.headNum = Convert.ToInt32(Id);
			}
		}

		public override int CompareTo(object obj) {
			TableOfContents other = obj as TableOfContents;
			return order.CompareTo(other.order);
		}
	}

	internal class FittedTextPanel : UITextPanel<string>
	{
		string text;
		public FittedTextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
			this.text = text;
		}

		const float infoScaleX = 1f;
		const float infoScaleY = 1f;
		public override void Draw(SpriteBatch spriteBatch) {
			Rectangle hitbox = new Rectangle((int)GetInnerDimensions().X, (int)GetInnerDimensions().Y, (int)Width.Pixels, 100);

			string info = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].info ?? "No info available";
			int hoveredSnippet = -1;
			TextSnippet[] textSnippets = ChatManager.ParseMessage(text, Color.White).ToArray();
			ChatManager.ConvertNormalSnippets(textSnippets);

			for (int i = 0; i < ChatManager.ShadowDirections.Length; i++) {
				ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, Main.fontMouseText, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + ChatManager.ShadowDirections[i] * 1,
					Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
			}
			Vector2 size = ChatManager.DrawColorCodedString(Main.spriteBatch, Main.fontMouseText, textSnippets,
				new Vector2(2, 15 + 3) + hitbox.TopLeft(), Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out hoveredSnippet, hitbox.Width - (7 * 2), false);

		}
	}

	internal class SubpageButton : UIPanel
	{
		string buttonString;

		public static string[] displayArray = new string[4];
		public static string[] displayLastArray = new string[4];
		bool displayRecord = true; // To prevent multiple messages from accuring by holding down click
		int recordCooldown = 0; // Allow only one display every 5 seconds

		public SubpageButton(string type) {
			buttonString = type;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			if (recordCooldown > 0) recordCooldown--;

			if (buttonString == "Boss Records" || buttonString == "Kill Count") {
				buttonString = "Kill Count";
				if (BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].type == BossChecklistType.Event) buttonString = "Kill Count";
				else buttonString = "Boss Records";
			}
			BackgroundColor = Color.Brown;
			base.DrawSelf(spriteBatch);

			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 stringAdjust = Main.fontMouseText.MeasureString(buttonString);
			Vector2 pos = new Vector2(innerDimensions.X - (stringAdjust.X / 3) + Width.Pixels / 3, innerDimensions.Y - 10);
			if (buttonString != "Disclaimer" && buttonString != "recordAlts") {
				DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, buttonString, pos, Color.Gold);
			}

			Texture2D text = ModContent.GetTexture("Terraria/UI/Achievement_Categories");
			Rectangle exclamPos = new Rectangle((int)GetInnerDimensions().X - 12, (int)GetInnerDimensions().Y - 12, 32, 32);

			if (buttonString == "") {
				if (BossLogUI.SubPageNum == 0) {
					if (Id == "Display Records") {
						Rectangle exclamCut = new Rectangle(34 * 2, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) {
							Main.hoverItemName = "Left-click to display your current records";
							if (displayLastArray[3] != "") Main.hoverItemName += "\nRight-click to display the records of your last fight";
							if (displayRecord && recordCooldown == 0) {
								if (Main.mouseLeft) {
									recordCooldown = 600;
									displayRecord = false;
									/*if (Main.dedServ)
									{
										NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("[" + Main.LocalPlayer.name + "'s current records with " + BossChecklist.bossTracker.allBosses[BossLogUI.PageNum].name + "]"), new Color(82, 175, 82));
										if (displayArray[0] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[0]), new Color(138, 210, 137));
										if (displayArray[1] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[1]), new Color(138, 210, 137));
										if (displayArray[2] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[2]), new Color(138, 210, 137));
										if (displayArray[3] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[3]), new Color(138, 210, 137));
									}
									else*/
									{
										Main.NewText("[" + Main.LocalPlayer.name + "'s current records with " + BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].name + "]", new Color(82, 175, 82));
										if (displayArray[0] != "") Main.NewText(displayArray[0], new Color(138, 210, 137));
										if (displayArray[1] != "") Main.NewText(displayArray[1], new Color(138, 210, 137));
										if (displayArray[2] != "") Main.NewText(displayArray[2], new Color(138, 210, 137));
										if (displayArray[3] != "") Main.NewText(displayArray[3], new Color(138, 210, 137));
									}
								}
								else if (Main.mouseRight && displayLastArray[3] != "") {
									recordCooldown = 600;
									displayRecord = false;
									/*if (Main.dedServ)
									{
										NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("[" + Main.LocalPlayer.name + "'s last fight stats with " + BossChecklist.bossTracker.allBosses[BossLogUI.PageNum].name + "]"), new Color(82, 175, 82));
										if (displayLastArray[0] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[0]), new Color(138, 210, 137));
										if (displayLastArray[1] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[1]), new Color(138, 210, 137));
										if (displayLastArray[2] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[2]), new Color(138, 210, 137));
										if (displayLastArray[3] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[3]), new Color(138, 210, 137));
									}
									else*/
									{
										Main.NewText("[" + Main.LocalPlayer.name + "'s last fight stats with " + BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].name + "]", new Color(82, 175, 82));
										Main.NewText(displayLastArray[0], new Color(138, 210, 137));
										Main.NewText(displayLastArray[1], new Color(138, 210, 137));
										Main.NewText(displayLastArray[2], new Color(138, 210, 137));
										Main.NewText(displayLastArray[3], new Color(138, 210, 137));
									}
								}
							}
							else if (Main.mouseLeftRelease && Main.mouseRightRelease) {
								displayRecord = true;
							}
						}
					}
					else {
						if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
							Rectangle exclamCut = new Rectangle(34 * 3, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) Main.hoverItemName = "Click to see your 'Worst' records" +
																	"\nRecords are shown as your best compared to your last fight";
						}
						else {
							Rectangle exclamCut = new Rectangle(0, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) Main.hoverItemName = "Click to see your 'Best' records" +
																	"\nRecords are shown as your worst compared to your last fight";
						}
					}
				}
				else if (BossLogUI.SubPageNum == 2) {
					if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
						Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to view collectibles only";
					}
					else {
						Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to view all other loot";
					}
				}
				else if (BossLogUI.SubPageNum == 3) {

				}
			}
		}
	}

	class BossLogUI : UIState
	{
		public BossAssistButton bosslogbutton;
		
		public BossLogPanel PageOne;
		public BossLogPanel PageTwo;

		public SubpageButton recordButton;
		public SubpageButton spawnButton;
		public SubpageButton lootButton;
		//public SubpageButton collectButton;
		public SubpageButton displayRecordButton;
		public SubpageButton toolTipButton;

		public UIImageButton NextPage;
		public UIImageButton PrevPage;
		public BookUI filterPanel;
		private List<BookUI> filterCheck;
		private List<BookUI> filterCheckMark;
		private List<UIText> filterTypes;

		public BookUI FilterTab;
		public BookUI ToCTab;
		public BookUI CreditsTab;
		public BookUI BossTab;
		public BookUI MiniBossTab;
		public BookUI EventTab;

		public UIList prehardmodeList;
		public UIList hardmodeList;
		public FixedUIScrollbar scrollOne;
		public FixedUIScrollbar scrollTwo;

		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles
		public FixedUIScrollbar pageTwoScroll;

		public static int PageNum = 0; // Selected Boss Page
		public static int SubPageNum = 0; // Selected Topic Tab (Loot, Stats, etc.)
		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool[] AltPage; // Flip between best and worst
		public static bool visible = false;

		public override void OnInitialize() {
			Texture2D bookTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(36 * 1, 0, 34, 38));
			bosslogbutton = new BossAssistButton(bookTexture, "Boss Log");
			bosslogbutton.Id = "OpenUI";
			bosslogbutton.Width.Set(34, 0f);
			bosslogbutton.Height.Set(38, 0f);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnClick += new MouseEvent(OpenBossLog);
			
			AltPage = new bool[]
			{
				false, false, false, false
			};
			
			ToCTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			ToCTab.Height.Pixels = 76;
			ToCTab.Width.Pixels = 32;
			ToCTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			ToCTab.Top.Pixels = (Main.screenHeight / 2) - 250 + 20;
			ToCTab.Id = "TableOfContents_Tab";
			ToCTab.OnClick += new MouseEvent(OpenViaTab);

			BossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			BossTab.Height.Pixels = 76;
			BossTab.Width.Pixels = 32;
			BossTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			BossTab.Top.Pixels = (Main.screenHeight / 2) - 250 + 30 + 76;
			BossTab.Id = "Bosses_Tab";
			BossTab.OnClick += new MouseEvent(OpenViaTab);

			MiniBossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			MiniBossTab.Height.Pixels = 76;
			MiniBossTab.Width.Pixels = 32;
			MiniBossTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			MiniBossTab.Top.Pixels = (Main.screenHeight / 2) - 250 + 40 + (76 * 2);
			MiniBossTab.Id = "MiniBosses_Tab";
			MiniBossTab.OnClick += new MouseEvent(OpenViaTab);

			EventTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			EventTab.Height.Pixels = 76;
			EventTab.Width.Pixels = 32;
			EventTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			EventTab.Top.Pixels = (Main.screenHeight / 2) - 250 + 50 + (76 * 3);
			EventTab.Id = "Events_Tab";
			EventTab.OnClick += new MouseEvent(OpenViaTab);

			CreditsTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			CreditsTab.Height.Pixels = 76;
			CreditsTab.Width.Pixels = 32;
			CreditsTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			CreditsTab.Top.Pixels = (Main.screenHeight / 2) - 250 + 60 + (76 * 4);
			CreditsTab.Id = "Credits_Tab";
			CreditsTab.OnClick += new MouseEvent(OpenViaTab);
			
			PageOne = new BossLogPanel { Id = "PageOne" };
			PageOne.Width.Pixels = 375;
			PageOne.Height.Pixels = 480;
			PageOne.Left.Pixels = (Main.screenWidth / 2) - 400 + 20;
			PageOne.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;

			Texture2D prevTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(0, 0, 22, 22));
			PrevPage = new BossAssistButton(prevTexture, "") { Id = "Previous" };
			PrevPage.Width.Pixels = 14;
			PrevPage.Height.Pixels = 20;
			PrevPage.Left.Pixels = 8;
			PrevPage.Top.Pixels = 416;
			PrevPage.OnClick += new MouseEvent(PageChangerClicked);

			prehardmodeList = new UIList();
			prehardmodeList.Left.Pixels = 4;
			prehardmodeList.Top.Pixels = 44;
			prehardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			prehardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			prehardmodeList.PaddingTop = 5;

			scrollOne = new FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			scrollTwo = new FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Pixels = 50f;
			scrollTwo.Left.Pixels = -28;
			scrollTwo.Height.Set(-24f, 0.75f);
			scrollTwo.HAlign = 1f;

			PageTwo = new BossLogPanel { Id = "PageTwo" };
			PageTwo.Width.Pixels = 375;
			PageTwo.Height.Pixels = 480;
			PageTwo.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;

			pageTwoItemList = new UIList();

			pageTwoScroll = new FixedUIScrollbar();
			
			filterPanel = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Filter"));
			filterPanel.Id = "filterPanel";
			filterPanel.Height.Pixels = 76;
			filterPanel.Width.Pixels = 152;
			filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			filterPanel.Top.Pixels = (Main.screenHeight / 2) - 250 + 30 + 76;
			
			FilterTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			FilterTab.Height.Pixels = 76;
			FilterTab.Width.Pixels = 32;
			FilterTab.Left.Pixels = 0;
			FilterTab.Top.Pixels = 0;
			FilterTab.Id = "Filter_Tab";
			FilterTab.OnClick += new MouseEvent(ToggleFilterPanel);
			filterPanel.Append(FilterTab);

			Texture2D checkCrop = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(0, 0, 22, 20));
			Texture2D checkBox = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(3 * 24, 0, 22, 20));
			filterCheckMark = new List<BookUI>();
			filterCheck = new List<BookUI>();
			filterTypes = new List<UIText>();

			for (int i = 0; i < 3; i++) {
				BookUI newCheck = new BookUI(checkCrop);
				newCheck.Id = "C_" + i;
				filterCheckMark.Add(newCheck);

				BookUI newCheckBox = new BookUI(checkBox);
				newCheckBox.Id = "F_" + i;
				newCheckBox.Top.Pixels = (20 * i) + 5;
				newCheckBox.Left.Pixels = 125;
				newCheckBox.OnClick += new MouseEvent(ChangeFilter);
				newCheckBox.Append(filterCheckMark[i]);
				filterCheck.Add(newCheckBox);

				string type = "Bosses";
				if (i == 1) type = "Mini bosses";
				if (i == 2) type = "Events";
				UIText bosses = new UIText(type, 0.85f);
				bosses.Top.Pixels = 10 + (20 * i);
				bosses.Left.Pixels = 35;
				filterTypes.Add(bosses);
			}

			Texture2D nextTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(24, 0, 22, 22));
			NextPage = new BossAssistButton(nextTexture, "") { Id = "Next" };
			NextPage.Width.Pixels = 14;
			NextPage.Height.Pixels = 20;
			NextPage.Left.Pixels = PageTwo.Width.Pixels - (int)(NextPage.Width.Pixels * 3);
			NextPage.Top.Pixels = 416;
			NextPage.OnClick += new MouseEvent(PageChangerClicked);
			PageTwo.Append(NextPage);

			hardmodeList = new UIList();
			hardmodeList.Left.Pixels = 4;
			hardmodeList.Top.Pixels = 44;
			hardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			hardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			hardmodeList.PaddingTop = 5;

			recordButton = new SubpageButton("Boss Records");
			recordButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			recordButton.Height.Pixels = 25;
			recordButton.Left.Pixels = 0;
			recordButton.Top.Pixels = 15;
			recordButton.OnClick += new MouseEvent(OpenRecord);
			recordButton.OnRightDoubleClick += new MouseEvent(ResetStats);

			spawnButton = new SubpageButton("Spawn Item");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 - 8;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += new MouseEvent(OpenSpawn);

			lootButton = new SubpageButton("Loot Table");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2 - 16;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += new MouseEvent(OpenLoot);

			/*
            collectButton = new SubpageButton("Collectibles");
            collectButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
            collectButton.Height.Pixels = 25;
            collectButton.Left.Pixels = PageTwo.Width.Pixels / 2 - 8;
            collectButton.Top.Pixels = 50;
            collectButton.OnClick += new MouseEvent(OpenCollect);
			*/

			toolTipButton = new SubpageButton("Disclaimer");
			toolTipButton.Width.Pixels = 32;
			toolTipButton.Height.Pixels = 32;
			toolTipButton.Left.Pixels = PageTwo.Width.Pixels - toolTipButton.Width.Pixels - 30;
			toolTipButton.Top.Pixels = 100;
			toolTipButton.OnClick += new MouseEvent(SwapRecordPage);

			displayRecordButton = new SubpageButton("Display Records");
			displayRecordButton.Width.Pixels = 32;
			displayRecordButton.Height.Pixels = 32;
			displayRecordButton.Left.Pixels = PageTwo.Width.Pixels - displayRecordButton.Width.Pixels - 30;
			displayRecordButton.Top.Pixels = 128;

			Append(ToCTab);
			Append(CreditsTab);
			Append(filterPanel);
			Append(BossTab);
			Append(MiniBossTab);
			Append(EventTab);

			Append(PageOne);
			Append(PageTwo);
		}

		public override void Update(GameTime gameTime) {
			visible = Main.playerInventory;
			if (!visible) RemoveChild(bosslogbutton);
			else if (!HasChild(bosslogbutton)) Append(bosslogbutton);

			if (BossChecklist.ToggleBossLog.JustPressed) {
				PageNum = -1;
				SubPageNum = 0;
				BossLogPanel.visible = true;
				BookUI.visible = true;
				UpdateTableofContents();
			}
			else if (BossLogPanel.visible && BookUI.visible) {
				// Player opens their inventory to close the UI
				if (Main.LocalPlayer.controlInv || Main.mouseItem.type != 0) {
					BossLogPanel.visible = false;
					BookUI.visible = false;
					Main.playerInventory = true;
					filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
					foreach (UIText uitext in filterTypes) {
						filterPanel.RemoveChild(uitext);
					}
					filterPanel.Width.Pixels = 32;
				}
			}

			// We reset the position of the button to make sure it updates with the screen res
			bosslogbutton.Left.Pixels = Main.screenWidth - bosslogbutton.Width.Pixels - 190;
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			PageOne.Left.Pixels = (Main.screenWidth / 2) - 400 + 20;
			PageOne.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;
			PageTwo.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;
			
			// Updating tabs to proper positions
			if (PageNum == -2) CreditsTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			else CreditsTab.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - 16;
			if (PageNum >= FindNext(BossChecklistType.Boss) || PageNum == -2) BossTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			else BossTab.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - 16;
			if (PageNum >= FindNext(BossChecklistType.MiniBoss) || PageNum == -2) MiniBossTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			else MiniBossTab.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - 16;
			if (PageNum >= FindNext(BossChecklistType.Event) || PageNum == -2) EventTab.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			else EventTab.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - 16;
			
			if (PageNum != -1) {
				filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				filterPanel.Width.Pixels = 32;
			}

			if (filterPanel.HasChild(filterCheck[0])) {
				Texture2D check = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(0, 0, 22, 20));
				Texture2D circle = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(48, 0, 22, 20));
				Texture2D ex = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(24, 0, 22, 20));

				if (BossChecklist.BossLogConfig.FilterBosses == "Show") filterCheckMark[0].SetImage(check);
				else filterCheckMark[0].SetImage(circle);

				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") filterCheckMark[1].SetImage(check);
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") filterCheckMark[1].SetImage(ex);
				else filterCheckMark[1].SetImage(circle);

				if (BossChecklist.BossLogConfig.FilterEvents == "Show") filterCheckMark[2].SetImage(check);
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") filterCheckMark[2].SetImage(ex);
				else filterCheckMark[2].SetImage(circle);
			}

			base.Update(gameTime);
		}

		private void OpenBossLog(UIMouseEvent evt, UIElement listeningElement) {
			PageNum = -1;
			SubPageNum = 0;
			BossLogPanel.visible = true;
			BookUI.visible = true; filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
			foreach (UIText uitext in filterTypes) {
				filterPanel.RemoveChild(uitext);
			}
			UpdateTableofContents();
		}

		public void ToggleFilterPanel(UIMouseEvent evt, UIElement listeningElement) {
			if (filterPanel.Left.Pixels != (Main.screenWidth / 2) - 400 - 16 - 120) {
				filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16 - 120;
				filterPanel.Width.Pixels = 152;
				foreach (BookUI uiimage in filterCheck) {
					filterPanel.Append(uiimage);
				}
				foreach (UIText uitext in filterTypes) {
					filterPanel.Append(uitext);
				}
			}
			else {
				filterPanel.Left.Pixels = (Main.screenWidth / 2) - 400 - 16;
				foreach (BookUI uiimage in filterCheck) {
					filterPanel.RemoveChild(uiimage);
				}
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				filterPanel.Width.Pixels = 32;
			}
		}

		public void ChangeFilter(UIMouseEvent evt, UIElement listeningElement) {
			string rowID = listeningElement.Id.Substring(2, 1);
			if (rowID == "0") {
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") BossChecklist.BossLogConfig.FilterBosses = "Hide when completed";
				else BossChecklist.BossLogConfig.FilterBosses = "Show";
			}
			if (rowID == "1") {
				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") BossChecklist.BossLogConfig.FilterMiniBosses = "Hide when completed";
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide when completed") BossChecklist.BossLogConfig.FilterMiniBosses = "Hide";
				else BossChecklist.BossLogConfig.FilterMiniBosses = "Show";
			}
			if (rowID == "2") {
				if (BossChecklist.BossLogConfig.FilterEvents == "Show") BossChecklist.BossLogConfig.FilterEvents = "Hide when completed";
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide when completed") BossChecklist.BossLogConfig.FilterEvents = "Hide";
				else BossChecklist.BossLogConfig.FilterEvents = "Show";
			}
			UpdateTableofContents();
		}
		
		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement.Id == "Bosses_Tab") PageNum = FindNext(BossChecklistType.Boss);
			else if (listeningElement.Id == "MiniBosses_Tab") PageNum = FindNext(BossChecklistType.MiniBoss);
			else if (listeningElement.Id == "Events_Tab") PageNum = FindNext(BossChecklistType.Event);
			else if (listeningElement.Id == "Credits_Tab") UpdateCredits();
			else UpdateTableofContents();

			if (PageNum >= 0) ResetBothPages();
		}

		private void ResetStats(UIMouseEvent evt, UIElement listeningElement) {
			// Since it only applies to Boss Icons, the page check is unnecessary
			if (BossChecklist.BossLogConfig.ResetRecordsBool) {
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.fightTime = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.fightTime2 = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.dodgeTime = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.totalDodges = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.totalDodges2 = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.kills = 0;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.deaths = 0;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.brink = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.brinkPercent = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.brink2 = -1;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat.brinkPercent2 = -1;
			}
			OpenRecord(evt, listeningElement);
		}

		private void ChangeSpawnItem(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement.Id == "NextItem") {
				RecipePageNum++;
				RecipeShown = 0;
			}
			else if (listeningElement.Id == "PrevItem") {
				RecipePageNum--;
				RecipeShown = 0;
			}
			else if (listeningElement.Id.Contains("CycleItem")) {
				int index = listeningElement.Id.IndexOf('_');
				// if (index != -1) Main.NewText(listeningElement.Id.Substring(index + 1));
				if (RecipeShown == Convert.ToInt32(listeningElement.Id.Substring(index + 1)) - 1) RecipeShown = 0;
				else RecipeShown++;
			}
			OpenSpawn(evt, listeningElement);
		}

		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			pageTwoItemList.Clear();
			prehardmodeList.Clear();
			hardmodeList.Clear();
			PageOne.RemoveChild(scrollOne);
			PageTwo.RemoveChild(scrollTwo);
			PageTwo.RemoveChild(pageTwoScroll);
			RecipeShown = 0;
			RecipePageNum = 0;
			BossLogPanel.validItems = null;

			if (listeningElement.Id == "Previous") {
				if (PageNum > -1) PageNum--;
				else if (PageNum == -2) PageNum = BossChecklist.bossTracker.SortedBosses.Count - 1;
				if (PageNum == -1) UpdateTableofContents();
				else {
					if (SubPageNum == 0) OpenRecord(evt, listeningElement);
					else if (SubPageNum == 1) OpenSpawn(evt, listeningElement);
					else if (SubPageNum == 2) {
						if (!AltPage[SubPageNum]) OpenLoot(evt, listeningElement);
						else OpenCollect(evt, listeningElement);
					}
					//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
				}
			}
			else if (listeningElement.Id == "Next") {
				if (PageNum != BossChecklist.bossTracker.SortedBosses.Count - 1) PageNum++;
				else UpdateCredits();

				if (PageNum != -2) {
					if (SubPageNum == 0) OpenRecord(evt, listeningElement);
					else if (SubPageNum == 1) OpenSpawn(evt, listeningElement);
					else if (SubPageNum == 2) {
						if (!AltPage[SubPageNum]) OpenLoot(evt, listeningElement);
						else OpenCollect(evt, listeningElement);
					}
					//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
				}
			}
			else if (listeningElement.Id == "TableOfContents") UpdateTableofContents();
			else if (listeningElement.Id == "Credits") UpdateCredits();
			ResetPageButtons();
		}

		private void OpenRecord(UIMouseEvent evt, UIElement listeningElement) {
			SubPageNum = 0;
			ResetBothPages();
			if (PageNum < 0) return;
		}

		private void OpenSpawn(UIMouseEvent evt, UIElement listeningElement) {
			SubPageNum = 1;
			int TotalRecipes = 0;
			ResetBothPages();
			if (PageNum < 0) return;
			if (BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count < 1) return;
			FittedTextPanel info = new FittedTextPanel(BossChecklist.bossTracker.SortedBosses[PageNum].info);
			info.Top.Pixels = 75;
			info.Width.Pixels = 300;
			PageTwo.Append(info);

			List<Item> ingredients = new List<Item>();
			List<int> requiredTiles = new List<int>();
			string recipeMod = "Vanilla";
			//List<Recipe> recipes = Main.recipe.ToList();
			Item spawn = new Item();
			if (BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem[RecipePageNum] != 0) {
				RecipeFinder finder = new RecipeFinder();
				finder.SetResult(BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem[RecipePageNum]);

				foreach (Recipe recipe in finder.SearchRecipes()) {
					if (TotalRecipes == RecipeShown) {
						foreach (Item item in recipe.requiredItem) ingredients.Add(item);
						foreach (int tile in recipe.requiredTile) {
							if (tile != -1 && tile != 0) requiredTiles.Add(tile);
						}
						if (recipe is ModRecipe modRecipe) {
							recipeMod = modRecipe.mod.DisplayName;
						}
					}
					TotalRecipes++;
				}
				spawn.SetDefaults(BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem[RecipePageNum]);

				LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
				spawnItemSlot.Height.Pixels = 50;
				spawnItemSlot.Width.Pixels = 50;
				spawnItemSlot.Top.Pixels = 225;
				spawnItemSlot.Left.Pixels = 33 + (56 * 2);
				PageTwo.Append(spawnItemSlot);

				int row = 0;
				int col = 0;
				for (int k = 0; k < ingredients.Count; k++) {
					LogItemSlot ingList = new LogItemSlot(ingredients[k], false, ingredients[k].HoverName, ItemSlot.Context.GuideItem);
					ingList.Height.Pixels = 50;
					ingList.Width.Pixels = 50;
					ingList.Top.Pixels = 225 + (56 * (row + 1));
					ingList.Left.Pixels = 33 + (56 * col);
					PageTwo.Append(ingList);
					col++;
					if (k == 4 || k == 9) {
						if (ingList.item.type == 0) break;
						col = 0;
						row++;
					}
				}

				Item craft = new Item();
				if (ingredients.Count > 0 && requiredTiles.Count == 0) {
					craft.SetDefaults(ItemID.PowerGlove);

					LogItemSlot craftItem = new LogItemSlot(craft, false, "By Hand", ItemSlot.Context.EquipArmorVanity);
					craftItem.Height.Pixels = 50;
					craftItem.Width.Pixels = 50;
					craftItem.Top.Pixels = 225 + (56 * (row + 2));
					craftItem.Left.Pixels = 33;
					PageTwo.Append(craftItem);
				}
				else if (requiredTiles.Count > 0) {
					for (int l = 0; l < requiredTiles.Count; l++) {
						if (requiredTiles[l] == -1) break; // Prevents extra empty slots from being created
						LogItemSlot tileList;
						if (requiredTiles[l] == 26) {
							craft.SetDefaults(0);
							string altarType;
							if (WorldGen.crimson) altarType = "Crimson Altar";
							else altarType = "Demon Altar";
							tileList = new LogItemSlot(craft, false, altarType, ItemSlot.Context.EquipArmorVanity);
						}
						else {
							for (int m = 0; m < ItemLoader.ItemCount; m++) {
								craft.SetDefaults(m);
								if (craft.createTile == requiredTiles[l]) break;
							}
							tileList = new LogItemSlot(craft, false, craft.HoverName, ItemSlot.Context.EquipArmorVanity);
						}
						tileList.Height.Pixels = 50;
						tileList.Width.Pixels = 50;
						tileList.Top.Pixels = 225 + (56 * (row + 2));
						tileList.Left.Pixels = 33 + (56 * l);
						PageTwo.Append(tileList);
						if (requiredTiles[l] == 26) {
							Texture2D altarTexture = BossChecklist.instance.GetTexture("Resources/Demon_Altar");
							if (WorldGen.crimson) altarTexture = BossChecklist.instance.GetTexture("Resources/Crimson_Altar");
							UIImage altar = new UIImage(altarTexture);
							altar.Height.Pixels = 50;
							altar.Width.Pixels = 50;
							altar.ImageScale = 0.75f;
							altar.Top.Pixels = tileList.Top.Pixels + tileList.Height.Pixels / 3 * 0.45f;
							altar.Left.Pixels = tileList.Left.Pixels + tileList.Width.Pixels / 3 * 0.15f;
							PageTwo.Append(altar);
						}
					}
				}

				if (RecipePageNum > 0) {
					Texture2D prevTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(0, 0, 22, 22));
					BossAssistButton PrevItem = new BossAssistButton(prevTexture, "");
					PrevItem.Id = "PrevItem";
					PrevItem.Top.Pixels = 240;
					PrevItem.Left.Pixels = 125;
					PrevItem.Width.Pixels = 14;
					PrevItem.Height.Pixels = 20;
					PrevItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(PrevItem);
				}

				if (RecipePageNum < BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count - 1) {
					Texture2D nextTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(24, 0, 22, 22));
					BossAssistButton NextItem = new BossAssistButton(nextTexture, "");
					NextItem.Id = "NextItem";
					NextItem.Top.Pixels = 240;
					NextItem.Left.Pixels = 203;
					NextItem.Width.Pixels = 14;
					NextItem.Height.Pixels = 20;
					NextItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(NextItem);
				}

				if (TotalRecipes > 1) {
					Texture2D credTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(72, 0, 22, 22));
					BossAssistButton CycleItem = new BossAssistButton(credTexture, "Cycle Alt Recipes");
					CycleItem.Id = "CycleItem_" + TotalRecipes;
					CycleItem.Top.Pixels = 354;
					CycleItem.Left.Pixels = 274;
					CycleItem.Width.Pixels = 22;
					CycleItem.Height.Pixels = 22;
					CycleItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(CycleItem);
				}

				string recipeMessage = "This item is not craftable.";
				if (TotalRecipes > 0) {
					recipeMessage = "Provided by: " + recipeMod;
				}

				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = 0;
				ModdedRecipe.Top.Pixels = 400;
				PageTwo.Append(ModdedRecipe);
			}
		}

		private void OpenLoot(UIMouseEvent evt, UIElement listeningElement) {
			SubPageNum = 2;
			ResetBothPages();
			if (PageNum < 0) return;
			int row = 0;
			int col = 0;

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 125;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 125 - 80;

			pageTwoScroll.SetView(10f, 1000f);
			pageTwoScroll.Top.Pixels = 125;
			pageTwoScroll.Left.Pixels = -18;
			pageTwoScroll.Height.Set(-88f, 0.75f);
			pageTwoScroll.HAlign = 1f;

			pageTwoItemList.Clear();
			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];
			LootRow newRow = new LootRow(0) { Id = "Loot0" };
			for (int i = 0; i < shortcut.loot.Count; i++) {
				Item expertItem = new Item();
				expertItem.SetDefaults(shortcut.loot[i]);
				if (!expertItem.expert || expertItem.Name.Contains("Treasure Bag")) continue;
				else {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(expertItem, Collection.loot.Any(x => x.type == expertItem.type), expertItem.Name, ItemSlot.Context.ShopItem);
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Id = "loot_" + i;
					lootTable.Left.Pixels = (col * 56);
					newRow.Append(lootTable);
					col++;
					if (col == 6 || i == shortcut.loot.Count - 1) {
						col = 0;
						row++;
						pageTwoItemList.Add(newRow);
						newRow = new LootRow(row) { Id = "Loot" + row };
					}
				}
			}
			for (int i = 0; i < shortcut.loot.Count; i++) {
				Item loot = new Item();
				loot.SetDefaults(shortcut.loot[i]);

				if (loot.expert || loot.Name.Contains("Treasure Bag")) continue;
				else {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(loot, Collection.loot.Any(x => x.type == loot.type), loot.Name, ItemSlot.Context.TrashItem);
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Id = "loot_" + i;
					lootTable.Left.Pixels = (col * 56);
					newRow.Append(lootTable);
					col++;
					if (col == 6 || i == shortcut.loot.Count - 1) {
						col = 0;
						row++;
						pageTwoItemList.Add(newRow);
						newRow = new LootRow(row) { Id = "Loot" + row };
					}
				}
			}
			if (row > 5) PageTwo.Append(pageTwoScroll);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(pageTwoScroll);
		}

		private void OpenCollect(UIMouseEvent evt, UIElement listeningElement) {
			SubPageNum = 2;
			ResetBothPages();
			if (PageNum < 0) return;
			int row = 0;
			int col = 0;

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 235;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 240 - 75;

			pageTwoScroll.SetView(10f, 1000f);
			pageTwoScroll.Top.Pixels = 250;
			pageTwoScroll.Left.Pixels = -18;
			pageTwoScroll.Height.Set(-220f, 0.75f);
			pageTwoScroll.HAlign = 1f;

			pageTwoItemList.Clear();
			LootRow newRow = new LootRow(0) { Id = "Collect0" };
			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];
			for (int i = 0; i < shortcut.collection.Count; i++) {
				if (shortcut.collection[i] == -1) continue;
				Item collectible = new Item();
				collectible.SetDefaults(shortcut.collection[i]);

				BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];
				LogItemSlot collectionTable = new LogItemSlot(collectible, Collection.collectibles.Any(x => x.type == collectible.type), collectible.Name);
				collectionTable.Height.Pixels = 50;
				collectionTable.Width.Pixels = 50;
				collectionTable.Id = "collect_" + i;
				collectionTable.Left.Pixels = (56 * (col));
				newRow.Append(collectionTable);
				col++;
				if (col == 6 || i == shortcut.collection.Count - 1) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row) { Id = "Collect" + row };
				}
			}
			if (row > 3) PageTwo.Append(pageTwoScroll);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(pageTwoScroll);
		}

		public void UpdateTableofContents() {
			PageNum = -1;
			ResetBothPages();
			int nextCheck = 0;
			bool nextCheckBool = false;
			prehardmodeList.Clear();
			hardmodeList.Clear();

			List<BossInfo> copiedList = new List<BossInfo>(BossChecklist.bossTracker.SortedBosses.Count);
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				copiedList.Add(boss);
			}
			copiedList.Sort((x, y) => x.progression.CompareTo(y.progression));

			/// Readd Evil Opposites, instead fade out/blacken the non valid type

			for (int i = 0; i < copiedList.Count; i++) {
				if (!copiedList[i].downed()) nextCheck++;
				if (nextCheck == 1) nextCheckBool = true;

				TableOfContents next = new TableOfContents(copiedList[i].progression, copiedList[i].name, nextCheckBool);
				nextCheckBool = false;

				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;
				BossChecklistType type = copiedList[i].type;

				if (copiedList[i].progression <= 6f) {
					if (copiedList[i].downed()) {
						if ((mbFilter == "Show" && type == BossChecklistType.MiniBoss) || (eFilter == "Show" && type == BossChecklistType.Event) || (type == BossChecklistType.Boss && bFilter != "Hide when completed")) {
							next.PaddingTop = 5;
							next.PaddingLeft = 22;
							next.TextColor = Color.LawnGreen;
							next.Id = i.ToString();
							next.OnClick += new MouseEvent(UpdatePage);
							prehardmodeList.Add(next);
						}
					}
					else if (!copiedList[i].downed()) {
						if ((mbFilter != "Hide" && type == BossChecklistType.MiniBoss) || (eFilter != "Hide" && type == BossChecklistType.Event) || type == BossChecklistType.Boss) {
							nextCheck++;
							next.PaddingTop = 5;
							next.PaddingLeft = 22;
							next.TextColor = Color.IndianRed;
							next.Id = i.ToString();
							next.OnClick += new MouseEvent(UpdatePage);
							prehardmodeList.Add(next);
						}
					}
				}
				else {
					if (copiedList[i].downed()) {
						if ((mbFilter == "Show" && type == BossChecklistType.MiniBoss) || (eFilter == "Show" && type == BossChecklistType.Event) || (type == BossChecklistType.Boss && bFilter != "Hide when completed")) {
							next.PaddingTop = 5;
							next.PaddingLeft = 22;
							next.TextColor = Color.LawnGreen;
							next.Id = i.ToString();
							next.OnClick += new MouseEvent(UpdatePage);
							hardmodeList.Add(next);
						}
					}
					else if (!copiedList[i].downed()) {
						if ((mbFilter != "Hide" && type == BossChecklistType.MiniBoss) || (eFilter != "Hide" && type == BossChecklistType.Event) || type == BossChecklistType.Boss) {
							nextCheck++;
							next.PaddingTop = 5;
							next.PaddingLeft = 22;
							next.TextColor = Color.IndianRed;
							next.Id = i.ToString();
							next.OnClick += new MouseEvent(UpdatePage);
							hardmodeList.Add(next);
						}
					}
				}
			}

			if (prehardmodeList.Count > 14) PageOne.Append(scrollOne);
			PageOne.Append(prehardmodeList);
			prehardmodeList.SetScrollbar(scrollOne);
			if (hardmodeList.Count > 14) PageTwo.Append(scrollTwo);
			PageTwo.Append(hardmodeList);
			hardmodeList.SetScrollbar(scrollTwo);
		}

		private void UpdateCredits() {
			PageNum = -2;
			ResetBothPages();
			List<string> optedMods = new List<string>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (boss.modSource != "Vanilla" && boss.modSource != "Unknown") // TODO: find a way to get source mod from old integrations without necessitating mod updates.
				{
					string sourceDisplayName = boss.SourceDisplayName;
					if (!optedMods.Contains(sourceDisplayName)) {
						optedMods.Add(sourceDisplayName);
					}
				}
			}

			pageTwoItemList.Left.Pixels = 15;
			pageTwoItemList.Top.Pixels = 75;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 66;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 75 - 80;

			pageTwoScroll.SetView(10f, 1000f);
			pageTwoScroll.Top.Pixels = 90;
			pageTwoScroll.Left.Pixels = -24;
			pageTwoScroll.Height.Set(-60f, 0.75f);
			pageTwoScroll.HAlign = 1f;

			pageTwoItemList.Clear();

			if (optedMods.Count != 0) {
				foreach (string mod in optedMods) {
					UIText modListed = new UIText("●" + mod, 0.85f) {
						PaddingTop = 8,
						PaddingLeft = 5
					};
					pageTwoItemList.Add(modListed);
				}
			}
			else // No mods are using the Log
			{
				pageTwoItemList.Left.Pixels = 0;
				pageTwoItemList.Top.Pixels = 15;
				pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels;
				pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 75 - 80;

				string noMods = "None of your loaded mods have added \npages to the Boss Log. If you want your \nfavorite mods to be included, suggest \nadding their own boss pages to the mod's \ndiscord or forums page!";
				UIText noModListed = new UIText(noMods) {
					TextColor = Color.LightBlue,
					PaddingTop = 8,
					PaddingLeft = 5
				};
				pageTwoItemList.Add(noModListed);
			}
			if (optedMods.Count > 11) PageTwo.Append(pageTwoScroll);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(pageTwoScroll);
		}

		private void UpdatePage(UIMouseEvent evt, UIElement listeningElement) {
			PageNum = Convert.ToInt32(listeningElement.Id);
			PageOne.RemoveAllChildren();
			ResetPageButtons();
			if (SubPageNum == 0) OpenRecord(evt, listeningElement);
			else if (SubPageNum == 1) OpenSpawn(evt, listeningElement);
			else if (SubPageNum == 2) {
				if (!AltPage[SubPageNum]) OpenLoot(evt, listeningElement);
				else OpenCollect(evt, listeningElement);
			}
			//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
		}

		private void ResetBothPages() {
			PageOne.RemoveAllChildren();
			PageTwo.RemoveAllChildren();
			ResetPageButtons();
			if (PageNum >= 0) {
				if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource != "Unknown") {
					PageTwo.Append(spawnButton);
					PageTwo.Append(lootButton);
					//PageTwo.Append(collectButton);
					PageTwo.Append(recordButton);
				}
				else {
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 3;
					PageTwo.Append(brokenPanel);

					FittedTextPanel brokenDisplay = new FittedTextPanel("Records, spawn items, and the loot table are disabled for this page. The mod has either not submitted enough info for a page or has it improperly set up.");
					brokenDisplay.Height.Pixels = 200;
					brokenDisplay.Width.Pixels = 340;
					brokenDisplay.Top.Pixels = -12;
					brokenDisplay.Left.Pixels = -15;
					brokenPanel.Append(brokenDisplay);
				}

				if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource == "Unknown" && BossChecklist.bossTracker.SortedBosses[PageNum].npcIDs.Count == 0) {
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 14;
					PageOne.Append(brokenPanel);

					FittedTextPanel brokenDisplay = new FittedTextPanel("The display for this page is unavailable. The mod has either not submitted a page or has it improperly set up.");
					brokenDisplay.Height.Pixels = 200;
					brokenDisplay.Width.Pixels = 340;
					brokenDisplay.Top.Pixels = 0;
					brokenDisplay.Left.Pixels = -15;
					brokenPanel.Append(brokenDisplay);
				}
			}
		}

		private void ResetPageButtons() {
			PageOne.RemoveChild(PrevPage);
			PageTwo.RemoveChild(NextPage);
			PageTwo.RemoveChild(toolTipButton);
			PageTwo.RemoveChild(displayRecordButton);

			if (PageNum == -2) PageOne.Append(PrevPage);
			else if (PageNum == -1) PageTwo.Append(NextPage);
			else {
				if (SubPageNum != 1 && BossChecklist.bossTracker.SortedBosses[PageNum].modSource != "Unknown") {
					toolTipButton = new SubpageButton("");
					toolTipButton.Width.Pixels = 32;
					toolTipButton.Height.Pixels = 32;
					toolTipButton.Left.Pixels = PageTwo.Width.Pixels - toolTipButton.Width.Pixels - 30;
					toolTipButton.Top.Pixels = 86;
					toolTipButton.OnClick += new MouseEvent(SwapRecordPage);
					PageTwo.Append(toolTipButton);
					if (SubPageNum == 0) {
						displayRecordButton = new SubpageButton("");
						displayRecordButton.Width.Pixels = 32;
						displayRecordButton.Height.Pixels = 32;
						displayRecordButton.Left.Pixels = PageTwo.Width.Pixels - displayRecordButton.Width.Pixels - 30;
						displayRecordButton.Top.Pixels = 128;
						displayRecordButton.Id = "Display Records";
						PageTwo.Append(displayRecordButton);
					}
				}

				PageTwo.Append(NextPage);
				PageOne.Append(PrevPage);
			}
		}

		private void SwapRecordPage(UIMouseEvent evt, UIElement listeningElement) {
			if (SubPageNum == 2) {
				if (AltPage[SubPageNum]) OpenLoot(evt, listeningElement);
				else OpenCollect(evt, listeningElement);
			}
			AltPage[SubPageNum] = !AltPage[SubPageNum];
		}

		public int FindNext(BossChecklistType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.downed() && x.type == entryType);

		public static Texture2D GetBossHead(int boss) => NPCID.Sets.BossHeadTextures[boss] != -1 ? Main.npcHeadBossTexture[NPCID.Sets.BossHeadTextures[boss]] : Main.npcHeadTexture[0];

		public static int[] GetVanillaBossTrophyPos(int item) {
			if (item == ItemID.EyeofCthulhuTrophy) return new int[] { 0, 0 }; //Position on tile table, times 3
			else if (item == ItemID.EaterofWorldsTrophy) return new int[] { 3, 0 };
			else if (item == ItemID.BrainofCthulhuTrophy) return new int[] { 6, 0 };
			else if (item == ItemID.SkeletronTrophy) return new int[] { 9, 0 };
			else if (item == ItemID.QueenBeeTrophy) return new int[] { 12, 0 };
			else if (item == ItemID.WallofFleshTrophy) return new int[] { 15, 0 };
			else if (item == ItemID.DestroyerTrophy) return new int[] { 18, 0 };
			else if (item == ItemID.SkeletronPrimeTrophy) return new int[] { 21, 0 };
			else if (item == ItemID.RetinazerTrophy) return new int[] { 24, 0 };
			else if (item == ItemID.SpazmatismTrophy) return new int[] { 27, 0 };
			else if (item == ItemID.PlanteraTrophy) return new int[] { 30, 0 };
			else if (item == ItemID.GolemTrophy) return new int[] { 33, 0 };
			else if (item == ItemID.KingSlimeTrophy) return new int[] { 54, 3 };
			else if (item == ItemID.DukeFishronTrophy) return new int[] { 57, 3 };
			else if (item == ItemID.AncientCultistTrophy) return new int[] { 60, 3 };
			else if (item == ItemID.MoonLordTrophy) return new int[] { 69, 3 };
			else if (item == ItemID.BossTrophyBetsy) return new int[] { 75, 3 };
			return new int[] { 0, 0 }; // Default is Eye of Cthulhu
		}

		public static Texture2D CropTexture(Texture2D texture, Rectangle snippet) {
			Texture2D croppedTexture = new Texture2D(Main.graphics.GraphicsDevice, snippet.Width, snippet.Height);
			Color[] data = new Color[snippet.Width * snippet.Height];
			texture.GetData(0, snippet, data, 0, data.Length);
			croppedTexture.SetData(data);
			return croppedTexture;
		}
	}
}
