---
status: aceita (rodapé da barra lateral substituído pelo ADR-0066)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0011: Navegação híbrida, com barra lateral fixa e abas dentro do projeto

## Contexto e problema

O ideia.md fixa uma barra lateral sempre visível, com o chamado abrindo em página inteira ao lado dela. Alguns módulos atravessam projetos (a fila de chamados da TI), outros só fazem sentido dentro de um projeto (documentos, acessos a portais). Como organizar a navegação?

## Fatores de decisão

- Cada coisa no lugar em que é usada.
- Fila única de chamados para quem atende vários projetos.
- Tudo de um projeto acessível em um só lugar.
- Navegação previsível e sempre visível.

## Opções consideradas

- A. Híbrida: barra lateral com o que atravessa projetos e abas dentro de cada projeto
- B. Tudo dentro do projeto (estilo Jira e Azure DevOps)
- C. Só módulos globais, com filtro de projeto em cada um

## Resultado da decisão

Opção escolhida: **A. Híbrida**, porque a TI trabalha numa fila única de chamados e o gestor encontra tudo de um projeto em um só lugar.

Escopo:

| Item | Classificação |
| --- | --- |
| Barra lateral fixa: Início, Projetos, Chamados (lista geral com os filtros do MVP) e Administração (só para administradores) | v1 |
| Administração: Usuários, Auditoria e Configurações da instalação (nome, logo e cadastros como fornecedores e modelos, detalhados em cada módulo) | v1 |
| Rodapé da barra: nome da pessoa, troca de tema e "Sair" | v1 (substituído pelo ADR-0066: conta, tema e Sair no menu do avatar, no cabeçalho) |
| Abas dentro do projeto: Visão geral, Cronograma, Chamados, Documentos, Links, Acessos, Termos de aceite e Membros (lista final conforme as decisões de cada módulo) | v1 |
| Busca global (projetos, chamados pelo número ou título, documentos) | depois (na v1, cada lista tem a sua busca) |
| Barra lateral recolhível | fora (decisão de Marcelo: a barra é fixa e sempre expandida) |
| Caixa de e-mails simulada do MVP | fora (era só para demonstração) |

### Consequências

- Bom, porque a navegação é previsível e sempre visível.
- Bom, porque cada módulo fica onde é usado.
- Ruim, porque chamados aparecem em dois lugares (lista geral e aba do projeto), com a mesma regra de acesso nos dois.
- Ruim, porque a barra fixa ocupa espaço permanente na tela, o que pesa em telas com muito conteúdo, como o Gantt.

### Confirmação

Testes cobrindo: o item Administração só aparece para administradores; a lista de chamados do projeto e a lista geral aplicam as mesmas regras de acesso; as abas do projeto respeitam o papel da pessoa no projeto.

## Prós e contras das opções

### A. Híbrida

- Bom, porque serve tanto a quem atende vários projetos quanto a quem toca um projeto.
- Ruim, porque chamados aparecem em dois lugares.

### B. Tudo dentro do projeto

- Bom, porque é o modelo mais simples de entender.
- Ruim, porque a TI precisa entrar projeto por projeto para ver a fila.

### C. Só módulos globais

- Bom, porque concentra cada tipo de item em um lugar.
- Ruim, porque documentos, links e acessos, que são sempre de um projeto, ficam misturados.
