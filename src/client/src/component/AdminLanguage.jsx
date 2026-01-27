import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { createLanguage, updateLanguage, deleteLanguage } from "../api/AxiosLanguage";
import { getLanguages } from "../api/AxiosTranslation";
import { FaChevronDown, FaChevronUp, FaPen, FaTrash, FaPlus, FaArrowLeft } from "react-icons/fa";
import { toast } from "sonner";
import '../style/AdminLanguage.css';

export default function AdminLanguage() {
  const history = useHistory();
  const [languages, setLanguages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [expandedId, setExpandedId] = useState(null);
  const [editingId, setEditingId] = useState(null);
  const [newLanguageName, setNewLanguageName] = useState('');
  const [editFormData, setEditFormData] = useState({
    name_language: ''
  });
  const [showNewLanguageForm, setShowNewLanguageForm] = useState(false);

  useEffect(() => {
    fetchLanguages();
  }, []);

  const fetchLanguages = async () => {
    setLoading(true);
    try {
      const data = await getLanguages();
      setLanguages(data || []);
    } catch (error) {
      console.error('Erreur lors du chargement des langues:', error);
      toast.error('Erreur lors du chargement des langues');
    } finally {
      setLoading(false);
    }
  };

  const toggleExpand = (id) => {
    if (expandedId === id) {
      setExpandedId(null);
      setEditingId(null);
    } else {
      setExpandedId(id);
      setEditingId(null);
    }
  };

  const startEdit = (language) => {
    setEditingId(language.id_language);
    setEditFormData({
      name_language: language.name_language
    });
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditFormData({
      name_language: ''
    });
  };

  const handleEditChange = (e) => {
    const { name, value } = e.target;
    setEditFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSaveEdit = async (id_language) => {
    if (!editFormData.name_language.trim()) {
      toast.error('Le nom de la langue est requis');
      return;
    }

    try {
      await updateLanguage(id_language, editFormData.name_language);
      toast.success('Langue mise à jour avec succès!');
      fetchLanguages();
      setEditingId(null);
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(`Erreur: ${error.message || 'Une erreur est survenue'}`);
    }
  };

  const handleDelete = async (id_language, name) => {
    if (window.confirm(`Êtes-vous sûr de vouloir supprimer la langue "${name}"?`)) {
      try {
        await deleteLanguage(id_language);
        toast.success('Langue supprimée avec succès!');
        fetchLanguages();
      } catch (error) {
        console.error('Erreur:', error);
        toast.error(`Erreur lors de la suppression: ${error.message}`);
      }
    }
  };

  const handleAddNewLanguage = async (e) => {
    e.preventDefault();
    
    if (!newLanguageName.trim()) {
      toast.error('Le nom de la langue est requis');
      return;
    }

    try {
      await createLanguage(newLanguageName);
      toast.success('Langue créée avec succès!');
      setNewLanguageName('');
      setShowNewLanguageForm(false);
      fetchLanguages();
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(`Erreur: ${error.message || 'Une erreur est survenue'}`);
    }
  };

  return (
    <div className="admin-language-container">
      <div className="admin-language-back-button" style={{position: "fixed", left: "20px", top: "80px", zIndex: 40}}>
        <button
          onClick={() => history.push('/admin/room')}
          className="button-type font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaArrowLeft /> Retour
        </button>
      </div>
      <div className="admin-language-header">
        <button
          onClick={() => setShowNewLanguageForm(!showNewLanguageForm)}
          className="button-type font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaPlus /> Ajouter une langue
        </button>
      </div>

      {/* Formulaire d'ajout rapide */}
      {showNewLanguageForm && (
        <div className="admin-language-new-form">
          <form onSubmit={handleAddNewLanguage}>
            <div className="form-group">
              <label className="font-title font-semibold">Nom de la langue</label>
              <input
                type="text"
                value={newLanguageName}
                onChange={(e) => setNewLanguageName(e.target.value)}
                placeholder="Ex: Français"
                className="form-input"
              />
            </div>
            <div className="form-buttons">
              <button type="submit" className="button-confirm font-title font-bold">
                Créer
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowNewLanguageForm(false);
                  setNewLanguageName('');
                }}
                className="button-cancel font-title font-bold"
              >
                Annuler
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div style={{ textAlign: 'center', padding: '2rem', fontSize: '1.125rem' }}>
          Chargement des langues...
        </div>
      ) : languages.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>
          <p>Aucune langue trouvée. Cliquez sur "Ajouter une langue" pour en créer une.</p>
        </div>
      ) : (
        <div className="admin-language-list">
          {languages.map((language) => (
            <div key={language.id_language} className="language-item">
              <div
                className="language-header"
                onClick={() => toggleExpand(language.id_language)}
              >
                <div className="language-info">
                  <span className="language-name font-title font-bold">
                    {language.name_language}
                  </span>
                </div>
                <span className="chevron-icon">
                  {expandedId === language.id_language ? (
                    <FaChevronUp />
                  ) : (
                    <FaChevronDown />
                  )}
                </span>
              </div>

              {expandedId === language.id_language && (
                <div className="language-content">
                  {editingId === language.id_language ? (
                    <form className="edit-form" onSubmit={(e) => {
                      e.preventDefault();
                      handleSaveEdit(language.id_language);
                    }}>
                      <div className="form-group">
                        <label className="font-title font-semibold">Nom</label>
                        <input
                          type="text"
                          name="name_language"
                          value={editFormData.name_language}
                          onChange={handleEditChange}
                          className="form-input"
                        />
                      </div>
                      <div className="form-buttons">
                        <button type="submit" className="button-confirm font-title font-bold">
                          Enregistrer
                        </button>
                        <button
                          type="button"
                          onClick={cancelEdit}
                          className="button-cancel font-title font-bold"
                        >
                          Annuler
                        </button>
                      </div>
                    </form>
                  ) : (
                    <div className="language-actions">
                      <button
                        onClick={() => startEdit(language)}
                        className="action-button edit-button font-title font-bold"
                      >
                        <FaPen /> Modifier
                      </button>
                      <button
                        onClick={() => handleDelete(language.id_language, language.name_language)}
                        className="action-button delete-button font-title font-bold"
                      >
                        <FaTrash /> Supprimer
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
