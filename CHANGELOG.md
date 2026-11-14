# 📜 Changelog

Todas as mudanças do **Digitalizar**, versão por versão.
O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e as versões seguem o [Versionamento Semântico](https://semver.org/lang/pt-BR/).

> As versões 2.0 e 3.0 foram etapas intermediárias. O que elas trouxeram está descrito aqui, e os downloads estão nas releases **v2.1.0** e **v3.1.0**, que já incluem tudo.

---

## [3.3.1] – 2026-11-14 · Preto e branco mais fiel e edição que vale em qualquer modo 🖋️

### Corrigido
- **Windows: o programa fechava ao abrir e a verificação de atualização dava erro.** O executável publicado na 3.3.0 foi compilado com o Mono e chamava métodos que o .NET Framework do Windows não tem (`Split`/`TrimEnd` com um caractere). Agora o código usa só chamadas compatíveis e o pacote é compilado com o compilador do Windows.
- **Preto e branco perdia traços finos e claros** (lápis, caneta fraca): o limiar fixo transformava cinza-claro em branco. O preto e branco agora é **adaptativo**: cada ponto é comparado com o papel em volta, então linhas e textos finos continuam pretos, mesmo com papel cinza ou sombra.
- **A edição rápida não surtia efeito no preto e branco**, porque recebia a imagem já convertida. Agora o editor ajusta o **original em cores** e converte para o modo do documento no fim. O resultado vai para o arquivo salvo, para a cópia (Ctrl+V) e para o PDF.
- Depois de editar, o original colorido é mantido: dá para trocar o modo de cor de novo sem perder qualidade.

### Adicionado
- Um erro inesperado agora grava o `erro.log` ao lado do programa e mostra uma mensagem, em vez de o programa fechar sozinho.
- Fluxo do GitHub Actions que compila, assina (SignPath Foundation, quando configurada) e publica as versões do PC. Veja [CODE_SIGNING.md](CODE_SIGNING.md).

---

## [3.3.0] – 2026-11-11 · "Várias páginas e sempre atualizado" 📄

### Adicionado
- **Trocar o modo de cor depois de digitalizar** (Colorido, Tons de cinza e Preto e branco), como no Android. O scanner passa a digitalizar sempre em cores; o programa guarda o original colorido e converte na hora. Em JPEG e PNG o arquivo salvo é regravado com o novo modo; no PDF a mudança espera o **Salvar PDF**.
- **PDF de várias páginas**, só quando o tipo de arquivo é **PDF**:
  - Aparece uma **faixa de páginas** com miniaturas, o botão **+** para digitalizar a próxima página e o **×** para excluir uma página (com confirmação; a última página não pode ser excluída).
  - O botão **Novo PDF** começa um documento novo.
  - **O PDF não é salvo sozinho:** as páginas se juntam e o arquivo só é gravado no botão **Salvar PDF**. Ao começar outro PDF, digitalizar em JPEG/PNG ou fechar o programa com um PDF não salvo, o programa pergunta se quer salvar. (JPEG e PNG continuam salvando sozinhos.)
  - Com **duas páginas ou mais**, o tipo de arquivo fica **travado em PDF**; ao sobrar uma página só, destrava.
  - Com JPEG ou PNG, a faixa some e cada digitalização continua gerando um arquivo.
  - O editor rápido abre a página escolhida e devolve o resultado para ela.
- **Atualização dentro do programa**, direto das releases do GitHub:
  - Verificação **automática** ao abrir (pode ser desligada em **Opções → Atualizações**) e **manual**, com o botão **Verificar agora**.
  - Um aviso mostra a nova versão e as **notas da versão**, com **Atualizar agora**, **Depois** e **Pular esta versão**.
  - O programa baixa o pacote do seu sistema (Windows ou Linux), troca o executável e reinicia sozinho. Se algo falhar, abre a página da versão no navegador.
- Traduções dos novos textos nos 15 idiomas.

### Corrigido
- **3.2.1:** a pasta padrão de destino estava fixa num caminho de usuário. Agora é `Documentos/Scanned Documents` (Windows) ou `~/Scanned Documents` (Linux) do usuário que abre o programa.
- A janela de Opções agora cabe em telas baixas (tem rolagem).

### Observações
- Editar uma imagem em cinza ou preto e branco faz dela o novo "original": a cor dessa página não volta mais.
- PDFs de várias páginas em 600 ou 1200 DPI usam bastante memória. Em 1200 DPI a digitalização é sempre colorida e o arquivo fica muito grande.
- A atualização usa a API pública do GitHub, que limita as consultas por rede (60 por hora). Se o limite for atingido, o programa avisa e tenta de novo depois.
- Não foi possível testar em scanner real nem no Windows; os testes foram feitos no Linux com um scanner simulado.

## [3.2.0] – 2026-11-07 · "Fala a sua língua" 🌍

### Adicionado
- **Idiomas:** a interface está traduzida para 15 idiomas.
  - Na primeira vez, o programa usa o idioma do sistema (`CurrentUICulture`). Se ele não estiver disponível, usa inglês.
  - Ordem nas **Opções → Idioma**: Idioma do sistema, Português (Brasil), Português (Portugal), English (United States), English (United Kingdom), Español, um separador e depois Deutsch, Français, Italiano, Nederlands, Polski, Русский, Türkçe, Українська, 日本語 e 中文（简体）.
  - O idioma novo é aplicado ao reiniciar, com o botão **Reiniciar agora**.
  - As traduções ficam em `src/idiomas.tsv`, uma coluna por idioma; um texto que falte usa o inglês.
- **Preferências guardadas:** além do tema e da cor, o `config.ini` guarda o idioma, o scanner (pelo nome), o tamanho do documento, o tipo de arquivo, o modo de cor, a resolução, a pasta de destino, o painel recolhido ou expandido, o tamanho, a posição e o estado da janela (normal/maximizada) e o valor do Recorte rápido.
- Separadores no menu dos seletores.

### Alterado
- Os seletores mostram os valores traduzidos e guardam o valor interno, por isso a configuração não depende do idioma.
- A janela só volta para a posição guardada se ela ainda estiver visível num dos monitores.

### Corrigido
- O texto "Redes e contato" aparecia com a grafia de Portugal na versão pt-BR.

---

## [3.1.0] – 2026-11-07 · "Muito prazer!" 🐧

### Adicionado
- **Versão para Linux.** O mesmo executável roda com o Mono e usa o **SANE** (`scanimage`) para falar com o scanner.
  - Lista os scanners com `scanimage -L` e digitaliza no tamanho do documento escolhido (`-x`/`-y` em mm).
  - O preto e branco é capturado em cinza e convertido pelo programa, o que funciona em qualquer driver.
  - Usa a barra de título do próprio Linux.
  - "Compartilhar", "Abrir" e "Abrir pasta" usam o `xdg-open`.
  - Os ícones da interface passam a ser símbolos Unicode equivalentes (☰ ⚙ ✎ ↗ …).
  - O pacote traz `digitalizar.sh` (verifica o Mono e o SANE) e `instalar-atalho.sh` (cria o atalho no menu de aplicativos).
- Janela **Sobre**: autor, redes sociais, contato, versão, data de compilação, plataforma, sistema, tecnologia de digitalização, formatos e pasta de configurações. O botão **Copiar informações** copia tudo para a área de transferência.
- Atalho **F1** para abrir o Sobre; botão **Sobre o Digitalizar** dentro das Opções.
- Autor e direitos autorais nas propriedades do `.exe` (Detalhes).

### Alterado
- A pasta padrão no Linux é `~/Scanned Documents`.

---

## [3.0.0] – 2026-11-07 · "Cara nova" 🎨

### Adicionado
- **Barra de título integrada** ao estilo do Chrome, feita com `WM_NCCALCSIZE`/`WM_NCHITTEST`.
  - Mantém o arraste, o duplo clique, o encaixe nas bordas (Aero Snap), a sombra e os cantos arredondados do Windows 11.
  - O botão maximizar devolve `HTMAXBUTTON`, por isso os *layouts* de encaixe do Windows 11 aparecem ao passar o mouse.
  - Os botões minimizar, maximizar e fechar seguem o padrão do Windows 11 (fechar fica vermelho ao passar o mouse).
- **Painel lateral recolhível** (botão ☰) com uma animação *ease-out* de 220 ms; recolhido, mostra só o botão de digitalizar.
- **Tamanho do documento**: A4 (padrão), A5, A6, B5, B6 (ISO 216), Carta, Executivo (EUA), Foto 10 × 15, Foto 13 × 18, Cartão de visita e Área máxima do vidro. A área é enviada ao WIA (extensões em pixels, limitadas ao máximo do scanner); se o driver ignorar o pedido, a imagem é cortada a partir do canto de referência.
- **Opções** (⚙):
  - tema **Claro**, **Escuro** ou **Usar o tema do Windows**, que acompanha a mudança do sistema em tempo real;
  - **cor de destaque**: 10 cores (Azul, Turquesa, Verde, Lima, Âmbar, Laranja, Vermelho, Rosa, Roxo, Grafite) ou a cor do Windows, com tons próprios para claro e escuro;
  - as configurações ficam guardadas em `%APPDATA%\Digitalizar\config.ini`.
- **Compartilhar**: abre a janela de partilha do Windows (verbo `Windows.ModernShare`), com fallback para o Explorador.
- Barras de rolagem e contorno da janela seguem o tema escuro.

### Alterado
- **Ícone minimalista** (folha + linha de leitura), redesenhado para cada tamanho: 16, 20, 24, 32, 40, 48, 64, 96, 128 e 256 px.
- Botões de ação com os ícones do Windows 11 (Segoe Fluent Icons / MDL2); em janelas estreitas, mostram só o ícone, com o nome numa dica.
- Todos os controles passaram a desenhar com a paleta do tema atual.

---

## [2.1.0] – 2026-11-07 · "Ajustes finos" ✂️

### Adicionado
- **Recorte rápido** na Edição rápida: um switch (desligado por padrão) que mostra um campo de pixels (padrão **3 px**) e o botão **Aplicar**. Corta a mesma medida em todas as bordas, a partir do recorte atual, e a medida é contada na resolução original.
- Controle **Interruptor** (switch) no estilo Windows 11, usado também no **Limiar**.

### Alterado
- Ícone redesenhado no estilo Fluent.
- Barra lateral principal de 360 → 410 px e painel do editor de 350 → 400 px.

### Corrigido
- O texto dos itens do menu dos seletores aparecia deslocado na vertical; agora fica centrado e alinhado com o indicador da opção selecionada.

---

## [2.0.0] – 2026-11-07 · "Agora dá para editar" 🖌️

### Adicionado
- **Edição rápida**:
  - **Girar**: −180° a +180°, com encaixe a cada 45° e botões de ±90°;
  - **Recortar**: arrastar, mover, redimensionar pelos cantos e regra dos terços;
  - **Brilho** e **Contraste**;
  - **Níveis**: entrada preto/branco, gama, saída preto/branco;
  - **Curvas**: interpolação cúbica monotônica (Fritsch–Carlson) e histograma ao fundo;
  - **Limiar**: preto e branco puro;
  - **Salvar** e **Salvar como…** (JPEG, PNG, PDF). A pré-visualização é calculada numa cópia reduzida e o resultado final na resolução original;
  - atalhos Ctrl+S, Ctrl+Shift+S, Enter e Esc; duplo clique num slider volta ao padrão.
- Seletores em formato de card no estilo Windows 11, com menu escuro.
- Botões arredondados, barra de progresso fina e pré-visualização.
- Primeiro ícone do programa.

---

## [1.0.0] – 2026-11-07 · "O começo de tudo" 🌱

### Adicionado
- Executável portátil em C# (Windows Forms + .NET Framework 4, que já vem no Windows), com cerca de 34 KB.
- Detecção de scanners pelo **WIA**, com seleção automática da HP.
- Fonte: área do vidro.
- Formatos: **JPEG** (padrão), **PNG** e **PDF** (gerador de PDF próprio: JPEG para cor/cinza, 1 bit com compressão Flate para preto e branco).
- Modos: **Cor**, **Cinza** e **Preto e branco** (padrão).
- Resoluções: 75, 150, **300** (padrão), 600 e 1200 DPI, com aviso para 1200 DPI em cor.
- Pasta padrão *Documentos › Scanned Documents*, com opção de escolher outra.
- Ações depois de digitalizar: **Copiar imagem**, **Abrir** e **Abrir pasta**.
- `compilar.bat`: recompila usando o `csc.exe` que já vem no Windows.

[3.3.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v3.3.0
[3.2.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v3.2.0
[3.1.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v3.1.0
[3.0.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v3.1.0
[2.1.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v2.1.0
[2.0.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v2.1.0
[1.0.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/v1.0.0
