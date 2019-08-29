using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader.IO;

namespace BossChecklist
{
    public class WorldAssist : ModWorld
    {
        public static bool downedBetsy;
        public static List<bool> ActiveBossesList = new List<bool>();
        public static List<int> ModBossTypes = new List<int>();
        public static List<string> ModBossMessages = new List<string>();
        
        string EventKey = "";
        bool isBloodMoon = false;
        bool isPumpkinMoon = false;
        bool isFrostMoon = false;
        bool isEclipse = false;

        public override void PreUpdate()
        {
			List<BossInfo> BL = BossChecklist.instance.setup.SortedBosses;
			if (ActiveBossesList.Count != BL.Count)
			{
				while (ActiveBossesList.Count > BL.Count) ActiveBossesList.RemoveAt(ActiveBossesList.Count - 1);
				while (ActiveBossesList.Count < BL.Count) ActiveBossesList.Add(false);
			}
			
			for (int n = 0; n < Main.maxNPCs; n++)
			{
				NPC b = Main.npc[n];
				if (NPCAssist.ListedBossNum(b) != -1)
				{
					if (!ActiveBossesList[NPCAssist.ListedBossNum(b)])
					{
						for (int i = 0; i < BossChecklist.instance.setup.SortedBosses[NPCAssist.ListedBossNum(b)].ids.Count; i++)
						{
							int thisType = BossChecklist.instance.setup.SortedBosses[NPCAssist.ListedBossNum(b)].ids[i];
							if (Main.npc.Any(npc => npc.type == thisType && npc.active))
							{
								ActiveBossesList[NPCAssist.ListedBossNum(b)] = true;
								break;
							}
						}
					}
					else // ActiveBossesList[NPCAssist.ListedBossNum(b)]
					{
						bool otherValidNPC = false;
						for (int i = 0; i < BossChecklist.instance.setup.SortedBosses[NPCAssist.ListedBossNum(b)].ids.Count; i++)
						{
							int otherType = BossChecklist.instance.setup.SortedBosses[NPCAssist.ListedBossNum(b)].ids[i];
							if (Main.npc.Any(npc => npc.type == otherType && npc.active))
							{
								otherValidNPC = true;
								break;
							}
						}
						if (!otherValidNPC)
						{
							if ((b.type != NPCID.MoonLordHead && b.life >= 0 && CheckRealLife(b.realLife)) || (b.type == NPCID.MoonLordHead && b.life <= 0))
							{
								// Replace with GetClientDespawnMessage()?
								if (!BossChecklist.ClientConfig.ODespawnBool)
								{
									if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(GetDespawnMessage(b), Colors.RarityPurple);
									else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(GetDespawnMessage(b)), Colors.RarityPurple);
								}
								ActiveBossesList[NPCAssist.ListedBossNum(b)] = false;
							}
						}
					}
				}
			}
        }

