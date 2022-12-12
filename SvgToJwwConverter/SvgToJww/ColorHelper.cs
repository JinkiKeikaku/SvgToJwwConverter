using System;
using System.Drawing;

namespace SvgToJwwConverter.SvgToJww {
    static class ColorHelper
    {
        /// <summary>
        /// HTMLの色文字列をColorに変換する。変換できない場合はColor.Black。
        /// </summary>
        public static Color HtmlToColor(string htmlColor)
        {
            var htmlLowerCase = htmlColor.ToLower().Trim();
            try
            {
                if (htmlLowerCase.StartsWith("rgb"))
                {
                    return ArgbToColor(htmlLowerCase);
                }
                else if (htmlLowerCase.StartsWith("#"))
                {
                    return HexToColor(htmlLowerCase);
                }
                else
                {
                    // Fallback to ColorTranslator for named colors, e.g. "Black", "White" etc.
                    return ColorTranslator.FromHtml(htmlLowerCase);
                }
            }
            catch
            {
                // ColorTranslator throws System.Exception, don't really care what the actual error is.
            }

            return Color.Black;
        }

        private static Color HexToColor(string htmlLowerCase)
        {
            var len = htmlLowerCase.Length;

            // #RGB
            if (len == 4)
            {
                var r = Convert.ToInt32(htmlLowerCase.Substring(1, 1), 16);
                var g = Convert.ToInt32(htmlLowerCase.Substring(2, 1), 16);
                var b = Convert.ToInt32(htmlLowerCase.Substring(3, 1), 16);

                return Color.FromArgb(r + r * 16, g + g * 16, b + b * 16);
            }

            // #RGBA
            else if (len == 5)
            {
                var r = Convert.ToInt32(htmlLowerCase.Substring(1, 1), 16);
                var g = Convert.ToInt32(htmlLowerCase.Substring(2, 1), 16);
                var b = Convert.ToInt32(htmlLowerCase.Substring(3, 1), 16);
                var a = Convert.ToInt32(htmlLowerCase.Substring(4, 1), 16);

                return Color.FromArgb(a + a * 16, r + r * 16, g + g * 16, b + b * 16);
            }

            // #RRGGBB
            else if (len == 7)
            {
                return Color.FromArgb(
                    Convert.ToInt32(htmlLowerCase.Substring(1, 2), 16),
                    Convert.ToInt32(htmlLowerCase.Substring(3, 2), 16),
                    Convert.ToInt32(htmlLowerCase.Substring(5, 2), 16));
            }

            // #RRGGBBAA
            else if (len == 9)
            {
                return Color.FromArgb(
                    Convert.ToInt32(htmlLowerCase.Substring(7, 2), 16),
                    Convert.ToInt32(htmlLowerCase.Substring(1, 2), 16),
                    Convert.ToInt32(htmlLowerCase.Substring(3, 2), 16),
                    Convert.ToInt32(htmlLowerCase.Substring(5, 2), 16));
            }

            return Color.Empty;
        }

        private static Color ArgbToColor(string htmlLowerCase)
        {
            int left = htmlLowerCase.IndexOf('(');
            int right = htmlLowerCase.IndexOf(')');

            if (left < 0 || right < 0)
            {
                return Color.Empty;
            }

            string noBrackets = htmlLowerCase.Substring(left + 1, right - left - 1);

            string[] parts = noBrackets.Split(',');

            int r = int.Parse(parts[0]);
            int g = int.Parse(parts[1]);
            int b = int.Parse(parts[2]);

            if (parts.Length == 3)
            {
                return Color.FromArgb(r, g, b);
            }
            else if (parts.Length == 4)
            {
                float a = float.Parse(parts[3]);
                return Color.FromArgb((int)(a * 255), r, g, b);
            }
            return Color.Empty;
        }
    }
}
