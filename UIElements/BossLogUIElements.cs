using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist.UIElements
{
	class BossLogUIElements
	{
		internal class BossAssistButton : UIImageButton
		{
			internal string buttonType;
			internal Texture2D texture;
			internal int cycleFrame = 0;
			internal bool slowDown = true;
			private Vector2 offset;
			internal bool dragging;

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
				if (Id == "OpenUI") {
					DragStart(evt);
				}
			}

			public override void RightMouseUp(UIMouseEvent evt) {
				base.RightMouseUp(evt);
				if (Id == "OpenUI") {
					DragEnd(evt);
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (Id != "OpenUI") {
					return;
				}
				if (ContainsPoint(Main.MouseScreen)) {
					Main.LocalPlayer.mouseInterface = true;
				}

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
					if (!myPlayer.hasOpenedTheBossLog) {
						spriteBatch.Draw(BossLogUI.borderTexture, innerDimensions.ToRectangle(), Main.DiscoColor);
					}
					else if (BossChecklist.DebugConfig.NewRecordsDisabled || BossChecklist.DebugConfig.RecordTrackingDisabled) {
						spriteBatch.Draw(BossLogUI.borderTexture, innerDimensions.ToRectangle(), Color.IndianRed);
					}

					if (myPlayer.hasNewRecord.Any(x => x == true)) {
						slowDown = !slowDown;
						if (slowDown) {
							cycleFrame++;
						}
						if (cycleFrame >= 19) {
							cycleFrame = 0;
						}

						Texture2D bookBorder = BossChecklist.instance.GetTexture("Resources/LogUI_ButtonBorder");
						Rectangle source = new Rectangle(0, 40 * cycleFrame, 34, 38);
						spriteBatch.Draw(bookBorder, innerDimensions.ToRectangle(), source, BossChecklist.BossLogConfig.BossLogColor);
					}
					else if (IsMouseHovering) {
						spriteBatch.Draw(BossLogUI.borderTexture, innerDimensions.ToRectangle(), Color.Goldenrod);
					}

					// Drawing the entire book while dragging if the mouse happens to go off screen/out of window
					if (dragging) {
						spriteBatch.Draw(texture, innerDimensions.ToRectangle(), Color.White);
					}
				}

				if (IsMouseHovering && !dragging) {
					BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
					if (!Id.StartsWith("CycleItem")) {
						DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontMouseText, translated, pos, Color.White);
					}
					else {
						Main.hoverItemName = buttonType;
					}
				}
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					Main.player[Main.myPlayer].mouseInterface = true;
				}
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

				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
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
				bool hiddenItemUnobtained = (Id.Contains("loot_") || Id.Contains("collect_")) && !Collection.loot.Contains(new ItemDefinition(item.type)) && !Collection.collectibles.Contains(new ItemDefinition(item.type));
				if (BossChecklist.BossLogConfig.BossSilhouettes && !selectedBoss.downed() && hiddenItemUnobtained) {
					spriteBatch.Draw(Main.inventoryBack13Texture, rectangle.TopLeft(), Main.inventoryBack13Texture.Bounds, Color.DimGray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
					Texture2D hiddenItem = Main.npcHeadTexture[0];
					Vector2 vec = new Vector2(rectangle.X + ((rectangle.Width * scale) / 2) - (hiddenItem.Width / 2), rectangle.Y + ((rectangle.Height * scale) / 2) - (hiddenItem.Height / 2));
					spriteBatch.Draw(hiddenItem, vec, Color.White);
					if (IsMouseHovering) {
						Main.hoverItemName = $"Defeat {selectedBoss.name} to view obtainable {(BossLogUI.AltPage[2] ? "collectibles" : "loot")}.\n(This can be turned off with the silhouettes config)";
					}
					return;
				}
				else if (item.type != 0 || hoverText == demonAltar || hoverText == crimsonAltar || Id.StartsWith("ingredient_")) {
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
				}

				Main.inventoryBack6Texture = backup;
				Main.inventoryBack7Texture = backup2;

				if (hoverText == crimsonAltar || hoverText == demonAltar) {
					Main.instance.LoadTiles(TileID.DemonAltar);
					int offsetX = 0;
					int offsetY = 0;
					int offsetSrc = 0;
					if (hoverText == crimsonAltar) {
						offsetSrc = 3;
					}
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
					if (hasItem) {
						spriteBatch.Draw(BossLogUI.checkMarkTexture, rect, Color.White); // hasItem first priority
					}
					else if (!Main.expertMode && (item.expert || item.expertOnly)) {
						spriteBatch.Draw(BossLogUI.xTexture, rect, Color.White);
					}
				}

				if (Id.StartsWith("collect_") && BossChecklist.DebugConfig.ShowCollectionType) {
					string showType = "";
					BossInfo boss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
					int index = boss.collection.FindIndex(x => x == item.type);
					CollectionType type = boss.collectType[index];
					if (type == CollectionType.Trophy) {
						showType = "Trophy";
					}
					else if (type == CollectionType.MusicBox) {
						showType = "Music";
					}
					else if (type == CollectionType.Mask) {
						showType = "Mask";
					}

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
					else {
						Main.hoverItemName = hoverText;
					}
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
			private int itemTimer = 240;
			private int shownPage = 0;
			public static bool shownAltPage = false;
			private int[] itemShown;
			private List<List<int>> validItems;
			public static int headNum = -1;

			public override void Draw(SpriteBatch spriteBatch) {
				BossInfo selectedBoss;
				// Pre-drawing
				// PageTwo check to prevent the timer from counting down twice (once for each page)
				if (BossLogUI.PageNum >= 0 && BossLogUI.SubPageNum == 2 && BossLogUI.AltPage[BossLogUI.SubPageNum] && Id == "PageTwo") {
					// This page check allows this code to only run when the page has changed.
					if (shownPage != BossLogUI.PageNum || !shownAltPage) {
						shownPage = BossLogUI.PageNum;
						shownAltPage = true;
						selectedBoss = BossChecklist.bossTracker.SortedBosses[shownPage];
						validItems = new List<List<int>> { new List<int>(), new List<int>(), new List<int>() };
						for (int i = 0; i < selectedBoss.collection.Count; i++) {
							int item = selectedBoss.collection[i];
							CollectionType type = selectedBoss.collectType[i];
							if (type == CollectionType.Trophy) {
								validItems[0].Add(item);
							}
							if (type == CollectionType.Mask) {
								validItems[1].Add(item);
							}
							if (type == CollectionType.MusicBox) {
								validItems[2].Add(item);
							}
						}
						if (validItems[0].Count == 0) {
							validItems[0].Add(0);
						}
						if (validItems[1].Count == 0) {
							validItems[1].Add(0);
						}
						if (validItems[2].Count == 0) {
							validItems[2].Add(0);
						}
						itemShown = new int[] { 0, 0, 0 };
						itemTimer = 240;
					}

					// The timer to cycle through multiple items in a given collection type
					if (itemTimer <= 0) {
						itemTimer = 240; // Items cycle through every 4 seconds
						for (int i = 0; i < itemShown.Length; i++) {
							if (itemShown[i] == validItems[i].Count - 1) {
								itemShown[i] = 0;
							}
							else {
								itemShown[i]++;
							}
						}
					}
					else {
						itemTimer--;
					}
				}

				base.Draw(spriteBatch);
				if (BossLogUI.PageNum >= 0) {
					selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
					if (selectedBoss.modSource == "Unknown" && Id == "PageTwo") {
						return; // Prevents drawings on the page if the boss has no info
					}
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
								if (bossList[i].type == EntryType.Boss && bossList[i].progression <= 6f && (bossList[i].available() || bossList[i].downed())) {
									totalPreHard++;
									if (bossList[i].downed()) {
										downedBosses++;
									}
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
									if (bossList[i].downed()) {
										downedBosses++;
									}
								}
							}
							string completion = $"{downedBosses}/{totalHard}";
							Vector2 stringSize = Main.fontMouseText.MeasureString(completion);
							pos = new Vector2(GetInnerDimensions().X + GetInnerDimensions().Width - stringSize.X - 26, GetInnerDimensions().Y + 15);
							Utils.DrawBorderString(spriteBatch, completion, pos, Color.White);
						}
					}

					if (!IsMouseHovering) {
						headNum = -1;
					}

					if (headNum != -1) {
						BossInfo headBoss = BossChecklist.bossTracker.SortedBosses[headNum];
						if (headBoss.type != EntryType.Event || headBoss.internalName == "Lunar Event") {
							int headsDisplayed = 0;
							int adjustment = 0;
							Color maskedHead = BossLogUI.MaskBoss(headBoss);
							for (int h = 0; h < headBoss.npcIDs.Count; h++) {
								Texture2D head = BossLogUI.GetBossHead(headBoss.npcIDs[h]);
								if (headBoss.overrideIconTexture != "") {
									head = ModContent.GetTexture(headBoss.overrideIconTexture);
								}
								if (head != Main.npcHeadTexture[0]) {
									headsDisplayed++;
									spriteBatch.Draw(head, new Rectangle(Main.mouseX + 15 + ((head.Width + 2) * adjustment), Main.mouseY + 15, head.Width, head.Height), maskedHead);
									adjustment++;
								}
							}
							Texture2D noHead = Main.npcHeadTexture[0];
							if (headsDisplayed == 0) {
								spriteBatch.Draw(noHead, new Rectangle(Main.mouseX + 15 + ((noHead.Width + 2) * adjustment), Main.mouseY + 15, noHead.Width, noHead.Height), maskedHead);
							}
						}
						else {
							Color maskedHead = BossLogUI.MaskBoss(headBoss);
							Texture2D eventIcon = BossLogUI.GetEventIcon(headBoss);
							Rectangle iconpos = new Rectangle(Main.mouseX + 15, Main.mouseY + 15, eventIcon.Width, eventIcon.Height);
							if (eventIcon != Main.npcHeadTexture[0]) {
								spriteBatch.Draw(eventIcon, iconpos, maskedHead);
							}
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
								if (head != Main.npcHeadTexture[0]) {
									Rectangle headPos = new Rectangle(pageRect.X + pageRect.Width - head.Width - 10 - ((head.Width + 2) * adjustment), pageRect.Y + 5, head.Width, head.Height);
									if (headsDisplayed == 0) {
										firstHeadPos = headPos;
									}
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

						// Hovering over the head icon will display the defeated text
						if (Main.mouseX >= firstHeadPos.X && Main.mouseX < firstHeadPos.X + firstHeadPos.Width) {
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
										if (killTimes == 0 && deathTimes == 0) {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										}
										else {
											recordNumbers = $"{killTimes} {Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Kills")} / {deathTimes} {Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths")}";
										}
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
										if (BestRecord_ticks == -1) {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
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
												if (Difference_ticks < 0) {
													Difference_ticks *= -1;
												}
												string sign = isNewRecord[i] ? "+" : "-";
												double Difference_seconds = (double)Difference_ticks / 60;
												string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";

												string calcPrev = $"{PrevRecord_calcMin}:{PrevRecord_calcSec.ToString("00.00")}";
												string calcDiff = $"{Difference_seconds.ToString("0.00")}s";
												compareNumbers = $"{type} {calcPrev} ({sign}{calcDiff})";
											}
										}
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
										else {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
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
										if (BestHits == -1) {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											double Timer_seconds = (double)Timer_ticks / 60;
											recordNumbers = $"{BestHits} hits [{Timer_seconds.ToString("0.00")}s]";

											if (BestHits != PrevHits && PrevHits >= 0) {
												string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";
												string sign = isNewRecord[i] ? "-" : "+";
												int difference = BestHits - PrevHits;
												if (difference < 0) {
													difference *= -1;
												}
												compareNumbers = $"{type} {PrevHits} hits ({sign}{difference})";
											}
										}
									}
									else {
										WorldStats wldRcd = WorldAssist.worldRecords[BossLogUI.PageNum].stat;
										if (wldRcd.hitsTakenWorld >= 0 && wldRcd.hitsTakenHolder != "") {
											double wld_seconds = (double)wldRcd.dodgeTimeWorld / 60;
											recordNumbers = $"{wldRcd.hitsTakenWorld} hits [{wld_seconds.ToString("0.00")}s]";
											compareNumbers = wldRcd.hitsTakenHolder;
										}
										else {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
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
										if (BestRecord == -1 || BestHealth == -1) {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											double RecordPercent = (double)BestRecord / BestHealth;
											recordNumbers = $"{BestRecord}/{BestHealth} [{RecordPercent.ToString("###.##%")}]";
											if (BestRecord != PrevRecord && PrevRecord > 0) {
												string type = isNewRecord[i] ? "Previous Best:" : "Last Attempt:";
												string sign = isNewRecord[i] ? "-" : "+";
												int difference = BestRecord - PrevRecord;
												if (difference < 0) {
													difference *= -1;
												}
												compareNumbers = $"{type} {PrevRecord}/{PrevHealth} ({sign}{difference})";
											}
										}
									}
									else {
										WorldStats wldRcd = WorldAssist.worldRecords[BossLogUI.PageNum].stat;
										if ((wldRcd.healthLossWorld >= 0 && wldRcd.healthLossHolder != "") || wldRcd.healthAtStartWorld > 0) {
											double wldPercent = (double)wldRcd.healthLossWorld / wldRcd.healthAtStartWorld;
											recordNumbers = $"{wldRcd.healthLossWorld}/{wldRcd.healthAtStartWorld} [{wldPercent.ToString("###.##%")}]";
											compareNumbers = wldRcd.hitsTakenHolder;
										}
										else {
											recordNumbers = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
									}
								}

								Rectangle posRect = new Rectangle(pageRect.X, pageRect.Y + 100 + (75 * i), 64, 64);
								Rectangle cutRect = new Rectangle(66 * achCoord[0], 66 * achCoord[1], 64, 64);
								spriteBatch.Draw(achievements, posRect, cutRect, Color.White);

								if (Main.mouseX >= posRect.X && Main.mouseX < posRect.X + 64) {
									if (Main.mouseY >= posRect.Y && Main.mouseY < posRect.Y + 64) {
										// TODO: Change these texts to something better. A description of the record type
										if (i == 0 && !BossLogUI.AltPage[i]) {
											Main.hoverItemName = "Boss kills and player deaths.";
										}
										if (i == 1) {
											Main.hoverItemName = "The fastest you've defeated a mighty foe.";
										}
										if (i == 2) {
											Main.hoverItemName = "Avoid as many attacks as you can!";
										}
										if (i == 3) {
											Main.hoverItemName = "How close can you be to death and still defeat you enemy?";
										}
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
								if (offset == 0 && offsetY == 5) {
									break; // For now, we stop drawing any banners that exceed the books limit (might have to reimplement as a UIList for scrolling purposes)
								}
								int npcID = selectedBoss.npcIDs[i];
								if (npcID < NPCID.Count) {
									int init = Item.NPCtoBanner(npcID) + 21;
									if (init <= 21) {
										continue;
									}

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
										if (NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem]) {
											faded = Color.White;
										}
									}
									else {
										continue;
									}

									for (int j = 0; j < 3; j++) {
										Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset, GetInnerDimensions().ToRectangle().Y + 100 + 16 * j + offsetY);
										Rectangle rect = new Rectangle(init * 18, (jump * 18) + (j * 18), 16, 16);
										spriteBatch.Draw(banner, pos, rect, faded);

										if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + 16) {
											if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + 16) {
												string killcount = $"{Lang.GetNPCNameValue(npcID)}: {NPC.killCount[Item.NPCtoBanner(npcID)]}";
												if (NPC.killCount[Item.NPCtoBanner(npcID)] < ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]) {
													killcount += $" / {ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]}";
												}
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
									if (bannerItemID <= 0) {
										continue;
									}

									Item newItem = new Item();
									newItem.SetDefaults(bannerItemID);
									if (newItem.createTile <= -1) {
										continue;
									}

									Main.instance.LoadTiles(newItem.createTile);
									Texture2D banner = Main.tileTexture[newItem.createTile];

									// Code adapted from TileObject.DrawPreview
									var tileData = TileObjectData.GetTileData(newItem.createTile, newItem.placeStyle);
									int styleColumn = tileData.CalculatePlacementStyle(newItem.placeStyle, 0, 0); // adjust for StyleMultiplier
									int styleRow = 0;
									//int num3 = tileData.DrawYOffset;
									if (tileData.StyleWrapLimit > 0) {
										styleRow = styleColumn / tileData.StyleWrapLimit * tileData.StyleLineSkip; // row quotient
										styleColumn %= tileData.StyleWrapLimit; // remainder
									}

									int x;
									int y;
									if (tileData.StyleHorizontal) {
										x = tileData.CoordinateFullWidth * styleColumn;
										y = tileData.CoordinateFullHeight * styleRow;
									}
									else {
										x = tileData.CoordinateFullWidth * styleRow;
										y = tileData.CoordinateFullHeight * styleColumn;
									}

									int bannerID = NPCLoader.GetNPC(npcID).banner;
									string source = NPCLoader.GetNPC(npcID).mod.DisplayName;

									Color faded = NPC.killCount[bannerID] >= 50 ? Color.White : new Color(128, 128, 128, 128);

									int[] heights = tileData.CoordinateHeights;
									int heightOffSet = 0;
									int heightOffSetTexture = 0;
									for (int j = 0; j < heights.Length; j++) { // could adjust for non 1x3 here and below if we need to.
										Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset, GetInnerDimensions().ToRectangle().Y + 100 + heightOffSet + offsetY);
										Rectangle rect = new Rectangle(x, y + heightOffSetTexture, tileData.CoordinateWidth, tileData.CoordinateHeights[j]);
										Main.spriteBatch.Draw(banner, pos, rect, faded);
										heightOffSet += heights[j];
										heightOffSetTexture += heights[j] + tileData.CoordinatePadding;

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
								int bagItem = selectedBoss.loot[i];
								if (BossChecklist.registeredBossBagTypes.Contains(bagItem)) {
									bag = Main.itemTexture[bagItem];
									DrawAnimation drawAnim = Main.itemAnimations[bagItem];
									sourceRect = drawAnim != null ? sourceRect = drawAnim.GetFrame(bag) : bag.Bounds;
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
							if (hasMusicBox) {
								musicBoxItem.SetDefaults(selectedMusicBox);
							}

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
								else {
									mask = ModContent.GetTexture(ItemLoader.GetItem(selectedMask).Texture + "_Head");
								}

								int frameCut = mask.Height / 24;
								Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - (mask.Width / 2) - 8, pageRect.Y + (pageRect.Height / 2) - (frameCut / 2) - 86, mask.Width, frameCut);
								Rectangle cutRect = new Rectangle(0, 0, mask.Width, frameCut);
								spriteBatch.Draw(mask, posRect, cutRect, Color.White);
							}

							// Draw Trophies
							styleX = styleY = top = left = width = height = 0;

							Texture2D trophyTexture;
							Item trophyItem = new Item();
							trophyItem.SetDefaults(hasTrophy ? selectedTrophy : ItemID.WeaponRack);

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
				bool MatchesCreditsTab = BossLogUI.PageNum == -2 && Id == "Credits_Tab";
				bool MatchesBossTab = BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Boss) && Id == "Boss_Tab";
				bool MatchesMinibossTab = BossLogUI.PageNum == BossLogUI.FindNext(EntryType.MiniBoss) && Id == "Miniboss_Tab";
				bool MatchesEventTab = BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Event) && Id == "Event_Tab";
				return !(MatchesCreditsTab || MatchesBossTab || MatchesMinibossTab || MatchesEventTab);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				if (Id == "ToCFilter_Tab") {
					Texture2D pages = BossChecklist.instance.GetTexture("Resources/LogUI_Back");
					Vector2 pagePos = new Vector2((Main.screenWidth / 2) - 400, (Main.screenHeight / 2) - 250);
					spriteBatch.Draw(pages, pagePos, BossChecklist.BossLogConfig.BossLogColor);
				}
				if (!Id.EndsWith("_Tab")) {
					base.DrawSelf(spriteBatch);
				}
				else {
					// Tab drawing
					SpriteEffects effect = SpriteEffects.FlipHorizontally;
					if (Left.Pixels <= 0) {
						effect = SpriteEffects.None;
					}

					Color color = new Color(153, 199, 255);
					if (Id == "Boss_Tab") {
						color = new Color(255, 168, 168);
					}
					else if (Id == "Miniboss_Tab") {
						color = new Color(153, 253, 119);
					}
					else if (Id == "Event_Tab") {
						color = new Color(196, 171, 254);
					}
					else if (Id == "Credits_Tab") {
						color = new Color(218, 175, 133);
					}
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

					if (Id == "Boss_Tab") {
						texture = BossLogUI.bossNavTexture;
					}
					else if (Id == "Miniboss_Tab") {
						texture = BossLogUI.minibossNavTexture;
					}
					else if (Id == "Event_Tab") {
						texture = BossLogUI.eventNavTexture;
					}
					else if (Id == "Credits_Tab") {
						texture = BossLogUI.credTexture;
					}
					else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum == -1) {
						texture = BossLogUI.filterTexture;
					}
					else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum != -1) {
						texture = BossLogUI.tocTexture;
					}

					if (DrawTab(Id)) {
						spriteBatch.Draw(texture, pos, Color.White);
					}
					else {
						return;
					}

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
						else if (Id == "Credits_Tab") {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpCred");
						}
						else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum == -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ToggleFilters");
						}
						else if (Id == "ToCFilter_Tab" && BossLogUI.PageNum != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpTOC");
						}
						if (tabMessage != "") {
							Main.hoverItemName = tabMessage;
						}
					}
				}

				if (Id.Contains("C_") && IsMouseHovering) {
					if (Id == "C_0") {
						Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterBosses.ToLower().Replace(" ", "")}");
					}
					if (Id == "C_1") {
						Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterMiniBosses.ToLower().Replace(" ", "")}");
					}
					if (Id == "C_2") {
						Main.hoverItemName = Language.GetTextValue($"Mods.BossChecklist.BossLog.Terms.{BossChecklist.BossLogConfig.FilterEvents.ToLower().Replace(" ", "")}");
					}
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
				List<BossInfo> sortedBosses = BossChecklist.bossTracker.SortedBosses;
				// name check, for when progression matches
				// index should never be -1 since variables passed in are within bounds
				int index = sortedBosses.FindIndex(x => x.progression == order && (x.name == bossName || x.internalName == bossName));

				bool allLoot = false;
				bool allCollect = false;
				bool condCollect = false;
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

				foreach (int loot in sortedBosses[index].loot) {
					if (loot == 0 || loot == -1) {
						continue;
					}
					if (sortedBosses[index].npcIDs[0] < NPCID.Count) {
						if (WorldGen.crimson && (loot == ItemID.DemoniteOre || loot == ItemID.CorruptSeeds || loot == ItemID.UnholyArrow)) {
							continue;
						}
						else if (!WorldGen.crimson && (loot == ItemID.CrimtaneOre || loot == ItemID.CrimsonSeeds)) {
							continue;
						}
					}
					int indexLoot = modPlayer.BossTrophies[index].loot.FindIndex(x => x.Type == loot);
					if (indexLoot != -1) {
						allLoot = true;
					}
					else { // Item not obtained
						Item newItem = new Item();
						newItem.SetDefaults(loot);
						if (newItem.expert && !Main.expertMode) {
							continue;
						}
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
						if (collectible == -1 || collectible == 0) {
							continue;
						}
						int indexCollect = modPlayer.BossTrophies[index].collectibles.FindIndex(x => x.Type == collectible);
						if (indexCollect != -1) {
							allCollect = true;
						}
						else {
							allCollect = false;
							break;
						}
					}
				}

				Vector2 pos2 = new Vector2(innerDimensions.X + Main.fontMouseText.MeasureString(displayName).X + 6, innerDimensions.Y - 2);

				if (allLoot && (allCollect || condCollect)) {
					spriteBatch.Draw(BossLogUI.goldChestTexture, pos2, Color.White);
				}
				else if (allLoot) {
					spriteBatch.Draw(BossLogUI.chestTexture, pos2, Color.White);
				}
				else if (allCollect) {
					spriteBatch.Draw(BossLogUI.starTexture, pos2, Color.White);
				}
				// TODO: Hover explanation or description.txt explanation.

				if (order != -1f) {
					BossChecklist BA = BossChecklist.instance;
					int pagenum = Convert.ToInt32(Id);
					BossInfo selectedBoss = sortedBosses[pagenum];
					Texture2D checkGrid = BossLogUI.checkboxTexture;

					if (selectedBoss.downed()) {
						if (BossChecklist.BossLogConfig.SelectedCheckmarkType == "X and  ☐") {
							checkGrid = BossLogUI.xTexture;
						}
						else if (BossChecklist.BossLogConfig.SelectedCheckmarkType != "Strike-through") {
							checkGrid = BossLogUI.checkMarkTexture;
						}
						else {
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
					}
					else {
						checkGrid = BossChecklist.BossLogConfig.SelectedCheckmarkType == "✓ and  X" ? BossLogUI.xTexture : BossLogUI.checkboxTexture;
						if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) {
							checkGrid = BossLogUI.circleTexture;
						}
					}

					if (BossChecklist.BossLogConfig.SelectedCheckmarkType != "Strike-through") {
						if (!selectedBoss.hidden) {
							spriteBatch.Draw(BossLogUI.checkboxTexture, pos, Color.White);
							spriteBatch.Draw(checkGrid, pos, Color.White);
						}
					}

					// Use the appropriate text color for conditions
					if (BossChecklist.BossLogConfig.ColoredBossText) {
						if (IsMouseHovering) {
							TextColor = TextColor = Color.SkyBlue;
						}
						//if (IsMouseHovering && sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.DarkSeaGreen;
						//else if (IsMouseHovering && !sortedBosses[Convert.ToInt32(Id)].downed()) TextColor = Color.IndianRed;
						else if (!selectedBoss.downed() && nextCheck && BossChecklist.BossLogConfig.DrawNextMark) {
							TextColor = new Color(248, 235, 91);
						}
						else if (selectedBoss.downed()) {
							TextColor = Colors.RarityGreen;
						}
						else if (!selectedBoss.downed()) {
							TextColor = Colors.RarityRed;
						}
						if (modPlayer.hasNewRecord[pagenum]) {
							TextColor = Main.DiscoColor;
						}
					}
					else {
						TextColor = IsMouseHovering ? Color.DarkGray : Color.Silver;
					}
					// Hidden boss text color overwrites previous text color alterations
					if ((!selectedBoss.available() && !selectedBoss.downed()) || selectedBoss.hidden) {
						TextColor = Color.DimGray;
					}
					if (IsMouseHovering) {
						BossLogPanel.headNum = Convert.ToInt32(Id);
					}
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
				if (BossLogUI.PageNum < 0) {
					return;
				}

				if (buttonString == "Mods.BossChecklist.BossLog.DrawnText.Records" || buttonString == "LegacyInterface.101") {
					EntryType BossType = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum].type;
					buttonString = BossType == EntryType.Event ? "LegacyInterface.101" : "Mods.BossChecklist.BossLog.DrawnText.Records";
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
							if (IsMouseHovering) {
								Main.hoverItemName = "Click to see the best records for this world\n(Multiplayer Under Construction!)";
							}
						}
						else {
							Rectangle exclamCut = new Rectangle(34 * 2, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) {
								Main.hoverItemName = "Click to see your personal records";
							}
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
							if (IsMouseHovering) {
								Main.hoverItemName = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewCollect");
							}
						}
						else {
							Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
							spriteBatch.Draw(text, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) {
								Main.hoverItemName = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewLoot");
							}
						}
					}
					else if (BossLogUI.SubPageNum == 3) {
						// Unused currently
					}
				}
			}
		}
	}
}
