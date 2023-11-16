using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
	[Flags]
	internal enum NetRecordID : int {
		None = 0,
		PreviousAttempt = 1,
		FirstVictory = 2,
		SuccessfulAttempt = 4,
		PersonalBest_Duration = 8,
		PersonalBest_HitsTaken = 16,
		WorldRecord_Duration = 32,
		WorldRecord_HitsTaken = 64,
		WorldRecord_OnlyDeaths = 128,
		PersonalBest_Reset = 256, // Resetting personal best records will also remove record from World Records
		FirstVictory_Reset = 512,
	}

	/// <summary>
	/// Players are able to set personal records for boss fights.
	/// This will hold the statistics and records of those fights, including the player's previous fight, first victory, and personal best.
	/// <para>[Statistics]</para>
	/// <list type="bullet">
	/// <item> <term>Kills</term> <description>The total amount of fights that the player has won against the boss.</description> </item>
	/// <item> <term>Deaths</term> <description>The total amount of deaths a player has experienced while fighting the boss.</description> </item>
	/// <item> <term>Attempts</term> <description>The amount of fights a player has started against the boss, win or loss.</description> </item>
	/// <item> <term>Play Time First</term> <description>The amount of play time that has passed up until the first kill of the boss.</description> </item>
	/// </list>
	/// <para>[Records]</para>
	/// <list type="bullet">
	/// <item> <term>Duration</term> <description>The amount of time it took to defeat the boss.</description> </item>
	/// <item> <term>HitsTaken</term> <description>The amount of times a player has taken damage while fighting the boss.</description> </item>
	/// </list>
	/// </summary>
	public class PersonalRecords : TagSerializable {
		/// Statistics
		public int kills;
		public int deaths;
		public int attempts;
		public long playTimeFirst = -1;

		/// Trackers
		public int Tracker_Duration;
		public int Tracker_HitsTaken;
		public int Tracker_Deaths;

		/// Records
		public int durationPrev = -1;
		public int durationBest = -1;
		public int durationPrevBest = -1;
		public int durationFirst = -1;

		public int hitsTakenPrev = -1;
		public int hitsTakenBest = -1;
		public int hitsTakenPrevBest = -1;
		public int hitsTakenFirst = -1;

		private readonly string bossKey = "";
		public string BossKey => bossKey;
		public int RecordIndex => BossChecklist.bossTracker.BossRecordKeys.IndexOf(BossKey);

		public bool CanBeSaved => attempts > 0;

		public Point GetStats(int category) {
			return category switch {
				(int)SubCategory.PreviousAttempt => new Point(durationPrev, hitsTakenPrev),
				(int)SubCategory.FirstVictory => new Point(durationFirst, hitsTakenFirst),
				(int)SubCategory.PersonalBest => new Point(durationBest, hitsTakenBest),
				_ => new Point(-1, -1)
			};
		}

		public bool UnlockedFirstVictory => playTimeFirst > 0; // unlocked in log once a play time is tracked
		public bool UnlockedPersonalBest => kills >= 2; // unlocked in log once the boss has been killed at least twice

		public static Func<TagCompound, PersonalRecords> DESERIALIZER = tag => new PersonalRecords(tag);

		public PersonalRecords(string bossKey) {
			this.bossKey = bossKey;
		}

		private PersonalRecords(TagCompound tag) {
			bossKey = tag.Get<string>(nameof(BossKey));

			kills = tag.Get<int>(nameof(kills));
			deaths = tag.Get<int>(nameof(deaths));
			attempts = tag.Get<int>(nameof(attempts));
			playTimeFirst = tag.Get<long>(nameof(playTimeFirst));

			durationPrev = tag.Get<int>(nameof(durationPrev));
			durationBest = tag.Get<int>(nameof(durationBest));
			durationPrevBest = tag.Get<int>(nameof(durationPrevBest));
			durationFirst = tag.Get<int>(nameof(durationFirst));

			hitsTakenPrev = tag.Get<int>(nameof(hitsTakenPrev));
			hitsTakenBest = tag.Get<int>(nameof(hitsTakenBest));
			hitsTakenPrevBest = tag.Get<int>(nameof(hitsTakenPrevBest));
			hitsTakenFirst = tag.Get<int>(nameof(hitsTakenFirst));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(BossKey), BossKey },

				{ nameof(kills), kills },
				{ nameof(deaths), deaths },
				{ nameof(attempts), attempts },
				{ nameof(playTimeFirst), playTimeFirst },

				{ nameof(durationPrev), durationPrev },
				{ nameof(durationBest), durationBest },
				{ nameof(durationPrevBest), durationPrevBest },
				{ nameof(durationFirst), durationFirst },

				{ nameof(hitsTakenPrev), hitsTakenPrev },
				{ nameof(hitsTakenBest), hitsTakenBest },
				{ nameof(hitsTakenPrevBest), hitsTakenPrevBest },
				{ nameof(hitsTakenFirst), hitsTakenFirst },
			};
		}

		internal bool IsCurrentlyBeingTracked => IsTracking; // value cannot be changed outside of PersonalStats.
		private bool IsTracking = false;

		/// <summary>
		/// Restarts the trackers and begins tracking the fight.
		/// </summary>
		/// <returns>If the tracker starting was successful. This should typically only return false if an instance of the boss is still active. </returns>
		internal bool StartTracking() {
			if (IsTracking)
				return false; // do not reset or start tracking if it currently is tracking already

			IsTracking = true;
			Tracker_Duration = Tracker_HitsTaken = Tracker_Deaths = 0;
			return true;
		}

		/// <summary>
		/// Restarts the trackers of each player stored by the server. Afterwards, it restarts the trackers for all clients.
		/// </summary>
		internal void StartTracking_Server(int whoAmI) {
			if (Main.netMode != NetmodeID.Server || StartTracking() is false)
				return;

			// Needs to be updated client side as well
			// Send packets from the server to all participating players to reset their trackers for the recordIndex provided
			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write((byte)PacketMessageType.ResetTrackers);
			packet.Write(RecordIndex);
			packet.Send(toClient: whoAmI); // Server --> Multiplayer client
		}

		/// <summary>
		/// Stops the trackers from updating and stores the info tracked.
		/// </summary>
		/// <param name="allowRecordSaving">Should the tracked data be used to update the records? This should typically be only false when no player interaction occured.</param>
		/// <param name="savePreviousAttempt">Should the tracked data be recorded as a previous attempt?</param>
		/// <returns>The NetRecordID provided by the updated data. Used to communicate with the server what data needs to be sent and updated.</returns>
		internal NetRecordID? StopTracking(bool allowRecordSaving, bool savePreviousAttempt) {
			if (!IsTracking)
				return null; // do not change any stats if tracking is not currently enabled

			NetRecordID serverParse = NetRecordID.None;
			IsTracking = false;
			attempts++; // attempts always increase by one for every fight, not matter the outcome
			deaths += Tracker_Deaths; // same goes for tracked deaths

			// update previous attempt records
			if (savePreviousAttempt) {
				durationPrev = Tracker_Duration;
				hitsTakenPrev = Tracker_HitsTaken;
				serverParse |= NetRecordID.PreviousAttempt;
			}

			// record should only occur when the boss is defeated
			if (allowRecordSaving) {
				kills++; // increase kill counter when recording
				serverParse |= NetRecordID.SuccessfulAttempt;
				if (!UnlockedFirstVictory) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						playTimeFirst = Main.ActivePlayerFileData.GetPlayTime().Ticks; // server cant easily get this information
					}
					else if (Main.netMode == NetmodeID.Server) {
						playTimeFirst = 1; // server does not need actual value, but it needs to be greater than 0 to record Personal Best records
					}

					// if this was the first kill, update the first victory records
					durationFirst = Tracker_Duration;
					hitsTakenFirst = Tracker_HitsTaken;
					serverParse |= NetRecordID.FirstVictory;

					// personal best records are also updated, even if not displayed
					durationBest = Tracker_Duration;
					hitsTakenBest = Tracker_HitsTaken;
					serverParse |= NetRecordID.PersonalBest_Duration;
					serverParse |= NetRecordID.PersonalBest_HitsTaken;
				}
				else {
					// every kill after the first has the tracked record individually compared for a personal best
					// if applicable, the previous best record stats must be updated first, to store the original value
					if (durationBest > Tracker_Duration) {
						durationPrevBest = durationBest;
						durationBest = Tracker_Duration;
						serverParse |= NetRecordID.PersonalBest_Duration;
					}

					if (hitsTakenBest > Tracker_HitsTaken) {
						hitsTakenPrevBest = hitsTakenBest;
						hitsTakenBest = Tracker_HitsTaken;
						serverParse |= NetRecordID.PersonalBest_HitsTaken;
					}

					// If a single player client is running this method, no world record can be made, so send the personal best text.
					if (Main.netMode == NetmodeID.SinglePlayer && Networking.NewPersonalBest(serverParse)) {
						PlayerAssist modplayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
						modplayer.NewRecordState = PlayerAssist.RecordState_PersonalBest;
						modplayer.SubmitCombatText(RecordIndex);
					}
				}
			}

			return serverParse;
		}

		/// <summary>
		/// Stops the trackers from updating and stores the info tracked on the server's side.
		/// Packets with the updated record data are then sent to their respective players.
		/// </summary>
		/// <param name="whoAmI">The player whose records are being updated.</param>
		/// <param name="allowRecordSaving">Should the tracked data be used to update the records? This should typically be only false when no player interaction occured.</param>
		/// <param name="savePreviousAttempt">Should the tracked data be recorded as a previous attempt?</param>
		/// <returns>If the NetRecordID contains a new personal best. Used to check for World Records for the server later on.</returns>
		internal bool StopTracking_Server(int whoAmI, bool allowRecordSaving, bool savePreviousAttempt) {
			if (Main.netMode != NetmodeID.Server || StopTracking(allowRecordSaving, savePreviousAttempt) is not NetRecordID netRecord)
				return false; // do not change any stats if tracking is not currently enabled

			// Then send the mod packet to the client
			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write((byte)PacketMessageType.UpdateRecordsFromServerToPlayer);
			packet.Write(RecordIndex);
			NetSendRecords(packet, netRecord);
			packet.Send(toClient: whoAmI); // Server --> Multiplayer client (Player's only need to see their own records)

			return Networking.NewPersonalBest(netRecord);
		}

		/// <summary>
		/// Resets the record data stored back to 'No Record'. If this is run by a multiplayer client, the reset is sent to the server as well via packet.
		/// </summary>
		/// <param name="category">Specifies the records being reset by the Log's current subcategory.</param>
		internal void ResetStats(SubCategory category) {
			// No point in clearing Previous Attempt, its always updated each fight
			// World Records cannot be reset through normal means (localhost must remove all players from the holders list)
			NetRecordID resetType = NetRecordID.None;
			if (category == SubCategory.FirstVictory) {
				playTimeFirst = durationFirst = hitsTakenFirst = -1;
				resetType = NetRecordID.FirstVictory_Reset;
			}
			else if (category == SubCategory.PersonalBest) {
				kills = deaths = 0;
				durationBest = hitsTakenBest = -1;
				durationPrevBest = hitsTakenPrevBest = -1;
				resetType = NetRecordID.PersonalBest_Reset;
			}

			if (Main.netMode == NetmodeID.SinglePlayer || resetType == NetRecordID.None)
				return;

			// This method is only called by a Multiplayer client, as it derives from the UI
			// Send a ModPacket to update the server records accordingly
			ModPacket packet = BossChecklist.instance.GetPacket();
			packet.Write((byte)PacketMessageType.ResetPlayerRecordForServer);
			packet.Write(RecordIndex);
			packet.Write((int)resetType);
			packet.Send(); // Multiplayer client --> Server
		}

		/// <summary>
		/// Resets the record data stored back to 'No Record' on the server's side.
		/// </summary>
		/// <param name="netRecord"></param>
		internal void ResetStats_Server(NetRecordID netRecord) {
			if (netRecord.HasFlag(NetRecordID.FirstVictory_Reset)) {
				playTimeFirst = durationFirst = hitsTakenFirst = -1;
			}

			if (netRecord.HasFlag(NetRecordID.PersonalBest_Reset)) {
				kills = deaths = 0;
				durationBest = hitsTakenBest = -1;
				durationPrevBest = hitsTakenPrevBest = -1;
			}
		}

		/// <summary>
		/// Writes the record data needed to be sent to players.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="recordType"></param>
		internal void NetSendRecords(BinaryWriter writer, NetRecordID recordType) {
			writer.Write((int)recordType); // Write the record type(s) we are changing as NetRecieve will need to read this value.
			writer.Write(Tracker_Deaths); // deaths are always tracked

			if (recordType.HasFlag(NetRecordID.PreviousAttempt)) {
				writer.Write(durationPrev);
				writer.Write(hitsTakenPrev);
			}

			if (recordType.HasFlag(NetRecordID.FirstVictory)) {
				writer.Write(durationFirst);
				writer.Write(hitsTakenFirst);
			}

			if (recordType.HasFlag(NetRecordID.PersonalBest_Duration)) {
				writer.Write(durationBest);
				writer.Write(durationPrevBest);
			}

			if (recordType.HasFlag(NetRecordID.PersonalBest_HitsTaken)) {
				writer.Write(hitsTakenBest);
				writer.Write(hitsTakenPrevBest);
			}
		}

		/// <summary>
		/// Receives and reads the record data sent by the server.
		/// </summary>
		/// <param name="reader"></param>
		internal void NetReceiveRecords(BinaryReader reader) {
			NetRecordID recordType = (NetRecordID)reader.ReadInt32();
			attempts++; // attempts always increase by one
			deaths += reader.ReadInt32(); // since tracked deaths are being sent, just increase the value by the tracked amount
			bool beatenBefore = UnlockedFirstVictory;
			if (recordType.HasFlag(NetRecordID.SuccessfulAttempt))
				kills++;

			if (recordType.HasFlag(NetRecordID.PreviousAttempt)) {
				durationPrev = reader.ReadInt32();
				hitsTakenPrev = reader.ReadInt32();
			}

			if (recordType.HasFlag(NetRecordID.FirstVictory)) {
				durationFirst = reader.ReadInt32();
				hitsTakenFirst = reader.ReadInt32();
				playTimeFirst = Main.ActivePlayerFileData.GetPlayTime().Ticks; // Server cannot send this information, nor needs to
			}

			PlayerAssist modplayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (recordType.HasFlag(NetRecordID.PersonalBest_Duration)) {
				durationBest = reader.ReadInt32();
				durationPrevBest = reader.ReadInt32();

				if (beatenBefore)
					modplayer.NewRecordState = PlayerAssist.RecordState_PersonalBest;
			}

			if (recordType.HasFlag(NetRecordID.PersonalBest_HitsTaken)) {
				hitsTakenBest = reader.ReadInt32();
				hitsTakenPrevBest = reader.ReadInt32();

				if (beatenBefore)
					modplayer.NewRecordState = PlayerAssist.RecordState_PersonalBest;
			}
		}

		/// <summary>
		/// Gets the personal kills and deaths of an entry record in a string format.
		/// If the designated boss has not been killed nor has killed a player, 'Unchallenged' will be returned instead.
		/// </summary>
		public string GetKDR() {
			if (kills == 0 && deaths == 0)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.Unchallenged");

			return $"{kills} {Language.GetTextValue($"{BossLogUI.LangLog}.Records.Kills")} / {deaths} {Language.GetTextValue($"{BossLogUI.LangLog}.Records.Deaths")}";
		}

		/// <summary>
		/// Gets the duration time of an entry record in a string format.
		/// If the designated boss has not been defeated yet, 'No Record' will be returned instead.
		/// </summary>
		/// <param name="ticks">The about of ticks a fight took.</param>
		/// <param name="sign">Only used when finding a time difference using <see cref="TimeConversionDiff"/>.</param>
		public static string TimeConversion(int ticks, string sign = "") {
			if (ticks == -1)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.NoRecord");

			const int TicksPerSecond = 60;
			const int TicksPerMinute = TicksPerSecond * 60;
			int minutes = ticks / TicksPerMinute; // Minutes will still show if 0
			float seconds = (float)(ticks - (float)(minutes * TicksPerMinute)) / TicksPerSecond;
			float milliseconds = (float)((seconds - (int)seconds) * 1000);

			return BossChecklist.FeatureConfig.TimeValueFormat switch {
				"Simple" => $"{sign}{(minutes > 0 ? $"{minutes}m " : "")}{(int)seconds}s ({milliseconds:0}ms)",
				_ => $"{sign}{minutes}:{seconds:00.000}"
			};
		}

		/// <summary>
		/// Takes a duration record and compares it against another.
		/// The result will be in a string format along with a symbol to represent the difference direction.
		/// </summary>
		/// <param name="recordTicks">The recorded amount of ticks that is being compared against.</param>
		/// <param name="compareTicks">The amount of ticks from the compare value.</param>
		/// <param name="diff">The color value that represents the record time difference. 
		///		<list type="bullet">
		///		<item><term>Red</term> <description>The time recorded is slower than the compare value (+)</description></item>
		///		<item><term>Yellow</term> <description>No difference between the record's time (±)</description></item>
		///		<item><term>Green</term> <description>The time recorded is faster than the compare value (-)</description></item></list>
		/// </param>
		public static string TimeConversionDiff(int recordTicks, int compareTicks, out Color diff) {
			if (recordTicks == -1 || compareTicks == -1) {
				diff = default;
				return ""; // records cannot be compared
			}

			// A color and sign should be picked
			int tickDiff = recordTicks - compareTicks;
			string sign;
			if (tickDiff > 0) {
				sign = "+";
				diff = Colors.RarityRed;
			}
			else if (tickDiff == 0) {
				sign = "±";
				diff = Colors.RarityYellow;
			}
			else {
				tickDiff *= -1;
				sign = "-";
				diff = Colors.RarityGreen;
			}

			return TimeConversion(tickDiff, sign);
		}

		/// <summary>
		/// Gets the hits taken entry record in a string format.
		/// If the user's record is zero, 'No Hit!' will be returned instead.
		/// Otherwise, if the entry has not been defeated yet, 'No Record' will be returned instead.
		/// </summary>
		/// <param name="count">The record being checked.</param>
		public static string HitCount(int count) {
			if (count == -1)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.NoRecord");
			
			if (count == 0)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.NoHit");
			
			return $"{count} {Language.GetTextValue($"{BossLogUI.LangLog}.Records.Hit{(count == 1 ? "" : "Plural")}")}";
		}

		/// <summary>
		/// Takes a Hits Taken record and compares it against another.
		/// The result will be in a string format along with a symbol to represent the difference direction.
		/// </summary>
		/// <param name="count">The recorded amount of hits that is being compared against.</param>
		/// <param name="compareCount">The amount of hits from the compare value.</param>
		/// <param name="diff">The color value that represents the record time difference. 
		///		<list type="bullet">
		///		<item><term>Red</term> <description>The time recorded is slower than the compare value (+)</description></item>
		///		<item><term>Yellow</term> <description>No difference between the record's time (±)</description></item>
		///		<item><term>Green</term> <description>The time recorded is faster than the compare value (-)</description></item></list>
		/// </param>
		public static string HitCountDiff(int count, int compareCount, out Color diff) {
			if (count == -1 || compareCount == -1) {
				diff = default;
				return ""; // records cannot be compared
			}

			// A color and sign should be picked
			int countDiff = count - compareCount;
			string sign;
			if (countDiff > 0) {
				sign = "+";
				diff = Colors.RarityRed;
			}
			else if (countDiff == 0) {
				sign = "±";
				diff = Colors.RarityYellow;
			}
			else {
				countDiff *= -1;
				sign = "-";
				diff = Colors.RarityGreen;
			}

			return $"{sign}{countDiff}";
		}

		/// <summary>
		/// Gets the recorded play time snapshot for the entry's first defeation in a string format.
		/// If the entry has not yet been defeated, 'Unchallenged' will be returned instead.
		/// </summary>
		public string PlayTimeToString() {
			if (kills == 0)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.Unchallenged");

			int hours = (int)(playTimeFirst / TimeSpan.TicksPerHour);
			int minutes = (int)((playTimeFirst - (hours * TimeSpan.TicksPerHour)) / TimeSpan.TicksPerMinute);
			float seconds = (float)((playTimeFirst - (float)(hours * TimeSpan.TicksPerHour) - (float)(minutes * TimeSpan.TicksPerMinute)) / TimeSpan.TicksPerSecond);
			float milliseconds = (float)((seconds - (int)seconds) * 1000);

			return BossChecklist.FeatureConfig.TimeValueFormat switch {
				"Simple" => $"{(hours > 0 ? hours + "h " : "")}{minutes}m {(int)seconds}s ({milliseconds:0}ms)",
				_ => $"{(hours > 0 ? hours + ":" : "")}{minutes}:{seconds:0.000}"
			};
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
	public class WorldRecord : TagSerializable {
		public int totalKills;
		public int totalDeaths;

		public List<string> durationHolder = new List<string> { };
		public int durationWorld = -1;
		
		public List<string> hitsTakenHolder = new List<string> { };
		public int hitsTakenWorld = -1;

		private readonly string bossKey = "";
		public string BossKey => bossKey;
		public int RecordIndex => BossChecklist.bossTracker.BossRecordKeys.IndexOf(BossKey);
		
		public bool CanBeSaved => totalKills > 0 || totalDeaths > 0;

		public bool DurationNotRecorded => durationHolder.Count == 0 || durationWorld == -1;
		public bool HitsTakenNotRecorded => hitsTakenHolder.Count == 0 || hitsTakenWorld == -1;

		public static Func<TagCompound, WorldRecord> DESERIALIZER = tag => new WorldRecord(tag);

		public WorldRecord(string bossKey) {
			this.bossKey = bossKey;
		}

		private WorldRecord(TagCompound tag) {
			bossKey = tag.Get<string>(nameof(BossKey));

			totalKills = tag.Get<int>(nameof(totalKills));
			totalDeaths = tag.Get<int>(nameof(totalDeaths));

			durationHolder = tag.GetList<string>(nameof(durationHolder)).ToList();
			durationWorld = tag.Get<int>(nameof(durationWorld));

			hitsTakenHolder = tag.GetList<string>(nameof(hitsTakenHolder)).ToList();
			hitsTakenWorld = tag.Get<int>(nameof(hitsTakenWorld));
		}

		public TagCompound SerializeData() {
			return new TagCompound {
				{ nameof(BossKey), BossKey },

				{ nameof(totalKills), totalKills },
				{ nameof(totalDeaths), totalDeaths },

				{ nameof(durationHolder), durationHolder },
				{ nameof(durationWorld), durationWorld },

				{ nameof(hitsTakenHolder), hitsTakenHolder },
				{ nameof(hitsTakenWorld), hitsTakenWorld },
			};
		}

		/// <summary>
		/// Global deaths should still be tracked and updated to all players when the boss despawns.
		/// </summary>
		internal void UpdateGlobalDeaths(List<int> playersInteracted) {
			if (Main.netMode != NetmodeID.Server)
				return; // Only the server should be able to adjust world records

			foreach (Player player in Main.player) {
				if (!player.active || !playersInteracted.Contains(player.whoAmI))
					continue;

				PersonalRecords playerRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][RecordIndex];
				totalDeaths += playerRecords.Tracker_Deaths;
			}

			// updates need to be sent to ALL players
			foreach (Player player in Main.player) {
				if (!player.active)
					continue;

				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.UpdateWorldRecordsToAllPlayers);
				packet.Write(RecordIndex);
				NetSendWorldRecords(packet, NetRecordID.WorldRecord_OnlyDeaths); // only deaths needs to be update
				packet.Send(player.whoAmI);
			}
		}

		/// <summary>
		/// Compares the world records against the trackers if it is concluded to have a new personal best record.
		/// </summary>
		/// <param name="playersInteracted"></param>
		internal void CheckForWorldRecords_Server(List<int> playersInteracted) {
			if (Main.netMode != NetmodeID.Server)
				return; // Only the server should be able to adjust world records

			NetRecordID netRecord = NetRecordID.None;
			totalKills++; // kills always increase by one when an entry dies

			List<int> WorldRecordAchieved_Duration = new List<int>();
			List<int> WorldRecordAchieved_HitsTaken = new List<int>();

			// only players that have interacted with the boss should have their deaths and records added
			foreach (Player player in Main.player) {
				if (!player.active || !playersInteracted.Contains(player.whoAmI))
					continue;

				PersonalRecords playerRecords = BossChecklist.ServerCollectedRecords[player.whoAmI][RecordIndex];
				totalDeaths += playerRecords.Tracker_Deaths;

				bool Beaten_Duration = playerRecords.Tracker_Duration < durationWorld;
				bool Matched_Duration = playerRecords.Tracker_Duration == durationWorld;

				if (DurationNotRecorded || Beaten_Duration || Matched_Duration) {
					netRecord |= NetRecordID.WorldRecord_Duration;
					durationWorld = playerRecords.Tracker_Duration;
					if (Beaten_Duration) {
						durationHolder.Clear();
						WorldRecordAchieved_Duration.Clear();
					}

					if (!durationHolder.Contains(Main.player[player.whoAmI].name)) {
						durationHolder.Add(Main.player[player.whoAmI].name);
						WorldRecordAchieved_Duration.Add(player.whoAmI);
					}
				}

				bool Beaten_HitsTaken = playerRecords.Tracker_HitsTaken < hitsTakenWorld;
				bool Matched_HitsTaken = playerRecords.Tracker_HitsTaken == hitsTakenWorld;

				if (HitsTakenNotRecorded || Beaten_HitsTaken || Matched_HitsTaken) {
					netRecord |= NetRecordID.WorldRecord_HitsTaken;
					hitsTakenWorld = playerRecords.Tracker_HitsTaken;
					if (Beaten_HitsTaken) {
						hitsTakenHolder.Clear();
						WorldRecordAchieved_HitsTaken = new List<int>();
					}

					if (!hitsTakenHolder.Contains(Main.player[player.whoAmI].name)) {
						hitsTakenHolder.Add(Main.player[player.whoAmI].name);
						WorldRecordAchieved_HitsTaken.Add(player.whoAmI);
					}
				}
			}

			// updates need to be sent to ALL players
			foreach (Player player in Main.player) {
				if (!player.active)
					continue;

				ModPacket packet = BossChecklist.instance.GetPacket();
				packet.Write((byte)PacketMessageType.UpdateWorldRecordsToAllPlayers);
				packet.Write(RecordIndex);
				NetSendWorldRecords(packet, netRecord);
				packet.Write(WorldRecordAchieved_Duration.Contains(player.whoAmI) || WorldRecordAchieved_HitsTaken.Contains(player.whoAmI));
				packet.Send(player.whoAmI);
			}
		}



		internal void NetSend(BinaryWriter writer) {
			writer.Write(totalKills);
			writer.Write(totalDeaths);
			writer.Write(durationWorld);
			writer.Write(hitsTakenWorld);

			writer.Write(durationHolder.Count);
			foreach (string name in durationHolder) {
				writer.Write(name);
			}

			writer.Write(hitsTakenHolder.Count);
			foreach (string name in hitsTakenHolder) {
				writer.Write(name);
			}
		}

		internal void NetRecieve(BinaryReader reader) {
			totalKills = reader.ReadInt32();
			totalDeaths = reader.ReadInt32();
			durationWorld = reader.ReadInt32();
			hitsTakenWorld = reader.ReadInt32();

			int durationHolderCount = reader.ReadInt32();
			durationHolder.Clear();
			for (int i = 0; i < durationHolderCount; i++) {
				durationHolder.Add(reader.ReadString());
			}

			int hitsTakenHolderCount = reader.ReadInt32();
			hitsTakenHolder.Clear();
			for (int i = 0; i < hitsTakenHolderCount; i++) {
				hitsTakenHolder.Add(reader.ReadString());
			}
		}

		/// <summary>
		/// Writes the world record data needed to be sent to all clients.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="netRecords"></param>
		internal void NetSendWorldRecords(BinaryWriter writer, NetRecordID netRecords) {
			writer.Write((int)netRecords); // Write the record type(s) we are changing. NetRecieve will need to read this value.
			writer.Write(totalDeaths);
			if (netRecords.HasFlag(NetRecordID.WorldRecord_OnlyDeaths))
				return;

			// Packet should have any beaten record values and holders written on it
			if (netRecords.HasFlag(NetRecordID.WorldRecord_Duration)) {
				writer.Write(durationWorld);
				writer.Write(durationHolder.Count);
				foreach (string name in durationHolder) {
					writer.Write(name);
				}
			}

			if (netRecords.HasFlag(NetRecordID.WorldRecord_HitsTaken)) {
				writer.Write(hitsTakenWorld);
				writer.Write(hitsTakenHolder.Count);
				foreach (string name in hitsTakenHolder) {
					writer.Write(name);
				}
			}
		}

		/// <summary>
		/// Receives and reads the world record data sent by the server.
		/// </summary>
		/// <param name="reader"></param>
		internal void NetReceiveWorldRecords(BinaryReader reader) {
			NetRecordID netRecords = (NetRecordID)reader.ReadInt32(); // Read the type of record being updated

			totalDeaths = reader.ReadInt32();
			if (netRecords.HasFlag(NetRecordID.WorldRecord_OnlyDeaths))
				return;

			totalKills++; // Kills always increase by 1, since records will only be updated when a boss is defeated

			// Set the world record values and holders
			if (netRecords.HasFlag(NetRecordID.WorldRecord_Duration)) {
				durationWorld = reader.ReadInt32();
				int durationHolderTotal = reader.ReadInt32();
				durationHolder.Clear();
				for (int i = 0; i < durationHolderTotal; i++) {
					durationHolder.Add(reader.ReadString());
				}
			}

			if (netRecords.HasFlag(NetRecordID.WorldRecord_HitsTaken)) {
				hitsTakenWorld = reader.ReadInt32();
				int hitsTakenHolderTotal = reader.ReadInt32();
				hitsTakenHolder.Clear();
				for (int i = 0; i < hitsTakenHolderTotal; i++) {
					hitsTakenHolder.Add(reader.ReadString());
				}
			}

			PlayerAssist modplayer = Main.LocalPlayer.GetModPlayer<PlayerAssist>();
			if (reader.ReadBoolean() && modplayer.NewRecordState == PlayerAssist.RecordState_PersonalBest)
				modplayer.NewRecordState = PlayerAssist.RecordState_WorldRecord;

			modplayer.SubmitCombatText(RecordIndex);
		}

		/// <summary>
		/// Gets the total kills and deaths of an entry in a string format.
		/// If the entry has not yet been defeated, 'Unchallenged' will be returned instead.
		/// </summary>
		public string GetGlobalKDR() {
			if (totalKills == 0 && totalDeaths == 0)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.Unchallenged");

			return $"{totalKills} {Language.GetTextValue($"{BossLogUI.LangLog}.Records.Kills")} / {totalDeaths} {Language.GetTextValue($"{BossLogUI.LangLog}.Records.Deaths")}";
		}

		/// <summary>
		/// Lists the current holders of the duration world record.
		/// If the entry has not yet been defated, 'Be the first to claim the world record!' will be returned instead.
		/// </summary>
		public string ListDurationRecordHolders() {
			if (DurationNotRecorded)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.ClaimRecord");

			string list = Language.GetTextValue($"{BossLogUI.LangLog}.Records.RecordHolder");
			return list + "\n • " + string.Join("\n • ", durationHolder);
		}

		/// <summary>
		/// Lists the current holders of the hits taken world record.
		/// If the entry has not yet been defated, 'Be the first to claim the world record!' will be returned instead.
		/// </summary>
		public string ListHitsTakenRecordHolders() {
			if (HitsTakenNotRecorded)
				return Language.GetTextValue($"{BossLogUI.LangLog}.Records.ClaimRecord");

			string list = Language.GetTextValue($"{BossLogUI.LangLog}.Records.RecordHolder");
			return list + "\n • " + string.Join("\n • ", hitsTakenHolder);
		}
	}
}
