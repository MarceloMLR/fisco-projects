# Design v3: estrutura clara e telas na altura da janela

Data: 17/10/2026. Esta versão substitui a v2 ("moldura e folha") e responde aos ajustes do Marcelo. O canvas de telas e o design system FiscoProjects estão publicados com a v3. Nenhum ADR muda antes da aprovação.

## O que mudou desde a v2

**Estrutura.** Saíram a moldura azul e a folha. Ficaram:
- uma barra lateral cinza levíssimo, de cima a baixo, com o nome da instalação no topo, Início, Projetos, Chamados, Administração e Meus projetos;
- à direita, um cabeçalho invisível (da cor do conteúdo e sem linha), com a trilha, o sino com contador vermelho e o avatar.

O conteúdo fica direto no fundo branco e ocupa o resto da janela.

**Telas na altura da janela.** A janela não rola como um todo. Cada região longa rola por dentro, e as tabelas descem até o fim com a paginação colada embaixo. Isso vale para tabelas, a conversa do chamado, a linha do tempo e os painéis laterais. Testei cada tela em 1440×900 e em 1920×1080, sem sobra branca embaixo nem conteúdo encolhido no canto.

**Cronograma.** A linha do tempo agora se ajusta ao espaço:
- o período inteiro cabe na largura, sem barra de rolagem lateral;
- a grade, os fins de semana e a linha de hoje descem até o fim da área.

Isso vale na aba, no diálogo expandido e nas fases da Visão geral.

**Chamado.**
- Saiu o botão "Pedir informações". O status "Aguardando solicitante" continua no menu de status.
- A conversa tem rolagem própria, entre o cabeçalho e a caixa de resposta. O recolhimento de interações antigas foi mantido.
- Cada papel tem uma cor:
  - solicitante: cinza, à esquerda;
  - responsável: azul, a cor que você aprovou;
  - gerente do projeto: violeta;
  - líder técnico: verde-azulado;
  - time de desenvolvimento: ocre;
  - time funcional: oliva.
- Os avatares do painel "Pessoas" usam as mesmas cores e servem de legenda.

**Abrir chamado.**
- Tudo cabe numa tela, sem rolagem.
- Na primeira linha ficam o projeto, o time (só Funcional e Desenvolvimento, sem descrição) e a prioridade (sem o "resolver em X dias").
- Abaixo vêm o título e a descrição, que ocupa o espaço que sobra.
- Os anexos ficam num botão "Escolher arquivos", que aceita vários arquivos de uma vez.
- As margens laterais caíram para 32px.

**Chamados do projeto.** Voltou a ser a lista, a mesma tabela da página Chamados já filtrada pelo projeto. O botão Abrir chamado fica ali.

**Sem TI e sem filas.** Saíram a seção Filas da página Chamados, a opção TI na abertura e o cadastro "Filas de atendimento" na Administração.

**Progresso em verde.** A parte concluída das barras de progresso, da faixa de fases e das tarefas no cronograma é verde, inclusive no caminho crítico (que segue com contorno e restante em vermelho).

**Entrada.** A página de entrada é um cartão dividido, com sombra, sobre um fundo marinho profundo com dois brilhos e uma grade de pontos nas bordas:
- à esquerda, um degradê azul com o nome da instalação, uma frase e formas geométricas (anéis, pontos e cartões em camadas com uma marca de concluído e uma barra de progresso), sem o desenho do cronograma;
- à direita, "Entrar", o e-mail da empresa e o botão Entrar.

É o único lugar em que o marinho preenche uma área.

**Outras telas sem espaço vazio.**
- Links virou tabela por categoria.
- Membros virou lista por time com um painel da pessoa selecionada: papel, chamados e tarefas abertas, e "Passar pendências".
- Documentos, Acessos, Termos, Usuários, Auditoria e Clientes descem até o fim da tela.
- Meu perfil foi dividido em preferências e um painel com a conta e os projetos.

## Por tela

- **Meu trabalho.** Uma fila por urgência (Atrasado, Aguardando você, Hoje e segunda, Próximos 7 dias), com uma ação por linha. À direita, Meus projetos com a faixa de fases e Próximos marcos.
- **Notificações.** Abrem no sino, com itens agrupados por dia e um ponto vermelho nas não lidas.
- **Projetos.** Tabela com a faixa do cronograma e o desvio do prazo final.
- **Novo projeto.** Formulário com a largura disponível e a prévia do modelo ao lado. Inclui o campo Líder técnico.
- **Visão geral.** Quatro números (andamento, prazo final, chamados abertos, próximo marco), as fases na largura toda, "Precisa de atenção" e a equipe.
- **Cronograma.**
  - Linha do tempo ou Lista; Dias, Semanas ou Meses; Hoje; Exibir; Expandir; Nova tarefa.
  - O diálogo expandido cobre quase a tela inteira.
