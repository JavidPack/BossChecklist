using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using ReLogic.OS;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
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
	internal static class BossLogUIElements
	{
		internal class BossAssistButton : UIImageButton
		{
			public string Id { get; init; } = "";
			internal string buttonType;
			internal Asset<Texture2D> texture;
			internal int cycleFrame = 0;
			internal bool slowDown = true;
			private Vector2 offset;
			internal bool dragging;

			public BossAssistButton(Asset<Texture2D> texture, string type) : base(texture) {
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
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(translated);
				Vector2 pos = new Vector2(innerDimensions.X - (stringAdjust.X / 3), innerDimensions.Y - 24);

				base.DrawSelf(spriteBatch);

				// Draw the Boss Log Color
				if (Id == "OpenUI") {
					Asset<Texture2D> cover = BossLogUI.colorTexture;
					Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
					if (!IsMouseHovering && !dragging) {
						cover = BossLogUI.fadedTexture;
						coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
					}
					spriteBatch.Draw(cover.Value, innerDimensions.ToRectangle(), coverColor);

					// Border Selection
					PlayerAssist myPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					if (!myPlayer.hasOpenedTheBossLog) {
						spriteBatch.Draw(BossLogUI.borderTexture.Value, innerDimensions.ToRectangle(), Main.DiscoColor);
					}
					else if (BossChecklist.DebugConfig.NewRecordsDisabled || BossChecklist.DebugConfig.RecordTrackingDisabled) {
						spriteBatch.Draw(BossLogUI.borderTexture.Value, innerDimensions.ToRectangle(), Color.IndianRed);
					}

					if (myPlayer.hasNewRecord.Any(x => x == true)) {
						slowDown = !slowDown;
						if (slowDown) {
							cycleFrame++;
						}
						if (cycleFrame >= 19) {
							cycleFrame = 0;
						}

						Asset<Texture2D> bookBorder = BossChecklist.instance.Assets.Request<Texture2D>("Resources/LogUI_ButtonBorder");
						Rectangle source = new Rectangle(0, 40 * cycleFrame, 34, 38);
						spriteBatch.Draw(bookBorder.Value, innerDimensions.ToRectangle(), source, BossChecklist.BossLogConfig.BossLogColor);
					}
					else if (IsMouseHovering) {
						spriteBatch.Draw(BossLogUI.borderTexture.Value, innerDimensions.ToRectangle(), Color.Goldenrod);
					}

					// Drawing the entire book while dragging if the mouse happens to go off screen/out of window
					if (dragging) {
						spriteBatch.Draw(texture.Value, innerDimensions.ToRectangle(), Color.White);
					}
				}

				if (IsMouseHovering && !dragging) {
					BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
					if (!Id.StartsWith("CycleItem")) {
						spriteBatch.DrawString(FontAssets.MouseText.Value, translated, pos, Color.White);
					}
					else {
						BossUISystem.Instance.UIHoverText = buttonType;
					}
				}
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					Main.player[Main.myPlayer].mouseInterface = true;
				}
			}
		}

		internal class LogItemSlot : UIElement
		{
			public string Id { get; init; } = "";
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

				Width.Set(TextureAssets.InventoryBack9.Width() * scale, 0f);
				Height.Set(TextureAssets.InventoryBack9.Height() * scale, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				float oldScale = Main.inventoryScale;
				Main.inventoryScale = scale;
				Rectangle rectangle = GetInnerDimensions().ToRectangle();
				// Make backups of the original itemslot textures, as we will replace them temporarily for our visuals
				var backup = TextureAssets.InventoryBack6;
				var backup2 = TextureAssets.InventoryBack7;

				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
				BossCollection collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];
				bool masked = BossChecklist.BossLogConfig.BossSilhouettes;

				if (Id.StartsWith("loot_") || Id.StartsWith("collect_")) {
					if (masked && !selectedBoss.IsDownedOrForced) {
						// Boss Silhouettes always makes itemslot background red, reguardless of obtainable
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
					}
					else if (hasItem) {
						// Otherwise, if the item is obtained make the itemslot background green
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack3;
						// If on the Collectibles page, the display type should be marked
						bool OnCollectionPage = BossLogUI.CategoryPageNum == CategoryPage.Loot && BossLogUI.AltPageSelected[(int)CategoryPage.Loot] == 1;
						if (OnCollectionPage && item.type == BossLogUI.CollectibleDisplayType) {
							TextureAssets.InventoryBack7 = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_HighlightedCollectible", AssetRequestMode.ImmediateLoad);
						}
					}
					else if ((item.expert && !Main.expertMode) || (item.master && !Main.masterMode)) {
						// If not obtained and the item is mode restricted, itemslot background is red
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
					}
					// Otherwise, any unobtained items use the original trash-itemslot background color
				}

				string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
				string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");

				if (masked && !selectedBoss.IsDownedOrForced) {
					item.color = Color.Black;
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
					string hoverText = $"Defeat {selectedBoss.name} to view obtainable {(BossLogUI.AltPageSelected[(int)CategoryPage.Loot] == 1 ? "collectibles" : "loot")}";
					Rectangle rect2 = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 32, 32);
					if (item.expert && !Main.expertMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyExpert").Value, rect2, Color.White);
						hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly");
					}
					if (item.master && !Main.masterMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyMaster").Value, rect2, Color.White);
						hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly");
					}
					if (IsMouseHovering) {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
					return;
				}
				else if (item.type != ItemID.None || hoverText == demonAltar || hoverText == crimsonAltar || Id.StartsWith("ingredient_")) {
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
				}

				// Set the itemslot textures back to their original state
				TextureAssets.InventoryBack6 = backup;
				TextureAssets.InventoryBack7 = backup2;

				if (hoverText == crimsonAltar || hoverText == demonAltar) {
					Main.instance.LoadTiles(TileID.DemonAltar);
					int offsetX = 0;
					int offsetY = 0;
					int offsetSrc = hoverText == crimsonAltar ? 3 : 0;
					for (int i = 0; i < 6; i++) {
						float scale = 0.64f;
						Rectangle src = new Rectangle((offsetX + offsetSrc) * 18, offsetY * 18, 16, 16 + (offsetY * 2));
						// Determine the position of EACH tile of the selected altar (multi-tile, 3x2)
						float posX = rectangle.X + (rectangle.Width / 2) - (src.Width * scale / 2) + (src.Width * scale * (offsetX - 1));
						float posY = rectangle.Y + (rectangle.Height / 2) - (src.Height * scale / 2) + (src.Height * scale / 2 * (offsetY == 0 ? -1 : 1));
						Vector2 pos = new Vector2(posX, posY);
						spriteBatch.Draw(TextureAssets.Tile[TileID.DemonAltar].Value, pos, src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

						offsetX++;
						if (offsetX == 3) {
							offsetX = 0;
							offsetY++;
						}
					}
				}

				Rectangle rect = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 22, 20);
				if (item.type != ItemID.None && (Id.StartsWith("loot_") || Id.StartsWith("collect_"))) {
					if (hasItem) {
						// Obtainability check take priority over any expert/master mode restriction
						if (!masked || (masked && selectedBoss.IsDownedOrForced)) {
							spriteBatch.Draw(BossLogUI.checkMarkTexture.Value, rect, Color.White);
						}
					}
					else {
						Rectangle rect2 = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 32, 32);
						if (item.expert && !Main.expertMode) {
							spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyExpert").Value, rect2, Color.White);
						}
						if (item.master && !Main.masterMode) {
							spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyMaster").Value, rect2, Color.White);
						}
					}
				}

				if (Id.StartsWith("collect_") && BossChecklist.DebugConfig.ShowCollectionType) {
					BossInfo boss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
					int index = boss.collection.FindIndex(x => x == item.type);
					CollectionType type = boss.collectType[index];

					if (type != CollectionType.Generic) {
						string showType = "";
						Texture2D showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Trophy").Value;
						if (type == CollectionType.Trophy) {
							showType = "Trophy";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Trophy").Value;
						}
						else if (type == CollectionType.MusicBox) {
							showType = "Music";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Music").Value;
						}
						else if (type == CollectionType.Mask) {
							showType = "Mask";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Mask").Value;
						}
						else if (type == CollectionType.Pet) {
							showType = "Pet";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Pet").Value;
						}
						else if (type == CollectionType.Mount) {
							showType = "Mount";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Pet").Value;
						}
						else if (type == CollectionType.Relic) {
							showType = "Relic";
							showIcon = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Trophy").Value;
						}

						Rectangle rect2 = new Rectangle((int)rectangle.BottomLeft().X - 4, (int)rectangle.BottomLeft().Y - 15, 22, 20);
						spriteBatch.Draw(showIcon, rect2, Color.White);
						if (IsMouseHovering) {
							Utils.DrawBorderString(spriteBatch, showType, rectangle.TopLeft(), Colors.RarityAmber, 0.8f);
						}
					}
				}

				if (IsMouseHovering) {
					if (hoverText != Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand")) {
						if (item.type != ItemID.None && (Id.StartsWith("loot_") || Id.StartsWith("collect_")) && !hasItem) {
							if (!Main.expertMode && (item.expert || item.expertOnly)) {
								BossUISystem.Instance.UIHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly");
							}
							else if (!Main.masterMode && (item.master || item.masterOnly)) {
								BossUISystem.Instance.UIHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly");
							}
							else {
								BossUISystem.Instance.UIHoverText = item.HoverName;
							}
						}
						else if (hoverText == crimsonAltar || hoverText == demonAltar) {
							BossUISystem.Instance.UIHoverText = hoverText;
						}
						else if (item.type != ItemID.None || hoverText != "") {
							Color newcolor = ItemRarity.GetColor(item.rare);
							float num3 = (float)(int)Main.mouseTextColor / 255f;
							if (item.expert || item.expertOnly) {
								newcolor = Main.DiscoColor;
							}
							Main.HoverItem = item;
							Main.hoverItemName = $"[c/{newcolor.Hex3()}: {hoverText}]";
						}
						else {
							BossUISystem.Instance.UIHoverText = hoverText;
						}
					}
					else {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
				}
				Main.inventoryScale = oldScale;
			}
		}

		internal class LootRow : UIElement
		{
			public string Id { get; init; } = "";
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
			public string Id { get; init; } = "";
			public static int headNum = -1;

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);

				BossInfo selectedBoss;
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
					Main.LocalPlayer.cursorItemIconEnabled = false;
					Main.LocalPlayer.cursorItemIconID = -1;
					Main.ItemIconCacheUpdate(0);
				}

				if (BossLogUI.PageNum == -1) { // Table of Contents
					List<BossInfo> bossList = BossChecklist.bossTracker.SortedBosses;
					if (Id == "PageOne") {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 30, GetInnerDimensions().Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.PreHardmode"), pos, Colors.RarityAmber, 0.6f);
					}
					else if (Id == "PageTwo") {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 35, GetInnerDimensions().Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Hardmode"), pos, Colors.RarityAmber, 0.6f);
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
							foreach (int id in headBoss.npcIDs) {
								Asset<Texture2D> head = BossLogUI.GetBossHead(id);
								if (headBoss.overrideIconTexture != "") {
									head = ModContent.Request<Texture2D>(headBoss.overrideIconTexture);
								}
								if (head != TextureAssets.NpcHead[0]) {
									headsDisplayed++;
									spriteBatch.Draw(head.Value, new Rectangle(Main.mouseX + 15 + ((head.Width() + 2) * adjustment), Main.mouseY + 15, head.Width(), head.Height()), maskedHead);
									adjustment++;
								}
							}
							Asset<Texture2D> noHead = TextureAssets.NpcHead[0];
							if (headsDisplayed == 0) {
								spriteBatch.Draw(noHead.Value, new Rectangle(Main.mouseX + 15 + ((noHead.Width() + 2) * adjustment), Main.mouseY + 15, noHead.Width(), noHead.Height()), maskedHead);
							}
						}
						else {
							Color maskedHead = BossLogUI.MaskBoss(headBoss);
							Asset<Texture2D> eventIcon = BossLogUI.GetEventIcon(headBoss);
							Rectangle iconpos = new Rectangle(Main.mouseX + 15, Main.mouseY + 15, eventIcon.Width(), eventIcon.Height());
							if (eventIcon != TextureAssets.NpcHead[0]) {
								spriteBatch.Draw(eventIcon.Value, iconpos, maskedHead);
							}
						}
					}
				}
				else if (BossLogUI.PageNum == -2) { // Mod Developers Credits
					if (Id == "PageOne") {
						// Credits Page
						Vector2 stringPos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
						Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksDevs"), stringPos, Color.IndianRed);

						Asset<Texture2D> users = BossChecklist.instance.Assets.Request<Texture2D>("Resources/Extra_CreditUsers");
						string[] usernames = { "Jopojelly", "SheepishShepherd", "direwolf420", "RiverOaken", "Orian", "Panini" };
						string[] titles = { "Mod Owner", "Mod Co-Owner", "Code Contributor", "Spriter", "Beta Tester", "Beta Tester" };
						Color[] colors = { Color.CornflowerBlue, Color.Goldenrod, Color.Tomato, Color.MediumPurple, new Color(49, 210, 162), Color.HotPink };
						const float nameScaling = 0.85f;
						const float titleScaling = 0.75f;

						int row = 0;
						for (int i = 0; i < usernames.Length; i++) {
							bool left = i % 2 == 0;
							bool panini = usernames[i] == "Panini";

							Vector2 userpos = new Vector2(pageRect.X + (left ? 75 : 225) - (panini ? 10 : 0), pageRect.Y + 75 + (125 * row));
							Rectangle userselected = new Rectangle(0 + (60 * i), 0, 60 + (panini ? 10 : 0), 58);
							spriteBatch.Draw(users.Value, userpos, userselected, Color.White);
							
							Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(usernames[i]);
							stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * nameScaling) / 2) + (panini ? 5 : 0), userpos.Y - 25);
							Utils.DrawBorderString(spriteBatch, usernames[i], stringPos, colors[i], nameScaling);

							stringAdjust = FontAssets.MouseText.Value.MeasureString(titles[i]);
							stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * titleScaling) / 2) + (panini ? 5 : 0), userpos.Y + userselected.Height + 10);
							Utils.DrawBorderString(spriteBatch, titles[i], stringPos, colors[i], titleScaling);

							if (!left) {
								row++;
							}
						}
					}

					if (Id == "PageTwo") { // Supported Mod Credits Page
						if (BossUISystem.Instance.OptedModNames.Count > 0) {
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
						Asset<Texture2D> bossTexture = null;
						Rectangle bossSourceRectangle = new Rectangle();
						if (selectedBoss.pageTexture != "BossChecklist/Resources/BossTextures/BossPlaceholder_byCorrina") {
							bossTexture = ModContent.Request<Texture2D>(selectedBoss.pageTexture);
							bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width(), bossTexture.Height());
						}
						else if (selectedBoss.npcIDs.Count > 0) {
							Main.instance.LoadNPC(selectedBoss.npcIDs[0]);
							bossTexture = TextureAssets.Npc[selectedBoss.npcIDs[0]];
							bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width(), bossTexture.Height() / Main.npcFrameCount[selectedBoss.npcIDs[0]]);
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
							spriteBatch.Draw(bossTexture.Value, pageRect.Center(), bossSourceRectangle, maskedBoss, 0, bossSourceRectangle.Center(), drawScale, SpriteEffects.None, 0f);
						}

						Rectangle firstHeadPos = new Rectangle();

						if (selectedBoss.type != EntryType.Event || selectedBoss.internalName == "Lunar Event") {
							int headsDisplayed = 0;
							int adjustment = 0;
							Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
							for (int h = selectedBoss.npcIDs.Count - 1; h > -1; h--) {
								Texture2D head = BossLogUI.GetBossHead(selectedBoss.npcIDs[h]).Value;
								if (head != TextureAssets.NpcHead[0].Value) {
									Rectangle src = new Rectangle(0, 0, head.Width, head.Height);
									// Weird special case for Deerclops. Its head icon has a significant amount of whitespace.
									if (selectedBoss.Key == "Terraria Deerclops") {
										src = new Rectangle(2, 0, 48, 40);
									}
									int xHeadOffset = pageRect.X + pageRect.Width - src.Width - 10 - ((src.Width + 2) * adjustment);
									Rectangle headPos = new Rectangle(xHeadOffset, pageRect.Y + 5, src.Width, src.Height);
									if (headsDisplayed == 0) {
										firstHeadPos = headPos;
									}
									spriteBatch.Draw(head, headPos, src, maskedHead);
									headsDisplayed++;
									adjustment++;
								}
							}
							if (headsDisplayed == 0) {
								Texture2D noHead = TextureAssets.NpcHead[0].Value;
								int xHeadOffset = pageRect.X + pageRect.Width - noHead.Width - 10;
								Rectangle noHeadPos = new Rectangle(xHeadOffset, pageRect.Y + 5, noHead.Width, noHead.Height);
								firstHeadPos = noHeadPos;
								spriteBatch.Draw(noHead, noHeadPos, maskedHead);
							}
						}
						else {
							Color maskedHead = BossLogUI.MaskBoss(selectedBoss);
							Asset<Texture2D> eventIcon = BossLogUI.GetEventIcon(selectedBoss);
							Rectangle iconpos = new Rectangle(pageRect.X + pageRect.Width - eventIcon.Width() - 10, pageRect.Y + 5, eventIcon.Width(), eventIcon.Height());
							firstHeadPos = iconpos;
							spriteBatch.Draw(eventIcon.Value, iconpos, maskedHead);
						}

						string isDefeated = $"{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}";
						string notDefeated = $"{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Undefeated", Main.worldName)}";

						if (selectedBoss.ForceDownedByPlayer(Main.LocalPlayer)) {
							isDefeated = $"''{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}''";
						}

						Asset<Texture2D> texture = selectedBoss.IsDownedOrForced ? BossLogUI.checkMarkTexture : BossLogUI.xTexture;
						Vector2 defeatpos = new Vector2(firstHeadPos.X + (firstHeadPos.Width / 2), firstHeadPos.Y + firstHeadPos.Height - (texture.Height() / 2));
						spriteBatch.Draw(texture.Value, defeatpos, Color.White);

						// Hovering over the head icon will display the defeated text
						if (BossLogUI.MouseIntersects(firstHeadPos.X, firstHeadPos.Y, firstHeadPos.Width, firstHeadPos.Height)) {
							BossUISystem.Instance.UIHoverText = selectedBoss.IsDownedOrForced ? isDefeated : notDefeated;
							BossUISystem.Instance.UIHoverTextColor = selectedBoss.IsDownedOrForced ? Colors.RarityGreen : Colors.RarityRed;
						}

						bool enabledCopyButtons = BossChecklist.DebugConfig.AccessInternalNames && selectedBoss.modSource != "Unknown";
						Vector2 pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 5);
						Utils.DrawBorderString(spriteBatch, selectedBoss.name, pos, Color.Goldenrod);

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Vector2 vec2 = new Vector2(pageRect.X + 5, pos.Y);
							spriteBatch.Draw(clipboard, vec2, Color.Goldenrod);
						}

						pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 30);
						Utils.DrawBorderString(spriteBatch, selectedBoss.SourceDisplayName, pos, new Color(150, 150, 255));

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Vector2 vec2 = new Vector2(pageRect.X + 5, pageRect.Y + 5);

							Color copied = (Platform.Get<IClipboard>().Value == selectedBoss.Key) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, vec2, copied);

							// Hovering and rightclick will copy to clipboard
							if (BossLogUI.MouseIntersects(vec2.X, vec2.Y, clipboard.Width, clipboard.Height)) {
								BossUISystem.Instance.UIHoverText = $"Click to copy internal 'boss key' to clipboard:\n{selectedBoss.Key}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.Key;
								}
							}

							vec2 = new Vector2(pageRect.X + 5, pageRect.Y + 30);
							copied = (Platform.Get<IClipboard>().Value == selectedBoss.modSource) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, vec2, copied);

							if (BossLogUI.MouseIntersects(vec2.X, vec2.Y, clipboard.Width, clipboard.Height)) {
								BossUISystem.Instance.UIHoverText = $"Click to copy internal 'mod source' to clipboard:\n{selectedBoss.modSource}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.modSource;
								}
							}
						}
					}
					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == 0 && selectedBoss.modSource != "Unknown") {
						if (selectedBoss.type != EntryType.Event) {
							// Boss Records Subpage
							Asset<Texture2D> construction = ModContent.Request<Texture2D>("Terraria/Images/UI/Creative/Journey_Toggle", AssetRequestMode.ImmediateLoad);
							Rectangle innerRect = GetInnerDimensions().ToRectangle();
							Rectangle conRect = new Rectangle(innerRect.X + innerRect.Width - 32 - 30, innerRect.Y + 60, 32, 34);
							spriteBatch.Draw(construction.Value, conRect, Color.White);

							if (Main.mouseX >= conRect.X && Main.mouseX < conRect.X + conRect.Width) {
								if (Main.mouseY >= conRect.Y && Main.mouseY < conRect.Y + conRect.Height) {
									BossUISystem.Instance.UIHoverText = "Boss records is still under construction and may not work.\nThis includes any configs related to boss records.";
									BossUISystem.Instance.UIHoverTextColor = Color.Gold;
								}
							}

							foreach (BossInfo info in BossChecklist.bossTracker.SortedBosses) {
								if (info.type != EntryType.Event) {
									continue;
								}
								if (info.npcIDs.Contains(selectedBoss.npcIDs[0])) {
									Texture2D icon = BossLogUI.GetEventIcon(info).Value;
									Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + 15, GetInnerDimensions().ToRectangle().Y + 50);
									bool masked = BossChecklist.BossLogConfig.BossSilhouettes;
									Color faded = info.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
									spriteBatch.Draw(icon, pos, faded);
									if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + icon.Width) {
										if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + icon.Height) {
											BossUISystem.Instance.UIHoverText = info.name + "\nClick to view page";
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												BossLogUI.PageNum = BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == info.Key);
											}
										}
									}
								}
							}

							// Beginning of record drawing
							Asset<Texture2D> achievements = ModContent.Request<Texture2D>("Terraria/Images/UI/Achievements");
							PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
							BossStats record = modPlayer.RecordsForWorld[BossLogUI.PageNum].stat;
							WorldStats wldRecord = WorldAssist.worldRecords[BossLogUI.PageNum].stat;

							bool[] isNewRecord = new bool[4];
							string recordTitle = "";
							string recordValue = "";
							string compareNumbers = "";
							int[] achCoord = new int[] { 0, 0 };

							for (int recordSlot = 0; recordSlot < 4; recordSlot++) { // 4 spots total
								if (recordSlot == 0) {
									recordValue = $"{Main.LocalPlayer.name}";
									// Which sub-category are we in?
									achCoord = new int[] { -1, -1 }; // No achievement drawing
									if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
										recordTitle = "Previous Attempt";
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 1) {
										recordTitle = "First Victory";
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 2) {
										recordTitle = "Personal Best";
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 3) {
										recordTitle = $"World Records";
										recordValue = $"{Main.worldName}";
									}
								}
								if (recordSlot == 1) {
									if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] != 3) {
										// Kills & Deaths
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.KDR");
										achCoord = new int[] { 0, 3 };
										if (record.kills == 0 && record.deaths == 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										}
										else {
											string killTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Kills");
											string deathTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths");
											recordValue = $"{record.kills} {killTerm} / {record.deaths} {deathTerm}";
										}
									}
									else {
										// World Kills & Deaths
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.KDRWorld");
										achCoord = wldRecord.totalKills >= wldRecord.totalDeaths ? new int[] { 4, 10 } : new int[] { 4, 8 };
										if (wldRecord.totalKills == 0 && wldRecord.totalDeaths == 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										}
										else {
											string killTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Kills");
											string deathTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths");
											recordValue = $"{wldRecord.totalKills} {killTerm} / {wldRecord.totalDeaths} {deathTerm}";
										}
									}
								}
								else if (recordSlot == 2) {
									// Fight Duration
									recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Duration");
									achCoord = new int[] { 4, 9 };

									if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
										// Last Attempt
										if (record.durationPrev == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationPrev);
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 1) {
										// First Victory
										if (record.durationFirs == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationFirs);
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 2) {
										// Personal Best
										if (record.durationBest == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationBest);

											if (BossLogUI.CompareState != -1) {
												// If comparing is on
												int compare = true ? modPlayer.duration_CompareValue : record.durationBest;
												string sign = compare - record.durationBest > 0 ? "+" : "";
												string hex = sign == "" ? Colors.RarityGreen.Hex3() : Color.IndianRed.Hex3();
												compareNumbers = $"[{sign}{RecordTimeConversion(compare - record.durationBest)}]";
											}
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 3) {
										// World Record
										achCoord = new int[] { 2, 12 };
										if (wldRecord.durationWorld < 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(wldRecord.durationWorld);

											if (BossLogUI.CompareState != -1 && wldRecord.durationWorld >= 0) {
												// If comparing is on
												int compare = true ? modPlayer.duration_CompareValue : record.durationBest;
												string sign = compare - wldRecord.durationWorld > 0 ? "+" : "";
												string hex = sign == "" ? Colors.RarityGreen.Hex3() : Color.IndianRed.Hex3();
												compareNumbers = $"[{sign}{RecordTimeConversion(compare - wldRecord.durationWorld)}]";
											}
										}
									}
								}
								else if (recordSlot == 3) { // Hits Taken
									recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Dodge");
									achCoord = new int[] { 3, 0 };

									if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
										// Last Attempt
										if (record.hitsTakenPrev == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenPrev.ToString();
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 1) {
										// First Victory
										if (record.hitsTakenFirs == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenFirs.ToString();
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 2) {
										// Personal Best
										if (record.hitsTakenBest == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenBest.ToString();

											if (BossLogUI.CompareState != -1) {
												// If comparing is on
												int compare = true ? modPlayer.hitsTaken_CompareValue : record.hitsTakenBest;
												string sign = compare - record.hitsTakenBest > 0 ? "+" : "";
												string hex = sign == "" ? Colors.RarityGreen.Hex3() : Color.IndianRed.Hex3();
												compareNumbers = $"[{sign}{compare - record.hitsTakenBest}]";
											}
										}
									}
									else if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 3) {
										// World Record
										achCoord = new int[] { 0, 7 };
										recordValue = wldRecord.hitsTakenWorld.ToString();

										if (BossLogUI.CompareState != -1 && wldRecord.hitsTakenWorld >= 0) {
											// If comparing is on
											int compare = true ? modPlayer.hitsTaken_CompareValue : record.hitsTakenBest;
											string sign = compare - wldRecord.hitsTakenWorld > 0 ? "+" : "";
											string hex = sign == "" ? Colors.RarityGreen.Hex3() : Color.IndianRed.Hex3();
											compareNumbers = $"[{sign}{compare - wldRecord.hitsTakenWorld}]";
										}
									}
								}

								if (achCoord[0] != -1) {
									Rectangle posRect = new Rectangle(pageRect.X + 15, pageRect.Y + 125 + (75 * recordSlot), 64, 64);
									Rectangle cutRect = new Rectangle(66 * achCoord[0], 66 * achCoord[1], 64, 64);

									Asset<Texture2D> slot = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordSlot", AssetRequestMode.ImmediateLoad);
									spriteBatch.Draw(slot.Value, new Vector2(posRect.X, posRect.Y), new Color(175, 175, 125));
									spriteBatch.Draw(achievements.Value, posRect, cutRect, Color.White);

									if (BossLogUI.MouseIntersects(posRect.X, posRect.Y, 64, 64)) {
										// TODO: Change these texts to something better. A description of the record type
										if (recordSlot == 1 && BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
											BossUISystem.Instance.UIHoverText = "Total times you killed the boss and total times the boss has killed you!";
										}
										if (recordSlot == 2) {
											BossUISystem.Instance.UIHoverText = "The quickest time you became victorious!";
										}
										if (recordSlot == 3) {
											BossUISystem.Instance.UIHoverText = "Avoid as many attacks as you can for a no-hitter!";
										}
									}
								}
								/*
								if (isNewRecord[i] && modPlayer.hasNewRecord[BossLogUI.PageNum]) {
									Texture2D text = ModContent.GetTexture("Terraria/UI/UI_quickicon1");
									Rectangle exclam = new Rectangle(pageRect.X + 59, pageRect.Y + 96 + (75 * i), 9, 24);
									spriteBatch.Draw(text, exclam, Color.White);
								}
								*/

								int offsetY = 135 + (recordSlot * 75);
								CalculatedStyle inner = GetInnerDimensions();

								Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(recordTitle);
								float strScale = 1.2f;
								Vector2 firstTitle = new Vector2(stringAdjust.X * strScale, stringAdjust.Y * strScale);

								float len = recordSlot == 0 ? firstTitle.Length() : stringAdjust.Length();
								Color col = recordSlot == 0 ? Color.Goldenrod : Color.Gold;
								float scl = recordSlot == 0 ? strScale : 1f;

								Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (len / 2) + 2, inner.Y + offsetY);
								Utils.DrawBorderString(spriteBatch, recordTitle, pos, col, scl);

								stringAdjust = FontAssets.MouseText.Value.MeasureString(recordValue);
								pos = new Vector2(inner.X + (inner.Width / 2) - ((int)stringAdjust.Length() / 2) + 2, inner.Y + offsetY + 25);
								Utils.DrawBorderString(spriteBatch, recordValue, pos, Color.White);

								if (compareNumbers != "") {
									Vector2 pos2 = new Vector2(pos.X + stringAdjust.Length(), pos.Y);
									Color color = Color.White;
									if (compareNumbers.StartsWith("[+")) {
										color = Color.LightSalmon;
									}
									else if (compareNumbers.StartsWith("[-")) {
										color = Color.LightGreen;
									}
									Utils.DrawBorderString(spriteBatch, compareNumbers, pos2, color, 0.85f);
									compareNumbers = "";
								}
							}
						}
						else {
							var bosses = BossChecklist.bossTracker.SortedBosses;
							int offset = 0;
							int offsetY = 0;

							int headTextureOffsetX = 0;
							int headTextureOffsetY = 0;
							foreach (int npcID in selectedBoss.npcIDs) {
								foreach (BossInfo info in bosses) {
									if (info.type == EntryType.Event) {
										continue;
									}
									if (info.npcIDs.Contains(npcID)) {
										Texture2D head = info.overrideIconTexture == "" ? BossLogUI.GetBossHead(npcID).Value : ModContent.Request<Texture2D>(info.overrideIconTexture).Value;
										Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + headTextureOffsetX + 15, GetInnerDimensions().ToRectangle().Y + 100);
										bool masked = BossChecklist.BossLogConfig.BossSilhouettes;
										Color headColor = info.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;

										spriteBatch.Draw(head, pos, headColor);
										headTextureOffsetX += head.Width + 5;
										if (BossLogUI.MouseIntersects(pos.X, pos.Y, head.Width, head.Height)) {
											BossUISystem.Instance.UIHoverText = info.name + "\nClick to view page";
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												BossLogUI.PageNum = bosses.FindIndex(x => x.Key == info.Key);
											}
										}
										if (head.Height > headTextureOffsetY) {
											headTextureOffsetY = head.Height;
										}
										break;
									}
								}
							}

							offset = 0;
							if (headTextureOffsetY != 0) {
								offsetY = headTextureOffsetY + 5;
							}

							foreach (int npcID in selectedBoss.npcIDs) {
								if (offset == 0 && offsetY == 4) {
									break; // For now, we stop drawing any banners that exceed the books limit (TODO: might have to reimplement as a UIList for scrolling purposes)
								}

								if (npcID < NPCID.Count) {
									int init = Item.NPCtoBanner(npcID) + 21;
									if (init <= 21) {
										continue;
									}

									Main.instance.LoadNPC(npcID);
									Main.instance.LoadTiles(TileID.Banners);
									Asset<Texture2D> banner = TextureAssets.Tile[TileID.Banners];

									int jump = 0;
									if (init >= 222) {
										jump = 6;
										init -= 222;
									}
									else if (init >= 111) {
										jump = 3;
										init -= 111;
									}

									int bannerID = Item.NPCtoBanner(npcID);
									int bannerItem = Item.BannerToItem(bannerID);
									bool reachedKillCount = NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem];
									bool masked = BossChecklist.BossLogConfig.BossSilhouettes;
									Color bannerColor = reachedKillCount ? Color.White : masked ? Color.Black : BossLogUI.faded;
									if (bannerID <= 0 || NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npcID)]) {
										continue;
									}

									for (int j = 0; j < 3; j++) {
										Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset + 15, GetInnerDimensions().ToRectangle().Y + 100 + (16 * j) + offsetY);
										Rectangle rect = new Rectangle(init * 18, (jump * 18) + (j * 18), 16, 16);
										spriteBatch.Draw(banner.Value, pos, rect, bannerColor);

										if (BossLogUI.MouseIntersects(pos.X, pos.Y, 16, 16)) {
											string npcName = masked ? "???" : Lang.GetNPCNameValue(npcID);
											string killcount = $"\n{NPC.killCount[Item.NPCtoBanner(npcID)]}";
											if (!reachedKillCount) {
												killcount += $" / {ItemID.Sets.KillsToBanner[Item.BannerToItem(bannerID)]}";
											}
											BossUISystem.Instance.UIHoverText = npcName + killcount;
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

									int bannerItemID = NPCLoader.GetNPC(npcID).BannerItem;
									if (bannerItemID <= 0) {
										continue;
									}

									Item newItem = new Item(bannerItemID);
									if (newItem.createTile <= -1) {
										continue;
									}

									Main.instance.LoadTiles(newItem.createTile);
									Asset<Texture2D> banner = TextureAssets.Tile[newItem.createTile];

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

									int bannerID = NPCLoader.GetNPC(npcID).Banner;
									string source = NPCLoader.GetNPC(npcID).Mod.DisplayName;

									bool masked = BossChecklist.BossLogConfig.BossSilhouettes;
									Color bannerColor = NPC.killCount[bannerID] >= 50 ? Color.White : masked ? Color.Black : BossLogUI.faded;
									
									int[] heights = tileData.CoordinateHeights;
									int heightOffSet = 0;
									int heightOffSetTexture = 0;
									for (int j = 0; j < heights.Length; j++) { // could adjust for non 1x3 here and below if we need to.
										Vector2 pos = new Vector2(GetInnerDimensions().ToRectangle().X + offset, GetInnerDimensions().ToRectangle().Y + 100 + heightOffSet + offsetY);
										Rectangle rect = new Rectangle(x, y + heightOffSetTexture, tileData.CoordinateWidth, tileData.CoordinateHeights[j]);
										Main.spriteBatch.Draw(banner.Value, pos, rect, bannerColor);
										heightOffSet += heights[j];
										heightOffSetTexture += heights[j] + tileData.CoordinatePadding;

										if (BossLogUI.MouseIntersects(pos.X, pos.Y, 16, 16)) {
											BossUISystem.Instance.UIHoverText = $"{Lang.GetNPCNameValue(npcID)}: {NPC.killCount[Item.NPCtoBanner(npcID)]}\n[{source}]";
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
					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == CategoryPage.Spawn) {
						// Spawn Item Subpage
					}

					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == CategoryPage.Loot) {
						if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
							// Loot Table Subpage
							Asset<Texture2D> bag = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_TreasureBag");
							Rectangle sourceRect = bag.Value.Bounds;
							foreach (int bagItem in selectedBoss.loot) {
								if (BossChecklist.registeredBossBagTypes.Contains(bagItem)) {
									Main.instance.LoadItem(bagItem);
									bag = TextureAssets.Item[bagItem];
									DrawAnimation drawAnim = Main.itemAnimations[bagItem];
									sourceRect = drawAnim != null ? sourceRect = drawAnim.GetFrame(bag.Value) : bag.Value.Bounds;
									break;
								}
							}
							Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - 5 - (bag.Width() / 2), pageRect.Y + 88, sourceRect.Width, sourceRect.Height);
							spriteBatch.Draw(bag.Value, posRect, sourceRect, Color.White);
						}
						else {
							// Collectibles Subpage

							// If there isn't anything to display, just end all drawing
							if (BossLogUI.CollectibleDisplayType == -1) {
								return;
							}

							Item DisplayedItem = new Item(BossLogUI.CollectibleDisplayType);
							int index = selectedBoss.collection.FindIndex(x => x == DisplayedItem.type);
							if (index == -1) {
								return;
							}
							CollectionType type = selectedBoss.collectType[index];

							// If the displayed item is generic, don't draw anything
							if (type == CollectionType.Generic) {
								return;
							}

							// Draw frame
							Asset<Texture2D> frame = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_CollectibleFrame", AssetRequestMode.ImmediateLoad);
							float frameScale = 1.5f; // Frame scale only
							float scale = 1.5f; // The textures being drawn will be scaled up
							int frameX = pageRect.X + pageRect.Width / 2 - (int)(frame.Value.Width * frameScale / 2);
							Rectangle frameR = new Rectangle(frameX, pageRect.Y + 80, (int)(frame.Value.Width * frameScale), (int)(frame.Value.Height * frameScale));
							spriteBatch.Draw(frame.Value, frameR, Color.White);

							int maskTop = 0;

							// Relics, Trophies, and Musicboxes require tile drawing
							if (type == CollectionType.Relic || type == CollectionType.Trophy || type == CollectionType.MusicBox || type == CollectionType.Mask) {
								Item tileToDisplay = new Item(DisplayedItem.type);
								if (type == CollectionType.Mask) {
									tileToDisplay.SetDefaults(ItemID.Mannequin);
								}
								
								// Tile data for drawing
								// Special case for Relics needed
								int tileType = tileToDisplay.createTile;
								int placeType = type == CollectionType.Relic ? 0 : tileToDisplay.placeStyle;
								int relicStyle = tileToDisplay.placeStyle;

								TileObjectData data = TileObjectData.GetTileData(tileType, placeType);
								int width = data.CoordinateWidth;
								int height = data.CoordinateHeights[0];

								int styleX = 0; // x coordinate of tile style
								int styleY = 0; // Y coordinate of tile style
								if (data.StyleWrapLimit > 0) {
									styleX = (placeType % data.StyleWrapLimit) * data.CoordinateFullWidth;
									styleY = (placeType / data.StyleWrapLimit) * data.CoordinateFullHeight;
								}
								else if (type == CollectionType.Mask) {
									int genderOffset = Main.LocalPlayer.Male ? 0 : 2;
									styleX = (data.DrawStyleOffset + genderOffset) * data.CoordinateFullWidth;
									styleY = 0;
								}
								else {
									styleY = placeType * (height + 2) * data.Height;
								}

								// top-left corner of tile style (texture)
								int topLeftX = styleX;
								int topLeftY = styleY;
								// Offsets for multi-tiles
								int offsetX = 0;
								int offsetY = 0;

								// Load our tile textures
								Main.instance.LoadTiles(tileType);
								Asset<Texture2D> tileTexture = TextureAssets.Tile[tileType];

								// Start drawing the tile texture
								for (int i = 0; i < data.Width * data.Height; i++) {
									if (i != 0 && i % data.Width == 0) {
										styleX = topLeftX;
										styleY += 18;
										offsetX = 0;
										offsetY++;
									}
									int posX = frameR.X + frameR.Width / 2 - (int)(width * data.Width * scale) / 2 + (int)((styleX - topLeftX - (2 * offsetX)) * scale);
									int posY = frameR.Y + frameR.Height / 2 - (int)(height * data.Height * scale) / 2 + (int)((styleY - topLeftY - (2 * offsetY)) * scale);
									Rectangle posRect = new Rectangle(posX, posY, (int)(width * scale), (int)(height * scale));
									Rectangle cutRect = new Rectangle(styleX, styleY, width, height);
									spriteBatch.Draw(tileTexture.Value, posRect, cutRect, Color.White);

									// If the display item is a relic, we'll also need to draw the floating boss part
									if (i == 0) {
										if (type == CollectionType.Relic) {
											if (tileToDisplay.type < ItemID.Count) {
												Asset<Texture2D> relics = ModContent.Request<Texture2D>("Terraria/Images/Extra_198", AssetRequestMode.ImmediateLoad);
												// Since relics take up a 3x3 square area of the tile, the width of the texture can be used in place of the height
												Rectangle posRect2 = new Rectangle(posRect.X, posRect.Y, (int)(relics.Value.Width * scale), (int)(relics.Value.Width * scale));
												Rectangle cutRect2 = new Rectangle(0, relicStyle * relics.Value.Width, relics.Value.Width, relics.Value.Width);
												spriteBatch.Draw(relics.Value, posRect2, cutRect2, Color.White);
											}
											else {
												// ??
											}
										}
										if (type == CollectionType.Mask) {
											maskTop = posY;
										}
									}
									styleX += 18;
									offsetX++;
								}
							}

							if (type == CollectionType.Mask) {
								// Setup for Mannequin drawing
								Asset<Texture2D> headTexture = TextureAssets.ArmorHead[DisplayedItem.headSlot];

								int posX = (int)(frameR.X + frameR.Width / 2 - (headTexture.Value.Width / 2 * scale));
								int posY = maskTop;
								Rectangle pos = new Rectangle(posX, posY, (int)(headTexture.Value.Width * scale), (int)(headTexture.Value.Height / 20 * scale));
								Rectangle src = new Rectangle(0, 0, headTexture.Value.Width, headTexture.Value.Height / 20);
								spriteBatch.Draw(headTexture.Value, pos, src, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
							}
							else if (type == CollectionType.Pet) {
								int projectileType = DisplayedItem.shoot;

								Asset<Texture2D> itemTexture = TextureAssets.Item[DisplayedItem.type];
								Asset<Texture2D> projTexture = TextureAssets.Projectile[projectileType];
								Main.instance.LoadProjectile(projectileType);
								int totalFrames = Main.projFrames[projectileType];

								int posX = frameR.X + frameR.Width / 2 - (int)(projTexture.Value.Width * scale) / 2;
								int posY = frameR.Y + frameR.Height / 2 - (int)(projTexture.Value.Height / totalFrames * scale) / 2;
								Rectangle pos = new Rectangle(posX, posY, (int)(projTexture.Value.Width * scale), (int)(projTexture.Value.Height / totalFrames * scale));
								Rectangle src = new Rectangle(0, 0, projTexture.Value.Width, projTexture.Value.Height / totalFrames);
								spriteBatch.Draw(projTexture.Value, pos, src, Color.White);

								posX = frameR.X + 18;
								posY = frameR.Y + 18;
								pos = new Rectangle(posX, posY, itemTexture.Value.Width, itemTexture.Value.Height);
								src = new Rectangle(0, 0, itemTexture.Value.Width, itemTexture.Value.Height);
								spriteBatch.Draw(itemTexture.Value, pos, src, Color.White);
							}
							else if (type == CollectionType.Mount) {
								int mountType = DisplayedItem.mountType;

								Asset<Texture2D> mountTexture = Mount.mounts[mountType].frontTexture;
								int totalFrames = Mount.mounts[mountType].totalFrames;
								spriteBatch.Draw(mountTexture.Value, new Rectangle(frameR.X, frameR.Y, mountTexture.Value.Width, mountTexture.Value.Height / totalFrames), Color.White);
							}
						}
					}
				}
			}

			public string RecordTimeConversion(int ticks) {
				double seconds = (double)ticks / 60;
				double seconds00 = seconds % 60;
				int minutes = (int)seconds / 60;
				string sign = "";
				if (seconds00 < 0) {
					seconds00 *= -1;
					sign = "-";
				}
				return $"{sign}{minutes}:{seconds00.ToString("00.00")}";
			}
		}

		internal class FixedUIScrollbar : UIScrollbar
		{
			protected override void DrawSelf(SpriteBatch spriteBatch) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.DrawSelf(spriteBatch);
				UserInterface.ActiveInstance = temp;
			}

			public override void MouseDown(UIMouseEvent evt) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.MouseDown(evt);
				UserInterface.ActiveInstance = temp;
			}

			public override void Click(UIMouseEvent evt) {
				UserInterface temp = UserInterface.ActiveInstance;
				UserInterface.ActiveInstance = BossUISystem.Instance.BossLogInterface;
				base.MouseDown(evt);
				UserInterface.ActiveInstance = temp;
			}

			public override void ScrollWheel(UIScrollWheelEvent evt) {
				//Main.NewText(evt.ScrollWheelValue);
				base.ScrollWheel(evt);
				//if (BossLogUI.PageNum < 0 || BossLogUI.SubPageNum != 1) return;
				if (this.Parent != null && this.Parent.IsMouseHovering) {
					//Main.NewText(evt.ScrollWheelValue);
					this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
				}
				else if (this.Parent != null && this.Parent.IsMouseHovering) {
					//Main.NewText(evt.ScrollWheelValue);
					this.ViewPosition -= (float)evt.ScrollWheelValue / 1000;
				}
			}
		}

		internal class BookUI : UIImage
		{
			public string Id { get; init; } = "";
			private Asset<Texture2D> book;

			public BookUI(Asset<Texture2D> texture) : base(texture) {
				book = texture;
			}

			public static bool DrawTab(string Id) {
				bool MatchesCreditsTab = BossLogUI.PageNum == -2 && Id == "Credits_Tab";
				bool MatchesBossTab = BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Boss) && Id == "Boss_Tab";
				bool MatchesMinibossTab = (BossLogUI.PageNum == BossLogUI.FindNext(EntryType.MiniBoss) || BossChecklist.BossLogConfig.OnlyBosses) && Id == "Miniboss_Tab";
				bool MatchesEventTab = (BossLogUI.PageNum == BossLogUI.FindNext(EntryType.Event) || BossChecklist.BossLogConfig.OnlyBosses) && Id == "Event_Tab";
				return !(MatchesCreditsTab || MatchesBossTab || MatchesMinibossTab || MatchesEventTab);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				if (Id == "ToCFilter_Tab") {
					// The hardback part of the UIPanel should be layered under all of the tabs, so it is drawn here
					Asset<Texture2D> pages = BossChecklist.instance.Assets.Request<Texture2D>("Resources/LogUI_Back");
					BossLogPanel panel = BossUISystem.Instance.BossLog.BookArea;
					Vector2 pagePos = new Vector2(panel.Left.Pixels, panel.Top.Pixels);
					spriteBatch.Draw(pages.Value, pagePos, BossChecklist.BossLogConfig.BossLogColor);
				}
				if (!Id.EndsWith("_Tab")) {
					base.DrawSelf(spriteBatch);
				}
				else {
					// Tab drawing
					SpriteEffects effect = SpriteEffects.FlipHorizontally;

					Color color = new Color(153, 199, 255);
					if (Id == "Boss_Tab") {
						color = new Color(255, 168, 168);
						if (BossLogUI.PageNum >= BossLogUI.FindNext(EntryType.Boss) || BossLogUI.PageNum == -2) {
							effect = SpriteEffects.None;
						}
					}
					else if (Id == "Miniboss_Tab") {
						color = new Color(153, 253, 119);
						if (BossLogUI.PageNum >= BossLogUI.FindNext(EntryType.MiniBoss) || BossLogUI.PageNum == -2) {
							effect = SpriteEffects.None;
						}
					}
					else if (Id == "Event_Tab") {
						color = new Color(196, 171, 254);
						if (BossLogUI.PageNum >= BossLogUI.FindNext(EntryType.Event) || BossLogUI.PageNum == -2) {
							effect = SpriteEffects.None;
						}
					}
					else if (Id == "Credits_Tab") {
						color = new Color(218, 175, 133);
					}
					else if (Id == "ToCFilter_Tab") {
						effect = SpriteEffects.None;
					}
					color = Color.Tan;

					if (DrawTab(Id)) {
						spriteBatch.Draw(book.Value, GetDimensions().ToRectangle(), new Rectangle(0, 0, book.Width(), book.Height()), color, 0f, Vector2.Zero, effect, 0f);
					}
				}
				if (Id == "Event_Tab") {
					// Paper Drawing
					// The paper part of the UIPanel should be layered on top of all tabs, so it is drawn here
					Asset<Texture2D> pages = BossChecklist.instance.Assets.Request<Texture2D>("Resources/LogUI_Paper");
					BossLogPanel panel = BossUISystem.Instance.BossLog.BookArea;
					Vector2 pagePos = new Vector2(panel.Left.Pixels, panel.Top.Pixels);
					spriteBatch.Draw(pages.Value, pagePos, Color.White);
				}

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					// Needed to remove mousetext from outside sources when using the Boss Log
					Main.player[Main.myPlayer].mouseInterface = true;
					Main.mouseText = true;

					// Item icons such as hovering over a bed will not appear
					Main.LocalPlayer.cursorItemIconEnabled = false;
					Main.LocalPlayer.cursorItemIconID = -1;
					Main.ItemIconCacheUpdate(0);
				}

				if (Id.EndsWith("_Tab")) {
					// Tab Icon
					Asset<Texture2D> texture = BossLogUI.tocTexture;

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

					Rectangle inner = GetInnerDimensions().ToRectangle();
					int offsetX = inner.X < Main.screenWidth / 2 ? 2 : -2;
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (texture.Value.Width / 2) + offsetX, inner.Y + (inner.Height / 2) - (texture.Value.Height / 2));

					if (DrawTab(Id)) {
						spriteBatch.Draw(texture.Value, pos, Color.White);
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
							BossUISystem.Instance.UIHoverText = tabMessage;
						}
					}
				}

				if (Id.Contains("F_") && IsMouseHovering) {
					string termPrefix = "Mods.BossChecklist.BossLog.Terms.";
					string termLang = $"{termPrefix}hide";
					string termLang2 = $"";
					if (Id == "F_0") {
						termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterBosses.ToLower().Replace(" ", "")}";
						termLang2 = $"{termPrefix}Bosses";
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_1") {
						if (!BossChecklist.BossLogConfig.OnlyBosses) {
							termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterMiniBosses.ToLower().Replace(" ", "")}";
							termLang2 = $"{termPrefix}MiniBosses";
						}
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_2") {
						if (!BossChecklist.BossLogConfig.OnlyBosses) {
							termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterEvents.ToLower().Replace(" ", "")}";
							termLang2 = $"{termPrefix}Events";
						}
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_3") {
						//TODO: lang
						BossUISystem.Instance.UIHoverText = "Toggle hidden visibility";
					}
				}
			}
		}

		internal class TableOfContents : UIText
		{
			public int PageNum { get; init; }
			float order = 0;
			bool nextCheck;
			bool downed;
			string bossName;
			string displayName;

			public TableOfContents(int pageNum, float order, string displayName, string bossName, bool downed, bool nextCheck, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
				PageNum = pageNum;
				this.order = order;
				this.nextCheck = nextCheck;
				this.downed = downed;
				Recalculate();
				this.bossName = bossName;
				this.displayName = displayName;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				CalculatedStyle inner = GetInnerDimensions();
				Vector2 pos = new Vector2(inner.X - 20, inner.Y - 5);
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				List<BossInfo> sortedBosses = BossChecklist.bossTracker.SortedBosses;
				// name check, for when progression matches
				// index should never be -1 since variables passed in are within bounds
				int index = sortedBosses.FindIndex(x => x.progression == order && (x.name == bossName || x.internalName == bossName));

				if (order != -1) {
					BossChecklist BA = BossChecklist.instance;
					BossInfo selectedBoss = sortedBosses[PageNum];
					// Use the appropriate text color for conditions
					if (BossChecklist.BossLogConfig.ColoredBossText) {
						if (IsMouseHovering) {
							TextColor = TextColor = Color.SkyBlue;
						}
						//if (IsMouseHovering && sortedBosses[pageNum].IsDownedOrForced) TextColor = Color.DarkSeaGreen;
						//else if (IsMouseHovering && !sortedBosses[pageNum].IsDownedOrForced) TextColor = Color.IndianRed;
						else if (!downed) {
							if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) {
								TextColor = new Color(248, 235, 91);
							}
							else {
								TextColor = Colors.RarityRed;
							}
						}
						else if (downed) {
							TextColor = Colors.RarityGreen;
						}
						if (modPlayer.hasNewRecord[PageNum]) {
							TextColor = Main.DiscoColor;
						}
					}
					else {
						if (selectedBoss.IsDownedOrForced) {
							TextColor = IsMouseHovering ? Color.Silver : Color.DarkGray;
						}
						else if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) {
							TextColor = new Color(248, 235, 91);
						}
						else {
							TextColor = IsMouseHovering ? Color.Silver : Color.Gainsboro;
						}
					}
					// Hidden boss text color overwrites previous text color alterations
					if ((!selectedBoss.available() && !downed) || selectedBoss.hidden) {
						TextColor = Color.DimGray;
					}
					if (IsMouseHovering) {
						BossLogPanel.headNum = PageNum;
					}
				}

				// base drawing comes after colors so they do not flicker when updating check list
				base.Draw(spriteBatch);

				bool allLoot = true;
				bool allCollect = true;
				Item checkItem = new Item();

				// Loop through player saved loot and boss loot to see if every item was obtained
				foreach (int loot in sortedBosses[index].loot) {
					// Check for corruption/crimson vanilla items, and skip them based on world evil
					// May need new method for looking for these items.
					if (sortedBosses[index].npcIDs[0] < NPCID.Count) {
						if (WorldGen.crimson && (loot == ItemID.DemoniteOre || loot == ItemID.CorruptSeeds || loot == ItemID.UnholyArrow)) {
							continue;
						}
						else if (!WorldGen.crimson && (loot == ItemID.CrimtaneOre || loot == ItemID.CrimsonSeeds)) {
							continue;
						}
					}
					// Find the index of the itemID within the player saved loot
					int indexLoot = modPlayer.BossTrophies[index].loot.FindIndex(x => x.Type == loot);
					// Skip expert/master mode items if the world is not in expert/master mode.
					// TODO: Do something similar for task related items, such as Otherworld music boxes needing to be unlocked.
					if (!Main.expertMode || !Main.masterMode) {
						checkItem.SetDefaults(loot);
						if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly)) {
							continue;
						}
						if (!Main.masterMode && (checkItem.master || checkItem.masterOnly)) {
							continue;
						}
					}
					// If the item index is not found, end the loop and set allLoot to false
					// If this never occurs, the user successfully obtained all the items!
					if (indexLoot == -1) {
						allLoot = false;
						break;
					}
				}

				//Repeast everything for collectibles as well
				foreach (int collectible in sortedBosses[index].collection) {
					if (collectible == -1 || collectible == 0) {
						continue;
					}
					int indexCollect = modPlayer.BossTrophies[index].collectibles.FindIndex(x => x.Type == collectible);
					if (!Main.expertMode || !Main.masterMode) {
						checkItem.SetDefaults(collectible);
						if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly)) {
							continue;
						}
						if (!Main.masterMode && (checkItem.master || checkItem.masterOnly)) {
							continue;
						}
					}
					if (indexCollect == -1) {
						allCollect = false;
						break;
					}
				}

				if (BossChecklist.BossLogConfig.LootCheckVisibility) {
					CalculatedStyle parent = this.Parent.GetInnerDimensions();
					int hardModeOffset = sortedBosses[index].progression > BossTracker.WallOfFlesh ? 10 : 0;

					if (allLoot) {
						Texture2D texture = BossLogUI.chestTexture.Value;
						int offsetX = allCollect ? -6 : 7;
						Vector2 pos2 = new Vector2(parent.X + parent.Width - (texture.Width * 2) + offsetX - hardModeOffset, inner.Y - 2);
						spriteBatch.Draw(texture, pos2, Color.White);
						if (BossLogUI.MouseIntersects(pos2.X, pos2.Y, texture.Width, texture.Height)) {
							BossUISystem.Instance.UIHoverText = $"All Loot Obtained!\n[Localization Needed]";
						}
					}
					if (allCollect) {
						Texture2D texture = BossLogUI.goldChestTexture.Value;
						int offsetX = allLoot ? -1 : -14;
						Vector2 pos2 = new Vector2(parent.X + parent.Width - texture.Width + offsetX - hardModeOffset, inner.Y - 2);
						spriteBatch.Draw(texture, pos2, Color.White);
						if (BossLogUI.MouseIntersects(pos2.X, pos2.Y, texture.Width, texture.Height)) {
							BossUISystem.Instance.UIHoverText = $"All Collectibles Obtained!\n[Localization Needed]";
						}
					}
				}
				// TODO: Hover explanation or description.txt explanation.

				if (order != -1f) {
					BossChecklist BA = BossChecklist.instance;
					BossInfo selectedBoss = sortedBosses[PageNum];
					Asset<Texture2D> checkGrid = BossLogUI.checkboxTexture;
					string checkType = BossChecklist.BossLogConfig.SelectedCheckmarkType;

					if (downed) {
						if (checkType == "X and  ☐") {
							checkGrid = BossLogUI.xTexture;
						}
						else if (checkType != "Strike-through") {
							checkGrid = BossLogUI.checkMarkTexture;
						}
						else {
							Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(displayName);
							Asset<Texture2D> strike = BossChecklist.instance.Assets.Request<Texture2D>("Resources/Checks_Strike");
							int w = strike.Value.Width / 3;
							int h = strike.Value.Height;

							Color hoverColor = IsMouseHovering ? BossLogUI.faded : Color.White;
							int offsetY = (int)(inner.Y + (stringAdjust.Y / 3) - h / 2);

							Rectangle strikePos = new Rectangle((int)(inner.X - w), offsetY, w, h);
							Rectangle strikeSrc = new Rectangle(0, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);

							strikePos = new Rectangle((int)inner.X, offsetY, (int)stringAdjust.X, h);
							strikeSrc = new Rectangle(w, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, IsMouseHovering ? Color.Transparent : Color.White);

							strikePos = new Rectangle((int)(inner.X + (int)stringAdjust.X), offsetY, w, h);
							strikeSrc = new Rectangle(w * 2, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);
						}
					}
					else {
						checkGrid = checkType == "✓ and  X" ? BossLogUI.xTexture : BossLogUI.checkboxTexture;
						if (nextCheck && BossChecklist.BossLogConfig.DrawNextMark) {
							checkGrid = checkType == "Strike-through" ? BossLogUI.strikeNTexture : BossLogUI.circleTexture;
						}
					}

					if (checkType != "Strike-through" || checkGrid == BossLogUI.strikeNTexture) {
						if (!selectedBoss.hidden) {
							if (checkGrid != BossLogUI.strikeNTexture) {
								spriteBatch.Draw(BossLogUI.checkboxTexture.Value, pos, Color.White);
							}
							spriteBatch.Draw(checkGrid.Value, pos, Color.White);
						}
					}
				}
			}

			public override int CompareTo(object obj) {
				TableOfContents other = obj as TableOfContents;
				return order.CompareTo(other.order);
			}
		}

		internal class ProgressBar : UIElement
		{
			internal readonly Asset<Texture2D> fullBar = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_ProgressBar", AssetRequestMode.ImmediateLoad);
			internal int[] downedEntries;
			internal int[] totalEntries;

			public ProgressBar() {
				downedEntries = totalEntries = new int[] { 0, 0, 0 };
			}

			public override void Draw(SpriteBatch spriteBatch) {
				CalculatedStyle inner = GetInnerDimensions();
				int w = fullBar.Value.Width;
				int h = fullBar.Value.Height;
				int barWidth = (int)inner.Width - 12;

				// Beginning of bar
				Rectangle src = new Rectangle(0, 0, w / 3, h);
				Rectangle pos = new Rectangle((int)inner.X, (int)inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// Center of bar
				src = new Rectangle(w / 3, 0, w / 3, h);
				pos = new Rectangle((int)inner.X + 6, (int)inner.Y, barWidth, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// End of bar
				src = new Rectangle(2 * (w / 3), 0, w / 3, h);
				pos = new Rectangle((int)inner.X + (int)inner.Width - 6, (int)inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				BossLogConfiguration configs = BossChecklist.BossLogConfig;
				int allDownedEntries = downedEntries[0];
				int allAccountedEntries = totalEntries[0];
				if (!configs.OnlyBosses) {
					if (configs.FilterMiniBosses != "Hide") {
						allDownedEntries += downedEntries[1];
						allAccountedEntries += totalEntries[1];
					}
					if (configs.FilterMiniBosses != "Hide") {
						allDownedEntries += downedEntries[2];
						allAccountedEntries += totalEntries[2];
					}
				}

				float percentage = (float)allDownedEntries / (float)allAccountedEntries;
				int meterWidth = (int)(barWidth * percentage); 

				Rectangle meterPos = new Rectangle((int)inner.X + 4, (int)inner.Y + 4, meterWidth + 2, (int)inner.Height - 8);
				Color bookColor = BossChecklist.BossLogConfig.BossLogColor;
				bookColor.A = 180;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, Color.White);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, bookColor);

				string percentDisplay = $"{((float)percentage * 100).ToString("#0.0")}%";
				float scale = 0.85f;
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(percentDisplay) * scale;
				Vector2 percentPos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.X / 2), inner.Y - stringAdjust.Y);
				Utils.DrawBorderString(spriteBatch, percentDisplay, percentPos, Colors.RarityAmber, scale);

				if (BossLogUI.MouseIntersects(inner.X, inner.Y, (int)inner.Width, (int)inner.Height)) {
					BossUISystem.Instance.UIHoverText = $"Total: {allDownedEntries}/{allAccountedEntries}";
					if (!configs.OnlyBosses) {
						if (configs.FilterMiniBosses != "Hide" || configs.FilterEvents != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\nBosses: {downedEntries[0]}/{totalEntries[0]}";
						}
						if (configs.FilterMiniBosses != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\nMini-Bosses: {downedEntries[1]}/{totalEntries[1]}";
						}
						if (configs.FilterEvents != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\nEvents: {downedEntries[2]}/{totalEntries[2]}";
						}
					}
				}
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

				foreach (Vector2 direction in ChatManager.ShadowDirections) {
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + direction * 1,
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}
				Vector2 size = ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets,
					new Vector2(2, 15 + 3) + hitbox.TopLeft(), Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out hoveredSnippet, hitbox.Width - (7 * 2), false);
			}
		}

		internal class SubpageButton : UIPanel
		{
			string buttonString;
			int AltButtonNum;

			public SubpageButton(string type) {
				buttonString = type;
				AltButtonNum = -1;
			}

			public SubpageButton(int num) {
				buttonString = "";
				AltButtonNum = num;
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
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(translated);
				Vector2 pos = new Vector2(innerDimensions.X + ((Width.Pixels - stringAdjust.X) / 2) - 12, innerDimensions.Y - 10);
				if (AltButtonNum == -1) {
					spriteBatch.DrawString(FontAssets.MouseText.Value, translated, pos, Color.Gold);
				}

				Rectangle exclamPos = new Rectangle((int)GetInnerDimensions().X - 12, (int)GetInnerDimensions().Y - 12, 32, 32);

				if (AltButtonNum >= 0) {
					if (BossLogUI.CategoryPageNum == CategoryPage.Record) {
						string[] hoverTexts = {
							"Previous Attempt",
							"First Record",
							"Best Record",
							"World Record"
						};

						int selected = 0;

						if (AltButtonNum == BossLogUI.AltPageSelected[(int)CategoryPage.Record]) {
							selected = 1;
						}
						else if (AltButtonNum == BossLogUI.CompareState) {
							selected = 2;
						}

						Asset<Texture2D> texture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordTabs", AssetRequestMode.ImmediateLoad);
						Rectangle exclamCut = new Rectangle(32 * AltButtonNum, 32 * selected, 32, 32);
						spriteBatch.Draw(texture.Value, exclamPos, exclamCut, Color.White);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = hoverTexts[AltButtonNum];
						}
					}
					else if (BossLogUI.CategoryPageNum == CategoryPage.Spawn) {
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
					else if (BossLogUI.CategoryPageNum == CategoryPage.Loot) {
						Asset<Texture2D> texture = ModContent.Request<Texture2D>("Terraria/Images/UI/Achievement_Categories", AssetRequestMode.ImmediateLoad);
						if (BossLogUI.AltPageSelected[(int)BossLogUI.CategoryPageNum] == 0) {
							Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
							spriteBatch.Draw(texture.Value, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) {
								BossUISystem.Instance.UIHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewCollect");
							}
						}
						else {
							Rectangle exclamCut = new Rectangle(34 * 1, 0, 32, 32);
							spriteBatch.Draw(texture.Value, exclamPos, exclamCut, Color.White);
							if (IsMouseHovering) {
								BossUISystem.Instance.UIHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewLoot");
							}
						}
					}
				}
			}
		}
	}
}
