using CadMath2D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace SvgHelper {
    public class SvgAttributes {
        double? mOpacity = null;
        TransformMatrix? mTransform = null;
        Dictionary<string, string>? mStyles = null;

        public SvgAttributes()
        {
        }

        public SvgAttributes Copy()
        {
            var s = new SvgAttributes();
            foreach (var a in Attributes)
            {
                s.Attributes.Add(a.Key, a.Value);
            }
            return s;
        }

        public void ReadFromXmlReader(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    Debug.WriteLine($"SvgAttributes:ReadFromXmlReader {reader.Name:reader.Value}");
                    Attributes[reader.Name] = reader.Value;
                }
                reader.MoveToElement();
            }
        }

        public void Write(XmlWriter writer)
        {
            foreach (var a in Attributes)
            {
                writer.WriteStartAttribute(a.Key);
                writer.WriteValue(a.Value);
                writer.WriteEndAttribute();
            }
        }

        /// <summary>
        /// このアトリビュートの透明度。階層をめぐってトータルの透明度は
        /// GetMultipliedOpacity()を使うこと。
        /// </summary>
        public double Opacity {
            get {
                if (mOpacity == null)
                {
                    mOpacity = 1.0;
                    var a = GetAttribute("opacity");
                    if (a != null && Double.TryParse(a, out var v)) mOpacity = v;
                }
                return (double)mOpacity;
            }
        }

        /// <summary>
        /// このアトリビュートの変換行列。階層をめぐってトータルの変換行列は
        /// GetMultipliedTransformMatrix()を使うこと。
        /// </summary>
        public TransformMatrix TransformMatrix {
            get {
                if (mTransform == null)
                {
                    var t = GetAttribute("transform");
                    mTransform = t != null ? CreateTransformMatrix(t) : new TransformMatrix();
                }
                return mTransform;
            }
        }

        /// <summary>
        /// 属性
        /// </summary>
        Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>();

        /// <summary>
        /// style属性
        /// </summary>
        public Dictionary<string, string> Styles {
            get {
                if (mStyles == null)
                {
                    mStyles = new Dictionary<string, string>();
                    var s = GetAttribute("style");
                    if (s != null)
                    {
                        var sa = Regex.Split(s, @" *; *");
                        foreach (var a in sa)
                        {
                            var aa = Regex.Split(a.Trim(), @" *: *");
                            if (aa.Length == 2) mStyles[aa[0]] = aa[1];
                        }
                    }
                }
                return mStyles;
            }
        }


        public string? GetAttribute(string name)
        {
            string? v = null;
            if (Attributes.TryGetValue(name, out v)) return v;
            return null;
        }

        public void SetAttribute(string name, string value)
        {
            Attributes[name] = value;
        }


        TransformMatrix CreateTransformMatrix(string value)
        {
            TransformMatrix t = new();
            MatchCollection ss = Regex.Matches(value, @"[a-z]+?\s*\(.+?\)");
            for (var i = 0; i < ss.Count; i++)
            {
                var sa = ss[i].Value.Split('(', ')');
                if (sa.Length < 2) continue;
                var cmd = sa[0].Trim();
                var args = Array.ConvertAll(Regex.Split(sa[1], @"[\s,]+"), Double.Parse);
                switch (cmd)
                {
                    case "matrix":
                        if (args.Length == 6)
                        {
                            t.Transform(args[0], args[2], args[1], args[3], args[4], args[5]);
                        }
                        break;
                    case "rotate":
                    {
                        switch (args.Length)
                        {
                            case 1:
                                t.Rotate(CadMath.DegToRad(args[0]));
                                break;
                            case 3:
                            {
                                t.Translate(-args[1], -args[2]);
                                t.Rotate(CadMath.DegToRad(args[0]));
                                t.Translate(args[1], args[2]);
                            }
                            break;
                        }
                    }
                    break;
                    case "translate":
                    {
                        if (args.Length == 2) t.Translate(args[0], args[1]);
                        //if (args.Length == 2) t.Translate(args[0], -args[1]);
                    }
                    break;
                    case "scale":
                    {
                        if (args.Length == 2) t.Scale(args[0], args[1]);
                    }
                    break;
                    case "skewX":
                    {
                        if (args.Length == 1)
                        {
                            var sk = Math.Tan(CadMath.DegToRad(args[0]));
                            t.Transform(1.0, -sk, 0, 1, 0, 0);
                        }
                    }
                    break;
                    case "skewY":
                    {
                        if (args.Length == 1)
                        {
                            var sk = Math.Tan(CadMath.DegToRad(args[0]));
                            t.Transform(1.0, 0.0, -sk, 1, 0, 0);
                        }
                    }
                    break;
                }
            }
            return t;
        }

    }
}
