using System;

namespace BossChecklist
{
	internal enum EntryType
	{
		Boss,
		MiniBoss,
		Event
	}

	internal enum OrphanType
	{
		Loot,
		Collection,
		SpawnItem,
		EventNPC
	}

	internal enum CollectionType
	{
		Generic,
		Trophy,
		Mask,
		MusicBox,
		Relic,
		Pet,
		Mount
	}

	internal enum CategoryPage
	{
		Record,
		Spawn,
		Loot
	}

	enum PacketMessageType : byte
	{
		RequestHideBoss,
		RequestClearHidden,
		SendRecordsToServer,
		RecordUpdate,
		WorldRecordUpdate,
		ResetTrackers
	}

	[Flags]
	internal enum NetRecordID : int
	{
		None = 0,
		Duration_Best = 1,
		HitsTaken_Best = 2,
		Duration_First = 4,
		HitsTaken_First = 8,
		ResetAll = 4
	}

	internal enum RecordCategory : int
	{
		PreviousAttempt,
		BestRecord,
		FirstRecord,
		WorldRecord,
		None
	}
}
