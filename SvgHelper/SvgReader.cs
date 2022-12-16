using System.Diagnostics;
using System.Xml;

namespace SvgHelper {
    public class SvgReader {
        public SvgReader()
        {
        }
        /// <summary>
        /// SVG読み込み
        /// </summary>
        /// <param name="stream">ファイルストリーム</param>
        /// <param name="completed">読み込み完了後この関数がコールされます。
        /// completedの引数にはsvgのルート要素が入ります。</param>
        /// <param name="cancelToken">非同期処理でキャンセルに使います。</param>
        public void Read(
            Stream stream, Action<SvgShapeContainer> completed, 
            CancellationToken? cancelToken)
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
                            var root = SvgShapeContainer.CreateInstance(reader, reader.Name, null, cancelToken);
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
                    default:
                        break;
                }
                if (cancelToken?.IsCancellationRequested == true)
                {
                    cancelToken?.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
