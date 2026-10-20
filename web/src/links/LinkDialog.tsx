import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQuery } from '@tanstack/react-query';
import { useId } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, documentProblems, type ProjectLink } from '../api/client';
import { activeLinkCategoriesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';

const errors = {
  titleRequired: 'links.errors.titleRequired',
  urlInvalid: 'links.errors.urlInvalid',
  categoryRequired: 'links.errors.categoryRequired',
  descriptionTooLong: 'links.errors.descriptionTooLong',
} as const;

type LinkError = (typeof errors)[keyof typeof errors];
const isLinkError = (message: string | undefined): message is LinkError =>
  Object.values(errors).some((error) => error === message);

// An address of the web, as the API takes it (ADR-0032): http or https, up to 2,000 characters.
const webAddress = /^https?:\/\/\S+$/i;

const schema = z.object({
  title: z.string().trim().min(1, errors.titleRequired).max(120, errors.titleRequired),
  url: z
    .string()
    .trim()
    .refine((url) => url.length <= 2000 && webAddress.test(url), errors.urlInvalid),
  categoryId: z.string().min(1, errors.categoryRequired),
  description: z.string().trim().max(500, errors.descriptionTooLong),
});

type Values = z.input<typeof schema>;

/** The fields the API refused, from the errors of its validation problem. */
function refusedFields(problem: unknown): string[] {
  return typeof problem === 'object' &&
    problem !== null &&
    'errors' in problem &&
    typeof problem.errors === 'object' &&
    problem.errors !== null
    ? Object.keys(problem.errors)
    : [];
}

/**
 * "Novo link" and "Editar link" (canvas ProjetoLinks, ADR-0032): the title, the address, the category and what it is
 * for. The categories are the active ones and, for a link, the one it already has, even if it became inactive.
 */
export function LinkDialog({
  projectId,
  link,
  onClose,
  onSaved,
}: {
  projectId: string;
  /** The link to change; none for a new one. */
  link?: ProjectLink;
  onClose: () => void;
  /** The link was saved, with the message; closed tells the project was closed meanwhile. */
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const { data: active, isError } = useQuery(activeLinkCategoriesQuery());
  const categories = [
    ...(active ?? []).map((category) => ({ value: category.id, label: category.name })),
    ...(link !== undefined && !(active ?? []).some((category) => category.id === link.categoryId)
      ? [
          {
            value: link.categoryId,
            label: t('links.dialog.inactive', { name: link.categoryName }),
          },
        ]
      : []),
  ];
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: link?.title ?? '',
      url: link?.url ?? '',
      categoryId: link?.categoryId ?? '',
      description: link?.description ?? '',
    },
  });
  const errorText = (message: string | undefined) =>
    isLinkError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const body = {
      title: values.title.trim(),
      url: values.url.trim(),
      categoryId: values.categoryId,
      description: values.description.trim() === '' ? null : values.description.trim(),
    };
    try {
      const { response, error } =
        link === undefined
          ? await api.POST('/api/projects/{projectId}/links', {
              params: { path: { projectId } },
              body,
            })
          : await api.PUT('/api/projects/{projectId}/links/{linkId}', {
              params: { path: { projectId, linkId: link.id } },
              body,
            });
      if (response.ok) {
        onSaved(
          t(link === undefined ? 'links.messages.added' : 'links.messages.saved', {
            title: body.title,
          }),
        );
        return;
      }
      if (response.status === 400) {
        const refused = refusedFields(error);
        const fields = [
          ['title', errors.titleRequired],
          ['url', errors.urlInvalid],
          ['categoryId', errors.categoryRequired],
          ['description', errors.descriptionTooLong],
        ] as const;
        for (const [field, message] of fields) {
          if (refused.includes(field)) {
            setError(field, { message });
          }
        }
        return;
      }
      setError('root', {
        message:
          response.status === 409 && problemType(error) === documentProblems.projectClosed
            ? t('links.messages.closed')
            : t('links.messages.failed'),
      });
    } catch {
      setError('root', { message: t('links.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(link === undefined ? 'links.dialog.newTitle' : 'links.dialog.editTitle')}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('links.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t(link === undefined ? 'links.dialog.add' : 'links.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {isError && <Alert tone="danger">{t('links.messages.failed')}</Alert>}
        <TextField
          label={t('links.dialog.title')}
          autoComplete="off"
          maxLength={130}
          helper={t('links.dialog.titleHelp')}
          error={errorText(fieldErrors.title?.message)}
          {...register('title')}
        />
        <TextField
          label={t('links.dialog.url')}
          type="url"
          autoComplete="off"
          maxLength={2010}
          helper={t('links.dialog.urlHelp')}
          error={errorText(fieldErrors.url?.message)}
          {...register('url')}
        />
        <FormField
          label={t('links.dialog.category')}
          error={errorText(fieldErrors.categoryId?.message)}
          helpId={`${ids}-category-help`}
        >
          <Controller
            control={control}
            name="categoryId"
            render={({ field }) => (
              <Select<string>
                label={t('links.dialog.category')}
                value={field.value}
                options={[{ value: '', label: t('links.category'), disabled: true }, ...categories]}
                invalid={fieldErrors.categoryId !== undefined}
                describedBy={`${ids}-category-help`}
                onChange={field.onChange}
              />
            )}
          />
        </FormField>
        <TextArea
          label={t('links.dialog.description')}
          rows={3}
          maxLength={510}
          helper={t('links.dialog.descriptionHelp')}
          error={errorText(fieldErrors.description?.message)}
          {...register('description')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
