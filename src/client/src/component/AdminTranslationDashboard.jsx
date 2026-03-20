import React, { useState, useEffect } from 'react';
import { useHistory } from 'react-router-dom';
import { getTranslationDashboard, getEntiereTranslationsByNameSpace, insertTranslation, autoTranslate } from '../api/AxiosTranslation';
import { FaArrowLeft, FaSync, FaChevronDown, FaChevronUp, FaMagic } from 'react-icons/fa';
import { toast } from 'sonner';
import '../style/AdminTranslation.css';
import '../style/AdminTranslationDashboard.css';

const extractText = (entry) =>
  typeof entry === 'object' && entry !== null ? (entry.text || '') : (entry || '');

const toKeyMap = (data) => {
  const map = {};
  Object.values(data).forEach((value) => {
    if (typeof value === 'object' && value !== null && value.translation_key) {
      map[value.translation_key] = value;
    }
  });
  return map;
};

export default function AdminTranslationDashboard() {
  const history = useHistory();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [expandedNamespaces, setExpandedNamespaces] = useState(new Set());
  const [selectedVisitorType, setSelectedVisitorType] = useState(null);
  const [activeSection, setActiveSection] = useState('i18n');
  const [sourceLangId, setSourceLangId] = useState(null);
  const [translatingNamespace, setTranslatingNamespace] = useState(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const result = await getTranslationDashboard();
      setData(result);
      if (result?.languages?.length > 0) {
        const en = result.languages.find(l => l.name?.toUpperCase() === 'EN');
        setSourceLangId(en?.id ?? result.languages[0].id);
      }
    } catch (e) {
      console.error('Erreur dashboard traductions:', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchData(); }, []);

  const toggleNamespace = (ns) => {
    const next = new Set(expandedNamespaces);
    if (next.has(ns)) next.delete(ns);
    else next.add(ns);
    setExpandedNamespaces(next);
  };

  // Group i18n missing by namespace
  const i18nByNamespace = data?.i18nMissing?.reduce((acc, item) => {
    if (!acc[item.namespace]) acc[item.namespace] = [];
    acc[item.namespace].push(item);
    return acc;
  }, {}) ?? {};

  // Filter infospots based on selected visitor type
  const filteredInfospots = data?.infospots?.filter(spot => {
    if (selectedVisitorType === null) return spot.missingLanguages.length > 0;
    return !spot.translations.some(t => t.visitorTypeId === selectedVisitorType.id);
  }) ?? [];

  const totalI18nMissing = data?.i18nMissing?.length ?? 0;
  const totalInfospotsMissing = data?.infospots?.filter(s => s.missingLanguages.length > 0).length ?? 0;

  const handleTranslateNamespace = async (ns, items) => {
    if (!sourceLangId) return;
    setTranslatingNamespace(ns);
    try {
      const sourceMap = toKeyMap(await getEntiereTranslationsByNameSpace(ns, sourceLangId));

      // Group truly missing keys by target language (exclude source lang)
      const byTargetLang = {};
      for (const item of items) {
        for (const lang of item.missingIn) {
          if (lang.id === sourceLangId) continue;
          if (!byTargetLang[lang.id]) byTargetLang[lang.id] = { langId: lang.id, keys: [] };
          byTargetLang[lang.id].keys.push(item.key);
        }
      }

      let inserted = 0;
      for (const { langId, keys } of Object.values(byTargetLang)) {
        const texts = keys.map(k => extractText(sourceMap[k])).filter(Boolean);
        if (texts.length === 0) continue;

        const result = await autoTranslate(sourceLangId, langId, texts);
        if (!result?.translations) continue;

        for (let i = 0; i < keys.length; i++) {
          if (result.translations[i]) {
            await insertTranslation(langId, ns, keys[i], result.translations[i]);
            inserted++;
          }
        }
      }

      if (inserted > 0) {
        toast.success(`${inserted} traduction(s) insérée(s) dans "${ns}"`);
        fetchData();
      } else {
        toast.info('Aucune traduction à insérer (vérifiez la langue source)');
      }
    } catch (e) {
      toast.error('Erreur LibreTranslate — vérifiez l\'URL dans appsettings.json');
      console.error(e);
    } finally {
      setTranslatingNamespace(null);
    }
  };

  if (loading) {
    return (
      <div className="dashboard-container">
        <div className="translation-loader">
          <div className="spinner"></div>
          <span className="font-texts">Chargement du dashboard...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="dashboard-container">
      {/* Back button */}
      <div style={{ position: 'fixed', left: '20px', top: '80px', zIndex: 40 }}>
        <button
          onClick={() => history.push('/admin/translation')}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#f06b42', color: 'white' }}
        >
          <FaArrowLeft /> Traductions
        </button>
      </div>

      {/* Header */}
      <div className="dashboard-header">
        <h1 className="font-title font-bold">Dashboard traductions</h1>
        <button
          onClick={fetchData}
          className="button-type font-title font-bold"
          style={{ backgroundColor: '#3c2c53', color: 'white' }}
        >
          <FaSync /> Rafraîchir
        </button>
      </div>

      {/* Stats */}
      <div className="dashboard-stats">
        <div className={`stat-card ${totalI18nMissing > 0 ? 'stat-warning' : 'stat-ok'}`}>
          <span className="stat-number font-title font-bold">{totalI18nMissing}</span>
          <span className="stat-label font-texts">
            {totalI18nMissing > 1 ? 'clés i18n incomplètes' : 'clé i18n incomplète'}
          </span>
        </div>
        <div className={`stat-card ${totalInfospotsMissing > 0 ? 'stat-warning' : 'stat-ok'}`}>
          <span className="stat-number font-title font-bold">{totalInfospotsMissing}</span>
          <span className="stat-label font-texts">
            {totalInfospotsMissing > 1 ? 'infobulles incomplètes' : 'infobulle incomplète'}
          </span>
        </div>
      </div>

      {/* Tabs */}
      <div className="section-tabs">
        <button
          className={`tab-btn font-title font-bold ${activeSection === 'i18n' ? 'tab-active' : ''}`}
          onClick={() => setActiveSection('i18n')}
        >
          Traductions i18n
          {totalI18nMissing > 0 && <span className="tab-badge">{totalI18nMissing}</span>}
        </button>
        <button
          className={`tab-btn font-title font-bold ${activeSection === 'infospots' ? 'tab-active' : ''}`}
          onClick={() => setActiveSection('infospots')}
        >
          Infobulles
          {totalInfospotsMissing > 0 && <span className="tab-badge">{totalInfospotsMissing}</span>}
        </button>
      </div>

      {/* i18n section */}
      {activeSection === 'i18n' && (
        <div className="dashboard-section">
          {Object.keys(i18nByNamespace).length === 0 ? (
            <div className="all-good-message font-texts">
              Toutes les traductions i18n sont complètes.
            </div>
          ) : (
            <div className="admin-translation-list">
              {Object.entries(i18nByNamespace).map(([ns, items]) => (
                <div key={ns} className="namespace-item">
                  <div className="namespace-header" onClick={() => toggleNamespace(ns)}>
                    <div className="namespace-info">
                      <span className="namespace-name font-title font-bold">{ns}</span>
                      <span className="missing-count-badge font-texts">
                        {items.length} clé{items.length > 1 ? 's' : ''} incomplète{items.length > 1 ? 's' : ''}
                      </span>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }} onClick={e => e.stopPropagation()}>
                      {sourceLangId && (
                        <button
                          className="translate-ns-btn font-title font-bold"
                          style={{ padding: '0.4rem 0.9rem', fontSize: '0.85rem' }}
                          disabled={translatingNamespace === ns}
                          onClick={() => handleTranslateNamespace(ns, items)}
                        >
                          <FaMagic />
                          {translatingNamespace === ns ? 'Traduction…' : 'Auto-traduire'}
                        </button>
                      )}
                      <span className="chevron-icon" onClick={() => toggleNamespace(ns)}>
                        {expandedNamespaces.has(ns) ? <FaChevronUp /> : <FaChevronDown />}
                      </span>
                    </div>
                  </div>

                  {expandedNamespaces.has(ns) && (
                    <div className="namespace-content">
                      <table className="translation-table">
                        <thead>
                          <tr>
                            <th className="font-title" style={{ width: '40%' }}>Clé</th>
                            {data.languages.map(lang => (
                              <th key={lang.id} className="font-title">{lang.name}</th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {items.map(item => (
                            <tr key={item.key}>
                              <td>
                                <span className="translation-key font-texts">{item.key}</span>
                              </td>
                              {data.languages.map(lang => {
                                const isMissing = item.missingIn.some(l => l.id === lang.id);
                                const isEmpty = item.emptyIn.some(l => l.id === lang.id);
                                return (
                                  <td key={lang.id} className="coverage-cell">
                                    {isMissing ? (
                                      <span className="badge badge-missing font-texts">Absent</span>
                                    ) : isEmpty ? (
                                      <span className="badge badge-empty font-texts">Vide</span>
                                    ) : (
                                      <span className="badge badge-ok font-texts">OK</span>
                                    )}
                                  </td>
                                );
                              })}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Infospots section */}
      {activeSection === 'infospots' && (
        <div className="dashboard-section">
          <div className="infospot-filters">
            <label className="font-title font-semibold">Filtrer par type de visiteur :</label>
            <div className="vt-filter-chips">
              <button
                className={`vt-chip font-texts ${selectedVisitorType === null ? 'vt-chip-active' : ''}`}
                onClick={() => setSelectedVisitorType(null)}
              >
                Infobulles incomplètes (langues)
              </button>
              {data?.visitorTypes?.map(vt => (
                <button
                  key={vt.id}
                  className={`vt-chip font-texts ${selectedVisitorType?.id === vt.id ? 'vt-chip-active' : ''}`}
                  onClick={() => setSelectedVisitorType(selectedVisitorType?.id === vt.id ? null : vt)}
                >
                  {vt.name}
                </button>
              ))}
            </div>
          </div>

          {filteredInfospots.length === 0 ? (
            <div className="all-good-message font-texts">
              {selectedVisitorType
                ? `Toutes les infobulles ont une traduction pour "${selectedVisitorType.name}".`
                : 'Toutes les infobulles sont complètes dans toutes les langues.'}
            </div>
          ) : (
            <div className="infospots-grid">
              {filteredInfospots.map(spot => (
                <div key={spot.id} className="infospot-card">
                  <div className="infospot-card-header">
                    <div>
                      <span className="infospot-id font-texts">#{spot.id}</span>
                      <span className="infospot-room font-title font-bold">
                        {spot.roomName}{spot.roomNumber ? ` — ${spot.roomNumber}` : ''}
                      </span>
                    </div>
                    {spot.roomId && (
                      <button
                        className="button-type font-texts"
                        style={{ backgroundColor: '#f06b42', color: 'white', fontSize: '0.8rem', padding: '0.3rem 0.75rem' }}
                        onClick={() => history.push(`/admin/room/${spot.roomId}`)}
                      >
                        Voir →
                      </button>
                    )}
                  </div>

                  <div className="coverage-row">
                    <span className="font-texts coverage-label">Langues :</span>
                    <div className="coverage-badges">
                      {data.languages.map(lang => {
                        const isMissing = spot.missingLanguages.some(l => l.id === lang.id);
                        return (
                          <span
                            key={lang.id}
                            className={`badge ${isMissing ? 'badge-missing' : 'badge-ok'} font-texts`}
                          >
                            {lang.name}
                          </span>
                        );
                      })}
                    </div>
                  </div>

                  {data.visitorTypes.length > 0 && (
                    <div className="coverage-row">
                      <span className="font-texts coverage-label">Types visiteur :</span>
                      <div className="coverage-badges">
                        {spot.translations.some(t => t.visitorTypeId === null) && (
                          <span className="badge badge-ok font-texts">Général</span>
                        )}
                        {data.visitorTypes.map(vt => {
                          const covered = spot.translations.some(t => t.visitorTypeId === vt.id);
                          return (
                            <span
                              key={vt.id}
                              className={`badge ${covered ? 'badge-ok' : 'badge-absent'} font-texts`}
                            >
                              {vt.name}
                            </span>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
