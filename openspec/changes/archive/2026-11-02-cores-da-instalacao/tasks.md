## 1. Decisão e roadmap

- [x] 1.1 ADR-0146 aceito, os status do ADR-0082, do ADR-0100 e do ADR-0103 apontando para ele, e a Acesso-8.5 em andamento no roadmap

## 2. Domínio e API

- [x] 2.1 `InstallationSettings` com as três cores só pelo formato, sem a regra do contraste, e a migração `InstallationColors`; testes do domínio
- [x] 2.2 `ChangeInstallationColors`, `UpdateInstallation` sem a cor e `GetInstallation` com as três; testes dos casos de uso
- [x] 2.3 `PUT /api/installation/colors`, `GET /api/installation` com as três e `PUT /api/installation` sem `mainColor`. Testes:
  - "Salvar as cores";
  - "Cor sem contraste";
  - "Cor fora do formato pela API";
  - "Gerente de projeto não altera as cores", "Usuário não altera as cores" e "Cores sem sessão";
  - "Troca da cor na auditoria";
  - "Marca na página de entrada";
  - os testes existentes com o corpo novo.

## 3. Cores na tela

- [x] 3.1 `npm run api:update`. A derivação das três cores no `brandColors.ts` (`textOn`, `colorNotes`, `sidebarIsDark` pela cor) e os usos de `--on-action` fora da cor principal. Testes:
  - "Cor padrão";
  - "Contraste nos dois temas";
  - "Texto escuro sobre uma cor clara";
  - "Links legíveis com uma cor clara";
  - "Botão no tema escuro";
  - "Painel da entrada legível";
  - "Textos da barra lateral legíveis";
  - "Cores de status iguais".
- [x] 3.2 `BrandColors`, `Sidebar`, `favicon.ts` e a página de entrada com as três cores. Testes:
  - "Sem cor secundária";
  - "Trocar a cor secundária";
  - "Sem cor da barra lateral";
  - "Barra lateral escura no tema claro";
  - "Barra lateral clara";
  - "Logo escuro na barra lateral escura do tema claro";
  - "Selo na cor principal".
- [x] 3.3 `colorFormats.ts` e o `ColorPicker`. Testes:
  - "Cor em vários formatos";
  - "Valor que não é cor";
  - "Seletor pelo teclado";
  - "Cancelar o seletor";
  - "Escolher a cor pelo seletor";
  - "Seletor no celular".
- [x] 3.4 `ColorField`, `ColorsPreview` e `ColorsPage` em `/admin/colors`, na coluna da Administração. Testes:
  - "Cores na Administração";
  - "Prévia das cores antes de salvar";
  - "Prévia nos dois temas";
  - "Salvar as cores";
  - "Descartar as cores";
  - "Trocar a cor principal";
  - "Voltar ao padrão".
- [x] 3.5 Configurações gerais sem a cor e com a prévia nas cores salvas; a Auditoria com as cores novas. Testes "Configurações gerais na Administração", "Prévia antes de salvar" e "Descartar alterações" atualizados.
- [x] 3.6 Conferência no navegador, no computador e no celular, nos temas claro e escuro: o seletor arrastado, as três cores aplicadas e a barra lateral escura com o logo escuro.

## 4. Documentação

- [x] 4.1 `CLAUDE.md` (o `Administration`, as cores da instalação e a seção Cores), `docs/glossario.md` e o README dos ADRs
