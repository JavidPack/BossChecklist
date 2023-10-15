using System.Collections.Concurrent;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BossChecklist
{
	class ItemAssist : GlobalItem {
		public override bool OnPickup(Item item, Player player) {
			if (Main.netMode != NetmodeID.Server && Main.myPlayer == player.whoAmI && BossChecklist.bossTracker.EntryLootCache[item.type]) {
				// Add the item to the list if it is not already present
				ConcurrentDictionary<ItemDefinition, object> itemsList = player.GetModPlayer<PlayerAssist>().BossItemsCollected;
				itemsList.TryAdd(new ItemDefinition(item.type), null);
			}
			return base.OnPickup(item, player);
		}

		public override void OnCreated(Item item, ItemCreationContext context) {
			if (Main.netMode != NetmodeID.Server && BossChecklist.bossTracker.EntryLootCache[item.type]) {
				ConcurrentDictionary<ItemDefinition, object> itemsList = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossItemsCollected;
				itemsList.TryAdd(new ItemDefinition(item.type), null);
			}
		}
	}
}
