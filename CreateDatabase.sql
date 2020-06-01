CREATE DATABASE  IF NOT EXISTS `greenwelldatabase` /*!40100 DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `greenwelldatabase`;
-- MySQL dump 10.13  Distrib 8.0.19, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: greenwelldatabase
-- ------------------------------------------------------
-- Server version	8.0.19

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(95) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `files`
--

DROP TABLE IF EXISTS `files`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `files` (
  `fileID` int NOT NULL AUTO_INCREMENT,
  `filename` text,
  `fullPath` varchar(100) DEFAULT NULL,
  `author` int DEFAULT NULL,
  `uploadDate` date DEFAULT NULL,
  `extType` varchar(5) DEFAULT NULL,
  `fileSize` double DEFAULT NULL,
  `adminOnly` tinyint DEFAULT NULL,
  `approved` tinyint DEFAULT NULL,
  PRIMARY KEY (`fileID`),
  KEY `author` (`author`),
  CONSTRAINT `files_ibfk_2` FOREIGN KEY (`author`) REFERENCES `users` (`userID`)
) ENGINE=InnoDB AUTO_INCREMENT=149 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `files`
--

LOCK TABLES `files` WRITE;
/*!40000 ALTER TABLE `files` DISABLE KEYS */;
INSERT INTO `files` VALUES (142,'CreateDatabase.sql','Empty/CreateDatabase.sql',NULL,NULL,NULL,NULL,NULL,1),(146,'Library Access.pdf','Empty/Library Access.pdf',NULL,NULL,NULL,NULL,NULL,1),(147,'SendGrid Secrets','SendGrid Secrets',NULL,NULL,NULL,NULL,1,1),(148,'ToDO','ToDO',NULL,NULL,NULL,NULL,1,1);
/*!40000 ALTER TABLE `files` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tagmap`
--

DROP TABLE IF EXISTS `tagmap`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tagmap` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fileId` int NOT NULL,
  `tagId` int NOT NULL,
  PRIMARY KEY (`id`),
  KEY `fileId_idx` (`fileId`),
  KEY `tagId_idx` (`tagId`),
  CONSTRAINT `fileId` FOREIGN KEY (`fileId`) REFERENCES `files` (`fileID`),
  CONSTRAINT `tagId` FOREIGN KEY (`tagId`) REFERENCES `tags` (`tagID`)
) ENGINE=InnoDB AUTO_INCREMENT=44 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tagmap`
--

LOCK TABLES `tagmap` WRITE;
/*!40000 ALTER TABLE `tagmap` DISABLE KEYS */;
INSERT INTO `tagmap` VALUES (37,142,65),(41,146,67),(42,147,68),(43,148,69);
/*!40000 ALTER TABLE `tagmap` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tags`
--

DROP TABLE IF EXISTS `tags`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tags` (
  `tagName` varchar(30) DEFAULT NULL,
  `tagID` int NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`tagID`)
) ENGINE=InnoDB AUTO_INCREMENT=70 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tags`
--

LOCK TABLES `tags` WRITE;
/*!40000 ALTER TABLE `tags` DISABLE KEYS */;
INSERT INTO `tags` VALUES ('admin',65),('approved?',66),('library',67),('Scary',68),('share',69);
/*!40000 ALTER TABLE `tags` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `userName` varchar(30) DEFAULT NULL,
  `userID` int NOT NULL,
  `aspnetRoles` varchar(8) DEFAULT NULL,
  PRIMARY KEY (`userID`),
  CONSTRAINT `users_chk_1` CHECK (((`aspnetRoles` = _utf8mb4'Admin') or (`aspnetRoles` = _utf8mb4'Employee')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2020-04-19 11:28:49
