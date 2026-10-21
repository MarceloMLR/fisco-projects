## ADDED Requirements

### Requirement: Voltar do navegador

Um Voltar do navegador MUST voltar uma página. Um link ou botão dentro de uma linha de tabela MUST fazer só a ação dele, sem escolher a linha também; a gaveta aberta pelo endereço MUST abrir e fechar sem entrar no histórico; e o formulário salvo ou cancelado MUST ser trocado pela página seguinte (ADR-0105).

#### Scenario: Link dentro da linha
- **WHEN** a pessoa clica no link ou dá Enter nele, dentro de uma linha de tabela
- **THEN** só o link age, e a linha não é escolhida; o clique fora do link escolhe a linha

#### Scenario: Gaveta fora do histórico
- **WHEN** a pessoa abre e fecha o termo de aceite e depois volta uma vez
- **THEN** ela sai da aba de termos

#### Scenario: Novo projeto pela tela
- **WHEN** a pessoa cria um projeto
- **THEN** a página do projeto troca o formulário no histórico

#### Scenario: Cancelar o Novo projeto
- **WHEN** a pessoa cancela o Novo projeto
- **THEN** a lista de projetos troca o formulário no histórico

#### Scenario: Abrir chamado pela aba do projeto
- **WHEN** a pessoa abre um chamado pela aba do projeto e depois volta uma vez
- **THEN** ela volta à aba Chamados do projeto