- **Chamados (página).** Visões à esquerda, com "Sem responsável nos meus times" em destaque, e a tabela que desce até o fim.
- **Chamado.** Cabeçalho com status, prioridade e prazo; a conversa por papel; a resposta fixa embaixo. Ao lado ficam Pessoas, Prazos, No fornecedor e os anexos da conversa.
- **Abrir chamado.** Descrito acima.
- **Documentos, Links, Acessos, Termos, Membros.** Listas que ocupam a tela, com o detalhe ao lado quando há um item selecionado.
- **Administração.** Usuários, Auditoria e Clientes em tabelas que ocupam a tela; Configurações com a prévia da marca.
- **Celular.** O cabeçalho é branco e o menu abre como gaveta. A conversa do chamado segue as cores por papel.

## ADRs a revisar se o design for aprovado

| ADR | O que muda |
| --- | --- |
| 0010 Meu trabalho | Saem os atalhos "Abrir chamado" e "Novo projeto". Entram a fila por urgência e Próximos marcos. Meus projetos ganha a faixa de fases. |
| 0011 Navegação | Nome, tema e Sair vão para o avatar no cabeçalho. Entram o cabeçalho com trilha e "Meus projetos" na barra lateral. |
| 0021 Recursos do Gantt | O nome para o usuário passa a ser "Linha do tempo". A linha do tempo se ajusta ao espaço. Entra a ação Expandir (diálogo quase de tela inteira). A parte concluída das tarefas fica verde. |
| 0023 Destinos dos chamados | Só dois destinos: Funcional e Desenvolvimento (antes "Técnico"). Saem as filas compartilhadas e a fila TI. Sai o responsável na abertura. Fica a atribuição automática: quem do time responder primeiro vira o responsável. Sai o "Assumir chamado". |
| 0027 Troca do responsável | Só o gerente e o líder técnico trocam o responsável, um a um ou em lote; quem atende não passa o chamado a um colega. |
| 0029 Abertura e lista | Abertura sem responsável e sem fila. "Abrir chamado" fica só na lista geral e na aba Chamados do projeto. Novas visões: "Sem responsável nos meus times" e "Que acompanho". |
| 0038 Sino | O sino sai da barra lateral para o canto superior direito, com contador vermelho. |
| 0040 Papéis | Sem filas, não há atendentes de fila; o papel "Equipe de TI" continua fora. |
| 0044 Permissões nos chamados | Sai a coluna "atendente de fila". "Assumir" sai da matriz. "Trocar o responsável" fica só para administrador e gestores (gerente e líder técnico). |
| 0062 e 0063 Direção e cores | A estrutura passa a ser clara. O marinho fica só no painel da entrada. Entram as cores por papel na conversa e o verde na parte concluída das barras. |
| 0065 Forma | Cantos de 3, 6, 10 e 12px conforme o tamanho da peça. |

**Novos ADRs propostos**
- Telas na altura da janela, com rolagem interna.
- Faixa de fases (ScheduleStrip).
- Campo Líder técnico no Novo projeto.
- Cores por papel na conversa do chamado.

## Decisões de 17/10/2026

1. **Atribuição automática:** fica. Quem do time responder primeiro vira o responsável, e o botão "Assumir" sai.
2. **Troca do responsável:** só o gerente e o líder técnico.
3. **Cronograma:** a parte concluída das tarefas também fica verde.
4. **Estrutura:** o nome da instalação vai para o topo da barra lateral, que ocupa a altura toda; o cabeçalho fica invisível, sem linha.
5. **Entrada:** sai o desenho do cronograma; entram fundo marinho com sombra no cartão e formas geométricas.

## Fontes

- [Eleken: exemplos de páginas de entrada (cartões com sombra, painéis de marca, fundos com degradê)](https://www.eleken.co/blog-posts/login-page-examples)

- [Jira: locais da tela do item (campos e painéis à direita)](https://developer.atlassian.com/cloud/jira/platform/new-issue-view-ui-locations)
- [Linear: Inbox](https://linear.app/docs/inbox)
- [Zendesk Community: cor diferente para respostas de terceiros no ticket](https://support.zendesk.com/hc/ja/community/posts/4408860714906-Change-the-color-of-a-third-party-reply-to-tickets)
