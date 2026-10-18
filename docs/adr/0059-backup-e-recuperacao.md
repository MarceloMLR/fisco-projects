---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0059: Backups automáticos do Azure SQL ajustados, mantidos no Brasil e com teste de restauração

## Contexto e problema

O Azure SQL faz backups automáticos e permite restaurar o banco a qualquer ponto dentro de um período que, por padrão, é de 7 dias (configurável de 1 a 35 dias; no plano Basic, de 1 a 7). Também oferece retenção de longo prazo por até 10 anos. Por padrão, os backups ficam em armazenamento geo-redundante, copiados para a região par; a região par da Brazil South fica nos Estados Unidos, o que contraria a decisão de manter os dados no Brasil (ADR-0052). Como proteger os dados contra perda?

## Fatores de decisão

- Recuperar de erros e perdas de dados.
- Manter os dados no Brasil.
- Garantir que a restauração funciona de fato.
- Custo baixo.

## Opções consideradas

- A. Backups automáticos ajustados, com teste de restauração
- B. Só o padrão do Azure
- C. Backups próprios, com exportações agendadas

## Resultado da decisão

Opção escolhida: **A. Backups automáticos ajustados, com teste de restauração**.

Definições:

| Item | Definição |
| --- | --- |
| Restauração a qualquer ponto no tempo | 14 dias no plano S0 (o Basic chega a 7) |
| Retenção de longo prazo | Um backup por mês, guardado por 12 meses |
| Onde ficam os backups | Redundância por zona, dentro da Brazil South, em vez da cópia geo-redundante para a região par |
| Key Vault | Exclusão reversível e proteção contra exclusão definitiva |
| Estado do Terraform | Versionado (ADR-0053) |
| Arquivos no SharePoint | Histórico de versões e lixeira do Microsoft 365, conforme a política da empresa |
| Teste de restauração | A cada 3 meses, seguindo um roteiro documentado |
| Metas | Perder no máximo alguns minutos de dados e voltar ao ar em até 4 horas |

Todas as configurações ficam no Terraform (ADR-0053); a redundância dos backups é um parâmetro da instalação.

### Consequências

- Bom, porque os dados ficam protegidos e dentro do Brasil.
- Bom, porque o teste periódico confirma que a restauração funciona.
- Ruim, porque, se a região Brazil South inteira ficar indisponível, não é possível restaurar em outra região; a empresa que preferir pode trocar o parâmetro para geo-redundante.
- Ruim, porque a retenção de longo prazo tem custo de armazenamento (pequeno nesse volume).

### Confirmação

O Terraform define retenção, redundância por zona e retenção de longo prazo; o Key Vault tem proteção contra exclusão; o roteiro de restauração existe na documentação e há registro do último teste trimestral.

## Prós e contras das opções

### A. Automáticos ajustados com teste

- Bom, porque protege, mantém no Brasil e é verificado.
- Ruim, porque exige uma rotina de teste.

### B. Só o padrão

- Bom, porque não exige esforço.
- Ruim, porque guarda apenas 7 dias, copia os backups para os Estados Unidos e não verifica a restauração.

### C. Backups próprios

- Bom, porque dá controle total.
- Ruim, porque duplica o que o Azure já faz.
