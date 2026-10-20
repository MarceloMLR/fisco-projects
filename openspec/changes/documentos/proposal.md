# Proposta: documentos (Documentos-1)

## Why

Os documentos dos projetos ficam soltos no SharePoint, sem status nem ligação com o cronograma. O ADR-0031 decidiu a área de documentos da v1, e o ADR-0045, quem faz o quê nela:

- **Pastas padrão:** todo projeto novo recebe as pastas configuradas na Administração. O padrão vem do MVP: 01 Gestão do projeto, 02 Contratos e compras, 03 Requisitos e especificações, 04 Atas de reunião, 05 Entregas e manuais e 06 Termos de aceite.
- **Versões:** arquivo com o mesmo nome na mesma pasta vira nova versão, com o histórico.
- **Status manual:** Rascunho, Em revisão, Aprovado e Obsoleto, com quem mudou e quando. Os obsoletos saem da lista padrão.
- **Vínculo:** cada documento pode ser ligado a uma fase, tarefa ou marco do cronograma.
- **Busca e visualização:** busca por nome no projeto; PDF, imagens, CSV e texto abrem no navegador, e qualquer arquivo pode ser baixado.
- **Exclusão:** só os gestores excluem; o termo assinado nunca.

## What Changes

- **Módulo novo `Documents`** (schema `documents`, ADR-0092): as pastas padrão, as pastas de cada projeto, os documentos e as versões. Os arquivos vão pelo `IFileStorage`, na pasta `{código} - {nome}/Documentos/{pasta}`.
- **Pastas padrão** (`/api/admin/document-folders`):
  - o administrador inclui, renomeia, desativa e reativa as pastas;
  - os nomes não se repetem, e a ordem é a do nome;
  - a pasta de termos de aceite não é desativada;
  - a mudança vale para os projetos novos.
- **Pastas do projeto:**
  - criadas a partir das pastas padrão ativas quando o projeto é criado, por um contrato novo do `Projects` (`IProjectSetup`);
  - o projeto criado antes recebe as pastas na primeira vez que a área é aberta.
- **Documentos** (`/api/projects/{id}/documents`):
  - lista por pasta ou busca no projeto, com o filtro "Todos, menos obsoletos" ou "Todos";
  - envio de um arquivo por vez, até `Documents:MaxMegabytes` (25), sem programas nem scripts;
  - nova versão para o mesmo nome na mesma pasta; a nova versão de um aprovado volta para Em revisão (ADR-0093);
  - mudança de status, com quem mudou e quando; só os gestores aprovam;
  - vínculo com uma fase, tarefa ou marco, por um contrato novo do `Projects` (`IScheduleItems`);
  - abrir no navegador (PDF, imagens, CSV e texto) e baixar, de qualquer versão;
  - exclusão pelos gestores, com o arquivo e as versões.
- **Permissões** (ADR-0045):
  - quem está no projeto vê e baixa; o leitor só vê;
  - a pasta de termos é só leitura e aparece só para o administrador e o gerente (ADR-0093);
  - o projeto encerrado é só leitura (ADR-0016).
- **Na tela:**
  - a aba "Documentos" do projeto, como no canvas ProjetoDocumentos, com a contagem;
  - as pastas numa coluna, a lista, "Enviar arquivos" e arrastar e soltar;
  - os detalhes numa gaveta (ADR-0081), com "Abrir", "Baixar", o "⋮" (status, vínculo, excluir), as propriedades e as versões;
  - "Pastas padrão" na Administração, desenhada como "Fornecedores".
- **Demonstração:** documentos fictícios nas pastas do ERP-FIN, com versões, status e vínculos.

## Capabilities

### New Capabilities

- `documentos`: pastas padrão, pastas do projeto, documentos, versões, status, vínculo com o cronograma, visualização, exclusão e as permissões.

### Modified Capabilities

- `dados-de-demonstracao`: documentos na demonstração.

## Impact

- **Backend:**
  - o módulo `Documents`, com as tabelas `default_folders`, `folders`, `documents` e `document_versions` e a migração;
  - os contratos `IProjectSetup` e `IScheduleItems` no `Projects.Contracts`;
  - as regras de tipo de arquivo (`FileTypes`) passam para o `Files.Contracts`, e os anexos dos chamados as usam.
- **Frontend:** a aba Documentos, a página "Pastas padrão" e o componente `SideNav`, a coluna de navegação já usada em Chamados.
- **Documentação:** ADR-0092 e ADR-0093.

## Non-goals

- Subpastas, edição de arquivos do Office no navegador, fluxo de aprovação com aprovador e busca pelo conteúdo (depois, ADR-0031).
- Escolher a pasta do projeto no SharePoint entre as existentes, que chega com o SharePoint real (M365-3).
- Os termos de aceite e os termos assinados, que chegam na Documentos-5.
- Mover e renomear documentos, que nenhum ADR prevê.
- Avisos de documentos: nenhum evento de documento está na tabela do ADR-0037.
