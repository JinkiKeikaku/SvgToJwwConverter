using CadMath2D;
using JwwHelper;
using SvgHelper;
using SvgToJwwConverter.SvgToJww.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CadMath2D.PolylineHelper;
using static CadMath2D.CadPointHelper;
using static CadMath2D.CadPoint;
using static CadMath2D.CadMath;
using System.Drawing;
using System.Windows;

namespace SvgToJwwConverter.SvgToJww {
    static class ShapeConverter {
        public static JwwData[] CreateJwwShape(SvgElement element, TransformMatrix m)
        {
            var svgShape = ConvertToSvgShape(element);
            if (svgShape != null)
            {
                svgShape.Transform(m);
                return ConvertToJww(svgShape);
            }
            return Array.Empty<JwwData>();
        }

        private static JwwData[] ConvertToJww(SvgShape svgShape) 
        { 
            var element = svgShape.Element;
            switch (svgShape)
            {
                case SvgLine s:
                {
                    var js = new JwwSen();
                    js.m_start_x = s.P0.X;
                    js.m_start_y = s.P0.Y;
                    js.m_end_x = s.P1.X;
                    js.m_end_y = s.P1.Y;
                    return new[] { js };
                }
                case SvgPolyline s:
                {
                    var sa = new List<JwwData>();
                    if (IsSolidEnable(s.FillColor))
                    {
                        if(s.Points.Count > 1)
                        {
                            if (s.Points.Count < 5)
                            {
                                var js = new JwwSolid
                                {
                                    m_start_x = s.Points[0].X,
                                    m_start_y = s.Points[0].Y,
                                    m_end_x = s.Points[1].X,
                                    m_end_y = s.Points[1].Y
                                };
                                if (s.Points.Count == 2)
                                {
                                    js.m_DPoint2_x = s.Points[0].X;
                                    js.m_DPoint2_y = s.Points[0].Y;
                                    js.m_DPoint3_x = s.Points[0].X;
                                    js.m_DPoint3_y = s.Points[0].Y;
                                } else if (s.Points.Count == 3)
                                {
                                    js.m_DPoint2_x = s.Points[2].X;
                                    js.m_DPoint2_y = s.Points[2].Y;
                                    js.m_DPoint3_x = s.Points[0].X;
                                    js.m_DPoint3_y = s.Points[0].Y;
                                } else
                                {
                                    js.m_DPoint2_x = s.Points[2].X;
                                    js.m_DPoint2_y = s.Points[2].Y;
                                    js.m_DPoint3_x = s.Points[3].X;
                                    js.m_DPoint3_y = s.Points[3].Y;
                                }

                                SetSolidColor(js, s.FillColor);
                                js.m_nPenStyle = 0;
                                sa.Add(js);
                            } else
                            {
                                var ta = ConvertPolygonToTriangle(s.Points);
                                foreach (var t in ta)
                                {
                                    var js = new JwwSolid()
                                    {
                                        m_start_x = t.P0.X,
                                        m_start_y = t.P0.Y,
                                        m_end_x = t.P1.X,
                                        m_end_y = t.P1.Y,
                                        m_DPoint2_x = t.P2.X,
                                        m_DPoint2_y = t.P2.Y,
                                        m_DPoint3_x = t.P0.X,
                                        m_DPoint3_y = t.P0.Y,
                                    };
                                    SetSolidColor(js, s.FillColor);
                                    js.m_nPenStyle = 0;
                                    sa.Add(js);
                                }
                            }
                        }
                    }
                    if (IsLineEnable(s.LineColor))
                    {
                        EnumPoints(s.Points, s.IsClosed, (p1, p2, _) => {
                            var js = new JwwSen();
                            js.m_start_x = p1.X;
                            js.m_start_y = p1.Y;
                            js.m_end_x = p2.X;
                            js.m_end_y = p2.Y;
                            sa.Add(js);
                            return true;
                        });
                    }
                    return sa.ToArray();
                }

                case SvgEllipse s:
                {
                    var sa = new List<JwwData>();
                    if (IsSolidEnable(s.FillColor))
                    {
                        var solid = new JwwSolid();
                        solid.m_start_x = s.P0.X;
                        solid.m_start_y = s.P0.Y;
                        solid.m_end_x = s.Rx;
                        solid.m_end_y = s.Ry / s.Rx;
                        solid.m_DPoint2_x = s.Angle;
                        solid.m_DPoint2_y = 0.0;
                        solid.m_DPoint3_x = 2.0 * Math.PI;
                        solid.m_DPoint3_y = 100;
                        solid.m_nPenStyle = 101;
                        SetSolidColor(solid, s.FillColor);
                        sa.Add(solid);
                    }
                    if (IsLineEnable(s.LineColor))
                    {
                        var js = new JwwEnko();
                        js.m_start_x = s.P0.X;
                        js.m_start_y = s.P0.Y;
                        js.m_bZenEnFlg = 1;
                        js.m_dHankei = s.Rx;
                        js.m_dHenpeiRitsu = s.Ry / s.Rx;
                        js.m_radKaishiKaku = 0.0;
                        js.m_radEnkoKaku = 2.0 * Math.PI;
                        sa.Add(js);
                    }
                    return sa.ToArray();
                }
                case SvgGroup s:
                {
                    var sa = new List<JwwData>();
                    foreach(var ss in s.Shapes)
                    {
                        sa.AddRange(ConvertToJww(ss));
                    }
                    return sa.ToArray();
                }
            }
            return Array.Empty<JwwData>();
        }

