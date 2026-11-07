# 🤝 Como contribuir

Que bom que você quer ajudar! O Digitalizar é de código aberto (licença MIT) e toda ajuda é bem-vinda: código, ideias, testes em outros scanners, traduções ou correções na documentação.

## 🐞 Encontrou um problema?

Abra uma **[Issue](https://github.com/giosepeluiz/project-scanner/issues)** contando:
- o que você fez e o que esperava que acontecesse;
- o que aconteceu de fato (um print ajuda muito);
- as **informações do software**: abra o *Sobre* (F1) e clique em **Copiar informações**;
- o modelo do scanner ou da impressora.

## 💡 Quer sugerir algo?

Abra uma Issue explicando a ideia. Lembre-se da filosofia do projeto: **simples, leve e com o básico bem feito**. Funções que deixam o programa pesado podem ficar de fora.

## 🔧 Quer enviar código?

1. Faça um **fork** do repositório.
2. Crie uma branch: `git checkout -b minha-melhoria`.
3. Faça as alterações em `src/`.
4. Compile e teste:
   - **Windows:** `cd src` e `compilar.bat` (usa o compilador que já vem no Windows);
   - **Linux:** veja o comando com `mcs` no [README](README.md#️-para-desenvolvedores).
5. Faça o commit com uma mensagem clara: `git commit -m "Corrige o recorte em imagens giradas"`.
6. Envie (`git push origin minha-melhoria`) e abra um **Pull Request**.

### Algumas regras para o código

- **C# 5** (compatível com o `csc.exe` do .NET Framework 4 que vem no Windows): sem interpolação de strings (`$"..."`), sem `?.` e sem `nameof`.
- Sem dependências externas: só o que já vem com o Windows / Mono.
- Textos da interface em **português do Brasil**.
- Cores sempre pela classe `Tema`, para funcionar nos temas claro e escuro.
- O que for específico de um sistema deve ficar atrás de `Config.Windows`.

Obrigado! 💙
