using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BossChecklist
{
	enum PacketMessageType : byte {
		RequestHideBoss,
		RequestClearHidden,
		RequestMarkedDownEntry,
		RequestClearMarkedDowns,
		SendDespawnMessage,
		SendLimbMessage,
		SendPersonalBestRecordsToServer,
		UpdateRecordsFromServerToPlayer,
		RequestWorldRecords,
		SendWorldRecordsFromServerToPlayer,
		UpdateWorldRecordsToAllPlayers,
		ResetPlayerRecordForServer,
		ResetTrackers
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
	}
}
