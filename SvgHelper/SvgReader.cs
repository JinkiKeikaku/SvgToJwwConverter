using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SvgHelper {
    public class SvgReader {
        public SvgReader()
        {
        }

        public void Read(Stream stream, Action<SvgShapeContainer> completed)
        {
            XmlReaderSettings settings = new();
            settings.DtdProcessing = DtdProcessing.Parse;// Ignore;
            Debug.IndentSize = 2;
            Debug.IndentLevel = 0;
            SvgShapeContainer.IdElementMap.Clear();
            SvgShapeContainer.IdStyleMap.Clear();
            SvgShapeContainer.ClassStyleMap.Clear();

            var reader = XmlReader.Create(stream, settings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                    {
                        Debug.WriteLine($"Element {reader.Name}");
                        Debug.IndentLevel += 1;
                        if (reader.IsEmptyElement) goto case XmlNodeType.EndElement;
                        if (reader.Name == "svg")
                        {
                            var root = SvgShapeContainer.CreateInstance(reader, reader.Name, null);
                            root.Read(reader);
                            completed(root);
                        }
                    }
                    break;
                    case XmlNodeType.Text:
                        Debug.WriteLine($"EndElement {reader.Value}");
                        break;
                    case XmlNodeType.EndElement: //要素の終了(閉じタグなど)
                        Debug.IndentLevel -= 1;
                        Debug.WriteLine($"EndElement {reader.Name}");
                        break;
                    case XmlNodeType.Comment:
                        Debug.WriteLine($"Comment {reader.Value}");
                        break;
                }
            }
        }
    }
}
