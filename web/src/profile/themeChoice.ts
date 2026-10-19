import BrightnessAutoOutlined from '@mui/icons-material/BrightnessAutoOutlined';
import DarkModeOutlined from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlined from '@mui/icons-material/LightModeOutlined';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type ThemePreference, themePreferences } from '../api/client';
import { useSession } from '../session/SessionContext';

const icons = {
  Light: LightModeOutlined,
  Dark: DarkModeOutlined,
  Automatic: BrightnessAutoOutlined,
} as const;

/** The themes the person can choose, with their names and icons, in the order of the screens (ADR-0014). */
export function useThemeOptions() {
  const { t } = useTranslation();
  return themePreferences.map((value) => ({
    value,
    label: t(`profile.themes.${value}`),
    icon: icons[value],
  }));
}

/**
 * The theme in the profile of the person in the session and how to change it. A refusal of the API leaves the
 * theme as it was and a message to show.
 */
export function useThemeChoice() {
  const { t } = useTranslation();
  const { session, chooseTheme } = useSession();
  const [message, setMessage] = useState<string>();
  const theme: ThemePreference = session.status === 'signed-in' ? session.user.theme : 'Automatic';

  const choose = useCallback(
    (chosen: ThemePreference) => {
      if (chosen === theme) {
        return;
      }
      chooseTheme(chosen).catch(() => {
        setMessage(t('profile.themeFailed'));
      });
    },
    [theme, chooseTheme, t],
  );

  const dismiss = useCallback(() => {
    setMessage(undefined);
  }, []);

  return { theme, choose, message, dismiss };
}
