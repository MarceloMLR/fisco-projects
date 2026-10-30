import { zodResolver } from '@hookform/resolvers/zod';
import BlockOutlined from '@mui/icons-material/BlockOutlined';
import LinkOffOutlined from '@mui/icons-material/LinkOffOutlined';
import OpenInNewOutlined from '@mui/icons-material/OpenInNewOutlined';
import PictureAsPdfOutlined from '@mui/icons-material/PictureAsPdfOutlined';
import TaskAltOutlined from '@mui/icons-material/TaskAltOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import type SvgIcon from '@mui/material/SvgIcon';
import { useQuery } from '@tanstack/react-query';
import { type ReactNode, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { api, type LocalSigning } from '../api/client';
import { Alert } from '../components/Alert';
import { TextArea } from '../components/TextArea';
import { InstallationLogo } from '../installation/InstallationLogo';
import { useInstallation } from '../installation/useInstallation';
import { isAcceptanceError, refusalSchema } from './acceptances';

/** The address of the API behind the link of a signer: /sign/{token} → /api/local-signatures/{token}. */
const documentUrl = (token: string) =>
  `/api/local-signatures/${encodeURIComponent(token)}/document`;

/** The request of the link, or null when the link is no longer valid (404). */
async function signingOf(token: string, signal: AbortSignal): Promise<LocalSigning | null> {
  const { data, response } = await api.GET('/api/local-signatures/{token}', {
    params: { path: { token } },
    signal,
  });
  if (response.status === 404) {
    return null;
  }
  if (data === undefined) {
    throw new Error('The request of the link did not load.');
  }
  return data;
}

type Outcome = 'signed' | 'refused';

/**
 * The page of the local provider of signatures (ADR-0129), outside the structure of the screens and without a
 * session, always light: the signer opens the link the drawer of the term shows, reads the term and signs or refuses
 * it with the reason. A link answered, cancelled or unknown says it is no longer valid, without the term. Only outside
 * production, where the local provider exists.
 */
export function LocalSigningPage() {
  const { t } = useTranslation();
  const { token = '' } = useParams();
  const [outcome, setOutcome] = useState<Outcome>();
  const [refusing, setRefusing] = useState(false);
  const [failure, setFailure] = useState<string>();
  const [signing, setSigning] = useState(false);
  const { data, isPending, isError } = useQuery({
    queryKey: ['local-signing', token],
    queryFn: ({ signal }) => signingOf(token, signal),
    retry: false,
    staleTime: Infinity,
  });

  const sign = async () => {
    setSigning(true);
    setFailure(undefined);
    try {
      const { response } = await api.POST('/api/local-signatures/{token}/sign', {
        params: { path: { token } },
      });
      if (response.ok) {
        setOutcome('signed');
      } else {
        setFailure(
          t(response.status === 404 ? 'localSigning.invalidDetail' : 'localSigning.failed'),
        );
      }
    } catch {
      setFailure(t('localSigning.failed'));
    } finally {
      setSigning(false);
    }
  };

  let content: ReactNode;
  if (isPending) {
    content = (
      <Box role="status" sx={{ color: 'var(--text-secondary)' }}>
        {t('localSigning.loading')}
      </Box>
    );
  } else if (isError) {
    content = <Alert tone="danger">{t('localSigning.failed')}</Alert>;
  } else if (data === null) {
    content = (
      <Result
        icon={LinkOffOutlined}
        tone="neutral"
        title={t('localSigning.invalid')}
        detail={t('localSigning.invalidDetail')}
      />
    );
  } else if (outcome === 'signed') {
    content = (
      <Result
        icon={TaskAltOutlined}
        tone="success"
        title={t('localSigning.signed', { name: data.signer })}
        detail={t('localSigning.signedDetail')}
      />
    );
  } else if (outcome === 'refused') {
    content = (
      <Result
        icon={BlockOutlined}
        tone="danger"
        title={t('localSigning.refused')}
        detail={t('localSigning.refusedDetail')}
      />
    );
  } else {
    content = (
      <>
        <Box>
          <Box
            component="h1"
            sx={{
              margin: 0,
              fontSize: 22,
              lineHeight: '30px',
              fontWeight: 500,
              overflowWrap: 'anywhere',
            }}
          >
            {data.title}
          </Box>
          <Box component="p" sx={{ margin: 'var(--space-2) 0 0' }}>
            {t('localSigning.greeting', { name: data.signer })}
          </Box>
          <Box component="p" sx={{ margin: 'var(--space-1) 0 0', color: 'var(--text-secondary)' }}>
            {t('localSigning.intro')}
          </Box>
        </Box>
        {data.message !== null && (
          <Box component="section" aria-label={t('localSigning.message')}>
            <Box component="h2" sx={labelSx}>
              {t('localSigning.message')}
            </Box>
            <Box
              sx={{
                marginTop: 'var(--space-2)',
                padding: 'var(--space-3)',
                border: '1px solid var(--msg-manager-border)',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--msg-manager-bg)',
                whiteSpace: 'pre-wrap',
              }}
            >
              {data.message}
            </Box>
          </Box>
        )}
        <Box component="section" aria-label={t('localSigning.document')}>
          <Box component="h2" sx={labelSx}>
            {t('localSigning.document')}
          </Box>
          <Box
            sx={{
              marginTop: 'var(--space-2)',
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-3)',
              padding: 'var(--space-3)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-md)',
            }}
          >
            <PictureAsPdfOutlined
              aria-hidden
              sx={{ flex: 'none', color: 'var(--text-secondary)' }}
            />
            <Box sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>{data.fileName}</Box>
            <Button
              variant="outlined"
              size="small"
              startIcon={<OpenInNewOutlined />}
              href={documentUrl(token)}
              target="_blank"
              rel="noopener noreferrer"
              sx={{ flex: 'none' }}
            >
              {t('localSigning.open')}
            </Button>
          </Box>
        </Box>
        {failure !== undefined && <Alert tone="danger">{failure}</Alert>}
        {refusing ? (
          <RefusalForm
            token={token}
            onBack={() => {
              setRefusing(false);
            }}
            onRefused={() => {
              setOutcome('refused');
            }}
          />
        ) : (
          <Box
            sx={{
              display: 'flex',
              gap: 'var(--space-2)',
              justifyContent: 'flex-end',
              flexWrap: 'wrap',
            }}
          >
            <Button
              variant="outlined"
              onClick={() => {
                setRefusing(true);
              }}
            >
              {t('localSigning.refuse')}
            </Button>
            <Button
              variant="contained"
              disabled={signing}
              onClick={() => {
                void sign();
              }}
            >
              {t('localSigning.sign')}
            </Button>
          </Box>
        )}
      </>
    );
  }

  return <SigningFrame>{content}</SigningFrame>;
}

