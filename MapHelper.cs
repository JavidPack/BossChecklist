using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace BossChecklist
{
	public class MapHelper : ModMapLayer {
		public override void Draw(ref MapOverlayDrawContext context, ref string text) {
			if (BossChecklist.FeatureConfig.ItemMapDrawingEnabled is false || Main.item is null)
				return; // loop through items only if at least one of the configs is enabled

			foreach (Item item in Main.ActiveItems) {
				if (!IsWhitelistedItem(item.type))
					continue; // do not draw items that are inacive or not whitelisted

				// Using vanilla function to get the item frame and texture, also takes care of loading the texture.
				Main.GetItemDrawFrame(item.type, out Texture2D itemTexture, out Rectangle itemFrame); 
				
				// Assuming all frames have an equal width and height, calculate the
				// amount of columns and rows the spritesheet is supposed to have.
				int columns = itemTexture.Width / Math.Max(1, itemFrame.Width);
				int rows = itemTexture.Height / Math.Max(1, itemFrame.Height);

				SpriteFrame spriteFrame = new SpriteFrame(
					(byte)columns,
					(byte)rows,
					(byte)(itemFrame.X / Math.Max(1, itemTexture.Width / Math.Max(1, columns))),
					(byte)(itemFrame.Y / Math.Max(1, itemTexture.Height / Math.Max(1, rows))));

				if (context.Draw(itemTexture, item.VisualPosition / 16, Color.White, spriteFrame, 1f, 1.2f, Alignment.Center).IsMouseOver)
					text = item.HoverName; // Display the item's hover name when hovering over the icon
			}
		}

		public static bool IsWhitelistedItem(int type) {
			if (ItemID.Sets.BossBag[type]) {
				return BossChecklist.FeatureConfig.TreasureBagsOnMap;
			}
			else if (type == ItemID.ShadowScale || type == ItemID.TissueSample) {
				return BossChecklist.FeatureConfig.ScalesOnMap;
			}
			else if (RecipeGroup.recipeGroups[RecipeGroupID.Fragment].ValidItems.Contains(type)) {
				return BossChecklist.FeatureConfig.FragmentsOnMap;
			}
			return false;
		}
	}
}
