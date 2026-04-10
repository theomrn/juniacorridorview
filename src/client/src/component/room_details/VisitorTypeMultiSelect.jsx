import React, { useState, useRef, useEffect } from 'react';

/**
 * Dropdown with checkboxes for selecting multiple visitor types.
 * Props:
 *   visitorTypes  — array of { id_visitor_type, name_visitor_type }
 *   selected      — array of id_visitor_type (numbers/strings)
 *   onChange      — (newSelected: array) => void
 *   allVisitorsLabel — string label for the "all visitors" option
 */
const VisitorTypeMultiSelect = ({ visitorTypes, selected, onChange, allVisitorsLabel }) => {
  const [open, setOpen] = useState(false);
  const containerRef = useRef(null);

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggle = (id) => {
    const strId = String(id);
    if (selected.map(String).includes(strId)) {
      onChange(selected.filter((s) => String(s) !== strId));
    } else {
      onChange([...selected, id]);
    }
  };

  const getLabel = () => {
    if (selected.length === 0) return allVisitorsLabel;
    if (selected.length === 1) {
      const vt = visitorTypes.find((v) => String(v.id_visitor_type) === String(selected[0]));
      return vt ? vt.name_visitor_type : allVisitorsLabel;
    }
    return `${selected.length} types`;
  };

  return (
    <div ref={containerRef} className="relative w-full">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="w-full p-2 rounded orange-border text-left bg-white flex justify-between items-center"
      >
        <span className="truncate">{getLabel()}</span>
        <span className="ml-2 text-xs">{open ? '▲' : '▼'}</span>
      </button>

      {open && (
        <div className="absolute z-50 w-full bg-white border border-junia-orange rounded mt-1 shadow-lg max-h-48 overflow-y-auto">
          <label className="flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer">
            <input
              type="checkbox"
              checked={selected.length === 0}
              onChange={() => onChange([])}
              className="accent-junia-orange"
            />
            <span>{allVisitorsLabel}</span>
          </label>
          {visitorTypes.map((vt) => (
            <label
              key={vt.id_visitor_type}
              className="flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer"
            >
              <input
                type="checkbox"
                checked={selected.map(String).includes(String(vt.id_visitor_type))}
                onChange={() => toggle(vt.id_visitor_type)}
                className="accent-junia-orange"
              />
              <span>{vt.name_visitor_type}</span>
            </label>
          ))}
        </div>
      )}
    </div>
  );
};

export default VisitorTypeMultiSelect;
