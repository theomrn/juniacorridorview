import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { getLanguages, getEntiereTranslationsByNameSpace, updateTranslation } from "../api/AxiosTranslation";
import { FaLanguage, FaChevronDown, FaChevronUp, FaSave, FaArrowLeft, FaUsers } from "react-icons/fa";
import { toast } from "sonner";
import '../style/AdminTranslation.css';

export default function AdminTranslation() {
  const history = useHistory();
  const [selectedLanguage, setSelectedLanguage] = useState(null);
  const [languages, setLanguages] = useState([]);
  const [namespaces] = useState(['home', 'navbar', 'contact', 'navigation']);
  const [translations, setTranslations] = useState({});
  const [translationIds, setTranslationIds] = useState({});
  const [loading, setLoading] = useState(false);
  const [savingNamespace, setSavingNamespace] = useState(null);
  const [expandedNamespace, setExpandedNamespace] = useState(null);

  useEffect(() => {
    const fetchLanguages = async () => {
      try {
        const data = await getLanguages();
        setLanguages(data);
        if (data.length > 0) {
          setSelectedLanguage(data[0].id_language);
        }
      } catch (error) {
        console.error('Erreur lors du chargement des langues:', error);
        toast.error('Erreur lors du chargement des langues');
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
          }
        }

        setTranslations(translationsData);
        setTranslationIds(idsData);
      } catch (error) {
        console.error('Erreur lors du chargement des traductions:', error);
        toast.error('Erreur lors du chargement des traductions');
      } finally {
        setLoading(false);
      }
    };

    fetchAllTranslations();
  }, [selectedLanguage, namespaces]);

  const handleTranslationChange = (namespace, key, value) => {
    setTranslations({
      ...translations,
      [namespace]: {
        ...translations[namespace],
        [key]: value,
      }
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

      for (const [key, value] of Object.entries(nsTranslations)) {
        const id = nsIds[key];
        if (id) {
          await updateTranslation(id, value);
        }
      }
      toast.success(`Namespace "${namespace}" sauvegardé avec succès !`);
    } catch (error) {
      console.error(`Erreur lors de la sauvegarde du namespace "${namespace}":`, error);
      toast.error(`Erreur lors de la sauvegarde du namespace "${namespace}"`);
    } finally {
      setSavingNamespace(null);
    }
  };

  const getTranslationCount = (namespace) => {
    return Object.keys(translations[namespace] || {}).length;
  };

  return (
    <div className="admin-translation-container">
      <div style={{ position: "fixed", left: "20px", top: "80px", zIndex: 40 }}>
        <button
          onClick={() => history.push('/admin/room')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaArrowLeft /> Retour
        </button>
      </div>
      <div className="admin-translation-header">
        <button
          onClick={() => history.push('/admin/language')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaLanguage /> Gérer les langues
        </button>
        <button
          onClick={() => history.push('/admin/visitor')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaUsers /> Types de visiteurs
        </button>
      </div>

      <div className="language-selector">
        <label className="font-title font-semibold">Sélectionner la langue :</label>
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

      {loading ? (
        <div className="translation-loader">
          <div className="spinner"></div>
          <span className="font-texts">Chargement des traductions...</span>
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
                      {translationCount} traduction{translationCount > 1 ? 's' : ''}
                    </span>
                  </div>
                  <span className="chevron-icon">
                    {expandedNamespace === namespace ? (
                      <FaChevronUp />
                    ) : (
                      <FaChevronDown />
                    )}
                  </span>
                </div>

                {expandedNamespace === namespace && (
                  <div className="namespace-content">
                    <table className="translation-table">
                      <thead>
                        <tr>
                          <th className="font-title" style={{ width: '35%' }}>Clé</th>
                          <th className="font-title">Valeur</th>
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
                                placeholder="Entrer le texte"
                              />
                            </td>
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
                        {savingNamespace === namespace ? 'Sauvegarde...' : 'Sauvegarder'}
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
