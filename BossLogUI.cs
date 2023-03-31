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
		public BossLogPanel BookArea; // The main panel for the UI. All content is aligned within this area.
		public BossLogPanel PageOne; // left page content panel
		public BossLogPanel PageTwo; // right page content panel

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

		// Navigation
		public UIImageButton NextPage;
		public UIImageButton PrevPage;

		public static SubPage SelectedSubPage = SubPage.Records;
		public SubPageButton recordButton;
		public SubPageButton spawnButton;
		public SubPageButton lootButton;

		// Book Tabs
		public BookUI ToCTab; // also used for the filter tab
		public BookUI CreditsTab;
		public BookUI BossTab;
		public BookUI MiniBossTab;
		public BookUI EventTab;
		public BookUI InfoTab; // shows users info about the enabled progression mode
		public BookUI ShortcutsTab; // shows users how to change an entry's hidden/defeation state
		public BookUI filterPanel; // contains the filter buttons
		private List<BookUI> filterCheckBoxes; // checkmarks for the filters
		private List<BookUI> filterCheckMark; // checkmarks for the filters
		public bool filterOpen = false; // when true, the filter panel is visible to the user

		// Table of Contents related
		public UIList prehardmodeList; // lists for pre-hardmode and hardmode entries
		public UIList hardmodeList;
		public ProgressBar prehardmodeBar; // progress bars for pre-hardmode and hardmode entries
		public ProgressBar hardmodeBar;
		public BossLogUIElements.FixedUIScrollbar scrollOne; // scroll bars for table of contents lists (and other elements too)
		public BossLogUIElements.FixedUIScrollbar scrollTwo;
		public bool showHidden = false; // when true, hidden bosses are visible on the list
		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles

		// Record page related
		public SubPageButton[] AltPageButtons;
		public static SubCategory RecordSubCategory = SubCategory.PreviousAttempt;
		public static SubCategory CompareState = SubCategory.None; // Compare record values to one another
		//public static int[] AltPageSelected; // AltPage for Records is "Player Best/World Best(Server)"
		//public static int[] TotalAltPages; // The total amount of "subpages" for Records, Spawn, and Loot pages

		// Spawn Info page related
		public static int SpawnItemSelected = 0;
		public static int RecipeSelected = 0;

		// Loot page related
		public static bool OtherworldUnlocked = false;

		// Cropped Textures
		public static Asset<Texture2D> bookTexture;
		public static Asset<Texture2D> borderTexture;
		public static Asset<Texture2D> fadedTexture;
		public static Asset<Texture2D> tabTexture;
		public static Asset<Texture2D> infoTexture;
		public static Asset<Texture2D> colorTexture;
		public static Asset<Texture2D> bookUITexture;
		public static Asset<Texture2D> prevTexture;
		public static Asset<Texture2D> nextTexture;
		public static Asset<Texture2D> subpageTexture;
		public static Asset<Texture2D> tocTexture;
		public static Asset<Texture2D> credTexture;
		public static Asset<Texture2D> bossNavTexture;
		public static Asset<Texture2D> minibossNavTexture;
		public static Asset<Texture2D> eventNavTexture;
		public static Asset<Texture2D> filterTexture;
		public static Asset<Texture2D> mouseTexture;
		public static Asset<Texture2D> hiddenTexture;
		public static Asset<Texture2D> cycleTexture;
		public static Asset<Texture2D> checkMarkTexture;
		public static Asset<Texture2D> xTexture;
		public static Asset<Texture2D> circleTexture;
		public static Asset<Texture2D> strikeNTexture;
		public static Asset<Texture2D> checkboxTexture;
		public static Asset<Texture2D> chestTexture;
		public static Asset<Texture2D> goldChestTexture;
		public static Asset<Texture2D> prevRecordTexture;
		public static Asset<Texture2D> bestRecordTexture;
		public static Asset<Texture2D> firstRecordTexture;
		public static Asset<Texture2D> worldRecordTexture;
		public static Asset<Texture2D> creditModSlot;
		public static Asset<Texture2D> recordSlot;

		// Extra stuff
		public static int headNum = -1;
		public static Rectangle slotRectRef; // just grabs the size of a normal inventory slot
		public static readonly Color faded = new Color(128, 128, 128, 128);
		public UIImage PromptCheck; // checkmark for the toggle prompt config button

		// Boss Log visibiltiy helpers
		private bool bossLogVisible;
		internal static bool PendingToggleBossLogUI; // Allows toggling boss log visibility from methods not run during UIScale so Main.screenWidth/etc are correct for ResetUIPositioning method
		
		/// <summary>
		/// Appends or removes UI elements based on the visibility status it is set to.
		/// </summary>
		public bool BossLogVisible {
			get => bossLogVisible;
			set {
				if (value) {
					Append(BookArea);
					Append(ShortcutsTab);
					Append(InfoTab);
					Append(ToCTab);
					Append(filterPanel);
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
					RemoveChild(filterPanel);
					RemoveChild(ToCTab);
					RemoveChild(InfoTab);
					RemoveChild(ShortcutsTab);
					RemoveChild(BookArea);
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

					// When opening for the first time, check if the Progression Mode prompt is enabled and provide the prompt
					// If the prompt is disabled, just set the page to the Table of Contents.
					// TODO: Disabled progression mode related stuff until it is reworked on until an issue with player data is resolved
					// TODO: remove false from if statement once fixed
					if (!BossChecklist.BossLogConfig.PromptDisabled) {
						PageNum = Page_Prompt; // All page logic is handled in this method, so return afterwards.
					}
					else {
						PageNum = Page_TableOfContents;
					}
				}
				else if (modPlayer.enteredWorldReset) {
					// If the Log has been opened before, check for a world change.
					// This is to reset the page from what the user previously had back to the Table of Contents when entering another world.
					modPlayer.enteredWorldReset = false;
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
			else if (PageNum >= 0) {
				// If UI is closed on a new record page, remove the new record from the list
				int selectedEntryIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
				if (selectedEntryIndex != -1 && modPlayer.hasNewRecord.Length > 0) {
					modPlayer.hasNewRecord[selectedEntryIndex] = false;
				}
			}

			BossLogVisible = show; // Setting the state makes the UIElements append/remove making them visible/invisible
		}

		public Asset<Texture2D> RequestResource(string path) => ModContent.Request<Texture2D>("BossChecklist/Resources/" + path, AssetRequestMode.ImmediateLoad);

		public override void OnInitialize() {
			bookTexture = RequestResource("Book_Outline");
			borderTexture = RequestResource("Book_Border");
			fadedTexture = RequestResource("Book_Faded");
			colorTexture = RequestResource("Book_Color");
			bookUITexture = RequestResource("LogUI_Back");
			tabTexture = RequestResource("LogUI_Tab");
			infoTexture = RequestResource("LogUI_InfoTab");

			prevTexture = RequestResource("Nav_Prev");
			nextTexture = RequestResource("Nav_Next");
			subpageTexture = RequestResource("Nav_Subpage");
			tocTexture = RequestResource("Nav_Contents");
			credTexture = RequestResource("Nav_Credits");
			bossNavTexture = RequestResource("Nav_Boss");
			minibossNavTexture = RequestResource("Nav_Miniboss");
			eventNavTexture = RequestResource("Nav_Event");
			filterTexture = RequestResource("Nav_Filter");
			mouseTexture = RequestResource("Extra_Shortcuts");
			hiddenTexture = RequestResource("Nav_Hidden");
			cycleTexture = RequestResource("Extra_CycleRecipe");

			checkMarkTexture = RequestResource("Checks_Check");
			xTexture = RequestResource("Checks_X");
			circleTexture = RequestResource("Checks_Next");
			strikeNTexture = RequestResource("Checks_StrikeNext");
			checkboxTexture = RequestResource("Checks_Box");
			chestTexture = RequestResource("Checks_Chest");
			goldChestTexture = RequestResource("Checks_GoldChest");

			prevRecordTexture = RequestResource("Nav_RecordPrev");
			bestRecordTexture = RequestResource("Nav_RecordBest");
			firstRecordTexture = RequestResource("Nav_RecordFirst");
			worldRecordTexture = RequestResource("Nav_RecordWorld");

			creditModSlot = RequestResource("Extra_CreditModSlot");
			recordSlot = RequestResource("Extra_RecordSlot");

			slotRectRef = TextureAssets.InventoryBack.Value.Bounds;

			bosslogbutton = new OpenLogButton(bookTexture);
			bosslogbutton.Width.Set(34, 0f);
			bosslogbutton.Height.Set(38, 0f);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnClick += (a, b) => ToggleBossLog(true);

			/* Keep incase more alt pages are reintroduced
			TotalAltPages = new int[] {
				4, // Record types have their own pages (Last, Best, First, World)
				1, // All spawn info is on one page
				1 // Loot and Collectibles occupy the same page
			};
			*/

			BookArea = new BossLogPanel();
			BookArea.Width.Pixels = bookUITexture.Value.Width;
			BookArea.Height.Pixels = bookUITexture.Value.Height;

			InfoTab = new BookUI(infoTexture) {
				Id = "Info_Tab"
			};
			InfoTab.Width.Pixels = infoTexture.Value.Width;
			InfoTab.Height.Pixels = infoTexture.Value.Height;

			ShortcutsTab = new BookUI(infoTexture) {
				Id = "Shortcut_Tab"
			};
			ShortcutsTab.Width.Pixels = infoTexture.Value.Width;
			ShortcutsTab.Height.Pixels = infoTexture.Value.Height;

			ToCTab = new BookUI(tabTexture) {
				Id = "ToCFilter_Tab"
			};
			ToCTab.Width.Pixels = tabTexture.Value.Width;
			ToCTab.Height.Pixels = tabTexture.Value.Height;
			ToCTab.OnClick += OpenViaTab;
			ToCTab.OnRightClick += (a, b) => ClearForcedDowns();

			BossTab = new BookUI(tabTexture) {
				Id = "Boss_Tab"
			};
			BossTab.Width.Pixels = tabTexture.Value.Width;
			BossTab.Height.Pixels = tabTexture.Value.Height;
			BossTab.OnClick += OpenViaTab;

			MiniBossTab = new BookUI(tabTexture) {
				Id = "Miniboss_Tab"
			};
			MiniBossTab.Width.Pixels = tabTexture.Value.Width;
			MiniBossTab.Height.Pixels = tabTexture.Value.Height;
			MiniBossTab.OnClick += OpenViaTab;

			EventTab = new BookUI(tabTexture) {
				Id = "Event_Tab"
			};
			EventTab.Width.Pixels = tabTexture.Value.Width;
			EventTab.Height.Pixels = tabTexture.Value.Height;
			EventTab.OnClick += OpenViaTab;

			CreditsTab = new BookUI(tabTexture) {
				Id = "Credits_Tab"
			};
			CreditsTab.Width.Pixels = tabTexture.Value.Width;
			CreditsTab.Height.Pixels = tabTexture.Value.Height;
			CreditsTab.OnClick += OpenViaTab;

			PageOne = new BossLogPanel() {
				Id = "PageOne",
			};
			PageOne.Width.Pixels = 375;
			PageOne.Height.Pixels = 480;

			PrevPage = new NavigationalButton(prevTexture) {
				Id = "Previous"
			};
			PrevPage.Width.Pixels = prevTexture.Value.Width;
			PrevPage.Height.Pixels = prevTexture.Value.Height;
			PrevPage.Left.Pixels = 8;
			PrevPage.Top.Pixels = 416;
			PrevPage.OnClick += PageChangerClicked;

			prehardmodeList = new UIList();
			prehardmodeList.Left.Pixels = 4;
			prehardmodeList.Top.Pixels = 44;
			prehardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			prehardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			prehardmodeList.PaddingTop = 5;

			// Order matters here
			prehardmodeBar = new ProgressBar();
			prehardmodeBar.Left.Pixels = PrevPage.Left.Pixels + PrevPage.Width.Pixels + 10;
			prehardmodeBar.Height.Pixels = 14;
			prehardmodeBar.Top.Pixels = PrevPage.Top.Pixels + (PrevPage.Height.Pixels / 2) - (prehardmodeBar.Height.Pixels / 2);
			prehardmodeBar.Width.Pixels = PageOne.Width.Pixels - (prehardmodeBar.Left.Pixels * 2);

			PageTwo = new BossLogPanel() {
				Id = "PageTwo"
			};
			PageTwo.Width.Pixels = 375;
			PageTwo.Height.Pixels = 480;

			pageTwoItemList = new UIList();

			Asset<Texture2D> filterPanelTexture = RequestResource("LogUI_Filter");
			filterPanel = new BookUI(filterPanelTexture) {
				Id = "filterPanel"
			};
			filterPanel.Height.Pixels = 166;
			filterPanel.Width.Pixels = 50;

			filterCheckMark = new List<BookUI>();
			filterCheckBoxes = new List<BookUI>();

			List<Asset<Texture2D>> filterNav = new List<Asset<Texture2D>>() {
				bossNavTexture,
				minibossNavTexture,
				eventNavTexture,
				hiddenTexture
			};

			for (int i = 0; i < 4; i++) {
				BookUI newCheck = new BookUI(checkMarkTexture) {
					Id = "C_" + i
				};
				newCheck.Left.Pixels = filterNav[i].Value.Width * 0.56f;
				newCheck.Top.Pixels = filterNav[i].Value.Height * 3 / 8;
				filterCheckMark.Add(newCheck);

				BookUI newCheckBox = new BookUI(filterNav[i]) {
					Id = "F_" + i
				};
				newCheckBox.Top.Pixels = (34 * i) + 15;
				newCheckBox.Left.Pixels = (25) - (filterNav[i].Value.Width / 2);
				newCheckBox.OnClick += ChangeFilter;
				if (filterNav[i] == hiddenTexture) {
					newCheckBox.OnRightClick += (a, b) => ClearHiddenList();
				}
				newCheckBox.Append(filterCheckMark[i]);
				filterCheckBoxes.Add(newCheckBox);
			}

			// Setup the inital checkmarks to display what the user has prematurely selected
			Filters_SetImage();

			// Append the filter checks to the filter panel
			foreach (BookUI uiimage in filterCheckBoxes) {
				filterPanel.Append(uiimage);
			}

			NextPage = new NavigationalButton(nextTexture) {
				Id = "Next"
			};
			NextPage.Width.Pixels = nextTexture.Value.Width;
			NextPage.Height.Pixels = nextTexture.Value.Height;
			NextPage.Left.Pixels = PageTwo.Width.Pixels - NextPage.Width.Pixels - 12;
			NextPage.Top.Pixels = 416;
			NextPage.OnClick += PageChangerClicked;
			PageTwo.Append(NextPage);

			hardmodeList = new UIList();
			hardmodeList.Left.Pixels = 19;
			hardmodeList.Top.Pixels = 44;
			hardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			hardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			hardmodeList.PaddingTop = 5;

			// Order matters here
			hardmodeBar = new ProgressBar();
			hardmodeBar.Left.Pixels = NextPage.Left.Pixels - 10 - prehardmodeBar.Width.Pixels;
			hardmodeBar.Height.Pixels = 14;
			hardmodeBar.Top.Pixels = NextPage.Top.Pixels + (NextPage.Height.Pixels / 2) - (hardmodeBar.Height.Pixels / 2);
			hardmodeBar.Width.Pixels = prehardmodeBar.Width.Pixels;

			recordButton = new SubPageButton(subpageTexture, SubPage.Records);
			recordButton.Width.Pixels = subpageTexture.Value.Width;
			recordButton.Height.Pixels = subpageTexture.Value.Height;
			recordButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 - (int)recordButton.Width.Pixels - 8;
			recordButton.Top.Pixels = 5;
			recordButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.Records);
			recordButton.OnRightClick += (a, b) => ResetStats();

			spawnButton = new SubPageButton(subpageTexture, SubPage.SpawnInfo);
			spawnButton.Width.Pixels = subpageTexture.Value.Width;
			spawnButton.Height.Pixels = subpageTexture.Value.Height;
			spawnButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 + 8;
			spawnButton.Top.Pixels = 5;
			spawnButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.SpawnInfo);

			lootButton = new SubPageButton(subpageTexture, SubPage.LootAndCollectibles);
			lootButton.Width.Pixels = subpageTexture.Value.Width;
			lootButton.Height.Pixels = subpageTexture.Value.Height;
			lootButton.Left.Pixels = (int)PageTwo.Width.Pixels / 2 - (int)lootButton.Width.Pixels / 2;
			lootButton.Top.Pixels = 5 + subpageTexture.Value.Height + 10;
			lootButton.OnClick += (a, b) => UpdateSelectedPage(PageNum, SubPage.LootAndCollectibles);
			lootButton.OnRightClick += RemoveItem;

			SubPageButton PrevRecordButton = new SubPageButton(prevRecordTexture, SubCategory.PreviousAttempt);
			PrevRecordButton.OnClick += (a, b) => HandleRecordTypeButton(SubCategory.PreviousAttempt);
			PrevRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(SubCategory.PreviousAttempt, false);

			SubPageButton BestRecordButton = new SubPageButton(bestRecordTexture, SubCategory.PersonalBest);
			BestRecordButton.OnClick += (a, b) => HandleRecordTypeButton(SubCategory.PersonalBest);
			BestRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(SubCategory.PersonalBest, false);

			SubPageButton FirstRecordButton = new SubPageButton(firstRecordTexture, SubCategory.FirstVictory);
			FirstRecordButton.OnClick += (a, b) => HandleRecordTypeButton(SubCategory.FirstVictory);
			FirstRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(SubCategory.FirstVictory, false);

			SubPageButton WorldRecordButton = new SubPageButton(worldRecordTexture, SubCategory.WorldRecord);
			WorldRecordButton.OnClick += (a, b) => HandleRecordTypeButton(SubCategory.WorldRecord);
			WorldRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(SubCategory.WorldRecord, false);

			AltPageButtons = new SubPageButton[] {
				PrevRecordButton,
				BestRecordButton,
				FirstRecordButton,
				WorldRecordButton
			};

			// scroll one currently only appears for the table of contents, so its fields can be set here
			scrollOne = new BossLogUIElements.FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			// scroll two is used in more areas, such as the display spawn info message box, so its fields are set when needed
			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
		}

		public override void Update(GameTime gameTime) {
			if (PendingToggleBossLogUI) {
				PendingToggleBossLogUI = false;
				ToggleBossLog(!BossLogVisible);
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
				BossInfo entry = BossChecklist.bossTracker.SortedBosses[headNum];
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
		public void ResetUIPositioning() {
			// Reset the position of the button to make sure it updates with the screen res
			BookArea.Left.Pixels = (Main.screenWidth / 2) - (BookArea.Width.Pixels / 2);
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (BookArea.Height.Pixels / 2) - 6;
			PageOne.Left.Pixels = BookArea.Left.Pixels + 20;
			PageOne.Top.Pixels = BookArea.Top.Pixels + 12;
			PageTwo.Left.Pixels = BookArea.Left.Pixels - 15 + BookArea.Width.Pixels - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = BookArea.Top.Pixels + 12;

			ShortcutsTab.Left.Pixels = BookArea.Left.Pixels + 40;
			ShortcutsTab.Top.Pixels = BookArea.Top.Pixels - infoTexture.Value.Height + 8;

			InfoTab.Left.Pixels = ShortcutsTab.Left.Pixels + ShortcutsTab.Width.Pixels - 8;
			InfoTab.Top.Pixels = ShortcutsTab.Top.Pixels;

			int offsetY = 50;

			// ToC/Filter Tab and Credits Tab never flips to the other side, just disappears when on said page
			ToCTab.Left.Pixels = BookArea.Left.Pixels - 20;
			ToCTab.Top.Pixels = BookArea.Top.Pixels + offsetY;
			CreditsTab.Left.Pixels = BookArea.Left.Pixels + BookArea.Width.Pixels - 12;
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);
			filterPanel.Top.Pixels = -5000; // throw offscreen

			// Reset book tabs Y positioning after BookArea adjusted
			BossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 1);
			MiniBossTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 2);
			EventTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 3);
			CreditsTab.Top.Pixels = BookArea.Top.Pixels + offsetY + (BossTab.Height.Pixels * 4);

			// Update the navigation tabs to the proper positions
			// This does not need to occur if the Progression prompt is shown, as they are not visible
			if (PageNum != Page_Prompt) {
				BossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Boss) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				MiniBossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.MiniBoss) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				EventTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Event) || PageNum == Page_Credits ? -20 : BookArea.Width.Pixels - 12);
				UpdateFilterTabPos(false); // Update filter tab visibility
			}
		}

		/// <summary>
		/// Toggles the visibility state of the filter panel. This can occur when the tab is clicked or if the page is changed.
		/// </summary>
		/// <param name="tabClicked"></param>
		private void UpdateFilterTabPos(bool tabClicked) {
			if (tabClicked) {
				filterOpen = !filterOpen;
			}
			if (PageNum != Page_TableOfContents) {
				filterOpen = false;
			}

			if (filterOpen) {
				filterPanel.Top.Pixels = ToCTab.Top.Pixels;
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20 - filterPanel.Width.Pixels;
				filterPanel.Left.Pixels = ToCTab.Left.Pixels + ToCTab.Width.Pixels;
			}
			else {
				ToCTab.Left.Pixels = BookArea.Left.Pixels - 20;
				filterPanel.Top.Pixels = -5000; // throw offscreen
			}
		}

		/// <summary>
		/// The logic behind the filters changing images when toggled.
		/// </summary>
		private void Filters_SetImage() {
			// ...Bosses
			filterCheckMark[0].SetImage(BossChecklist.BossLogConfig.FilterBosses == "Show" ? checkMarkTexture : circleTexture);

			// ...Mini-Bosses
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				filterCheckMark[1].SetImage(xTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
				filterCheckMark[1].SetImage(checkMarkTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") {
				filterCheckMark[1].SetImage(xTexture);
			}
			else {
				filterCheckMark[1].SetImage(circleTexture);
			}

			// ...Events
			if (BossChecklist.BossLogConfig.OnlyShowBossContent) {
				filterCheckMark[2].SetImage(xTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
				filterCheckMark[2].SetImage(checkMarkTexture);
			}
			else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") {
				filterCheckMark[2].SetImage(xTexture);
			}
			else {
				filterCheckMark[2].SetImage(circleTexture);
			}

			// ...Hidden Entries
			filterCheckMark[3].SetImage(showHidden ? checkMarkTexture : xTexture);
		}

		/// <summary>
		/// Cycles through the visibility state of the selected filter,
		/// including bosses, mini-bosses, events and hidden entries.
		/// </summary>
		private void ChangeFilter(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI filter)
				return;

			string filterID = filter.Id.Substring(2, 1);
			if (filterID == "0") {
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") {
					BossChecklist.BossLogConfig.FilterBosses = "Hide when completed";
				}
				else {
					BossChecklist.BossLogConfig.FilterBosses = "Show";
				}
			}
			else if (filterID == "1" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Hide when completed";
				}
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide when completed") {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Hide";
				}
				else {
					BossChecklist.BossLogConfig.FilterMiniBosses = "Show";
				}
			}
			else if (filterID == "2" && !BossChecklist.BossLogConfig.OnlyShowBossContent) {
				if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
					BossChecklist.BossLogConfig.FilterEvents = "Hide when completed";
				}
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide when completed") {
					BossChecklist.BossLogConfig.FilterEvents = "Hide";
				}
				else {
					BossChecklist.BossLogConfig.FilterEvents = "Show";
				}
			}
			else if (filterID == "3") {
				showHidden = !showHidden;
			}

			// Save the filters to the configs, update the checkmark images and refresh the page to update the table of content lists.
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			Filters_SetImage();
			RefreshPageContent();
		}

		// TODO: [??] Implement separate Reset tabs? Including: Clear Hidden List, Clear Forced Downs, Clear Records, Clear Boss Loot, etc

		private void ClearHiddenList() {
			if (!BossChecklist.DebugConfig.ResetHiddenEntries || WorldAssist.HiddenBosses.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.HiddenBosses.Clear();
			showHidden = false;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestClearHidden);
				packet.Send();
			}
			RefreshPageContent();
		}

		private void ClearForcedDowns() {
			if (!BossChecklist.DebugConfig.ResetForcedDowns || WorldAssist.ForcedMarkedEntries.Count == 0)
				return;

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return;

			WorldAssist.ForcedMarkedEntries.Clear();
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestClearForceDowns);
				packet.Send();
			}
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
			int recordIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
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
				return; // do not do anything if not on the loot page (ex. can't remove loot on spawn info page)

			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt))
				return; // player must be holding alt

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			// Alt right-click the "Loot / Collection" button to entirely clear the selected boss page's loot/collection list
			// Alt right-click an item slot to remove that item from the selected boss page's loot/collection list
			// Note: items removed are removed from ALL boss loot pages retroactively
			if (listeningElement is SubPageButton) {
				modPlayer.BossItemsCollected.Clear();
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
		public void OpenProgressionModePrompt() {
			BossLogPageNumber = Page_Prompt; // make sure the page number is updated directly (using PageNum will trigger the page set up)
			ResetUIPositioning(); // Updates ui elements and tabs to be properly positioned in relation the the new pagenum
			PageOne.RemoveAllChildren(); // remove all content from both pages before appending new content for the prompt
			PageTwo.RemoveAllChildren();

			// create a text box for the progression mode description
			FittedTextPanel textBox = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.ProgressionModeDescription");
			textBox.Width.Pixels = PageOne.Width.Pixels - 30;
			textBox.Height.Pixels = PageOne.Height.Pixels - 70;
			textBox.Left.Pixels = 10;
			textBox.Top.Pixels = 60;
			PageOne.Append(textBox);

			// create buttons for the different progression mode options
			Asset<Texture2D> backdropTexture = RequestResource("Extra_RecordSlot");
			UIImage[] backdrops = new UIImage[] {
				new UIImage(backdropTexture),
				new UIImage(backdropTexture),
				new UIImage(backdropTexture),
				new UIImage(backdropTexture)
			};

			Color bookColor = BossChecklist.BossLogConfig.BossLogColor;

			backdrops[0].OnClick += (a, b) => ContinueDisabled();
			backdrops[0].OnMouseOver += (a, b) => { backdrops[0].Color = bookColor; };
			backdrops[0].OnMouseOut += (a, b) => { backdrops[0].Color = Color.White; };

			backdrops[1].OnClick += (a, b) => ContinueEnabled();
			backdrops[1].OnMouseOver += (a, b) => { backdrops[1].Color = bookColor; };
			backdrops[1].OnMouseOut += (a, b) => { backdrops[1].Color = Color.White; };

			backdrops[2].OnClick += (a, b) => CloseAndConfigure();
			backdrops[2].OnMouseOver += (a, b) => { backdrops[2].Color = bookColor; };
			backdrops[2].OnMouseOut += (a, b) => { backdrops[2].Color = Color.White; };

			backdrops[3].OnClick += (a, b) => DisablePromptMessage();
			backdrops[3].OnMouseOver += (a, b) => { backdrops[3].Color = bookColor; };
			backdrops[3].OnMouseOut += (a, b) => { backdrops[3].Color = Color.White; };

			Asset<Texture2D>[] buttonTextures = new Asset<Texture2D>[] {
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.SteampunkGoggles}", AssetRequestMode.ImmediateLoad),
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.Blindfold}", AssetRequestMode.ImmediateLoad),
				ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.Wrench}", AssetRequestMode.ImmediateLoad),
				checkboxTexture
			};

			UIImage[] buttons = new UIImage[] {
				new UIImage(buttonTextures[0]),
				new UIImage(buttonTextures[1]),
				new UIImage(buttonTextures[2]),
				new UIImage(buttonTextures[3])
			};

			FittedTextPanel[] textOptions = new FittedTextPanel[] {
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.DisableProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.EnableProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.ConfigProgressMode"),
				new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.DisableProgressPrompt"),
			};

			PromptCheck = new UIImage(BossChecklist.BossLogConfig.PromptDisabled ? checkMarkTexture : xTexture);

			for (int i = 0; i < buttonTextures.Length; i++) {
				backdrops[i].Width.Pixels = backdropTexture.Value.Width;
				backdrops[i].Height.Pixels = backdropTexture.Value.Height;
				backdrops[i].Left.Pixels = 25;
				backdrops[i].Top.Pixels = 75 + (75 * i);

				buttons[i].Width.Pixels = buttonTextures[i].Value.Width;
				buttons[i].Height.Pixels = buttonTextures[i].Value.Height;
				buttons[i].Left.Pixels = 15;
				buttons[i].Top.Pixels = backdrops[i].Height.Pixels / 2 - buttons[i].Height.Pixels / 2;

				textOptions[i].Width.Pixels = backdrops[i].Width.Pixels - (buttons[i].Left.Pixels + buttons[i].Width.Pixels + 15);
				textOptions[i].Height.Pixels = backdrops[i].Height.Pixels;
				textOptions[i].Left.Pixels = buttons[i].Left.Pixels + buttons[i].Width.Pixels;
				textOptions[i].Top.Pixels = -10;
				textOptions[i].PaddingTop = 0;
				textOptions[i].PaddingLeft = 15;

				if (i == buttonTextures.Length - 1) {
					buttons[i].Append(PromptCheck);
				}
				backdrops[i].Append(buttons[i]);
				backdrops[i].Append(textOptions[i]);
				PageTwo.Append(backdrops[i]);
			}
		}

		/// <summary>
		/// Fully disables Progression Mode and redirects the player to the Table of Contents.
		/// </summary>
		public void ContinueDisabled() {
			BossChecklist.BossLogConfig.MaskTextures = false;
			BossChecklist.BossLogConfig.MaskNames = false;
			BossChecklist.BossLogConfig.UnmaskNextBoss = true;
			BossChecklist.BossLogConfig.MaskBossLoot = false;
			BossChecklist.BossLogConfig.MaskHardMode = false;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

			PageNum = Page_TableOfContents; // switch page to Table of Contents when clicked
		}

		/// <summary>
		/// Fully enables Progression Mode and redirects the player to the Table of Contents.
		/// </summary>
		public void ContinueEnabled() {
			BossChecklist.BossLogConfig.MaskTextures = true;
			BossChecklist.BossLogConfig.MaskNames = true;
			BossChecklist.BossLogConfig.UnmaskNextBoss = false;
			BossChecklist.BossLogConfig.MaskBossLoot = true;
			BossChecklist.BossLogConfig.MaskHardMode = true;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

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
		public void DisablePromptMessage() {
			BossChecklist.BossLogConfig.PromptDisabled = !BossChecklist.BossLogConfig.PromptDisabled;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			if (BossChecklist.BossLogConfig.PromptDisabled) {
				PromptCheck.SetImage(checkMarkTexture);
			}
			else {
				PromptCheck.SetImage(xTexture);
			}
		}

		/// <summary>
		/// Navigational logic for clicking on the record type buttons.
		/// Left-clicks will swap to that record category. Right-clicks will set the compare state to the record category.
		/// <para>Note: Handling buttons within the for loop upon button creation makes altPage reach max value. This method seems to be a good substitute.</para>
		/// </summary>
		/// <param name="type">The record category you are chainging</param>
		/// <param name="leftClick"></param>
		public void HandleRecordTypeButton(SubCategory type, bool leftClick = true) {
			// If left-clicking, there is no point in changing the page to the one the player is already on
			// If right-clicking, we cannot compare the record to itself
			if (RecordSubCategory == type)
				return; // in either case, just do nothing

			if (!leftClick) {
				// If it is already the compare state, reset the compare state to off. Otherwise, just set it to the selected type.
				CompareState = CompareState == type ? SubCategory.None : type;
			}
			else {
				if (CompareState == type) {
					CompareState = SubCategory.None; // If switching to the compared state, reset compare state to off.
				}
				UpdateSelectedPage(PageNum, SelectedSubPage, type); // update the record category to the new type
			}
		}

		/// <summary>
		/// Handlesthe logic for interacting with the Table of Content texts.
		/// Left or right clicking will jump to the boss's page.
		/// Holding alt while left-clicking will mark the boss as defeated.
		/// Holding alt while right-clicking will hide the boss from the table of contents.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="leftClick"></param>
		internal void JumpToBossPage(int index, bool leftClick = true) {
			if (!Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt)) {
				PageNum = index; // jump to boss page
			}
			else {
				// While holding alt, a user can interact with any boss list entry
				// Left-clicking forces a completion check on or off
				// Right-clicking hides the boss from the list
				BossInfo entry = BossChecklist.bossTracker.SortedBosses[index];
				if (leftClick) {
					// toggle defeation state and update the world save data
					if (WorldAssist.ForcedMarkedEntries.Contains(entry.Key)) {
						WorldAssist.ForcedMarkedEntries.Remove(entry.Key);
					}
					else if (!entry.downed()) {
						WorldAssist.ForcedMarkedEntries.Add(entry.Key);
					}

					// handle the global update with a packet
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.RequestForceDownBoss);
						packet.Write(entry.Key);
						packet.Write(entry.ForceDowned);
						packet.Send();
					}
				}
				else {
					// toggle hidden state and update the world save data
					entry.hidden = !entry.hidden;
					if (entry.hidden) {
						WorldAssist.HiddenBosses.Add(entry.Key);
					}
					else {
						WorldAssist.HiddenBosses.Remove(entry.Key);
					}

					// handle the global update with a packet and update the legacy checklist as well
					BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.RequestHideBoss);
						packet.Write(entry.Key);
						packet.Write(entry.hidden);
						packet.Send();
					}
				}
				RefreshPageContent(); // update the checklist by refreshing page content
			}
		}

		/// <summary>
		/// Contains the logic needed for the book tabs.
		/// </summary>
		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string id = book.Id;
			if (PageNum == Page_Prompt || !BookUI.DrawTab(id))
				return; // if the page is on the prompt or if the tab isn't drawn to begin with, no logic should be run

			if (id == "ToCFilter_Tab" && PageNum == Page_TableOfContents) {
				UpdateFilterTabPos(true);
				return; // if it was the filter tab, just open the tab without changing or refrshing the page
			}

			// Remove new records when navigating from a page with a new record
			if (PageNum >= 0) {
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				BossInfo entry = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (entry.GetRecordIndex != -1) {
					modPlayer.hasNewRecord[entry.GetRecordIndex] = false;
				}
			}

			// determine which page to open base on tab ID
			if (id == "Boss_Tab") {
				PageNum = FindNext(EntryType.Boss);
			}
			else if (id == "Miniboss_Tab") {
				PageNum = FindNext(EntryType.MiniBoss);
			}
			else if (id == "Event_Tab") {
				PageNum = FindNext(EntryType.Event);
			}
			else if (id == "Credits_Tab") {
				PageNum = Page_Credits;
			}
			else {
				PageNum = Page_TableOfContents;
			}
		}

		/// <summary>
		/// Handles the logic behind clicking the next/prev navigation buttons, and thus "turning the page".
		/// </summary>
		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not NavigationalButton button)
				return;

			// Remove new records when navigating from a page with a new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex != -1) {
				modPlayer.hasNewRecord[BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex] = false;
			}

			// Calculate what page the Log needs to update to
			List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
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
						BossInfo currentBoss = BossList[NewPageValue];
						if (!currentBoss.hidden && currentBoss.available()) {
							if (!BossChecklist.BossLogConfig.OnlyShowBossContent) {
								break; // if 'only show bosses' is not enabled
							}
							else if (currentBoss.type == EntryType.Boss) {
								break; // or if it IS enabled and the entry is a boss
							}
						}

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
		/// Updates desired page and subcategory when called. Used for buttons that use navigation.
		/// </summary>
		/// <param name="pageNum">The page you want to switch to.</param>
		/// <param name="catPage">The category page you want to set up, which includes record/event data, summoning info, and loot checklist. Defaults to the record page.</param>
		/// <param name="altPage">The alternate category page you want to display. As of now this just applies for the record category page, which includes last attempt, first record, best record, and world record.</param>
		public void UpdateSelectedPage(int pageNum, SubPage catPage = SubPage.Records, SubCategory altPage = SubCategory.None) {
			BossLogPageNumber = pageNum; // Directly change the BossLogPageNumber value in order to prevent an infinite loop
			
			// Only on boss pages does updating the category page matter
			if (PageNum >= 0) {
				SelectedSubPage = catPage;
				if (altPage != SubCategory.None) {
					RecordSubCategory = altPage;
				}
			}

			RefreshPageContent();
		}

		/// <summary>
		/// Restructures all page content and elements without changing the page or subcategory.
		/// </summary>
		public void RefreshPageContent() {
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
			if (PageNum == -3) {
				OpenProgressionModePrompt();
				return; // If the page is somehow the prompt, redirect to the open prompt method
			}

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
				// Entry pages need to have the category pages set up, but only for entries fully implemented
				BossInfo entry = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (entry.modSource != "Unknown") {
					PageTwo.Append(recordButton);
					PageTwo.Append(spawnButton);
					PageTwo.Append(lootButton);
				}
				else {
					// if the boss has an unknown source, it is likely that the mod call for it is using the old mod call
					// this should be brought to the developer's attention, so a message will be displayed on the boss's page
					UIPanel brokenPanel = new UIPanel();
					brokenPanel.Height.Pixels = 160;
					brokenPanel.Width.Pixels = 340;
					brokenPanel.Top.Pixels = 150;
					brokenPanel.Left.Pixels = 3;
					PageTwo.Append(brokenPanel);

					// TODO: this will likely always output the NotImplemented, but I don't want to remove it just yet
					bool entryHasOldCall = BossChecklist.bossTracker.OldCalls.Values.Any(x => x.Contains(entry.name));
					string message = entryHasOldCall ? "NotImplemented" : "LogFeaturesNotAvailable";
					FittedTextPanel brokenDisplay = new FittedTextPanel($"Mods.BossChecklist.BossLog.DrawnText.{message}");
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

			string nextBoss = "";
			foreach (BossInfo entry in BossChecklist.bossTracker.SortedBosses) {
				entry.hidden = WorldAssist.HiddenBosses.Contains(entry.Key);

				if (!entry.VisibleOnChecklist())
					continue; // If the boss should not be visible on the Table of Contents, skip the entry in the list

				// Setup display name. Show "???" if unavailable and Silhouettes are turned on
				string displayName = entry.DisplayName;
				BossLogConfiguration cfg = BossChecklist.BossLogConfig;

				if (nextBoss == "" && !entry.IsDownedOrForced) {
					nextBoss = entry.Key;
				}

				bool namesMasked = cfg.MaskNames && !entry.IsDownedOrForced;
				bool hardMode = cfg.MaskHardMode && !Main.hardMode && entry.progression > BossTracker.WallOfFlesh && !entry.IsDownedOrForced;
				bool availability = cfg.HideUnavailable && !entry.available() && !entry.IsDownedOrForced;
				if (namesMasked || hardMode || availability) {
					displayName = "???";
				}

				if (cfg.DrawNextMark && cfg.MaskNames && cfg.UnmaskNextBoss) {
					if (!entry.IsDownedOrForced && entry.available() && !entry.hidden && nextBoss == entry.Key) {
						displayName = entry.DisplayName;
					}
				}

				// The first boss that isnt downed to have a nextCheck will set off the next check for the rest
				// Bosses that ARE downed will still be green due to the ordering of colors within the draw method
				// Update forced downs. If the boss is actaully downed, remove the force check.
				if (entry.ForceDowned) {
					displayName += "*";
					if (entry.downed()) {
						WorldAssist.ForcedMarkedEntries.Remove(entry.Key);
					}
				}

				bool allLoot = false;
				bool allCollect = false;
				if (BossChecklist.BossLogConfig.LootCheckVisibility) {
					PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
					allLoot = allCollect = true;

					// Loop through player saved loot and boss loot to see if every item was obtained
					foreach (int loot in entry.lootItemTypes) {
						int index = entry.loot.FindIndex(x => x.itemId == loot);

						if (loot == entry.treasureBag)
							continue;

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

						if (collectCount == entry.collection.Count) {
							allCollect = false; // If all the items were skipped due to the DroppedLootCheck config, don't mark as all collectibles obtained
						}
					}
				}

				bool isNext = nextBoss == entry.Key && cfg.DrawNextMark;
				TableOfContents next = new TableOfContents(entry.GetIndex, displayName, isNext, allLoot, allCollect) {
					PaddingTop = 5,
					PaddingLeft = 22 + (entry.progression <= BossTracker.WallOfFlesh ? 10 : 0)
				};
				next.OnClick += (a, b) => JumpToBossPage(next.Index);
				next.OnRightClick += (a, b) => JumpToBossPage(next.Index, false);

				if (entry.progression <= BossTracker.WallOfFlesh) {
					prehardmodeList.Add(next);
				}
				else {
					hardmodeList.Add(next);
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

			// Calculate Progress Bar downed entries
			int[] prehDown = new int[] { 0, 0, 0 };
			int[] prehTotal = new int[] { 0, 0, 0 };
			int[] hardDown = new int[] { 0, 0, 0 };
			int[] hardTotal = new int[] { 0, 0, 0 };
			Dictionary<string, int[]> prehEntries = new Dictionary<string, int[]>();
			Dictionary<string, int[]> hardEntries = new Dictionary<string, int[]>();

			foreach (BossInfo entry in BossChecklist.bossTracker.SortedBosses) {
				if (entry.hidden)
					continue; // The only way to manually remove entries from the progress bar is by hiding them

				if (entry.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported)
					continue; // Unknown and Unsupported entries can be automatically removed through configs if desired

				if (entry.progression <= BossTracker.WallOfFlesh) {
					if (entry.available() || (entry.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!prehEntries.ContainsKey(entry.modSource)) {
							prehEntries.Add(entry.modSource, new int[] { 0, 0 });
						}
						prehTotal[(int)entry.type]++;
						prehEntries[entry.modSource][1] += 1;

						if (entry.IsDownedOrForced) {
							prehDown[(int)entry.type]++;
							prehEntries[entry.modSource][0] += 1;
						}
					}
				}
				else {
					if (entry.available() || (entry.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!hardEntries.ContainsKey(entry.modSource)) {
							hardEntries.Add(entry.modSource, new int[] { 0, 0 });
						}
						hardTotal[(int)entry.type]++;
						hardEntries[entry.modSource][1] += 1;

						if (entry.IsDownedOrForced) {
							if (!hardEntries.ContainsKey(entry.modSource)) {
								hardEntries.Add(entry.modSource, new int[] { 0, 0 });
							}
							hardDown[(int)entry.type]++;
							hardEntries[entry.modSource][0] += 1;
						}
					}
				}
			}

			prehardmodeBar.downedEntries = prehDown;
			prehardmodeBar.totalEntries = prehTotal;
			prehEntries.ToList().Sort((x, y) => x.Key.CompareTo(y.Key));
			prehardmodeBar.modAllEntries = prehEntries;

			hardmodeBar.downedEntries = hardDown;
			hardmodeBar.totalEntries = hardTotal;
			hardEntries.ToList().Sort((x, y) => x.Key.CompareTo(y.Key));
			hardmodeBar.modAllEntries = hardEntries;


			PageOne.Append(prehardmodeBar);
			if (!BossChecklist.BossLogConfig.MaskHardMode || Main.hardMode) {
				PageTwo.Append(hardmodeBar);
			}
		}

		/// <summary>
		/// Sets up the content need for the credits page,
		/// listing off all mod contributors as well as the mods using the updated mod calls.
		/// </summary>
		private void UpdateCredits() {
			Dictionary<string, string> optedMods = BossUISystem.Instance.RegisteredMods; // The mods are already tracked in a list
			if (optedMods.Count > 0) {
				// create a list for the mod names using updated mod calls
				pageTwoItemList.Clear();
				pageTwoItemList.Left.Pixels = 34;
				pageTwoItemList.Top.Pixels = 65;
				pageTwoItemList.Width.Pixels = creditModSlot.Value.Width;
				pageTwoItemList.Height.Pixels = creditModSlot.Value.Height * 3 + 15;

				int row = 0; // this will track the row pos, increasing by one after the column limit is reached
				int col = 0; // this will track the column pos, increasing by one every icon added, and resetting to zero when the next row is made
				UIImage newRow = new UIImage(creditModSlot); // begin with the first row
				newRow.Width.Pixels = creditModSlot.Value.Width;
				newRow.Height.Pixels = creditModSlot.Value.Height;
				pageTwoItemList.Add(newRow); // add the initial row to the page
				foreach (KeyValuePair<string, string> mod in optedMods) {
					ModIcon icon = new ModIcon(ModContent.Request<Texture2D>(mod.Value), mod.Key);
					icon.Width.Pixels = 80;
					icon.Height.Pixels = 80;
					icon.Left.Pixels = 6 + (14 * (col + 1)) + (col * 80);
					icon.Top.Pixels = 6 + 12;
					newRow.Append(icon);

					col++; // after each icon added, move to the next column
					if (col == 3) {
						col = 0;
						row++;

						// create a new row after all column are filled
						newRow = new UIImage(creditModSlot);
						newRow.Top.Pixels = creditModSlot.Value.Height * row;
						newRow.Width.Pixels = creditModSlot.Value.Width;
						newRow.Height.Pixels = creditModSlot.Value.Height;
						pageTwoItemList.Add(newRow);
					}
				}

				// increase rows until at least 3 rows are visible
				while (row < 2) {
					newRow = new UIImage(creditModSlot);
					newRow.Top.Pixels = creditModSlot.Value.Height * row;
					newRow.Width.Pixels = creditModSlot.Value.Width;
					newRow.Height.Pixels = creditModSlot.Value.Height;
					pageTwoItemList.Add(newRow);
					row++;
				}
				
				PageTwo.Append(pageTwoItemList); // append the list with all the children attached

				// prepare the scrollbar in case it is needed for an excessive amount of mods
				if (row > 2) {
					scrollTwo.SetView(10f, 1000f);
					scrollTwo.Top.Pixels = 92;
					scrollTwo.Left.Pixels = -8;
					scrollTwo.Height.Set(-60f, 0.75f);
					scrollTwo.HAlign = 1f;
					PageTwo.Append(scrollTwo);
					pageTwoItemList.SetScrollbar(scrollTwo);
				}
			}
			else {
				// No mods are using the updated mod calls to use the Log, so create a text panel to inform the user
				UIPanel brokenPanel = new UIPanel();
				brokenPanel.Height.Pixels = 220;
				brokenPanel.Width.Pixels = 340;
				brokenPanel.Top.Pixels = 120;
				brokenPanel.Left.Pixels = 18;
				PageTwo.Append(brokenPanel);

				FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.NoModsSupported");
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

			BossInfo entry = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (entry.modSource == "Unknown" || entry.type != EntryType.Boss)
				return; // No elements are created on unsupport pages or on Mini-boss/Event pages

			// Set up the record type navigation buttons
			// Only bosses have records (Events will have banners of the enemies in the event drawn on it)
			// The entry also must be fully supported to have these buttons created
			PersonalStats stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld[entry.GetRecordIndex].stats;

			bool noKills = stats.kills == 0; // has the player killed this boss before?
			if (noKills && RecordSubCategory != SubCategory.PreviousAttempt && RecordSubCategory != SubCategory.WorldRecord) {
				RecordSubCategory = SubCategory.PreviousAttempt; // If a boss record does not have the selected subcategory type, it should default back to previous attempt.
			}

			// iterate through AltPageButtons to appeand each button where needed
			for (int i = 0; i < AltPageButtons.Length; i++) {
				if ((i == (int)SubCategory.PersonalBest || i == (int)SubCategory.FirstVictory) && noKills)
					continue; // If a player has no kills against a boss, they can't have a First or Best record, so skip the button creation
				int offset = noKills ? 0 : (i + (i < 2 ? 0 : 1) - 2) * 12; // offset needed if best record and first record are missing
				AltPageButtons[i].Left.Pixels = (int)lootButton.Left.Pixels + ((i - 2) * 28) + offset + (i >= 2 ? (int)lootButton.Width.Pixels : 0);
				AltPageButtons[i].Top.Pixels = (int)lootButton.Top.Pixels + (int)lootButton.Height.Pixels / 2 - 11;
				PageTwo.Append(AltPageButtons[i]);
			}

			// create 4 slots for each stat category value
			for (int i = 0; i < 4; i++) {
				if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE && i > 0 && RecordSubCategory != SubCategory.WorldRecord)
					break;

				if (BossChecklist.DebugConfig.DisableWorldRecords && i > 0 && RecordSubCategory == SubCategory.WorldRecord)
					break;

				RecordDisplaySlot slot = new RecordDisplaySlot(recordSlot, entry, RecordSubCategory, i);
				slot.Width.Pixels = recordSlot.Value.Width;
				slot.Height.Pixels = recordSlot.Value.Height;
				slot.Left.Pixels = PageTwo.Width.Pixels / 2 - recordSlot.Value.Width / 2;
				slot.Top.Pixels = 35 + (75 * (i + 1));
				PageTwo.Append(slot);
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

			BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (selectedBoss.modSource == "Unknown")
				return; // prevent unsupported entries from displaying info
						// a message panel will take its place notifying the user that the mods calls are out of date

			// Before anything, create a message box to display the spawn info provided
			var message = new UIMessageBox(selectedBoss.DisplaySpawnInfo);
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

			if (SpawnItemSelected >= selectedBoss.spawnItem.Count) {
				SpawnItemSelected = 0; // the selected spawn item number is greater than how many are in the list, so reset it back to 0
			}

			// Once the spawn description has been made, start structuring the spawn items showcase
			// If the spawn item list is empty, inform the player that there are no summon items for the boss/event through text
			if (selectedBoss.spawnItem.Count == 0 || selectedBoss.spawnItem[SpawnItemSelected] == ItemID.None) {
				string type = selectedBoss.type == EntryType.Boss ? "Boss" : selectedBoss.type == EntryType.Event ? "Event" : "MiniBoss";
				UIText info = new UIText(Language.GetTextValue($"Mods.BossChecklist.BossLog.DrawnText.NoSpawn{type}"));
				info.Left.Pixels = (PageTwo.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(info.Text).X / 2) - 5;
				info.Top.Pixels = 300;
				PageTwo.Append(info);
				return; // since no items are listed, the recipe code does not need to occur
			}

			// If a valid item is found, an item slot can be created
			int itemType = selectedBoss.spawnItem[SpawnItemSelected]; // grab the item type
			Item spawn = ContentSamples.ItemsByType[itemType]; // create an item for the item slot to use and reference
			if (selectedBoss.Key == "Terraria TorchGod" && itemType == ItemID.Torch) {
				spawn.stack = 101; // apply a custom stack count for the torches needed for the Torch God summoning event
			}

			LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
			spawnItemSlot.Width.Pixels = slotRectRef.Width;
			spawnItemSlot.Height.Pixels = slotRectRef.Height;
			spawnItemSlot.Left.Pixels = 48 + (56 * 2);
			spawnItemSlot.Top.Pixels = 230;
			PageTwo.Append(spawnItemSlot);

			// if more than one item is used for summoning, append navigational button to cycle through the items
			// a previous item button will appear if it is not the first item listed
			if (SpawnItemSelected > 0) {
				NavigationalButton PrevItem = new NavigationalButton(prevTexture) {
					Id = "PrevItem"
				};
				PrevItem.Width.Pixels = prevTexture.Value.Width;
				PrevItem.Height.Pixels = prevTexture.Value.Width;
				PrevItem.Left.Pixels = spawnItemSlot.Left.Pixels - PrevItem.Width.Pixels - 6;
				PrevItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (PrevItem.Height.Pixels / 2);
				PrevItem.OnClick += ChangeSpawnItem;
				PageTwo.Append(PrevItem);
			}
			// a next button will appear if it is not the last item listed
			if (SpawnItemSelected < BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count - 1) {
				NavigationalButton NextItem = new NavigationalButton(nextTexture) {
					Id = "NextItem"
				};
				NextItem.Width.Pixels = nextTexture.Value.Width;
				NextItem.Height.Pixels = nextTexture.Value.Height;
				NextItem.Left.Pixels = spawnItemSlot.Left.Pixels + spawnItemSlot.Width.Pixels + 6;
				NextItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (NextItem.Height.Pixels / 2);
				NextItem.OnClick += ChangeSpawnItem;
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
				string noncraftable = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Noncraftable");
				UIText craftText = new UIText(noncraftable, 0.8f);
				craftText.Left.Pixels = 10;
				craftText.Top.Pixels = 205;
				PageTwo.Append(craftText);
				return;
			}
			else {
				// display where the recipe originates form
				string recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.RecipeFrom", recipeMod);
				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = 10;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);

				// if more than one recipe exists for the selected item, append a button that cycles through all possible recipes
				if (TotalRecipes > 1) {
					NavigationalButton CycleItem = new NavigationalButton(cycleTexture, "Mods.BossChecklist.BossLog.DrawnText.CycleRecipe") {
						Id = "CycleItem_" + TotalRecipes
					};
					CycleItem.Width.Pixels = cycleTexture.Value.Width;
					CycleItem.Height.Pixels = cycleTexture.Value.Height;
					CycleItem.Left.Pixels = 240;
					CycleItem.Top.Pixels = 240;
					CycleItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(CycleItem);
				}
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			// To note, we do not need an item row as recipes have a max ingredient size of 14, so there is no need for a scrollbar
			foreach (Item item in ingredients) {
				// Create an item slot for the current item
				LogItemSlot ingList = new LogItemSlot(item, false, item.HoverName, ItemSlot.Context.GuideItem, 0.85f) {
					Id = $"ingredient_{item.type}"
				};
				ingList.Width.Pixels = slotRectRef.Width * 0.85f;
				ingList.Height.Pixels = slotRectRef.Height * 0.85f;
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
				LogItemSlot craftItem = new LogItemSlot(new Item(ItemID.PowerGlove), false, Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand"), ItemSlot.Context.EquipArmorVanity, 0.85f);
				craftItem.Width.Pixels = slotRectRef.Width * 0.85f;
				craftItem.Height.Pixels = slotRectRef.Height * 0.85f;
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

					string hoverText = "";
					Item craftStation = new Item(0);
					if (tile == TileID.DemonAltar) {
						// Demon altars do not have an item id, so a texture will be created solely to be drawn here
						// A demon altar will be displayed if the world evil is corruption
						// A crimson altar will be displayed if the world evil is crimson
						string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
						string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");
						hoverText = WorldGen.crimson ? crimsonAltar : demonAltar;
					}
					else {
						// Look for items that create the tile when placed, and use that item for the item slot
						foreach (Item item in ContentSamples.ItemsByType.Values) {
							if (item.createTile == tile) {
								craftStation.SetDefaults(item.type);
								break;
							}
						}
					}

					LogItemSlot tileList = new LogItemSlot(craftStation, false, hoverText, ItemSlot.Context.EquipArmorVanity, 0.85f);
					tileList.Width.Pixels = slotRectRef.Width * 0.85f;
					tileList.Height.Pixels = slotRectRef.Height * 0.85f;
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

			BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
			List<ItemDefinition> obtainedItems = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
			List<int> bossItems = new List<int>(selectedBoss.lootItemTypes.Union(selectedBoss.collection)); // combined list of loot and collectibles
			bossItems.Remove(selectedBoss.treasureBag); // the treasurebag should not be displayed on the loot table, but drawn above it instead

			// Prevents itemslot creation for items that are dropped from within the opposite world evil, if applicable
			if (!Main.drunkWorld && !ModLoader.TryGetMod("BothEvils", out Mod mod)) {
				foreach (DropRateInfo loot in selectedBoss.loot) {
					if (loot.conditions is null)
						continue;

					bool isCorruptionLocked = WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCorruption || x is Conditions.IsCorruptionAndNotExpert);
					bool isCrimsonLocked = !WorldGen.crimson && loot.conditions.Any(x => x is Conditions.IsCrimson || x is Conditions.IsCrimsonAndNotExpert);

					if (bossItems.Contains(loot.itemId) && (isCorruptionLocked || isCrimsonLocked)) {
						bossItems.Remove(loot.itemId);
					}
				}
			}

			// If the boss items contains any otherworld music boxes
			if (bossItems.Intersect(BossTracker.otherWorldMusicBoxTypes).Any()) {
				FieldInfo TOWMusicUnlocked = typeof(Main).GetField("TOWMusicUnlocked", BindingFlags.Static | BindingFlags.NonPublic);
				bool OWUnlocked = (bool)TOWMusicUnlocked.GetValue(null);
				if (OtherworldUnlocked != OWUnlocked) {
					OtherworldUnlocked = OWUnlocked;
				}
			}

			int row = 0; // this will track the row pos, increasing by one after the column limit is reached
			int col = 0; // this will track the column pos, increasing by one every item, and resetting to zero when the next row is made
			LootRow newRow = new LootRow(0); // the initial row to start with

			foreach (int item in bossItems) {
				Item selectedItem = ContentSamples.ItemsByType[item];
				bool hasObtained = obtainedItems.Any(x => x.Type == item) || obtainedItems.Any(x => x.Type == item);

				// Create an item slot for the current item
				LogItemSlot itemSlot = new LogItemSlot(selectedItem, hasObtained, "", ItemSlot.Context.TrashItem) {
					Id = "loot_" + item
				};
				itemSlot.Width.Pixels = slotRectRef.Width;
				itemSlot.Height.Pixels = slotRectRef.Height;
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
		}

		/// <summary>
		/// Used to locate the next available entry for the player to fight that is not defeated, marked as defeated, or hidden.
		/// Mainly used for positioning and navigation.
		/// </summary>
		/// <returns>The index of the next available entry within the boss tracker.</returns>
		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.IsDownedOrForced && x.available() && !x.hidden && x.type == entryType);

		/// <summary> Determines if a texture should be masked by a black sihlouette. </summary>
		public static Color MaskBoss(BossInfo entry) {
			if (!entry.IsDownedOrForced) {
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
			else if (entry.hidden) {
				return Color.Black;
			}
			return Color.White;
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
