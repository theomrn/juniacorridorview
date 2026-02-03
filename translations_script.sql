START TRANSACTION;

DELETE FROM translations;
DELETE FROM languages;

-- =========================
-- FRANÇAIS
-- =========================

INSERT INTO languages (name_language)
VALUES ('Français');

SET @id_language := LAST_INSERT_ID();

INSERT INTO translations (id_language, namespace, translation_key, text)
VALUES
(@id_language,'home','guidedTour','Visite Guidée'),
(@id_language,'home','freeTour','Visite Libre'),
(@id_language,'home','welcomeText','Bienvenue dans la visite virtuelle de la Halle Technologique de Junia ! Découvrez un espace dédié à l\'innovation, où technologie et créativité se rencontrent pour façonner le futur. Plongez dans nos installations, explorez nos projets, et laissez-vous inspirer par notre savoir-faire. Bonne visite !'),
(@id_language,'home','halleTitle','La Halle Technologique'),
(@id_language,'home','halleText','La Halle Technologique : un espace d\'innovation et d\'excellence. Explorez ce lieu unique dédié à la recherche, au développement et à la formation technologique. Avec ses équipements de pointe et ses projets collaboratifs, la Halle Technologique est à la croisée des chemins entre industrie, créativité et apprentissage.'),

(@id_language,'navbar','immersion','Immersion'),
(@id_language,'navbar','guidedTour','Visite guidée'),
(@id_language,'navbar','home','Accueil'),
(@id_language,'navbar','admin','Administrateur'),
(@id_language,'navbar','adminTour','Gestion des Parcours'),
(@id_language,'navbar','adminRoom','Gestion des Salles'),
(@id_language,'navbar','adminBuilding','Gestion des Bâtiments'),
(@id_language,'navbar','adminUser','Gestion des Administrateurs'),
(@id_language,'navbar','adminConvert','Conversion AVIF'),
(@id_language,'navbar','adminTranslation','Gestion des Traductions'),
(@id_language,'navbar','adminLanguage','Gestion des Langues'),
(@id_language,'navbar','mainMenu','Menu Principal'),


(@id_language,'pano','freeTour','Visite Libre'),
(@id_language,'pano','loading','Chargement des données...'),
(@id_language,'pano','guidedTour','Visite Guidée'),
(@id_language,'pano','otherRooms','Autres Salles'),
(@id_language,'pano','currentLoading','Chargement des Données...'),
(@id_language,'pano','loadingSuccess','Chargement des données réussi'),
(@id_language,'pano','loadingError','Erreur lors du chargement des données'),


(@id_language,'tour','loading','Chargement des données...'),
(@id_language,'tour','startTour','Commencer le Parcours'),
(@id_language,'tour','room','Salle '),
(@id_language,'tour','noRoom','Salle inconnue'),
(@id_language,'tour','currentLoading','Chargement des Parcours...'),
(@id_language,'tour','loadingSuccess','Chargement des parcours réussi'),
(@id_language,'tour','loadingError','Erreur lors du chargement des Parcours'),

(@id_language,'room','languages','Langages'),
(@id_language,'room','addroom','Ajouter une salle'),
(@id_language,'room','fileconversion','Convertion fichier'),
(@id_language,'room','translation','Traduction'),
(@id_language,'room','course','Parcours'),
(@id_language,'room','buildings','Bâtiments'),
(@id_language,'room','Findroom','Rechercher une salle...'),
(@id_language,'room','building','Bâtiment'),
(@id_language,'room','floor','Étage'),
(@id_language,'room','name','Nom'),
(@id_language,'room','number','Numéro'),

(@id_language,'language','addlanguage','Ajouter une langue'),
(@id_language,'language','namelanguage','Nom de la langue'),
(@id_language,'language','create','Créer'),
(@id_language,'language','cancel','Annuler'),
(@id_language,'language','loadinglanguages','Chargement des langues...'),
(@id_language,'language','notfindlanguage','Aucune langue trouvée. Cliquez sur Ajouter une langue pour en créer une.'),
(@id_language,'language','name','Nom'),
(@id_language,'language','save','Enregistrer'),
(@id_language,'language','modify','Modifier'),
(@id_language,'language','delete','Supprimer'),
(@id_language,'language','namerequired','Le nom de la langue est requis'),
(@id_language,'language','errorloadinglanguage','Erreur lors du chargement des langues'),
(@id_language,'language','succeslanguage','Langue mise à jour avec succès!'),
(@id_language,'language','error','Une erreur est survenue'),
(@id_language,'language','confirmdelete','Êtes-vous sûr de vouloir supprimer la langue'),
(@id_language,'language','succesdelete','Langue supprimée avec succès!'),
(@id_language,'language','errordelete','Erreur lors de la suppression:'),
(@id_language,'language','createlanguage','Langue créée avec succès!'),
(@id_language,'language','return','Retour');


