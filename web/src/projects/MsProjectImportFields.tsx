import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import Box from '@mui/material/Box';
import { useId, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import type { MsProjectPreview, MsProjectResource } from '../api/client';
import { Alert } from '../components/Alert';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { PersonField, type PersonChoice } from '../components/PersonField';
import { PersonLines } from '../components/PersonLines';
import { Select } from '../components/Select';
import { sizeText } from '../format/size';
import {
  namesOf,
  type ResourceOwner,
  type RoleOfNewPerson,
  rolesOfNewPeople,
} from './msProjectImport';

/**
 * The file of the MS Project in "A partir do MS Project" (ADR-0135): the button that chooses the XML, its name and size,
 * and the helper that says how to save it, or the reason the server refused it.
 */
export function MsProjectFileField({
  file,
  reading,
  error,
  onChoose,
}: {
  file: File | null;
  reading: boolean;
  error?: string;
  onChoose: (file: File) => void;
}) {
  const { t } = useTranslation();
  const input = useRef<HTMLInputElement>(null);
  const helpId = useId();
  return (
    <FormField
      label={t('projects.form.msProject.file')}
      error={error}
      helper={t('projects.form.msProject.fileHelper')}
      helpId={helpId}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', flexWrap: 'wrap' }}>
        <Button
          variant="outlined"
          size="small"
          startIcon={<UploadFileOutlined />}
          loading={reading}
          aria-describedby={helpId}
          onClick={() => input.current?.click()}
        >
          {t(file === null ? 'projects.form.msProject.choose' : 'projects.form.msProject.change')}
        </Button>
        <Box
          component="span"
          data-testid="ms-project-file-name"
          sx={{
            minWidth: 0,
            overflowWrap: 'anywhere',
            color: file === null ? 'var(--text-secondary)' : 'var(--text)',
          }}
        >
          {file === null
            ? t('projects.form.msProject.noFile')
            : `${file.name} · ${sizeText(file.size)}`}
        </Box>
      </Box>
      <input
        ref={input}
        type="file"
        accept=".xml,application/xml,text/xml"
        hidden
        aria-hidden
        tabIndex={-1}
        data-testid="ms-project-file"
        onChange={(event) => {
          const picked = event.target.files?.[0];
          event.target.value = '';
          if (picked !== undefined) {
            onChoose(picked);
          }
        }}
      />
    </FormField>
  );
}

/** What the conversion changed, in an info alert, each kind with the names of the lines (ADR-0135). */
export function MsProjectWarnings({ preview }: { preview: MsProjectPreview }) {
  const { t } = useTranslation();
  if (preview.warnings.length === 0) {
    return null;
  }
  return (
    <Alert tone="info" title={t('projects.form.msProject.warnings')}>
      <Box component="ul" sx={{ margin: 'var(--space-1) 0 0', paddingLeft: 'var(--space-4)' }}>
        {preview.warnings.map((warning) => {
          const { names, more } = namesOf(warning);
          return (
            <li key={warning.kind}>
              {t(`projects.form.msProject.warning.${warning.kind}`, {
                count: warning.names.length,
                title: preview.title,
                names:
                  more === 0
                    ? names
                    : t('projects.form.msProject.namesAndMore', { names, count: more }),
              })}
            </li>
          );
        })}
      </Box>
    </Alert>
  );
}

type OwnerKind = ResourceOwner['kind'];

/** The role a person of the form keeps: the manager and the tech lead keep theirs (ADR-0135). */
function keptRole(
  person: PersonChoice | null,
  managerId: string | null,
  techLeadId: string | null,
): 'Manager' | 'TechLead' | undefined {
  if (person?.id == null) {
    return undefined;
  }
  return person.id === managerId ? 'Manager' : person.id === techLeadId ? 'TechLead' : undefined;
}

/** A resource of the file and who it becomes: a person with the role, the client or nobody. */
function OwnerRow({
  resource,
  owner,
  clientName,
  managerId,
  techLeadId,
  onChange,
}: {
  resource: MsProjectResource;
  owner: ResourceOwner;
  clientName?: string;
  managerId: string | null;
  techLeadId: string | null;
  onChange: (owner: ResourceOwner) => void;
}) {
  const { t } = useTranslation();
  const titleId = useId();
  // The client only in a project with one; without it, the resource stays without an owner.
  const kind: OwnerKind =
    owner.kind === 'client' && clientName === undefined ? 'nobody' : owner.kind;
  const kinds = [
    { value: 'person' as const, label: t('projects.form.msProject.owners.person') },
    ...(clientName === undefined
      ? []
      : [
          {
            value: 'client' as const,
            label: t('projects.form.msProject.owners.client', { name: clientName }),
          },
        ]),
    { value: 'nobody' as const, label: t('projects.form.msProject.owners.nobody') },
  ];
  const person = owner.kind === 'person' ? owner.person : null;
  const role: RoleOfNewPerson = owner.kind === 'person' ? owner.role : 'Contributor';
  const kept = keptRole(person, managerId, techLeadId);
  return (
    <Box
      component="li"
      aria-labelledby={titleId}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-3)',
        padding: 'var(--space-3) 0',
        borderTop: '1px solid var(--border)',
      }}
    >
      <Box id={titleId} sx={{ fontSize: 14, lineHeight: '20px', fontWeight: 500 }}>
        <PersonLines
          name={resource.name}
          email={resource.email}
          note={t('projects.form.msProject.owners.tasks', { count: resource.tasks })}
        />
      </Box>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, minmax(0, 1fr))',
          gap: 'var(--space-3)',
          alignItems: 'start',
          // Below 700 px the fields stack, as the rows of the form (fp-form-row).
          '@media (max-width: 700px)': { gridTemplateColumns: 'minmax(0, 1fr)' },
        }}
      >
        <FormField label={t('projects.form.msProject.owners.kind')}>
          <Select<OwnerKind>
            label={t('projects.form.msProject.owners.kindOf', { name: resource.name })}
            value={kind}
            options={kinds}
            onChange={(value) => {
              onChange(
                value === 'person'
                  ? { kind: 'person', person: null, role: 'Contributor' }
                  : { kind: value },
              );
            }}
          />
        </FormField>
        {kind === 'person' && (
          <PersonField
            label={t('projects.form.msProject.owners.personField')}
            accessibleLabel={t('projects.form.msProject.owners.personOf', { name: resource.name })}
            placeholder={t('projects.form.msProject.owners.noPerson')}
            value={person}
            onChange={(chosen) => {
              onChange({ kind: 'person', person: chosen, role });
            }}
          />
        )}
        {kind === 'person' && person !== null && (
          <FormField label={t('projects.form.msProject.owners.role')}>
            {kept === undefined ? (
              <Select<RoleOfNewPerson>
                label={t('projects.form.msProject.owners.roleOf', { name: resource.name })}
                value={role}
                options={rolesOfNewPeople.map((value) => ({
                  value,
                  label: t(`projects.roles.${value}`),
                }))}
                onChange={(value) => {
                  onChange({ kind: 'person', person, role: value });
                }}
              />
            ) : (
              <Select
                label={t('projects.form.msProject.owners.roleOf', { name: resource.name })}
                value={kept}
                options={[{ value: kept, label: t(`projects.roles.${kept}`) }]}
                disabled
                onChange={() => undefined}
              />
            )}
          </FormField>
        )}
      </Box>
    </Box>
  );
}

