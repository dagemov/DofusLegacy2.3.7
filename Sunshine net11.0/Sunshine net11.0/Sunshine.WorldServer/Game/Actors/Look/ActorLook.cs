using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Look
{
    public class ActorLook
    {
        private Dictionary<int, Color> _colors = new Dictionary<int, Color>();
        private List<short> _scales = new List<short>();
        private List<short> _skins = new List<short>();
        private List<SubActorLook> _subActorLooks = new List<SubActorLook>();
        private short _bonesID;

        public List<SubActorLook> SubActorLooks => _subActorLooks;
        public short BonesID
        {
            get => _bonesID;
            set => _bonesID = value;
        }

        public List<short> Skins => _skins;
        public List<short> Scales => _scales;
        public Dictionary<int, Color> Colors => _colors;

        public ActorLook(short bones, short[] skins, Dictionary<int, Color> indexedColors, short[] scales, SubActorLook[] subActorLooks)
        {
            _bonesID = bones;
            _skins = skins?.ToList() ?? new List<short>();
            _colors = indexedColors ?? new Dictionary<int, Color>();
            _scales = scales?.ToList() ?? new List<short>();
            _subActorLooks = subActorLooks?.ToList() ?? new List<SubActorLook>();
        }

        public ActorLook() { }

        public ActorLook AuraLook
        {
            get
            {
                SubActorLook subActorLook = _subActorLooks.FirstOrDefault(x =>
                    x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_BASE_FOREGROUND);

                return subActorLook?.Look;
            }
        }

        public EntityLook GetEntityLook()
        {
            return new EntityLook()
            {
                bonesId = BonesID,
                scales = Scales.ToArray(),
                skins = Skins.ToArray(),
                subentities = SubActorLooks.Select(e => e.GetSubEntity()).ToArray(),
                indexedColors = Colors
                    .Select(x => x.Key << 24 | (x.Value.ToArgb() & 16777215))
                    .ToArray(),
            };
        }

        public void AddSkin(short skin)
        {
            if (!_skins.Contains(skin))
                _skins.Add(skin);
        }

        public void AddSkin(short skin, short firstSkin)
        {
            if (_skins.Contains(skin))
                return;

            int index = _skins.IndexOf(firstSkin);

            if (index < 0 || index >= _skins.Count)
                _skins.Add(skin);
            else
                _skins.Insert(index + 1, skin);
        }

        public void RemoveSkin(short skin)
        {
            _skins.Remove(skin);
        }

        public void AddColor(int index, Color color)
        {
            _colors[index] = color;
        }

        public void RemoveColor(int index)
        {
            _colors.Remove(index);
        }

        public void AddSubLook(SubActorLook subLook)
        {
            _subActorLooks.Add(subLook);
        }

        public void SetAuraSkin(short skin)
        {
            ActorLook actorLook = AuraLook;
            if (actorLook == null)
            {
                actorLook = new ActorLook();
                AddSubLook(new SubActorLook(0, SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_BASE_FOREGROUND, actorLook));
            }

            actorLook.BonesID = skin;
        }

        public void RemoveAuras()
        {
            _subActorLooks.RemoveAll(x =>
                x.BindingCategory == SubEntityBindingPointCategoryEnum.HOOK_POINT_CATEGORY_BASE_FOREGROUND);
        }


        public string ToDebugString()
        {
            string colors = Colors.Count == 0
                ? "[]"
                : "[" + string.Join(", ", Colors.Select(x => $"{x.Key}=#{x.Value.ToArgb() & 0xFFFFFF:X6}")) + "]";

            string subLooks = SubActorLooks.Count == 0
                ? "[]"
                : "[" + string.Join(", ", SubActorLooks.Select(x =>
                    $"(index={x.BindingIndex}, category={(int)x.BindingCategory}, look={x.Look?.ToDebugString() ?? "null"})")) + "]";

            return $"bones={BonesID}, skins=[{string.Join(", ", Skins)}], scales=[{string.Join(", ", Scales)}], colors={colors}, subLooks={subLooks}";
        }

        public override string ToString()
        {
            return ToDebugString();
        }

        public ActorLook Clone()
        {
            ActorLook actorLook = new ActorLook();
            actorLook.BonesID = _bonesID;
            actorLook._colors = _colors.ToDictionary(x => x.Key, x => x.Value);
            actorLook._skins = _skins.ToList();
            actorLook._scales = _scales.ToList();
            actorLook._subActorLooks = _subActorLooks
                .Select(x => new SubActorLook(x.BindingIndex, x.BindingCategory, x.Look.Clone()))
                .ToList();

            return actorLook;
        }
    }
}