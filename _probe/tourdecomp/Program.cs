using System;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
class P {
  static void Main() {
    var d = new CSharpDecompiler(@"D:\SteamLibrary\steamapps\common\Descenders\Descenders_Data\Managed\Assembly-CSharp.dll",
      new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
    foreach (var name in new[] { "ItemRigidbody", "PhotonRigidbodyView" }) {
      Console.WriteLine("======== " + name + " ========");
      try {
        string code = d.DecompileTypeAsString(new FullTypeName(name));
        // print first ~3500 chars + any Serialize/Photon/network mentions
        Console.WriteLine(code.Length > 4000 ? code.Substring(0, 4000) : code);
        Console.WriteLine("--- hits ---");
        foreach (var k in new[] { "Photon", "Serialize", "network", "velocity", "isMine", "RPC", "Owner" }) {
          int i = 0, n = 0;
          while ((i = code.IndexOf(k, i, StringComparison.OrdinalIgnoreCase)) >= 0 && n < 3) {
            int a = Math.Max(0, i - 60); int len = Math.Min(160, code.Length - a);
            Console.WriteLine(code.Substring(a, len).Replace("\r"," ").Replace("\n"," "));
            Console.WriteLine("====");
            i++; n++;
          }
        }
      } catch (Exception ex) { Console.WriteLine(ex.Message); }
    }
  }
}
