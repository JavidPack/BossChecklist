using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	/* Summary of Records : SheepishShepherd
	 * 
	 * Kills represent the amount of times a player has particiapted in killing a boss.
	 * Deaths represent the amount of deaths a player has accumulated during a participated boss fight
	 *		Kills MUST have player participation
	 *		Note that the player does not need to die by damage from the boss.
	 *		Also keep in mind players that are not anywhere near the boss fight and may die from other sources.
	 * 
	 * Prev represents the last attempt/previous best record for comparison
	 *		Whenever a player defeats a boss, we record their records and keep them stored.
	 *		There is no need to 'reset' these stats as they are only meant to tell you what you just got.
	 * 
	 * Firs represents the first successful attempt made for records
	 *		If a player has nothing recorded we keep the Prev Records records stored in the First Records as well.
	 *		Ideally, First Records will never change and cannot be 'reset'.
	 *		They can be restricted to a certain extent, but something to prevent abuse is needed. 
	 *		TODO: Looking for thoughts on the issue above.
	 * 
	 * Best represents the best record
	 *		Compare the Prev Records numbers to the current Best Records and determine if its better to record.
	 *		Best Records can be reset 
	 */

	/// <summary>
	/// Boss records are player-based and all personal records will be stored here and save to a ModPlayer.
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
	/// As the name implies, world records are world-based. All world records will be stored here and saved to a ModSystem.
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
	/// <item> <term>Kills</term> <description>The total amount of fights that the player has won against the boss</description> </item>
	/// <item> <term>Deaths</term> <description>The total amount of deaths a player has experienced while fighting the boss</description> </item>
	/// <item> <term>Attempts</term> <description>The amount of fights a player has died in (up until the first victory) while fighting the boss</description> </item>
	/// </list>
	/// <para>[Records]</para>
	/// <list type="bullet">
	/// <item> <term>Duration</term> <description>The amount of time it took to defeat the boss</description> </item>
	/// <item> <term>HitsTaken</term> <description>The amount of times a player has taken damage while fighting the boss</description> </item>
	/// </list>
	/// </summary>
	public class PersonalStats : TagSerializable
	{
		/// Boss Kills and Player Deaths
		public int kills;
		public int deaths;
		public int attempts;

		/// Fight Duration
		public int durationPrev = -1;
		public int durationBest = -1;
		public int durationFirst = -1;

		/// Total Hits Taken
		public int hitsTakenPrev = -1;
		public int hitsTakenBest = -1;
		public int hitsTakenFirst = -1;

		public static Func<TagCompound, PersonalStats> DESERIALIZER = tag => new PersonalStats(tag);

		public PersonalStats() { }

		private PersonalStats(TagCompound tag) {
			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			deaths = tag.Get<int>(nameof(attempts));

			durationPrev = tag.Get<int>(nameof(durationPrev));
			durationFirst = tag.Get<int>(nameof(durationFirst));
			durationBest = tag.Get<int>(nameof(durationBest));

			hitsTakenPrev = tag.Get<int>(nameof(hitsTakenPrev));
			hitsTakenFirst = tag.Get<int>(nameof(hitsTakenFirst));
			hitsTakenBest = tag.Get<int>(nameof(hitsTakenBest));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(attempts), attempts },

				{ nameof(durationPrev), durationPrev },
				{ nameof(durationFirst), durationFirst },
				{ nameof(durationBest), durationBest },

				{ nameof(hitsTakenPrev), hitsTakenPrev },
				{ nameof(hitsTakenFirst), hitsTakenFirst },
				{ nameof(hitsTakenBest), hitsTakenBest },
			};
		}

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord); // We need this for NetRecieve as well
			if (!specificRecord.HasFlag(RecordID.ResetAll)) {
				// If ResetAll is flagged there is no need to write the rest
				// Prev records ALWAYS are written. They always update as either record attempts or old records
				if (specificRecord.HasFlag(RecordID.Duration)) {
					writer.Write(durationBest);
				}
				writer.Write(durationPrev);
				if (specificRecord.HasFlag(RecordID.HitsTaken)) {
					writer.Write(hitsTakenBest);
				}
				writer.Write(hitsTakenPrev);
			}
		}

		//TODO: Change NetSend/NetRecieve to account for FirstRecord
		internal void NetRecieve(BinaryReader reader, Player player, int boss) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();
			if (!brokenRecords.HasFlag(RecordID.ResetAll)) {
				// Determine if a new record was made (Prev records need to change still)
				bool newRecord = brokenRecords.HasFlag(RecordID.Duration) || brokenRecords.HasFlag(RecordID.HitsTaken);

				kills++; // Kills always increase by 1, since records can only be made when a boss is defeated

				if (brokenRecords.HasFlag(RecordID.Duration)) {
					durationBest = reader.ReadInt32();
				}
				durationPrev = reader.ReadInt32();

				if (brokenRecords.HasFlag(RecordID.HitsTaken)) {
					hitsTakenBest = reader.ReadInt32();
				}
				hitsTakenPrev = reader.ReadInt32();
				
				if (newRecord) {
					player.GetModPlayer<PlayerAssist>().hasNewRecord[boss] = true;
					CombatText.NewText(player.getRect(), Color.LightYellow, "New Record!", true);
				}
			}
			else {
				// If ResetAll was flagged, change all variables to their default
				kills = deaths = attempts = 0;
				durationBest = durationPrev = hitsTakenBest = hitsTakenPrev = -1;
			}
		}
	}

	/* Summary of World Records : SheepishShepherd
	 * 
	 * All players that join a "world" are recorded to a list
	 * Server Host can remove anyone from this list (ex. Troll, wrong character join)
	 * Server grabs BEST Records from the list of players and determines which one is the best
	 * The player's name and record will be displayed on the World Record alt page for everyone to see and try to beat.
	 */

	/// <summary>
	/// In multiplayer, players are able to set world records against other players.
	/// This will contain record values the respective record holders.
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

		internal void NetSend(BinaryWriter writer, RecordID specificRecord) {
			writer.Write((int)specificRecord); // We need this for NetRecieve as well
			// Packet should have any beaten records written on it
			if (specificRecord.HasFlag(RecordID.Duration)) {
				writer.Write(durationHolder);
				writer.Write(durationWorld);
			}
			if (specificRecord.HasFlag(RecordID.HitsTaken)) {
				writer.Write(hitsTakenHolder);
				writer.Write(hitsTakenWorld);
			}
		}

		internal void NetRecieve(BinaryReader reader) {
			RecordID brokenRecords = (RecordID)reader.ReadInt32();

			if (brokenRecords.HasFlag(RecordID.Duration)) {
				durationHolder = reader.ReadString();
				durationWorld = reader.ReadInt32();
			}
			if (brokenRecords.HasFlag(RecordID.HitsTaken)) {
				hitsTakenHolder = reader.ReadString();
				hitsTakenWorld = reader.ReadInt32();
			}
		}
	}
}
