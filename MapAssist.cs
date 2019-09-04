using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
/// To prevent corruption searching lag in the future


/// ADDITION: Batby suggests a map icon of the nearest corruption/crimson when you talk to the dryad aboout percentage evil
/// ADDITION: Batby suggests a map icon of the nearest corruption/crimson when you talk to the dryad aboout percentage evil
/// ADDITION: Batby suggests a map icon of the nearest corruption/crimson when you talk to the dryad aboout percentage evil

/*

	Ye fabled Crash regaurding the Map. Unsure if fixed now but just in case it isnt:
	
	Index was out of range. Must be non-negative and less than the size of the collection.
	Parameter name: index
	   at System.ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
	   at System.Collections.Generic.List`1.get_Item(Int32 index)
	   at BossAssist.MapAssist.DrawIcons() in C:\Users\TurtleShark's Bros\Documents\My Games\Terraria\ModLoader\Mod Sources\BossAssist\BossLogUI.cs:line 86
	   at BossAssist.MapAssist.DrawFullscreenMap() in C:\Users\TurtleShark's Bros\Documents\My Games\Terraria\ModLoader\Mod Sources\BossAssist\BossLogUI.cs:line 78
	   at BossAssist.BossAssist.PostDrawFullscreenMap(String& mouseText) in C:\Users\TurtleShark's Bros\Documents\My Games\Terraria\ModLoader\Mod Sources\BossAssist\DataManager.cs:line 157
	   at Terraria.ModLoader.ModHooks.PostDrawFullscreenMap(String& mouseText)
	   at Terraria.Main.DrawMap()
	   at Terraria.Main.do_Draw(GameTime gameTime)
	   at Terraria.Main.DoDraw(GameTime gameTime)

*/

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
			tilePos = new Vector2();
			shouldDraw = false;
		}

		public static void DrawFullscreenMap() {
			UpdateMapLocations();
			DrawIcons();
		}

		private static void UpdateMapLocations() {
			whitelistPos.Clear();
			whitelistType.Clear();

			foreach (Item item in Main.item) {
				if (!item.active) continue;
				if (IsWhiteListItem(item)) {
					whitelistPos.Add(item.Center);
					whitelistType.Add(item.type);
				}
			}
		}

		private static void DrawIcons() {
			foreach (Vector2 item in whitelistPos) {
				Texture2D drawTexture = Main.itemTexture[whitelistType[whitelistPos.IndexOf(item)]];
				Vector2 drawPosition = CalculateDrawPos(new Vector2(item.X / 16, item.Y / 16));

				if (WhiteListType(whitelistType[whitelistPos.IndexOf(item)]) == 1 && !BossChecklist.ClientConfig.FragmentsBool) continue;
				if (WhiteListType(whitelistType[whitelistPos.IndexOf(item)]) == 2 && !BossChecklist.ClientConfig.ScalesBool) continue;

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

		#region EvilFinder
		public static Vector2 tilePos;
		public static bool shouldDraw = false;
		public static int evilType = 0;

		public static void DrawNearestEvil(Vector2 pos) {
			if (pos == new Vector2(0, 0) || evilType == 0) return;
			Texture2D drawTexture = null;
			if (evilType == 1) drawTexture = Main.itemTexture[ItemID.CorruptFishingCrate];
			else if (evilType == 2) drawTexture = Main.itemTexture[ItemID.CrimsonFishingCrate];
			Vector2 drawPosition = CalculateDrawPos(pos);

			DrawTextureOnMap(drawTexture, drawPosition);
		}

		public static int ValidEvilTile(int type) {
			List<int> validCrimsonTiles = new List<int>()
			{
				TileID.CrimsonHardenedSand,
				TileID.Crimsand,
				TileID.CrimsonSandstone,
				TileID.Crimstone,
				TileID.CrimtaneThorns,
				TileID.FleshIce,
				TileID.FleshGrass
			};

			List<int> validCorruptionTiles = new List<int>()
			{
				TileID.Ebonstone,
				TileID.Ebonsand,
				TileID.CorruptGrass,
				TileID.CorruptIce,
				TileID.CorruptHardenedSand,
				TileID.CorruptThorns,
				TileID.CorruptSandstone,
			};

			if (validCorruptionTiles.Contains(type)) return 1;
			if (validCrimsonTiles.Contains(type)) return 2;
			return 0;
		}

		public static void LocateNearestEvil() {
			shouldDraw = false;
			tilePos = new Vector2(0, 0);

			float tileDistance = float.MaxValue;
			Vector2 nearestTile = new Vector2(0, 0);

			for (int x = (int)(Main.leftWorld / 16); x < (int)(Main.rightWorld / 16); x++) {
				for (int y = (int)(Main.topWorld / 16); y < (int)(Main.bottomWorld / 16); y++) {
					if (x >= Main.LocalPlayer.position.X) break;
					if (!Main.tile[x, y].active() || ValidEvilTile(Main.tile[x, y].type) == 0) continue;
					float currentTileDistance = Vector2.Distance(new Vector2(x, y).ToWorldCoordinates(), Main.LocalPlayer.Center);
					if (currentTileDistance < tileDistance) {
						tileDistance = currentTileDistance;
						nearestTile = new Vector2(x, y);
						evilType = ValidEvilTile(Main.tile[x, y].type);
					}
				}
			}

			if (tileDistance != float.MaxValue) {
				tilePos = nearestTile;
				shouldDraw = true;
			}
			else {
				shouldDraw = false;
				tilePos = new Vector2(0, 0);
				evilType = 0;
			}
		}
		#endregion
	}
}