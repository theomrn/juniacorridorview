# Junia Corridor View

Application web de visite virtuelle des couloirs et salles de Junia. Elle permet aux visiteurs d'explorer des panoramas 360°, et aux administrateurs de gérer les salles, bâtiments, visites guidées, et traductions.

## Stack

| Couche | Technologie |
|--------|-------------|
| Frontend | React 19 + Vite, React Router DOM v5, Tailwind CSS, i18next |
| Backend | ASP.NET Core (.NET 10), Swagger/OpenAPI |
| Base de données | MySQL 8.4 |
| Traduction auto | LibreTranslate (self-hosted) |
| Auth | Firebase (frontend) + Firebase Admin SDK (backend) |
| Panorama | Panolens 0.12.1 + Three.js |

## Lancement

### Production — Docker complet (livrable client)

Tout tourne dans des conteneurs (MySQL, LibreTranslate, API .NET, client React via nginx).
Les données et images sont pré-chargées automatiquement au premier démarrage.

**Prérequis :** Docker Desktop installé et démarré.

```bash
# Configurer Firebase (une seule fois)
cp src/client/src/firebaseConfig.js.example src/client/src/firebaseConfig.js
# → remplir les credentials Firebase dans le fichier

# Lancer la stack complète avec données pré-chargées
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build
```

Accès :
- Application → http://localhost:80
- API .NET → http://localhost:5078

> La base de données est initialisée automatiquement au premier démarrage via les scripts SQL montés dans `docker-entrypoint-initdb.d` :
> - `db_script_empty.sql` — schéma
> - `translations_script.sql` — traductions
> - `inserts_buildings.sql` — données (bâtiments, salles, images, tours) — via `docker-compose.prod.yml`
>
> Les images sont servies directement depuis `seed/images/` et `seed/previews/` via bind mounts.
>
> **Important :** si le volume `db_data` existe déjà (relance), les scripts d'init ne s'exécutent pas à nouveau. Pour repartir de zéro : `docker compose down -v` avant le démarrage.

---

### Développement — local + services Docker

Le client React et l'API .NET tournent en local (hot reload). MySQL et LibreTranslate tournent en Docker.

**Prérequis :** Docker Desktop, Node.js ≥ 16, .NET 10 SDK.

```bash
# 1. Configurer Firebase (une seule fois)
cp src/client/src/firebaseConfig.js.example src/client/src/firebaseConfig.js

# 2. Installer les dépendances Node
npm install

# 3. Démarrer les services d'infrastructure
docker compose up db libretranslate
# → MySQL exposé sur localhost:3306
# → LibreTranslate exposé sur localhost:5000

# 4. Dans un autre terminal, lancer le frontend + backend
npm run dev
```

Accès :
- Client React → http://localhost:5173
- API .NET → http://localhost:5078
- Swagger → http://localhost:5078/swagger

> `docker-compose.override.yml` expose automatiquement les ports 3306 et 5000 à l'hôte en dev.

---

## Configuration Firebase

1. Copier `src/client/src/firebaseConfig.js.example` → `src/client/src/firebaseConfig.js`
2. Remplir les credentials du projet Firebase
3. Dans la console Firebase, activer l'authentification par email/mot de passe
4. Le backend utilise Firebase Admin SDK — le `ProjectId` est configuré dans `appsettings.json` (valeur par défaut : `yp-2425-10`)

## Structure du projet

```
├── src/
│   ├── client/              # Frontend React + Vite
│   │   └── src/
│   │       ├── component/   # Composants pages et UI
│   │       ├── api/         # Fichiers Axios par domaine
│   │       └── i18n.ts      # Initialisation i18next
│   └── DataBaseApi/         # Backend ASP.NET Core
│       ├── Controllers/     # ApiController + TranslationController
│       ├── Services/        # DatabaseService, FileOptimiserService
│       └── Models/          # DTOs
├── docker/                  # Dockerfiles et config nginx/MySQL
├── docker-compose.yml           # Stack de base (schéma vide)
├── docker-compose.prod.yml      # Surcharge prod : monte inserts_buildings.sql
├── docker-compose.override.yml  # Surcharge dev (ports exposés)
├── db_script_empty.sql          # Schéma initial de la base
├── translations_script.sql      # Données de traduction initiales
├── inserts_buildings.sql        # Données migrées (généré par ImportToNewApp)
└── seed/
    ├── images/                  # Images panoramiques (bind mount → wwwroot/images)
    └── previews/                # Plans d'étages + previews salles (bind mount → wwwroot/previews)
```

