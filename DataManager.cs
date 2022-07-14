using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	/// <summary>
	/// Record container for player-based records. All personal records should be stored here and saved to a ModPlayer.
	/// </summary>
	public class BossRecord : TagSerializable
	{
		internal string bossKey;
		internal PersonalStats stats = new PersonalStats();

		public static Func<TagCompound, BossRecord> DESERIALIZER = tag => new BossRecord(tag);

		private BossRecord(TagCompound tag) {
			bossKey = tag.Get<string>(nameof(bossKey));
			stats = tag.Get<PersonalStats>(nameof(stats));
		}

		public BossRecord(string bossKey) {
			this.bossKey = bossKey;
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(bossKey), bossKey },
				{ nameof(stats), stats }
			};
		}
	}

	/// <summary>
	/// Record container for world-based records. All world records should be stored here and saved to a ModSystem.
	/// </summary>
	public class WorldRecord : TagSerializable
	{
		internal string bossKey;
		internal WorldStats stats = new WorldStats();

		public static Func<TagCompound, WorldRecord> DESERIALIZER = tag => new WorldRecord(tag);

		private WorldRecord(TagCompound tag) {
			bossKey = tag.Get<string>(nameof(bossKey));
			stats = tag.Get<WorldStats>(nameof(stats));
		}

		public WorldRecord(string bossKey) {
			this.bossKey = bossKey;
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(bossKey), bossKey },
				{ nameof(stats), stats }
			};
		}
	}

	/// <summary>
	/// Players are able to set personal records for boss fights.
	/// This will hold the statistics and records of those fights, including the player's previous fight, first victory, and personal best.
	/// <para>[Statistics]</para>
	/// <list type="bullet">
	/// <item> <term>Kills</term> <description>The total amount of fights that the player has won against the boss.</description> </item>
	/// <item> <term>Deaths</term> <description>The total amount of deaths a player has experienced while fighting the boss.</description> </item>
	/// <item> <term>Attempts</term> <description>The amount of fights a player has started against the boss, win or loss.</description> </item>
	/// <item> <term>Failures</term> <description>The amount of fights a player has died in (up until the first victory) while fighting the boss.</description> </item>
	/// </list>
	/// <para>[Records]</para>
	/// <list type="bullet">
	/// <item> <term>Duration</term> <description>The amount of time it took to defeat the boss.</description> </item>
	/// <item> <term>HitsTaken</term> <description>The amount of times a player has taken damage while fighting the boss.</description> </item>
	/// </list>
	/// </summary>
	public class PersonalStats : TagSerializable
	{
		/// Statistics
		public int kills;
		public int deaths;
		public int attempts;
		public int failures;

		/// Records
		public int durationPrev = -1;
		public int durationBest = -1;
		public int durationFirst = -1;

		public int hitsTakenPrev = -1;
		public int hitsTakenBest = -1;
		public int hitsTakenFirst = -1;

		// TODO: Figure out how Previous Best record should be implemented before adding them here

		public static Func<TagCompound, PersonalStats> DESERIALIZER = tag => new PersonalStats(tag);

		public PersonalStats() { }

		private PersonalStats(TagCompound tag) {
			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			attempts = tag.Get<int>(nameof(attempts));
			failures = tag.Get<int>(nameof(failures));

			durationPrev = tag.Get<int>(nameof(durationPrev));
			durationBest = tag.Get<int>(nameof(durationBest));
			durationFirst = tag.Get<int>(nameof(durationFirst));

			hitsTakenPrev = tag.Get<int>(nameof(hitsTakenPrev));
			hitsTakenBest = tag.Get<int>(nameof(hitsTakenBest));
			hitsTakenFirst = tag.Get<int>(nameof(hitsTakenFirst));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(attempts), attempts },
				{ nameof(failures), failures },

				{ nameof(durationPrev), durationPrev },
				{ nameof(durationBest), durationBest },
				{ nameof(durationFirst), durationFirst },

				{ nameof(hitsTakenPrev), hitsTakenPrev },
				{ nameof(hitsTakenBest), hitsTakenBest },
				{ nameof(hitsTakenFirst), hitsTakenFirst },
			};
		}

		internal void NetSend(BinaryWriter writer, NetRecordID recordType) {
			// Write the record type(s) we are changing. NetRecieve will need to read this value.
			writer.Write((int)recordType);

			// If the record type is a reset, nothing else needs to be done, as the records will be wiped. Otherwise...
			if (!recordType.HasFlag(NetRecordID.ResetAll)) {
				// ...previous records are always overwritten for the player to view...
				writer.Write(durationPrev);
				writer.Write(hitsTakenPrev);

				// ... and any first or new records we set will be flagged for sending
				if (recordType.HasFlag(NetRecordID.Duration_Best))
					writer.Write(durationBest);
				if (recordType.HasFlag(NetRecordID.HitsTaken_Best))
					writer.Write(hitsTakenBest);
				if (recordType.HasFlag(NetRecordID.Duration_First))
					writer.Write(durationFirst);
				if (recordType.HasFlag(NetRecordID.HitsTaken_First))
					writer.Write(hitsTakenFirst);
			}
		}

		internal void NetRecieve(BinaryReader reader) {
			NetRecordID recordType = (NetRecordID)reader.ReadInt32();
			if (recordType.HasFlag(NetRecordID.ResetAll)) {
				// ResetAll resets all fields to their default value
				kills = deaths = attempts = failures = 0;
				durationPrev = durationBest = durationFirst = hitsTakenPrev = hitsTakenBest = hitsTakenFirst = -1;
			}
			else {
				// Determine if a new record was made (Prev records need to change still)
				bool newRecord = recordType.HasFlag(NetRecordID.Duration_Best) || recordType.HasFlag(NetRecordID.HitsTaken_Best);

				kills++; // Kills always increase by 1, since records will only be updated when a boss is defeated
				durationPrev = reader.ReadInt32();
				hitsTakenPrev = reader.ReadInt32();

				if (recordType.HasFlag(NetRecordID.Duration_Best))
					durationBest = reader.ReadInt32();
				if (recordType.HasFlag(NetRecordID.HitsTaken_Best))
					hitsTakenBest = reader.ReadInt32();
				if (recordType.HasFlag(NetRecordID.Duration_First))
					durationFirst = reader.ReadInt32();
				if (recordType.HasFlag(NetRecordID.HitsTaken_First))
					hitsTakenFirst = reader.ReadInt32();
			}
		}
	}

	/* Plans for World Records
	 * All players that join a "world" are recorded to a list
	 * Server Host can remove anyone from this list (ex. Troll, wrong character join)
	 * Server grabs BEST Records from the list of players and determines which one is the best
	 */

	/// <summary>
	/// In multiplayer, players are able to set world records against other players.
	/// This will contain global kills and deaths as well as the best record's value and holder.
	/// </summary>
	public class WorldStats : TagSerializable
	{
		public int totalKills;
		public int totalDeaths;

		// TODO: Make a list of record holders for players who match (more for hits Taken)
		public string durationHolder = "";
		public int durationWorld = -1;
		
		public string hitsTakenHolder = "";
		public int hitsTakenWorld = -1;

		public static Func<TagCompound, WorldStats> DESERIALIZER = tag => new WorldStats(tag);

		public WorldStats() { }

		private WorldStats(TagCompound tag) {
			totalKills = tag.Get<int>(nameof(totalKills));
			totalDeaths = tag.Get<int>(nameof(totalDeaths));

			durationHolder = tag.Get<string>(nameof(durationHolder));
			durationWorld = tag.Get<int>(nameof(durationWorld));

			hitsTakenHolder = tag.Get<string>(nameof(hitsTakenHolder));
			hitsTakenWorld = tag.Get<int>(nameof(hitsTakenWorld));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(totalKills), totalKills },
				{ nameof(totalDeaths), totalDeaths },

				{ nameof(durationHolder), durationHolder },
				{ nameof(durationWorld), durationWorld },

				{ nameof(hitsTakenHolder), hitsTakenHolder },
				{ nameof(hitsTakenWorld), hitsTakenWorld },
			};
		}

		internal void NetSend(BinaryWriter writer, NetRecordID specificRecord) {
			// Write the record type(s) we are changing. NetRecieve will need to read this value.
			writer.Write((int)specificRecord);

			// Packet should have any beaten record values and holders written on it
			if (specificRecord.HasFlag(NetRecordID.Duration_Best)) {
				writer.Write(durationHolder);
				writer.Write(durationWorld);
			}
			if (specificRecord.HasFlag(NetRecordID.HitsTaken_Best)) {
				writer.Write(hitsTakenHolder);
				writer.Write(hitsTakenWorld);
			}
		}

		internal void NetRecieve(BinaryReader reader) {
			// Read the type of record being updated
			NetRecordID brokenRecords = (NetRecordID)reader.ReadInt32();

			// Since 'totalKills' are shared between all players, it will be updated outside of NetRecieve

			// Set the world record values and holders
			if (brokenRecords.HasFlag(NetRecordID.Duration_Best)) {
				durationHolder = reader.ReadString();
				durationWorld = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(NetRecordID.HitsTaken_Best)) {
				hitsTakenHolder = reader.ReadString();
				hitsTakenWorld = reader.ReadInt32();
			}
		}
	}
}
