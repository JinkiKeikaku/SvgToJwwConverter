using CadMath2D;
using JwwHelper;
using SvgHelper;
using SvgToJwwConverter.SvgToJww.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgToJwwConverter.SvgToJww {
    static class ShapeConverter {
        public static JwwData[] CreateJwwShape(SvgElement element, TransformMatrix m)
        {
            var jwwShapes = new List<JwwData>();
            var svgShape = ConvertToSvgShape(element);
            switch (svgShape)
            {
                case SvgLine s:
                {
                    s.Transform(m);
                    var js = new JwwSen();
                    js.m_start_x = s.P0.X;
                    js.m_start_y = s.P0.Y;
                    js.m_end_x = s.P1.X;
                    js.m_end_y = s.P1.Y;
                    return new[] { js };
                }
                case SvgPolyline s:
                {
                    s.Transform(m);
                    var ss = new List<JwwData>();
                    PolylineHelper.EnumPoints(s.Points, s.IsClosed, (p1, p2, _) => {
                        var js = new JwwSen();
                        js.m_start_x = p1.X;
                        js.m_start_y = p1.Y;
                        js.m_end_x = p2.X;
                        js.m_end_y = p2.Y;
                        ss.Add(js);
                        return true;
                    });
                    return ss.ToArray();
                }

                case SvgEllipse s:
                {
                    s.Transform(m);
                    var js = new JwwEnko();
                    js.m_start_x = s.P0.X;
                    js.m_start_y = s.P0.Y;
                    js.m_bZenEnFlg = 1;
                    js.m_dHankei = s.Rx;
                    js.m_dHenpeiRitsu = s.Ry / s.Rx;
                    js.m_radKaishiKaku = 0.0;
                    js.m_radEnkoKaku = 2.0 * Math.PI;
                    return new[] { js };
                }
            }
            return Array.Empty<JwwData>();
        }


        public static SvgShape? ConvertToSvgShape(SvgElement element)
        {
            switch (element.NodeName)
            {
                case "line":
                {
                    return new SvgLine(
                            element, 
                            GetPoint(element, "x1", "y1"), GetPoint(element, "x2", "y2"));
                }
                case "rect":
                {
                    var p = GetPoint(element, "x", "y");
                    var r = new CadRect(
                        p,
                        new CadPoint(p.X + element.GetDouble("width"), p.Y + element.GetDouble("height"))
                    );
                    r.Sort();
                    return new SvgPolyline(element, r.GetVertices(), true);
                }
                case "circle":
                {
                    return new SvgEllipse(
                        element,
                        GetPoint(element, "cx", "cy"), element.GetDouble("r"));
                }
                case "ellipse":
                {
                    return new SvgEllipse(
                        element,
                        GetPoint(element, "cx", "cy"), 
                        element.GetDouble("rx"), element.GetDouble("ry"), 0.0);
                }
                case "polyline":
                    return CreatePolyline(element, false);
                case "polygon":
                    return CreatePolyline(element, true);
            }
            return null;
        }

        static SvgShape CreatePolyline(SvgElement element, bool isPolygon)
        {
            return new SvgPolyline(
                element,
                 element.GetPoints("points").ConvertAll<CadPoint>(it=>new CadPoint(it.x, it.y)), 
                isPolygon);
        }



        static CadPoint GetPoint(SvgElement element, string x, string y)
        {
            return new CadPoint(element.GetDouble(x), element.GetDouble(y));
        }


    }
}
