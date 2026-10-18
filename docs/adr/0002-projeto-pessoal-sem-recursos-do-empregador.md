---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0002: Tratar o FiscoProjects como projeto pessoal, sem recursos do empregador

## Contexto e problema

O FiscoProjects (nome provisório anterior: Marco) nasceu como ideia de ferramenta interna da empresa onde Marcelo trabalha, com a intenção de formalizar o projeto com a liderança e ter tempo alocado. A decisão agora é desenvolvê-lo como projeto pessoal, com possibilidade de o código ser adquirido por uma empresa no futuro. Quem é o titular do código e que regras garantem essa titularidade?

A Lei do Software (Lei nº 9.609/1998, art. 4º, § 2º) atribui os direitos exclusivamente ao empregado quando o programa é criado sem relação com o contrato de trabalho e sem uso de recursos, informações tecnológicas, segredos de negócio, materiais, instalações ou equipamentos do empregador. Quando o programa decorre da natureza das funções do empregado, a regra (caput do art. 4º) favorece o empregador.

## Fatores de decisão

- Poder vender ou licenciar o código no futuro sem disputa de titularidade.
- Não expor dados de clientes nem informações da empresa.
- Manter aberta a possibilidade de um acordo com a empresa mais tarde.

## Opções consideradas

- A. Projeto pessoal de verdade
- B. Projeto da empresa
- C. Acordo por escrito com a empresa (por exemplo, titularidade de Marcelo e licença de uso para a empresa)

## Resultado da decisão

Opção escolhida: **A. Projeto pessoal de verdade**, porque é a única que garante a titularidade do código sem depender de negociação.

Regras:

- Desenvolver fora do expediente, em máquina e contas pessoais: GitHub, tenant Microsoft e conta do Claude pessoais, nunca as pagas pela empresa.
- Não usar dados reais: o Excel de acessos, nomes de clientes e chamados reais ficam de fora. Dados de demonstração são fictícios.
- Fornecedores (como a Avalara) são apenas registros cadastráveis, nunca nomes fixos no código.
- Antes de oferecer o código a qualquer empresa, sobretudo ao empregador, pedir a um advogado que analise o contrato de trabalho. Esta decisão não é parecer jurídico.

### Consequências

- Bom, porque Marcelo mantém todos os direitos e pode vender ou licenciar o código.
- Bom, porque nenhum dado de cliente ou da empresa entra no repositório.
- Ruim, porque não há tempo alocado nem usuários reais da empresa no começo: o uso inicial fica com Marcelo e pessoas convidadas.
- Saem do plano do ideia.md: "formalizar o projeto com a liderança" e "conferir a política de IA da empresa".

### Confirmação

O repositório fica em conta pessoal; o seed e os testes usam apenas dados fictícios; uma revisão antes de cada release confirma que não há nomes de clientes, da empresa ou de fornecedores fixos no código.

## Prós e contras das opções

### A. Projeto pessoal de verdade

- Bom, porque a titularidade é clara.
- Bom, porque permite vender para qualquer empresa.
- Ruim, porque o desenvolvimento acontece só no tempo livre.
- Ruim, porque não há usuários reais desde cedo.

### B. Projeto da empresa

- Bom, porque traz tempo alocado, usuários reais e visibilidade interna.
- Ruim, porque o código pertence à empresa e não pode ser vendido.

### C. Acordo por escrito com a empresa

- Bom, porque permite uso real na empresa sem perder a titularidade.
- Ruim, porque exige negociação e advogado, e pode não ser aceito.
- Neutro, porque continua possível no futuro, por um novo ADR.

## Mais informações

- Lei nº 9.609/1998: https://planalto.gov.br/ccivil_03/leis/l9609.htm
