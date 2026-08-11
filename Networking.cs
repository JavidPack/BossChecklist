using BossChecklist.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BossChecklist
{
	enum PacketMessageType : byte {
		RequestHideBoss,
		RequestClearHidden,
		RequestMarkedDownEntry,
		RequestClearMarkedDowns,
		SendClientConfigMessage,
		UpdateAllowTracking,
		SendPersonalBestRecordsToServer,
		UpdateRecordsFromServerToPlayer,
		RequestWorldRecords,
		SendWorldRecordsFromServerToPlayer,
		UpdateWorldRecordsToAllPlayers,
		ResetPlayerRecordForServer,
		ResetTrackers,
		RequestBossStateToggle, // Client --> Server
		SyncBossState,          // Server --> All Clients
	}

	internal enum ClientMessageType : byte {
		Despawn,
		Limb,
		Moon
	}

	internal class Networking {
		public static bool NewPersonalBest(NetRecordID netRecord) => netRecord.HasFlag(NetRecordID.PersonalBest_Duration) || netRecord.HasFlag(NetRecordID.PersonalBest_HitsTaken);
		public static bool NewWorldRecord(NetRecordID netRecord) => netRecord.HasFlag(NetRecordID.WorldRecord_Duration) || netRecord.HasFlag(NetRecordID.WorldRecord_HitsTaken);
		public static bool ResettingRecords(NetRecordID netRecord) => netRecord.HasFlag(NetRecordID.PersonalBest_Reset) || netRecord.HasFlag(NetRecordID.FirstVictory_Reset);

		/// <summary>
		/// Send a packet to the server to add, remove, or clear entries from the hidden entries list.
		/// </summary>
		/// <param name="Key">Provide an entry key to add/remove the entry. Leave blank to clear the entire hidden list.</param>
		public static void RequestHiddenEntryUpdate(string Key = null, bool hide = true) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write(string.IsNullOrEmpty(Key) ? (byte)PacketMessageType.RequestClearHidden : (byte)PacketMessageType.RequestHideBoss);
			if (!string.IsNullOrEmpty(Key)) {
				packet.Write(Key);
				packet.Write(hide);
			}
			packet.Send(); // Multiplayer --> Server
		}

		/// <summary>
		/// Send a packet to the server to add, remove, or clear entries from the marked entries list.
		/// </summary>
		/// <param name="Key">Provide an entry key to add/remove the entry. Leave blank to clear the entire marked list.</param>
		public static void RequestMarkedEntryUpdate(string Key = null, bool mark = true) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write(string.IsNullOrEmpty(Key) ? (byte)PacketMessageType.RequestClearMarkedDowns : (byte)PacketMessageType.RequestMarkedDownEntry);
			if (!string.IsNullOrEmpty(Key)) {
				packet.Write(Key);
				packet.Write(mark);
			}
			packet.Send(); // Multiplayer --> Server
		}


		internal static bool TryApplyBossStateToggle(string bossKey, bool downed) {

			EntryInfo entry = BossChecklist.bossTracker.FindEntryFromKey(bossKey);

			if (entry is null || entry.setDowned is null)
				return false; 

			entry.setDowned(downed);
			BossLogSystem.MarkedEntries.Remove(bossKey);

			return true; // if true means toggle success
		}

		public static void RequestBossStateToggle(string bossKey, bool downed) {
			if (Main.netMode == NetmodeID.SinglePlayer) {
				if(!TryApplyBossStateToggle(bossKey, downed)) {
					Main.NewText(Language.GetTextValue("Mods.BossChecklist.Configs.DebugTools.Failed", BossChecklist.bossTracker.FindEntryFromKey(bossKey).name, Color.Red));
				}
				return;
			}

			else if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			else {
				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.RequestBossStateToggle);
				packet.Write(bossKey);
				packet.Write(downed);
				packet.Send();
			}
		}
	}
}
