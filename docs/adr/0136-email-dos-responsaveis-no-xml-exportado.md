---
status: aceita
date: 2026-10-31
decision-makers: Marcelo Ribeiro
---

# ADR-0136: E-mail dos responsáveis no XML do MS Project, exportado só pela gestão

Muda, no ADR-0021, quem baixa o XML do MS Project e o que vai nele: a Cronograma-7 o deixou para todas as pessoas do projeto, sem o e-mail dos responsáveis. Com a importação (ADR-0135), o e-mail passa a ir no arquivo, e só o administrador e o gerente do projeto o baixam. A exportação em PDF continua para todas as pessoas do projeto.

## Contexto e problema

A importação do MS Project sugere a pessoa de cada recurso pelo e-mail dele (ADR-0135). O XML que o próprio FiscoProjects exporta traz os responsáveis só pelo nome, então, na ida e volta, ninguém é sugerido e quem cria o projeto liga cada recurso à mão.

O e-mail de uma pessoa da empresa é dado pessoal (ADR-0058). Num arquivo baixado, ele sai do controle do sistema: vai para o computador de quem baixou e para onde a pessoa mandar o arquivo.

## Fatores de decisão

- A ida e volta pelo MS Project com as pessoas sugeridas, sem ligar recurso por recurso.
- O e-mail fora do sistema só com quem responde pelo projeto.
- A regra de quem faz o quê num lugar só (`ProjectPermissions`, ADR-0043).

## Opções consideradas

- **A.** O e-mail no XML, que continua para todas as pessoas do projeto.
- **B.** O e-mail no XML, que passa a ser só do administrador e do gerente do projeto.
- **C.** Sem o e-mail no XML, como na Cronograma-7.

## Resultado da decisão

Opção escolhida: **B**, a pedido de Marcelo.

- O recurso de cada pessoa responsável leva o nome e o e-mail dela (`EmailAddress` do MSPDI). A pessoa anonimizada vai sem e-mail. O cliente como responsável continua um recurso só com o nome (ADR-0102).
- Só o administrador e o gerente do projeto baixam o XML, também com o projeto encerrado. As outras pessoas do projeto recebem 403, e quem não está nele, 404.
- O "⋮" da aba Cronograma mostra "Exportar para o MS Project (XML)" só para quem pode baixar. "Exportar em PDF" continua para todas as pessoas do projeto.
- A importação não muda: o e-mail do arquivo só serve para a sugestão e não é guardado (ADR-0135).

### Consequências

- Bom, porque o XML exportado volta pela importação com as pessoas já sugeridas.
- Bom, porque o e-mail só sai do sistema com quem gerencia o projeto, que já vê as pessoas dele.
- Ruim, porque o líder técnico, o colaborador, o desenvolvedor e o leitor deixam de baixar o XML; quem precisar pede ao gerente, ou usa o PDF.

### Confirmação

- Os testes da exportação conferem o e-mail no recurso e a resposta para cada papel no projeto.
- O teste da ida e volta confere a pessoa sugerida pelo e-mail.

## Prós e contras das opções

### A. O e-mail para todas as pessoas do projeto

- Bom, porque ninguém perde a exportação.
- Ruim, porque o e-mail de todos os responsáveis sai do sistema com qualquer pessoa do projeto, inclusive o leitor.

### B. O e-mail só com a gestão

- Bom, porque junta a ida e volta com o e-mail fora do sistema só com quem responde pelo projeto.
- Ruim, porque as outras pessoas do projeto perdem o XML.

### C. Sem o e-mail

- Bom, porque nenhum dado pessoal sai no arquivo.
- Ruim, porque a ida e volta pede ligar cada recurso à mão.
