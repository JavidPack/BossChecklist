using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.UI.Chat;
using Terraria.ModLoader;

namespace BossChecklist.UI
{
	class BossChecklistUI : UIState
	{
		public UIHoverImageButton toggleCompletedButton;
		public UIHoverImageButton toggleMiniBossButton;
		public UIHoverImageButton toggleEventButton;
		public UIPanel checklistPanel;
		public UIList checklistList;

		float spacing = 8f;
		public static bool visible = false;
		public static bool showCompleted = true;
		public static bool showMiniBoss = true;
		public static bool showEvent = true;
		public static string hoverText = "";

		public override void OnInitialize()
		{
			checklistPanel = new UIPanel();
			checklistPanel.SetPadding(10);
			checklistPanel.Left.Pixels = 0;
			checklistPanel.HAlign = 1f;
			checklistPanel.Top.Set(50f, 0f);
			checklistPanel.Width.Set(250f, 0f);
			checklistPanel.Height.Set(-100, 1f);
			checklistPanel.BackgroundColor = new Color(73, 94, 171);

			toggleCompletedButton = new UIHoverImageButton(Main.itemTexture[ItemID.SuspiciousLookingEye], "Toggle Completed");
			toggleCompletedButton.OnClick += ToggleCompletedButtonClicked;
			toggleCompletedButton.Left.Pixels = spacing;
			toggleCompletedButton.Top.Pixels = spacing;
			checklistPanel.Append(toggleCompletedButton);

			toggleMiniBossButton = new UIHoverImageButton(Main.itemTexture[ItemID.CandyCorn], "Toggle Mini Bosses");
			toggleMiniBossButton.OnClick += ToggleMiniBossButtonClicked;
			toggleMiniBossButton.Left.Pixels = spacing + 32;
			toggleMiniBossButton.Top.Pixels = spacing;
			checklistPanel.Append(toggleMiniBossButton);

			toggleEventButton = new UIHoverImageButton(Main.itemTexture[ItemID.SnowGlobe], "Toggle Events");
			toggleEventButton.OnClick += ToggleEventButtonClicked;
			toggleEventButton.Left.Pixels = spacing + 64;
			toggleEventButton.Top.Pixels = spacing;
			checklistPanel.Append(toggleEventButton);

			checklistList = new UIList();
			checklistList.Top.Pixels = 32f + spacing;
			checklistList.Width.Set(-25f, 1f);
			checklistList.Height.Set(-32f, 1f);
			checklistList.ListPadding = 12f;
			checklistPanel.Append(checklistList);

			FixedUIScrollbar checklistListScrollbar = new FixedUIScrollbar();
			checklistListScrollbar.SetView(100f, 1000f);
			//checklistListScrollbar.Height.Set(0f, 1f);
			checklistListScrollbar.Top.Pixels = 32f + spacing;
			checklistListScrollbar.Height.Set(-32f - spacing, 1f);
			checklistListScrollbar.HAlign = 1f;
			checklistPanel.Append(checklistListScrollbar);
			checklistList.SetScrollbar(checklistListScrollbar);

			// Checklistlist populated when the panel is shown: UpdateCheckboxes()

			Append(checklistPanel);

			// TODO, game window resize issue
			InitializeVanillaBosses();
		}

		private void ToggleCompletedButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showCompleted = !showCompleted;
			UpdateCheckboxes();
		}

