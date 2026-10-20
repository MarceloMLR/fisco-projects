# Proposta: acessos (Documentos-3)

## Why

Os acessos aos portais dos clientes (login e senha) ficam numa planilha com os acessos de todos os clientes. O ADR-0033 e o ADR-0034 decidiram os acessos na v1, e o ADR-0045, quem faz o quê:

- **Acessos por projeto:** portal, ambiente, endereço, login, senha e observação.
- **Senha cifrada:** com o Data Protection, num propósito só dos acessos, com o conjunto de chaves no banco, nunca apagado, e a cifra das chaves por certificado como opção da instalação (ADR-0033).
- **Senha mascarada:** aparece só com "Mostrar" e vai para a área de transferência com "Copiar", sem registro de quem mostrou ou copiou (ADR-0034).
- **Nunca por e-mail, nunca na auditoria nem em logs:** a auditoria registra só que a senha mudou (ADR-0033, ADR-0039).
- **Permissões:** todos do projeto veem, mostram e copiam; todos, menos o leitor, cadastram e editam; só os gestores excluem (ADR-0045).

## What Changes

- **Módulo novo `Credentials`** (schema `credentials`, ADR-0092): os acessos (`portal_credentials`) e o conjunto de chaves do Data Protection dos acessos (`data_protection_keys`).
- **Senhas cifradas:**
  - um Data Protection só dos acessos, separado do usado pelo cookie da sessão, com o propósito `FiscoProjects.Credentials.Password` e as chaves no banco;
  - a senha cifrada com chave revogada continua legível;
  - com `Credentials:KeyCertificate` (arquivo e senha, ou impressão digital), as chaves ficam cifradas pelo certificado; sem ele, o sistema funciona e as chaves ficam sem cifra (ADR-0033).
- **Acessos** (`/api/projects/{id}/credentials`):
  - a lista do projeto, sem as senhas, com a busca por portal e usuário e o filtro por ambiente;
  - a senha de um acesso só por `GET .../{credentialId}/password`, sem guardar no navegador;
  - incluir, editar (a senha em branco fica como estava) e excluir;
  - o ambiente é um texto, com os ambientes já usados no projeto como sugestão;
  - a auditoria com o projeto e só a marca de que a senha mudou.
- **Na tela:**
  - a aba "Acessos" do projeto, como no canvas ProjetoAcessos, com a contagem;
  - os acessos agrupados por ambiente, a busca, os ambientes num `SegmentedControl` e "Novo acesso";
  - em cada linha, copiar o usuário, a senha mascarada com "Mostrar senha" e "Copiar senha", e o "⋮" com editar e excluir.
- **Demonstração:** acessos fictícios do ERP-FIN em três ambientes.

## Capabilities

### New Capabilities

- `acessos`: acessos dos projetos, senha cifrada e mascarada, permissões e a tela.

### Modified Capabilities

- `dados-de-demonstracao`: acessos na demonstração.

## Impact

- **Backend:** o módulo `Credentials`, com as tabelas `portal_credentials` e `data_protection_keys` e a migração.
- **Frontend:** a aba Acessos.
- **Documentação:** CLAUDE.md, glossário, roadmap e instalação (o certificado das chaves).

## Non-goals

- A importação da planilha, que chega na Documentos-4.
- Restringir um acesso a pessoas escolhidas, lembrete de troca de senha (depois, ADR-0034).
- Registro de quem mostrou ou copiou, exportação e gerador de senhas (fora, ADR-0034).
- A cifra das chaves pelo Key Vault, que chega com a infraestrutura do Azure (Fundação-6); o certificado já cobre a opção do ADR-0033.
