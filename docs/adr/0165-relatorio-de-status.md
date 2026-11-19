---
status: aceita
date: 2026-11-19
decision-makers: Marcelo Ribeiro
---

# ADR-0165: Relatório de status em PowerPoint e PDF, num modelo que a empresa edita

Completa o ADR-0073 (visão geral do projeto), cujos números o relatório reaproveita, o ADR-0092 (documentos do projeto), onde o relatório fica guardado, e o ADR-0164 (o que ameaça o cronograma), de onde vêm os riscos. A regra das horas no relatório fica no ADR-0166. Aprovado por Marcelo em 19/11/2026.

## Contexto e problema

Toda semana, o gerente de um projeto de implantação manda ao cliente um status report em PowerPoint: o cabeçalho com o cliente, o projeto, a situação e a data; o avanço total e de cada fase; os próximos marcos; a previsão do fim e a sequência das fases; e um slide por risco, com o chamado, o andamento dele e as etapas que dependem da resolução. Ele copia esses números do cronograma e dos chamados, e cada empresa tem o próprio visual.

Em 19/11/2026, Marcelo trouxe o pedido de um gerente de projetos e um status report de exemplo (usado só pela estrutura; o arquivo tem dados reais e não entra no repositório) e decidiu: o relatório é para o cliente; quem gera escolhe PDF ou PowerPoint e o modelo padrão ou o da empresa; a empresa edita o modelo padrão no próprio PowerPoint, mexendo só no que não atrapalha a geração, sobe e o deixa como padrão para todos; só o administrador e o gerente geram; e há a opção "Mostrar horas". Como gerar o PowerPoint, como o modelo funciona e de onde sai o PDF?

## Fatores de decisão

- Sem custo de licença e sem dependência pesada, com licenças permissivas (ADR-0050, ADR-0061).
- O modelo da empresa vale igual para o PowerPoint e para o PDF.
- Os números saem do servidor, das mesmas regras da Visão geral e do cronograma, sem uma segunda conta.
- Roda no App Service de hoje (ADR-0052), sem contêiner.
- O que foi mandado ao cliente fica guardado no projeto.

## Opções consideradas

Gerar o PowerPoint:

- G1. No servidor, preenchendo um `.pptx` modelo com campos, pelo Open XML SDK (Microsoft, MIT), que o projeto já usa na importação da planilha dos acessos (ADR-0094).
- G2. No navegador, pelo PptxGenJS (MIT), criando o arquivo do zero.
- G3. Uma biblioteca paga (Aspose.Slides, Syncfusion Presentation ou GemBox.Presentation), que gera o PowerPoint e o PDF.

Gerar o PDF:

- P1. Pelo Microsoft Graph, que converte em PDF o `.pptx` guardado no SharePoint (`/content?format=pdf`).
- P2. Pelo LibreOffice no servidor.
- P3. Por uma página de impressão própria, como o PDF do cronograma.

O modelo da empresa:

- M1. Um `.pptx` com campos entre chaves, que o administrador baixa, edita no PowerPoint e sobe, conferido pelo sistema.
- M2. Um editor de modelo dentro do GerenWare.
- M3. Só o modelo padrão, com o logo e as cores da instalação.

## Resultado da decisão

Opções escolhidas: **G1**, **P1** e **M1**, com o modelo padrão de início, aprovadas por Marcelo. G2 não abre o modelo da empresa; G3 custa licença por desenvolvedor e pede um ADR de licença comercial (ADR-0050). P2 pede centenas de megabytes e um contêiner; P3 desenharia o relatório duas vezes e ignoraria o modelo da empresa. M2 é um produto à parte; M3 não atende o pedido de Marcelo.

Regras:

