import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { installationQuery } from '../api/queries';

/**
 * The brand of the installation (ADR-0003): its name and the address of its logo and of the one for the dark theme
 * (ADR-0100), null without them. While the API has not answered, the name is empty, so the screen never shows a name
 * that is not the installation's; if the API fails, it is the product name.
 */
export function useInstallation(): {
  name: string;
  logoUrl: string | null;
  darkLogoUrl: string | null;
} {
  const { t } = useTranslation();
  const { data, isError } = useQuery(installationQuery());
  return {
    name: data?.name ?? (isError ? t('app.name') : ''),
    logoUrl: data?.logoUrl ?? null,
    darkLogoUrl: data?.darkLogoUrl ?? null,
  };
}
