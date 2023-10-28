using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace BossChecklist
{
	public static class MapAssist {
		public static void DrawFullscreenMap() {
			foreach (Item item in Main.item) {
				if (!item.active || !IsWhitelistedItem(item.type))
					continue; // do not draw items that are inacive or not whitelisted

				// Icons are draw on map
				Asset<Texture2D> itemTexture = TextureAssets.Item[item.type];
				if (!itemTexture.IsLoaded)
					Main.instance.LoadItem(item.type);

				Main.spriteBatch.Draw(
					texture: itemTexture.Value,
					destinationRectangle: CalculateDrawPos(new Vector2(item.Center.X / 16, item.Center.Y / 16), itemTexture.Value),
					sourceRectangle: Main.itemAnimations[item.type]?.GetFrame(itemTexture.Value) ?? itemTexture.Value.Bounds,
					color: Color.White,
					rotation: 0f,
					origin: new Vector2(itemTexture.Width() / 2, itemTexture.Height() / 2),
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}
		}

		private static Rectangle CalculateDrawPos(Vector2 tilePos, Texture2D texture) {
			Vector2 halfScreen = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
			Vector2 relativePos = tilePos - Main.mapFullscreenPos;
			relativePos *= Main.mapFullscreenScale / 16;
			relativePos = relativePos * 16 + halfScreen;
			return new Rectangle((int)relativePos.X, (int)relativePos.Y, texture.Width, texture.Height);
		}

		public static bool IsWhitelistedItem(int type) {
			if (ItemID.Sets.BossBag[type]) {
				return BossChecklist.ClientConfig.TreasureBagsBool;
			}
			else if (type == ItemID.ShadowScale || type == ItemID.TissueSample) {
				return BossChecklist.ClientConfig.ScalesBool;
			}
			else if (RecipeGroup.recipeGroups[RecipeGroupID.Fragment].ValidItems.Any(x => x == type)) {
				return BossChecklist.ClientConfig.FragmentsBool;
			}
			return false;
		}
	}
}