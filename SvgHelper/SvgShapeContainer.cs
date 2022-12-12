using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace SvgHelper {
    public class SvgShapeContainer : SvgElement
    {
        public static Dictionary<string, SvgElement> IdElementMap { get; } = new();
        public static Dictionary<string, Dictionary<string, string>> IdStyleMap { get; } = new();
        public static Dictionary<string, Dictionary<string, string>> ClassStyleMap { get; } = new();

        public static string CssStyleString = "";
        public static SvgShapeContainer CreateInstance(
            XmlReader reader, string nodeName, SvgShapeContainer parent
        )
        {
            var c = new SvgShapeContainer(nodeName, parent);
            c.Initialize(reader);
            return c;
        }
        public static SvgShapeContainer CreateInstance(
            string nodeName, SvgShapeContainer parent
        )
        {
            var c = new SvgShapeContainer(nodeName, parent);
            return c;
        }

        protected SvgShapeContainer(string nodeName, SvgElement parent) : base(nodeName, parent) { }
        public List<SvgElement> ShapeElementList { get; } = new();

        protected virtual void Initialize(XmlReader reader)
        {
            Attributes.ReadFromXmlReader(reader);
        }

        override public void Read(XmlReader reader)
        {
            if (reader.IsEmptyElement) return;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var element = CreateElement(reader);
                        if (element != null)
                        {
                            ShapeElementList.Add(element);
                            var id = element.ID;
                            if (id != null) IdElementMap[id] = element;
                        }
                        break;
                    case XmlNodeType.Text:
                        System.Diagnostics.Debug.WriteLine("Text " + reader.Value);
                        break;
                    case XmlNodeType.EndElement: //要素の終了(閉じタグなど)
                        if (reader.Name == NodeName) return;
                        break;

                    case XmlNodeType.Comment:
                        System.Diagnostics.Debug.WriteLine("Comment " + reader.Value);
                        break;
                }
            }
        }

        protected override void WriteBody(XmlWriter writer)
        {
            foreach(var s in ShapeElementList)
            {
                s.Write(writer);
            }
        }

        SvgElement? CreateElement(XmlReader reader)
        {
            var name = reader.Name;
            SvgElement? element = null;
            switch (name)
            {
                case "defs":
                    if (!reader.IsEmptyElement)
                    {
                        //defsはShapeElementListに追加しない。IDも使わない。そのためelementに代入しない。
                        var defsContainer = CreateInstance(reader, name, this);
                        defsContainer.Read(reader);
                    }
                    break;
                case "g":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            element = CreateInstance(reader, name, this);
                            element.Read(reader);
                        }
                    }
                    break;
                case "style":
                    {
                        if (reader.IsEmptyElement) break;
                        var attrs = new SvgAttributes();
                        attrs.ReadFromXmlReader(reader);
                        if (attrs.GetAttribute("type") != "text/css") break;
                        CssStyleString = ReadText(reader, name);
                        CssStyleString = CssStyleString.Replace("\r", "").Replace("\n", "");
                        var matches = Regex.Matches(CssStyleString, @"#.+?{.+?}|\..+?{.+?}");
                        foreach (Match m in matches)
                        {
                            var s = m.Value;
                            var key = s.Substring(0, s.IndexOf('{'));
                            var value = s[s.IndexOf('{')..s.IndexOf('}')];
//                        var value = s[s.IndexOf('{')..^1];
                        if (key.StartsWith('#'))
                            {
                                var styles = new Dictionary<string, string>();
                                var sa = Regex.Split(value[1..], @" *; *");
                                foreach (var a in sa)
                                {
                                    var aa = Regex.Split(a.Trim(), @" *: *");
                                    if (aa.Length == 2) styles[aa[0]] = aa[1];
                                }
                                IdStyleMap[key[1..]] = styles;
                            }
                            else if (key.StartsWith('.'))
                            {
                                var styles = new Dictionary<string, string>();
                                var sa = Regex.Split(value[1..], @" *; *");
                                foreach (var a in sa)
                                {
                                    var aa = Regex.Split(a.Trim(), @" *: *");
                                    if (aa.Length == 2) styles[aa[0]] = aa[1];
                                }
                                ClassStyleMap[key[1..]] = styles;
                                //ClassStyleMap.Add(key[1..], value[1..]);
                            }
                        }
                    }
                    break;
                default:
                    {
                        element = new SvgShapeElement(name, this);
                        element.Read(reader);
                    }
                    break;
            }
            return element;
        }
    }

}
