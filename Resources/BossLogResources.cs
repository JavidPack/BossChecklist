using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace BossChecklist.Resources
{
	internal class BossLogResources
	{
		public static Asset<Texture2D> Button_Book;
		public static Asset<Texture2D> Button_Border;
		public static Asset<Texture2D> Button_Color;
		public static Asset<Texture2D> Button_Faded;

		public static Asset<Texture2D> Log_BackPanel;
		public static Asset<Texture2D> Log_Paper;
		public static Asset<Texture2D> Log_Tab;
		public static Asset<Texture2D> Log_Tab2;
		public static Asset<Texture2D> FilterPanel;

		public static Asset<Texture2D> Nav_Prev;
		public static Asset<Texture2D> Nav_Next;
		public static Asset<Texture2D> Nav_SubPage;
		public static Asset<Texture2D> Nav_TableOfContents;
		public static Asset<Texture2D> Nav_Credits;
		public static Asset<Texture2D> Nav_Boss;
		public static Asset<Texture2D> Nav_MiniBoss;
		public static Asset<Texture2D> Nav_Event;
		public static Asset<Texture2D> Nav_Filter;
		public static Asset<Texture2D>[] Nav_Record_Category;

		public static Asset<Texture2D> Check_Box;
		public static Asset<Texture2D> Check_Check;
		public static Asset<Texture2D> Check_X;
		public static Asset<Texture2D> Check_Next;
		public static Asset<Texture2D> Check_Strike;
		public static Asset<Texture2D> Check_Chest;
		public static Asset<Texture2D> Check_GoldChest;

		public static Asset<Texture2D> Credit_DevSlot;
		public static Asset<Texture2D> Credit_ModSlot;
		public static Asset<Texture2D> Credit_Register;
		public static Asset<Texture2D> Credit_NoMods;
		public static Asset<Texture2D>[] Credit_Devs;

		public static Asset<Texture2D> Indicator_Interaction;
		public static Asset<Texture2D> Indicator_OnlyBosses;
		public static Asset<Texture2D> Indicator_Manual;
		public static Asset<Texture2D> Indicator_Progression;

		public static Asset<Texture2D> Content_RecordSlot;
		public static Asset<Texture2D> Content_PromptSlot;
		public static Asset<Texture2D> Content_Cycle;
		public static Asset<Texture2D> Content_ToggleHidden;
		public static Asset<Texture2D> Content_BossKey;
		public static Asset<Texture2D> Content_ProgressiveOn;
		public static Asset<Texture2D> Content_ProgressiveOff;
		public static Asset<Texture2D>[] Content_CollectibleType;

		public static Dictionary<BossLogConfiguration.FilterType, Asset<Texture2D>> FilterToIcon;

		public static Asset<Texture2D> RequestResource(string path, bool immediate = false) => ModContent.Request<Texture2D>("BossChecklist/Resources/" + path, immediate ? AssetRequestMode.ImmediateLoad : AssetRequestMode.AsyncLoad);

		private static Asset<Texture2D> PreloadResource(string path) => RequestResource(path, true);

		public static Asset<Texture2D> RequestVanillaTexture(string path, bool immediate = false) => Main.Assets.Request<Texture2D>("Images/" + path, immediate ? AssetRequestMode.ImmediateLoad : AssetRequestMode.AsyncLoad);

		public static Asset<Texture2D> RequestItemTexture(int type) {
			if (!TextureAssets.Item[type].IsLoaded)
				Main.instance.LoadItem(type);

			return TextureAssets.Item[type];
		}

		public static void PreloadLogAssets() {
			Button_Book = PreloadResource("Book_Outline");
			Button_Border = PreloadResource("Book_Border");
			Button_Color = PreloadResource("Book_Color");
			Button_Faded = PreloadResource("Book_Faded");

			Log_BackPanel = PreloadResource("LogUI_Back");
			Log_Paper = PreloadResource("LogUI_Paper");
			Log_Tab = PreloadResource("LogUI_Tab");
			Log_Tab2 = PreloadResource("LogUI_InfoTab");
			FilterPanel = PreloadResource("LogUI_Filter");

			Nav_Prev = PreloadResource("Nav_Prev");
			Nav_Next = PreloadResource("Nav_Next");
			Nav_SubPage = PreloadResource("Nav_SubPage_Button");
			Nav_TableOfContents = PreloadResource("Nav_Contents");
			Nav_Credits = PreloadResource("Nav_Credits");
			Nav_Boss = PreloadResource("Nav_Boss");
			Nav_MiniBoss = PreloadResource("Nav_Miniboss");
			Nav_Event = PreloadResource("Nav_Event");
			Nav_Filter = PreloadResource("Nav_Filter");

			Nav_Record_Category = [
				PreloadResource("Nav_Record_PreviousAttempt"),
				PreloadResource("Nav_Record_FirstVictory"),
				PreloadResource("Nav_Record_PersonalBest"),
				PreloadResource("Nav_Record_WorldRecord"),
			];

			Check_Box = PreloadResource("Checks_Box");
			Check_Check = PreloadResource("Checks_Check");
			Check_X = PreloadResource("Checks_X");
			Check_Next = PreloadResource("Checks_Next");
			Check_Strike = PreloadResource("Checks_Strike");
			Check_Chest = PreloadResource("Checks_Chest");
			Check_GoldChest = PreloadResource("Checks_Chest_Gold");

			Credit_DevSlot = PreloadResource("Credits_Panel_Dev");
			Credit_ModSlot = PreloadResource("Credits_Panel_Mod");
			Credit_Register = PreloadResource("Credits_Panel_Register");
			Credit_NoMods = PreloadResource("Credits_Panel_NoMods");

			Credit_Devs = new Asset<Texture2D>[BossLogUI.BossChecklistModContributors.Keys.Count];
			for (int i = 0; i < BossLogUI.BossChecklistModContributors.Keys.Count; i++) {
				Credit_Devs[i] = PreloadResource("Credits_" + BossLogUI.BossChecklistModContributors.Keys.ToList()[i]);
			}

			Indicator_Interaction = PreloadResource("Indicator_Interaction");
			Indicator_OnlyBosses = PreloadResource("Indicator_OnlyBosses");
			Indicator_Manual = PreloadResource("Indicator_Manual");
			Indicator_Progression = PreloadResource("Indicator_Progression");

			Content_RecordSlot = PreloadResource("Extra_RecordSlot");
			Content_PromptSlot = PreloadResource("Extra_PromptSlot");
			Content_Cycle = PreloadResource("Extra_CycleRecipe");
			Content_ToggleHidden = PreloadResource("Nav_Hidden");
			Content_BossKey = PreloadResource("Extra_Key");
			Content_ProgressiveOn = PreloadResource($"Extra_ProgressiveOn");
			Content_ProgressiveOff = PreloadResource($"Extra_ProgressiveOff");

			Content_CollectibleType = [
				PreloadResource("Checks_Trophy"), // No texture specifically for Relic
				PreloadResource("Checks_Pet"),
				PreloadResource("Checks_Trophy"),
				PreloadResource("Checks_Mask"),
				PreloadResource("Checks_Music"),
				PreloadResource("Checks_Generic"),
			];

			FilterToIcon = new Dictionary<BossLogConfiguration.FilterType, Asset<Texture2D>>() {
				[BossLogConfiguration.FilterType.Show] = Check_Check,
				[BossLogConfiguration.FilterType.HideWhenCompleted] = Check_Next,
				[BossLogConfiguration.FilterType.Hide] = Check_X
			};
		}
	}
}
