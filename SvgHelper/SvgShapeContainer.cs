using System.Text.RegularExpressions;
using System.Xml;

namespace SvgHelper {

    public class SvgShapeContainer : SvgElement {
        private CancellationToken? mCancelToken;
        public static Dictionary<string, SvgElement> IdElementMap { get; } = new();
        public static Dictionary<string, Dictionary<string, string>> IdStyleMap { get; } = new();
        public static Dictionary<string, Dictionary<string, string>> ClassStyleMap { get; } = new();
        public static string CssStyleString = "";

        /// <summary>
        /// インスタンス作成
        /// </summary>
        public static SvgShapeContainer CreateInstance(
            XmlReader reader, string nodeName, SvgShapeContainer? parent, CancellationToken? cancelToken
        )
        {
            var c = new SvgShapeContainer(nodeName, parent, cancelToken);
            c.Initialize(reader);
            return c;
        }

        protected SvgShapeContainer(
            string nodeName, SvgElement? parent, CancellationToken? cancelToken
        ) : base(nodeName, parent)
        {
            mCancelToken = cancelToken;
        }

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
                    default:
                        break;
                }
            }
        }

        protected override void WriteBody(XmlWriter writer)
        {
            foreach (var s in ShapeElementList)
            {
                s.Write(writer);
                if (mCancelToken?.IsCancellationRequested == true)
                {
                    mCancelToken?.ThrowIfCancellationRequested();
                }
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
                        var defsContainer = CreateInstance(reader, name, this, mCancelToken);
                        defsContainer.Read(reader);
                    }
                    break;
                case "g":
                {
                    if (!reader.IsEmptyElement)
                    {
                        element = CreateInstance(reader, name, this, mCancelToken);
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
                        } else if (key.StartsWith('.'))
                        {
                            var styles = new Dictionary<string, string>();
                            var sa = Regex.Split(value[1..], @" *; *");
                            foreach (var a in sa)
                            {
                                var aa = Regex.Split(a.Trim(), @" *: *");
                                if (aa.Length == 2) styles[aa[0]] = aa[1];
                            }
                            ClassStyleMap[key[1..]] = styles;
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
