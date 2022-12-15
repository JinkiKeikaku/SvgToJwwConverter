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
using CadMath2D.Curves;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using CadMath2D.Path;
using SvgToJwwConverter.Properties;

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
                        if (s.Points.Count > 1)
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
                    foreach (var ss in s.Shapes)
                    {
                        sa.AddRange(ConvertToJww(ss));
                    }
                    return sa.ToArray();
                }
                case SvgPath s:
                {
                    var sa = new List<JwwData>();
                    if (IsSolidEnable(s.FillColor))
                    {
                        var poly = PathConverter.PathToConnectedPolygon(s.PathList, Settings.Default.CurveDiv);
                        var ts = PolylineHelper.ConvertPolygonToTriangle (poly);
                        foreach (var t in ts)
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
                    if (IsLineEnable(s.LineColor))
                    {
                        var polyList = PathConverter.PathToPolygons(s.PathList, Settings.Default.CurveDiv);
                        foreach (var poly in polyList)
                        {
                            EnumPoints(poly, false, (p1, p2, _) => {
                                var js = new JwwSen();
                                js.m_start_x = p1.X;
                                js.m_start_y = p1.Y;
                                js.m_end_x = p2.X;
                                js.m_end_y = p2.Y;
                                sa.Add(js);
                                return true;
                            });
                        }
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

        private static SvgShape? ConvertToSvgShape(SvgElement element)
        {
            switch (element.NodeName)
            {
                case "g":
                    if (element is SvgShapeContainer sc) return CreateGroup(sc);
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
                case "path":
                    return CreatePath(element);
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

        static SvgGroup? CreateGroup(SvgShapeContainer shapeContainer)
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

        static SvgPath? CreatePath(SvgElement element)
        {
            var pathString = element.GetAttribute("d", false);
            if (pathString == null) return null;
            // 単純に数値と１文字のコマンドで分ける。区切り文字のコンマやスペースのチェックは行わない。
            //以下の構文でコマンドと数値に分割された文字列の配列になる。
            var pa = Regex.Matches(pathString, @"[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?|[a-z]|[A-Z]")
                .OfType<Match>()
                .Select(m => m.Groups[0].Value)
                .ToArray();
            var cp = new CadPoint();
            var lastCt = new CadPoint();//最後のベジェの制御点
            var isBesier2Ciontinue = false;
            var isBesier3Ciontinue = false;
            //            var pts = new List<CadPoint>();
            string cmd = "";
            //            var shapes = new List<CadShape>();
            var pathList = PathMaker.CreatePathList(pm => {

                for (var i = 0; i < pa.Length; i++)
                {
                    var isBesier2Draw = false;
                    var isBesier3Draw = false;
                    //前回のコマンドの次が数値の場合、前回のコマンドを続けて実行。ただし、M(m)の場合はL(l)とする。
                    //前回のコマンドが非対応の場合はコマンドが""。
                    if (cmd != "" && double.TryParse(pa[i], out var tmp))
                    {
                        i--;
                        if (i < 0) break;
                        if (cmd == "m") cmd = "l";
                        if (cmd == "M") cmd = "L";
                    } else
                    {
                        cmd = pa[i];
                    }
                    switch (cmd)
                    {
                        case "m":
                        case "M":
                        {
                            //最初の点
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = cmd == "M" ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.BeginPath(ConvertPathPoint(cp));
                        }
                        break;

                        case "l":
                        case "L":
                        {
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = cmd == "L" ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.AddLine(ConvertPathPoint(cp));
                        }
                        break;
                        case "h":
                        case "H":
                        {
                            var x = double.Parse(pa[++i]);
                            cp = cmd == "H" ? new CadPoint(x, cp.Y) : new CadPoint(cp.X + x, cp.Y);
                            pm.AddLine(ConvertPathPoint(cp));
                        }
                        break;
                        case "v":
                        case "V":
                        {
                            var y = double.Parse(pa[++i]);
                            cp = cmd == "V" ? new CadPoint(cp.X, y) : new CadPoint(cp.X, cp.Y + y);
                            pm.AddLine(ConvertPathPoint(cp));
                        }
                        break;
                        case "Z":
                        case "z":
                        {
                            pm.EndPath(true);
                        }
                        break;
                        case "A":
                        case "a":
                        {
                            var rx = double.Parse(pa[++i]);
                            var ry = double.Parse(pa[++i]);
                            var xAxisRotation = double.Parse(pa[++i]);
                            var largeArcFlag = int.Parse(pa[++i]);
                            var sweepFlag = int.Parse(pa[++i]);
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            var p = cp;
                            cp = cmd == "A" ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            var d = CurveConverter.ArcDirection.Left;
                            //円弧の向きはCadでは逆になることに注意
                            if (largeArcFlag == 1)
                            {
                                d = sweepFlag == 0 ? CurveConverter.ArcDirection.RightLarge : CurveConverter.ArcDirection.LeftLarge;
                            } else
                            {
                                d = sweepFlag == 0 ? CurveConverter.ArcDirection.Right : CurveConverter.ArcDirection.Left;
                            }
                            var f = ry / rx;
                            var angle = CadMath.DegToRad(xAxisRotation);
                            var p1 = cp - p;
                            p1.Rotate(-angle);
                            p1.Y /= f;
                            var p2 = p1.Copy();
                            //始点を基準にした円になった
                            var a2 = rx * rx - CadPoint.Dot(p1, p1) / 4;
                            if (a2 < 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Svg path A: small radius rx:{rx} ry:{ry}");
                                rx = p1.Hypot() * 0.5;
                                ry = rx * f;
                            }
                            pm.AddArc(rx, ry / rx, ConvertPathAngle(xAxisRotation), ConvertPathPoint(cp), d);
                        }
                        break;
                        case "Q":
                        case "q":
                        {
                            //2次ベジェ
                            var f = cmd == "Q";
                            var p = cp;
                            var xc = double.Parse(pa[++i]);
                            var yc = double.Parse(pa[++i]);
                            cp = f ? new CadPoint(xc, yc) : new CadPoint(cp.X + xc, cp.Y + yc);
                            var ct = cp;
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = f ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.AddBezier(ConvertPathPoint(ct), ConvertPathPoint(ct), ConvertPathPoint(cp));
                            //                            pts.AddRange(Beziers.CreateBezier2(p, ct, cp, 16, true));
                            lastCt = ct;
                            isBesier2Draw = true;
                        }
                        break;
                        case "T":
                        case "t":
                        {
                            //2次ベジェ。制御点は前回の点対称
                            var p = cp;
                            var ct = isBesier2Ciontinue ? lastCt.RotatePoint(p, Math.PI) : p;
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = cmd == "T" ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.AddBezier(ConvertPathPoint(ct), ConvertPathPoint(ct), ConvertPathPoint(cp));
                            lastCt = ct;
                            isBesier2Draw = true;
                        }
                        break;
                        case "C":
                        case "c":
                        {
                            //3次ベジェ
                            var f = cmd == "C";
                            var p = cp;//始点
                            var xc1 = double.Parse(pa[++i]);
                            var yc1 = double.Parse(pa[++i]);
                            var ct1 = f ? new CadPoint(xc1, yc1) : new CadPoint(cp.X + xc1, cp.Y + yc1);
                            //                            var ct1 = cp;
                            var xc2 = double.Parse(pa[++i]);
                            var yc2 = double.Parse(pa[++i]);
                            var ct2 = f ? new CadPoint(xc2, yc2) : new CadPoint(cp.X + xc2, cp.Y + yc2);
                            //                            var ct2 = cp;
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = f ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.AddBezier(ConvertPathPoint(ct1), ConvertPathPoint(ct2), ConvertPathPoint(cp));
                            lastCt = ct2;
                            isBesier3Draw = true;
                        }
                        break;
                        case "S":
                        case "s":
                        {
                            //3次ベジェ。制御点は前回の点対称
                            var f = cmd == "S";
                            var p = cp;
                            var ct1 = isBesier3Ciontinue ? lastCt.RotatePoint(p, Math.PI) : p;
                            var xc2 = double.Parse(pa[++i]);
                            var yc2 = double.Parse(pa[++i]);
                            var ct2 = f ? new CadPoint(xc2, yc2) : new CadPoint(cp.X + xc2, cp.Y + yc2);
                            //                            var ct2 = cp;
                            var x = double.Parse(pa[++i]);
                            var y = double.Parse(pa[++i]);
                            cp = f ? new CadPoint(x, y) : new CadPoint(cp.X + x, cp.Y + y);
                            pm.AddBezier(ConvertPathPoint(ct1), ConvertPathPoint(ct2), ConvertPathPoint(cp));
                            lastCt = ct2;
                            isBesier3Draw = true;
                        }
                        break;

                        default:
                            cmd = "";
                            break;
                    }
                    isBesier2Ciontinue = isBesier2Draw; ;
                    isBesier3Ciontinue = isBesier3Draw;
                }
            });
            var pathShape = new SvgPath(element);
            pathShape.PathList.AddRange(pathList);
            pathShape.LineColor = GetColor(element, "stroke");
            pathShape.FillColor = GetColor(element, "fill");
            return pathShape;

        }

        //static List<CadPoint>? CreatePath(SvgElement element, List<CadPoint> pts, bool loop)
        //{
        //    PolylineHelper.Degenerate(pts, loop);
        //    if (pts.Count < 2) return null;
        //    var s = new List<CadPoint>();
        //    foreach (var p in pts)
        //    {
        //        s.Add(p.Copy());
        //    }
        //    if (loop) s.Add(pts[0].Copy());

        //    return s;
        //}


        static CadPoint ConvertPathPoint(CadPoint p) => new CadPoint(p.X, p.Y);

        static double ConvertPathAngle(double a) => a;


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
