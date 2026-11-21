using System; using System.IO;
namespace Digitalizar { static class T { static void Main(string[] a) {
 var raw = File.ReadAllBytes(a[0]); int w = BitConverter.ToInt32(raw,0), h = BitConverter.ToInt32(raw,4);
 var bgr = new byte[w*h*3]; Array.Copy(raw,8,bgr,0,bgr.Length);
 var sw = System.Diagnostics.Stopwatch.StartNew(); int ow, oh; string log;
 var r = Otimizar.Executar(bgr,w,h,out ow,out oh,out log);
 Console.WriteLine("ms="+sw.ElapsedMilliseconds+" "+(r==null?"SEM MUDANCA":ow+"x"+oh)+" "+log);
 if (r!=null) File.WriteAllBytes(a[1], Cat(Cat(BitConverter.GetBytes(ow),BitConverter.GetBytes(oh)),r));
}
public static byte[] Cat(byte[] x, byte[] y){ var z=new byte[x.Length+y.Length]; Array.Copy(x,z,x.Length); Array.Copy(y,0,z,x.Length,y.Length); return z; } }}
