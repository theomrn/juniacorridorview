import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { getLanguages, getEntiereTranslationsByNameSpace, updateTranslation, insertTranslation, autoTranslate } from "../api/AxiosTranslation";
import { FaLanguage, FaChevronDown, FaChevronUp, FaSave, FaArrowLeft, FaUsers, FaChartBar, FaMagic } from "react-icons/fa";
import { toast } from "sonner";
import '../style/AdminTranslation.css';
import { useTranslation } from 'react-i18next';

const extractText = (entry) =>
  typeof entry === 'object' && entry !== null ? (entry.text || '') : (entry || '');

// Le backend retourne un tableau — on le convertit en map { translation_key → entry }
const toKeyMap = (data) => {
  const map = {};
  Object.values(data).forEach((value) => {
    if (typeof value === 'object' && value !== null && value.translation_key) {
      map[value.translation_key] = value;
    }
  });
  return map;
};

export default function AdminTranslation() {
  const { t } = useTranslation('adminTranslation');
  const history = useHistory();
  const [selectedLanguage, setSelectedLanguage] = useState(null);
  const [englishLangId, setEnglishLangId] = useState(null);
  const [languages, setLanguages] = useState([]);
  const [namespaces] = useState([
    'common', 'home', 'login', 'navbar',
    'adminUser', 'adminTour', 'adminRoom', 'adminBuilding',
    'adminRoomDetails', 'adminTranslation', 'adminLanguage', 'adminVisitor',
    'pano', 'tour',
  ]);
  const [translations, setTranslations] = useState({});
  const [translationIds, setTranslationIds] = useState({});
  const [loading, setLoading] = useState(false);
  const [savingNamespace, setSavingNamespace] = useState(null);
  const [translatingNamespace, setTranslatingNamespace] = useState(null);
  const [translatingKey, setTranslatingKey] = useState(null);
  const [expandedNamespace, setExpandedNamespace] = useState(null);

  useEffect(() => {
    const fetchLanguages = async () => {
      try {
        const data = await getLanguages();
        setLanguages(data);
        if (data.length > 0) {
          setSelectedLanguage(data[0].id_language);
          const en = data.find(l => l.code_language === 'en') ?? data[0];
          setEnglishLangId(en.id_language);
        }
      } catch (error) {
        console.error('Erreur lors du chargement des langues:', error);
        toast.error(t('errorLoadingLanguages'));
      }
    };
    fetchLanguages();
  }, []);

  useEffect(() => {
    const fetchAllTranslations = async () => {
      if (!selectedLanguage) return;
      setLoading(true);
      const translationsData = {};
      const idsData = {};
      try {
        for (const ns of namespaces) {
          const data = await getEntiereTranslationsByNameSpace(ns, selectedLanguage);
          if (Object.keys(data).length > 0) {
            translationsData[ns] = {};
            idsData[ns] = {};
            Object.entries(data).forEach(([key, value]) => {
              if (typeof value === 'object' && value !== null) {
                const translationKey = value.translation_key || key;
                translationsData[ns][translationKey] = value.text || '';
                idsData[ns][translationKey] = value.id_translation;
              } else {
                translationsData[ns][key] = value || '';
              }
            });
          } else if (englishLangId && String(englishLangId) !== String(selectedLanguage)) {
            // Nouvelle langue : charger les clés depuis EN, valeurs vides
            const sourceData = await getEntiereTranslationsByNameSpace(ns, englishLangId);
            if (Object.keys(sourceData).length > 0) {
              translationsData[ns] = {};
              idsData[ns] = {};
              Object.entries(sourceData).forEach(([key, value]) => {
                const translationKey = typeof value === 'object' && value !== null
                  ? (value.translation_key || key)
                  : key;
                translationsData[ns][translationKey] = '';
              });
            }
          }
        }
        setTranslations(translationsData);
        setTranslationIds(idsData);
      } catch (error) {
        console.error('Erreur lors du chargement des traductions:', error);
        toast.error(t('errorLoadingTranslations'));
      } finally {
        setLoading(false);
      }
    };
    fetchAllTranslations();
  }, [selectedLanguage, englishLangId, namespaces]);

  const handleTranslationChange = (namespace, key, value) => {
    setTranslations({
      ...translations,
      [namespace]: { ...translations[namespace], [key]: value },
    });
  };

  const toggleNamespace = (namespace) => {
    setExpandedNamespace(expandedNamespace === namespace ? null : namespace);
  };

  const handleSaveNamespace = async (namespace) => {
    setSavingNamespace(namespace);
    try {
      const nsTranslations = translations[namespace] || {};
      const nsIds = translationIds[namespace] || {};
      const newIds = { ...nsIds };
      for (const [key, value] of Object.entries(nsTranslations)) {
        const id = nsIds[key];
        if (id) {
          await updateTranslation(id, value);
        } else {
          const result = await insertTranslation(selectedLanguage, namespace, key, value);
          if (result?.id) newIds[key] = result.id;
        }
      }
      setTranslationIds({ ...translationIds, [namespace]: newIds });
      toast.success(t('namespaceSavedSuccess', { namespace }));
    } catch (error) {
      toast.error(t('errorSavingNamespace', { namespace }));
    } finally {
      setSavingNamespace(null);
    }
  };

  const handleTranslateKey = async (namespace, key) => {
    if (!englishLangId || String(englishLangId) === String(selectedLanguage)) return;
    setTranslatingKey(`${namespace}.${key}`);
    try {
      const sourceMap = toKeyMap(await getEntiereTranslationsByNameSpace(namespace, englishLangId));
      const sourceText = extractText(sourceMap[key]);
      if (!sourceText) {
        toast.error('Aucun texte source pour cette clé');
        return;
      }
      const result = await autoTranslate(Number(englishLangId), Number(selectedLanguage), [sourceText]);
      if (result?.translations?.[0]) {
        handleTranslationChange(namespace, key, result.translations[0]);
      }
    } catch {
      toast.error('Erreur LibreTranslate — vérifiez l\'URL dans appsettings.json');
    } finally {
      setTranslatingKey(null);
    }
  };

  const handleTranslateNamespace = async (namespace) => {
    if (!englishLangId || String(englishLangId) === String(selectedLanguage)) return;
    setTranslatingNamespace(namespace);
    try {
      const sourceMap = toKeyMap(await getEntiereTranslationsByNameSpace(namespace, englishLangId));
      const emptyKeys = Object.entries(translations[namespace] || {})
        .filter(([, v]) => !v || v.trim() === '')
        .map(([k]) => k);

      if (emptyKeys.length === 0) {
        toast.info('Aucun champ vide dans ce namespace');
        return;
      }

      const texts = emptyKeys.map(k => extractText(sourceMap[k]));
      const result = await autoTranslate(Number(englishLangId), Number(selectedLanguage), texts);
      if (!result?.translations) {
        toast.error('Erreur LibreTranslate — vérifiez l\'URL dans appsettings.json');
        return;
      }

      const updated = { ...translations[namespace] };
      emptyKeys.forEach((k, i) => {
        if (result.translations[i]) updated[k] = result.translations[i];
      });
      setTranslations({ ...translations, [namespace]: updated });
      toast.success(`${result.translations.length} champ(s) traduit(s) — pensez à sauvegarder`);
    } catch {
      toast.error('Erreur LibreTranslate — vérifiez l\'URL dans appsettings.json');
    } finally {
      setTranslatingNamespace(null);
    }
  };

  const getTranslationCount = (namespace) =>
    Object.keys(translations[namespace] || {}).length;

  const canTranslate = englishLangId && selectedLanguage && String(englishLangId) !== String(selectedLanguage);

  return (
    <div className="admin-translation-container">
      <div style={{ position: "fixed", left: "20px", top: "80px", zIndex: 40 }}>
        <button
          onClick={() => history.push('/admin/room')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaArrowLeft /> {t('back')}
        </button>
      </div>
      <div className="admin-translation-header">
        <button
          onClick={() => history.push('/admin/language')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaLanguage /> {t('manageLanguages')}
        </button>
        <button
          onClick={() => history.push('/admin/visitor')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaUsers /> {t('visitorTypes')}
        </button>
        <button
          onClick={() => history.push('/admin/translation-dashboard')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#3c2c53', color: 'white' }}
        >
          <FaChartBar /> Dashboard
        </button>
      </div>

      <div className="language-selector">
        <div className="language-selector-item">
          <label className="font-title font-semibold">{t('selectLanguage')} :</label>
          <select
            value={selectedLanguage || ''}
            onChange={(e) => setSelectedLanguage(e.target.value)}
            className="font-texts"
          >
            {languages.map((language) => (
              <option key={language.id_language} value={language.id_language}>
                {language.name_language}
              </option>
            ))}
          </select>
        </div>
      </div>

      {loading ? (
        <div className="translation-loader">
          <div className="spinner"></div>
          <span className="font-texts">{t('loadingTranslations')}</span>
        </div>
      ) : (
        <div className="admin-translation-list">
          {namespaces.map((namespace) => {
            const translationCount = getTranslationCount(namespace);
            if (translationCount === 0) return null;

            return (
              <div key={namespace} className="namespace-item">
                <div
                  className="namespace-header"
                  onClick={() => toggleNamespace(namespace)}
                >
                  <div className="namespace-info">
                    <span className="namespace-name font-title font-bold">
                      {namespace}
                    </span>
                    <span className="namespace-count font-texts">
                      {translationCount} {translationCount > 1 ? t('translations') : t('translation')}
                    </span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }} onClick={e => e.stopPropagation()}>
                    {canTranslate && (
                      <button
                        onClick={() => handleTranslateNamespace(namespace)}
                        disabled={translatingNamespace === namespace}
                        className="translate-ns-btn font-title font-bold"
                        style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}
                      >
                        <FaMagic />
                        {translatingNamespace === namespace ? 'Traduction…' : 'Traduire les vides'}
                      </button>
                    )}
                    <span className="chevron-icon" onClick={() => toggleNamespace(namespace)}>
                      {expandedNamespace === namespace ? <FaChevronUp /> : <FaChevronDown />}
                    </span>
                  </div>
                </div>

                {expandedNamespace === namespace && (
                  <div className="namespace-content">
                    <table className="translation-table">
                      <thead>
                        <tr>
                          <th className="font-title" style={{ width: '35%' }}>{t('key')}</th>
                          <th className="font-title">{t('value')}</th>
                          {canTranslate && <th style={{ width: '48px' }}></th>}
                        </tr>
                      </thead>
                      <tbody>
                        {Object.entries(translations[namespace] || {}).map(([key, value]) => (
                          <tr key={key}>
                            <td>
                              <span className="translation-key font-texts">{key}</span>
                            </td>
                            <td>
                              <input
                                type="text"
                                value={value}
                                onChange={(e) => handleTranslationChange(namespace, key, e.target.value)}
                                className="translation-input font-texts"
                                placeholder={t('enterText')}
                              />
                            </td>
                            {canTranslate && (
                              <td>
                                <button
                                  className="translate-icon-btn"
                                  title="Traduire automatiquement depuis EN (LibreTranslate)"
                                  disabled={translatingKey === `${namespace}.${key}`}
                                  onClick={() => handleTranslateKey(namespace, key)}
                                >
                                  {translatingKey === `${namespace}.${key}` ? '…' : <FaMagic />}
                                </button>
                              </td>
                            )}
                          </tr>
                        ))}
                      </tbody>
                    </table>

                    <div className="namespace-actions">
                      <button
                        onClick={() => handleSaveNamespace(namespace)}
                        disabled={savingNamespace === namespace}
                        className="save-button font-title font-bold"
                      >
                        <FaSave />
                        {savingNamespace === namespace ? t('saving') : t('save')}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
