# Proposta: exportar o cronograma (Cronograma-7)

## Why

Clientes e equipes que usam o MS Project precisam receber o cronograma, e o gerente leva o cronograma impresso ou em PDF para reuniões. O ADR-0021 pôs na v1 a exportação em PDF pela página de impressão, com o período e a escala escolhidos pela pessoa, e no formato XML do MS Project. O ADR-0043 deixou exportar para todas as pessoas do projeto, e o ADR-0071 pôs a exportação no menu "⋮" da aba.

## What Changes

- **XML do MS Project** (`GET /api/projects/{id}/schedule/ms-project`), gerado no servidor (ADR-0051):
  - o arquivo tem as fases, as tarefas, as subtarefas e os marcos, em níveis;
  - cada item leva as datas, a duração em dias úteis, o progresso, o responsável e as dependências "termina, então começa";
  - leva também a linha de base, se houver;
  - o calendário da instalação vai junto: os dias de trabalho da semana e os feriados do período;
  - quem vê o cronograma exporta (ADR-0043); quem não está no projeto recebe 404.
- **PDF pela página de impressão** (`/projects/{id}/schedule/print`):
  - a pessoa escolhe o período (de e até) e a escala (dias, semanas ou meses) e abre a página;
  - a página mostra o cabeçalho do projeto e a tabela com o nome, o início, o fim, o progresso e de quem cada item depende;
  - ao lado da tabela fica a linha do tempo, com fases, barras, marcos, dias não úteis e hoje, mais a linha de base e o caminho crítico se estiverem ligados em "Exibir";
  - a legenda vai no fim;
  - o botão "Imprimir" abre a impressão do navegador, que salva em PDF;
  - a página é em paisagem e quebra entre as linhas, repetindo a escala no topo de cada página.
- **Menu "⋮"** (ADR-0071): passa a aparecer para todos que veem o cronograma, com "Exportar em PDF" e "Exportar para o MS Project (XML)"; refazer e a linha de base continuam só para quem edita.

## Non-goals

- Gerar o PDF no servidor, exportar em Excel e gerar .mpp nativo (ADR-0021).
- Importar do MS Project (depois da v1, ADR-0021).
- Validar o XML contra o XSD da Microsoft no CI: o esquema não está disponível sem rede. O teste lê o arquivo de volta e confere fases, tarefas, marcos, dependências, datas e calendário.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: exportar em XML do MS Project e em PDF pela página de impressão, e quem exporta.

## Impact

- **Backend (`Projects`):**
  - `MsProjectExport`, que monta o XML (MSPDI) com `System.Xml.Linq`, sem biblioteca nova;
  - o caso de uso `ExportSchedule`, a rota e a matriz de permissões.
- **Frontend:**
  - o "⋮" para todos;
  - o diálogo "Exportar em PDF";
  - a rota da página de impressão, com o `PrintTimeline` desenhado em SVG com os tokens do design system, fora do Syncfusion (ADR-0051).
- **Docs:** `CLAUDE.md` e roadmap.
