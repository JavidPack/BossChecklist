using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	/// <summary>
	/// Vanilla doesn't sync these values, so I will.
	/// </summary>
	class BossChecklistWorld : ModWorld
	{
		public static HashSet<string> HiddenBosses = new HashSet<string>();
		public override void Initialize() {
			HiddenBosses.Clear();
		}

		public override void Load(TagCompound tag) {
			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenBosses.Add(bossKey);
			}
		}

		public override TagCompound Save() {
			var HiddenBossesList = new List<string>(HiddenBosses);
			return new TagCompound {
				{"HiddenBossesList", HiddenBossesList}
			};
		}

		public override void NetSend(BinaryWriter writer) {
			BitsByte flags = new BitsByte();
			flags[0] = NPC.downedTowerSolar;
			flags[1] = NPC.downedTowerVortex;
			flags[2] = NPC.downedTowerNebula;
			flags[3] = NPC.downedTowerStardust;
			writer.Write(flags);

			writer.Write(HiddenBosses.Count);
			foreach (var bossKey in HiddenBosses) {
				writer.Write(bossKey);
			}
		}

		public override void NetReceive(BinaryReader reader) {
			BitsByte flags = reader.ReadByte();
			NPC.downedTowerSolar = flags[0];
			NPC.downedTowerVortex = flags[1];
			NPC.downedTowerNebula = flags[2];
			NPC.downedTowerStardust = flags[3];

			HiddenBosses.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				HiddenBosses.Add(reader.ReadString());
			}
			BossChecklist.instance.bossChecklistUI.UpdateCheckboxes();
			if (BossChecklist.BossLogConfig.HideUnavailable) BossChecklist.instance.BossLog.UpdateTableofContents();
		}
	}
}
