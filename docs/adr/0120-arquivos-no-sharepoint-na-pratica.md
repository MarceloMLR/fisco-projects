---
status: aceita
date: 2026-10-26
decision-makers: Marcelo Ribeiro
---

# ADR-0120: Como os arquivos ficam no SharePoint

Complementa o ADR-0030 (arquivos no SharePoint, na pasta de cada projeto), o ADR-0079 (módulo de arquivos com contrato único) e o ADR-0119 (o Microsoft Graph com a identidade do aplicativo).

## Contexto e problema

O contrato `IFileStorage` tem a implementação local desde a Acesso-4 (ADR-0079), e o ADR-0030 decidiu que os arquivos ficam no SharePoint da empresa, num site, biblioteca e pasta raiz configurados, com o aplicativo só lendo e escrevendo (Sites.Selected). Faltava decidir como o aplicativo chega ao SharePoint, como o administrador dá o site a ele, como os ids e as versões do contrato viram os do SharePoint e onde fica o endereço do site, que traz o nome do tenant.

## Fatores de decisão

- Nenhum segredo: o aplicativo já prova quem é pela identidade gerenciada do App Service (ADR-0117, ADR-0119).
- O menor acesso: só o site configurado, só arquivos, nunca permissões (ADR-0030).
- O mesmo contrato e os mesmos testes da implementação local (ADR-0079).
- Nada pessoal no repositório nem nos logs públicos (ADR-0116).

## Opções consideradas

Chegar ao SharePoint:

1. O Microsoft Graph, com o mesmo token de aplicativo da busca de pessoas, por `HttpClient`.
2. A API REST do próprio SharePoint, que, com token de aplicativo, só aceita certificado.

Dar o site ao aplicativo:

1. Um script do administrador, com o Microsoft Graph PowerShell, que cria a permissão de escrita no site.
2. O Terraform, que não tem recurso para permissões de site e precisaria da permissão `Sites.FullControl.All` na conta de quem aplica.

## Resultado da decisão

Graph pelo `HttpClient` e o script do administrador.

- **Token:** o `IGraphTokens` passa para o `Users.Contracts`, porque o `Users` é dono do aplicativo do Entra; o `Files` o recebe pelo contrato.
- **Permissão:** o Terraform dá ao aplicativo `Sites.Selected` com o consentimento do administrador. O `infra/scripts/Grant-SharePointSite.ps1`, rodado por quem administra o SharePoint, dá o site com o papel `write`: ler, criar e excluir arquivos e pastas, sem permissões.
- **Raiz:** o site é achado pelo endereço, a biblioteca pelo nome (ou a padrão do site), e a pasta raiz, quando configurada, é criada na primeira vez. Sem pasta raiz, os arquivos ficam na própria biblioteca, como num site só do sistema; num site que já tem outras áreas, a pasta raiz é a área dos projetos. Um id fora da raiz conta como inexistente, para nada de fora ser aberto ou excluído.
- **Ids e versões:** os ids do contrato são os dos itens do SharePoint, que não mudam ao renomear. A versão de um arquivo é a do SharePoint ("1.0", "2.0"); a nova versão é o envio com o mesmo nome ("replace"), e "manter os dois" tenta o nome seguinte enquanto o SharePoint diz que o nome já existe ("fail").
- **Detalhes:** o tipo do arquivo para o navegador sai do nome, como na implementação local. O envio vai inteiro, com o limite de cada módulo (até 25 MB, abaixo dos 250 MB do envio simples). A exclusão vai para a lixeira do site, e a de uma pasta confere o `eTag` lido. Quando o Graph limita as chamadas (429) ou está ocupado (503, 504), a chamada é repetida depois da espera pedida, até quatro vezes. As falhas dizem o código do Graph, nunca os nomes dos arquivos.
- **Configuração:** em `FileStorage:SharePoint` (`SiteUrl`, `Library`, `RootFolder`). O endereço do site vem do `local.auto.tfvars`, fora do Git, e a conferência da infraestrutura no pipeline o lê da configuração da aplicação no ar e o mascara antes de tudo, como o e-mail dos alertas (ADR-0116).
- **Testes:** um SharePoint simulado no processo (`FakeSharePoint`) responde ao Graph, e os testes do contrato valem para as duas implementações.

Ligar um projeto a uma pasta que já existe no SharePoint, prevista no ADR-0030, fica para uma change própria: hoje o sistema cria a pasta `{código} - {nome}` na raiz. Renomear o projeto não renomeia essa pasta: os arquivos já guardados continuam nela, porque os ids não mudam, mas uma pasta criada depois, como a dos anexos de um chamado novo, vai para uma pasta com o nome novo. Guardar uma pasta só por projeto fica para a mesma change.

### Consequências

- Boa, porque o aplicativo escreve só no site dado, sem segredo, e o contrato não muda para os módulos.
- Boa, porque o administrador vê e tira o acesso do aplicativo no próprio site.
- Ruim, porque dar o site é um passo à mão, fora do Terraform, que precisa do Microsoft Graph PowerShell.
- Neutra, porque o envio passa pela memória da aplicação, dentro dos limites que os módulos já impõem.
