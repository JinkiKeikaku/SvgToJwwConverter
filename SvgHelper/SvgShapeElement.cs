using System.Xml;

namespace SvgHelper
{
    public class SvgShapeElement : SvgElement
    {
        public string Text="";
        public SvgShapeElement(string nodeName, SvgElement? parent): base(nodeName, parent){}

        public override void Read(XmlReader reader)
        {
            Attributes.ReadFromXmlReader(reader);
            Text = !reader.IsEmptyElement ? ReadText(reader, NodeName) : "";
        }
        protected override void WriteBody(XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(Text)) writer.WriteString(Text);
        }
    }
}
