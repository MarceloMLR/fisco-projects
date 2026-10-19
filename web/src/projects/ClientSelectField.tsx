import { useQuery } from '@tanstack/react-query';
import { useId, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { clientChoicesQuery } from '../api/queries';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { noClient } from './projectForm';

/**
 * The client of a project (ADR-0015): an active client, or none for an internal project. An inactive client stays
 * offered only when it is already the client of the project.
 */
export function ClientSelectField({
  value,
  onChange,
  currentClientId,
  error,
}: {
  /** The id of the client, or noClient. */
  value: string;
  onChange: (value: string) => void;
  currentClientId?: string | null;
  error?: string;
}) {
  const { t } = useTranslation();
  const helpId = useId();
  const { data: choices } = useQuery(clientChoicesQuery());
  const options = useMemo(
    () => [
      { value: noClient, label: t('projects.form.noClient') },
      ...(choices ?? [])
        .filter((client) => client.status === 'Active' || client.id === currentClientId)
        .map((client) => ({
          value: client.id,
          label:
            client.status === 'Active'
              ? client.name
              : t('projects.form.inactiveClient', { name: client.name }),
        })),
    ],
    [choices, currentClientId, t],
  );
  // A client just included shows once the choices are read again.
  const known = options.some((option) => option.value === value);

  return (
    <FormField
      label={t('projects.form.client')}
      optional={t('projects.form.optional')}
      error={error}
      helpId={helpId}
    >
      <Select
        label={t('projects.form.client')}
        value={known ? value : noClient}
        options={options}
        onChange={onChange}
        invalid={error !== undefined}
        describedBy={error === undefined ? undefined : helpId}
      />
    </FormField>
  );
}
