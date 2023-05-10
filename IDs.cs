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
		Music,
		Relic,
		Pet,
		Mount
	}

	[Flags]
	internal enum NetRecordID : int
	{
		None = 0,
		Duration_Best = 1,
		HitsTaken_Best = 2,
		FirstRecord = 4,
		ResetAll = 8
	}

	internal enum SubPage
	{
		Records,
		SpawnInfo,
		LootAndCollectibles
	}

	internal enum SubCategory
	{
		PreviousAttempt,
		PersonalBest,
		FirstVictory,
		WorldRecord,
		None
	}
}