## Fonctionnalités

### Côté visiteur
- **Page d'accueil** : choix entre visite guidée et visite libre
- **Panorama 360°** : visionneuse immersive avec hotspots d'information, navigation entre salles
- **Plan de bâtiment** : plans interactifs par étage

### Côté administration
- **Salles** : création, édition, suppression, upload d'images panoramiques, placement sur le plan
- **Bâtiments & étages** : gestion de la hiérarchie bâtiment → étage → salle
- **Visites guidées** : création et ordonnancement des étapes de visite
- **Traductions** : gestion multilingue des contenus (stockées en base, pas dans des fichiers locaux)
- **Langues** : ajout de nouvelles langues avec traduction automatique via LibreTranslate
- **Utilisateurs** : gestion des comptes administrateurs

## Génération du livrable (migration depuis l'ancienne base)

Les outils de migration sont dans `../DB/` (hors du repo juniacorridorview).

### Prérequis

- Ancienne base MySQL Aiven accessible
- Docker Desktop démarré
- .NET SDK installé

### Étapes

```bash
# 1. Extraire les images de l'ancienne base
cd ../DB/ExtractDataBasePicture
dotnet run
# → génère DB/Pictures/{Pictures, Plans, RoomPreview, InfoPopup}/

# 2. Démarrer une DB vide + l'API (sans données)
cd ../juniacorridorview
docker compose down -v
docker compose up db api -d

# 3. Migrer via l'API
cd ../DB/ImportToNewApp
dotnet run
# → peuple la DB via l'API
# → génère juniacorridorview/seed/images/
# → génère juniacorridorview/seed/previews/
# → génère juniacorridorview/inserts_buildings.sql

# 4. Stopper (le livrable est prêt)
cd ../juniacorridorview
docker compose down -v
```

### Livrer au client

Le dossier `juniacorridorview/` est le livrable. Le client fait :

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build
```

---

## Traduction automatique

Déclenchée depuis `AdminTranslation.jsx`, traitée côté backend via `LibreTranslateService`.

**Flux :**
1. L'admin sélectionne une langue cible dans l'interface de traduction
2. Deux modes de déclenchement :
   - **Clé unique** : bouton icône sur une ligne → traduit ce seul texte depuis l'anglais
   - **Masse** : bouton "Traduire les vides" sur un namespace → traduit toutes les clés vides d'un bloc
3. Le backend appelle `POST http://libretranslate:5000/translate` (ou `http://localhost:5000` en dev)
4. Les traductions sont affichées localement dans le formulaire — l'admin valide avant de sauvegarder

**Endpoint :** `POST /api/translations/auto-translate`
```json
{ "sourceLangId": 1, "targetLangId": 2, "texts": ["Hello", "Room"] }
```

**Configuration (`appsettings.json`) :**
```json
"LibreTranslate": {
  "Url": "http://libretranslate:5000/translate",
  "ApiKey": ""
}
```
La langue source doit exister en base. Si LibreTranslate n'est pas accessible, le bouton indique une erreur de configuration.

---

## Dashboard de monitoring i18n

Route : `/admin/translation-dashboard` — composant `AdminTranslationDashboard.jsx`.

Audit de la couverture des traductions sur l'ensemble du projet, en deux sections :

### Traductions i18n

