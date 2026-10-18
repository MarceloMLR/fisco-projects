import { useTranslation } from 'react-i18next';

// Temporary page until the screen structure arrives with the design system (Fundação-5).
export function App() {
    const { t } = useTranslation();

    return (
        <main>
            <h1>{t('app.name')}</h1>
            <p>{t('app.underConstruction')}</p>
        </main>
    );
}
