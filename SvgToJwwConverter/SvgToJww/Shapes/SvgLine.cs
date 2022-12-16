using CadMath2D;
using SvgHelper;
using System.Drawing;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgLine : SvgShape {
        public CadPoint P0;
        public CadPoint P1;
        public Color LineColor;
        public double LineWidth;

        public SvgLine(
            SvgElement element, CadPoint p0, CadPoint p1
        ) : base(element)
        {
            P0 = p0;
            P1 = p1;
        }



        public override void Offset(CadPoint dp)
        {
            P0.Offset(dp);
            P1.Offset(dp);
        }

        public override void Transform(TransformMatrix m)
        {
            P0.Set(m * P0);
            P1.Set(m * P1);
        }
    }
}
