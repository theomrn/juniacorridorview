SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

-- Désactiver temporairement la vérification pour le nettoyage des tables
SET FOREIGN_KEY_CHECKS = 0; 

--
-- 1. Drop Tables (Ordre inverse de dépendance)
--

DROP TABLE IF EXISTS `Tour_Steps`;
DROP TABLE IF EXISTS `Tours`;
DROP TABLE IF EXISTS `Links`;
DROP TABLE IF EXISTS `Info_Popup`;
DROP TABLE IF EXISTS `Picture_Meta`;
DROP TABLE IF EXISTS `Room_Previews`;
DROP TABLE IF EXISTS `Pictures`;
DROP TABLE IF EXISTS `Rooms`;
DROP TABLE IF EXISTS `Floors`;
DROP TABLE IF EXISTS `Buildings`;

-- Réactiver la vérification pour la création des tables
SET FOREIGN_KEY_CHECKS = 1;

--
-- 2. Create Tables (Ordre de dépendance respecté)
--

-- 2.1. Buildings
CREATE TABLE `Buildings` (
`id_buildings` int NOT NULL AUTO_INCREMENT,
`name` longtext,
PRIMARY KEY (`id_buildings`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.2. Floors (Dépend de Buildings)
CREATE TABLE `Floors` (
`id_floors` int NOT NULL AUTO_INCREMENT,
`plan_path` varchar(255) DEFAULT NULL,
`id_buildings` int DEFAULT NULL,
`name` longtext,
PRIMARY KEY (`id_floors`),
KEY `fk_floors_buildings` (`id_buildings`),
CONSTRAINT `fk_floors_buildings` FOREIGN KEY (`id_buildings`) REFERENCES `Buildings` (`id_buildings`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.3. Rooms (Dépend de Floors)
CREATE TABLE `Rooms` (
`id_rooms` int NOT NULL AUTO_INCREMENT,
`name` longtext,
`number` longtext,
`hidden` tinyint(1) DEFAULT NULL,
`id_floors` int DEFAULT NULL,
`plan_x` decimal(10,8) DEFAULT NULL,
`plan_y` decimal(10,8) DEFAULT NULL,
PRIMARY KEY (`id_rooms`),
KEY `fk_rooms_floors` (`id_floors`),
CONSTRAINT `fk_rooms_floors` FOREIGN KEY (`id_floors`) REFERENCES `Floors` (`id_floors`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=50 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.4. Tours
CREATE TABLE `Tours` (
`id_tours` int NOT NULL AUTO_INCREMENT,
`title` longtext,
`description` longtext,
`hidden` tinyint(1) DEFAULT '0',
PRIMARY KEY (`id_tours`)
) ENGINE=InnoDB AUTO_INCREMENT=25 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.5. Tour_Steps (Dépend de Rooms et Tours)
CREATE TABLE `Tour_Steps` (
`id_tour_steps` int NOT NULL AUTO_INCREMENT,
`id_rooms` int DEFAULT NULL,
`step_number` int DEFAULT NULL,
`id_tours` int DEFAULT NULL,
PRIMARY KEY (`id_tour_steps`),
KEY `fk_tour_steps_rooms` (`id_rooms`),
KEY `fk_tour_steps_tours` (`id_tours`),
CONSTRAINT `fk_tour_steps_rooms` FOREIGN KEY (`id_rooms`) REFERENCES `Rooms` (`id_rooms`) ON DELETE CASCADE,
CONSTRAINT `fk_tour_steps_tours` FOREIGN KEY (`id_tours`) REFERENCES `Tours` (`id_tours`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=989141747 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.6. Pictures (Dépend de Rooms)
CREATE TABLE `Pictures` (
`id_pictures` int NOT NULL AUTO_INCREMENT,
`id_rooms` int DEFAULT NULL,
`picture_path` varchar(255) DEFAULT NULL,
PRIMARY KEY (`id_pictures`),
KEY `fk_pictures_rooms` (`id_rooms`),
CONSTRAINT `fk_pictures_rooms` FOREIGN KEY (`id_rooms`) REFERENCES `Rooms` (`id_rooms`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=54 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.7. Picture_Meta (Dépend de Pictures)
CREATE TABLE `Picture_Meta` (
`id_pictures` int NOT NULL,
`mime_type` varchar(100) DEFAULT NULL,
`width` int DEFAULT NULL,
`height` int DEFAULT NULL,
`last_accessed` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
PRIMARY KEY (`id_pictures`),
CONSTRAINT `Picture_Meta_ibfk_1` FOREIGN KEY (`id_pictures`) REFERENCES `Pictures` (`id_pictures`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.8. Room_Previews (Dépend de Rooms)
CREATE TABLE `Room_Previews` (
`id_room_previews` int NOT NULL AUTO_INCREMENT,
`id_rooms` int NOT NULL,
`preview_path` varchar(255) NOT NULL,
PRIMARY KEY (`id_room_previews`),
KEY `id_rooms` (`id_rooms`),
CONSTRAINT `Room_Previews_ibfk_1` FOREIGN KEY (`id_rooms`) REFERENCES `Rooms` (`id_rooms`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.9. Info_Popup (Dépend de Pictures)
CREATE TABLE `Info_Popup` (
`id_info_popup` int NOT NULL AUTO_INCREMENT,
`id_pictures` int DEFAULT NULL,
`position_x` float DEFAULT NULL,
`position_y` float DEFAULT NULL,
`position_z` float DEFAULT NULL,
`text` longtext,
`title` longtext,
`image_path` varchar(255) DEFAULT NULL,
PRIMARY KEY (`id_info_popup`),
KEY `fk_info_popup_pictures` (`id_pictures`),
CONSTRAINT `fk_info_popup_pictures` FOREIGN KEY (`id_pictures`) REFERENCES `Pictures` (`id_pictures`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=157 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2.10. Links (Dépend de Pictures)
CREATE TABLE `Links` (
`id_links` int NOT NULL AUTO_INCREMENT,
`id_pictures` int DEFAULT NULL,
`position_x` float DEFAULT NULL,
`position_y` float DEFAULT NULL,
`position_z` float DEFAULT NULL,
`id_pictures_destination` int DEFAULT NULL,
PRIMARY KEY (`id_links`),
KEY `fk_links_pictures` (`id_pictures`),
KEY `fk_links_pictures_destination` (`id_pictures_destination`),
CONSTRAINT `fk_links_pictures` FOREIGN KEY (`id_pictures`) REFERENCES `Pictures` (`id_pictures`) ON DELETE CASCADE,
CONSTRAINT `fk_links_pictures_destination` FOREIGN KEY (`id_pictures_destination`) REFERENCES `Pictures` (`id_pictures`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=88 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

SET @@SESSION.SQL_LOG_BIN = COALESCE(@MYSQLDUMP_TEMP_LOG_BIN, 1);