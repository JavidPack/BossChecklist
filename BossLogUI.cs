using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

/* Patch Notes:
 *   + Added hidden mask feature. Bosses dont show what they look like until defeated
 *   + Upgraded the spawn item tab to contain multiple items and all their recipes (You do not have to change your call, it still works with a singular int)
 *   + Records now work in Multiplayer!
 *   + Fixed limb messages giving the wrong name in MultiPlayer
 *   + Boss Log now hides itself when if you are dead. Thats where the respawn timer should be.
 *   + Boss Log Pages now supports multiple boss types and now checks for all boss types to be inactive for records
 *   + Bosses with multiple IDs now show all corresponding head icons within the Boss Log
 *   + Boss Collection display now shows all possible trophies/masks/music boxes for ALL bosses now
 */

namespace BossChecklist
{
	internal class BossAssistButton : UIImageButton
	{
		internal string buttonType;
		internal Texture2D texture;
		internal int cycleFrame = 0;
		internal bool slowDown = true;
		private Vector2 offset;
		public static bool dragging;

		public BossAssistButton(Texture2D texture, string type) : base(texture) {
			buttonType = type;
			this.texture = texture;
		}

		private void DragStart(UIMouseEvent evt) {
			var dimensions = GetDimensions().ToRectangle();
			offset = new Vector2(evt.MousePosition.X - dimensions.Left, evt.MousePosition.Y - dimensions.Top);
			dragging = true;
		}

		private void DragEnd(UIMouseEvent evt) {
			Vector2 end = evt.MousePosition;
			dragging = false;

			Left.Set(end.X - Main.screenWidth - offset.X, 1f);
			Top.Set(end.Y - Main.screenHeight - offset.Y, 1f);

			Recalculate();
			BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
		}

		public override void RightMouseDown(UIMouseEvent evt) {
			base.RightMouseDown(evt);
			if (Id == "OpenUI") DragStart(evt);
		}

		public override void RightMouseUp(UIMouseEvent evt) {
			base.RightMouseUp(evt);
			if (Id == "OpenUI") DragEnd(evt);
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (Id != "OpenUI") return;
			if (ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;

			if (dragging) {
				Left.Set(Main.mouseX - Main.screenWidth - offset.X, 1f);
				Top.Set(Main.mouseY - Main.screenHeight - offset.Y, 1f);
				Recalculate();
			}
			else {
				Vector2 configVec = BossChecklist.BossLogConfig.BossLogPos;
				Left.Set(configVec.X, 1f);
				Top.Set(configVec.Y, 1f);
			}

			var parentSpace = Parent.GetDimensions().ToRectangle();
			if (!GetDimensions().ToRectangle().Contains(parentSpace)) {
				Left.Pixels = Utils.Clamp(Left.Pixels, -parentSpace.Right, -Width.Pixels);
				Top.Pixels = Utils.Clamp(Top.Pixels, -parentSpace.Bottom,  -Height.Pixels);
				Recalculate();
				BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
			}
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
				if (!IsMouseHovering && !dragging) {
					source = new Rectangle(36 * 2, 0, 34, 38);
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
				}
				spriteBatch.Draw(bookCover, innerDimensions.ToRectangle(), source, coverColor);

				// Border Selection
				PlayerAssist myPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				Texture2D border = BossLogUI.CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(36, 0, 34, 38));
				if (!myPlayer.hasOpenedTheBossLog) spriteBatch.Draw(border, innerDimensions.ToRectangle(), Main.DiscoColor);
				else if (BossChecklist.DebugConfig.RecordingDisabled) spriteBatch.Draw(border, innerDimensions.ToRectangle(), Color.IndianRed);
				
				if (myPlayer.hasNewRecord) {
					slowDown = !slowDown;
					if (slowDown) cycleFrame++;
					if (cycleFrame >= 19) cycleFrame = 0;

					Texture2D bookBorder = BossChecklist.instance.GetTexture("Resources/LogUI_ButtonBorder");
					source = new Rectangle(0, 40 * cycleFrame, 34, 38);
					spriteBatch.Draw(bookBorder, innerDimensions.ToRectangle(), source, Color.White);
				}

				// Drawing the entire book while dragging if the mouse happens to go off screen/out of window
				if (dragging) spriteBatch.Draw(texture, innerDimensions.ToRectangle(), Color.White);
			}
			
			if (IsMouseHovering && !dragging) {
				BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
				if (!Id.Contains("CycleItem")) DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, buttonType, pos, Color.White);
				else Main.hoverItemName = buttonType;
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
			Rectangle rectangle = GetInnerDimensions().ToRectangle();
			var backup = Main.inventoryBack6Texture;
			var backup2 = Main.inventoryBack7Texture;

			Main.inventoryBack6Texture = Main.inventoryBack15Texture;

			BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];

			if (Id.Contains("loot_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
				Main.inventoryBack6Texture = BossChecklist.instance.GetTexture("Resources/Extra_ExpertCollected");
			}

			if (Id.Contains("collect_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
			}
			
			// Prevents empty slots from being drawn
			if (item.type != 0 || hoverText == "Demon Altar" || hoverText == "Crimson Altar" || Id.Contains("ingredient_"))
				ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());

			Main.inventoryBack6Texture = backup;
			Main.inventoryBack7Texture = backup2;

			if (hoverText == "Crimson Altar" || hoverText == "Demon Altar") {
				Main.instance.LoadTiles(TileID.DemonAltar);
				int offsetX = 0;
				int offsetY = 0;
				int offsetSrc = 0;
				if (hoverText == "Crimson Altar") offsetSrc = 3;
				for (int i = 0; i < 6; i++) {
					Vector2 pos = new Vector2(rectangle.X + (rectangle.Width / 2) - (24 * 0.64f) + (16 * offsetX * 0.64f) - 3, rectangle.Y + (rectangle.Height / 2) - (16 * 0.64f) + (16 * offsetY * 0.64f) - 3);
					Rectangle src = new Rectangle((offsetX + offsetSrc) * 18, offsetY * 18, 16, 16 + (offsetY * 2));
					spriteBatch.Draw(Main.tileTexture[TileID.DemonAltar], pos, src, Color.White, 0f, Vector2.Zero, 0.64f, SpriteEffects.None, 0f);

					offsetX++;
					if (offsetX == 3) {
						offsetX = 0;
						offsetY++;
					}
				}
			}

			Texture2D checkMark = BossChecklist.instance.GetTexture("Resources/LogUI_Checks");
			Rectangle rect = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 22, 20);
			Rectangle source = new Rectangle(0, 0, 22, 20);
			if (item.type != 0 && (Id.Contains("loot_") || Id.Contains("collect_"))) {
				if (hasItem) spriteBatch.Draw(checkMark, rect, source, Color.White); // hasItem first priority
				else if (!Main.expertMode && (item.expert || item.expertOnly)) {
					source = new Rectangle(24, 0, 22, 20);
					spriteBatch.Draw(checkMark, rect, source, Color.White);
				}
			}

