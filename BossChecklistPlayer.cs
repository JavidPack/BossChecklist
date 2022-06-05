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
					BossUISystem.Instance.bossChecklistUI.UpdateCheckboxes();
				}
				BossChecklistUI.Visible = !BossChecklistUI.Visible;
			}
			if (BossChecklist.ToggleBossLog.JustPressed) {
				BossLogUI.PendingToggleBossLogUI = true;
				//BossUISystem.Instance.BossLog.ToggleBossLog(!BossUISystem.Instance.BossLog.BossLogVisible);

				// Debug assistance, allows for reinitializing BossLog in-game
				//BossChecklist.instance.BossLog.RemoveAllChildren();
				//var isInitializedFieldInfo = typeof(Terraria.UI.UIElement).GetField("_isInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				//isInitializedFieldInfo.SetValue(BossChecklist.instance.BossLog, false);
				//BossChecklist.instance.BossLog.Activate();
			}
		}

		public override void SetControls() {
			if (BossUISystem.Instance.BossLog.BossLogVisible) {
				if (Main.LocalPlayer.controlInv) {
					BossUISystem.Instance.BossLog.ToggleBossLog(false);
					Main.LocalPlayer.releaseInventory = false;
				}
				else if (Main.LocalPlayer.controlCreativeMenu) {
					BossUISystem.Instance.BossLog.ToggleBossLog(false);
					Main.LocalPlayer.releaseCreativeMenu = false;
				}
			}
		}

		public override void PostUpdate() {
			if (Main.LocalPlayer.dead && BossUISystem.Instance.BossLog.BossLogVisible)
				BossUISystem.Instance.BossLog.ToggleBossLog(false);
		}

		public override void OnEnterWorld(Player player) {
			BossChecklistUI.Visible = false;
			BossUISystem.Instance.BossLog.ToggleBossLog(false);
		}
	}
}
