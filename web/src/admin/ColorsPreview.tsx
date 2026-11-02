import CheckOutlined from '@mui/icons-material/CheckOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import HomeOutlined from '@mui/icons-material/HomeOutlined';
import NotificationsNoneOutlined from '@mui/icons-material/NotificationsNoneOutlined';
import ScheduleOutlined from '@mui/icons-material/ScheduleOutlined';
import SupportAgentOutlined from '@mui/icons-material/SupportAgentOutlined';
import ViewTimelineOutlined from '@mui/icons-material/ViewTimelineOutlined';
import Box from '@mui/material/Box';
import type SvgIcon from '@mui/material/SvgIcon';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { SegmentedControl } from '../components/SegmentedControl';
import { initials } from '../components/initials';
import { InstallationLogo } from '../installation/InstallationLogo';
import { type BrandColorChoice, brandVariables, sidebarIsDark } from '../theme/brandColors';
import type ptBR from '../locales/pt-BR.json';
import type { ColorScheme } from '../theme/tokens';

type PreviewText = keyof (typeof ptBR)['colors']['preview'];

// When the preview is narrow (on the phone, or beside the fields on a small computer), the sidebar becomes a band on
// top and the tabs that do not fit are left out, never scrolled.
const narrow = '@container colors-preview (max-width: 560px)';
const tight = '@container colors-preview (max-width: 420px)';

function NavItem({
  icon: Icon,
  text,
  state,
}: {
  icon: typeof SvgIcon;
  text: string;
  state?: 'active' | 'hover';
}) {
  return (
    <Box
      sx={{
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-2)',
        height: 32,
        padding: '0 var(--space-2)',
        borderRadius: 'var(--radius-md)',
        color: state === 'active' ? 'var(--sidebar-text-active)' : 'var(--sidebar-text)',
        backgroundColor:
          state === 'active'
            ? 'var(--sidebar-active-bg)'
            : state === 'hover'
              ? 'var(--sidebar-hover-bg)'
              : 'transparent',
        fontWeight: state === 'active' ? 500 : 400,
        whiteSpace: 'nowrap',
        '& > svg': {
          fontSize: 16,
          color: state === 'active' ? 'var(--sidebar-indicator)' : 'var(--sidebar-muted)',
        },
        '&::before':
          state === 'active'
            ? {
                content: '""',
                position: 'absolute',
                left: -8,
                top: 6,
                bottom: 6,
                width: 3,
                borderRadius: '0 2px 2px 0',
                backgroundColor: 'var(--sidebar-indicator)',
                [narrow]: { left: 8, right: 8, top: 'auto', bottom: 2, width: 'auto', height: 2 },
              }
            : undefined,
      }}
    >
      <Icon />
      {text}
    </Box>
  );
}

/** The main button, as it is and under the pointer. */
function MainButton({ text, hovered = false }: { text: string; hovered?: boolean }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        height: 30,
        padding: '0 var(--space-3)',
        borderRadius: 'var(--radius-md)',
        backgroundColor: hovered ? 'var(--action-hover)' : 'var(--action)',
        color: 'var(--on-action)',
        fontWeight: 500,
      }}
    >
      {text}
    </Box>
  );
}

function Owner({ name }: { name: string }) {
  return (
    <Box
      component="span"
      sx={{ display: 'inline-flex', alignItems: 'center', gap: '6px', minWidth: 0 }}
    >
      <Box
        component="span"
        sx={{
          display: 'inline-grid',
          placeItems: 'center',
          flex: 'none',
          width: 22,
          height: 22,
          borderRadius: 'var(--radius-full)',
          backgroundColor: 'var(--avatar-owner)',
          color: 'var(--on-avatar-role)',
          fontSize: 10,
          fontWeight: 500,
        }}
      >
        {initials(name)}
      </Box>
      <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
        {name}
      </Box>
    </Box>
  );
}

function Progress({ percent }: { percent: number }) {
  return (
    <Box
      component="span"
      sx={{ height: 6, borderRadius: 3, backgroundColor: 'var(--border)', overflow: 'hidden' }}
    >
      <Box
        component="i"
        sx={{
          display: 'block',
          height: '100%',
          width: `${String(percent)}%`,
          backgroundColor: 'var(--success)',
        }}
      />
    </Box>
  );
}

