import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { getVisitorTypes, createVisitorType, updateVisitorType, deleteVisitorType } from "../api/AxiosVisitor";
import { FaChevronDown, FaChevronUp, FaPen, FaTrash, FaPlus, FaArrowLeft } from "react-icons/fa";
import { toast } from "sonner";
import '../style/AdminVisitor.css';

export default function AdminVisitor() {
  const history = useHistory();
  const [visitorTypes, setVisitorTypes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [expandedId, setExpandedId] = useState(null);
  const [editingId, setEditingId] = useState(null);
  const [newVisitorTypeName, setNewVisitorTypeName] = useState('');
  const [editFormData, setEditFormData] = useState({
    name: ''
  });
  const [showNewVisitorTypeForm, setShowNewVisitorTypeForm] = useState(false);

  useEffect(() => {
    fetchVisitorTypes();
  }, []);

  const fetchVisitorTypes = async () => {
    setLoading(true);
    try {
      const data = await getVisitorTypes();
      setVisitorTypes(data || []);
      console.log(data);
      console.log(visitorTypes);
    } catch (error) {
      console.error('Erreur lors du chargement des types de visiteurs:', error);
      toast.error('Erreur lors du chargement des types de visiteurs');
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

  const startEdit = (visitorType) => {
    setEditingId(visitorType.id_visitor_type);
    setEditFormData({
      name: visitorType.name_visitor_type
    });
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditFormData({
      name: ''
    });
  };

  const handleEditChange = (e) => {
    const { name, value } = e.target;
    setEditFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSaveEdit = async (id_visitor_type) => {
    if (!editFormData.name.trim()) {
      toast.error('Le nom du type de visiteur est requis');
      return;
    }

    try {
      await updateVisitorType(id_visitor_type, editFormData.name);
      toast.success('Type de visiteur mis à jour avec succès!');
      fetchVisitorTypes();
      setEditingId(null);
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(`Erreur: ${error.message || 'Une erreur est survenue'}`);
    }
  };

  const handleDelete = async (id_visitor_type, name) => {
    if (window.confirm(`Êtes-vous sûr de vouloir supprimer le type de visiteur "${name}"?`)) {
      try {
        await deleteVisitorType(id_visitor_type);
        toast.success('Type de visiteur supprimé avec succès!');
        fetchVisitorTypes();
      } catch (error) {
        console.error('Erreur:', error);
        toast.error(`Erreur lors de la suppression: ${error.message}`);
      }
    }
  };

  const handleAddNewVisitorType = async (e) => {
    e.preventDefault();

    if (!newVisitorTypeName.trim()) {
      toast.error('Le nom du type de visiteur est requis');
      return;
    }

    try {
      await createVisitorType(newVisitorTypeName);
      toast.success('Type de visiteur créé avec succès!');
      setNewVisitorTypeName('');
      setShowNewVisitorTypeForm(false);
      fetchVisitorTypes();
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(`Erreur: ${error.message || 'Une erreur est survenue'}`);
    }
  };

  return (
    <div className="admin-visitor-container">
      <div className="admin-visitor-back-button" style={{position: "fixed", left: "20px", top: "80px", zIndex: 40}}>
        <button
          onClick={() => history.push('/admin/translation')}
          className="button-type font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaArrowLeft /> Retour
        </button>
      </div>
      <div className="admin-visitor-header">
        <button
          onClick={() => setShowNewVisitorTypeForm(!showNewVisitorTypeForm)}
          className="button-type font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaPlus /> Ajouter un type de visiteur
        </button>
      </div>

      {showNewVisitorTypeForm && (
        <div className="admin-visitor-new-form">
          <form onSubmit={handleAddNewVisitorType}>
            <div className="form-group">
              <label className="font-title font-semibold">Nom du type de visiteur</label>
              <input
                type="text"
                value={newVisitorTypeName}
                onChange={(e) => setNewVisitorTypeName(e.target.value)}
                placeholder="Ex: Étudiant, Professeur, Visiteur..."
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
                  setShowNewVisitorTypeForm(false);
                  setNewVisitorTypeName('');
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
          Chargement des types de visiteurs...
        </div>
      ) : visitorTypes.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>
          <p>Aucun type de visiteur trouvé. Cliquez sur "Ajouter un type de visiteur" pour en créer un.</p>
        </div>
      ) : (
        <div className="admin-visitor-list">
          {visitorTypes.map((visitorType) => (
            <div key={visitorType.id_visitor_type} className="visitor-item">
              <div
                className="visitor-header"
                onClick={() => toggleExpand(visitorType.id_visitor_type)}
              >
                <div className="visitor-info">
                  <span className="visitor-name font-title font-bold">
                    {visitorType.name_visitor_type}
                  </span>
                </div>
                <span className="chevron-icon">
                  {expandedId === visitorType.id_visitor_type ? (
                    <FaChevronUp />
                  ) : (
                    <FaChevronDown />
                  )}
                </span>
              </div>

              {expandedId === visitorType.id_visitor_type && (
                <div className="visitor-content">
                  {editingId === visitorType.id_visitor_type ? (
                    <form className="edit-form" onSubmit={(e) => {
                      e.preventDefault();
                      handleSaveEdit(visitorType.id_visitor_type);
                    }}>
                      <div className="form-group">
                        <label className="font-title font-semibold">Nom</label>
                        <input
                          type="text"
                          name="name"
                          value={editFormData.name}
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
                    <div className="visitor-actions">
                      <button
                        onClick={() => startEdit(visitorType)}
                        className="action-button edit-button font-title font-bold"
                      >
                        <FaPen /> Modifier
                      </button>
                      <button
                        onClick={() => handleDelete(visitorType.id_visitor_type, visitorType.name_visitor_type)}
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
