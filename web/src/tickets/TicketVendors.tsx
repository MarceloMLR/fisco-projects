import { zodResolver } from '@hookform/resolvers/zod';
import type { TFunction } from 'i18next';
import AddOutlined from '@mui/icons-material/AddOutlined';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import {
  api,
  type Ticket,
  ticketProblems,
  type VendorTicket,
  type VendorTicketStatus,
  vendorTicketStatuses,
} from '../api/client';
import { queryKeys, vendorChoicesQuery } from '../api/queries';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { DueDate } from '../components/DueDate';
import { FormField } from '../components/FormField';
import { Select } from '../components/Select';
import { TextArea } from '../components/TextArea';
import { TextField } from '../components/TextField';
import { formatDayText } from '../format/dates';
import { useDates } from '../format/useDates';
import { problemField } from './ticketChanges';
import { VendorStatusBadge } from './TicketTags';

const labelSx = { fontSize: 13, lineHeight: '18px', color: 'var(--text-secondary)' } as const;

const errors = {
  vendorRequired: 'tickets.vendors.errors.vendorRequired',
  codeRequired: 'tickets.vendors.errors.codeRequired',
  openedOnInvalid: 'tickets.vendors.errors.openedOnInvalid',
  linkInvalid: 'tickets.vendors.errors.linkInvalid',
  noteTooLong: 'tickets.vendors.errors.noteTooLong',
} as const;

type VendorError = (typeof errors)[keyof typeof errors];
const isVendorError = (message: string | undefined): message is VendorError =>
  Object.values(errors).some((error) => error === message);

const webAddress = /^https?:\/\/\S+$/i;

const note = z.string().max(1000, errors.noteTooLong);

/** The fields of a registration, with the day it was opened up to today, in the time zone of the installation. */
const registrationSchema = (today: string) =>
  z.object({
    vendorId: z.string().min(1, errors.vendorRequired),
    code: z.string().trim().min(1, errors.codeRequired).max(60, errors.codeRequired),
    openedOn: z
      .string()
      .refine((day) => /^\d{4}-\d{2}-\d{2}$/.test(day) && day <= today, errors.openedOnInvalid),
    status: z.enum(vendorTicketStatuses),
    link: z
      .string()
      .trim()
      .refine(
        (link) => link === '' || (link.length <= 500 && webAddress.test(link)),
        errors.linkInvalid,
      ),
    note,
  });

type RegistrationValues = z.input<ReturnType<typeof registrationSchema>>;

const updateSchema = z.object({ status: z.enum(vendorTicketStatuses), note });

type UpdateValues = z.input<typeof updateSchema>;

/** Why the API refused a registration or an update, in words of the screen. */
function refusalText(status: number, problem: unknown, t: TFunction): string {
  const type = problemField(problem, 'type');
  if (type === ticketProblems.ticketClosed) {
    return t('tickets.vendors.messages.closed');
  }
  if (type === ticketProblems.ticketChanged) {
    return t('tickets.vendors.messages.changed');
  }
  if (status === 403 || status === 404) {
    return t('tickets.vendors.messages.forbidden');
  }
  return t('tickets.vendors.messages.failed');
}

/** Whether the API refused the field, in the errors of a validation problem. */
function refusedField(problem: unknown, field: string): boolean {
  const fields = problemField(problem, 'errors');
  return typeof fields === 'object' && fields !== null && field in fields;
}

/**
 * "Registrar no fornecedor" (ADR-0028): the vendor among the active ones, the number of the ticket there, the day it
 * was opened, the status there, the link and a note.
 */
