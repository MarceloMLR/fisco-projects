## MODIFIED Requirements

### Requirement: Conversa pela tela

A página do chamado MUST mostrar a descrição no topo e a conversa em duas colunas, o solicitante à esquerda e os demais à direita. Cada mensagem MUST ter a cor e o selo do papel de quem escreveu (o solicitante em rosa; sem cor de papel, cinza) e "(você)" na própria pessoa. A conversa MUST rolar por dentro, com a resposta fixa embaixo, o separador por dia e as interações recolhidas acima de 10 (ADR-0069, ADR-0104).

#### Scenario: Cores e lados por papel
- **WHEN** a gerente abre um chamado com mensagens do solicitante, do líder técnico e dela
- **THEN** a do solicitante fica à esquerda e as outras à direita, cada uma com o selo e a cor do papel, e a dela com "(você)"

#### Scenario: Solicitante em rosa e quem não tem papel em cinza
- **WHEN** o chamado tem mensagens do solicitante e do administrador, que não está no projeto
- **THEN** a do solicitante tem a cor dele, com o selo "Solicitante", e a do administrador fica no cinza neutro, com o selo "Administrador"

#### Scenario: Interações antigas recolhidas
- **WHEN** um chamado tem 14 mensagens
- **THEN** a conversa mostra as 10 últimas e "Mostrar 4 interações anteriores", que mostra as outras

#### Scenario: Responder pela tela
- **WHEN** quem atende digita uma resposta com uma menção e um anexo e escolhe "Enviar"
- **THEN** a mensagem aparece no fim da conversa, e o anexo, em "Anexos da conversa"

#### Scenario: Sem resposta para quem só vê
- **WHEN** um leitor abre o chamado
- **THEN** a conversa aparece sem a caixa de resposta, com "Acompanhar" no painel
