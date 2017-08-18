using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace BossChecklist
{
	/// <summary>
	/// Vanilla doesn't sync these values, so I will.
	/// </summary>
	class BossChecklistWorld : ModWorld
	{
		public override void NetSend(BinaryWriter writer)
		{
			BitsByte flags = new BitsByte();
			flags[0] = NPC.downedTowerSolar;
			flags[1] = NPC.downedTowerVortex;
			flags[2] = NPC.downedTowerNebula;
			flags[3] = NPC.downedTowerStardust;
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			NPC.downedTowerSolar = flags[0];
			NPC.downedTowerVortex = flags[1];
			NPC.downedTowerNebula = flags[2];
			NPC.downedTowerStardust = flags[3];
		}
	}
}
