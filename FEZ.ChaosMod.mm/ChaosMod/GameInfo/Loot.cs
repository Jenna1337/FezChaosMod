using System;
using System.Collections.Generic;
using System.Linq;

namespace FezGame.GameInfo
{
	public class Loot
	{
		public LootMetadataList Keys = new LootMetadataList();
		public LootMetadataList GoldCubes = new LootMetadataList();
		public LootMetadataList AntiCubes = new LootMetadataList();
		public LootMetadataList CubeBits = new LootMetadataList();
		public LootMetadataList Owls = new LootMetadataList();
		public LootMetadataList PieceOfHeart = new LootMetadataList();
		public LootMetadataList Maps = new LootMetadataList();
		public LootMetadataList Artifacts = new LootMetadataList();

        public float TotalCubes => GoldCubes.Count + AntiCubes.Count + CubeBits.Count / 8f;

        public LootMetadataList All => new LootMetadataList(){
			Keys,
			GoldCubes,
			AntiCubes,
			CubeBits,
			PieceOfHeart,
			Artifacts,
			Maps,
			Owls,
		};

		public bool HasLoot()
		{
			return Keys.Count > 0 || GoldCubes.Count > 0 || AntiCubes.Count > 0 || CubeBits.Count > 0 || PieceOfHeart.Count > 0 || Artifacts.Count > 0 || Maps.Count > 0;
		}
		public void Add(Loot other)
		{
			this.Keys.AddRange(other.Keys);
			this.GoldCubes.AddRange(other.GoldCubes);
			this.AntiCubes.AddRange(other.AntiCubes);
			this.CubeBits.AddRange(other.CubeBits);
			this.PieceOfHeart.AddRange(other.PieceOfHeart);
			this.Artifacts.AddRange(other.Artifacts);
			this.Maps.AddRange(other.Maps);
			this.Owls.AddRange(other.Owls);
		}
	}
}