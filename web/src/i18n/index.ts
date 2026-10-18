import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import ptBR from '../locales/pt-BR.json';

// ADR-0013: Portuguese only in v1, with every visible text in the translation files.
export const language = 'pt-BR';

void i18n.use(initReactI18next).init({
  lng: language,
  fallbackLng: language,
  resources: { [language]: { translation: ptBR } },
  initAsync: false,
  interpolation: { escapeValue: false }, // React already escapes the values.
});

export default i18n;