		private void ToggleMiniBossButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showMiniBoss = !showMiniBoss;
			UpdateCheckboxes();
		}

		private void ToggleEventButtonClicked(UIMouseEvent evt, UIElement listeningElement)
		{
			Main.PlaySound(SoundID.MenuOpen);
			showEvent = !showEvent;
			UpdateCheckboxes();
		}

		/*public bool ThoriumModDownedScout
		{
			get { return ThoriumMod.ThoriumWorld.downedScout; }
		}
		public bool CalamityDS => CalamityMod.CalamityWorld.downedDesertScourge;*/

		internal void UpdateCheckboxes()
		{
			checklistList.Clear();

			foreach (BossInfo boss in allBosses)
			{
				if (boss.available())
				{
					if (showCompleted || !boss.downed())
					{
						if (boss.type == BossChecklistType.Event && !showEvent)
							continue;
						if (boss.type == BossChecklistType.MiniBoss && !showMiniBoss)
							continue;
						UIBossCheckbox box = new UIBossCheckbox(boss);
						checklistList.Add(box);
					}
				}
			}

			//if (BossChecklist.instance.thoriumLoaded)
			//{
			//	if (ThoriumModDownedScout)
			//	{
			//		// Add items here
			//	}
			//}
		}


		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			hoverText = "";
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (checklistPanel.ContainsPoint(MousePosition))
			{
				Main.player[Main.myPlayer].mouseInterface = true;
			}
		}

		public TextSnippet hoveredTextSnipped;
		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);

			// now we can draw after all other drawing.
			if (hoveredTextSnipped != null)
			{
				hoveredTextSnipped.OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{
					hoveredTextSnipped.OnClick();
				}
				hoveredTextSnipped = null;
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

		List<BossInfo> allBosses;

		private void InitializeVanillaBosses()
		{
			allBosses = new List<BossInfo> {
			// Bosses -- Vanilla
			new BossInfo(BossChecklistType.Boss, "Slime King", SlimeKing, () => true, () => NPC.downedSlimeKing, $"Use [i:{ItemID.SlimeCrown}], randomly in outer 3rds of map, or kill 150 slimes during slime rain."),
			new BossInfo(BossChecklistType.Boss, "Eye of Cthulhu", EyeOfCthulhu, () => true, () => NPC.downedBoss1,  $"Use [i:{ItemID.SuspiciousLookingEye}] at night, or 1/3 chance nightly if over 200 HP\nAchievement : [a:EYE_ON_YOU]"),
			new BossInfo(BossChecklistType.Boss, "Eater of Worlds / Brain of Cthulhu", EaterOfWorlds, () => true, () => NPC.downedBoss2,  $"Use [i:{ItemID.WormFood}] or [i:{ItemID.BloodySpine}] or break 3 Crimson Hearts or Shadow Orbs"),
			new BossInfo(BossChecklistType.Boss, "Queen Bee", QueenBee, () => true, () => NPC.downedQueenBee,  $"Use [i:{ItemID.Abeemination}] or break Larva in Jungle"),
			new BossInfo(BossChecklistType.Boss, "Skeletron", Skeletron, () => true, () => NPC.downedBoss3,  $"Visit dungeon or use [i:{ItemID.ClothierVoodooDoll}] at night"),
			new BossInfo(BossChecklistType.Boss, "Wall of Flesh", WallOfFlesh, () => true, () => Main.hardMode  ,  $"Spawn by throwing [i:{ItemID.GuideVoodooDoll}] in lava in the Underworld. [c/FF0000:Starts Hardmode!]"),
			new BossInfo(BossChecklistType.Boss, "The Twins", TheTwins, () => true, () => NPC.downedMechBoss2,  $"Use [i:{ItemID.MechanicalEye}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "The Destroyer",TheDestroyer, () => true, () => NPC.downedMechBoss1,  $"Use [i:{ItemID.MechanicalWorm}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "Skeletron Prime", SkeletronPrime, () => true, () => NPC.downedMechBoss3,  $"Use [i:{ItemID.MechanicalSkull}] at night to spawn"),
			new BossInfo(BossChecklistType.Boss, "Plantera", Plantera, () => true, () => NPC.downedPlantBoss,  $"Break a Plantera's Bulb in jungle after 3 Mechanical bosses have been defeated"),
			new BossInfo(BossChecklistType.Boss, "Golem", Golem, () => true, () => NPC.downedGolemBoss,  $"Use [i:{ItemID.LihzahrdPowerCell}] on Lihzahrd Altar"),
			new BossInfo(BossChecklistType.Boss, "Duke Fishron", DukeFishron, () => true, () => NPC.downedFishron,  $"Fish in ocean using the [i:{ItemID.TruffleWorm}] bait"),
			new BossInfo(BossChecklistType.Boss, "Lunatic Cultist", LunaticCultist, () => true, () => NPC.downedAncientCultist,  $"Kill the cultists outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Boss, "Moonlord", Moonlord, () => true, () => NPC.downedMoonlord,  $"Use [i:{ItemID.CelestialSigil}] or defeat all {(BossChecklist.tremorLoaded ? 5 : 4)} pillars. {(BossChecklist.tremorLoaded ? "[c/FF0000:Starts Tremode!]" : "")}"),
			// Event Bosses -- Vanilla
			new BossInfo(BossChecklistType.Event, "Nebula Pillar", LunaticCultist + .1f, () => true, () => NPC.downedTowerNebula,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Vortex Pillar", LunaticCultist + .2f, () => true, () => NPC.downedTowerVortex,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Solar Pillar", LunaticCultist +.3f, () => true, () => NPC.downedTowerSolar,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			new BossInfo(BossChecklistType.Event, "Stardust Pillar", LunaticCultist + .4f, () => true, () => NPC.downedTowerStardust,  $"Kill the Lunatic Cultist outside the dungeon post-Golem"),
			// TODO, all other event bosses...Maybe all pillars as 1?
			new BossInfo(BossChecklistType.MiniBoss, "Clown", WallOfFlesh + 0.1f, () => true, () => NPC.downedClown,  $"Spawns during Hardmode Bloodmoon"),
			new BossInfo(BossChecklistType.Event, "Goblin Army", EyeOfCthulhu + 0.5f, () => true, () => NPC.downedGoblins,  $"Occurs randomly at dawn once a Shadow Orb or Crimson Heart has been destroyed. Alternatively, spawn with [i:{ItemID.GoblinBattleStandard}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Ice Queen", Plantera + 0.9f, () => true, () => NPC.downedChristmasIceQueen,  $"Spawns during Wave 11 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Santa-NK1", Plantera + 0.6f, () => true, () => NPC.downedChristmasSantank,  $"Spawns during Wave 7 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Everscream", Plantera + 0.3f, () => true, () => NPC.downedChristmasTree,  $"Spawns during Wave 4 of Frost Moon. Start Frost Moon with [i:{ItemID.NaughtyPresent}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Frost Legion", WallOfFlesh + 0.6f, () => true, () => NPC.downedFrost,  $"Use [i:{ItemID.SnowGlobe}] to start. Find [i:{ItemID.SnowGlobe}] by opening [i:{ItemID.Present}] while in Hardmode during Christmas season."),
			new BossInfo(BossChecklistType.MiniBoss, "Pumpking", Plantera + 0.3f, () => true, () => NPC.downedHalloweenKing,  $"Spawns during Wave 7 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
			new BossInfo(BossChecklistType.MiniBoss, "Mourning Wood", Plantera + 0.6f, () => true, () => NPC.downedHalloweenTree,  $"Spawns during Wave 4 of Pumpkin Moon. Start Pumpkin Moon with [i:{ItemID.PumpkinMoonMedallion}]"),
			new BossInfo(BossChecklistType.Event, "Martian Madness", Golem + 0.4f, () => true, () => NPC.downedMartians,  $"After defeating Golem, find a Martian Probe above ground and let it escape."),
			new BossInfo(BossChecklistType.Event, "Pirate Invasion", WallOfFlesh + 0.7f, () => true, () => NPC.downedPirates,  $"Occurs randomly in Hardmode after an Altar has been destroyed. Alternatively, spawn with [i:{ItemID.PirateMap}]"),
			new BossInfo(BossChecklistType.Event, "Old One's Army 1", EaterOfWorlds + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1,  $"After finding the Tavernkeep, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			new BossInfo(BossChecklistType.Event, "Old One's Army 2", TheTwins + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT2,  $"After defeating a mechanical boss, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),
			new BossInfo(BossChecklistType.Event, "Old One's Army 3", Golem + 0.5f, () => true, () => Terraria.GameContent.Events.DD2Event.DownedInvasionT3,  $"After defeating Golem, activate [i:{ItemID.DD2ElderCrystalStand}] with [i:{ItemID.DD2ElderCrystal}]"),

			// ThoriumMod -- Working, missing some minibosses/bosses?
			/*
			new BossInfo("The Grand Thunder Bird", SlimeKing - 0.5f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedThunderBird,  $"Spawn during day by shooting a [i:{ModLoader.GetMod("ThoriumMod")?.ItemType("StormFlare") ?? 0}] with a [i:{ModLoader.GetMod("ThoriumMod")?.ItemType("StrongFlareGun") ?? 0}]"),
			new BossInfo("The Queen Jellyfish", Skeletron - 0.5f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedJelly),
			new BossInfo("Granite Energy Storm", Skeletron + 0.2f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedStorm),
			new BossInfo("The Star Scouter", Skeletron + 0.3f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedScout),
			new BossInfo("The Buried Champion", Skeletron + 0.4f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedChampion),
			new BossInfo("Borean Strider", WallOfFlesh + .05f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedStrider),
			new BossInfo("Coznix, the Fallen Beholder", WallOfFlesh + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedFallenBeholder),
			new BossInfo("The Lich", SkeletronPrime + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedLich),
			new BossInfo("Abyssion, The Forgotten One", Plantera + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedDepthBoss),
			new BossInfo("The Ragnarok", Moonlord + .1f, () => BossChecklist.instance.thoriumLoaded, () => ThoriumMod.ThoriumWorld.downedRealityBreaker),
			*/

			// Bluemagic -- Working 100%
			//new BossInfo("Abomination", DukeFishron + 0.2f, () => BossChecklist.instance.bluemagicLoaded, () => Bluemagic.BluemagicWorld.downedAbomination),
			//new BossInfo("Spirit of Purity", Moonlord + 0.9f, () => BossChecklist.instance.bluemagicLoaded, () => Bluemagic.BluemagicWorld.downedPuritySpirit),
			//new BossInfo("Spirit of Chaos", Moonlord + 1.9f, () => BossChecklist.instance.bluemagicLoaded, () => Bluemagic.BluemagicWorld.downedChaosSpirit),

			// Calamity -- Looks like some bosses are still WIP?
			//new BossInfo("Desert Scourge", SlimeKing + .5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedDesertScourge),
			//new BossInfo("The Hive Mind", QueenBee + .51f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedHiveMind),
			//new BossInfo("The Perforator", QueenBee + .51f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedPerforator),
			//new BossInfo("Slime God", Skeletron + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedSlimeGod),
			//new BossInfo("Cryogen", WallOfFlesh + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedCryogen),
			//new BossInfo("Calamitas", Plantera - 0.3f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedCalamitas),
			//new BossInfo("Plaguebringer Goliath", Golem + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedPlaguebringer),
			//new BossInfo("The Devourer of Gods", Moonlord + 0.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedDoG),
			//new BossInfo("Jungle Dragon, Yharon", Moonlord + 1.5f, () => BossChecklist.instance.calamityLoaded, () => CalamityMod.CalamityWorld.downedYharon),
			// CalamityMod.CalamityWorld.downedYharon
			
			// SacredTools -- Working 100%
			//new BossInfo("Grand Harpy", Skeletron + .3f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.downedHarpy),
			//new BossInfo("Harpy Queen, Raynare", Plantera - 0.4f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.downedRaynare),
			//new BossInfo("Abaddon", LunaticCultist + .5f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.downedAbaddon),
			//new BossInfo("Flare Serpent", Moonlord + .2f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.FlariumSpawns),
			//new BossInfo("Lunarians", Moonlord + .3f, () => BossChecklist.instance.sacredToolsLoaded, () => SacredTools.ModdedWorld.downedLunarians),

			// Joost
			//new BossInfo("Jumbo Cactuar", Moonlord + 0.7f, () => BossChecklist.instance.joostLoaded, () => JoostMod.JoostWorld.downedJumboCactuar),
			//new BossInfo("SA-X", Moonlord + 0.8f, () => BossChecklist.instance.joostLoaded, () => JoostMod.JoostWorld.downedSAX),

			// CrystiliumMod -- Need exposed downedBoss bools
			//new BossInfo(, 0.1f, () => BossChecklist.instance.crystiliumLoaded, () => CrystiliumMod.CrystalWorld.),
			
			// Pumpking -- downedBoss bools incorrectly programed
			//new BossInfo("Pumpking Horseman", DukeFishron + 0.3f, () => BossChecklist.instance.pumpkingLoaded, () => Pumpking.PumpkingWorld.downedPumpkingHorseman),
			//new BossInfo("Terra Lord", Moonlord + 0.4f, () => BossChecklist.instance.pumpkingLoaded, () => Pumpking.PumpkingWorld.downedTerraLord),
			};
		}

		internal void AddBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.Boss, bossname, bossValue, () => true, bossDowned, bossInfo));
		}

		internal void AddMiniBoss(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.MiniBoss, bossname, bossValue, () => true, bossDowned, bossInfo));
		}

		internal void AddEvent(string bossname, float bossValue, Func<bool> bossDowned, string bossInfo = null)
		{
			allBosses.Add(new BossInfo(BossChecklistType.Event, bossname, bossValue, () => true, bossDowned, bossInfo));
		}

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
		//internal int spawnItemID;
		internal string info;
		internal BossChecklistType type;

		internal BossInfo(BossChecklistType type, string name, float progression, Func<bool> available, Func<bool> downed, string info = null)
		{
			this.type = type;
			this.name = name;
			this.progression = progression;
			this.available = available;
			this.downed = downed;
			this.info = info;
		}
	}

	internal enum BossChecklistType
	{
		Boss,
		MiniBoss,
		Event
	}

	public class FixedUIScrollbar : UIScrollbar
	{
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.bossChecklistInterface;
			base.DrawSelf(spriteBatch);
			UserInterface.ActiveInstance = temp;
		}

		public override void MouseDown(UIMouseEvent evt)
		{
			UserInterface temp = UserInterface.ActiveInstance;
			UserInterface.ActiveInstance = BossChecklist.bossChecklistInterface;
			base.MouseDown(evt);
			UserInterface.ActiveInstance = temp;
		}
	}
}
