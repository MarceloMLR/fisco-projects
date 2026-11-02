## MODIFIED Requirements

### Requirement: Marca visível sem sessão

O nome, o logo, o logo para o tema escuro e as três cores da instalação MUST poder ser lidos sem sessão, porque a página de entrada os mostra (ADR-0009). Nenhuma outra configuração MUST ser exposta sem sessão.

#### Scenario: Marca na página de entrada
- **WHEN** alguém sem sessão abre a página de entrada
- **THEN** a página mostra o nome, o logo, e o painel e o fundo na cor secundária da instalação ou, sem ela, na cor principal

### Requirement: Cor secundária

O administrador MUST poder escolher uma cor secundária, opcional, em hexadecimal, ou voltar ao padrão. Com ela MUST mudar, nos dois temas, a linha selecionada, a notificação não lida, a menção e o painel e o fundo da página de entrada. Sem ela, esses destaques MUST ficar os do design system, e o painel e o fundo da entrada MUST seguir a cor principal (ADR-0146, ADR-0147).

#### Scenario: Sem cor secundária
- **WHEN** a instalação não tem cor secundária
- **THEN** a linha selecionada, a notificação não lida e a menção usam as cores do design system, e o painel e o fundo da entrada usam a cor principal

#### Scenario: Trocar a cor secundária
- **WHEN** o administrador escolhe uma cor secundária e salva
- **THEN** a linha selecionada, a notificação não lida, a menção e o painel da entrada passam a usar o matiz dela, nos dois temas, e o botão principal continua na cor principal

#### Scenario: Painel da entrada legível
- **WHEN** a cor secundária é clara, como um amarelo
- **THEN** o fundo do painel da entrada fica mais escuro que ela, até o texto branco ter pelo menos 4,5:1

#### Scenario: Fundo da entrada na cor secundária
- **WHEN** o administrador escolhe uma cor secundária e salva
- **THEN** o fundo da página de entrada, atrás do cartão e do painel, fica num tom escuro do matiz dela, nos dois temas, no lugar do azul do design system

### Requirement: Tela Cores

A seção Instalação da coluna da Administração MUST ter Cores, logo depois de Configurações gerais, com os campos da cor principal, da secundária e da barra lateral, a prévia das telas com Claro e Escuro e a barra de ações com Descartar e Salvar alterações, habilitados só quando algo mudou. Só o administrador MUST abrir a seção e salvar as cores (ADR-0046, ADR-0146).

#### Scenario: Cores na Administração
- **WHEN** o administrador escolhe Cores na coluna da Administração
- **THEN** a tela mostra os três campos, cada um com a amostra, o valor, "Voltar ao padrão", onde a cor aparece e os avisos de ajuste, e a prévia

#### Scenario: Prévia das cores antes de salvar
- **WHEN** o administrador digita ou escolhe uma cor
- **THEN** a prévia mostra a barra lateral, o cabeçalho, os botões, a lista, a menção e o painel e o fundo da entrada com a cor na hora, e o resto da aplicação só muda depois de salvar

#### Scenario: Prévia nos dois temas
- **WHEN** o administrador escolhe Escuro na prévia
- **THEN** a prévia mostra as cores como ficam no tema escuro, sem mudar o tema da pessoa

#### Scenario: Salvar as cores
- **WHEN** o administrador muda as cores e escolhe Salvar alterações
- **THEN** as três cores são gravadas, a aplicação inteira passa a usá-las e a barra de ações fica desabilitada

#### Scenario: Descartar as cores
- **WHEN** o administrador muda as cores e escolhe Descartar
- **THEN** os campos e a prévia voltam às cores salvas e a barra de ações fica desabilitada

#### Scenario: Cor fora do formato pela API
- **WHEN** alguém manda à API uma cor que não é `#RRGGBB`
- **THEN** a API responde 400 no campo dessa cor e nada muda

#### Scenario: Gerente de projeto não altera as cores
- **WHEN** um gerente de projeto pede para trocar as cores da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Usuário não altera as cores
- **WHEN** um usuário pede para trocar as cores da instalação
- **THEN** a API responde 403 e nada muda

#### Scenario: Cores sem sessão
- **WHEN** alguém sem sessão pede para trocar as cores da instalação
- **THEN** a API responde 401 e nada muda
