import io.github.giosepeluiz.digitalizar.Visao;
import javax.imageio.ImageIO; import java.awt.image.BufferedImage; import java.io.*; import java.nio.file.*;
public class Teste {
  public static void main(String[] a) throws Exception {
    String js=new String(Files.readAllBytes(Paths.get("casos.json")));
    for (int k=0;k<20;k++){
      File f=new File("caso"+k+".jpg"); if(!f.exists()) break;
      BufferedImage im=ImageIO.read(f); int w=im.getWidth(),h=im.getHeight();
      int[] px=im.getRGB(0,0,w,h,null,0,w);
      long t0=System.nanoTime();
      int[] t=new int[3]; int[] g=Visao.cinzaReduzido(px,w,h,320,t);
      Visao.Quad q=Visao.detectar(g,t[0],t[1]);
      long t1=System.nanoTime();
      if(q==null){System.out.println("caso"+k+": NADA"); continue;}
      Visao.Quad qs=q.escalar(t[2],t[2]);
      int[] t2=new int[3]; int[] g2=Visao.cinzaReduzido(px,w,h,1400,t2);
      Visao.Quad qr=Visao.refinar(q.escalar(t[2]/(float)t2[2],t[2]/(float)t2[2]),g2,t2[0],t2[1]).escalar(t2[2],t2[2]);
      long t3=System.nanoTime();
      // erro vs verdade
      int i0=js.indexOf("\"caso"+k+".jpg\""); int qi=js.indexOf("\"q\": [[",i0)+7; String sub=js.substring(qi,js.indexOf("]]",qi));
      String[] nums=sub.replace("[","").replace("]","").split(",");
      double e1=0,e2=0;
      for(int i=0;i<4;i++){double gx=Double.parseDouble(nums[2*i].trim()),gy=Double.parseDouble(nums[2*i+1].trim());
        double b1=1e9,b2=1e9; for(int j=0;j<4;j++){b1=Math.min(b1,Math.hypot(qs.x[j]-gx,qs.y[j]-gy));b2=Math.min(b2,Math.hypot(qr.x[j]-gx,qr.y[j]-gy));}
        e1=Math.max(e1,b1);e2=Math.max(e2,b2);}
      System.out.printf("caso%d conf=%.2f erro max: bruto=%.1fpx refinado=%.1fpx (detecao %dms, refino %dms)%n",k,q.confianca,e1,e2,(t1-t0)/1000000,(t3-t1)/1000000);
      int[] tam=Visao.ajustarAoPapel(Visao.tamanhoSaida(qr),21,29.7);
      int[] o=Visao.endireitar(px,w,h,qr,tam[0],tam[1]);
      int[] o2=o.clone(); Visao.realcar(o2,tam[0],tam[1],k%3);
      long t4=System.nanoTime();
      BufferedImage b=new BufferedImage(tam[0]*2,tam[1],BufferedImage.TYPE_INT_RGB);
      b.setRGB(0,0,tam[0],tam[1],o,0,tam[0]); b.setRGB(tam[0],0,tam[0],tam[1],o2,0,tam[0]);
      ImageIO.write(b,"jpg",new File("saida"+k+".jpg"));
      System.out.printf("   saida %dx%d realce %dms%n",tam[0],tam[1],(t4-t3)/1000000);
    }
  }
}
