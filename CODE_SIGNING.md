# 🔏 Assinatura de código

Os executáveis do Digitalizar **não têm assinatura digital** por enquanto. Por isso o Windows pode mostrar o aviso azul do SmartScreen na primeira vez ("O Windows protegeu o computador").

O pedido de assinatura gratuita à SignPath Foundation foi recusado em novembro de 2026, porque o projeto ainda não tem a visibilidade pública que o programa exige. Quando o projeto crescer, o pedido pode ser refeito.

## Como conferir o arquivo baixado
- Compare o **SHA-256** do arquivo com o que aparece na release (`Get-FileHash arquivo.zip` no PowerShell).
- Clique com o botão direito no `.zip` → **Propriedades** → **Desbloquear** → OK, e só depois extraia.
- O código-fonte completo está neste repositório e pode ser compilado por você mesmo (veja "Para desenvolvedores" no README).
