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
					int BossIndex = modplayer.BossTrophies.FindIndex(boss => boss.bossKey == BossList[i].Key);
					if (BossIndex == -1) continue;
					// Loot Collections
					if (BossList[i].loot.Contains(item.type)) {
						if (modplayer.BossTrophies[i].loot.All(x => x.Type != item.type)) {
							modplayer.BossTrophies[i].loot.Add(new ItemDefinition(item.type));
						}
					}
					// Boss Collections
					if (BossList[i].collection.Contains(item.type)) {
						if (modplayer.BossTrophies[i].collectibles.All(x => x.Type != item.type)) {
							modplayer.BossTrophies[i].collectibles.Add(new ItemDefinition(item.type));
						}
					}
				}
			}
			return base.OnPickup(item, player);
		}

		public override void OnCraft(Item item, Recipe recipe) {
			if (Main.netMode != NetmodeID.Server) {
				Player player = Main.LocalPlayer;
				// Loot and Collections Updating
				List<BossInfo> BossList = BossChecklist.bossTracker.SortedBosses;
				PlayerAssist modplayer = player.GetModPlayer<PlayerAssist>();
				for (int i = 0; i < BossList.Count; i++) {
					int BossIndex = modplayer.BossTrophies.FindIndex(boss => boss.bossKey == BossList[i].Key);
					if (BossIndex == -1) continue;
					// Loot Collections
					if (BossList[i].loot.Contains(item.type)) {
						if (modplayer.BossTrophies[i].loot.All(x => x.Type != item.type)) {
							modplayer.BossTrophies[i].loot.Add(new ItemDefinition(item.type));
						}
					}
					// Boss Collections
					if (BossList[i].collection.Contains(item.type)) {
						if (modplayer.BossTrophies[i].collectibles.All(x => x.Type != item.type)) {
							modplayer.BossTrophies[i].collectibles.Add(new ItemDefinition(item.type));
						}
					}
				}
			}
		}
	}
}
