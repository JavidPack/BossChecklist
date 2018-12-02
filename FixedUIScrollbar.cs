using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BossChecklist.UI
{
	internal class FixedUIScrollbar : UIScrollbar
	{
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.bossChecklistInterface;
			base.DrawSelf(spriteBatch);
			UserInterface.ActiveInstance = temp;
		}

		public override void MouseDown(UIMouseEvent evt)
		{
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.bossChecklistInterface;
			base.MouseDown(evt);
			UserInterface.ActiveInstance = temp;
		}
	}
}
