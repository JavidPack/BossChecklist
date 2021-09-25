using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BossChecklist
{
	public static class MapAssist
	{
		public static List<Vector2> whitelistPos;
		public static List<int> whitelistType;

		internal static void FullMapInitialize() {
			whitelistPos = new List<Vector2>();
			whitelistType = new List<int>();
		}

		public static void DrawFullscreenMap() {
			UpdateMapLocations();
			DrawIcons();
		}

		private static void UpdateMapLocations() {
			whitelistPos.Clear();
			whitelistType.Clear();

			for (int i = 0; i < Main.maxItems; i++) {
				if (!Main.item[i].active) continue;
				if (WhiteListType(Main.item[i].type) != -1) {
					whitelistPos.Add(Main.item[i].Center);
					whitelistType.Add(Main.item[i].type);
				}
			}
		}

		private static void DrawIcons() {
			for (int v = 0; v < whitelistPos.Count; v++) {
				int type = WhiteListType(whitelistType[v]);
				if (type == -1) continue;
				if (type == 1 && !BossChecklist.ClientConfig.FragmentsBool) continue;
				if (type == 2 && !BossChecklist.ClientConfig.ScalesBool) continue;

				Texture2D drawTexture = TextureAssets.Item[whitelistType[v]].Value;
				DrawAnimation drawAnim = Main.itemAnimations[whitelistType[v]];
				Rectangle sourceRect = drawAnim != null ? drawAnim.GetFrame(drawTexture) : drawTexture.Bounds;
				Vector2 drawPosition = CalculateDrawPos(new Vector2(whitelistPos[v].X / 16, whitelistPos[v].Y / 16));
				
				DrawTextureOnMap(drawTexture, drawPosition, sourceRect);
			}
		}

		private static Vector2 CalculateDrawPos(Vector2 tilePos) {
			Vector2 halfScreen = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
			Vector2 relativePos = tilePos - Main.mapFullscreenPos;
			relativePos *= Main.mapFullscreenScale / 16;
			relativePos = relativePos * 16 + halfScreen;

			Vector2 drawPosition = new Vector2((int)relativePos.X, (int)relativePos.Y);
			return drawPosition;
		}

		private static void DrawTextureOnMap(Texture2D texture, Vector2 drawPosition, Rectangle source) {
			Rectangle drawPos = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, texture.Width, texture.Height);
			Vector2 originLoc = new Vector2(texture.Width / 2, texture.Height / 2);
			Main.spriteBatch.Draw(texture, drawPos, source, Color.White, 0f, originLoc, SpriteEffects.None, 0f);
		}

		public static int WhiteListType(int type) {
			if (type == ItemID.ShadowScale || type == ItemID.TissueSample) {
				return 2;
			}
			else if (RecipeGroup.recipeGroups[RecipeGroupID.Fragment].ValidItems.Any(x => x == type)) {
				return 1;
			}
			else {
				if (BossChecklist.registeredBossBagTypes.Contains(type)) {
					return 0;
				}
				return -1;
			}
		}
	}
}