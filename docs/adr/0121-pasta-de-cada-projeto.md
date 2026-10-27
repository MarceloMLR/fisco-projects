---
status: aceita
date: 2026-10-26
decision-makers: Marcelo Ribeiro
---

# ADR-0121: Uma pasta por projeto no armazenamento, renomeada com o projeto

Complementa o ADR-0030 (arquivos no SharePoint, na pasta de cada projeto), o ADR-0079 (módulo de arquivos com contrato único), o ADR-0111 (exclusão de projeto) e o ADR-0120 (os arquivos no SharePoint na prática).

## Contexto e problema

Cada módulo montava o caminho `{código} - {nome}` a partir da raiz sempre que criava uma pasta e guardava só o id da pasta de baixo. Quando o projeto mudava de nome, o que já estava guardado ficava na pasta antiga, e o que era criado depois ia para uma pasta nova: o projeto ficava dividido em duas pastas no SharePoint. A pasta onde ficam os projetos só podia ser escolhida no Terraform, por quem instala, e cada empresa organiza o SharePoint do seu jeito ("Projetos", "Projects", "Clientes/Projetos").

## Fatores de decisão

- O ADR-0030 liga cada projeto a uma pasta.
- O que varia por empresa é configuração, feita por quem administra o sistema (ADR-0003).
- O mesmo contrato e os mesmos testes nas duas implementações do armazenamento (ADR-0079).
- A alteração do projeto não pode depender de o SharePoint estar no ar.

## Opções consideradas

Quem guarda a pasta do projeto:

1. O `Projects`, dono do projeto, com um contrato para os outros módulos.
2. O `Documents`, dono das pastas de documentos, com o `Tickets` dependendo dele.
3. O `Files`, que não tem tabelas (ADR-0079).

Quando o projeto muda de nome:

1. Renomear a pasta, depois de gravar o projeto.
2. Nunca renomear, mantendo só a pasta pelo id.

Onde fica a pasta dos projetos:

1. Nas Configurações gerais, pelo administrador.
2. No Terraform, por quem instala.

## Resultado da decisão

O `Projects` guarda a pasta, a pasta é renomeada com o projeto, e a pasta dos projetos fica nas Configurações gerais, escolhidas por Marcelo.

- **Pasta do projeto:** o projeto guarda o id da pasta no armazenamento e o nome com que ela foi criada ou renomeada pela última vez. A pasta é criada com o projeto, com as pastas de documentos, como `{código} - {nome}`, dentro da pasta dos projetos. Um projeto que ainda não tem pasta e cuja pasta `{código} - {nome}` já existe na raiz, de antes desta decisão, passa a usá-la. Uma pasta apagada por fora é criada de novo no próximo arquivo.
- **Contrato:** os módulos pedem as pastas de dentro do projeto pelo `IProjectStorage` do `Projects.Contracts` e guardam só os ids das pastas de baixo, como antes. Nenhum módulo monta mais o caminho pelo nome.
- **Renomear:** quando o código ou o nome muda, o `Projects` renomeia a pasta depois de gravar, pelo `IFileStorage.RenameFolderAsync`, que mantém o id da pasta e de tudo dentro dela. Se já houver uma pasta com o nome novo ao lado ou o armazenamento falhar, o projeto fica gravado, a pasta fica com o nome que tinha, e a próxima alteração dos dados do projeto tenta de novo.
- **Implementação local:** passa a dar ids que não mudam ao renomear, por um índice na pasta interna, como o SharePoint. Os ids que ela entregou antes, que eram os caminhos, continuam valendo, inclusive dentro de uma pasta renomeada depois.
- **Pasta dos projetos:** um caminho dentro da raiz do armazenamento, nas Configurações gerais, vazio por padrão (a própria raiz), conferido pelas regras de nomes do armazenamento e criado quando falta. Mudá-lo vale para os projetos que ainda não têm pasta. A raiz configurada no Terraform continua sendo o limite do que o aplicativo toca (ADR-0120).
- **Exclusão:** os módulos apagam o que registraram e as pastas que ficaram vazias até a pasta do projeto, que o `Projects` apaga por último, só se estiver vazia. A pasta dos projetos nunca é apagada.

Escolher uma pasta que já existe no SharePoint para um projeto, prevista no ADR-0030, fica para a M365-3.2.

### Consequências

- Boa, porque o projeto nunca mais fica dividido, e a pasta acompanha o nome dele.
- Boa, porque cada empresa escolhe onde ficam os projetos, sem passar por quem opera o Azure.
- Ruim, porque um projeto que já ficou dividido antes desta decisão continua dividido.
- Neutra, porque a pasta pode ficar por um tempo com o nome antigo, quando o SharePoint falha ou o nome já está em uso, sem perder nada.
