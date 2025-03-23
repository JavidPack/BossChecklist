using BossChecklist.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
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

namespace BossChecklist.UIElements
{
	internal static class BossLogUIElements {
		/// <summary>
		/// Hides certain mouse over interactions from appearing such as tile icons or NPC names.
		/// </summary>
		static void HideMouseOverInteractions() {
			Main.player[Main.myPlayer].mouseInterface = true;
			Main.mouseText = true;
			Main.LocalPlayer.cursorItemIconEnabled = false;
			Main.LocalPlayer.cursorItemIconID = -1;
			Main.ItemIconCacheUpdate(0);
		}

		/// <summary>
		/// Calculates the desired text scale needed to fit text in a UI element.
		/// </summary>
		/// <param name="textWidth">The width of the text value. Usually obtained using FontAssets.MouseText.Value.MeasureString</param>
		/// <param name="maxWidth">The maximum width the text is allowed to take up. Usually the element's width with some padding.</param>
		/// <param name="defaultScale">If the text does not need to be scaled down, this will serve as the default desired scale.</param>
		static float AutoScaleText(float textWidth, float maxWidth, float defaultScale = 1f) => textWidth * defaultScale > maxWidth ? maxWidth / textWidth : defaultScale;

		/// <summary>
		/// All Log related UIElements should hide mouse over interactions and lock the vanilla scroll wheel
		/// </summary>
		internal class LogUIElement : UIElement {
			internal BossLogUI LogUI => BossUISystem.Instance.BossLog;
			public string hoverText;
			internal Color hoverTextColor = Color.White;
			internal Texture2D asset = null;
			internal Color assetColor = Color.White;

			public LogUIElement() { }

			public LogUIElement(Texture2D asset) {
				this.asset = asset;
				this.Width.Pixels = asset.Width;
				this.Height.Pixels = asset.Height;
			}

			public override void Update(GameTime gameTime) {
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
					PlayerInput.LockVanillaMouseScroll("BossChecklist/BossLogUIElement");

				base.Update(gameTime);
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
					HideMouseOverInteractions();

				if (asset is not null)
					spriteBatch.Draw(asset, GetInnerDimensions().ToRectangle(), assetColor);

				base.Draw(spriteBatch);

				if (ContainsPoint(Main.MouseScreen) && !string.IsNullOrEmpty(hoverText)) {
					BossUISystem.Instance.UIHoverText = hoverText;
					BossUISystem.Instance.UIHoverTextColor = hoverTextColor;
				}
			}
		}

		internal class OpenLogButton : UIImageButton {
			internal Asset<Texture2D> texture;
			private Vector2 offset;
			internal bool dragging;
			internal Color? borderColor;

			PlayerAssist GetModPlayer => Main.LocalPlayer.GetModPlayer<PlayerAssist>();

			public OpenLogButton(Asset<Texture2D> texture) : base(texture) {
				Width.Pixels = texture.Value.Width;
				Height.Pixels = texture.Value.Height;

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
				borderColor = null;
				// Determine a border color for the button
				if (IsMouseHovering || dragging) {
					borderColor = Color.Goldenrod; // If hovering over or dragging the button, the book will be highlighted in a gold border
				}
				else if (!BossChecklist.FeatureConfig.RecordTrackingEnabled || !BossChecklist.FeatureConfig.AllowNewRecords) { // configs can be checked as it is checked from a client
					borderColor = Color.Firebrick; // If Records are disabled in any way, the book will be highlighted with a red border
				}
				else if (!GetModPlayer.hasOpenedTheBossLog || (BossChecklist.FeatureConfig.NewRecordLogGlow && GetModPlayer.hasNewRecord.Contains(true))) {
					borderColor = Main.DiscoColor;
				}

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
				if ((ContainsPoint(Main.MouseScreen) || dragging) && !PlayerInput.IgnoreMouseInterface)
					HideMouseOverInteractions();

				base.DrawSelf(spriteBatch);

				Rectangle inner = GetInnerDimensions().ToRectangle();

				// When hovering over the button, draw a 'Boss Log' text over the button
				// text shouldn't appear if dragging the element
				if (IsMouseHovering && !dragging) {
					string hoverText = Language.GetTextValue($"{BossLogUI.LangLog}.Common.BossLog");
					Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(hoverText);
					Vector2 pos = new Vector2(inner.X - (stringAdjust.X / 3), inner.Y - 24);
					spriteBatch.DrawString(FontAssets.MouseText.Value, hoverText, pos, Color.White);
				}

				Color coverColor = BossChecklist.BossLogConfig.BossLogColor;
				if (!IsMouseHovering && !dragging)
					coverColor = new Color(coverColor.R, coverColor.G, coverColor.B, 128);

				spriteBatch.Draw(!IsMouseHovering && !dragging ? BossLogResources.Button_Faded.Value : BossLogResources.Button_Color.Value, inner, coverColor);

				// UIImageButtons are normally faded, so if dragging and not draw the button fully opaque
				// This is most likely to occur when the mouse travels off screen while dragging
				if (dragging)
					spriteBatch.Draw(texture.Value, inner, Color.White);

				if (borderColor.HasValue)
					spriteBatch.Draw(BossLogResources.Button_Border.Value, inner, borderColor.Value); // Draw a colored border if one was set
			}
		}

		internal class NavigationalButton : LogUIElement {
			public string Id { get; init; } = "";
			public int? Anchor { get; init; } = null;
			public SubCategory? Record_Anchor { get; init; }

			internal Asset<Texture2D> texture;
			internal Color iconColor;
			internal bool hoverButton;

			public NavigationalButton(Asset<Texture2D> texture, bool hoverButton, Color? color = null) {
				Width.Pixels = texture.Value.Width;
				Height.Pixels = texture.Value.Height;

				this.texture = texture;
				this.iconColor = hoverButton || color == null ? Color.White : color.Value;
				this.hoverButton = hoverButton;
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);
				if (Anchor.HasValue)
					LogUI.PendingPageNum = Anchor.Value;

				if (Record_Anchor.HasValue) {
					LogUI.RecordSubCategory = Record_Anchor.Value;
					if (Record_Anchor.Value == LogUI.CompareState)
						LogUI.CompareState = SubCategory.None;
					LogUI.RefreshPageContent();
				}

				if (Id == "CopyKey") {
					string bossKey = LogUI.GetLogEntryInfo.Key;
					if (Platform.Get<IClipboard>().Value != bossKey) {
						Platform.Get<IClipboard>().Value = bossKey;
						SoundEngine.PlaySound(SoundID.Unlock);
					}
				}
			}

			public override void RightClick(UIMouseEvent evt) {
				base.RightClick(evt);
				if (Record_Anchor.HasValue && Record_Anchor.Value != LogUI.RecordSubCategory) {
					LogUI.CompareState = LogUI.CompareState == Record_Anchor.Value ? SubCategory.None : Record_Anchor.Value;
					LogUI.RefreshPageContent();
				}
			}

			public override void MouseOver(UIMouseEvent evt) {
				if (hoverButton)
					SoundEngine.PlaySound(SoundID.MenuTick);
			}

			private Color HoverColor => ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface ? Color.White : BossLogUI.faded;

			public override void Draw(SpriteBatch spriteBatch) {
				if (Record_Anchor.HasValue) {
					spriteBatch.Draw(texture.Value, GetInnerDimensions().ToRectangle(), LogUI.RecordSubCategory == Record_Anchor.Value ? iconColor : HoverColor);
				}
				else {
					spriteBatch.Draw(texture.Value, GetInnerDimensions().ToRectangle(), hoverButton ? HoverColor : iconColor);
				}
				base.Draw(spriteBatch);
			}
		}

		internal class IndicatorPanel : LogUIElement {
			public string Id { get; init; } = "";
			private Asset<Texture2D> section;
			private Asset<Texture2D> end;
			private Asset<Texture2D> back;

			public IndicatorPanel(int iconCount) {
				Width.Pixels = (10 * 2) + ((18 + 2) * iconCount - 2); // indicator end panel width = 10; indicator back width = 18;
				Height.Pixels = 26; // indicator panel height = 26;
			}

