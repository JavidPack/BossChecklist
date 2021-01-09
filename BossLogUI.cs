using BossChecklist.UIElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
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
		public UIHoverImageButton toggleHidden;
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
		public BossLogUIElements.FixedUIScrollbar scrollOne;
		public BossLogUIElements.FixedUIScrollbar scrollTwo;

		public UIList pageTwoItemList; // Item slot lists that include: Loot tables, spawn item, and collectibles

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
		public static bool showHidden = false;
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
			if (PageNum == -3) {
				resetPage = true;
			}
			if (resetPage) {
				PageNum = -1;
				SubPageNum = 0;
				ToCTab.Left.Set(-416, 0.5f);
				filterPanel.Left.Set(-416 + ToCTab.Width.Pixels, 0.5f);
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				UpdateTableofContents();
			}
			else {
				UpdateSubPage(SubPageNum);
			}
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
			ToCTab.Left.Set(-416, 0.5f);
			ToCTab.Top.Set(-250 + 20, 0.5f);
			ToCTab.Id = "ToCFilter_Tab";
			ToCTab.OnClick += new MouseEvent(OpenViaTab);

			BossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			BossTab.Height.Pixels = 76;
			BossTab.Width.Pixels = 32;
			BossTab.Left.Set(-416, 0.5f);
			BossTab.Top.Set(-250 + 30 + 76, 0.5f);
			BossTab.Id = "Boss_Tab";
			BossTab.OnClick += new MouseEvent(OpenViaTab);

			MiniBossTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			MiniBossTab.Height.Pixels = 76;
			MiniBossTab.Width.Pixels = 32;
			MiniBossTab.Left.Set(-416, 0.5f);
			MiniBossTab.Top.Set(-250 + 40 + (76 * 2), 0.5f);
			MiniBossTab.Id = "Miniboss_Tab";
			MiniBossTab.OnClick += new MouseEvent(OpenViaTab);

			EventTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			EventTab.Height.Pixels = 76;
			EventTab.Width.Pixels = 32;
			EventTab.Left.Set(-416, 0.5f);
			EventTab.Top.Set(-250 + 50 + (76 * 3), 0.5f);
			EventTab.Id = "Event_Tab";
			EventTab.OnClick += new MouseEvent(OpenViaTab);

			CreditsTab = new BookUI(BossChecklist.instance.GetTexture("Resources/LogUI_Tab"));
			CreditsTab.Height.Pixels = 76;
			CreditsTab.Width.Pixels = 32;
			CreditsTab.Left.Set(-416, 0.5f);
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

			scrollOne = new BossLogUIElements.FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
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
			filterPanel.Left.Set(-416, 0.5f);
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
				if (i == 1) {
					type = "Mini bosses";
				}
				if (i == 2) {
					type = "Events";
				}
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

			toggleHidden = new UIHoverImageButton(Main.inventoryTickOffTexture, "Toggle hidden visibility");
			toggleHidden.Left.Pixels = 112 - Main.inventoryTickOffTexture.Width;
			toggleHidden.Top.Pixels = Main.inventoryTickOffTexture.Height * 2 / 3;
			toggleHidden.OnClick += (a, b) => ToggleHidden();
			filterPanel.Append(toggleHidden);
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
			CreditsTab.Left.Pixels = -416 + (PageNum == -2 ? 0 : 800);
			BossTab.Left.Pixels = -416 + (PageNum >= FindNext(EntryType.Boss) || PageNum == -2 ? 0 : 800);
			MiniBossTab.Left.Pixels = -416 + (PageNum >= FindNext(EntryType.MiniBoss) || PageNum == -2 ? 0 : 800);
			EventTab.Left.Pixels = -416 + (PageNum >= FindNext(EntryType.Event) || PageNum == -2 ? 0 : 800);

			if (PageNum != -1) {
				ToCTab.Left.Set(-416, 0.5f);
				filterPanel.Left.Set(-416 + ToCTab.Width.Pixels, 0.5f);
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
				filterCheckMark[0].SetImage(BossChecklist.BossLogConfig.FilterBosses == "Show" ? checkMarkTexture : circleTexture);

				if (BossChecklist.BossLogConfig.FilterMiniBosses == "Show") {
					filterCheckMark[1].SetImage(checkMarkTexture);
				}
				else if (BossChecklist.BossLogConfig.FilterMiniBosses == "Hide") {
					filterCheckMark[1].SetImage(xTexture);
				}
				else {
					filterCheckMark[1].SetImage(circleTexture);
				}

				if (BossChecklist.BossLogConfig.FilterEvents == "Show") {
					filterCheckMark[2].SetImage(checkMarkTexture);
				}
				else if (BossChecklist.BossLogConfig.FilterEvents == "Hide") {
					filterCheckMark[2].SetImage(xTexture);
				}
				else {
					filterCheckMark[2].SetImage(circleTexture);
				}
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
			if (filterPanel.Left.Pixels != -416 - 120 + ToCTab.Width.Pixels) {
				ToCTab.Left.Set(-416 - 120, 0.5f);
				filterPanel.Left.Set(-416 - 120 + ToCTab.Width.Pixels, 0.5f);
				filterPanel.Width.Pixels = 152;
				foreach (BookUI uiimage in filterCheck) {
					filterPanel.Append(uiimage);
				}
				foreach (UIText uitext in filterTypes) {
					filterPanel.Append(uitext);
				}
			}
			else {
				ToCTab.Left.Set(-416, 0.5f);
				filterPanel.Left.Set(-416 + ToCTab.Width.Pixels, 0.5f);
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
				if (BossChecklist.BossLogConfig.FilterBosses == "Show") {
					BossChecklist.BossLogConfig.FilterBosses = "Hide when completed";
				}
				else {
					BossChecklist.BossLogConfig.FilterBosses = "Show";
				}
			}
			if (rowID == "1") {
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
			if (rowID == "2") {
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
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			UpdateTableofContents();
		}

		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (!BookUI.DrawTab(listeningElement.Id)) {
				return;
			}

			// Reset new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && modPlayer.hasNewRecord[PageNum]) {
				modPlayer.hasNewRecord[PageNum] = false;
			}

			if (listeningElement.Id == "ToCFilter_Tab" && PageNum == -1) {
				ToggleFilterPanel(evt, listeningElement);
				return;
			}
			if (listeningElement.Id == "Boss_Tab") {
				PageNum = FindNext(EntryType.Boss);
			}
			else if (listeningElement.Id == "Miniboss_Tab") {
				PageNum = FindNext(EntryType.MiniBoss);
			}
			else if (listeningElement.Id == "Event_Tab") {
				PageNum = FindNext(EntryType.Event);
			}
			else if (listeningElement.Id == "Credits_Tab") {
				UpdateCredits();
			}
			else {
				UpdateTableofContents();
			}
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
							if (AltPage[2]) {
								modPlayer.BossTrophies[i].collectibles.Clear();
							}
							else {
								modPlayer.BossTrophies[i].loot.Clear();
							}
						}
					}
					else {
						if (AltPage[2]) {
							modPlayer.BossTrophies[PageNum].collectibles.Clear();
						}
						else {
							modPlayer.BossTrophies[PageNum].loot.Clear();
						}
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
				if (RecipeShown == Convert.ToInt32(listeningElement.Id.Substring(index + 1)) - 1) {
					RecipeShown = 0;
				}
				else {
					RecipeShown++;
				}
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

			// Move to next/prev
			List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
			if (listeningElement.Id == "Next") {
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
			if (PageNum >= 0 && (BossList[PageNum].hidden || !BossList[PageNum].available())) {
				while (PageNum >= 0) {
					BossInfo currentBoss = BossList[PageNum];
					if (!currentBoss.hidden && currentBoss.available()) {
						break;
					}
					if (listeningElement.Id == "Next") {
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

			ResetBothPages();
			UpdateSubPage(SubPageNum);
		}

		private void ToggleHidden() {
			showHidden = !showHidden;
			toggleHidden.SetImage(showHidden ? Main.inventoryTickOnTexture : Main.inventoryTickOffTexture);
			UpdateTableofContents();
		}

		private void OpenRecord() {
			ResetBothPages();
			if (PageNum < 0) {
				return;
			}
			// Incase we want to put any UI stuff on these pages
		}

		private void OpenSpawn() {
			ResetBothPages();
			int TotalRecipes = 0;
			if (PageNum < 0) {
				return;
			}
			pageTwoItemList.Clear();

			BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
			if (boss.modSource == "Unknown") {
				return;
			}
			
			var message = new UIMessageBox(Language.GetTextValue(boss.info));
			message.Width.Set(-34f, 1f);
			message.Height.Set(-370f, 1f);
			message.Top.Set(85f, 0f);
			message.Left.Set(-10f, 0f);
			//message.PaddingRight = 30;
			PageTwo.Append(message);

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
			scrollTwo.SetView(100f, 1000f);
			scrollTwo.Top.Set(91f, 0f);
			scrollTwo.Height.Set(-382f, 1f);
			scrollTwo.Left.Set(-20, 0f);
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
							if (tile != -1) {
								requiredTiles.Add(tile);
							}
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
						// Fills in rows with empty slots. New rows start after 7 items
						if (ingList.item.type == 0) {
							break;
						}
						col = 0;
						row++;
					}
					else if (k == 13) {
						// Hopefully no mod uses more than 14 items to craft this spawn item
						// If so, cut off the remaining row to prevent the itemslots from overflowing below the page. TODO? change this?
						break;
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
						if (requiredTiles[l] == -1) {
							break; // Prevents extra empty slots from being created
						}
						LogItemSlot tileList;
						if (requiredTiles[l] == 26) {
							craft.SetDefaults(0);
							string demonAltar = Language.GetTextValue("MapObject.DemonAltar");
							string crimsonAltar = Language.GetTextValue("MapObject.CrimsonAltar");
							string altarType = WorldGen.crimson ? crimsonAltar : demonAltar;
							tileList = new LogItemSlot(craft, false, altarType, ItemSlot.Context.EquipArmorVanity, 0.85f);
						}
						else {
							for (int m = 0; m < ItemLoader.ItemCount; m++) {
								craft.SetDefaults(m);
								if (craft.createTile == requiredTiles[l]) {
									break;
								}
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
				if (TotalRecipes > 0) {
					recipeMessage = Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.RecipeFrom", recipeMod);
				}

				UIText ModdedRecipe = new UIText(recipeMessage, 0.8f);
				ModdedRecipe.Left.Pixels = -5;
				ModdedRecipe.Top.Pixels = 205;
				PageTwo.Append(ModdedRecipe);
			}
		}

		private void OpenLoot() {
			ResetBothPages();
			BossLogPanel.shownAltPage = false;
			if (AltPage[SubPageNum]) {
				OpenCollect();
				return;
			}
			if (PageNum < 0) {
				return;
			}
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
				if (BossChecklist.registeredBossBagTypes.Contains(shortcut.loot[i])) {
					continue;
				}
				Item expertItem = new Item();
				expertItem.SetDefaults(shortcut.loot[i]);
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
				if (BossChecklist.registeredBossBagTypes.Contains(shortcut.loot[i])) {
					continue;
				}
				Item loot = new Item();
				loot.SetDefaults(shortcut.loot[i]);
				if (shortcut.npcIDs[0] < NPCID.Count) {
					if (WorldGen.crimson) {
						if (loot.type == ItemID.DemoniteOre || loot.type == ItemID.CorruptSeeds || loot.type == ItemID.UnholyArrow) {
							continue;
						}
					}
					else { // Corruption
						if (loot.type == ItemID.CrimtaneOre || loot.type == ItemID.CrimsonSeeds) {
							continue;
						}
					}
				}
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
			if (row > 5) {
				PageTwo.Append(scrollTwo);
			}
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		private void OpenCollect() {
			ResetBothPages();
			if (PageNum < 0) {
				return;
			}
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
				if (shortcut.collection[i] == -1) {
					continue;
				}
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
			if (row > 3) {
				PageTwo.Append(scrollTwo);
			}
			PageTwo.Append(pageTwoItemList);
			pageTwoItemList.SetScrollbar(scrollTwo);
		}

		public bool[] CalculateTableOfContents(List<BossInfo> bossList) {
			bool[] visibleList = new bool[bossList.Count];
			for (int i = 0; i < bossList.Count; i++) {
				BossInfo boss = bossList[i];
				// if the boss cannot get through the config checks, it will remain false (invisible)
				bool HideUnsupported = boss.modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported;
				bool HideUnavailable = (!boss.available()) && BossChecklist.BossLogConfig.HideUnavailable;
				bool HideHidden = boss.hidden && !showHidden;
				if (HideUnsupported || HideUnavailable || HideHidden) {
					continue;
				}

				// Check filters as well
				EntryType type = boss.type;
				string bFilter = BossChecklist.BossLogConfig.FilterBosses;
				string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
				string eFilter = BossChecklist.BossLogConfig.FilterEvents;

				bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && boss.downed();
				bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && boss.downed()));
				bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && boss.downed()));
				if (FilterBoss || FilterMiniBoss || FilterEvent) {
					continue;
				}

				visibleList[i] = true; // Boss will show on the Table of Contents
			}
			return visibleList;
		}

		public void UpdateTableofContents() {
			PageNum = -1;
			ResetBothPages();
			bool nextCheck = true;
			prehardmodeList.Clear();
			hardmodeList.Clear();

			List<BossInfo> referenceList = BossChecklist.bossTracker.SortedBosses;
			bool[] visibleList = CalculateTableOfContents(referenceList);

			for (int i = 0; i < visibleList.Length; i++) {
				BossInfo boss = referenceList[i];
				boss.hidden = WorldAssist.HiddenBosses.Contains(boss.Key);
				if (!visibleList[i]) {
					continue;
				}

				// Setup display name. Use Internal Name if config is on, and show "???" if unavailable and Silhouettes are turned on
				string displayName = BossChecklist.DebugConfig.ShowInternalNames ? boss.internalName : boss.name;
				if (!boss.available() && !boss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) {
					displayName = "???";
				}

				// The first boss that isnt downed to have a nextCheck will set off the next check for the rest
				// Bosses that ARE downed will still be green due to the ordering of colors within the draw method
				TableOfContents next = new TableOfContents(boss.progression, displayName, boss.name, nextCheck);
				if (!boss.downed()) {
					nextCheck = false;
				}
				
				next.PaddingTop = 5;
				next.PaddingLeft = 22;
				next.Id = i.ToString();
				next.OnClick += new MouseEvent(JumpToBossPage);

				if (boss.downed()) {
					next.TextColor = Colors.RarityGreen; // TextColor is to prevent flashing text on page updating
					if (boss.progression <= 6f) {
						prehardmodeList.Add(next);
					}
					else {
						hardmodeList.Add(next);
					}
				}
				else {
					next.TextColor = boss.available() ? Colors.RarityRed : Color.SlateGray;
					if (boss.progression <= 6f) {
						prehardmodeList.Add(next);
					}
					else {
						hardmodeList.Add(next);
					}
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
				if (pgBoss.hidden) {
					WorldAssist.HiddenBosses.Add(pgBoss.Key);
				}
				else {
					WorldAssist.HiddenBosses.Remove(pgBoss.Key);
				}
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

			scrollOne = new BossLogUIElements.FixedUIScrollbar();
			scrollOne.SetView(100f, 1000f);
			scrollOne.Top.Pixels = 50f;
			scrollOne.Left.Pixels = -18;
			scrollOne.Height.Set(-24f, 0.75f);
			scrollOne.HAlign = 1f;

			scrollTwo = new BossLogUIElements.FixedUIScrollbar();
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

			if (PageNum == -2) {
				PageOne.Append(PrevPage);
			}
			else if (PageNum == -1) {
				PageTwo.Append(NextPage);
			}
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
			if (PageNum == -1) {
				UpdateTableofContents(); // Handle new page
			}
			else if (PageNum == -2) {
				UpdateCredits();
			}
			else {
				if (SubPageNum == 0) {
					OpenRecord();
				}
				else if (SubPageNum == 1) {
					OpenSpawn();
				}
				else if (SubPageNum == 2) {
					OpenLoot();
				}
			}
		}

		private void SwapRecordPage() {
			AltPage[SubPageNum] = !AltPage[SubPageNum];
			if (SubPageNum == 2) {
				OpenLoot();
			}
			if (SubPageNum == 1) {
				OpenSpawn();
			}
		}
		
		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.downed() && x.type == entryType);

		public static Color MaskBoss(BossInfo boss) => (((!boss.downed() || !boss.available()) && BossChecklist.BossLogConfig.BossSilhouettes) || boss.hidden) ? Color.Black : Color.White;

		public static Texture2D GetBossHead(int boss) => NPCID.Sets.BossHeadTextures[boss] != -1 ? Main.npcHeadBossTexture[NPCID.Sets.BossHeadTextures[boss]] : Main.npcHeadTexture[0];

		public static Texture2D GetEventIcon(BossInfo boss) {
			if (boss.overrideIconTexture != "" && boss.overrideIconTexture != "Terraria/NPC_Head_0") {
				return ModContent.GetTexture(boss.overrideIconTexture);
			}
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
