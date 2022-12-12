using CadMath2D;
using SvgHelper;

namespace SvgToJwwConverter.SvgToJww.Shapes {
    abstract class SvgShape {
        public SvgElement Element;
        public SvgShape(SvgElement element)
        {
            Element = element;
        }
        public abstract void Offset(CadPoint dp);
        public abstract void Transform(TransformMatrix m);
    }
}
