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
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;
using System;

namespace BossChecklist.UIElements
{
	internal static class BossLogUIElements
	{
		/// <summary>
		/// Hides certain mouse over interactions from appearing such as tile icons or NPC names.
		/// </summary>
		static void HideMouseOverInteractions() 
		{
			Main.mouseText = true;
			Main.LocalPlayer.cursorItemIconEnabled = false;
			Main.LocalPlayer.cursorItemIconID = -1;
			Main.ItemIconCacheUpdate(0);
		}

		internal class OpenLogButton : UIImageButton
		{
			internal Asset<Texture2D> texture;
			private Vector2 offset;
			internal bool dragging;

			public OpenLogButton(Asset<Texture2D> texture) : base(texture) {
				this.texture = texture;
			}

			private void DragStart(UIMouseEvent evt) {
				var dimensions = GetDimensions().ToRectangle();
				offset = new Vector2(evt.MousePosition.X - dimensions.Left, evt.MousePosition.Y - dimensions.Top); // aligns the element's center with the mouse position
				dragging = true;
			}

			private void DragEnd(UIMouseEvent evt) {
				// Set the new position
				Left.Set(evt.MousePosition.X - Main.screenWidth - offset.X, 1f);
				Top.Set(evt.MousePosition.Y - Main.screenHeight - offset.Y, 1f);
				Recalculate();

				// Update and save the new button position
				BossChecklist.BossLogConfig.BossLogPos = new Vector2(Left.Pixels, Top.Pixels);
				BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

				dragging = false;
			}

			public override void RightMouseDown(UIMouseEvent evt) {
				base.RightMouseDown(evt);
				DragStart(evt);
			}

			public override void RightMouseUp(UIMouseEvent evt) {
				base.RightMouseUp(evt);
				DragEnd(evt);
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				if (dragging) {
					Left.Set(Main.mouseX - Main.screenWidth - offset.X, 1f);
					Top.Set(Main.mouseY - Main.screenHeight - offset.Y, 1f);
					//Recalculate();
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
				base.DrawSelf(spriteBatch);

				if ((ContainsPoint(Main.MouseScreen) || dragging) && !PlayerInput.IgnoreMouseInterface) {
					Main.LocalPlayer.mouseInterface = true;
					HideMouseOverInteractions();
				}

				// When hovering over the button, draw a 'Boss Log' text over the button
				CalculatedStyle bookArea = GetInnerDimensions();
				string hoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BossLog");
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(hoverText);
				Vector2 pos = new Vector2(bookArea.X - (stringAdjust.X / 3), bookArea.Y - 24);
				if (IsMouseHovering && !dragging) {
					spriteBatch.DrawString(FontAssets.MouseText.Value, hoverText, pos, Color.White); // text shouldn't appear if dragging the element
				}

				Asset<Texture2D> cover = BossLogUI.colorTexture;
				Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
				if (!IsMouseHovering && !dragging) {
					cover = BossLogUI.fadedTexture; // make the color match the faded UIImageButton
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);
				}
				spriteBatch.Draw(cover.Value, bookArea.ToRectangle(), coverColor);

				// UIImageButtons are normally faded, so if dragging and not draw the button fully opaque
				// This is most likely to occur when the mouse travels off screen while dragging
				if (dragging) {
					spriteBatch.Draw(texture.Value, bookArea.ToRectangle(), Color.White);
				}

				// Determine a border color for the button
				PlayerAssist player = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				Color? borderColor = null;

				if (IsMouseHovering || dragging) {
					borderColor = Color.Goldenrod; // If hovering over or dragging the button, the book will be highlighted in a gold border
				}
				else if (BossChecklist.DebugConfig.NewRecordsDisabled || BossChecklist.DebugConfig.RecordTrackingDisabled || BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
					borderColor = Color.Firebrick; // If Records are disabled in any way, the book will be highlighted with a red border
				}
				else if (!player.hasOpenedTheBossLog || player.hasNewRecord.Any(x => x == true)) {
					float modifier = Main.masterColor / 200f; // If the player has not opened the log or has not viewed a new record page, the book will be hightlighted with a flashing log-colored border
					borderColor = new Color(coverColor.R * modifier, coverColor.G * modifier, coverColor.B * modifier);
				}

				if (borderColor.HasValue) {
					spriteBatch.Draw(BossLogUI.borderTexture.Value, bookArea.ToRectangle(), borderColor.Value); // Draw a colored border if one was set
				}
			}
		}

		internal class NavigationalButton : UIImageButton
		{
			public string Id { get; init; } = "";
			internal Asset<Texture2D> texture;
			internal string hoverText;

