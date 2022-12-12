using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SvgToJwwConverter {
    public static class Utility {

        public static string Title => $"{Properties.Resources.AppName} {Utility.AppVersion}";

        /// <summary>
        /// バージョン文字列の取得
        /// </summary>
        public static string AppVersion {
            get {

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var v = asm?.GetName()?.Version;
                if (v == null) return "";
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        public static void LoadJwwDll()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyDir != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                    if (e.Name.StartsWith("JwwHelper,", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = Path.Combine(assemblyDir,
                        string.Format("JwwHelper_{0}.dll", (IntPtr.Size == 4) ? "x86" : "x64"));
                        return Assembly.LoadFile(fileName);
                    }
                    return null;
                };
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

    }
}
