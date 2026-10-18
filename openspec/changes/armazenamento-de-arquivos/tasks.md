# Tasks

## 1. Contrato e regras de nomes

- [x] 1.1 Criar o módulo `Files` (projetos, solução e host) com o contrato `IFileStorage` e os dados devolvidos em `Files.Contracts`, e as regras de `FileNames` na `Application`; testar "Nome com caracteres não aceitos", nomes reservados, corte do nome e caminhos recusados; verificar com `dotnet build -warnaserror`, `dotnet test` e os testes de arquitetura

## 2. Implementação local

- [x] 2.1 Criar a implementação local em disco (pastas, envio com arquivo temporário, versões na pasta interna, abrir, excluir arquivo com as versões, excluir pasta só vazia, tipo pela extensão, caminhos sempre dentro da raiz) e os testes de contrato reaproveitáveis; testar "Pastas criadas no caminho", "Pasta que já existe", "Listar subpastas", "Enviar um arquivo", "Abrir um arquivo", "Arquivo inexistente", "Nova versão de um arquivo", "Manter os dois arquivos", "Excluir um arquivo", "Excluir uma pasta vazia", "Pasta com conteúdo não é excluída" e "Caminho que sai da raiz"; verificar com `dotnet test`
- [ ] 2.2 Criar as opções `FileStorage` (provedor e pasta local, validadas ao subir, local recusado em produção), registrar o módulo no host com `App_Data/files` no desenvolvimento e no `.gitignore`; testar "Implementação local no desenvolvimento" e "Implementação local em produção"; verificar com `dotnet test`

## 3. Verificação final

- [ ] 3.1 Atualizar o ADR-0079 no índice, o glossário, o guia de instalação (configuração dos arquivos), o guia de módulos e o `CLAUDE.md`; rodar todos os scripts de `scripts/ci` sem erro, abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions

## Workflow follow-up

- Marcar a Acesso-4 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo: `/opsx:archive` na mesma branch, ADR-0079 como aceita, roadmap como "Concluída" e merge.
