using System.Text.RegularExpressions;
using System.Xml;

namespace SvgHelper {
    public abstract class SvgElement {
        public string NodeName { get; }
        public SvgAttributes Attributes { get; } = new();
        public SvgElement? Parent { get; private set; }


        public SvgElement(string nodeName, SvgElement? parent)
        {
            NodeName = nodeName;
            Parent = parent;
        }

        public string? ID => Attributes.GetAttribute("id");
        public string? Class => Attributes.GetAttribute("class");
        public CadMath2D.TransformMatrix TransformMatrix => Attributes.TransformMatrix;

        public abstract void Read(XmlReader reader);

        public virtual void Write(XmlWriter writer, string? ns = null)
        {
            writer.WriteStartElement(NodeName, ns);
            Attributes.Write(writer);
            WriteBody(writer);
            writer.WriteEndElement();
        }

        protected virtual void WriteBody(XmlWriter writer) { }

        public SvgElement? SetParent(SvgElement? newElement)
        {
            var p = Parent;
            Parent = newElement;
            return p;
        }

        public string? GetAttribute(string name, bool inherited)
        {
            string? v = null;
            if (Attributes.Styles.TryGetValue(name, out v)) return v;
            if (ID != null)
            {
                if (SvgShapeContainer.IdStyleMap.TryGetValue(ID, out var styles))
                {
                    if (styles.TryGetValue(name, out v)) return v;
                }
            }
            if (Class != null)
            {
                if (SvgShapeContainer.ClassStyleMap.TryGetValue(Class, out var styles))
                {
                    if (styles.TryGetValue(name, out v)) return v;
                }
            }

            v = Attributes.GetAttribute(name);
            if (v != null) return v;
            if (inherited && Parent != null) return Parent.GetAttribute(name, inherited);
            return DefaultAttributes.TryGetValue(name, out v) ? v : null;
        }

        public double GetDouble(string name, double deafultValue = 0, bool inherited = false)
        {
            var a = GetAttribute(name, inherited);
            if (a != null && Double.TryParse(a, out var v)) return v;
            return deafultValue;
        }

        public void SetDouble(string name, double value)
        {
            Attributes.SetAttribute(name, value.ToString());
        }

        public (double x, double y) GetPoint(string keyX, string keyY)
        {
            return (GetDouble(keyX), GetDouble(keyY));
        }
        public void SetPoint(string keyX, string keyY, double x, double y)
        {
            SetDouble(keyX, x);
            SetDouble(keyY, y);
        }

        public void SetAttribute(string name, string value)
        {
            Attributes.SetAttribute(name, value);
        }

        public List<(double x, double y)> GetPoints(string name)
        {
            var a = new List<(double x, double y)>();
            var pts = GetAttribute(name, false);
            if (pts == null) return a;
            var sp = Regex.Matches(pts, @"[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\s*,\s*[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?");
            foreach (Match s in sp)
            {
                var ss = s.Value.Split(',');
                if (ss.Length == 2) a.Add((double.Parse(ss[0]), double.Parse(ss[1])));
            }
            return a;
        }

        public CadMath2D.TransformMatrix GetMultipliedTransformMatrix()
        {
            if (Parent == null) return Attributes.TransformMatrix;
            return Attributes.TransformMatrix * Parent.GetMultipliedTransformMatrix();
        }

        public double GetMultipliedOpacity()
        {
            if (Parent == null) return Attributes.Opacity;
            return Attributes.Opacity * Parent.GetMultipliedOpacity();
        }

        protected string ReadText(XmlReader reader, string nodeName)
        {
            var text = "";
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        //<tspan>などをここで処理すべき
                        break;
                    case XmlNodeType.Text:
                        text += reader.Value.Trim();//改行やスペースが入るから1trim()
                        break;
                    case XmlNodeType.EndElement: //要素の終了(閉じタグなど)
                        if (reader.Name == nodeName) return text;
                        break;
                    case XmlNodeType.Comment:
                        System.Diagnostics.Debug.WriteLine("Comment " + reader.Value);
                        break;
                }
            }
            return text;
        }

        static readonly IReadOnlyDictionary<string, string> DefaultAttributes =
            new Dictionary<string, string>()
            {
                { "fill", "#000000"},
                { "fill-opacity", "1"},
                { "fill-rule", "nonzero"},
                { "font-family", "monospace"},
                { "font-size", "12"},
                //{ "font-size", "medium"},
                { "font-style", "normal"},
                { "font-weight", "normal"},
                { "letter-spacing", "normal"},

                { "stroke", "none"},
                { "stroke-dasharray", "none"},
                { "stroke-dashoffset", "0"},
                { "stroke-linecap", "butt"},
                { "stroke-linejoin", "miter"},
                { "stroke-miterlimit", "4"},
                { "stroke-opacity", "1"},
                { "stroke-width", "1"},

                { "text-anchor", "start"},
        };
    }
}