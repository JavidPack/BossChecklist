using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BossChecklist.UIElements
{
	internal class FixedUIScrollbar : UIScrollbar
	{
		protected override void DrawSelf(SpriteBatch spriteBatch) {
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossUISystem.bossChecklistInterface;
			base.DrawSelf(spriteBatch);
			UserInterface.ActiveInstance = temp;
		}

		public override void MouseDown(UIMouseEvent evt) {
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossUISystem.bossChecklistInterface;
			base.MouseDown(evt);
			UserInterface.ActiveInstance = temp;
		}

		public override void ScrollWheel(UIScrollWheelEvent evt) {
			base.ScrollWheel(evt);
			if (this.Parent != null && this.Parent.IsMouseHovering)
				this.ViewPosition -= (float)evt.ScrollWheelValue / 5; // hovering over the scroll bar will make the scoll slower
		}
	}
}
