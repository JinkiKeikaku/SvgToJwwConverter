using CadMath2D;
using JwwHelper;
using SvgHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SvgToJwwConverter.SvgToJww {
    static class Converter {
        public static void ConvertToJww(
            string outPath, SvgShapeContainer container, 
            CancellationToken cancelToken, Action<string> processMessage)
        {
            var writer = new JwwHelper.JwwWriter();
            var tmp = Path.GetTempFileName();
            var buf = Properties.Resources.template;
            File.WriteAllBytes(tmp, buf);
            writer.InitHeader(tmp);
            writer.Header.m_adScale[0] = 1.0;
            writer.Header.m_nBitMapFirstDraw = 0;

            File.Delete(tmp);
            var jwwPaperSize = JwwPaper.GetJwwPaperSize(writer.Header.m_nZumen);

            foreach (var element in container.ShapeElementList)
            {
                switch (element)
                {
                    case SvgShapeElement shape:
                    {
                        var m = shape.GetMultipliedTransformMatrix();
                        var sa = ShapeConverter.CreateJwwShape(
                            shape, ConvertToJww(m, jwwPaperSize), cancelToken, processMessage);
                        AddJwwData(writer, sa);
                    }
                    break;
                    case SvgShapeContainer shapeContainer:
                    {
                        var m = shapeContainer.GetMultipliedTransformMatrix();
                        var sa = ShapeConverter.CreateJwwShape(
                            shapeContainer, ConvertToJww(m, jwwPaperSize), cancelToken, processMessage);
                        AddJwwData(writer, sa);
                    }
                    break;
                }
            }
            writer.Write(outPath);
        }
        private static void AddJwwData(JwwWriter writer, IReadOnlyList<JwwData> datas)
        {
            foreach (var data in datas)
            {
                writer.AddData(data);
            }
        }



        private static TransformMatrix ConvertToJww(TransformMatrix m, JwwPaper paper)
        {
            double originX = paper.Width / 2;
            double originY = paper.Height / 2;
            var a = m.Copy();
            a.C = -a.C;
            a.D = -a.D;
            a.Tx -= originX;
            a.Ty = -a.Ty + originY;
            return a;
        }
    }
}
