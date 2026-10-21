---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0110: Como a anonimização e o prazo da auditoria funcionam

Complementa o ADR-0058 (dados pessoais, anonimização a pedido e retenção), que decidiu o quê; este decide como, com as escolhas de Marcelo de 21/10/2026.

## Contexto e problema

O ADR-0058 decidiu anonimizar a pessoa mantendo o histórico, guardar a auditoria por 5 anos configuráveis e deixar o texto livre das mensagens de fora. Faltava decidir quando uma pessoa pode ser anonimizada, onde o nome dela fica guardado além do cadastro, como trocar tudo sem deixar a troca pela metade, o que a auditoria mostra depois e como o prazo é escolhido e aplicado.

## Fatores de decisão

- Nenhum nome, e-mail ou vínculo com o Entra da pessoa sobrando no banco, inclusive na auditoria (confirmação do ADR-0058).
- A troca inteira ou nada: uma falha no meio não pode deixar parte dos dados com o nome e perder o nome necessário para repetir.
- Cada módulo continua dono das próprias tabelas (ADR-0047).
- Evitar anonimizar alguém por engano, porque não dá para desfazer.

## Opções consideradas

1. Anonimizar só pessoa já desativada, numa transação só, com cada módulo apagando os próprios dados pessoais pela conexão da anonimização.
2. Anonimizar numa ação que também desativa, com cada módulo avisado depois de gravar, como os avisos (ADR-0038).
3. Apagar por um processo em segundo plano, que varre as tabelas depois.

## Resultado da decisão

Opção 1.

- **Quando**: só o administrador anonimiza, e só uma pessoa já desativada (dois passos, por escolha de Marcelo). Não dá para desfazer: a pessoa anonimizada não volta a ser ativada nem muda de papel.
- **O que muda no cadastro**: o nome vira o identificador da configuração `Users:AnonymousName` ("Pessoa removida nº {0}", com o número da anonimização, em sequência), o e-mail fica vazio, o papel volta a Usuário e os vínculos com o provedor de identidade são apagados. Os chamados, as mensagens, as tarefas e o histórico continuam, com o identificador no lugar do nome.
- **Nos outros módulos**, pelo contrato `IPersonalDataEraser` da persistência, na mesma transação, pela mesma conexão (como a auditoria já grava, ADR-0039):
  - Auditoria: os registros da pessoa e as entradas recusadas com o e-mail dela trocam o rótulo pelo identificador; os campos com o nome ou o e-mail dela passam a dizer só que mudaram, sem o valor, como uma senha (ADR-0033).
  - Chamados: as menções "@" à pessoa nas mensagens mostram o identificador (por escolha de Marcelo; o texto digitado continua de fora, ADR-0058).
  - Avisos: os e-mails para a pessoa são esquecidos (os que esperavam envio não saem mais); nos e-mails guardados de outras pessoas, o nome dela vira o identificador (por escolha de Marcelo); as notificações do sino e os itens do resumo dela são apagados.
- **O registro da anonimização** é a própria gravação do cadastro, com quem fez e quando, e os campos de nome e e-mail só como alterados.
- **Prazo da auditoria**: de 1 a 10 anos (padrão 5), em "Guardar auditoria por" nas Configurações gerais (por escolha de Marcelo). A limpeza roda uma vez por mês, no fuso da instalação, apaga os registros mais antigos que o prazo e, quando apaga algum, deixa na auditoria um registro de quantos apagou e desde que dia guarda.

### Consequências

- Boa, porque a troca é completa ou não acontece, e pode ser repetida depois de uma falha.
- Boa, porque cada módulo mexe só nas próprias tabelas, por um contrato.
- Ruim, porque o texto digitado nas mensagens, descrições e nomes de arquivos pode continuar citando a pessoa (ADR-0058).
- Neutra, porque o administrador desativa antes, num passo a mais.
