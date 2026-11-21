# CLAUDE.md · Digitalizar (project-scanner)

Contexto e regras do projeto para o Claude Code. Leia tudo antes de mexer; o que está aqui são **decisões já tomadas** do dono do projeto.

## O projeto
- **Digitalizar**: aplicativo de digitalização simples, leve, "só o básico bem feito", de código aberto (MIT). Criado por **Giosepe Luiz dos Santos** (GitHub: `giosepeluiz`), "com a ajuda do Anthropic Claude" (os créditos ficam como estão no Sobre/README). Nasceu da necessidade de usar a HP DeskJet 2700 sem os apps pesados dos fabricantes.
- Repositório: `github.com/giosepeluiz/project-scanner`.
- **PC** (Windows + Linux, a mesma base de código): `src/`, C# 5 / Windows Forms, scanner por **WIA** (Windows) e **SANE** (Linux). Versão atual: **3.5.1**.
- **Android**: `android/`, Java 8 puro (sem AndroidX, sem Gradle), scanner de rede **eSCL/AirScan** + câmera inteligente. Versão atual: **1.5.1**. A numeração do Android acompanha a do PC "duas casas atrás" (Android 1.5 = funções da 3.5).
- Quando o dono diz **"PC"**, são sempre **Windows e Linux juntos**. Toda função nova entra no PC **e** no Android (a menos que ele diga o contrário) e as duas versões devem se comportar do mesmo jeito.

## Idioma e estilo
- Converse com o dono em **português do Brasil**, separador decimal **"."** (ponto).
- Textos da interface, commits, changelogs e README: pt-BR. Commits: mensagem clara em pt-BR (ex.: `Otimizar: acerta retrato/paisagem…`). Termine commits com o trailer de co-autoria que a sessão indicar.
- **Sem emojis na interface do Android** (use texto ou ícones de fonte). Emojis só em README/changelog.
- Tom do README: informal e visual. Capturas de tela no README **sempre com o conteúdo digitalizado borrado**.

## Regras de código
**PC (`src/`)**
- **C# 5** (compila com o `csc.exe` do .NET Framework 4 que vem no Windows): sem `$"..."`, sem `?.`, sem `nameof`, sem funções locais, sem `out var`, sem tuplas. Use `Func<>`/lambdas.
- **Zero dependências externas.** Só o que vem com o Windows/Mono. O que for específico de sistema fica atrás de `Config.Windows`.
- Todo texto da interface passa por `L.T("…")` (ou `L.F` com `{0}`) e **precisa de tradução** em `src/idiomas.tsv` (15 idiomas; coluna 1 = pt-BR; célula vazia usa en-US). O Android tem a sua própria cópia: `android/app/assets/idiomas.tsv`. Mexeu num texto → atualize os dois TSV e apague as linhas de textos que não existem mais.
- Preferências: `Prefs.Definir/Obter/ObterBool`. Cores: sempre pela classe `Tema` (claro e escuro). Toda configuração do usuário deve ser **persistente**.
- Depois de compilar com Mono, confira que o `.exe` só usa membros que existem no .NET Framework 4: `mcs -out:/tmp/Il.exe tools/verificar-il.cs && mono /tmp/Il.exe src/Digitalizar.exe` lista os membros de fora do assembly (revise os novos). O Mono aceita coisas que o `csc` 4 não aceita.

**Android (`android/`)**
- Java 8, sem AndroidX/Gradle, compilado por `android/build.sh`. Visual e regras "irmãs" do PC.
- **Nunca salvar sozinho**: o usuário toca em Salvar. O modo de cor pode ser trocado depois de digitalizar. Em PDF, várias páginas (adicionar/excluir páginas do mesmo PDF).

