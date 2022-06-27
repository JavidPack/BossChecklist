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
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.ModLoader.Config;

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
						BossUISystem.Instance.UIHoverText = translated;
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
				bool maskedItems = BossChecklist.BossLogConfig.MaskBossLoot || (BossChecklist.BossLogConfig.MaskHardMode && !Main.hardMode && selectedBoss.progression > BossTracker.WallOfFlesh);

				if (Id.StartsWith("loot_")) {
					if (maskedItems && !selectedBoss.IsDownedOrForced) {
						// Boss Silhouettes always makes itemslot background red, reguardless of obtainable
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
					}
					else if (hasItem) {
						// Otherwise, if the item is obtained make the itemslot background green
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack3;
					}
					else if ((item.expert && !Main.expertMode) || (item.master && !Main.masterMode)) {
						// If not obtained and the item is mode restricted, itemslot background is red
						TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
					}
					// Otherwise, any unobtained items use the original trash-itemslot background color
				}

				string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
				string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");

				if (maskedItems && !selectedBoss.IsDownedOrForced && Id.StartsWith("loot_")) {
					item.color = Color.Black;
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
					string hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.MaskedItems", selectedBoss.DisplayName);
					Rectangle rect2 = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 32, 32);
					if ((item.expert || item.expertOnly) && !Main.expertMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyExpert").Value, rect2, Color.White);
						hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly");
					}
					if ((item.master || item.masterOnly) && !Main.masterMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyMaster").Value, rect2, Color.White);
						hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly");
					}
					if (IsMouseHovering) {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
					return;
				}
				else if (item.type != ItemID.None || hoverText == demonAltar || hoverText == crimsonAltar) {
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
				}

				// Set the itemslot textures back to their original state
				TextureAssets.InventoryBack6 = backup;
				TextureAssets.InventoryBack7 = backup2;

				// Draw evil altars if needed
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
				if (item.type != ItemID.None && Id.StartsWith("loot_")) {
					// Draw collectible border
					if (selectedBoss.collection.Contains(item.type)) {
						string texturePath = "BossChecklist/Resources/Extra_HighlightedCollectible";
						Asset<Texture2D> border = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
						spriteBatch.Draw(border.Value, rectangle.TopLeft(), Color.White);
					}
					if (hasItem) {
						// Obtainability check take priority over any expert/master mode restriction
						if (!maskedItems || (maskedItems && selectedBoss.IsDownedOrForced)) {
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

				if (Id.StartsWith("loot_") && BossChecklist.DebugConfig.ShowCollectionType) {
					BossInfo boss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
					boss.collectType.TryGetValue(item.type, out CollectionType type);

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
						if (item.type != ItemID.None && Id.StartsWith("loot_") && !hasItem) {
							if (!Main.expertMode && (item.expert || item.expertOnly)) {
								BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly";
							}
							else if (!Main.masterMode && (item.master || item.masterOnly)) {
								BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly";
							}
							else {
								BossUISystem.Instance.UIHoverText = item.HoverName;
							}
							Main.HoverItem = item;
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

				if (BossLogUI.PageNum == -3) {
					if (Id == "PageOne") {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 10, GetInnerDimensions().Y + 15);
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.BeforeYouBegin");
						Utils.DrawBorderString(spriteBatch, message, pos, Color.White, 0.8f);

						float textScale = 1f;
						message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.EnableProgressionMode");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message) * textScale;
						pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
					else if (Id == "PageTwo") {
						float textScale = 1f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.SelectAnOption");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
				}
				if (BossLogUI.PageNum == -1) { // Table of Contents
					List<BossInfo> bossList = BossChecklist.bossTracker.SortedBosses;
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					if (Id == "PageOne") {
						float textScale = 0.6f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.PreHardmode");
						Vector2 stringSize = FontAssets.DeathText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}
					else if (Id == "PageTwo") {
						float textScale = 0.6f;
						string message = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Hardmode");
						Vector2 stringSize = FontAssets.DeathText.Value.MeasureString(message) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 15);
						Utils.DrawBorderStringBig(spriteBatch, message, pos, Colors.RarityAmber, textScale);
					}

					if (!IsMouseHovering) {
						headNum = -1;
					}

					if (headNum != -1) {
						BossInfo headBoss = BossChecklist.bossTracker.SortedBosses[headNum];
						int headsDisplayed = 0;
						int offset = 0;
						Color maskedHead = BossLogUI.MaskBoss(headBoss);
						foreach (Asset<Texture2D> headIcon in headBoss.headIconTextures) {
							Texture2D head = headIcon.Value;
							headsDisplayed++;
							spriteBatch.Draw(head, new Rectangle(Main.mouseX + 15 + ((head.Width + 2) * offset), Main.mouseY + 15, head.Width, head.Height), maskedHead);
							offset++;
						}
					}
				}
				else if (BossLogUI.PageNum == -2) { // Mod Developers Credits
					if (Id == "PageOne") {
						// Credits Page
						string specialThanks = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksDevs");
						float textScale = 1.15f;
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(specialThanks) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 10);
						Utils.DrawBorderString(spriteBatch, specialThanks, pos, Main.DiscoColor, textScale);

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

							Vector2 userpos = new Vector2(pageRect.X + (pageRect.Width / 2) - 30 + (left ? -85 : 85) - (panini ? 10 : 0), pageRect.Y + 75 + (125 * row));
							Rectangle userselected = new Rectangle(0 + (60 * i), 0, 60 + (panini ? 10 : 0), 58);
							spriteBatch.Draw(users.Value, userpos, userselected, Color.White);
							
							Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(usernames[i]);
							Vector2 stringPos = new Vector2(userpos.X + (userselected.Width / 2) - ((stringAdjust.X * nameScaling) / 2) + (panini ? 5 : 0), userpos.Y - 25);
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
							string thanksMods = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksMods");
							float textScale = 1.15f;
							Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(thanksMods) * textScale;
							Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 10);
							Utils.DrawBorderString(spriteBatch, thanksMods, pos, Color.LightSkyBlue, textScale);

							string notice = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.Notice");
							textScale = 0.9f;
							Vector2 stringSize2 = FontAssets.MouseText.Value.MeasureString(notice) * textScale;
							Vector2 pos2 = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize2.X / 2), pos.Y + stringSize2.Y + 5);
							Utils.DrawBorderString(spriteBatch, notice, pos2, Color.LightBlue, textScale);
						}
					}
				}
				else if (BossLogUI.PageNum >= 0) { // Boss Pages
					selectedBoss = BossChecklist.bossTracker.SortedBosses[BossLogUI.PageNum];
					bool masked = BossLogUI.MaskBoss(selectedBoss) == Color.Black;
					if (Id == "PageOne") {
						if (selectedBoss.customDrawing != null) {
							// If a custom drawing is active, full drawing control is given to the modder within the boss portrait
							// Nothing else will be drawn, including any base texture. Modders must supply that if they wish.
							selectedBoss.customDrawing(spriteBatch, pageRect, BossLogUI.MaskBoss(selectedBoss));
						}
						else {
							Asset<Texture2D> bossTexture = null;
							Rectangle bossSourceRectangle = new Rectangle();
							if (selectedBoss.portraitTexture != null) {
								bossTexture = selectedBoss.portraitTexture;
								bossSourceRectangle = new Rectangle(0, 0, bossTexture.Value.Width, bossTexture.Value.Height);
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
								spriteBatch.Draw(bossTexture.Value, pageRect.Center(), bossSourceRectangle, BossLogUI.MaskBoss(selectedBoss), 0, bossSourceRectangle.Center(), drawScale, SpriteEffects.None, 0f);
							}
						}

						// Everything below this point is outside of the boss portrait (Boss head icons, boss names, etc)

						Rectangle firstHeadPos = new Rectangle();
						bool countedFirstHead = false;
						int offset = 0;
						int totalWidth = 0;
						int lastX = 0;
						foreach (Asset<Texture2D> headTexture in selectedBoss.headIconTextures) {
							Texture2D head = headTexture.Value;
							Rectangle src = new Rectangle(0, 0, head.Width, head.Height);
							// Weird special case for Deerclops. Its head icon has a significant amount of whitespace.
							if (selectedBoss.Key == "Terraria Deerclops") {
								src = new Rectangle(2, 0, 48, 40);
							}
							int xHeadOffset = pageRect.X + pageRect.Width - src.Width - 10 - ((src.Width + 2) * offset);
							Rectangle headPos = new Rectangle(xHeadOffset, pageRect.Y + 5, src.Width, src.Height);
							if (!countedFirstHead) {
								firstHeadPos = headPos;
								countedFirstHead = true;
							}
							spriteBatch.Draw(head, headPos, src, BossLogUI.MaskBoss(selectedBoss));
							offset++;
							totalWidth += headPos.Width;
							lastX = xHeadOffset;
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
						if (BossLogUI.MouseIntersects(lastX, firstHeadPos.Y, totalWidth, firstHeadPos.Height)) {
							BossUISystem.Instance.UIHoverText = selectedBoss.IsDownedOrForced ? isDefeated : notDefeated;
							BossUISystem.Instance.UIHoverTextColor = selectedBoss.IsDownedOrForced ? Colors.RarityGreen : Colors.RarityRed;
						}

						bool enabledCopyButtons = BossChecklist.DebugConfig.AccessInternalNames && selectedBoss.modSource != "Unknown";
						Vector2 pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 5);
						Utils.DrawBorderString(spriteBatch, selectedBoss.DisplayName, pos, Color.Goldenrod);

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
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopyKey");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.Key}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.Key;
								}
							}

							vec2 = new Vector2(pageRect.X + 5, pageRect.Y + 30);
							copied = (Platform.Get<IClipboard>().Value == selectedBoss.modSource) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, vec2, copied);

							if (BossLogUI.MouseIntersects(vec2.X, vec2.Y, clipboard.Width, clipboard.Height)) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopySource");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.modSource}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.modSource;
								}
							}
						}
					}
					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == 0 && selectedBoss.modSource != "Unknown") {
						if (selectedBoss.type == EntryType.Boss) {
							// Boss Records Subpage
							Asset<Texture2D> construction = ModContent.Request<Texture2D>("Terraria/Images/UI/Creative/Journey_Toggle", AssetRequestMode.ImmediateLoad);
							Rectangle innerRect = pageRect;
							Rectangle conRect = new Rectangle(innerRect.X + innerRect.Width - 32 - 30, innerRect.Y + 100, 32, 34);
							spriteBatch.Draw(construction.Value, conRect, Color.White);

							if (Main.mouseX >= conRect.X && Main.mouseX < conRect.X + conRect.Width) {
								if (Main.mouseY >= conRect.Y && Main.mouseY < conRect.Y + conRect.Height) {
									string translated = 
									BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.UnderConstruction";
									BossUISystem.Instance.UIHoverTextColor = Color.Gold;
								}
							}

							foreach (BossInfo info in BossChecklist.bossTracker.SortedBosses) {
								if (info.type != EntryType.Event) {
									continue;
								}
								if (info.npcIDs.Contains(selectedBoss.npcIDs[0])) {
									Texture2D icon = info.headIconTextures[0].Value;
									Vector2 pos = new Vector2(pageRect.X + 15, pageRect.Y + 100);
									Color faded = info.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
									spriteBatch.Draw(icon, pos, faded);
									if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + icon.Width) {
										if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + icon.Height) {
											string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
											BossUISystem.Instance.UIHoverText = info.DisplayName + "\n" + translated;
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												// Reset UI positions when changing the page
												BossLogUI.PageNum = BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == info.Key);
												BossUISystem.Instance.BossLog.ResetUIPositioning();
											}
										}
									}
								}
							}

							// Beginning of record drawing
							Asset<Texture2D> achievements = ModContent.Request<Texture2D>("Terraria/Images/UI/Achievements");
							PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

							if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
								return;
							}

							BossStats record = modPlayer.RecordsForWorld[BossLogUI.PageNumToRecordIndex(modPlayer.RecordsForWorld)].stat;
							WorldStats wldRecord = WorldAssist.worldRecords[BossLogUI.PageNumToRecordIndex(WorldAssist.worldRecords)].stat;

							string recordTitle = "";
							string recordValue = "";
							int[] achCoord = new int[] { 0, 0 };

							for (int recordSlot = 0; recordSlot < 4; recordSlot++) { // 4 spots total
								if (recordSlot == 0) {
									recordValue = Main.LocalPlayer.name;
									// Which sub-category are we in?
									achCoord = new int[] { -1, -1 }; // No achievement drawing
									if (BossLogUI.RecordPageSelected == RecordType.PreviousAttempt) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PreviousRecord");
									}
									else if (BossLogUI.RecordPageSelected == RecordType.FirstRecord) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.FirstRecord");
									}
									else if (BossLogUI.RecordPageSelected == RecordType.BestRecord) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BestRecord");
									}
									else if (BossLogUI.RecordPageSelected == RecordType.WorldRecord) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.WorldRecord");
										recordValue = Main.worldName;
									}
								}
								if (recordSlot == 1) {
									if (BossLogUI.RecordPageSelected == RecordType.WorldRecord) {
										// World Global Kills & Deaths
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
									else if (BossLogUI.RecordPageSelected == RecordType.BestRecord) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Attempt");
										achCoord = new int[] { 0, 9 };
										if (record.kills == 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										}
										else {
											recordValue = $"#{record.kills}";
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.FirstRecord) {
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Defeats");
										achCoord = new int[] { 4, 8 };
										if (record.kills == 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										}
										else {
											string deathTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths");
											recordValue = $"{record.deaths} {deathTerm}";
										}
									}
									else {
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
								}
								else if (recordSlot == 2) {
									// Fight Duration
									recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Duration");
									achCoord = new int[] { 4, 9 };

									if (BossLogUI.RecordPageSelected == RecordType.PreviousAttempt) {
										// Last Attempt
										if (record.durationPrev == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationPrev);
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.FirstRecord) {
										// First Victory
										if (record.durationFirs == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationFirs);
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.BestRecord) {
										// Personal Best
										if (record.durationBest == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(record.durationBest);
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.WorldRecord) {
										// World Record
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.DurationWorld");
										achCoord = new int[] { 2, 12 };
										if (wldRecord.durationWorld < 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = RecordTimeConversion(wldRecord.durationWorld);
										}
									}
								}
								else if (recordSlot == 3) { // Hits Taken
									recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Dodge");
									achCoord = new int[] { 3, 0 };

									if (BossLogUI.RecordPageSelected == RecordType.PreviousAttempt) {
										// Last Attempt
										if (record.hitsTakenPrev == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenPrev.ToString();
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.FirstRecord) {
										// First Victory
										if (record.hitsTakenFirs == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenFirs.ToString();
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.BestRecord) {
										// Personal Best
										if (record.hitsTakenBest == -1) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = record.hitsTakenBest.ToString();
										}
									}
									else if (BossLogUI.RecordPageSelected == RecordType.WorldRecord) {
										// World Record
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.DodgeWorld");
										achCoord = new int[] { 0, 7 };
										
										if (wldRecord.durationWorld < 0) {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");
										}
										else {
											recordValue = wldRecord.hitsTakenWorld.ToString();
										}
									}
								}

								if (achCoord[0] != -1) {
									Rectangle slotPos = new Rectangle(pageRect.X + 15, pageRect.Y + 100 + (75 * recordSlot), 64, 64);
									Rectangle cutRect = new Rectangle(66 * achCoord[0], 66 * achCoord[1], 64, 64);

									Asset<Texture2D> slot = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordSlot", AssetRequestMode.ImmediateLoad);
									spriteBatch.Draw(slot.Value, new Vector2(slotPos.X, slotPos.Y), new Color(175, 175, 125));
									spriteBatch.Draw(achievements.Value, slotPos, cutRect, Color.White);

									if (BossLogUI.MouseIntersects(slotPos.X, slotPos.Y, 64, 64)) {
										// TODO: Change these texts to something better. A description of the record type
										if (recordSlot == 1) {
											if (BossLogUI.RecordPageSelected == RecordType.PreviousAttempt) {
												BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.KDRDescription";
											}
											else if (BossLogUI.RecordPageSelected == RecordType.FirstRecord) {
												BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.FirstKDRDescription";
											}
											else if (BossLogUI.RecordPageSelected == RecordType.BestRecord) {
												BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.BestKDRDescription";
											}
											else if (BossLogUI.RecordPageSelected == RecordType.WorldRecord) {
												BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.GlobalKDRDescription";
											}
										}
										if (recordSlot == 2) {
											BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.DurationDescription";
										}
										if (recordSlot == 3) {
											BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.HitsTakenDescription";
										}
									}

									// Draw trophies hoverover for World Record holder names
									if (BossLogUI.RecordPageSelected == RecordType.WorldRecord && (recordSlot == 2 || recordSlot == 3)) {
										Asset<Texture2D> trophy = Main.Assets.Request<Texture2D>($"Images/Item_{ItemID.GolfTrophyGold}", AssetRequestMode.ImmediateLoad);
										Vector2 trophyPos = new Vector2(slotPos.X + slot.Value.Width - trophy.Value.Width / 2, slotPos.Y + slot.Value.Height / 2 - trophy.Value.Height / 2);
										spriteBatch.Draw(trophy.Value, trophyPos, Color.White);

										string message = "$Mods.BossChecklist.BossLog.HoverText.ClaimRecord";
										if (BossLogUI.MouseIntersects(trophyPos.X, trophyPos.Y, trophy.Value.Width, trophy.Value.Height)) {
											string holderText = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.RecordHolder");
											if (recordSlot == 2 && !string.IsNullOrEmpty(wldRecord.durationHolder)) {
												message = $"{holderText}\n" + wldRecord.durationHolder;
											}
											else if (recordSlot == 3 && !string.IsNullOrEmpty(wldRecord.hitsTakenHolder)) {
												message = $"{holderText}\n" + wldRecord.hitsTakenHolder;
											}
											BossUISystem.Instance.UIHoverText = message;
										}
									}

									// Draw compare numbers if selected
									if (BossLogUI.CompareState != RecordType.None && recordSlot != 0 && recordSlot != 1) {
										int initialRecordValue = -1;
										int compareRecordValue = -1;
										string comparisonValue = "";

										if (recordSlot == 2) {
											// Duration comparison
											initialRecordValue = GetRecordValue(BossLogUI.RecordPageSelected, RecordID.Duration);
											compareRecordValue = GetRecordValue(BossLogUI.CompareState, RecordID.Duration);
											comparisonValue = RecordTimeConversion(initialRecordValue - compareRecordValue);
										}
										else if (recordSlot == 3) {
											// Hits Taken comparison
											initialRecordValue = GetRecordValue(BossLogUI.RecordPageSelected, RecordID.HitsTaken);
											compareRecordValue = GetRecordValue(BossLogUI.CompareState, RecordID.HitsTaken);
											comparisonValue = (initialRecordValue - compareRecordValue).ToString();
										}

										if (comparisonValue != "" && initialRecordValue >= 0 && compareRecordValue >= 0) {
											bool badRecord = compareRecordValue < initialRecordValue;
											string sign = badRecord ? "+" : (initialRecordValue - compareRecordValue == 0) ? "-" : "";
											comparisonValue = sign + comparisonValue;

											float textScale = 0.65f;
											Vector2 textSize = FontAssets.MouseText.Value.MeasureString(comparisonValue) * textScale;
											Vector2 textPos = new Vector2(slotPos.X + slot.Value.Width - 50 - (textSize.X / 2), slotPos.Y + slot.Value.Height - textSize.Y);
											Color textColor = badRecord ? Color.LightSalmon : Color.LightGreen;
											Utils.DrawBorderString(spriteBatch, comparisonValue, textPos, textColor, textScale);

											string compareRecordString = recordSlot == 2 ? RecordTimeConversion(compareRecordValue) : compareRecordValue.ToString();
											textSize = FontAssets.MouseText.Value.MeasureString(compareRecordString) * textScale;
											textPos = new Vector2(slotPos.X + slot.Value.Width - 50 - (textSize.X / 2), slotPos.Y + 6);
											Utils.DrawBorderString(spriteBatch, compareRecordString, textPos, Color.SkyBlue, textScale);
										}

									}
								}

								int offsetY = 110 + (recordSlot * 75);
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
							}
						}
						else if (selectedBoss.type == EntryType.MiniBoss) {
							foreach (BossInfo info in BossChecklist.bossTracker.SortedBosses) {
								if (info.type != EntryType.Event) {
									continue;
								}
								if (info.npcIDs.Contains(selectedBoss.npcIDs[0])) {
									Texture2D icon = info.headIconTextures[0].Value;
									Vector2 pos = new Vector2(pageRect.X + 15, pageRect.Y + 100);
									Color faded = info.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
									spriteBatch.Draw(icon, pos, faded);
									if (Main.mouseX >= pos.X && Main.mouseX <= pos.X + icon.Width) {
										if (Main.mouseY >= pos.Y && Main.mouseY <= pos.Y + icon.Height) {
											string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
											BossUISystem.Instance.UIHoverText = info.DisplayName + "\n" + translated;
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												// Reset UI positions when changing the page
												BossLogUI.PageNum = BossChecklist.bossTracker.SortedBosses.FindIndex(x => x.Key == info.Key);
												BossUISystem.Instance.BossLog.ResetUIPositioning();
											}
										}
									}
								}
							}
						}
						else if (selectedBoss.type == EntryType.Event) {
							var bosses = BossChecklist.bossTracker.SortedBosses;
							int offset = 0;
							int offsetY = 0;

							int headTextureOffsetX = 0;
							int headTextureOffsetY = 0;

							foreach (int npcID in selectedBoss.npcIDs) {
								int npcIndex = bosses.FindIndex(x => x.npcIDs.Contains(npcID) && x.type != EntryType.Event);
								if (npcIndex == -1) {
									continue;
								}

								BossInfo addedNPC = bosses[npcIndex];
								Texture2D head = addedNPC.headIconTextures[0].Value;
								Vector2 pos = new Vector2(pageRect.X + headTextureOffsetX + 15, pageRect.Y + 100);
								Color headColor = addedNPC.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;

								spriteBatch.Draw(head, pos, headColor);
								headTextureOffsetX += head.Width + 5;
								if (BossLogUI.MouseIntersects(pos.X, pos.Y, head.Width, head.Height)) {
									string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
									BossUISystem.Instance.UIHoverText = addedNPC.DisplayName + "\n" + translated;
									if (Main.mouseLeft && Main.mouseLeftRelease) {
										// Reset UI positions when changing the page
										BossLogUI.PageNum = npcIndex;
										BossUISystem.Instance.BossLog.ResetUIPositioning();
									}
								}
								if (head.Height > headTextureOffsetY) {
									headTextureOffsetY = head.Height;
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
									Color bannerColor = reachedKillCount ? Color.White : masked ? Color.Black : BossLogUI.faded;
									if (bannerID <= 0 || NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npcID)]) {
										continue;
									}

									for (int j = 0; j < 3; j++) {
										Vector2 pos = new Vector2(pageRect.X + offset + 15, pageRect.Y + 100 + (16 * j) + offsetY);
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
									if (bannerItemID <= 0 || !ContentSamples.ItemsByType.TryGetValue(bannerItemID, out Item item)) {
										continue;
									}

									if (item.createTile <= -1) {
										continue;
									}

									Main.instance.LoadTiles(item.createTile);
									Asset<Texture2D> banner = TextureAssets.Tile[item.createTile];

									// Code adapted from TileObject.DrawPreview
									var tileData = TileObjectData.GetTileData(item.createTile, item.placeStyle);
									int styleColumn = tileData.CalculatePlacementStyle(item.placeStyle, 0, 0); // adjust for StyleMultiplier
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
									int bannerItem = NPCLoader.GetNPC(npcID).BannerItem;
									string source = NPCLoader.GetNPC(npcID).Mod.DisplayName;
									bool reachedKillCount = NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem];

									Color bannerColor = NPC.killCount[bannerID] >= 50 ? Color.White : masked ? Color.Black : BossLogUI.faded;
									
									int[] heights = tileData.CoordinateHeights;
									int heightOffSet = 0;
									int heightOffSetTexture = 0;
									for (int j = 0; j < heights.Length; j++) { // could adjust for non 1x3 here and below if we need to.
										Vector2 pos = new Vector2(pageRect.X + offset + 15, pageRect.Y + 100 + heightOffSet + offsetY);
										Rectangle rect = new Rectangle(x, y + heightOffSetTexture, tileData.CoordinateWidth, tileData.CoordinateHeights[j]);
										Main.spriteBatch.Draw(banner.Value, pos, rect, bannerColor);
										heightOffSet += heights[j];
										heightOffSetTexture += heights[j] + tileData.CoordinatePadding;

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
							}
						}
					}
					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == CategoryPage.Spawn) {
						// Spawn Item Subpage
					}

					if (Id == "PageTwo" && BossLogUI.CategoryPageNum == CategoryPage.Loot) {
						if (BossLogUI.RecordPageSelected == RecordType.PreviousAttempt) {
							// Loot Table Subpage
							Asset<Texture2D> bagTexture;
							if (selectedBoss.treasureBag != 0) {
								Main.instance.LoadItem(selectedBoss.treasureBag);
								bagTexture = TextureAssets.Item[selectedBoss.treasureBag];
							}
							else {
								bagTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_TreasureBag", AssetRequestMode.ImmediateLoad);
							}
							DrawAnimation drawAnim = Main.itemAnimations[selectedBoss.treasureBag]; // 0 is null
							Rectangle srcRect = drawAnim != null ? srcRect = drawAnim.GetFrame(bagTexture.Value) : bagTexture.Value.Bounds;
							Rectangle posRect = new Rectangle(pageRect.X + (pageRect.Width / 2) - 5 - (bagTexture.Width() / 2), pageRect.Y + 88, srcRect.Width, srcRect.Height);
							spriteBatch.Draw(bagTexture.Value, posRect, srcRect, Color.White);
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

			public int GetRecordValue(RecordType type, RecordID id) {
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				BossStats records = modPlayer.RecordsForWorld[BossLogUI.PageNumToRecordIndex(modPlayer.RecordsForWorld)].stat;
				WorldStats worldRecords = WorldAssist.worldRecords[BossLogUI.PageNumToRecordIndex(WorldAssist.worldRecords)].stat;
				if (id == RecordID.None || id == RecordID.ResetAll) {
					return -1;
				}

				if (type == RecordType.PreviousAttempt) {
					return id == RecordID.Duration ? records.durationPrev : records.hitsTakenPrev;
				}
				else if (type == RecordType.FirstRecord) {
					return id == RecordID.Duration ? records.durationFirs : records.hitsTakenFirs;
				}
				else if (type == RecordType.BestRecord) {
					return id == RecordID.Duration ? records.durationBest : records.hitsTakenBest;
				}
				else if (type == RecordType.WorldRecord) {
					return id == RecordID.Duration ? worldRecords.durationWorld : worldRecords.hitsTakenWorld;
				}

				return -1;
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

					if (DrawTab(Id) && BossLogUI.PageNum != -3) {
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

				if (Id.EndsWith("_Tab") && BossLogUI.PageNum != -3) {
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
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpBoss", bossList[BossLogUI.FindNext(EntryType.Boss)].DisplayName);
						}
						else if (Id == "Miniboss_Tab" && BossLogUI.FindNext(EntryType.MiniBoss) != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpMini", bossList[BossLogUI.FindNext(EntryType.MiniBoss)].DisplayName);
						}
						else if (Id == "Event_Tab" && BossLogUI.FindNext(EntryType.Event) != -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.JumpEvent", bossList[BossLogUI.FindNext(EntryType.Event)].DisplayName);
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
						BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ToggleVisibility";
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
			string bossKey;
			string displayName;

			public TableOfContents(int pageNum, float order, string displayName, string bossKey, bool downed, bool nextCheck, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
				PageNum = pageNum;
				this.order = order;
				this.nextCheck = nextCheck;
				this.downed = downed;
				Recalculate();
				this.bossKey = bossKey;
				this.displayName = displayName;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				CalculatedStyle inner = GetInnerDimensions();
				Vector2 pos = new Vector2(inner.X - 20, inner.Y - 5);
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				List<BossInfo> sortedBosses = BossChecklist.bossTracker.SortedBosses;
				// name check, for when progression matches
				// index should never be -1 since variables passed in are within bounds
				int index = sortedBosses.FindIndex(x => x.progression == order && (x.Key == bossKey));

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

				bool ItemDataExists = modPlayer.BossItemsCollected.TryGetValue(sortedBosses[index].Key, out List<ItemDefinition> items);
				if (BossChecklist.BossLogConfig.LootCheckVisibility && ItemDataExists) {
					bool allLoot = true;
					bool allCollect = true;

					// Loop through player saved loot and boss loot to see if every item was obtained
					foreach (int loot in sortedBosses[index].lootItemTypes) {
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
						int indexLoot = items.FindIndex(x => x.Type == loot);
						// Skip expert/master mode items if the world is not in expert/master mode.
						// TODO: Do something similar for task related items, such as Otherworld music boxes needing to be unlocked.
						if (!Main.expertMode || !Main.masterMode) {
							Item checkItem = ContentSamples.ItemsByType[loot];
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
						int indexCollect = items.FindIndex(x => x.Type == collectible);
						if (!Main.expertMode || !Main.masterMode) {
							Item checkItem = ContentSamples.ItemsByType[collectible];
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

					CalculatedStyle parent = this.Parent.GetInnerDimensions();
					int hardModeOffset = sortedBosses[index].progression > BossTracker.WallOfFlesh ? 10 : 0;
					string looted = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllLoot");
					string collected = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllCollectibles");

					if (allLoot && allCollect) {
						Texture2D texture = BossLogUI.goldChestTexture.Value;
						Vector2 pos2 = new Vector2(parent.X + parent.Width - texture.Width - hardModeOffset, inner.Y - 2);
						spriteBatch.Draw(texture, pos2, Color.White);
						if (BossLogUI.MouseIntersects(pos2.X, pos2.Y, texture.Width, texture.Height)) {
							BossUISystem.Instance.UIHoverText = $"{looted}\n{collected}";
						}
					}
					else if (allLoot || allCollect) {
						Texture2D texture = BossLogUI.chestTexture.Value;
						Vector2 pos2 = new Vector2(parent.X + parent.Width - texture.Width - hardModeOffset, inner.Y - 2);
						spriteBatch.Draw(texture, pos2, Color.White);
						if (BossLogUI.MouseIntersects(pos2.X, pos2.Y, texture.Width, texture.Height)) {
							BossUISystem.Instance.UIHoverText = allLoot ? looted : collected;
						}
					}
				}

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
				int barWidth = (int)inner.Width - 8;

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
				pos = new Rectangle((int)inner.X + (int)inner.Width - 4, (int)inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				BossLogConfiguration configs = BossChecklist.BossLogConfig;
				int allDownedEntries = downedEntries[0];
				int allAccountedEntries = totalEntries[0];
				if (!configs.OnlyBosses) {
					if (configs.FilterMiniBosses != "Hide") {
						allDownedEntries += downedEntries[1];
						allAccountedEntries += totalEntries[1];
					}
					if (configs.FilterEvents != "Hide") {
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
					string total = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Total");
					string bosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Bosses");
					string miniBosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.MiniBosses");
					string events = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Events");
					BossUISystem.Instance.UIHoverText = $"{total}: {allDownedEntries}/{allAccountedEntries}";
					if (!configs.OnlyBosses) {
						if (configs.FilterMiniBosses != "Hide" || configs.FilterEvents != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\n{bosses}: {downedEntries[0]}/{totalEntries[0]}";
						}
						if (configs.FilterMiniBosses != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\n{miniBosses}: {downedEntries[1]}/{totalEntries[1]}";
						}
						if (configs.FilterEvents != "Hide") {
							BossUISystem.Instance.UIHoverText += $"\n{events}: {downedEntries[2]}/{totalEntries[2]}";
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

				string translation = Language.GetTextValue(text);
				TextSnippet[] textSnippets = ChatManager.ParseMessage(translation, Color.White).ToArray();
				ChatManager.ConvertNormalSnippets(textSnippets);

				foreach (Vector2 direction in ChatManager.ShadowDirections) {
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + direction * 1,
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}
				Vector2 size = ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets,
					new Vector2(2, 15 + 3) + hitbox.TopLeft(), Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out int hoveredSnippet, hitbox.Width - (7 * 2), false);
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
				if (buttonString == "") {
					BackgroundColor = Color.Transparent;
					BorderColor = Color.Transparent;
				}
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
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PreviousRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.FirstRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BestRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.WorldRecord")
						};

						int selected = 0;

						if (AltButtonNum == (int)BossLogUI.RecordPageSelected) {
							selected = 1;
						}
						else if (AltButtonNum == (int)BossLogUI.CompareState) {
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
						// NO CURRENT ALTPAGE, BUTTON NOT NEEDED
					}
					else if (BossLogUI.CategoryPageNum == CategoryPage.Loot) {
						// NO CURRENT ALTPAGE, BUTTON NOT NEEDED
					}
				}
			}
		}
	}
}
