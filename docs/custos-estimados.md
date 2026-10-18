# FiscoProjects: custos estimados no Azure

Registro para consulta e para a apresentação do projeto (pedido de Marcelo em 2026-10-15).

## Como os valores foram obtidos

- Data da consulta: 15/10/2026.
- Fonte: API de preços de varejo do Azure (Azure Retail Prices), consultada pela ferramenta de preços do Azure.
- Região: Brazil South.
- Preços de tabela, sem impostos e sem descontos. A tabela em reais da Microsoft equivale a cerca de R$ 5,22 por dólar.
- Meses de 730 horas.
- Os preços mudam: reconsultar antes de apresentar e informar a data da consulta.

## Premissas

- 5 a 6 pessoas usando, não simultaneamente, em horário comercial.
- Frontend (React) e backend (ASP.NET Core) no mesmo App Service.
- Microsoft 365 (Entra ID, SharePoint e Exchange Online) já existente na empresa, sem custo adicional para o FiscoProjects.
- Domínio próprio não incluído.
- Banco Azure SQL (ADR-0048).

## Preço por item

| Item | Descrição | R$/mês | US$/mês |
| --- | --- | --- | --- |
| App Service Linux B1 | 1 núcleo, 1,75 GB | 76,21 | 14,60 |
| App Service Linux B2 | 2 núcleos, 3,5 GB | 156,22 | 29,93 |
| App Service Linux P0v3 | Premium, com slots de deploy | 422,96 | 81,03 |
| Azure SQL Basic | 5 DTUs, até 2 GB | 30,92 | 5,93 |
| Azure SQL S0 | 10 DTUs, até 250 GB | 92,95 | 17,81 |
| Azure SQL S1 | 20 DTUs, até 250 GB | 185,90 | 35,62 |
| Azure SQL serverless (pago) | R$ 5,17 por hora de vCore; mínimo de 0,5 vCore acordado | variável | variável |
| Azure SQL oferta gratuita | 100 mil segundos de vCore e 32 GB por mês, por banco | 0,00 | 0,00 |
| Application Insights | faixa gratuita; acima dela, R$ 24,01 por GB | ~0 | ~0 |

## Cenários para 5 a 6 pessoas

| Cenário | Composição | R$/mês |
| --- | --- | --- |
| Recomendado | App Service B1 + Azure SQL S0 | ≈ 170 |
| Econômico | App Service B1 + Azure SQL Basic (sobe para S0 sem migração) | ≈ 107 |
| Desenvolvimento e demonstração no Azure | App Service B1 + Azure SQL na oferta gratuita | ≈ 76 |
| Desenvolvimento só na máquina local | Docker com SQL Server | 0 |
| Serverless pago (só comparação, não recomendado) | App Service B1 + serverless acordado ~200 h/mês | ≈ 450 ou mais |

Por que não serverless em produção: com uso em horário comercial o banco fica acordado cerca de 200 horas por mês, e os processos em segundo plano impedem a pausa automática. O custo passaria de R$ 375 por mês, podendo chegar a mais de R$ 1.700 se o banco nunca pausar.

## Crescimento

| Cenário | Composição | R$/mês |
| --- | --- | --- |
| Equipe maior | App Service B2 + Azure SQL S1 | ≈ 342 |
| Com deploy sem parada | App Service P0v3 + Azure SQL S1 | ≈ 609 |

## Comparação com PostgreSQL (em dólar, Brazil South)

| Cenário | Com PostgreSQL | Com Azure SQL |
| --- | --- | --- |
| Desenvolvimento e demonstração | ≈ US$ 47 (≈ US$ 15 nos 12 primeiros meses de conta nova) | ≈ US$ 15 |
| Produção pequena (B2) | ≈ US$ 62 | ≈ US$ 66 |
| Produção com folga (P0v3) | ≈ US$ 190 | ≈ US$ 170 |

## Outras observações

- Na região East US os preços são menores (PostgreSQL cerca de metade; demais itens de 15% a 30% menos), mas a latência aumenta e os dados ficam fora do Brasil.
- Licenças do Microsoft 365 não entram na conta, porque já existem na empresa.

## Frase para a apresentação

Operar o FiscoProjects para uma equipe de 5 a 6 pessoas custa cerca de R$ 170 por mês no Azure (ou cerca de R$ 107 na configuração econômica), aproveitando o Microsoft 365 que a empresa já tem.
