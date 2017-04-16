using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using Terraria;
using Terraria.UI.Chat;

namespace BossChecklist.UI
{
	class UIBossCheckbox : UIElement
	{
		internal UICheckbox checkbox;
		internal bool expanded;
		BossInfo boss;
		float descriptionHeight = 18;

		public UIBossCheckbox(BossInfo boss)
		{
			this.boss = boss;
			Width = StyleDimension.Fill;
			Height.Pixels = 15;

			checkbox = new UICheckbox(boss.progression, boss.name, 1f, false);
			checkbox.Selected = boss.downed();
			//checkbox.spawnItemID = boss.spawnItemID;
			Append(checkbox);

			OnClick += Box_OnClick;
		}

		private void Box_OnClick(UIMouseEvent evt, UIElement listeningElement)
		{
			UIBossCheckbox clicked = listeningElement as UIBossCheckbox;
			foreach (var item in BossChecklist.instance.bossChecklistUI.checklistList._items)
			{
				UIBossCheckbox box = (item as UIBossCheckbox);
				if (box != clicked)
				{
					box.expanded = false;
					box.Height.Pixels = 15;
					box.Recalculate();
				}
			}

			expanded = !expanded;
			Height.Pixels = expanded ? 15 + descriptionHeight : 15;
			Recalculate();
		}

		public override int CompareTo(object obj)
		{
			UIBossCheckbox other = obj as UIBossCheckbox;
			return boss.progression.CompareTo(other.boss.progression);
		}

		const float infoScaleX = 1f;
		const float infoScaleY = 1f;
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			Rectangle hitbox = GetInnerDimensions().ToRectangle();
			//Main.spriteBatch.Draw(Main.magicPixel, hitbox, Color.Red * 0.6f);

			if (expanded)
			{
				string info = boss.info ?? "No info available";
				int hoveredSnippet = -1;
				TextSnippet[] textSnippets = ChatManager.ParseMessage(info, Color.White);
				ChatManager.ConvertNormalSnippets(textSnippets);

				for (int i = 0; i < ChatManager.ShadowDirections.Length; i++)
				{
					ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, Main.fontMouseText, textSnippets, new Vector2(2, 15 + 3) + hitbox.TopLeft() + ChatManager.ShadowDirections[i] * 1, 
						Color.Black, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), hitbox.Width - (7 * 2), 1);
				}
				Vector2 size = ChatManager.DrawColorCodedString(Main.spriteBatch, Main.fontMouseText, textSnippets,
					new Vector2(2, 15 + 3) + hitbox.TopLeft(), Color.White, 0f, Vector2.Zero, new Vector2(infoScaleX, infoScaleY), out hoveredSnippet, hitbox.Width - (7 * 2), false);

				if (hoveredSnippet > -1)
				{
					// because of draw order, we'll do the hover later.
					BossChecklist.instance.bossChecklistUI.hoveredTextSnipped = textSnippets[hoveredSnippet];
					//array[hoveredSnippet].OnHover();
					//if (Main.mouseLeft && Main.mouseLeftRelease)
					//{
					//	array[hoveredSnippet].OnClick();
					//}
				}

				float newSize = size.Y - hitbox.Y;
				if (newSize != descriptionHeight)
				{
					descriptionHeight = newSize;
					Height.Pixels = 15 + (2 * 3) + descriptionHeight;
					Recalculate();
				}
			}
		}
	}
}

