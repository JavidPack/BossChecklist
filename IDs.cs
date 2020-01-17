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
	}

	[Flags]
	internal enum RecordID : int
	{
		None = 0,
		Kills = 1,
		Deaths = 2,
		ShortestFightTime = 4,
		PreviousFightTime = 8,
		LeastHits = 16,
		PreviousHits = 32,
		DodgeTime = 64,
		BestBrink = 128,
		PreviousBrink = 512,
		ResetAll = 1024
	}
}
