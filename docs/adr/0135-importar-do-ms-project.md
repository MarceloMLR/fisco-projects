---
status: aceita
date: 2026-10-31
decision-makers: Marcelo Ribeiro
---

# ADR-0135: Importar o cronograma do XML do MS Project no Novo projeto

O ADR-0021 deixou para depois da v1 a importação de arquivos do MS Project, ".mpp e XML, com prévia e avisos de conversão". Com a Linha do tempo própria (ADR-0134), Marcelo pediu a importação como a Gantt-3. Este ADR decide o formato, o lugar na tela e como o que o FiscoProjects não tem é convertido. Os níveis seguem o ADR-0088, as dependências o ADR-0019 e as datas o calendário da instalação (ADR-0018).

## Contexto e problema

Quem chega ao FiscoProjects costuma ter o cronograma no MS Project e não quer digitá-lo de novo. O MS Project grava em dois formatos:

- **.mpp:** binário e fechado. Só é lido por bibliotecas que o decifraram.
- **XML (MSPDI):** aberto, documentado e o mesmo que a exportação já grava (Cronograma-7). Ele é gravado pelo MS Project em "Salvar como", "XML".

O MS Project tem mais do que o FiscoProjects:

- **Níveis:** quantos se queira. O FiscoProjects tem fase, tarefa e subtarefa, num nível só de subtarefas (ADR-0088).
- **Dependências:** quatro tipos, com intervalos (lag), e dependências de tarefas-resumo. O FiscoProjects tem só "termina, então começa", sem intervalo, entre itens com datas próprias (ADR-0019, ADR-0088).
- **O que mais traz:** calendários próprios, restrições, recursos com custos, progresso e linha de base.

O servidor é a fonte das datas do cronograma (ADR-0051), contadas no calendário da instalação (ADR-0018).

## Fatores de decisão

- Nenhuma licença comercial ou copyleft forte, e o build sem baixar nada além dos pacotes do NuGet e do npm (ADR-0061, ADR-0090).
- O mesmo caminho do modelo de projeto (ADR-0017): o cronograma nasce com o projeto, em dias úteis do calendário da instalação, e a pessoa vê a prévia antes de criar.
- A pessoa sabe, antes de criar, o que mudou na conversão.
- Arquivo enviado pela pessoa é dado não confiável: nada de DTD nem entidades externas, com limites de tamanho.

## Opções consideradas

### Formato

- **F1.** Só o XML do MS Project, com um leitor próprio (`System.Xml.Linq`, como a exportação).
- **F2.** XML e .mpp pela MPXJ (`MPXJ.Net`, LGPL-2.1, a biblioteca Java convertida pelo IKVM no build).
- **F3.** XML e .mpp pela Aspose.Tasks (paga).

### Lugar

- **L1.** Uma terceira origem do cronograma no "Novo projeto", ao lado de "Em branco" e "A partir de um modelo".
- **L2.** "Importar do MS Project" no "⋮" da aba Cronograma, num projeto de cronograma vazio.

## Resultado da decisão

Opções escolhidas: **F1** e **L1**.

- **Formato.** Só o XML. Quem tem um .mpp abre no MS Project e salva como XML; a tela diz isso quando recebe um .mpp ou outro arquivo.
  - A MPXJ leria o .mpp, mas é LGPL, que a lista de licenças do NuGet não aceita hoje.
  - Ela também converte a biblioteca Java no build, baixando do Maven, que a verificação e a publicação não alcançam.
  - E ela pesa dezenas de MB a mais na aplicação.
  - Volta a ser opção, com ADR próprio, se o .mpp fizer falta.
- **Lugar.** "A partir do MS Project" no "Novo projeto", com o arquivo, o início e a prévia das fases e dos itens, como o modelo.
  - O início vem preenchido com o primeiro dia do arquivo e pode ser trocado, para usar um cronograma antigo como base.
  - A API lê o arquivo na prévia, sem guardar nada, e o projeto é criado com o cronograma convertido, na mesma transação, como o modelo.
- **O que vem do arquivo:** as fases, as tarefas, as subtarefas, os marcos, a ordem, as durações, as dependências "termina, então começa", o progresso e os responsáveis, a pedido de Marcelo, para trazer também projetos em andamento.
- **O que não vem:**
  - a linha de base;
  - os calendários e as restrições;
  - os recursos que não são pessoas (material e custo);
  - as notas.
- **Responsáveis.**
  - Cada recurso de trabalho do arquivo aparece na prévia, com o nome, o e-mail e quantas tarefas tem.
  - A prévia sugere a pessoa achada pelo e-mail do recurso no diretório da empresa, a mesma busca de pessoas do sistema (ADR-0119). Quem cria pode trocar a pessoa, deixar sem pessoa ou, num projeto com cliente, escolher o cliente (ADR-0102). Esta última é a sugestão quando o recurso tem o nome do cliente, como a exportação grava.
  - Para cada pessoa que ainda não está no projeto, quem cria escolhe o papel, Colaborador de início ou Desenvolvedor (ADR-0042). O gerente e o líder técnico do formulário mantêm o papel deles.
  - Na criação, essas pessoas entram no projeto com o papel escolhido, como na inclusão de pessoas (ADR-0005), e ficam responsáveis pelas tarefas dos seus recursos.
  - A tarefa com mais de um recurso fica com o primeiro (aviso). O recurso sem pessoa deixa as tarefas dele sem responsável (aviso). O marco não tem responsável (ADR-0021).
  - O e-mail dos recursos só serve para achar a pessoa e não é guardado.