-- =========================
-- ENGLISH
-- =========================

INSERT INTO languages (name_language)
VALUES ('English');

SET @id_language := LAST_INSERT_ID();

INSERT INTO translations (id_language, namespace, translation_key, text)
VALUES
(@id_language,'home','guidedTour','Guided Tour'),
(@id_language,'home','freeTour','Free Tour'),
(@id_language,'home','welcomeText','Welcome to the virtual tour of the Junia Technology Hall! Discover a space dedicated to innovation, where technology and creativity meet to shape the future. Dive into our facilities, explore our projects, and get inspired by our expertise. Enjoy your visit!'),
(@id_language,'home','halleTitle','The Technology Hall'),
(@id_language,'home','halleText','The Technology Hall: a space for innovation and excellence. Explore this unique place dedicated to research, development, and technological training. With cutting-edge equipment and collaborative projects, the Technology Hall stands at the crossroads of industry, creativity, and learning.'),

(@id_language,'navbar','immersion','Immersion'),
(@id_language,'navbar','guidedTour','Guided Tour'),
(@id_language,'navbar','home','Home'),
(@id_language,'navbar','admin','Administrator'),
(@id_language,'navbar','adminTour','Tour Management'),
(@id_language,'navbar','adminRoom','Room Management'),
(@id_language,'navbar','adminBuilding','Building Management'),
(@id_language,'navbar','adminUser','Admin Management'),
(@id_language,'navbar','adminConvert','AVIF Conversion'),
(@id_language,'navbar','adminTranslation','Translation Management'),
(@id_language,'navbar','adminLanguage','Language Management'),
(@id_language,'navbar','mainMenu','Main Menu'),

(@id_language,'pano','freeTour','Free Tour'),
(@id_language,'pano','loading','Loading data...'),
(@id_language,'pano','guidedTour','Guided Tour'),
(@id_language,'pano','otherRooms','Others Rooms'),
(@id_language,'pano','currentLoading','Loading data...'),
(@id_language,'pano','loadingSuccess','Data loaded successfully'),
(@id_language,'pano','loadingError','Error while loading data'),

(@id_language,'tour','loading','Loading data...'),
(@id_language,'tour','startTour','Start Tour'),
(@id_language,'tour','room','Room '),
(@id_language,'tour','noRoom','Unknown room'),
(@id_language,'tour','currentLoading','Loading tours...'),
(@id_language,'tour','loadingSuccess','Tours loaded successfully'),
(@id_language,'tour','loadingError','Error while loading tours'),

(@id_language,'room','languages','Languages'),
(@id_language,'room','addroom','Add room'),
(@id_language,'room','fileconversion','File conversion'),
(@id_language,'room','translation','Translation'),
(@id_language,'room','course','Course'),
(@id_language,'room','buildings','Buildings'),
(@id_language,'room','Findroom','Find room...'),
(@id_language,'room','building','Building'),
(@id_language,'room','floor','Floor'),
(@id_language,'room','name','Name'),
(@id_language,'room','number','Number'),

(@id_language,'language','addlanguage','Add language'),
(@id_language,'language','namelanguage','Name language'),
(@id_language,'language','create','Create'),
(@id_language,'language','cancel','Cancel'),
(@id_language,'language','loadinglanguages','Loading languages...'),
(@id_language,'language','notfindlanguage','No language found. Click Add language to create one.'),
(@id_language,'language','name','Name'),
(@id_language,'language','save','Save'),
(@id_language,'language','modify','Modify'),
(@id_language,'language','delete','Delete'),
(@id_language,'language','namerequired','The language name is required'),
(@id_language,'language','errorloadinglanguage','Error loading languages'),
(@id_language,'language','succeslanguage','Language successfully updated!'),
(@id_language,'language','error','An error has occurred'),
(@id_language,'language','confirmdelete','Are you sure you want to delete the language'),
(@id_language,'language','succesdelete','Language removed successfully!'),
(@id_language,'language','errordelete','Error during deletion:'),
(@id_language,'language','createlanguage','Language created successfully!'),
(@id_language,'language','return','Return');

COMMIT;
