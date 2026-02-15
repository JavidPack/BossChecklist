using BossChecklist.Resources;
using BossChecklist.Systems;
using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using static BossChecklist.UIElements.BossLogUIElements;

namespace BossChecklist
{
	internal enum PageCategory {
		Records,
		SpawnInfo,
		LootAndCollectibles
	}

	internal enum RecordCategory {
		PreviousAttempt,
		FirstVictory,
		PersonalBest,
		WorldRecord,
		None
	}

	class BossLogUI : UIState {
		public OpenLogButton OpenBossLogButton; // The main button to open the Boss Log
		public LogPanel BookArea; // The main panel for the UI. All content is aligned within this area.
		public LogPanel LeftPage; // left page content panel
		public LogPanel RightPage; // right page content panel

		private int BossLogPageNumber;
		public const int Page_TableOfContents = -1;
		public const int Page_Credits = -2; // The credits page is the last page after all entries, despite being -2
		public const int Page_Prompt = -3;

		/// <summary>
		/// The page number for the client's Boss Log. Changing the page value will automatically update the log to dispaly the selected page.
		/// </summary>
		public int PageNum {
			get => BossLogPageNumber;
			set {
				if (value == -3) {
					OpenProgressionModePrompt();
				}
				else if (!HiddenEntriesMode) {
					if (PageNum >= 0 && GetLogEntryInfo.IsRecordIndexed(out int recordIndex))
						GetRecordModPlayer.hasNewRecord[recordIndex] = false; // Remove new records when navigating from a page with a new record

					BossLogPageNumber = value;
					TableOfContentsTab.Anchor = value == Page_TableOfContents ? null : -1; // Update ToC/Filter tab anchor (and hover text)
					RefreshPageContent();
				}
			}
		}

		/// <summary>
		/// Gets the EntryInfo of the entry on the selected page. Returns null if not on an entry page.
		/// </summary>
		public EntryInfo GetLogEntryInfo => PageNum >= 0 ? BossChecklist.bossTracker.SortedEntries[PageNum] : null;
		public PersonalRecords GetPlayerRecords => GetLogEntryInfo.IsRecordIndexed(out int recordIndex) ? GetRecordModPlayer.RecordsForWorld?[recordIndex] : null;
		public WorldRecord GetWorldRecords => GetLogEntryInfo.IsRecordIndexed(out int recordIndex) ? RecordSystem.WorldRecordsForWorld[recordIndex] : null;
		public BossLogModPlayer GetModPlayer => Main.LocalPlayer.GetModPlayer<BossLogModPlayer>();
		public RecordModPlayer GetRecordModPlayer => Main.LocalPlayer.GetModPlayer<RecordModPlayer>();
		public static bool AltKeyIsDown => Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.Right);

		// Navigation
		public NavigationalButton NextPageButton;
		public NavigationalButton PreviousPageButton;

		public PageCategory SelectedSubPage = PageCategory.SpawnInfo;
		public SubPageButton OpenRecordPageButton;
		public SubPageButton OpenSpawnPageButton;
		public SubPageButton OpenLootPageButton;

		// Book Tabs
		public LogTab TableOfContentsTab; // also used for the filter tab
		public LogTab CreditsTab;
		public LogTab NextBossTab;
		public LogTab NextMiniBossTab;
		public LogTab NextEventTab;
		public IndicatorPanel AltInteractionsTab;
		public IndicatorIcon InteractionIcon;
		public IndicatorPanel IndicatorTab;
		public List<IndicatorIcon> Indicators;
		public FilterIcon FilterPanel; // contains the filter buttons, (not a filter icon, but it works)
		public List<FilterIcon> FilterIcons;
		public bool FilterPanelIsOpen = false; // when true, the filter panel is visible to the user

		// Table of Contents related
		public LogScrollbar ScrollBarLeftPage; // scroll bars for table of contents lists (and other elements too)
		public LogScrollbar ScrollBarRightPage;
		public bool ProgressBarState = false; // when true, hovering over the progress bar will split up the entry percentages by mod instead of entry type

		public Dictionary<string, bool> HiddenEntriesPending = new Dictionary<string, bool>();
		private bool hiddenListOpen = false;
		public bool HiddenEntriesMode {
			get => hiddenListOpen;
			set {
				hiddenListOpen = value;
				if (value is false) {
					foreach (KeyValuePair<string, bool> hiddenState in HiddenEntriesPending) {
						if (!hiddenState.Value) {
							BossLogSystem.HiddenEntries.Remove(hiddenState.Key);
						}
						else if (!BossLogSystem.HiddenEntries.Contains(hiddenState.Key)) {
							BossLogSystem.HiddenEntries.Add(hiddenState.Key);
						}
					}
					SoundEngine.PlaySound(HiddenEntriesPending.Count > 0 ? SoundID.ResearchComplete : SoundID.MenuClose);
					HiddenEntriesPending.Clear();
				}
				else {
					SoundEngine.PlaySound(SoundID.MenuOpen);
				}
			}
		}

		// Credits related
		public static readonly Dictionary<string, string> BossChecklistModContributors = new Dictionary<string, string>() {
			{ "Jopojelly", "Creator & Owner" },
			{ "SheepishShepherd", "Co-Owner & Maintainer"},
			{ "direwolf420", "Code Contributor" },
			{ "riveren", "Boss Log Sprites"},
			{ "Orian", "Early Testing" },
			{ "Panini", "Early Server Testing" }
		};

		// Record page related
		public RecordCategory SelectedRecordCategory = RecordCategory.PreviousAttempt;
		public RecordCategory SelectedRecordComparison = RecordCategory.None; // Compare record values to one another
		public List<NavigationalButton> RecordCategoryButtons;
		public List<int> EventEntryNPCBannerIDList;

		// Spawn Info page related
		public static int SpawnItemSelected = 0;
		public static int RecipeSelected = 0;

		// Loot page related
		public static bool OtherworldMusicUnlocked = false;

		// Extra stuff
		public const string LangLog = "Mods.BossChecklist.Log"; // Short name variable to quickly access the needed string for the Log translations
		public static int headNum = -1;
		public static readonly Color faded = new Color(128, 128, 128, 128);

		// Boss Log visibiltiy helpers
		internal static bool PendingToggleBossLogUI; // Allows toggling boss log visibility from methods not run during UIScale so Main.screenWidth/etc are correct for ResetUIPositioning method
		internal static bool PendingConfigChange; // Allows configs to be updated on Log close, when needed

		internal static bool PendingHiddenEntryChange; // Updates Table of Contents hidden entries once the hidden list is closed
		
		private bool PendingPageChange; // Allows changing the page outside of the UIState without causing ordering or drawing issues.
		private int PageChangeValue;
		public int PendingPageNum {
			get => PageChangeValue;
			set {
				if (!HiddenEntriesMode) {
					PageChangeValue = value;
					PendingPageChange = true;
				}
			}
		}

		private bool bossLogVisible;
		/// <summary>
		/// Appends or removes UI elements based on the visibility status it is set to.
		/// </summary>
		public bool BossLogVisible {
			get => bossLogVisible;
			set {
				if (value) {
					Append(BookArea);
					Append(TableOfContentsTab);
					Append(FilterPanel);
					Append(IndicatorTab);
					Append(AltInteractionsTab);
					Append(CreditsTab);
					Append(NextBossTab);
					Append(NextMiniBossTab);
					Append(NextEventTab);
					Append(LeftPage);
					Append(RightPage);
				}
				else {
					RemoveChild(RightPage);
					RemoveChild(LeftPage);
					RemoveChild(NextEventTab);
					RemoveChild(NextMiniBossTab);
					RemoveChild(NextBossTab);
					RemoveChild(CreditsTab);
					RemoveChild(AltInteractionsTab);
					RemoveChild(IndicatorTab);
					RemoveChild(FilterPanel);
					RemoveChild(TableOfContentsTab);
					RemoveChild(BookArea);

					if (PendingConfigChange) {
						PendingConfigChange = false;
						BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
					}
				}
				bossLogVisible = value;
			}
		}

		/// <summary>
		/// Toggles the Boss Log's visibility state. Defaults to visible.
		/// </summary>
		/// <param name="show">The visibility state desired</param>
		public void ToggleBossLog(bool show = true) {
			// First, determine if the player has ever opened the Log before
			if (show) {
				// Reset the position of the book ui content to make sure it updates with the screen res
				BookArea.Left.Pixels = (Main.screenWidth / 2) - (BookArea.Width.Pixels / 2);
				BookArea.Top.Pixels = (Main.screenHeight / 2) - (BookArea.Height.Pixels / 2) - 6;

				// Mark the player as having opened the Log if they have not been so already
				if (!GetModPlayer.hasOpenedTheBossLog) {
					GetModPlayer.enteredWorldReset = false; // If opening for the first time, this doesn't need to occur again until the next world reset

					// When opening for the first time, open the Progression Mode prompt if enabled. Otherwise, open the Table of Contents.
					PageNum = BossChecklist.BossLogConfig.PromptDisabled ? Page_TableOfContents : Page_Prompt;
				}
				else if (GetModPlayer.enteredWorldReset) {
					// If the Log has been opened before, check for a world change.
					// This is to reset the page from what the user previously had back to the Table of Contents when entering another world.
					GetModPlayer.enteredWorldReset = false;
					PageNum = Page_TableOfContents;
				}
				else {
					RefreshPageContent(); // Otherwise, just default to the last page selected
				}

				// Update UI Element positioning before marked visible
				// This will always occur after adjusting UIScale, since the UI has to be closed in order to open up the menu options
				//ResetUIPositioning();
				Main.playerInventory = false; // hide the player inventory
			}
			else if (PageNum >= 0 && GetLogEntryInfo.IsRecordIndexed(out int selectedEntryIndex) && GetRecordModPlayer.hasNewRecord.Length > 0) {
				GetRecordModPlayer.hasNewRecord[selectedEntryIndex] = false; // If UI is closed on a new record page, remove the new record from the list
			}
			else if (PageNum == Page_TableOfContents && HiddenEntriesMode) {
				HiddenEntriesPending.Clear(); // closing the hide list in anyway other than the filter button should not save hidden statuses
				HiddenEntriesMode = false;
			}

			BossLogVisible = show; // Setting the state makes the UIElements append/remove making them visible/invisible
		}

