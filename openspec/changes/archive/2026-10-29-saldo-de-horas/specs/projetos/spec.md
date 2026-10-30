## MODIFIED Requirements

### Requirement: Excluir projeto

Só o administrador MUST poder excluir um projeto, de qualquer status, digitando o código do projeto para confirmar. A exclusão MUST apagar o projeto e tudo o que é dele em todos os módulos, sem que nada do projeto fique em nenhuma tabela. O cliente, os modelos de projeto e os fornecedores MUST ficar, e os números dos chamados apagados MUST não ser reaproveitados (ADR-0029, ADR-0041, ADR-0111).

#### Scenario: Excluir projeto
- **WHEN** o administrador exclui um projeto com cronograma, chamados com anexos, documentos, links, acessos, termos de aceite, horas apontadas, horas contratadas e horas adicionais, digitando o código
- **THEN** o projeto deixa de existir, nenhuma tabela guarda mais nada dele, os arquivos que o sistema criou saem do armazenamento, e o cliente e os modelos de projeto continuam

#### Scenario: Código errado
- **WHEN** o administrador pede a exclusão com um código diferente do código do projeto
- **THEN** a API recusa com 400 no campo do código, e nada é apagado

#### Scenario: Só o administrador exclui
- **WHEN** o gerente ou outra pessoa do projeto pede o resumo ou a exclusão
- **THEN** a API recusa com 403, e quem não está no projeto recebe 404

#### Scenario: Número de chamado não reaproveitado
- **WHEN** um chamado é aberto depois da exclusão de um projeto com chamados
- **THEN** ele recebe o número seguinte ao maior já usado, nunca um dos apagados
