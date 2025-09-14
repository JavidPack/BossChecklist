using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist.Systems
{
	public class DownedSystem : ModSystem {
		// Moon events are not tracked when defeated in vanilla. BossChecklist adds these checks.
		public static bool downedBloodMoon = false;
		public static bool downedFrostMoon = false;
		public static bool downedPumpkinMoon = false;
		public static bool downedSolarEclipse = false;

		// These mini-bosses are not tracked when defeated in vanilla. BossChecklist adds these checks.
		public static bool downedDarkMage = false;
		public static bool downedOgre = false;
		public static bool downedFlyingDutchman = false;
		public static bool downedMartianSaucer = false;

		public const string LangChat = "Mods.BossChecklist.ChatMessages"; // used in multiple files for custom defeated and despawn messages

		public override void ClearWorld() {
			downedBloodMoon = downedFrostMoon = downedPumpkinMoon = downedSolarEclipse = false; // clear moon downs
			downedDarkMage = downedOgre = downedFlyingDutchman = downedMartianSaucer = false; // clear mini-boss downs
		}

		public override void SaveWorldData(TagCompound tag) {
			var downed = new List<string>();
			if (downedBloodMoon)
				downed.Add("bloodmoon");
			if (downedFrostMoon)
				downed.Add("frostmoon");
			if (downedPumpkinMoon)
				downed.Add("pumpkinmoon");
			if (downedSolarEclipse)
				downed.Add("solareclipse");
			if (downedDarkMage)
				downed.Add("darkmage");
			if (downedOgre)
				downed.Add("ogre");
			if (downedFlyingDutchman)
				downed.Add("flyingdutchman");
			if (downedMartianSaucer)
				downed.Add("martiansaucer");

			tag["downed"] = downed;
		}

		public override void LoadWorldData(TagCompound tag) {
			var downed = tag.GetList<string>("downed");
			downedBloodMoon = downed.Contains("bloodmoon");
			downedFrostMoon = downed.Contains("frostmoon");
			downedPumpkinMoon = downed.Contains("pumpkinmoon");
			downedSolarEclipse = downed.Contains("solareclipse");
			downedDarkMage = downed.Contains("darkmage");
			downedOgre = downed.Contains("ogre");
			downedFlyingDutchman = downed.Contains("flyingdutchman");
			downedMartianSaucer = downed.Contains("martiansaucer");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.WriteFlags(downedBloodMoon, downedFrostMoon, downedPumpkinMoon, downedSolarEclipse, downedDarkMage, downedOgre, downedFlyingDutchman, downedMartianSaucer);
		}

		public override void NetReceive(BinaryReader reader) {
			reader.ReadFlags(out downedBloodMoon, out downedFrostMoon, out downedPumpkinMoon, out downedSolarEclipse, out downedDarkMage, out downedOgre, out downedFlyingDutchman, out downedMartianSaucer);
		}

		// Two on methods are added to track when moon events have ended to mark them as 'defeated'
		public override void Load() {
			On_Main.UpdateTime_StartDay += OnStartDay_CheckMoonEvents;
			On_Main.UpdateTime_StartNight += OnStartNight_CheckEclipseDown;
		}

		/// <summary>
		/// Before varibles are change for day (dawn), check for any moon events and mark as defeated it so.
		/// </summary>
		internal static void OnStartDay_CheckMoonEvents(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
			if (Main.bloodMoon) {
				AnnounceMoonEventEnd("BloodMoon"); // Sends a message to all players that the moon event has ended
				NPC.SetEventFlagCleared(ref downedBloodMoon, -1);
			}
			if (Main.snowMoon) {
				AnnounceMoonEventEnd("FrostMoon");
				NPC.SetEventFlagCleared(ref downedFrostMoon, -1);
			}
			if (Main.pumpkinMoon) {
				AnnounceMoonEventEnd("PumpkinMoon");
				NPC.SetEventFlagCleared(ref downedPumpkinMoon, -1);
			}
			orig(ref stopEvents);
		}

		/// <summary>
		/// Before varibles are change for night (dusk), check for the eclipse event and mark as defeated it so.
		/// </summary>
		internal static void OnStartNight_CheckEclipseDown(On_Main.orig_UpdateTime_StartNight orig, ref bool stopEvents) {
			if (Main.eclipse) {
				AnnounceMoonEventEnd("Eclipse");
				NPC.SetEventFlagCleared(ref downedSolarEclipse, -1);
			}
			orig(ref stopEvents); // Original method turns off any moon states
		}

		public static void AnnounceMoonEventEnd(string eventType) {
			if (Main.netMode is NetmodeID.Server) {
				// OnStartDay and OnStartNight are not called for multiplayer clients. The server must tell each client to trigger this method.
				// Note: Moon messages are client based, so clients will need to read their own configs to determine the message output.
				foreach (Player player in Main.ActivePlayers) {
					ModPacket packet = BossChecklist.instance.GetPacket();
					packet.Write((byte)PacketMessageType.SendClientConfigMessage);
					packet.Write((byte)ClientMessageType.Moon);
					packet.Write(eventType);
					packet.Send(player.whoAmI); // Server --> Multiplayer client
				}
			}
			else {
				string announcementType = "";
				if (BossChecklist.FeatureConfig.MoonMessages == FeatureConfiguration.MessageType.Generic) {
					string eventTypeLocal = Language.Exists($"Bestiary_Events.{eventType}") ? Language.GetTextValue($"Bestiary_Events.{eventType}") : Language.GetTextValue($"Bestiary_Invasions.{eventType}");
					announcementType = Language.GetText($"{LangChat}.EventEnd.Generic").Format(eventType == "Eclipse" ? eventTypeLocal.ToLower() : eventTypeLocal);
				}
				else if (BossChecklist.FeatureConfig.MoonMessages == FeatureConfiguration.MessageType.Unique) {
					announcementType = Language.GetTextValue($"{LangChat}.EventEnd.{eventType}");
				}

				if (!string.IsNullOrEmpty(announcementType))
					Main.NewText(announcementType, new Color(50, 255, 130));
			}
		}
	}
}
