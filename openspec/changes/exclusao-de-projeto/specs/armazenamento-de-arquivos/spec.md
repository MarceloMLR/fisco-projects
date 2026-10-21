## ADDED Requirements

### Requirement: Pastas vazias acima

O armazenamento MUST dizer em que pasta está um arquivo ou uma pasta, para que, ao apagar o que registrou, um módulo apague também as pastas que ficaram vazias acima, uma a uma, até a raiz. A raiz e as pastas com qualquer conteúdo MUST nunca ser apagadas (ADR-0041, ADR-0111).

#### Scenario: Pasta de um arquivo
- **WHEN** um módulo pergunta a pasta de um arquivo ou de uma pasta
- **THEN** recebe a pasta onde ele está, nenhuma quando ele está na raiz, e nenhuma quando ele não existe

#### Scenario: Pastas vazias acima
- **WHEN** um módulo apaga o último arquivo de "ERP-FIN - Projeto/Chamados/#104" e pede as pastas vazias
- **THEN** "#104", "Chamados" e "ERP-FIN - Projeto" saem, se nada mais estiver nelas, e a raiz fica
