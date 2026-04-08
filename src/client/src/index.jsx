import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import axios from 'axios';
import firebase from "firebase/compat/app";
import "firebase/compat/auth";

import App from './App';
import FallbackApp from './component/FallbackApp';
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
  let i18nInitialized = false;
  try {
    await initI18n();
    i18nInitialized = true;
  } catch (error) {
    console.error('Failed to initialize i18n:', error);
  }

  ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
      <BrowserRouter>
        {i18nInitialized ? <App /> : <FallbackApp />}
      </BrowserRouter>
    </React.StrictMode>
  );
}

bootstrap();
