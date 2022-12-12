using CadMath2D;
using SvgHelper;
using System.Collections.Generic;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgGroup : SvgShape {
        public List<SvgShape> Shapes = new();

        public SvgGroup(SvgElement element) : base(element)
        {
        }
        public override void Offset(CadPoint dp)
        {
            foreach (var s in Shapes) s.Offset(dp);
        }
        public override void Transform(TransformMatrix m)
        {
            foreach (var s in Shapes) s.Transform(m);
        }
    }
}
