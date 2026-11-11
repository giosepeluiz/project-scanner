# Changelog · Digitalizar para Android

O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e as versões seguem o [Versionamento Semântico](https://semver.org/lang/pt-BR/).
A numeração do Android acompanha a do PC com duas "casas" de diferença: **1.3** do Android corresponde às funções da **3.3** do PC. O histórico do PC está no [CHANGELOG da raiz](../CHANGELOG.md).

## [1.3.0] – 2026-11-11 · "Sempre atualizado" 🔄

Acompanha as funções da **3.3** do PC.

### Adicionado
- **Atualização dentro do app**, direto das releases do GitHub (tags `android-v*`):
  - Verificação **automática** ao abrir (liga/desliga em **Opções → Atualizações**) e **manual** com **Verificar agora**.
  - Aviso de nova versão com as **notas da versão**, e as ações **Atualizar agora**, **Depois** e **Pular esta versão**.
  - O app baixa o APK e abre o instalador do Android. Na primeira vez, o Android pede para permitir instalar apps desta fonte.
  - Nova permissão: `REQUEST_INSTALL_PACKAGES`.

### Alterado
- **Várias páginas só com PDF:** a faixa de páginas aparece apenas quando o tipo de arquivo é PDF. Com JPEG ou PNG, cada digitalização é uma imagem só.
- Novo esquema de `versionCode` (`100000 + maior×10000 + menor×100 + correção`), para nunca ficar abaixo de um APK de teste anterior.

### Observações
- Como o APK é assinado com a mesma chave, a atualização instala por cima sem perder as configurações.
- Não testado em aparelho real.

## [1.2.0] – 2026-11-11 · "Agora no seu bolso" 📱

Primeira versão pública do Digitalizar para Android.

### Adicionado
- Digitalização por **scanner de rede** (eSCL / AirScan / Mopria), com detecção automática (mDNS) e **Adicionar por IP**.
- Digitalização pela **câmera**, com detecção automática do documento, captura automática quando a folha fica parada, lanterna, correção de perspectiva, ajuste de proporção do papel (A4 etc.) e remoção de sombras. Também abre fotos da **Galeria**.
- **PDF de várias páginas**: adicionar e excluir páginas antes de salvar. JPEG e PNG geram um arquivo por página.
- **Salvar só quando o usuário tocar em "Salvar"**, sem gravação automática.
- **Troca do modo de cor depois de digitalizar** (colorido para cinza ou preto e branco, e de volta).
- Edição rápida (recorte, giro, brilho, contraste, níveis, curvas e limiar), copiar, compartilhar e abrir.
- 15 idiomas, temas claro/escuro/sistema e 10 cores de destaque (mesmo esquema do PC).
- Ícones Font Awesome Free no lugar de emojis.

### Observações
- Ainda não foi testado em muitos aparelhos reais. Relatos de problemas são bem-vindos nas Issues.
- Uma digitalização feita em cinza ou preto e branco não volta a ser colorida.

[1.3.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/android-v1.3.0
[1.2.0]: https://github.com/giosepeluiz/project-scanner/releases/tag/android-v1.2.0