		public override void OnInitialize() {
			BossLogResources.PreloadLogAssets();

			OpenBossLogButton = new OpenLogButton(BossLogResources.Button_Book);
			OpenBossLogButton.Left.Set(Main.screenWidth - OpenBossLogButton.Width.Pixels - 190, 0f);
			OpenBossLogButton.Top.Pixels = Main.screenHeight - OpenBossLogButton.Height.Pixels - 8;
			OpenBossLogButton.OnLeftClick += (a, b) => ToggleBossLog(true);

			BookArea = new LogPanel();
			BookArea.Width.Pixels = BossLogResources.Log_BackPanel.Value.Width;
			BookArea.Height.Pixels = BossLogResources.Log_BackPanel.Value.Height;

			TableOfContentsTab = new LogTab(BossLogResources.Log_Tab, BossLogResources.Nav_TableOfContents) {
				Id = "TableOfContents"
			};
			TableOfContentsTab.OnLeftClick += (a, b) => UpdateFilterTabPos(true);

			NextBossTab = new LogTab(BossLogResources.Log_Tab, BossLogResources.Nav_Boss) {
				Id = "Boss"
			};

			NextMiniBossTab = new LogTab(BossLogResources.Log_Tab, BossLogResources.Nav_MiniBoss) {
				Id = "MiniBoss"
			};

			NextEventTab = new LogTab(BossLogResources.Log_Tab, BossLogResources.Nav_Event) {
				Id = "Event"
			};

			CreditsTab = new LogTab(BossLogResources.Log_Tab, BossLogResources.Nav_Credits) {
				Id = "Credits",
				Anchor = -2,
				hoverText = $"{LangLog}.Tabs.Credits" // hoverText will never change, so initialize it
			};

			LeftPage = new LogPanel() {
				Id = "PageOne",
			};
			RightPage = new LogPanel() {
				Id = "PageTwo"
			};
			LeftPage.Width.Pixels = RightPage.Width.Pixels = 375; // The left and right pages have the same width and height
			LeftPage.Height.Pixels = RightPage.Height.Pixels = 480;

			PreviousPageButton = new NavigationalButton(BossLogResources.Nav_Prev, true) {
				Id = "Previous"
			};
			PreviousPageButton.Left.Pixels = 8;
			PreviousPageButton.Top.Pixels = 416;
			PreviousPageButton.OnLeftClick += PageChangerClicked;

			FilterPanel = new FilterIcon(BossLogResources.FilterPanel);
			FilterIcons = new List<FilterIcon>() {
				new FilterIcon(BossLogResources.Nav_Boss) { Id = "Boss" },
				new FilterIcon(BossLogResources.Nav_MiniBoss) { Id = "MiniBoss" },
				new FilterIcon(BossLogResources.Nav_Event) { Id = "Event" },
				new FilterIcon(BossLogResources.RequestItemTexture(ItemID.EchoMonolith)) { Id = "Hidden" },
			};

			int offsetY = 0;
			foreach (FilterIcon icon in FilterIcons) {
				icon.check = BossLogResources.Check_Check;
				icon.Top.Pixels = offsetY + 15;
				icon.Left.Pixels = 25 - (int)(icon.Width.Pixels / 2);
				FilterPanel.Append(icon);
				offsetY += 34;
			}

			Indicators = new List<IndicatorIcon>() {
				new IndicatorIcon(BossLogResources.Indicator_OnlyBosses) { Id = "OnlyBosses" },
				new IndicatorIcon(BossLogResources.Indicator_Manual) { Id = "Manual" },
				new IndicatorIcon(BossLogResources.Indicator_Progression) { Id = "Progression" },
			};
			IndicatorTab = new IndicatorPanel(Indicators.Count) { Id = "Configurations" };

			int offsetX = 2;
			foreach (IndicatorIcon icon in Indicators) {
				icon.Left.Pixels = 10 + offsetX;
				icon.Top.Pixels = 8;
				IndicatorTab.Append(icon);
				offsetX += 20;
			}

			InteractionIcon = new IndicatorIcon(BossLogResources.Indicator_Interaction);
			AltInteractionsTab = new IndicatorPanel(1) { Id = "Interactions" };
			InteractionIcon.Left.Pixels = (int)(AltInteractionsTab.Width.Pixels / 2 - InteractionIcon.Width.Pixels / 2);
			InteractionIcon.Top.Pixels = 8;
			AltInteractionsTab.Append(InteractionIcon);

			NextPageButton = new NavigationalButton(BossLogResources.Nav_Next, true) {
				Id = "Next"
			};
			NextPageButton.Left.Pixels = RightPage.Width.Pixels - NextPageButton.Width.Pixels - 12;
			NextPageButton.Top.Pixels = 416;
			NextPageButton.OnLeftClick += PageChangerClicked;
			RightPage.Append(NextPageButton);

			OpenRecordPageButton = new SubPageButton(BossLogResources.Nav_SubPage, PageCategory.Records);
			OpenRecordPageButton.Left.Pixels = (int)RightPage.Width.Pixels / 2 - BossLogResources.Nav_SubPage.Value.Width / 2;
			OpenRecordPageButton.Top.Pixels = 5 + BossLogResources.Nav_SubPage.Value.Height + 10;

			OpenSpawnPageButton = new SubPageButton(BossLogResources.Nav_SubPage, PageCategory.SpawnInfo);
			OpenSpawnPageButton.Left.Pixels = (int)RightPage.Width.Pixels / 2 - BossLogResources.Nav_SubPage.Value.Width - 8;
			OpenSpawnPageButton.Top.Pixels = 5;

			OpenLootPageButton = new SubPageButton(BossLogResources.Nav_SubPage, PageCategory.LootAndCollectibles);
			OpenLootPageButton.Left.Pixels = (int)RightPage.Width.Pixels / 2 + 8;
			OpenLootPageButton.Top.Pixels = 5;

			// Record Type navigation buttons
			RecordCategoryButtons = new List<NavigationalButton>();
			for (int value = 0; value < 4; value++) {
				RecordCategoryButtons.Add(
					new NavigationalButton(BossLogResources.Nav_Record_Category[value], true) {
						Record_Anchor = (RecordCategory)value,
						hoverText = $"{LangLog}.Records.Category.{(RecordCategory)value}"
					}
				);
			}

			EventEntryNPCBannerIDList = new List<int>();

			// scroll one currently only appears for the table of contents, so its fields can be set here
			ScrollBarLeftPage = new LogScrollbar();
			ScrollBarLeftPage.SetView(100f, 1000f);
			ScrollBarLeftPage.Top.Pixels = 50f;
			ScrollBarLeftPage.Left.Pixels = -18;
			ScrollBarLeftPage.Height.Set(-24f, 0.75f);
			ScrollBarLeftPage.HAlign = 1f;

			// scroll two is used in more areas, such as the display spawn info message box, so its fields are set when needed
			ScrollBarRightPage = new LogScrollbar();
		}

		public override void Update(GameTime gameTime) {
			if (PendingToggleBossLogUI) {
				PendingToggleBossLogUI = false;
				ToggleBossLog(!BossLogVisible);
			}
			if (PendingPageChange) {
				PendingPageChange = false;
				PageNum = PageChangeValue;
			}
			this.AddOrRemoveChild(OpenBossLogButton, Main.playerInventory);
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

			if (headNum != -1) {
				EntryInfo entry = BossChecklist.bossTracker.SortedEntries[headNum];
				bool isNotProgressed = BossChecklist.BossLogConfig.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext;
				int headOffset = 0;
				foreach (Asset<Texture2D> headIcon in entry.headIconTextures()) {
					Texture2D headShown = isNotProgressed ? TextureAssets.NpcHead[0].Value : headIcon.Value;
					spriteBatch.Draw(headShown, new Vector2(Main.mouseX + 15 + headOffset, Main.mouseY + 15), MaskBoss(entry));
					headOffset += headShown.Width + 2;
				}
			}
		}

