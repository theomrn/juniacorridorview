import React from 'react';
import ReactDOM from 'react-dom/client'; // <-- Utilisation de React 18
import { BrowserRouter } from 'react-router-dom';

import App from './App';
import './i18n';
import './output.css'; // Vérifie si ce fichier existe bien
import './App.css'
// Utilisation de createRoot pour React 18
ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
);