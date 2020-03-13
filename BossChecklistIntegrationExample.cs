/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace <YourModsNamespace>
{
	// This class provides an example of Boss Checklist integration that other Mods can copy into their mods.
	// By copying this class into your mod, you can access Boss Checklist boss data reliably and with type safety without requiring a strong dependency.
	public static class BossChecklistIntegration
	{
		// Boss Checklist might add new features, so a version is passed into GetBossInfo. 
		// If a new version of the GetBossInfo Call is implemented, find this class in the Boss Checklist Github once again and replace this version with the new version: https://github.com/JavidPack/BossChecklist/blob/master/BossChecklistIntegrationExample.cs
		private static readonly Version BossChecklistAPIVersion = new Version(1, 1);

		public class BossChecklistBossInfo
		{
			internal string key = ""; // equal to "modSource internalName"
			internal string modSource = "";
			internal string internalName = "";
			internal string displayName = "";

			internal float progression = 0f; // See https://github.com/JavidPack/BossChecklist/blob/master/BossTracker.cs#L13 for vanilla boss values
			internal Func<bool> downed = () => false;

			internal bool isBoss = false;
			internal bool isMiniboss = false;
			internal bool isEvent = false;

			internal List<int> npcIDs = new List<int>(); // Does not include minions, only npcids that count towards the NPC still being alive.
			internal List<int> spawnItem = new List<int>();
			internal List<int> loot = new List<int>();
			internal List<int> collection = new List<int>();
		}

		public static Dictionary<string, BossChecklistBossInfo> bossInfos = new Dictionary<string, BossChecklistBossInfo>();

		public static bool DoBossChecklistIntegration() {
			// Make sure to call this method in PostAddRecipes or later for best results
			Mod BossChecklist = ModLoader.GetMod("BossChecklist");
			if (BossChecklist != null && BossChecklist.Version >= BossChecklistAPIVersion) {
				object currentBossInfoResponse = BossChecklist.Call("GetBossInfoDictionary", BossChecklistAPIVersion.ToString());
				if (currentBossInfoResponse is Dictionary<string, Dictionary<string, object>> bossInfoList) {
					bossInfos = bossInfoList.ToDictionary(boss => boss.Key, boss => new BossChecklistBossInfo() {
						key = boss.Value.ContainsKey("key") ? boss.Value["key"] as string : "",
						modSource = boss.Value.ContainsKey("modSource") ? boss.Value["modSource"] as string : "",
						internalName = boss.Value.ContainsKey("internalName") ? boss.Value["internalName"] as string : "",
						displayName = boss.Value.ContainsKey("displayName") ? boss.Value["displayName"] as string : "",

						progression = boss.Value.ContainsKey("progression") ? Convert.ToSingle(boss.Value["progression"]) : 0f,
						downed = boss.Value.ContainsKey("downed") ? boss.Value["downed"] as Func<bool> : () => false,

						isBoss = boss.Value.ContainsKey("isBoss") ? Convert.ToBoolean(boss.Value["isBoss"]) : false,
						isMiniboss = boss.Value.ContainsKey("isMiniboss") ? Convert.ToBoolean(boss.Value["isMiniboss"]) : false,
						isEvent = boss.Value.ContainsKey("isEvent") ? Convert.ToBoolean(boss.Value["isEvent"]) : false,

						npcIDs = boss.Value.ContainsKey("npcIDs") ? boss.Value["npcIDs"] as List<int> : new List<int>(),
						spawnItem = boss.Value.ContainsKey("spawnItem") ? boss.Value["spawnItem"] as List<int> : new List<int>(),
						loot = boss.Value.ContainsKey("loot") ? boss.Value["loot"] as List<int> : new List<int>(),
						collection = boss.Value.ContainsKey("collection") ? boss.Value["collection"] as List<int> : new List<int>(),
					});
					return true;
				}
			}
			return false;
		}

		// This method shows an example of using the BossChecklistBossInfo data for something cool in your mod.
		public static float DownedBossedProgress() {
			if (bossInfos.Count == 0) // bossInfos might be empty, if BossChecklist isn't present or something goes wrong.
				return 0;

			return (float)bossInfos.Count(x => x.Value.downed()) / bossInfos.Count();
		}
	}
}
*/