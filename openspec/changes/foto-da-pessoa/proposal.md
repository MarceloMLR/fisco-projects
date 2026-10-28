## Why

Com a entrada pelo Entra ID, as pessoas da empresa já têm a foto no Microsoft 365, mas o FiscoProjects mostra só as iniciais. Marcelo pediu a foto no avatar, a princípio sem o contorno na cor do papel, para validar na tela (M365-5, fora do roadmap). O ADR-0124 substitui a regra "Fotos do Microsoft 365 não são usadas na v1" do ADR-0118 e do design system.

## What Changes

- `GET /api/people/{id}/photo` traz a foto da pessoa no Microsoft 365, lida pelo Graph com o token do aplicativo, guardada só na memória do servidor por uma hora e no navegador por uma hora; 404 sem vínculo com o Entra ID, sem foto ou com o Graph fora do ar.
- O avatar de uma pessoa mostra a foto cobrindo o círculo quando ela carrega, sem contorno na cor do papel; sem foto, as iniciais, como hoje. Vale para o avatar que abre o cartão, o do próprio cartão, o da pessoa na sessão (menu da conta e Meu perfil), o do detalhe da pessoa em Membros e o das listas da menção.
- O Graph simulado dos testes passa a ter fotos.

## Capabilities

### New Capabilities

### Modified Capabilities
- `usuarios-e-papeis`: a foto de uma pessoa pela API.
- `estrutura-das-telas`: a foto no avatar.

## Non-goals

- Guardar a foto no banco ou enviar foto pelo FiscoProjects.
- O contorno na cor do papel em volta da foto, que Marcelo valida depois.
- Fotos para a entrada local e a demonstração, que continuam com as iniciais.
- Nova permissão no Entra ID: o `User.Read.All` do ADR-0119 já dá a foto.

## Impact

- `Users`: o caso de uso `GetPersonPhoto`, a leitura do Graph (`GraphPersonPhotos`) com a memória (`PersonPhotoCache`) e o endpoint; o `FakeGraph` do `FiscoProjects.Testing`.
- Telas: `Avatar`, `CardAvatar`, menu da conta, Meu perfil, Membros e menção; tipos da API.
- `docs/adr/0124-foto-da-pessoa-pelo-microsoft-365.md`, ADR-0118 (status), `docs/roadmap.md`, `CLAUDE.md`, `docs/glossario.md`.
- ADRs: 0004, 0058, 0069, 0110, 0118, 0119, 0124.
