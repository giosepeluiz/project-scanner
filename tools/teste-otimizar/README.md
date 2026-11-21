# Testes do "Otimizar" (PC e Android)

`bash tools/teste-otimizar/testar.sh` gera páginas sintéticas, roda o `src/Otimizar.cs` (Mono) e o
`android/.../Otimizar.java` (JDK) e confere (1) a posição final da página e (2) que as duas versões dão
**exatamente o mesmo resultado**. Os arquivos `.raw` têm 8 bytes de cabeçalho (largura e altura, int32 little-endian) e os pixels BGR (C#) / RGB (Java; nas imagens cinzas dá no mesmo).

- `T.cs`: roda `Otimizar.Executar` num `.raw` e grava o resultado.
- `java/…/TO.java` e `java/android/graphics/Bitmap.java`: roda o `Otimizar.executar` do Android no computador (o `Bitmap` é um stub só para compilar).
- `gerar-texto.py`: páginas de texto digitado (serifa, sans, maiúsculas) com e sem borda preta, inclinadas, giradas 0/90/180/270°.
- `gerar-croqui.py`: página parecida com uma folha de obra (caixas, réguas, números, faixa preta fina na borda).
- `conferir.py`: compara cada saída com a página original em todas as rotações.

Limitações conhecidas (não são bugs a "consertar" sem uma ideia nova): em croqui/letra de mão/números/maiúsculas não há pista
confiável de qual lado é o "em cima" — por isso o giro de 90° tem padrão (horário) e o de 180° só acontece com certeza.
Se mudar o algoritmo, **mude C# e Java juntos** e rode este teste.
