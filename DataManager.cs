using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	// TODO: Any way to migrate data from BossAssist save?
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

		public int fightTime = -1; // Quickest time a player has defeated a boss
		public int fightTime2 = -1; // Slowest time a player has defeated a boss

		public int fightTimeL = -1;

		public int dodgeTime = -1; // Most time that a player has not been damaged during a boss fight
		public int totalDodges = -1; // Least amount of times the player has been damaged
		public int totalDodges2 = -1; // Most amount of times the player has been damaged

		public int dodgeTimeL = -1;
		public int totalDodgesL = -1;

		public int brink = -1; // Least amount of health a player has had during a boss fight
		public int brinkPercent = -1; // The above stat in %
		public int brink2 = -1; // "Highest" lowest amount of health a player has had during a boss fight
		public int brinkPercent2 = -1; // The above stat in %

		public int brinkL = -1;
		public int brinkPercentL = -1;

		public static Func<TagCompound, BossStats> DESERIALIZER = tag => new BossStats(tag);

		public BossStats() { }
		private BossStats(TagCompound tag) {
			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			fightTime = tag.Get<int>(nameof(fightTime));
			fightTime2 = tag.Get<int>(nameof(fightTime2));
			dodgeTime = tag.Get<int>(nameof(dodgeTime));
			totalDodges = tag.Get<int>(nameof(totalDodges));
			totalDodges2 = tag.Get<int>(nameof(totalDodges2));
			brink = tag.Get<int>(nameof(brink));
			brinkPercent = tag.Get<int>(nameof(brinkPercent));
			brink2 = tag.Get<int>(nameof(brink2));
			brinkPercent2 = tag.Get<int>(nameof(brinkPercent2));
		}

		public TagCompound SerializeData() {
			return new TagCompound
			{
				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(fightTime), fightTime },
				{ nameof(fightTime2), fightTime2 },
				{ nameof(dodgeTime), dodgeTime },
				{ nameof(totalDodges), totalDodges },
				{ nameof(totalDodges2), totalDodges2 },
				{ nameof(brink), brink },
				{ nameof(brinkPercent), brinkPercent },
				{ nameof(brink2), brink2 },
				{ nameof(brinkPercent2), brinkPercent2 }
			};
		}

		internal void NetRecieve(BinaryReader reader) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();

			//RecordID.Kills will just be increased by 1 automatically
			kills++;

			if (brokenRecords.HasFlag(RecordID.ShortestFightTime)) {
				fightTime = reader.ReadInt32();
				Main.NewText("New Record for Quickest Fight!");
			}
			if (brokenRecords.HasFlag(RecordID.LongestFightTime)) fightTime2 = reader.ReadInt32();
			fightTimeL = reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.BestBrink)) {
				brink2 = reader.ReadInt32();
				brinkPercent2 = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(RecordID.WorstBrink)) {
				brink = reader.ReadInt32();
				brinkPercent = reader.ReadInt32();
			}
			brinkL = reader.ReadInt32();
			brinkPercentL = reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.LeastHits)) totalDodges = reader.ReadInt32();
			if (brokenRecords.HasFlag(RecordID.MostHits)) totalDodges2 = reader.ReadInt32();
			totalDodgesL = reader.ReadInt32();
			if (brokenRecords.HasFlag(RecordID.DodgeTime)) dodgeTime = reader.ReadInt32();
			dodgeTimeL = reader.ReadInt32();
		}

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord);
			// Kills update by 1 automatically
			// Deaths have to be sent elsewhere (NPCLoot wont run if the player dies)

			if (specificRecord.HasFlag(RecordID.ShortestFightTime)) writer.Write(fightTimeL);
			if (specificRecord.HasFlag(RecordID.LongestFightTime)) writer.Write(fightTimeL);
			writer.Write(fightTimeL);

			if (specificRecord.HasFlag(RecordID.BestBrink)) {
				writer.Write(brinkL);
				writer.Write(brinkPercentL);
			}
			if (specificRecord.HasFlag(RecordID.WorstBrink)) {
				writer.Write(brinkL);
				writer.Write(brinkPercentL);
			}
			writer.Write(brinkL);
			writer.Write(brinkPercentL);

			if (specificRecord.HasFlag(RecordID.LeastHits)) writer.Write(totalDodgesL);
			if (specificRecord.HasFlag(RecordID.MostHits)) writer.Write(totalDodgesL);
			writer.Write(totalDodgesL);
			if (specificRecord.HasFlag(RecordID.DodgeTime)) writer.Write(dodgeTimeL);
			writer.Write(dodgeTimeL);
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