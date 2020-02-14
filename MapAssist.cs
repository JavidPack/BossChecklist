using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace BossChecklist
{
	public static class MapAssist
	{
		#region [Item Drawing]
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
				if (IsWhiteListItem(Main.item[i])) {
					whitelistPos.Add(Main.item[i].Center);
					whitelistType.Add(Main.item[i].type);
				}
			}
		}

		private static void DrawIcons() {
			for (int v = 0; v < whitelistPos.Count; v++) {
				Texture2D drawTexture = Main.itemTexture[whitelistType[v]];
				Vector2 drawPosition = CalculateDrawPos(new Vector2(whitelistPos[v].X / 16, whitelistPos[v].Y / 16));

				int type = WhiteListType(whitelistType[v]);
				if (type == -1) continue;
				if (type == 1 && !BossChecklist.ClientConfig.FragmentsBool) continue;
				if (type == 2 && !BossChecklist.ClientConfig.ScalesBool) continue;

				DrawTextureOnMap(drawTexture, drawPosition);
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

		private static void DrawTextureOnMap(Texture2D texture, Vector2 drawPosition) {
			Rectangle drawPos = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, texture.Width, texture.Height);
			Vector2 originLoc = new Vector2(texture.Width / 2, texture.Height / 2);
			Main.spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, originLoc, SpriteEffects.None, 0f);
		}

		public static bool IsWhiteListItem(Item item) {
			if (item.consumable && item.Name == "Treasure Bag" && item.expert) return true;
			if (item.rare == 9 && item.damage <= 0 && item.Name.Contains("Fragment")) return true;
			if (item.type == ItemID.ShadowScale || item.type == ItemID.TissueSample) return true;
			return false;
		}

		public static int WhiteListType(int type) {
			Item item = new Item();
			item.SetDefaults(type);

			if (item.consumable && item.Name == "Treasure Bag" && item.expert) return 0;
			if (item.rare == 9 && item.damage <= 0 && item.Name.Contains("Fragment")) return 1;
			if (item.type == ItemID.ShadowScale || item.type == ItemID.TissueSample) return 2;
			return -1;
		}
		#endregion
	}
}