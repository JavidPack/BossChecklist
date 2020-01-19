using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	// Migrating data from BossAssist is possible, but is vastly unecessary. Going to be doing a clean slate for merge.
	public class BossRecord : TagSerializable
	{
		internal string bossName;
		internal BossStats stat = new BossStats();

		public static Func<TagCompound, BossRecord> DESERIALIZER = tag => new BossRecord(tag);

		private BossRecord(TagCompound tag) {
			bossName = tag.Get<string>(nameof(bossName));
			stat = tag.Get<BossStats>(nameof(stat));
		}

		public BossRecord(string boss) {
			bossName = boss;
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(bossName), bossName },
				{ nameof(stat), stat }
			};
		}
	}

	public class WorldRecord : TagSerializable
	{
		internal string bossName;
		internal WorldStats stat = new WorldStats();

		public static Func<TagCompound, WorldRecord> DESERIALIZER = tag => new WorldRecord(tag);

		private WorldRecord(TagCompound tag) {
			bossName = tag.Get<string>(nameof(bossName));
			stat = tag.Get<WorldStats>(nameof(stat));
		}

		public WorldRecord(string boss) {
			bossName = boss;
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(bossName), bossName },
				{ nameof(stat), stat }
			};
		}
	}

	public class BossStats : TagSerializable
	{
		public int kills; // How many times the player has killed the boss
		public int deaths; // How many times the player has died during a participated boss fight

		// Best represents the best record
		// Prev represents the last attempt/previous best record for comparison

		// Fight Duration
		public int durationBest = -1;
		public int durationPrev = -1;

		// Total Hits / Dodge Timer (Less hits is better)
		public int hitsTakenBest = -1;
		public int hitsTakenPrev = -1;
		public int dodgeTimeBest = -1;
		public int dodgeTimePrev = -1;

		// Lowest Health Point while survivng (Lower is better, dying resets it!)
		// Having it high is an accomplishment too, but its already covered with Hits Taken, as in no hits means no health loss
		public int healthLossBest = -1;
		public int healthLossPrev = -1;
		public int healthAtStart = -1;
		public int healthAtStartPrev = -1;

		public static Func<TagCompound, BossStats> DESERIALIZER = tag => new BossStats(tag);

		public BossStats() { }
		private BossStats(TagCompound tag) {
			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			durationBest = tag.Get<int>(nameof(durationBest));
			durationPrev = tag.Get<int>(nameof(durationPrev));
			dodgeTimeBest = tag.Get<int>(nameof(dodgeTimeBest));
			hitsTakenBest = tag.Get<int>(nameof(hitsTakenBest));
			hitsTakenPrev = tag.Get<int>(nameof(hitsTakenPrev));
			healthLossPrev = tag.Get<int>(nameof(healthLossPrev));
			healthLossBest = tag.Get<int>(nameof(healthLossBest));
			healthAtStart = tag.Get<int>(nameof(healthAtStart));
			healthAtStartPrev = tag.Get<int>(nameof(healthAtStartPrev));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(durationBest), durationBest },
				{ nameof(durationPrev), durationPrev },
				{ nameof(dodgeTimeBest), dodgeTimeBest },
				{ nameof(hitsTakenBest), hitsTakenBest },
				{ nameof(hitsTakenPrev), hitsTakenPrev },
				{ nameof(healthLossPrev), healthLossPrev },
				{ nameof(healthLossBest), healthLossBest },
				{ nameof(healthAtStart), healthAtStart },
				{ nameof(healthAtStartPrev), healthAtStartPrev },
			};
		}

		internal void NetRecieve(BinaryReader reader, Player player, int boss) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();
			if (!brokenRecords.HasFlag(RecordID.ResetAll)) {
				// Determine if a new record was made (Prev records need to change still)
				bool newRecord = brokenRecords.HasFlag(RecordID.ShortestFightTime) || brokenRecords.HasFlag(RecordID.LeastHits) || brokenRecords.HasFlag(RecordID.BestBrink);

				kills++; // Kills always increase by 1, since records can only be made when a boss is defeated
				if (brokenRecords.HasFlag(RecordID.ShortestFightTime)) durationBest = reader.ReadInt32();
				durationPrev = reader.ReadInt32();
				if (brokenRecords.HasFlag(RecordID.LeastHits)) hitsTakenBest = reader.ReadInt32();
				hitsTakenPrev = reader.ReadInt32();
				if (brokenRecords.HasFlag(RecordID.DodgeTime)) dodgeTimeBest = reader.ReadInt32();
				if (brokenRecords.HasFlag(RecordID.BestBrink)) {
					healthLossBest = reader.ReadInt32();
					healthAtStart = reader.ReadInt32();
				}
				healthLossPrev = reader.ReadInt32();
				healthAtStartPrev = reader.ReadInt32();
				if (newRecord) {
					player.GetModPlayer<PlayerAssist>().hasNewRecord[boss] = true;
					CombatText.NewText(player.getRect(), Color.LightYellow, "New Record!", true);
				}
			}
			else { // If ResetAll was flagged, change all variables to their default
				kills = deaths = 0;
				durationBest = durationPrev = -1;
				hitsTakenBest = hitsTakenPrev = dodgeTimeBest = dodgeTimePrev = -1;
				healthLossBest = healthLossPrev = healthAtStart = healthAtStartPrev = -1;
			}
		}

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord); // We need this for NetRecieve as well

			if (!specificRecord.HasFlag(RecordID.ResetAll)) { // If ResetAll is flagged there is no need to write the rest
				// Prev records ALWAYS are written. They always update as either record attempts or old records
				if (specificRecord.HasFlag(RecordID.ShortestFightTime)) writer.Write(durationBest);
				writer.Write(durationPrev);
				if (specificRecord.HasFlag(RecordID.LeastHits)) writer.Write(hitsTakenBest);
				writer.Write(hitsTakenPrev);
				if (specificRecord.HasFlag(RecordID.DodgeTime)) writer.Write(dodgeTimePrev);
				if (specificRecord.HasFlag(RecordID.BestBrink)) {
					writer.Write(healthLossBest);
					writer.Write(healthAtStart);
				}
				writer.Write(healthLossPrev);
				writer.Write(healthAtStartPrev);
			}
		}
	}

	public class WorldStats : TagSerializable
	{
		public string durationHolder = "";
		public int durationWorld = -1;
		
		public string hitsTakenHolder = "";
		public int hitsTakenWorld = -1;
		public int dodgeTimeWorld = -1;

		public string healthLossHolder = "";
		public int healthLossWorld = -1;
		public int healthAtStartWorld = -1;

		public static Func<TagCompound, WorldStats> DESERIALIZER = tag => new WorldStats(tag);

		public WorldStats() { }
		private WorldStats(TagCompound tag) {
			durationHolder = tag.Get<string>(nameof(durationHolder));
			durationWorld = tag.Get<int>(nameof(durationWorld));
			hitsTakenHolder = tag.Get<string>(nameof(hitsTakenHolder));
			hitsTakenWorld = tag.Get<int>(nameof(hitsTakenWorld));
			dodgeTimeWorld = tag.Get<int>(nameof(dodgeTimeWorld));
			healthLossHolder = tag.Get<string>(nameof(healthLossHolder));
			healthLossWorld = tag.Get<int>(nameof(healthLossWorld));
			healthAtStartWorld = tag.Get<int>(nameof(healthAtStartWorld));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(durationHolder), durationHolder },
				{ nameof(durationWorld), durationWorld },
				{ nameof(hitsTakenHolder), hitsTakenHolder },
				{ nameof(hitsTakenWorld), hitsTakenWorld },
				{ nameof(dodgeTimeWorld), dodgeTimeWorld },
				{ nameof(healthLossHolder), healthLossHolder },
				{ nameof(healthLossWorld), healthLossWorld },
				{ nameof(healthAtStartWorld), healthAtStartWorld },
			};
		}

		internal void NetRecieve(BinaryReader reader) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.ShortestFightTime)) {
				durationHolder = reader.ReadString();
				durationWorld = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(RecordID.LeastHits)) {
				hitsTakenHolder = reader.ReadString();
				hitsTakenWorld = reader.ReadInt32();
				dodgeTimeWorld = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(RecordID.BestBrink)) {
				healthLossHolder = reader.ReadString();
				healthLossWorld = reader.ReadInt32();
				healthAtStartWorld = reader.ReadInt32();
			}
		}

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord); // We need this for NetRecieve as well
			// Packet should have any beaten records written on it
			if (specificRecord.HasFlag(RecordID.ShortestFightTime)) {
				writer.Write(durationHolder);
				writer.Write(durationWorld);
			}
			if (specificRecord.HasFlag(RecordID.LeastHits)) {
				writer.Write(hitsTakenHolder);
				writer.Write(hitsTakenWorld);
				writer.Write(dodgeTimeWorld);
			}
			if (specificRecord.HasFlag(RecordID.BestBrink)) {
				writer.Write(healthLossHolder);
				writer.Write(healthLossWorld);
				writer.Write(healthAtStartWorld);
			}
		}
	}

	public class BossCollection : TagSerializable
	{
		internal string bossName;
		internal List<ItemDefinition> loot;
		internal List<ItemDefinition> collectibles;

		public static Func<TagCompound, BossCollection> DESERIALIZER = tag => new BossCollection(tag);

		private BossCollection(TagCompound tag) {
			bossName = tag.Get<string>(nameof(bossName));
			loot = tag.Get<List<ItemDefinition>>(nameof(loot));
			collectibles = tag.Get<List<ItemDefinition>>(nameof(collectibles));
		}

		public BossCollection(string boss) {
			bossName = boss;
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(bossName), bossName },
				{ nameof(loot), loot },
				{ nameof(collectibles), collectibles },
			};
		}
	}
}