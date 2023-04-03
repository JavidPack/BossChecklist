using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.ModLoader;
using Terraria.ID;

namespace BossChecklist
{
	class BossRadarUI : UIState
	{
		internal static List<int> type;

		internal static List<int> bossHeadIndex;

		internal static List<Vector2> drawPos;

		internal static List<float> drawRotation;

		internal static List<bool> drawLOS;

		internal static Asset<Texture2D> arrowTexture; //<- = null in Mod.Unload()

		internal static int[] whitelistNPCs;

		internal static bool whitelistFilled = false;

		internal static bool blacklistChanged = false;

		public override void OnInitialize() {
			type = new List<int>();
			bossHeadIndex = new List<int>();
			drawPos = new List<Vector2>();
			drawRotation = new List<float>();
			drawLOS = new List<bool>();
			arrowTexture = ModContent.Request<Texture2D>("BossChecklist/Resources/Extra_RadarArrow");
			whitelistNPCs = new int[0];
		}

		private bool SetDrawPos() {
			type.Clear();
			bossHeadIndex.Clear();
			drawPos.Clear();
			drawRotation.Clear();
			drawLOS.Clear();
			for (int k = 0; k < Main.maxNPCs; k++) {
				NPC npc = Main.npc[k];

				//limit to 20 drawn at all times
				if (type.Count >= 20 || !npc.active) continue;

				//don't draw anything if it has no boss head texture
				int lbossHeadIndex = npc.GetBossHeadTextureIndex();
				if (lbossHeadIndex < 0 || lbossHeadIndex >= TextureAssets.NpcHeadBoss.Length) continue;

				if (Array.BinarySearch(whitelistNPCs, npc.type) > -1) //if in whitelist
				{
					Player player = Main.LocalPlayer;
					Vector2 between = npc.Center - player.Center;
					//screen "radius" is 960, "diameter" is 1920

					int ltype = npc.type;
					Vector2 ldrawPos = Vector2.Zero;

					//when to draw the icon, has to be PendingResolutionWidth/Height because ScreenWidth/Height doesn't work in this case

					//rectangle at which the npc ISN'T rendered (so its sprite won't draw aswell as the NPC itself)

					//independent of resolution, but scales with zoom factor

					float zoomFactorX = 0.25f * BossUISystem.ZoomFactor.X;
					float zoomFactorY = 0.25f * BossUISystem.ZoomFactor.Y;
					//for some reason with small hitbox NPCs, it starts drawing closer to the player than it should when zoomed in too much
					if (zoomFactorX > 0.175f) zoomFactorX = 0.175f;
					if (zoomFactorY > 0.175f) zoomFactorY = 0.175f;

					int rectPosX = (int)(Main.screenPosition.X + (Main.PendingResolutionWidth * zoomFactorX));
					int rectPosY = (int)(Main.screenPosition.Y + (Main.PendingResolutionHeight * zoomFactorY));
					int rectWidth = (int)(Main.PendingResolutionWidth * (1 - 2f * zoomFactorX));
					int rectHeight = (int)(Main.PendingResolutionHeight * (1 - 2f * zoomFactorY));

					//padding for npc height
					Rectangle rectangle = new Rectangle(rectPosX - npc.width / 2,
						rectPosY - npc.height / 2,
						rectWidth + npc.width,
						rectHeight + npc.height);

					if (!rectangle.Intersects(npc.getRect())) {
						if (between.X == 0f) between.X = 0.0001f; //protection against division by zero
						if (between.Y == 0f) between.Y = 0.0001f; //protection against NaN
						if (player.gravDir != 1f) between.Y = -between.Y;
						float slope = between.Y / between.X;

						Vector2 pad = new Vector2
						(
							(Main.screenWidth + npc.width) / 2,
							(Main.screenHeight + npc.height) / 2
						);

						//first iteration

						if (between.Y > 0) //target below player
						{
							//use lower border which is positive
							if (between.Y > pad.Y) {
								ldrawPos.Y = pad.Y;
							}
							else {
								ldrawPos.Y = between.Y;
							}
						}
						else //target above player
						{
							//use upper border which is negative
							if (between.Y < -pad.Y) {
								ldrawPos.Y = -pad.Y;
							}
							else {
								ldrawPos.Y = between.Y;
							}
						}
						ldrawPos.X = ldrawPos.Y / slope;

						//second iteration

						if (ldrawPos.X > 0) //if x is outside the right edge
						{
							//use right border which is positive
							if (ldrawPos.X > pad.X) {
								ldrawPos.X = pad.X;
							}
						}
						else if (ldrawPos.X <= 0) //if x is outside the left edge
						{
							//use left border which is negative
							if (ldrawPos.X <= -pad.X) {
								ldrawPos.X = -pad.X;
							}
						}
						ldrawPos.Y = ldrawPos.X * slope;

						//revert offset
						ldrawPos += new Vector2(pad.X, pad.Y);

						//since we were operating based on Center to Center, we need to put the drawPos back to position instead
						ldrawPos -= npc.Size / 2;

						type.Add(ltype);
						bossHeadIndex.Add(lbossHeadIndex);
						drawPos.Add(ldrawPos);
						drawRotation.Add(between.ToRotation());
						drawLOS.Add(Collision.CanHitLine(player.position, player.width, player.height, npc.position, npc.width, npc.height));
					}
				}
			}
			return type.Count > 0;
		}

