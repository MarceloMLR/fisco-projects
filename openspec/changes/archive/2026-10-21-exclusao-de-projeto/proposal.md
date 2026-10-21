## Why

O ADR-0041 decidiu que o administrador pode excluir um projeto de vez, com todo o histórico, para limpar projetos de teste ou criados por engano. Hoje o projeto só é cancelado, e o que ele tem em cada módulo fica para sempre.

## What Changes

- ADR-0111: como a exclusão funciona — primeiro os arquivos, depois os dados de todos os módulos numa transação, e um registro só na auditoria com o resumo.
- Contrato `IProjectDeletion` (`Projects.Contracts`), implementado pelos chamados, documentos (com links e termos), acessos e avisos: conta o que o módulo tem do projeto, apaga os arquivos que ele registrou e apaga as linhas na transação recebida.
- `GET /api/projects/{id}/deletion` traz o resumo (chamados, documentos, links, acessos, termos de aceite, tarefas e marcos, pessoas) e `POST /api/projects/{id}/deletion` exclui com o código digitado, só para o administrador.
- O armazenamento passa a dizer a pasta de um arquivo ou de uma pasta, e as pastas que ficam vazias são apagadas até a raiz, nunca a raiz nem pasta com conteúdo.
- Os itens do resumo diário passam a guardar o projeto, para saírem com ele.
- Tela: "Excluir projeto" em "Ações do projeto", só para o administrador, com o resumo e o código para confirmar; a Auditoria diz a exclusão e o resumo.
- Demonstração: um projeto de teste para excluir.

## Capabilities

### New Capabilities

### Modified Capabilities
- `projetos`: exclusão do projeto pelo administrador, o resumo antes de excluir, a tela e o registro na auditoria.
- `armazenamento-de-arquivos`: a pasta de um arquivo ou de uma pasta, e as pastas vazias apagadas acima dela.

## Impact

- Backend: `Projects` (caso de uso, endpoints, permissão `CanDelete`), `Tickets`, `Documents`, `Credentials` e `Notifications` (implementações do contrato), `Files` (`FolderOfAsync`), `Notifications` (migração dos itens do resumo).
- Frontend: `ProjectLayout` (ação e diálogo), textos da Auditoria, consultas invalidadas.
- Demonstração: plano e carga.
