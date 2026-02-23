import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { getVisitorTypes, createVisitorType, updateVisitorType, deleteVisitorType } from "../api/AxiosVisitor";
import { FaChevronDown, FaChevronUp, FaPen, FaTrash, FaPlus, FaArrowLeft } from "react-icons/fa";
import { toast } from "sonner";
import '../style/AdminVisitor.css';

export default function AdminVisitor() {
  const history = useHistory();
  const { t } = useTranslation('adminVisitor');
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
      toast.error(t('errorLoading'));
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
      toast.error(t('nameRequired'));
      return;
    }

    try {
      await updateVisitorType(id_visitor_type, editFormData.name);
      toast.success(t('updatedSuccess'));
      fetchVisitorTypes();
      setEditingId(null);
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(t('error'));
    }
  };

  const handleDelete = async (id_visitor_type, name) => {
    if (window.confirm(t('confirmDelete', { name }))) {
      try {
        await deleteVisitorType(id_visitor_type);
        toast.success(t('deletedSuccess'));
        fetchVisitorTypes();
      } catch (error) {
        console.error('Erreur:', error);
        toast.error(`${t('errorDeleting')} ${error.message}`);
      }
    }
  };

  const handleAddNewVisitorType = async (e) => {
    e.preventDefault();

    if (!newVisitorTypeName.trim()) {
      toast.error(t('nameRequired'));
      return;
    }

    try {
      await createVisitorType(newVisitorTypeName);
      toast.success(t('createdSuccess'));
      setNewVisitorTypeName('');
      setShowNewVisitorTypeForm(false);
      fetchVisitorTypes();
    } catch (error) {
      console.error('Erreur:', error);
      toast.error(t('error'));
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
          <FaArrowLeft /> {t('back')}
        </button>
      </div>
      <div className="admin-visitor-header">
        <button
          onClick={() => setShowNewVisitorTypeForm(!showNewVisitorTypeForm)}
          className="button-type font-title font-bold flex items-center gap-2"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaPlus /> {t('addVisitorType')}
        </button>
      </div>

      {showNewVisitorTypeForm && (
        <div className="admin-visitor-new-form">
          <form onSubmit={handleAddNewVisitorType}>
            <div className="form-group">
              <label className="font-title font-semibold">{t('nameLabel')}</label>
              <input
                type="text"
                value={newVisitorTypeName}
                onChange={(e) => setNewVisitorTypeName(e.target.value)}
                placeholder={t('namePlaceholder')}
                className="form-input"
              />
            </div>
            <div className="form-buttons">
              <button type="submit" className="button-confirm font-title font-bold">
                {t('create')}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowNewVisitorTypeForm(false);
                  setNewVisitorTypeName('');
                }}
                className="button-cancel font-title font-bold"
              >
                {t('cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div style={{ textAlign: 'center', padding: '2rem', fontSize: '1.125rem' }}>
          {t('loading')}
        </div>
      ) : visitorTypes.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>
          <p>{t('notFound')}</p>
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
                        <label className="font-title font-semibold">{t('nameEditLabel')}</label>
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
                          {t('save')}
                        </button>
                        <button
                          type="button"
                          onClick={cancelEdit}
                          className="button-cancel font-title font-bold"
                        >
                          {t('cancel')}
                        </button>
                      </div>
                    </form>
                  ) : (
                    <div className="visitor-actions">
                      <button
                        onClick={() => startEdit(visitorType)}
                        className="action-button edit-button font-title font-bold"
                      >
                        <FaPen /> {t('modify')}
                      </button>
                      <button
                        onClick={() => handleDelete(visitorType.id_visitor_type, visitorType.name_visitor_type)}
                        className="action-button delete-button font-title font-bold"
                      >
                        <FaTrash /> {t('delete')}
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
