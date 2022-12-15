using CadMath2D;
using CadMath2D.Path;
using SvgHelper;
using System.Collections.Generic;
using System.Drawing;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgPath : SvgShape {
        public List<IPathElement> PathList = new();
        public Color LineColor;
        public Color FillColor;

        public SvgPath(SvgElement element) : base(element)
        {
        }
        public override void Offset(CadPoint dp)
        {
            var m = TransformMatrix.CreateOffsetMatrix(dp.X, dp.Y);
            foreach (var pa in PathList)
            {
                pa.Transform(m);
            }
        }
        public override void Transform(TransformMatrix m)
        {
            foreach (var pa in PathList)
            {
                pa.Transform(m);
            }
        }
    }
}
