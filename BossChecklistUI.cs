using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;

namespace BossChecklist.UI
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
		public static bool visible = false;
		public static bool showCompleted = true;
		public static bool showMiniBoss = true;
		public static bool showEvent = true;
		public static bool showHidden = false;
		public static string hoverText = "";

		public override void OnInitialize()
		{
			checklistPanel = new UIPanel();
			checklistPanel.SetPadding(10);
			checklistPanel.Left.Pixels = 0;
			checklistPanel.HAlign = 1f;
			checklistPanel.Top.Set(50f, 0f);
			checklistPanel.Width.Set(250f, 0f);
			checklistPanel.Height.Set(-100, 1f);
			checklistPanel.BackgroundColor = new Color(73, 94, 171);

			//Main.spriteBatch.Begin(); 
			Texture2D completedToggle = ResizeImage(Main.itemTexture[ItemID.SuspiciousLookingEye], 32, 32);
			Texture2D miniBossToggle = ResizeImage(Main.itemTexture[ItemID.CandyCorn], 32, 32);
			Texture2D eventToggle = ResizeImage(Main.itemTexture[ItemID.SnowGlobe], 32, 32);
			Texture2D showHiddenToggle = ResizeImage(Main.itemTexture[ItemID.InvisibilityPotion], 32, 32);
			//Main.spriteBatch.End();

			toggleCompletedButton = new UIHoverImageButton(completedToggle, "Toggle Completed");
			toggleCompletedButton.OnClick += ToggleCompletedButtonClicked;
			toggleCompletedButton.Left.Pixels = spacing;
			toggleCompletedButton.Top.Pixels = 0;
			checklistPanel.Append(toggleCompletedButton);

			toggleMiniBossButton = new UIHoverImageButton(miniBossToggle, "Toggle Mini Bosses");
			toggleMiniBossButton.OnClick += ToggleMiniBossButtonClicked;
			toggleMiniBossButton.Left.Pixels = spacing + 32;
			toggleMiniBossButton.Top.Pixels = 0;
			checklistPanel.Append(toggleMiniBossButton);

			toggleEventButton = new UIHoverImageButton(eventToggle, "Toggle Events");
			toggleEventButton.OnClick += ToggleEventButtonClicked;
			toggleEventButton.Left.Pixels = spacing + 64;
			toggleEventButton.Top.Pixels = 0;
			checklistPanel.Append(toggleEventButton);

			toggleHiddenButton = new UIHoverImageButton(showHiddenToggle, "Toggle Show Hidden Bosses\n(Alt-Click to clear Hidden bosses)\n(Alt-Click on boss to hide)");
			toggleHiddenButton.OnClick += ToggleHiddenButtonClicked;
			toggleHiddenButton.Left.Pixels = spacing + 96;
			toggleHiddenButton.Top.Pixels = 0;
			checklistPanel.Append(toggleHiddenButton);

			checklistList = new UIList();
			checklistList.Top.Pixels = 32f + spacing;
			checklistList.Width.Set(-25f, 1f);
			checklistList.Height.Set(-32f, 1f);
			checklistList.ListPadding = 12f;
			checklistPanel.Append(checklistList);

			FixedUIScrollbar checklistListScrollbar = new FixedUIScrollbar();
			checklistListScrollbar.SetView(100f, 1000f);
			//checklistListScrollbar.Height.Set(0f, 1f);
			checklistListScrollbar.Top.Pixels = 32f + spacing;
			checklistListScrollbar.Height.Set(-32f - spacing, 1f);
			checklistListScrollbar.HAlign = 1f;
			checklistPanel.Append(checklistListScrollbar);
			checklistList.SetScrollbar(checklistListScrollbar);

			// Checklistlist populated when the panel is shown: UpdateCheckboxes()

			Append(checklistPanel);

			// TODO, game window resize issue
		}

		private void ToggleCompletedButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showCompleted = !showCompleted;
			UpdateCheckboxes();
		}

		private void ToggleMiniBossButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showMiniBoss = !showMiniBoss;
			UpdateCheckboxes();
		}

		private void ToggleEventButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showEvent = !showEvent;
			UpdateCheckboxes();
		}

		private void ToggleHiddenButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt))
			{
				BossChecklistWorld.HiddenBosses.Clear();
				showHidden = false;
				UpdateCheckboxes();

				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)BossChecklistMessageType.RequestClearHidden);
					packet.Send();
				}
				return;
			}
			showHidden = !showHidden;
			UpdateCheckboxes();
		}

		/*public bool ThoriumModDownedScout
		{
			get { return ThoriumMod.ThoriumWorld.downedScout; }
		}
		public bool CalamityDS => CalamityMod.CalamityWorld.downedDesertScourge;*/

		internal void UpdateCheckboxes()
		{
			checklistList.Clear();

			foreach (BossInfo boss in BossChecklist.bossTracker.allBosses)
			{
				boss.hidden = BossChecklistWorld.HiddenBosses.Contains(boss.name);
				if (boss.available() && (!boss.hidden || showHidden))
				{
					if (showCompleted || !boss.downed())
					{
						if (boss.type == BossChecklistType.Event && !showEvent)
							continue;
						if (boss.type == BossChecklistType.MiniBoss && !showMiniBoss)
							continue;
						UIBossCheckbox box = new UIBossCheckbox(boss);
						checklistList.Add(box);
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

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			hoverText = "";
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (checklistPanel.ContainsPoint(MousePosition))
			{
				Main.player[Main.myPlayer].mouseInterface = true;
			}
		}

		public TextSnippet hoveredTextSnipped;
		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);

			// now we can draw after all other drawing.
			if (hoveredTextSnipped != null)
			{
				hoveredTextSnipped.OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{
					hoveredTextSnipped.OnClick();
				}
				hoveredTextSnipped = null;
			}
		}

		private Texture2D ResizeImage(Texture2D texture2D, int desiredWidth, int desiredHeight)
		{
			RenderTarget2D renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, desiredWidth, desiredHeight);
			Main.instance.GraphicsDevice.SetRenderTarget(renderTarget);
			Main.instance.GraphicsDevice.Clear(Color.Transparent);
			Main.spriteBatch.Begin();

			float scale = 1;
			if (texture2D.Width > desiredWidth || texture2D.Height > desiredHeight)
			{
				if (texture2D.Height > texture2D.Width)
					scale = (float)desiredWidth / texture2D.Height;
				else
					scale = (float)desiredWidth / texture2D.Width;
			}

			//new Vector2(texture2D.Width / 2 * scale, texture2D.Height / 2 * scale) desiredWidth/2, desiredHeight/2
			Main.spriteBatch.Draw(texture2D, new Vector2(desiredWidth / 2, desiredHeight / 2), null, Color.White, 0f, new Vector2(texture2D.Width / 2, texture2D.Height / 2), scale, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.instance.GraphicsDevice.SetRenderTarget(null);
			return renderTarget;
		}
	}
}