function RegisterDialog({
  ticket,
  onClose,
  onDone,
}: {
  ticket: Ticket;
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const ids = useId();
  const queryClient = useQueryClient();
  const { toDayValue } = useDates();
  const today = toDayValue(new Date());
  const { data: vendors, isError } = useQuery(vendorChoicesQuery());
  const schema = useMemo(() => registrationSchema(today), [today]);
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<RegistrationValues>({
    resolver: zodResolver(schema),
    defaultValues: { vendorId: '', code: '', openedOn: today, status: 'Open', link: '', note: '' },
  });
  const errorText = (message: string | undefined) =>
    isVendorError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    try {
      const { response, error } = await api.POST('/api/tickets/{number}/vendor-tickets', {
        params: { path: { number: ticket.number } },
        body: {
          vendorId: values.vendorId,
          code: values.code.trim(),
          openedOn: values.openedOn,
          status: values.status,
          link: values.link.trim() === '' ? null : values.link.trim(),
          note: values.note.trim() === '' ? null : values.note.trim(),
        },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
        const vendor = vendors?.find((item) => item.id === values.vendorId)?.name ?? '';
        onDone(t('tickets.vendors.messages.registered', { code: values.code.trim(), vendor }));
        return;
      }
      if (response.status === 400 && refusedField(error, 'vendorId')) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.vendors });
        setError('vendorId', { message: errors.vendorRequired });
        return;
      }
      setError('root', { message: refusalText(response.status, error, t) });
    } catch {
      setError('root', { message: t('tickets.vendors.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('tickets.vendors.register.title')}
      subtitle={t('tickets.vendors.register.subtitle', { number: ticket.number })}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.vendors.cancel')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting || vendors?.length === 0}
          >
            {t('tickets.vendors.register.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {isError && <Alert tone="danger">{t('tickets.vendors.messages.loadFailed')}</Alert>}
        {vendors?.length === 0 && <Alert tone="info">{t('tickets.vendors.register.none')}</Alert>}
        <FormField
          label={t('tickets.vendors.fields.vendor')}
          error={errorText(fieldErrors.vendorId?.message)}
          helpId={`${ids}-vendor-help`}
        >
          <Controller
            control={control}
            name="vendorId"
            render={({ field }) => (
              <Select<string>
                label={t('tickets.vendors.fields.vendor')}
                value={field.value}
                options={[
                  { value: '', label: t('tickets.vendors.register.choose'), disabled: true },
                  ...(vendors ?? []).map((vendor) => ({ value: vendor.id, label: vendor.name })),
                ]}
                invalid={fieldErrors.vendorId !== undefined}
                describedBy={`${ids}-vendor-help`}
                onChange={field.onChange}
              />
            )}
          />
        </FormField>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
            gap: 'var(--space-4)',
          }}
        >
          <TextField
            label={t('tickets.vendors.fields.code')}
            autoComplete="off"
            maxLength={60}
            error={errorText(fieldErrors.code?.message)}
            {...register('code')}
          />
          <TextField
            label={t('tickets.vendors.fields.openedOn')}
            type="date"
            max={today}
            error={errorText(fieldErrors.openedOn?.message)}
            {...register('openedOn')}
          />
        </Box>
        <FormField label={t('tickets.vendors.fields.status')}>
          <Controller
            control={control}
            name="status"
            render={({ field }) => (
              <Select<VendorTicketStatus>
                label={t('tickets.vendors.fields.status')}
                value={field.value}
                options={vendorTicketStatuses.map((status) => ({
                  value: status,
                  label: t(`tickets.vendors.statuses.${status}`),
                }))}
                onChange={field.onChange}
              />
            )}
          />
        </FormField>
        <TextField
          label={`${t('tickets.vendors.fields.link')} ${t('tickets.vendors.optional')}`}
          type="url"
          autoComplete="off"
          maxLength={500}
          error={errorText(fieldErrors.link?.message)}
          {...register('link')}
        />
        <TextArea
          label={`${t('tickets.vendors.fields.note')} ${t('tickets.vendors.optional')}`}
          rows={3}
          maxLength={1000}
          error={errorText(fieldErrors.note?.message)}
          {...register('note')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}

/** "Atualizar" a ticket at a vendor (ADR-0028): the new status there, with a note for the conversation. */
function UpdateDialog({
  ticket,
  record,
  onClose,
  onDone,
}: {
  ticket: Ticket;
  record: VendorTicket;
  onClose: () => void;
  onDone: (message: string) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<UpdateValues>({
    resolver: zodResolver(updateSchema),
    defaultValues: { status: record.status, note: '' },
  });

  const save = handleSubmit(async (values) => {
    try {
      const { response, error } = await api.PUT('/api/tickets/{number}/vendor-tickets/{id}', {
        params: { path: { number: ticket.number, id: record.id } },
        body: {
          status: values.status,
          note: values.note.trim() === '' ? null : values.note.trim(),
        },
      });
      if (response.ok) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.tickets });
        onDone(t('tickets.vendors.messages.updated', { code: record.code, vendor: record.vendor }));
        return;
      }
      setError('root', { message: refusalText(response.status, error, t) });
    } catch {
      setError('root', { message: t('tickets.vendors.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t('tickets.vendors.update.title')}
      subtitle={t('tickets.vendors.update.subtitle', { code: record.code, vendor: record.vendor })}
      onClose={onClose}
      onSubmit={(event) => {
        void save(event);
      }}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('tickets.vendors.cancel')}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {t('tickets.vendors.update.confirm')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <FormField label={t('tickets.vendors.fields.status')}>
          <Controller
            control={control}
            name="status"
            render={({ field }) => (
              <Select<VendorTicketStatus>
                label={t('tickets.vendors.fields.status')}
                value={field.value}
                options={vendorTicketStatuses.map((status) => ({
                  value: status,
                  label: t(`tickets.vendors.statuses.${status}`),
                }))}
                onChange={field.onChange}
              />
            )}
          />
        </FormField>
        <TextArea
          label={`${t('tickets.vendors.fields.annotation')} ${t('tickets.vendors.optional')}`}
          rows={3}
          maxLength={1000}
          error={
            fieldErrors.note?.message === errors.noteTooLong ? t(errors.noteTooLong) : undefined
          }
          {...register('note')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}

/**
 * "No fornecedor" (canvas Chamado, ADR-0028): each ticket registered at a vendor, with the vendor, its number there
 * (a link, when it has one), the status there and, when it is stale, how many business days it has gone without an
 * update. Who serves the ticket registers ("Registrar") and updates them ("Atualizar"); our deadlines keep running.
 */
export function VendorSection({
  ticket,
  onMessage,
}: {
  ticket: Ticket;
  onMessage: (message: string) => void;
}) {
  const { t } = useTranslation();
  const [registering, setRegistering] = useState(false);
  const [updating, setUpdating] = useState<VendorTicket>();
  const canRegister = ticket.permissions.canRegisterVendorTicket;

  return (
    <Box
      component="section"
      aria-labelledby="ticket-vendors"
      sx={{ paddingTop: 'var(--space-4)', borderTop: '1px solid var(--border)' }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 'var(--space-2)',
          minHeight: 28,
          marginBottom: 'var(--space-2)',
        }}
      >
        <Box
          component="h2"
          id="ticket-vendors"
          sx={{ margin: 0, fontSize: 13, lineHeight: '18px', fontWeight: 500 }}
        >
          {t('tickets.vendors.title')}
        </Box>
        {canRegister && (
          <Button
            variant="text"
            size="small"
            startIcon={<AddOutlined />}
            onClick={() => {
              setRegistering(true);
            }}
          >
            {t('tickets.vendors.register.button')}
          </Button>
        )}
      </Box>
      {ticket.vendorTickets.length === 0 ? (
        <Box component="span" sx={labelSx}>
          {t('tickets.vendors.none')}
        </Box>
      ) : (
        <Box
          component="ul"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-4)',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          {ticket.vendorTickets.map((record) => (
            <Box
              component="li"
              key={record.id}
              aria-label={t('tickets.vendors.record', { code: record.code, vendor: record.vendor })}
              sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-1)' }}
            >
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  justifyContent: 'space-between',
                  gap: 'var(--space-2)',
                }}
              >
                <Box sx={{ minWidth: 0 }}>
                  <Box
                    component="span"
                    sx={{ display: 'block', fontSize: 14, lineHeight: '20px', fontWeight: 500 }}
                  >
                    {record.vendor}
                  </Box>
                  {record.link === null ? (
                    <Box
                      component="span"
                      sx={{ ...labelSx, fontFamily: 'var(--font-mono)', fontWeight: 500 }}
                    >
                      {record.code}
                    </Box>
                  ) : (
                    <Box
                      component="a"
                      href={record.link}
                      target="_blank"
                      rel="noopener noreferrer"
                      sx={{
                        ...labelSx,
                        fontFamily: 'var(--font-mono)',
                        fontWeight: 500,
                        color: 'var(--action-text)',
                        textDecoration: 'none',
                        '&:hover': { textDecoration: 'underline' },
                      }}
                    >
                      {record.code}
                    </Box>
                  )}
                </Box>
                <VendorStatusBadge status={record.status} />
              </Box>
              {record.staleDays !== null && (
                <DueDate state="soon">
                  {t('tickets.vendors.stale', { count: record.staleDays })}
                </DueDate>
              )}
              {record.note !== null && (
                <Box
                  component="p"
                  sx={{
                    ...labelSx,
                    margin: 0,
                    display: '-webkit-box',
                    WebkitBoxOrient: 'vertical',
                    WebkitLineClamp: 3,
                    overflow: 'hidden',
                    overflowWrap: 'anywhere',
                  }}
                >
                  {record.note}
                </Box>
              )}
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: 'var(--space-2)',
                }}
              >
                <Box component="span" sx={{ ...labelSx, fontSize: 12, lineHeight: '16px' }}>
                  {t('tickets.vendors.openedOn', { day: formatDayText(record.openedOn) })}
                </Box>
                {canRegister && (
                  <Button
                    variant="text"
                    size="small"
                    aria-label={t('tickets.vendors.update.button', { code: record.code })}
                    onClick={() => {
                      setUpdating(record);
                    }}
                  >
                    {t('tickets.vendors.update.label')}
                  </Button>
                )}
              </Box>
            </Box>
          ))}
        </Box>
      )}
      {registering && (
        <RegisterDialog
          ticket={ticket}
          onClose={() => {
            setRegistering(false);
          }}
          onDone={(message) => {
            setRegistering(false);
            onMessage(message);
          }}
        />
      )}
      {updating !== undefined && (
        <UpdateDialog
          ticket={ticket}
          record={updating}
          onClose={() => {
            setUpdating(undefined);
          }}
          onDone={(message) => {
            setUpdating(undefined);
            onMessage(message);
          }}
        />
      )}
    </Box>
  );
}
