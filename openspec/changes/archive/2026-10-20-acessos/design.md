# Design: acessos (Documentos-3)

## Context

- **ADR-0033:** senha cifrada com o Data Protection, propósito exclusivo, chaves no banco e nunca apagadas, leitura com chave revogada, cifra das chaves por certificado opcional.
- **ADR-0034:** portal, ambiente, endereço, login, senha e observação; máscara com "Mostrar" e "Copiar", sem registro de quem mostrou; visibilidade pelo projeto.
- **ADR-0045:** todos veem, mostram e copiam; todos, menos o leitor, cadastram e editam; só os gestores excluem e importam.
- **ADR-0092:** os acessos ficam no módulo `Credentials`.
- **O que já existe:**
  - o cookie da sessão usa o Data Protection padrão do ASP.NET Core, com as chaves fora do banco;
  - a auditoria já tem o campo secreto (`IsAuditSecret`), que registra só que mudou;
  - os links, com os grupos, a busca e o `SegmentedControl`, que servem de modelo para a tela.
- **Canvas ProjetoAcessos:**
  - as ferramentas: "Buscar por portal ou usuário", os ambientes, "Importar planilha" e "Novo acesso";
  - os grupos por ambiente, com a contagem;
  - as colunas Portal, Endereço, Usuário (com copiar), Senha (mascarada, com mostrar e copiar), Observação e "⋮".

## Goals / Non-Goals

**Goals:**
- o módulo `Credentials`, com a senha cifrada e as chaves no banco;
- a lista sem senhas e a senha só quando pedida;
- as permissões do ADR-0045 e a tela.

**Non-Goals:**
- a importação (Documentos-4);
- restringir a pessoas escolhidas, lembrete de troca, registro de quem viu, exportação e gerador;
- o Key Vault para as chaves (Fundação-6).

## Decisions

### 1. Data Protection só dos acessos

- **Separado do da sessão:** o módulo monta um provedor próprio do Data Protection, com o nome de aplicação `FiscoProjects.Credentials` e o repositório de chaves no banco do módulo. O cookie da sessão continua como está, sem depender do módulo.
- **Repositório de chaves:** o `CredentialKeyRepository` (`IXmlRepository`) lê e grava o XML de cada chave em `credentials.data_protection_keys`, por um escopo próprio do banco. As chaves nunca são apagadas, e a tabela fica fora da auditoria.
- **Propósito:** `FiscoProjects.Credentials.Password`.
- **Leitura:** pelo `IPersistedDataProtector.DangerousUnprotect` com `ignoreRevocationErrors`, para a senha cifrada com chave revogada continuar legível (ADR-0033).
- **Certificado (opção da instalação):**
  - `Credentials:KeyCertificate:Path` e `Credentials:KeyCertificate:Password` (um `.pfx`), ou `Credentials:KeyCertificate:Thumbprint` (o certificado instalado na máquina);
  - com ele, as chaves novas saem cifradas pelo certificado (`ProtectKeysWithCertificate`);
  - sem ele, as chaves ficam sem cifra, como o ADR-0033 aceita, e o guia de instalação recomenda o certificado em produção.
- **Interface na Application:** `IPasswordProtector`, com `Protect` e `Unprotect`, para os casos de uso não conhecerem o Data Protection.

### 2. Acessos (`portal_credentials`)

- **Campos:** o projeto, o portal (até 120), o ambiente (até 60), o endereço (opcional, http ou https, até 2.000), o usuário (até 200), a senha cifrada, a observação (opcional, até 500), quem incluiu e quando, quem mudou por último e quando.
- **Ambiente:** texto, não uma lista da Administração. Os ambientes já usados no projeto viram sugestão no diálogo, e o grupo da tela é o texto.
- **Auditoria:**
  - o portal é o rótulo e o projeto é o contexto;
  - a senha é secreta (`IsAuditSecret`): o registro diz só que mudou (ADR-0033);
  - quem incluiu e as datas ficam de fora.

### 3. API

- **Lista:** `GET /api/projects/{id}/credentials?search=&environment=`:
  - traz os acessos sem a senha, na ordem do ambiente e do portal;
  - a busca procura no portal e no usuário;
  - traz os ambientes usados no projeto, com a contagem, sem a busca;
  - traz o que a pessoa pode fazer (`canEdit`, `canDelete`), falso no projeto encerrado.
- **Senha:** `GET /api/projects/{id}/credentials/{credentialId}/password`, com `Cache-Control: no-store`. Sem registro de quem pediu (ADR-0034).
- **Mudanças:** `POST`, `PUT .../{credentialId}` (com a senha nula para manter) e `DELETE .../{credentialId}`.
- **Logs:** nenhuma rota registra o corpo da requisição; a senha não entra em mensagens de erro.

### 4. Permissões (`CredentialAccess`, Domain)

- **Ver, mostrar e copiar:** todos do projeto e o administrador.
- **Incluir e editar:** todos, menos o leitor.
- **Excluir:** administrador, gerente e líder técnico (e importar, na Documentos-4).

### 5. Tela

- **Aba "Acessos":** depois de "Links", com a contagem; rota `/projects/:id/credentials`.
- **Tabela (`DataTable` com linhas de grupo):** um grupo por ambiente; Portal, Endereço (sem o protocolo, abre em nova aba), Usuário (com "Copiar usuário"), Senha (máscara de pontos, "Mostrar senha"/"Esconder senha" e "Copiar senha"), Observação e o "⋮".
- **Mostrar:** pede a senha à API e a mostra no lugar da máscara até "Esconder senha" ou até a pessoa sair da aba; nada fica guardado no cache das consultas.
- **Copiar:** pede a senha e a põe na área de transferência sem mostrá-la, com a mensagem "Senha copiada".
- **Diálogo:** formulário RHF com Zod: Portal, Ambiente (com as sugestões num `datalist`), Endereço, Usuário, Senha (com "Mostrar senha"; na edição, em branco mantém) e Observação.
- **Celular:** a coluna Portal traz o usuário embaixo, e ficam a Senha e o "⋮".

### 6. Demonstração

- Acessos fictícios do ERP-FIN em Produção, Homologação e Fornecedores, com senhas fictícias geradas pelo plano.

## Risks / Trade-offs

- **[Sem certificado, chaves legíveis]** Quem levar o banco inteiro leva as chaves. Mitigação: o certificado em produção, pelo guia de instalação (ADR-0033).
- **[Senha na tela]** A senha mostrada fica na tela até esconder. Mitigação: começa mascarada e volta à máscara ao sair da aba (ADR-0034).
