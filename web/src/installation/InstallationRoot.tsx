import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { installationQuery } from '../api/queries';
import { BrandColors } from './BrandColors';
import { faviconOf, showFavicon } from './favicon';
import { useInstallation } from './useInstallation';

/**
 * Around every screen, with or without a session: the tab of the browser has the name and the logo of the installation
 * (ADR-0108), and the screens its main color.
 */
export function InstallationRoot() {
  const { name, logoUrl } = useInstallation();
  const { data: installation } = useQuery(installationQuery());
  const mainColor = installation?.mainColor ?? null;
  const loaded = installation !== undefined;

  useEffect(() => {
    if (name !== '') {
      document.title = name;
    }
  }, [name]);

  // Once the brand is known, so the badge never flashes before the logo.
  useEffect(() => {
    if (loaded) {
      showFavicon(faviconOf(logoUrl, mainColor));
    }
  }, [loaded, logoUrl, mainColor]);

  return (
    <>
      <BrandColors />
      <Outlet />
    </>
  );
}
