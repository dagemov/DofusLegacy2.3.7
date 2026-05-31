using Sunshine.Protocol.IO;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("Breed", "com.ankamagames.dofus.datacenter.breeds", true)]
    [Serializable]
    public class Breed : IDataObject, IIndexedData
    {
        private const string MODULE = "Breeds";
        public int id;
        public uint shortNameId;
        public uint longNameId;
        public uint descriptionId;
        public uint gameplayDescriptionId;
        public string maleLook;
        public string femaleLook;
        public uint creatureBonesId;
        public int maleArtwork;
        public int femaleArtwork;
        public List<List<uint>> statsPointsForStrength;
        public List<List<uint>> statsPointsForIntelligence;
        public List<List<uint>> statsPointsForChance;
        public List<List<uint>> statsPointsForAgility;
        public List<List<uint>> statsPointsForVitality;
        public List<List<uint>> statsPointsForWisdom;
        public List<uint> breedSpellsId;
        public List<uint> maleColors;
        public List<uint> femaleColors;
        public List<uint> alternativeMaleSkin;
        public List<uint> alternativeFemaleSkin;
        public string altermaleskin;
        public string alterfemaleskin;

        int IIndexedData.Id
		{
			get
			{
				return this.id;
			}
		}

        

        [D2OIgnore]
		public int Id
		{
			get
			{
				return this.id;
			}
			set
			{
				this.id = value;
			}
		}
		[D2OIgnore]
		public uint ShortNameId
		{
			get
			{
				return this.shortNameId;
			}
			set
			{
				this.shortNameId = value;
			}
		}
		[D2OIgnore]
		public uint LongNameId
		{
			get
			{
				return this.longNameId;
			}
			set
			{
				this.longNameId = value;
			}
		}
		[D2OIgnore]
		public uint DescriptionId
		{
			get
			{
				return this.descriptionId;
			}
			set
			{
				this.descriptionId = value;
			}
		}
		[D2OIgnore]
		public uint GameplayDescriptionId
		{
			get
			{
				return this.gameplayDescriptionId;
			}
			set
			{
				this.gameplayDescriptionId = value;
			}
		}
		[D2OIgnore]
		public string MaleLook
		{
			get
			{
				return this.maleLook;
			}
			set
			{
				this.maleLook = value;
			}
		}
		[D2OIgnore]
		public string FemaleLook
		{
			get
			{
				return this.femaleLook;
			}
			set
			{
				this.femaleLook = value;
			}
		}
		[D2OIgnore]
		public uint CreatureBonesId
		{
			get
			{
				return this.creatureBonesId;
			}
			set
			{
				this.creatureBonesId = value;
			}
		}
		[D2OIgnore]
		public int MaleArtwork
		{
			get
			{
				return this.maleArtwork;
			}
			set
			{
				this.maleArtwork = value;
			}
		}
		[D2OIgnore]
		public int FemaleArtwork
		{
			get
			{
				return this.femaleArtwork;
			}
			set
			{
				this.femaleArtwork = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForStrength
		{
			get
			{
				return this.statsPointsForStrength;
			}
			set
			{
				this.statsPointsForStrength = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForIntelligence
		{
			get
			{
				return this.statsPointsForIntelligence;
			}
			set
			{
				this.statsPointsForIntelligence = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForChance
		{
			get
			{
				return this.statsPointsForChance;
			}
			set
			{
				this.statsPointsForChance = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForAgility
		{
			get
			{
				return this.statsPointsForAgility;
			}
			set
			{
				this.statsPointsForAgility = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForVitality
		{
			get
			{
				return this.statsPointsForVitality;
			}
			set
			{
				this.statsPointsForVitality = value;
			}
		}
		[D2OIgnore]
		public List<List<uint>> StatsPointsForWisdom
		{
			get
			{
				return this.statsPointsForWisdom;
			}
			set
			{
				this.statsPointsForWisdom = value;
			}
		}
		[D2OIgnore]
		public List<uint> BreedSpellsId
		{
			get
			{
				return this.breedSpellsId;
			}
			set
			{
				this.breedSpellsId = value;
			}
		}
		[D2OIgnore]
		public List<uint> MaleColors
		{
			get
			{
				return this.maleColors;
			}
			set
			{
				this.maleColors = value;
			}
		}
		[D2OIgnore]
		public List<uint> FemaleColors
		{
			get
			{
				return this.femaleColors;
			}
			set
			{
				this.femaleColors = value;
			}
		}
        [D2OIgnore]
        public List<uint> AlternativeMaleSkin
        {
            get
            {
                return this.alternativeMaleSkin;
            }
            set
            {
                this.alternativeMaleSkin = value;
            }
        }
        [D2OIgnore]
        public List<uint> AlternativeFemaleSkin
        {
            get
            {
                return this.alternativeFemaleSkin;
            }
            set
            {
                this.alternativeFemaleSkin = value;
            }
        }

        [D2OIgnore]
        public string alternativemaleskinsun
        {
            get
            {
                StringBuilder _skin = new StringBuilder();
                for (int i = 0; i < AlternativeMaleSkin.Count; i++)
                {
                    if (i == 0)
                        _skin.Append(AlternativeMaleSkin[0]);
                    else
                        _skin.Append("|" + AlternativeMaleSkin[0]);
                }
                return _skin.ToString();
            }
            set
            {
                this.altermaleskin = value;
            }
        }

        [D2OIgnore]
        public string alternativefemaleskinsun
        {
            get
            {
                StringBuilder _skin = new StringBuilder();
                for (int i = 0; i < AlternativeFemaleSkin.Count; i++)
                {
                    if (i == 0)
                        _skin.Append(AlternativeFemaleSkin[0]);
                    else
                        _skin.Append("|" + AlternativeFemaleSkin[0]);
                }
                return _skin.ToString();
            }
            set
            {
                this.alterfemaleskin = value;
            }
        }
    }
}
