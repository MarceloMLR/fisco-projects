import Box from '@mui/material/Box';
import Snackbar from '@mui/material/Snackbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { MyWorkItem, MyWorkView } from '../api/client';
import { myWorkQuery, queryKeys } from '../api/queries';
import { Alert } from '../components/Alert';
import { SegmentedControl } from '../components/SegmentedControl';
import { Select } from '../components/Select';
import { formatLongDayText } from '../format/dates';
import { ProgressDialog } from '../projects/ScheduleItemProgress';
import { completeTask } from '../projects/scheduleProgress';
import { useSession } from '../session/SessionContext';
import { Page } from '../shell/Page';
import { theme } from '../theme/theme';
import { PeopleChangeDialog } from '../tickets/TicketPeopleDialogs';
import { MyProjectsCard, UpcomingMilestonesCard } from './MyWorkCards';
import { MyWorkQueue } from './MyWorkQueue';

const views: readonly MyWorkView[] = ['ForMe', 'OpenedByMe', 'Following'];

/**
 * Meu trabalho, the home page (ADR-0070, ADR-0096, canvas Main and CelularMeuTrabalho): today, the views of the queue
 * with their counts, the queue by urgency, Meus projetos and Próximos marcos. Concluir takes a task to 100% at once;
 * Iniciar and Atualizar ask for the percentage; Atribuir changes the assignee of the ticket. There is no "Abrir
 * chamado" nor "Novo projeto" here (ADR-0070).
 */
export function MyWorkPage() {
  const { t } = useTranslation();
  const desktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true });
  const queryClient = useQueryClient();
  const { session } = useSession();
  const userId = session.status === 'signed-in' ? session.user.id : '';
  const { data: work, isError } = useQuery(myWorkQuery());
  const [view, setView] = useState<MyWorkView>('ForMe');
  /** What an action of the queue did, at the bottom, as in the tabs of a project. */
  const [message, setMessage] = useState<string>();
  const [failure, setFailure] = useState<string>();
  /** The task whose percentage is asked for (Iniciar and Atualizar). */
  const [progressOf, setProgressOf] = useState<{
    projectId: string;
    item: { id: string; name: string; progress: number };
  }>();
  const [assigning, setAssigning] = useState<number>();
  const [busy, setBusy] = useState<string>();

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.myWork });
    void queryClient.invalidateQueries({ queryKey: queryKeys.projects });
    void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
  };
  const done = (text: string) => {
    setMessage(text);
    refresh();
  };

  const act = async (item: MyWorkItem) => {
    setFailure(undefined);
    if (item.action === 'Assign' && item.ticket !== null) {
      setAssigning(item.ticket.number);
      return;
    }
    if ((item.action === 'Start' || item.action === 'Update') && item.task !== null) {
      setProgressOf({
        projectId: item.project.id,
        item: { id: item.id, name: item.title, progress: item.task.progress },
      });
      return;
    }
    if (item.action === 'Complete') {
      setBusy(item.id);
      const failed = await completeTask(item.project.id, item.id, t);
      setBusy(undefined);
      if (failed === undefined) {
        done(t('projects.schedule.messages.completed', { name: item.title }));
      } else {
        setFailure(failed);
      }
    }
  };

  const options = views.map((value) => ({
    value,
    label: t(`myWork.views.${value}`),
    count: work?.views.find((entry) => entry.view === value)?.count,
  }));
  const viewControl = desktop ? (
    <SegmentedControl<MyWorkView>
      label={t('myWork.views.label')}
      value={view}
      options={options}
      onChange={setView}
    />
  ) : (
    <Select<MyWorkView>
      label={t('myWork.views.label')}
      value={view}
      options={options.map(({ value, label, count }) => ({
        value,
        label: count === undefined ? label : `${label} (${String(count)})`,
      }))}
      onChange={setView}
    />
  );

  const queue =
    work === undefined ? (
      isError ? (
        <Alert tone="danger">{t('myWork.loadFailed')}</Alert>
      ) : null
    ) : (
      <MyWorkQueue
        work={work}
        view={view}
        userId={userId}
        desktop={desktop}
        busy={busy}
        onAction={(item) => {
          void act(item);
        }}
      />
    );

  return (
    <Page
      subtitle={work === undefined ? undefined : formatLongDayText(work.today)}
      actions={desktop ? viewControl : undefined}
    >
      {!desktop && viewControl}
      {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
      {desktop ? (
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gridTemplateColumns: { md: 'minmax(0, 1fr) 340px', lg: 'minmax(0, 1fr) 380px' },
            gap: 'var(--space-5)',
          }}
        >
          <Box sx={{ display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0 }}>
            {queue}
          </Box>
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '20px',
              minWidth: 0,
              minHeight: 0,
            }}
          >
            <MyProjectsCard fill />
            <UpcomingMilestonesCard fill />
          </Box>
        </Box>
      ) : (
        <>
          {queue}
          <MyProjectsCard />
          <UpcomingMilestonesCard />
        </>
      )}
      {progressOf !== undefined && (
        <ProgressDialog
          projectId={progressOf.projectId}
          item={progressOf.item}
          onClose={() => {
            setProgressOf(undefined);
          }}
          onSaved={(message) => {
            setProgressOf(undefined);
            done(message);
          }}
        />
      )}
      {assigning !== undefined && (
        <PeopleChangeDialog
          kind="assignee"
          numbers={[assigning]}
          onClose={() => {
            setAssigning(undefined);
          }}
          onDone={(message) => {
            setAssigning(undefined);
            done(message);
          }}
        />
      )}
      <Snackbar
        open={message !== undefined}
        message={message}
        autoHideDuration={6000}
        onClose={() => {
          setMessage(undefined);
        }}
      />
    </Page>
  );
}
