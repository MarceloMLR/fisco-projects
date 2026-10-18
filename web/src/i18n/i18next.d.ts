import type ptBR from '../locales/pt-BR.json';

// Translation keys are typed from pt-BR.json: a key that does not exist is a type error.
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof ptBR };
  }
}
