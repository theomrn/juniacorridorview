# Junia Corridor View

Junia Corridor View is a comprehensive web application designed to manage and visualize room and building information, including panoramic (360°) images, across various floors and buildings. It provides an intuitive interface for both administrators and visitors to organize, explore, and interact with spatial data effectively.

## Project Structure

This is a **unified monorepo project** combining:
- **Frontend**: React.js application (Vite) in `src/client/`
- **Backend**: .NET API in `src/DataBaseApi/`
- **Database**: MySQL
- **Legacy**: Old Node.js/Express server in `src/server_old/`

## Installation

### Prerequisites
- Node.js (v16 or higher)
- .NET 10.0
- MySQL database
- Firebase credentials

### Setup Steps

1. **Clone and install dependencies**:
   ```bash
   npm install
   ```

2. **Configure Firebase**:
   - Copy `src/client/src/firebaseConfig.js.example` to `src/client/src/firebaseConfig.js`
   - Add your Firebase configuration credentials

3. **Initialize the database**:
   - Create a MySQL database
   - Run the SQL scripts in the project root:
     - `db_script_empty.sql`: Initial database schema
     - `translations_script.sql`: Translation setup and sample data
   ```bash
   mysql -u your_user -p your_database < db_script_empty.sql
   mysql -u your_user -p your_database < translations_script.sql
   ```
   - Update the database connection string in the `.NET backend configuration`

4. **Start development servers**:
   ```bash
   npm run dev
   ```
   This runs:
   - React client on Vite dev server
   - .NET API on `http://localhost:5078`

4. **Build for production**:
   ```bash
   npm run build
   ```

## Features

### Visitor Features

#### Home Page
- Choose between two exploration modes:
  - **Guided Tour**: Follow a predefined path through rooms with structured information
  - **Free Tour**: Explore buildings freely at your own pace

#### Panoramic Viewer (360° Mode)
- View immersive panoramic images of rooms
- Interactive information hotspots providing technical details
- Room navigation and building maps
- Smooth transitions between rooms

#### Building Navigation
- Visual floor plans with room placements
- Real-time location tracking
- Multi-floor navigation

### Administration Features

#### Room Management
- View all rooms in card-based layout with detailed metadata
- Add, edit, and delete room information
- Upload and manage panoramic (360°) images
- Assign images to rooms
- Place rooms on interactive floor plans
- Room categorization and filtering

#### Building & Floor Management
- Create and manage buildings
- Organize floors within buildings
- Associate rooms with specific buildings and floors
- Edit floor information and metadata

#### Guided Tour Management
- Create and edit guided tours
- Manage tour steps and sequence
- Add multiple rooms to tours
- Organize rooms in carousel format

#### Language & Translation Management
- Support for multiple languages
- Manage translations for:
  - Room names and descriptions
  - Building and floor information
  - User interface elements
  - Technical hotspot information

#### User Management
- Create and manage admin accounts
- Control access permissions
- User role management

#### File Management
- Convert and optimize panoramic images
- Handle AVIF image format support
- File upload and storage management

## Tech Stack

### Frontend
- **Framework**: React 19.2.3 with Vite
- **Routing**: React Router DOM 5.2.0
- **Styling**: Tailwind CSS with PostCSS
- **State Management**: React Context API
- **API Client**: Axios
- **Panorama Viewer**: Panolens 0.12.1 with Three.js
- **UI Components**: React Icons, React Select, Sonner (toasts)
- **Animations**: Framer Motion
- **Drag & Drop**: dnd-kit
- **Internationalization**: i18next with HTTP backend
- **Firebase**: Firebase 11.8.1 for authentication and services

### Backend
- **Framework**: .NET 8.0 with ASP.NET Core
- **Database**: MySQL
- **API Documentation**: Swagger/OpenAPI
- **CORS**: Enabled for all origins
- **Compression**: Gzip & Brotli HTTP compression
- **File Processing**: Multer for uploads
- **Email**: Nodemailer
- **Real-time**: Socket.io for live updates
- **Firebase Admin SDK**: For backend authentication

### Database
- **System**: MySQL
- Schema includes:
  - Users & Authentication
  - Buildings, Floors, Rooms
  - Panoramic Images & Metadata
  - Guided Tours & Steps
  - Translations & Languages
  - Technical Information Hotspots

## Available Scripts

```bash
# Development mode (runs client + .NET server concurrently)
npm run dev

# Development - Client only (Vite dev server)
npm run dev:client

# Development - Server only (.NET API)
npm run dev:server

# Production build
npm run build

# Preview production build
npm run preview
```

## API Structure

The backend exposes REST endpoints through `http://localhost:5078`:

### Main Controllers
- **ApiController**: Core room, building, and floor operations
- **TranslationController**: Language and translation management

### Services
- **DatabaseService**: Database operations and queries
- **FileOptimiserService**: Image optimization and format conversion

## Component Architecture

### Key Client Components
- `App.jsx`: Main routing and authentication logic
- `Navbar.jsx`: Navigation and user menu
- `Home.jsx`: Landing page with tour selection
- `Tour.jsx`: Guided tour interface
- `Pano.jsx`: Panorama 360° viewer
- `AdminRoom.jsx`: Room list and management
- `AdminRoomDetails.jsx`: Detailed room editing
- `AdminBuilding.jsx`: Building management
- `AdminTour.jsx`: Tour creation and editing
- `AdminTranslation.jsx`: Language and translation management
- `AdminUser.jsx`: User account management
- `AdminLanguage.jsx`: Language configuration

### Sub-Components
- `buildings/`: Building-related components
- `dialogs/`: Modal dialogs for forms and confirmations
- `plan/`: Floor plan visualization
- `room_details/`: Detailed room information displays

## Internationalization

The application supports multiple languages using i18next:
- Language files in `src/client/src/i18n/locales/`
- HTTP backend for dynamic translation loading
- Full translation coverage for UI and content

## Authentication

- Firebase-based authentication for users
- JWT token management
- Admin role-based access control
- Protected routes for admin panels

## Database Scripts

- `db_script_empty.sql`: Initial database schema
- `translations_script.sql`: Translation setup and sample data

## Performance Features

- Image optimization and AVIF format support
- HTTP compression (Gzip & Brotli)
- Optimized panoramic image rendering with Three.js
- Efficient asset bundling with Vite

## Development Notes

- The project uses modern ES modules and JSX syntax
- Tailwind CSS for responsive design
- Environment configuration through `.env` files
- Firebase configuration stored separately for security
- Hot module reloading during development
