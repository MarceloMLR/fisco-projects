---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0094: Leitura das planilhas de acessos com o Open XML SDK e o CSV por um leitor próprio

Detalha a importação de planilha do ADR-0034.

## Contexto e problema

O ADR-0034 decidiu a importação dos acessos a partir de uma planilha `.xlsx` ou `.csv`: mapear as colunas, filtrar as linhas e importar só as marcadas. O backend precisa ler os dois formatos. O ADR-0061 proíbe dependência com licença comercial ou copyleft forte sem ADR, e o ADR-0050 já afastou bibliotecas com licença comercial.

Com que biblioteca o sistema lê a planilha?

## Fatores de decisão

- Licença permissiva, sem custo e sem chave (ADR-0061).
- Biblioteca mantida, com versões recentes e sem vulnerabilidade conhecida (o build quebra com pacote vulnerável).
- Ler só os valores das células: a importação não escreve planilhas nem lê fórmulas, estilos ou gráficos.
- Proteção contra arquivo malicioso (arquivo compactado que se expande demais, XML com entidades).
- O CSV exportado pelo Excel em português usa ponto e vírgula e, muitas vezes, a codificação Windows-1252.

## Opções consideradas

- A. Open XML SDK (`DocumentFormat.OpenXml`, MIT, da Microsoft) para o `.xlsx` e um leitor próprio de CSV
- B. ClosedXML (MIT), que usa o Open XML SDK por baixo, e um leitor próprio de CSV
- C. ExcelDataReader (MIT), que lê `.xlsx`, `.xls` e CSV
- D. EPPlus 5 ou superior
- E. Leitor próprio do `.xlsx` (ZIP e XML com as bibliotecas do .NET)

## Resultado da decisão

Opção escolhida: **A. Open XML SDK e um leitor próprio de CSV**, porque é a biblioteca oficial do formato, com licença MIT e sem dependências além do próprio SDK e do `System.IO.Packaging` da Microsoft, e porque lê só o que a importação precisa (as células, com os textos compartilhados), com limite de tamanho por parte do arquivo. O CSV é simples o bastante para um leitor próprio, que trata as aspas, o separador (ponto e vírgula, vírgula ou tabulação) e a codificação (UTF-8, com ou sem BOM, ou Windows-1252).

Regras:

- **Versão:** `DocumentFormat.OpenXml` 3.5.1, fixada no `Directory.Packages.props` e usada só pelo módulo `Credentials`.
- **Leitura:**
  - a primeira linha com algum valor é o cabeçalho, e as linhas vazias ficam de fora;
  - cada célula vira texto: o valor guardado (o resultado, numa fórmula), com os números sem formatação regional;
  - o arquivo protegido por senha, o `.xls` antigo e o arquivo corrompido são recusados com uma mensagem para salvar como `.xlsx` ou `.csv`.
- **Proteção:** limite de tamanho do arquivo e de cada parte descompactada, e o XML lido sem DTD.

### Consequências

- Bom, porque a dependência nova é da própria Microsoft, com licença MIT.
- Bom, porque o CSV do Excel em português é lido sem configuração.
- Ruim, porque o `.xls` antigo não é lido: a pessoa salva a planilha como `.xlsx` ou `.csv`.
- Ruim, porque o leitor de CSV é código nosso, com testes próprios.

### Confirmação

Testes cobrindo: a leitura de um `.xlsx` com textos compartilhados, números e células vazias; o CSV com ponto e vírgula, com aspas e quebra de linha dentro do campo, em UTF-8 e em Windows-1252; a recusa do arquivo que não é planilha.

## Prós e contras das opções

### A. Open XML SDK e leitor próprio de CSV

- Bom, porque é a biblioteca oficial, MIT e mantida pela Microsoft.
- Ruim, porque a API é de baixo nível (textos compartilhados e referências de célula ficam por nossa conta).

### B. ClosedXML

- Bom, porque a API é simples.
- Ruim, porque traz o Open XML SDK e outras dependências para só ler células.

### C. ExcelDataReader

- Bom, porque lê também o `.xls` antigo.
- Ruim, porque o `.xls` exige o pacote de codificações e o leitor de CSV dele não detecta o separador.

### D. EPPlus 5 ou superior

- Ruim, porque tem licença comercial (ADR-0061).

### E. Leitor próprio do `.xlsx`

- Bom, porque não traz dependência.
- Ruim, porque o formato tem detalhes (textos compartilhados, textos em linha, referências de célula) que a biblioteca oficial já trata.
