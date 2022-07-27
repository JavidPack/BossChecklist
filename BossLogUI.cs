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

namespace BossChecklist
{
	class BossLogUI : UIState
	{
		// The main button to open the Boss Log
		public BossAssistButton bosslogbutton;

		// All contents are within these areas
		// The selected boss page starts out with an invalid number for the initial check
		public static int PageNum = -3;
		public BossLogPanel BookArea;
		public BossLogPanel PageOne;
		public BossLogPanel PageTwo;

		// Each Boss entry has 3 subpage categories
		public static CategoryPage CategoryPageNum = CategoryPage.Record;
		public SubpageButton recordButton;
		public SubpageButton spawnButton;
		public SubpageButton lootButton;

		public SubpageButton[] AltPageButtons;
		public static RecordCategory RecordPageSelected = RecordCategory.PreviousAttempt;
		public static RecordCategory CompareState = RecordCategory.None; // Compare record values to one another
		//public static int[] AltPageSelected; // AltPage for Records is "Player Best/World Best(Server)"
		//public static int[] TotalAltPages; // The total amount of "subpages" for Records, Spawn, and Loot pages

		public UIImageButton NextPage;
		public UIImageButton PrevPage;
		public BookUI filterPanel;
		private List<BookUI> filterCheck;
		private List<BookUI> filterCheckMark;

		public BookUI ToCTab;
		public BookUI CreditsTab;
		public BookUI BossTab;
		public BookUI MiniBossTab;
		public BookUI EventTab;
		public BookUI InfoTab;
		public bool filterOpen = false;

		public UIList prehardmodeList;
		public UIList hardmodeList;
		public ProgressBar prehardmodeBar;
		public ProgressBar hardmodeBar;
		public BossLogUIElements.FixedUIScrollbar scrollOne;
		public BossLogUIElements.FixedUIScrollbar scrollTwo;

		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles
		public UIImage PromptCheck;

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
		public static Asset<Texture2D> tocTexture;
		public static Asset<Texture2D> credTexture;
		public static Asset<Texture2D> bossNavTexture;
		public static Asset<Texture2D> minibossNavTexture;
		public static Asset<Texture2D> eventNavTexture;
		public static Asset<Texture2D> filterTexture;
		public static Asset<Texture2D> hiddenTexture;
		public static Asset<Texture2D> cycleTexture;
		public static Asset<Texture2D> checkMarkTexture;
		public static Asset<Texture2D> xTexture;
		public static Asset<Texture2D> circleTexture;
		public static Asset<Texture2D> strikeNTexture;
		public static Asset<Texture2D> checkboxTexture;
		public static Asset<Texture2D> chestTexture;
		public static Asset<Texture2D> goldChestTexture;
		public static Rectangle slotRectRef;
		public static readonly Color faded = new Color(128, 128, 128, 128);

		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool showHidden = false;
		internal static bool PendingToggleBossLogUI; // Allows toggling boss log visibility from methods not run during UIScale so Main.screenWidth/etc are correct for ResetUIPositioning method

		private bool bossLogVisible;
		public bool BossLogVisible {
			get => bossLogVisible;
			set {
				if (value) {
					Append(BookArea);
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
					RemoveChild(BookArea);
				}
				bossLogVisible = value;
			}
		}

		public void ToggleBossLog(bool show = true) {
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum == -3) {
				BossLogVisible = show;
				if (show) {
					OpenProgressionModePrompt();
					Main.playerInventory = false;
				}
				return;
			}

			if (show) {
				modPlayer.hasOpenedTheBossLog = true;
				// If the prompt isn't shown, try checking for a reset.
				// This is to reset the page from what the user previously had back to the Table of Contents
				if (modPlayer.enteredWorldReset) {
					modPlayer.enteredWorldReset = false;
					PageNum = -1;
					CategoryPageNum = 0;
					UpdateTableofContents();
				}
				else {
					UpdateCatPage(CategoryPageNum);
				}

				// Update UI Element positioning before marked visible
				// This will always occur after adjusting UIScale, since the UI has to be closed in order to open up the menu options
				ResetUIPositioning();
				UpdateTabNavPos();
			}

			// If UI is closed on a new record page, remove the new record from the list
			if (BossLogVisible && show == false && PageNum >= 0) {
				UpdateRecordHighlight();
			}

			BossLogVisible = show;
			if (show) {
				// TODO: Small fix to update hidden list on open
				Main.playerInventory = false;
			}
		}

		public override void OnInitialize() {
			bookTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Outline");
			borderTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Border");
			fadedTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Faded");
			colorTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Color");
			bookUITexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Back");
			tabTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab");
			infoTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_InfoTab");

			prevTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Prev");
			nextTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Next");
			tocTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Contents");
			credTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Credits");
			bossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Boss");
			minibossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Miniboss");
			eventNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Event");
			filterTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Filter");
			hiddenTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Hidden");
			cycleTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_CycleRecipe");

			checkMarkTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check", AssetRequestMode.ImmediateLoad);
			xTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X", AssetRequestMode.ImmediateLoad);
			circleTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Next", AssetRequestMode.ImmediateLoad);
			strikeNTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_StrikeNext", AssetRequestMode.ImmediateLoad);
			checkboxTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Box", AssetRequestMode.ImmediateLoad);
			chestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Chest");
			goldChestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_GoldChest");

			slotRectRef = TextureAssets.InventoryBack.Value.Bounds;

			bosslogbutton = new BossAssistButton(bookTexture, "Mods.BossChecklist.BossLog.Terms.BossLog") {
				Id = "OpenUI"
			};
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

			ToCTab = new BookUI(tabTexture) {
				Id = "ToCFilter_Tab"
			};
			ToCTab.Width.Pixels = tabTexture.Value.Width;
			ToCTab.Height.Pixels = tabTexture.Value.Height;
			ToCTab.OnClick += OpenViaTab;
			ToCTab.OnRightDoubleClick += (a, b) => ClearForcedDowns();

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

			PrevPage = new BossAssistButton(prevTexture, "") {
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

			Asset<Texture2D> filterPanelTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Filter");
			filterPanel = new BookUI(filterPanelTexture) {
				Id = "filterPanel"
			};
			filterPanel.Height.Pixels = 166;
			filterPanel.Width.Pixels = 50;

			filterCheckMark = new List<BookUI>();
			filterCheck = new List<BookUI>();

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
					newCheckBox.OnRightDoubleClick += (a, b) => ClearHiddenList();
				}
				newCheckBox.Append(filterCheckMark[i]);
				filterCheck.Add(newCheckBox);
			}

