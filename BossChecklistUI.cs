using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;

namespace BossChecklist.UI
{
	class BossChecklistUI : UIState
	{
		public UIPanel checklistPanel;
		public UIList checklistList;

		float spacing = 8f;
		public static bool visible = false;

		public override void OnInitialize()
		{
			checklistPanel = new UIPanel();
			checklistPanel.SetPadding(10);
			checklistPanel.Left.Pixels = 0;
			checklistPanel.HAlign = 1f;
			checklistPanel.Top.Set(0f, 0f);
			checklistPanel.Width.Set(250f, 0f);
			checklistPanel.Height.Set(0f, 1f);
			checklistPanel.BackgroundColor = new Color(73, 94, 171);

			checklistList = new UIList();
			checklistList.Width.Set(0f, 1f);
			checklistList.Height.Set(0f, 1f);
			checklistList.ListPadding = 12f;
			checklistPanel.Append(checklistList);

			UIScrollbar checklistListScrollbar = new UIScrollbar();
			checklistListScrollbar.SetView(100f, 1000f);
			checklistListScrollbar.Height.Set(0f, 1f);
			checklistListScrollbar.HAlign = 1f;
			checklistPanel.Append(checklistListScrollbar);
			checklistList.SetScrollbar(checklistListScrollbar);

			// Checklistlist populated when the panel is shown: UpdateCheckboxes()

			Append(checklistPanel);

			// TODO, game window resize issue
		}