| Item | Definição |
| --- | --- |
| Quem gera | O administrador e o gerente do projeto (ADR-0040), em qualquer situação do projeto, como a exportação para o MS Project (ADR-0136). As outras pessoas do projeto recebem 403, e quem está fora, 404 |
| Ao gerar | O formato (PowerPoint ou PDF), o modelo (o padrão do GerenWare ou o da empresa, quando há), "Mostrar horas" (ADR-0166, desmarcado), os chamados que entram como riscos (sugeridos: os que ameaçam marcos, ADR-0164) e os textos do gerente: o resumo do período, o foco atual, os próximos passos, o que precisa do cliente e, em cada risco, a classificação e o ponto de atenção. Os textos do último relatório do projeto vêm preenchidos |
| O que vem do sistema | O cliente e o logo dele, o projeto, a situação, o gerente e a data; o andamento total e de cada fase, contra o previsto; o prazo final e o desvio contra a linha de base; os próximos marcos; as fases com os períodos; as tarefas atrasadas e o que elas empurram (ADR-0164); os chamados em aberto e os vencidos; e, em cada risco, o número, o título, o status, a prioridade, o registro no fornecedor com o andamento e os marcos que dependem dele |
| Onde fica | Cada relatório gerado é guardado como documento do projeto, na pasta padrão marcada como a dos relatórios, criada pela migração como "Relatórios de status" e visível de início só para a gestão (ADR-0148), que o administrador renomeia como as outras; o nome do arquivo leva o código do projeto e a data |
| O modelo padrão | Faz parte do produto, sem nome de empresa, cliente ou fornecedor (ADR-0003): 16:9, com o logo e as cores da instalação (ADR-0146) e o logo do cliente, e os slides de andamento, um de cada risco e o das horas, só com "Mostrar horas" |
| O modelo da empresa | O administrador baixa o padrão e a lista dos campos, edita no PowerPoint (cores, fontes, fundos, posições, logos e slides fixos) e sobe de volta. O sistema confere antes de aceitar: recusa campo desconhecido e arquivo acima de 2 MB e aponta os campos que faltam ("o slide 2 perdeu `{{chamado.numero}}`"). Um modelo ativo por instalação, com "Voltar ao padrão", guardado pelo `IFileStorage`, só do administrador (ADR-0046) |
| Os campos | Texto entre chaves duplas (`{{projeto.nome}}`), que guarda o formato de quem o escreveu; listas numa tabela com uma linha-modelo, repetida para cada item; o slide de risco repetido para cada risco; os logos e as barras de progresso em formas pelo nome. Um campo que o PowerPoint quebrou em pedaços na edição continua valendo |
| O PDF | O Microsoft Graph converte o `.pptx` guardado no SharePoint (ADR-0120). Sem o SharePoint, como na máquina de desenvolvimento, só o PowerPoint, e a opção diz por quê. O arquivo convertido tem até 2 MB, o limite da conversão |
| Auditoria | O relatório guardado entra na auditoria como qualquer documento; o modelo da empresa, quando muda |

Antes da Status-5, um teste no tenant de desenvolvimento confere a conversão com a permissão que o aplicativo já tem (`Sites.Selected`, que a documentação da conversão não cita). Se não funcionar, o PDF passa a P3 e este ADR é atualizado por outro.

Fora:

| Item | Classificação |
| --- | --- |
| Editor de modelo na tela | fora (M2) |
| Um modelo por cliente | depois |
| Gráficos nativos do PowerPoint | depois |
| Mandar o relatório por e-mail ao cliente e gerar toda semana sozinho | depois |
| Horas de cada pessoa no relatório | fora (ADR-0128, ADR-0166) |

### Consequências

- Bom, porque o gerente gera em segundos o que hoje monta à mão, com os números do sistema.
- Bom, porque custa zero em licença, não traz pacote novo (o Open XML SDK já está no projeto) e o PDF sai igual ao PowerPoint, no modelo da empresa.
- Bom, porque cada relatório mandado fica guardado no projeto, com o histórico.
- Ruim, porque o PDF depende do SharePoint e do limite de 2 MB da conversão.
- Ruim, porque a empresa pode estragar o modelo apagando um campo; a conferência no envio limita o estrago, sem eliminá-lo.

### Confirmação

Testes do domínio: o preenchimento dos campos, inclusive quebrados em pedaços, das listas e dos slides repetidos, e a conferência do modelo (campo desconhecido, campo faltando, tamanho). Testes da API: quem gera e a recusa dos outros (403 e 404), as escolhas, o relatório guardado na pasta dos relatórios, o PDF pelo `FakeSharePoint` e a opção sem o SharePoint; o modelo da empresa só pelo administrador. Testes de tela: a geração, o modelo na Administração e a conferência. O arquivo gerado abre sem reparo no PowerPoint, conferido pelo validador do Open XML SDK.

## Prós e contras das opções

### G1. Open XML SDK no servidor

- Bom, porque abre e preenche o modelo da empresa, sem custo e sem pacote novo.
- Ruim, porque o preenchimento é escrito por nós.

### G2. PptxGenJS no navegador

- Bom, porque é simples para criar slides do zero.
- Ruim, porque não abre o modelo da empresa.

### G3. Biblioteca paga

- Bom, porque faz PowerPoint e PDF numa peça só.
- Ruim, porque custa licença por desenvolvedor e prende o produto a um fornecedor.

### P1. Conversão pelo Microsoft Graph

- Bom, porque o PDF sai igual ao PowerPoint, sem custo e sem pacote.
- Ruim, porque depende do SharePoint e tem o limite de 2 MB.

### P2. LibreOffice no servidor

- Bom, porque não depende do Microsoft 365.
- Ruim, porque pede um contêiner e centenas de megabytes, e a fidelidade é menor.

### P3. Página de impressão própria

- Bom, porque já existe para o cronograma.
- Ruim, porque desenha o relatório duas vezes e não segue o modelo da empresa.

### M1. Modelo `.pptx` com campos

- Bom, porque a empresa usa a ferramenta que já conhece.
- Ruim, porque um campo apagado some do relatório até o modelo ser corrigido.

### M2. Editor na tela

- Bom, porque não deixa estragar os campos.
- Ruim, porque é um produto à parte e nunca vai ter o que o PowerPoint tem.

### M3. Só o padrão

- Bom, porque é o mais simples.
- Ruim, porque não atende o pedido.