        static bool IsLineEnable(Color c)
        {
            if (Properties.Settings.Default.OnlyLine) return true;
            return c.A != 0;
        }

        static bool IsSolidEnable(Color c)
        {
            if (Properties.Settings.Default.OnlyLine) return false;
            return c.A != 0;
        }

        static bool SetSolidColor(JwwSolid js, Color c)
        {
            if (Properties.Settings.Default.OnlyLine) return false;
            js.m_nPenColor = 10;
            js.m_Color = c.R + (c.G << 8) + (c.B << 16);
            return true;
        }




        public static List<TriangleParam> ConvertPolygonToTriangle(
            IReadOnlyList<CadPoint> points
        )
        {
            //ear clipping アルゴリズムを使います。
            var dst = new List<TriangleParam>();
            if (points.Count < 3) return dst;
            var pts = CreateNormalizedPolygon(points, true);
            var lastCount = pts.Count;
            var c = 0;
            while (pts.Count > 3 && c < 10)
            {
                for (var i = pts.Count - 1; i >= 0; i--)
                {
                    var n = pts.Count;
                    var p1 = pts[(i - 1 + n) % n];
                    var p2 = pts[i];
                    var p3 = pts[(i + 1) % n];
                    var a = Cross(p3 - p2, p1 - p2);
                    if (a < 0) continue;
                    var isEar = true;
                    var hull = new CadPoint[] { p1, p2, p3 };
                    for (var j = 0; j < n - 3; j++)
                    {
                        var p11 = pts[(j + i + 1) % n];
                        var p12 = pts[(j + i + 2) % n];
                        var p13 = pts[(j + i + 3) % n];
                        //凹の角を調べます。
                        if (Cross(p13 - p12, p11 - p12) < 0)
                        {
                            //凹の角の頂点が３角形の中（周を除く）に入っているか。
                            if (PtInTriangle(p1, p2, p3, p12) &&
                                !IsPointOnLines(hull, p12, true))
                            {
                                isEar = false;
                                break;
                            }
                        }
                    }
                    if (!isEar) continue;
                    dst.Add(new(p1.Copy(), p2.Copy(), p3.Copy()));
                    pts.RemoveAt(i);
                    if (pts.Count <= 2) break;
                }
                if (lastCount == pts.Count)
                {
                    c++;
                } else
                {
                    lastCount = pts.Count;
                }
            }
            return dst;
        }




