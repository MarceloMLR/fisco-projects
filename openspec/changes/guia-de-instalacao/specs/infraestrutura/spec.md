## ADDED Requirements

### Requirement: Guia de instalação

O `docs/instalacao.md` MUST levar quem instala, o fornecedor ou a TI do cliente, do que o cliente providencia até a primeira entrada do administrador, a partir do pacote da versão, e MUST ensinar a atualizar para uma versão nova e a voltar a uma anterior. Todo script de `infra/scripts` MUST aparecer no guia, e todo script citado nele MUST existir (ADR-0053, ADR-0127).

#### Scenario: Cada script no guia
- **WHEN** o guia de instalação é conferido
- **THEN** todo script de `infra/scripts` aparece nele, e todo script citado existe

#### Scenario: Guia do começo ao fim
- **WHEN** o guia de instalação é conferido
- **THEN** ele tem, nessa ordem, as partes "Antes de começar", "Quem instala", "Pacote da versão", "Instalar" e "Atualizar para uma versão nova"