			public NavigationalButton(Asset<Texture2D> texture, string hoverText = null) : base(texture) {
				this.texture = texture;
				this.hoverText = hoverText;
			}			

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
					Main.LocalPlayer.mouseInterface = true;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				base.DrawSelf(spriteBatch);
				if (IsMouseHovering && !string.IsNullOrEmpty(hoverText)) {
					BossLogPanel.headNum = -1; // Fixes PageTwo head drawing when clicking on ToC boss and going back to ToC
					BossUISystem.Instance.UIHoverText = Language.GetTextValue(hoverText); // Display the hover text in a tooltip-like box
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

				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[BossUISystem.Instance.BossLog.PageNum];
				bool maskedItems = BossChecklist.BossLogConfig.MaskBossLoot || (BossChecklist.BossLogConfig.MaskHardMode && !Main.hardMode && selectedBoss.progression > BossTracker.WallOfFlesh);

				// Make backups of the original itemslot textures, as we will replace them temporarily for our visuals
				var backup2 = TextureAssets.InventoryBack7;
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

				bool isDemonAltar = hoverText == Language.GetTextValue("MapObject.DemonAltar");
				bool isCrimsonAltar = hoverText == Language.GetTextValue("MapObject.CrimsonAltar");

				if (maskedItems && !selectedBoss.IsDownedOrForced && Id.StartsWith("loot_")) {
					item.color = Color.Black;
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
					item.color = default; // Item color needs to be reset back to it's default state
					TextureAssets.InventoryBack7 = backup2; // Set the itemslot textures back to their original state
					string altHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.MaskedItems", selectedBoss.DisplayName);
					Rectangle rect2 = new Rectangle(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, 32, 32);
					if ((item.expert || item.expertOnly) && !Main.expertMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyExpert").Value, rect2, Color.White);
						altHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly");
					}
					if ((item.master || item.masterOnly) && !Main.masterMode) {
						spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/UI/WorldCreation/IconDifficultyMaster").Value, rect2, Color.White);
						altHoverText = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly");
					}
					if (IsMouseHovering) {
						BossUISystem.Instance.UIHoverText = altHoverText;
					}
					return; // The remaining logic isn't needed when the item is masked (hidden), so end draw code here
				}
				else if (item.type != ItemID.None || isDemonAltar || isCrimsonAltar) {
					if (item.color == Color.Black) {
						item.color = default;
					}
					ItemSlot.Draw(spriteBatch, ref item, context, rectangle.TopLeft());
					TextureAssets.InventoryBack7 = backup2; // Set the itemslot textures back to their original state
				}

				// Draws the evil altars in the designated slots if needed
				if (isCrimsonAltar || isDemonAltar) {
					Main.instance.LoadTiles(TileID.DemonAltar);
					int offsetX = 0;
					int offsetY = 0;
					int offsetSrc = isCrimsonAltar ? 3 : 0;
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
					selectedBoss.collectType.TryGetValue(item.type, out CollectionType type);

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

				// When hovering, determine what the hovertext should be
				if (IsMouseHovering) {
					if (isCrimsonAltar || isDemonAltar || hoverText == Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand")) {
						BossUISystem.Instance.UIHoverText = hoverText; // 'Demon/Crimson Altar' or 'by Hand' should just show their normal hoverText
					}
					else if (item.type != ItemID.None) {
						// All items should show their normal tooltips, unless unobtainable
						if (!Main.expertMode && (item.expert || item.expertOnly) && !hasItem) {
							BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ItemIsExpertOnly";
							BossUISystem.Instance.UIHoverTextColor = Main.DiscoColor;
						}
						else if (!Main.masterMode && (item.master || item.masterOnly) && !hasItem) {
							BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ItemIsMasterOnly";
							BossUISystem.Instance.UIHoverTextColor = new Color(255, (byte)(Main.masterColor * 200f), 0, Main.mouseTextColor); // mimics Master Mode color
						}
						else {
							Main.HoverItem = item;
							Main.hoverItemName = item.HoverName;
						}
					}
					else {
						BossUISystem.Instance.UIHoverText = hoverText; // Empty item, default to hoverText if applicable
					}
				}
				Main.inventoryScale = oldScale;
			}
		}

		internal class LootRow : UIElement
		{
			public string Id { get; init; } = "";
			// Had to put the itemslots in a row in order to be put in a UIList with scroll functionality
			readonly int order;

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

		/// <summary>
		/// Creates an image of a mod's icon when mod and file path are provided.
		/// When hovering over the icon, the mod's display name is shown.
		/// </summary>
		internal class ModIcon : UIElement {
			readonly Asset<Texture2D> icon;
			readonly string modName;

			public ModIcon (string modName, string iconPath) {
				this.modName = modName;
				if (iconPath == "BossChecklist/Resources/Extra_NoIcon") {
					icon = ModContent.Request<Texture2D>(iconPath, AssetRequestMode.ImmediateLoad); // mods without icons use this texture instead
				}
				else {
					icon = ModLoader.GetMod(modName).Assets.Request<Texture2D>(iconPath); // HasAsset check already done before added to Registered list
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);

				spriteBatch.Draw(icon.Value, GetInnerDimensions().ToRectangle(), Color.White); // innerDimensions will resize the icon to the needed size

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					BossUISystem.Instance.UIHoverText = ModLoader.GetMod(modName).DisplayName;
				}
			}
		}

		internal class BossLogPanel : UIElement
		{
			public string Id { get; init; } = "";
			public static int headNum = -1;

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle pageRect = GetInnerDimensions().ToRectangle();
				int selectedLogPage = BossUISystem.Instance.BossLog.PageNum;

				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
					// Needed to remove mousetext from outside sources when using the Boss Log
					Main.player[Main.myPlayer].mouseInterface = true;
					HideMouseOverInteractions();
				}

				if (selectedLogPage == -3) {
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
				if (selectedLogPage == -1) { // Table of Contents
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
				else if (selectedLogPage == -2) {
					if (Id == "PageOne") {
						// Mod Developers Credits
						string specialThanks = Language.GetTextValue("Mods.BossChecklist.BossLog.Credits.ThanksDevs");
						float textScale = 1.15f;
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(specialThanks) * textScale;
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X / 2), pageRect.Y + 10);
						Utils.DrawBorderString(spriteBatch, specialThanks, pos, Main.DiscoColor, textScale);

						Asset<Texture2D> users = BossChecklist.instance.Assets.Request<Texture2D>("Resources/Extra_CreditUsers");
						string[] usernames = { "Jopojelly", "SheepishShepherd", "direwolf420", "riveren", "Orian", "Panini" };
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
					else if (Id == "PageTwo" && BossUISystem.Instance.RegisteredMods.Count > 0) {
						// Supported Mod Credits Page
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
				else if (selectedLogPage >= 0) {
					// Boss Pages
					BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[selectedLogPage];
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

						if (selectedBoss.ForceDowned) {
							isDefeated = $"''{Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Defeated", Main.worldName)}''";
						}

						Asset<Texture2D> texture = selectedBoss.IsDownedOrForced ? BossLogUI.checkMarkTexture : BossLogUI.xTexture;
						Vector2 defeatpos = new Vector2(firstHeadPos.X + (firstHeadPos.Width / 2), firstHeadPos.Y + firstHeadPos.Height - (texture.Height() / 2));
						spriteBatch.Draw(texture.Value, defeatpos, Color.White);

						// Hovering over the head icon will display the defeated text
						Rectangle hoverRect = new Rectangle(lastX, firstHeadPos.Y, totalWidth, firstHeadPos.Height);
						if (Main.MouseScreen.Between(hoverRect.TopLeft(), hoverRect.BottomRight())) {
							BossUISystem.Instance.UIHoverText = selectedBoss.IsDownedOrForced ? isDefeated : notDefeated;
							BossUISystem.Instance.UIHoverTextColor = selectedBoss.IsDownedOrForced ? Colors.RarityGreen : Colors.RarityRed;
						}

						bool enabledCopyButtons = BossChecklist.DebugConfig.AccessInternalNames && selectedBoss.modSource != "Unknown";
						Vector2 pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 5);
						string progression = BossChecklist.DebugConfig.ShowProgressionValue ? $"[{selectedBoss.progression}f] " : "";
						Utils.DrawBorderString(spriteBatch, progression + selectedBoss.DisplayName, pos, Color.Goldenrod);

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Vector2 vec2 = new Vector2(pageRect.X + 5, pos.Y);
							spriteBatch.Draw(clipboard, vec2, Color.Goldenrod);
						}

