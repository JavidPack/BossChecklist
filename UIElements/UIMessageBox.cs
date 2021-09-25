using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BossChecklist.UIElements
{
	// This class is a WIP implementation migrated from RecipeBrowser.
	internal class UIMessageBox : UIPanel
	{
		private string text;
		protected UIScrollbar _scrollbar;
		private float height;
		internal bool heightNeedsRecalculating;
		//private List<Tuple<string, float>> drawtexts = new List<Tuple<string, float>>();
		private List<List<TextSnippet>> drawTextSnippets = new List<List<TextSnippet>>();

		public UIMessageBox(string text) {
			this.text = text;
			if (this._scrollbar != null) {
				this._scrollbar.ViewPosition = 0;
				heightNeedsRecalculating = true;
			}
			OverflowHidden = true; // DrawChildren for OverflowHidden, lazy.
		}

		//protected override void DrawChildren(SpriteBatch spriteBatch)
		//{
		//	base.DrawChildren(spriteBatch);

		//	Vector2 position = this.Parent.GetDimensions().Position();
		//	Vector2 dimensions = new Vector2(this.Parent.GetDimensions().Width, this.Parent.GetDimensions().Height);
		//	foreach (UIElement current in this.Elements)
		//	{
		//		Vector2 position2 = current.GetDimensions().Position();
		//		Vector2 dimensions2 = new Vector2(current.GetDimensions().Width, current.GetDimensions().Height);
		//		if (Collision.CheckAABBvAABBCollision(position, dimensions, position2, dimensions2))
		//		{
		//			current.Draw(spriteBatch);
		//		}
		//	}
		//}

		public override void OnActivate() {
			base.OnActivate();
			heightNeedsRecalculating = true;
		}

		internal void SetText(string text) {
			this.text = text;
			if (this._scrollbar != null) {
				this._scrollbar.ViewPosition = 0;
				heightNeedsRecalculating = true;
			}
		}

		protected override void DrawChildren(SpriteBatch spriteBatch) {
			base.DrawChildren(spriteBatch);

			CalculatedStyle space = GetInnerDimensions();
			//Main.spriteBatch.Draw(Main.magicPixel, space.ToRectangle(), Color.Yellow * .7f);
			//Main.spriteBatch.Draw(Main.magicPixel, GetOuterDimensions().ToRectangle(), Color.Red * .7f);
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			float position = 0f;
			if (this._scrollbar != null) {
				position = -this._scrollbar.GetValue();
			}
			//foreach (var drawtext in drawtexts)
			//{
			//	if (position + drawtext.Item2 > space.Height)
			//		break;
			//	if (position >= 0)
			//		Utils.DrawBorderString(spriteBatch, drawtext.Item1, new Vector2(space.X, space.Y + position), Color.White, 1f);
			//	position += drawtext.Item2;
			//}
			//float offset = 0;
			TextSnippet[] texts;
			foreach (var snippetList in drawTextSnippets) {
				texts = snippetList.ToArray();
				float snippetListHeight = ChatManager.GetStringSize(font, texts, Vector2.One).Y;
				//Main.NewText($"Y: {ChatManager.GetStringSize(font, texts, Vector2.One).X}");
				if (position > -snippetListHeight) {
					//foreach (var snippet in snippetList)
					//{
					int hoveredSnippet = -1;
					ChatManager.ConvertNormalSnippets(texts);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, texts, new Vector2(space.X, space.Y + position /*+ offset*/), 0f, Vector2.Zero, Vector2.One, out hoveredSnippet);
					//offset += 20;
					//offset += texts.Max(t => (int)ChatManager.GetStringSize(FontAssets.MouseText, texts, Vector2.One).X);
					if (hoveredSnippet > -1 && IsMouseHovering) {
						BossUISystem.Instance.BossLog.hoveredTextSnippet = texts[hoveredSnippet];
						// BossChecklist change: Use hoveredTextSnippet to bypass clippingRectangle and draw order issues.
						//texts[hoveredSnippet].OnHover();
						//if (Main.mouseLeft && Main.mouseLeftRelease/* && Terraria.GameInput.PlayerInput.Triggers.JustReleased.MouseLeft*/) {
						//	texts[hoveredSnippet].OnClick();
						//}
					}
				}
				position += snippetListHeight;
				if (position > space.Height)
					break;
				//}
			}
			//if (drawTextSnippets.Count > 0)
			//{
			//	//ChatManager.DrawColorCodedStringShadow()
			//	int hoveredSnippet = -1;
			//	ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, drawTextSnippets[0].ToArray(), new Vector2(space.X, space.Y + position), 0f, Vector2.Zero, Vector2.One, out hoveredSnippet);
			//	if (hoveredSnippet > -1)
			//	{
			//		//array[hoveredSnippet].OnHover();
			//	}
			//}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			this.Recalculate();
		}

		public override void RecalculateChildren() {
			base.RecalculateChildren();
			if (!heightNeedsRecalculating) {
				return;
			}
			CalculatedStyle space = GetInnerDimensions();
			if (space.Width <= 0 || space.Height <= 0) {
				return;
			}
			DynamicSpriteFont font = FontAssets.MouseText.Value;

			drawTextSnippets = WordwrapStringSmart(text, Color.White, font, (int)space.Width, -1);
			//height = ChatManager.GetStringSize(font, text, Vector2.One, space.Width).Y;

			//Main.NewText($"h: {height} ");
			//Main.NewText($"a: {drawTextSnippets[0][0].GetStringLength(font)} ");
			//// 18* 28 == 504
			//// 18 * ? == 588
			//float hsum = 0;
			height = 0;
			foreach (var snippetList in drawTextSnippets) {
				var texts = snippetList.ToArray();
				height += ChatManager.GetStringSize(font, texts, Vector2.One).Y;
				//Main.NewText($"calc Y: {ChatManager.GetStringSize(font, texts, Vector2.One).Y}");
			}

			//Main.NewText($"Count: {drawTextSnippets.Count}  Height:{height}    sum: {hsum}");

			//height = hsum;

			//height = ChatManager.GetStringSize(font, "1", Vector2.One).Y;

			/*
			drawtexts.Clear();
			float position = 0f;
			float textHeight = font.MeasureString("A").Y;
			string[] lines = text.Split('\n');
			foreach (string line in lines)
			{
				string drawString = line;
				if (drawString.Length == 0)
				{
					position += textHeight;
				}
				while (drawString.Length > 0)
				{
					string remainder = "";
					while (font.MeasureString(drawString).X > space.Width)
					{
						remainder = drawString[drawString.Length - 1] + remainder;
						drawString = drawString.Substring(0, drawString.Length - 1);
					}
					if (remainder.Length > 0)
					{
						int index = drawString.LastIndexOf(' ');
						if (index >= 0)
						{
							remainder = drawString.Substring(index + 1) + remainder;
							drawString = drawString.Substring(0, index);
						}
					}
					drawtexts.Add(new Tuple<string, float>(drawString, textHeight));
					position += textHeight;
					drawString = remainder;
				}
			}
			height = position;
			*/
			heightNeedsRecalculating = false;
		}

		public override void Recalculate() {
			base.Recalculate();
			this.UpdateScrollbar();
		}

		public override void ScrollWheel(UIScrollWheelEvent evt) {
			base.ScrollWheel(evt);
			if (this._scrollbar != null) {
				this._scrollbar.ViewPosition -= (float)evt.ScrollWheelValue;
			}
		}

		public void SetScrollbar(UIScrollbar scrollbar) {
			this._scrollbar = scrollbar;
			this.UpdateScrollbar();
			this.heightNeedsRecalculating = true;
		}

		private void UpdateScrollbar() {
			if (this._scrollbar == null) {
				return;
			}
			this._scrollbar.SetView(base.GetInnerDimensions().Height, this.height);
		}

		// Attempt at fix: problems: long words crash it, spaces seem to be miscounted.
		public static List<List<TextSnippet>> WordwrapStringSmart(string text, Color c, DynamicSpriteFont font, int maxWidth, int maxLines) {
			TextSnippet[] array = ChatManager.ParseMessage(text, c).ToArray();
			List<List<TextSnippet>> finalList = new List<List<TextSnippet>>();
			List<TextSnippet> list2 = new List<TextSnippet>();
			for (int i = 0; i < array.Length; i++) {
				TextSnippet textSnippet = array[i];
				string[] array2 = textSnippet.Text.Split(new char[]
					{
						'\n'
					});
				for (int j = 0; j < array2.Length - 1; j++) {
					list2.Add(textSnippet.CopyMorph(array2[j]));
					finalList.Add(list2);
					list2 = new List<TextSnippet>();
				}
				list2.Add(textSnippet.CopyMorph(array2[array2.Length - 1]));
			}
			finalList.Add(list2);
			if (maxWidth != -1) {
				for (int k = 0; k < finalList.Count; k++) {
					List<TextSnippet> currentLine = finalList[k];
					float usedWidth = 0f;
					for (int l = 0; l < currentLine.Count; l++) {
						//float stringLength = list3[l].GetStringLength(font); // GetStringLength doesn't match UniqueDraw
						float stringLength = ChatManager.GetStringSize(font, new TextSnippet[] { currentLine[l] }, Vector2.One).X;
						//float stringLength2 = ChatManager.GetStringSize(font, " ", Vector2.One).X;
						//float stringLength3 = ChatManager.GetStringSize(font, "1", Vector2.One).X;

						if (stringLength + usedWidth > (float)maxWidth) {
							int num2 = maxWidth - (int)usedWidth;
							if (usedWidth > 0f) {
								num2 -= 16;
							}
							float toFill = num2;
							bool filled = false;
							int successfulIndex = -1;
							int index = 0;
							while (index < currentLine[l].Text.Length && !filled) {
								if (currentLine[l].Text[index] == ' ' || isChinese(currentLine[l].Text[index])) {
									if (ChatManager.GetStringSize(font, currentLine[l].Text.Substring(0, index), Vector2.One).X < toFill)
										successfulIndex = index;
									else {
										filled = true;
										//if (successfulIndex == 0)
										//	successfulIndex = index;
									}
								}
								index++;
							}
							if (currentLine[l].Text.Length == 0) {
								filled = true;
							}
							int num4 = successfulIndex;

							if (successfulIndex == -1) // last item is too big
							{
								if (l == 0) // 1st item in list, keep it and move on
								{
									//list2 = new List<TextSnippet>{currentLine[l]};
									list2 = new List<TextSnippet>();
									for (int m = l + 1; m < currentLine.Count; m++) {
										list2.Add(currentLine[m]);
									}
									finalList[k] = finalList[k].Take(/*l + */ 1).ToList<TextSnippet>(); // take 1
									finalList.Insert(k + 1, list2);
								}
								else // midway through list, keep previous and move this to next
								{
									list2 = new List<TextSnippet>();
									for (int m = l; m < currentLine.Count; m++) {
										list2.Add(currentLine[m]);
									}
									finalList[k] = finalList[k].Take(l).ToList<TextSnippet>(); // take previous ones
									finalList.Insert(k + 1, list2);
								}
							}
							else {
								string newText = currentLine[l].Text.Substring(0, num4);
								string newText2 = currentLine[l].Text.Substring(num4).TrimStart();
								list2 = new List<TextSnippet>
								{
									currentLine[l].CopyMorph(newText2)
								};
								for (int m = l + 1; m < currentLine.Count; m++) {
									list2.Add(currentLine[m]);
								}
								currentLine[l] = currentLine[l].CopyMorph(newText);
								finalList[k] = finalList[k].Take(l + 1).ToList<TextSnippet>();
								finalList.Insert(k + 1, list2);
							}
							break;
						}
						usedWidth += stringLength;
					}
				}
			}
			if (maxLines != -1) {
				while (finalList.Count > 10) {
					finalList.RemoveAt(10);
				}
			}
			return finalList;
		}

		public static bool isChinese(char a) {
			return a >= 0x4E00 && a <= 0x9FA5;
		}
	}
}