- Liste toutes les clés de namespace dont la traduction est **absente** (pas d'entrée en base) ou **vide** (texte whitespace)
- Affichage en tableaux pliables par namespace
- Chaque cellule indique le statut par langue : **OK** (vert) / **Absent** (rouge) / **Vide** (jaune)

### Infobulles (infospots)

- Liste les infobulles sans traduction pour une ou plusieurs langues
- Filtre par type de visiteur (chips cliquables)
- Chaque carte affiche : ID, salle associée, langues couvertes/manquantes, types de visiteur couverts
- Lien direct vers la page de détail de la salle concernée

**Endpoint :** `GET /api/translations/dashboard`
Retourne les clés i18n incomplètes et les infobulles non traduites, calculées par requêtes SQL en base.

---

## Configuration des ports et proxy

### Carte des ports

| Service | Dev (local) | Docker prod | Variable de config |
|---------|-------------|-------------|--------------------|
| Client React (Vite) | `localhost:5173` | `localhost:80` | `vite.config.js → server.port` |
| API .NET | `localhost:5078` | `localhost:5078` | `ASPNETCORE_URLS` / `launchSettings.json` |
| MySQL | `localhost:3306` (exposé via override) | réseau interne Docker | `ConnectionStrings__DefaultConnection` |
| LibreTranslate | `localhost:5000` (exposé via override) | réseau interne Docker | `LibreTranslate__Url` |

### Proxy Vite (`vite.config.js`)

En développement, Vite proxie tous les appels `/api/*` vers l'API .NET :

```js
proxy: {
  '/api': {
    target: 'http://localhost:5078',  // port de l'API .NET locale
    changeOrigin: true,
    secure: false,
  }
}
```

**Exception** : `getLanguagesId()` dans `AxiosTranslation.js` appelle `http://localhost:5078` directement (sans proxy) — nécessaire pour le bootstrap i18n avant le montage de l'app React.

Pour changer le port de l'API, modifier `target` ici **et** `ASPNETCORE_URLS` dans `launchSettings.json`.

### `appsettings.json` — configuration du backend

Fichier : `src/DataBaseApi/DataBaseApi/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=corridorview_simple;User Id=root;Password=root;SslMode=none;AllowPublicKeyRetrieval=True;CharSet=utf8mb4;"
  },
  "Firebase": {
    "ProjectId": "yp-2425-10"
  },
  "LibreTranslate": {
    "Url": "http://localhost:5000/translate",
    "ApiKey": ""
  }
}
```

| Clé | Dev | Prod (Docker) | À changer si… |
|-----|-----|---------------|----------------|
| `ConnectionStrings.DefaultConnection` → `Server` | `localhost` | `db` (nom du service Docker) | MySQL tourne ailleurs |
| `ConnectionStrings.DefaultConnection` → `Port` | `3306` | `3306` | Port MySQL personnalisé |
| `Firebase.ProjectId` | `yp-2425-10` | idem | Vous changez de projet Firebase |
| `LibreTranslate.Url` | `http://localhost:5000/translate` | `http://libretranslate:5000/translate` | LibreTranslate tourne ailleurs |
| `LibreTranslate.ApiKey` | `""` (pas de clé) | `""` | Instance LibreTranslate avec clé API |

**En production Docker**, `appsettings.json` n'est pas utilisé directement — les variables d'environnement définies dans `docker-compose.yml` le surchargent (convention ASP.NET Core : `__` remplace `:`) :

```yaml
environment:
  ConnectionStrings__DefaultConnection: "Server=db;Port=3306;..."
  Firebase__ProjectId: "yp-2425-10"
  LibreTranslate__Url: "http://libretranslate:5000/translate"
  LibreTranslate__ApiKey: ""
```

Pour changer la configuration en prod, modifier ces variables dans `docker-compose.yml` — pas `appsettings.json`.

### `docker-compose.override.yml` — ports exposés en dev

Ce fichier est mergé automatiquement par Docker Compose quand il existe. Il expose MySQL et LibreTranslate à l'hôte pour que l'API .NET locale puisse y accéder :

```yaml
services:
  db:
    ports:
      - "3306:3306"    # MySQL accessible depuis localhost:3306
  libretranslate:
    ports:
      - "5000:5000"    # LibreTranslate accessible depuis localhost:5000
```

En production (`docker compose up --build`), ce fichier est toujours chargé — si vous ne voulez pas exposer ces ports en prod, supprimez ou renommez `docker-compose.override.yml` et passez l'option `-f docker-compose.yml` explicitement.

### Ajouter ou changer les langues LibreTranslate

LibreTranslate ne charge que les langues listées dans `LT_LOAD_ONLY` (dans `docker-compose.yml`) pour réduire l'utilisation mémoire :

```yaml
libretranslate:
  environment:
    LT_LOAD_ONLY: "fr,en,es"   # codes ISO 639-1, séparés par des virgules
```

Modifier cette variable puis relancer `docker compose up --build libretranslate` pour prendre en compte les nouvelles langues.

---

## Points techniques notables

- **Proxy Vite** : en dev, les appels `/api/*` sont proxiés vers `http://localhost:5078` (API .NET locale). Exception : `getLanguagesId()` appelle `http://localhost:5078` directement (bootstrap i18n avant montage de l'app).
- **Traductions en base** : les traductions i18next sont stockées en MySQL et servies par `TranslationController`. Pas de fichiers JSON locaux.
- **Conflit Panolens/Three.js** : `panolens@0.12.1` requiert `three@^0.125.2` mais l'app utilise `three@^0.176` — résolu via `overrides` dans `package.json`.
- **Auth admin** : vérifiée via `localStorage.getItem("isAuthenticated") === "true"` dans `PrivateRoute`.
