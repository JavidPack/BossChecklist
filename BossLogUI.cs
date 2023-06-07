using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using static BossChecklist.UIElements.BossLogUIElements;
using Terraria.GameContent.ItemDropRules;
using System.Reflection;

namespace BossChecklist
{
	class BossLogUI : UIState
	{
		public OpenLogButton bosslogbutton; // The main button to open the Boss Log
		public LogPanel BookArea; // The main panel for the UI. All content is aligned within this area.
		public LogPanel PageOne; // left page content panel
		public LogPanel PageTwo; // right page content panel

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
				else {
					UpdateSelectedPage(value, SelectedSubPage);
				}
			}
		}

		/// <summary>
		/// Gets the EntryInfo of the entry on the selected page. Returns null if not on an entry page.
		/// </summary>
		public EntryInfo GetLogEntryInfo => PageNum >= 0 ? BossChecklist.bossTracker.SortedEntries[PageNum] : null;

		// Navigation
		public NavigationalButton NextPage;
		public NavigationalButton PrevPage;

		public static SubPage SelectedSubPage = SubPage.Records;
		public SubPageButton recordButton;
		public SubPageButton spawnButton;
		public SubPageButton lootButton;

		// Book Tabs
		public LogTab ToCTab; // also used for the filter tab
		public LogTab CreditsTab;
		public LogTab BossTab;
		public LogTab MiniBossTab;
		public LogTab EventTab;
		public IndicatorPanel IndicatorTab;
		public List<IndicatorIcon> Indicators;
		public FilterIcon FilterPanel; // contains the filter buttons, (not a filter icon, but it works)
		public List<FilterIcon> FilterIcons;
		public bool filterOpen = false; // when true, the filter panel is visible to the user

		// Table of Contents related
		public UIList prehardmodeList; // lists for pre-hardmode and hardmode entries
		public UIList hardmodeList;
		public ProgressBar prehardmodeBar; // progress bars for pre-hardmode and hardmode entries
		public ProgressBar hardmodeBar;
		public LogScrollbar scrollOne; // scroll bars for table of contents lists (and other elements too)
		public LogScrollbar scrollTwo;
		public bool showHidden = false; // when true, hidden entries are visible on the list
		public bool barState = false; // when true, hovering over the progress bar will split up the entry percentages by mod instead of entry type
		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles

		// Credits related
		public readonly Dictionary<string, string> contributors = new Dictionary<string, string>() {
			{ "Jopojelly", "Creator & Owner" },
			{ "SheepishShepherd", "Co-Owner & Maintainer"},
			{ "direwolf420", "Code Contributor" },
			{ "riveren", "Boss Log Sprites"},
			{ "Orian", "Early Testing" },
			{ "Panini", "Early Server Testing" }
		};

		// Record page related
		public static SubCategory RecordSubCategory = SubCategory.PreviousAttempt;
		public static SubCategory CompareState = SubCategory.None; // Compare record values to one another

		// Spawn Info page related
		public static int SpawnItemSelected = 0;
		public static int RecipeSelected = 0;

		// Loot page related
		public static bool OtherworldUnlocked = false;

		// Log UI textures
		public static Asset<Texture2D> Texture_Button_Book;
		public static Asset<Texture2D> Texture_Button_Border;
		public static Asset<Texture2D> Texture_Button_Color;
		public static Asset<Texture2D> Texture_Button_Faded;

		public static Asset<Texture2D> Texture_Log_BackPanel;
		public static Asset<Texture2D> Texture_Log_Paper;
		public static Asset<Texture2D> Texture_Log_Tab;
		public static Asset<Texture2D> Texture_Log_Tab2;
		public static Asset<Texture2D> Texture_Log_FilterPanel;

		public static Asset<Texture2D> Texture_Nav_Prev;
		public static Asset<Texture2D> Texture_Nav_Next;
		public static Asset<Texture2D> Texture_Nav_SubPage;
		public static Asset<Texture2D> Texture_Nav_TableOfContents;
		public static Asset<Texture2D> Texture_Nav_Credits;
		public static Asset<Texture2D> Texture_Nav_Boss;
		public static Asset<Texture2D> Texture_Nav_MiniBoss;
		public static Asset<Texture2D> Texture_Nav_Event;
		public static Asset<Texture2D> Texture_Nav_Filter;

		public static Asset<Texture2D> Texture_Check_Box;
		public static Asset<Texture2D> Texture_Check_Check;
		public static Asset<Texture2D> Texture_Check_X;
		public static Asset<Texture2D> Texture_Check_Next;
		public static Asset<Texture2D> Texture_Check_Strike;
		public static Asset<Texture2D> Texture_Check_Chest;
		public static Asset<Texture2D> Texture_Check_GoldChest;

		public static Asset<Texture2D> Texture_Credit_DevSlot;
		public static Asset<Texture2D> Texture_Credit_ModSlot;

		public static Asset<Texture2D> Texture_Content_RecordSlot;
		public static Asset<Texture2D> Texture_Content_Cycle;
		public static Asset<Texture2D> Texture_Content_ToggleHidden;
		public static Asset<Texture2D> Texture_Content_BossKey;

		// Extra stuff
		public const string LangLog = "Mods.BossChecklist.Log";
		public static int headNum = -1;
		public static readonly Color faded = new Color(128, 128, 128, 128);
		public UIImage PromptCheck; // checkmark for the toggle prompt config button
		public UIText PageOneTitle;
		public UIText PageTwoTitle;

		// Boss Log visibiltiy helpers
		private bool bossLogVisible;
		internal static bool PendingToggleBossLogUI; // Allows toggling boss log visibility from methods not run during UIScale so Main.screenWidth/etc are correct for ResetUIPositioning method
		internal static bool PendingConfigChange; // Allows configs to be updated on Log close, when needed
		
		private bool PendingPageChange; // Allows changing the page outside of the UIState without causing ordering or drawing issues.
		private int PageChangeValue;
		public int PendingPageNum {
			get => PageChangeValue;
			set {
				PageChangeValue = value;
				PendingPageChange = true;
			}
		}

		/// <summary>
		/// Appends or removes UI elements based on the visibility status it is set to.
		/// </summary>
		public bool BossLogVisible {
			get => bossLogVisible;
			set {
				if (value) {
					Append(BookArea);
					Append(ToCTab);
					Append(FilterPanel);
					Append(IndicatorTab);
					Append(CreditsTab);
					Append(BossTab);
					Append(MiniBossTab);
					Append(EventTab);
					Append(PageOne);
					Append(PageTwo);
				}
				else {
					RemoveChild(PageTwo);
					RemoveChild(PageOne);
					RemoveChild(EventTab);
					RemoveChild(MiniBossTab);
					RemoveChild(BossTab);
					RemoveChild(CreditsTab);
					RemoveChild(IndicatorTab);
					RemoveChild(FilterPanel);
					RemoveChild(ToCTab);
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
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();

			if (show) {
				// Mark the player as having opened the Log if they have not been so already
				if (!modPlayer.hasOpenedTheBossLog) {
					modPlayer.hasOpenedTheBossLog = true; // This will only ever happen once per character
					modPlayer.enteredWorldReset = false; // If opening for the first time, this doesn't need to occur again until the next world reset

					// When opening for the first time, open the Progression Mode prompt if enabled. Otherwise, open the Table of Contents.
					PageNum = BossChecklist.BossLogConfig.PromptDisabled ? Page_TableOfContents : Page_Prompt;
				}
				else {
					BossTab.Anchor = FindNextEntry(EntryType.Boss); // Update the Anchors for all entry tabs every time the Boss Log is opened
					MiniBossTab.Anchor = FindNextEntry(EntryType.MiniBoss);
					EventTab.Anchor = FindNextEntry(EntryType.Event);

					if (modPlayer.enteredWorldReset) {
						// If the Log has been opened before, check for a world change.
						// This is to reset the page from what the user previously had back to the Table of Contents when entering another world.
						modPlayer.enteredWorldReset = false;
						PageNum = Page_TableOfContents;
					}
					else {
						RefreshPageContent(); // Otherwise, just default to the last page selected
					}
				}

				// Update UI Element positioning before marked visible
				// This will always occur after adjusting UIScale, since the UI has to be closed in order to open up the menu options
				//ResetUIPositioning();
				Main.playerInventory = false; // hide the player inventory
			}
			else if (PageNum >= 0) {
				// If UI is closed on a new record page, remove the new record from the list
				int selectedEntryIndex = GetLogEntryInfo.GetRecordIndex;
				if (selectedEntryIndex != -1 && modPlayer.hasNewRecord.Length > 0)
					modPlayer.hasNewRecord[selectedEntryIndex] = false;
			}

			BossLogVisible = show; // Setting the state makes the UIElements append/remove making them visible/invisible
		}

		public static Asset<Texture2D> RequestVanillaTexture(string path) => Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);

		public static Asset<Texture2D> RequestResource(string path) => ModContent.Request<Texture2D>("BossChecklist/Resources/" + path, AssetRequestMode.ImmediateLoad);

		public override void OnInitialize() {
			Texture_Button_Book = RequestResource("Book_Outline");
			Texture_Button_Border = RequestResource("Book_Border");
			Texture_Button_Color = RequestResource("Book_Color");
			Texture_Button_Faded = RequestResource("Book_Faded");

			Texture_Log_BackPanel = RequestResource("LogUI_Back");
			Texture_Log_Paper = RequestResource("LogUI_Paper");
			Texture_Log_Tab = RequestResource("LogUI_Tab");
			Texture_Log_Tab2 = RequestResource("LogUI_InfoTab");
			Texture_Log_FilterPanel = RequestResource("LogUI_Filter");

			Texture_Nav_Prev = RequestResource("Nav_Prev");
			Texture_Nav_Next = RequestResource("Nav_Next");
			Texture_Nav_SubPage = RequestResource("Nav_SubPage_Button");
			Texture_Nav_TableOfContents = RequestResource("Nav_Contents");
			Texture_Nav_Credits = RequestResource("Nav_Credits");
			Texture_Nav_Boss = RequestResource("Nav_Boss");
			Texture_Nav_MiniBoss = RequestResource("Nav_Miniboss");
			Texture_Nav_Event = RequestResource("Nav_Event");
			Texture_Nav_Filter = RequestResource("Nav_Filter");

			Texture_Check_Box = RequestResource("Checks_Box");
			Texture_Check_Check = RequestResource("Checks_Check");
			Texture_Check_X = RequestResource("Checks_X");
			Texture_Check_Next = RequestResource("Checks_Next");
			Texture_Check_Strike = RequestResource("Checks_Strike");
			Texture_Check_Chest = RequestResource("Checks_Chest");
			Texture_Check_GoldChest = RequestResource("Checks_Chest_Gold");

			Texture_Credit_DevSlot = RequestResource("Credits_Panel_Dev");
			Texture_Credit_ModSlot = RequestResource("Credits_Panel_Mod");

			Texture_Content_RecordSlot = RequestResource("Extra_RecordSlot");
			Texture_Content_Cycle = RequestResource("Extra_CycleRecipe");
			Texture_Content_ToggleHidden = RequestResource("Nav_Hidden");
			Texture_Content_BossKey = RequestResource("Extra_Key");

			bosslogbutton = new OpenLogButton(Texture_Button_Book);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnLeftClick += (a, b) => ToggleBossLog(true);

			BookArea = new LogPanel();
			BookArea.Width.Pixels = Texture_Log_BackPanel.Value.Width;
			BookArea.Height.Pixels = Texture_Log_BackPanel.Value.Height;

			ToCTab = new LogTab(Texture_Log_Tab, Texture_Nav_TableOfContents) {
				Id = "TableOfContents"
			};
			ToCTab.OnLeftClick += (a, b) => UpdateFilterTabPos(true);
			ToCTab.OnRightClick += (a, b) => ClearMarkedDowns();

			BossTab = new LogTab(Texture_Log_Tab, Texture_Nav_Boss) {
				Id = "Boss"
			};

			MiniBossTab = new LogTab(Texture_Log_Tab, Texture_Nav_MiniBoss) {
				Id = "MiniBoss"
			};

			EventTab = new LogTab(Texture_Log_Tab, Texture_Nav_Event) {
				Id = "Event"
			};

			CreditsTab = new LogTab(Texture_Log_Tab, Texture_Nav_Credits) {
				Id = "Credits",
				Anchor = -2,
				hoverText = $"{LangLog}.Tabs.Credits" // hoverText will never change, so initialize it
			};

			PageOne = new LogPanel() {
				Id = "PageOne",
			};
			PageOne.Width.Pixels = 375;
			PageOne.Height.Pixels = 480;

			PageOneTitle = new UIText("", 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			PageOneTitle.Top.Pixels = 18;

			PrevPage = new NavigationalButton(Texture_Nav_Prev, true) {
				Id = "Previous"
			};
			PrevPage.Left.Pixels = 8;
			PrevPage.Top.Pixels = 416;
			PrevPage.OnLeftClick += PageChangerClicked;

			prehardmodeList = new UIList();
			prehardmodeList.Left.Pixels = 4;
			prehardmodeList.Top.Pixels = 44;
			prehardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			prehardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			prehardmodeList.PaddingTop = 5;

			PageTwo = new LogPanel() {
				Id = "PageTwo"
			};
			PageTwo.Width.Pixels = 375;
			PageTwo.Height.Pixels = 480;

			PageTwoTitle = new UIText("", 0.6f, true) {
				TextColor = Colors.RarityAmber
			};
			PageTwoTitle.Top.Pixels = 18;

			pageTwoItemList = new UIList();

			FilterPanel = new FilterIcon(Texture_Log_FilterPanel);
			FilterIcons = new List<FilterIcon>() {
				new FilterIcon(Texture_Nav_Boss) { Id = "Boss" },
				new FilterIcon(Texture_Nav_MiniBoss) { Id = "MiniBoss" },
				new FilterIcon(Texture_Nav_Event) { Id = "Event" },
				new FilterIcon(Texture_Content_ToggleHidden) { Id = "Hidden" },
			};

			int offsetY = 0;
			foreach (FilterIcon icon in FilterIcons) {
				icon.check = Texture_Check_Check;
				icon.Top.Pixels = offsetY + 15;
				icon.Left.Pixels = 25 - (int)(icon.Width.Pixels / 2);
				FilterPanel.Append(icon);
				offsetY += 34;
			}

			IndicatorTab = new IndicatorPanel();
			Indicators = new List<IndicatorIcon>() {
				new IndicatorIcon(RequestResource("Indicator_OnlyBosses")) { Id = "OnlyBosses" },
				new IndicatorIcon(RequestResource("Indicator_Progression")) { Id = "Progression" },
			};

			int offsetX = 0;
			foreach (IndicatorIcon icon in Indicators) {
				icon.Left.Pixels = 10 + offsetX;
				icon.Top.Pixels = 8;
				IndicatorTab.Append(icon);
				offsetX += 22;
			}

			NextPage = new NavigationalButton(Texture_Nav_Next, true) {
				Id = "Next"
			};
			NextPage.Left.Pixels = PageTwo.Width.Pixels - NextPage.Width.Pixels - 12;
			NextPage.Top.Pixels = 416;
			NextPage.OnLeftClick += PageChangerClicked;
			PageTwo.Append(NextPage);

			hardmodeList = new UIList();
			hardmodeList.Left.Pixels = 19;
			hardmodeList.Top.Pixels = 44;
			hardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			hardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			hardmodeList.PaddingTop = 5;

			recordButton = new SubPageButton(Texture_Nav_SubPage, SubPage.Records);
			recordButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 - (int)recordButton.Width.Pixels - 8;
			recordButton.Top.Pixels = 5;
			recordButton.OnLeftClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.Records);
			recordButton.OnRightClick += (a, b) => ResetStats();

			spawnButton = new SubPageButton(Texture_Nav_SubPage, SubPage.SpawnInfo);
			spawnButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 + 8;
			spawnButton.Top.Pixels = 5;
			spawnButton.OnLeftClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.SpawnInfo);

			lootButton = new SubPageButton(Texture_Nav_SubPage, SubPage.LootAndCollectibles);
			lootButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 - (int)lootButton.Width.Pixels / 2;
			lootButton.Top.Pixels = 5 + Texture_Nav_SubPage.Value.Height + 10;
			lootButton.OnLeftClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.LootAndCollectibles);

			// scroll one currently only appears for the table of contents, so its fields can be set here
			scrollOne = new LogScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			// scroll two is used in more areas, such as the display spawn info message box, so its fields are set when needed
			scrollTwo = new LogScrollbar();
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
			this.AddOrRemoveChild(bosslogbutton, Main.playerInventory);
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
				int headOffset = 0;
				foreach (Asset<Texture2D> headIcon in entry.headIconTextures) {
					spriteBatch.Draw(headIcon.Value, new Vector2(Main.mouseX + 15 + headOffset, Main.mouseY + 15), MaskBoss(entry));
					headOffset += headIcon.Value.Width + 2;
				}
			}
		}

		/// <summary>
		/// Resets the positioning of the Boss Log's common UI elements.
		/// This includes the main book area, the page areas, and all book tabs.
		/// </summary>
		private void ResetUIPositioning() {
			// Reset the position of the button to make sure it updates with the screen res
			BookArea.Left.Pixels = (Main.screenWidth / 2) - (BookArea.Width.Pixels / 2);
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (BookArea.Height.Pixels / 2) - 6;
			PageOne.Left.Pixels = BookArea.Left.Pixels + 20;
			PageOne.Top.Pixels = BookArea.Top.Pixels + 12;
			PageTwo.Left.Pixels = BookArea.Left.Pixels - 15 + BookArea.Width.Pixels - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = BookArea.Top.Pixels + 12;

			if (PageNum == Page_Prompt)
				return; // Tab positioning does not need to occur as they will not be drawn

			int offsetY = 50;

			// ToC/Filter Tab and Credits Tab never flips to the other side, just disappears when on said page
			ToCTab.Top.Pixels = BookArea.Top.Pixels + offsetY;
			IndicatorTab.Left.Pixels = PageTwo.Left.Pixels + PageTwo.Width.Pixels - 25 - IndicatorTab.Width.Pixels;
			IndicatorTab.Top.Pixels = PageTwo.Top.Pixels - IndicatorTab.Height.Pixels - 6;
			UpdateFilterTabPos(false); // Update filter tab visibility
			CreditsTab.Left.Pixels = BookArea.Left.Pixels + BookArea.Width.Pixels - 12;
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);

			// Reset book tabs Y positioning after BookArea adjusted
			BossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 1);
			MiniBossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 2);
			EventTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 3);
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);

			// Update the navigation tabs to the proper positions
			BossTab.Left.Pixels = BookArea.Left.Pixels + (BossTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
			MiniBossTab.Left.Pixels = BookArea.Left.Pixels + (MiniBossTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
			EventTab.Left.Pixels = BookArea.Left.Pixels + (EventTab.OnLeftSide() ? -20 : BookArea.Width.Pixels - 12);
		}

		/// <summary>
		/// Toggles the visibility state of the filter panel. This can occur when the tab is clicked or if the page is changed.
		/// </summary>
		/// <param name="tabClicked"></param>
		private void UpdateFilterTabPos(bool tabClicked) {
			if (tabClicked && PageNum != Page_TableOfContents)
				return; // Filter tab position should not change if not on the Table of Contents page when clicked

			if (PageNum != Page_TableOfContents) {
				filterOpen = false; // If the page is not on the Table of Contents, the filters tab should be in the closed position
			}
			else if (tabClicked) {
				filterOpen = !filterOpen;
			}

			if (filterOpen) {
				FilterPanel.Top.Pixels = ToCTab.Top.Pixels;
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20 - FilterPanel.Width.Pixels;
				FilterPanel.Left.Pixels = ToCTab.Left.Pixels + ToCTab.Width.Pixels;
				UpdateFilterCheckAndTooltip(); // Update filter display state when the filter panel is opened
			}
			else {
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20;
				FilterPanel.Top.Pixels = -5000; // throw offscreen
			}
		}

		/// <summary>
		/// The logic behind the filters changing checkmarks and hoverTexts where needed.
		/// </summary>
		public void UpdateFilterCheckAndTooltip() {
			// Update all hoverTexts
			foreach(FilterIcon icon in FilterIcons) {
				icon.hoverText = icon.UpdateHoverText();
			}

			// ...Bosses
			FilterIcons[0].check = BossChecklist.BossLogConfig.FilterBosses == "Show" ? Texture_Check_Check : Texture_Check_Next;

			// ...Mini-Bosses
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				FilterIcons[1].check = Texture_Check_X;
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
				FilterIcons[1].check = Texture_Check_Check;
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") {
				FilterIcons[1].check = Texture_Check_X;
			}
			else {
				FilterIcons[1].check = Texture_Check_Next;
			}

			// ...Events
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				FilterIcons[2].check = Texture_Check_X;
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
				FilterIcons[2].check = Texture_Check_Check;
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") {
				FilterIcons[2].check = Texture_Check_X;
			}
			else {
				FilterIcons[2].check = Texture_Check_Next;
			}

			// ...Hidden Entries
			FilterIcons[3].check = showHidden ? Texture_Check_Check : Texture_Check_X;
		}

		// TODO: [??] Implement separate Reset tabs? Including: Clear Hidden List, Clear Marked Downs, Clear Records, Clear Boss Loot, etc

		public void ClearHiddenList() {
			if (!BossChecklist.DebugConfig.ResetHiddenEntries || WorldAssist.HiddenEntries.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.HiddenEntries.Clear();
			BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
			Networking.RequestHiddenEntryUpdate();


			BossTab.Anchor = FindNextEntry(EntryType.Boss);
			MiniBossTab.Anchor = FindNextEntry(EntryType.MiniBoss);
			EventTab.Anchor = FindNextEntry(EntryType.Event);

			showHidden = false;
			RefreshPageContent();
		}

		private void ClearMarkedDowns() {
			if (!BossChecklist.DebugConfig.ResetForcedDowns || WorldAssist.MarkedEntries.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.MarkedEntries.Clear();
			Networking.RequestMarkedEntryUpdate();

			BossTab.Anchor = FindNextEntry(EntryType.Boss);
			MiniBossTab.Anchor = FindNextEntry(EntryType.MiniBoss);
			EventTab.Anchor = FindNextEntry(EntryType.Event);

			RefreshPageContent();
		}

		/// <summary>
		/// While in debug mode, users are able to reset their records of a specific boss by alt and right-clicking the recordnavigation button
		/// <para>TODO: Update to allow clearing Best Records only, First Records only, and All Records (including previous, excluding world records)</para>
		/// </summary>
		private void ResetStats() {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return; // temporary block is recordcode is disabled

			if (!BossChecklist.DebugConfig.ResetRecordsBool || SelectedSubPage != 0)
				return; // do not do anything if not on the record page (ex. can't reset record on loot page)

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return; // player must be holding alt

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			int recordIndex = GetLogEntryInfo.GetRecordIndex;
			PersonalStats stats = modPlayer.RecordsForWorld[recordIndex].stats;
			stats.kills = 0;
			stats.deaths = 0;

			stats.durationBest = -1;
			stats.durationPrev = -1;

			stats.hitsTakenBest = -1;
			stats.hitsTakenPrev = -1;
			RefreshPageContent();  // update page to show changes
		}

		/// <summary>
		/// While in debug mode, players will be able to remove obtained items from their player save data using the right-click button on the selected item slot.
		/// </summary>
		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			if (!BossChecklist.DebugConfig.ResetLootItems || SelectedSubPage != SubPage.LootAndCollectibles)
				return; // do not do anything if the loot page isn't the active

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return; // player must be holding alt to remove any items

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			// Alt right-click the treasure bag icon to clear the items in the selected entry's page from the player's obtained items list
			// Alt right-click an item slot to remove that item from the player's obtained items list
			// Note: items removed are removed from ALL boss loot pages retroactively
			if (listeningElement is NavigationalButton) {
				foreach (int item in GetLogEntryInfo.lootItemTypes) {
					modPlayer.BossItemsCollected.Remove(new ItemDefinition(item));
				}
			}
			else if (listeningElement is LogItemSlot slot) {
				modPlayer.BossItemsCollected.Remove(new ItemDefinition(slot.item.type));
			}
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
			PageOne.RemoveAllChildren(); // remove all content from both pages before appending new content for the prompt
			PageTwo.RemoveAllChildren();

			// create a text box for the progression mode description
			FittedTextPanel textBox = new FittedTextPanel($"{LangLog}.ProgressionMode.Description");
			textBox.Width.Pixels = PageOne.Width.Pixels - 30;
			textBox.Height.Pixels = PageOne.Height.Pixels - 70;
			textBox.Left.Pixels = 10;
			textBox.Top.Pixels = 60;
			PageOne.Append(textBox);

			// create buttons for the different progression mode options
			UIImage[] backdrops = new UIImage[] {
				new UIImage(Texture_Content_RecordSlot),
				new UIImage(Texture_Content_RecordSlot),
				new UIImage(Texture_Content_RecordSlot),
				new UIImage(Texture_Content_RecordSlot)
			};

			backdrops[0].OnLeftClick += (a, b) => SelectProgressionModeState(false);
			backdrops[1].OnLeftClick += (a, b) => SelectProgressionModeState(true);
			backdrops[2].OnLeftClick += (a, b) => CloseAndConfigure();
			backdrops[3].OnLeftClick += (a, b) => DisablePromptMessage();
			foreach (UIImage backdrop in backdrops) {
				backdrop.OnMouseOver += (a, b) => { backdrop.Color = BossChecklist.BossLogConfig.BossLogColor; };
				backdrop.OnMouseOut += (a, b) => { backdrop.Color = Color.White; };
			}

			UIImage[] buttons = new UIImage[] {
				new UIImage(RequestVanillaTexture($"Images/Item_{ItemID.Binoculars}")),
				new UIImage(RequestVanillaTexture($"Images/Item_{ItemID.Blindfold}")),
				new UIImage(RequestVanillaTexture($"Images/UI/Camera_1")),
				new UIImage(Texture_Check_Box)
			};

			FittedTextPanel[] textOptions = new FittedTextPanel[] {
				new FittedTextPanel($"{LangLog}.ProgressionMode.SelectDisable"),
				new FittedTextPanel($"{LangLog}.ProgressionMode.SelectEnable"),
				new FittedTextPanel($"{LangLog}.ProgressionMode.SelectConfig"),
				new FittedTextPanel($"{LangLog}.ProgressionMode.DisablePrompt"),
			};

			PromptCheck = new UIImage(BossChecklist.BossLogConfig.PromptDisabled ? Texture_Check_Check : Texture_Check_X);

			for (int i = 0; i < backdrops.Length; i++) {
				backdrops[i].Left.Pixels = 25;
				backdrops[i].Top.Pixels = 75 + (75 * i);

				buttons[i].Left.Pixels = 15;
				buttons[i].Top.Pixels = backdrops[i].Height.Pixels / 2 - buttons[i].Height.Pixels / 2;

				textOptions[i].Width.Pixels = backdrops[i].Width.Pixels - (buttons[i].Left.Pixels + buttons[i].Width.Pixels + 15);
				textOptions[i].Height.Pixels = backdrops[i].Height.Pixels;
				textOptions[i].Left.Pixels = buttons[i].Left.Pixels + buttons[i].Width.Pixels;
				textOptions[i].Top.Pixels = -10;
				textOptions[i].PaddingTop = 0;
				textOptions[i].PaddingLeft = 15;

				if (i == backdrops.Length - 1) {
					buttons[i].Append(PromptCheck);
				}
				backdrops[i].Append(buttons[i]);
				backdrops[i].Append(textOptions[i]);
				PageTwo.Append(backdrops[i]);
			}
		}

		/// <summary>
		/// Fully enables or disables Progression Mode based on option selected and redirects the player to the Table of Contents.
		/// </summary>
		private void SelectProgressionModeState(bool enabled) {
			BossChecklist.BossLogConfig.ProgressionModeEnable = enabled;
			BossChecklist.BossLogConfig.ProgressionModeDisable = !enabled;
			BossChecklist.BossLogConfig.UnmaskNextBoss = !enabled;
			PendingConfigChange = true; // save the option selected before proceeding
			BossChecklist.BossLogConfig.UpdateIndicators();

			PageNum = Page_TableOfContents; // switch page to Table of Contents when clicked
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
		/// Toggles whether the prompt will show on future characters. This can still be changed under the configs.
		/// </summary>
		private void DisablePromptMessage() {
			BossChecklist.BossLogConfig.PromptDisabled = !BossChecklist.BossLogConfig.PromptDisabled;
			PendingConfigChange = true;
			PromptCheck.SetImage(BossChecklist.BossLogConfig.PromptDisabled ? Texture_Check_Check : Texture_Check_X);
		}

		/// <summary>
		/// Handles the logic behind clicking the next/prev navigation buttons, and thus "turning the page".
		/// </summary>
		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not NavigationalButton button)
				return;
			// Remove new records when navigating from a page with a new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && GetLogEntryInfo.GetRecordIndex != -1)
				modPlayer.hasNewRecord[GetLogEntryInfo.GetRecordIndex] = false;

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
			if (NewPageValue >= 0) {
				// if the new boss page is hidden, unavailable, or otherwise invalid...
				bool HiddenOrUnAvailable = BossList[NewPageValue].hidden || !BossList[NewPageValue].available();
				bool OnlyDisplayBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && BossList[NewPageValue].type != EntryType.Boss;
				if (HiddenOrUnAvailable || OnlyDisplayBosses) {
					// ...repeat the new page calculation until a valid boss page is selected
					// or until its reached page -1 (table of contents) or -2 (credits)
					while (NewPageValue >= 0) {
						EntryInfo currentBoss = BossList[NewPageValue];
						if (!currentBoss.hidden && currentBoss.available() && (!BossChecklist.BossLogConfig.OnlyShowBossContent || currentBoss.type == EntryType.Boss))
							break; // if 'only show bosses' is not enabled or if it IS enabled and the entry is a boss

						// same calulation as before, but repeated until a valid page is selected
						if (button.Id == "Next") {
							NewPageValue = NewPageValue < BossList.Count - 1 ? NewPageValue + 1 : Page_Credits;
						}
						else {
							NewPageValue = NewPageValue >= 0 ? NewPageValue - 1 : BossList.Count - 1;
						}
					}
				}
			}

			PageNum = NewPageValue; // Once a valid page is found, change the page.
		}

		/// <summary>
		/// Updates desired page, subpage, and subcategory when called. Used for buttons that use navigation.
		/// </summary>
		/// <param name="pageNum">The page you want to switch to.</param>
		/// <param name="subPage">The category page you want to set up, which includes record/event data, summoning info, and loot checklist.</param>
		/// <param name="subCategory">The alternate category page you want to display. As of now this just applies for the record category page, which includes last attempt, first record, best record, and world record.</param>
		private void UpdateSelectedPage(int pageNum, SubPage subPage, SubCategory subCategory = SubCategory.None) {
			BossLogPageNumber = pageNum; // Directly change the BossLogPageNumber value in order to prevent an infinite loop

			// Only on boss pages does updating the category page matter
			if (PageNum >= 0) {
				SelectedSubPage = subPage;
				if (subCategory != SubCategory.None)
					RecordSubCategory = subCategory;
			}

			ToCTab.Anchor = PageNum == Page_TableOfContents ? null : -1; // Update ToC/Filter tab anchor (and hover text)
			RefreshPageContent();
		}

		/// <summary>
		/// Restructures all page content and elements without changing the page or subpage.
		/// </summary>
		public void RefreshPageContent() {
			if (PageNum == Page_Prompt) {
				OpenProgressionModePrompt();
				return; // If the page is somehow the prompt, redirect to the open prompt method
			}

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
				if (SelectedSubPage == SubPage.Records) {
					OpenRecord();
				}
				else if (SelectedSubPage == SubPage.SpawnInfo) {
					OpenSpawn();
				}
				else if (SelectedSubPage == SubPage.LootAndCollectibles) {
					OpenLoot();
				}
			}
		}

		/// <summary>
		/// Clears page content and replaces navigational elements.
		/// </summary>
		private void ResetBothPages() {
			PageOne.RemoveAllChildren(); // remove all elements from the pages
			PageTwo.RemoveAllChildren();

			// Replace all of the page's navigational buttons
			if (PageNum != Page_Credits) {
				PageTwo.Append(NextPage); // Next page button can appear on any page except the Credits
			}
			if (PageNum != Page_TableOfContents) {
				PageOne.Append(PrevPage); // Prev page button can appear on any page except the Table of Contents
			}

			if (PageNum >= 0) {
				if (BossChecklist.DebugConfig.AccessInternalNames && GetLogEntryInfo.modSource != "Unknown") {
					NavigationalButton keyButton = new NavigationalButton(Texture_Content_BossKey, true) {
						Id = "CopyKey",
						hoverText = $"{Language.GetTextValue($"{LangLog}.EntryPage.CopyKey")}:\n{GetLogEntryInfo.Key}"
					};
					keyButton.Left.Pixels = 5;
					keyButton.Top.Pixels = 55;
					PageOne.Append(keyButton);
				}

				// Entry pages need to have the category pages set up, but only for entries fully implemented
				if (GetLogEntryInfo.modSource != "Unknown") {
					PageTwo.Append(recordButton);
					PageTwo.Append(spawnButton);
					PageTwo.Append(lootButton);
				}
				else {
					// Old mod calls are no longer supported and will not add entries
					// if somehow the entry has an unknown source, make a panel to show something went wrong
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 3;
					PageTwo.Append(brokenPanel);

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
			prehardmodeList.Clear(); // clear both lists before setting up content
			hardmodeList.Clear();

			// Pre-Hard Mode List Title
			string title = Language.GetTextValue($"{LangLog}.TableOfContents.PreHardmode");
			PageOneTitle.SetText(title);
			PageOneTitle.Left.Pixels = (int)((PageOne.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			PageOne.Append(PageOneTitle);

			// Hard Mode List Title
			title = Language.GetTextValue($"{LangLog}.TableOfContents.Hardmode");
			PageTwoTitle.SetText(title);
			PageTwoTitle.Left.Pixels = (int)((PageTwo.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			PageTwo.Append(PageTwoTitle);

			string hintText = Language.GetTextValue($"{LangLog}.HintTexts.MarkEntry") + "\n" + Language.GetTextValue($"{LangLog}.HintTexts.HideEntry");
			if (BossChecklist.DebugConfig.ResetForcedDowns) {
				hintText += "\n" + Language.GetTextValue($"{BossLogUI.LangLog}.HintTexts.ClearMarked");
			}
			if (BossChecklist.DebugConfig.ResetHiddenEntries) {
				hintText += "\n" + Language.GetTextValue($"{BossLogUI.LangLog}.HintTexts.ClearHidden");
			}

			Asset<Texture2D> icon = RequestVanillaTexture("Images/UI/WorldCreation/IconRandomName");
			NavigationalButton tips = new NavigationalButton(icon, true) {
				hoverText = hintText
			};
			tips.Left.Pixels = PageOneTitle.Left.Pixels / 2 - icon.Value.Width / 2;
			tips.Top.Pixels = PageOneTitle.Top.Pixels - 10;
			PageOne.Append(tips);

			foreach (EntryInfo entry in BossChecklist.bossTracker.SortedEntries) {
				entry.hidden = WorldAssist.HiddenEntries.Contains(entry.Key);

				if (!entry.VisibleOnChecklist())
					continue; // If the boss should not be visible on the Table of Contents, skip the entry in the list

				// Setup display name. Show "???" if unavailable and Silhouettes are turned on
				string displayName = entry.DisplayName;
				BossLogConfiguration cfg = BossChecklist.BossLogConfig;

				bool namesMasked = cfg.MaskNames && !entry.IsDownedOrMarked;
				bool hardMode = cfg.MaskHardMode && !Main.hardMode && entry.progression > BossTracker.WallOfFlesh && !entry.IsDownedOrMarked;
				bool availability = cfg.HideUnavailable && !entry.available() && !entry.IsDownedOrMarked;
				bool maskedButNext = cfg.DrawNextMark && cfg.MaskNames && cfg.UnmaskNextBoss && !entry.IsDownedOrMarked && entry.available() && !entry.hidden && FindNextEntry() == entry.GetIndex;
				if ((namesMasked || hardMode || availability) && !maskedButNext)
					displayName = "???";

				// The first entry that isnt downed to have a nextCheck will set off the next check for the rest
				// Entries that ARE downed will still be green due to the ordering of colors within the draw method
				// Update marked downs. If the boss is actually downed, remove the mark.
				if (entry.MarkedAsDowned) {
					displayName += "*";
					if (WorldAssist.MarkedEntries.Contains(entry.Key) && entry.downed()) {
						WorldAssist.MarkedEntries.Remove(entry.Key);
						Networking.RequestMarkedEntryUpdate(entry.Key, entry.MarkedAsDowned);
					}
				}

				bool allLoot = false;
				bool allCollect = false;
				if (BossChecklist.BossLogConfig.LootCheckVisibility) {
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					allLoot = allCollect = true;

					// Loop through player saved loot and boss loot to see if every item was obtained
					foreach (int loot in entry.lootItemTypes) {
						if (loot == entry.treasureBag)
							continue;

						int index = entry.loot.FindIndex(x => x.itemId == loot);
						if (index != -1 && entry.loot[index].conditions is not null) {
							bool isCorruptionLocked = WorldGen.crimson && entry.loot[index].conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
							bool isCrimsonLocked = !WorldGen.crimson && entry.loot[index].conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

							if (isCorruptionLocked || isCrimsonLocked)
								continue; // Skips items that are dropped within the opposing world evil
						}

						if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && entry.collection.Contains(loot))
							continue; // If the CheckedDroppedLoot config enabled, skip loot items that are considered collectibles for the check

						Item checkItem = ContentSamples.ItemsByType[loot];
						if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
							continue; // Skip items that are expert exclusive if not in an expert world

						if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
							continue; // Skip items that are master exclusive if not in an master world

						// If the item index is not found, end the loop and set allLoot to false
						// If this never occurs, the user successfully obtained all the items!
						if (!modPlayer.BossItemsCollected.Contains(new ItemDefinition(loot))) {
							allLoot = false; // If the item is not located in the player's obtained list, allLoot must be false
							break; // end further item checking
						}
					}

					if (entry.collection.Count == 0) {
						allCollect = allLoot; // If no collection items were setup, consider it false until all loot has been obtained
					}
					else {
						int collectCount = 0;
						foreach (int collectible in entry.collection) {
							if (collectible == -1 || collectible == 0)
								continue; // Skips empty items

							if (BossChecklist.BossLogConfig.OnlyCheckDroppedLoot && !entry.lootItemTypes.Contains(collectible)) {
								collectCount++;
								continue; // If the CheckedDroppedLoot config enabled, skip collectible items that aren't also considered loot
							}

							Item checkItem = ContentSamples.ItemsByType[collectible];
							if (!Main.expertMode && (checkItem.expert || checkItem.expertOnly))
								continue; // Skip items that are expert exclusive if not in an expert world

							if (!Main.masterMode && (checkItem.master || checkItem.masterOnly))
								continue; // Skip items that are master exclusive if not in an master world

							if (!OtherworldUnlocked && BossTracker.otherWorldMusicBoxTypes.Contains(checkItem.type))
								continue;

							if (!modPlayer.BossItemsCollected.Contains(new ItemDefinition(collectible))) {
								allCollect = false; // If the item is not located in the player's obtained list, allCollect must be false
								break; // end further item checking
							}
						}

						if (collectCount == entry.collection.Count)
							allCollect = false; // If all the items were skipped due to the DroppedLootCheck config, don't mark as all collectibles obtained
					}
				}

				Color textColor = Color.PapayaWhip; // default color when ColoredBossText is false
				if ((!entry.available() && !entry.IsDownedOrMarked) || entry.hidden) {
					textColor = Color.DimGray; // Hidden or Unavailable entry text color takes priority over all other text color alterations
				}
				else if (BossChecklist.BossLogConfig.ColoredBossText) {
					if (entry.GetRecordIndex != -1 && Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasNewRecord[entry.GetRecordIndex]) {
						textColor = Main.DiscoColor;
					}
					else {
						textColor = entry.IsDownedOrMarked ? Colors.RarityGreen : Colors.RarityRed;
					}
				}

				TableOfContents listedEntry = new TableOfContents(entry.GetIndex, displayName, textColor, allLoot, allCollect) {
					PaddingTop = 5,
					PaddingLeft = entry.progression <= BossTracker.WallOfFlesh ? 32 : 22
				};

				if (entry.progression <= BossTracker.WallOfFlesh) {
					prehardmodeList.Add(listedEntry);
				}
				else {
					hardmodeList.Add(listedEntry);
				}
			}

			PageOne.Append(prehardmodeList);
			if (prehardmodeList.Count > 13) {
				scrollOne.SetView(100f, 1000f);
				scrollOne.Top.Pixels = 50f;
				scrollOne.Left.Pixels = -18;
				scrollOne.Height.Set(-24f, 0.75f);
				scrollOne.HAlign = 1f;

				PageOne.Append(scrollOne);
				prehardmodeList.SetScrollbar(scrollOne);
			}
			PageTwo.Append(hardmodeList);
			if (hardmodeList.Count > 13) {
				scrollTwo.SetView(100f, 1000f);
				scrollTwo.Top.Pixels = 50f;
				scrollTwo.Left.Pixels = -13;
				scrollTwo.Height.Set(-24f, 0.75f);
				scrollTwo.HAlign = 1f;

				PageTwo.Append(scrollTwo);
				hardmodeList.SetScrollbar(scrollTwo);
			}

			// Order matters here
			prehardmodeBar = new ProgressBar(false);
			prehardmodeBar.Left.Pixels = (int)(PrevPage.Left.Pixels + PrevPage.Width.Pixels + 10);
			prehardmodeBar.Height.Pixels = 14;
			prehardmodeBar.Top.Pixels = (int)(PrevPage.Top.Pixels + (PrevPage.Height.Pixels / 2) - (prehardmodeBar.Height.Pixels / 2));
			prehardmodeBar.Width.Pixels = (int)(PageOne.Width.Pixels - (prehardmodeBar.Left.Pixels * 2));

			// Order matters here
			hardmodeBar = new ProgressBar(true);
			hardmodeBar.Left.Pixels = NextPage.Left.Pixels - 10 - prehardmodeBar.Width.Pixels;
			hardmodeBar.Height.Pixels = 14;
			hardmodeBar.Top.Pixels = prehardmodeBar.Top.Pixels;
			hardmodeBar.Width.Pixels = prehardmodeBar.Width.Pixels;

			PageOne.Append(prehardmodeBar);
			if (!BossChecklist.BossLogConfig.MaskHardMode || Main.hardMode)
				PageTwo.Append(hardmodeBar);
		}

		/// <summary>
		/// Sets up the content need for the credits page,
		/// listing off all mod contributors as well as the mods using the updated mod calls.
		/// </summary>
		private void UpdateCredits() {
			// Developers Title
			string title = Language.GetTextValue($"{LangLog}.Credits.Devs");
			PageOneTitle.SetText(title);
			PageOneTitle.Left.Pixels = (int)((PageOne.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			PageOne.Append(PageOneTitle);

			// Registered Mods Title
			title = Language.GetTextValue($"{LangLog}.Credits.Mods");
			PageTwoTitle.SetText(title);
			PageTwoTitle.Left.Pixels = (int)((PageTwo.Width.Pixels / 2) - (FontAssets.DeathText.Value.MeasureString(title).X * 0.6f / 2));
			PageTwo.Append(PageTwoTitle);

			// Registered Mods subtitle
			title = Language.GetTextValue($"{LangLog}.Credits.Notice");
			UIText subtitle = new UIText(title) {
				TextColor = Color.Salmon
			};
			subtitle.Left.Pixels = (int)((PageTwo.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(title).X / 2));
			subtitle.Top.Pixels = 56;
			PageTwo.Append(subtitle);

			// Developers Display
			UIList creditList = new UIList();
			creditList.Width.Pixels = Texture_Credit_DevSlot.Value.Width;
			creditList.Height.Pixels = Texture_Credit_DevSlot.Value.Height * 4 + 20;
			creditList.Left.Pixels = (int)(PageOne.Width.Pixels / 2 - Texture_Credit_DevSlot.Value.Width / 2) - 8;
			creditList.Top.Pixels = 60;
			foreach (KeyValuePair<string, string> user in contributors) {
				creditList.Add(new ContributorCredit(Texture_Credit_DevSlot, RequestResource($"Credits_{user.Key}"), user.Key, user.Value));
			}
			PageOne.Append(creditList);

			scrollOne.SetView(10f, 1000f);
			scrollOne.Top.Pixels = 80;
			scrollOne.Left.Pixels = -8;
			scrollOne.Height.Set(-60f, 0.75f);
			scrollOne.HAlign = 1f;
			creditList.SetScrollbar(scrollOne);
			PageOne.Append(scrollOne); // scroll bar for developers

			if (BossUISystem.Instance.RegisteredMods.Count > 0) {
				// Registered Mods Display
				pageTwoItemList.Clear();
				pageTwoItemList.Width.Pixels = Texture_Credit_ModSlot.Value.Width;
				pageTwoItemList.Height.Pixels = Texture_Credit_ModSlot.Value.Height * 3 + 15;
				pageTwoItemList.Left.Pixels = (int)(PageTwo.Width.Pixels / 2 - Texture_Credit_ModSlot.Value.Width / 2) - 8;
				pageTwoItemList.Top.Pixels = 85;
				foreach (string mod in BossUISystem.Instance.RegisteredMods.Keys) {
					pageTwoItemList.Add(new ContributorCredit(Texture_Credit_ModSlot, mod));
				}
				PageTwo.Append(pageTwoItemList);

				scrollTwo.SetView(10f, 1000f);
				scrollTwo.Top.Pixels = 87;
				scrollTwo.Left.Pixels = -8;
				scrollTwo.Height.Set(-60f, 0.75f);
				scrollTwo.HAlign = 1f;
				pageTwoItemList.SetScrollbar(scrollTwo);
				if (BossUISystem.Instance.RegisteredMods.Count > 3)
					PageTwo.Append(scrollTwo); // scroll bar for registered mods
			}
			else {
				// No mods are using the updated mod calls to use the Log, so create a text panel to inform the user
				UIPanel brokenPanel = new UIPanel();
				brokenPanel.Height.Pixels = 220;
				brokenPanel.Width.Pixels = 340;
				brokenPanel.Top.Pixels = 120;
				brokenPanel.Left.Pixels = 18;
				PageTwo.Append(brokenPanel);

				FittedTextPanel brokenDisplay = new FittedTextPanel($"{LangLog}.Credits.ModsEmpty");
				brokenDisplay.Height.Pixels = 200;
				brokenDisplay.Width.Pixels = 340;
				brokenDisplay.Top.Pixels = 0;
				brokenDisplay.Left.Pixels = -15;
				brokenPanel.Append(brokenDisplay);
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

			if (GetLogEntryInfo.type == EntryType.Boss) {
				PersonalStats stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld[GetLogEntryInfo.GetRecordIndex].stats;
				bool noKills = stats.kills == 0; // has the player killed this boss before?
				if (noKills && RecordSubCategory != SubCategory.PreviousAttempt && RecordSubCategory != SubCategory.WorldRecord) {
					RecordSubCategory = SubCategory.PreviousAttempt; // If a boss record does not have the selected subcategory type, it should default back to previous attempt.
				}

				if (BossChecklist.DebugConfig.ResetRecordsBool) {
					// TODO: Add the functionaility of clearing a singular record by alt+reight-clicking the record achievement icon
					Asset<Texture2D> icon = RequestVanillaTexture("Images/UI/WorldCreation/IconRandomName");
					NavigationalButton tips = new NavigationalButton(icon, true) {
						hoverText = $"{LangLog}.HintTexts.ClearAllRecords"
					};
					tips.Left.Pixels = lootButton.Left.Pixels / 2 - icon.Value.Width / 2;
					tips.Top.Pixels = lootButton.Top.Pixels + lootButton.Height.Pixels / 2 - icon.Value.Height / 2;
					PageTwo.Append(tips);
				}

				#region Experimental Feature Notice
				// TODO: Experimental feature notice, eventually will need to be removed
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
				bnuuyIcon.Left.Pixels = (PageTwo.Width.Pixels - (lootButton.Left.Pixels / 2) - bnuuy.Value.Width / 2);
				bnuuyIcon.Top.Pixels = lootButton.Top.Pixels;
				PageTwo.Append(bnuuyIcon);
				#endregion
			}

			// create 4 slots for each stat category value
			for (int i = 0; i < 4; i++) {
				if (i > 0 && GetLogEntryInfo.type != EntryType.Boss)
					break; // Mini-bosses and Events only display the first slot

				if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE && i > 0 && RecordSubCategory != SubCategory.WorldRecord)
					break;

				if (BossChecklist.DebugConfig.DisableWorldRecords && i > 0 && RecordSubCategory == SubCategory.WorldRecord)
					break;

				RecordDisplaySlot slot;
				if (GetLogEntryInfo.type == EntryType.Boss) {
					slot = new RecordDisplaySlot(Texture_Content_RecordSlot, RecordSubCategory, i);
					slot.Left.Pixels = PageTwo.Width.Pixels / 2 - Texture_Content_RecordSlot.Value.Width / 2;
					slot.Top.Pixels = 35 + (75 * (i + 1));
					PageTwo.Append(slot);
				}
				else {
					slot = new RecordDisplaySlot(Texture_Content_RecordSlot, null, null);
					slot.Left.Pixels = PageTwo.Width.Pixels / 2 - Texture_Content_RecordSlot.Value.Width / 2;
					slot.Top.Pixels = 35 + (75 * (i + 1));
					PageTwo.Append(slot);
				}

				if (i == 0) {
					if (GetLogEntryInfo.type == EntryType.Boss) {
						Asset<Texture2D> recordIcon = RequestResource($"Nav_Record_{RecordSubCategory}");
						NavigationalButton RecordSubCategoryButton = new NavigationalButton(recordIcon, true) {
							Id = "SubCategory",
							hoverText = $"{LangLog}.Records.Category.Cycle"
						};
						RecordSubCategoryButton.Left.Pixels = slot.Width.Pixels - recordIcon.Value.Width - 15;
						RecordSubCategoryButton.Top.Pixels = slot.Height.Pixels / 2 - recordIcon.Value.Height / 2;
						slot.Append(RecordSubCategoryButton);
					}

					int offset = 0;
					foreach (string entryKey in GetLogEntryInfo.relatedEntries) {
						EntryInfo relatedEntry = BossChecklist.bossTracker.SortedEntries[BossChecklist.bossTracker.SortedEntries.FindIndex(x => x.Key == entryKey)];

						Asset<Texture2D> headIcon = relatedEntry.headIconTextures[0];
						string hoverText = relatedEntry.DisplayName + "\n" + Language.GetTextValue($"{LangLog}.EntryPage.ViewPage");
						Color iconColor = relatedEntry.IsDownedOrMarked ? Color.White : MaskBoss(relatedEntry) == Color.Black ? Color.Black : faded;

						NavigationalButton entryIcon = new NavigationalButton(headIcon, false, iconColor) {
							Id = GetLogEntryInfo.type == EntryType.Event ? "eventIcon" : "bossIcon",
							Anchor = relatedEntry.GetIndex,
							hoverText = hoverText
						};
						entryIcon.Left.Pixels = 15 + offset;
						entryIcon.Top.Pixels = slot.Height.Pixels / 2 - headIcon.Value.Height / 2;
						slot.Append(entryIcon);

						if (GetLogEntryInfo.type == EntryType.Boss || GetLogEntryInfo.type == EntryType.MiniBoss)
							break;

						offset += 10 + headIcon.Value.Width;
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
			PageTwo.Append(message);

			// create a scroll bar in case the message is too long for the box to contain
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Set(91f, 0f);
			scrollTwo.Height.Set(-382f, 1f);
			scrollTwo.Left.Set(-5, 0f);
			scrollTwo.HAlign = 1f;
			PageTwo.Append(scrollTwo);
			message.SetScrollbar(scrollTwo);

			if (SpawnItemSelected >= GetLogEntryInfo.spawnItem.Count)
				SpawnItemSelected = 0; // the selected spawn item number is greater than how many are in the list, so reset it back to 0

			// Once the spawn description has been made, start structuring the spawn items showcase
			// If the spawn item list is empty, inform the player that there are no summon items for the boss/event through text
			if (GetLogEntryInfo.spawnItem.Count == 0 || GetLogEntryInfo.spawnItem[SpawnItemSelected] == ItemID.None) {
				UIText info = new UIText(Language.GetTextValue($"{LangLog}.SpawnInfo.NoSpawnItem", Language.GetTextValue($"{LangLog}.Common.{GetLogEntryInfo.type}")));
				info.Left.Pixels = (PageTwo.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(info.Text).X / 2) - 5;
				info.Top.Pixels = 300;
				PageTwo.Append(info);
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
			PageTwo.Append(spawnItemSlot);

			// if more than one item is used for summoning, append navigational button to cycle through the items
			// a previous item button will appear if it is not the first item listed
			if (SpawnItemSelected > 0) {
				NavigationalButton PrevItem = new NavigationalButton(Texture_Nav_Prev, true) {
					Id = "PrevItem"
				};
				PrevItem.Left.Pixels = spawnItemSlot.Left.Pixels - PrevItem.Width.Pixels - 6;
				PrevItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (PrevItem.Height.Pixels / 2);
				PrevItem.OnLeftClick += ChangeSpawnItem;
				PageTwo.Append(PrevItem);
			}
			// a next button will appear if it is not the last item listed
			if (SpawnItemSelected < GetLogEntryInfo.spawnItem.Count - 1) {
				NavigationalButton NextItem = new NavigationalButton(Texture_Nav_Next, true) {
					Id = "NextItem"
				};
				NextItem.Left.Pixels = spawnItemSlot.Left.Pixels + spawnItemSlot.Width.Pixels + 6;
				NextItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (NextItem.Height.Pixels / 2);
				NextItem.OnLeftClick += ChangeSpawnItem;
				PageTwo.Append(NextItem);
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
				.Where(r => r.HasResult(itemType));

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
				PageTwo.Append(craftText);
				return;
			}
			else {
				// display where the recipe originates form
				string recipeMessage = Language.GetTextValue($"{LangLog}.SpawnInfo.RecipeFrom", recipeMod);
				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = 10;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);

				// if more than one recipe exists for the selected item, append a button that cycles through all possible recipes
				if (TotalRecipes > 1) {
					NavigationalButton CycleItem = new NavigationalButton(Texture_Content_Cycle, true) {
						Id = "CycleItem_" + TotalRecipes,
						hoverText = $"{LangLog}.SpawnInfo.CycleRecipe"
					};
					CycleItem.Left.Pixels = 20 + (int)(TextureAssets.InventoryBack9.Width() * 0.85f / 2 - Texture_Content_Cycle.Value.Width / 2);
					CycleItem.Top.Pixels = 240 + (int)(TextureAssets.InventoryBack9.Height() * 0.85f / 2 - Texture_Content_Cycle.Value.Height / 2);
					CycleItem.OnLeftClick += ChangeSpawnItem;
					PageTwo.Append(CycleItem);
				}
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			// To note, we do not need an item row as recipes have a max ingredient size of 14, so there is no need for a scrollbar
			foreach (Item item in ingredients) {
				// Create an item slot for the current item
				LogItemSlot ingList = new LogItemSlot(item, ItemSlot.Context.GuideItem, 0.85f) {
					Id = $"ingredient_{item.type}"
				};
				ingList.Left.Pixels = 20 + (48 * col);
				ingList.Top.Pixels = 240 + (48 * (row + 1));
				PageTwo.Append(ingList);

				col++;
				// if col hit the max that can be drawn on the page move onto the next row
				if (col == 6) {
					if (row == 1)
						break; // Recipes should not be able to have more than 14 ingredients, so end the loop
					
					if (ingList.item.type == ItemID.None)
						break; // if the current row ends with a blank item, end the loop. this will prevent another row of blank items
					
					col = 0;
					row++;
				}
			}

			if (requiredTiles.Count == 0) {
				// If there were no tiles required for the recipe, add a 'By Hand' slot
				// TODO: Change the Power Glove to the Hand of Creation
				LogItemSlot craftItem = new LogItemSlot(new Item(ItemID.PowerGlove), ItemSlot.Context.EquipArmorVanity, 0.85f) {
					hoverText = $"{LangLog}.SpawnInfo.ByHand"
				};
				craftItem.Top.Pixels = 240 + (48 * (row + 2));
				craftItem.Left.Pixels = 20;
				PageTwo.Append(craftItem);
			}
			else if (requiredTiles.Count > 0) {
				// iterate through all required tiles to list them in item slots
				col = 0; // reset col to zero for the crafting stations
				foreach (int tile in requiredTiles) {
					if (tile == -1)
						break; // Prevents extra empty slots from being created

					string altarType = WorldGen.crimson ? "MapObject.CrimsonAltar" : "MapObject.DemonAltar";
					Item craftStation = new Item(0);
					if (tile != TileID.DemonAltar) {
						// Look for items that create the tile when placed, and use that item for the item slot
						foreach (Item item in ContentSamples.ItemsByType.Values) {
							if (item.createTile == tile) {
								craftStation.SetDefaults(item.type);
								break;
							}
						}
					}

					LogItemSlot tileList = new LogItemSlot(craftStation, ItemSlot.Context.EquipArmorVanity, 0.85f) {
						hoverText = tile == TileID.DemonAltar ? altarType : null
					};
					tileList.Left.Pixels = 20 + (48 * col);
					tileList.Top.Pixels = 240 + (48 * (row + 2));
					PageTwo.Append(tileList);
					col++; // if multiple crafting stations are needed
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
			pageTwoItemList.Clear();
			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 125;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 125 - 80;

			// create an image of the entry's treasure bag
			if (GetLogEntryInfo.treasureBag > 0)
				Main.instance.LoadItem(GetLogEntryInfo.treasureBag);

			Asset<Texture2D> bagTexture = GetLogEntryInfo.treasureBag > 0 ? TextureAssets.Item[GetLogEntryInfo.treasureBag] : RequestResource("Extra_TreasureBag");
			NavigationalButton treasureBag = new NavigationalButton(bagTexture, false);
			treasureBag.Left.Pixels = PageTwo.Width.Pixels / 2 - bagTexture.Value.Width / 2;
			treasureBag.Top.Pixels = 88;
			treasureBag.OnRightClick += RemoveItem;
			PageTwo.Append(treasureBag);

			List<ItemDefinition> obtainedItems = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
			List<int> bossItems = new List<int>(GetLogEntryInfo.lootItemTypes.Union(GetLogEntryInfo.collection)); // combined list of loot and collectibles
			bossItems.Remove(GetLogEntryInfo.treasureBag); // the treasurebag should not be displayed on the loot table, but drawn above it instead

			// Prevents itemslot creation for items that are dropped from within the opposite world evil, if applicable
			if (!Main.drunkWorld && !ModLoader.TryGetMod("BothEvils", out Mod mod)) {
				foreach (DropRateInfo loot in GetLogEntryInfo.loot) {
					if (loot.conditions is null)
						continue;

					bool isCorruptionLocked = WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
					bool isCrimsonLocked = !WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

					if (bossItems.Contains(loot.itemId) && (isCorruptionLocked || isCrimsonLocked))
						bossItems.Remove(loot.itemId);
				}
			}

			// If the boss items contains any otherworld music boxes
			if (bossItems.Intersect(BossTracker.otherWorldMusicBoxTypes).Any()) {
				FieldInfo TOWMusicUnlocked = typeof(Main).GetField("TOWMusicUnlocked", BindingFlags.Static | BindingFlags.NonPublic);
				bool OWUnlocked = (bool)TOWMusicUnlocked.GetValue(null);
				if (OtherworldUnlocked != OWUnlocked)
					OtherworldUnlocked = OWUnlocked;
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			LootRow newRow = new LootRow(0); // the initial row to start with

			foreach (int item in bossItems) {
				Item selectedItem = ContentSamples.ItemsByType[item];
				bool hasObtained = obtainedItems.Any(x => x.Type == item) || obtainedItems.Any(x => x.Type == item);

				// Create an item slot for the current item
				LogItemSlot itemSlot = new LogItemSlot(selectedItem, ItemSlot.Context.TrashItem) {
					Id = "loot_" + item,
					hasItem = hasObtained
				};
				itemSlot.Left.Pixels = (col * 56) + 15;
				itemSlot.OnRightClick += RemoveItem; // debug functionality
				newRow.Append(itemSlot); // append the item slot to the current row

				col++; // increase col before moving on to the next item
				// if col hit the max that can be drawn on the page, add the current row to the list and move onto the next row
				if (col == 6) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row);
				}
			}

			// Once all our items have been added, append the final row to the list
			// This does not need to occur if col is at 0, as the current row is empty
			if (col != 0) {
				row++;
				pageTwoItemList.Add(newRow);
			}

			PageTwo.Append(pageTwoItemList); // append the list to the page so the items can be seen

			// If more than 5 rows exist, a scroll bar is needed to access all items in the loot list
			if (row > 5) {
				scrollTwo.SetView(10f, 1000f);
				scrollTwo.Top.Pixels = 125;
				scrollTwo.Left.Pixels = -3;
				scrollTwo.Height.Set(-88f, 0.75f);
				scrollTwo.HAlign = 1f;

				PageTwo.Append(scrollTwo);
				pageTwoItemList.SetScrollbar(scrollTwo);
			}

			if (BossChecklist.DebugConfig.ResetLootItems) {
				Asset<Texture2D> icon = RequestVanillaTexture("Images/UI/WorldCreation/IconRandomName");
				NavigationalButton tips = new NavigationalButton(icon, true) {
					hoverText = Language.GetTextValue($"{LangLog}.HintTexts.RemoveItem") + "\n" + Language.GetTextValue($"{LangLog}.HintTexts.ClearItems")
				};
				tips.Left.Pixels = lootButton.Left.Pixels / 2 - icon.Value.Width / 2;
				tips.Top.Pixels = lootButton.Top.Pixels + lootButton.Height.Pixels / 2 - icon.Value.Height / 2;
				PageTwo.Append(tips);
			}
		}

		/// <summary>
		/// Used to locate the next available entry for the player to fight that is not defeated, marked as defeated, or hidden.
		/// </summary>
		/// <param name="entryType">Add an entry type to specifically look for the next available entry of that type.</param>
		/// <returns>The index of the next available entry within the SortedEntries list.</returns>
		public static int FindNextEntry(EntryType? entryType = null) => BossChecklist.bossTracker.SortedEntries.FindIndex(x => !x.IsDownedOrMarked && x.VisibleOnChecklist() && (!entryType.HasValue || x.type == entryType));

		/// <summary> Determines if a texture should be masked by a black sihlouette. </summary>
		public static Color MaskBoss(EntryInfo entry) {
			if (!entry.IsDownedOrMarked) {
				if (BossChecklist.BossLogConfig.MaskTextures) {
					return Color.Black;
				}
				else if (!Main.hardMode && entry.progression > BossTracker.WallOfFlesh && BossChecklist.BossLogConfig.MaskHardMode) {
					return Color.Black;
				}
				else if (!entry.available()) {
					return Color.Black;
				}
			}
			return entry.hidden ? Color.Black : Color.White;
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
