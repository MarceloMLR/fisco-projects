import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { useInstallation } from './useInstallation';

/** Around every screen, with or without a session: the tab of the browser has the name of the installation. */
export function InstallationRoot() {
  const { name } = useInstallation();

  useEffect(() => {
    if (name !== '') {
      document.title = name;
    }
  }, [name]);

  return <Outlet />;
}