/**
 * "Responsáveis do arquivo" (ADR-0135): each work resource of the file, with its e-mail and how many tasks it has, and
 * who it becomes in the project, suggested by the server.
 */
export function MsProjectOwners({
  preview,
  owners,
  clientName,
  managerId,
  techLeadId,
  onChange,
}: {
  preview: MsProjectPreview;
  owners: readonly ResourceOwner[];
  clientName?: string;
  managerId: string | null;
  techLeadId: string | null;
  onChange: (owners: ResourceOwner[]) => void;
}) {
  const { t } = useTranslation();
  const titleId = useId();
  if (preview.resources.length === 0) {
    return null;
  }
  return (
    <Box
      component="section"
      aria-labelledby={titleId}
      sx={{ display: 'flex', flexDirection: 'column' }}
    >
      <Box
        component="h3"
        id={titleId}
        sx={{ margin: 0, fontSize: 15, lineHeight: '22px', fontWeight: 500, color: 'var(--text)' }}
      >
        {t('projects.form.msProject.owners.title')}
      </Box>
      <Box
        component="p"
        sx={{
          margin: 'var(--space-1) 0 var(--space-3)',
          fontSize: 13,
          lineHeight: '18px',
          color: 'var(--text-secondary)',
        }}
      >
        {t('projects.form.msProject.owners.helper')}
      </Box>
      <Box component="ul" sx={{ margin: 0, padding: 0, listStyle: 'none' }}>
        {preview.resources.map((resource, index) => (
          <OwnerRow
            // The resources of a preview have no ids of their own in the API.
            key={`${String(index)}-${resource.name}`}
            resource={resource}
            owner={owners[index] ?? { kind: 'nobody' }}
            clientName={clientName}
            managerId={managerId}
            techLeadId={techLeadId}
            onChange={(owner) => {
              onChange(owners.map((current, position) => (position === index ? owner : current)));
            }}
          />
        ))}
      </Box>
    </Box>
  );
}