- **Progresso.**
  - Cada tarefa com datas próprias leva o percentual concluído do arquivo.
  - A tarefa a 100% fica concluída no fim dela, nunca depois de hoje.
  - O marco a 100% fica atingido no dia dele, nunca depois de hoje.
  - A tarefa-resumo calcula o dela das subtarefas, como sempre (ADR-0088).
- **Níveis.**
  - As tarefas-resumo do nível 1 viram fases.
  - O nível 2 vira tarefa ou marco; uma tarefa-resumo do nível 2 vira tarefa com subtarefas.
  - O nível 3 vira subtarefa.
  - Do nível 4 para baixo, as tarefas sobem para subtarefas da tarefa do nível 2 que as contém, e as tarefas-resumo desses níveis saem (aviso).
  - As tarefas soltas no nível 1 entram numa fase com o título do projeto no arquivo (aviso).
  - A tarefa-resumo do projeto inteiro (nível 0) é ignorada.
- **Datas.**
  - Cada item começa no seu dia do arquivo, contado em dias úteis a partir do primeiro dia, a partir do início escolhido. O dia não útil vai para o dia útil seguinte.
  - Cada tarefa dura os dias úteis entre o início e o fim do arquivo, de 1 a 999. Uma tarefa mais longa fica com 999 (aviso).
  - O marco fica no seu dia.
  - Depois, como em toda mudança, as dependências empurram as seguintes (ADR-0019). As datas da prévia são as que o cronograma vai ter, calculadas pelo servidor.
- **Dependências.**
  - Entram as "termina, então começa" entre itens com datas próprias.
  - As de outro tipo e as de tarefas-resumo saem (aviso), e os intervalos saem, ficando a dependência (aviso).
  - Uma dependência que formaria ciclo sai (aviso).
- **Ignorados:** as tarefas inativas, as linhas vazias e as tarefas sem nome.
- **Limites.**
  - Arquivo de até 5 MB, com até 2.000 tarefas.
  - Nomes cortados no tamanho do FiscoProjects: 150 caracteres na fase e 200 no item (aviso).
  - Arquivo que não é XML do MS Project, com DTD, ou sem nenhuma tarefa dá 400, com a mensagem de salvar como XML.
- **Quem importa:** quem cria projetos (o administrador e o gerente de projeto, ADR-0040), como no "Novo projeto".

### Consequências

- Bom, porque quem vem do MS Project começa no FiscoProjects sem redigitar o cronograma, inclusive um projeto em andamento, com o progresso e os responsáveis, e com os avisos do que mudou.
- Bom, porque nenhuma dependência nova entra, e o leitor é testado como a exportação.
- Bom, porque as datas são as do calendário da instalação, como as de qualquer cronograma, e o projeto importado é editado como qualquer outro.
- Ruim, porque o .mpp precisa ser salvo como XML antes.
- Ruim, porque a linha de base fica de fora: quem edita a salva de novo no FiscoProjects.
- Ruim, porque ligar os recursos às pessoas e escolher os papéis é um passo a mais na prévia, quando o arquivo não tem os e-mails.
- Ruim, porque níveis, tipos de dependência e intervalos do MS Project se perdem; a prévia diz quais.

### Confirmação

- Os cenários de "Importar do MS Project" na spec do cronograma, com arquivos de exemplo nos testes, inclusive o XML que a própria exportação grava.
- O arquivo com DTD ou entidade externa é recusado, por teste.

## Prós e contras das opções

### F1. Só o XML, com leitor próprio

- Bom, porque é aberto, sem licença, e o mesmo formato da exportação.
- Ruim, porque o .mpp precisa de um passo a mais no MS Project.

### F2. MPXJ

- Bom, porque lê o .mpp direto e muitos outros formatos.
- Ruim, porque é LGPL, converte Java no build (Maven) e pesa dezenas de MB.

### F3. Aspose.Tasks

- Bom, porque lê o .mpp.
- Ruim, porque é paga, por desenvolvedor e por instalação, o problema que o ADR-0134 tirou.

### L1. No "Novo projeto"

- Bom, porque segue o modelo de projeto: o cronograma nasce com o projeto, com prévia.
- Ruim, porque um projeto já criado não importa; quem errou cria outro e exclui o primeiro.

### L2. Na aba Cronograma vazia

- Bom, porque serve a um projeto já criado.
- Ruim, porque é um segundo caminho, com as permissões do cronograma e o desfazer, para o mesmo resultado.
