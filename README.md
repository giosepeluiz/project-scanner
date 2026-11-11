<div align="center">

<img src="assets/icons/Digitalizar_256.png" width="128" alt="Ícone do Digitalizar">

# Project Scanner · Digitalizar

### Digitalize, ajuste e compartilhe em segundos, sem instalar nada.

[![Versão](https://img.shields.io/badge/versão-3.3.0-4cc2ff?style=for-the-badge)](https://github.com/giosepeluiz/project-scanner/releases/latest)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078d4?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/giosepeluiz/project-scanner/releases/latest)
[![Linux](https://img.shields.io/badge/Linux-SANE-fcc624?style=for-the-badge&logo=linux&logoColor=black)](https://github.com/giosepeluiz/project-scanner/releases/latest)
[![Android](https://img.shields.io/badge/Android-1.3.0-3ddc84?style=for-the-badge&logo=android&logoColor=white)](https://github.com/giosepeluiz/project-scanner/releases/tag/android-v1.3.0)
[![Licença MIT](https://img.shields.io/badge/licença-MIT-6ccb5f?style=for-the-badge)](LICENSE)

**[⬇️ Baixar](#️-download)** · **[📖 Como usar](#-como-funciona-passo-a-passo)** · **[🕰️ Versões](#️-histórico-de-versões)** · **[🤝 Contribuir](#-código-aberto-contribua)**

<img src="docs/screenshots/01-principal.png" width="860" alt="Tela principal do Digitalizar">

</div>

> 🇬🇧 **English summary:** *Digitalizar* is a free, portable scanning app for Windows (WIA) and Linux (SANE). Scan to JPEG, PNG or PDF, copy the image right after scanning, and crop, rotate or fix it in a built-in quick editor. Available in 15 languages (it starts in your system language) and remembers all your settings. No installation needed. There is also an **Android version** (network scanners + camera scan), see the [`android/`](android/) folder. Open source under the MIT license.

---

## 👋 Olá!

Existem vários programas de digitalização por aí, e eu sei disso. O **Digitalizar** nasceu de uma necessidade pessoal: eu precisava de alguns recursos que os outros programas não tinham, e não precisava de outros tantos que deixavam esses programas pesados, lentos e cheios de telas.

Então ele é, de propósito, **um aplicativo simples, com o básico bem feito**: você abre, escolhe as opções, digitaliza e pronto. O arquivo é salvo automaticamente e você já pode **copiar a imagem** para colar onde quiser (WhatsApp, e-mail, Affinity, Word…) logo depois de digitalizar, sem precisar abrir o arquivo.

Com o tempo ele ganhou um editor rápido para corrigir a imagem, visual moderno no estilo do Windows 11, tema claro e escuro, cores de destaque e até uma versão para Linux. E continua leve: cabe num pendrive e não instala nada no seu computador.

## ✨ O que ele faz

| | |
|---|---|
| 🖨️ **Encontra o scanner sozinho** | Detecta impressoras multifuncionais e scanners ligados por USB ou Wi-Fi |
| 📄 **Tamanhos de papel padrão** | A4, A5, A6, B5, B6, Carta, Executivo, fotos 10 × 15 e 13 × 18, cartão de visita ou área máxima do vidro |
| 🎨 **Cor, cinza ou preto e branco** | De 75 a 1200 DPI |
| 💾 **JPEG, PNG ou PDF** | Salvo automaticamente na pasta que você escolher |
| 📋 **Copiar logo depois de digitalizar** | É só colar com Ctrl+V onde quiser |
| ✂️ **Edição rápida** | Recortar, girar, brilho, contraste, níveis, curvas e limiar, com pré-visualização ao vivo |
| ↗️ **Compartilhar** | Abre a janela de compartilhamento do Windows |
| 🌗 **Tema claro, escuro ou do sistema** | Mais 10 cores de destaque (ou a cor do próprio Windows) |
| 📄 **PDF de várias páginas** | Com o tipo **PDF**, adicione páginas, exclua e comece outro PDF; faixa de miniaturas |
| 🎨 **Trocar o modo de cor depois** | Colorido, tons de cinza ou preto e branco mesmo depois de digitalizar |
| 🔄 **Atualização no programa** | Aviso de nova versão com as notas, verificação automática ou manual |
| 🌍 **15 idiomas** | Abre no idioma do sistema; português (Brasil e Portugal), inglês, espanhol e mais 10 nas Opções |
| 💾 **Lembra tudo** | Idioma, tema, cor, scanner, tamanho, formato, resolução, pasta, painel e janela ficam guardados |
| 🎒 **Portátil** | Um único arquivo, sem instalação |

## ⬇️ Download

Pegue a versão mais recente em **[Releases](https://github.com/giosepeluiz/project-scanner/releases/latest)**:

| Sistema | Arquivo | Como usar |
|---|---|---|
| 🪟 **Windows 10/11** | [`Digitalizar-3.3.0-windows.zip`](https://github.com/giosepeluiz/project-scanner/releases/download/v3.3.0/Digitalizar-3.3.0-windows.zip) | Descompacte e abra o `Digitalizar.exe`. Pronto! |
| 🐧 **Linux** | [`Digitalizar-3.3.0-linux.tar.gz`](https://github.com/giosepeluiz/project-scanner/releases/download/v3.3.0/Digitalizar-3.3.0-linux.tar.gz) | Descompacte e rode `./digitalizar.sh` (veja abaixo) |
| 📱 **Android 10+** | [`Digitalizar-1.3.0-android.apk`](https://github.com/giosepeluiz/project-scanner/releases/download/android-v1.3.0/Digitalizar-1.3.0-android.apk) | Baixe no celular e toque para instalar. Veja a [versão Android](#-versão-para-android) |

> [!WARNING]
> **Na primeira vez, o Windows pode mostrar um aviso azul ("O Windows protegeu o computador").** Isso acontece com todo programa sem assinatura digital paga. Clique em **Mais informações → Executar assim mesmo**. O código está todo aqui, aberto, para quem quiser conferir.
>
> 💡 **Dica:** clique com o botão direito no `.zip` baixado → **Propriedades** → marque **Desbloquear** → OK, e só depois extraia. Assim o aviso nem aparece. O hash SHA-256 de cada arquivo está nos anexos da release, para você conferir. A [política de assinatura de código](CODE_SIGNING.md) explica como estamos tratando isso.

### 🔏 Assinatura de código

Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

O Digitalizar usa a assinatura de código gratuita da **SignPath Foundation** para projetos de código aberto. As versões assinadas serão publicadas aqui, em Releases, e compiladas automaticamente pelo GitHub Actions. Veja a [política de assinatura](CODE_SIGNING.md).

### 🐧 No Linux

Você precisa do **Mono** (para rodar o programa) e do **SANE** (para falar com o scanner):

```bash
sudo apt install mono-complete sane-utils    # Debian / Ubuntu / Mint
sudo apt install hplip                       # se o scanner for HP (ex.: DeskJet 2700)

tar -xzf Digitalizar-3.3.0-linux.tar.gz
cd Digitalizar
./digitalizar.sh                             # abre o programa
./instalar-atalho.sh                         # (opcional) cria o atalho no menu
```

> As versões **1.0 e 2.x são só para Windows**. A versão para **Linux** existe a partir da **3.1** (a mais recente é a **3.3**).

## 📱 Versão para Android

O **Digitalizar para Android** (versão **1.3.0**, que acompanha as funções da 3.3 do PC) fica na pasta [`android/`](android/) e tem release própria, com a tag `android-v1.3.0`. Ele faz tudo o que o PC faz com scanners de rede e **também digitaliza pela câmera**: um detector de documentos acha a folha, endireita e transforma a foto num scan.

| | 🖥️ PC | 📱 Android |
|---|---|---|
| Origem | Scanner USB ou de rede (WIA / SANE) | Scanner de rede (eSCL / AirScan) **ou câmera** |
| Salvar | Automático (o PDF espera o **Salvar PDF**) | **Só ao tocar em "Salvar"** |
| PDF | **Várias páginas** (só com PDF), com adicionar e excluir | **Várias páginas** (só com PDF), com adicionar e excluir |
| Cor | **Pode trocar depois** de digitalizar | **Pode trocar depois** de digitalizar |
| Atualização | **Dentro do programa**, pelo GitHub | **Dentro do app**, pelo GitHub |
| Linguagem | C# | Java puro, sem AndroidX |

A comparação completa está no **[README do Android](android/README.md)** e o histórico no **[CHANGELOG do Android](android/CHANGELOG.md)**.

## 📖 Como funciona, passo a passo

### 1. Digitalizar

<img src="docs/screenshots/02-tamanhos.png" width="760" alt="Escolha do tamanho do documento">

1. Coloque o documento no vidro, encostado no **canto de referência** (normalmente marcado com uma setinha).
2. Escolha o **Scanner**. Se a impressora estava desligada, ligue-a e clique em **Detectar**.
3. Escolha o **Tamanho do documento**. O programa digitaliza só aquela área, o que é mais rápido e gera arquivos menores.
4. Escolha o **Tipo de arquivo**, o **Modo de cor** e a **Resolução**. Uma dica: 300 DPI é ótimo para documentos e 600 para desenhos com detalhes finos.
5. Clique em **Digitalizar**. A imagem aparece à direita e é **salva automaticamente** com um nome tipo `Scan_2026-11-07_124300.jpg`. Depois de digitalizar, ainda dá para **trocar o Modo de cor**: o arquivo é regravado. Com o tipo **PDF**, aparece a faixa de páginas: use o **+** para digitalizar outra página (todas vão para o mesmo PDF), **Salvar PDF** para gravar o arquivo (o PDF não salva sozinho) e **Novo PDF** para começar outro. Com duas páginas ou mais o tipo fica travado em PDF.

Para trocar a pasta de destino, clique no card **Salvar arquivo em**; o link **Padrão** volta para a pasta original.

### 2. Depois de digitalizar

| Botão | O que faz |
|---|---|
| ✎ **Edição rápida** | Abre o editor (ou dê duplo clique na pré-visualização) |
| ❐ **Copiar imagem** | Copia para a área de transferência: é só colar com Ctrl+V |
| ↗ **Compartilhar** | Abre a janela de compartilhamento do Windows (e-mail, Teams, Bluetooth, apps…) |
| **Abrir** | Abre o arquivo no visualizador padrão |
| **Abrir pasta** | Abre a pasta com o arquivo já selecionado |

### 3. Edição rápida

<img src="docs/screenshots/05-recorte.png" width="760" alt="Edição rápida: recorte">

- **Girar:** slider de −180° a +180° que **encaixa sozinho a cada 45°**, mais botões para girar 90° para cada lado.
- **Recortar:** clique em *Recortar*, arraste na imagem para marcar a área, mova o retângulo ou ajuste pelos cantos, e conclua com **Enter**.

<img src="docs/screenshots/06-recorte-rapido-limiar.png" width="760" alt="Recorte rápido">

- **Recorte rápido:** ligue o switch, escolha quantos pixels cortar (o padrão é **3 px**) e clique em **Aplicar**. Ele corta a mesma medida em todas as bordas, ótimo para tirar aquela sombra preta que o scanner deixa na borda. Cada clique corta mais um pouco.
- **Brilho e contraste.**

<img src="docs/screenshots/07-niveis-curvas.png" width="760" alt="Níveis e curvas">

- **Níveis:** ponto preto e branco de entrada, tons médios (gama) e ponto preto e branco de saída.
- **Curvas:** clique na curva para criar pontos e arraste; o botão direito remove um ponto. O histograma da imagem aparece ao fundo para ajudar.
- **Limiar:** transforma a imagem em preto e branco puro, perfeito para desenhos feitos à mão.
- **Salvar** substitui o arquivo; **Salvar como…** grava com outro nome ou noutro formato (JPEG, PNG ou PDF).

> 💡 A pré-visualização é rápida, mas ao salvar as edições são aplicadas na **resolução original**, sem perder qualidade. Dê duplo clique num slider para voltar ao valor padrão.

### 4. Personalizar

<p>
<img src="docs/screenshots/03-painel-recolhido.png" width="420" alt="Painel recolhido">
<img src="docs/screenshots/04-tema-claro.png" width="420" alt="Tema claro">
</p>

- **☰** recolhe e expande o painel lateral, com uma animação suave. Recolhido, sobra mais espaço para a imagem e fica só o botão de digitalizar.
- **⚙ Opções:**
  - **Atualizações:** o programa procura novas versões no GitHub ao abrir (dá para desligar) e o botão **Verificar agora** faz a busca na hora. Quando há versão nova, aparece um aviso com as notas; **Atualizar agora** baixa, troca o programa e reinicia.
  - **Idioma:** na primeira vez, o programa abre no idioma do sistema. Nas Opções dá para escolher outro: no topo da lista ficam Português (Brasil), Português (Portugal), English (US e UK) e Español; depois de um traço vêm Deutsch, Français, Italiano, Nederlands, Polski, Русский, Türkçe, Українська, 日本語 e 中文. O novo idioma vale ao reiniciar (há um botão **Reiniciar agora**).
  - **Tema:** claro, escuro ou do Windows. Muda na hora.
  - **Cor de destaque:** 10 cores ou a cor do Windows. Muda na hora.

> 💾 **Tudo o que você escolhe fica guardado:** idioma, tema, cor, scanner, tamanho do documento, tipo de arquivo, modo de cor, resolução, pasta, painel recolhido ou expandido, tamanho e posição da janela e o valor do Recorte rápido. Os padrões (A4, JPEG, Preto e branco, 300 DPI) só valem na primeira vez que o programa é aberto.

<p>
<img src="docs/screenshots/08-opcoes.png" width="340" alt="Opções">
<img src="docs/screenshots/09-sobre.png" width="340" alt="Sobre">
</p>

- **Sobre (F1):** autoria, contato e informações técnicas, com um botão para copiá-las.

### ⌨️ Atalhos

| Tecla | Ação |
|---|---|
| `F1` | Abre a janela Sobre |
| `Ctrl + S` | Salvar (no editor) |
| `Ctrl + Shift + S` | Salvar como (no editor) |
| `Enter` | Concluir o recorte |
| `Esc` | Sair do recorte / fechar a janela |
| Duplo clique num slider | Voltar ao valor padrão |

Quer mais detalhes? Veja o **[Manual completo](docs/MANUAL.md)**.

## 🕰️ Histórico de versões

### 🚀 Versão 3.x: "Cara nova" + "Muito prazer!" + "Fala a sua língua" + "Sempre atualizado"
**Releases: [v3.3.0](https://github.com/giosepeluiz/project-scanner/releases/tag/v3.3.0) (mais recente) · [v3.2.0](https://github.com/giosepeluiz/project-scanner/releases/tag/v3.2.0) · [v3.1.0](https://github.com/giosepeluiz/project-scanner/releases/tag/v3.1.0)** · 🪟 Windows + 🐧 Linux

A maior atualização até agora. O programa passou a parecer (e se comportar como) um app nativo do Windows 11, e ainda ganhou uma versão para Linux.

**O que chegou na 3.0:**
- **Barra de título integrada**, como no Chrome: o menu, o nome do programa e as opções ficam dentro da própria barra. E continua tudo funcionando: arrastar, duplo clique para maximizar, encaixar nas bordas e os *layouts* de encaixe do Windows 11 ao passar o mouse no botão maximizar.
- **Painel lateral recolhível** com uma animação suave.
- **Tamanho do documento:** 11 tamanhos padrão (ISO 216 e norte-americanos), com medidas em cm. O programa só digitaliza aquela área.
- **Opções:** tema claro, escuro ou do sistema, e 10 cores de destaque (ou a do Windows). Cada cor tem um tom pensado para o modo claro e outro para o escuro.
- **Compartilhar:** a janela de partilha do Windows, direto do programa.
- **Ícone novo e minimalista**, redesenhado para cada tamanho (16 a 256 px), para ficar nítido em qualquer lugar.
- Botões com os ícones do Windows 11; quando a janela fica estreita, eles viram só ícone.

**O que a 3.1 acrescentou:**
- Janela **Sobre**: quem criou, redes sociais, contato e todas as informações técnicas, com um botão para copiá-las (útil se precisar relatar um problema).
- Atalho **F1** para abrir o Sobre.
- O nome do autor aparece nas propriedades do `.exe` (botão direito → Propriedades → Detalhes).
- 🐧 **Estreia no Linux!** O mesmo programa roda com o Mono e usa o SANE para falar com o scanner. As diferenças: usa a barra de título do próprio Linux, e o "Compartilhar" abre a pasta do arquivo.

**O que a 3.2 acrescentou:** 🌍
- **15 idiomas.** O programa abre no idioma do sistema (ou em inglês, se o idioma do sistema não estiver na lista), e dá para trocar nas Opções. Os principais ficam no topo: Português (Brasil), Português (Portugal), English (US), English (UK) e Español. Depois vêm Deutsch, Français, Italiano, Nederlands, Polski, Русский, Türkçe, Українська, 日本語 e 中文 (simplificado).
- **O programa lembra de tudo:** idioma, tema, cor, scanner, tamanho, tipo, modo de cor, resolução, pasta, painel recolhido ou expandido, tamanho e posição da janela (inclusive maximizada) e o valor do Recorte rápido.
- Nas Opções, o tema "do sistema" aparece com o nome certo no Linux.

**O que a 3.3 acrescentou:** 📄
- **Trocar o modo de cor depois de digitalizar:** Colorido, Tons de cinza ou Preto e branco. O scanner digitaliza sempre em cores e o programa converte na hora. Em JPEG e PNG o arquivo é regravado.
- **PDF de várias páginas** (só com o tipo PDF): faixa de miniaturas, **+** para digitalizar a próxima página, **×** para excluir e **Novo PDF** para recomeçar. O PDF só é gravado em **Salvar PDF** (o programa avisa se houver algo sem salvar) e, com duas páginas ou mais, o tipo de arquivo fica travado em PDF.
- **Atualização dentro do programa:** ao abrir, ele procura novas versões no GitHub (dá para desligar nas Opções) e mostra um aviso com as **notas da versão**. Também dá para verificar à mão em **Opções → Atualizações**. **Atualizar agora** baixa, troca o programa e reinicia.
- A pasta padrão de destino deixou de ficar presa a um usuário específico.
- 📱 No mesmo pacote de novidades, o **Android 1.3.0** ganhou a atualização dentro do app e a mesma regra de várias páginas só com PDF.

> Por que **3.2** e não 4.0? Pelo [Versionamento Semântico](https://semver.org/lang/pt-BR/), um recurso novo que não quebra nada do que já existia aumenta o número do meio (*minor*). O 4.0 fica para uma mudança realmente grande.

### 🖌️ Versão 2.x: "Agora dá para editar" + "Ajustes finos"
**Release: [v2.1.0](https://github.com/giosepeluiz/project-scanner/releases/tag/v2.1.0)** · 🪟 Windows

**O que chegou na 2.0:**
- A **Edição rápida**: recortar, girar (encaixando a cada 45°), brilho, contraste, níveis, curvas e limiar, com **Salvar** e **Salvar como**.
- Seletores com cara de Windows 11: cards integrados ao app, com seta azul e menu escuro.
- Botões arredondados, barra de progresso fina e pré-visualização maior.
- O primeiro ícone próprio do programa.

**O que a 2.1 acrescentou:**
- **Recorte rápido:** um switch que, ligado, corta X pixels de todas as bordas com um clique (3 px por padrão).
- O texto do menu dos seletores estava descentrado na vertical; foi corrigido.
- Painéis laterais um pouco mais largos, para respirar melhor.
- O limiar ganhou um switch no estilo Windows 11.
- O ícone foi redesenhado no estilo Fluent.

### 🌱 Versão 1.0: "O começo de tudo"
**Release: [v1.0.0](https://github.com/giosepeluiz/project-scanner/releases/tag/v1.0.0)** · 🪟 Windows

A primeira versão: um executável de apenas **34 KB** que funciona sem instalar nada.
- Reconhece o scanner pelo **WIA**, o mesmo sistema do app "Digitalizar" do Windows.
- Fonte: área do vidro. Tipos: JPEG, PNG e PDF. Modos: cor, cinza e preto e branco. Resoluções de 75 a 1200 DPI.
- Salva na pasta *Documentos › Scanned Documents*, com opção de escolher outra.
- Depois de digitalizar: **Copiar imagem**, **Abrir** e **Abrir pasta**.

📜 O histórico completo, com todos os detalhes técnicos, está no **[CHANGELOG](CHANGELOG.md)**.

## 🛠️ Para desenvolvedores

O projeto é escrito em **C#** (Windows Forms) e é compatível com o compilador que **já vem no Windows**: não precisa instalar o Visual Studio nem o .NET SDK.

```bat
cd src
compilar.bat
```

No Linux, compile com o Mono:

```bash
cd src
mcs -target:winexe -out:Digitalizar.exe -win32icon:Digitalizar.ico \
    -resource:Digitalizar.ico,Digitalizar.ico -resource:idiomas.tsv,idiomas.tsv \
    $(for t in 16 20 24 32 40 48 64; do echo -n "-resource:icones/Digitalizar_$t.png,ico_$t.png "; done) \
    -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:Microsoft.CSharp.dll -r:System.Core.dll -r:System.dll \
    -r:System.IO.Compression.dll -r:System.IO.Compression.FileSystem.dll *.cs
```

| Arquivo | O que tem |
|---|---|
| `src/Janela.cs` | Janela principal e inicialização |
| `src/Documento.cs` | Documento digitalizado: páginas, modo de cor e salvamento |
| `src/Atualizador.cs` | Verificação e instalação de novas versões pelo GitHub |
| `src/Editor.cs` | Edição rápida (recorte, rotação, tons) |
| `src/Comum.cs` | Scanner (WIA no Windows, SANE no Linux), processamento de imagem e gerador de PDF |
| `src/Controles.cs` | Controles próprios: seletores, botões, sliders, switch, editor de curvas |
| `src/Moldura.cs` | Barra de título integrada |
| `src/Tema.cs` | Temas, cores de destaque e preferências guardadas (`config.ini`) |
| `src/Idioma.cs` · `src/idiomas.tsv` | Sistema de idiomas e as traduções (uma coluna por idioma) |
| `src/Opcoes.cs` · `src/Sobre.cs` | Janelas de Opções e Sobre |
| `android/` | Versão Android (Java), com o próprio README, CHANGELOG e `build.sh` |

Cada versão está marcada com uma *tag* (`v1.0.0`, `v2.1.0`, `v3.1.0`, `v3.2.0`, `v3.3.0`), então dá para ver o código de qualquer uma delas.

## 🤝 Código aberto: contribua!

Este projeto é de **código aberto**, sob a **[licença MIT](LICENSE)**. Você pode usar, estudar, modificar e distribuir à vontade, inclusive em projetos comerciais, desde que mantenha o aviso de direitos autorais.

Encontrou um bug? Tem uma ideia? Quer melhorar algo? **Toda contribuição é bem-vinda!**

1. Faça um **fork** do projeto.
2. Crie uma branch para a sua alteração: `git checkout -b minha-melhoria`
3. Faça o commit: `git commit -m "Adiciona tal coisa"`
4. Envie para o seu fork: `git push origin minha-melhoria`
5. Abra um **Pull Request** contando o que mudou.

Prefere só avisar? Abra uma **[Issue](https://github.com/giosepeluiz/project-scanner/issues)**. Mais detalhes em **[CONTRIBUTING](CONTRIBUTING.md)**.

## 👤 Autor

<table>
<tr>
<td>

Criado por **Giosepe Luiz dos Santos**, com a ajuda do **Anthropic Claude Opus 5.5**.

[![Instagram](https://img.shields.io/badge/Instagram-giosepeluiz-E4405F?style=flat-square&logo=instagram&logoColor=white)](https://instagram.com/giosepeluiz)
[![GitHub](https://img.shields.io/badge/GitHub-giosepeluiz-181717?style=flat-square&logo=github&logoColor=white)](https://github.com/giosepeluiz)
[![Facebook](https://img.shields.io/badge/Facebook-giosepeluiz-1877F2?style=flat-square&logo=facebook&logoColor=white)](https://facebook.com/giosepeluiz)
[![E-mail](https://img.shields.io/badge/E--mail-contato%40arqueirover.de-6ccb5f?style=flat-square&logo=maildotru&logoColor=white)](mailto:contato@arqueirover.de)

</td>
</tr>
</table>

## 💬 Para terminar

Se o Digitalizar te poupou alguns minutos (ou alguma dor de cabeça com programas pesados), deixe uma ⭐ no repositório: isso ajuda outras pessoas a encontrá-lo. E se fizer algo legal com o código, me conta!

<div align="center">

*Feito com ☕ entre o Brasil e Portugal.*

</div>
