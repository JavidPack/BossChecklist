using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	class ItemAssist : GlobalItem
	{
		public override bool OnPickup(Item item, Player player) {
			if (Main.netMode != NetmodeID.Server && Main.myPlayer == player.whoAmI && BossChecklist.bossTracker.EntryLootCache[item.type]) {
				// Add the item to the list if it is not already present
				List<ItemDefinition> itemsList = player.GetModPlayer<PlayerAssist>().BossItemsCollected;
				if (!itemsList.Any(x => x.Type == item.type)) {
					itemsList.Add(new ItemDefinition(item.type));
				}
			}
			if (item.type == ItemID.TorchGodsFavor && !WorldAssist.downedTorchGod) {
				WorldAssist.downedTorchGod = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			return base.OnPickup(item, player);
		}

		public override void OnCreate(Item item, ItemCreationContext context) {
			if (Main.netMode != NetmodeID.Server && BossChecklist.bossTracker.EntryLootCache[item.type]) {
				List<ItemDefinition> itemsList = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
				if (!itemsList.Any(x => x.Type == item.type)) {
					itemsList.Add(new ItemDefinition(item.type));
				}
			}
		}

		public override bool? UseItem(Item item, Player player) {
			if (item.type == ItemID.TorchGodsFavor && !WorldAssist.downedTorchGod) {
				WorldAssist.downedTorchGod = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			return null;
		}
	}
}