		internal void UpdateCheckboxes()
		{
			checklistList.Clear();

			foreach (BossInfo boss in allBosses)
			{
				if (boss.available())
				{
					UICheckbox box = new UICheckbox(boss.progression, boss.name, 1f, false);
					box.Selected = boss.downed();
					checklistList.Add(box);
				}
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (checklistPanel.ContainsPoint(MousePosition))
			{
				Main.player[Main.myPlayer].mouseInterface = true;
			}
		}

		public const float SlimeKing = 1f;
		public const float EyeOfCthulhu = 2f;
		public const float EaterOfWorlds = 3f;
		public const float QueenBee = 4f;
		public const float Skeletron = 5f;
		public const float WallOfFlesh = 6f;
		public const float TheTwins = 7f;
		public const float TheDestroyer = 8f;
		public const float SkeletronPrime = 9f;
		public const float Plantera = 10f;
		public const float Golem = 11f;
		public const float DukeFishron = 12f;
		public const float LunaticCultist = 13f;
		public const float Moonlord = 14f;

		BossInfo[] allBosses = new BossInfo[] {
			// Bosses -- Vanilla
			new BossInfo("Slime King", SlimeKing, () => true, () => NPC.downedSlimeKing),
			new BossInfo("Eye of Cthulhu", EyeOfCthulhu, () => true, () => NPC.downedBoss1),
			new BossInfo("Eater of Worlds / Brain of Cthulhu", EaterOfWorlds, () => true, () => NPC.downedBoss2),
			new BossInfo("Queen Bee", QueenBee, () => true, () => NPC.downedQueenBee),
			new BossInfo("Skeletron", Skeletron, () => true, () => NPC.downedBoss3),
			new BossInfo("Wall of Flesh", WallOfFlesh, () => true, () => Main.hardMode),
			new BossInfo("The Twins", TheTwins, () => true, () => NPC.downedMechBoss2),
			new BossInfo("The Destroyer",TheDestroyer, () => true, () => NPC.downedMechBoss1),
			new BossInfo("Skeletron Prime", SkeletronPrime, () => true, () => NPC.downedMechBoss3),
			new BossInfo("Plantera", Plantera, () => true, () => NPC.downedPlantBoss),
			new BossInfo("Golem", Golem, () => true, () => NPC.downedGolemBoss),
			new BossInfo("Duke Fishron", DukeFishron, () => true, () => NPC.downedFishron),
			new BossInfo("Lunatic Cultist", LunaticCultist, () => true, () => NPC.downedAncientCultist),
			new BossInfo("Moonlord", Moonlord, () => true, () => NPC.downedMoonlord),
			// Event Bosses -- Vanilla
			new BossInfo("Nebula Pillar", LunaticCultist + .1f, () => true, () => NPC.downedTowerNebula),
			new BossInfo("Vortex Pillar", LunaticCultist + .2f, () => true, () => NPC.downedTowerVortex),
			new BossInfo("Solar Pillar", LunaticCultist +.3f, () => true, () => NPC.downedTowerSolar),
			new BossInfo("Stardust Pillar", LunaticCultist + .4f, () => true, () => NPC.downedTowerStardust),
			// TODO, all other event bosses...Maybe all pillars as 1?

			new BossInfo("The Grand Thunder Bird", SlimeKing - 0.5f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedThunderBird),
			new BossInfo("The Queen Jellyfish", Skeletron - 0.5f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedJelly),
			//new BossInfo(, 0.1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.),
			//new BossInfo(, 0.1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.),
			new BossInfo("Coznix, the Fallen Beholder", WallOfFlesh + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedFallenBeholder),
			new BossInfo("The Lich", SkeletronPrime + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedLich),
			//new BossInfo(, 0.1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedPatchwerk),
			//new BossInfo(, 0.1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedSkelly),

			new BossInfo("Abomination", DukeFishron + 0.2f, () => BossChecklist.instance.bluemagicLoaded, () => Bluemagic.BluemagicWorld.downedAbomination),
			new BossInfo("Spirit of Purity", Moonlord + 0.9f, () => BossChecklist.instance.bluemagicLoaded, () => Bluemagic.BluemagicWorld.downedPuritySpirit),
			
			//new BossInfo(, 0.1f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.),

			//new BossInfo(, 0.1f, () => BossChecklist.instance.crystiliumLoaded, () => CrystiliumMod.CrystalWorld.),

			new BossInfo("Desert Scourge", SlimeKing + .5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedDesertScourge),
			new BossInfo("Calamitas", Plantera - 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedCalamitas),
			new BossInfo("The Devourer of Gods", Golem - 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedDevourerofGods),
			new BossInfo("Plaguebringer Goliath", Golem + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedPlaguebringerGoliath),
			new BossInfo("Slime God", Skeletron + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedSlimeGod),

		//	new BossInfo(, 0.1f, () => BossChecklist.instance.crystiliumLoaded, () => Tremor.CustomWorldData.),

			new BossInfo("Pumpking Horseman", DukeFishron + 0.3f, () => BossChecklist.instance.pumpkingLoaded, () => Pumpking.PumpkingWorld.downedPumpkingHorseman),
			new BossInfo("Terra Lord", Moonlord + 0.4f, () => BossChecklist.instance.pumpkingLoaded, () => Pumpking.PumpkingWorld.downedTerraLord),

		};

		//			Calamity
		//	--King Slime
		//	Desert Scourge
		//	Sea Dragon Leviathan (WIP) - Meant to be fought in Hardmode but can be fought earlier for a challenge
		//	--Eye of Cthulhu
		//	--Eater of Worlds/Brain of Cthulhu
		//	--Queen Bee
		//	The Perforator/The Hive Mind
		//	--Skeletron
		//	Slime God
		//	--Wall of Flesh
		//	Cryogen
		//	--The Destroyer/Skeletron Prime/The Twins
		//	Calamitas
		//	--Plantera
		//	The Devourer of Gods
		//	--The Golem
		//	Plaguebringer Goliath
		//	--Duke Fishron
		//	--Lunatic Cultist
		//	--Moon Lord
		//	Providence, the Profaned God (Currently being programmed and sprited, Hallowed Boss) Profaned Guardian (Miniboss that Providence spawns every 10% life)
		//	Alpha & Omega (WIP)
		//	Andromeda (Martian Star Destroyer) (WIP, Space Boss)
		//	Dargon
		//	Yharim, Lord of the Cosmos/Soul of Yharim (WIP)

		//			Thorium
		//The Grand Thunder Bird -- 1000 hp: Grand Flare Gun, Storm flare to summon
		//The Queen Jellyfish - Pre Skeletron boss / 4000 -- Jellyfish Resonator
		//Granite Energy Storm -- 4250 hp, Unstable Core, post skele
		//The Star Scouter - Post Skeletron Sky boss / 10000	-- Star Caller
		//Coznix, the Fallen Beholder - Early Hardmode Underworld boss / 14000 Life -- Void Lens
		//The Lich - Post mechanical Hardmode Boss / 16500 -- Grim Harvest Sigil 
		//The Ragnarok - Doom Sayer's Coin - 1 of each pillar fragment

		//			BlueMagic:
		//Abomination -- Post fishron - Foul Orb
		//Spirit of Purity --  Elemental Purge

		//			SacredTools:
		//	?????Abaddon -- 80000 -- Key of Obliteration
		//	Grand Harpy -- after skeltron Feather Talisman  -- fether and sunplate blocks
		//	Grand Harpy Queen -- post mechs, after skeletron prime -  Golden Feather Talisman
		//	oblivion shade - post cultist
		//	The Flare Serpent -- post moon, before spirit of purity - 300000HP --Obsidian Core

		//		Crystilium
		//	Crystal King -- Around Duke: summon at fountain with cryptic crystal

	}

	public class BossInfo
	{
		internal Func<bool> available;
		internal Func<bool> downed;
		internal string name;
		internal float progression;

		public BossInfo(string name, float progression, Func<bool> available, Func<bool> downed)
		{
			this.name = name;
			this.progression = progression;
			this.available = available;
			this.downed = downed;
		}
	}
}
