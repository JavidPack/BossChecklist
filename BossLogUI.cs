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
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;

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
				Top.Pixels = Utils.Clamp(Top.Pixels, -parentSpace.Bottom, -Height.Pixels);
				Recalculate();
				BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			CalculatedStyle innerDimensions = GetInnerDimensions();
			string translated = Language.GetTextValue(buttonType);
			Vector2 stringAdjust = Main.fontMouseText.MeasureString(translated);
			Vector2 pos = new Vector2(innerDimensions.X - (stringAdjust.X / 3), innerDimensions.Y - 24);

			base.DrawSelf(spriteBatch);

			// Draw the Boss Log Color
			if (Id == "OpenUI") {
				Texture2D cover = BossLogUI.colorTexture;
				Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
				if (!IsMouseHovering && !dragging) {
					cover = BossLogUI.fadedTexture;
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
				}
				spriteBatch.Draw(cover, innerDimensions.ToRectangle(), coverColor);

				// Border Selection
				PlayerAssist myPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				if (!myPlayer.hasOpenedTheBossLog) spriteBatch.Draw(BossLogUI.borderTexture, innerDimensions.ToRectangle(), Main.DiscoColor);
				else if (BossChecklist.DebugConfig.NewRecordsDisabled || BossChecklist.DebugConfig.RecordTrackingDisabled) spriteBatch.Draw(BossLogUI.borderTexture, innerDimensions.ToRectangle(), Color.IndianRed);

				if (myPlayer.hasNewRecord.Any(x => x == true)) {
					slowDown = !slowDown;
					if (slowDown) cycleFrame++;
					if (cycleFrame >= 19) cycleFrame = 0;

					Texture2D bookBorder = BossChecklist.instance.GetTexture("Resources/LogUI_ButtonBorder");
					Rectangle source = new Rectangle(0, 40 * cycleFrame, 34, 38);
					spriteBatch.Draw(bookBorder, innerDimensions.ToRectangle(), source, BossChecklist.BossLogConfig.BossLogColor);
				}

				// Drawing the entire book while dragging if the mouse happens to go off screen/out of window
				if (dragging) spriteBatch.Draw(texture, innerDimensions.ToRectangle(), Color.White);
			}

			if (IsMouseHovering && !dragging) {
				BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
				if (!Id.StartsWith("CycleItem")) DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, translated, pos, Color.White);
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

			if (Id.StartsWith("loot_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
				Main.inventoryBack6Texture = BossChecklist.instance.GetTexture("Resources/Extra_ExpertCollected");
			}

			if (Id.StartsWith("collect_") && hasItem) {
				Main.inventoryBack7Texture = Main.inventoryBack3Texture;
			}

			string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
			string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");

			// Prevents empty slots from being drawn
			if (item.type != 0 || hoverText == demonAltar || hoverText == crimsonAltar || Id.StartsWith("ingredient_")) {
				ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
			}

			Main.inventoryBack6Texture = backup;
			Main.inventoryBack7Texture = backup2;

			if (hoverText == crimsonAltar || hoverText == demonAltar) {
				Main.instance.LoadTiles(TileID.DemonAltar);
				int offsetX = 0;
				int offsetY = 0;
				int offsetSrc = 0;
				if (hoverText == crimsonAltar) offsetSrc = 3;
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
			
			Rectangle rect = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 22, 20);
			if (item.type != 0 && (Id.StartsWith("loot_") || Id.StartsWith("collect_"))) {
				if (hasItem) spriteBatch.Draw(BossLogUI.checkMarkTexture, rect, Color.White); // hasItem first priority
				else if (!Main.expertMode && (item.expert || item.expertOnly)) spriteBatch.Draw(BossLogUI.xTexture, rect, Color.White);
			}

			if (Id.StartsWith("collect_") && BossChecklist.DebugConfig.ShowCollectionType) {
				string showType = "";
				BossInfo boss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
				int index = boss.collection.FindIndex(x => x == item.type);
				CollectionType type = boss.collectType[index];
				if (type == CollectionType.Trophy) showType = "Trophy";
				else if (type == CollectionType.MusicBox) showType = "Music";
				else if (type == CollectionType.Mask) showType = "Mask";

				if (showType != "") {
					Vector2 measure = Main.fontMouseText.MeasureString(showType);
					Vector2 pos = new Vector2(rectangle.X + (Width.Pixels / 2) - (measure.X * 0.8f / 2), rectangle.Top);
					Utils.DrawBorderString(spriteBatch, showType, pos, Colors.RarityAmber, 0.8f);
				}
			}

			if (IsMouseHovering) {
				if (hoverText != Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand")) {
					if (item.type != 0 && (Id.StartsWith("loot_") || Id.StartsWith("collect_")) && !Main.expertMode && (item.expert || item.expertOnly)) {
						Main.hoverItemName = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly");
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
		public static List<List<int>> validItems;
		public static int headNum = -1;

		public override void Draw(SpriteBatch spriteBatch) {
			BossInfo selectedBoss;
			// Pre-drawing
			if (BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2 && BossLogUI.AltPage[BossLogUI.SubPageNum] && Id == "PageTwo") // PageTwo check to prevent the timer from counting down twice (once for each page)
			{
				selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
				if (validItems == null) {
					validItems = new List<List<int>> { new List<int>(), new List<int>(), new List<int>() };
					for (int i = 0; i < selectedBoss.collection.Count; i++) {
						int item = selectedBoss.collection[i];
						CollectionType type = selectedBoss.collectType[i];
						if (type == CollectionType.Trophy) validItems[0].Add(item);
						if (type == CollectionType.Mask) validItems[1].Add(item);
						if (type == CollectionType.MusicBox) validItems[2].Add(item);
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
			if (BossLogUI.PageNum >= 0) {
				selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
				if(selectedBoss.modSource == "Unknown" && Id == "PageTwo") return; // Prevents drawings on the page if the boss has no info
			}
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

			if (BossLogUI.PageNum == -1) { // Table of Contents
				List<BossInfo> bossList = BossChecklist.bossTracker.SortedBosses;
				Vector2 pos = new Vector2(GetInnerDimensions().X + 19, GetInnerDimensions().Y + 15);
				if (Id == "PageOne") {
					Utils.DrawBorderStringBig(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.PreHardmode"), pos, Colors.RarityAmber, 0.6f);
					if (BossChecklist.BossLogConfig.CountDownedBosses) {
						int totalPreHard = 0;
						int downedBosses = 0;
						for (int i = 0; i < bossList.Count; i++) {
							if (bossList[i].type == EntryType.Boss && bossList[i].progression <= 6f) {
								totalPreHard++;
								if (bossList[i].downed()) downedBosses++;
							}
						}
						string completion = $"{downedBosses}/{totalPreHard}";
						Vector2 stringSize = Main.fontMouseText.MeasureString(completion);
						pos = new Vector2(GetInnerDimensions().X + GetInnerDimensions().Width - stringSize.X - 16, GetInnerDimensions().Y + 15);
						Utils.DrawBorderString(spriteBatch, completion, pos, Color.White);
					}
				}
				else if (Id == "PageTwo") {
					Utils.DrawBorderStringBig(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Hardmode"), pos, Colors.RarityAmber, 0.6f);
					if (BossChecklist.BossLogConfig.CountDownedBosses) {
						int totalHard = 0;
						int downedBosses = 0;
						for (int i = 0; i < bossList.Count; i++) {
							if (bossList[i].type == EntryType.Boss && bossList[i].progression > 6f) {
								totalHard++;
								if (bossList[i].downed()) downedBosses++;
							}
						}
						string completion = $"{downedBosses}/{totalHard}";
						Vector2 stringSize = Main.fontMouseText.MeasureString(completion);
						pos = new Vector2(GetInnerDimensions().X + GetInnerDimensions().Width - stringSize.X - 26, GetInnerDimensions().Y + 15);
						Utils.DrawBorderString(spriteBatch, completion, pos, Color.White);
					}
				}

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
			else if (BossLogUI.PageNum == -2) { // Mod Developers Credits
				if (Id == "PageOne") {
					// Credits Page
					Vector2 stringPos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
					Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksDevs"), stringPos, Color.IndianRed);

					Texture2D users = BossChecklist.instance.GetTexture("Resources/Extra_CreditUsers");
					float textScaling = 0.75f;
					
					string username = "Jopojelly";
					Vector2 userpos = new Vector2(pageRect.X + 100, pageRect.Y + 75);
					Rectangle userselected = new Rectangle(0 + (59 * 1), 0, 59, 58);
					spriteBatch.Draw(users, userpos, userselected, Color.White);

					Vector2 stringAdjust = Main.fontMouseText.MeasureString(username);
					stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * 0.75f) / 2), userpos.Y - 25);
					Utils.DrawBorderString(spriteBatch, username, stringPos, Color.CornflowerBlue, textScaling);

					username = "Sheepish Shepherd";
					userpos = new Vector2(pageRect.X + 200, pageRect.Y + 75);
					userselected = new Rectangle(0 + (59 * 0), 0, 59, 58);
					spriteBatch.Draw(users, userpos, userselected, Color.White);

					stringAdjust = Main.fontMouseText.MeasureString(username);
					stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * 0.75f) / 2), userpos.Y - 25);
					Utils.DrawBorderString(spriteBatch, username, stringPos, Color.Goldenrod, textScaling);

					username = "direwolf420";
					userpos = new Vector2(pageRect.X + 50, pageRect.Y + 180);
					userselected = new Rectangle(0 + (59 * 3), 0, 59, 58);
					spriteBatch.Draw(users, userpos, userselected, Color.White);

					stringAdjust = Main.fontMouseText.MeasureString(username);
					stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * 0.75f) / 2), userpos.Y - 25);
					Utils.DrawBorderString(spriteBatch, username, stringPos, Color.Tomato, textScaling);

					username = "Orian";
					userpos = new Vector2(pageRect.X + 150, pageRect.Y + 180);
					userselected = new Rectangle(0 + (59 * 2), 0, 59, 58);
					spriteBatch.Draw(users, userpos, userselected, Color.White);

					stringAdjust = Main.fontMouseText.MeasureString(username);
					stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * 0.75f) / 2), userpos.Y - 25);
					Utils.DrawBorderString(spriteBatch, username, stringPos, new Color(49, 210, 162), textScaling);

					username = "Panini";
					userpos = new Vector2(pageRect.X + 241, pageRect.Y + 180);
					userselected = new Rectangle(0 + (59 * 4), 0, 68, 58);
					spriteBatch.Draw(users, userpos, userselected, Color.White);

					stringAdjust = Main.fontMouseText.MeasureString(username);
					stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * 0.75f) / 2), userpos.Y - 25);
					Utils.DrawBorderString(spriteBatch, username, stringPos, Color.HotPink, textScaling);

					// "Spriters"
					stringPos = new Vector2(pageRect.X + 20, pageRect.Y + 390);
					Utils.DrawBorderString(spriteBatch, "...and thank you RiverOaken for an amazing book sprite!", stringPos, Color.MediumPurple, textScaling);
				}

				if (Id == "PageTwo") { // Supported Mod Credits Page
					List<string> optedMods = new List<string>();
					foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
						if (boss.modSource != "Terraria" && boss.modSource != "Unknown") {
							string sourceDisplayName = boss.SourceDisplayName;
							if (!optedMods.Contains(sourceDisplayName)) {
								optedMods.Add(sourceDisplayName);
							}
						}
					}
					
					if (optedMods.Count > 0) {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 5, GetInnerDimensions().Y + 5);
						Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksMods"), pos, Color.LightSkyBlue);
						pos = new Vector2(GetInnerDimensions().X + 5, GetInnerDimensions().Y + 35);
						Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.Notice"), pos, Color.LightBlue);
					}
				}
			}
			else if (BossLogUI.PageNum >= 0) { // Boss Pages
				selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
				if (Id == "PageOne") {
					Texture2D bossTexture = null;
					Rectangle bossSourceRectangle = new Rectangle();
					if (selectedBoss.pageTexture != "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
						bossTexture = ModContent.GetTexture(selectedBoss.pageTexture);
						bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width, bossTexture.Height);
					}
					else if (selectedBoss.npcIDs.Count > 0) {
						Main.instance.LoadNPC(selectedBoss.npcIDs[0]);
						bossTexture = Main.npcTexture[selectedBoss.npcIDs[0]];
						bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width, bossTexture.Height / Main.npcFrameCount[selectedBoss.npcIDs[0]]);
					}
					if (bossTexture != null) {
						float drawScale = 1f;
						float xScale = (float)pageRect.Width / bossSourceRectangle.Width;
						// TODO: pageRect.Height might be too much, we might want to trim off the top a bit (May need adjusting, but changed to -150)
						float yScale = (float)(pageRect.Height - 150) / bossSourceRectangle.Height;
						if (xScale < 1 || yScale < 1) {
							drawScale = xScale < yScale ? xScale : yScale;
						}
						Color maskedBoss = BossLogUI.MaskBoss(selectedBoss);
						spriteBatch.Draw(bossTexture, pageRect.Center(), bossSourceRectangle, maskedBoss, 0, bossSourceRectangle.Center(), drawScale, SpriteEffects.None, 0f);
					}

					Rectangle firstHeadPos = new Rectangle();

					if (selectedBoss.type != EntryType.Event || selectedBoss.internalName == "Lunar Event") {
						int headsDisplayed = 0;
						int adjustment = 0;
						Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
						for (int h = selectedBoss.npcIDs.Count - 1; h > -1; h--) {
							Texture2D head = BossLogUI.GetBossHead(selectedBoss.npcIDs[h]);
							if (selectedBoss.overrideIconTexture != "") head = ModContent.GetTexture(selectedBoss.overrideIconTexture);
							if (BossLogUI.GetBossHead(selectedBoss.npcIDs[h]) != Main.npcHeadTexture[0]) {
								Rectangle headPos = new Rectangle(pageRect.X + pageRect.Width - head.Width - 10 - ((head.Width + 2) * adjustment), pageRect.Y + 5, head.Width, head.Height);
								if (headsDisplayed == 0) firstHeadPos = headPos;
								spriteBatch.Draw(head, headPos, maskedHead);
								headsDisplayed++;
								adjustment++;
							}
						}
						if (headsDisplayed == 0) {
							Texture2D noHead = Main.npcHeadTexture[0];
							Rectangle noHeadPos = new Rectangle(pageRect.X + pageRect.Width - noHead.Width - 10 - ((noHead.Width + 2) * adjustment), pageRect.Y + 5, noHead.Width, noHead.Height);
							firstHeadPos = noHeadPos;
							spriteBatch.Draw(noHead, noHeadPos, maskedHead);
						}
					}
					else {
						Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
						Texture2D eventIcon = BossLogUI.GetEventIcon(selectedBoss);
						Rectangle iconpos = new Rectangle(pageRect.X + pageRect.Width - eventIcon.Width - 10, pageRect.Y + 5, eventIcon.Width, eventIcon.Height);
						firstHeadPos = iconpos;
						spriteBatch.Draw(eventIcon, iconpos, maskedHead);
					}
					
					string isDefeated = $"[c/{Colors.RarityGreen.Hex3()}:{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}]";
					string notDefeated = $"[c/{Colors.RarityRed.Hex3()}:{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Undefeated", Main.worldName)}]";

					Texture2D texture = selectedBoss.downed() ? BossLogUI.checkMarkTexture : BossLogUI.xTexture;
					Vector2 defeatpos = new Vector2(firstHeadPos.X + (firstHeadPos.Width / 2), firstHeadPos.Y + firstHeadPos.Height - (texture.Height / 2));
					spriteBatch.Draw(texture, defeatpos, Color.White);
					if (Main.mouseX >= defeatpos.X && Main.mouseX < defeatpos.X + texture.Width) {
						if (Main.mouseY >= defeatpos.Y && Main.mouseY < defeatpos.Y + texture.Height) {
							Main.hoverItemName = selectedBoss.downed() ? isDefeated : notDefeated;
						}
					}
					else if (Main.mouseX >= firstHeadPos.X && Main.mouseX < firstHeadPos.X + firstHeadPos.Width) {
						if (Main.mouseY >= firstHeadPos.Y && Main.mouseY < firstHeadPos.Y + firstHeadPos.Height) {
							Main.hoverItemName = selectedBoss.downed() ? isDefeated : notDefeated;
						}
					}

					bool config = BossChecklist.DebugConfig.ShowInternalNames;

					Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
					Utils.DrawBorderString(spriteBatch, selectedBoss.name, pos, Color.Goldenrod);
					
					pos = new Vector2(pageRect.X + 5, pageRect.Y + (config ? 42 : 30));
					Utils.DrawBorderString(spriteBatch, selectedBoss.SourceDisplayName, pos, new Color(150, 150, 255));

					if (config) {
						pos = new Vector2(pageRect.X + 5, pageRect.Y + 25);
						Utils.DrawBorderString(spriteBatch, $"(Internal: {selectedBoss.internalName})", pos, Color.Goldenrod, 0.75f);
						
						pos = new Vector2(pageRect.X + 5, pageRect.Y + 60);
						Utils.DrawBorderString(spriteBatch, $"(Internal: {selectedBoss.modSource})", pos, new Color(150, 150, 255), 0.75f);
					}

					//pos = new Vector2(pageRect.X + 5, pageRect.Y + (config ? 75 : 55));
					//Utils.DrawBorderString(spriteBatch, selectedBoss.downed() ? isDefeated : notDefeated, pos, selectedBoss.downed() ? Colors.RarityGreen : Colors.RarityRed);

				}
				if (Id == "PageTwo" && BossLogUI.SubPageNum == 0 && selectedBoss.modSource != "Unknown") {
					if (selectedBoss.type != EntryType.Event) {
						// Boss Records Subpage
						Texture2D achievements = ModContent.GetTexture("Terraria/UI/Achievements");
						PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						BossStats record = modPlayer.AllBossRecords[BossLogUI.PageNum].stat;

						bool[] isNewRecord = new bool[4];
						string recordType = "";
						string recordNumbers = "";
						string compareNumbers = "";
						int[] achCoord = new int[] { 0, 0 };

						for (int i = 0; i < 4; i++) { // 4 Records total
							if (i == 0) { // Kills & Deaths
								if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
									recordType = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.KDR");
									int killTimes = record.kills;
									int deathTimes = record.deaths;
									achCoord = killTimes >= deathTimes ? new int[] { 4, 10 } : new int[] { 4, 8 };
									if (killTimes == 0 && deathTimes == 0) recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
									else recordNumbers = $"{killTimes} {Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Kills")} / {deathTimes} {Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths")}";
								}
								else {
									recordType = $"World Records";
									recordNumbers = $"for {Main.worldName}";
									achCoord = new int[] { 4, 6 };
								}
							}
							else if (i == 1) { // Fight Duration
								recordType = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Duration");
								achCoord = new int[] { 4, 9 };
								
								int BestRecord_ticks = record.durationBest;
								int PrevRecord_ticks = record.durationPrev;
								int LastAttempt = modPlayer.durationLastFight;
								
								if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
									isNewRecord[i] = LastAttempt == BestRecord_ticks && LastAttempt > 0;
									if (BestRecord_ticks > 0) {
										// Formatting for best record
										double BestRecord_seconds = (double)BestRecord_ticks / 60;
										int BestRecord_calcMin = (int)BestRecord_seconds / 60;
										double BestRecord_calcSec = BestRecord_seconds % 60;
										recordNumbers = $"{BestRecord_calcMin}:{BestRecord_calcSec.ToString("00.00")}";

										if (BestRecord_ticks != PrevRecord_ticks && PrevRecord_ticks > 0) {
											double PrevRecord_seconds = (double)PrevRecord_ticks / 60;
											int PrevRecord_calcMin = (int)PrevRecord_seconds / 60;
											double PrevRecord_calcSec = PrevRecord_seconds % 60;

											int Difference_ticks = BestRecord_ticks - PrevRecord_ticks;
											if (Difference_ticks < 0) Difference_ticks *= -1;
											string sign = isNewRecord[i] ? "+" : "-";
											double Difference_seconds = (double)Difference_ticks / 60;
											string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";

											string calcPrev = $"{PrevRecord_calcMin}:{PrevRecord_calcSec.ToString("00.00")}";
											string calcDiff = $"{Difference_seconds.ToString("0.00")}s";
											compareNumbers = $"{type} {calcPrev} ({sign}{calcDiff})";
										}
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
								else {
									WorldStats wldRcd = WorldAssist.worldRecords[BossLogUI.PageNum].stat;
									if (wldRcd.durationWorld > 0 && wldRcd.durationHolder != "") {
										double wld_seconds = (double)wldRcd.durationWorld / 60;
										int wld_calcMin = (int)wld_seconds / 60;
										double wld_calcSec = wld_seconds % 60;
										recordNumbers = $"{wld_calcMin}:{wld_calcSec.ToString("00.00")}";
										compareNumbers = wldRcd.durationHolder;
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
							}
							else if (i == 2) { // Hits Taken
								recordType = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Dodge");
								achCoord = new int[] { 0, 7 };

								int BestHits = record.hitsTakenBest;
								int PrevHits = record.hitsTakenPrev;
								int LastAttempt = modPlayer.hitsTakenLastFight;
								int Timer_ticks = record.dodgeTimeBest;

								if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
									isNewRecord[i] = LastAttempt == BestHits && LastAttempt > 0;
									if (BestHits >= 0) {
										double Timer_seconds = (double)Timer_ticks / 60;
										recordNumbers = $"{BestHits} hits [{Timer_seconds.ToString("0.00")}s]";

										if (BestHits != PrevHits && PrevHits >= 0) {
											string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";
											string sign = isNewRecord[i] ? "-" : "+";
											int difference = BestHits - PrevHits;
											if (difference < 0) difference *= -1;
											compareNumbers = $"{type} {PrevHits} hits ({sign}{difference})";
										}
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
								else {
									WorldStats wldRcd = WorldAssist.worldRecords[BossLogUI.PageNum].stat;
									if (wldRcd.hitsTakenWorld >= 0 && wldRcd.hitsTakenHolder != "") {
										double wld_seconds = (double)wldRcd.dodgeTimeWorld / 60;
										recordNumbers = $"{wldRcd.hitsTakenWorld} hits [{wld_seconds.ToString("0.00")}s]";
										compareNumbers = wldRcd.hitsTakenHolder;
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
							}
							else if (i == 3) { // Health Lost
								recordType = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Health");
								achCoord = new int[] { 3, 0 };

								int BestRecord = record.healthLossBest;
								int PrevRecord = record.healthLossPrev;
								int BestHealth = record.healthAtStart;
								int PrevHealth = record.healthAtStartPrev;
								int LastAttempt = modPlayer.healthLossLastFight;

								if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
									isNewRecord[i] = LastAttempt == BestRecord && LastAttempt > 0;
									if (BestRecord > 0 && BestHealth > 0) {
										double RecordPercent = (double)((BestRecord * 100) / BestHealth);
										recordNumbers = $"{BestRecord}/{BestHealth} [{RecordPercent.ToString("0")}%]";
										if (BestRecord != PrevRecord && PrevRecord > 0) {
											string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";
											string sign = isNewRecord[i] ? "-" : "+";
											int difference = BestRecord - PrevRecord;
											if (difference < 0) difference *= -1;
											compareNumbers = $"{type} {PrevRecord}/{PrevHealth} ({sign}{difference})";
										}
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
								else {
									WorldStats wldRcd = WorldAssist.worldRecords[BossLogUI.PageNum].stat;
									if ((wldRcd.healthLossWorld >= 0 && wldRcd.healthLossHolder != "") || wldRcd.healthAtStartWorld > 0) {
										double wldPercent = (double)((wldRcd.healthLossWorld * 100) / wldRcd.healthAtStartWorld);
										recordNumbers = $"{wldRcd.healthLossWorld}/{wldRcd.healthAtStartWorld} [{wldPercent}%]";
										compareNumbers = wldRcd.hitsTakenHolder;
									}
									else recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
								}
							}

							Rectangle posRect = new Rectangle(pageRect.X, pageRect.Y + 100 + (75 * i), 64, 64);
							Rectangle cutRect = new Rectangle(66 * achCoord[0], 66 * achCoord[1], 64, 64);
							spriteBatch.Draw(achievements, posRect, cutRect, Color.White);

							if (Main.mouseX >= posRect.X && Main.mouseX < posRect.X + 64) {
								if (Main.mouseY >= posRect.Y && Main.mouseY < posRect.Y + 64) {
									// TODO: Change these texts to something better. A description of the record type
									if (i == 0 && !BossLogUI.AltPage[i]) Main.hoverItemName = "Boss kills and player deaths.";
									if (i == 1) Main.hoverItemName = "The fastest you've defeated a mighty foe.";
									if (i == 2) Main.hoverItemName = "Avoid as many attacks as you can!";
									if (i == 3) Main.hoverItemName = "How close can you be to death and still defeat you enemy?";
								}
							}
							
							if (isNewRecord[i] && modPlayer.hasNewRecord[BossLogUI.PageNum]) {
								Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
								Rectangle exclam = new Rectangle(pageRect.X + 59, pageRect.Y + 96 + (75 * i), 9, 24);
								spriteBatch.Draw(text, exclam, Color.White);
							}

							int offsetY = compareNumbers == "" ? 110 + (i * 75) : 100 + (i * 75);

							Vector2 stringAdjust = Main.fontMouseText.MeasureString(recordType);
							Vector2 pos = new Vector2(GetInnerDimensions().X + (GetInnerDimensions().Width / 2 - 35) - (stringAdjust.X / 3), GetInnerDimensions().Y + offsetY);
							Utils.DrawBorderString(spriteBatch, recordType, pos, Color.Goldenrod);

							stringAdjust = Main.fontMouseText.MeasureString(recordNumbers);
							pos = new Vector2(GetInnerDimensions().X + (GetInnerDimensions().Width / 2 - 35) - (stringAdjust.X / 3), GetInnerDimensions().Y + offsetY + 25);
							Utils.DrawBorderString(spriteBatch, recordNumbers, pos, Color.White);

							if (compareNumbers != "") {
								stringAdjust = Main.fontMouseText.MeasureString(compareNumbers);
								float scale = 0.75f;
								pos = new Vector2(GetInnerDimensions().X + (GetInnerDimensions().Width / 2 - 35) - (stringAdjust.X * scale / 3), GetInnerDimensions().Y + offsetY + 50);
								Utils.DrawBorderString(spriteBatch, compareNumbers, pos, Color.White, scale);
								compareNumbers = "";
							}
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

								int bannerItemID = NPCLoader.GetNPC(npcID).bannerItem;
								if (bannerItemID == -1) continue;

								Item newItem = new Item();
								newItem.SetDefaults(bannerItemID);

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
				if (Id == "PageTwo" && BossLogUI.SubPageNum == 1) {
					// Spawn Item Subpage
				}

				if (Id == "PageTwo" && BossLogUI.SubPageNum == 2) {
					if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
						// Loot Table Subpage
						Texture2D bag = ModContent.GetTexture("BossChecklist/Resources/Extra_TreasureBag");
						Rectangle sourceRect = bag.Bounds;
						for (int i = 0; i < selectedBoss.loot.Count; i++) {
							Item bagItem = new Item();
							bagItem.SetDefaults(selectedBoss.loot[i]);
							bool foundModBag = bagItem.modItem != null && selectedBoss.npcIDs.Any(x => x == bagItem.modItem.BossBagNPC);
							if (BossLogUI.vanillaBags.Contains(bagItem.type) || foundModBag) {
								bag = Main.itemTexture[bagItem.type];
								DrawAnimation drawAnim = Main.itemAnimations[bagItem.type];
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
						
						int styleX = 0; // x coordinate of tile style
						int styleY = 0; // Y coordinate of tile style
						int top = 0; // top-left corner of tile style
						int left = 0; // top-left corner of tile style
						int width = 0;
						int height = 0;

						int offsetX = 0; // Offsets for multi-tiles
						int offsetY = 0;

						// PageNum already corresponds with the index of the saved player data

						Texture2D template = ModContent.GetTexture("BossChecklist/Resources/Extra_CollectionTemplate");
						Rectangle ctRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (template.Width / 2) - 20, pageRect.Y + 84, template.Width, template.Height);
						spriteBatch.Draw(template, ctRect, Color.White);

						// Draw Music Boxes
						Texture2D musicBoxTexture;
						Item musicBoxItem = new Item();
						musicBoxItem.SetDefaults(ItemID.MusicBoxOverworldDay);
						if (hasMusicBox) musicBoxItem.SetDefaults(selectedMusicBox);

						int musicBoxStyle = musicBoxItem.placeStyle;
						int musicBoxTileType = musicBoxItem.createTile;

						Main.instance.LoadTiles(musicBoxTileType);
						musicBoxTexture = Main.tileTexture[musicBoxTileType];

						if (musicBoxItem.createTile > 0) {
							TileObjectData data = TileObjectData.GetTileData(musicBoxTileType, musicBoxStyle);

							width = data.CoordinateWidth;
							height = data.CoordinateHeights[0];
							if (data.StyleWrapLimit > 0) {
								styleX = (musicBoxStyle % data.StyleWrapLimit) * data.CoordinateFullWidth;
								styleY = (musicBoxStyle / data.StyleWrapLimit) * data.CoordinateFullHeight;
							}
							else {
								styleY = musicBoxStyle * (height + 2) * data.Height;
							}
							left = styleX;
							top = styleY;
						}

						offsetX = offsetY = 0;
						
						for (int i = 0; i < 4; i++) {
							if (i != 0 && i % 2 == 0) {
								styleX = left;
								styleY += 18;
								offsetX = 0;
								offsetY++;
							}
							Rectangle posRect = new Rectangle(pageRect.X + 210 + styleX - left - (2 * offsetX), pageRect.Y + 160 + styleY - top - (2 * offsetY), 16, 16);
							Rectangle cutRect = new Rectangle(styleX, styleY, width, height);
							spriteBatch.Draw(musicBoxTexture, posRect, cutRect, Color.White);
							styleX += 18;
							offsetX++;
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
						styleX = styleY = top = left = width = height = 0;

						Texture2D trophyTexture;
						Item trophyItem = new Item();
						trophyItem.SetDefaults(ItemID.WeaponRack);
						if (hasTrophy) trophyItem.SetDefaults(selectedTrophy);

						int trophyStyle = trophyItem.placeStyle;
						int trophyTileType = trophyItem.createTile;

						Main.instance.LoadTiles(trophyTileType);
						trophyTexture = Main.tileTexture[trophyTileType];

						if (trophyItem.createTile > 0) {
							TileObjectData data = TileObjectData.GetTileData(trophyTileType, trophyStyle);
							
							width = data.CoordinateWidth;
							height = data.CoordinateHeights[0];
							if (data.StyleWrapLimit > 0) {
								styleX = (trophyStyle % data.StyleWrapLimit) * data.CoordinateFullWidth;
								styleY = (trophyStyle / data.StyleWrapLimit) * data.CoordinateFullHeight;
							}
							else {
								styleY = trophyStyle * (height + 2) * data.Height;
							}
							left = styleX;
							top = styleY;
						}

						offsetX = offsetY = 0;

						for (int i = 0; i < 9; i++) {
							if (i != 0 && i % 3 == 0) {
								styleX = left;
								styleY += 18;
								offsetX = 0;
								offsetY++;
							}
							Rectangle posRect = new Rectangle(pageRect.X + 98 + styleX - left - (2 * offsetX), pageRect.Y + 126 + styleY - top - (2 * offsetY), width, height);
							Rectangle cutRect = new Rectangle(styleX, styleY, width, height);
							spriteBatch.Draw(trophyTexture, posRect, cutRect, Color.White);
							styleX += 18;
							offsetX++;
						}
					}
				}
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
			if (!Id.EndsWith("_Tab")) base.DrawSelf(spriteBatch);
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

			if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
				// Needed to remove mousetext from outside sources when using the Boss Log
				Main.player[Main.myPlayer].mouseInterface = true;
				Main.mouseText = true;

				// Item icons such as hovering over a bed will not appear
				Main.LocalPlayer.showItemIcon = false;
				Main.LocalPlayer.showItemIcon2 = -1;
				Main.ItemIconCacheUpdate(0);
			}

			if (Id.EndsWith("_Tab")) {
				// Tab Icon
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Texture2D texture = BossLogUI.tocTexture;
				Vector2 pos = new Vector2(inner.X + Width.Pixels / 2 - 11, inner.Y + Height.Pixels / 2 - 11);

				if (Id == "Boss_Tab") texture = BossLogUI.bossNavTexture;
				else if (Id == "Miniboss_Tab") texture = BossLogUI.minibossNavTexture;
				else if (Id == "Event_Tab") texture = BossLogUI.eventNavTexture;
				else if (Id == "Credits_Tab") texture = BossLogUI.credTexture;
				else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum == -1) texture = BossLogUI.filterTexture;
				else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum != -1) texture = BossLogUI.tocTexture;

				if (DrawTab(Id)) spriteBatch.Draw(texture, pos, Color.White);
				else return;

				if (IsMouseHovering) {
					List<BossInfo> bossList = BossChecklist.bossTracker.SortedBosses;
					string tabMessage = "";
					if (Id == "Boss_Tab" && BossLogUI.FindNext(EntryType.Boss) != -1) {
						tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpBoss", bossList[BossLogUI.FindNext(EntryType.Boss)].name);
					}
					else if (Id == "Miniboss_Tab" && BossLogUI.FindNext(EntryType.MiniBoss) != -1) {
						tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpMini", bossList[BossLogUI.FindNext(EntryType.MiniBoss)].name);
					}
					else if (Id == "Event_Tab" && BossLogUI.FindNext(EntryType.Event) != -1) {
						tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpEvent", bossList[BossLogUI.FindNext(EntryType.Event)].name);
					}
					else if (Id == "Credits_Tab") tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpCred");
					else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum == -1) tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ToggleFilters");
					else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum != -1) tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpTOC");
					if (tabMessage != "") Main.hoverItemName = tabMessage;
				}
			}

			if (Id.Contains("C_") && IsMouseHovering) {
				if (Id == "C_0") Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterBosses.ToLower().Replace(" ", "")}");
				if (Id == "C_1") Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterMiniBosses.ToLower().Replace(" ", "")}");
				if (Id == "C_2") Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterEvents.ToLower().Replace(" ", "")}");
			}
		}
	}

	internal class TableOfContents : UIText
	{
		float order = 0;
		bool nextCheck;
		string bossName;
		string displayName;

		public TableOfContents(float order, string displayName, string bossName, bool nextCheck, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
			this.order = order;
			this.nextCheck = nextCheck;
			Recalculate();
			this.bossName = bossName;
			this.displayName = displayName;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);

			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 pos = new Vector2(innerDimensions.X - 20, innerDimensions.Y - 5);
			spriteBatch.Draw(BossLogUI.checkboxTexture, pos, Color.White);

			List<BossInfo> sortedBosses = BossChecklist.bossTracker.SortedBosses;
			// name check, for when progression matches
			// index should never be -1 since variables passed in are within bounds
			int index = sortedBosses.FindIndex(x => x.progression == order && (x.name == bossName || x.internalName == bossName));

			bool allLoot = false;
			bool allCollect = false;
			bool condCollect = false;
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

			foreach (int loot in sortedBosses[index].loot) {
				if (loot == 0 || loot == -1) continue;
				if (sortedBosses[index].npcIDs[0] < NPCID.Count) {
					if (WorldGen.crimson && (loot == ItemID.DemoniteOre || loot == ItemID.CorruptSeeds || loot == ItemID.UnholyArrow)) continue;
					else if (!WorldGen.crimson && (loot == ItemID.CrimtaneOre || loot == ItemID.CrimsonSeeds)) continue;
				}
				int indexLoot = modPlayer.BossTrophies[index].loot.FindIndex(x => x.Type == loot);
				if (indexLoot != -1) allLoot = true;
				else { // Item not obtained
					Item newItem = new Item();
					newItem.SetDefaults(loot);
					if (newItem.expert && !Main.expertMode) continue;
					else {
						allLoot = false;
						break;
					}
				}
			}
			if (sortedBosses[index].collection.Count == 0 || sortedBosses[index].collection.All(x => x <= 0)) {
				condCollect = true;
			}
			else {
				foreach (int collectible in sortedBosses[index].collection) {
					if (collectible == -1 || collectible == 0) continue;
					int indexCollect = modPlayer.BossTrophies[index].collectibles.FindIndex(x => x.Type == collectible);
					if (indexCollect != -1) allCollect = true;
					else {
						allCollect = false;
						break;
					}
				}
			}

			Vector2 pos2 = new Vector2(innerDimensions.X + Main.fontMouseText.MeasureString(displayName).X + 6, innerDimensions.Y - 2);

			if (allLoot && (allCollect || condCollect)) spriteBatch.Draw(BossLogUI.goldChestTexture, pos2, Color.White);
			else if (allLoot) spriteBatch.Draw(BossLogUI.chestTexture, pos2, Color.White);
			else if (allCollect) spriteBatch.Draw(BossLogUI.starTexture, pos2, Color.White);

			if (order != -1f) {
				BossChecklist BA = BossChecklist.instance;
				int pagenum = Convert.ToInt32(Id);
				Texture2D checkGrid = BossLogUI.checkboxTexture;

				if (sortedBosses[pagenum].downed()) {
					if (BossChecklist.BossLogConfig.SelectedCheckmarkType == "X and  ☐") checkGrid = BossLogUI.xTexture;
					else checkGrid = BossLogUI.checkMarkTexture;
				}
				else {
					if (BossChecklist.BossLogConfig.SelectedCheckmarkType == "✓ and  X") checkGrid = BossLogUI.xTexture;
					else checkGrid = BossLogUI.checkboxTexture;
					if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) checkGrid = BossLogUI.circleTexture;
				}

				spriteBatch.Draw(checkGrid, pos, Color.White);

				if (BossChecklist.BossLogConfig.ColoredBossText) {
					if (IsMouseHovering) TextColor = TextColor = Color.SkyBlue;
					//if (IsMouseHovering && sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.DarkSeaGreen;
					//else if (IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.IndianRed;
					else if (!sortedBosses[pagenum].downed() && nextCheck && BossChecklist.BossLogConfig.DrawNextMark) TextColor = new Color(248, 235, 91);
					else if (sortedBosses[pagenum].downed()) TextColor = Colors.RarityGreen;
					else if (!sortedBosses[pagenum].downed()) TextColor = Colors.RarityRed;
					if (modPlayer.hasNewRecord[pagenum]) TextColor = Main.DiscoColor;
				}
				else {
					if (IsMouseHovering) TextColor = new Color(80, 85, 100);
					else TextColor = new Color(140, 145, 160);
				}

				if ((!sortedBosses[pagenum].available() && !sortedBosses[pagenum].downed()) || sortedBosses[pagenum].hidden) {
					TextColor = Color.SlateGray;
					Vector2 stringAdjust = Main.fontMouseText.MeasureString(displayName);
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
		public FittedTextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
			this.text = text;
		}

		const float infoScaleX = 1f;
		const float infoScaleY = 1f;
		public override void Draw(SpriteBatch spriteBatch) {
			Rectangle hitbox = new Rectangle((int)GetInnerDimensions().X, (int)GetInnerDimensions().Y, (int)Width.Pixels, 100);
			
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
		
		public SubpageButton(string type) {
			buttonString = type;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			if (BossLogUI.PageNum < 0) return;

			if (buttonString == "Mods.BossChecklist.BossLog.DrawnText.Records" || buttonString == "LegacyInterface.101") {
				if (BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].type == EntryType.Event) buttonString = "LegacyInterface.101"; // Kill Count
				else buttonString = "Mods.BossChecklist.BossLog.DrawnText.Records";
			}
			BackgroundColor = Color.Brown;
			base.DrawSelf(spriteBatch);

			CalculatedStyle innerDimensions = GetInnerDimensions();
			string translated = Language.GetTextValue(buttonString);
			Vector2 stringAdjust = Main.fontMouseText.MeasureString(translated);
			Vector2 pos = new Vector2(innerDimensions.X + ((Width.Pixels - stringAdjust.X) / 2) - 12, innerDimensions.Y - 10);
			if (buttonString != "Disclaimer" && buttonString != "recordAlts") {
				DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, translated, pos, Color.Gold);
			}

			Texture2D text = ModContent.GetTexture("Terraria/UI/Achievement_Categories");
			Rectangle exclamPos = new Rectangle((int)GetInnerDimensions().X - 12, (int)GetInnerDimensions().Y - 12, 32, 32);

			if (buttonString == "") {
				if (BossLogUI.SubPageNum == 0) {
					if (!BossLogUI.AltPage[BossLogUI.SubPageNum]) {
						Rectangle exclamCut = new Rectangle(34 * 3, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to see the best records for this world\n(Multiplayer Under Construction!)";
					}
					else {
						Rectangle exclamCut = new Rectangle(34 * 2, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = "Click to see your personal records";
					}
				}
				else if (BossLogUI.SubPageNum == 1) {
					/* NO CURRENT ALTPAGE, BUTTON NOT NEEDED
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
						if (IsMouseHovering) Main.hoverItemName = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewCollect");
					}
					else {
						Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
						spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
						if (IsMouseHovering) Main.hoverItemName = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewLoot");
					}
				}
				else if (BossLogUI.SubPageNum == 3) {
					// Unused currently
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
		public static List<int> vanillaBags;

		// Cropped Textures
		public static Texture2D bookTexture;
		public static Texture2D borderTexture;
		public static Texture2D fadedTexture;
		public static Texture2D colorTexture;
		public static Texture2D prevTexture;
		public static Texture2D nextTexture;
		public static Texture2D tocTexture;
		public static Texture2D credTexture;
		public static Texture2D bossNavTexture;
		public static Texture2D minibossNavTexture;
		public static Texture2D eventNavTexture;
		public static Texture2D filterTexture;
		public static Texture2D checkMarkTexture;
		public static Texture2D xTexture;
		public static Texture2D circleTexture;
		public static Texture2D checkboxTexture;
		//public static Texture2D silverStarTexture; // unused
		public static Texture2D chestTexture;
		public static Texture2D starTexture;
		public static Texture2D goldChestTexture;
		
		public static int PageNum = -3; // Selected Boss Page (starts out with an invalid number for the initial check)
		public static int SubPageNum = 0; // Selected Topic Tab (Records, Spawn Info, Loot/Collection)
		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool[] AltPage; // AltPage for Records is "Player Best/World Best(Server)"

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
			else UpdateSubPage(SubPageNum);
			BossLogVisible = show;
			if (show) {
				// TODO: Small fix to update hidden list on open
				Main.playerInventory = false;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true; // Removes rainbow glow
			}
		}

		public override void OnInitialize() {
			// Book Button
			bookTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(0, 0, 34, 38));
			borderTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(36, 0, 34, 38));
			fadedTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(72, 0, 34, 38));
			colorTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Button"), new Rectangle(108, 0, 34, 38));

			// Nav
			prevTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(0, 0, 22, 22));
			nextTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(24, 0, 22, 22));
			tocTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(48, 0, 22, 22));
			credTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(72, 0, 22, 22));
			bossNavTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(0, 24, 22, 22));
			minibossNavTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(24, 24, 22, 22));
			eventNavTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(48, 24, 22, 22));
			filterTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Nav"), new Rectangle(72, 24, 22, 22));

			// Checks
			checkMarkTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(0, 0, 22, 20));
			xTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(24, 0, 22, 20));
			circleTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(48, 0, 22, 20));
			checkboxTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(72, 0, 22, 20));
			chestTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(24, 22, 22, 20));
			starTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(48, 22, 22, 20));
			goldChestTexture = CropTexture(BossChecklist.instance.GetTexture("Resources/LogUI_Checks"), new Rectangle(72, 22, 22, 20));

			vanillaBags = new List<int>() {
				ItemID.KingSlimeBossBag,
				ItemID.EyeOfCthulhuBossBag,
				ItemID.EaterOfWorldsBossBag,
				ItemID.BrainOfCthulhuBossBag,
				ItemID.QueenBeeBossBag,
				ItemID.SkeletronBossBag,
				ItemID.WallOfFleshBossBag,
				ItemID.TwinsBossBag,
				ItemID.DestroyerBossBag,
				ItemID.SkeletronPrimeBossBag,
				ItemID.PlanteraBossBag,
				ItemID.GolemBossBag,
				ItemID.FishronBossBag,
				ItemID.MoonLordBossBag,
				ItemID.BossBagBetsy
			};

			bosslogbutton = new BossAssistButton(bookTexture, "Mods.BossChecklist.BossLog.Terms.BossLog");
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

			filterCheckMark = new List<BookUI>();
			filterCheck = new List<BookUI>();
			filterTypes = new List<UIText>();

			for (int i = 0; i < 3; i++) {
				BookUI newCheck = new BookUI(checkMarkTexture);
				newCheck.Id = "C_" + i;
				filterCheckMark.Add(newCheck);

				BookUI newCheckBox = new BookUI(checkboxTexture);
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

			recordButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.Records");
			recordButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			recordButton.Height.Pixels = 25;
			recordButton.Left.Pixels = 0;
			recordButton.Top.Pixels = 15;
			recordButton.OnClick += (a, b) => UpdateSubPage(0);
			recordButton.OnRightDoubleClick += (a, b) => ResetStats();

			spawnButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.SpawnInfo");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 - 8;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += (a, b) => UpdateSubPage(1);

			lootButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.LootCollect");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2 - 16;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += (a, b) => UpdateSubPage(2);
			lootButton.OnRightDoubleClick += new MouseEvent(RemoveItem);

			toolTipButton = new SubpageButton("Disclaimer");
			toolTipButton.Width.Pixels = 32;
			toolTipButton.Height.Pixels = 32;
			toolTipButton.Left.Pixels = PageTwo.Width.Pixels - toolTipButton.Width.Pixels - 30;
			toolTipButton.Top.Pixels = 100;
			toolTipButton.OnClick += (a, b) => SwapRecordPage();
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
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") filterCheckMark[0].SetImage(checkMarkTexture);
				else filterCheckMark[0].SetImage(circleTexture);

				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") filterCheckMark[1].SetImage(checkMarkTexture);
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") filterCheckMark[1].SetImage(xTexture);
				else filterCheckMark[1].SetImage(circleTexture);

				if (BossChecklist.BossLogConfig.FilterEvents == "Show") filterCheckMark[2].SetImage(checkMarkTexture);
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") filterCheckMark[2].SetImage(xTexture);
				else filterCheckMark[2].SetImage(circleTexture);
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

			// Reset new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && modPlayer.hasNewRecord[PageNum]) {
				modPlayer.hasNewRecord[PageNum] = false;
			}

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
				UpdateSubPage(SubPageNum);
			}
		}

		// TODO: Test both ResetStats and RemoveItem() in multiplayer
		private void ResetStats() {
			if (BossChecklist.DebugConfig.ResetRecordsBool && SubPageNum == 0) {
				BossStats stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().AllBossRecords[PageNum].stat;
				stats.kills = 0;
				stats.deaths = 0;
				stats.durationBest = -1;
				stats.durationPrev = -1;
				stats.hitsTakenBest = -1;
				stats.hitsTakenPrev = -1;
				stats.dodgeTimeBest = -1;
				stats.healthLossBest = -1;
				stats.healthLossPrev = -1;
				stats.healthAtStart = -1;
				stats.healthAtStartPrev = -1;
				OpenRecord();
				
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					RecordID specificRecord = RecordID.ResetAll;
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.RecordUpdate);
					packet.Write((int)PageNum);
					stats.NetSend(packet, specificRecord);
					packet.Send(toClient: Main.LocalPlayer.whoAmI);
				}
			}
		}

		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			// Double right-click an item slot to remove that item from the selected boss page's loot/collection list
			// Double right-click the "Loot / Collection" button to entirely clear the selected boss page's loot/collection list
			// If holding Alt while right-clicking will do the above for ALL boss lists
			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();
			if (BossChecklist.DebugConfig.ResetLootItems && SubPageNum == 2) {
				string ID = listeningElement.Id;
				if (ID == "") {
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						for (int i = 0; i < modPlayer.BossTrophies.Count; i++) {
							if (AltPage[2]) modPlayer.BossTrophies[i].collectibles.Clear();
							else modPlayer.BossTrophies[i].loot.Clear();
						}
					}
					else {
						if (AltPage[2]) modPlayer.BossTrophies[PageNum].collectibles.Clear();
						else modPlayer.BossTrophies[PageNum].loot.Clear();
					}
				}
				else if (ID.StartsWith("collect_")) {
					int itemType = Convert.ToInt32(ID.Substring(8));
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						for (int i = 0; i < modPlayer.BossTrophies.Count; i++) {
							modPlayer.BossTrophies[i].collectibles.RemoveAll(x => x.Type == itemType);
						}
					}
					else {
						List<ItemDefinition> collection = modPlayer.BossTrophies[PageNum].collectibles;
						collection.RemoveAll(x => x.Type == itemType);
					}
				}
				else if (ID.StartsWith("loot_")) {
					int itemType = Convert.ToInt32(ID.Substring(5));
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						for (int i = 0; i < modPlayer.BossTrophies.Count; i++) {
							modPlayer.BossTrophies[i].loot.RemoveAll(x => x.Type == itemType);
						}
					}
					else {
						List<ItemDefinition> loot = modPlayer.BossTrophies[PageNum].loot;
						loot.RemoveAll(x => x.Type == itemType);
					}
				}
				OpenLoot();
			}
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
			// Reset new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && modPlayer.hasNewRecord[PageNum]) {
				modPlayer.hasNewRecord[PageNum] = false;
			}
			pageTwoItemList.Clear();
			prehardmodeList.Clear();
			hardmodeList.Clear();
			PageOne.RemoveChild(scrollOne);
			PageTwo.RemoveChild(scrollTwo);
			RecipeShown = 0;
			RecipePageNum = 0;
			BossLogPanel.validItems = null;

			// Move to next/prev
			List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
			if (listeningElement.Id == "Next") {
				if (PageNum < BossList.Count - 1) PageNum++;
				else PageNum = -2;
			}
			else { // button is previous
				if (PageNum >= 0) PageNum--;
				else PageNum = BossList.Count - 1;
			}

			// If the page is hidden or unavailable, keep moving till its not or until page is at either end
			if (PageNum >= 0 && (BossList[PageNum].hidden || !BossList[PageNum].available())) {
				while (PageNum >= 0) {
					BossInfo currentBoss = BossList[PageNum];
					if (!currentBoss.hidden && currentBoss.available()) {
						break;
					}
					if (listeningElement.Id == "Next") {
						if (PageNum < BossList.Count - 1) PageNum++;
						else PageNum = -2;
					}
					else { // button is previous
						if (PageNum >= 0) PageNum--;
						else PageNum = BossList.Count - 1;
					}
				}
			}
			ResetBothPages();
			UpdateSubPage(SubPageNum);
		}

		private void OpenRecord() {
			ResetBothPages();
			if (PageNum < 0) return;
			// Incase we want to put any UI stuff on these pages
		}

		private void OpenSpawn() {
			ResetBothPages();
			int TotalRecipes = 0;
			if (PageNum < 0) return;
			pageTwoItemList.Clear();

			BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (boss.modSource == "Unknown") return;
			
			var message = new UIMessageBox(Language.GetTextValue(boss.info));
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
			
			if (boss.spawnItem.Count == 0 || boss.spawnItem.All(x => x <= 0)) {
				string type = "";
				if (boss.type == EntryType.MiniBoss) type = "MiniBoss";
				else if (boss.type == EntryType.Event) type = "Event";
				else type = "Boss";
				UIText info = new UIText(Language.GetTextValue($"Mods.BossChecklist.BossLog.DrawnText.NoSpawn{type}"));
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
				finder.SetResult(boss.spawnItem[RecipePageNum]);

				foreach (Recipe recipe in finder.SearchRecipes()) {
					if (TotalRecipes == RecipeShown) {
						foreach (Item item in recipe.requiredItem) {
							Item clone = item.Clone();
							OverrideForGroups(recipe, clone);
							ingredients.Add(clone);
						}
						foreach (int tile in recipe.requiredTile) {
							if (tile != -1) requiredTiles.Add(tile);
						}
						if (recipe is ModRecipe modRecipe) {
							recipeMod = modRecipe.mod.DisplayName;
						}
					}
					TotalRecipes++;
				}
				spawn.SetDefaults(boss.spawnItem[RecipePageNum]);

				LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
				spawnItemSlot.Height.Pixels = 50;
				spawnItemSlot.Width.Pixels = 50;
				spawnItemSlot.Top.Pixels = 230;
				spawnItemSlot.Left.Pixels = 33 + (56 * 2);
				PageTwo.Append(spawnItemSlot);

				int row = 0;
				int col = 0;
				for (int k = 0; k < ingredients.Count; k++) {
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

					LogItemSlot craftItem = new LogItemSlot(craft, false, Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand"), ItemSlot.Context.EquipArmorVanity, 0.85f);
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
							if (WorldGen.crimson) altarType = Language.GetTextValue("MapObject.CrimsonAltar");
							else altarType = Language.GetTextValue("MapObject.DemonAltar");
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
					BossAssistButton CycleItem = new BossAssistButton(tocTexture, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.CycleRecipe"));
					CycleItem.Id = "CycleItem_" + TotalRecipes;
					CycleItem.Top.Pixels = 245;
					CycleItem.Left.Pixels = 40;
					CycleItem.Width.Pixels = 22;
					CycleItem.Height.Pixels = 22;
					CycleItem.OnClick += new MouseEvent(ChangeSpawnItem);
					PageTwo.Append(CycleItem);
				}

				string recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Noncraftable");
				if (TotalRecipes > 0) recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.RecipeFrom", recipeMod);

				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = -5;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);
			}
		}

		private void OpenLoot() {
			ResetBothPages();
			if (AltPage[SubPageNum]) {
				OpenCollect();
				return;
			}
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
				if (vanillaBags.Contains(shortcut.loot[i])) continue;
				Item expertItem = new Item();
				expertItem.SetDefaults(shortcut.loot[i]);
				if (expertItem.modItem != null && shortcut.npcIDs.Any(x => x == expertItem.modItem.BossBagNPC)) continue;
				if (expertItem.expert) {
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
				if (vanillaBags.Contains(shortcut.loot[i])) continue;
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
				if (loot.modItem != null && shortcut.npcIDs.Any(x => x == loot.modItem.BossBagNPC)) continue;
				if (!loot.expert) {
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

			List<BossInfo> referenceList = BossChecklist.bossTracker.SortedBosses;

			for (int i = 0; i < referenceList.Count; i++) {
				referenceList[i].hidden = WorldAssist.HiddenBosses.Contains(referenceList[i].Key);
				if (referenceList[i].modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported) continue;
				if ((!referenceList[i].available() || referenceList[i].hidden) && BossChecklist.BossLogConfig.HideUnavailable) continue;
				if (!referenceList[i].downed()) nextCheck++;
				if (nextCheck == 1) nextCheckBool = true;

				string displayName = referenceList[i].name;
				if (BossChecklist.DebugConfig.ShowInternalNames) displayName = referenceList[i].internalName;
				else if (!referenceList[i].available() && !referenceList[i].downed()) displayName = "???";

				TableOfContents next = new TableOfContents(referenceList[i].progression, displayName, referenceList[i].name, nextCheckBool);
				nextCheckBool = false;

				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;
				EntryType type = referenceList[i].type;

				next.PaddingTop = 5;
				next.PaddingLeft = 22;
				next.Id = i.ToString();
				next.OnClick += new MouseEvent(JumpToBossPage);

				if (referenceList[i].downed()) {
					next.TextColor = Colors.RarityGreen;
					if ((mbFilter == "Show" && type == EntryType.MiniBoss) || (eFilter == "Show" && type == EntryType.Event) || (type == EntryType.Boss && bFilter == "Show")) {
						if (referenceList[i].progression <= 6f) prehardmodeList.Add(next);
						else hardmodeList.Add(next);
					}
				}
				else {
					nextCheck++;
					next.TextColor = Colors.RarityRed;
					if (!referenceList[i].available()) next.TextColor = Color.SlateGray;
					if ((mbFilter != "Hide" && type == EntryType.MiniBoss) || (eFilter != "Hide" && type == EntryType.Event) || type == EntryType.Boss) {
						if (referenceList[i].progression <= 6f) prehardmodeList.Add(next);
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
			pageTwoItemList.Clear();

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 90;
			scrollTwo.Left.Pixels = -24;
			scrollTwo.Height.Set(-60f, 0.75f);
			scrollTwo.HAlign = 1f;

			if (optedMods.Count > 0) {
				foreach (string mod in optedMods) {
					UIText modListed = new UIText("●" + mod, 0.85f) {
						PaddingTop = 8,
						PaddingLeft = 5
					};
					pageTwoItemList.Add(modListed);
				}
				if (optedMods.Count > 11) PageTwo.Append(scrollTwo);
				PageTwo.Append(pageTwoItemList);
				pageTwoItemList.SetScrollbar(scrollTwo);
			}
			else // No mods are using the Log
			{
				UIPanel brokenPanel = new UIPanel();
				brokenPanel.Height.Pixels = 220;
				brokenPanel.Width.Pixels = 340;
				brokenPanel.Top.Pixels = 120;
				brokenPanel.Left.Pixels = 3;
				PageTwo.Append(brokenPanel);

				FittedTextPanel brokenDisplay = new FittedTextPanel(Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.NoModsSupported"));
				brokenDisplay.Height.Pixels = 200;
				brokenDisplay.Width.Pixels = 340;
				brokenDisplay.Top.Pixels = 0;
				brokenDisplay.Left.Pixels = -15;
				brokenPanel.Append(brokenDisplay);
			}
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
			UpdateSubPage(SubPageNum);
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

					FittedTextPanel brokenDisplay = new FittedTextPanel(Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.LogFeaturesNotAvailable"));
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

					FittedTextPanel brokenDisplay = new FittedTextPanel(Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.NotImplemented"));
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

			if (PageNum == -2) PageOne.Append(PrevPage);
			else if (PageNum == -1) PageTwo.Append(NextPage);
			else {
				BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (boss.modSource != "Unknown") {
					bool eventCheck = SubPageNum == 0 && boss.type == EntryType.Event;
					if (!eventCheck && SubPageNum != 1) { // Not needed for Spawn Info currently
						toolTipButton = new SubpageButton("");
						toolTipButton.Width.Pixels = 32;
						toolTipButton.Height.Pixels = 32;
						toolTipButton.Left.Pixels = PageTwo.Width.Pixels - toolTipButton.Width.Pixels - 30;
						toolTipButton.Top.Pixels = 86;
						toolTipButton.OnClick += (a, b) => SwapRecordPage();
						PageTwo.Append(toolTipButton);
					}
				}
				PageTwo.Append(NextPage);
				PageOne.Append(PrevPage);
			}
		}

		public void UpdateSubPage(int subpage) {
			SubPageNum = subpage;
			if (PageNum == -1) UpdateTableofContents(); // Handle new page
			else if (PageNum == -2) UpdateCredits();
			else {
				if (SubPageNum == 0) OpenRecord();
				else if (SubPageNum == 1) OpenSpawn();
				else if (SubPageNum == 2) OpenLoot();
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
			if (boss.overrideIconTexture != "" && boss.overrideIconTexture != "Terraria/NPC_Head_0") return ModContent.GetTexture(boss.overrideIconTexture);
			switch (boss.internalName) {
				case "Frost Legion": return ModContent.GetTexture("Terraria/Extra_7");
				case "Frost Moon": return ModContent.GetTexture("Terraria/Extra_8");
				case "Goblin Army": return ModContent.GetTexture("Terraria/Extra_9");
				case "Martian Madness": return ModContent.GetTexture("Terraria/Extra_10");
				case "Pirate Invasion": return ModContent.GetTexture("Terraria/Extra_11");
				case "Pumpkin Moon": return ModContent.GetTexture("Terraria/Extra_12");
				case "Old One's Army": return BossLogUI.GetBossHead(NPCID.DD2LanePortal);
				case "Blood Moon": return BossChecklist.instance.GetTexture("Resources/BossTextures/EventBloodMoon_Head");
				case "Solar Eclipse": return BossChecklist.instance.GetTexture("Resources/BossTextures/EventSolarEclipse_Head");
				default: return Main.npcHeadTexture[0];
			}
		}

		public static Texture2D CropTexture(Texture2D texture, Rectangle snippet) {
			Texture2D croppedTexture = new Texture2D(Main.graphics.GraphicsDevice, snippet.Width, snippet.Height);
			Color[] data = new Color[snippet.Width * snippet.Height];
			texture.GetData(0, snippet, data, 0, data.Length);
			croppedTexture.SetData(data);
			return croppedTexture;
		}

		public static void OverrideForGroups(Recipe recipe, Item item) {
			// This method taken from RecipeBrowser with permission.
			string nameOverride;
			if (recipe.ProcessGroupsForText(item.type, out nameOverride)) {
				//Main.toolTip.name = name;
			}
			if (recipe.anyIronBar && item.type == 22) {
				nameOverride = Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(22);
			}
			else if (recipe.anyWood && item.type == 9) {
				nameOverride = Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(9);
			}
			else if (recipe.anySand && item.type == 169) {
				nameOverride = Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(169);
			}
			else if (recipe.anyFragment && item.type == 3458) {
				nameOverride = Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("LegacyMisc.51");
			}
			else if (recipe.anyPressurePlate && item.type == 542) {
				nameOverride = Language.GetTextValue("LegacyMisc.37") + " " + Language.GetTextValue("LegacyMisc.38");
			}
			if (nameOverride != "") {
				item.SetNameOverride(nameOverride);
			}
		}
	}
}
