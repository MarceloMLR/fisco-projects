## MODIFIED Requirements

### Requirement: Carga dos dados de demonstração

O comando `npm run demo:data` MUST carregar, pela API indicada (padrão `http://localhost:5175`), as pessoas do diretório de demonstração com os papéis delas, clientes, projetos com gerente, líder técnico e membros, e os cronogramas, cada ação por quem a faria na tela e todo nome fictício. Projetos que já existem MUST ficar como estão: o comando carrega só os que faltam. Ao terminar, MUST dizer para abrir as telas em `http://localhost:5173` (ADR-0002, ADR-0003, ADR-0089).

#### Scenario: Carregar dados de demonstração
- **WHEN** o comando roda numa instalação de desenvolvimento sem os dados
- **THEN** o administrador cria os clientes e os projetos, a gerente de cada projeto monta o cronograma, cada responsável atualiza o progresso da própria tarefa, e o comando diz o que carregou e que as telas abrem em `http://localhost:5173`, com o `npm run dev`

#### Scenario: Só os dados que faltam
- **WHEN** o comando roda numa demonstração que já tem parte dos projetos do plano
- **THEN** o comando carrega só os projetos que faltam, com os clientes e fornecedores que faltam, e diz quais ficaram como estavam

#### Scenario: Dados já carregados
- **WHEN** o comando roda de novo e todos os projetos do plano já existem
- **THEN** o comando avisa que os dados já estão lá, diz como apagar o banco local e os arquivos para carregar de novo e onde abrir as telas, e não cria nem muda nada

#### Scenario: Sem entrada local
- **WHEN** o comando roda contra uma API com a entrada local desligada, como a de produção
- **THEN** o comando para com uma mensagem, sem criar nada

## ADDED Requirements

### Requirement: Demonstração mais rica

A carga de demonstração MUST incluir seis clientes e projetos em todos os status (Planejamento, Em andamento, Pausado, Concluído e Cancelado), um deles interno e um criado a partir de um modelo, projetos de uma segunda gerente e chamados, documentos, links, acessos e termos de aceite também fora do projeto principal (ADR-0017, ADR-0043, ADR-0089).

#### Scenario: Projetos em todos os status
- **WHEN** o comando carrega os dados de demonstração
- **THEN** a página Projetos mostra projetos em Planejamento, Em andamento, Pausado, Concluído e Cancelado, de seis clientes e internos, todos com chamados

#### Scenario: Projeto a partir de um modelo
- **WHEN** o comando carrega os dados de demonstração
- **THEN** o administrador cria um projeto a partir do modelo "Implantação de ERP", com o cronograma gerado pelo servidor a partir do dia escolhido

#### Scenario: Projeto cancelado com chamados
- **WHEN** o comando cancela um projeto com chamados ainda abertos
- **THEN** os chamados ficam fechados com o projeto, com o evento na conversa

#### Scenario: Segunda gerente
- **WHEN** o comando carrega os dados de demonstração e Beatriz Lima entra
- **THEN** os projetos dela aparecem com os cronogramas montados por ela, e o Meu trabalho dela mostra uma tarefa atrasada, uma da semana e um chamado aguardando a resposta dela
