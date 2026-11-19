## ADDED Requirements

### Requirement: Marca do produto sem logo da instalação

Sem logo da instalação, a barra lateral, o painel da página de entrada e a prévia das cores MUST mostrar a marca do produto, dos arquivos de `docs/marca/`, sem redesenho: a logo GerenWare no lugar do nome, quando a instalação tem o nome do produto, ou o símbolo ao lado do nome. A marca MUST NOT mudar com as cores da instalação, e a página MUST trazer os ícones e o manifesto da marca (ADR-0159, ADR-0160).

#### Scenario: Arquivos iguais aos da marca
- **WHEN** um arquivo da marca usado pelas telas fica diferente do de `docs/marca/`
- **THEN** os testes do frontend falham e dizem qual

#### Scenario: Logo inteira com o nome do produto
- **WHEN** a instalação não tem logo e se chama GerenWare
- **THEN** a barra lateral e o painel da entrada mostram a logo GerenWare, sem o nome escrito ao lado, e quem usa leitor de tela ouve o nome

#### Scenario: Símbolo ao lado de outro nome
- **WHEN** a instalação não tem logo e tem outro nome
- **THEN** a barra lateral e o painel da entrada mostram o símbolo da GerenWare e, ao lado, o nome da instalação

#### Scenario: Logo para o fundo escuro
- **WHEN** a barra lateral é escura, pelo tema ou pela cor da instalação, e a instalação não tem logo e se chama GerenWare
- **THEN** a barra lateral mostra a logo feita para fundo escuro, e a clara mostra a de fundo claro

#### Scenario: Ícones do navegador e do celular
- **WHEN** alguém abre a página antes de qualquer resposta da API
- **THEN** a página já aponta o ícone da aba, o ícone para a tela do celular e o manifesto, todos da marca

#### Scenario: Frase da página de entrada
- **WHEN** alguém abre a página de entrada
- **THEN** o painel mostra, embaixo da marca, a frase "Implantação e suporte dos seus clientes em um só lugar." e o texto de apoio do manual

### Requirement: Ícone da aba do navegador

A aba do navegador MUST mostrar o logo da instalação, quando há; sem logo, MUST mostrar o símbolo da GerenWare, que a página já traz antes de a marca chegar. O ícone MUST mudar quando o logo muda, e o logo MUST entrar só depois de a marca chegar (ADR-0108, ADR-0160).

#### Scenario: Logo na aba
- **WHEN** a instalação tem logo e alguém abre uma tela
- **THEN** a aba do navegador mostra o logo

#### Scenario: Símbolo na aba sem logo
- **WHEN** a instalação não tem logo e alguém abre a página de entrada
- **THEN** a aba mostra o símbolo da GerenWare, com qualquer cor principal

#### Scenario: Logo no lugar do ícone da página
- **WHEN** a marca da instalação chega com um logo
- **THEN** a aba passa a ter um ícone só, o logo, no lugar do símbolo

## MODIFIED Requirements

### Requirement: Logo da instalação

O administrador MUST poder enviar, trocar e remover o logo da instalação, um PNG ou SVG de até 1 MB. Com logo, ele MUST tomar o lugar da marca do produto no topo da barra lateral, com até 28 px de altura, e no painel da página de entrada; sem logo, aparece a marca do produto (ADR-0160). Arquivo de outro formato, acima de 1 MB ou SVG com script MUST ser recusado. O logo MUST ser servido de forma que um SVG nunca execute scripts (ADR-0003, ADR-0030).

#### Scenario: Enviar o logo
- **WHEN** o administrador envia um logo PNG e salva
- **THEN** a barra lateral e a página de entrada mostram o logo no lugar da marca do produto

#### Scenario: Remover o logo
- **WHEN** o administrador remove o logo e salva
- **THEN** a barra lateral e a página de entrada voltam a mostrar a marca do produto

#### Scenario: Logo recusado
- **WHEN** o administrador envia um JPG, um arquivo acima de 1 MB ou um SVG com script
- **THEN** o envio é recusado com o motivo e o logo anterior continua

