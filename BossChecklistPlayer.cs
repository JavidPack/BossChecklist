using BossChecklist.UIElements;
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
			if (BossChecklist.ToggleBossLog.JustPressed) { 
				BossChecklist.instance.BossLog.ToggleBossLog(!BossChecklist.instance.BossLog.BossLogVisible);

				// Debug assistance, allows for reinitializing BossLog in-game
				//BossChecklist.instance.BossLog.RemoveAllChildren();
				//var isInitializedFieldInfo = typeof(Terraria.UI.UIElement).GetField("_isInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				//isInitializedFieldInfo.SetValue(BossChecklist.instance.BossLog, false);
				//BossChecklist.instance.BossLog.Activate();
			}
		}

		public override void SetControls() {
			if (BossChecklist.instance.BossLog.BossLogVisible && Main.LocalPlayer.controlInv) {
				BossChecklist.instance.BossLog.ToggleBossLog(false);
				Main.LocalPlayer.releaseInventory = false;
			}
		}

		public override void PostUpdate() {
			if (Main.LocalPlayer.dead && BossChecklist.instance.BossLog.BossLogVisible)
				BossChecklist.instance.BossLog.ToggleBossLog(false);
		}

		public override void OnEnterWorld(Player player) {
			BossChecklistUI.Visible = false;
			BossChecklist.instance.BossLog.ToggleBossLog(false);
		}
	}
}
