using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using Terraria.GameContent.UI.Chat;
using Terraria;

namespace BossChecklist.UI
{
	class UICheckbox : UIText
	{
		static Texture2D checkboxTexture = ((BossChecklist)ModLoader.GetMod("BossChecklist")).GetTexture("checkBox");
		static Texture2D checkmarkTexture = ((BossChecklist)ModLoader.GetMod("BossChecklist")).GetTexture("checkMark");
		public event EventHandler SelectedChanged;
		float order = 0;
		//public Item spawnItem;
		public int spawnItemID;

		private bool selected = false;
		public bool Selected
		{
			get { return selected; }
			set
			{
				if (value != selected)
				{
					selected = value;
					SelectedChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public UICheckbox(float order, string text, float textScale = 1, bool large = false) : base(text, textScale, large)
		{
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

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle innerDimensions = base.GetInnerDimensions();
			Vector2 pos = new Vector2(innerDimensions.X - 20, innerDimensions.Y - 5);

			spriteBatch.Draw(checkboxTexture, pos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			if (Selected)
				spriteBatch.Draw(checkmarkTexture, pos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

			base.DrawSelf(spriteBatch);

			if (IsMouseHovering && spawnItemID != 0)
			{
				BossChecklistUI.hoverText = "Spawn using: [i:" + spawnItemID + "]";
				//BossChecklistUI.hoverText = "Spawn using: " + ItemTagHandler.GenerateTag(spawnItem);

				// other approaches, for reference
				//  Main.hoverItemName = "Spawn using: " + ItemTagHandler.GenerateTag(spawnItem); // doesn't work for tags.
				//  Dont use MouseText since it doesn't calculate position
				//	Main.instance.MouseText("Spawn using: " + ItemTagHandler.GenerateTag(spawnItem));

				//	Main.toolTip = new Item();
				//  Main.toolTip.SetDefaults(1);
				//	Main.toolTip.name = "Spawn using: " + ItemTagHandler.GenerateTag(spawnItem);
			}
		}

		public override int CompareTo(object obj)
		{
			UICheckbox other = obj as UICheckbox;
			return order.CompareTo(other.order);
		}
	}
}