						pos = new Vector2(pageRect.X + 5 + (enabledCopyButtons ? 25 : 0), pageRect.Y + 30);
						Utils.DrawBorderString(spriteBatch, selectedBoss.SourceDisplayName, pos, new Color(150, 150, 255));

						if (enabledCopyButtons) {
							Texture2D clipboard = ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value;
							Rectangle clipRect = new Rectangle(pageRect.X + 5, pageRect.Y + 5, clipboard.Width, clipboard.Height);

							Color copied = (Platform.Get<IClipboard>().Value == selectedBoss.Key) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, clipRect, copied);

							// Hovering and rightclick will copy to clipboard
							if (Main.MouseScreen.Between(clipRect.TopLeft(), clipRect.BottomRight())) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopyKey");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.Key}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.Key;
								}
							}

							clipRect.Y += 25;
							copied = (Platform.Get<IClipboard>().Value == selectedBoss.modSource) ? Color.Gold : Color.White;
							spriteBatch.Draw(clipboard, clipRect, copied);

							if (Main.MouseScreen.Between(clipRect.TopLeft(), clipRect.BottomRight())) {
								string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.CopySource");
								BossUISystem.Instance.UIHoverText = $"{translated}:\n{selectedBoss.modSource}";
								if (Main.mouseLeft && Main.mouseLeftRelease) {
									Platform.Get<IClipboard>().Value = selectedBoss.modSource;
								}
							}
						}
					}
					else if (Id == "PageTwo" && selectedBoss.modSource != "Unknown") {
						if (BossLogUI.CategoryPageType == 0) {
							if (selectedBoss.type == EntryType.Boss) {
								// Boss Records Subpage
								Asset<Texture2D> construction = ModContent.Request<Texture2D>("Terraria/Images/UI/Creative/Journey_Toggle", AssetRequestMode.ImmediateLoad);
								Rectangle conRect = new Rectangle(pageRect.X + 30, pageRect.Y + 100, construction.Value.Width, construction.Value.Height);
								spriteBatch.Draw(construction.Value, conRect, Color.White);

								if (Main.MouseScreen.Between(conRect.TopLeft(), conRect.BottomRight())) {
									string noticeText;
									if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
										noticeText = $"World Records is currently {(BossChecklist.DebugConfig.DisableWorldRecords ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
											"\nThe World Records feature is still under construction." +
											"\nThis feature is known to not work and cause issues, so enable at your own risk." +
											$"\nWorld Records can be {(BossChecklist.DebugConfig.DisableWorldRecords ? "enabled" : "disabled")} under the Feature Testing configs.";
									}
									else {
										noticeText = $"Boss Records is currently {(BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
											"\nThis section of the Boss Log is still under construction." +
											"\nAny features or configs related to this page may not work or cause issues." +
											$"\nBoss Records can be {(BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE ? "enabled" : "disabled")} under the Feature Testing configs.";
									}
									BossUISystem.Instance.UIHoverText = noticeText;
									BossUISystem.Instance.UIHoverTextColor = Color.Gold;
								}

								foreach (BossInfo entry in BossChecklist.bossTracker.SortedBosses) {
									if (entry.type != EntryType.Event)
										continue;

									if (entry.npcIDs.Contains(selectedBoss.npcIDs[0])) {
										Texture2D icon = entry.headIconTextures[0].Value;
										Rectangle headPos = new Rectangle(pageRect.X + 15, pageRect.Y + 100, icon.Width, icon.Height);
										Color faded = entry.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
										spriteBatch.Draw(icon, headPos, faded);
										if (Main.MouseScreen.Between(headPos.TopLeft(), headPos.BottomRight())) {
											string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
											BossUISystem.Instance.UIHoverText = entry.DisplayName + "\n" + translated;
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												BossUISystem.Instance.BossLog.PageNum = entry.GetIndex; // Reset UI positions when changing the page
											}
										}
									}
								}

								// Beginning of record drawing
								Texture2D achievements = ModContent.Request<Texture2D>("Terraria/Images/UI/Achievements").Value;
								PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

								if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
									return;

								int recordIndex = BossChecklist.bossTracker.SortedBosses[selectedLogPage].GetRecordIndex;
								PersonalStats record = modPlayer.RecordsForWorld[recordIndex].stats;
								WorldStats wldRecord = WorldAssist.worldRecords[recordIndex].stats;

								string recordTitle = "";
								string recordValue = "";
								int[] achCoord = new int[] { -1, -1 };
								string NoRecord = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoRecord");

								for (int recordSlot = 0; recordSlot < 4; recordSlot++) { // 4 spots total
									if (recordSlot == 0) {
										recordValue = Main.LocalPlayer.name;
										// Which sub-category are we in?
										if (BossLogUI.RecordPageType == RecordCategory.PreviousAttempt) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PreviousRecord");
										}
										else if (BossLogUI.RecordPageType == RecordCategory.FirstRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.FirstRecord");
										}
										else if (BossLogUI.RecordPageType == RecordCategory.BestRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BestRecord");
										}
										else if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.WorldRecord");
											recordValue = Main.worldName;
										}
									}
									if (recordSlot == 1) {
										string Unchallenged = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Unchallenged");
										if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
											// World Global Kills & Deaths
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.KDRWorld");
											achCoord = wldRecord.totalKills >= wldRecord.totalDeaths ? new int[] { 4, 10 } : new int[] { 4, 8 };
											if (wldRecord.totalKills == 0 && wldRecord.totalDeaths == 0) {
												recordValue = Unchallenged;
											}
											else {
												string killTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Kills");
												string deathTerm = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Deaths");
												recordValue = $"{wldRecord.totalKills} {killTerm} / {wldRecord.totalDeaths} {deathTerm}";
											}
										}
										else if (BossLogUI.RecordPageType == RecordCategory.PreviousAttempt) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Attempt");
											achCoord = new int[] { 0, 9 };
											recordValue = record.kills == 0 ? Unchallenged : $"#{record.kills}";
										}
										else if (BossLogUI.RecordPageType == RecordCategory.FirstRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PlayTime");
											achCoord = new int[] { 7, 10 };
											recordValue = record.kills == 0 ? Unchallenged : $"{TicksToPlayTime(record.playTimeFirst)}";
										}
										else {
											// Kills & Deaths
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.KDR");
											achCoord = new int[] { 0, 3 };
											if (record.kills == 0 && record.deaths == 0) {
												recordValue = Unchallenged;
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

										if (BossLogUI.RecordPageType == RecordCategory.PreviousAttempt) {
											recordValue = record.durationPrev == -1 ? NoRecord : RecordTimeConversion(record.durationPrev);
										}
										else if (BossLogUI.RecordPageType == RecordCategory.FirstRecord) {
											recordValue = record.durationFirst == -1 ? NoRecord : RecordTimeConversion(record.durationFirst);
										}
										else if (BossLogUI.RecordPageType == RecordCategory.BestRecord) {
											recordValue = record.durationBest == -1 ? NoRecord : RecordTimeConversion(record.durationBest);
										}
										else if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.DurationWorld");
											achCoord = new int[] { 2, 12 };
											recordValue = wldRecord.durationWorld == -1 ? NoRecord : RecordTimeConversion(wldRecord.durationWorld);
										}
									}
									else if (recordSlot == 3) { // Hits Taken
										recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Dodge");
										achCoord = new int[] { 3, 0 };

										if (BossLogUI.RecordPageType == RecordCategory.PreviousAttempt) {
											recordValue = record.hitsTakenPrev == -1 ? NoRecord : record.hitsTakenPrev.ToString();
										}
										else if (BossLogUI.RecordPageType == RecordCategory.FirstRecord) {
											recordValue = record.hitsTakenFirst == -1 ? NoRecord : record.hitsTakenFirst.ToString();
										}
										else if (BossLogUI.RecordPageType == RecordCategory.BestRecord) {
											recordValue = record.hitsTakenBest == -1 ? NoRecord : record.hitsTakenBest.ToString();
										}
										else if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
											recordTitle = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.DodgeWorld");
											achCoord = new int[] { 0, 7 };
											recordValue = wldRecord.hitsTakenWorld < 0 ? NoRecord : wldRecord.hitsTakenWorld.ToString();
										}

										if (recordValue == "0") {
											recordValue = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.NoHit");
										}
									}

									if (achCoord[0] != -1) {
										Rectangle achPos = new Rectangle(pageRect.X + 15, pageRect.Y + 100 + (75 * recordSlot), 64, 64);
										Rectangle cutRect = new Rectangle(66 * achCoord[0], 66 * achCoord[1], 64, 64);

										Texture2D slot = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordSlot", AssetRequestMode.ImmediateLoad).Value;
										spriteBatch.Draw(slot, new Vector2(achPos.X, achPos.Y), new Color(175, 175, 125));
										spriteBatch.Draw(achievements, achPos, cutRect, Color.White);

										if (Main.MouseScreen.Between(achPos.TopLeft(), achPos.BottomRight())) {
											if (recordSlot == 1) {
												if (BossLogUI.RecordPageType == RecordCategory.PreviousAttempt) {
													BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.KDRDescription";
												}
												else if (BossLogUI.RecordPageType == RecordCategory.FirstRecord) {
													BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.FirstKDRDescription";
												}
												else if (BossLogUI.RecordPageType == RecordCategory.BestRecord) {
													BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.BestKDRDescription";
												}
												else if (BossLogUI.RecordPageType == RecordCategory.WorldRecord) {
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
										if (recordSlot == 2 || recordSlot == 3) {
											if (BossLogUI.RecordPageType == RecordCategory.WorldRecord && (recordSlot == 2 || recordSlot == 3)) {
												Texture2D trophy = Main.Assets.Request<Texture2D>($"Images/Item_{ItemID.GolfTrophyGold}", AssetRequestMode.ImmediateLoad).Value;
												Rectangle trophyPos = new Rectangle(achPos.X + slot.Width - trophy.Width / 2, achPos.Y + slot.Height / 2 - trophy.Height / 2, trophy.Width, trophy.Height);
												spriteBatch.Draw(trophy, trophyPos, Color.White);

												string message = "$Mods.BossChecklist.BossLog.HoverText.ClaimRecord";
												if (Main.MouseScreen.Between(trophyPos.TopLeft(), trophyPos.BottomRight())) {
													string holderText = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.RecordHolder");
													if (recordSlot == 2 && wldRecord.durationHolder.Count > 0) {
														message = $"{holderText}";
														foreach (string name in wldRecord.durationHolder) {
															message += $":\n{name}";
														}
													}
													else if (recordSlot == 3 && wldRecord.hitsTakenHolder.Count > 0) {
														message = $"{holderText}";
														foreach (string name in wldRecord.hitsTakenHolder) {
															message += $":\n{name}";
														}
													}
													BossUISystem.Instance.UIHoverText = message;
												}
											}
											else if (BossLogUI.RecordPageType == RecordCategory.BestRecord) {
												Texture2D trophy = Main.Assets.Request<Texture2D>($"Images/Item_{ItemID.GolfTrophySilver}", AssetRequestMode.ImmediateLoad).Value;
												Rectangle trophyPos = new Rectangle(achPos.X + slot.Width - trophy.Width / 2, achPos.Y + slot.Height / 2 - trophy.Height / 2, trophy.Width, trophy.Height);

												// Do not draw the trophy unless a Previous Best record is available
												if ((recordSlot == 2 && record.durationPrevBest != -1) || (recordSlot == 3 && record.hitsTakenPrevBest != -1)) {
													spriteBatch.Draw(trophy, trophyPos, Color.White);

													string message = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PreviousBest");
													if (Main.MouseScreen.Between(trophyPos.TopLeft(), trophyPos.BottomRight())) {
														if (recordSlot == 2)
															message += $":\n{RecordTimeConversion(record.durationPrevBest)}";
														else if (recordSlot == 3)
															message += $":\n{record.hitsTakenPrevBest}";

														BossUISystem.Instance.UIHoverText = message;
													}
												}
											}
										}

										// Draw compare numbers if selected
										if (BossLogUI.CompareState != RecordCategory.None && recordSlot != 0 && recordSlot != 1) {
											int initialRecordValue = -1;
											int compareRecordValue = -1;
											string comparisonValue = "";

											if (recordSlot == 2) {
												// Duration comparison
												initialRecordValue = GetRecordValue(BossLogUI.RecordPageType, recordSlot);
												compareRecordValue = GetRecordValue(BossLogUI.CompareState, recordSlot);
												comparisonValue = RecordTimeConversion(initialRecordValue - compareRecordValue);
											}
											else if (recordSlot == 3) {
												// Hits Taken comparison
												initialRecordValue = GetRecordValue(BossLogUI.RecordPageType, recordSlot);
												compareRecordValue = GetRecordValue(BossLogUI.CompareState, recordSlot);
												comparisonValue = (initialRecordValue - compareRecordValue).ToString();
											}

											if (comparisonValue != "" && initialRecordValue >= 0 && compareRecordValue >= 0) {
												bool badRecord = compareRecordValue < initialRecordValue;
												string sign = badRecord ? "+" : (initialRecordValue - compareRecordValue == 0) ? "-" : "";
												comparisonValue = sign + comparisonValue;

												float textScale = 0.65f;
												Vector2 textSize = FontAssets.MouseText.Value.MeasureString(comparisonValue) * textScale;
												Vector2 textPos = new Vector2(achPos.X + slot.Width - 50 - (textSize.X / 2), achPos.Y + slot.Height - textSize.Y);
												Color textColor = badRecord ? Color.LightSalmon : Color.LightGreen;
												Utils.DrawBorderString(spriteBatch, comparisonValue, textPos, textColor, textScale);

												string compareRecordString = recordSlot == 2 ? RecordTimeConversion(compareRecordValue) : compareRecordValue.ToString();
												textSize = FontAssets.MouseText.Value.MeasureString(compareRecordString) * textScale;
												textPos = new Vector2(achPos.X + slot.Width - 50 - (textSize.X / 2), achPos.Y + 6);
												Utils.DrawBorderString(spriteBatch, compareRecordString, textPos, Color.SkyBlue, textScale);
											}

										}
									}

									int offsetY = 110 + (recordSlot * 75);
									CalculatedStyle inner = GetInnerDimensions();

									Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(recordTitle);
									Color col = recordSlot == 0 ? Color.Goldenrod : Color.Gold;
									float scl = recordSlot == 0 ? 1.15f : 1f;
									Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.Length() * scl / 2) + 2, inner.Y + offsetY);
									Utils.DrawBorderString(spriteBatch, recordTitle, pos, col, scl);

									stringAdjust = FontAssets.MouseText.Value.MeasureString(recordValue);
									pos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.Length() / 2) + 2, inner.Y + offsetY + 25);
									Utils.DrawBorderString(spriteBatch, recordValue, pos, Color.White);
								}
							}
							else if (selectedBoss.type == EntryType.MiniBoss) {
								foreach (BossInfo entry in BossChecklist.bossTracker.SortedBosses) {
									if (entry.type != EntryType.Event)
										continue;

									if (entry.npcIDs.Contains(selectedBoss.npcIDs[0])) {
										Texture2D icon = entry.headIconTextures[0].Value;
										Rectangle headPos = new Rectangle(pageRect.X + 15, pageRect.Y + 100, icon.Width, icon.Height);
										Color faded = entry.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;
										spriteBatch.Draw(icon, headPos, faded);
										if (Main.MouseScreen.Between(headPos.TopLeft(), headPos.BottomRight())) {
											string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
											BossUISystem.Instance.UIHoverText = entry.DisplayName + "\n" + translated;
											if (Main.mouseLeft && Main.mouseLeftRelease) {
												BossUISystem.Instance.BossLog.PageNum = entry.GetIndex; // Reset UI positions when changing the page
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
									if (npcIndex == -1)
										continue;

									BossInfo addedNPC = bosses[npcIndex];
									Texture2D head = addedNPC.headIconTextures[0].Value;
									Rectangle headPos = new Rectangle(pageRect.X + headTextureOffsetX + 15, pageRect.Y + 100, head.Width, head.Height);
									Color headColor = addedNPC.IsDownedOrForced ? Color.White : masked ? Color.Black : BossLogUI.faded;

									spriteBatch.Draw(head, headPos, headColor);
									headTextureOffsetX += head.Width + 5;
									if (Main.MouseScreen.Between(headPos.TopLeft(), headPos.BottomRight())) {
										string translated = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ViewPage");
										BossUISystem.Instance.UIHoverText = addedNPC.DisplayName + "\n" + translated;
										if (Main.mouseLeft && Main.mouseLeftRelease) {
											BossUISystem.Instance.BossLog.PageNum = addedNPC.GetIndex; // Reset UI positions when changing the page
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
									if (offset == 0 && offsetY == 4)
										break; // For now, we stop drawing any banners that exceed the books limit (TODO: might have to reimplement as a UIList for scrolling purposes)

									if (npcID < NPCID.Count) {
										int init = Item.NPCtoBanner(npcID) + 21;
										if (init <= 21)
											continue;

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
										if (bannerID <= 0 || NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npcID)])
											continue;

										for (int j = 0; j < 3; j++) {
											Rectangle bannerPos = new Rectangle(pageRect.X + offset + 15, pageRect.Y + 100 + (16 * j) + offsetY, 16, 16);
											Rectangle rect = new Rectangle(init * 18, (jump * 18) + (j * 18), 16, 16);
											spriteBatch.Draw(banner.Value, bannerPos, rect, bannerColor);

											if (Main.MouseScreen.Between(bannerPos.TopLeft(), bannerPos.BottomRight())) {
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
										if (bannerItemID <= 0 || !ContentSamples.ItemsByType.TryGetValue(bannerItemID, out Item item))
											continue;

										if (item.createTile <= -1)
											continue;

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
											Rectangle bannerPos = new Rectangle(pageRect.X + offset + 15, pageRect.Y + 100 + heightOffSet + offsetY, 16, 16);
											Rectangle rect = new Rectangle(x, y + heightOffSetTexture, tileData.CoordinateWidth, tileData.CoordinateHeights[j]);
											Main.spriteBatch.Draw(banner.Value, bannerPos, rect, bannerColor);
											heightOffSet += heights[j];
											heightOffSetTexture += heights[j] + tileData.CoordinatePadding;

											if (Main.MouseScreen.Between(bannerPos.TopLeft(), bannerPos.BottomRight())) {
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
						else if (BossLogUI.CategoryPageType == CategoryPage.Spawn) {
							// Spawn Item Subpage
						}
						else if (BossLogUI.CategoryPageType == CategoryPage.Loot) {
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

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}

			internal static string RecordTimeConversion(int ticks) {
				const int TicksPerSecond = 60;
				const int TicksPerMinute = TicksPerSecond * 60;

				// If a negative value is given (can happen when comparing records), avoid giving both minutes and seconds values a negative sign
				string sign = ""; 
				if (ticks < 0) {
					ticks *= -1;
					sign = "-";
				}

				int minutes = ticks / TicksPerMinute; // Minutes will still show if 0
				float seconds = (float)(ticks - (float)(minutes * TicksPerMinute)) / TicksPerSecond;
				return $"{sign}{minutes}:{seconds.ToString("00.00")}";
			}

			internal static string TicksToPlayTime(long ticks) {
				int hours = (int)(ticks / TimeSpan.TicksPerHour);
				int minutes = (int)((ticks - (hours * TimeSpan.TicksPerHour)) / TimeSpan.TicksPerMinute);
				float seconds = (float)((ticks - (float)(hours * TimeSpan.TicksPerHour) - (float)(minutes * TimeSpan.TicksPerMinute)) / TimeSpan.TicksPerSecond);
				return $"{(hours > 0 ? hours + ":" : "")}{(hours > 0 ? minutes.ToString("00") : minutes)}:{seconds.ToString("00.00")}";
			}

			internal static int GetRecordValue(RecordCategory type, int id) {
				int recordIndex = BossChecklist.bossTracker.SortedBosses[BossUISystem.Instance.BossLog.PageNum].GetRecordIndex;
				if (id != 2 && id != 3)
					return -1;

				if (type == RecordCategory.WorldRecord) {
					WorldStats worldRecords = WorldAssist.worldRecords[recordIndex].stats;
					return id == 2 ? worldRecords.durationWorld : worldRecords.hitsTakenWorld;
				}
				else {
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					PersonalStats records = modPlayer.RecordsForWorld[recordIndex].stats;
					if (type == RecordCategory.PreviousAttempt)
						return id == 2 ? records.durationPrev : records.hitsTakenPrev;
					else if (type == RecordCategory.FirstRecord)
						return id == 2 ? records.durationFirst : records.hitsTakenFirst;
					else if (type == RecordCategory.BestRecord)
						return id == 2 ? records.durationBest : records.hitsTakenBest;
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
			readonly Asset<Texture2D> book;
			readonly BossLogUI log;
			public static bool isDrawn;

			public BookUI(Asset<Texture2D> texture) : base(texture) {
				book = texture;
				log = BossUISystem.Instance.BossLog;
			}

			internal static bool DrawTab(string Id) {
				int page = BossUISystem.Instance.BossLog.PageNum;
				bool MatchesCreditsTab = page == -2 && Id == "Credits_Tab";
				bool MatchesBossTab = page == BossLogUI.FindNext(EntryType.Boss) && Id == "Boss_Tab";
				bool MatchesMinibossTab = (page == BossLogUI.FindNext(EntryType.MiniBoss) || BossChecklist.BossLogConfig.OnlyShowBossContent) && Id == "Miniboss_Tab";
				bool MatchesEventTab = (page == BossLogUI.FindNext(EntryType.Event) || BossChecklist.BossLogConfig.OnlyShowBossContent) && Id == "Event_Tab";
				return !(MatchesCreditsTab || MatchesBossTab || MatchesMinibossTab || MatchesEventTab);
			}

			internal string DetermineHintText() {
				int selectedLogPage = log.PageNum;
				string hintText = "";
				if (selectedLogPage == -1) {
					hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.MarkEntry");
					hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.HideEntry");
					if (BossChecklist.DebugConfig.ResetForcedDowns) {
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearMarked");
					}
					if (BossChecklist.DebugConfig.ResetHiddenEntries) {
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearHidden");
					}
				}
				else if (selectedLogPage >= 0) {
					if (BossLogUI.CategoryPageType == CategoryPage.Record && BossChecklist.DebugConfig.ResetRecordsBool && BossLogUI.RecordPageType != RecordCategory.WorldRecord) {
						//hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearRecord"); // TODO: Make this function. Clear a singular record
						hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearAllRecords");
					}
					if (BossLogUI.CategoryPageType == CategoryPage.Loot && BossChecklist.DebugConfig.ResetLootItems) {
						hintText += Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.RemoveItem");
						hintText += "\n" + Language.GetTextValue("Mods.BossChecklist.BossLog.HintTexts.ClearItems");
					}
				}

				return hintText;
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				int selectedLogPage = log.PageNum;

				if (Id == "Info_Tab") {
					if (BossChecklist.BossLogConfig.AnyProgressionModeConfigUsed) {
						Rectangle rect = GetDimensions().ToRectangle();
						spriteBatch.Draw(book.Value, rect, Color.Firebrick);

						Texture2D texture = Main.Assets.Request<Texture2D>($"Images/Item_{ItemID.Blindfold}", AssetRequestMode.ImmediateLoad).Value;
						float scale = 0.85f;
						Vector2 pos = new Vector2(rect.X + rect.Width / 2 - texture.Width * scale / 2, rect.Y + rect.Height / 2 - texture.Height * scale / 2);
						spriteBatch.Draw(texture, pos, texture.Bounds, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = "$Mods.BossChecklist.BossLog.HoverText.ProgressionModeIsEnabled";
							BossUISystem.Instance.UIHoverTextColor = Color.Wheat;
						}
					}
					return;
				}
				else if (Id == "Shortcut_Tab") {
					if (!string.IsNullOrEmpty(DetermineHintText())) {
						Rectangle rect = GetDimensions().ToRectangle();
						spriteBatch.Draw(book.Value, rect, Color.Tan);

						Texture2D texture = BossLogUI.mouseTexture.Value;
						Vector2 pos = new Vector2(rect.X + rect.Width / 2 - texture.Width / 2, rect.Y + rect.Height / 2 - texture.Height / 2);
						spriteBatch.Draw(texture, pos, texture.Bounds, Color.White);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = DetermineHintText();
							BossUISystem.Instance.UIHoverTextColor = Color.Wheat;
						}
					}
					return;
				}
				else if (Id == "ToCFilter_Tab") {
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
					if (Id == "Boss_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.Boss) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "Miniboss_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.MiniBoss) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "Event_Tab" && (selectedLogPage >= BossLogUI.FindNext(EntryType.Event) || selectedLogPage == -2)) {
						effect = SpriteEffects.None;
					}
					else if (Id == "ToCFilter_Tab") {
						effect = SpriteEffects.None;
					}

					if (DrawTab(Id) && selectedLogPage != -3) {
						spriteBatch.Draw(book.Value, GetDimensions().ToRectangle(), new Rectangle(0, 0, book.Width(), book.Height()), Color.Tan, 0f, Vector2.Zero, effect, 0f);
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
					HideMouseOverInteractions();
				}

				if (Id.EndsWith("_Tab") && selectedLogPage != -3) {
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
					else if (Id == "ToCFilter_Tab" && selectedLogPage == -1) {
						texture = BossLogUI.filterTexture;
					}
					else if (Id == "ToCFilter_Tab" && selectedLogPage != -1) {
						texture = BossLogUI.tocTexture;
					}

					Rectangle inner = GetInnerDimensions().ToRectangle();
					int offsetX = inner.X < Main.screenWidth / 2 ? 2 : -2;
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (texture.Value.Width / 2) + offsetX, inner.Y + (inner.Height / 2) - (texture.Value.Height / 2));

					if (DrawTab(Id))
						spriteBatch.Draw(texture.Value, pos, Color.White);
					else
						return;

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
						else if (Id == "ToCFilter_Tab" && selectedLogPage == -1) {
							tabMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ToggleFilters");
						}
						else if (Id == "ToCFilter_Tab" && selectedLogPage != -1) {
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
						if (!BossChecklist.BossLogConfig.OnlyShowBossContent) {
							termLang = $"{termPrefix}{BossChecklist.BossLogConfig.FilterMiniBosses.ToLower().Replace(" ", "")}";
							termLang2 = $"{termPrefix}MiniBosses";
						}
						BossUISystem.Instance.UIHoverText = $"{Language.GetTextValue(termLang)} {Language.GetTextValue(termLang2)}";
					}
					if (Id == "F_2") {
						if (!BossChecklist.BossLogConfig.OnlyShowBossContent) {
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
			public int Index { get; init; }
			readonly float order = 0;
			readonly bool isNext;
			readonly bool downed;
			readonly string displayName;
			readonly bool allLoot;
			readonly bool allCollectibles;

			public TableOfContents(int index, string displayName, bool nextCheck, bool loot, bool collect, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
				this.Index = index;
				this.displayName = displayName;
				this.isNext = nextCheck;
				this.order = BossChecklist.bossTracker.SortedBosses[Index].progression;
				this.downed = BossChecklist.bossTracker.SortedBosses[Index].IsDownedOrForced;
				this.allLoot = loot;
				this.allCollectibles = collect;
			}

			public override void MouseOver(UIMouseEvent evt) {
				if (BossChecklist.DebugConfig.ShowProgressionValue) {
					SetText($"[{order}f] {displayName}");
				}
				base.MouseOver(evt);
			}

			public override void MouseOut(UIMouseEvent evt) {
				SetText(displayName);
				base.MouseOut(evt);
			}

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Vector2 pos = new Vector2(inner.X - 20, inner.Y - 5);
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[Index];

				if (order != -1) {
					// Use the appropriate text color for conditions
					if ((!selectedBoss.available() && !downed) || selectedBoss.hidden) {
						TextColor = Color.DimGray; // Hidden or Unavailable entry text color takes priority over all other text color alterations
					}
					else if (BossChecklist.BossLogConfig.ColoredBossText) {
						int recordIndex = BossChecklist.bossTracker.SortedBosses[Index].GetRecordIndex;
						if (recordIndex != -1 && modPlayer.hasNewRecord[recordIndex]) {
							TextColor = Main.DiscoColor;
						}
						else if (IsMouseHovering) {
							TextColor = TextColor = Color.SkyBlue;
						}
						else if (isNext) {
							TextColor = new Color(248, 235, 91);
						}
						else {
							TextColor = downed ? Colors.RarityGreen : Colors.RarityRed;
						}
					}
					else {
						TextColor = IsMouseHovering ? Color.Silver : Color.White; // Disabled colored text
					}

					if (IsMouseHovering) {
						BossLogPanel.headNum = Index;
					}
				}

				// base drawing comes after colors so they do not flicker when updating check list
				base.Draw(spriteBatch);

				Rectangle parent = this.Parent.GetInnerDimensions().ToRectangle();
				int hardModeOffset = selectedBoss.progression > BossTracker.WallOfFlesh ? 10 : 0;
				string looted = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllLoot");
				string collected = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllCollectibles");
				Texture2D texture = null;
				string hoverText = "";

				if (allLoot && allCollectibles) {
					texture = BossLogUI.goldChestTexture.Value;
					hoverText = $"{looted}\n{collected}";
				}
				else if (allLoot || allCollectibles) {
					texture = BossLogUI.chestTexture.Value;
					if (allLoot) {
						looted = Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.AllDropLoot");
					}
					hoverText = allLoot ? Language.GetTextValue(looted) : Language.GetTextValue(collected);
				}

				if (texture != null) {
					Rectangle chestPos = new Rectangle(parent.X + parent.Width - texture.Width - hardModeOffset, inner.Y - 2, texture.Width, texture.Height);
					spriteBatch.Draw(texture, chestPos, Color.White);
					if (Main.MouseScreen.Between(chestPos.TopLeft(), chestPos.BottomRight())) {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
				}

				if (order != -1f) {
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

							Rectangle strikePos = new Rectangle(inner.X - w, offsetY, w, h);
							Rectangle strikeSrc = new Rectangle(0, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);

							strikePos = new Rectangle(inner.X, offsetY, (int)stringAdjust.X, h);
							strikeSrc = new Rectangle(w, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, IsMouseHovering ? Color.Transparent : Color.White);

							strikePos = new Rectangle(inner.X + (int)stringAdjust.X, offsetY, w, h);
							strikeSrc = new Rectangle(w * 2, 0, w, h);
							spriteBatch.Draw(strike.Value, strikePos, strikeSrc, hoverColor);
						}
					}
					else {
						checkGrid = checkType == "✓ and  X" ? BossLogUI.xTexture : BossLogUI.checkboxTexture;
						if (isNext && BossChecklist.BossLogConfig.DrawNextMark) {
							checkGrid = checkType == "Strike-through" ? BossLogUI.strikeNTexture : BossLogUI.circleTexture;
						}
					}

					if ((checkType != "Strike-through" || checkGrid == BossLogUI.strikeNTexture) && !selectedBoss.hidden) {
						if (checkGrid != BossLogUI.strikeNTexture) {
							spriteBatch.Draw(BossLogUI.checkboxTexture.Value, pos, Color.White);
						}
						spriteBatch.Draw(checkGrid.Value, pos, Color.White);
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
			internal Dictionary<string, int[]> modAllEntries;
			internal bool ModSourceMode = false;

			public ProgressBar() {
				downedEntries = totalEntries = new int[] { 0, 0, 0 };
			}

			public override void Click(UIMouseEvent evt) => ModSourceMode = !ModSourceMode;

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				int w = fullBar.Value.Width;
				int h = fullBar.Value.Height;
				int barWidth = inner.Width - 8;

				// Beginning of bar
				Rectangle src = new Rectangle(0, 0, w / 3, h);
				Rectangle pos = new Rectangle(inner.X, inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// Center of bar
				src = new Rectangle(w / 3, 0, w / 3, h);
				pos = new Rectangle(inner.X + 6, inner.Y, barWidth, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				// End of bar
				src = new Rectangle(2 * (w / 3), 0, w / 3, h);
				pos = new Rectangle(inner.X + inner.Width - 4, inner.Y, w / 3, h);
				spriteBatch.Draw(fullBar.Value, pos, src, Color.White);

				BossLogConfiguration configs = BossChecklist.BossLogConfig;
				int allDownedEntries = downedEntries[0];
				int allAccountedEntries = totalEntries[0];

				// If OnlyBosses config is disabled, we'll count the MiniBosses and Events to the total count as well
				if (!configs.OnlyShowBossContent) {
					allDownedEntries += downedEntries[1] + downedEntries[2];
					allAccountedEntries += totalEntries[1] + totalEntries[2];
				}

				float percentage = allAccountedEntries == 0 ? 1f : (float)allDownedEntries / (float)allAccountedEntries;
				int meterWidth = (int)(barWidth * percentage); 

				Rectangle meterPos = new Rectangle(inner.X + 4, inner.Y + 4, meterWidth + 2, (int)inner.Height - 8);
				Color bookColor = BossChecklist.BossLogConfig.BossLogColor;
				bookColor.A = 180;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, Color.White);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterPos, bookColor);

				string percentDisplay = $"{((float)percentage * 100).ToString("#0.0")}%";
				float scale = 0.85f;
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(percentDisplay) * scale;
				Vector2 percentPos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.X / 2), inner.Y - stringAdjust.Y);
				Utils.DrawBorderString(spriteBatch, percentDisplay, percentPos, Colors.RarityAmber, scale);

				if (IsMouseHovering) {
					if (ModSourceMode) {
						int[] value = modAllEntries["Terraria"];
						float entryPercentage = (float)((float)value[0] / (float)value[1]) * 100;
						BossUISystem.Instance.UIHoverText = $"Terraria: {value[0]}/{value[1]} ({entryPercentage.ToString("#0.0")}%)";
						foreach (KeyValuePair<string, int[]> entry in modAllEntries) {
							if (entry.Key == "Unknown" || entry.Key == "Terraria")
								continue;

							entryPercentage = (float)((float)entry.Value[0] / (float)entry.Value[1]) * 100;
							BossUISystem.Instance.UIHoverText += $"\n{entry.Key}: {entry.Value[0]}/{entry.Value[1]} ({entryPercentage.ToString("#0.0")}%)";
						}
						if (modAllEntries.ContainsKey("Unknown")) {
							value = modAllEntries["Unknown"];
							entryPercentage = (float)((float)value[0] / (float)value[1]) * 100;
							BossUISystem.Instance.UIHoverText = $"\nUnknown: {value[0]}/{value[1]} ({entryPercentage.ToString("#0.0")}%)";
						}
						BossUISystem.Instance.UIHoverText += $"\n[{Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.EntryCompletion")}]";
					}
					else {
						string total = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Total");
						string bosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Bosses");
						string miniBosses = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.MiniBosses");
						string events = Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.Events");
						BossUISystem.Instance.UIHoverText = $"{total}: {allDownedEntries}/{allAccountedEntries}";
						if (!configs.OnlyShowBossContent) {
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
						BossUISystem.Instance.UIHoverText += $"\n[{Language.GetTextValue("Mods.BossChecklist.BossLog.HoverText.ModCompletion")}]";
					}
				}
			}
		}

		internal class FittedTextPanel : UITextPanel<string>
		{
			readonly string text;
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

				ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft(),
					Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out _, hitbox.Width - (7 * 2), false);
			}
		}

		internal class SubpageButton : UIPanel
		{
			string buttonString;
			readonly int AltButtonNum;

			public SubpageButton(string type) {
				buttonString = type;
				AltButtonNum = -1;
			}

			public SubpageButton(int num) {
				buttonString = "";
				AltButtonNum = num;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				int selectedLogPage = BossUISystem.Instance.BossLog.PageNum;
				if (selectedLogPage < 0)
					return;

				if (buttonString == "Mods.BossChecklist.BossLog.DrawnText.Records" || buttonString == "LegacyInterface.101") {
					EntryType BossType = BossChecklist.bossTracker.SortedBosses[selectedLogPage].type;
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

				if (AltButtonNum >= 0) {
					if (BossLogUI.CategoryPageType == CategoryPage.Record) {
						string[] hoverTexts = {
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.PreviousRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.BestRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.FirstRecord"),
							Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.WorldRecord")
						};

						int selected = 0;

						if (AltButtonNum == (int)BossLogUI.RecordPageType) {
							selected = 1;
						}
						else if (AltButtonNum == (int)BossLogUI.CompareState) {
							selected = 2;
						}

						Rectangle inner = GetInnerDimensions().ToRectangle();
						Texture2D texture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordTabs", AssetRequestMode.ImmediateLoad).Value;
						Rectangle exclamPos = new Rectangle(inner.X + (inner.Width / 2) - 18, inner.Y + (inner.Height / 2) - 18, 36, 36);
						Rectangle exclamCut = new Rectangle(36 * AltButtonNum, 36 * selected, 36, 36);
						spriteBatch.Draw(texture, exclamPos, exclamCut, Color.White);

						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = hoverTexts[AltButtonNum];
							BossUISystem.Instance.UIHoverTextColor = Colors.RarityGreen;
						}
					}
					else if (BossLogUI.CategoryPageType == CategoryPage.Spawn) {
						// NO CURRENT ALTPAGE, BUTTON NOT NEEDED
					}
					else if (BossLogUI.CategoryPageType == CategoryPage.Loot) {
						// NO CURRENT ALTPAGE, BUTTON NOT NEEDED
					}
				}
			}
		}
	}
}