		/// <summary>
		/// Resets the positioning of the Boss Log's common UI elements.
		/// This includes the main book area, the page areas, and all book tabs.
		/// </summary>
		private void ResetUIPositioning() {
			LeftPage.Left.Pixels = BookArea.Left.Pixels + 20;
			LeftPage.Top.Pixels = BookArea.Top.Pixels + 12;
			RightPage.Left.Pixels = BookArea.Left.Pixels - 15 + BookArea.Width.Pixels - RightPage.Width.Pixels;
			RightPage.Top.Pixels = BookArea.Top.Pixels + 12;

			int offsetY = 50;

			// ToC/Filter Tab and Credits Tab never flips to the other side, just disappears when on said page
			TableOfContentsTab.Top.Pixels = BookArea.Top.Pixels + offsetY;
			IndicatorTab.Left.Pixels = RightPage.Left.Pixels + RightPage.Width.Pixels - 25 - IndicatorTab.Width.Pixels;
			IndicatorTab.Top.Pixels = RightPage.Top.Pixels - IndicatorTab.Height.Pixels - 6;
			AltInteractionsTab.Left.Pixels = IndicatorTab.Left.Pixels - AltInteractionsTab.Width.Pixels - 2;
			AltInteractionsTab.Top.Pixels = RightPage.Top.Pixels - AltInteractionsTab.Height.Pixels - 6;
			AltInteractionsTab.hoverText = GenerateInteractionHoverText();
			UpdateFilterTabPos(false); // Update filter tab visibility
			CreditsTab.Left.Pixels = BookArea.Left.Pixels + BookArea.Width.Pixels - 12;
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (NextBossTab.Height.Pixels * 4);

			// Reset book tabs Y positioning after BookArea adjusted
			NextBossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (NextBossTab.Height.Pixels * 1);
			NextMiniBossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (NextBossTab.Height.Pixels * 2);
			NextEventTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (NextBossTab.Height.Pixels * 3);
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (NextBossTab.Height.Pixels * 4);

			// Update the navigation tabs to the proper positions
			NextBossTab.Left.Pixels = BookArea.Left.Pixels + (NextBossTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
			NextMiniBossTab.Left.Pixels = BookArea.Left.Pixels + (NextMiniBossTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
			NextEventTab.Left.Pixels = BookArea.Left.Pixels + (NextEventTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
		}

		/// <summary>
		/// Toggles the visibility state of the filter panel. This can occur when the tab is clicked or if the page is changed.
		/// </summary>
		/// <param name="tabClicked"></param>
		private void UpdateFilterTabPos(bool tabClicked) {
			if (tabClicked && PageNum != Page_TableOfContents)
				return; // Filter tab position should not change if not on the Table of Contents page when clicked

			if (HiddenEntriesMode)
				return; // The Hidden List should be confirmed and closed before being able to close the filters tab

			if (PageNum != Page_TableOfContents) {
				FilterPanelIsOpen = false; // If the page is not on the Table of Contents, the filters tab should be in the closed position
			}
			else if (tabClicked) {
				FilterPanelIsOpen = !FilterPanelIsOpen;
			}

			if (FilterPanelIsOpen) {
				FilterPanel.Top.Pixels = TableOfContentsTab.Top.Pixels;
				TableOfContentsTab.Left.Pixels = BookArea.Left.Pixels - 20 - FilterPanel.Width.Pixels;
				FilterPanel.Left.Pixels = TableOfContentsTab.Left.Pixels + TableOfContentsTab.Width.Pixels;
				FilterIcons.ForEach(x => x.UpdateFilterIcon()); // Update filter display state when the filter panel is opened
			}
			else {
				TableOfContentsTab.Left.Pixels = BookArea.Left.Pixels - 20;
				FilterPanel.Top.Pixels = -5000; // throw offscreen
			}
		}

		/// <summary>
		/// While in debug mode, users are able to reset their records of a specific boss by alt and right-clicking the recordnavigation button
		/// </summary>
		private void ResetStats() {
			if (!BossChecklist.BossLogConfig.Debug.EnabledResetOptions || SelectedSubPage != PageCategory.Records || GetPlayerRecords is null)
				return; // must be on a valid record page and must have the reset records config enabled

			if (!AltKeyIsDown)
				return; // player must be holding alt

			GetPlayerRecords.ResetStats(SelectedRecordCategory); // TODO: Fix this and make a reset for all records of a boss
			RefreshPageContent();  // update page to show changes
		}

		private void RemovePlayerFromWorldRecord() {
			// TODO: Localhost can remove players from record holders list
		}

		/// <summary>
		/// While in debug mode, players will be able to remove obtained items from their player save data using the right-click button on the selected item slot.
		/// </summary>
		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			if (!BossChecklist.BossLogConfig.Debug.EnabledResetOptions || SelectedSubPage != PageCategory.LootAndCollectibles)
				return; // do not do anything if the loot page isn't active

			if (listeningElement is not LogItemSlot slot || !AltKeyIsDown)
				return; // player must be holding alt over an itemslot to remove items

			// Note: items removed are removed from ALL boss loot pages retroactively
			GetModPlayer.BossItemsCollected.Remove(new ItemDefinition(slot.item.type));
			RefreshPageContent(); // update page to show changes
		}

		/// <summary>
		/// Cycles through the spawn items on an entry.
		/// Also cycles through recipes for spawn items.
		/// </summary>
		private void ChangeSpawnItem(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not NavigationalButton button)
				return;

			string id = button.Id;
			if (id == "NextItem") {
				SpawnItemSelected++;
				RecipeSelected = 0;
			}
			else if (id == "PrevItem") {
				SpawnItemSelected--;
				RecipeSelected = 0;
			}
			else if (id.Contains("CycleItem")) {
				int index = id.IndexOf('_');
				if (RecipeSelected == Convert.ToInt32(id.Substring(index + 1)) - 1) {
					RecipeSelected = 0;
				}
				else {
					RecipeSelected++;
				}
			}
			RefreshPageContent();
		}

		/// <summary>
		/// Sets up the content needed for the Progression Mode prompt, including text boxes and buttons.
		/// </summary>
		private void OpenProgressionModePrompt() {
			BossLogPageNumber = Page_Prompt; // make sure the page number is updated directly (using PageNum will trigger the page set up)
			ResetUIPositioning(); // Updates ui elements and tabs to be properly positioned in relation the the new pagenum
			LeftPage.RemoveAllChildren(); // remove all content from both pages before appending new content for the prompt
			RightPage.RemoveAllChildren();

			// create a text box for the progression mode description
			FittedTextPanel textBox = new FittedTextPanel($"{LangLog}.ProgressionMode.Description");
			textBox.Width.Pixels = LeftPage.Width.Pixels - 30;
			textBox.Height.Pixels = LeftPage.Height.Pixels - 70;
			textBox.Left.Pixels = 10;
			textBox.Top.Pixels = 60;
			LeftPage.Append(textBox);

			LogUIElement selectEnable = new LogUIElement(BossLogResources.Content_PromptSlot.Value) {
				hoverText = $"{LangLog}.ProgressionMode.SelectEnable"
			};
			selectEnable.Left.Pixels = RightPage.Width.Pixels / 2 - BossLogResources.Content_PromptSlot.Value.Width - 10;
			selectEnable.Top.Pixels = 125;
			selectEnable.OnLeftClick += (a, b) => SelectProgressionModeState(true);
			selectEnable.OnMouseOver += (a, b) => { selectEnable.assetColor = BossChecklist.BossLogConfig.BossLogColor; };
			selectEnable.OnMouseOut += (a, b) => { selectEnable.assetColor = Color.White; };

			UIImage selectEnableImage = new UIImage(BossLogResources.Content_ProgressiveOn);
			selectEnableImage.Left.Pixels = selectEnable.Width.Pixels / 2 - selectEnableImage.Width.Pixels / 2;
			selectEnableImage.Top.Pixels = selectEnable.Height.Pixels / 2 - selectEnableImage.Height.Pixels / 2;

			selectEnable.Append(selectEnableImage);
			RightPage.Append(selectEnable);

			LogUIElement selectDisable = new LogUIElement(BossLogResources.Content_PromptSlot.Value) {
				hoverText = $"{LangLog}.ProgressionMode.SelectDisable"
			};
			selectDisable.Left.Pixels = RightPage.Width.Pixels / 2 + 10;
			selectDisable.Top.Pixels = 125;
			selectDisable.OnLeftClick += (a, b) => SelectProgressionModeState(false);
			selectDisable.OnMouseOver += (a, b) => { selectDisable.assetColor = BossChecklist.BossLogConfig.BossLogColor; };
			selectDisable.OnMouseOut += (a, b) => { selectDisable.assetColor = Color.White; };

			UIImage selectDisableImage = new UIImage(BossLogResources.Content_ProgressiveOff);
			selectDisableImage.Left.Pixels = selectDisable.Width.Pixels / 2 - selectDisableImage.Width.Pixels / 2;
			selectDisableImage.Top.Pixels = selectDisable.Height.Pixels / 2 - selectDisableImage.Height.Pixels / 2;

			selectDisable.Append(selectDisableImage);
			RightPage.Append(selectDisable);

			LogUIElement togglePrompt = new LogUIElement(BossLogResources.Content_RecordSlot.Value);
			togglePrompt.Left.Pixels = 25;
			togglePrompt.Top.Pixels = 125 + BossLogResources.Content_PromptSlot.Value.Height + 25;
			togglePrompt.OnMouseOver += (a, b) => { selectDisable.assetColor = BossChecklist.BossLogConfig.BossLogColor; };
			togglePrompt.OnMouseOut += (a, b) => { selectDisable.assetColor = Color.White; };

			UIImage togglePromptCheck = new UIImage(BossLogResources.Check_Box);
			togglePromptCheck.Left.Pixels = 15;
			togglePromptCheck.Top.Pixels = togglePrompt.Height.Pixels / 2 - togglePromptCheck.Height.Pixels / 2;
			UIImage PromptCheck = new UIImage(BossChecklist.BossLogConfig.PromptDisabled ? BossLogResources.Check_Check : BossLogResources.Check_X);
			togglePrompt.OnLeftClick += (a, b) => DisablePromptMessage(PromptCheck); // toggling the prompt check will update the child image

			FittedTextPanel textOptions = new FittedTextPanel($"{LangLog}.ProgressionMode.DisablePrompt");
			textOptions.Width.Pixels = togglePrompt.Width.Pixels - (togglePromptCheck.Left.Pixels + togglePromptCheck.Width.Pixels + 15);
			textOptions.Height.Pixels = togglePrompt.Height.Pixels / 2;
			textOptions.Left.Pixels = togglePromptCheck.Left.Pixels + togglePromptCheck.Width.Pixels;
			textOptions.Top.Pixels = 5;
			textOptions.PaddingTop = 0;
			textOptions.PaddingLeft = 15;
			togglePrompt.Append(textOptions);

			togglePromptCheck.Append(PromptCheck);
			togglePrompt.Append(togglePromptCheck);
			RightPage.Append(togglePrompt);
		}

		/// <summary>
		/// Closes the UI and opens the configs to allow the player to customize the Progression Mode options to their liking.
		/// </summary>
		public void CloseAndConfigure() {
			ToggleBossLog(false);
			PageNum = Page_TableOfContents;

			// A whole bunch of janky code to show the config and scroll down.
			try {
				IngameFancyUI.CoverNextFrame();
				Main.playerInventory = false;
				Main.editChest = false;
				Main.npcChatText = "";
				Main.inFancyUI = true;

				var modConfigFieldInfo = Assembly.GetEntryAssembly().GetType("Terraria.ModLoader.UI.Interface").GetField("modConfig", BindingFlags.Static | BindingFlags.NonPublic);
				var modConfig = (UIState)modConfigFieldInfo.GetValue(null);

				Type UIModConfigType = Assembly.GetEntryAssembly().GetType("Terraria.ModLoader.Config.UI.UIModConfig");
				var SetModMethodInfo = UIModConfigType.GetMethod("SetMod", BindingFlags.Instance | BindingFlags.NonPublic);

				//Interface.modConfig.SetMod("BossChecklist", BossChecklist.BossLogConfig);
				SetModMethodInfo.Invoke(modConfig, new object[] { BossChecklist.instance, BossChecklist.BossLogConfig });
				Main.InGameUI.SetState(modConfig);

				//private UIList mainConfigList;
				//var mainConfigListFieldInfo = UIModConfigType.GetField("mainConfigList", BindingFlags.Instance | BindingFlags.NonPublic);
				//UIList mainConfigList = (UIList)mainConfigListFieldInfo.GetValue(modConfig);

				var uIScrollbarFieldInfo = UIModConfigType.GetField("uIScrollbar", BindingFlags.Instance | BindingFlags.NonPublic);
				UIScrollbar uIScrollbar = (UIScrollbar)uIScrollbarFieldInfo.GetValue(modConfig);
				uIScrollbar.GoToBottom();

				//mainConfigList.Goto(delegate (UIElement element) {
				//	if(element is UISortableElement sortableElement && sortableElement.Children.FirstOrDefault() is Terraria.ModLoader.Config.UI.ConfigElement configElement) {
				//		return configElement.TextDisplayFunction().IndexOf("Test", StringComparison.OrdinalIgnoreCase) != -1;
				//	}
				//});
			}
			catch (Exception) {
				BossChecklist.instance.Logger.Warn("Force opening ModConfig menu failed, code update required");
			}
		}

		/// <summary>
		/// Fully enables or disables Progression Mode based on option selected and redirects the player to the Table of Contents.
		/// </summary>
		private void SelectProgressionModeState(bool enabled) {
			BossChecklist.BossLogConfig.ProgressiveChecklist = enabled;
			if (BossChecklist.BossLogConfig.ProgressiveChecklist)
				BossChecklist.BossLogConfig.DrawNextMark = true;

			PendingConfigChange = true; // save the option selected before proceeding
			BossChecklist.BossLogConfig.UpdateIndicators();

			PageNum = Page_TableOfContents; // switch page to Table of Contents when clicked
		}

		/// <summary>
		/// Toggles whether the prompt will show on future characters. This can still be changed under the configs.
		/// </summary>
		private void DisablePromptMessage(UIImage element) {
			BossChecklist.BossLogConfig.PromptDisabled = !BossChecklist.BossLogConfig.PromptDisabled;
			PendingConfigChange = true;
			element.SetImage(BossChecklist.BossLogConfig.PromptDisabled ? BossLogResources.Check_Check : BossLogResources.Check_X);
		}

		/// <summary>
		/// Handles the logic behind clicking the next/prev navigation buttons, and thus "turning the page".
		/// </summary>
		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not NavigationalButton button)
				return;

