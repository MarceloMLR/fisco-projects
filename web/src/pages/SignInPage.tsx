import { zodResolver } from '@hookform/resolvers/zod';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import LockOutlined from '@mui/icons-material/LockOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { z } from 'zod';
import { api, signInProblems } from '../api/client';
import { Alert } from '../components/Alert';
import { TextField } from '../components/TextField';
import { InstallationLogo } from '../installation/InstallationLogo';
import { useInstallation } from '../installation/useInstallation';
import { SessionPending } from '../session/RequireSession';
import { type SignInState, useSession } from '../session/SessionContext';
import { microsoftSignIn } from '../session/microsoftSignIn';
import { rememberEmail, rememberedEmail } from '../session/rememberedEmail';
import { SignInArt } from './SignInArt';

// Below this width the card fills the screen and the brand panel shrinks (fp-screens.css).
const phone = '@media (max-width: 860px)';

const emailErrors = {
  required: 'signIn.errors.emailRequired',
  invalid: 'signIn.errors.emailInvalid',
} as const;

const form = z.object({
  email: z
    .string()
    .trim()
    .min(1, { error: emailErrors.required })
    .pipe(z.email({ error: emailErrors.invalid })),
});

type Refusal = 'notInDirectory' | 'inactive' | 'guest' | 'notAssigned' | 'failed';

/** Why the API sent the person back from the sign-in through Entra ID, in the "refused" parameter (ADR-0117). */
const refusalsOfEntra: readonly Refusal[] = ['guest', 'notAssigned', 'inactive', 'failed'];

function refusalOf(search: string): Refusal | undefined {
  const refused = new URLSearchParams(search).get('refused');
  return refusalsOfEntra.find((refusal) => refusal === refused);
}

/** How this installation signs people in: the local sign-in of the demo, Entra ID, or both. */
interface SignInMethods {
  local: boolean;
  entra: boolean;
}

/** Before the API answers, the page shows the local sign-in, as it did before the Entra ID. */
const untilKnown: SignInMethods = { local: true, entra: false };

/** The sign-in methods, or nothing when the API did not answer them. */
async function loadSignInMethods(): Promise<SignInMethods | undefined> {
  try {
    const { data } = await api.GET('/api/session/sign-in-options');
    return data === undefined ? undefined : { local: data.localSignIn, entra: data.entraSignIn };
  } catch {
    return undefined;
  }
}

/** Where the person goes after signing in: back to the screen asked, unless someone else was in the session. */
function destination(state: unknown, userId: string): string {
  const asked = state as Partial<SignInState> | null;
  const from = asked?.from;
  if (from === undefined || (asked?.userId !== undefined && asked.userId !== userId)) {
    return '/';
  }
  return `${from.pathname}${from.search}${from.hash}`;
}

/**
 * Sign-in page (ADR-0009), as in the sign-in screen of the canvas: split card over the navy background, brand panel on the
 * left and the e-mail form on the right. It signs in through Entra ID (ADR-0117) and, without a tenant or in the
 * demonstration, by the local sign-in of the demo (ADR-0078).
 */
export function SignInPage() {
  const { session } = useSession();
  const location = useLocation();

  if (session.status === 'signed-in') {
    return <Navigate to={destination(location.state, session.user.id)} replace />;
  }
  if (session.status === 'checking') {
    return <SessionPending />;
  }
  return <SignInScreen />;
}

