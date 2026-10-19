import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { BrandColors } from './BrandColors';
import { useInstallation } from './useInstallation';

/**
 * Around every screen, with or without a session: the tab of the browser has the name of the installation, and the
 * screens its main color.
 */
export function InstallationRoot() {
  const { name } = useInstallation();

  useEffect(() => {
    if (name !== '') {
      document.title = name;
    }
  }, [name]);

  return (
    <>
      <BrandColors />
      <Outlet />
    </>
  );
}
