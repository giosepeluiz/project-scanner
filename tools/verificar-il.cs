using System; using System.Reflection; using System.Reflection.Emit; using System.Collections.Generic; using System.Linq;
static class Il { static void Main(string[] a){
 var asm=Assembly.LoadFrom(a[0]); var ops=new Dictionary<short,OpCode>();
 foreach(var f in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)){var o=(OpCode)f.GetValue(null);ops[o.Value]=o;}
 var seen=new SortedSet<string>();
 foreach(var t in asm.GetTypes()) foreach(var m in t.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(t.GetConstructors(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))){
  MethodBody b; try{b=m.GetMethodBody();}catch{continue;} if(b==null)continue; var il=b.GetILAsByteArray(); if(il==null)continue;
  int i=0; while(i<il.Length){ short v=il[i++]; if(v==0xFE) v=(short)(0xFE00|il[i++]); OpCode op; if(!ops.TryGetValue(v,out op))break;
   int sz; switch(op.OperandType){case OperandType.InlineNone:sz=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:sz=1;break;case OperandType.InlineVar:sz=2;break;case OperandType.InlineI8:case OperandType.InlineR:sz=8;break;case OperandType.InlineSwitch:int n=BitConverter.ToInt32(il,i);sz=4+4*n;break;default:sz=4;break;}
   if(op==OpCodes.Call||op==OpCodes.Callvirt||op==OpCodes.Newobj){ try{ var mb=asm.ManifestModule.ResolveMethod(BitConverter.ToInt32(il,i)); var dt=mb.DeclaringType; if(dt!=null && dt.Assembly!=asm) seen.Add(dt.FullName+"::"+mb.Name+"("+string.Join(",",mb.GetParameters().Select(p=>p.ParameterType.Name))+")"); }catch{} }
   i+=sz; } }
 foreach(var s in seen) Console.WriteLine(s); } }
