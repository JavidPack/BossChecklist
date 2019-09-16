using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace BossChecklist
{
	class BossRadarUI : UIState
	{
		internal static List<int> type;

		internal static List<int> bossHeadIndex;

		internal static List<Vector2> drawPos;

		internal static List<float> drawRotation;

		internal static List<bool> drawLOS;

		internal static List<Color> drawColor;

		internal static Texture2D arrowTexture; //<- = null in Mod.Unload()

		internal static int[] whitelistNPCs;

		internal static bool whitelistFilled = false;

		public override void OnInitialize() {
			type = new List<int>();
			bossHeadIndex = new List<int>();
			drawPos = new List<Vector2>();
			drawRotation = new List<float>();
			drawLOS = new List<bool>();
			drawColor = new List<Color>();
			arrowTexture = BossChecklist.instance.GetTexture("Resources/RadarArrow");
		}

		private bool SetDrawPos() {
			type.Clear();
			bossHeadIndex.Clear();
			drawPos.Clear();
			drawRotation.Clear();
			drawLOS.Clear();
			drawColor.Clear();
			for (int k = 0; k < 200; k++) {
				NPC npc = Main.npc[k];

				//limit to 20 drawn at all times
				if (type.Count >= 20 || !npc.active) continue;

				if (Array.BinarySearch(whitelistNPCs, npc.type) > -1) //if in whitelist
				{
					Vector2 between = npc.Center - Main.LocalPlayer.Center;
					//screen "radius" is 960, "diameter" is 1920

					int ltype = npc.type;
					Vector2 ldrawPos = Vector2.Zero;

					//when to draw the icon, has to be PendingResolutionWidth/Height because ScreenWidth/Height doesn't work in this case

					//rectangle at which the npc ISN'T rendered (so its sprite won't draw aswell as the NPC itself)

					//independent of resolution, but scales with zoom factor

					float zoomFactorX = 0.25f * BossChecklist.ZoomFactor.X;
					float zoomFactorY = 0.25f * BossChecklist.ZoomFactor.Y;
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
						if (Main.LocalPlayer.gravDir != 1f) between.Y = -between.Y;
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
						ldrawPos -= new Vector2(npc.width / 2, npc.height / 2);

						//get boss head texture if it has one and use that instead of the NPC texture
						int lbossHeadIndex = -1;
						if (npc.GetBossHeadTextureIndex() >= 0 && npc.GetBossHeadTextureIndex() < Main.npcHeadBossTexture.Length) {
							lbossHeadIndex = npc.GetBossHeadTextureIndex();
						}

						//get color if NPC has any
						drawColor.Add(npc.color);

						type.Add(ltype);
						bossHeadIndex.Add(lbossHeadIndex);
						drawPos.Add(ldrawPos);
						drawRotation.Add((float)Math.Atan2(between.Y, between.X));
						drawLOS.Add(Collision.CanHitLine(Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height, npc.position, npc.width, npc.height));
					}
				}
			}
			return type.Count > 0;
		}

		//Update
		public override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (!whitelistFilled) {
				List<int> idList = new List<int>();
				for (int i = 0; i < BossChecklist.bossTracker.SortedBosses.Count; i++) {
					if (BossChecklist.bossTracker.SortedBosses[i].type == BossChecklistType.Event) continue;
					for (int j = 0; j < BossChecklist.bossTracker.SortedBosses[i].npcIDs.Count; j++) {
						int ID = BossChecklist.bossTracker.SortedBosses[i].npcIDs[j];
						if (!BlackListedID(ID)) idList.Add(ID);
					}
				}
				whitelistNPCs = idList.ToArray();
				Array.Sort(whitelistNPCs);
				whitelistFilled = true;
			}
			//do stuff
			SetDrawPos();
		}

		private bool BlackListedID(int ID) {
			if (ID == NPCID.EaterofWorldsBody || ID == NPCID.EaterofWorldsTail) return true;
			return false;
		}

		//Draw
		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			if (!BossChecklist.ClientConfig.BossRadarBool) return;

			for (int i = 0; i < type.Count; i++) {
				Vector2 ldrawPos = drawPos[i]; //contains top left corner of draw pos
				Texture2D tex;
				int tempWidth;
				int tempHeight;
				float scaleFactor;
				int finalWidth;
				int finalHeight;
				//if it's a boss, draw the head texture instead, no scaling
				if (bossHeadIndex[i] != -1) {
					Main.instance.LoadNPC(type[i]);
					tex = Main.npcHeadBossTexture[bossHeadIndex[i]];
					if (tex == null) continue;
					tempWidth = tex.Width;
					tempHeight = tex.Height;
					finalWidth = tex.Width;
					finalHeight = tex.Height;
				}
				else {
					Main.instance.LoadNPC(type[i]);
					//MIGHT NOT BE NEEDED ANYMORE, but leave it in, in case some modded boss doesn't have boss head texture
					//scale image down to max 64x64, only one of them needs to be max
					tex = Main.npcTexture[type[i]];
					if (tex == null) continue;
					tempWidth = tex.Width;
					tempHeight = tex.Height / Main.npcFrameCount[type[i]];
					scaleFactor = (float)64 / ((tempWidth > tempHeight) ? tempWidth : tempHeight);
					if (scaleFactor > 0.75f) //because when fully zoomed out, the texture isn't actually drawn in 1:1 scale onto the screen
					{
						scaleFactor = 0.75f; //only scale down, don't scale up
					}
					finalWidth = (int)(tempWidth * scaleFactor);
					finalHeight = (int)(tempHeight * scaleFactor);
				}

				int arrowPad = 10;

				//adjust pos if outside of screen, more padding for arrow
				if (ldrawPos.X >= Main.screenWidth - finalWidth - arrowPad) ldrawPos.X = Main.screenWidth - finalWidth - arrowPad;
				if (ldrawPos.X <= finalWidth + arrowPad) ldrawPos.X = finalWidth + arrowPad;
				if (ldrawPos.Y >= Main.screenHeight - finalHeight - arrowPad) ldrawPos.Y = Main.screenHeight - finalHeight - arrowPad;
				if (ldrawPos.Y <= finalHeight + arrowPad) ldrawPos.Y = finalHeight + arrowPad;

				//create rect around center
				Rectangle outputRect = new Rectangle((int)ldrawPos.X - (finalWidth / 2), (int)ldrawPos.Y - (finalHeight / 2), finalWidth, finalHeight);

				//set color overlay if NPC has one
				Color color = Color.LightGray;
				if (drawColor[i] != default(Color)) {
					color = new Color(
						Math.Max(drawColor[i].R - 25, 50),
						Math.Max(drawColor[i].G - 25, 50),
						Math.Max(drawColor[i].B - 25, 50),
						Math.Max((byte)(drawColor[i].A * 1.5f), (byte)75));
				}
				color *= drawLOS[i] ? 0.75f : 0.5f;
				spriteBatch.Draw(tex, outputRect, new Rectangle(0, 0, tempWidth, tempHeight), color);

				//draw Arrow
				Vector2 stupidOffset = drawRotation[i].ToRotationVector2() * 24f;
				Vector2 drawPosArrow = ldrawPos + stupidOffset;
				color = drawLOS[i] ? Color.Green * 0.75f : Color.Red * 0.75f;
				color.A = 150;
				spriteBatch.Draw(arrowTexture, drawPosArrow, null, color, drawRotation[i], arrowTexture.Bounds.Size() / 2, 1f, SpriteEffects.None, 0f);
			}
		}
	}
}