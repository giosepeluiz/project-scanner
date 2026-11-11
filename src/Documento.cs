// Documento aberto: uma ou várias páginas digitalizadas em cor, que podem mudar de modo (cor, cinza, P&B) depois
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Digitalizar
{
    /// Uma página: guarda a digitalização original (em cor) e gera as versões a partir dela
    class Pagina : IDisposable
    {
        public Bitmap Original;      // sempre em cor, direto do scanner (ou o resultado da edição rápida)
        public Bitmap Atual;         // original no modo do documento (só fica na memória enquanto a página está selecionada)
        public Bitmap Mini;          // miniatura para a faixa de páginas
        public int Dpi;
        public PaginaPdf Pdf;        // cópia já codificada para o PDF (apagada quando o modo muda)
        public string ModoRender;    // modo em que Atual e Mini foram geradas

        public Pagina(Bitmap original, int dpi, string modo)
        {
            Original = original;
            Dpi = dpi;
            Original.SetResolution(dpi, dpi);
            Atual = Imagens.Renderizar(Original, modo);
            ModoRender = modo;
            RefazerMini(modo);
        }

        /// Refaz Atual e Mini se a página foi gerada em outro modo
        public void Aplicar(string modo)
        {
            if (ModoRender == modo) return;
            if (Atual != null) Atual.Dispose();
            Atual = Imagens.Renderizar(Original, modo);
            ModoRender = modo;
            Pdf = null;
            RefazerMini(modo);
        }

        public void RefazerMini(string modo)
        {
            if (Mini != null) Mini.Dispose();
            using (var pequena = Imagens.Reduzir(Original, 280))
            {
                Mini = Imagens.Renderizar(pequena, modo);
            }
        }

        public void Dispose()
        {
            if (Original != null) Original.Dispose();
            if (Atual != null) Atual.Dispose();
            if (Mini != null) Mini.Dispose();
            Original = Atual = Mini = null;
        }
    }

    class Documento : IDisposable
    {
        public readonly string Tipo;                 // JPEG, PNG ou PDF (o tipo escolhido quando o documento foi criado)
        public string Modo;                          // Cor, Cinza ou Preto e branco
        public string Caminho;                       // arquivo salvo (null até o primeiro salvamento)
        public bool Pendente;                        // PDF com mudanças que ainda não foram gravadas (só o PDF espera o botão Salvar)
        public readonly List<Pagina> Paginas = new List<Pagina>();
        int atual;

        public Documento(string tipo, string modo) { Tipo = tipo; Modo = modo; }

        public bool EhPdf { get { return Tipo == "PDF"; } }
        public int Indice { get { return atual; } }
        public Pagina PaginaAtual { get { return Paginas.Count == 0 ? null : Paginas[atual]; } }
        public Bitmap ImagemAtual { get { var p = PaginaAtual; return p == null ? null : p.Atual; } }

        /// Mostra outra página: libera a imagem grande da anterior e renderiza a nova
        public void Selecionar(int i)
        {
            if (i < 0 || i >= Paginas.Count) return;
            if (Paginas.Count > 0 && atual < Paginas.Count && atual != i)
            {
                var ant = Paginas[atual];
                if (ant.Atual != null) { ant.Atual.Dispose(); ant.Atual = null; }
            }
            atual = i;
            Garantir(Paginas[i]);
        }

        void Garantir(Pagina p)
        {
            if (p.Atual == null) p.Atual = Imagens.Renderizar(p.Original, Modo);
        }

        public void Adicionar(Pagina p)
        {
            if (Paginas.Count > 0 && atual < Paginas.Count && Paginas[atual].Atual != null)
            {
                Paginas[atual].Atual.Dispose();
                Paginas[atual].Atual = null;
            }
            p.Aplicar(Modo);                    // a página nasceu no modo que estava escolhido no início da digitalização
            Paginas.Add(p);
            atual = Paginas.Count - 1;
            Pendente = EhPdf;
        }

        /// Exclui uma página (a última não pode ser excluída)
        public bool Excluir(int i)
        {
            if (Paginas.Count <= 1 || i < 0 || i >= Paginas.Count) return false;
            bool eraAtual = i == atual;
            Paginas[i].Dispose();
            Paginas.RemoveAt(i);
            if (i < atual) atual--;
            if (atual >= Paginas.Count) atual = Paginas.Count - 1;
            if (eraAtual) Garantir(Paginas[atual]);
            Pendente = EhPdf;
            return true;
        }

        /// Troca o modo de todas as páginas, sempre a partir do original em cor
        public void MudarModo(string modo)
        {
            if (modo == Modo) return;
            Modo = modo;
            Pendente = EhPdf;
            for (int i = 0; i < Paginas.Count; i++)
            {
                var p = Paginas[i];
                p.Pdf = null;
                p.ModoRender = modo;
                p.RefazerMini(modo);
                if (i == atual)
                {
                    if (p.Atual != null) p.Atual.Dispose();
                    p.Atual = Imagens.Renderizar(p.Original, modo);
                }
                else if (p.Atual != null) { p.Atual.Dispose(); p.Atual = null; }
            }
        }

        /// A edição rápida devolveu uma nova imagem para a página atual: ela vira o novo original
        public void SubstituirAtual(Bitmap editada)
        {
            var p = PaginaAtual;
            if (p == null) return;
            int dpi = (int)Math.Round(editada.HorizontalResolution);
            if (dpi < 30) dpi = p.Dpi;
            if (p.Original != null) p.Original.Dispose();
            if (p.Atual != null) p.Atual.Dispose();
            p.Original = Imagens.Copia24(editada);
            p.Original.SetResolution(dpi, dpi);
            p.Dpi = dpi;
            p.Atual = editada;                  // a imagem editada já está no modo atual
            p.ModoRender = Modo;
            p.Pdf = null;
            p.RefazerMini(Modo);
            Pendente = EhPdf;
        }

        /// Grava no disco: JPEG/PNG gravam a imagem atual; PDF grava todas as páginas num só arquivo
        public void Salvar(string pasta)
        {
            if (Paginas.Count == 0) return;
            Pendente = false;
            if (Caminho == null) Caminho = Imagens.NovoCaminho(pasta, Tipo);
            if (EhPdf)
            {
                var lista = new List<PaginaPdf>();
                foreach (var p in Paginas)
                {
                    if (p.Pdf == null)
                    {
                        if (p.Atual != null) p.Pdf = Pdf.Codificar(p.Atual, Modo, p.Dpi);
                        else using (var tmp = Imagens.Renderizar(p.Original, Modo)) p.Pdf = Pdf.Codificar(tmp, Modo, p.Dpi);
                    }
                    lista.Add(p.Pdf);
                }
                Imagens.GuardarPdf(Caminho, lista);
            }
            else
            {
                var p = Paginas[0];
                Imagens.GuardarEm(p.Atual, Caminho, p.Dpi);
            }
        }

        public void Dispose()
        {
            foreach (var p in Paginas) p.Dispose();
            Paginas.Clear();
        }
    }
}