## Decisões de produto (já tomadas)
- Várias páginas **só com PDF**; com 2+ páginas o tipo fica travado em PDF e destrava com 1. No PC, PDF não salva sozinho (botão "Salvar PDF"); JPEG/PNG/JXL salvam sozinhos.
- O modo de cor (cor, cinza, P&B adaptativo) pode mudar depois de digitalizar; a edição rápida trabalha sobre o **original colorido**.
- Atualização dentro do app a partir das releases do GitHub (manual + checagem automática, com notas da versão). **O atualizador procura os arquivos `Digitalizar-<v>-windows.zip` e `Digitalizar-<v>-linux.tar.gz` — mantenha esses nomes.**
- **JPEG XL sem perdas** (`.jxl`) é o tipo padrão na primeira execução. Codificador próprio (`src/JpegXl.cs`, `android/.../Jxl.java`: modular, códigos de prefixo, LZ77/RLE, grupos 256×256, YCoCg RCT, preditor gradiente), validado com `pillow-jxl-plugin`; C# e Java geram arquivos idênticos.
- Girar na edição rápida de **1 em 1°** (botões −1° / +1°), sem encaixes.
- **Otimizar** (PC 3.5 / Android 1.5): interruptor **ligado por padrão**, texto "Otimizar" (**sem IA**: o dono desistiu da ideia porque não dá para embutir chave de API com segurança). No PC fica acima de "Salvar arquivo em"; no Android, abaixo do botão Digitalizar. Mostra uma **borda grossa em degradê girando** em volta da imagem enquanto trabalha. Faz: (1) tira as bordas pretas do scanner e endireita a página (retângulo de menor área); sem moldura, apara faixas pretas coladas na borda; (2) sem moldura, corrige a inclinação do texto (±7°); (3) põe o texto na horizontal. Detalhes em `src/Otimizar.cs` (e o port `android/.../Otimizar.java`, que **tem de dar exatamente as mesmas decisões** — mude os dois juntos).
  - **Limitação real:** em croqui/letra de mão/números/maiúsculas não existe pista confiável de qual lado é o "em cima". Por isso: texto de lado → **90° horário** por padrão (o jeito como o dono coloca as folhas no scanner); 270° só com assimetria bem clara (`LIMITE_FORTE`); **180° só com certeza** (`LIMITE_ASSIM`). Se um dia quiser resolver de verdade, só com reconhecimento de caracteres (OCR), o que fere a regra "sem dependências/sem IA".
- Distribuição: **sem assinatura de código**. O pedido à SignPath Foundation foi recusado; **ignore SignPath**. Microsoft Store (MSIX) é só uma ideia futura.

## Compilar e testar
```bash
# PC (Linux, Mono) – comando completo no README ("Para desenvolvedores")
cd src && mcs -target:winexe -out:Digitalizar.exe -win32icon:Digitalizar.ico … *.cs
# Windows: src\compilar.bat   (a lista de arquivos dele e a do release-pc.yml precisam incluir todo .cs novo)

# Android (uma vez: bash android/tools/preparar.sh  -> baixa o SDK enxuto para $ASDK, padrão /opt/asdk)
cd android && ./build.sh            # usa keystore/digitalizar.jks (NÃO está no git)

# Testes
bash tools/teste-otimizar/testar.sh   # Otimizar: C# x Java idênticos + posição final das páginas de teste
bash android/teste/testar.sh          # detecção do documento da câmera (Visao.java)
```
- Não dá para testar aqui: scanner real, Windows real, aparelho Android real, GitHub Actions. Diga isso com franqueza nas notas das releases.

## Releases (o dono publica; você prepara)
1. Suba a versão: `src/Janela.cs` (`AssemblyVersion`/`AssemblyFileVersion` `X.Y.Z.0`), `android/.../Config.java` (`VERSAO`), `android/app/AndroidManifest.xml` (`versionName` e `versionCode`: Android 1.5.1 = `110501`).
2. Atualize **`CHANGELOG.md`**, **`android/CHANGELOG.md`** e o **"🕰️ Histórico de versões"** do `README.md` e do `android/README.md` (ele pediu para **nunca esquecer**), incluindo a lista de releases e as tabelas de versão.
3. `bash tools/empacotar.sh` gera `entrega/Digitalizar-<v>-windows.zip`, `-linux.tar.gz`, `Digitalizar-<a>-android.apk` e `SHA256SUMS-<v>.txt` (os binários **não** vão para o git).
4. Commit + push na `main`. Tags: `vX.Y.Z` (PC) e `android-vX.Y.Z` (Android).
5. Preencha (sem publicar) o formulário `https://github.com/giosepeluiz/project-scanner/releases/new?tag=<tag>&target=main` com título, notas em pt-BR e os SHA-256. O dono **anexa os arquivos e publica** (o push pela nuvem foi recusado antes; o GitHub Actions não está rodando por ele).
6. **Publicar o Android antes do PC**: a release Android usa o rótulo **None** (não é "latest"); a do PC fica **Latest**.
7. Depois de publicado, atualize no README os links de download e as badges de versão (hoje ainda apontam para versões antigas: `README.md` linhas ~9, 12, 58–60, 75, 85 e `android/README.md`).
- Nunca coloque keystore, senha ou binários no git. O `.gitignore` já cobre `*.jks`, `*.apk`, `*.zip`, `*.exe`, `android/keystore/`, `entrega/`.

## Estado atual (21/11/2026)
- PC **3.5.1** e Android **1.5.1** commitados na `main`; as releases `v3.5.0` e `android-v1.5.0` já estão publicadas; `v3.5.1` e `android-v1.5.1` estão **preparadas** (formulários preenchidos) e dependem de o dono anexar os arquivos e publicar.
- Pendente depois disso: atualizar links de download/badges nos READMEs (passo 7).
- Em aberto / ideias: lado "em cima" em letra de mão (limitação acima); Microsoft Store/MSIX; testar em scanner real, Windows e Android reais; abrir `.jxl` no Windows exige a extensão "JPEG XL Image Extension" (Microsoft Store) ou um visualizador como o IrfanView.