			// Calculate what page the Log needs to update to
			List<EntryInfo> BossList = BossChecklist.bossTracker.SortedEntries;
			int NewPageValue = PageNum;
			if (button.Id == "Next") {
				NewPageValue = NewPageValue < BossList.Count - 1 ? NewPageValue + 1 : Page_Credits;
			}
			else {
				NewPageValue = NewPageValue >= 0 ? NewPageValue - 1 : BossList.Count - 1;
			}

			// Figure out next page based on the new page number value
			// If the page is hidden or unavailable, keep moving till its not or until page reaches the end
			// Also check for "Only Bosses" navigation
			while (NewPageValue >= 0) {
				if (NewPageValue >= 0 && BossList[NewPageValue].VisibleOnPageContent())
					break;

				// same calulation as before, but repeated until a valid page is selected
				if (button.Id == "Next") {
					NewPageValue = NewPageValue < BossList.Count - 1 ? NewPageValue + 1 : Page_Credits;
				}
				else {
					NewPageValue = NewPageValue >= 0 ? NewPageValue - 1 : BossList.Count - 1;
				}
			}

			PageNum = NewPageValue; // Once a valid page is found, change the page.
		}

		/// <summary>
		/// Restructures all page content and elements without changing the page or subpage.
		/// </summary>
		public void RefreshPageContent() {
			if (PageNum == Page_Prompt) {
				OpenProgressionModePrompt();
				return; // If the page is somehow the prompt, redirect to the open prompt method
			}

			NextBossTab.Anchor = FindNextEntry(EntryType.Boss); // Updates the 'next' entries for the tabs and next checkmark
			NextMiniBossTab.Anchor = FindNextEntry(EntryType.MiniBoss);
			NextEventTab.Anchor = FindNextEntry(EntryType.Event);
			
			ResetUIPositioning(); // Repositions common ui elements when the UI is updated
			ResetBothPages(); // Reset the content of both pages before appending new content for the page

			// Set up the designated page content
			if (PageNum == Page_TableOfContents) {
				UpdateTableofContents();
			}
			else if (PageNum == Page_Credits) {
				UpdateCredits();
			}
			else {
				if (SelectedSubPage == PageCategory.Records) {
					OpenRecord();
				}
				else if (SelectedSubPage == PageCategory.SpawnInfo) {
					OpenSpawn();
				}
				else if (SelectedSubPage == PageCategory.LootAndCollectibles) {
					OpenLoot();
				}
			}
		}

		/// <summary>
		/// Updates the hovertext for the key combinations (ex. Alt+Right-Click element) that are available to the current page.
		/// </summary>
		private string GenerateInteractionHoverText() {
			string interactions = null;
			string HiddenTexts = LangLog + ".HintTexts";
			if (PageNum == Page_TableOfContents) {
				if (HiddenEntriesMode) {
					interactions =
					Language.GetTextValue($"{HiddenTexts}.HideEntry") +
					(BossChecklist.BossLogConfig.Debug.EnabledResetOptions ? "\n" + Language.GetTextValue($"{HiddenTexts}.ClearHidden") : "");
				}
				else {
					interactions =
					Language.GetTextValue($"{HiddenTexts}.MarkEntry") +
					(BossChecklist.BossLogConfig.Debug.EnabledResetOptions ? "\n" + Language.GetTextValue($"{HiddenTexts}.ClearMarked") : "");
				}
			}
			else if (PageNum >= 0 && BossChecklist.BossLogConfig.Debug.EnabledResetOptions) {
				if (SelectedSubPage == PageCategory.Records && GetLogEntryInfo.type == EntryType.Boss) {
					interactions =
						Language.GetTextValue($"{HiddenTexts}.ClearAllRecords") + "\n" +
						Language.GetTextValue($"{HiddenTexts}.ClearRecord");
				}
				else if (SelectedSubPage == PageCategory.LootAndCollectibles) {
					interactions =
						Language.GetTextValue($"{HiddenTexts}.RemoveItem") + "\n" +
						Language.GetTextValue($"{HiddenTexts}.ClearItems");
				}
			}
			return BossChecklist.BossLogConfig.ShowInteractionTooltips ? interactions : null;
		}

		/// <summary>
		/// Clears page content and replaces navigational elements.
		/// </summary>
		private void ResetBothPages() {
			LeftPage.RemoveAllChildren(); // remove all elements from the pages
			RightPage.RemoveAllChildren();

			// Replace all of the page's navigational buttons
			if (PageNum != Page_Credits) {
				RightPage.Append(NextPageButton); // Next page button can appear on any page except the Credits
			}
			if (PageNum != Page_TableOfContents) {
				LeftPage.Append(PreviousPageButton); // Prev page button can appear on any page except the Table of Contents
			}

			if (PageNum >= 0) {
				EventEntryNPCBannerIDList.Clear(); // clear event banner list
				if (BossChecklist.BossLogConfig.Debug.AccessInternalNames && GetLogEntryInfo.modSource != "Unknown") {
					NavigationalButton keyButton = new NavigationalButton(BossLogResources.Content_BossKey, true) {
						Id = "CopyKey",
						hoverText = $"{Language.GetTextValue($"{LangLog}.EntryPage.CopyKey")}:\n{GetLogEntryInfo.Key}"
					};
					keyButton.Left.Pixels = 5;
					keyButton.Top.Pixels = 55;
					LeftPage.Append(keyButton);
				}

				// Entry pages need to have the category pages set up, but only for entries fully implemented
				if (GetLogEntryInfo.modSource != "Unknown") {
					RightPage.Append(OpenRecordPageButton);
					RightPage.Append(OpenSpawnPageButton);
					RightPage.Append(OpenLootPageButton);
					//lootButton.isLocked = !GetLogEntryInfo.IsAutoDownedOrMarked;
				}
				else {
					// Old mod calls are no longer supported and will not add entries
					// if somehow the entry has an unknown source, make a panel to show something went wrong
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 3;
					RightPage.Append(brokenPanel);

					FittedTextPanel brokenDisplay = new FittedTextPanel($"{LangLog}.EntryPage.LogFeaturesNotAvailable");
					brokenDisplay.Height.Pixels = 200;
					brokenDisplay.Width.Pixels = 340;
					brokenDisplay.Top.Pixels = -12;
					brokenDisplay.Left.Pixels = -15;
					brokenPanel.Append(brokenDisplay);
				}
			}
		}

		/// <summary>
		/// Sets up the content needed for the table of contents,
		/// including the list of pre-hardmode and hardmode entries and
		/// the progress bar of defeated entries.
		/// </summary>
		private void UpdateTableofContents() {
			if (GetModPlayer.hasOpenedTheBossLog is false)
				GetModPlayer.hasOpenedTheBossLog = true; // This will only ever happen once per character

			UIList entriesListPreHardmode = new UIList(); // lists for pre-hardmode and hardmode entries
			UIList entriesListHardmode = new UIList();

			entriesListPreHardmode.Left.Pixels = 4;
			entriesListHardmode.Left.Pixels = 19;

			entriesListPreHardmode.Top.Pixels = entriesListHardmode.Top.Pixels = 44;
			entriesListPreHardmode.Width.Pixels = entriesListHardmode.Width.Pixels = LeftPage.Width.Pixels - 60;
			entriesListPreHardmode.Height.Pixels = entriesListHardmode.Height.Pixels = LeftPage.Height.Pixels - 136;
			entriesListPreHardmode.PaddingTop = entriesListHardmode.PaddingTop = 5;

			// Pre-Hard Mode List Title
			string title = Language.GetTextValue($"{LangLog}.TableOfContents.PreHardmode");
			UIText leftPageTitle = new UIText(title, 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			leftPageTitle.Top.Pixels = 18;
			leftPageTitle.Left.Pixels = (int)((LeftPage.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			LeftPage.Append(leftPageTitle);

			// Hard Mode List Title
			title = Language.GetTextValue($"{LangLog}.TableOfContents.Hardmode");
			UIText rightPageTitle = new UIText(title, 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			rightPageTitle.Top.Pixels = 18;
			rightPageTitle.Left.Pixels = (int)((RightPage.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			RightPage.Append(rightPageTitle);

			foreach (EntryInfo entry in BossChecklist.bossTracker.SortedEntries) {
				entry.hidden = BossLogSystem.HiddenEntries.Contains(entry.Key);

				if (!entry.VisibleOnChecklist())
					continue; // If the boss should not be visible on the Table of Contents, skip the entry in the list

				// Setup display name. Show "???" if unavailable and Silhouettes are turned on
				string displayName = entry.DisplayName;
				if (BossChecklist.BossLogConfig.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext)
					displayName = "???";

				// The first entry that isnt downed to have a nextCheck will set off the next check for the rest
				// Entries that ARE downed will still be green due to the ordering of colors within the draw method
				// Update marked downs. If the boss is actually downed, remove the mark.
				if (BossChecklist.BossLogConfig.AutomaticChecklist && entry.MarkedAsDowned) {
					displayName += "*";
					/*
					if (WorldAssist.MarkedEntries.Contains(entry.Key) && entry.downed()) {
						WorldAssist.MarkedEntries.Remove(entry.Key); // TODO: move this to a more apprpriate method
						Networking.RequestMarkedEntryUpdate(entry.Key, entry.MarkedAsDowned);
					}
					*/
				}

				bool allLoot = BossChecklist.BossLogConfig.LootCheckVisibility;
				bool allCollect = BossChecklist.BossLogConfig.LootCheckVisibility;
				if (BossChecklist.BossLogConfig.LootCheckVisibility) {
					// Loop through player saved loot and boss loot to see if every item was obtained
					foreach (int loot in entry.lootItemTypes) {
						if (loot == entry.TreasureBag)
							continue;

						int index = entry.loot.FindIndex(x => x.itemId == loot);
						if (index != -1 && entry.loot[index].conditions is not null) {
							bool isCorruptionLocked = WorldGen.crimson && entry.loot[index].conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
							bool isCrimsonLocked = !WorldGen.crimson && entry.loot[index].conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

							if (isCorruptionLocked || isCrimsonLocked)
								continue; // Skips items that are dropped within the opposing world evil
						}

						Item checkItem = ContentSamples.ItemsByType[loot];
						if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
							continue; // Skip items that are expert exclusive if not in an expert world

						if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
							continue; // Skip items that are master exclusive if not in an master world

						// If the item index is not found, end the loop and set allLoot to false
						// If this never occurs, the user successfully obtained all the items!
						if (!GetModPlayer.BossItemsCollected.Contains(new ItemDefinition(loot))) {
							allLoot = false; // If the item is not located in the player's obtained list, allLoot must be false
							break; // end further item checking
						}
					}

					if (entry.collectibles.Count == 0) {
						allCollect = allLoot; // If no collectible items were setup, consider it false until all loot has been obtained
					}
					else {
						foreach (int collectible in entry.collectibles.Keys.ToList()) {
							if (collectible <= 0)
								continue; // Skips empty items

							if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && !entry.CollectibleDrops.Contains(collectible))
								continue; // If the OnlyCheckDroppedLoot config is enabled, skip collectible items that aren't also drops

							Item checkItem = ContentSamples.ItemsByType[collectible];
							if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
								continue; // Skip items that are expert exclusive if not in an expert world

							if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
								continue; // Skip items that are master exclusive if not in an master world

							if (!OtherworldMusicUnlocked && BossChecklist.bossTracker.otherWorldMusicBoxTypes.Contains(checkItem.type))
								continue; // skip other worldly music boxes if the user has not unlocked the ability to record them

							if (collectible == entry.TreasureBag)
								continue; // Skip the treasure bag as it shouldn't be counted towards the completion check

							if (!GetModPlayer.BossItemsCollected.Contains(new ItemDefinition(collectible))) {
								allCollect = false; // If the item is not located in the player's obtained list, allCollect must be false
								break; // end further item checking
							}
						}
					}
				}

				Color textColor = Color.PapayaWhip; // default color when ColoredBossText is false
				if (HiddenEntriesMode) {
					textColor = entry.hidden ? Color.DimGray : Color.DarkGray;
				}
				else if ((!entry.available() && !entry.IsAutoDownedOrMarked) || entry.hidden) {
					textColor = Color.DimGray; // Hidden or Unavailable entry text color takes priority over all other text color alterations
				}
				else if (BossChecklist.BossLogConfig.ColoredBossText) {
					textColor = entry.IsAutoDownedOrMarked ? Colors.RarityGreen : Colors.RarityRed;
				}

				TableOfContents listedEntry = new TableOfContents(entry.GetIndex, displayName, textColor, allLoot, allCollect) {
					PaddingTop = 5,
					PaddingLeft = entry.progression <= BossTracker.WallOfFlesh ? 32 : 22
				};

				if (entry.progression <= BossTracker.WallOfFlesh) {
					entriesListPreHardmode.Add(listedEntry);
				}
				else {
					entriesListHardmode.Add(listedEntry);
				}
			}

			LeftPage.Append(entriesListPreHardmode);
			if (entriesListPreHardmode.Count > 13) {
				ScrollBarLeftPage.SetView(100f, 1000f);
				ScrollBarLeftPage.Top.Pixels = 50f;
				ScrollBarLeftPage.Left.Pixels = -18;
				ScrollBarLeftPage.Height.Set(-24f, 0.75f);
				ScrollBarLeftPage.HAlign = 1f;

				LeftPage.Append(ScrollBarLeftPage);
				entriesListPreHardmode.SetScrollbar(ScrollBarLeftPage);
			}
			RightPage.Append(entriesListHardmode);
			if (entriesListHardmode.Count > 13) {
				ScrollBarRightPage.SetView(100f, 1000f);
				ScrollBarRightPage.Top.Pixels = 50f;
				ScrollBarRightPage.Left.Pixels = -13;
				ScrollBarRightPage.Height.Set(-24f, 0.75f);
				ScrollBarRightPage.HAlign = 1f;

				RightPage.Append(ScrollBarRightPage);
				entriesListHardmode.SetScrollbar(ScrollBarRightPage);
			}

			if (BossChecklist.BossLogConfig.ShowProgressBars) {
				// Order matters here
				ProgressBar preHardmodeProgressBar = new ProgressBar(false);
				preHardmodeProgressBar.Left.Pixels = (int)(PreviousPageButton.Left.Pixels + PreviousPageButton.Width.Pixels + 10);
				preHardmodeProgressBar.Height.Pixels = 14;
				preHardmodeProgressBar.Top.Pixels = (int)(PreviousPageButton.Top.Pixels + (PreviousPageButton.Height.Pixels / 2) - (preHardmodeProgressBar.Height.Pixels / 2));
				preHardmodeProgressBar.Width.Pixels = (int)(LeftPage.Width.Pixels - (preHardmodeProgressBar.Left.Pixels * 2));

				// Order matters here
				ProgressBar hardmodeProgressBar = new ProgressBar(true);
				hardmodeProgressBar.Left.Pixels = NextPageButton.Left.Pixels - 10 - preHardmodeProgressBar.Width.Pixels;
				hardmodeProgressBar.Height.Pixels = 14;
				hardmodeProgressBar.Top.Pixels = preHardmodeProgressBar.Top.Pixels;
				hardmodeProgressBar.Width.Pixels = preHardmodeProgressBar.Width.Pixels;

				LeftPage.Append(preHardmodeProgressBar);
				if (!BossChecklist.BossLogConfig.ProgressiveChecklist || Main.hardMode)
					RightPage.Append(hardmodeProgressBar);
			}
		}

		/// <summary>
		/// Sets up the content need for the credits page,
		/// listing off all mod contributors as well as the mods using the updated mod calls.
		/// </summary>
		private void UpdateCredits() {
			// Developers Title
			string title = Language.GetTextValue($"{LangLog}.Credits.Devs");
			UIText leftPageTitle = new UIText(title, 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			leftPageTitle.Left.Pixels = (int)((LeftPage.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			LeftPage.Append(leftPageTitle);

			// Registered Mods Title
			title = Language.GetTextValue($"{LangLog}.Credits.Mods");
			UIText rightPageTitle = new UIText(title, 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			rightPageTitle.Top.Pixels = 18;
			rightPageTitle.Left.Pixels = (int)((RightPage.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			RightPage.Append(rightPageTitle);

			// Registered Mods subtitle
			title = Language.GetTextValue($"{LangLog}.Credits.Notice");
			UIText subtitle = new UIText(title) {
				TextColor = Color.Salmon
			};
			subtitle.Left.Pixels = (int)((RightPage.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(title).X / 2));
			subtitle.Top.Pixels = 56;
			RightPage.Append(subtitle);

			// Developers Display
			UIList creditList = new UIList();
			creditList.Width.Pixels = BossLogResources.Credit_DevSlot.Value.Width;
			creditList.Height.Pixels = BossLogResources.Credit_DevSlot.Value.Height * 4 + 20;
			creditList.Left.Pixels = (int)(LeftPage.Width.Pixels / 2 - BossLogResources.Credit_DevSlot.Value.Width / 2) - 8;
			creditList.Top.Pixels = 60;
			foreach (KeyValuePair<string, string> user in BossChecklistModContributors) {
				creditList.Add(new ContributorCredit(BossLogResources.Credit_DevSlot, BossLogResources.Credit_Devs[BossChecklistModContributors.Keys.ToList().IndexOf(user.Key)], user.Key, user.Value));
			}
			LeftPage.Append(creditList);

			ScrollBarLeftPage.SetView(10f, 1000f);
			ScrollBarLeftPage.Top.Pixels = 80;
			ScrollBarLeftPage.Left.Pixels = -8;
			ScrollBarLeftPage.Height.Set(-60f, 0.75f);
			ScrollBarLeftPage.HAlign = 1f;
			creditList.SetScrollbar(ScrollBarLeftPage);
			LeftPage.Append(ScrollBarLeftPage); // scroll bar for developers

			// Registered Mods Display
			UIList registeredModsList = new UIList();
			registeredModsList.Width.Pixels = BossLogResources.Credit_ModSlot.Value.Width;
			registeredModsList.Height.Pixels = BossLogResources.Credit_ModSlot.Value.Height * 3 + 15;
			registeredModsList.Left.Pixels = (int)(RightPage.Width.Pixels / 2 - BossLogResources.Credit_ModSlot.Value.Width / 2);
			registeredModsList.Top.Pixels = 85;
			if (BossChecklist.bossTracker.RegisteredMods.Count > 0) {
				foreach (string mod in BossChecklist.bossTracker.RegisteredMods.Keys) {
					registeredModsList.Add(new ContributorCredit(BossLogResources.Credit_ModSlot, mod));
				}
			}
			else {
				// if none of the loaded mods have registered an entry, convey this to the user
				string NoModsTitle = Language.GetTextValue($"{LangLog}.Credits.ModsEmpty");
				registeredModsList.Add(new ContributorCredit(BossLogResources.Credit_NoMods, NoModsTitle, "") { Id = "NoMods" });
			}

			// add a slot that tells mod developers they can register their own mods
			string RegisterTitle = Language.GetTextValue($"{LangLog}.Credits.Register");
			string RegisterDescription = Language.GetTextValue($"{LangLog}.Credits.Learn");
			registeredModsList.Add(new ContributorCredit(BossLogResources.Credit_Register, RegisterTitle, RegisterDescription) { Id = "Register" });
			RightPage.Append(registeredModsList);

			ScrollBarRightPage.SetView(10f, 1000f);
			ScrollBarRightPage.Top.Pixels = 87;
			ScrollBarRightPage.Left.Pixels = -8;
			ScrollBarRightPage.Height.Set(-60f, 0.75f);
			ScrollBarRightPage.HAlign = 1f;
			registeredModsList.SetScrollbar(ScrollBarRightPage);
			if (registeredModsList.Count > 3) {
				RightPage.Append(ScrollBarRightPage); // scroll bar for registered mods
				registeredModsList.Left.Pixels -= 8;
			}
		}

		/// <summary>
		/// Sets up the content needed for the record info page.
		/// Includes the navigation buttons for alternate record types such as previous attempt or best record.
		/// </summary>
		private void OpenRecord() {
			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			// Set up the record type navigation buttons
			// Only bosses have records (Events will have banners of the enemies in the event drawn on it)
			// The entry also must be fully supported to have these buttons created

			if (GetLogEntryInfo.type != EntryType.Boss) {
				RecordDisplaySlot slot = new RecordDisplaySlot(BossLogResources.Content_RecordSlot);
				if (GetLogEntryInfo.type == EntryType.MiniBoss) {
					slot.title = Language.GetTextValue("Mods.BossChecklist.Log.Records.Kills");
					slot.value = GetRecordModPlayer.MiniBossKills.ContainsKey(GetLogEntryInfo.Key) ? GetRecordModPlayer.MiniBossKills[GetLogEntryInfo.Key].ToString() : "0";
				}
				slot.Left.Pixels = (int)(RightPage.Width.Pixels / 2 - BossLogResources.Content_RecordSlot.Value.Width / 2);
				slot.Top.Pixels = 35 + 75;
				RightPage.Append(slot);

				float offset = 0;
				foreach (string entryKey in GetLogEntryInfo.relatedEntries) {
					EntryInfo relatedEntry = BossChecklist.bossTracker.FindEntryFromKey(entryKey);

					string hoverText = relatedEntry.DisplayName + "\n" + Language.GetTextValue($"{LangLog}.EntryPage.ViewPage");
					Color iconColor = relatedEntry.IsAutoDownedOrMarked ? Color.White : MaskBoss(relatedEntry) == Color.Black ? Color.Black : faded;

					NavigationalButton entryIcon = new NavigationalButton(relatedEntry.headIconTextures().First(), false, iconColor) {
						Id = GetLogEntryInfo.type == EntryType.Event ? "eventIcon" : "bossIcon",
						Anchor = relatedEntry.GetIndex,
						hoverText = hoverText
					};
					entryIcon.Left.Pixels = GetLogEntryInfo.type == EntryType.Event ? 15 + offset : (int)(slot.Width.Pixels - entryIcon.Width.Pixels - 15);
					entryIcon.Top.Pixels = (int)(slot.Height.Pixels / 2 - entryIcon.Height.Pixels / 2);
					slot.Append(entryIcon);

					offset += 10 + entryIcon.Width.Pixels;
				}

				// Determine all banner ids and prevent duplicates for event entries
				if (GetLogEntryInfo.type == EntryType.Event) {
					foreach (int npc in GetLogEntryInfo.npcIDs) {
						int bannerID = Item.NPCtoBanner(npc);
						if (!EventEntryNPCBannerIDList.Contains(bannerID))
							EventEntryNPCBannerIDList.Add(bannerID);
					}
				}
			}
			else if (GetLogEntryInfo.IsRecordIndexed(out int recordIndex)) {
				bool[] buttonConditions = {
					true, // always shown
					GetPlayerRecords.UnlockedFirstVictory,
					GetPlayerRecords.UnlockedPersonalBest,
					Main.netMode == NetmodeID.MultiplayerClient // only shows up on servers
				};
				int count = 0;
				int total = buttonConditions.Count(true);
				foreach (NavigationalButton button in RecordCategoryButtons) {
					if (buttonConditions[RecordCategoryButtons.IndexOf(button)]) {
						RightPage.Append(button);
						int xOffset = count % 2 == 0 ? (count + 1 == total ? 15 : 0) : 30;
						int yOffset = count > 1 ? 30 : (total > 2 ? 0 : 15);
						button.Left.Pixels = (int)(OpenRecordPageButton.Left.Pixels + OpenRecordPageButton.Width.Pixels + 20 + xOffset);
						button.Top.Pixels = (int)(OpenRecordPageButton.Top.Pixels + yOffset);
						count++;
					}
				}

				if (buttonConditions[(int)SelectedRecordCategory] is false)
					SelectedRecordCategory = RecordCategory.PreviousAttempt; // If no access granted, default back to previous attempt

				if (SelectedRecordCategory == RecordCategory.WorldRecord)
					SelectedRecordComparison = RecordCategory.None; // World records are not exclusively set by the player. Also, world record holders are displayed similarly

				if (SelectedRecordComparison != RecordCategory.None && buttonConditions[(int)SelectedRecordComparison] is false)
					SelectedRecordComparison = RecordCategory.None; // If no access granted, default back to None

				// create 4 slots for each stat category value
				for (int i = 0; i < 4; i++) {
					RecordDisplaySlot slot = new RecordDisplaySlot(BossLogResources.Content_RecordSlot, SelectedRecordCategory, i, recordIndex);
					slot.Left.Pixels = (int)(RightPage.Width.Pixels / 2 - BossLogResources.Content_RecordSlot.Value.Width / 2);
					slot.Top.Pixels = (int)(35 + (75 * (i + 1)));
					RightPage.Append(slot);

					if (i == 0) {
						UIImage categoryIcon = new UIImage(BossLogResources.Nav_Record_Category[(int)SelectedRecordCategory]);
						categoryIcon.Left.Pixels = 15;
						categoryIcon.Top.Pixels = (int)(slot.Height.Pixels / 2 - categoryIcon.Height.Pixels / 2);
						categoryIcon.OnRightClick += (a, b) => ResetStats();
						slot.Append(categoryIcon);

						#region Experimental Feature Notice
						/*
						Asset<Texture2D> bnuuy = RequestVanillaTexture("Images/UI/Creative/Journey_Toggle");
						string noticeText;
						if (RecordSubCategory == SubCategory.WorldRecord) {
							noticeText = $"World Records are currently {(BossChecklist.DebugConfig.DisableWorldRecords ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
								"\nThe World Records feature is still under construction." +
								"\nThis feature is known to not work and cause issues, so enable at your own risk." +
								$"\nWorld Records can be {(BossChecklist.DebugConfig.DisableWorldRecords ? "enabled" : "disabled")} under the Feature Testing configs.";
						}
						else {
							noticeText = $"Boss Records are currently {(BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE ? $"[c/{Color.Red.Hex3()}:disabled]" : $"[c/{Color.LightGreen.Hex3()}:enabled]")}" +
								"\nThis section of the Boss Log is still under construction." +
								"\nAny features or configs related to this page may not work or cause issues." +
								$"\nBoss Records can be toggled under the Feature Testing configs.";
						}

						NavigationalButton bnuuyIcon = new NavigationalButton(bnuuy, false) {
							Id = "bnuuyIcon",
							hoverText = noticeText,
							hoverTextColor = Color.Gold
						};
						bnuuyIcon.Left.Pixels = (int)(slot.Width.Pixels - bnuuyIcon.Width.Pixels - 15);
						bnuuyIcon.Top.Pixels = (int)(slot.Height.Pixels / 2 - bnuuyIcon.Height.Pixels / 2);
						slot.AddOrRemoveChild(bnuuyIcon, GetLogEntryInfo.type == EntryType.Boss);
						*/
						#endregion

						float offset = 0;
						foreach (string entryKey in GetLogEntryInfo.relatedEntries) {
							EntryInfo relatedEntry = BossChecklist.bossTracker.FindEntryFromKey(entryKey);

							string hoverText = relatedEntry.DisplayName + "\n" + Language.GetTextValue($"{LangLog}.EntryPage.ViewPage");
							Color iconColor = relatedEntry.IsAutoDownedOrMarked ? Color.White : MaskBoss(relatedEntry) == Color.Black ? Color.Black : faded;

							NavigationalButton entryIcon = new NavigationalButton(relatedEntry.headIconTextures().First(), false, iconColor) {
								Id = GetLogEntryInfo.type == EntryType.Event ? "eventIcon" : "bossIcon",
								Anchor = relatedEntry.GetIndex,
								hoverText = hoverText
							};
							entryIcon.Left.Pixels = GetLogEntryInfo.type == EntryType.Event ? 15 + offset : (int)(slot.Width.Pixels - entryIcon.Width.Pixels - 15);
							entryIcon.Top.Pixels = (int)(slot.Height.Pixels / 2 - entryIcon.Height.Pixels / 2);
							slot.Append(entryIcon);

							offset += 10 + entryIcon.Width.Pixels;
						}
					}
					else if (i >= 2) {
						NavigationalButton trophy = null;
						if (SelectedRecordCategory == RecordCategory.WorldRecord) {
							trophy = new NavigationalButton(BossLogResources.RequestItemTexture(ItemID.GolfTrophyGold), false) {
								hoverText = i == 2 ? GetWorldRecords.ListDurationRecordHolders() : GetWorldRecords.ListHitsTakenRecordHolders()
							};
						}
						else if (SelectedRecordComparison != RecordCategory.None) {
							// default to world records as these are shared among all players and only have one record type value
							int recordValue = i == 2 ? GetPlayerRecords.GetStatByCategory(SelectedRecordCategory) : GetPlayerRecords.GetStatByCategory(SelectedRecordCategory, false);
							int compValue = i == 2 ? GetWorldRecords.durationWorld : GetWorldRecords.hitsTakenWorld;

							if (SelectedRecordComparison != RecordCategory.WorldRecord)
								compValue = i == 2 ? GetPlayerRecords.GetStatByCategory(SelectedRecordComparison) : GetPlayerRecords.GetStatByCategory(SelectedRecordComparison, false);

							string compValueString = i == 2 ? PersonalRecords.TimeConversion(compValue) : PersonalRecords.HitCount(compValue);
							string diffValue = i == 2 ? PersonalRecords.TimeConversionDiff(recordValue, compValue, out Color color) : PersonalRecords.HitCountDiff(recordValue, compValue, out color);
							string path = $"{LangLog}.Records.Category.{SelectedRecordComparison}";
							trophy = new NavigationalButton(BossLogResources.RequestItemTexture(ItemID.GolfTrophySilver), false) {
								Id = "CompareStat",
								hoverText = $"[c/{Color.Wheat.Hex3()}:{Language.GetTextValue(path)}: {compValueString}]{(diffValue != "" ? $"\n{diffValue}" : "")}",
								hoverTextColor = color
							};
						}
						else if (SelectedRecordCategory == RecordCategory.PersonalBest && ((i == 2 && GetPlayerRecords.durationPrevBest != -1) || (i == 3 && GetPlayerRecords.hitsTakenPrevBest != -1))) {
							string compValueString = i == 2 ? PersonalRecords.TimeConversion(GetPlayerRecords.durationPrevBest) : PersonalRecords.HitCount(GetPlayerRecords.hitsTakenPrevBest);
							string diffValue = i == 2 ? PersonalRecords.TimeConversionDiff(GetPlayerRecords.durationBest, GetPlayerRecords.durationPrevBest, out Color color) : PersonalRecords.HitCountDiff(GetPlayerRecords.hitsTakenBest, GetPlayerRecords.hitsTakenPrevBest, out color);
							string path = $"{LangLog}.Records.PreviousBest";
							trophy = new NavigationalButton(BossLogResources.RequestItemTexture(ItemID.GolfTrophyBronze), false) {
								hoverText = $"[c/{Color.Wheat.Hex3()}:{Language.GetTextValue(path)}: {compValueString}]{(diffValue != "" ? $"\n{diffValue}" : "")}",
								hoverTextColor = color
							};
						}

						if (trophy is not null) {
							trophy.Left.Pixels = slot.Width.Pixels - trophy.Width.Pixels * 2 / 3;
							trophy.Top.Pixels = slot.Height.Pixels / 2 - trophy.Height.Pixels / 2;
							slot.Append(trophy);

							if (trophy.Id == "CompareStat") {
								UIImage compareIcon = new UIImage(BossLogResources.Nav_Record_Category[(int)SelectedRecordComparison]);
								compareIcon.Left.Pixels = -(int)(compareIcon.Width.Pixels / 3);
								compareIcon.Top.Pixels = (int)(trophy.Height.Pixels - compareIcon.Height.Pixels * 2 / 3);
								trophy.Append(compareIcon);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets up the content needed for the spawn info page, 
		/// creating a text box containing a description of how to summon the boss or event. 
		/// It can also have an item slot that cycles through summoning items along with the item's recipe for crafting.
		/// </summary>
		private void OpenSpawn() {
			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			if (GetLogEntryInfo.modSource == "Unknown")
				return; // prevent unsupported entries from displaying info
						// a message panel will take its place notifying the user that the mods calls are out of date

			// Before anything, create a message box to display the spawn info provided
			var message = new UIMessageBox(GetLogEntryInfo.DisplaySpawnInfo);
			message.Width.Set(-34f, 1f);
			message.Height.Set(-370f, 1f);
			message.Top.Set(85f, 0f);
			message.Left.Set(5f, 0f);
			RightPage.Append(message);

			// create a scroll bar in case the message is too long for the box to contain
			ScrollBarRightPage.SetView(100f, 1000f);
			ScrollBarRightPage.Top.Set(91f, 0f);
			ScrollBarRightPage.Height.Set(-382f, 1f);
			ScrollBarRightPage.Left.Set(-5, 0f);
			ScrollBarRightPage.HAlign = 1f;
			RightPage.Append(ScrollBarRightPage);
			message.SetScrollbar(ScrollBarRightPage);

			if (SpawnItemSelected >= GetLogEntryInfo.spawnItem.Count)
				SpawnItemSelected = 0; // the selected spawn item number is greater than how many are in the list, so reset it back to 0

			// Once the spawn description has been made, start structuring the spawn items showcase
			// If the spawn item list is empty, inform the player that there are no summon items for the boss/event through text
			if (GetLogEntryInfo.spawnItem.Count == 0 || GetLogEntryInfo.spawnItem[SpawnItemSelected] == ItemID.None) {
				UIText info = new UIText(Language.GetTextValue($"{LangLog}.SpawnInfo.NoSpawnItem", Language.GetTextValue($"{LangLog}.Common.{GetLogEntryInfo.type}")));
				info.Left.Pixels = (RightPage.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(info.Text).X / 2) - 5;
				info.Top.Pixels = 300;
				RightPage.Append(info);
				return; // since no items are listed, the recipe code does not need to occur
			}

			// If a valid item is found, an item slot can be created
			int itemType = GetLogEntryInfo.spawnItem[SpawnItemSelected]; // grab the item type
			Item spawn = ContentSamples.ItemsByType[itemType]; // create an item for the item slot to use and reference
			if (GetLogEntryInfo.Key == "Terraria TorchGod" && itemType == ItemID.Torch) {
				spawn.stack = 101; // apply a custom stack count for the torches needed for the Torch God summoning event
			}

			LogItemSlot spawnItemSlot = new LogItemSlot(spawn, ItemSlot.Context.EquipDye);
			spawnItemSlot.Left.Pixels = 48 + (56 * 2);
			spawnItemSlot.Top.Pixels = 230;
			RightPage.Append(spawnItemSlot);

			// if more than one item is used for summoning, append navigational button to cycle through the items
			// a previous item button will appear if it is not the first item listed
			if (SpawnItemSelected > 0) {
				NavigationalButton PrevItem = new NavigationalButton(BossLogResources.Nav_Prev, true) {
					Id = "PrevItem"
				};
				PrevItem.Left.Pixels = spawnItemSlot.Left.Pixels - PrevItem.Width.Pixels - 6;
				PrevItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (PrevItem.Height.Pixels / 2);
				PrevItem.OnLeftClick += ChangeSpawnItem;
				RightPage.Append(PrevItem);
			}
			// a next button will appear if it is not the last item listed
			if (SpawnItemSelected < GetLogEntryInfo.spawnItem.Count - 1) {
				NavigationalButton NextItem = new NavigationalButton(BossLogResources.Nav_Next, true) {
					Id = "NextItem"
				};
				NextItem.Left.Pixels = spawnItemSlot.Left.Pixels + spawnItemSlot.Width.Pixels + 6;
				NextItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (NextItem.Height.Pixels / 2);
				NextItem.OnLeftClick += ChangeSpawnItem;
				RightPage.Append(NextItem);
			}

			/// Code below handles all of the recipe searching and displaying
			// Finally, if the item has a recipe, look for all possible recipes and display them
			// create empty lists for ingredients and required tiles
			List<Item> ingredients = new List<Item>();
			List<int> requiredTiles = new List<int>();
			string recipeMod = "Terraria"; // we can inform players where the recipe originates from, starting with vanilla as a base

			// search for all recipes that have the item as a result
			var itemRecipes = Main.recipe
				.Take(Recipe.numRecipes)
				.Where(r => r.HasResult(itemType) && !r.Disabled);

			// iterate through all the recipes to gather the information we need to display for the recipe
			int TotalRecipes = 0;
			foreach (Recipe recipe in itemRecipes) {
				if (TotalRecipes == RecipeSelected) {
					foreach (Item item in recipe.requiredItem) {
						Item clone = item.Clone();
						OverrideForGroups(recipe, clone); // account for recipe group names
						ingredients.Add(clone); // populate the ingredients list with all items found in the required items
					}

					requiredTiles.AddRange(recipe.requiredTile); // populate the required tiles list

					if (recipe.Mod != null) {
						recipeMod = recipe.Mod.DisplayName; // if the recipe was added by a mod, credit the mod
					}
				}
				TotalRecipes++; // add to recipe count. this will be useful later for cycling through other recipes.
			}

			// If the recipe selected is greater than the total recipes, no recipe will be shown.
			// To avoid this, set the selected recipe back to 0 and reconstruct the spawn page, which will iterate through the recipes again.
			if (TotalRecipes != 0 && RecipeSelected + 1 > TotalRecipes) {
				RecipeSelected = 0;
				RefreshPageContent();
				return; // don't let the code run twice
			}
			// once this check has passed, the page elements can now be created

			// If no recipes were found, skip the recipe item slot code and inform the user the item is not craftable
			if (TotalRecipes == 0) {
				string noncraftable = Language.GetTextValue($"{LangLog}.SpawnInfo.Noncraftable");
				UIText craftText = new UIText(noncraftable, 0.8f);
				craftText.Left.Pixels = 10;
				craftText.Top.Pixels = 205;
				RightPage.Append(craftText);
				return;
			}
			else {
				// display where the recipe originates form
				string recipeMessage = Language.GetTextValue($"{LangLog}.SpawnInfo.RecipeFrom", recipeMod);
				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = 10;
				ModdedRecipe.Top.Pixels = 205;
				RightPage.Append(ModdedRecipe);

				// if more than one recipe exists for the selected item, append a button that cycles through all possible recipes
				if (TotalRecipes > 1) {
					NavigationalButton CycleItem = new NavigationalButton(BossLogResources.Content_Cycle, true) {
						Id = "CycleItem_" + TotalRecipes,
						hoverText = $"{LangLog}.SpawnInfo.CycleRecipe"
					};
					CycleItem.Left.Pixels = 20 + (int)(TextureAssets.InventoryBack9.Width() * 0.85f / 2 - BossLogResources.Content_Cycle.Value.Width / 2);
					CycleItem.Top.Pixels = 240 + (int)(TextureAssets.InventoryBack9.Height() * 0.85f / 2 - BossLogResources.Content_Cycle.Value.Height / 2);
					CycleItem.OnLeftClick += ChangeSpawnItem;
					RightPage.Append(CycleItem);
				}
			}

			Point slotPos = Point.Zero; // Handles the current position of the current ItemSlot, using X for row and Y for column positions
			// Since recipe ingredients have a maximum of 14, the UIList pageTwoItemList and its scrollbar element are not needed
			foreach (Item item in ingredients) {
				// Create an item slot for the current item
				LogItemSlot ingList = new LogItemSlot(item, ItemSlot.Context.GuideItem, 0.85f) {
					Id = LogItemSlot.SpawnItemCraftingSlot,
					hasItem = Main.LocalPlayer.inventory.Any(x => x.type == item.type && x.stack >= item.stack)
				};
				ingList.Left.Pixels = 20 + (48 * slotPos.Y);
				ingList.Top.Pixels = 240 + (48 * (slotPos.X + 1));
				RightPage.Append(ingList);

				slotPos.Y++;
				if (slotPos.Y == 7) {
					slotPos.Y = 0;
					slotPos.X++; // if column value hits the maximum amount per row attempt to move onto the next row

					if (slotPos.X == 2)
						break; // recipes should not be able to have more than 14 ingredients (2 rows)
				}
			}

			// Make a new row if the current one isnt empty
			if (slotPos.Y != 0) {
				slotPos.Y = 0;
				slotPos.X++;
			}

			if (requiredTiles.Count == 0) {
				// If there were no tiles required for the recipe, add a 'By Hand' slot
				LogItemSlot byHandCrafting = new LogItemSlot(new Item(ItemID.HandOfCreation), ItemSlot.Context.EquipArmorVanity, 0.85f) {
					Id = LogItemSlot.SpawnItemCraftingSlot,
					hoverText = $"{LangLog}.SpawnInfo.ByHand",
					hasItem = true
				};
				byHandCrafting.Top.Pixels = 240 + (48 * (slotPos.X + 1));
				byHandCrafting.Left.Pixels = 20;
				RightPage.Append(byHandCrafting);
			}
			else if (requiredTiles.Count > 0) {
				// iterate through all required tiles to list them in item slots
				slotPos.Y = 0; // reset col to zero for the crafting stations
				foreach (int tile in requiredTiles) {
					if (tile == -1)
						break; // Prevents extra empty slots from being created

					LogItemSlot tileList = new LogItemSlot(ContentSamples.ItemsByType.Values.FirstOrDefault(x => x.createTile == tile) ?? new Item(ItemID.None), ItemSlot.Context.EquipArmorVanity, 0.85f) {
						Id = LogItemSlot.SpawnItemCraftingSlot,
						hoverText = Lang.GetMapObjectName(Terraria.Map.MapHelper.TileToLookup(tile, Recipe.GetRequiredTileStyle(tile))), // retrieves the designated tile name (as it appears on the map)
						hasItem = Main.LocalPlayer.adjTile[tile]
					};
					tileList.Left.Pixels = 20 + (48 * slotPos.Y);
					tileList.Top.Pixels = 240 + (48 * (slotPos.X + 1));
					RightPage.Append(tileList);
					slotPos.Y++; // if multiple crafting stations are needed
				}
			}
		}

		/// <summary>
		/// Sets up the content needed for the loot page, creating a list of item slots of the boss's loot and collectibles.
		/// </summary>
		private void OpenLoot() {
			if (PageNum < 0)
				return; // Code should only run if it is on an entry page

			// set up an item list for the listed loot itemslots
			UIList lootItemList = new UIList();
			lootItemList.Left.Pixels = 0;
			lootItemList.Top.Pixels = 125;
			lootItemList.Width.Pixels = RightPage.Width.Pixels - 25;
			lootItemList.Height.Pixels = RightPage.Height.Pixels - 125 - 80;

			// create an image of the entry's treasure bag
			TreasureBag treasureBag = new TreasureBag(GetLogEntryInfo.TreasureBag);
			treasureBag.Left.Pixels = RightPage.Width.Pixels / 2 - treasureBag.Width.Pixels / 2;
			treasureBag.Top.Pixels = 88;
			RightPage.Append(treasureBag);

			List<ItemDefinition> obtainedItems = GetModPlayer.BossItemsCollected;
			List<DropRateInfo> bossDrops = new List<DropRateInfo>(GetLogEntryInfo.loot); // combined list of loot and collectibles

			foreach (DropRateInfo drop in GetLogEntryInfo.loot) {
				// the treasurebag should not be displayed on the loot table, but drawn above it instead
				// coins should not be displayed
				if (drop.itemId == GetLogEntryInfo.TreasureBag || ItemID.Sets.CommonCoin[drop.itemId]) {
					bossDrops.Remove(drop);
					continue;
				}

				if (drop.conditions is null)
					continue; // this includes the expert item for vanilla entries

				foreach (var cond in drop.conditions) {
					// any item that is unobtainable should not be displayed
					if (cond.CanShowItemDropInUI() is false) {
						bossDrops.Remove(drop);
						break;
					}
				}
			}

			// Standard collectible items should appear first. Generic collectibles only appear if they are obtainable.
			List<int> bossItems = new List<int>();
			for (int i = 0; i < (int)CollectibleType.Generic; i++) {
				foreach (KeyValuePair<int, CollectibleType> item in GetLogEntryInfo.collectibles.Where(x => x.Value == (CollectibleType)i)) {
					if (!bossItems.Contains(item.Key))
						bossItems.Add(item.Key);
				}
			}

			// the expert item should appear after the master mode collectibles
			if (GetLogEntryInfo.ExpertItem > 0 && !bossItems.Contains(GetLogEntryInfo.ExpertItem)) {
				if (bossItems.Count >= (int)CollectibleType.Trophy) {
					bossItems.Insert((int)CollectibleType.Trophy, GetLogEntryInfo.ExpertItem);
				}
				else {
					bossItems.Add(GetLogEntryInfo.ExpertItem);
				}
			}

			// finally, the rest of the drops are added (including the generic collectibles)
			foreach (DropRateInfo drop in bossDrops) {
				if (!bossItems.Contains(drop.itemId))
					bossItems.Add(drop.itemId);
			}

			// If the boss items contains any otherworld music boxes, check the status of OtherWorld music obtainability
			if (bossItems.Intersect(BossChecklist.bossTracker.otherWorldMusicBoxTypes).Any()) {
				FieldInfo TOWMusicUnlocked = typeof(Main).GetField("TOWMusicUnlocked", BindingFlags.Static | BindingFlags.NonPublic);
				bool OWUnlocked = (bool)TOWMusicUnlocked.GetValue(null);
				if (OtherworldMusicUnlocked != OWUnlocked)
					OtherworldMusicUnlocked = OWUnlocked;
			}

			Point slotPos = Point.Zero; // Handles the current position of the current ItemSlot, using X for row and Y for column positions
			LootRow newRow = new LootRow(0); // the initial row to start with

			foreach (int item in bossItems) {
				if (!ContentSamples.ItemsByType.TryGetValue(item, out Item selectedItem))
					continue;

				if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && !GetLogEntryInfo.lootItemTypes.Contains(item))
					continue; // If only dropped lootis being checked, skip any items not found in the loot table

				// Create an item slot for the current item
				LogItemSlot itemSlot = new LogItemSlot(selectedItem, ItemSlot.Context.TrashItem) {
					Id = "loot_" + item,
					hasItem = obtainedItems.Any(x => x.Type == item),
					itemResearched = GetModPlayer.IsItemResearched(item)
				};
				itemSlot.Left.Pixels = (slotPos.Y * 56) + 15;
				itemSlot.OnRightClick += RemoveItem; // debug functionality
				newRow.Append(itemSlot); // append the item slot to the current row

				slotPos.Y++; // increase col before moving on to the next item
				// if col hit the max that can be drawn on the page, add the current row to the list and move onto the next row
				if (slotPos.Y == 6) {
					slotPos.Y = 0;
					slotPos.X++;
					lootItemList.Add(newRow);
					newRow = new LootRow(slotPos.X);
				}
			}

			// Once all our items have been added, append the final row to the list
			// This does not need to occur if col is at 0, as the current row is empty
			if (slotPos.Y != 0) {
				slotPos.X++;
				lootItemList.Add(newRow);
			}

			RightPage.Append(lootItemList); // append the list to the page so the items can be seen

			// If more than 5 rows exist, a scroll bar is needed to access all items in the loot list
			if (slotPos.X > 5) {
				ScrollBarRightPage.SetView(10f, 1000f);
				ScrollBarRightPage.Top.Pixels = 125;
				ScrollBarRightPage.Left.Pixels = -3;
				ScrollBarRightPage.Height.Set(-88f, 0.75f);
				ScrollBarRightPage.HAlign = 1f;

				RightPage.Append(ScrollBarRightPage);
				lootItemList.SetScrollbar(ScrollBarRightPage);
			}
		}

		/// <summary>
		/// Used to locate the next available entry for the player to fight that is not defeated, marked as defeated, or hidden.
		/// </summary>
		/// <param name="entryType">Add an entry type to specifically look for the next available entry of that type.</param>
		/// <returns>The index of the next available entry within the SortedEntries list.</returns>
		public static int FindNextEntry(EntryType? entryType = null) => BossChecklist.bossTracker.SortedEntries.FindIndex(x => !x.IsAutoDownedOrMarked && x.available() && x.VisibleOnChecklist() && (!entryType.HasValue || x.type == entryType));

		/// <summary> Determines if a texture should be masked by a black sihlouette. </summary>
		public static Color MaskBoss(EntryInfo entry) {
			if (!BossChecklist.BossLogConfig.ProgressiveChecklist)
				return Color.White;

			return entry.IsUpNext ? Color.Black : Color.White;
		}

		public static void OverrideForGroups(Recipe recipe, Item item) {
			// This method taken from RecipeBrowser with permission.
			if (recipe.ProcessGroupsForText(item.type, out string nameOverride)) {
				//Main.toolTip.name = name;
			}
			if (nameOverride != "") {
				item.SetNameOverride(nameOverride);
			}
		}
	}
}
