import GlobalStyles from '@mui/material/GlobalStyles';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { installationQuery } from '../api/queries';
import { brandStyleSheet } from '../theme/brandColors';

/**
 * The main color of the installation on every screen, the sign-in page included (ADR-0082): the brand tokens of both
 * themes, derived from it. Without a color of its own, the design system's stay as they are.
 */
export function BrandColors() {
  const mainColor = useQuery(installationQuery()).data?.mainColor ?? null;
  const styles = useMemo(
    () => (mainColor === null ? null : brandStyleSheet(mainColor)),
    [mainColor],
  );
  return styles === null ? null : <GlobalStyles styles={styles} />;
}
