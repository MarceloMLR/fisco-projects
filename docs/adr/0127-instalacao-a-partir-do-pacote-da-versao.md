---
status: aceita
date: 2026-10-29
decision-makers: Marcelo Ribeiro
---

# ADR-0127: Instalação na nuvem do cliente a partir do pacote da versão

Completa o ADR-0053 (infraestrutura como código), o ADR-0055 e o ADR-0125 (publicação), para as instalações que não são a de Marcelo.

## Contexto e problema

Cada empresa tem a própria instalação (ADR-0003), e um cliente pode querer o FiscoProjects na própria assinatura do Azure, junto do Microsoft 365 dele. A instalação de Marcelo é criada pelo Terraform a partir do repositório e publicada pelo pipeline do repositório, que monta o pacote da aplicação a cada publicação (ADR-0115, ADR-0125). O repositório é público, e os logs do GitHub Actions também (ADR-0116): um environment por cliente mostraria o nome da instalação nos logs, e os parâmetros dela teriam de estar no repositório. Em 29/10/2026, Marcelo perguntou como quem vende software instala na nuvem do cliente e escolheu partir de uma versão publicada, não de uma cópia do repositório. Como instalar e atualizar o FiscoProjects na assinatura de um cliente?

## Fatores de decisão

- O cliente recebe uma versão pronta, não o código-fonte para manter.
- Nenhum parâmetro de cliente no repositório nem nos logs públicos (ADR-0002, ADR-0116).
- As mesmas ferramentas da instalação de Marcelo: Terraform, os scripts de `infra/scripts` e o comando `migrate` (ADR-0053, ADR-0125).
- Quem opera pode ser o fornecedor ou a TI do cliente, que não precisa de .NET SDK, Node nem do repositório.
- Uma instalação pequena, com poucos clientes no começo.

## Opções consideradas

- A. Pacote de cada versão anexado à versão no GitHub, instalado e atualizado por quem opera, o fornecedor (padrão) ou a TI do cliente, pelo Terraform e por um script de publicação.
- B. Uma cópia do repositório para cada cliente.
- C. O pipeline deste repositório com um environment por cliente.
- D. Aplicativo gerenciado do Azure Marketplace (Azure Managed Application).

## Resultado da decisão

Opção escolhida: **A. Pacote da versão**, porque entrega ao cliente só a versão, usa o mesmo Terraform e o mesmo comando de migração da instalação de Marcelo e não põe nada do cliente no repositório nem nos logs.

Regras:

- **Pacote.** Cada versão publicada no GitHub numa tag `vX.Y.Z` ganha o `fiscoprojects-vX.Y.Z.zip`, montado pelo workflow `release` a partir do commit da tag e anexado à versão: a aplicação (`app.zip`), o arquivo `VERSION`, o Terraform sem os parâmetros dos ambientes deste repositório, os scripts de `infra/scripts` e os guias de instalação, restauração e custos. Só entra o que está no Git. O workflow não alcança o Azure.
- **Arquivos privados da instalação.** Os parâmetros de cada instalação (o `local.auto.tfvars`, com todos os valores, e o `backend.hcl` do estado) ficam numa pasta privada de quem opera e são copiados para a pasta de cada versão. Nunca vão para o repositório.
- **Quem instala.** O padrão é o fornecedor, com uma conta criada pelo cliente no tenant dele e os papéis da instalação, reduzidos depois aos das atualizações. A alternativa é a TI do cliente, com o pacote e o guia. Em qualquer caso, os scripts do SharePoint e do Exchange podem ser rodados pelos administradores do cliente.
- **Publicação à mão.** O `infra/scripts/Publish-Version.ps1` aplica as migrações pelo comando `migrate` do pacote, com a conta do Azure CLI de quem opera (do grupo dos administradores do banco), abrindo o firewall do banco só para o endereço dela e só durante as migrações, e publica o `app.zip`, esperando o `/health`.
- **Atualização.** Quem opera baixa o pacote da versão nova, copia os arquivos privados, aplica o Terraform da versão e roda o script. A volta a uma versão anterior é publicar o pacote dela: as migrações só acrescentam (ADR-0125).
- A instalação de Marcelo continua pelo `deploy-dev` e pelo `deploy-prod`.

### Consequências

- Bom, porque o cliente recebe uma versão marcada e repetível, e o fornecedor mantém um código só.
- Bom, porque nada do cliente fica no repositório público nem nos logs.
- Bom, porque a TI do cliente instala sem .NET SDK, Node ou Git: basta o Azure CLI, o Terraform, o PowerShell e o ASP.NET Core Runtime.
- Ruim, porque cada atualização de cliente é feita à mão por quem opera; um pipeline por cliente pede o repositório privado.
- Ruim, porque, enquanto o repositório for público, o pacote também é: ele só tem o que já está no repositório.
- Neutro, porque o pacote é montado de novo no `deploy-prod`, para a instalação de Marcelo, a partir do mesmo commit.

### Confirmação

Testes do pipeline: o workflow `release` só com `contents: write`, chamando os scripts; o pacote de uma versão com a aplicação, o `VERSION`, o Terraform, os scripts e os guias, sem os ambientes, o `local.auto.tfvars` e o estado; nada anexado numa tag que não é de versão. O `Publish-Version.ps1` rodado com o PowerShell 7 e substitutos do Azure CLI e do `dotnet`: a ordem dos passos, a conta do Azure CLI, a regra do firewall apagada mesmo com as migrações falhando e a parada sem a identidade da aplicação. O guia conferido contra a pasta `infra/scripts` e pela ordem das partes.

## Prós e contras das opções

### A. Pacote da versão

- Bom, porque separa o produto (a versão) da instalação (os parâmetros).
- Ruim, porque as atualizações dos clientes não são automáticas.

### B. Cópia do repositório por cliente

- Bom, porque não pede nada novo.
- Ruim, porque o cliente recebe o código-fonte, cada cópia se afasta das outras, e a TI do cliente precisa do .NET SDK e do Node para montar a aplicação.

### C. Pipeline deste repositório por cliente

- Bom, porque as atualizações seriam automáticas e aprovadas no GitHub.
- Ruim, porque, com o repositório público, os nomes das instalações e os parâmetros apareceriam nos logs e no repositório.

### D. Azure Marketplace

- Bom, porque o cliente instala pelo portal do Azure e paga pelo Azure.
- Ruim, porque pede conta de parceiro, modelos ARM próprios e muito mais preparo do que poucos clientes justificam agora.
