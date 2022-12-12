using CadMath2D;
using SvgHelper;
using System.Collections.Generic;
using System.Drawing;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgPath : SvgShape {
        public List<List<CadPoint>> PointsList = new();
        public Color LineColor;
        public Color FillColor;

        public SvgPath(SvgElement element) : base(element)
        {
        }
        public override void Offset(CadPoint dp)
        {
            foreach (var pa in PointsList)
            {
                foreach (var p in pa) p.Offset(dp);
            }
        }
        public override void Transform(TransformMatrix m)
        {
            foreach (var pa in PointsList)
            {
                foreach (var p in pa) p.Set(m * p);
            }
        }
    }
}
