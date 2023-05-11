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
