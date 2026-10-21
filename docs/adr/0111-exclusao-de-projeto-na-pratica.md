---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0111: Como a exclusão de um projeto funciona

Complementa o ADR-0041 (exclusão definitiva de projeto pelo administrador), que decidiu o quê; este decide como os módulos apagam o que é deles, em que ordem e o que fica registrado.

## Contexto e problema

O ADR-0041 decidiu que só o administrador exclui um projeto, com tudo o que pertence a ele, depois de ver o que será apagado e digitar o código; que no armazenamento só sai o que o FiscoProjects criou; e que a auditoria registra quem excluiu e o resumo. O projeto tem dados em seis módulos (projetos e cronograma, chamados, documentos e termos, acessos, avisos e arquivos), cada um dono das próprias tabelas (ADR-0047), e os arquivos ficam fora do banco, onde não há transação. Faltava decidir como apagar tudo sem deixar o projeto pela metade nem arquivos sem dono, e o que a auditoria guarda.

## Fatores de decisão

- Os dados do banco, inteiros ou nada: uma falha no meio não pode deixar o projeto sem cronograma e com chamados, por exemplo.
- Nenhum arquivo criado pelo sistema esquecido no armazenamento, sem registro que aponte para ele.
- Cada módulo mexe só nas próprias tabelas, por contrato (ADR-0047).
- Repetir a exclusão depois de uma falha precisa terminar o trabalho.
- Uma exclusão é um fato só na auditoria, não centenas de registros de tarefas e mensagens.

## Opções consideradas

1. Em duas etapas: primeiro cada módulo apaga os próprios arquivos; depois, numa transação só, pela mesma conexão, cada módulo apaga as próprias linhas e o `Projects` apaga o projeto, com um registro de auditoria com o resumo.
2. Cada módulo apaga arquivos e linhas na própria transação, avisado depois que o projeto é apagado (como os avisos, ADR-0038).
3. Marcar o projeto como excluído e apagar tudo por um processo em segundo plano.

## Resultado da decisão

Opção 1.

- **Contrato `IProjectDeletion`** do `Projects.Contracts`, implementado por cada módulo que guarda algo do projeto: conta o que tem (para o resumo), apaga os arquivos e apaga as linhas na transação que recebe, sem depender do EF Core no contrato.
- **Primeiro os arquivos**: cada módulo apaga os arquivos que registrou (anexos dos chamados, documentos com as versões, termos) e depois as pastas que ficaram vazias, subindo até a raiz, sem nunca apagar a raiz nem uma pasta com algo dentro. Arquivo que já não existe é ignorado. Assim, uma falha deixa o projeto inteiro no banco, com algum arquivo faltando, e repetir a exclusão termina o trabalho; o contrário deixaria arquivos sem nenhum registro.
- **Depois os dados, numa transação serializável**: cada módulo apaga as próprias linhas pela conexão e pela transação do `Projects` (como a auditoria grava, ADR-0039) e devolve quantos itens apagou; por último, o `Projects` apaga o cronograma, as pessoas e o projeto.
- **Auditoria**: um registro só, "Excluiu um projeto", do administrador, com o código, o nome, o cliente e o status do projeto e quantos chamados, documentos, links, acessos, termos de aceite, tarefas e marcos e pessoas foram apagados. As linhas apagadas não geram registros próprios. Os registros anteriores ficam, e o projeto aparece neles como "—" (ADR-0084), até o prazo da auditoria (ADR-0110).
- **Avisos**: as notificações do sino e os itens do resumo diário do projeto são apagados; os itens do resumo passam a guardar o projeto. Os e-mails já na fila saem.
- **O que fica**: o cliente, os modelos de projeto salvos dele, os fornecedores e a sequência dos números dos chamados, que não são reaproveitados (ADR-0029).
- **Confirmação**: a tela mostra o resumo (`GET /api/projects/{id}/deletion`) e pede o código; a API também confere o código (`POST /api/projects/{id}/deletion`), para não excluir por engano por outro caminho.

### Consequências

- Boa, porque os dados do banco saem inteiros ou não saem, e repetir depois de uma falha termina a exclusão.
- Boa, porque nada criado pelo sistema fica no armazenamento sem registro, e nada de fora é apagado.
- Boa, porque a auditoria guarda o fato e o tamanho da exclusão num registro só.
- Ruim, porque uma falha entre as duas etapas deixa o projeto no ar com arquivos faltando até a exclusão ser repetida.
- Neutra, porque módulo novo que guarda algo de um projeto precisa implementar o contrato.
