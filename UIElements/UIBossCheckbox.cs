﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist.UIElements
{
	// TODO: investigate DD event problem: complete dd1, dd2 and 3 are checked off. -> vanilla bug.
	class UIBossCheckbox : UIElement
	{
		internal UICheckbox checkbox;
		internal UIHoverImageButton moreInfo;
		internal bool expanded;
		internal BossInfo boss;
		private float descriptionHeight = 18;
		private int bossIndex { get; }

		public UIBossCheckbox(BossInfo boss) {
			this.boss = boss;
			Width = StyleDimension.Fill;
			Height.Pixels = 15;

			checkbox = new UICheckbox(boss.progression, boss.DisplayName, 1f, false);
			if (boss.type == EntryType.Event)
				checkbox.TextColor = Color.MediumPurple;
			if (boss.type == EntryType.MiniBoss)
				checkbox.TextColor = Color.CornflowerBlue;
			if (boss.hidden)
				checkbox.TextColor = Color.DarkGreen;
			checkbox.Selected = boss.downed();
			//checkbox.spawnItemID = boss.spawnItemID;
			Append(checkbox);

			moreInfo = new UIHoverImageButton(ModContent.Request<Texture2D>("BossChecklist/UIElements/info", ReLogic.Content.AssetRequestMode.ImmediateLoad), "More Info");
			moreInfo.Left.Set(-24, 1f);
			moreInfo.SetVisibility(1f, 0.7f);
			moreInfo.OnClick += MoreInfo_OnClick;
			bossIndex = boss.GetIndex;

			OnClick += Box_OnClick;
		}

		private void MoreInfo_OnClick(UIMouseEvent evt, UIElement listeningElement) {
			BossUISystem.Instance.BossLog.ToggleBossLog(true);
			BossUISystem.Instance.BossLog.JumpToBossPage(bossIndex);
		}

		private void Box_OnClick(UIMouseEvent evt, UIElement listeningElement) {
			if (evt.Target == moreInfo)
				return;
			if (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) {
				boss.hidden = !boss.hidden;
				if (boss.hidden)
					WorldAssist.HiddenBosses.Add(boss.Key);
				else
					WorldAssist.HiddenBosses.Remove(boss.Key);
				BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
				if (BossChecklist.BossLogConfig.HideUnavailable) {
					BossUISystem.Instance.BossLog.UpdateSelectedPage(BossLogUI.Page_TableOfContents);
				}
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.RequestHideBoss);
					packet.Write(boss.Key);
					packet.Write(boss.hidden);
					packet.Send();
				}
				return;
			}

			UIBossCheckbox clicked = listeningElement as UIBossCheckbox;
			foreach (var item in BossUISystem.Instance.bossChecklistUI.checklistList._items) {
				UIBossCheckbox box = (item as UIBossCheckbox);
				if (box != clicked) {
					box.expanded = false;
					box.AddOrRemoveChild(box.moreInfo, box.expanded);
					box.Height.Pixels = 15;
					box.Recalculate();
				}
			}

			expanded = !expanded;
			PostExpand();
		}

		internal void PostExpand() {
			this.AddOrRemoveChild(moreInfo, expanded);
			Height.Pixels = expanded ? 15 + descriptionHeight : 15;
			Recalculate();
		}

		public override int CompareTo(object obj) {
			UIBossCheckbox other = obj as UIBossCheckbox;
			return boss.progression.CompareTo(other.boss.progression);
		}

		const float infoScaleX = 1f;
		const float infoScaleY = 1f;
		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			Rectangle hitbox = GetInnerDimensions().ToRectangle();
			//Main.spriteBatch.Draw(Main.magicPixel, hitbox, Color.Red * 0.6f);

			if (expanded) {
				string info = boss.DisplaySpawnInfo;
				int hoveredSnippet = -1;
				TextSnippet[] textSnippets = ChatManager.ParseMessage(info, Color.White).ToArray();
				ChatManager.ConvertNormalSnippets(textSnippets);

				foreach (Vector2 direction in ChatManager.ShadowDirections) {
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + direction * 1,
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}
				Vector2 size = ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, textSnippets,
					new Vector2(2, 15 + 3) + hitbox.TopLeft(), Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out hoveredSnippet, hitbox.Width - (7 * 2), false);

				if (hoveredSnippet > -1) {
					// because of draw order, we'll do the hover later.
					BossUISystem.Instance.bossChecklistUI.hoveredTextSnippet = textSnippets[hoveredSnippet];
					//array[hoveredSnippet].OnHover();
					//if (Main.mouseLeft && Main.mouseLeftRelease)
					//{
					//	array[hoveredSnippet].OnClick();
					//}
				}

				float newSize = size.Y - hitbox.Y;
				if (newSize != descriptionHeight) {
					descriptionHeight = newSize;
					Height.Pixels = 15 + (2 * 3) + descriptionHeight;
					Recalculate();
				}
			}
		}
	}
}

