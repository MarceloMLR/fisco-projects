## Context

O `Projects` é dono dos projetos e das pessoas de cada um (`project_members`), das permissões (`ProjectPermissions.CanManagePeople`: o administrador e o gerente, no projeto em aberto) e do cronograma, cuja leitura (`GetSchedule`) já junta os nomes dos responsáveis. A lista de Membros sai do `ListProjectMembers`. A anonimização chama cada `IPersonalDataEraser` na transação dela; as tarefas em segundo plano seguem o padrão do `TicketAutoClose`.

## Goals / Non-Goals

**Goals:**

- As ausências no módulo que conhece as pessoas dos projetos e o cronograma, sem contratos novos entre módulos.
- O mínimo de dado pessoal: só as datas, apagadas um ano depois do fim e na anonimização, fora da auditoria as datas.

**Non-Goals:**

- Motivo, meio dia, cadastro pela própria pessoa, avisos por e-mail, mudança das datas do cronograma.

## Decisions

- **Domínio.** `PersonAbsence` (`projects.person_absences`): a pessoa (`UserId`), o início e o fim (`DateOnly`), quem incluiu e quando. `PersonAbsence.IsValid(início, fim)` (início até o fim, de 1 a 366 dias corridos, de 2000 a 2099), `Overlaps(início, fim)` e `KeptUntil` (o fim mais um ano); a regra num lugar só.
- **Casos de uso** (`AbsenceUseCases.cs`): `ListMemberAbsences`, `AddMemberAbsence` e `RemoveMemberAbsence`, pelo projeto e pela pessoa dele: 404 sem o projeto, para quem não o vê ou sem a pessoa nele; 403 sem `ProjectPermissions.CanManagePeople`; 400 nas datas; 409 na sobreposição. `CleanUpAbsences` apaga as que passaram do `KeptUntil`, pelo dia da instalação (`InstallationClock`). O armazenamento é o `IAbsenceStore`.
- **API.** `GET /api/projects/{id}/members/{userId}/absences` (`ListMemberAbsences`: as ausências da pessoa, do fim mais recente para o mais antigo, e `canManage`), `POST` com `{ start, end }` (`AddMemberAbsence`, 201 com o id) e `DELETE .../{absenceId}` (`RemoveMemberAbsence`, 204). A lista de Membros traz `nextAbsence` (a ausência atual ou a próxima, que termina hoje ou depois). Cada item do `GET /schedule` traz `ownerAbsences`, as ausências do responsável que se cruzam com uma tarefa em aberto com datas próprias.
- **Auditoria.** `person_absences` pelo nome da pessoa (rótulo pela referência do usuário), com quem incluiu pelo nome; `Start` e `End` como segredo (`IsAuditSecret`), `CreatedAt` fora.
- **Retenção.** `AbsenceCleanup`, em segundo plano, a cada seis horas, roda o `CleanUpAbsences` (`ExecuteDelete`). `ProjectsPersonalData`, o `IPersonalDataEraser` do `Projects`, apaga as ausências da pessoa anonimizada, num contexto próprio na conexão e na transação da anonimização.
- **Tela.**
  - `MemberAbsences` (`web/src/projects/MemberAbsences.tsx`) depois dos chamados no detalhe da pessoa: a lista, "Incluir ausência" e o apagar com confirmação, pelo `memberAbsencesQuery`; o diálogo `AbsenceDialog`, com o início e o fim digitados (`maskTypedDay`) e as regras em `absences.ts` (`absenceText`, `awayText`).
  - Na lista de Membros, embaixo do e-mail, "Ausente até 14/11" ou "Ausente de 03/11 a 14/11" (próximos 30 dias), com o ícone de ausência.
  - No cronograma, o ícone de ausência (`OwnerAway`, `ScheduleStatus.tsx`) ao lado do responsável na Lista, com os períodos no nome acessível, e a linha "Ausente de 05/11 a 14/11, durante a tarefa" no detalhe.
- **Demonstração.** O plano ganha `absences` (a pessoa e os dias úteis a partir de hoje), incluídas pela gerente depois dos membros.

## Risks / Trade-offs

- [Rotas por projeto para um dado da pessoa] → A permissão do gerente é do projeto (ADR-0141); a ausência vale em todos os projetos da pessoa.
- [A pessoa só em projetos encerrados] → Não tem tarefas em aberto; sem ausência a cadastrar.
- [As datas fora da auditoria] → Decisão de minimização do ADR-0141.
