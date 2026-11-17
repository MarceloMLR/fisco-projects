## MODIFIED Requirements

### Requirement: Cores e fontes do design system

As cores das telas MUST vir só dos tokens do design system GerenWare, com valores próprios para o tema claro e o escuro; nenhuma cor MUST ser escrita direto no código. Sem cores da instalação, os valores MUST ser os da marca (ADR-0159): ação em esmeralda, neutros grafite, concluído em esmeralda e "em andamento" em azul-céu. As fontes MUST ser a IBM Plex Sans e a IBM Plex Mono, servidas pela própria aplicação, sem serviço de terceiros. Os pares de texto e fundo da estrutura, das etiquetas de status e dos selos de papel MUST ter contraste mínimo AA (4,5:1) nos dois temas.

#### Scenario: Cores dos tokens nos dois temas
- **WHEN** uma tela é exibida no tema claro ou no escuro
- **THEN** cada cor usada vem de um token do design system, com o valor desse tema

#### Scenario: Cor escrita direto no código
- **WHEN** o código do frontend traz uma cor escrita direto, fora da cópia dos tokens
- **THEN** os testes do frontend falham e apontam o arquivo

#### Scenario: Fonte servida pela aplicação
- **WHEN** uma tela é exibida
- **THEN** o texto usa a IBM Plex Sans, e os arquivos da fonte vêm do endereço da própria aplicação

#### Scenario: Contraste mínimo
- **WHEN** a estrutura é exibida no tema claro ou no escuro
- **THEN** cada par de texto e fundo da barra lateral, do cabeçalho e do conteúdo tem contraste de pelo menos 4,5:1

#### Scenario: Paleta da marca
- **WHEN** a instalação não escolheu nenhuma cor
- **THEN** o botão principal é esmeralda com texto branco nos dois temas, o texto e os fundos são da escala grafite, o concluído é esmeralda e o "em andamento" é azul-céu

#### Scenario: Contraste dos status e dos papéis
- **WHEN** uma etiqueta de status ou um selo de papel é exibido no tema claro ou no escuro
- **THEN** o texto tem pelo menos 4,5:1 sobre o fundo da etiqueta e sobre os fundos das telas, e a parte concluída de uma barra tem pelo menos 3:1 sobre o trilho

#### Scenario: Papéis da conversa em cores diferentes
- **WHEN** a conversa de um chamado mostra o responsável, o líder técnico e o time Funcional
- **THEN** o responsável fica em esmeralda, o líder técnico em azul e o time Funcional em oliva, com o matiz de cada papel a pelo menos 25 graus dos outros e a pelo menos 40 graus da esmeralda do responsável

#### Scenario: Alerta informativo em grafite
- **WHEN** uma tela mostra um alerta informativo
- **THEN** o alerta usa os tons neutros, e o azul-céu fica só nas etiquetas de "em andamento"

#### Scenario: Etiqueta de status com canto pequeno
- **WHEN** uma etiqueta de status é exibida
- **THEN** ela tem o canto de 3 px, o das barras e das etiquetas de papel
