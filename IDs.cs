using System;

namespace BossChecklist
{
	internal enum CollectibleType {
		Generic,
		Trophy,
		Mask,
		Music,
		Relic,
		Pet,
		Mount
	}

	internal enum SubPage {
		Records,
		SpawnInfo,
		LootAndCollectibles
	}

	internal enum SubCategory {
		PreviousAttempt,
		PersonalBest,
		FirstVictory,
		WorldRecord,
		None
	}
}
