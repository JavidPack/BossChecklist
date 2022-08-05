using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist.UIElements
{
	class BossChecklistUI : UIState
	{
		public UIHoverImageButton toggleCompletedButton;
		public UIHoverImageButton toggleMiniBossButton;
		public UIHoverImageButton toggleEventButton;
		public UIHoverImageButton toggleHiddenButton;
		public UIPanel checklistPanel;
		public UIList checklistList;

		float spacing = 8f;
		public static bool Visible {
			get { return BossUISystem.bossChecklistInterface.CurrentState == BossUISystem.Instance.bossChecklistUI; }
			set { BossUISystem.bossChecklistInterface.SetState(value ? BossUISystem.Instance.bossChecklistUI : null); }
		}

		public static bool showCompleted = true;
		public static bool showMiniBoss = true;
		public static bool showEvent = true;
		public static bool showHidden = false;

		bool imagesResized;
		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (IsMouseHovering)
				Terraria.GameInput.PlayerInput.LockVanillaMouseScroll("BossChecklist/BossChecklistUI");

			if (!imagesResized) {
				Main.instance.LoadItem(ItemID.SuspiciousLookingEye);
				//Main.instance.LoadItem(ItemID.CandyCorn);
				//Main.instance.LoadItem(ItemID.SnowGlobe);
				//Main.instance.LoadItem(ItemID.InvisibilityPotion);

				Texture2D completedToggle = ResizeImage(TextureAssets.Item[ItemID.SuspiciousLookingEye].Value, 26, 26);
				//Texture2D miniBossToggle = ResizeImage(TextureAssets.Item[ItemID.CandyCorn].Value, 26, 26);
				//Texture2D eventToggle = ResizeImage(TextureAssets.Item[ItemID.SnowGlobe].Value, 26, 26);
				//Texture2D showHiddenToggle = ResizeImage(TextureAssets.Item[ItemID.InvisibilityPotion].Value, 26, 26);
				toggleCompletedButton.SetImage(TextureAsset(completedToggle));
				//toggleMiniBossButton.SetImage(TextureAsset(miniBossToggle));
				//toggleEventButton.SetImage(TextureAsset(eventToggle));
				//toggleHiddenButton.SetImage(TextureAsset(showHiddenToggle));
				imagesResized = true;
			}
			BossUISystem.Instance.bossChecklistUI.checklistPanel.Left.Pixels = Main.playerInventory ? -200 : 0;
		}

		public static Asset<Texture2D> TextureAsset(Texture2D texture) {
			using MemoryStream stream = new();
			texture.SaveAsPng(stream, texture.Width, texture.Height);
			stream.Position = 0;
			return BossChecklist.instance.Assets.CreateUntracked<Texture2D>(stream, ".png");
		}

		public override void OnInitialize() {
			checklistPanel = new UIPanel();
			checklistPanel.SetPadding(10);
			checklistPanel.Left.Pixels = 0;
			checklistPanel.HAlign = 1f;
			checklistPanel.Top.Set(50f, 0f);
			checklistPanel.Width.Set(250f, 0f);
			checklistPanel.Height.Set(-100, 1f);
			checklistPanel.BackgroundColor = new Color(73, 94, 171);

			var buttonPanel = new UIPanel();
			buttonPanel.BackgroundColor = Color.AliceBlue;
			buttonPanel.SetPadding(6);
			buttonPanel.Width.Set(0, 1f);
			buttonPanel.Height.Set(36, 0f);
			checklistPanel.Append(buttonPanel);

			toggleCompletedButton = new UIHoverImageButton(TextureAssets.MagicPixel, "Toggle Completed");
			toggleCompletedButton.OnClick += ToggleCompletedButtonClicked;
			toggleCompletedButton.Left.Pixels = spacing;
			toggleCompletedButton.Top.Pixels = 0;
			toggleCompletedButton.SetVisibility(1f, 0.7f);
			buttonPanel.Append(toggleCompletedButton);

			toggleMiniBossButton = new UIHoverImageButton(ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Miniboss", AssetRequestMode.ImmediateLoad), "Toggle Mini Bosses");
			toggleMiniBossButton.OnClick += ToggleMiniBossButtonClicked;
			toggleMiniBossButton.Left.Pixels = spacing + 32;
			toggleMiniBossButton.Top.Pixels = 0;
			toggleMiniBossButton.SetVisibility(1f, 0.7f);
			buttonPanel.Append(toggleMiniBossButton);

			toggleEventButton = new UIHoverImageButton(ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Event", AssetRequestMode.ImmediateLoad), "Toggle Events");
			toggleEventButton.OnClick += ToggleEventButtonClicked;
			toggleEventButton.Left.Pixels = spacing + 64;
			toggleEventButton.Top.Pixels = 0;
			toggleEventButton.SetVisibility(1f, 0.7f);
			buttonPanel.Append(toggleEventButton);

			toggleHiddenButton = new UIHoverImageButton(ModContent.Request<Texture2D>("BossChecklist/Resources/Nav_Hidden", AssetRequestMode.ImmediateLoad), "Toggle Show Hidden Bosses\n(Alt-Click to clear Hidden bosses)\n(Alt-Click on boss to hide)");
			toggleHiddenButton.OnClick += ToggleHiddenButtonClicked;
			toggleHiddenButton.Left.Pixels = spacing + 96;
			toggleHiddenButton.Top.Pixels = 0;
			toggleHiddenButton.SetVisibility(1f, 0.7f);
			buttonPanel.Append(toggleHiddenButton);

			checklistList = new UIList();
			checklistList.Top.Pixels = 42f + spacing;
			checklistList.Width.Set(-25f, 1f);
			checklistList.Height.Set(-42f, 1f);
			checklistList.ListPadding = 12f;
			checklistPanel.Append(checklistList);

			FixedUIScrollbar checklistListScrollbar = new FixedUIScrollbar();
			checklistListScrollbar.SetView(100f, 1000f);
			//checklistListScrollbar.Height.Set(0f, 1f);
			checklistListScrollbar.Top.Pixels = 42f + spacing;
			checklistListScrollbar.Height.Set(-42f - spacing, 1f);
			checklistListScrollbar.HAlign = 1f;
			checklistPanel.Append(checklistListScrollbar);
			checklistList.SetScrollbar(checklistListScrollbar);

			// Checklistlist populated when the panel is shown: UpdateCheckboxes()

			Append(checklistPanel);
		}

		private void ToggleCompletedButtonClicked(UIMouseEvent evt, UIElement listeningElement) {
			showCompleted = !showCompleted;
			SoundEngine.PlaySound(showCompleted ? SoundID.MenuOpen : SoundID.MenuClose);
			UpdateCheckboxes();
		}

		private void ToggleMiniBossButtonClicked(UIMouseEvent evt, UIElement listeningElement) {
			showMiniBoss = !showMiniBoss;
			SoundEngine.PlaySound(showMiniBoss ? SoundID.MenuOpen : SoundID.MenuClose);
			UpdateCheckboxes();
		}

		private void ToggleEventButtonClicked(UIMouseEvent evt, UIElement listeningElement) {
			showEvent = !showEvent;
			SoundEngine.PlaySound(showEvent ? SoundID.MenuOpen : SoundID.MenuClose);
			UpdateCheckboxes();
		}

		private void ToggleHiddenButtonClicked(UIMouseEvent evt, UIElement listeningElement) {
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				WorldAssist.HiddenBosses.Clear();
				showHidden = false;
				UpdateCheckboxes();

				if (Main.netMode == NetmodeID.MultiplayerClient) {
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.RequestClearHidden);
					packet.Send();
				}
				SoundEngine.PlaySound(showHidden ? SoundID.MenuOpen : SoundID.MenuClose);
				return;
			}
			showHidden = !showHidden;
			SoundEngine.PlaySound(showHidden ? SoundID.MenuOpen : SoundID.MenuClose);
			UpdateCheckboxes();
		}

		/*public bool ThoriumModDownedScout
		{
			get { return ThoriumMod.ThoriumWorld.downedScout; }
		}
		public bool CalamityDS => CalamityMod.CalamityWorld.downedDesertScourge;*/

		internal void UpdateCheckboxes() {
			var expandedBoss = (checklistList._items.FirstOrDefault(x => x is UIBossCheckbox checkbox && checkbox.expanded) as UIBossCheckbox)?.boss;

			checklistList.Clear();

			foreach (BossInfo boss in BossChecklist.bossTracker.SortedBosses) {
				boss.hidden = WorldAssist.HiddenBosses.Contains(boss.Key);
				if (boss.available() && (!boss.hidden || showHidden)) {
					if (showCompleted || !boss.downed()) {
						if (boss.type == EntryType.Event && !showEvent)
							continue;
						if (boss.type == EntryType.MiniBoss && !showMiniBoss)
							continue;
						UIBossCheckbox box = new UIBossCheckbox(boss);
						checklistList.Add(box);

						if (expandedBoss == boss) {
							box.expanded = true;
							box.PostExpand();
						}
					}
				}
			}

			//if (BossChecklist.instance.thoriumLoaded)
			//{
			//	if (ThoriumModDownedScout)
			//	{
			//		// Add items here
			//	}
			//}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (checklistPanel.ContainsPoint(MousePosition)) {
				Main.player[Main.myPlayer].mouseInterface = true;

				// Doesn't fully fix problem. Clicks still happen in back to front manner.
				//Main.HoverItem = new Item();
				//Main.hoverItemName = "";
			}
		}

		public TextSnippet hoveredTextSnippet;
		public override void Draw(SpriteBatch spriteBatch) {
			base.Draw(spriteBatch);

			// now we can draw after all other drawing.
			if (hoveredTextSnippet != null) {
				hoveredTextSnippet.OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease) {
					hoveredTextSnippet.OnClick();
				}
				hoveredTextSnippet = null;
			}
		}

		private Texture2D ResizeImage(Texture2D texture2D, int desiredWidth, int desiredHeight) {
			RenderTarget2D renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, desiredWidth, desiredHeight);
			Main.instance.GraphicsDevice.SetRenderTarget(renderTarget);
			Main.instance.GraphicsDevice.Clear(Color.Transparent);
			Main.spriteBatch.Begin();

			float scale = 1;
			if (texture2D.Width > desiredWidth || texture2D.Height > desiredHeight) {
				if (texture2D.Height > texture2D.Width)
					scale = (float)desiredWidth / texture2D.Height;
				else
					scale = (float)desiredWidth / texture2D.Width;
			}

			//new Vector2(texture2D.Width / 2 * scale, texture2D.Height / 2 * scale) desiredWidth/2, desiredHeight/2
			Main.spriteBatch.Draw(texture2D, new Vector2(desiredWidth / 2, desiredHeight / 2), null, Color.White, 0f, new Vector2(texture2D.Width / 2, texture2D.Height / 2), scale, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.instance.GraphicsDevice.SetRenderTarget(null);

			Texture2D mergedTexture = new Texture2D(Main.instance.GraphicsDevice, desiredWidth, desiredHeight);
			Color[] content = new Color[desiredWidth * desiredHeight];
			renderTarget.GetData<Color>(content);
			mergedTexture.SetData<Color>(content);
			return mergedTexture;
		}
	}

	internal class HoveredTextSnippetTooltipHack : GlobalItem
	{
		const int paddingForBox = 10;
		public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y) {
			if (BossUISystem.Instance.bossChecklistUI.hoveredTextSnippet != null || BossUISystem.Instance.BossLog.hoveredTextSnippet != null) {
				var texts = lines.Select(z => z.Text);
				string longestText = texts.ToList().OrderByDescending(z => z.Length).First();
				int widthForBox = (int)FontAssets.MouseText.Value.MeasureString(longestText).X;
				int heightForBox = (int)texts.ToList().Sum(z => FontAssets.MouseText.Value.MeasureString(z).Y);

				Vector2 drawPosForBox = new Vector2(x - paddingForBox, y - paddingForBox);
				Rectangle drawRectForBox = new Rectangle(x, y, widthForBox, heightForBox);
				drawRectForBox.Inflate(paddingForBox, paddingForBox);
				// Draw the magic box
				Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectForBox, Color.NavajoWhite);
			}
			return base.PreDrawTooltip(item, lines, ref x, ref y);
		}
	}
}
