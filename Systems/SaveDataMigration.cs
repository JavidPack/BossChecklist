using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist.Systems
{
	public class WorldAssist : ModSystem {
		public override void LoadWorldData(TagCompound tag) {
			if (tag.TryGet("World_Record_Data", out TagCompound _))
				ModContent.GetInstance<RecordSystem>().LoadWorldData(tag); // ensure that old world record data is loaded on the new RecordSystem

			if (tag.TryGet("downed", out List<string> _))
				ModContent.GetInstance<DownedSystem>().LoadWorldData(tag); // ensure that old downed statuses are loaded on the new DownedSystem

			// Entries that are toggled as downed or hidden will not transfer. Users can easily toggle them again if they choose.
		}

		public override void SaveWorldData(TagCompound tag) { } // No longer using this ModSystem for saving data
	}

	public class PlayerAssist : ModPlayer {
		public override void LoadData(TagCompound tag) {
			if (tag.TryGet("Record_Data", out TagCompound _) || tag.TryGet("MiniBossKillCount", out TagCompound _))
				Player.GetModPlayer<RecordModPlayer>().LoadData(tag); // ensure that old record data is loaded on the new RecordModPlayer

			if (tag.TryGet("BossLootObtained", out List<ItemDefinition> _))
				Player.GetModPlayer<BossLogModPlayer>().LoadData(tag); // ensure that old loot checklist data is loaded on the new BossLogModPlayer (should also cover the prompt check)
		}

		public override void SaveData(TagCompound tag) { } // No longer using this ModPlayer for saving data
	}
}
