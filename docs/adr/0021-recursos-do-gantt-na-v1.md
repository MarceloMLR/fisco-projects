---
status: aceita (apresentação na tela definida pelo ADR-0071; quem baixa o XML e o e-mail nele, pelo ADR-0136)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0021: Recursos do Gantt na v1, com linha de base, caminho crítico, desfazer e exportação

## Contexto e problema

O Gantt do MVP já permite arrastar barras, mudar a duração pela borda, dar zoom e ver as setas de dependência. Com as dependências empurrando as tarefas seguintes (ADR-0019), o sistema perde a informação de quanto o projeto escorregou se não guardar o plano original. Além disso, clientes e equipes que usam o MS Project precisam receber o cronograma. Quais recursos o Gantt tem na v1?

## Fatores de decisão

- Mostrar o desvio do projeto em relação ao plano aprovado.
- Troca de cronogramas com quem usa o MS Project.
- Esforço proporcional para um desenvolvedor sozinho, deixando fora da v1 o que tem mais risco.

## Opções consideradas

- A. Gantt do MVP, sem linha de base
- B. Gantt do MVP mais uma linha de base simples
- C. Gantt completo: B mais caminho crítico, exportação e importação de arquivos do MS Project

## Resultado da decisão

Opção escolhida: **B, ampliada com caminho crítico, desfazer, exportação em PDF e exportação no formato XML do MS Project**. A importação de arquivos do MS Project fica para depois da v1, porque é o item de maior esforço: exige converter estruturas que o FiscoProjects não suporta (mais de dois níveis, outros tipos de dependência, intervalos entre tarefas) com prévia e avisos.

Na tela, o Gantt aparece como "Linha do tempo", ajustada ao espaço disponível, com a opção Expandir e a parte concluída em verde (ADR-0071 e ADR-0067).

Escopo:

| Item | Classificação |
| --- | --- |
| Barras que se arrastam e se redimensionam, no computador | v1 |
| Tabela ao lado do Gantt para editar nome, datas, duração, responsável e progresso | v1 |
| Zoom por dias, semanas e meses, com a linha do dia de hoje | v1 |
| Setas de dependência, criadas pela tabela ou ligando duas barras | v1 |
| Linha de base: uma por projeto, salva e atualizada pelo gestor, com os dias de diferença por tarefa | v1 |
| Caminho crítico destacado no Gantt | v1 |
| Desfazer as últimas ações da própria pessoa, incluindo as tarefas empurradas em cadeia; bloqueado se alguém alterou essas tarefas depois | v1 |
| Exportar em PDF pela página de impressão (período e zoom escolhidos pela pessoa) | v1 |
| Exportar no formato XML do MS Project | v1 |
| Importar arquivos do MS Project (.mpp e XML), com prévia e avisos de conversão | depois |
| Exportar em Excel | depois |
| Gerar o PDF no servidor | depois |
| Gerar arquivo .mpp nativo | fora (exigiria biblioteca paga) |

### Consequências

- Bom, porque a linha de base mostra o desvio do projeto mesmo com as dependências empurrando datas.
- Bom, porque a exportação em XML permite enviar o cronograma a quem usa o MS Project, que pode salvá-lo como .mpp.
- Ruim, porque caminho crítico e desfazer em cadeia aumentam o esforço e exigem testes cuidadosos.
- Ruim, porque a biblioteca de Gantt precisa suportar, ou permitir desenhar, linha de base e caminho crítico, o que restringe a escolha no bloco de arquitetura.
- Neutro, porque o PDF depende do layout de impressão do navegador; a geração no servidor fica para depois.

### Confirmação

Testes cobrindo: a linha de base guarda as datas e mostra a diferença por tarefa; o caminho crítico identifica corretamente as tarefas sem folga em cenários conhecidos, respeitando dias úteis e feriados; desfazer reverte a ação e as tarefas empurradas em cadeia, e é bloqueado quando outra pessoa alterou essas tarefas; o XML exportado é válido no esquema do MS Project e, lido de volta, preserva fases, tarefas, marcos, dependências e datas; a página de impressão respeita o período e o zoom escolhidos.

## Prós e contras das opções

### A. Gantt do MVP, sem linha de base

- Bom, porque é o menor esforço.
- Ruim, porque não mostra quanto o projeto atrasou em relação ao plano.

### B. Gantt do MVP com linha de base

- Bom, porque mostra o desvio com esforço moderado.
- Ruim, porque acrescenta dados e desenho ao Gantt.

### C. Gantt completo

- Bom, porque se aproxima de uma ferramenta profissional de cronograma.
- Ruim, porque a importação do MS Project, com conversão e prévia, pesa muito na v1.

## Mais informações

- MPXJ (https://github.com/joniles/mpxj): lê MPP e o XML do MS Project e grava o XML, disponível para .NET via IKVM, licença LGPL. Candidata para a importação futura e, opcionalmente, para a exportação; escolha e licença a confirmar no bloco de arquitetura.
- A exportação também pode gerar o XML do MS Project diretamente, sem biblioteca, por ser um formato documentado.
