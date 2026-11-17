import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { installationQuery } from '../api/queries';
import { NoticeProvider } from '../notices/Notices';
import { BrandColors } from './BrandColors';
import { faviconOf, showFavicon } from './favicon';
import { useInstallation } from './useInstallation';

/**
 * Around every screen, with or without a session: the tab of the browser has the name and the logo of the installation
 * (ADR-0108), the screens its main color, and the notices of all of them come from here (ADR-0133).
 */
export function InstallationRoot() {
  const { name, logoUrl } = useInstallation();
  const { data: installation } = useQuery(installationQuery());
  const loaded = installation !== undefined;

  useEffect(() => {
    if (name !== '') {
      document.title = name;
    }
  }, [name]);

  // Once the brand is known: until then the tab keeps the symbol of the product the page starts with.
  useEffect(() => {
    if (loaded) {
      showFavicon(faviconOf(logoUrl));
    }
  }, [loaded, logoUrl]);

  return (
    <NoticeProvider>
      <BrandColors />
      <Outlet />
    </NoticeProvider>
  );
}
