using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	// TODO: Any way to migrate data from BossAssist save?
	// TODO: Implement "undo" records feature for boss log.
	public class BossRecord : TagSerializable
	{
		internal string bossName;
		internal string modName;
		internal BossStats stat = new BossStats();

		public static Func<TagCompound, BossRecord> DESERIALIZER = tag => new BossRecord(tag);

		private BossRecord(TagCompound tag) {
			modName = tag.Get<string>(nameof(modName));
			bossName = tag.Get<string>(nameof(bossName));
			stat = tag.Get<BossStats>(nameof(stat));
		}

		public BossRecord(string mod, string boss) {
			modName = mod;
			bossName = boss;
		}

		public TagCompound SerializeData() {
			return new TagCompound
			{
				{ nameof(bossName), bossName },
				{ nameof(modName), modName },
				{ nameof(stat), stat }
			};
		}
	}

	public class BossStats : TagSerializable
	{
		public int kills; // How many times the player has killed the boss
		public int deaths; // How many times the player has died during a participated boss fight

		public int durationBest = -1; // Quickest time a player has defeated a boss
		public int durationWorst = -1; // Slowest time a player has defeated a boss

		public int durationLast = -1;

		public int dodgeTimeBest = -1; // Most time that a player has not been damaged during a boss fight
		public int hitsTakenBest = -1; // Least amount of times the player has been damaged
		public int hitsTakenWorst = -1; // Most amount of times the player has been damaged

		public int dodgeTimeLast = -1;
		public int hitsTakenLast = -1;

		public int healthLossBest = -1; // "Highest" lowest amount of health a player has had during a boss fight
		public int healthLossBestPercent = -1; // The above stat in %
		public int healthLossWorst = -1; // Least amount of health a player has had during a boss fight
		public int healthLossWorstPercent = -1; // The above stat in %

		public int healthLossLast = -1;
		public int healthLossLastPercent = -1;

		public static Func<TagCompound, BossStats> DESERIALIZER = tag => new BossStats(tag);

		public BossStats() { }
		private BossStats(TagCompound tag) {
			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			durationBest = tag.Get<int>(nameof(durationBest));
			durationWorst = tag.Get<int>(nameof(durationWorst));
			dodgeTimeBest = tag.Get<int>(nameof(dodgeTimeBest));
			hitsTakenBest = tag.Get<int>(nameof(hitsTakenBest));
			hitsTakenWorst = tag.Get<int>(nameof(hitsTakenWorst));
			healthLossWorst = tag.Get<int>(nameof(healthLossWorst));
			healthLossWorstPercent = tag.Get<int>(nameof(healthLossWorstPercent));
			healthLossBest = tag.Get<int>(nameof(healthLossBest));
			healthLossBestPercent = tag.Get<int>(nameof(healthLossBestPercent));
		}

		public TagCompound SerializeData() {
			return new TagCompound
			{
				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(durationBest), durationBest },
				{ nameof(durationWorst), durationWorst },
				{ nameof(dodgeTimeBest), dodgeTimeBest },
				{ nameof(hitsTakenBest), hitsTakenBest },
				{ nameof(hitsTakenWorst), hitsTakenWorst },
				{ nameof(healthLossWorst), healthLossWorst },
				{ nameof(healthLossWorstPercent), healthLossWorstPercent },
				{ nameof(healthLossBest), healthLossBest },
				{ nameof(healthLossBestPercent), healthLossBestPercent }
			};
		}

		internal void NetRecieve(BinaryReader reader) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();

			//RecordID.Kills will just be increased by 1 automatically
			kills++;

			if (brokenRecords.HasFlag(RecordID.ShortestFightTime)) {
				durationBest = reader.ReadInt32();
				Main.NewText("New Record for Quickest Fight!");
			}
			if (brokenRecords.HasFlag(RecordID.LongestFightTime)) durationWorst = reader.ReadInt32();
			durationLast = reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.BestBrink)) {
				healthLossBest = reader.ReadInt32();
				healthLossBestPercent = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(RecordID.WorstBrink)) {
				healthLossWorst = reader.ReadInt32();
				healthLossWorstPercent = reader.ReadInt32();
			}
			healthLossLast = reader.ReadInt32();
			healthLossLastPercent = reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.LeastHits)) hitsTakenBest = reader.ReadInt32();
			if (brokenRecords.HasFlag(RecordID.MostHits)) hitsTakenWorst = reader.ReadInt32();
			hitsTakenLast = reader.ReadInt32();
			if (brokenRecords.HasFlag(RecordID.DodgeTime)) dodgeTimeBest = reader.ReadInt32();
			dodgeTimeLast = reader.ReadInt32();
		}

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord);
			// Kills update by 1 automatically
			// Deaths have to be sent elsewhere (NPCLoot wont run if the player dies)

			if (specificRecord.HasFlag(RecordID.ShortestFightTime)) writer.Write(durationLast);
			if (specificRecord.HasFlag(RecordID.LongestFightTime)) writer.Write(durationLast);
			writer.Write(durationLast);

			if (specificRecord.HasFlag(RecordID.BestBrink)) {
				writer.Write(healthLossLast);
				writer.Write(healthLossLastPercent);
			}
			if (specificRecord.HasFlag(RecordID.WorstBrink)) {
				writer.Write(healthLossLast);
				writer.Write(healthLossLastPercent);
			}
			writer.Write(healthLossLast);
			writer.Write(healthLossLastPercent);

			if (specificRecord.HasFlag(RecordID.LeastHits)) writer.Write(hitsTakenLast);
			if (specificRecord.HasFlag(RecordID.MostHits)) writer.Write(hitsTakenLast);
			writer.Write(hitsTakenLast);
			if (specificRecord.HasFlag(RecordID.DodgeTime)) writer.Write(dodgeTimeLast);
			writer.Write(dodgeTimeLast);
		}
	}

	public class BossCollection : TagSerializable
	{
		internal string modName;
		internal string bossName;

		// TODO: Use ItemDefinition
		internal List<Item> loot;
		internal List<Item> collectibles;

		public static Func<TagCompound, BossCollection> DESERIALIZER = tag => new BossCollection(tag);

		private BossCollection(TagCompound tag) {
			modName = tag.Get<string>(nameof(modName));
			bossName = tag.Get<string>(nameof(bossName));
			loot = tag.Get<List<Item>>(nameof(loot));
			collectibles = tag.Get<List<Item>>(nameof(collectibles));
		}

		public BossCollection(string mod, string boss) {
			modName = mod;
			bossName = boss;
		}

		public TagCompound SerializeData() {
			return new TagCompound
			{
				{ nameof(modName), modName },
				{ nameof(bossName), bossName },
				{ nameof(loot), loot },
				{ nameof(collectibles), collectibles },
			};
		}
	}
}