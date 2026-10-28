---
status: aceita
date: 2026-10-27
decision-makers: Marcelo Ribeiro
---

# ADR-0124: Foto da pessoa no avatar, lida do Microsoft 365

Substitui a regra "Fotos do Microsoft 365 não são usadas na v1" do ADR-0118 e do Avatar do design system. Complementa o ADR-0119 (diretório pelo Microsoft Graph) e o ADR-0058 (dados pessoais).

## Contexto e problema

O avatar mostra as iniciais da pessoa num círculo, na cor do papel quando a tela usa as cores (ADR-0069). Com a entrada pelo Entra ID, as pessoas da empresa já têm a foto no Microsoft 365, que todos da empresa veem no Outlook e no Teams. Em 27/10/2026, Marcelo pediu a foto no avatar, sem o contorno na cor do papel por enquanto, para validar na tela. De onde vem a foto e como ela chega às telas, que mostram muitos avatares de uma vez?

## Fatores de decisão

- A foto é dado pessoal (ADR-0058): quanto menos cópias guardadas, menos o que apagar na anonimização (ADR-0110).
- Nenhuma permissão nova no Entra ID: o aplicativo já lê os usuários do tenant (`User.Read.All`, ADR-0119), que também dá a foto.
- Uma tela com vinte avatares não pode virar vinte chamadas ao Graph a cada abertura.
- A entrada local e a demonstração não têm fotos, e as telas continuam iguais para elas.
- O avatar é decorativo: o nome está sempre ao lado (Avatar do design system).

## Opções consideradas

- A. Lida do Graph pelo servidor quando a tela pede, guardada só na memória por uma hora e no navegador por uma hora.
- B. Copiada para o banco na entrada da pessoa e atualizada de tempos em tempos.
- C. Enviada pela própria pessoa em Meu perfil.

## Resultado da decisão

Opção escolhida: **A. Lida do Graph quando a tela pede, guardada só na memória**, porque não deixa cópia da foto no banco, acompanha a troca de foto no Microsoft 365 e usa a permissão que o aplicativo já tem.

Regras:

- `GET /api/people/{id}/photo` (`GetPersonPhoto`, no `Users`), para quem tem sessão, como o nome e o e-mail do cartão (ADR-0118): a foto de 96x96 do usuário no Microsoft 365 ou, quando esse tamanho não existe, a maior, só JPEG ou PNG e até 1 MB.
- 404 quando o usuário não tem vínculo com o Entra ID (a entrada local, a demonstração e o anonimizado, que perde os vínculos), quando não tem foto e quando o Graph falha; a falha vai para o log como aviso, sem identificar a pessoa.
- O servidor guarda a resposta do Graph na memória por uma hora (a falha, por um minuto), pelo identificador do objeto no Entra ID; nada vai para o banco. O navegador guarda a resposta por uma hora (`private, max-age=3600`), inclusive o 404, sem renovar a sessão (`KeepSession`).
- Na tela, o avatar de uma pessoa mostra as iniciais e, quando a foto carrega, a foto cobrindo o círculo, sem contorno na cor do papel. Sem foto ou com erro, ficam as iniciais. O papel continua no selo e na cor das mensagens (ADR-0069).

### Consequências

- Bom, porque o banco não guarda fotos, e a anonimização não precisa apagar nenhuma: sem o vínculo, a foto deixa de sair na hora, e a cópia na memória expira em até uma hora.
- Bom, porque a foto trocada no Microsoft 365 aparece sozinha, em até duas horas.
- Bom, porque a mesma foto, numa tela com vários avatares da pessoa, é pedida uma vez ao navegador e uma vez por hora ao Graph.
- Ruim, porque, nos chamados, o avatar com foto perde a cor do papel; o selo e a cor da mensagem continuam dizendo o papel. Marcelo valida se volta o contorno.
- Neutro, porque cada instância da aplicação tem a própria memória, e duas instâncias pedem a mesma foto ao Graph.

### Confirmação

Testes da API com o Graph simulado: a foto de quem entrou pelo Entra ID, o tamanho maior quando o de 96x96 não existe, o 404 de quem entrou localmente, de quem não tem foto, do anonimizado e com o Graph fora do ar, a memória entre dois pedidos e o cabeçalho de cache. Testes de tela: a foto cobre as iniciais quando carrega e some quando falha.

## Prós e contras das opções

### A. Lida do Graph quando a tela pede

- Bom, porque não guarda cópia.
- Ruim, porque depende do Graph no ar; sem ele, as telas mostram as iniciais.

### B. Copiada para o banco

- Bom, porque as telas não dependem do Graph.
- Ruim, porque a foto vira mais um dado pessoal guardado, a apagar na anonimização, e envelhece até a próxima atualização.

### C. Enviada em Meu perfil

- Bom, porque funciona também sem o Microsoft 365.
- Ruim, porque repete o que a empresa já mantém no Microsoft 365 e exige moderar imagens enviadas.
