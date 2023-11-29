using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
	}
}
