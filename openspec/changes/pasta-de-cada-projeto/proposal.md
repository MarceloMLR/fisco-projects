## Why

Hoje cada módulo monta o caminho da pasta do projeto pelo código e pelo nome (`{código} - {nome}`) toda vez que cria uma pasta. Quando o projeto muda de nome, o que já foi guardado fica na pasta antiga, e o que é criado depois (os anexos de um chamado novo, uma pasta de documentos usada pela primeira vez) vai para uma pasta nova: o projeto fica dividido em duas pastas no SharePoint. Além disso, a pasta onde ficam os projetos só pode ser escolhida no Terraform, por quem instala, e cada empresa organiza o SharePoint do seu jeito ("Projetos", "Projects", "Clientes/Projetos"). Encontrado no teste de ponta a ponta da M365-3, com Marcelo (correção M365-3.1, fora do roadmap).

## What Changes

- ADR-0121: uma pasta por projeto no armazenamento, guardada pelo `Projects` e renomeada com o projeto, e a pasta dos projetos nas Configurações gerais.
- `Administration`: a "Pasta dos projetos" nas Configurações gerais, numa seção nova "Arquivos": o caminho, dentro da raiz do armazenamento, onde as pastas dos projetos novos são criadas (vazia, na própria raiz), conferido com as regras de nomes do armazenamento e registrado na auditoria. Mudar vale para os projetos que ainda não têm pasta.
- `Projects`: o projeto guarda a pasta dele no armazenamento, criada com o projeto, com as pastas de documentos, como `{código} - {nome}` dentro da pasta dos projetos; um projeto antigo adota a pasta `{código} - {nome}` que já existe na raiz. Os módulos pedem as pastas de dentro dela pelo contrato novo `IProjectStorage` do `Projects.Contracts`, e uma pasta apagada por fora é criada de novo.
- `Documents` e `Tickets`: os documentos, os pedidos de aceite e os anexos dos chamados passam a ser guardados dentro da pasta do projeto, pelo contrato, sem montar o caminho.
- Renomear: quando o código ou o nome do projeto muda, a pasta do projeto é renomeada no armazenamento, depois de gravar, e os arquivos continuam abrindo. Se já houver uma pasta com o nome novo ou o armazenamento falhar, a pasta fica como estava, com tudo nela, e a próxima edição do projeto tenta de novo.
- `Files`: o contrato ganha "renomear pasta", mantendo o id; a implementação local passa a ter ids que não mudam ao renomear, como o SharePoint, e os ids que ela já entregou continuam valendo; a remoção do que foi guardado para na pasta do projeto, que o `Projects` apaga por último, só se estiver vazia. A pasta dos projetos nunca é apagada.

## Capabilities

### New Capabilities

### Modified Capabilities
- `armazenamento-de-arquivos`: renomear pasta, mantendo o id, e parar a remoção das pastas vazias numa pasta dada.
- `configuracao-da-instalacao`: a pasta dos projetos e a seção Arquivos das Configurações gerais.
- `projetos`: a pasta de cada projeto, renomeada com ele, e o que sai do armazenamento na exclusão.

## Non-goals

- Escolher uma pasta que já existe no SharePoint para um projeto (M365-3.2, logo depois desta).
- Mover para a pasta nova os projetos que já têm pasta quando a pasta dos projetos muda, ou juntar as duas pastas de um projeto que já ficou dividido.
- Renomear as pastas de dentro do projeto (Documentos, Chamados, Pedidos de aceite) ou a pasta de documentos quando a pasta padrão muda de nome.
- Mostrar ou abrir a pasta do projeto no SharePoint pela tela.

## Impact

- `Files` (contrato, implementações local e do SharePoint, `StoredFilesRemoval`, `FakeSharePoint` e os testes do contrato), `Administration` (configuração, migração, API e testes), `Projects` (projeto, migração, `IProjectStorage`, renomear, exclusão), `Documents` e `Tickets` (onde guardam os arquivos e a exclusão), `IProjectDeletion` (recebe a pasta do projeto).
- Telas: a seção Arquivos das Configurações gerais (`SettingsPage`), com os tipos da API gerados de novo.
- `docs/adr`, `docs/glossario.md`, `docs/instalacao.md`, `CLAUDE.md` e `docs/roadmap.md` (M365-3.1 e M365-3.2).
- ADRs: 0030, 0041, 0079, 0111, 0120.
