using CadMath2D;
using SvgHelper;
using System.Collections.Generic;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgPolyline : SvgShape {
        public List<CadPoint> Points = new();
        public bool IsClosed;
        public SvgPolyline(
            SvgElement element, IReadOnlyList<CadPoint> points, bool isClosed
        ) : base(element)
        {
            Points.AddRange(points);
            IsClosed = isClosed;
        }
        public override void Offset(CadPoint dp)
        {
            foreach (var p in Points) p.Offset(dp);
        }
        public override void Transform(TransformMatrix m)
        {
            foreach (var p in Points) p.Set(m * p);
        }
    }
}
