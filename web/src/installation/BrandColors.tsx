import GlobalStyles from '@mui/material/GlobalStyles';
import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { installationQuery } from '../api/queries';
import { brandStyleSheet } from '../theme/brandColors';

/**
 * The main and the secondary colors of the installation on every screen, the sign-in page included (ADR-0082,
 * ADR-0100): the brand tokens of both themes, derived from them. Without colors of its own, the design system's stay as
 * they are.
 */
export function BrandColors() {
  const { data } = useQuery(installationQuery());
  const mainColor = data?.mainColor ?? null;
  const secondaryColor = data?.secondaryColor ?? null;
  const styles = useMemo(
    () =>
      mainColor === null && secondaryColor === null
        ? null
        : brandStyleSheet(mainColor, secondaryColor),
    [mainColor, secondaryColor],
  );
  return styles === null ? null : <GlobalStyles styles={styles} />;
}
