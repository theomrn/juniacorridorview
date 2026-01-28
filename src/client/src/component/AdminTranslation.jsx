import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import {getLanguages, getEntiereTranslationsByNameSpace , updateTranslation} from "../api/AxiosTranslation";
import { FaLanguage } from "react-icons/fa";

export default function AdminTranslation() {
  const history = useHistory();
  const [selectedLanguage, setSelectedLanguage] = useState(null);
  const [languages, setLanguages] = useState([]);
  const [namespaces] = useState(['home', 'navbar', 'contact', 'navigation']);
  const [translations, setTranslations] = useState({});
  const [translationIds, setTranslationIds] = useState({});
  const [loading, setLoading] = useState(false);
  const [savingNamespace, setSavingNamespace] = useState(null);

  useEffect(() => {
    const fetchLanguages = async () => {
      const data = await getLanguages();
      setLanguages(data);
      if (data.length > 0) {
        setSelectedLanguage(data[0].id_language);
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
      setLoading(false);
    };
    
    fetchAllTranslations();
  }, [selectedLanguage]);

  const handleTranslationChange = (namespace, key, value) => {
    setTranslations({
      ...translations,
      [namespace]: {
        ...translations[namespace],
        [key]: value,
      }
    });
  };

  const handleSaveNamespace = async (namespace) => {
    setSavingNamespace(namespace);
    try {
      const nsTranslations = translations[namespace] || {};
      const nsIds = translationIds[namespace] || {};
      console.log('Saving translations for namespace:', namespace, nsTranslations, nsIds);
      console.log('translationsIds:', translationIds);
      for (const [key, value] of Object.entries(nsTranslations)) {
        const id = nsIds[key];
        // console.log(`Updating translation - Key: ${key}, ID: ${id}, Value: ${value}`);
        if (id) {
          console.log(`Updating translation ID ${id} with value "${value}"`);
          await updateTranslation(id, value);
        }
      }
      alert(`Namespace "${namespace}" sauvegardé avec succès !`);
    } catch (error) {
      console.error(`Erreur lors de la sauvegarde du namespace "${namespace}":`, error);
      alert(`Erreur lors de la sauvegarde du namespace "${namespace}"`);
    } finally {
      setSavingNamespace(null);
    }
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold" style={{ color: '#3c2c53' }}>Gestion des Traductions</h1>
        <button
          onClick={() => history.push('/admin/language')}
          className="px-4 py-2 font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white', borderRadius: '0.5rem' }}
        >
          <FaLanguage /> Gérer les langues
        </button>
      </div>

      <div className="mb-6">
        <label className="block text-sm font-medium mb-2">Sélectionner la langue:</label>
        <select
          value={selectedLanguage || ''}
          onChange={(e) => setSelectedLanguage(e.target.value)}
          className="px-4 py-2 border rounded-lg"
        >
          {languages.map((language) => (
            <option key={language.id_language} value={language.id_language}>
              {language.name_language}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="text-center py-6">Chargement des traductions...</div>
      ) : (
        <>
          {namespaces.map((namespace) => (
            Object.keys(translations[namespace] || {}).length > 0 && (
              <div key={namespace} className="mb-8">
                <div className="flex justify-between items-center mb-4">
                  <h2 className="text-xl font-semibold capitalize">{namespace}</h2>
                  <button
                    onClick={() => handleSaveNamespace(namespace)}
                    disabled={savingNamespace === namespace}
                    className="px-4 py-2 bg-blue-600 text-black rounded-lg hover:bg-blue-700 disabled:bg-gray-400"
                  >
                    {savingNamespace === namespace ? 'Sauvegarde...' : 'Sauvegarder'}
                  </button>
                </div>
                <table className="w-full border-collapse border border-gray-300">
                  <thead>
                    <tr className="bg-gray-100">
                      <th className="border border-gray-300 p-3 text-left">Clé</th>
                      <th className="border border-gray-300 p-3 text-left">Valeur</th>
                    </tr>
                  </thead>
                  <tbody>
                    {Object.entries(translations[namespace] || {}).map(([key, value]) => (
                      <tr key={key}>
                        <td className="border border-gray-300 p-3 font-medium">{key}</td>
                        <td className="border border-gray-300 p-3">
                          <input
                            type="text"
                            value={value}
                            onChange={(e) => handleTranslationChange(namespace, key, e.target.value)}
                            className="w-full px-3 py-2 border rounded"
                            placeholder="Entrer le texte"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          ))}
        </>
      )}
    </div>
  );
}
