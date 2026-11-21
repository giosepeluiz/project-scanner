package io.github.giosepeluiz.digitalizar;
import java.nio.file.*;
public class TO { public static void main(String[] a) throws Exception {
 byte[] raw=Files.readAllBytes(Paths.get(a[0]));
 int w=(raw[0]&255)|(raw[1]&255)<<8|(raw[2]&255)<<16, h=(raw[4]&255)|(raw[5]&255)<<8|(raw[6]&255)<<16;
 byte[] d=java.util.Arrays.copyOfRange(raw,8,raw.length);
 long t=System.currentTimeMillis(); Otimizar.Img r=Otimizar.executar(d,w,h);
 System.out.println("ms="+(System.currentTimeMillis()-t)+" "+(r==null?"SEM MUDANCA":r.w+"x"+r.h+" "+r.log));
 if(r!=null){ byte[] o=new byte[8+r.rgb.length]; for(int i=0;i<4;i++){o[i]=(byte)(r.w>>(8*i)); o[4+i]=(byte)(r.h>>(8*i));} System.arraycopy(r.rgb,0,o,8,r.rgb.length); Files.write(Paths.get(a[1]),o);} }}