        public override void PostUpdate()
        {
			if (Main.netMode != NetmodeID.Server)
			{
				// Loot Collections
				for (int i = 0; i < BossChecklist.instance.setup.SortedBosses.Count; i++)
				{
					for (int j = 0; j < BossChecklist.instance.setup.SortedBosses[i].loot.Count; j++)
					{
						int item = BossChecklist.instance.setup.SortedBosses[i].loot[j];
						if (Main.LocalPlayer.HasItem(item))
						{
							int BossIndex = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies.FindIndex(boss => boss.bossName == BossChecklist.instance.setup.SortedBosses[i].name && boss.modName == BossChecklist.instance.setup.SortedBosses[i].source);
							if (BossIndex == -1) continue;
							if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].loot.FindIndex(x => x.type == item) == -1)
							{
								Item newItem = new Item();
								newItem.SetDefaults(item);
								Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].loot.Add(newItem);
							}
						}
					}
				}

				// Boss Collections
				for (int i = 0; i < BossChecklist.instance.setup.SortedBosses.Count; i++)
				{
					for (int j = 0; j < BossChecklist.instance.setup.SortedBosses[i].collection.Count; j++)
					{
						int item = BossChecklist.instance.setup.SortedBosses[i].collection[j];
						if (Main.LocalPlayer.HasItem(item))
						{
							int BossIndex = Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies.FindIndex(boss => boss.bossName == BossChecklist.instance.setup.SortedBosses[i].name && boss.modName == BossChecklist.instance.setup.SortedBosses[i].source);
							if (BossIndex == -1) continue;
							if (Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].collectibles.FindIndex(x => x.type == item) == -1)
							{
								Item newItem = new Item();
								newItem.SetDefaults(item);
								Main.LocalPlayer.GetModPlayer<PlayerAssist>().BossTrophies[i].collectibles.Add(newItem);
							}
						}
					}
				}
			}

			// Event Ending Messages
			if (Main.bloodMoon) isBloodMoon = true;
            if (Main.snowMoon) isFrostMoon = true;
            if (Main.pumpkinMoon) isPumpkinMoon = true;
            if (Main.eclipse) isEclipse = true;

            if (!Main.bloodMoon && isBloodMoon)
            {
                isBloodMoon = false;
                EventKey = "The Blood Moon falls past the horizon...";
            }
            else if (!Main.snowMoon && isFrostMoon)
            {
                isFrostMoon = false;
                EventKey = "The Frost Moon melts as the sun rises...";
            }
            else if (!Main.pumpkinMoon && isPumpkinMoon)
            {
                isPumpkinMoon = false;
                EventKey = "The Pumpkin Moon ends its harvest...";
            }
            else if (!Main.eclipse && isEclipse)
            {
                isEclipse = false;
                EventKey = "The solar eclipse has ended... until next time...";
            }

            if (EventKey != "")
            {
                if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(EventKey, Colors.RarityGreen);
                else NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(EventKey), Colors.RarityGreen);
                EventKey = "";
            }
        }

        public string GetDespawnMessage(NPC boss)
        {
            if (Main.player.Any(playerCheck => playerCheck.active && !playerCheck.dead)) // If any player is active and alive
            {
                if (Main.dayTime && (boss.type == NPCID.EyeofCthulhu || boss.type == NPCID.TheDestroyer || boss.type == NPCID.Retinazer || boss.type == NPCID.Spazmatism))
                {
                    return boss.FullName + " flees as the sun rises...";
                }
                else if (boss.type == NPCID.WallofFlesh) return "Wall of Flesh has managed to cross the underworld...";
                else if (boss.type == NPCID.Retinazer) return "The Twins are no longer after you...";
                else return boss.FullName + " is no longer after you...";
            }
            else if (BossChecklist.ClientConfig.CDespawnBool)
			{
				if (boss.type == NPCID.KingSlime) return "King Slime leaves in triumph...";
				else if (boss.type == NPCID.EyeofCthulhu) return "Eye of Cthulhu has disappeared into the night...";
				else if (boss.type == NPCID.EaterofWorldsHead) return "Eater of Worlds burrows back underground...";
				else if (boss.type == NPCID.BrainofCthulhu) return "Brain of Cthulhu vanishes into the pits of the crimson...";
				else if (boss.type == NPCID.QueenBee) return "Queen Bee returns to her colony's nest...";
				else if (boss.type == NPCID.SkeletronHead) return "Skeletron continues to torture the Old Man...";
				else if (boss.type == NPCID.WallofFlesh) return "Wall of Flesh has managed to cross the underworld...";
				else if (boss.type == NPCID.Retinazer) return "Retinazer continues its observations...";
				else if (boss.type == NPCID.Spazmatism) return "Spazmatism continues its observations...";
				else if (boss.type == NPCID.TheDestroyer) return "The Destroyer seeks for another world to devour...";
				else if (boss.type == NPCID.SkeletronPrime) return "Skeletron Prime begins searching for a new victim...";
				else if (boss.type == NPCID.Plantera) return "Plantera continues its rest within the jungle...";
				else if (boss.type == NPCID.Golem) return "Golem deactivates in the bowels of the temple...";
				else if (boss.type == NPCID.DukeFishron) return "Duke Fishron returns to the ocean depths...";
				else if (boss.type == NPCID.CultistBoss) return "Lunatic Cultist goes back to its devoted worship...";
				else if (boss.type == NPCID.MoonLordCore) return "Moon Lord has left this realm...";
				else
				{
					for (int i = 0; i < ModBossTypes.Count; i++)
					{
						if (boss.type == ModBossTypes[i]) return ModBossMessages[i];
						// If a mod has submitted a custom despawn message, it will display here
					}
					return boss.FullName + " has killed every player!";
					// Otherwise it defaults to this
				}
            }
			else return boss.FullName + " has killed every player!";
		}

        public override void Initialize()
        {
            downedBetsy = false;
        }

		public bool CheckRealLife(int realNPC)
		{
			if (realNPC == -1) return true;
			if (Main.npc[realNPC].life >= 0) return true;
			else return false;
		}

        public override TagCompound Save()
        {
            var downed = new List<string>();
            if (downedBetsy)
            {
                downed.Add("betsy");
            }

            return new TagCompound
            {
                {"downed", downed}
            };
        }

        public override void Load(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            downedBetsy = downed.Contains("betsy");
        }
    }
}