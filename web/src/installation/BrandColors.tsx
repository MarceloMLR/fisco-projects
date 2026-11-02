import GlobalStyles from '@mui/material/GlobalStyles';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { installationQuery } from '../api/queries';
import { brandStyleSheet, isDefaultChoice } from '../theme/brandColors';

/**
 * The colors of the installation on every screen, the sign-in page included (ADR-0082, ADR-0146): the tokens of both
 * themes, derived from them. Without a color of its own, the design system's stay as they are.
 */
export function BrandColors() {
  const { data } = useQuery(installationQuery());
  const main = data?.mainColor ?? null;
  const secondary = data?.secondaryColor ?? null;
  const sidebar = data?.sidebarColor ?? null;
  const styles = useMemo(() => {
    const colors = { main, secondary, sidebar };
    return isDefaultChoice(colors) ? null : brandStyleSheet(colors);
  }, [main, secondary, sidebar]);
  return styles === null ? null : <GlobalStyles styles={styles} />;
}
