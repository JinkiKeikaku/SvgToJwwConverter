using CadMath2D;
using SvgHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    class SvgText : SvgShape {
        public enum TextBasis
        {
            BottomLeft, BottomCenter, BottomRight
        }
        public CadPoint P0 = new();
        public TextBasis Basis = TextBasis.BottomLeft;
        public string Text = "";
        public string FontName="";
        public double FontHeight = 16.0;
        public double FontWidth = 16.0;
        public double Width = 16.0;
        public double Angle = 0.0;
        public Color TextColor;


        public SvgText(SvgElement element) : base(element)
        {
        }
        public override void Offset(CadPoint dp)
        {
            P0.Offset(dp);
        }
        public override void Transform(TransformMatrix m)
        {
            var m2 = new Matrix2D(m.A, m.B, m.C, m.D);
            var r = CadPointHelper.TransformedRectangle(Width, FontHeight, Angle, m2);
            FontWidth = FontWidth * r.Width / Width;
            Width = r.Width;
            FontHeight = r.Height;
            Angle = r.AngleDeg;
            P0.Set(m * P0);
        }
    }
}
