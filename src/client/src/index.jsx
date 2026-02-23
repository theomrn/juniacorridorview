import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import axios from 'axios';
import firebase from "firebase/compat/app";
import "firebase/compat/auth";

import App from './App';
import { initI18n } from './i18n';
import './output.css';
import './App.css';

// Attach Firebase ID token to every outgoing request when a user is signed in
axios.interceptors.request.use(async (config) => {
  const user = firebase.auth().currentUser;
  if (user) {
    const token = await user.getIdToken();
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

async function bootstrap() {
  await initI18n();

  ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </React.StrictMode>
  );
}

bootstrap();
