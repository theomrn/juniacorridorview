import React from 'react';

const FallbackApp = () => {
  return (
    <>
      <nav className="relative justify-between bg-white px-2 shadow-lg">
        <div className="flex items-center">
          <img src="/img/logojunia.png" alt="Logo JUNIA" className="height-60 cursor-pointer"></img>
        </div>
        <div className="flex items-center text-5xl text-junia-purple font-title">
          Erreur de connexion
        </div>
        <div className="flex items-center text-xl text-junia-orange font-title gap-2">
          {/* Empty div to maintain layout */}
        </div>
      </nav>
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: 'calc(100vh - 80px)', // Adjust for navbar height
        backgroundColor: '#f0f0f0',
        fontFamily: 'Arial, sans-serif'
      }}>
        <div style={{
          textAlign: 'center',
          padding: '20px',
          backgroundColor: 'white',
          borderRadius: '8px',
          boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
          maxWidth: '500px'
        }}>
          <h1 style={{ color: '#e74c3c', marginBottom: '16px' }}>Erreur de connexion</h1>
          <p style={{ color: '#555', marginBottom: '16px' }}>
            Impossible de se connecter au serveur. Certaines fonctionnalités peuvent ne pas être disponibles.
          </p>
          <p style={{ color: '#777', fontSize: '14px' }}>
            Veuillez vérifier votre connexion internet et réessayer plus tard.
          </p>
          <button
            onClick={() => window.location.reload()}
            style={{
              marginTop: '16px',
              padding: '10px 20px',
              backgroundColor: '#3498db',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            Réessayer
          </button>
        </div>
      </div>
    </>
  );
};

export default FallbackApp;