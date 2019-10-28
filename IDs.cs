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
		LongestFightTime = 8,
		DodgeTime = 16,
		MostHits = 32,
		LeastHits = 64,
		BestBrink = 128,
		BestBrinkPercent = 256,
		WorstBrink = 512,
		WorstBrinkPercent = 1024,

		LastFightTime = 2048,
		LastDodgeTime = 4096,
		LastHits = 8192,
		LastBrink = 16384,
		LastBrinkPercent = 32768,
		ResetAll = 65536
	}
}
