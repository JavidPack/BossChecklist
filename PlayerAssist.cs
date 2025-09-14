using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class PlayerAssist : ModPlayer {
		public bool hasOpenedTheBossLog; // For the 'never opened' button glow for players who haven't noticed the new feature yet.
		public bool enteredWorldReset; // When players jon a different world, the boss log PageNum should reset back to its original state
		public List<ItemDefinition> BossItemsCollected;
		public bool IsItemResearched(int itemType) => Player.creativeTracker.ItemSacrifices.TryGetSacrificeNumbers(itemType, out int count, out int max) && count == max;

		public override void Initialize() {
			hasOpenedTheBossLog = false;
			enteredWorldReset = false;
			BossItemsCollected = new List<ItemDefinition>();
		}

		public override void SaveData(TagCompound tag) {
			tag["BossLogPrompt"] = hasOpenedTheBossLog;
			tag["BossLootObtained"] = BossItemsCollected;
		}

		public override void LoadData(TagCompound tag) {
			hasOpenedTheBossLog = tag.GetBool("BossLogPrompt"); // saved state of the unopened boss log prompt
			BossItemsCollected = tag.GetList<ItemDefinition>("BossLootObtained").ToList(); // Prepare the collectibles for the player.
		}

		public override void OnEnterWorld() {
			// PageNum starts out with an invalid number so jumping between worlds will always reset the BossLog when toggled
			enteredWorldReset = true;
		}
		
		// Respawn timer feature
		public override void UpdateDead() {
			if (Main.netMode == NetmodeID.Server || Player.whoAmI == 255 || Player.whoAmI != Main.myPlayer)
				return;

			// Timer sounds when a player is about to respawn
			if (BossChecklist.FeatureConfig.TimerSounds && Player.respawnTimer > 0 && Player.respawnTimer <= 180 && Player.respawnTimer % 60 == 0)
				SoundEngine.PlaySound(SoundID.MaxMana);
		}

		// Adds items that are picked up to the collected boss loot list
		public override bool OnPickup(Item item) {
			if (Main.netMode == NetmodeID.Server || Player.whoAmI == 255)
				return base.OnPickup(item);

			// Only add the item to the list if it is not already present
			if (BossChecklist.bossTracker.EntryLootCache[item.type] && !BossItemsCollected.Any(x => x.Type == item.type))
				BossItemsCollected.Add(new ItemDefinition(item.type));

			return base.OnPickup(item);
		}
	}
}
