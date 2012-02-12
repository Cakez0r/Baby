delimiter $$

#Create DB
CREATE DATABASE `baby` /*!40100 DEFAULT CHARACTER SET latin1 */$$


#Create Table QueuedUrl
CREATE TABLE `QueuedUrl` (
  `UrlID` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Url` varchar(1024) NOT NULL,
  `Visited` bit(1) NOT NULL,
  PRIMARY KEY (`UrlID`),
  UNIQUE KEY `UrlID_UNIQUE` (`UrlID`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1$$


#Create stored procedure GET_QueuedUrl
CREATE DEFINER=`root`@`localhost` PROCEDURE `GET_QueuedUrl`()
BEGIN
    DECLARE returnedUrl VARCHAR(1024);
    DECLARE urlID BIGINT;
    
    SELECT urlID = `UrlID`, returnedUrl = `Url` FROM `baby`.`QueuedUrl` WHERE `Visited` = 0 LIMIT 1;
    
    UPDATE `baby`.`QueuedUrl` SET Visited = 1 WHERE UrlID = urlID;
    
    SELECT returnedUrl;
END$$


#Create stored procedure GET_QueuedUrlCount
CREATE DEFINER=`root`@`localhost` PROCEDURE `GET_QueuedUrlCount`()
BEGIN
    SELECT COUNT(UrlID) FROM `baby`.`QueuedUrl`;
END$$


#Create stored procedure GET_QueuedUrls
CREATE DEFINER=`root`@`localhost` PROCEDURE `GET_QueuedUrls`(count INT)
BEGIN
    
    CREATE TEMPORARY TABLE urls ENGINE=memory SELECT UrlID, Url FROM `baby`.`QueuedUrl` LIMIT count;
    
    UPDATE `baby`.`QueuedUrl` SET Visited = 1 WHERE UrlID IN (SELECT UrlID FROM urls);
    
    SELECT Url FROM urls;
    
    DROP TEMPORARY TABLE IF EXISTS urls;
END$$


#Create stored procedure INS_QueuedURL
CREATE DEFINER=`root`@`localhost` PROCEDURE `INS_QueuedUrl`(url VARCHAR(1024))
BEGIN
    INSERT INTO `baby`.`QueuedUrl` (Url, Visited) VALUES (url, 0);
END$$

DELIMITER ;