const rowSx = {
  display: 'grid',
  gridTemplateColumns: '22px minmax(0, 1.6fr) minmax(0, 1fr) 80px',
  gap: 'var(--space-2)',
  alignItems: 'center',
  padding: '8px var(--space-3)',
  borderTop: '1px solid var(--border)',
  '& > span': { minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
  [narrow]: {
    gridTemplateColumns: '22px minmax(0, 1fr) 70px',
    '& > span:nth-of-type(3)': { display: 'none' },
  },
} as const;

function Check({ checked = false }: { checked?: boolean }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'grid',
        placeItems: 'center',
        width: 16,
        height: 16,
        boxSizing: 'border-box',
        borderRadius: 'var(--radius-sm)',
        border: '1.5px solid',
        borderColor: checked ? 'var(--control-selected)' : 'var(--border-control)',
        backgroundColor: checked ? 'var(--control-selected)' : 'transparent',
        color: 'var(--on-control-selected)',
        '& > svg': { fontSize: 12 },
      }}
    >
      {checked && <CheckOutlined />}
    </Box>
  );
}

/**
 * The preview of the colors (ADR-0146): the application drawn small, only with the tokens, with the colors being
 * chosen, in the light or the dark theme chosen here, without changing the theme of the person.
 */
export function ColorsPreview({
  colors,
  name,
  logo,
  darkLogo,
}: {
  colors: BrandColorChoice;
  name: string;
  logo: string | null;
  darkLogo: string | null;
}) {
  const { t } = useTranslation();
  const [scheme, setScheme] = useState<ColorScheme>('light');
  // The name over the sidebar has its own color only with a color of the sidebar; otherwise the text of the theme.
  const variables = useMemo<Record<string, string>>(
    () => ({ '--sidebar-title': 'var(--text)', ...brandVariables(colors, scheme) }),
    [colors, scheme],
  );
  const sidebarLogo = darkLogo !== null && sidebarIsDark(colors, scheme) ? darkLogo : logo;
  const p = (key: PreviewText) => t(`colors.preview.${key}`);

  return (
    <Box
      component="section"
      aria-label={p('label')}
      sx={{
        minWidth: 0,
        overflow: 'hidden',
        containerType: 'inline-size',
        containerName: 'colors-preview',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-lg)',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 'var(--space-3)',
          padding: 'var(--space-3) var(--space-4)',
          borderBottom: '1px solid var(--border)',
        }}
      >
        <Box component="h2" sx={{ margin: 0, fontSize: 15, lineHeight: '22px', fontWeight: 500 }}>
          {p('title')}
        </Box>
        <SegmentedControl
          label={p('scheme')}
          value={scheme}
          options={[
            { value: 'light', label: p('light') },
            { value: 'dark', label: p('dark') },
          ]}
          onChange={setScheme}
        />
      </Box>
      <Box
        data-testid="colors-preview-app"
        data-theme={scheme}
        aria-hidden="true"
        style={variables}
        sx={{
          display: 'grid',
          gridTemplateColumns: '200px minmax(0, 1fr)',
          minHeight: 420,
          backgroundColor: 'var(--bg-page)',
          color: 'var(--text)',
          fontSize: 13,
          lineHeight: '18px',
          [narrow]: { gridTemplateColumns: 'minmax(0, 1fr)' },
        }}
      >
        <Box
          data-testid="colors-preview-sidebar"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: '2px',
            padding: 'var(--space-3) var(--space-2)',
            backgroundColor: 'var(--sidebar-bg)',
            borderRight: '1px solid var(--sidebar-border)',
            [narrow]: {
              flexDirection: 'row',
              flexWrap: 'wrap',
              borderRight: 0,
              borderBottom: '1px solid var(--sidebar-border)',
            },
          }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-2)',
              padding: 'var(--space-1) var(--space-2) var(--space-3)',
              color: 'var(--sidebar-title)',
              fontWeight: 500,
              [narrow]: { width: '100%', paddingBottom: 'var(--space-1)' },
            }}
          >
            {sidebarLogo === null ? (
              <Box
                component="span"
                sx={{
                  display: 'inline-grid',
                  placeItems: 'center',
                  width: 24,
                  height: 24,
                  borderRadius: 'var(--radius-md)',
                  backgroundColor: 'var(--action)',
                  color: 'var(--on-action)',
                  '& > svg': { fontSize: 16 },
                }}
              >
                <ViewTimelineOutlined />
              </Box>
            ) : (
              <InstallationLogo src={sidebarLogo} maxHeight={24} />
            )}
            {name === '' ? p('company') : name}
          </Box>
          <NavItem icon={HomeOutlined} text={p('home')} />
          <NavItem icon={FolderOutlined} text={p('projects')} state="active" />
          <NavItem icon={SupportAgentOutlined} text={p('tickets')} state="hover" />
          <NavItem icon={ScheduleOutlined} text={p('hours')} />
          <Box
            sx={{
              height: '1px',
              margin: 'var(--space-2)',
              backgroundColor: 'var(--sidebar-divider)',
              [narrow]: { display: 'none' },
            }}
          />
          {[
            { label: p('myProjects'), muted: true },
            { label: p('project'), code: p('projectCode') },
            { label: p('otherProject'), code: p('otherProjectCode') },
          ].map((line) => (
            <Box
              key={line.label}
              sx={{
                display: 'flex',
                flexDirection: 'column',
                padding: '2px var(--space-2)',
                fontSize: line.muted === true ? 12 : 13,
                color: line.muted === true ? 'var(--sidebar-muted)' : 'var(--sidebar-text)',
                [narrow]: { display: 'none' },
              }}
            >
              <Box
                component="span"
                sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
              >
                {line.label}
              </Box>
              {line.code !== undefined && (
                <Box
                  component="span"
                  sx={{
                    fontFamily: 'var(--font-mono)',
                    fontSize: 11,
                    color: 'var(--sidebar-muted)',
                  }}
                >
                  {line.code}
                </Box>
              )}
            </Box>
          ))}
        </Box>
        <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 'var(--space-2)',
              padding: 'var(--space-3) var(--space-4)',
              borderBottom: '1px solid var(--border)',
              color: 'var(--text-secondary)',
            }}
          >
            <Box
              component="span"
              sx={{
                minWidth: 0,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {p('trail')}
            </Box>
            <Box
              component="span"
              sx={{ position: 'relative', display: 'inline-flex', '& > svg': { fontSize: 20 } }}
            >
              <NotificationsNoneOutlined />
              <Box
                component="b"
                sx={{
                  position: 'absolute',
                  top: -4,
                  right: -6,
                  minWidth: 16,
                  height: 16,
                  borderRadius: 'var(--radius-full)',
                  backgroundColor: 'var(--notify)',
                  color: 'var(--on-notify)',
                  fontSize: 10,
                  lineHeight: '16px',
                  textAlign: 'center',
                  fontWeight: 500,
                }}
              >
                {3}
              </Box>
            </Box>
          </Box>
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 'var(--space-3)',
              padding: 'var(--space-4)',
              minWidth: 0,
            }}
          >
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 'var(--space-2)',
              }}
            >
              <Box component="strong" sx={{ fontSize: 16, fontWeight: 500 }}>
                {p('project')}
              </Box>
              <Box
                component="span"
                sx={{
                  padding: '1px 6px',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius-sm)',
                  backgroundColor: 'var(--bg-subtle)',
                  color: 'var(--text-secondary)',
                  fontFamily: 'var(--font-mono)',
                  fontSize: 11,
                }}
              >
                {p('projectCode')}
              </Box>
            </Box>
            <Box
              sx={{
                display: 'flex',
                gap: 'var(--space-4)',
                boxShadow: 'inset 0 -1px 0 var(--border)',
                overflow: 'hidden',
              }}
            >
              {[p('overview'), p('schedule'), p('tickets'), p('documents')].map((tab, index) => (
                <Box
                  key={tab}
                  component="span"
                  sx={{
                    padding: 'var(--space-2) 0',
                    whiteSpace: 'nowrap',
                    color: index === 1 ? 'var(--action-text)' : 'var(--text-secondary)',
                    fontWeight: index === 1 ? 500 : 400,
                    boxShadow: index === 1 ? 'inset 0 -2px 0 var(--action-text)' : 'none',
                    [narrow]: index === 3 ? { display: 'none' } : {},
                    [tight]: index === 2 ? { display: 'none' } : {},
                  }}
                >
                  {tab}
                </Box>
              ))}
            </Box>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 'var(--space-2)',
              }}
            >
              <MainButton text={`+ ${p('newTask')}`} />
              <MainButton text={p('hovered')} hovered />
              <Box
                component="span"
                sx={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  height: 30,
                  boxSizing: 'border-box',
                  padding: '0 var(--space-3)',
                  border: '1px solid var(--border-strong)',
                  borderRadius: 'var(--radius-md)',
                  backgroundColor: 'var(--bg-surface)',
                  fontWeight: 500,
                }}
              >
                {p('newPhase')}
              </Box>
              <Box
                component="span"
                sx={{
                  color: 'var(--action-text)',
                  fontWeight: 500,
                  textDecoration: 'underline',
                  textUnderlineOffset: 2,
                }}
              >
                {p('baseline')}
              </Box>
            </Box>
            <Box
              sx={{
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius-lg)',
                overflow: 'hidden',
                backgroundColor: 'var(--bg-surface)',
              }}
            >
              <Box
                sx={{
                  ...rowSx,
                  borderTop: 0,
                  backgroundColor: 'var(--bg-subtle)',
                  color: 'var(--text-secondary)',
                  fontSize: 12,
                }}
              >
                <span />
                <span>{p('task')}</span>
                <span>{p('owner')}</span>
                <span>{p('progress')}</span>
              </Box>
              {[
                { task: p('task1'), person: p('person1'), percent: 50, selected: true },
                { task: p('task2'), person: p('person2'), percent: 20, selected: false },
                { task: p('task3'), person: p('person3'), percent: 0, selected: false },
              ].map((row) => (
                <Box
                  key={row.task}
                  data-testid={row.selected ? 'colors-preview-selected' : undefined}
                  sx={{
                    ...rowSx,
                    backgroundColor: row.selected ? 'var(--bg-selected)' : 'transparent',
                  }}
                >
                  <Check checked={row.selected} />
                  <span>{row.task}</span>
                  <Owner name={row.person} />
                  <Progress percent={row.percent} />
                </Box>
              ))}
            </Box>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 'var(--space-2)',
                padding: 'var(--space-2) var(--space-3)',
                border: '1px solid var(--border)',
                borderRadius: 'var(--radius-lg)',
                backgroundColor: 'var(--bg-surface)',
              }}
            >
              <Owner name={p('author')} />
              <Box component="span">
                <Box
                  component="mark"
                  sx={{
                    padding: '0 4px',
                    borderRadius: 'var(--radius-sm)',
                    backgroundColor: 'var(--bg-selected)',
                    color: 'var(--action-text)',
                    fontWeight: 500,
                  }}
                >
                  {`@${p('person1')}`}
                </Box>{' '}
                {p('mention')}
              </Box>
            </Box>
          </Box>
          <Box
            data-testid="colors-preview-sign-in"
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: '4px',
              margin: '0 var(--space-4) var(--space-4)',
              padding: 'var(--space-4)',
              borderRadius: 'var(--radius-lg)',
              backgroundImage:
                'linear-gradient(135deg, var(--brand-panel-bg), var(--brand-panel-bg-2))',
              color: 'var(--brand-panel-text)',
            }}
          >
            <Box component="span" sx={{ fontSize: 12, color: 'var(--brand-panel-muted)' }}>
              {p('signInLabel')}
            </Box>
            <Box component="strong" sx={{ fontSize: 15, fontWeight: 500 }}>
              {p('signInTitle')}
            </Box>
          </Box>
        </Box>
      </Box>
      <Box
        component="p"
        sx={{
          margin: 0,
          padding: 'var(--space-3) var(--space-4)',
          borderTop: '1px solid var(--border)',
          fontSize: 12,
          lineHeight: '16px',
          color: 'var(--text-secondary)',
        }}
      >
        {p('legend')}
      </Box>
    </Box>
  );
}
