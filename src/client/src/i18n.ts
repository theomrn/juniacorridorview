import i18n from 'i18next';
import HttpBackend from 'i18next-http-backend';
import { initReactI18next } from 'react-i18next';

async function loadLanguages(): Promise<string[]> {
  const response = await fetch('http://localhost:5078/api/translations');

  if (!response.ok) {
    throw new Error('Failed to load languages');
  }

  const data = await response.json();

  let langs: any[] = [];
    langs = data;
    
  const result = langs.map(l => String(l));
  return result;
}

export async function initI18n() {
  const languages = await loadLanguages();

  if (!languages.length) {
    throw new Error('No languages loaded');
  }

  await i18n
    .use(HttpBackend)
    .use(initReactI18next)
    .init({
      supportedLngs: languages,
      lng: languages[0],
      fallbackLng: languages[0],

      ns: ['common', 'home', 'login', 'navbar', 'adminUser', 'adminTour', 'adminRoom', 'adminBuilding', 'pano', 'tour', 'adminRoomDetails'],
      defaultNS: 'common',

      backend: {
        loadPath: 'http://localhost:5078/api/translations/{{lng}}/{{ns}}'
      },

      interpolation: {
        escapeValue: false
      }
    });

  return i18n;
}

export default i18n;