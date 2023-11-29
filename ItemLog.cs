using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	internal class ItemLog : GlobalItem {
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (BossUISystem.Instance.BossLog.BossLogVisible) {
				if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected.Any(x => x.Type == item.type)) {
					var line = new TooltipLine(Mod, "BossLog_Obtained", "✓ Obtained!") {
						OverrideColor = Colors.RarityYellow
					};
					tooltips.Add(line);
				}

				if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().IsItemResearched(item.type)) {
					var line = new TooltipLine(Mod, "BossLog_Researched", "✓ Researched!") {
						OverrideColor = Colors.RarityYellow
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