function SignInScreen() {
  const { t } = useTranslation();
  const { refresh } = useSession();
  const installation = useInstallation();
  const location = useLocation();
  const navigate = useNavigate();
  // Asked once; a submit before the answer waits for it, and asks again if there was no answer.
  const [firstMethods] = useState(loadSignInMethods);
  const [methods, setMethods] = useState<SignInMethods>();
  const [refusal, setRefusal] = useState<Refusal | undefined>(() => refusalOf(location.search));
  const [leaving, setLeaving] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.input<typeof form>, unknown, z.output<typeof form>>({
    resolver: zodResolver(form),
    defaultValues: { email: rememberedEmail() },
  });

  // The refusal that came back from Microsoft is shown once: the address goes back to the plain sign-in page.
  useEffect(() => {
    if (new URLSearchParams(location.search).has('refused')) {
      void navigate({ search: '' }, { replace: true, state: location.state as unknown });
    }
  }, [location.search, location.state, navigate]);

  useEffect(() => {
    let active = true;
    void firstMethods.then((known) => {
      if (active) {
        setMethods(known ?? untilKnown);
      }
    });
    return () => {
      active = false;
    };
  }, [firstMethods]);

  const shown = methods ?? untilKnown;

  /** To Microsoft, with the screen asked and, after an expired session, who had it (ADR-0117). */
  async function signInWithEntra(email: string) {
    const asked = location.state as Partial<SignInState> | null;
    const from = asked?.from;
    const { data, response } = await api.POST('/api/session/entra', {
      body: {
        email,
        returnUrl: from === undefined ? '/' : `${from.pathname}${from.search}${from.hash}`,
        previousUserId: asked?.userId ?? null,
      },
    });
    if (!response.ok || data === undefined) {
      setRefusal('failed');
      return;
    }
    rememberEmail(email);
    setLeaving(true);
    microsoftSignIn.go(data.location);
  }

  const signIn = handleSubmit(async ({ email }) => {
    setRefusal(undefined);
    try {
      const known = (await firstMethods) ?? (await loadSignInMethods()) ?? untilKnown;
      if (!known.local) {
        if (known.entra) {
          await signInWithEntra(email);
        } else {
          setRefusal('failed');
        }
        return;
      }
      const { error, response } = await api.POST('/api/session/local', { body: { email } });
      if (response.ok) {
        rememberEmail(email);
        await refresh();
        return;
      }
      if (response.status !== 403) {
        setRefusal('failed');
        return;
      }
      if (error?.type === signInProblems.inactive) {
        setRefusal('inactive');
        return;
      }
      // Outside the demo directory: with the Entra ID on too, the person is someone of the company.
      if (known.entra) {
        await signInWithEntra(email);
        return;
      }
      setRefusal('notInDirectory');
    } catch {
      setRefusal('failed');
    }
  });

  const emailError = errors.email?.message;
  const NoteIcon = shown.local && !shown.entra ? InfoOutlined : LockOutlined;
  const texts =
    shown.local && shown.entra
      ? { hint: t('signIn.hintBoth'), note: t('signIn.noteBoth') }
      : shown.local
        ? { hint: t('signIn.hintLocal'), note: t('signIn.noteLocal') }
        : { hint: t('signIn.hint'), note: t('signIn.note') };

  return (
    <Box
      component="main"
      sx={{
        position: 'relative',
        display: 'grid',
        placeItems: 'center',
        minHeight: '100vh',
        boxSizing: 'border-box',
        padding: 'var(--space-8)',
        overflow: 'hidden',
        background:
          'radial-gradient(1100px 680px at 12% 8%, var(--signin-glow), transparent 62%), radial-gradient(900px 620px at 94% 100%, var(--signin-glow), transparent 58%), var(--signin-bg)',
        color: 'var(--text)',
        fontSize: 14,
        lineHeight: '20px',
        // Grid of dots, faded toward the center. The mask uses only the alpha of the color.
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: 'radial-gradient(var(--signin-dots) 1.2px, transparent 1.6px)',
          backgroundSize: '24px 24px',
          maskImage:
            'radial-gradient(ellipse 70% 60% at 50% 50%, transparent 40%, var(--text) 100%)',
          pointerEvents: 'none',
        },
        [phone]: { padding: 0, placeItems: 'stretch', '&::before': { display: 'none' } },
      }}
    >
      <Box
        sx={{
          position: 'relative',
          display: 'grid',
          gridTemplateColumns: 'minmax(0, 1.12fr) minmax(0, 1fr)',
          width: 'min(1120px, 100%)',
          minHeight: 'min(640px, calc(100vh - 64px))',
          backgroundColor: 'var(--bg-surface)',
          borderRadius: 'var(--radius-xl)',
          boxShadow: 'var(--shadow-signin)',
          overflow: 'hidden',
          [phone]: {
            gridTemplateColumns: 'minmax(0, 1fr)',
            borderRadius: 0,
            boxShadow: 'none',
            minHeight: '100vh',
          },
        }}
      >
        <Box
          sx={{
            position: 'relative',
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-6)',
            minWidth: 0,
            padding: '40px',
            background:
              'linear-gradient(155deg, var(--brand-panel-bg-2) 0%, var(--brand-panel-bg) 64%)',
            color: 'var(--brand-panel-text)',
            overflow: 'hidden',
            isolation: 'isolate',
            [phone]: { padding: 'var(--space-6)', gap: 'var(--space-3)', minHeight: 200 },
          }}
        >
          <SignInArt />
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-3)',
              fontSize: 17,
              lineHeight: '24px',
              fontWeight: 500,
            }}
          >
            {installation.logoUrl === null ? (
              <Box
                component="span"
                aria-hidden="true"
                sx={{
                  display: 'inline-grid',
                  placeItems: 'center',
                  width: 32,
                  height: 32,
                  borderRadius: 'var(--radius-md)',
                  backgroundColor: 'var(--brand-panel-text)',
                  color: 'var(--brand-panel-bg)',
                }}
              >
                <ViewTimelineOutlined sx={{ fontSize: 'var(--icon-md)' }} />
              </Box>
            ) : (
              // The panel is always dark; a light tile keeps a dark logo visible, at the height of the badge.
              <Box
                component="span"
                sx={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  boxSizing: 'border-box',
                  height: 32,
                  padding: '4px 6px',
                  borderRadius: 'var(--radius-md)',
                  backgroundColor: 'var(--brand-panel-text)',
                }}
              >
                <InstallationLogo src={installation.logoUrl} maxHeight={24} maxWidth={148} />
              </Box>
            )}
            {installation.name}
          </Box>
          <Box
            component="p"
            sx={{
              maxWidth: 440,
              margin: 'var(--space-8) 0 0',
              fontSize: 32,
              lineHeight: '40px',
              fontWeight: 500,
              letterSpacing: '-0.01em',
              [phone]: { marginTop: 'var(--space-2)', fontSize: 24, lineHeight: '32px' },
            }}
          >
            {t('signIn.lead')}
          </Box>
          <Box
            component="p"
            sx={{
              maxWidth: 400,
              margin: 0,
              fontSize: 15,
              lineHeight: '24px',
              color: 'var(--brand-panel-muted)',
              [phone]: { display: 'none' },
            }}
          >
            {t('signIn.sub')}
          </Box>
        </Box>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            minWidth: 0,
            padding: '40px var(--space-12)',
            [phone]: { padding: 'var(--space-8) var(--space-6)' },
          }}
        >
          <Box
            component="form"
            noValidate
            onSubmit={(event) => {
              void signIn(event);
            }}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-4)',
              width: '100%',
              maxWidth: 380,
              margin: 'auto',
            }}
          >
            <Box
              component="h1"
              sx={{ margin: 0, fontSize: 28, lineHeight: '36px', fontWeight: 500 }}
            >
              {t('signIn.title')}
            </Box>
            <Box
              component="p"
              sx={{
                margin: 'calc(var(--space-2) * -1) 0 var(--space-2)',
                color: 'var(--text-secondary)',
                visibility: methods === undefined ? 'hidden' : 'visible',
              }}
            >
              {texts.hint}
            </Box>
            {refusal === 'failed' && (
              <Alert tone="danger" title={t('signIn.failed.title')}>
                {t('signIn.failed.body')}
              </Alert>
            )}
            {refusal !== undefined && refusal !== 'failed' && (
              <Alert tone="danger" title={t('signIn.refused.title')}>
                {t(`signIn.refused.${refusal}`)}
              </Alert>
            )}
            <TextField
              label={t('signIn.email')}
              type="email"
              size="lg"
              autoComplete="email"
              placeholder={t('signIn.emailPlaceholder')}
              error={
                emailError === undefined
                  ? undefined
                  : t(emailError as (typeof emailErrors)[keyof typeof emailErrors])
              }
              {...register('email')}
            />
            <Button
              type="submit"
              variant="contained"
              size="large"
              fullWidth
              disabled={isSubmitting || leaving}
            >
              {t('signIn.submit')}
            </Button>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 'var(--space-2)',
                fontSize: 13,
                lineHeight: '18px',
                color: 'var(--text-secondary)',
                visibility: methods === undefined ? 'hidden' : 'visible',
              }}
            >
              <NoteIcon aria-hidden sx={{ fontSize: 'var(--icon-sm)' }} />
              {texts.note}
            </Box>
          </Box>
          <Box
            component="p"
            sx={{
              margin: 0,
              paddingTop: 'var(--space-6)',
              borderTop: '1px solid var(--border)',
              fontSize: 13,
              lineHeight: '18px',
              color: 'var(--text-secondary)',
              textAlign: 'center',
            }}
          >
            {t('signIn.foot')}
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