#### Scenario: Logo sem scripts
- **WHEN** alguém abre o endereço do logo
- **THEN** a resposta proíbe a execução de scripts e a troca do tipo do arquivo pelo navegador

### Requirement: Cor principal

O administrador MUST poder trocar a cor principal da instalação por qualquer cor em hexadecimal, ou voltar à cor padrão do design system. Com a nova cor MUST mudar, nos temas claro e escuro, o botão principal, os links, as abas, os controles marcados, o anel de foco, o item ativo da barra lateral e o avatar do responsável; as cores de status MUST NOT mudar. Nenhuma cor MUST ser recusada pelo contraste (ADR-0146).

#### Scenario: Cor padrão
- **WHEN** ninguém trocou a cor principal, a secundária nem a da barra lateral
- **THEN** as telas usam as cores do design system, sem nenhuma mudança

#### Scenario: Trocar a cor principal
- **WHEN** o administrador escolhe outra cor e salva
- **THEN** o botão principal, os links e o item ativo da barra lateral passam a usar a nova cor, nos dois temas, e as cores de status continuam as mesmas

#### Scenario: Contraste nos dois temas
- **WHEN** a instalação tem qualquer cor principal
- **THEN** o texto do botão principal, os links sobre as superfícies e o item ativo da barra lateral têm contraste de pelo menos 4,5:1, no tema claro e no escuro

#### Scenario: Cor sem contraste
- **WHEN** o administrador escolhe uma cor clara, como um amarelo, sem 4,5:1 com o branco, e salva
- **THEN** a tela aceita a cor com o aviso dos ajustes, a API a grava, e o botão principal fica amarelo com o texto escuro

#### Scenario: Voltar ao padrão
- **WHEN** o administrador escolhe "Voltar ao padrão" e salva
- **THEN** a instalação volta à cor padrão do design system

### Requirement: Logo para o tema escuro

O administrador MUST poder enviar, trocar e remover um logo para o tema escuro, opcional, com as regras do logo. A barra lateral escura (a do tema escuro, ou a do tema claro com uma cor da barra lateral escura) MUST mostrar o logo escuro, quando há; sem ele, o logo. A barra lateral clara MUST mostrar o logo. O painel da entrada MUST continuar com o logo (ADR-0100, ADR-0146).

#### Scenario: Logo para o tema escuro pela API
- **WHEN** o administrador envia o logo para o tema escuro e depois o remove
- **THEN** a marca traz o endereço dele enquanto existe, o logo continua o mesmo, e um arquivo que não é PNG nem SVG é recusado

#### Scenario: Enviar o logo para o tema escuro
- **WHEN** o administrador escolhe o logo para o tema escuro em Visual e salva
- **THEN** o logo aparece num quadro escuro, é enviado, e "Remover logo escuro" o tira

#### Scenario: Logo escuro na barra lateral escura
- **WHEN** a barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral e a prévia mostram o logo escuro, e o painel da entrada mostra o logo

#### Scenario: Logo claro na barra lateral clara
- **WHEN** o tema é o claro, a barra lateral é clara e a instalação tem os dois logos
- **THEN** a barra lateral e a prévia mostram o logo

#### Scenario: Sem logo escuro
- **WHEN** a barra lateral é escura e a instalação não tem o logo escuro
- **THEN** a barra lateral mostra o logo

#### Scenario: Logo escuro na auditoria
- **WHEN** o administrador envia o logo para o tema escuro
- **THEN** a auditoria registra o nome do arquivo

#### Scenario: Logo escuro na barra lateral escura do tema claro
- **WHEN** o tema é o claro, a cor da barra lateral é escura e a instalação tem o logo escuro
- **THEN** a barra lateral mostra o logo escuro

## REMOVED Requirements

### Requirement: Logo na aba do navegador

**Reason**: O selo com o ícone do cronograma na cor principal deixou de existir; sem logo, a aba mostra o símbolo da GerenWare (ADR-0160).

**Migration**: O requisito "Ícone da aba do navegador" cobre o logo da instalação na aba e o símbolo sem logo.
