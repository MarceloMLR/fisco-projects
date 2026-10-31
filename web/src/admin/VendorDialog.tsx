import { zodResolver } from '@hookform/resolvers/zod';
import Box from '@mui/material/Box';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { api, ticketProblems, type Vendor } from '../api/client';
import { Alert } from '../components/Alert';
import { Dialog } from '../components/Dialog';
import { TextField } from '../components/TextField';
import { problemType } from '../projects/projectForm';
import { Button } from '../components/Button';

const errors = {
  nameRequired: 'vendors.errors.nameRequired',
  nameInUse: 'vendors.errors.nameInUse',
  portalInvalid: 'vendors.errors.portalInvalid',
} as const;

type VendorError = (typeof errors)[keyof typeof errors];
const isVendorError = (message: string | undefined): message is VendorError =>
  Object.values(errors).some((error) => error === message);

const schema = z.object({
  name: z.string().trim().min(1, errors.nameRequired).max(150, errors.nameRequired),
  portalUrl: z
    .string()
    .trim()
    .refine(
      (url) => url === '' || (url.length <= 500 && /^https?:\/\/\S+$/i.test(url)),
      errors.portalInvalid,
    ),
});

type Values = z.input<typeof schema>;

/**
 * "Novo fornecedor" and "Editar fornecedor" (ADR-0028, ADR-0046): the name, which no other vendor has, and the address
 * of the support portal, optional. A new vendor is active.
 */
export function VendorDialog({
  vendor,
  onClose,
  onSaved,
}: {
  /** The vendor to edit; none for a new one. */
  vendor?: Vendor;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors: fieldErrors },
  } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: vendor?.name ?? '', portalUrl: vendor?.portalUrl ?? '' },
  });
  const errorText = (message: string | undefined) =>
    isVendorError(message) ? t(message) : undefined;

  const save = handleSubmit(async (values) => {
    const name = values.name.trim();
    const portalUrl = values.portalUrl.trim() === '' ? null : values.portalUrl.trim();
    try {
      const { error, response } =
        vendor === undefined
          ? await api.POST('/api/admin/vendors', { body: { name, portalUrl } })
          : await api.PUT('/api/admin/vendors/{id}', {
              params: { path: { id: vendor.id } },
              body: { name, portalUrl, isActive: vendor.isActive },
            });
      if (response.ok) {
        onSaved(
          t(vendor === undefined ? 'vendors.messages.added' : 'vendors.messages.saved', { name }),
        );
        return;
      }
      if (response.status === 409 && problemType(error) === ticketProblems.vendorNameInUse) {
        setError('name', { message: errors.nameInUse });
        return;
      }
      setError('root', { message: t('vendors.messages.failed') });
    } catch {
      setError('root', { message: t('vendors.messages.failed') });
    }
  });

  return (
    <Dialog
      open
      title={t(vendor === undefined ? 'vendors.dialog.newTitle' : 'vendors.dialog.editTitle')}
      onClose={onClose}
      onSubmit={(event) => save(event)}
      actions={
        <>
          <Button variant="outlined" onClick={onClose}>
            {t('vendors.dialog.cancel')}
          </Button>
          <Button type="submit" variant="contained">
            {t(vendor === undefined ? 'vendors.dialog.add' : 'vendors.dialog.save')}
          </Button>
        </>
      }
    >
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <TextField
          label={t('vendors.dialog.name')}
          autoComplete="off"
          maxLength={160}
          error={errorText(fieldErrors.name?.message)}
          {...register('name')}
        />
        <TextField
          label={`${t('vendors.dialog.portal')} ${t('vendors.dialog.optional')}`}
          type="url"
          autoComplete="off"
          maxLength={500}
          helper={t('vendors.dialog.portalHelp')}
          error={errorText(fieldErrors.portalUrl?.message)}
          {...register('portalUrl')}
        />
        {fieldErrors.root?.message !== undefined && (
          <Alert tone="danger">{fieldErrors.root.message}</Alert>
        )}
      </Box>
    </Dialog>
  );
}