			if (IsMouseHovering) {
				if (hoverText != "By Hand") {
					if (item.type != 0 && (Id.Contains("loot_") || Id.Contains("collect_")) && !Main.expertMode && (item.expert || item.expertOnly)) {
						Main.hoverItemName = "This item is only obtainable in Expert Mode";
					}
					else if (item.type != 0 || hoverText != "") {
						Color newcolor = ItemRarity.GetColor(item.rare);
						float num3 = (float)(int)Main.mouseTextColor / 255f;
						if (item.expert || item.expertOnly) {
							newcolor = new Color((byte)(Main.DiscoR * num3), (byte)(Main.DiscoG * num3), (byte)(Main.DiscoB * num3), Main.mouseTextColor);
						}
						Main.HoverItem = item;
						Main.hoverItemName = $"[c/{newcolor.Hex3()}: {hoverText}]";
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
		public static int itemTimer = 300;
		public static int[] itemShown;
		public static List<int>[] validItems;
		public static int headNum = -1;

		public override void Draw(SpriteBatch spriteBatch) {
			BossInfo selectedBoss;
			if (BossLogUI.PageNum >= 0) selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
			else {
				int index = BossChecklist.bossTracker.SortedBosses.FindIndex(boss => boss.modSource == "Terraria" && boss.internalName == "KingSlime");
				selectedBoss = BossChecklist.bossTracker.SortedBosses[index];
			}

			if (BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2 && BossLogUI.AltPage[BossLogUI.SubPageNum] && Id == "PageTwo") // PageTwo check to prevent the timer from counting down twice (once for each page)
			{
				if (validItems == null) {
					validItems = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
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
					itemShown = new int[] { 0, 0, 0 };
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
			if (selectedBoss.modSource == "Unknown" && Id == "PageTwo") return; // Prevents drawings on the page if the boss has no info
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
					if (headBoss.type != EntryType.Event || headBoss.internalName == "Lunar Event") {
						int headsDisplayed = 0;
						int adjustment = 0;
						Color maskedHead = BossLogUI.MaskBoss(headBoss);
						for (int h = 0; h < headBoss.npcIDs.Count; h++) {
							Texture2D head = BossLogUI.GetBossHead(headBoss.npcIDs[h]);
							if (headBoss.overrideIconTexture != "") head = ModContent.GetTexture(headBoss.overrideIconTexture);
							if (head != Main.npcHeadTexture[0]) {
								headsDisplayed++;
								spriteBatch.Draw(head, new Rectangle(Main.mouseX + 15 + ((head.Width + 2) * adjustment), Main.mouseY + 15, head.Width, head.Height), maskedHead);
								adjustment++;
							}
						}
						Texture2D noHead = Main.npcHeadTexture[0];
						if (headsDisplayed == 0) spriteBatch.Draw(noHead, new Rectangle(Main.mouseX + 15 + ((noHead.Width + 2) * adjustment), Main.mouseY + 15, noHead.Width, noHead.Height), maskedHead);
					}
					else {
						Color maskedHead = BossLogUI.MaskBoss(headBoss);
						Texture2D eventIcon = BossLogUI.GetEventIcon(headBoss);
						Rectangle iconpos = new Rectangle(Main.mouseX + 15, Main.mouseY + 15, eventIcon.Width, eventIcon.Height);
						if (eventIcon != Main.npcHeadTexture[0]) spriteBatch.Draw(eventIcon, iconpos, maskedHead);
					}
				}
			}

			if (Id == "PageOne" && BossLogUI.PageNum >= 0) {
				if (selectedBoss.pageTexture != "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
					Texture2D bossTexture = ModContent.GetTexture(selectedBoss.pageTexture);
					Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (bossTexture.Width / 2), pageRect.Y + (pageRect.Height / 2) - (bossTexture.Height / 2), bossTexture.Width, bossTexture.Height);
					Rectangle cutRect = new Rectangle(0, 0, bossTexture.Width, bossTexture.Height);
					Color maskedBoss = BossLogUI.MaskBoss(selectedBoss);
					spriteBatch.Draw(bossTexture, posRect, cutRect, maskedBoss);
				}
				else if (selectedBoss.npcIDs.Count > 0) {
					Main.instance.LoadNPC(selectedBoss.npcIDs[0]);
					Texture2D NPCTexture = Main.npcTexture[selectedBoss.npcIDs[0]];
					Rectangle snippet = new Rectangle(0, 0, NPCTexture.Width, NPCTexture.Height / Main.npcFrameCount[selectedBoss.npcIDs[0]]);
					Vector2 bossPos = new Vector2(pageRect.X + (int)((Width.Pixels / 2) - (snippet.Width / 2)), pageRect.Y + (int)((Height.Pixels / 2) - (snippet.Height / 2)));
					Color maskedBoss = BossLogUI.MaskBoss(selectedBoss);
					spriteBatch.Draw(NPCTexture, bossPos, snippet, maskedBoss);
				}

				if (selectedBoss.type != EntryType.Event || selectedBoss.internalName == "Lunar Event") {
					int headsDisplayed = 0;
					int adjustment = 0;
					Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
					for (int h = selectedBoss.npcIDs.Count - 1; h > -1; h--) {
						Texture2D head = BossLogUI.GetBossHead(selectedBoss.npcIDs[h]);
						if (selectedBoss.overrideIconTexture != "") head = ModContent.GetTexture(selectedBoss.overrideIconTexture);
						if (BossLogUI.GetBossHead(selectedBoss.npcIDs[h]) != Main.npcHeadTexture[0]) {
							headsDisplayed++;
							Rectangle headPos = new Rectangle(pageRect.X + pageRect.Width - head.Width - 10 - ((head.Width + 2) * adjustment), pageRect.Y + 5, head.Width, head.Height);
							spriteBatch.Draw(head, headPos, maskedHead);
							adjustment++;
						}
					}
					Texture2D noHead = Main.npcHeadTexture[0];
					Rectangle noHeadPos = new Rectangle(pageRect.X + pageRect.Width - noHead.Width - 10 - ((noHead.Width + 2) * adjustment), pageRect.Y + 5, noHead.Width, noHead.Height);
					if (headsDisplayed == 0) spriteBatch.Draw(noHead, noHeadPos, maskedHead);
				}
				else {
					Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
					Texture2D eventIcon = BossLogUI.GetEventIcon(selectedBoss);
					Rectangle iconpos = new Rectangle(pageRect.X + pageRect.Width - eventIcon.Width - 10, pageRect.Y + 5, eventIcon.Width, eventIcon.Height);
					if (eventIcon != Main.npcHeadTexture[0]) spriteBatch.Draw(eventIcon, iconpos, maskedHead);
				}
				
				string isDefeated = "";
				if (selectedBoss.downed()) isDefeated = $"[c/{Colors.RarityGreen.Hex3()}:Defeated in {Main.worldName}]";
				else isDefeated = $"[c/{Colors.RarityRed.Hex3()}:Undefeated in {Main.worldName} ]";

				Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
				Utils.DrawBorderString(spriteBatch, selectedBoss.name, pos, Color.Goldenrod);
				
				pos = new Vector2(pageRect.X + 5, pageRect.Y + 30);
				Utils.DrawBorderString(spriteBatch, selectedBoss.SourceDisplayName, pos, new Color(150, 150, 255));

				pos = new Vector2(pageRect.X + 5, pageRect.Y + 55);
				Utils.DrawBorderString(spriteBatch, isDefeated, pos, Color.White);
			}

			if (Id == "PageOne" && BossLogUI.PageNum == -2) {
				// Credits Page
				Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
				Utils.DrawBorderString(spriteBatch, "Special thanks to:", pos, Color.IndianRed);

				Texture2D users = BossChecklist.instance.GetTexture("Resources/Extra_CreditUsers");
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
					if (boss.modSource != "Terraria" && boss.modSource != "Unknown") {
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
				if (selectedBoss.type != EntryType.Event) {
					// Boss Records Subpage
					Texture2D achievements = ModContent.GetTexture("Terraria/UI/Achievements");
					BossStats record = Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[BossLogUI.PageNum].stat;

					string recordType = "";
					string recordNumbers = "";
					int achX = 0;
					int achY = 0;

					for (int i = 0; i < 4; i++) { // 4 Records total
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

							int BestRecord = record.durationBest;
							int WorstRecord = record.durationWorst;
							int LastRecord = record.durationLast;

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

							int BestRecord = record.healthLossBest;
							int BestPercent = record.healthLossBestPercent;
							int WorstRecord = record.healthLossWorst;
							int WorstPercent = record.healthLossWorstPercent;
							int LastRecord = record.healthLossLast;
							int LastPercent = record.healthLossLastPercent;

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

							int timer = record.dodgeTimeBest;
							int low = record.hitsTakenBest;
							int high = record.hitsTakenWorst;
							int last = record.hitsTakenLast;

							double timer2 = (double)record.dodgeTimeBest / 60;
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
					int offset = 0;
					int offsetY = 0;
					for (int i = 0; i < selectedBoss.npcIDs.Count; i++) {
						if (offset == 0 && offsetY == 5) break; // For now, we stop drawing any banners that exceed the books limit (might have to reimplement as a UIList for scrolling purposes)
						int npcID = selectedBoss.npcIDs[i];
						if (npcID < NPCID.Count) {
							int init = Item.NPCtoBanner(npcID) + 21;
							if (init <= 21) continue;

							Main.instance.LoadNPC(npcID);
							Main.instance.LoadTiles(TileID.Banners);
							Texture2D banner = Main.tileTexture[TileID.Banners];

							int jump = 0;
							if (init >= 222) {
								jump = 6;
								init -= 222;
							}
							else if (init >= 111) {
								jump = 3;
								init -= 111;
							}

							Color faded = new Color(128, 128, 128, 128);
							int bannerID = Item.NPCtoBanner(npcID);
							if (bannerID > 0 && !NPCID.Sets.ExcludedFromDeathTally[NPCID.FromNetId(npcID)]) {
								int bannerItem = Item.BannerToItem(bannerID);
								if (NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem]) faded = Color.White;
							}
							else continue;

							for (int j = 0; j < 3; j++) {
								Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset, GetInnerDimensions().ToRectangle().Y + 100 + 16 * j + offsetY);
								Rectangle rect = new Rectangle(init * 18, (jump * 18) + (j * 18), 16, 16);
								spriteBatch.Draw(banner, pos, rect, faded);

								if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + 16) {
									if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + 16) {
										string killcount = $"{Lang.GetNPCNameValue(npcID)}: {NPC.killCount[Item.NPCtoBanner(npcID)]}";
										if (NPC.killCount[Item.NPCtoBanner(npcID)] < ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]) killcount += $" / {ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]}";
										Main.hoverItemName = killcount;
									}
								}
							}
							offset += 25;
							if (offset == 14 * 25) {
								offset = 0;
								offsetY += 64;
							}
						}
						else { // Its a modded NPC
							Main.instance.LoadNPC(npcID);

							Item newItem = new Item();
							newItem.SetDefaults(NPCLoader.GetNPC(npcID).bannerItem);

							Main.instance.LoadTiles(newItem.createTile);
							Texture2D banner = Main.tileTexture[newItem.createTile];

							int bannerID = NPCLoader.GetNPC(npcID).banner;
							string source = NPCLoader.GetNPC(npcID).mod.DisplayName;

							Color faded = new Color(128, 128, 128, 128);
							if (NPC.killCount[bannerID] >= 50) faded = Color.White;

							for (int j = 0; j < 3; j++) {
								Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset, GetInnerDimensions().ToRectangle().Y + 100 + 16 * j + offsetY);
								Rectangle rect = new Rectangle(0, j * 18, 16, 16);
								spriteBatch.Draw(banner, pos, rect, faded);

								if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + 16) {
									if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + 16) {
										Main.hoverItemName = $"{Lang.GetNPCNameValue(npcID)}: {NPC.killCount[Item.NPCtoBanner(npcID)].ToString()}\n[{source}]";
									}
								}
							}
							offset += 25;
							if (offset == 14 * 25) {
								offset = 0;
								offsetY += 64;
							}
						}
					}
				}
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 1) {
				// Spawn Item Subpage
			}

			if (Id == "PageTwo" && BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2) {
				if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
					// Loot Table Subpage
					Texture2D bag = Main.itemTexture[ItemID.SoulofMight];
					Rectangle sourceRect = new Rectangle();
					for (int i = 0; i < selectedBoss.loot.Count; i++) {
						Item bagItem = new Item();
						bagItem.SetDefaults(selectedBoss.loot[i]);
						if (bagItem.expert && bagItem.Name.Contains("Treasure Bag")) {
							bag = Main.itemTexture[ItemID.SoulofMight];
							DrawAnimation drawAnim = Main.itemAnimations[ItemID.SoulofMight];
							if (drawAnim != null) sourceRect = drawAnim.GetFrame(bag);
							else sourceRect = bag.Bounds;
							break;
							//if (bagItem.type < ItemID.Count) bag = ModContent.GetTexture("Terraria/Item_" + bagItem.type);
							//else bag = ModContent.GetTexture(ItemLoader.GetItem(bagItem.type).Texture);
						}
					}
					Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - 20 - (bag.Width / 2), pageRect.Y + 88, sourceRect.Width, sourceRect.Height);
					spriteBatch.Draw(bag, posRect, sourceRect, Color.White);
				}
				else {
					// Collectibles Subpage
					BossCollection Collections = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];
					
					int selectedTrophy = validItems[0][itemShown[0]];
					int selectedMask = validItems[1][itemShown[1]];
					int selectedMusicBox = validItems[2][itemShown[2]];

					bool hasTrophy = selectedTrophy > 0 && Collections.collectibles.Any(x => x.Type == selectedTrophy);
					bool hasMask = selectedMask > 0 && Collections.collectibles.Any(x => x.Type == selectedMask);
					bool hasMusicBox = selectedMusicBox > 0 && Collections.collectibles.Any(x => x.Type == selectedMusicBox);

					// PageNum already corresponds with the index of the saved player data

					Texture2D template = ModContent.GetTexture("BossChecklist/Resources/Extra_CollectionTemplate");
					Rectangle ctRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (template.Width / 2) - 20, pageRect.Y + 84, template.Width, template.Height);
					spriteBatch.Draw(template, ctRect, Color.White);
					if (!hasMusicBox) {
						Main.instance.LoadTiles(TileID.MusicBoxes);
						Texture2D musicBox = Main.tileTexture[139];

						int offsetX = 0;
						int offsetY = 0;

						for (int i = 0; i < 4; i++) {
							Rectangle posRect = new Rectangle(pageRect.X + 210 + (offsetX * 16), pageRect.Y + 160 + (offsetY * 16), 16, 16);
							Rectangle cutRect = new Rectangle(offsetX * 18, offsetY * 18, 16, 16);

							spriteBatch.Draw(musicBox, posRect, cutRect, Color.White);

							offsetX++;
							if (i == 1) {
								offsetX = 0;
								offsetY++;
							}
						}
					}

					// Draw Masks
					if (hasMask) {
						Texture2D mask;
						if (selectedMask < ItemID.Count) {
							Item newItem = new Item();
							newItem.SetDefaults(selectedMask);
							mask = ModContent.GetTexture("Terraria/Armor_Head_" + newItem.headSlot);
						}
						else mask = ModContent.GetTexture(ItemLoader.GetItem(selectedMask).Texture + "_Head");

						int frameCut = mask.Height / 24;
						Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (mask.Width / 2) - 8, pageRect.Y + (pageRect.Height / 2) - (frameCut / 2) - 86, mask.Width, frameCut);
						Rectangle cutRect = new Rectangle(0, 0, mask.Width, frameCut);
						spriteBatch.Draw(mask, posRect, cutRect, Color.White);
					}

					// Draw Trophies
					if (hasTrophy) {
						int offsetX = 0;
						int offsetY = 0;

						if (selectedTrophy < ItemID.Count) {
							Main.instance.LoadTiles(TileID.Painting3X3);
							Texture2D trophy = Main.tileTexture[TileID.Painting3X3];

							offsetX = BossLogUI.GetVanillaBossTrophyPos(selectedTrophy)[0];
							offsetY = BossLogUI.GetVanillaBossTrophyPos(selectedTrophy)[1];

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
							int selectedTile = ItemLoader.GetItem(selectedTrophy).item.createTile;
							Main.instance.LoadTiles(selectedTile);
							Texture2D trophy = Main.tileTexture[selectedTile];

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

					// Draw Music Boxes
					if (hasMusicBox) {
						int offsetX = 0;
						int offsetY = 0;

						if (selectedMusicBox < ItemID.Count) {
							Main.instance.LoadTiles(TileID.MusicBoxes);
							Texture2D musicBox = Main.tileTexture[TileID.MusicBoxes];

							if (selectedMusicBox == ItemID.MusicBoxBoss1) {
								if (Main.music[MusicID.Boss1].IsPlaying) offsetX = 2;
								offsetY = 10;
							}
							else if (selectedBoss.collection.Any(x => x == ItemID.MusicBoxBoss2)) {
								if (Main.music[MusicID.Boss2].IsPlaying) offsetX = 2;
								offsetY = 20;
							}
							else if (selectedMusicBox == ItemID.MusicBoxBoss3) {
								if (Main.music[MusicID.Boss3].IsPlaying) offsetX = 2;
								offsetY = 24;
							}
							else if (selectedMusicBox == ItemID.MusicBoxBoss4) {
								if (Main.music[MusicID.Boss4].IsPlaying) offsetX = 2;
								offsetY = 32;
							}
							else if (selectedMusicBox == ItemID.MusicBoxBoss5) {
								if (Main.music[MusicID.Boss5].IsPlaying) offsetX = 2;
								offsetY = 48;
							}
							else if (selectedMusicBox == ItemID.MusicBoxPlantera) {
								if (Main.music[MusicID.Plantera].IsPlaying) offsetX = 2;
								offsetY = 46;
							}
							else if (selectedMusicBox == ItemID.MusicBoxDD2) {
								if (Main.music[MusicID.OldOnesArmy].IsPlaying) offsetX = 2;
								offsetY = 78;
							}
							else if (selectedMusicBox == ItemID.MusicBoxLunarBoss) {
								if (Main.music[MusicID.LunarBoss].IsPlaying) offsetX = 2;
								offsetY = 64;
							}

							int backupX = offsetX;
							int backupY = offsetY;

							for (int i = 0; i < 4; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 210 + (offsetX * 16) - (backupX * 16), pageRect.Y + 160 + (offsetY * 16) - (backupY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, offsetY * 18, 16, 16);

								spriteBatch.Draw(musicBox, posRect, cutRect, Color.White);

								offsetX++;
								if (i == 1) {
									offsetX = backupX;
									offsetY++;
								}
							}
						}
						else {
							int selectedTile = ItemLoader.GetItem(selectedMusicBox).item.createTile;
							Main.instance.LoadTiles(selectedTile);
							Texture2D musicBox = Main.tileTexture[selectedTile];

							for (int i = 0; i < 4; i++) {
								Rectangle posRect = new Rectangle(pageRect.X + 210 + (offsetX * 16), pageRect.Y + 160 + (offsetY * 16), 16, 16);
								Rectangle cutRect = new Rectangle(offsetX * 18, offsetY * 18, 16, 16);

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
		
		public override void ScrollWheel(UIScrollWheelEvent evt) {
			//Main.NewText(evt.ScrollWheelValue);
			base.ScrollWheel(evt);
			//if (BossLogUI.PageNum < 0 || BossLogUI.SubPageNum != 1) return;
			if (this != null && this.Parent != null && this.Parent.IsMouseHovering) {
				//Main.NewText(evt.ScrollWheelValue);
				this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
			}
			else if (this != null && this.Parent != null && this.Parent.IsMouseHovering) {
				//Main.NewText(evt.ScrollWheelValue);
				this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
			}
		}
	}

	internal class BookUI : UIImage
	{
		Texture2D book;

		public BookUI(Texture2D texture) : base(texture) {
			book = texture;
		}

		public static bool DrawTab(string Id) {
			///if (BossLogUI.PageNum == -1 && Id == "Table of Contents_Tab") return false;
			///if (BossLogUI.PageNum != -1 && (Id == "ToCFilter_Tab")) return false;
			if (BossLogUI.PageNum == -2 && Id == "Credits_Tab") return false;
			if (BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Boss) && Id == "Boss_Tab") return false;
			if (BossLogUI.PageNum == BossLogUI.FindNext(EntryType.MiniBoss) && Id == "Miniboss_Tab") return false;
			if (BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Event) && Id == "Event_Tab") return false;
			return true;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			if (Id == "ToCFilter_Tab") {
				Texture2D pages = BossChecklist.instance.GetTexture("Resources/LogUI_Back");
				Vector2 pagePos = new Vector2((Main.screenWidth / 2) - 400, (Main.screenHeight / 2) - 250);
				spriteBatch.Draw(pages, pagePos, BossChecklist.BossLogConfig.BossLogColor);
			}
			if (!Id.Contains("_Tab")) base.DrawSelf(spriteBatch);
			else {
				// Tab drawing
				SpriteEffects effect = SpriteEffects.FlipHorizontally;
				if (Left.Pixels <= 0) effect = SpriteEffects.None;

				Color color = new Color(153, 199, 255);
				if (Id == "Boss_Tab") color = new Color(255, 168, 168);
				else if (Id == "Miniboss_Tab") color = new Color(153, 253, 119);
				else if (Id == "Event_Tab") color = new Color(196, 171, 254);
				else if (Id == "Credits_Tab") color = new Color(218, 175, 133);
				color = Color.Tan;

				if (DrawTab(Id)) {
					spriteBatch.Draw(book, GetDimensions().ToRectangle(), new Rectangle(0, 0, book.Width, book.Height), color, 0f, Vector2.Zero, effect, 0f);
				}
			}
			if (Id == "Event_Tab") {
				// Paper Drawing
				Texture2D pages = BossChecklist.instance.GetTexture("Resources/LogUI_Paper");
				Vector2 pagePos = new Vector2((Main.screenWidth / 2) - 400, (Main.screenHeight / 2) - 250);
				spriteBatch.Draw(pages, pagePos, Color.White);
			}
			//Main.playerInventory = false;

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
				
				if (Id == "Boss_Tab") cut = new Rectangle(0 * 24, 1 * 24, 22, 22);
				else if (Id == "Miniboss_Tab") cut = new Rectangle(1 * 24, 1 * 24, 22, 22);
				else if (Id == "Event_Tab") cut = new Rectangle(2 * 24, 1 * 24, 22, 22);
				else if (Id == "Credits_Tab") cut = new Rectangle(3 * 24, 0 * 24, 22, 22);
				else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum == -1) cut = new Rectangle(3 * 24, 1 * 24, 22, 22);
				else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum != -1) cut = new Rectangle(2 * 24, 0 * 24, 22, 22);

				if (DrawTab(Id)) spriteBatch.Draw(texture, pos, cut, Color.White);
				else return;

				if (IsMouseHovering) {
					string tabMessage = "Jump to ";
					if (Id == "ToCFilter_Tab") {
						if (BossLogUI.PageNum == -1) tabMessage = "Toggle Filters";
						else tabMessage += "Table of Contents";
					}
					else if (Id.ToLower().Contains("credit")) tabMessage += "Credits";
					else if (Id.ToLower().Contains("boss") || Id.ToLower().Contains("event")) {
						tabMessage += $"next {Id.Substring(0, Id.Length - 4)}";
					}
					if (Id.ToLower().Contains("boss") || Id.ToLower().Contains("event")) {
						if (Id == ("Boss_Tab") && BossLogUI.FindNext(EntryType.Boss) != -1) {
							tabMessage += $":\n{BossChecklist.bossTracker.SortedBosses[BossLogUI.FindNext(EntryType.Boss)].name}";
						}
						else if (Id == ("Miniboss_Tab") && BossLogUI.FindNext(EntryType.MiniBoss) != -1) {
							tabMessage += $":\n{BossChecklist.bossTracker.SortedBosses[BossLogUI.FindNext(EntryType.MiniBoss)].name}";
						}
						else if (Id == ("Event_Tab") && BossLogUI.FindNext(EntryType.Event) != -1) {
							tabMessage += $":\n{BossChecklist.bossTracker.SortedBosses[BossLogUI.FindNext(EntryType.Event)].name}";
						}
					}
					Main.hoverItemName = tabMessage;
				}
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
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

			foreach (int loot in sortedBosses[index].loot) {
				if (sortedBosses[index].npcIDs[0] < NPCID.Count) {
					if (WorldGen.crimson && (loot == ItemID.DemoniteOre || loot == ItemID.CorruptSeeds || loot == ItemID.UnholyArrow)) continue;
					else if (!WorldGen.crimson && (loot == ItemID.CrimtaneOre || loot == ItemID.CrimsonSeeds)) continue;
				}
				if (modPlayer.BossTrophies[index].loot.Any(x => x.Type == loot)) {
					allLoot = true;
				}
				else if (loot != sortedBosses[index].loot[0]) {
					allLoot = false;
					break;
				}
			}
			foreach (int collectible in sortedBosses[index].collection) {
				if (modPlayer.BossTrophies[index].collectibles.Any(x => x.Type == collectible)) {
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
				int pagenum = Convert.ToInt32(Id);

				Rectangle checkType = new Rectangle(0, 0, 22, 20);
				Rectangle exType = new Rectangle(0, 0, 22, 20);
				
				if (sortedBosses[pagenum].downed()) {
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
					if (IsMouseHovering) TextColor = TextColor = Color.SkyBlue;
					//if (IsMouseHovering && sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.DarkSeaGreen;
					//else if (IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.IndianRed;
					else if (!sortedBosses[pagenum].downed() && nextCheck && BossChecklist.BossLogConfig.DrawNextMark) TextColor = new Color(248, 235, 91);
					else if (sortedBosses[pagenum].downed()) TextColor = Colors.RarityGreen;
					else if (!sortedBosses[pagenum].downed()) TextColor = Colors.RarityRed;
				}
				else {
					if (IsMouseHovering) TextColor = new Color(80, 85, 100);
					else TextColor = new Color(140, 145, 160);
				}

				if ((!sortedBosses[pagenum].available() && !sortedBosses[pagenum].downed()) || sortedBosses[pagenum].hidden) {
					TextColor = Color.SlateGray;
					Vector2 stringAdjust = Main.fontMouseText.MeasureString(text);
					for (int i = 0; i < stringAdjust.X + 4; i++) {
						Texture2D strike = BossChecklist.instance.GetTexture("Resources/LogUI_Checks_Strike");
						Rectangle strikePos = new Rectangle((int)(innerDimensions.X + i - 3), (int)(innerDimensions.Y + (stringAdjust.Y / 4)), 4, 3);
						Rectangle strikeSrc = new Rectangle(0, 4, 4, 3);
						if (i == 0) {
							strikeSrc = new Rectangle(0, 0, 4, 3);
						}
						else if (i == stringAdjust.X + 3) {
							strikeSrc = new Rectangle(0, 8, 4, 3);
						}
						spriteBatch.Draw(strike, strikePos, strikeSrc, Color.White);
					}
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
		string empty;
		public FittedTextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
			this.text = text;
			this.empty = text;
		}

		const float infoScaleX = 1f;
		const float infoScaleY = 1f;
		public override void Draw(SpriteBatch spriteBatch) {
			
			text = "";
			Rectangle hitbox = new Rectangle((int)GetInnerDimensions().X, (int)GetInnerDimensions().Y, (int)Width.Pixels, 100);

			string info = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].info ?? "No info available";
			int hoveredSnippet = -1;
			TextSnippet[] textSnippets = ChatManager.ParseMessage(empty, Color.White).ToArray();
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
				if (BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].type == EntryType.Event) buttonString = "Kill Count";
				else buttonString = "Boss Records";
			}
			BackgroundColor = Color.Brown;
			base.DrawSelf(spriteBatch);

			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 stringAdjust = Main.fontMouseText.MeasureString(buttonString);
			Vector2 pos = new Vector2(innerDimensions.X + ((Width.Pixels - stringAdjust.X) / 2) - 12, innerDimensions.Y - 10);
			if (buttonString != "Disclaimer" && buttonString != "recordAlts") {
				DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, buttonString, pos, Color.Gold);
			}

			Texture2D text = ModContent.GetTexture("Terraria/UI/Achievement_Categories");
			Rectangle exclamPos = new Rectangle((int)GetInnerDimensions().X - 12, (int)GetInnerDimensions().Y - 12, 32, 32);

			if (buttonString == "") {
				if (BossLogUI.SubPageNum == 0) {
					if (Id == "Display Records") {
						if (Main.netMode != NetmodeID.MultiplayerClient) return; // The button only appears in Multiplayer
						Rectangle exclamCut = new Rectangle(34 * 2, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) {
							Main.hoverItemName = "Left-click to display your current records";
							if (displayLastArray[3] != "") Main.hoverItemName += "\nRight-click to display the records of your last fight";
							if (Main.mouseLeftRelease && Main.mouseRightRelease) displayRecord = true;
							else if (displayRecord && recordCooldown == 0) {
								if (Main.mouseLeft) {
									recordCooldown = 600;
									displayRecord = false;

									NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("[" + Main.LocalPlayer.name + "'s current records with " + BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].name + "]"), new Color(82, 175, 82));
									if (displayArray[0] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[0]), new Color(138, 210, 137));
									if (displayArray[1] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[1]), new Color(138, 210, 137));
									if (displayArray[2] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[2]), new Color(138, 210, 137));
									if (displayArray[3] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayArray[3]), new Color(138, 210, 137));
								}
								else if (Main.mouseRight && displayLastArray[3] != "") {
									recordCooldown = 600;
									displayRecord = false;
									NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("[" + Main.LocalPlayer.name + "'s last fight stats with " + BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].name + "]"), new Color(82, 175, 82));
									if (displayLastArray[0] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[0]), new Color(138, 210, 137));
									if (displayLastArray[1] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[1]), new Color(138, 210, 137));
									if (displayLastArray[2] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[2]), new Color(138, 210, 137));
									if (displayLastArray[3] != "") NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(displayLastArray[3]), new Color(138, 210, 137));
								}
							}
						}
					}
					else {
						if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
							Rectangle exclamCut = new Rectangle(34 * 3, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) Main.hoverItemName = "Records are shown as your best compared to your last fight" +
																	"\nClick to see your 'Worst' records";
						}
						else {
							Rectangle exclamCut = new Rectangle(0, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) Main.hoverItemName = "Records are shown as your worst compared to your last fight" +
																	"\nClick to see your 'Best' records";
						}
					}
				}
				else if (BossLogUI.SubPageNum == 1) {
					/*
					if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
						Rectangle exclamCut = new Rectangle(34 * 2, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to read more info";
					}
					else {
						Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to view spawn item recipes";
					}
					*/
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

		public BossLogPanel BookArea;
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

		public static int PageNum = -3; // Selected Boss Page (starts out with an invalid number for the initial check)
		public static int SubPageNum = 0; // Selected Topic Tab (Loot, Stats, etc.)
		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool[] AltPage; // Flip between best and worst
		
		private bool bossLogVisible;
		public bool BossLogVisible {
			get { return bossLogVisible; }
			set {
				if (value) {
					Append(ToCTab);
					Append(CreditsTab);
					Append(filterPanel);
					Append(BossTab);
					Append(MiniBossTab);
					Append(EventTab);
					Append(PageOne);
					Append(PageTwo);
					Append(BookArea);
				}
				else {
					RemoveChild(ToCTab);
					RemoveChild(CreditsTab);
					RemoveChild(filterPanel);
					RemoveChild(BossTab);
					RemoveChild(MiniBossTab);
					RemoveChild(EventTab);
					RemoveChild(PageOne);
					RemoveChild(PageTwo);
					RemoveChild(BookArea);
				}
				bossLogVisible = value;
			}
		}

		public void ToggleBossLog(bool show = true, bool resetPage = false) {
			if (PageNum == -3) resetPage = true;
			if (resetPage) {
				PageNum = -1;
				SubPageNum = 0;
				ToCTab.Left.Set(-400 - 16, 0.5f);
				filterPanel.Left.Set(-400 - 16 + ToCTab.Width.Pixels, 0.5f);
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				UpdateTableofContents();
			}
			else {
				if (PageNum == -1) UpdateTableofContents();
				else if (PageNum == -2) UpdateCredits();
				else {
					if (SubPageNum == 0) OpenRecord();
					else if (SubPageNum == 1) OpenSpawn();
					else if (SubPageNum == 2) OpenLoot();
				}
			}
			BossLogVisible = show;
			if (show) {
				Main.playerInventory = false;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true; // Removes rainbow glow
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasNewRecord = false; // if (PageNum >= 0 && SubPageNum == 0) ??
			}
		}

		public override void OnInitialize() {
			Texture2D bookTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(0, 0, 34, 38));
			bosslogbutton = new BossAssistButton(bookTexture, "Boss Log");
			bosslogbutton.Id = "OpenUI";
			bosslogbutton.Width.Set(34, 0f);
			bosslogbutton.Height.Set(38, 0f);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnClick += (a, b) => ToggleBossLog(true);

			AltPage = new bool[]
			{
				false, false, false, false
			};
			
			ToCTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			ToCTab.Height.Pixels = 76;
			ToCTab.Width.Pixels = 32;
			ToCTab.Left.Set(-400 - 16, 0.5f);
			ToCTab.Top.Set(-250 + 20, 0.5f);
			ToCTab.Id = "ToCFilter_Tab";
			ToCTab.OnClick += new MouseEvent(OpenViaTab);

			BossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			BossTab.Height.Pixels = 76;
			BossTab.Width.Pixels = 32;
			BossTab.Left.Set(-400 - 16, 0.5f);
			BossTab.Top.Set(-250 + 30 + 76, 0.5f);
			BossTab.Id = "Boss_Tab";
			BossTab.OnClick += new MouseEvent(OpenViaTab);

			MiniBossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			MiniBossTab.Height.Pixels = 76;
			MiniBossTab.Width.Pixels = 32;
			MiniBossTab.Left.Set(-400 - 16, 0.5f);
			MiniBossTab.Top.Set(-250 + 40 + (76 * 2), 0.5f);
			MiniBossTab.Id = "Miniboss_Tab";
			MiniBossTab.OnClick += new MouseEvent(OpenViaTab);

			EventTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			EventTab.Height.Pixels = 76;
			EventTab.Width.Pixels = 32;
			EventTab.Left.Set(-400 - 16, 0.5f);
			EventTab.Top.Set(-250 + 50 + (76 * 3), 0.5f);
			EventTab.Id = "Event_Tab";
			EventTab.OnClick += new MouseEvent(OpenViaTab);

			CreditsTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			CreditsTab.Height.Pixels = 76;
			CreditsTab.Width.Pixels = 32;
			CreditsTab.Left.Set(-400 - 16, 0.5f);
			CreditsTab.Top.Set(-250 + 60 + (76 * 4), 0.5f);
			CreditsTab.Id = "Credits_Tab";
			CreditsTab.OnClick += new MouseEvent(OpenViaTab);

			BookArea = new BossLogPanel();
			BookArea.Width.Pixels = 800;
			BookArea.Height.Pixels = 478;
			BookArea.Left.Pixels = (Main.screenWidth / 2) - 400;
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (478 / 2) - 6;

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
			
			filterPanel = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Filter"));
			filterPanel.Id = "filterPanel";
			filterPanel.Height.Pixels = 76;
			filterPanel.Width.Pixels = 152;
			filterPanel.Left.Set(-400 - 16, 0.5f);
			filterPanel.Top.Set(-250 + 20, 0.5f);

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
				newCheckBox.Left.Pixels = 5;
				newCheckBox.OnClick += new MouseEvent(ChangeFilter);
				newCheckBox.Append(filterCheckMark[i]);
				filterCheck.Add(newCheckBox);

				string type = "Bosses";
				if (i == 1) type = "Mini bosses";
				if (i == 2) type = "Events";
				UIText bosses = new UIText(type, 0.85f);
				bosses.Top.Pixels = 10 + (20 * i);
				bosses.Left.Pixels = 25;
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
			recordButton.OnClick += (a, b) => OpenRecord();
			recordButton.OnRightDoubleClick += (a, b) => ResetStats();

			spawnButton = new SubpageButton("Spawn Info");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 - 8;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += (a, b) => OpenSpawn();

			lootButton = new SubpageButton("Loot & Collection");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2 - 16;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += (a, b) => OpenLoot();

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
			toolTipButton.OnClick += (a, b) => SwapRecordPage();

			displayRecordButton = new SubpageButton("Display Records");
			displayRecordButton.Width.Pixels = 32;
			displayRecordButton.Height.Pixels = 32;
			displayRecordButton.Left.Pixels = PageTwo.Width.Pixels - displayRecordButton.Width.Pixels - 30;
			displayRecordButton.Top.Pixels = 128;
		}

		public override void Update(GameTime gameTime) {
			this.AddOrRemoveChild(bosslogbutton, Main.playerInventory);
			
			// We reset the position of the button to make sure it updates with the screen res
			BookArea.Left.Pixels = (Main.screenWidth / 2) - 400;
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (478 / 2) - 6;
			PageOne.Left.Pixels = (Main.screenWidth / 2) - 400 + 20;
			PageOne.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;
			PageTwo.Left.Pixels = (Main.screenWidth / 2) - 400 + 800 - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;
			
			// Updating tabs to proper positions
			if (PageNum == -2) CreditsTab.Left.Pixels = -400 - 16;
			else CreditsTab.Left.Pixels = -400 + 800 - 16;
			if (PageNum >= FindNext(EntryType.Boss) || PageNum == -2) BossTab.Left.Pixels = -400 - 16;
			else BossTab.Left.Pixels = -400 + 800 - 16;
			if (PageNum >= FindNext(EntryType.MiniBoss) || PageNum == -2) MiniBossTab.Left.Pixels = -400 - 16;
			else MiniBossTab.Left.Pixels = -400 + 800 - 16;
			if (PageNum >= FindNext(EntryType.Event) || PageNum == -2) EventTab.Left.Pixels = -400 - 16;
			else EventTab.Left.Pixels = -400 + 800 - 16;
			
			if (PageNum != -1) {
				ToCTab.Left.Set(-400 - 16, 0.5f);
				filterPanel.Left.Set(-400 - 16 + ToCTab.Width.Pixels, 0.5f);
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				filterPanel.Width.Pixels = 32;
				filterPanel.Top.Precent = 5f; // Throw it off screen.
			}
			else {
				filterPanel.Top.Precent = 0.5f;
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

		public TextSnippet hoveredTextSnippet;
		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);

			if (hoveredTextSnippet != null) {
				hoveredTextSnippet.OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease) {
					hoveredTextSnippet.OnClick();
				}
				hoveredTextSnippet = null;
			}
		}

		public void ToggleFilterPanel(UIMouseEvent evt, UIElement listeningElement) {
			if (filterPanel.Left.Pixels != -400 - 16 - 120 + ToCTab.Width.Pixels) {
				ToCTab.Left.Set(-400 - 16 - 120, 0.5f);
				filterPanel.Left.Set(-400 - 16 - 120 + ToCTab.Width.Pixels, 0.5f);
				filterPanel.Width.Pixels = 152;
				foreach (BookUI uiimage in filterCheck) {
					filterPanel.Append(uiimage);
				}
				foreach (UIText uitext in filterTypes) {
					filterPanel.Append(uitext);
				}
			}
			else {
				ToCTab.Left.Set(-400 - 16, 0.5f);
				filterPanel.Left.Set(-400 - 16 + ToCTab.Width.Pixels, 0.5f);
				foreach (BookUI uiimage in filterCheck) {
					filterPanel.RemoveChild(uiimage);
				}
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				filterPanel.Width.Pixels = 32;
			}
		}

		private void ChangeFilter(UIMouseEvent evt, UIElement listeningElement) {
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
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			UpdateTableofContents();
		}

		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (!BookUI.DrawTab(listeningElement.Id)) return;
			if (listeningElement.Id == "ToCFilter_Tab" && PageNum == -1) {
				ToggleFilterPanel(evt, listeningElement);
				return;
			}
			if (listeningElement.Id == "Boss_Tab") PageNum = FindNext(EntryType.Boss);
			else if (listeningElement.Id == "Miniboss_Tab") PageNum = FindNext(EntryType.MiniBoss);
			else if (listeningElement.Id == "Event_Tab") PageNum = FindNext(EntryType.Event);
			else if (listeningElement.Id == "Credits_Tab") UpdateCredits();
			else UpdateTableofContents();

			if (PageNum >= 0) {
				ResetBothPages();
				if (SubPageNum == 0) OpenRecord();
				else if (SubPageNum == 1) OpenSpawn();
				else if (SubPageNum == 2) OpenLoot();
			}
		}

		private void ResetStats() {
			if (BossChecklist.DebugConfig.ResetRecordsBool && Main.netMode == NetmodeID.SinglePlayer) {
				BossStats stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat;
				stats.durationBest = -1;
				stats.durationWorst = -1;
				stats.dodgeTimeBest = -1;
				stats.hitsTakenBest = -1;
				stats.hitsTakenWorst = -1;
				stats.kills = 0;
				stats.deaths = 0;
				stats.healthLossWorst = -1;
				stats.healthLossWorstPercent = -1;
				stats.healthLossBest = -1;
				stats.healthLossBestPercent = -1;
				OpenRecord();
				
				/*
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					BossStats newRecord = new BossStats() {
						durationLast = -1,
						hitsTakenLast = -1,
						dodgeTimeLast = -1,
						healthLossLast = -1,
						healthLossLastPercent = -1,
					};

					RecordID specificRecord = RecordID.ResetAll;
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.RecordUpdate);
					packet.Write((int)PageNum);
					newRecord.NetSend(packet, specificRecord);
					
					packet.Send(toClient: Main.LocalPlayer.whoAmI);
				}
				*/
			}
			// TODO: Make ResetStats and RemoveItem work in MP
		}

		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			/*
			if (BossChecklist.DebugConfig.RemoveItemFromList) {
				string ID = listeningElement.Id;
				if (ID.Contains("collect_")) {
					int itemType = Convert.ToInt32(listeningElement.Id.Substring(8));
					List<ItemDefinition> collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum].collectibles;
					int index = collection.FindIndex(x => x.Type == itemType);
					collection.RemoveAt(index);
					OpenCollect();
				}
				else if (ID.Contains("loot_")) {
					int itemType = Convert.ToInt32(listeningElement.Id.Substring(5));
					List<ItemDefinition> collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum].loot;
					int index = collection.FindIndex(x => x.Type == itemType);
					collection.RemoveAt(index);
					OpenLoot();
				}
			}
			*/
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
				if (RecipeShown == Convert.ToInt32(listeningElement.Id.Substring(index + 1)) - 1) RecipeShown = 0;
				else RecipeShown++;
			}
			OpenSpawn();
		}

		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			pageTwoItemList.Clear();
			prehardmodeList.Clear();
			hardmodeList.Clear();
			PageOne.RemoveChild(scrollOne);
			PageTwo.RemoveChild(scrollTwo);
			RecipeShown = 0;
			RecipePageNum = 0;
			BossLogPanel.validItems = null;

			if (listeningElement.Id == "Previous") {
				if (PageNum > -1) PageNum--;
				else if (PageNum == -2) PageNum = BossChecklist.bossTracker.SortedBosses.Count - 1;
				if (PageNum == -1) UpdateTableofContents();
				else {
					if (SubPageNum == 0) OpenRecord();
					else if (SubPageNum == 1) OpenSpawn();
					else if (SubPageNum == 2) OpenLoot();
					//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
				}
			}
			else if (listeningElement.Id == "Next") {
				if (PageNum != BossChecklist.bossTracker.SortedBosses.Count - 1) PageNum++;
				else UpdateCredits();

				if (PageNum != -2) {
					if (SubPageNum == 0) OpenRecord();
					else if (SubPageNum == 1) OpenSpawn();
					else if (SubPageNum == 2) OpenLoot();
					//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
				}
			}
			ResetPageButtons();
		}

		private void OpenRecord() {
			SubPageNum = 0;
			ResetBothPages();
			if (PageNum < 0) return;
		}

		private void OpenSpawn() {
			SubPageNum = 1;
			int TotalRecipes = 0;
			ResetBothPages();
			if (PageNum < 0) return;
			pageTwoItemList.Clear();
				
			if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource == "Unknown") return;
			string infoText = BossChecklist.bossTracker.SortedBosses[PageNum].info;

			var message = new UIMessageBox(infoText);
			message.Width.Set(-34f, 1f);
			message.Height.Set(-370f, 1f);
			message.Top.Set(85f, 0f);
			message.Left.Set(-10f, 0f);
			//message.PaddingRight = 30;
			PageTwo.Append(message);

			scrollTwo = new FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Set(91f, 0f);
			scrollTwo.Height.Set(-382f, 1f);
			scrollTwo.Left.Set(-20, 0f);
			scrollTwo.HAlign = 1f;
			PageTwo.Append(scrollTwo);
			message.SetScrollbar(scrollTwo);

			// Current code is mostly done in FittedTextPanel.Draw
			FittedTextPanel infoLines = new FittedTextPanel(infoText);
			infoLines.Left.Pixels = 0;
			infoLines.Top.Pixels = 75;
			infoLines.Width.Pixels = 300;
			infoLines.MaxWidth.Pixels = 300;
			infoLines.Height.Pixels = PageTwo.Height.Pixels - 150;
			infoLines.MaxHeight.Pixels = PageTwo.Height.Pixels - 150;
			infoLines.OverflowHidden = true;
			//PageTwo.Append(infoLines);

			if (BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count < 1) {
				if (BossChecklist.bossTracker.SortedBosses[PageNum].modSource == "Unknown") return;
				string type = "";
				if (BossChecklist.bossTracker.SortedBosses[PageNum].type == EntryType.MiniBoss) type = "miniboss";
				else if (BossChecklist.bossTracker.SortedBosses[PageNum].type == EntryType.Event) type = "event";
				else type = "boss";
				UIText info = new UIText($"This {type} has no summon item.");
				info.Left.Pixels = (PageTwo.Width.Pixels / 2) - (Main.fontMouseText.MeasureString(info.Text).X / 2) - 20;
				info.Top.Pixels = 300;
				PageTwo.Append(info);
				return;
			}
			List<Item> ingredients = new List<Item>();
			List<int> requiredTiles = new List<int>();
			string recipeMod = "Terraria";
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
				spawnItemSlot.Top.Pixels = 230;
				spawnItemSlot.Left.Pixels = 33 + (56 * 2);
				PageTwo.Append(spawnItemSlot);

				int row = 0;
				int col = 0;
				for (int k = 0; k < ingredients.Count - 1; k++) {
					LogItemSlot ingList = new LogItemSlot(ingredients[k], false, ingredients[k].HoverName, ItemSlot.Context.GuideItem, 0.85f);
					ingList.Id = "ingredient_" + k;
					ingList.Height.Pixels = 50;
					ingList.Width.Pixels = 50;
					ingList.Top.Pixels = 240 + (48 * (row + 1));
					ingList.Left.Pixels = 5 + (48 * col);
					PageTwo.Append(ingList);
					col++;
					if (k == 6) {
						if (ingList.item.type == 0) break;
						col = 0;
						row++;
					}
				}

				Item craft = new Item();
				if (ingredients.Count > 0 && requiredTiles.Count == 0) {
					craft.SetDefaults(ItemID.PowerGlove);

					LogItemSlot craftItem = new LogItemSlot(craft, false, "By Hand", ItemSlot.Context.EquipArmorVanity, 0.85f);
					craftItem.Height.Pixels = 50;
					craftItem.Width.Pixels = 50;
					craftItem.Top.Pixels = 240 + (48 * (row + 2));
					craftItem.Left.Pixels = 5;
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
							tileList = new LogItemSlot(craft, false, altarType, ItemSlot.Context.EquipArmorVanity, 0.85f);
						}
						else {
							for (int m = 0; m < ItemLoader.ItemCount; m++) {
								craft.SetDefaults(m);
								if (craft.createTile == requiredTiles[l]) break;
							}
							tileList = new LogItemSlot(craft, false, craft.HoverName, ItemSlot.Context.EquipArmorVanity, 0.85f);
						}
						tileList.Height.Pixels = 50;
						tileList.Width.Pixels = 50;
						tileList.Top.Pixels = 240 + (48 * (row + 2));
						tileList.Left.Pixels = 5 + (48 * l);
						PageTwo.Append(tileList);
					}
				}

				if (RecipePageNum > 0) {
					Texture2D prevTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(0, 0, 22, 22));
					BossAssistButton PrevItem = new BossAssistButton(prevTexture, "");
					PrevItem.Id = "PrevItem";
					PrevItem.Top.Pixels = 245;
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
					NextItem.Top.Pixels = 245;
					NextItem.Left.Pixels = 203;
					NextItem.Width.Pixels = 14;
					NextItem.Height.Pixels = 20;
					NextItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(NextItem);
				}

				if (TotalRecipes > 1) {
					Texture2D credTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(48, 0, 22, 22));
					BossAssistButton CycleItem = new BossAssistButton(credTexture, "Cycle alternative recipes");
					CycleItem.Id = "CycleItem_" + TotalRecipes;
					CycleItem.Top.Pixels = 245;
					CycleItem.Left.Pixels = 40;
					CycleItem.Width.Pixels = 22;
					CycleItem.Height.Pixels = 22;
					CycleItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(CycleItem);
				}

				string recipeMessage = "This item is not craftable.";
				if (TotalRecipes > 0) recipeMessage = "Recipe from: " + recipeMod;

				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = -5;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);
			}
		}

		private void OpenLoot() {
			SubPageNum = 2;
			if (AltPage[SubPageNum]) {
				OpenCollect();
				return;
			}
			ResetBothPages();
			if (PageNum < 0) return;
			int row = 0;
			int col = 0;

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 125;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 125 - 80;

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 125;
			scrollTwo.Left.Pixels = -18;
			scrollTwo.Height.Set(-88f, 0.75f);
			scrollTwo.HAlign = 1f;

			pageTwoItemList.Clear();
			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];
			LootRow newRow = new LootRow(0) { Id = "Loot0" };
			for (int i = 0; i < shortcut.loot.Count; i++) {
				Item expertItem = new Item();
				expertItem.SetDefaults(shortcut.loot[i]);
				if (!expertItem.expert || expertItem.Name.Contains("Treasure Bag")) continue;
				else {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(expertItem, Collection.loot.Any(x => x.Type == expertItem.type), expertItem.Name, ItemSlot.Context.ShopItem);
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Id = "loot_" + expertItem.type;
					lootTable.Left.Pixels = (col * 56);
					lootTable.OnRightDoubleClick += new MouseEvent(RemoveItem);
					newRow.Append(lootTable);
					col++;
					if (col == 6) {
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
				if (shortcut.npcIDs[0] < NPCID.Count) {
					if (WorldGen.crimson) {
						if (loot.type == ItemID.DemoniteOre || loot.type == ItemID.CorruptSeeds || loot.type == ItemID.UnholyArrow) continue;
					}
					else {
						if (loot.type == ItemID.CrimtaneOre || loot.type == ItemID.CrimsonSeeds) continue;
					}
				}

				if (loot.expert || loot.Name.Contains("Treasure Bag")) continue;
				else {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(loot, Collection.loot.Any(x => x.Type == loot.type), loot.Name, ItemSlot.Context.TrashItem);
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Id = "loot_" + loot.type;
					lootTable.Left.Pixels = (col * 56);
					lootTable.OnRightDoubleClick += new MouseEvent(RemoveItem);
					newRow.Append(lootTable);
					col++;
					if (col == 6) {
						col = 0;
						row++;
						pageTwoItemList.Add(newRow);
						newRow = new LootRow(row) { Id = "Loot" + row };
					}
				}
			}
			if (col != 0) {
				col = 0;
				row++;
				pageTwoItemList.Add(newRow);
				newRow = new LootRow(row) { Id = "Loot" + row };
			}
			if (row > 5) PageTwo.Append(scrollTwo);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		private void OpenCollect() {
			SubPageNum = 2;
			ResetBothPages();
			if (PageNum < 0) return;
			int row = 0;
			int col = 0;

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 235;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 240 - 75;

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 250;
			scrollTwo.Left.Pixels = -18;
			scrollTwo.Height.Set(-220f, 0.75f);
			scrollTwo.HAlign = 1f;

			pageTwoItemList.Clear();
			LootRow newRow = new LootRow(0) { Id = "Collect0" };
			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];
			for (int i = 0; i < shortcut.collection.Count; i++) {
				if (shortcut.collection[i] == -1) continue;
				Item collectible = new Item();
				collectible.SetDefaults(shortcut.collection[i]);

				BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];
				LogItemSlot collectionTable = new LogItemSlot(collectible, Collection.collectibles.Any(x => x.Type == collectible.type), collectible.Name);
				collectionTable.Height.Pixels = 50;
				collectionTable.Width.Pixels = 50;
				collectionTable.Id = "collect_" + collectible.type;
				collectionTable.Left.Pixels = (56 * (col));
				collectionTable.OnRightDoubleClick += new MouseEvent(RemoveItem);
				newRow.Append(collectionTable);
				col++;
				if (col == 6 || i == shortcut.collection.Count - 1) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row) { Id = "Collect" + row };
				}
			}
			if (row > 3) PageTwo.Append(scrollTwo);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		public void UpdateTableofContents() {
			PageNum = -1;
			ResetBothPages();
			int nextCheck = 0;
			bool nextCheckBool = false;
			prehardmodeList.Clear();
			hardmodeList.Clear();

			List<BossInfo> copiedList = new List<BossInfo>(BossChecklist.bossTracker.SortedBosses);

			for (int i = 0; i < copiedList.Count; i++) {
				if (copiedList[i].modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported) continue;
				if ((!copiedList[i].available() || copiedList[i].hidden) && BossChecklist.BossLogConfig.HideUnavailable) continue;
				if (!copiedList[i].downed()) nextCheck++;
				if (nextCheck == 1) nextCheckBool = true;

				string bossName = copiedList[i].name;
				if (!copiedList[i].available() && !copiedList[i].downed()) bossName = "???";

				TableOfContents next = new TableOfContents(copiedList[i].progression, bossName, nextCheckBool);
				nextCheckBool = false;

				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;
				EntryType type = copiedList[i].type;
				
				next.PaddingTop = 5;
				next.PaddingLeft = 22;
				next.Id = i.ToString();
				next.OnClick += new MouseEvent(JumpToBossPage);

				if (copiedList[i].downed()) {
					next.TextColor = Colors.RarityGreen;
					if ((mbFilter == "Show" && type == EntryType.MiniBoss) || (eFilter == "Show" && type == EntryType.Event) || (type == EntryType.Boss && bFilter != "Hide when completed")) {
						if (copiedList[i].progression <= 6f) prehardmodeList.Add(next);
						else hardmodeList.Add(next);
					}
				}
				else {
					nextCheck++;
					next.TextColor = Colors.RarityRed;
					if (!copiedList[i].available()) next.TextColor = Color.SlateGray;
					if ((mbFilter != "Hide" && type == EntryType.MiniBoss) || (eFilter != "Hide" && type == EntryType.Event) || type == EntryType.Boss) {
						if (copiedList[i].progression <= 6f) prehardmodeList.Add(next);
						else hardmodeList.Add(next);
					}
				}
			}
			
			if (prehardmodeList.Count > 13) PageOne.Append(scrollOne);
			PageOne.Append(prehardmodeList);
			prehardmodeList.SetScrollbar(scrollOne);
			if (hardmodeList.Count > 13) PageTwo.Append(scrollTwo);
			PageTwo.Append(hardmodeList);
			hardmodeList.SetScrollbar(scrollTwo);
		}

		private void UpdateCredits() {
			PageNum = -2;
			ResetBothPages();
			List<string> optedMods = new List<string>();
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (boss.modSource != "Terraria" && boss.modSource != "Unknown") {
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

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 90;
			scrollTwo.Left.Pixels = -24;
			scrollTwo.Height.Set(-60f, 0.75f);
			scrollTwo.HAlign = 1f;

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
			if (optedMods.Count > 11) PageTwo.Append(scrollTwo);
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		internal void JumpToBossPage(UIMouseEvent evt, UIElement listeningElement) {
			PageNum = Convert.ToInt32(listeningElement.Id);
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				BossInfo pgBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
				pgBoss.hidden = !pgBoss.hidden;
				if (pgBoss.hidden) WorldAssist.HiddenBosses.Add(pgBoss.Key);
				else WorldAssist.HiddenBosses.Remove(pgBoss.Key);
				BossChecklist.instance.bossChecklistUI.UpdateCheckboxes();
				UpdateTableofContents();
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.RequestHideBoss);
					packet.Write(pgBoss.Key);
					packet.Write(pgBoss.hidden);
					packet.Send();
				}
				return;
			}
			PageOne.RemoveAllChildren();
			ResetPageButtons();
			if (SubPageNum == 0) OpenRecord();
			else if (SubPageNum == 1) OpenSpawn();
			else if (SubPageNum == 2) OpenLoot();
			//else if (SubPageNum == 3) OpenCollect(evt, listeningElement);
		}

		private void ResetBothPages() {
			PageOne.RemoveAllChildren();
			PageTwo.RemoveAllChildren();

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
				BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (boss.modSource != "Unknown") {
					bool eventCheck = SubPageNum == 0 && boss.type == EntryType.Event;
					if (!eventCheck) {
						toolTipButton = new SubpageButton("");
						toolTipButton.Width.Pixels = 32;
						toolTipButton.Height.Pixels = 32;
						toolTipButton.Left.Pixels = PageTwo.Width.Pixels - toolTipButton.Width.Pixels - 30;
						toolTipButton.Top.Pixels = 86;
						toolTipButton.OnClick += (a, b) => SwapRecordPage();
						if (SubPageNum != 1) PageTwo.Append(toolTipButton);

						if (SubPageNum == 0 && Main.netMode == NetmodeID.MultiplayerClient) {
							displayRecordButton = new SubpageButton("");
							displayRecordButton.Width.Pixels = 32;
							displayRecordButton.Height.Pixels = 32;
							displayRecordButton.Left.Pixels = PageTwo.Width.Pixels - displayRecordButton.Width.Pixels - 30;
							displayRecordButton.Top.Pixels = 128;
							displayRecordButton.Id = "Display Records";
							PageTwo.Append(displayRecordButton);
						}
					}
				}
				PageTwo.Append(NextPage);
				PageOne.Append(PrevPage);
			}
		}

		private void SwapRecordPage() {
			AltPage[SubPageNum] = !AltPage[SubPageNum];
			if (SubPageNum == 2) OpenLoot();
			if (SubPageNum == 1) OpenSpawn();
		}

		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.downed() && x.type == entryType);

		public static Color MaskBoss(BossInfo boss) => (((!boss.downed() || !boss.available()) && BossChecklist.BossLogConfig.BossSilhouettes) || boss.hidden) ? Color.Black : Color.White;

		public static Texture2D GetBossHead(int boss) => NPCID.Sets.BossHeadTextures[boss] != -1 ? Main.npcHeadBossTexture[NPCID.Sets.BossHeadTextures[boss]] : Main.npcHeadTexture[0];

		public static Texture2D GetEventIcon(BossInfo boss) {
			if (boss.overrideIconTexture != "" && boss.overrideIconTexture != "Terraria/NPC_Head_0") return BossChecklist.instance.GetTexture(boss.overrideIconTexture);
			if (boss.internalName == "Frost Legion") return ModContent.GetTexture("Terraria/Extra_7");
			if (boss.internalName == "Frost Moon") return ModContent.GetTexture("Terraria/Extra_8");
			if (boss.internalName == "Goblin Army") return ModContent.GetTexture("Terraria/Extra_9");
			if (boss.internalName == "Martian Madness") return ModContent.GetTexture("Terraria/Extra_10");
			if (boss.internalName == "Pirate Invasion") return ModContent.GetTexture("Terraria/Extra_11");
			if (boss.internalName == "Pumpkin Moon") return ModContent.GetTexture("Terraria/Extra_12");
			if (boss.internalName == "Old One's Army") return BossLogUI.GetBossHead(NPCID.DD2LanePortal);
			if (boss.internalName == "Blood Moon") return BossChecklist.instance.GetTexture("Resources/BossTextures/EventBloodMoon_Head");
			if (boss.internalName == "Solar Eclipse") return BossChecklist.instance.GetTexture("Resources/BossTextures/EventSolarEclipse_Head");
			else return Main.npcHeadTexture[0];
		}

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
			else if (item == ItemID.MourningWoodTrophy) return new int[] { 0, 3 };
			else if (item == ItemID.PumpkingTrophy) return new int[] { 3, 3 };
			else if (item == ItemID.IceQueenTrophy) return new int[] { 6, 3 };
			else if (item == ItemID.SantaNK1Trophy) return new int[] { 9, 3 };
			else if (item == ItemID.EverscreamTrophy) return new int[] { 12, 3 };
			else if (item == ItemID.KingSlimeTrophy) return new int[] { 54, 3 };
			else if (item == ItemID.DukeFishronTrophy) return new int[] { 57, 3 };
			else if (item == ItemID.AncientCultistTrophy) return new int[] { 60, 3 };
			else if (item == ItemID.MartianSaucerTrophy) return new int[] { 63, 3 };
			else if (item == ItemID.FlyingDutchmanTrophy) return new int[] { 66, 3 };
			else if (item == ItemID.MoonLordTrophy) return new int[] { 69, 3 };
			else if (item == ItemID.BossTrophyDarkmage) return new int[] { 72, 3 };
			else if (item == ItemID.BossTrophyBetsy) return new int[] { 75, 3 };
			else if (item == ItemID.BossTrophyOgre) return new int[] { 78, 3 };
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