const labelSx = {
  margin: 0,
  fontSize: 12,
  lineHeight: '16px',
  fontWeight: 500,
  color: 'var(--text-secondary)',
} as const;

/** The page around the request: the brand of the installation and a card in the middle, always light. */
function SigningFrame({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { name, logoUrl } = useInstallation();
  return (
    <Box
      data-theme="light"
      sx={{
        minHeight: '100vh',
        backgroundColor: 'var(--bg-page)',
        color: 'var(--text)',
        fontFamily: 'var(--font-sans)',
        padding: { xs: 'var(--space-4)', md: 'var(--space-8)' },
      }}
    >
      <Box
        component="main"
        sx={{
          maxWidth: 640,
          margin: '0 auto',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-4)',
        }}
      >
        <Box
          component="header"
          sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', minHeight: 32 }}
        >
          {logoUrl !== null && <InstallationLogo src={logoUrl} maxHeight={32} />}
          <Box sx={{ fontWeight: 500 }}>{name}</Box>
          <Box
            component="span"
            sx={{ marginLeft: 'auto', fontSize: 13, color: 'var(--text-secondary)' }}
          >
            {t('localSigning.title')}
          </Box>
        </Box>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-5)',
            padding: { xs: 'var(--space-4)', md: 'var(--space-6)' },
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius-lg)',
            backgroundColor: 'var(--bg-surface)',
          }}
        >
          {children}
        </Box>
      </Box>
    </Box>
  );
}

/** The end of the page: what happened, with its icon, color and text (ADR-0065). */
function Result({
  icon: Icon,
  tone,
  title,
  detail,
}: {
  icon: typeof SvgIcon;
  tone: 'success' | 'danger' | 'neutral';
  title: string;
  detail: string;
}) {
  return (
    <Box role="status" sx={{ display: 'flex', alignItems: 'flex-start', gap: 'var(--space-3)' }}>
      <Icon
        aria-hidden
        sx={{
          flex: 'none',
          fontSize: 28,
          color: `var(--status-${tone}-text)`,
        }}
      />
      <Box>
        <Box component="h1" sx={{ margin: 0, fontSize: 20, lineHeight: '28px', fontWeight: 500 }}>
          {title}
        </Box>
        <Box component="p" sx={{ margin: 'var(--space-1) 0 0', color: 'var(--text-secondary)' }}>
          {detail}
        </Box>
      </Box>
    </Box>
  );
}

/** "Recusar": the reason, which who asked hears about; the request is refused. */
function RefusalForm({
  token,
  onBack,
  onRefused,
}: {
  token: string;
  onBack: () => void;
  onRefused: () => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<{ reason: string }>({
    resolver: zodResolver(refusalSchema),
    defaultValues: { reason: '' },
  });

  const refuse = handleSubmit(async ({ reason }) => {
    try {
      const { response } = await api.POST('/api/local-signatures/{token}/refuse', {
        params: { path: { token } },
        body: { reason: reason.trim() },
      });
      if (response.ok) {
        onRefused();
        return;
      }
      if (response.status === 400) {
        setError('reason', { message: 'acceptances.errors.reasonRequired' });
        return;
      }
      setError('root', {
        message: t(response.status === 404 ? 'localSigning.invalidDetail' : 'localSigning.failed'),
      });
    } catch {
      setError('root', { message: t('localSigning.failed') });
    }
  });

  return (
    <Box
      component="form"
      noValidate
      onSubmit={(event) => {
        void refuse(event);
      }}
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
    >
      {errors.root?.message !== undefined && <Alert tone="danger">{errors.root.message}</Alert>}
      <TextArea
        label={t('localSigning.reason')}
        rows={4}
        maxLength={1010}
        helper={t('localSigning.reasonHelp')}
        error={isAcceptanceError(errors.reason?.message) ? t(errors.reason.message) : undefined}
        {...register('reason')}
      />
      <Box
        sx={{
          display: 'flex',
          gap: 'var(--space-2)',
          justifyContent: 'flex-end',
          flexWrap: 'wrap',
        }}
      >
        <Button variant="outlined" onClick={onBack}>
          {t('localSigning.back')}
        </Button>
        <Button type="submit" variant="contained" color="error" disabled={isSubmitting}>
          {t('localSigning.confirmRefusal')}
        </Button>
      </Box>
    </Box>
  );
}
