import { useTranslation } from 'react-i18next';

// Temporary page, replaced by the screen structure that comes with the design system.
export function App() {
    const { t } = useTranslation();

    return (
        <main>
            <h1>{t('app.name')}</h1>
            <p>{t('app.underConstruction')}</p>
        </main>
    );
}
