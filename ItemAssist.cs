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
			if (Main.netMode != NetmodeID.Server && Main.myPlayer == player.whoAmI) {
				// Loot and Collections Updating
				List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
				PlayerAssist modplayer = player.GetModPlayer<PlayerAssist>();
				for (int i = 0; i < BossList.Count; i++) {
					if (!modplayer.BossItemsCollected.TryGetValue(BossList[i].Key, out List<ItemDefinition> items)) {
						continue;
					}
					if (items.Any(x => x.Type == item.type)) {
						continue;
					}
					items.Add(new ItemDefinition(item.type));
				}
			}
			if (item.type == ItemID.TorchGodsFavor) {
				WorldAssist.downedTorchGod = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			return base.OnPickup(item, player);
		}

		public override void OnCreate(Item item, ItemCreationContext context) {
			if (Main.netMode != NetmodeID.Server) {
				Player player = Main.LocalPlayer;
				// Loot and Collections Updating
				List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
				PlayerAssist modplayer = player.GetModPlayer<PlayerAssist>();
				for (int i = 0; i < BossList.Count; i++) {
					if (!modplayer.BossItemsCollected.TryGetValue(BossList[i].Key, out List<ItemDefinition> items)) {
						continue;
					}
					if (items.Any(x => x.Type == item.type)) {
						continue;
					}
					items.Add(new ItemDefinition(item.type));
				}
			}
		}

		public override bool? UseItem(Item item, Player player) {
			if (item.type == ItemID.TorchGodsFavor) {
				WorldAssist.downedTorchGod = true;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.WorldData);
				}
			}
			return null;
		}
	}
}
