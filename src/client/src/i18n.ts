import i18n from 'i18next';
import HttpBackend from 'i18next-http-backend';
import { initReactI18next } from 'react-i18next';
import { getLanguagesId } from './api/AxiosTranslation';
async function loadLanguages(): Promise<string[]> {
  let langs: any[] = [];
    langs = await getLanguagesId();
    
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

      ns: ['common', 'home', 'login', 'navbar', 'adminUser', 'adminTour', 'adminRoom', 'adminBuilding', 'pano', 'tour', 'adminRoomDetails', 'adminTranslation', 'adminLanguage', 'adminVisitor'],
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