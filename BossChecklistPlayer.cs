using BossChecklist.UI;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BossChecklist
{
	class BossChecklistPlayer : ModPlayer
	{
		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (BossChecklist.ToggleChecklistHotKey.JustPressed) {
				if (!BossChecklistUI.Visible) {
					BossChecklist.instance.bossChecklistUI.UpdateCheckboxes();
				}
				BossChecklistUI.Visible = !BossChecklistUI.Visible;
			}
		}

		public override void OnEnterWorld(Player player) {
			BossChecklistUI.Visible = false;
		}
	}
}
