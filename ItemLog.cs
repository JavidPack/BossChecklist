using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	internal class ItemLog : GlobalItem {
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (BossUISystem.Instance.BossLog.BossLogVisible) {
				if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected.Any(x => x.Type == item.type)) {
					var line = new TooltipLine(Mod, "BossLog_Obtained", "✓ " + Language.GetTextValue("Mods.BossChecklist.Log.LootAndCollection.Obtained")) {
						OverrideColor = Colors.RarityYellow
					};
					tooltips.Add(line);
				}

				// If in journey mode and the item can be researched, display if it is research or how many items left are needed
				if (Main.LocalPlayer.difficulty == PlayerDifficultyID.Creative && Main.LocalPlayerCreativeTracker.ItemSacrifices.TryGetSacrificeNumbers(item.type, out int count, out int max)) {
					bool isResearched = Main.LocalPlayer.GetModPlayer<PlayerAssist>().IsItemResearched(item.type);
					string text2 = isResearched ? "Mods.BossChecklist.Log.LootAndCollection.Researched" : "CommonItemTooltip.CreativeSacrificeNeeded";
					var line = new TooltipLine(Mod, "BossLog_Researched", (isResearched ? "✓ " : "") + Language.GetTextValue(text2, max - count)) {
						OverrideColor = isResearched ? Colors.RarityYellow : Colors.JourneyMode
					};
					tooltips.Add(line);
				}
			}
		}

		public override void OnCreated(Item item, ItemCreationContext context) {
			if (context is RecipeItemCreationContext || context is BuyItemCreationContext) {
				if (Main.netMode != NetmodeID.Server && BossChecklist.bossTracker.EntryLootCache[item.type]) {
					List<ItemDefinition> itemsList = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
					if (!itemsList.Any(x => x.Type == item.type))
						itemsList.Add(new ItemDefinition(item.type));
				}
			}
		}
	}
}
