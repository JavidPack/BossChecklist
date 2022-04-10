using System;
using System.Linq;
using Terraria.ModLoader;

namespace BossChecklist.Commands
{
	internal class BossProgressionCommand : ModCommand
	{
		public override CommandType Type => CommandType.Console;

		public override string Command => "BossProgress";

		public override string Description => "View Boss Progression";

		// public override string Usage => base.Usage;

		public override void Action(CommandCaller caller, string input, string[] args) {
			var sortedBosses = BossChecklist.bossTracker.SortedBosses.OrderBy(x => x.progression);
			foreach (var boss in sortedBosses) {
				bool downed = boss.downed();
				Console.ForegroundColor = downed ? ConsoleColor.Green : ConsoleColor.Yellow;
				Console.WriteLine($"{boss.type}: {boss.DisplayName}");
			}
			Console.ResetColor();
		}
	}
}