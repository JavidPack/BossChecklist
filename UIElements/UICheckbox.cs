using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BossChecklist.UIElements
{
	class UICheckbox : UIText {
		internal static Asset<Texture2D> checkboxTexture;
		internal static Asset<Texture2D> checkmarkTexture;
		public event EventHandler SelectedChanged;
		float order = 0;
		//public Item spawnItem;
		//public int spawnItemID;

		private bool selected = false;
		public bool Selected {
			get { return selected; }
			set {
				if (value != selected) {
					selected = value;
					SelectedChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public UICheckbox(float order, string text, float textScale = 1, bool large = false) : base(text, textScale, large) {
			this.order = order;
			this.Left.Pixels += 20;
			//TextColor = Color.Blue;
			//OnClick += UICheckbox_onLeftClick;
			Recalculate();
		}

		//private void UICheckbox_onLeftClick(UIMouseEvent evt, UIElement listeningElement)
		//{
		//	this.Selected = !Selected;
		//}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			CalculatedStyle innerDimensions = base.GetInnerDimensions();
			Vector2 pos = new Vector2(innerDimensions.X - 20, innerDimensions.Y - 5);

			//Rectangle hitbox = GetInnerDimensions().ToRectangle();
			//Main.spriteBatch.Draw(Main.magicPixel, hitbox, Color.Red * 0.6f);

			spriteBatch.Draw(checkboxTexture.Value, pos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			if (Selected)
				spriteBatch.Draw(checkmarkTexture.Value, pos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

			base.DrawSelf(spriteBatch);
		}

		public override int CompareTo(object obj) {
			UICheckbox other = obj as UICheckbox;
			return order.CompareTo(other.order);
		}
	}
}