			public override void RightClick(UIMouseEvent evt) {
				if (Id != "Interactions")
					return;

				if (!BossChecklist.BossLogConfig.Debug.EnabledResetOptions || !BossLogUI.AltKeyIsDown)
					return; // This button only covers the reset data options

				if (LogUI.PageNum == BossLogUI.Page_TableOfContents) {
					if (LogUI.HiddenEntriesMode) {
						// Set all entries to NOT hidden
						if (WorldAssist.HiddenEntries.Count > 0) {
							WorldAssist.HiddenEntries.Clear();
							BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
							Networking.RequestHiddenEntryUpdate();
							LogUI.RefreshPageContent();
						}
					}
					else {
						// Unmark all entries
						if (WorldAssist.MarkedEntries.Count > 0) {
							WorldAssist.MarkedEntries.Clear();
							Networking.RequestMarkedEntryUpdate();
							LogUI.RefreshPageContent();
						}
					}
				}
				else if (LogUI.PageNum >= 0 && BossChecklist.BossLogConfig.Debug.EnabledResetOptions) {
					if (LogUI.SelectedSubPage == SubPage.Records && LogUI.GetLogEntryInfo.type == EntryType.Boss) {
						if (LogUI.GetPlayerRecords is not null) {
							LogUI.GetPlayerRecords.ResetStats(LogUI.RecordSubCategory);
							LogUI.RefreshPageContent();
						}
					}
					else if (LogUI.SelectedSubPage == SubPage.LootAndCollectibles) {
						// Remove all items from the obtained list
						foreach (int item in LogUI.GetLogEntryInfo.lootItemTypes) {
							LogUI.GetModPlayer.BossItemsCollected.RemoveAll(x => x.Type == item);
						}
						foreach (int item in LogUI.GetLogEntryInfo.collectibles.Keys.ToList()) {
							LogUI.GetModPlayer.BossItemsCollected.RemoveAll(x => x.Type == item);
						}
					}
				}
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (LogUI.PageNum == BossLogUI.Page_Prompt)
					return;

				if (Id == "Interactions" && string.IsNullOrEmpty(hoverText))
					return;

				Rectangle inner = GetInnerDimensions().ToRectangle();

				end ??= BossLogResources.RequestResource("LogUI_IndicatorEnd", true);
				Rectangle endPanel = new Rectangle(inner.Right - end.Value.Width, inner.Y, end.Value.Width, end.Value.Height);
				Rectangle centerPanel = new Rectangle(inner.X + end.Value.Width, inner.Y, inner.Width - (end.Value.Width * 2), inner.Height);

				section ??= BossLogResources.RequestResource("LogUI_IndicatorSection", true);
				spriteBatch.Draw(end.Value, inner.TopLeft(), Color.White);
				spriteBatch.Draw(section.Value, centerPanel, Color.White);
				spriteBatch.Draw(end.Value, endPanel, end.Value.Bounds, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);

				if (Id == "Configurations") {
					back ??= BossLogResources.RequestResource("Indicator_Back", true);
					for (int i = 0; i < Children.Count(); i++) {
						spriteBatch.Draw(back.Value, new Vector2(centerPanel.X + (back.Value.Width + 2) * i, inner.Y + 6), Color.White);
					}
				}
				
				base.Draw(spriteBatch);
			}
		}

		internal class IndicatorIcon : LogUIElement {
			public string Id { get; init; }
			public Color Color { get; set; } = Color.White;
			internal Asset<Texture2D> texture;

			public IndicatorIcon(Asset<Texture2D> texture) {
				Width.Pixels = texture.Value.Width;
				Height.Pixels = texture.Value.Height;
				this.texture = texture;
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);

				if (Id == "Progression") {
					BossChecklist.BossLogConfig.ProgressiveChecklist = !BossChecklist.BossLogConfig.ProgressiveChecklist;
				}
				else if (Id == "Manual") {
					BossChecklist.BossLogConfig.AutomaticChecklist = !BossChecklist.BossLogConfig.AutomaticChecklist;
				}
				else if (Id == "OnlyBosses") {
					BossChecklist.BossLogConfig.OnlyShowBossContent = !BossChecklist.BossLogConfig.OnlyShowBossContent;
				}
				BossLogUI.PendingConfigChange = true;
				BossChecklist.BossLogConfig.UpdateIndicators();
				LogUI.RefreshPageContent();
			}

