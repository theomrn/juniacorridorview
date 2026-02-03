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
(@id_language,'tour','loadingError','Erreur lors du chargement des Parcours');


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
(@id_language,'tour','loadingError','Error while loading tours');

COMMIT;
