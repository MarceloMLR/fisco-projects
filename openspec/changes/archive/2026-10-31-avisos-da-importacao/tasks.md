## 1. Roadmap

- [x] 1.1 Gantt-3.1 no roadmap, em andamento, com o grupo 13 em 4 changes

## 2. Conversão e API

- [x] 2.1 `MsProjectImport.Read` guarda os recursos de material e custo com o tipo; a conversão ganha os avisos `OtherResources`, `UnusedResources`, `InactiveTasks` e `BlankLines`, o `Count` de cada aviso e as quantidades do arquivo (`MsProjectCounts`); testes da conversão para cada aviso novo e para as quantidades
- [x] 2.2 A prévia da API traz `counts` e o `count` de cada aviso; testes "Prévia do arquivo do MS Project" (com as quantidades) e "O que fica de fora do arquivo"

## 3. Tela

- [x] 3.1 `npm run api:update`; o quadro da conversão sempre visível, com o resumo, os avisos novos, as tarefas sem responsável e o tom; a rolagem até o quadro depois de ler um arquivo novo; a nota de cada recurso sem pessoa; o atalho "N ajustes na conversão" na prévia; os textos em `pt-BR.json`; testes de tela "Quadro da conversão na tela", "Arquivo sem ajustes na tela" e "Tarefas sem responsável na tela", e os de antes continuando a passar
- [x] 3.2 Conferência no navegador, no computador e no celular, nos temas claro e escuro, com o arquivo de exemplo do que não cabe

## 4. Documentação

- [x] 4.1 `CLAUDE.md` com os avisos novos e as quantidades da prévia
