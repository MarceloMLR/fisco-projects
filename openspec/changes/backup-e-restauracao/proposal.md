## Why

O ADR-0059 pede backups do Azure SQL ajustados e um roteiro de restauração testado a cada 3 meses, para voltar ao ar em até 4 horas perdendo no máximo alguns minutos de dados. As definições dos backups ficam no Terraform da Fundação-6, adiada pela assinatura do Azure; o roteiro pode ficar pronto agora.

## What Changes

- `docs/restauracao.md`: o que é guardado e onde, os cenários de recuperação (dados apagados por engano, banco perdido, arquivo no SharePoint, segredo no Key Vault, região fora do ar), com os comandos do Azure CLI, o que conferir depois da troca (e-mails repetidos, arquivos fora de sincronia, papéis) e o teste trimestral com a tabela de registro.
- `docs/instalacao.md` aponta para o roteiro.
- Roadmap: as definições dos backups (retenção, backup mensal, redundância por zona) entram na Fundação-6.

## Capabilities

### New Capabilities

### Modified Capabilities

## Impact

- Só documentação: `docs/restauracao.md`, `docs/instalacao.md` e `docs/roadmap.md`. Sem mudança de código nem de comportamento.
