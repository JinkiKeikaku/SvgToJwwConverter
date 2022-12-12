using CadMath2D;
using CadMath2D.Parameters;
using SvgHelper;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgEllipse : SvgShape {
        public CadPoint P0;
        public double Rx;
        public double Ry;
        public double Angle;

        public SvgEllipse(
            SvgElement element,
            CadPoint p0, double rx, double ry, double angle
        ) : base(element)
        {
            P0 = p0;
            Rx = rx;
            Ry = ry;
            Angle = angle;
        }
        public SvgEllipse(SvgElement element,CadPoint p0, double r) : base(element)
        {
            P0 = p0;
            Rx = r;
            Ry = r;
            Angle = 0.0;
        }

        public override void Offset(CadPoint dp)
        {
            P0.Offset(dp);
        }
        public override void Transform(TransformMatrix m)
        {
            var param = new OvalParameter(P0, Rx, Ry / Rx, Angle);
            param.Transform(m);
            P0.Set(param.P0);
            Rx = param.Radius;
            Ry = param.Radius * param.Flatness;
            Angle = param.Angle;
        }
    }
}
