## Context

A Gantt-3 (ADR-0135) lê o XML do MS Project na prévia e devolve os avisos da conversão, mostrados num quadro abaixo do arquivo e do início, no "Novo projeto". No teste de Marcelo com tarefas inativas, uma linha em branco e recursos de material e custo, o quadro ficou abaixo da área visível (a 1366×768, o quadro começa em y≈906) e o que ficou de fora não gerou aviso, porque o ADR-0135 só os lista como ignorados. O aviso das tarefas sem responsável, que o ADR-0135 pede, também não foi feito.

## Goals / Non-Goals

**Goals:**

- Que quem importa veja, sem procurar, o que veio do arquivo e o que mudou.
- Avisar tudo o que fica de fora: tarefas inativas, linhas em branco ou sem nome, recursos de material e custo e recursos de trabalho sem tarefa no cronograma.
- Avisar as tarefas que ficam sem responsável, conforme a escolha de cada recurso.

**Non-Goals:**

- Guardar os avisos no projeto.
- Mudar as regras de conversão do ADR-0135.

## Decisions

- **Sem ADR novo.** As regras de conversão continuam as do ADR-0135; o aviso das tarefas sem responsável já está nele, e os outros avisos só dizem o que ele manda ignorar. Não há alternativa real a decidir.
- **Quantidades (`counts`).** A prévia conta, no servidor:
  - as tarefas do arquivo: as linhas abaixo do resumo do projeto que não estão em branco nem sem nome (as inativas contam);
  - as tarefas que vieram: as linhas que viraram fase, tarefa, subtarefa ou marco (a fase com o título do projeto, para as soltas, não é linha do arquivo e não conta);
  - os recursos do arquivo: os que têm nome, de qualquer tipo;
  - os recursos que vieram: os de trabalho que são o primeiro de alguma tarefa trazida, os mesmos da lista de responsáveis.
- **Avisos novos**, depois dos que já existem, cada um com a quantidade (`count`), que é a dos nomes, menos nas linhas em branco:
  - `OtherResources`: os recursos de material e de custo, pelos nomes. O leitor passa a guardá-los com o tipo, e a conversão os separa.
  - `UnusedResources`: os recursos de trabalho que não ficaram como o primeiro de nenhuma tarefa trazida (sem tarefa, só como segundo recurso, só em marco ou só em linhas que saíram).
  - `InactiveTasks`: as linhas com nome que ficam de fora por estarem inativas ou abaixo de uma inativa, pelos nomes.
  - `BlankLines`: as linhas em branco ou sem nome e as que estão abaixo delas, só pela quantidade, porque não têm nome para mostrar.
- **Tarefas sem responsável, na tela.** Quem decide a pessoa de cada recurso é a tela, e a escolha muda depois da prévia, então a conta fica no navegador, pelos responsáveis escolhidos: o recurso com "Sem responsável", com "Uma pessoa da empresa" sem a pessoa, ou com o cliente num projeto sem cliente. O quadro lista esses recursos com as tarefas de cada um, e a linha do recurso diz, embaixo do "Responsável", que as tarefas dele ficam sem responsável.
- **O quadro.** Aparece a cada leitura, com o resumo no título ("14 de 17 tarefas e 3 de 5 recursos vieram do arquivo"; sem recursos no arquivo, só as tarefas). Embaixo, a lista dos avisos, com as tarefas sem responsável por último, ou "Nada mudou na conversão.". O tom é de atenção (`warning`) quando há algum item na lista e informativo (`info`) quando não há.
- **Rolagem.** Depois de ler um arquivo novo, a tela rola até o quadro (`scrollIntoView` com `block: 'nearest'`, suave, ou na hora com o movimento reduzido). Ler de novo pelo início não rola, porque a pessoa já está perto do quadro. O foco não muda: o quadro é um `status`, anunciado pelo leitor de tela.
- **Atalho na prévia.** Com algum item na lista, a prévia mostra "N ajustes na conversão", com N igual aos itens da lista. O atalho rola até o quadro e põe o foco nele, para quem usa o teclado e o leitor de tela.

## Risks / Trade-offs

- [O tom de atenção em toda importação com ajustes] → É o que Marcelo pediu: o ajuste muda o cronograma e precisa ser visto. O arquivo que vem inteiro fica no tom informativo.
- [Linhas abaixo de uma linha sem nome contam como em branco] → É raro no MS Project e evita um aviso a mais só para esse caso.
