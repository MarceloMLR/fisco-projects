---
status: aceita
date: 2026-10-27
decision-makers: Marcelo Ribeiro
---

# ADR-0122: Escolher uma pasta que já existe para o projeto

Complementa o ADR-0030 (cada projeto é ligado a uma pasta, escolhida entre as existentes ou criada pelo sistema), o ADR-0043 (permissões no projeto) e o ADR-0121 (uma pasta por projeto, renomeada com ele).

## Contexto e problema

Desde o ADR-0121, a pasta de cada projeto é sempre criada pelo sistema, como `{código} - {nome}` na pasta dos projetos. A empresa pode já ter, no SharePoint, uma pasta para o cliente ou para o projeto, e o ADR-0030 previu ligar o projeto a ela. Faltava decidir quando a pasta pode ser escolhida e trocada, quais pastas são aceitas, quem decide e o que acontece com a pasta escolhida quando o projeto muda de nome ou é excluído.

## Fatores de decisão

- Um projeto nunca fica dividido em duas pastas (ADR-0121).
- O sistema não apaga nem renomeia o que a empresa criou (ADR-0030, ADR-0041).
- Uma pasta de projeto não fica dentro da de outro, nem os mistura.

## Opções consideradas

Trocar a pasta depois da criação:

1. Só enquanto o projeto não guardou nenhum arquivo.
2. A qualquer momento, movendo os arquivos para a pasta nova.
3. Nunca: só na criação.

## Resultado da decisão

Opção 1, com Marcelo, que escolheu poder escolher na criação e na edição.

- **Quando:** na criação, por quem cria o projeto; depois, pelo administrador e pelo gerente do projeto, enquanto ele está aberto e não guardou nenhum arquivo (documento, termo ou anexo). Mover arquivos fica para depois, se for preciso.
- **Permissão:** "escolher a pasta" entra na matriz do ADR-0043: o administrador e o gerente podem; o líder técnico, o colaborador, o desenvolvedor e o leitor, não.
- **Pastas aceitas:** qualquer pasta que já existe dentro da raiz do armazenamento, menos a raiz, a pasta dos projetos e as pastas acima dela, a pasta do sistema (a dos logos), a pasta de outro projeto, as pastas dentro dela e as pastas acima dela.
- **A pasta escolhida é da empresa:** não é renomeada com o projeto nem apagada na exclusão; saem dela só os arquivos que o sistema guardou e as pastas vazias que ele criou. Se for apagada por fora, o projeto volta a ganhar a pasta criada pelo sistema no próximo arquivo.
- **Troca:** as pastas vazias que o sistema criou na pasta anterior saem, e a pasta anterior também, se foi criada pelo sistema e ficou vazia. A escolha e a troca entram na auditoria, pelo nome da pasta.
- **Na tela:** o diálogo "Escolher pasta" navega pelas pastas a partir da raiz e usa a pasta aberta; não cria, não renomeia nem exclui pastas.

### Consequências

- Boa, porque o projeto usa a estrutura que a empresa já tem no SharePoint.
- Boa, porque a regra de quais pastas servem fica num lugar só, para a lista, a criação e a troca.
- Ruim, porque, depois do primeiro arquivo, trocar a pasta exige mover os arquivos por fora e excluir e criar o projeto de novo.