			// Setup the inital checkmarks to display what the user has prematurely selected
			Filters_SetImage();

			// Append the filter checks to the filter panel
			foreach (BookUI uiimage in filterCheck) {
				filterPanel.Append(uiimage);
			}

			NextPage = new BossAssistButton(nextTexture, "") {
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

			recordButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.Records");
			recordButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			recordButton.Height.Pixels = 25;
			recordButton.Left.Pixels = PageTwo.Width.Pixels / 2 - recordButton.Width.Pixels - 8;
			recordButton.Top.Pixels = 15;
			recordButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Record);
			recordButton.OnRightDoubleClick += (a, b) => ResetStats();

			spawnButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.SpawnInfo");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 + 8;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Spawn);

			lootButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.LootCollect");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24 + 16;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Loot);
			lootButton.OnRightDoubleClick += RemoveItem;

			// These will serve as a reservation for our AltPage buttons
			SubpageButton PrevRecordButton = new SubpageButton((int)RecordCategory.PreviousAttempt);
			PrevRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.PreviousAttempt);
			PrevRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.PreviousAttempt, false);

			SubpageButton FirstRecordButton = new SubpageButton((int)RecordCategory.FirstRecord);
			FirstRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.FirstRecord);
			FirstRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.FirstRecord, false);

			SubpageButton BestRecordButton = new SubpageButton((int)RecordCategory.BestRecord);
			BestRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.BestRecord);
			BestRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.BestRecord, false);

			SubpageButton WorldRecordButton = new SubpageButton((int)RecordCategory.WorldRecord);
			WorldRecordButton.OnClick += (a, b) => HandleRecordTypeButton(RecordCategory.WorldRecord);
			WorldRecordButton.OnRightClick += (a, b) => HandleRecordTypeButton(RecordCategory.WorldRecord, false);

			AltPageButtons = new SubpageButton[] {
				PrevRecordButton,
				BestRecordButton,
				FirstRecordButton,
				WorldRecordButton
			};
		}

		public override void Update(GameTime gameTime) {
			if (PendingToggleBossLogUI) {
				PendingToggleBossLogUI = false;
				BossUISystem.Instance.BossLog.ToggleBossLog(!BossUISystem.Instance.BossLog.BossLogVisible);
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
		}

		public void ResetUIPositioning() {
			// Reset the position of the button to make sure it updates with the screen res
			BookArea.Left.Pixels = (Main.screenWidth / 2) - (BookArea.Width.Pixels / 2);
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (BookArea.Height.Pixels / 2) - 6;
			PageOne.Left.Pixels = BookArea.Left.Pixels + 20;
			PageOne.Top.Pixels = BookArea.Top.Pixels + 12;
			PageTwo.Left.Pixels = BookArea.Left.Pixels - 15 + BookArea.Width.Pixels - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = BookArea.Top.Pixels + 12;

			InfoTab.Left.Pixels = BookArea.Left.Pixels + 40;
			InfoTab.Top.Pixels = BookArea.Top.Pixels - infoTexture.Value.Height + 8;

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
			UpdateTabNavPos();
		}

		private void UpdateTabNavPos() {
			if (PageNum == -3)
				return;

			// Updating tabs to proper positions
			BossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Boss) || PageNum == -2 ? -20 : BookArea.Width.Pixels - 12);
			MiniBossTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.MiniBoss) || PageNum == -2 ? -20 : BookArea.Width.Pixels - 12);
			EventTab.Left.Pixels = BookArea.Left.Pixels + (PageNum >= FindNext(EntryType.Event) || PageNum == -2 ? -20 : BookArea.Width.Pixels - 12);
			UpdateFilterTabPos(false);
		}

		private void UpdateFilterTabPos(bool tabClicked) {
			if (tabClicked) {
				filterOpen = !filterOpen;
			}
			if (PageNum != -1) {
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

		private void Filters_SetImage() {
			// ...Bosses
			filterCheckMark[0].SetImage(BossChecklist.BossLogConfig.FilterBosses == "Show" ? checkMarkTexture : circleTexture);

			// ...Mini-Bosses
			if (BossChecklist.BossLogConfig.OnlyBosses) {
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
			if (BossChecklist.BossLogConfig.OnlyBosses) {
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

		private void ChangeFilter(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string rowID = book.Id.Substring(2, 1);
			if (rowID == "0") {
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") {
					BossChecklist.BossLogConfig.FilterBosses = "Hide when completed";
				}
				else {
					BossChecklist.BossLogConfig.FilterBosses = "Show";
				}
			}
			if (rowID == "1" && !BossChecklist.BossLogConfig.OnlyBosses) {
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
			if (rowID == "2" && !BossChecklist.BossLogConfig.OnlyBosses) {
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
			if (rowID == "3") {
				showHidden = !showHidden;
			}
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			Filters_SetImage();
			UpdateTableofContents();
		}

		// TODO: [??] Implement separate Reset tabs? Including: Clear Hidden List, Clear Forced Downs, Clear Records, Clear Boss Loot, etc

		private void ClearHiddenList() {
			if (!BossChecklist.DebugConfig.ResetHiddenEntries || WorldAssist.HiddenBosses.Count == 0)
				return;

			WorldAssist.HiddenBosses.Clear();
			showHidden = false;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestClearHidden);
				packet.Send();
			}
			UpdateTableofContents();
		}

		// Shouldn't need updating through server as Forced Down checks are visually client-sided
		// TODO: Make it server sided as well similar to Hidden bosses?
		private static void ClearForcedDowns() => Main.LocalPlayer.GetModPlayer<PlayerAssist>().ForceDownsForWorld.Clear();

		private static void UpdateRecordHighlight() {
			if (PageNum >= 0) {
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				if (BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex != -1) {
					modPlayer.hasNewRecord[BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex] = false;
				}
			}
		}

		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string id = book.Id;
			if (PageNum == -3 || !BookUI.DrawTab(id))
				return;

			if (id == "ToCFilter_Tab" && PageNum == -1) {
				UpdateFilterTabPos(true);
				return;
			}

			// Remove new records when navigating from a new record page
			UpdateRecordHighlight();

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
				UpdateCredits();
			}
			else {
				UpdateTableofContents();
			}

			// Update tabs to be properly positioned on either the left or right side
			UpdateFilterTabPos(false);
			UpdateTabNavPos();

			if (PageNum >= 0) {
				ResetBothPages();
				UpdateCatPage(CategoryPageNum);
			}
		}

		// Update to allow clearing Best Records only, First Records only, and All Records (including previous, excluding world records)
		private void ResetStats() {
			if (BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE)
				return;

			if (BossChecklist.DebugConfig.ResetRecordsBool && CategoryPageNum == 0) {
				PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
				int recordIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
				PersonalStats stats = modPlayer.RecordsForWorld[recordIndex].stats;
				stats.kills = 0;
				stats.deaths = 0;

				stats.durationBest = -1;
				stats.durationPrev = -1;

				stats.hitsTakenBest = -1;
				stats.hitsTakenPrev = -1;
				OpenRecord();
			}
		}

		private void RemoveItem(UIMouseEvent evt, UIElement listeningElement) {
			// Double right-click an item slot to remove that item from the selected boss page's loot/collection list
			// Double right-click the "Loot / Collection" button to entirely clear the selected boss page's loot/collection list
			// If holding Alt while right-clicking will do the above for ALL boss lists
			if (!BossChecklist.DebugConfig.ResetLootItems)
				return;

			Player player = Main.LocalPlayer;
			PlayerAssist modPlayer = player.GetModPlayer<PlayerAssist>();

			if (CategoryPageNum == CategoryPage.Loot) {
				string id = "";
				int itemType = -1;
				if (listeningElement is LogItemSlot slot) {
					id = slot.Id;
					itemType = slot.item.type;
				}


				if (id == "") {
					// If an Alt key is held while double right-clicking the Loot tab, all items from every boss will be cleared.
					// Otherwise, only the items for the selected page's boss will be cleared.
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						foreach (KeyValuePair<string, List<ItemDefinition>> entry in modPlayer.BossItemsCollected) {
							entry.Value.Clear();
						}
					}
					else {
						BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
						if (modPlayer.BossItemsCollected.TryGetValue(selectedBoss.Key, out List<ItemDefinition> items)) {
							items.Clear();
						}
					}
				}
				else if (id.StartsWith("loot_")) {
					// If an Alt key is held while double right-clicking an item slot, that item will be cleared from all boss item lists.
					// Otherwise, only the selected page's boss will have that item cleared.
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						foreach (KeyValuePair<string, List<ItemDefinition>> entry in modPlayer.BossItemsCollected) {
							entry.Value.RemoveAll(x => x.Type == itemType);
						}
					}
					else {
						BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
						if (modPlayer.BossItemsCollected.TryGetValue(selectedBoss.Key, out List<ItemDefinition> items)) {
							items.RemoveAll(x => x.Type == itemType);
						}
					}
				}
				OpenLoot();
			}
		}

		private void ChangeSpawnItem(UIMouseEvent evt, UIElement listeningElement) {
			BossAssistButton button = (BossAssistButton)listeningElement;
			string id = button.Id;
			if (id == "NextItem") {
				RecipePageNum++;
				RecipeShown = 0;
			}
			else if (id == "PrevItem") {
				RecipePageNum--;
				RecipeShown = 0;
			}
			else if (id.Contains("CycleItem")) {
				int index = id.IndexOf('_');
				if (RecipeShown == Convert.ToInt32(id.Substring(index + 1)) - 1) {
					RecipeShown = 0;
				}
				else {
					RecipeShown++;
				}
			}
			OpenSpawn();
		}

		private void PageChangerClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BossAssistButton button)
				return;

			// Remove new records when navigating from a new record page
			UpdateRecordHighlight();

			pageTwoItemList.Clear();
			prehardmodeList.Clear();
			hardmodeList.Clear();
			PageOne.RemoveChild(scrollOne);
			PageTwo.RemoveChild(scrollTwo);
			RecipeShown = 0;
			RecipePageNum = 0;

			// Move to next/prev
			List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
			if (button.Id == "Next") {
				if (PageNum < BossList.Count - 1) {
					PageNum++;
				}
				else {
					PageNum = -2;
				}
			}
			else { // button is previous
				if (PageNum >= 0) {
					PageNum--;
				}
				else {
					PageNum = BossList.Count - 1;
				}
			}

			// If the page is hidden or unavailable, keep moving till its not or until page is at either end
			// Also check for "Only Bosses" navigation
			bool bossesOnly = BossChecklist.BossLogConfig.OnlyBosses;
			if (PageNum >= 0) {
				bool HiddenOrUnAvailable = BossList[PageNum].hidden || !BossList[PageNum].available();
				bool OnlyDisplayBosses = BossChecklist.BossLogConfig.OnlyBosses && BossList[PageNum].type != EntryType.Boss;
				if ((HiddenOrUnAvailable || OnlyDisplayBosses)) {
					while (PageNum >= 0) {
						BossInfo currentBoss = BossList[PageNum];
						if (!currentBoss.hidden && currentBoss.available()) {
							if (BossChecklist.BossLogConfig.OnlyBosses) {
								if (currentBoss.type == EntryType.Boss) {
									break;
								}
							}
							else {
								break;
							}
						}

						if (button.Id == "Next") {
							if (PageNum < BossList.Count - 1) {
								PageNum++;
							}
							else {
								PageNum = -2;
							}
						}
						else { // button is previous
							if (PageNum >= 0) {
								PageNum--;
							}
							else {
								PageNum = BossList.Count - 1;
							}
						}
					}
				}
			}

			// Update tabs to be properly positioned on either the left or right side
			UpdateTabNavPos();
			ResetBothPages();
			UpdateCatPage(CategoryPageNum);
		}

		private void OpenRecord() {
			ResetBothPages();
			if (!PageTwo.HasChild(AltPageButtons[(int)RecordPageSelected])) {
				RecordPageSelected = RecordCategory.PreviousAttempt;
			}
			if (PageNum < 0)
				return;
		}

		private void OpenSpawn() {
			ResetBothPages();
			int TotalRecipes = 0;
			if (PageNum < 0)
				return;

			pageTwoItemList.Clear();
			BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (boss.modSource == "Unknown")
				return;
			
			var message = new UIMessageBox(boss.DisplaySpawnInfo);
			message.Width.Set(-34f, 1f);
			message.Height.Set(-370f, 1f);
			message.Top.Set(85f, 0f);
			message.Left.Set(5f, 0f);
			//message.PaddingRight = 30;
			PageTwo.Append(message);

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Set(91f, 0f);
			scrollTwo.Height.Set(-382f, 1f);
			scrollTwo.Left.Set(-5, 0f);
			scrollTwo.HAlign = 1f;
			PageTwo.Append(scrollTwo);
			message.SetScrollbar(scrollTwo);
			
			if (boss.spawnItem.Count == 0 || boss.spawnItem.All(x => x <= 0)) {
				string type = "Boss";
				if (boss.type == EntryType.MiniBoss) {
					type = "MiniBoss";
				}
				else if (boss.type == EntryType.Event) {
					type = "Event";
				}
				UIText info = new UIText(Language.GetTextValue($"Mods.BossChecklist.BossLog.DrawnText.NoSpawn{type}"));
				info.Left.Pixels = (PageTwo.Width.Pixels / 2) - (FontAssets.MouseText.Value.MeasureString(info.Text).X / 2) - 5;
				info.Top.Pixels = 300;
				PageTwo.Append(info);
				return;
			}
			List<Item> ingredients = new List<Item>();
			List<int> requiredTiles = new List<int>();
			string recipeMod = "Terraria";
			//List<Recipe> recipes = Main.recipe.ToList();
			if (boss.spawnItem[RecipePageNum] != 0) {
				var recipes = Main.recipe
					.Take(Recipe.numRecipes)
					.Where(r => r.HasResult(boss.spawnItem[RecipePageNum]));

				foreach (Recipe recipe in recipes) {
					if (TotalRecipes == RecipeShown) {
						foreach (Item item in recipe.requiredItem) {
							Item clone = item.Clone();
							OverrideForGroups(recipe, clone);
							ingredients.Add(clone);
						}

						requiredTiles.AddRange(recipe.requiredTile);

						if (recipe.Mod != null) {
							recipeMod = recipe.Mod.DisplayName;
						}
					}
					TotalRecipes++;
				}
				Item spawn = ContentSamples.ItemsByType[boss.spawnItem[RecipePageNum]];
				if (boss.Key == "Terraria TorchGod" && spawn.type == ItemID.Torch) {
					spawn.stack = 101;
				}

				LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
				spawnItemSlot.Width.Pixels = slotRectRef.Width;
				spawnItemSlot.Height.Pixels = slotRectRef.Height;
				spawnItemSlot.Left.Pixels = 48 + (56 * 2);
				spawnItemSlot.Top.Pixels = 230;
				PageTwo.Append(spawnItemSlot);

				int row = 0;
				int col = 0;
				for (int k = 0; k < ingredients.Count; k++) {
					LogItemSlot ingList = new LogItemSlot(ingredients[k], false, ingredients[k].HoverName, ItemSlot.Context.GuideItem, 0.85f) {
						Id = "ingredient_" + k
					};
					ingList.Width.Pixels = slotRectRef.Width * 0.85f;
					ingList.Height.Pixels = slotRectRef.Height * 0.85f;
					ingList.Left.Pixels = 20 + (48 * col);
					ingList.Top.Pixels = 240 + (48 * (row + 1));
					PageTwo.Append(ingList);
					col++;
					if (k == 6) {
						// Fills in rows with empty slots. New rows start after 7 items
						if (ingList.item.type == ItemID.None) {
							break;
						}
						col = 0;
						row++;
					}
					else if (k == 13) {
						// No mods should be able to use more than 14 items to craft this spawn item, so break incase it goes beyond that
						break;
					}
				}

				if (ingredients.Count > 0 && requiredTiles.Count == 0) {
					LogItemSlot craftItem = new LogItemSlot(new Item(ItemID.PowerGlove), false, Language.GetTextValue("Mods.BossChecklist.BossLog.Terms.ByHand"), ItemSlot.Context.EquipArmorVanity, 0.85f);
					craftItem.Width.Pixels = slotRectRef.Width * 0.85f;
					craftItem.Height.Pixels = slotRectRef.Height * 0.85f;
					craftItem.Top.Pixels = 240 + (48 * (row + 2));
					craftItem.Left.Pixels = 20;
					PageTwo.Append(craftItem);
				}
				else if (requiredTiles.Count > 0) {
					for (int l = 0; l < requiredTiles.Count; l++) {
						if (requiredTiles[l] == -1) {
							break; // Prevents extra empty slots from being created
						}
						LogItemSlot tileList;
						if (requiredTiles[l] == 26) {
							string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
							string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");
							string altarType = WorldGen.crimson ? crimsonAltar : demonAltar;
							tileList = new LogItemSlot(new Item(0), false, altarType, ItemSlot.Context.EquipArmorVanity, 0.85f);
						}
						else {
							Item craft = new Item(0);
							for (int m = 0; m < ItemLoader.ItemCount; m++) {
								Item craftItem = ContentSamples.ItemsByType[m];
								if (craftItem.createTile == requiredTiles[l]) {
									craft = craftItem;
									break;
								}
							}
							tileList = new LogItemSlot(craft, false, craft.HoverName, ItemSlot.Context.EquipArmorVanity, 0.85f);
						}
						tileList.Width.Pixels = slotRectRef.Width * 0.85f;
						tileList.Height.Pixels = slotRectRef.Height * 0.85f;
						tileList.Left.Pixels = 20 + (48 * l);
						tileList.Top.Pixels = 240 + (48 * (row + 2));
						PageTwo.Append(tileList);
					}
				}

				if (RecipePageNum > 0) {
					BossAssistButton PrevItem = new BossAssistButton(prevTexture, "") {
						Id = "PrevItem"
					};
					PrevItem.Width.Pixels = prevTexture.Value.Width;
					PrevItem.Height.Pixels = prevTexture.Value.Width;
					PrevItem.Left.Pixels = spawnItemSlot.Left.Pixels - PrevItem.Width.Pixels - 6;
					PrevItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (PrevItem.Height.Pixels / 2);
					PrevItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(PrevItem);
				}

				if (RecipePageNum < BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count - 1) {
					BossAssistButton NextItem = new BossAssistButton(nextTexture, "") {
						Id = "NextItem"
					};
					NextItem.Width.Pixels = nextTexture.Value.Width;
					NextItem.Height.Pixels = nextTexture.Value.Height;
					NextItem.Left.Pixels = spawnItemSlot.Left.Pixels + spawnItemSlot.Width.Pixels + 6;
					NextItem.Top.Pixels = spawnItemSlot.Top.Pixels + (spawnItemSlot.Height.Pixels / 2) - (NextItem.Height.Pixels / 2);
					NextItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(NextItem);
				}

				if (TotalRecipes > 1) {
					BossAssistButton CycleItem = new BossAssistButton(cycleTexture, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.CycleRecipe")) {
						Id = "CycleItem_" + TotalRecipes
					};
					CycleItem.Width.Pixels = cycleTexture.Value.Width;
					CycleItem.Height.Pixels = cycleTexture.Value.Height;
					CycleItem.Left.Pixels = 240;
					CycleItem.Top.Pixels = 240;
					CycleItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(CycleItem);
				}

				string recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.Noncraftable");
				if (TotalRecipes > 0) {
					recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.RecipeFrom", recipeMod);
				}

				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = 10;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);
			}
		}

		private void OpenLoot() {
			ResetBothPages();
			if (PageNum < 0)
				return;

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 125;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 125 - 80;

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 125;
			scrollTwo.Left.Pixels = -3;
			scrollTwo.Height.Set(-88f, 0.75f);
			scrollTwo.HAlign = 1f;

			pageTwoItemList.Clear();

			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			BossInfo selectedBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (!modPlayer.BossItemsCollected.TryGetValue(selectedBoss.Key, out List<ItemDefinition> obtainedItems))
				return;

			LootRow newRow = new LootRow(0) {
				Id = "Loot0"
			};

			// Create the combined list of loot and collectibles.
			List<int> bossItems = new List<int>(selectedBoss.collection.Union(selectedBoss.lootItemTypes));
			// Skip any treasurebag as they should not be display within the loot table.
			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (bossItems.Contains(boss.treasureBag)) {
					bossItems.Remove(boss.treasureBag);
				}
			}

			int row = 0;
			int col = 0;
			foreach (int item in bossItems) {
				Item selectedItem = ContentSamples.ItemsByType[item];
				
				bool hasObtained = obtainedItems.Any(x => x.Type == item) || obtainedItems.Any(x => x.Type == item);

				LogItemSlot itemSlot = new LogItemSlot(selectedItem, hasObtained, selectedItem.Name, ItemSlot.Context.TrashItem) {
					Id = "loot_" + item
				};
				itemSlot.Width.Pixels = slotRectRef.Width;
				itemSlot.Height.Pixels = slotRectRef.Height;
				itemSlot.Left.Pixels = (col * 56) + 15;
				itemSlot.OnRightDoubleClick += RemoveItem;
				newRow.Append(itemSlot);
				col++;
				if (col == 6) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row) {
						Id = "Loot" + row
					};
				}
			}

			// This adds the final row
			if (col != 0) {
				col = 0;
				row++;
				pageTwoItemList.Add(newRow);
				newRow = new LootRow(row) {
					Id = "Loot" + row
				};
			}

			// If we have more than 5 rows of items, a scroll bar is needed to access all items
			if (row > 5) {
				PageTwo.Append(scrollTwo);
			}
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		public static bool[] CalculateTableOfContents(List<BossInfo> bossList) {
			bool[] visibleList = new bool[bossList.Count];
			for (int i = 0; i < bossList.Count; i++) {
				BossInfo boss = bossList[i];
				// if the boss cannot get through the config checks, it will remain false (invisible)
				bool HideUnsupported = boss.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported;
				bool HideUnavailable = (!boss.available()) && BossChecklist.BossLogConfig.HideUnavailable;
				bool HideHidden = boss.hidden && !showHidden;
				bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyBosses && boss.type != EntryType.Boss;
				if (((HideUnavailable || HideHidden) && !boss.IsDownedOrForced) || SkipNonBosses || HideUnsupported) {
					continue;
				}

				// Check filters as well
				EntryType type = boss.type;
				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;

				bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && boss.IsDownedOrForced;
				bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && boss.IsDownedOrForced));
				bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && boss.IsDownedOrForced));
				if (FilterBoss || FilterMiniBoss || FilterEvent) {
					continue;
				}

				visibleList[i] = true; // Boss will show on the Table of Contents
			}
			return visibleList;
		}

		public void OpenProgressionModePrompt() {
			PageNum = -3;
			ResetBothPages();
			ResetUIPositioning();

			FittedTextPanel textBox = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.ProgressionModeDescription");
			textBox.Width.Pixels = PageOne.Width.Pixels - 30;
			textBox.Height.Pixels = PageOne.Height.Pixels - 70;
			textBox.Left.Pixels = 10;
			textBox.Top.Pixels = 60;
			PageOne.Append(textBox);

			Asset<Texture2D> backdropTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RecordSlot", AssetRequestMode.ImmediateLoad);
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
				ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Box", AssetRequestMode.ImmediateLoad)
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

			Asset<Texture2D> check = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check", AssetRequestMode.ImmediateLoad);
			Asset<Texture2D> x = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X", AssetRequestMode.ImmediateLoad);

			bool config = BossChecklist.BossLogConfig.PromptDisabled;
			PromptCheck = new UIImage(config ? check : x);

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

		public void ContinueDisabled() {
			BossChecklist.BossLogConfig.MaskTextures = false;
			BossChecklist.BossLogConfig.MaskNames = false;
			BossChecklist.BossLogConfig.UnmaskNextBoss = true;
			BossChecklist.BossLogConfig.MaskBossLoot = false;
			BossChecklist.BossLogConfig.MaskHardMode = false;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = -1;
			ToggleBossLog(true);
		}

		public void ContinueEnabled() {
			BossChecklist.BossLogConfig.MaskTextures = true;
			BossChecklist.BossLogConfig.MaskNames = true;
			BossChecklist.BossLogConfig.UnmaskNextBoss = false;
			BossChecklist.BossLogConfig.MaskBossLoot = true;
			BossChecklist.BossLogConfig.MaskHardMode = true;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);

			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = -1;
			ToggleBossLog(true);
		}

		public void CloseAndConfigure() {
			ToggleBossLog(false);
			Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true;
			PageNum = -1;

		}

		public void DisablePromptMessage() {
			BossChecklist.BossLogConfig.PromptDisabled = !BossChecklist.BossLogConfig.PromptDisabled;
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			if (BossChecklist.BossLogConfig.PromptDisabled) {
				PromptCheck.SetImage(ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check"));
			}
			else {
				PromptCheck.SetImage(ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X"));
			}
		}

		public void UpdateTableofContents() {
			PageNum = -1;
			ResetBothPages();
			bool nextCheck = true;
			prehardmodeList.Clear();
			hardmodeList.Clear();

			List<BossInfo> referenceList = BossChecklist.bossTracker.SortedBosses;
			bool[] visibleList = CalculateTableOfContents(referenceList);

			for (int bossIndex = 0; bossIndex < referenceList.Count; bossIndex++) {
				BossInfo boss = referenceList[bossIndex];
				boss.hidden = WorldAssist.HiddenBosses.Contains(boss.Key);

				// If the boss should not be visible on the Table of Contents, skip the entry in the list
				if (!visibleList[bossIndex]) {
					continue;
				}

				// Setup display name. Show "???" if unavailable and Silhouettes are turned on
				string displayName = boss.DisplayName;
				BossLogConfiguration cfg = BossChecklist.BossLogConfig;

				bool namesMasked = cfg.MaskNames && !boss.IsDownedOrForced;
				bool hardMode = cfg.MaskHardMode && !Main.hardMode && boss.progression > BossTracker.WallOfFlesh && !boss.IsDownedOrForced;
				bool availability = cfg.HideUnavailable && !boss.available() && !boss.IsDownedOrForced;
				if (namesMasked || hardMode || availability) {
					displayName = "???";
				}

				if (cfg.DrawNextMark && cfg.MaskNames && cfg.UnmaskNextBoss) {
					if (!boss.IsDownedOrForced && boss.available() && !boss.hidden && nextCheck) {
						displayName = boss.DisplayName;
					}
				}

				// The first boss that isnt downed to have a nextCheck will set off the next check for the rest
				// Bosses that ARE downed will still be green due to the ordering of colors within the draw method
				List<string> ChecksRef = Main.LocalPlayer.GetModPlayer<PlayerAssist>().ForceDownsForWorld;

				// Update forced downs. If the boss is actaully downed, remove the force check.
				if (boss.ForceDownedByPlayer(Main.LocalPlayer)) {
					displayName += "*";
					if (boss.downed()) {
						ChecksRef.RemoveAll(x => x == boss.Key);
					}
				}

				TableOfContents next = new TableOfContents(bossIndex, boss.progression, displayName, boss.IsDownedOrForced, nextCheck);
				if (!boss.IsDownedOrForced && boss.available() && !boss.hidden) {
					nextCheck = false;
				}
				
				next.PaddingTop = 5;
				next.PaddingLeft = 22 + (boss.progression <= BossTracker.WallOfFlesh ? 10 : 0);
				next.OnClick += JumpToBossPage;
				next.OnRightClick += HideBoss;

				if (boss.progression <= BossTracker.WallOfFlesh) {
					prehardmodeList.Add(next);
				}
				else {
					hardmodeList.Add(next);
				}
			}

			if (prehardmodeList.Count > 13) {
				PageOne.Append(scrollOne);
			}
			PageOne.Append(prehardmodeList);
			prehardmodeList.SetScrollbar(scrollOne);
			if (hardmodeList.Count > 13) {
				PageTwo.Append(scrollTwo);
			}
			PageTwo.Append(hardmodeList);
			hardmodeList.SetScrollbar(scrollTwo);

			// Calculate Progress Bar downed entries
			int[] prehDown = new int[] { 0, 0, 0 };
			int[] prehTotal = new int[] { 0, 0, 0 };
			int[] hardDown = new int[] { 0, 0, 0 };
			int[] hardTotal = new int[] { 0, 0, 0 };
			Dictionary<string, int[]> prehEntries = new Dictionary<string, int[]>();
			Dictionary<string, int[]> hardEntries = new Dictionary<string, int[]>();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				if (boss.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported) {
					continue;
				}

				if (boss.progression <= BossTracker.WallOfFlesh) {
					if (boss.available() || (boss.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!prehEntries.ContainsKey(boss.modSource)) {
							prehEntries.Add(boss.modSource, new int[] { 0, 0 });
						}
						prehTotal[(int)boss.type]++;
						prehEntries[boss.modSource][1] += 1;

						if (boss.IsDownedOrForced) {
							prehDown[(int)boss.type]++;
							prehEntries[boss.modSource][0] += 1;
						}
					}
				}
				else {
					if (boss.available() || (boss.IsDownedOrForced && BossChecklist.BossLogConfig.HideUnavailable)) {
						if (!hardEntries.ContainsKey(boss.modSource)) {
							hardEntries.Add(boss.modSource, new int[] { 0, 0 });
						}
						hardTotal[(int)boss.type]++;
						hardEntries[boss.modSource][1] += 1;

						if (boss.IsDownedOrForced) {
							if (!hardEntries.ContainsKey(boss.modSource)) {
								hardEntries.Add(boss.modSource, new int[] { 0, 0 });
							}
							hardDown[(int)boss.type]++;
							hardEntries[boss.modSource][0] += 1;
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

		private void UpdateCredits() {
			PageNum = -2;
			ResetBothPages();
			List<string> optedMods = BossUISystem.Instance.OptedModNames;

			pageTwoItemList.Left.Pixels = 15;
			pageTwoItemList.Top.Pixels = 65;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 51;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - pageTwoItemList.Top.Pixels - 80;
			pageTwoItemList.Clear();

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 90;
			scrollTwo.Left.Pixels = 5;
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
				if (optedMods.Count > 11) {
					PageTwo.Append(scrollTwo);
				}
				PageTwo.Append(pageTwoItemList);
				pageTwoItemList.SetScrollbar(scrollTwo);
			}
			else // No mods are using the Log
			{
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

		internal void HideBoss(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is TableOfContents table)
				JumpToBossPage(table.Index, false);
		}

		internal void JumpToBossPage(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is TableOfContents table)
				JumpToBossPage(table.Index, true);
		}

		internal void JumpToBossPage(int index, bool leftClick = true) {
			PageNum = index;
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				// While holding alt, a user can interact with any boss list entry
				// Left-clicking forces a completion check on or off
				// Right-clicking hides the boss from the list
				BossInfo pgBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (leftClick) {
					List<string> list = Main.LocalPlayer.GetModPlayer<PlayerAssist>().ForceDownsForWorld;
					if (list.Contains(pgBoss.Key)) {
						list.RemoveAll(x => x == pgBoss.Key);
					}
					else if (!pgBoss.IsDownedOrForced) {
						list.Add(pgBoss.Key);
					}
					UpdateTableofContents();
				}
				else {
					pgBoss.hidden = !pgBoss.hidden;
					if (pgBoss.hidden) {
						WorldAssist.HiddenBosses.Add(pgBoss.Key);
					}
					else {
						WorldAssist.HiddenBosses.Remove(pgBoss.Key);
					}
					BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
					UpdateTableofContents();
					if (Main.netMode == NetmodeID.MultiplayerClient) {
						ModPacket packet = BossChecklist.instance.GetPacket();
						packet.Write((byte)PacketMessageType.RequestHideBoss);
						packet.Write(pgBoss.Key);
						packet.Write(pgBoss.hidden);
						packet.Send();
					}
				}
				return; // Alt-clicking should never jump to a boss page
			}
			PageOne.RemoveAllChildren();
			ResetPageButtons();
			UpdateCatPage(CategoryPageNum);
			UpdateTabNavPos();
			UpdateFilterTabPos(false); // Clicking entry on the ToC should update filter panel
		}

		private void ResetBothPages() {
			PageOne.RemoveAllChildren();
			PageTwo.RemoveAllChildren();

			scrollOne = new BossLogUIElements.FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Pixels = 50f;
			scrollTwo.Left.Pixels = -13;
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

					FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.LogFeaturesNotAvailable");
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

					FittedTextPanel brokenDisplay = new FittedTextPanel("Mods.BossChecklist.BossLog.DrawnText.NotImplemented");
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
			for (int i = 0; i < AltPageButtons.Length; i++) {
				PageTwo.RemoveChild(AltPageButtons[i]);
			}

			if (PageNum == -3)
				return;

			if (PageNum == -2) {
				PageOne.Append(PrevPage);
			}
			else if (PageNum == -1) {
				PageTwo.Append(NextPage);
			}
			else {
				BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (boss.modSource != "Unknown" && !BossChecklist.DebugConfig.DISABLERECORDTRACKINGCODE) {
					// Only bosses have records. Events will have their own page with banners of the enemies in the event.
					// Spawn and Loot pages do not have alt pages currently, so skip adding them
					bool validRecordPage = CategoryPageNum != CategoryPage.Record || boss.type != EntryType.Boss;
					if (!validRecordPage) {
						PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						int recordIndex = BossChecklist.bossTracker.SortedBosses[PageNum].GetRecordIndex;
						PersonalStats record = modPlayer.RecordsForWorld[recordIndex].stats;
						int totalRecords = (int)RecordCategory.None;
						for (int i = 0; i < totalRecords; i++) {
							if ((i == 1 || i == 2) && record.kills == 0)
								continue; // If a player has no kills against a boss, they can't have a First or Best record, so skip the button creation

							AltPageButtons[i].Width.Pixels = 25;
							AltPageButtons[i].Height.Pixels = 25;
							int offset = record.kills == 0 ? 0 : (i + (i < 2 ? 0 : 1) - 2) * 12;
							if (CategoryPageNum == CategoryPage.Record) {
								// If First or Best buttons were skipped account for the positioning of Previous and World
								if (i < 2) {
									AltPageButtons[i].Left.Pixels = lootButton.Left.Pixels + (i - 2) * 25 + offset;
								}
								else {
									AltPageButtons[i].Left.Pixels = lootButton.Left.Pixels + lootButton.Width.Pixels + (i - 2) * 25 + offset;
								}
							}
							AltPageButtons[i].Top.Pixels = lootButton.Top.Pixels + lootButton.Height.Pixels / 2 - 11;
							PageTwo.Append(AltPageButtons[i]);
						}
					}
				}
				PageOne.Append(PrevPage);
				PageTwo.Append(NextPage);
			}
		}

		public void HandleRecordTypeButton(RecordCategory type, bool leftClick = true) {
			// Doing this in the for loop upon creating the buttons makes the altPage the max value for some reason. This method fixes it.
			if (!leftClick) {
				if (RecordPageSelected == type)
					return;

				if (CompareState == RecordPageSelected) {
					CompareState = RecordCategory.None;
				}
				else if (CompareState != type) {
					CompareState = type;
				}
				else {
					CompareState = RecordCategory.None;
				}
				//Main.NewText($"Set compare value to {CompareState}");
			}
			else {
				// If selecting the compared state altpage, reset compare state
				if (CompareState == type) {
					CompareState = RecordCategory.None;
				}
				UpdateCatPage(CategoryPageNum, type);
			}
		}

		public void UpdateCatPage(CategoryPage catPage, RecordCategory altPage = RecordCategory.None) {
			CategoryPageNum = catPage;
			// If altPage doesn't want to be changed use -1
			if (altPage != RecordCategory.None) {
				RecordPageSelected = altPage;
			}

			if (PageNum == -1) {
				UpdateTableofContents(); // Handle new page
			}
			else if (PageNum == -2) {
				UpdateCredits();
			}
			else {
				if (CategoryPageNum == CategoryPage.Record) {
					OpenRecord();
				}
				else if (CategoryPageNum == CategoryPage.Spawn) {
					OpenSpawn();
				}
				else if (CategoryPageNum == CategoryPage.Loot) {
					OpenLoot();
				}
			}
		}

		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.IsDownedOrForced && x.available() && !x.hidden && x.type == entryType);
		
		public static Color MaskBoss(BossInfo boss) {
			if (!boss.IsDownedOrForced) {
				if (BossChecklist.BossLogConfig.MaskTextures) {
					return Color.Black;
				}
				else if (!Main.hardMode && boss.progression > BossTracker.WallOfFlesh && BossChecklist.BossLogConfig.MaskHardMode) {
					return Color.Black;
				}
				else if (!boss.available()) {
					return Color.Black;
				}
			}
			else if (boss.hidden) {
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

		// Use Main.HoverItemName to get text value
		public static void DrawTooltipBG(SpriteBatch sb, string text, Color textColor = default) {
			if (text == "")
				return;

			int padd = 20;
			Vector2 stringVec = FontAssets.MouseText.Value.MeasureString(text);
			Rectangle bgPos = new Rectangle(Main.mouseX + 20, Main.mouseY + 20, (int)stringVec.X + padd, (int)stringVec.Y + padd - 5);
			bgPos.X = Utils.Clamp(bgPos.X, 0, Main.screenWidth - bgPos.Width);
			bgPos.Y = Utils.Clamp(bgPos.Y, 0, Main.screenHeight - bgPos.Height);

			Vector2 textPos = new Vector2(bgPos.X + padd / 2, bgPos.Y + padd / 2);
			if (textColor == default) {
				textColor = Main.MouseTextColorReal;
			}

			Utils.DrawInvBG(sb, bgPos, new Color(23, 25, 81, 255) * 0.925f);
			Utils.DrawBorderString(sb, text, textPos, textColor);
		}
	}
}