		//Update
		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (!BossChecklist.ClientConfig.BossRadarBool) return;

			if (!whitelistFilled || blacklistChanged) {
				List<int> idList = new List<int>();
				foreach (EntryInfo entry in BossChecklist.bossTracker.SortedEntries) {
					if (entry.type == EntryType.Event) continue;
					if (entry.type == EntryType.MiniBoss && !BossChecklist.ClientConfig.RadarMiniBosses) continue;
					foreach (int id in entry.npcIDs) {
						if (!BlackListedID(id) && GetBossHead(id) != TextureAssets.NpcHead[0]) idList.Add(id);
					}
				}
				whitelistNPCs = idList.ToArray();
				Array.Sort(whitelistNPCs);
				whitelistFilled = true;
				blacklistChanged = false;
			}

			SetDrawPos();
		}

		private Asset<Texture2D> GetBossHead(int boss) => NPCID.Sets.BossHeadTextures[boss] != -1 ? TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[boss]] : TextureAssets.NpcHead[0];

		private bool BlackListedID(int ID) {
			return BossChecklist.ClientConfig.RadarBlacklist.Any(npcDef => npcDef.Type == ID);
		}

		//Draw
		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			if (!BossChecklist.ClientConfig.BossRadarBool) return;

			for (int i = 0; i < type.Count; i++) {
				Vector2 ldrawPos = drawPos[i]; //contains top left corner of draw pos

				int headIndex = bossHeadIndex[i];
				if (headIndex == -1) continue;

				Asset<Texture2D> tex = TextureAssets.NpcHeadBoss[headIndex];
				if (tex == null) continue;
				int tempWidth = tex.Width();
				int tempHeight = tex.Height();
				int finalWidth = tex.Width();
				int finalHeight = tex.Height();

				int arrowPad = 10;

				//adjust pos if outside of screen, more padding for arrow
				if (ldrawPos.X >= Main.screenWidth - finalWidth - arrowPad) ldrawPos.X = Main.screenWidth - finalWidth - arrowPad;
				if (ldrawPos.X <= finalWidth + arrowPad) ldrawPos.X = finalWidth + arrowPad;
				if (ldrawPos.Y >= Main.screenHeight - finalHeight - arrowPad) ldrawPos.Y = Main.screenHeight - finalHeight - arrowPad;
				if (ldrawPos.Y <= finalHeight + arrowPad) ldrawPos.Y = finalHeight + arrowPad;

				//create rect around center
				Rectangle outputRect = new Rectangle((int)ldrawPos.X - (finalWidth / 2), (int)ldrawPos.Y - (finalHeight / 2), finalWidth, finalHeight);

				Color color = Color.LightGray;
				color *= drawLOS[i] ? BossChecklist.ClientConfig.OpacityFloat : BossChecklist.ClientConfig.OpacityFloat - 0.25f;
				spriteBatch.Draw(tex.Value, outputRect, new Rectangle(0, 0, tempWidth, tempHeight), color);

				//draw Arrow
				Vector2 stupidOffset = drawRotation[i].ToRotationVector2() * 24f;
				Vector2 drawPosArrow = ldrawPos + stupidOffset;
				color = drawLOS[i] ? Color.Green * BossChecklist.ClientConfig.OpacityFloat : Color.Red * BossChecklist.ClientConfig.OpacityFloat;
				color.A = 150;
				spriteBatch.Draw(arrowTexture.Value, drawPosArrow, null, color, drawRotation[i], arrowTexture.Value.Bounds.Size() / 2, 1f, SpriteEffects.None, 0f);
			}
		}
	}
}