import i18n from 'i18next';
import HttpBackend from 'i18next-http-backend';
import { initReactI18next } from 'react-i18next';

i18n
  .use(HttpBackend)
  .use(initReactI18next)
  .init({
    fallbackLng: '3',
    supportedLngs: ['3', '4'],
    ns: ['common', 'home'],
    defaultNS: 'common',

    backend: {
      loadPath: 'http://localhost:5078/api/translations/{{lng}}/{{ns}}'
    },

    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