        private static SvgShape? ConvertToSvgShape(SvgElement element)
        {
            switch (element.NodeName)
            {
                case "g":
                    if (element is SvgShapeContainer sc) return CreateGroupShape(sc);
                    return null;
                case "use":
                {
                    var id = element.GetAttribute("xlink:href", false);
                    if (id == null || !id.StartsWith('#')) return null;
                    id = id.Substring(1);
                    if (!SvgShapeContainer.IdElementMap.TryGetValue(id, out var entity)) return null;
                    //参照元のParentをuseのParentに差し替えて図形作成。その後parentを戻す。
                    var tmp = entity.SetParent(element);
                    var shape = ConvertToSvgShape(entity);
                    entity.SetParent(tmp);
                    //
                    if (shape != null)
                    {
                        var dp = GetPoint(element, "x", "y");
                        shape.Offset(dp);
                    }
                    return shape;
                }

                case "line":
                {
                    var s = new SvgLine(
                            element,
                            GetPoint(element, "x1", "y1"), GetPoint(element, "x2", "y2"));
                    s.LineColor = GetColor(element, "stroke");
                    return s;

                }
                case "rect":
                {
                    var p = GetPoint(element, "x", "y");
                    var r = new CadRect(
                        p,
                        new CadPoint(p.X + element.GetDouble("width"), p.Y + element.GetDouble("height"))
                    );
                    r.Sort();
                    var s = new SvgPolyline(element, r.GetVertices(), true);
                    s.LineColor = GetColor(element, "stroke");
                    s.FillColor = GetColor(element, "fill");
                    return s;
                }
                case "circle":
                {
                    var s = new SvgEllipse(
                        element,
                        GetPoint(element, "cx", "cy"), element.GetDouble("r"));
                    s.LineColor = GetColor(element, "stroke");
                    s.FillColor = GetColor(element, "fill");
                    return s;
                }
                case "ellipse":
                {
                    var s = new SvgEllipse(
                        element,
                        GetPoint(element, "cx", "cy"),
                        element.GetDouble("rx"), element.GetDouble("ry"), 0.0);
                    s.LineColor = GetColor(element, "stroke");
                    s.FillColor = GetColor(element, "fill");
                    return s;
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
            var s = new SvgPolyline(
                element,
                 element.GetPoints("points").ConvertAll<CadPoint>(it => new CadPoint(it.x, it.y)),
                isPolygon);
            s.LineColor = GetColor(element, "stroke");
            s.FillColor = GetColor(element, "fill");
            return s;
        }

        static SvgGroup? CreateGroupShape(SvgShapeContainer shapeContainer)
        {
            var g = new SvgGroup(shapeContainer);
            foreach (var element in shapeContainer.ShapeElementList)
            {
                var s = ConvertToSvgShape(element);
                if (s == null) continue;
                //var m = ConvertToPreCad(element.TransformMatrix);
                s.Transform(element.TransformMatrix);
                g.Shapes.Add(s);
            }
            if (g.Shapes.Count == 0) return null;
            return g;
        }

        static Color GetColor(SvgElement element, string name)
        {
            var opacity = element.GetMultipliedOpacity();
            var color = element.GetAttribute(name, true);
            if (color == null || color == "none")
            {
                return Color.Transparent;
            } else
            {
                try
                {
                    var c = ColorHelper.HtmlToColor(color);
                    var a = (int)(opacity * 255);
                    return Color.FromArgb(a, c);
                } catch
                {
                    System.Diagnostics.Debug.WriteLine($"color fail :{color}");
                }
            }
            return Color.Black;
        }



        static CadPoint GetPoint(SvgElement element, string x, string y)
        {
            return new CadPoint(element.GetDouble(x), element.GetDouble(y));
        }


    }
}
