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
		public static int[] AltPageSelected; // AltPage for Records is "Player Best/World Best(Server)"
		public static int[] TotalAltPages; // The total amount of "subpages" for Records, Spawn, and Loot pages
		public static int CompareState = -1; // Compare record values to one another. Value '-1' means not showing.

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
		public static Asset<Texture2D> bookTexture;
		public static Asset<Texture2D> borderTexture;
		public static Asset<Texture2D> fadedTexture;
		public static Asset<Texture2D> colorTexture;
		public static Asset<Texture2D> prevTexture;
		public static Asset<Texture2D> nextTexture;
		public static Asset<Texture2D> tocTexture;
		public static Asset<Texture2D> credTexture;
		public static Asset<Texture2D> bossNavTexture;
		public static Asset<Texture2D> minibossNavTexture;
		public static Asset<Texture2D> eventNavTexture;
		public static Asset<Texture2D> filterTexture;
		public static Asset<Texture2D> checkMarkTexture;
		public static Asset<Texture2D> xTexture;
		public static Asset<Texture2D> circleTexture;
		public static Asset<Texture2D> checkboxTexture;
		//public static Texture2D silverStarTexture; // unused
		public static Asset<Texture2D> chestTexture;
		public static Asset<Texture2D> starTexture;
		public static Asset<Texture2D> goldChestTexture;
		
		public static int RecipePageNum = 0;
		public static int RecipeShown = 0;
		public static bool showHidden = false;

		private bool bossLogVisible;
		public bool BossLogVisible {
			get { return bossLogVisible; }
			set {
				if (value) {
					Append(BookArea);
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
				CategoryPageNum = 0;
				ToCTab.Left.Set(-416, 0.5f);
				filterPanel.Left.Set(-416 + ToCTab.Width.Pixels, 0.5f);
				foreach (UIText uitext in filterTypes) {
					filterPanel.RemoveChild(uitext);
				}
				UpdateTableofContents();
			}
			else {
				UpdateCatPage(CategoryPageNum);
			}
			BossLogVisible = show;
			if (show) {
				// TODO: Small fix to update hidden list on open
				Main.playerInventory = false;
				Main.LocalPlayer.GetModPlayer<PlayerAssist>().hasOpenedTheBossLog = true; // Removes rainbow glow
			}
		}

		public override void OnInitialize() {
			bookTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Outline");
			borderTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Border");
			fadedTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Faded");
			colorTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Book_Color");

			prevTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Prev");
			nextTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Next");
			tocTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Contents");
			credTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Credits");
			bossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Boss");
			minibossNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Miniboss");
			eventNavTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Event");
			filterTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Filter");

			checkMarkTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Check", AssetRequestMode.ImmediateLoad);
			xTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_X", AssetRequestMode.ImmediateLoad);
			circleTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Next", AssetRequestMode.ImmediateLoad);
			checkboxTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Box", AssetRequestMode.ImmediateLoad);
			chestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Chest");
			starTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_Star");
			goldChestTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Checks_GoldChest");

			bosslogbutton = new BossAssistButton(bookTexture, "Mods.BossChecklist.BossLog.Terms.BossLog") {
				Id = "OpenUI"
			};
			bosslogbutton.Width.Set(34, 0f);
			bosslogbutton.Height.Set(38, 0f);
			bosslogbutton.Left.Set(Main.screenWidth - bosslogbutton.Width.Pixels - 190, 0f);
			bosslogbutton.Top.Pixels = Main.screenHeight - bosslogbutton.Height.Pixels - 8;
			bosslogbutton.OnClick += (a, b) => ToggleBossLog(true);

			AltPageSelected = new int[] {
				0, 0, 0
			};

			TotalAltPages = new int[] {
				4, // Last , Best, First, World
				1, // Spawn
				2 // Loot, Collectibles
			};
			
			ToCTab = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab")) {
				Id = "ToCFilter_Tab"
			};
			ToCTab.Height.Pixels = 76;
			ToCTab.Width.Pixels = 32;
			ToCTab.Left.Set(-416, 0.5f);
			ToCTab.Top.Set(-250 + 20, 0.5f);
			ToCTab.OnClick += OpenViaTab;

			BossTab = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab")) {
				Id = "Boss_Tab"
			};
			BossTab.Height.Pixels = 76;
			BossTab.Width.Pixels = 32;
			BossTab.Left.Set(-416, 0.5f);
			BossTab.Top.Set(-250 + 30 + 76, 0.5f);
			BossTab.OnClick += OpenViaTab;

			MiniBossTab = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab")) {
				Id = "Miniboss_Tab"
			};
			MiniBossTab.Height.Pixels = 76;
			MiniBossTab.Width.Pixels = 32;
			MiniBossTab.Left.Set(-416, 0.5f);
			MiniBossTab.Top.Set(-250 + 40 + (76 * 2), 0.5f);
			MiniBossTab.OnClick += OpenViaTab;

			EventTab = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab")) {
				Id = "Event_Tab"
			};
			EventTab.Height.Pixels = 76;
			EventTab.Width.Pixels = 32;
			EventTab.Left.Set(-416, 0.5f);
			EventTab.Top.Set(-250 + 50 + (76 * 3), 0.5f);
			EventTab.OnClick += OpenViaTab;

			CreditsTab = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Tab")) {
				Id = "Credits_Tab"
			};
			CreditsTab.Height.Pixels = 76;
			CreditsTab.Width.Pixels = 32;
			CreditsTab.Left.Set(-416, 0.5f);
			CreditsTab.Top.Set(-250 + 60 + (76 * 4), 0.5f);
			CreditsTab.OnClick += OpenViaTab;

			BookArea = new BossLogPanel();
			BookArea.Width.Pixels = 800;
			BookArea.Height.Pixels = 478;
			BookArea.Left.Pixels = (Main.screenWidth / 2) - 400;
			BookArea.Top.Pixels = (Main.screenHeight / 2) - (478 / 2) - 6;

			PageOne = new BossLogPanel() {
				Id = "PageOne"
			};
			PageOne.Width.Pixels = 375;
			PageOne.Height.Pixels = 480;
			PageOne.Left.Pixels = (Main.screenWidth / 2) - 400 + 20;
			PageOne.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;

			PrevPage = new BossAssistButton(prevTexture, "") {
				Id = "Previous"
			};
			PrevPage.Width.Pixels = 14;
			PrevPage.Height.Pixels = 20;
			PrevPage.Left.Pixels = 8;
			PrevPage.Top.Pixels = 416;
			PrevPage.OnClick += PageChangerClicked;

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
			scrollTwo.Left.Pixels = -13;
			scrollTwo.Height.Set(-24f, 0.75f);
			scrollTwo.HAlign = 1f;

			PageTwo = new BossLogPanel() {
				Id = "PageTwo"
			};
			PageTwo.Width.Pixels = 375;
			PageTwo.Height.Pixels = 480;
			PageTwo.Left.Pixels = (Main.screenWidth / 2) - 415 + 800 - PageTwo.Width.Pixels;
			PageTwo.Top.Pixels = (Main.screenHeight / 2) - 250 + 12;

			pageTwoItemList = new UIList();

			filterPanel = new BookUI(ModContent.Request<Texture2D>("BossChecklist/Resources/LogUI_Filter")) {
				Id = "filterPanel"
			};
			filterPanel.Height.Pixels = 76;
			filterPanel.Width.Pixels = 152;
			filterPanel.Left.Set(-416, 0.5f);
			filterPanel.Top.Set(-250 + 20, 0.5f);

			filterCheckMark = new List<BookUI>();
			filterCheck = new List<BookUI>();
			filterTypes = new List<UIText>();

			for (int i = 0; i < 3; i++) {
				BookUI newCheck = new BookUI(checkMarkTexture) {
					Id = "C_" + i
				};
				filterCheckMark.Add(newCheck);

				BookUI newCheckBox = new BookUI(checkboxTexture) {
					Id = "F_" + i
				};
				newCheckBox.Top.Pixels = (20 * i) + 5;
				newCheckBox.Left.Pixels = 5;
				newCheckBox.OnClick += ChangeFilter;
				newCheckBox.Append(filterCheckMark[i]);
				filterCheck.Add(newCheckBox);

				string type = i switch {
					1 => "Mini bosses",
					2 => "Events",
					_ => "Bosses"
				};
				UIText bosses = new UIText(type, 0.85f);
				bosses.Top.Pixels = 10 + (20 * i);
				bosses.Left.Pixels = 25;
				filterTypes.Add(bosses);
			}

			NextPage = new BossAssistButton(nextTexture, "") {
				Id = "Next"
			};
			NextPage.Width.Pixels = 14;
			NextPage.Height.Pixels = 20;
			NextPage.Left.Pixels = PageTwo.Width.Pixels + 15 - (int)(NextPage.Width.Pixels * 3);
			NextPage.Top.Pixels = 416;
			NextPage.OnClick += PageChangerClicked;
			PageTwo.Append(NextPage);

			hardmodeList = new UIList();
			hardmodeList.Left.Pixels = 19;
			hardmodeList.Top.Pixels = 44;
			hardmodeList.Width.Pixels = PageOne.Width.Pixels - 60;
			hardmodeList.Height.Pixels = PageOne.Height.Pixels - 136;
			hardmodeList.PaddingTop = 5;

			recordButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.Records");
			recordButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			recordButton.Height.Pixels = 25;
			recordButton.Left.Pixels = 15;
			recordButton.Top.Pixels = 15;
			recordButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Record);
			recordButton.OnRightDoubleClick += (a, b) => ResetStats();

			spawnButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.SpawnInfo");
			spawnButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24;
			spawnButton.Height.Pixels = 25;
			spawnButton.Left.Pixels = PageTwo.Width.Pixels / 2 - 8 + 15;
			spawnButton.Top.Pixels = 15;
			spawnButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Spawn);

			lootButton = new SubpageButton("Mods.BossChecklist.BossLog.DrawnText.LootCollect");
			lootButton.Width.Pixels = PageTwo.Width.Pixels / 2 - 24 + 15;
			lootButton.Height.Pixels = 25;
			lootButton.Left.Pixels = PageTwo.Width.Pixels / 2 - lootButton.Width.Pixels / 2;
			lootButton.Top.Pixels = 50;
			lootButton.OnClick += (a, b) => UpdateCatPage(CategoryPage.Loot);
			lootButton.OnRightDoubleClick += RemoveItem;

			// These will serve as a reservation for our AltPage buttons
			SubpageButton zero = new SubpageButton(0);
			zero.OnClick += (a, b) => ButtonClicked(0);

			SubpageButton one = new SubpageButton(1);
			one.OnClick += (a, b) => ButtonClicked(1);

			SubpageButton two = new SubpageButton(2);
			two.OnClick += (a, b) => ButtonClicked(2);

			SubpageButton three = new SubpageButton(3);
			three.OnClick += (a, b) => ButtonClicked(3);

			AltPageButtons = new SubpageButton[] {
				zero,
				one,
				two,
				three
			};

			toggleHidden = new UIHoverImageButton(TextureAssets.InventoryTickOff, "Toggle hidden visibility");
			toggleHidden.Left.Pixels = 112 - TextureAssets.InventoryTickOff.Width();
			toggleHidden.Top.Pixels = TextureAssets.InventoryTickOff.Height() * 2 / 3;
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
			PageTwo.Left.Pixels = (Main.screenWidth / 2) - 415 + 800 - PageTwo.Width.Pixels;
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

		public void ToggleFilterPanel() {
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
			BossChecklist.SaveConfig(BossChecklist.BossLogConfig);
			UpdateTableofContents();
		}

		private void OpenViaTab(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is not BookUI book)
				return;

			string id = book.Id;
			if (!BookUI.DrawTab(id)) {
				return;
			}

			// Reset new record
			PlayerAssist modPlayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (PageNum >= 0 && modPlayer.hasNewRecord[PageNum]) {
				modPlayer.hasNewRecord[PageNum] = false;
			}

			if (id == "ToCFilter_Tab" && PageNum == -1) {
				ToggleFilterPanel();
				return;
			}
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
			if (PageNum >= 0) {
				ResetBothPages();
				UpdateCatPage(CategoryPageNum);
			}
		}

		// TODO: Test both ResetStats and RemoveItem() in multiplayer
		private void ResetStats() {
			if (BossChecklist.DebugConfig.ResetRecordsBool && CategoryPageNum == 0) {
				BossStats stats = Main.LocalPlayer.GetModPlayer<PlayerAssist>().RecordsForWorld[PageNum].stat;
				stats.kills = 0;
				stats.deaths = 0;

				stats.durationBest = -1;
				stats.durationPrev = -1;

				stats.hitsTakenBest = -1;
				stats.hitsTakenPrev = -1;
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
			if (BossChecklist.DebugConfig.ResetLootItems && CategoryPageNum == CategoryPage.Loot) {
				string id = "";
				if (listeningElement is LogItemSlot slot)
					id = slot.Id;

				if (id == "") {
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						foreach (BossCollection trophy in modPlayer.BossTrophies) {
							if (AltPageSelected[2] == 1) {
								trophy.collectibles.Clear();
							}
							else if (AltPageSelected[2] == 0) {
								trophy.loot.Clear();
							}
						}
					}
					else {
						if (AltPageSelected[2] == 1) {
							modPlayer.BossTrophies[PageNum].collectibles.Clear();
						}
						else if (AltPageSelected[2] == 0) {
							modPlayer.BossTrophies[PageNum].loot.Clear();
						}
					}
				}
				else if (id.StartsWith("collect_")) {
					int itemType = Convert.ToInt32(id.Substring(8));
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						foreach (BossCollection trophy in modPlayer.BossTrophies) {
							trophy.collectibles.RemoveAll(x => x.Type == itemType);
						}
					}
					else {
						List<ItemDefinition> collection = modPlayer.BossTrophies[PageNum].collectibles;
						collection.RemoveAll(x => x.Type == itemType);
					}
				}
				else if (id.StartsWith("loot_")) {
					int itemType = Convert.ToInt32(id.Substring(5));
					if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
						foreach (BossCollection trophy in modPlayer.BossTrophies) {
							trophy.loot.RemoveAll(x => x.Type == itemType);
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

			ResetBothPages();
			UpdateCatPage(CategoryPageNum);
		}

		private void ToggleHidden() {
			showHidden = !showHidden;
			toggleHidden.SetImage(showHidden ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff);
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
			Item spawn = new Item();
			if (BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem[RecipePageNum] != 0) {
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
				spawn.SetDefaults(boss.spawnItem[RecipePageNum]);

				LogItemSlot spawnItemSlot = new LogItemSlot(spawn, false, spawn.HoverName, ItemSlot.Context.EquipDye);
				spawnItemSlot.Height.Pixels = 50;
				spawnItemSlot.Width.Pixels = 50;
				spawnItemSlot.Top.Pixels = 230;
				spawnItemSlot.Left.Pixels = 48 + (56 * 2);
				PageTwo.Append(spawnItemSlot);

				int row = 0;
				int col = 0;
				for (int k = 0; k < ingredients.Count; k++) {
					LogItemSlot ingList = new LogItemSlot(ingredients[k], false, ingredients[k].HoverName, ItemSlot.Context.GuideItem, 0.85f) {
						Id = "ingredient_" + k
					};
					ingList.Height.Pixels = 50;
					ingList.Width.Pixels = 50;
					ingList.Top.Pixels = 240 + (48 * (row + 1));
					ingList.Left.Pixels = 20 + (48 * col);
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
						tileList.Left.Pixels = 20 + (48 * l);
						PageTwo.Append(tileList);
					}
				}

				if (RecipePageNum > 0) {
					BossAssistButton PrevItem = new BossAssistButton(prevTexture, "") {
						Id = "PrevItem"
					};
					PrevItem.Top.Pixels = 245;
					PrevItem.Left.Pixels = 140;
					PrevItem.Width.Pixels = 14;
					PrevItem.Height.Pixels = 20;
					PrevItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(PrevItem);
				}

				if (RecipePageNum < BossChecklist.bossTracker.SortedBosses[PageNum].spawnItem.Count - 1) {
					BossAssistButton NextItem = new BossAssistButton(nextTexture, "") {
						Id = "NextItem"
					};
					NextItem.Top.Pixels = 245;
					NextItem.Left.Pixels = 218;
					NextItem.Width.Pixels = 14;
					NextItem.Height.Pixels = 20;
					NextItem.OnClick += ChangeSpawnItem;
					PageTwo.Append(NextItem);
				}

				if (TotalRecipes > 1) {
					BossAssistButton CycleItem = new BossAssistButton(tocTexture, Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.CycleRecipe")) {
						Id = "CycleItem_" + TotalRecipes
					};
					CycleItem.Top.Pixels = 245;
					CycleItem.Left.Pixels = 55;
					CycleItem.Width.Pixels = 22;
					CycleItem.Height.Pixels = 22;
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
			BossLogPanel.shownAltPage = false;
			if (AltPageSelected[(int)CategoryPageNum] == 1) {
				OpenCollect();
				return;
			}
			if (PageNum < 0) {
				return;
			}

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
			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];
			LootRow newRow = new LootRow(0) {
				Id = "Loot0"
			};

			int row = 0;
			int col = 0;
			foreach (int loot in shortcut.loot) {
				if (BossChecklist.registeredBossBagTypes.Contains(loot)) {
					continue;
				}
				Item selectedItem = new Item();
				selectedItem.SetDefaults(loot);
				if (selectedItem.master) {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(selectedItem, Collection.loot.Any(x => x.Type == selectedItem.type), selectedItem.Name, ItemSlot.Context.TrashItem) {
						Id = "loot_" + selectedItem.type
					};
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Left.Pixels = (col * 56) + 15;
					lootTable.OnRightDoubleClick += RemoveItem;
					newRow.Append(lootTable);
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
				if (selectedItem.expert) {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(selectedItem, Collection.loot.Any(x => x.Type == selectedItem.type), selectedItem.Name, ItemSlot.Context.TrashItem) {
						Id = "loot_" + selectedItem.type
					};
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Left.Pixels = (col * 56) + 15;
					lootTable.OnRightDoubleClick += RemoveItem;
					newRow.Append(lootTable);
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
			}
			foreach (int itemType in shortcut.loot) {
				if (BossChecklist.registeredBossBagTypes.Contains(itemType)) {
					continue;
				}
				Item loot = new Item();
				loot.SetDefaults(itemType);
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
				if (!loot.expert && !loot.master) {
					BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[PageNum];
					LogItemSlot lootTable = new LogItemSlot(loot, Collection.loot.Any(x => x.Type == loot.type), loot.Name, ItemSlot.Context.TrashItem) {
						Id = "loot_" + loot.type
					};
					lootTable.Height.Pixels = 50;
					lootTable.Width.Pixels = 50;
					lootTable.Left.Pixels = (col * 56) + 15;
					lootTable.OnRightDoubleClick += RemoveItem;
					newRow.Append(lootTable);
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
			}
			if (col != 0) {
				col = 0;
				row++;
				pageTwoItemList.Add(newRow);
				newRow = new LootRow(row) {
					Id = "Loot" + row
				};
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

			pageTwoItemList.Left.Pixels = 0;
			pageTwoItemList.Top.Pixels = 235;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 25;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 240 - 75;

			scrollTwo.SetView(10f, 1000f);
			scrollTwo.Top.Pixels = 250;
			scrollTwo.Left.Pixels = -3;
			scrollTwo.Height.Set(-220f, 0.75f);
			scrollTwo.HAlign = 1f;

			pageTwoItemList.Clear();
			LootRow newRow = new LootRow(0) {
				Id = "Collect0"
			};

			BossInfo shortcut = BossChecklist.bossTracker.SortedBosses[PageNum];

			int row = 0;
			int col = 0;
			for (int i = 0; i < shortcut.collection.Count; i++) {
				if (shortcut.collection[i] == -1) {
					continue;
				}
				Item collectible = new Item();
				collectible.SetDefaults(shortcut.collection[i]);

				BossCollection Collection = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[BossLogUI.PageNum];
				LogItemSlot collectionTable = new LogItemSlot(collectible, Collection.collectibles.Any(x => x.Type == collectible.type), collectible.Name) {
					Id = "collect_" + collectible.type
				};
				collectionTable.Height.Pixels = 50;
				collectionTable.Width.Pixels = 50;
				collectionTable.Left.Pixels = (col * 56) + 15;
				collectionTable.OnRightDoubleClick += RemoveItem;
				newRow.Append(collectionTable);
				col++;
				if (col == 6 || i == shortcut.collection.Count - 1) {
					col = 0;
					row++;
					pageTwoItemList.Add(newRow);
					newRow = new LootRow(row) {
						Id = "Collect" + row
					};
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
				bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyBosses && boss.type != EntryType.Boss;
				if (HideUnsupported || HideUnavailable || HideHidden || SkipNonBosses) {
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
				TableOfContents next = new TableOfContents(i, boss.progression, displayName, boss.name, boss.downed(), nextCheck);
				if (!boss.downed() && boss.available() && !boss.hidden) {
					nextCheck = false;
				}
				
				next.PaddingTop = 5;
				next.PaddingLeft = 22;
				next.OnClick += JumpToBossPage;

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

			pageTwoItemList.Left.Pixels = 30;
			pageTwoItemList.Top.Pixels = 75;
			pageTwoItemList.Width.Pixels = PageTwo.Width.Pixels - 66;
			pageTwoItemList.Height.Pixels = PageTwo.Height.Pixels - 75 - 80;
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

				FittedTextPanel brokenDisplay = new FittedTextPanel(Language.GetTextValue("Mods.BossChecklist.BossLog.DrawnText.NoModsSupported"));
				brokenDisplay.Height.Pixels = 200;
				brokenDisplay.Width.Pixels = 340;
				brokenDisplay.Top.Pixels = 0;
				brokenDisplay.Left.Pixels = -15;
				brokenPanel.Append(brokenDisplay);
			}
		}

		internal void JumpToBossPage(UIMouseEvent evt, UIElement listeningElement) {
			if (listeningElement is TableOfContents table)
				JumpToBossPage(table.PageNum);
		}

		internal void JumpToBossPage(int index) {
			PageNum = index;
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				BossInfo pgBoss = BossChecklist.bossTracker.SortedBosses[PageNum];
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
				return;
			}
			PageOne.RemoveAllChildren();
			ResetPageButtons();
			UpdateCatPage(CategoryPageNum);
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
			for (int i = 0; i < AltPageButtons.Length; i++) {
				PageTwo.RemoveChild(AltPageButtons[i]);
			}

			if (PageNum == -2) {
				PageOne.Append(PrevPage);
			}
			else if (PageNum == -1) {
				PageTwo.Append(NextPage);
			}
			else {
				BossInfo boss = BossChecklist.bossTracker.SortedBosses[PageNum];
				if (boss.modSource != "Unknown") {
					// Events do not have records. Instead we create their own page with banners of the enemies in the event.
					bool eventCheck = CategoryPageNum == CategoryPage.Record && boss.type == EntryType.Event;
					if (!eventCheck && CategoryPageNum != CategoryPage.Spawn) {
						for (int i = 0; i < TotalAltPages[(int)CategoryPageNum]; i++) {
							AltPageButtons[i].Width.Pixels = 32;
							AltPageButtons[i].Height.Pixels = 32;
							if (CategoryPageNum == CategoryPage.Record) {
								AltPageButtons[i].Left.Pixels = (PageTwo.Width.Pixels / 2) - 74 + ((AltPageButtons[0].Width.Pixels + 6) * i);
							}
							else {
								AltPageButtons[i].Left.Pixels = PageTwo.Width.Pixels - 24 - AltPageButtons[0].Width.Pixels;
							}
							AltPageButtons[i].Top.Pixels = 86;
							PageTwo.Append(AltPageButtons[i]);
						}
					}
				}
				PageOne.Append(PrevPage);
				PageTwo.Append(NextPage);
			}
		}

		public void ButtonClicked(int num) {
			// Doing this in the for loop upon creating the buttons makes the altPage the max value for some reason. This method fixes it.
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				if (CompareState == AltPageSelected[(int)CategoryPageNum]) {
					CompareState = -1;
				}
				else if (CompareState != num) {
					CompareState = num;
				}
				else {
					CompareState = -1;
				}
				Main.NewText($"Set compare value to {CompareState}");
			}
			else {
				// If selecting the compared state altpage, reset compare state
				if (CompareState == num) {
					CompareState = -1;
				}
				UpdateCatPage(CategoryPageNum, num);
			}
		}

		public void UpdateCatPage(CategoryPage catPage, int altPage = -1) {
			CategoryPageNum = catPage;
			// If altPage doesn't want to be changed use -1
			if (altPage != -1) {
				if (catPage == CategoryPage.Loot) {
					if (AltPageSelected[(int)catPage] == 0) {
						AltPageSelected[(int)catPage] = 1;
					}
					else if (AltPageSelected[(int)catPage] == 1) {
						AltPageSelected[(int)catPage] = 0;
					}
				}
				else {
					AltPageSelected[(int)catPage] = altPage;
				}
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
		
		public static int FindNext(EntryType entryType) => BossChecklist.bossTracker.SortedBosses.FindIndex(x => !x.downed() && x.available() && !x.hidden && x.type == entryType);

		public static Color MaskBoss(BossInfo boss) => ((!boss.downed() && BossChecklist.BossLogConfig.BossSilhouettes) || boss.hidden || (!boss.downed() && !boss.available())) ? Color.Black : Color.White;

		public static Asset<Texture2D> GetBossHead(int boss) => NPCID.Sets.BossHeadTextures[boss] != -1 ? TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[boss]] : TextureAssets.NpcHead[0];

		public static Asset<Texture2D> GetEventIcon(BossInfo boss) {
			if (boss.overrideIconTexture != "" && boss.overrideIconTexture != "Terraria/Images/NPC_Head_0") {
				return ModContent.Request<Texture2D>(boss.overrideIconTexture);
			}

			return boss.internalName switch {
				"Frost Legion"    => ModContent.Request<Texture2D>("Terraria/Images/Extra_7"),
				"Frost Moon"      => ModContent.Request<Texture2D>("Terraria/Images/Extra_8"),
				"Goblin Army"     => ModContent.Request<Texture2D>("Terraria/Images/Extra_9"),
				"Martian Madness" => ModContent.Request<Texture2D>("Terraria/Images/Extra_10"),
				"Pirate Invasion" => ModContent.Request<Texture2D>("Terraria/Images/Extra_11"),
				"Pumpkin Moon"    => ModContent.Request<Texture2D>("Terraria/Images/Extra_12"),
				"Old One's Army"  => BossLogUI.GetBossHead(NPCID.DD2LanePortal),
				"Blood Moon"      => BossChecklist.instance.Assets.Request<Texture2D>("Resources/BossTextures/EventBloodMoon_Head"),
				"Solar Eclipse"   => BossChecklist.instance.Assets.Request<Texture2D>("Resources/BossTextures/EventSolarEclipse_Head"),
				_                 => TextureAssets.NpcHead[0]
			};
		}

		/* Currently removed due to rendering issue that is unable to replicated
		public static Texture2D CropTexture(Texture2D texture, Rectangle snippet) {
			Texture2D croppedTexture = new Texture2D(Main.graphics.GraphicsDevice, snippet.Width, snippet.Height);
			Color[] data = new Color[snippet.Width * snippet.Height];
			texture.GetData(0, snippet, data, 0, data.Length);
			croppedTexture.SetData(data);
			return croppedTexture;
		}
		*/

		public static void OverrideForGroups(Recipe recipe, Item item) {
			// This method taken from RecipeBrowser with permission.
			string nameOverride;
			if (recipe.ProcessGroupsForText(item.type, out nameOverride)) {
				//Main.toolTip.name = name;
			}
			if (nameOverride != "") {
				item.SetNameOverride(nameOverride);
			}
		}

		// Use Main.HoverItemName to get text value
		public static void DrawTooltipBG(SpriteBatch sb, string text, Color textColor = default) {
			if (text == "") {
				return;
			}

			int padd = 20;
			Vector2 stringVec = FontAssets.MouseText.Value.MeasureString(text);
			Rectangle bgPos = new Rectangle(Main.mouseX + 20, Main.mouseY + 20, (int)stringVec.X + padd, (int)stringVec.Y + padd - 5);
			
			Vector2 textPos = new Vector2(Main.mouseX + 20 + padd / 2, Main.mouseY + 20 + padd / 2);
			if (textColor == default) {
				textColor = Main.MouseTextColorReal;
			}

			Utils.DrawInvBG(sb, bgPos, new Color(23, 25, 81, 255) * 0.925f);
			Utils.DrawBorderString(sb, text, textPos, textColor);
		}
	}
}
