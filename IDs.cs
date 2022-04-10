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
		WorldRecordUpdate
	}

	[Flags]
	internal enum RecordID : int
	{
		None = 0,
		ShortestFightTime = 1,
		LeastHits = 2,
		ResetAll = 4
	}
}