			public override void Draw(SpriteBatch spriteBatch) {
				spriteBatch.Draw(texture.Value, GetInnerDimensions().ToRectangle(), Color);
				base.Draw(spriteBatch);
			}
		}

		internal class FilterIcon : LogUIElement {
			public string Id { get; init; }
			internal Asset<Texture2D> icon;
			public Asset<Texture2D> check;

			public FilterIcon(Asset<Texture2D> icon) {
				Width.Pixels = icon.Value.Width;
				Height.Pixels = icon.Value.Height;
				this.icon = icon;
			}

			private string GetConfigValue() {
				return Id switch {
					"Boss" => BossChecklist.BossLogConfig.FilterBosses.Replace(" ", ""),
					"MiniBoss" => BossChecklist.BossLogConfig.FilterMiniBosses.Replace(" ", ""),
					"Event" => BossChecklist.BossLogConfig.FilterEvents.Replace(" ", ""),
					_ => ""
				};
			}

			public void UpdateFilterIcon() {
				check = Id switch {
					"Boss" => BossLogResources.FilterToIcon[BossChecklist.BossLogConfig.FilterBosses],
					"MiniBoss" => BossChecklist.BossLogConfig.OnlyShowBossContent ? BossLogResources.Check_X : BossLogResources.FilterToIcon[BossChecklist.BossLogConfig.FilterMiniBosses],
					"Event" => BossChecklist.BossLogConfig.OnlyShowBossContent ? BossLogResources.Check_X : BossLogResources.FilterToIcon[BossChecklist.BossLogConfig.FilterEvents],
					_ => null
				};

				// update the hover tooltips
				string LangFilter = "Mods.BossChecklist.Log.TableOfContents.Filter";
				string LangCommon = "Mods.BossChecklist.Log.Common";

				if (BossChecklist.BossLogConfig.OnlyShowBossContent && (Id == "MiniBoss" || Id == "Event"))
					hoverText = $"{LangFilter}.Disabled";

				hoverText = Id switch {
					"Boss" or "MiniBoss" or "Event" => Language.GetTextValue($"{LangFilter}.{GetConfigValue()}", Language.GetTextValue($"{LangCommon}.{Id}Plural")),
					"Hidden" => $"{LangFilter}.ToggleHidden" + (LogUI.HiddenEntriesMode ? "Close" : "Open"),
					_ => ""
				};
			}

			private string CycleFilterState(string value, bool boss = false) {
				return value switch {
					BossLogConfiguration.Option_Show => BossLogConfiguration.Option_HideWhenCompleted,
					BossLogConfiguration.Option_HideWhenCompleted => boss ? BossLogConfiguration.Option_Show : BossLogConfiguration.Option_Hide,
					BossLogConfiguration.Option_Hide => BossLogConfiguration.Option_Show,
					_ => ""
				};
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);

				if (Id is null)
					return; // don't do anything if the panel is clicked

				string ConfigHoverText = "";
				if (Id == "Boss") {
					BossChecklist.BossLogConfig.FilterBosses = ConfigHoverText = CycleFilterState(BossChecklist.BossLogConfig.FilterBosses, true);
				}
				else if (Id == "MiniBoss" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
					BossChecklist.BossLogConfig.FilterMiniBosses = ConfigHoverText = CycleFilterState(BossChecklist.BossLogConfig.FilterMiniBosses);
				}
				else if (Id == "Event" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
					BossChecklist.BossLogConfig.FilterEvents = ConfigHoverText = CycleFilterState(BossChecklist.BossLogConfig.FilterEvents);
				}
				else if (Id == "Hidden") {
					LogUI.HiddenEntriesMode = !LogUI.HiddenEntriesMode;
				}

				if (!string.IsNullOrEmpty(ConfigHoverText))
					BossLogUI.PendingConfigChange = true;

				UpdateFilterIcon();
				LogUI.RefreshPageContent(); // updates checklist based on filters modified
			}

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Color color = Color.White;
				float scale = 1f;
				Vector2 pos = inner.TopLeft();
				if (Id == "Hidden") {
					color = LogUI.HiddenEntriesMode ? Color.White : Color.DimGray;
					scale = LogUI.HiddenEntriesMode ? Main.cursorScale : 1f;
					pos = inner.Center() - new Vector2(icon.Value.Width / 2 * scale, icon.Value.Height / 2 * scale);
				}
				spriteBatch.Draw(icon.Value, pos, icon.Value.Bounds, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
				if (check is not null)
					spriteBatch.Draw(check.Value, new Vector2(inner.X + inner.Width - 10, inner.Y + inner.Height - 15), Color.White);

				base.Draw(spriteBatch);
			}
		}

		internal class SubPageButton : UIImage {
			readonly string buttonText;
			readonly SubPage subPageType;
			public bool isLocked = false;

			private Asset<Texture2D> selectionBorder;
			private Asset<Texture2D> locked;

			public SubPageButton(Asset<Texture2D> texture, SubPage type) : base(texture) {
				buttonText = Language.GetTextValue($"{BossLogUI.LangLog}.Tabs.{type}");
				subPageType = type;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				base.DrawSelf(spriteBatch);

				Rectangle inner = GetInnerDimensions().ToRectangle();
				if (subPageType == BossUISystem.Instance.BossLog.SelectedSubPage) {
					selectionBorder ??= BossLogResources.RequestResource("Nav_SubPage_Border");
					spriteBatch.Draw(selectionBorder.Value, inner, Color.White); // draw a border around the selected subpage
				}

				bool useKillCountText = subPageType == SubPage.Records && BossUISystem.Instance.BossLog.GetLogEntryInfo.type != EntryType.Boss; // Event entries should display 'Kill Count' instead of 'Records'
				string translated = Language.GetTextValue(useKillCountText ? "LegacyInterface.101" : buttonText);
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(translated);
				float scale = AutoScaleText(stringAdjust.X, this.Width.Pixels - 20f, 0.9f); // translated text value may exceed button size
				Vector2 pos = new Vector2(inner.X + (int)((Width.Pixels - stringAdjust.X * scale) / 2), inner.Y + 5);

				spriteBatch.DrawString(FontAssets.MouseText.Value, translated, pos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

				if (isLocked && BossChecklist.BossLogConfig.ProgressiveChecklist) {
					locked ??= BossLogResources.RequestVanillaTexture("UI/Workshop/PublicityPrivate", true);
					pos = new Vector2(inner.X + (inner.Width / 2 - locked.Value.Width / 2), inner.Y + inner.Height / 2 - locked.Value.Height / 2);
					spriteBatch.Draw(locked.Value, pos, Color.White);
				}
			}
		}

		internal class TreasureBag : LogUIElement {
			internal int itemType = 0;
			private readonly Asset<Texture2D> bagTexture;

			public TreasureBag(int bag) {
				itemType = bag > 0 ? bag : 0;
				bagTexture = bag > 0 ? BossLogResources.RequestItemTexture(bag) : BossLogResources.RequestResource("Extra_TreasureBag", true);

				Width.Set(bagTexture.Value.Height, 0f);
				Height.Set(bagTexture.Value.Height, 0f);
			}

			public override void RightClick(UIMouseEvent evt) {
				if (!BossChecklist.BossLogConfig.Debug.EnabledResetOptions || LogUI.SelectedSubPage != SubPage.LootAndCollectibles)
					return; // do not do anything if the loot page isn't the active

				if (!BossLogUI.AltKeyIsDown)
					return; // player must be holding alt to remove any items

				LogUI.GetLogEntryInfo.lootItemTypes.ForEach(item => LogUI.GetModPlayer.BossItemsCollected.RemoveAll(x => x.Type == item));
				LogUI.RefreshPageContent();
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (itemType != 0) {
					DrawAnimation drawAnim = Main.itemAnimations[itemType];
					Rectangle sourceRect = drawAnim != null ? drawAnim.GetFrame(bagTexture.Value) : bagTexture.Value.Bounds;
					spriteBatch.Draw(bagTexture.Value, GetInnerDimensions().ToRectangle(), sourceRect, Color.White);
				}
				else {
					spriteBatch.Draw(bagTexture.Value, GetInnerDimensions().ToRectangle(), Color.White);
				}
			}
		}

		internal class LogItemSlot : LogUIElement {
			public string Id { get; init; } = "";
			internal Item item;
			private readonly int context;
			private readonly float scale;
			internal bool hasItem;
			internal bool itemResearched;

			private Asset<Texture2D> highlight;
			private Asset<Texture2D> checkmark;
			private Asset<Texture2D> expertModeIcon;
			private Asset<Texture2D> masterModeIcon;
			private Asset<Texture2D> otherWorldIcon;

			public const string SpawnItemCraftingSlot = "SpawnItemCraftingSlot";

			public LogItemSlot(Item item, int context = ItemSlot.Context.TrashItem, float scale = 1f) {
				this.context = context;
				this.scale = scale;
				this.item = item;

				Width.Set(TextureAssets.InventoryBack9.Width() * scale, 0f);
				Height.Set(TextureAssets.InventoryBack9.Height() * scale, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				float oldScale = Main.inventoryScale;
				Main.inventoryScale = scale;

				if (item.IsAir && string.IsNullOrEmpty(hoverText))
					return; // blank item slots should not be drawn

				if (Id == SpawnItemCraftingSlot) {
					ItemSlot.Draw(spriteBatch, ref item, context, inner.TopLeft());
					Main.inventoryScale = oldScale;

					// Draws the evil altars in the designated slots if needed
					if (!string.IsNullOrEmpty(hoverText) && hoverText.EndsWith("Altar")) {
						Main.instance.LoadTiles(TileID.DemonAltar);
						int offsetX = 0;
						int offsetY = 0;
						int offsetSrc = WorldGen.crimson ? 3 : 0;
						for (int i = 0; i < 6; i++) {
							float scale = 0.64f;
							Rectangle src = new Rectangle((offsetX + offsetSrc) * 18, offsetY * 18, 16, 16 + (offsetY * 2));
							// Determine the position of EACH tile of the selected altar (multi-tile, 3x2)
							float posX = inner.X + (inner.Width / 2) - (src.Width * scale / 2) + (src.Width * scale * (offsetX - 1));
							float posY = inner.Y + (inner.Height / 2) - (src.Height * scale / 2) + (src.Height * scale / 2 * (offsetY == 0 ? -1 : 1));
							Vector2 pos2 = new Vector2(posX, posY);
							spriteBatch.Draw(TextureAssets.Tile[TileID.DemonAltar].Value, pos2, src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

							offsetX++;
							if (offsetX == 3) {
								offsetX = 0;
								offsetY++;
							}
						}
					}

					if (BossChecklist.BossLogConfig.SpawnItemCraftingChecklist && hasItem) {
						Vector2 posC = new Vector2(inner.X + inner.Width / 2, inner.Y + inner.Height / 2);
						checkmark ??= BossLogResources.Check_Check;
						spriteBatch.Draw(checkmark.Value, posC, Color.White);
					}

					// Hover text
					if (IsMouseHovering && string.IsNullOrEmpty(hoverText)) {
						Main.HoverItem = item;
						Main.hoverItemName = item.HoverName;
					}

					return; // This should cover everything for item slots in the Spawn subpage (spawn item slot, recipe slots, and empty tile slots)
				}

				/// Everything below is being set up for loot related itemslots ///
				EntryInfo entry = LogUI.GetLogEntryInfo;
				bool isMasterPet = entry.collectibles.ContainsKey(item.type) && (entry.collectibles[item.type] is CollectibleType.MasterPet);
				bool MasterItemRestricted = (item.type == entry.Relic || isMasterPet) && !Main.masterMode;
				bool ExpertItemRestricted = item.type == entry.ExpertItem && !Main.expertMode;
				bool OWmusicRestricted = BossChecklist.bossTracker.otherWorldMusicBoxTypes.Contains(item.type) && !BossLogUI.OtherworldUnlocked;
				bool isRestricted = MasterItemRestricted || ExpertItemRestricted || OWmusicRestricted;

				// Make a backups of the original itemslot texture and alter the texture to display the color needed
				// If the config 'Hide boss drops' is enabled and the boss hasn't been defeated yet, the itemslot should appear red, even if the item was already obtained
				// Otherwise, the itemslot will always appear green if obtained
				// If not obtained and the item is mode or seed restricted, itemslot background is red
				// Any other case should leave the itemslot color as is
				var backup = TextureAssets.InventoryBack7;
				if (hasItem) {
					TextureAssets.InventoryBack7 = TextureAssets.InventoryBack3;
				}
				else if (isRestricted) {
					TextureAssets.InventoryBack7 = TextureAssets.InventoryBack11;
				}
				
				// Draw the item slot and reset the fields to their original value
				ItemSlot.Draw(spriteBatch, ref item, context, inner.TopLeft());
				Main.inventoryScale = oldScale;
				TextureAssets.InventoryBack7 = backup;

				// Draw golden border around items that are considered collectibles
				if (entry.collectibles.ContainsKey(item.type)) {
					highlight ??= BossLogResources.RequestResource("Extra_HighlightedCollectible");
					spriteBatch.Draw(highlight.Value, inner.TopLeft(), Color.White);
				}

				// Similar to the logic of deciding the itemslot color, decide what should be drawn and what text should show when hovering over
				// Masked item takes priority, displaying 'Defeat this entry to show items'
				// If the item has not been obtained, check for item restrictions and apply those icons and texts
				// If no item restrictions exist, display normal item tooltips, and draw a checkmark for obtained items
				Vector2 pos = new Vector2(inner.X + inner.Width / 2, inner.Y + inner.Height / 2);

				if (itemResearched && (hasItem || !isRestricted)) {
					checkmark ??= BossLogResources.RequestResource("Checks_Researched");
					spriteBatch.Draw(checkmark.Value, pos, Color.White);
				}

				if (!hasItem) {
					if (MasterItemRestricted) {
						masterModeIcon ??= BossLogResources.RequestVanillaTexture("UI/WorldCreation/IconDifficultyMaster");
						spriteBatch.Draw(masterModeIcon.Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = $"{BossLogUI.LangLog}.LootAndCollection.ItemIsMasterOnly";
							BossUISystem.Instance.UIHoverTextColor = new Color(255, (byte)(Main.masterColor * 200f), 0, Main.mouseTextColor); // mimics Master Mode color
						}
					}
					else if (ExpertItemRestricted) {
						expertModeIcon ??= BossLogResources.RequestVanillaTexture("UI/WorldCreation/IconDifficultyExpert");
						spriteBatch.Draw(expertModeIcon.Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = $"{BossLogUI.LangLog}.LootAndCollection.ItemIsExpertOnly";
							BossUISystem.Instance.UIHoverTextColor = Main.DiscoColor; // mimics Expert Mode color
						}
					}
					else if (OWmusicRestricted) {
						otherWorldIcon ??= BossLogResources.RequestVanillaTexture("UI/WorldCreation/IconRandomSeed");
						spriteBatch.Draw(otherWorldIcon.Value, pos, Color.White);
						if (IsMouseHovering) {
							BossUISystem.Instance.UIHoverText = $"{BossLogUI.LangLog}.LootAndCollection.ItemIsLocked";
							BossUISystem.Instance.UIHoverTextColor = Color.Goldenrod;
						}
					}
					else {
						if (IsMouseHovering) {
							Main.HoverItem = item;
							Main.hoverItemName = item.HoverName;
						}
					}
				}
				else {
					spriteBatch.Draw(BossLogResources.Check_Check.Value, pos, Color.White);
					if (IsMouseHovering) {
						Main.HoverItem = item;
						Main.hoverItemName = item.HoverName;
					}
				}

				// Finally, if the 'Show collectible type' config is enabled, draw their respective icons and texts where needed
				if (BossChecklist.BossLogConfig.Debug.ShowCollectionType && entry.collectibles.TryGetValue(item.type, out CollectibleType type)) {
					Vector2 iconPos = new Vector2(inner.Left - 4, inner.Bottom - 15);
					spriteBatch.Draw(BossLogResources.Content_CollectibleType[(int)type].Value, iconPos, Color.White);
					if (IsMouseHovering)
						Utils.DrawBorderString(spriteBatch, type.ToString(), inner.TopLeft(), Colors.RarityAmber, 0.8f);
				}
			}
		}

		internal class LootRow : LogUIElement {
			readonly int order; // Had to put the itemslots in a row in order to be put in a UIList with scroll functionality

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

		internal class LogPanel : LogUIElement {
			public string Id { get; init; } = "";

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle pageRect = GetInnerDimensions().ToRectangle();
				if (Id == "") {
					spriteBatch.Draw(BossLogResources.Log_BackPanel.Value, pageRect, BossChecklist.BossLogConfig.BossLogColor); // Main panel draws the Log Book (with color)...
					spriteBatch.Draw(BossLogResources.Log_Paper.Value, pageRect, Color.White); //.. and the paper on top
				}

				int selectedLogPage = LogUI.PageNum;
				if (selectedLogPage == BossLogUI.Page_Prompt) {
					if (Id == "PageOne") {
						Vector2 pos = new Vector2(GetInnerDimensions().X + 10, GetInnerDimensions().Y + 15);
						string message = Language.GetTextValue($"{BossLogUI.LangLog}.ProgressionMode.BeforeYouBegin");
						Utils.DrawBorderString(spriteBatch, message, pos, Color.White, 0.8f);

						message = Language.GetTextValue($"{BossLogUI.LangLog}.ProgressionMode.AskEnable");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message);
						float scale = AutoScaleText(stringSize.X, this.Width.Pixels - 15f * 2); // header might exceed page width
						pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X * scale / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, scale);
					}
					else if (Id == "PageTwo") {
						string message = Language.GetTextValue($"{BossLogUI.LangLog}.ProgressionMode.SelectAnOption");
						Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(message);
						float scale = AutoScaleText(stringSize.X, this.Width.Pixels - 15f * 2); // header might exceed page width
						Vector2 pos = new Vector2(pageRect.X + (pageRect.Width / 2) - (stringSize.X * scale / 2), pageRect.Y + 40);
						Utils.DrawBorderString(spriteBatch, message, pos, Colors.RarityAmber, scale);
					}
				}
				else if (selectedLogPage >= 0) {
					// Boss Pages
					EntryInfo entry = LogUI.GetLogEntryInfo;
					bool masked = BossLogUI.MaskBoss(entry) == Color.Black;
					if (Id == "PageOne") {
						if (entry.customDrawing != null) {
							// If a custom drawing is active, full drawing control is given to the modder within the boss portrait
							// Nothing else will be drawn, including any base texture. Modders must supply that if they wish.
							entry.customDrawing(spriteBatch, pageRect, BossLogUI.MaskBoss(entry));
						}
						else {
							Asset<Texture2D> bossTexture = null;
							Rectangle bossSourceRectangle = new Rectangle();
							if (entry.portraitTexture != null) {
								bossTexture = entry.portraitTexture;
								bossSourceRectangle = new Rectangle(0, 0, bossTexture.Value.Width, bossTexture.Value.Height);
							}
							else if (entry.npcIDs.Count > 0) {
								Main.instance.LoadNPC(entry.npcIDs[0]);
								bossTexture = TextureAssets.Npc[entry.npcIDs[0]];
								bossSourceRectangle = new Rectangle(0, 0, bossTexture.Width(), bossTexture.Height() / Main.npcFrameCount[entry.npcIDs[0]]);
							}
							if (bossTexture != null) {
								float drawScale = 1f;
								float xScale = (float)pageRect.Width / bossSourceRectangle.Width;
								// TODO: pageRect.Height might be too much, we might want to trim off the top a bit (May need adjusting, but changed to -150)
								float yScale = (float)(pageRect.Height - 150) / bossSourceRectangle.Height;
								if (xScale < 1 || yScale < 1) {
									drawScale = xScale < yScale ? xScale : yScale;
								}
								spriteBatch.Draw(bossTexture.Value, pageRect.Center(), bossSourceRectangle, BossLogUI.MaskBoss(entry), 0, bossSourceRectangle.Center(), drawScale, SpriteEffects.None, 0f);
							}
						}

						// Everything below this point is outside of the boss portrait (Boss head icons, boss names, etc)

						Rectangle firstHeadPos = new Rectangle();
						bool countedFirstHead = false;
						int offset = 0;
						int totalWidth = 0;
						int lastX = 0;
						foreach (Asset<Texture2D> headTexture in entry.headIconTextures().Reverse<Asset<Texture2D>>()) {
							Texture2D head = headTexture.Value;
							Rectangle src = new Rectangle(0, 0, head.Width, head.Height);
							// Weird special case for Deerclops. Its head icon has a significant amount of whitespace.
							if (entry.Key == "Terraria Deerclops") {
								src = new Rectangle(2, 0, 48, 40);
							}
							int xHeadOffset = pageRect.Right - src.Width - 10 - ((src.Width + 2) * offset);
							Rectangle headPos = new Rectangle(xHeadOffset, pageRect.Y + 5, src.Width, src.Height);
							if (!countedFirstHead) {
								firstHeadPos = headPos;
								countedFirstHead = true;
							}
							spriteBatch.Draw(head, headPos, src, BossLogUI.MaskBoss(entry));
							offset++;
							totalWidth += headPos.Width;
							lastX = xHeadOffset;
						}

						string isDefeated = $"{Language.GetTextValue($"{BossLogUI.LangLog}.EntryPage.Defeated", Main.worldName)}";
						string notDefeated = $"{Language.GetTextValue($"{BossLogUI.LangLog}.EntryPage.Undefeated", Main.worldName)}";

						if (entry.MarkedAsDowned) {
							isDefeated = $"''{Language.GetTextValue($"{BossLogUI.LangLog}.EntryPage.Defeated", Main.worldName)}''";
						}

						Asset<Texture2D> texture = entry.IsAutoDownedOrMarked ? BossLogResources.Check_Check : BossLogResources.Check_X;
						Vector2 defeatpos = new Vector2(firstHeadPos.X + (firstHeadPos.Width / 2), firstHeadPos.Y + firstHeadPos.Height - (texture.Height() / 2));
						spriteBatch.Draw(texture.Value, defeatpos, Color.White);

						// Hovering over the head icon will display the defeated text
						Rectangle hoverRect = new Rectangle(lastX, firstHeadPos.Y, totalWidth, firstHeadPos.Height);
						if (Main.MouseScreen.Between(hoverRect.TopLeft(), hoverRect.BottomRight())) {
							BossUISystem.Instance.UIHoverText = entry.IsAutoDownedOrMarked ? isDefeated : notDefeated;
							BossUISystem.Instance.UIHoverTextColor = entry.IsAutoDownedOrMarked ? Colors.RarityGreen : Colors.RarityRed;
						}

						Vector2 pos = new Vector2(pageRect.X + 5, pageRect.Y + 5);
						string progression = BossChecklist.BossLogConfig.Debug.ShowProgressionValue ? $"[{entry.progression}f] " : "";
						Utils.DrawBorderString(spriteBatch, progression + entry.DisplayName, pos, Color.Goldenrod);

						pos = new Vector2(pageRect.X + 5, pageRect.Y + 30);
						Utils.DrawBorderString(spriteBatch, entry.ModDisplayName, pos, new Color(150, 150, 255));
					}
					else if (Id == "PageTwo" && entry.modSource != "Unknown") {
						if (LogUI.SelectedSubPage == SubPage.Records) {
							if (entry.type == EntryType.Boss) {
								// Boss Records SubPage
							}
							else if (entry.type == EntryType.MiniBoss) {
								// Mini-boss Records SubPage
							}
							else if (entry.type == EntryType.Event) {
								int offset = 25;
								int offsetY = 64;
								int col = 0;
								int row = 0;
								const int maxBannersPerRow = 12;

								foreach (int bannerID in LogUI.Banners) {
									int npcID = Item.BannerToNPC(bannerID);
									int bannerItem = Item.BannerToItem(bannerID);

									if (bannerID <= 0 || bannerItem <= 0 || !ContentSamples.ItemsByType.TryGetValue(bannerItem, out Item item) || item?.createTile <= -1)
										continue; // a banner is not assigned or is invalid

									if (NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npcID)])
										continue; // skip over excluded npcs

									bool isModNPC = npcID >= NPCID.Count;
									int bannerTile = isModNPC ? item.createTile : TileID.Banners;
									int bannerPlaceStyle = isModNPC ? item.placeStyle : bannerID + 21;
									Asset<Texture2D> banner = TextureAssets.Tile[bannerTile];
									Main.instance.LoadTiles(bannerTile);

									bool reachedKillCount = NPC.killCount[bannerID] >= ItemID.Sets.KillsToBanner[bannerItem];
									Color bannerColor = reachedKillCount ? Color.White : masked ? Color.Black : BossLogUI.faded;

									if (banner is null)
										continue; // no texture given to draw

									/// Code adapted from TileObject.DrawPreview
									var tileData = TileObjectData.GetTileData(bannerTile, bannerPlaceStyle);
									int styleColumn = tileData.CalculatePlacementStyle(bannerPlaceStyle, 0, 0); // adjust for StyleMultiplier
									int styleRow = 0;
									//int num3 = tileData.DrawYOffset;
									if (tileData.StyleWrapLimit > 0) {
										styleRow = styleColumn / tileData.StyleWrapLimit * tileData.StyleLineSkip; // row quotient
										styleColumn %= tileData.StyleWrapLimit; // remainder
									}
									int x = tileData.StyleHorizontal ? tileData.CoordinateFullWidth * styleColumn : tileData.CoordinateFullWidth * styleRow;
									int y = tileData.StyleHorizontal ? tileData.CoordinateFullHeight * styleRow : tileData.CoordinateFullHeight * styleColumn;
									int[] heights = tileData.CoordinateHeights;
									int heightOffSet = 0;
									int heightOffSetTexture = 0;

									for (int j = 0; j < heights.Length; j++) { // could adjust for non 1x3 here and below if we need to.
										Rectangle bannerPos = new Rectangle(pageRect.X + 40 + (offset * (col % maxBannersPerRow)), pageRect.Y + 185 + heightOffSet + (offsetY * row), 16, 16);
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

									col++; // increase banner count after banner is fully drawn
									if (col % maxBannersPerRow == 0)
										row++; // if banners per row has been reached, increase row count

									if (row == 3)
										break; // For now, we stop drawing any banners that exceed the books limit (TODO: might have to reimplement as a UIList for scrolling purposes)
								}
							}
						}
						else if (LogUI.SelectedSubPage == SubPage.SpawnInfo) {
							// Spawn Item Subpage
						}
						else if (LogUI.SelectedSubPage == SubPage.LootAndCollectibles) {
							// Loot Table Subpage
						}
					}
				}
			}
		}

		internal class RecordDisplaySlot : UIImage {
			internal int slotID = -1;
			internal string title;
			internal string value;
			internal Point ach;
			internal string tooltip;

			PersonalRecords stats_player => BossUISystem.Instance.BossLog.GetPlayerRecords;
			WorldRecord stats_world => BossUISystem.Instance.BossLog.GetWorldRecords;

			public RecordDisplaySlot(Asset<Texture2D> texture, string title = null, string value = null) : base(texture) {
				this.title = title;
				this.value = value;
				this.ach = new Point(-1, -1);
			}

			public RecordDisplaySlot(Asset<Texture2D> texture, SubCategory subCategory, int slot, int RecordIndex) : base(texture) {
				Width.Pixels = texture.Value.Width;
				Height.Pixels = texture.Value.Height;

				slotID = slot;
				title = GetTitle(subCategory)[slot];
				value = GetValue(subCategory)[slot];
				tooltip = GetTooltip(subCategory)[slot];
				ach = GetAchCoords(subCategory)[slot];
			}

			private string[] GetTitle(SubCategory sub) {
				string path = $"{BossLogUI.LangLog}.Records";
				return new string[] {
					Language.GetTextValue($"{path}.Category.{sub}"),
					Language.GetTextValue($"{path}.Title.{sub}"),
					Language.GetTextValue($"{path}.Title.Duration{(sub == SubCategory.WorldRecord ? "World" : "")}"),
					Language.GetTextValue($"{path}.Title.HitsTaken{(sub == SubCategory.WorldRecord ? "World" : "")}")
				};
			}

			private string[] GetValue(SubCategory sub) {
				// Defaults to Previous Attempt, the subcategory users will first see
				string unique = stats_player.attempts == 0 ? Language.GetTextValue($"{BossLogUI.LangLog}.Records.Unchallenged") : $"#{stats_player.attempts}";
				string duration = PersonalRecords.TimeConversion(stats_player.durationPrev);
				string hitsTaken = PersonalRecords.HitCount(stats_player.hitsTakenPrev);

				if (sub == SubCategory.PersonalBest) {
					unique = stats_player.GetKDR();
					duration = PersonalRecords.TimeConversion(stats_player.durationBest);
					hitsTaken = PersonalRecords.HitCount(stats_player.hitsTakenBest);
				}
				else if (sub == SubCategory.FirstVictory) {
					unique = stats_player.PlayTimeToString();
					duration = PersonalRecords.TimeConversion(stats_player.durationFirst);
					hitsTaken = PersonalRecords.HitCount(stats_player.hitsTakenFirst);
				}
				else if (sub == SubCategory.WorldRecord) {
					unique = stats_world.GetGlobalKDR();
					duration = PersonalRecords.TimeConversion(stats_world.durationWorld);
					hitsTaken = PersonalRecords.HitCount(stats_world.hitsTakenWorld);
				}

				return new string[] {
					Language.GetTextValue(sub == SubCategory.WorldRecord ? Main.worldName : Main.LocalPlayer.name),
					unique,
					duration,
					hitsTaken
				};
			}

			private string[] GetTooltip(SubCategory sub) {
				string path = $"{BossLogUI.LangLog}.Records.Tooltip";
				return new string[] {
					"",
					Language.GetTextValue($"{path}.{sub}"),
					Language.GetTextValue($"{path}.Duration"),
					Language.GetTextValue($"{path}.HitsTaken")
				};
			}

			private Point[] GetAchCoords(SubCategory sub) {
				Point uniqueAch = new Point(0, 9);

				if (sub == SubCategory.PersonalBest) {
					uniqueAch = new Point(0, 3);
				}
				else if (sub == SubCategory.FirstVictory) {
					uniqueAch = new Point(7, 10);
				}
				else if (sub == SubCategory.WorldRecord) {
					uniqueAch = stats_world.totalKills >= stats_world.totalDeaths ? new Point(4, 10) : new Point(4, 8);
				}

				return new Point[] {
					new Point(-1, -1),
					uniqueAch,
					sub == SubCategory.WorldRecord ? new Point(2, 12) : new Point(4, 9),
					sub == SubCategory.WorldRecord ? new Point(0, 7) : new Point(3, 0)
				};
			}

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle inner = GetInnerDimensions().ToRectangle();

				// Draw an achievement icon that represents the record type
				if (ach.X >= 0 && ach.Y >= 0) {
					Asset<Texture2D> achievements = BossLogResources.RequestVanillaTexture("UI/Achievements", false);
					Rectangle achSlot = new Rectangle(66 * ach.X, 66 * ach.Y, 64, 64);
					spriteBatch.Draw(achievements.Value, inner.TopLeft(), achSlot, Color.White);

					if (Main.MouseScreen.Between(inner.TopLeft(), new Vector2(inner.X + 64, inner.Y + 64))) {
						BossUISystem.Instance.UIHoverText = tooltip;
					}
				}

				// Draw the title and record value texts
				if (!string.IsNullOrEmpty(title)) {
					Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(title);
					Color col = slotID == 0 ? Color.Goldenrod : Color.Gold;
					float scl = AutoScaleText(stringAdjust.X, this.Width.Pixels - (64 * 2) - 15f); // record title may overlap with icon
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (int)(stringAdjust.X * scl / 2) + 2, inner.Y + (int)(stringAdjust.Y * scl / 3));
					Utils.DrawBorderString(spriteBatch, title, pos, col, scl);
				}

				if (!string.IsNullOrEmpty(value)) {
					Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(value);
					Color col = slotID == 0 ? Color.LightYellow : Color.White;
					float scl = AutoScaleText(stringAdjust.X, this.Width.Pixels - (64f * 2) - 15f); // record value may overlap with icon
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (int)(stringAdjust.X * scl / 2) + 2, inner.Y + inner.Height - (int)stringAdjust.Y * scl);
					Utils.DrawBorderString(spriteBatch, value, pos, col, scl);
				}
			}
		}

		internal class ContributorCredit : UIImage {
			internal string Id { get; init; }
			internal Asset<Texture2D> icon;
			internal string name;
			internal string devTitle;
			internal int[] entryCounts = null;

			public ContributorCredit(Asset<Texture2D> texture, Asset<Texture2D> character, string name, string title) : base(texture) {
				Id = "Dev";
				this.icon = character;
				this.name = name;
				this.devTitle = title;
			}

			public ContributorCredit(Asset<Texture2D> texture, string modName) : base(texture) {
				Id = "Mod";
				this.icon = GetModIcon(modName);
				this.name = BossUISystem.RemoveChatTags(ModLoader.GetMod(modName));
				this.entryCounts = BossChecklist.bossTracker.RegisteredMods[modName];
			}

			public ContributorCredit(Asset<Texture2D> texture, string name, string description) : base(texture) {
				this.icon = null;
				this.name = name;
				this.devTitle = description;
			}

			private Asset<Texture2D> GetModIcon(string modName) {
				if (ModLoader.TryGetMod(modName, out Mod mod)) {
					if (mod.HasAsset("icon"))
						return ModContent.Request<Texture2D>(mod.Name + "/icon");

					if (mod.HasAsset("icon_workshop"))
						return ModContent.Request<Texture2D>(mod.Name + "/icon_workshop");
				}
				return BossLogResources.RequestResource("Credits_NoIcon");
			}

			private Point MaxLength() {
				return Id switch {
					"Dev" => new Point(224, 224),
					"Mod" => new Point(208, -1), // -1 because unused
					"NoMods" => new Point(260, -1),
					"Register" => new Point(280, 275),
					_ => new Point(-1, -1)
				};
			}

			private Point GetTextPos() {
				return Id switch {
					"Dev" => new Point(80, 85),
					"Mod" => new Point(95, -1), // -1 because unused
					"NoMods" => new Point(40, -1),
					"Register" => new Point(45, 25),
					_ => new Point(-1, -1)
				};
			}

			public override void Draw(SpriteBatch spriteBatch) {
				base.Draw(spriteBatch);
				Rectangle inner = GetInnerDimensions().ToRectangle();
				int ModOffset = string.IsNullOrEmpty(devTitle) ? 8 : 0;

				if (icon is not null) {
					Rectangle iconRect = new Rectangle(inner.X + ModOffset, inner.Y + ModOffset, 80, 80);
					spriteBatch.Draw(icon.Value, iconRect, Color.White); // character/icon drawing
					if (icon.Name == "Resources\\Credits_NoIcon" && Main.MouseScreen.Between(iconRect.TopLeft(), iconRect.BottomRight()))
						BossUISystem.Instance.UIHoverText = $"{BossLogUI.LangLog}.Credits.NoIcon";
				}

				float scale = AutoScaleText(FontAssets.MouseText.Value.MeasureString(name).X, MaxLength().X);
				spriteBatch.DrawString(FontAssets.MouseText.Value, name, new Vector2(inner.X + GetTextPos().X, inner.Y + 11), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); // Draw the dev/mod name as a string

				if (!string.IsNullOrEmpty(devTitle)) {
					scale = AutoScaleText(FontAssets.MouseText.Value.MeasureString(devTitle).X, MaxLength().Y); // Mod name might exceed panel size
					spriteBatch.DrawString(FontAssets.MouseText.Value, devTitle, new Vector2(inner.X + GetTextPos().Y, inner.Y + 45), Color.LemonChiffon, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); // Draw the dev title as a string
				}
				else if (entryCounts != null) {
					int xOffset = 94 + (70 * 2 / 3); // draw each entry count submitted by the mod
					foreach (int entryNum in entryCounts) {
						Vector2 textSize = FontAssets.MouseText.Value.MeasureString(entryNum.ToString()); // no need to auto-scale as it is unlikely for a mod to submit 100+ entries
						Vector2 pos = new Vector2(inner.X + xOffset - (int)(textSize.X / 2), inner.Y + 54);
						spriteBatch.DrawString(FontAssets.MouseText.Value, entryNum.ToString(), pos, Color.LemonChiffon);
						xOffset += 74;
					}
				}
			}
		}

		internal class LogTab : LogUIElement {
			public string Id { get; init; } = "";
			internal Asset<Texture2D> texture;
			internal Asset<Texture2D> icon;

			private int? anchor = null;
			public int? Anchor {
				get => anchor;
				set {
					anchor = value;
					if (Id == "TableOfContents") {
						this.hoverText = value == null ? $"{BossLogUI.LangLog}.Tabs.ToggleFilters" : $"{BossLogUI.LangLog}.Tabs.TableOfContents";
					}
					else if (Id != "Credits" && value.HasValue && value.Value >= 0 && value.Value < BossChecklist.bossTracker.SortedEntries.Count) {
						EntryInfo entry = BossChecklist.bossTracker.SortedEntries[value.Value];
						string type = Language.GetTextValue($"{BossLogUI.LangLog}.Common.{(BossChecklist.BossLogConfig.ProgressiveChecklist ? "Entry" : entry.type)}");
						this.hoverText = Language.GetTextValue($"{BossLogUI.LangLog}.Tabs.NextEntry", type, entry.DisplayName);
					}
				}
			}

			public LogTab(Asset<Texture2D> texture, Asset<Texture2D> icon) {
				Width.Pixels = texture.Value.Width;
				Height.Pixels = texture.Value.Height;

				this.texture = texture;
				this.icon = icon;
			}

			public bool Visibile() {
				int page = LogUI.PageNum;
				if (page == BossLogUI.Page_Prompt)
					return false; // Tabs never show up on the Progression Mode prompt

				if ((BossChecklist.BossLogConfig.ProgressiveChecklist || BossChecklist.BossLogConfig.OnlyShowBossContent) && (Id == "MiniBoss" || Id == "Event"))
					return false; // Mini-boss and Event tabs won't show when OnlyShowBossContent is enable

				return Id switch {
					"TableOfContents" => true,
					"Credits" => page != BossLogUI.Page_Credits,
					_ => Anchor.HasValue && Anchor >= 0 && page != Anchor
				};
			}

			public bool OnLeftSide() {
				int page = LogUI.PageNum;
				return Id switch {
					"TableOfContents" => true,
					"Credits" => false,
					_ => page > Anchor || page == BossLogUI.Page_Credits
				};
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);
				if (Anchor.HasValue)
					LogUI.PendingPageNum = Anchor.Value;
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (this.Visibile()) {
					// Tab drawing
					base.Draw(spriteBatch);
					Rectangle inner = GetInnerDimensions().ToRectangle();

					spriteBatch.Draw(texture.Value, inner, texture.Value.Bounds, Color.Tan, 0f, Vector2.Zero, OnLeftSide() ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);

					int offsetX = inner.X < Main.screenWidth / 2 ? 2 : -2;
					Vector2 pos = new Vector2(inner.X + (inner.Width / 2) - (icon.Value.Width / 2) + offsetX, inner.Y + (inner.Height / 2) - (icon.Value.Height / 2));
					Asset<Texture2D> iconTexture = Id == "TableOfContents" && LogUI.PageNum == BossLogUI.Page_TableOfContents ? BossLogResources.Nav_Filter : icon;
					spriteBatch.Draw(iconTexture.Value, pos, Color.White);
				}
			}
		}

		internal class TableOfContents : UIText {
			readonly EntryInfo entry;
			readonly bool markAsNext;
			readonly string displayName;
			readonly bool allLoot;
			readonly bool allCollectibles;

			internal Color defaultColor;

			internal BossLogUI GetParentLog => BossUISystem.Instance.BossLog;

			public TableOfContents(int index, string displayName, Color entryColor, bool loot, bool collect, float textScale = 1, bool large = false) : base(displayName, textScale, large) {
				this.entry = BossChecklist.bossTracker.SortedEntries[index];
				this.displayName = displayName;
				this.markAsNext = BossChecklist.BossLogConfig.DrawNextMark && entry.IsUpNext && !entry.hidden && !GetParentLog.HiddenEntriesMode;
				this.allLoot = loot;
				this.allCollectibles = collect;
				TextColor = this.defaultColor = markAsNext && BossChecklist.BossLogConfig.ColoredBossText ? new Color(248, 235, 91) : entryColor;
			}

			public override void LeftClick(UIMouseEvent evt) {
				if (GetParentLog.HiddenEntriesMode) {
					entry.hidden = !entry.hidden;
					SoundEngine.PlaySound(SoundID.Unlock);
					TextColor = this.defaultColor = entry.hidden ? Color.DimGray : Color.DarkGray;
					if (!GetParentLog.HiddenEntriesPending.ContainsKey(entry.Key)) {
						GetParentLog.HiddenEntriesPending.Add(entry.Key, entry.hidden);
					}
					else {
						GetParentLog.HiddenEntriesPending[entry.Key] = entry.hidden;
					}
					return;
				}

				if (BossChecklist.BossLogConfig.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext)
					return;

				GetParentLog.PendingPageNum = entry.GetIndex; // jump to entry page
			}

			public override void RightClick(UIMouseEvent evt) {
				if (GetParentLog.HiddenEntriesMode) {
					entry.hidden = !entry.hidden;
					SoundEngine.PlaySound(SoundID.Unlock);
					TextColor = this.defaultColor = entry.hidden ? Color.DimGray : Color.DarkGray;
					if (!GetParentLog.HiddenEntriesPending.ContainsKey(entry.Key)) {
						GetParentLog.HiddenEntriesPending.Add(entry.Key, entry.hidden);
					}
					else {
						GetParentLog.HiddenEntriesPending[entry.Key] = entry.hidden;
					}
				}
				else {
					// Entries must not already be downed to add/remove them from the MarkedEntries list
					// Entries that are downed will automatically be removed from the lsit when the TableOfContents list is generated
					if (WorldAssist.MarkedEntries.Contains(entry.Key)) {
						WorldAssist.MarkedEntries.Remove(entry.Key);
					}
					else {
						WorldAssist.MarkedEntries.Add(entry.Key);
					}

					Networking.RequestMarkedEntryUpdate(entry.Key, entry.MarkedAsDowned);

					GetParentLog.RefreshPageContent(); // refresh the page to show visual changes
				}
			}

			public override void MouseOver(UIMouseEvent evt) {
				BossLogUI.headNum = entry.GetIndex;
				if (BossChecklist.BossLogConfig.Debug.ShowProgressionValue) {
					SetText($"[{entry.progression}f] {displayName}");
				}
				TextColor = BossChecklist.BossLogConfig.ColoredBossText ? Color.SkyBlue : Color.Silver;
				base.MouseOver(evt);
			}

			public override void MouseOut(UIMouseEvent evt) {
				BossLogUI.headNum = -1; // MouseOut will occur even if the element is removed when changing pages!
				SetText(displayName);
				TextColor = defaultColor;
				base.MouseOut(evt);
			}

			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle inner = GetInnerDimensions().ToRectangle();
				Vector2 pos = new Vector2(inner.X - 20, inner.Y - 5);

				if (BossChecklist.FeatureConfig.NewRecordLogGlow && entry.IsRecordIndexed(out int recordIndex) && GetParentLog.GetModPlayer.hasNewRecord[recordIndex])
					this.TextColor = Main.DiscoColor;

				// base drawing comes after colors so they do not flicker when updating check list
				base.Draw(spriteBatch);

				Rectangle parent = this.Parent.GetInnerDimensions().ToRectangle();
				int hardModeOffset = entry.progression > BossTracker.WallOfFlesh ? 10 : 0;
				string looted = Language.GetTextValue($"{BossLogUI.LangLog}.TableOfContents.AllLoot");
				string collected = Language.GetTextValue($"{BossLogUI.LangLog}.TableOfContents.AllCollectibles");

				if (!GetParentLog.HiddenEntriesMode && allLoot) {
					// When all loot is obtained, also check for collectibles as a bonus
					Texture2D texture = !BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && allCollectibles ? BossLogResources.Check_GoldChest.Value : BossLogResources.Check_Chest.Value;
					string hoverText = !BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && allCollectibles ? $"{looted}\n{collected}" : looted;
					Rectangle chestPos = new Rectangle(parent.X + parent.Width - texture.Width - hardModeOffset, inner.Y - 2, texture.Width, texture.Height);
					spriteBatch.Draw(texture, chestPos, Color.White);
					if (Main.MouseScreen.Between(chestPos.TopLeft(), chestPos.BottomRight())) {
						BossUISystem.Instance.UIHoverText = hoverText;
					}
				}					

				Asset<Texture2D> checkGrid = BossLogResources.Check_Box;
				string checkType = BossChecklist.BossLogConfig.SelectedCheckmarkType;

				if (GetParentLog.HiddenEntriesMode) {
					// Do not draw checkmark status if HiddenEntriesMode is enabled. Eye toggle buttons should appear instead.
					checkGrid = entry.hidden ? TextureAssets.InventoryTickOff : TextureAssets.InventoryTickOn;
					pos.Y += 7;
				}
				else if (entry.IsAutoDownedOrMarked) {
					if (checkType == BossLogConfiguration.CheckType_XAndEmpty) {
						checkGrid = BossLogResources.Check_X;
					}
					else if (checkType != BossLogConfiguration.CheckType_StrikeThrough) {
						checkGrid = BossLogResources.Check_Check;
					}
					else {
						Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(displayName);
						Asset<Texture2D> strike = BossLogResources.Check_Strike;
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
					checkGrid = checkType == BossLogConfiguration.CheckType_CheckAndX ? BossLogResources.Check_X : BossLogResources.Check_Box;
					if (markAsNext) {
						checkGrid = BossLogResources.Check_Next;
					}
				}

				if (GetParentLog.HiddenEntriesMode) {
					spriteBatch.Draw(checkGrid.Value, pos, Color.White);
				}
				else if (!entry.hidden && checkType != BossLogConfiguration.CheckType_StrikeThrough) {
					spriteBatch.Draw(BossLogResources.Check_Box.Value, pos, Color.White);
					spriteBatch.Draw(checkGrid.Value, pos, Color.White);
				}
			}

			public override int CompareTo(object obj) {
				TableOfContents other = obj as TableOfContents;
				return entry.progression.CompareTo(other.entry.progression);
			}
		}

		internal class ProgressBar : LogUIElement {
			private Asset<Texture2D> fullBar;
			internal readonly float percentageTotal;
			internal readonly Point countsTotal;
			internal Dictionary<EntryType, float> PercentagesByType;
			internal Dictionary<string, float> PercentagesByMod;
			internal Dictionary<EntryType, Point> CountsByType;
			internal Dictionary<string, Point> CountsByMod;

			internal bool InitializeDividers;
			internal Dictionary<Rectangle, string> Sections;

			public ProgressBar(bool hardMode) {
				InitializeDividers = true; // a progress bar is created, let this UIelement know it should attempt to make section dividers asap

				// Start with the total percentage and total counts
				this.percentageTotal = CalculateTotalPercentage(BossChecklist.bossTracker.SortedEntries, hardMode, out int d, out int t);
				this.countsTotal = new Point(d, t);

				// populate percentage values
				PercentagesByType = new Dictionary<EntryType, float>() {
					{ EntryType.Boss, 0f },
					{ EntryType.MiniBoss, 0f },
					{ EntryType.Event, 0f }
				};
				CountsByType = new Dictionary<EntryType, Point>();

				foreach (EntryType type in PercentagesByType.Keys) {
					PercentagesByType[type] = CalculateTotalPercentage(BossChecklist.bossTracker.SortedEntries.FindAll(entry => entry.type == type), hardMode, out int downed, out int total);
					if (total == 0)
						PercentagesByType.Remove(type); // Remove entries not found by type. This can occur when filtering out entry types

					CountsByType.TryAdd(type, new Point(downed, total));
				}

				PercentagesByMod = new Dictionary<string, float>(); // create a new dictionary, adding Terraria and Unknown entries menually if they exist on the Table of Contents
				CountsByMod = new Dictionary<string, Point>();

				// Terraria entries should appear first
				float TerrariaPercentage = CalculateTotalPercentage(BossChecklist.bossTracker.SortedEntries.FindAll(entry => entry.modSource == "Terraria"), hardMode, out int downedTerraria, out int totalTerraria);
				if (totalTerraria != 0) {
					PercentagesByMod.TryAdd("Terraria", TerrariaPercentage);
					CountsByMod.TryAdd("Terraria", new Point(downedTerraria, totalTerraria));
				}

				// populate dictionary with modded entries
				foreach (string mod in BossChecklist.bossTracker.RegisteredMods.Keys) {
					PercentagesByMod.TryAdd(mod, CalculateTotalPercentage(BossChecklist.bossTracker.SortedEntries.FindAll(entry => entry.modSource == mod), hardMode, out int downed, out int total));
					if (total == 0)
						PercentagesByMod.Remove(mod); // If their are no listed entries, remove the mod

					CountsByMod.TryAdd(mod, new Point(downed, total));
				}

				// Unknown entries should appear last
				float UnknownPercentage = CalculateTotalPercentage(BossChecklist.bossTracker.SortedEntries.FindAll(entry => entry.modSource == "Unknown"), hardMode, out int downedUnknown, out int totalUnknown);
				if (totalUnknown != 0) {
					PercentagesByMod.TryAdd("Unknown", UnknownPercentage);
					CountsByMod.TryAdd("Unknown", new Point(downedUnknown, totalUnknown));
				}
			}

			private float CalculateTotalPercentage(List<EntryInfo> entries, bool hardMode, out int downed, out int total) {
				total = 0;
				downed = 0;
				foreach (EntryInfo entry in entries) {
					if (!entry.VisibleOnChecklist() || (hardMode && entry.progression <= BossTracker.WallOfFlesh) || (!hardMode && entry.progression > BossTracker.WallOfFlesh))
						continue; // skip entry if it is not visible on the checklist or if it is not on the selected hardmode status

					total++;
					if (entry.IsAutoDownedOrMarked)
						downed++;
				}

				return total == 0 ? 1f : (float)downed / (float)total;
			}

			private void GenerateDividers() {
				Sections = new Dictionary<Rectangle, string>();

				Rectangle inner = GetInnerDimensions().ToRectangle();
				int barFull = inner.Width - 12 + 4;
				int barRemainder = (int)(barFull * this.percentageTotal);
				int meterX = inner.X + 4;
				if (LogUI.barState) {
					string finalValue = CountsByMod.First().Key;
					foreach (KeyValuePair<string, Point> pair in CountsByMod) {
						if (pair.Value.X != 0)
							finalValue = pair.Key; // determine the final section needed to be created
					}

					foreach (KeyValuePair<string, Point> pair in CountsByMod) {
						if (pair.Value.X == 0)
							continue; // if no downs, don't create a section

						int length = (int)(barFull * ((float)pair.Value.X / (float)countsTotal.Y));
						string modName = ModLoader.TryGetMod(pair.Key, out Mod mod) ? mod.DisplayName : pair.Key;
						string hoverText = $"{modName}: {pair.Value.X}/{pair.Value.Y} ({(((float)pair.Value.X / (float)countsTotal.Y) * 100).ToString("#0.0")}%)";

						if (pair.Key == finalValue)
							length += barRemainder - length + 1; // the final rectangle needs to cover any remaining bar left

						Sections.TryAdd(new Rectangle(meterX, inner.Y, length - 1, inner.Height), hoverText);

						meterX += length;
						barRemainder -= length;
					}
				}
				else {
					EntryType finalValue = CountsByType.First().Key;
					foreach (KeyValuePair<EntryType, Point> pair in CountsByType) {
						if (pair.Value.X != 0)
							finalValue = pair.Key; // determine the final section needed to be created
					}

					foreach (KeyValuePair<EntryType, Point> pair in CountsByType) {
						if (pair.Value.X == 0)
							continue; // if no downs, don't create a section

						int length = (int)(barFull * ((float)pair.Value.X / (float)countsTotal.Y));
						string hoverText = $"{pair.Key}: {pair.Value.X}/{pair.Value.Y} ({(((float)pair.Value.X / (float)countsTotal.Y) * 100).ToString("#0.0")}%)";

						if (pair.Key == finalValue)
							length += barRemainder - length + 1; // the final rectangle needs to cover any remaining bar left

						Sections.TryAdd(new Rectangle(meterX, inner.Y, length - 1, inner.Height), hoverText);

						meterX += length;
						barRemainder -= length;
					}
				}
			}

			public override void LeftClick(UIMouseEvent evt) {
				base.LeftClick(evt);
				LogUI.barState = !LogUI.barState;
				GenerateDividers(); // update the dividers based on the new bar state
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);
				if (InitializeDividers) {
					GenerateDividers(); // Can only generate dividers once the dimensions are declared
					InitializeDividers = false;
				}
			}

			public override void Draw(SpriteBatch spriteBatch) {
				if (LogUI.HiddenEntriesMode)
					return;

				base.Draw(spriteBatch);
				Rectangle inner = GetInnerDimensions().ToRectangle();

				// drawing a percentage value above the bar
				string percentDisplay = $"{(this.percentageTotal * 100).ToString("#0.0")}%";
				float scale = 0.85f;
				Vector2 stringAdjust = FontAssets.MouseText.Value.MeasureString(percentDisplay) * scale;
				Vector2 percentPos = new Vector2(inner.X + (inner.Width / 2) - (stringAdjust.X / 2), inner.Y - stringAdjust.Y);
				Utils.DrawBorderString(spriteBatch, percentDisplay, percentPos, Colors.RarityAmber, scale);

				fullBar ??= BossLogResources.RequestResource("Extra_ProgressBar");
				int wCut = fullBar.Value.Width / 3;
				int h = fullBar.Value.Height;
				int extraBar = 4; // this is the small bit of bar that is the the end sections, unless the texture is changed, this is vital
				int barWidth = inner.Width - 12 + extraBar;

				// Drawing the full bar
				spriteBatch.Draw(fullBar.Value, new Rectangle(inner.X, inner.Y, wCut, h), new Rectangle(0, 0, wCut, h), Color.White); // Beginning of bar
				spriteBatch.Draw(fullBar.Value, new Rectangle(inner.X + wCut, inner.Y, barWidth - extraBar, h), new Rectangle(wCut, 0, wCut, h), Color.White); // Center of bar
				spriteBatch.Draw(fullBar.Value, new Rectangle(inner.X + inner.Width - wCut, inner.Y, wCut, h), new Rectangle(2 * wCut, 0, wCut, h), Color.White); // End of bar

				// drawing the progress meter
				Color barColor = BossChecklist.BossLogConfig.BossLogColor;
				barColor.A = 180;
				Rectangle meterProgress = new Rectangle(inner.X + 4, inner.Y + 4, (int)(barWidth * this.percentageTotal), inner.Height - 8);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterProgress, Color.White); // The base meter, using white will lighten the book color over drawn over top
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, meterProgress, barColor); // The base meter, using white will lighten the book color over drawn over top

				// drawing the section dividers as well as the hover text and hover color of each section where applicable
				foreach (KeyValuePair<Rectangle, string> pair in Sections) {
					if (Main.MouseScreen.Between(pair.Key.TopLeft(), pair.Key.BottomRight())) {
						BossUISystem.Instance.UIHoverText = pair.Value;
						Rectangle section = new Rectangle(pair.Key.X, pair.Key.Y + 4, pair.Key.Width, pair.Key.Height - 8);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, section, BossChecklist.BossLogConfig.BossLogColor);
					}

					if (pair.Key != Sections.Last().Key) {
						Rectangle divider = new Rectangle(pair.Key.X + pair.Key.Width - 1, pair.Key.Y + 4, 2, pair.Key.Height - 8);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, divider, BossChecklist.BossLogConfig.BossLogColor);
					}
				}
			}
		}

		internal class FittedTextPanel : UITextPanel<string> {
			readonly string text;
			public FittedTextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
				this.text = text;
			}

			const float infoScaleX = 1f;
			const float infoScaleY = 1f;
			public override void Draw(SpriteBatch spriteBatch) {
				Rectangle hitbox = new Rectangle((int)GetInnerDimensions().X, (int)GetInnerDimensions().Y, (int)Width.Pixels, 100);
;
				TextSnippet[] textSnippets = ChatManager.ParseMessage(Language.GetTextValue(text), Color.White).ToArray();
				ChatManager.ConvertNormalSnippets(textSnippets);

				foreach (Vector2 direction in ChatManager.ShadowDirections) {
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + direction * 1,
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}

				ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft(),
					Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out _, hitbox.Width - (7 * 2), false);
			}
		}
	}
}
