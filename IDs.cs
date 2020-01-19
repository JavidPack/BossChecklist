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
		MusicBox
	}

	enum PacketMessageType : byte
	{
		RequestHideBoss,
		RequestClearHidden,
		SendRecordsToServer,
		RecordUpdate,
		WorldRecordUpdate
	}

	[Flags]
	internal enum RecordID : int
	{
		None = 0,
		ShortestFightTime = 1,
		LeastHits = 2,
		DodgeTime = 4,
		BestBrink = 8,
		ResetAll = 16
	}
}
