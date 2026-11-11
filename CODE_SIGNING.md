# Assinatura de código

Os executáveis do Windows do **Digitalizar** são compilados no GitHub Actions a partir deste repositório
(`.github/workflows/release-pc.yml`) e, quando a assinatura estiver ativa, assinados pela SignPath.

> Estado: **pedido à SignPath Foundation ainda não feito/aprovado.** Até lá, o Windows pode mostrar o aviso azul
> "O Windows protegeu o computador" (veja o README para o passo a passo).

## Política (quando aprovada)

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

- **Quem assina:** somente o fluxo do GitHub Actions deste repositório, para tags `v*`.
- **O que é assinado:** apenas o `Digitalizar.exe` compilado a partir do código público deste repositório.
- **Papéis:** autor, revisor e aprovador: [@giosepeluiz](https://github.com/giosepeluiz).
- **Privacidade:** o programa só acessa a internet para verificar novas versões no GitHub (pode ser desligado em Opções → Atualizações) e não envia dados pessoais.

## Para o mantenedor: como ligar a assinatura

1. Pedir a assinatura gratuita em <https://signpath.org/apply> (projeto de código aberto, licença MIT, repositório público; exige autenticação em dois fatores).
2. Na SignPath, criar o projeto com o slug do passo 3, colar o conteúdo de `.signpath/artifact-configuration.xml` como configuração do artefato, criar a política de assinatura (ex.: `release-signing`) e conectar o GitHub como sistema de build confiável.
3. Depois de aprovado, no GitHub (Settings → Secrets and variables → Actions) criar:
   - o segredo `SIGNPATH_API_TOKEN`;
   - as variáveis `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG` e `SIGNPATH_SIGNING_POLICY_SLUG`.
3. Enviar uma tag `vX.Y.Z`. Enquanto a variável `SIGNPATH_ORGANIZATION_ID` não existir, o fluxo compila e publica sem assinar.
