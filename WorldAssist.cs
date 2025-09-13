using BossChecklist.Systems;
using BossChecklist.UIElements;
using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	public class WorldAssist : ModSystem {
		public static HashSet<string> HiddenEntries = new HashSet<string>();
		public static HashSet<string> MarkedEntries = new HashSet<string>();

		public override void ClearWorld() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
		}

		public override void OnWorldLoad() {
			HiddenEntries.Clear();
			MarkedEntries.Clear();
		}

		public override void SaveWorldData(TagCompound tag) {
			var HiddenBossesList = new List<string>(HiddenEntries);
			var MarkedAsDownedList = new List<string>(MarkedEntries);
			tag["HiddenBossesList"] = HiddenBossesList;
			tag["downed_Forced"] = MarkedAsDownedList;
		}

		public override void LoadWorldData(TagCompound tag) {
			var HiddenBossesList = tag.GetList<string>("HiddenBossesList");
			foreach (var bossKey in HiddenBossesList) {
				HiddenEntries.Add(bossKey);
			}

			var MarkedAsDownedList = tag.GetList<string>("downed_Forced");
			foreach (var bossKey in MarkedAsDownedList) {
				MarkedEntries.Add(bossKey);
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(HiddenEntries.Count);
			foreach (var bossKey in HiddenEntries) {
				writer.Write(bossKey);
			}

			writer.Write(MarkedEntries.Count);
			foreach (var bossKey in MarkedEntries) {
				writer.Write(bossKey);
			}
		}

		public override void NetReceive(BinaryReader reader) {
			HiddenEntries.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				HiddenEntries.Add(reader.ReadString());
			}

			MarkedEntries.Clear();
			count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				MarkedEntries.Add(reader.ReadString());
			}

			// Update checklist to match Hidden and Marked Downed entries
			if (BossChecklistUI.Visible)
				BossLogSystem.Instance.bossChecklistUI.UpdateCheckboxes();

			if (BossLogSystem.Instance.BossLog.BossLogVisible && BossLogSystem.Instance.BossLog.PageNum == -1) {
				BossLogSystem.Instance.BossLog.RefreshPageContent();
			}
		}
	}
}
