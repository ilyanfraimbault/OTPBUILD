CREATE TABLE Players
(
    PlayerName    VARCHAR(50) PRIMARY KEY,
    TwitchChannel VARCHAR(50)
);

CREATE TABLE PlayerChampions
(
    PlayerName VARCHAR(50) NOT NULL,
    Champion   INT         NOT NULL,
    PlayRate   FLOAT       NOT NULL,
    PRIMARY KEY (PlayerName, Champion),
    FOREIGN KEY (PlayerName) REFERENCES Players (PlayerName)
);

CREATE TABLE Summoners
(
    Id            VARCHAR(63) NOT NULL,
    Puuid         VARCHAR(78) NOT NULL,
    Name          VARCHAR(50),
    AccountId     VARCHAR(56),
    ProfileIconId INT,
    RevisionDate  BIGINT,
    Level         BIGINT,
    PlayerName    VARCHAR(50),
    PRIMARY KEY (Puuid),
    FOREIGN KEY (PlayerName) REFERENCES Players (PlayerName)
);

CREATE TABLE Accounts
(
    Puuid    VARCHAR(78) NOT NULL,
    GameName VARCHAR(50),
    TagLine  VARCHAR(50),
    PRIMARY KEY (Puuid)
);

CREATE TABLE Games
(
    GameDuration       INT         NOT NULL,
    GameStartTimestamp BIGINT      NOT NULL,
    GameId             BIGINT      NOT NULL,
    GameVersion        VARCHAR(50) NOT NULL,
    GameType           VARCHAR(50) NOT NULL,
    MatchId            VARCHAR(50) NOT NULL,
    PlatformId         VARCHAR(10) NOT NULL,
    Winner             INT         NOT NULL,
    PRIMARY KEY (GameId)
);

CREATE TABLE StatPerks
(
    id      INT AUTO_INCREMENT PRIMARY KEY,
    defense INT NOT NULL,
    flex    INT NOT NULL,
    offense INT NOT NULL
);

CREATE TABLE StyleSelection
(
    id   INT AUTO_INCREMENT PRIMARY KEY,
    perk INT NOT NULL,
    var1 INT NOT NULL,
    var2 INT NOT NULL,
    var3 INT NOT NULL
);

CREATE TABLE PerksStyle
(
    id              INT AUTO_INCREMENT PRIMARY KEY,
    description     VARCHAR(50) NOT NULL,
    style           INT         NOT NULL,
    styleSelection1 INT         NOT NULL,
    styleSelection2 INT         NOT NULL,
    styleSelection3 INT,
    styleSelection4 INT,
    FOREIGN KEY (styleSelection1) REFERENCES StyleSelection (id),
    FOREIGN KEY (styleSelection2) REFERENCES StyleSelection (id),
    FOREIGN KEY (styleSelection3) REFERENCES StyleSelection (id),
    FOREIGN KEY (styleSelection4) REFERENCES StyleSelection (id)
);

CREATE TABLE Perks
(
    id             INT AUTO_INCREMENT PRIMARY KEY,
    statPerks      INT NOT NULL,
    primaryStyle   INT NOT NULL,
    secondaryStyle INT NOT NULL,
    FOREIGN KEY (statPerks) REFERENCES StatPerks (id),
    FOREIGN KEY (primaryStyle) REFERENCES PerksStyle (id),
    FOREIGN KEY (secondaryStyle) REFERENCES PerksStyle (id)
);

CREATE TABLE Participants
(
    GameId         BIGINT      NOT NULL,
    SummonerPuuid  VARCHAR(78) NOT NULL,
    Champion       INT         NOT NULL,
    TeamId         INT         NOT NULL,
    Kills          INT         NOT NULL,
    Deaths         INT         NOT NULL,
    Assists        INT         NOT NULL,
    item0          INT         NOT NULL,
    item1          INT         NOT NULL,
    item2          INT         NOT NULL,
    item3          INT         NOT NULL,
    item4          INT         NOT NULL,
    item5          INT         NOT NULL,
    item6          INT         NOT NULL,
    spellCast1     INT         NOT NULL,
    spellCast2     INT         NOT NULL,
    spellCast3     INT         NOT NULL,
    spellCast4     INT         NOT NULL,
    SummonerSpell1 INT         NOT NULL,
    SummonerSpell2 INT         NOT NULL,
    Perks          INT         NOT NULL,
    TeamPosition   VARCHAR(10) NOT NULL,
    PRIMARY KEY (GameId, SummonerPuuid),
    FOREIGN KEY (Perks) REFERENCES Perks (id),
    FOREIGN KEY (SummonerPuuid) REFERENCES Summoners (Puuid),
    FOREIGN KEY (GameId) REFERENCES Games (GameId)
);

CREATE PROCEDURE getGame(game_id BIGINT)
BEGIN
    SELECT GameDuration,
           GameStartTimestamp,
           G.GameId,
           GameVersion,
           GameType,
           MatchId,
           PlatformId,
           Winner,
           SummonerPuuid,
           S.Id                           AS SummonerId,
           Name                           AS SummonerName,
           Level                          AS SummonerLevel,
           A.GameName                     AS GameName,
           A.TagLine                      AS TagLine,
           Champion,
           TeamId,
           Kills,
           Deaths,
           Assists,
           item0,
           item1,
           item2,
           item3,
           item4,
           item5,
           item6,
           spellCast1,
           spellCast2,
           spellCast3,
           spellCast4,
           SummonerSpell1,
           SummonerSpell2,
           TeamPosition,
           defense,
           flex,
           offense,
           primaryStyle.description       AS primaryStyleDescription,
           primaryStyle.style             AS primaryStyle,
           primaryStyle.styleSelection1   AS primStyleSelection1,
           primaryStyle.styleSelection2   AS primStyleSelection2,
           primaryStyle.styleSelection3   AS primStyleSelection3,
           primaryStyle.styleSelection4   AS primStyleSelection4,
           secondaryStyle.description     AS secondaryStyleDescription,
           secondaryStyle.style           AS secondaryStyle,
           secondaryStyle.styleSelection1 AS secStyleSelection1,
           secondaryStyle.styleSelection2 AS secStyleSelection2,
           primStyleSelection1.perk       AS primStyleSelection1Perk,
           primStyleSelection1.var1       AS primStyleSelection1Var1,
           primStyleSelection1.var2       AS primStyleSelection1Var2,
           primStyleSelection1.var3       AS primStyleSelection1Var3,
           primStyleSelection2.perk       AS primStyleSelection2Perk,
           primStyleSelection2.var1       AS primStyleSelection2Var1,
           primStyleSelection2.var2       AS primStyleSelection2Var2,
           primStyleSelection2.var3       AS primStyleSelection2Var3,
           primStyleSelection3.perk       AS primStyleSelection3Perk,
           primStyleSelection3.var1       AS primStyleSelection3Var1,
           primStyleSelection3.var2       AS primStyleSelection3Var2,
           primStyleSelection3.var3       AS primStyleSelection3Var3,
           primStyleSelection4.perk       AS primStyleSelection4Perk,
           primStyleSelection4.var1       AS primStyleSelection4Var1,
           primStyleSelection4.var2       AS primStyleSelection4Var2,
           primStyleSelection4.var3       AS primStyleSelection4Var3,
           secStyleSelection1.perk        AS secStyleSelection1Perk,
           secStyleSelection1.var1        AS secStyleSelection1Var1,
           secStyleSelection1.var2        AS secStyleSelection1Var2,
           secStyleSelection1.var3        AS secStyleSelection1Var3,
           secStyleSelection2.perk        AS secStyleSelection2Perk,
           secStyleSelection2.var1        AS secStyleSelection2Var1,
           secStyleSelection2.var2        AS secStyleSelection2Var2,
           secStyleSelection2.var3        AS secStyleSelection2Var3
    FROM Games G
             JOIN Participants P on G.GameId = P.GameId
             JOIN Summoners S on P.SummonerPuuid = S.Puuid
             JOIN Accounts A on S.Puuid = A.Puuid
             JOIN Perks P2 on P2.id = P.Perks
             JOIN PerksStyle primaryStyle on primaryStyle.id = P2.primaryStyle
             JOIN PerksStyle secondaryStyle on secondaryStyle.id = P2.secondaryStyle
             JOIN StyleSelection primStyleSelection1 on primStyleSelection1.id = primaryStyle.styleSelection1
             JOIN StyleSelection primStyleSelection2 on primStyleSelection2.id = primaryStyle.styleSelection2
             JOIN StyleSelection primStyleSelection3 on primStyleSelection3.id = primaryStyle.styleSelection3
             JOIN StyleSelection primStyleSelection4 on primStyleSelection4.id = primaryStyle.styleSelection4
             JOIN StyleSelection secStyleSelection1 on secStyleSelection1.id = secondaryStyle.styleSelection1
             JOIN StyleSelection secStyleSelection2 on secStyleSelection2.id = secondaryStyle.styleSelection2
             JOIN StatPerks statPerks on statPerks.id = P2.statPerks
    WHERE G.GameId = game_id;
END;

DELIMITER //

CREATE PROCEDURE insertParticipant(
    IN p_GameId BIGINT,
    IN p_SummonerPuuid VARCHAR(78),
    IN p_SummonerId VARCHAR(63),
    IN p_GameName VARCHAR(50),
    IN p_TagLine VARCHAR(50),
    IN p_Champion INT,
    IN p_TeamId INT,
    IN p_Kills INT,
    IN p_Deaths INT,
    IN p_Assists INT,
    IN p_Item0 INT,
    IN p_Item1 INT,
    IN p_Item2 INT,
    IN p_Item3 INT,
    IN p_Item4 INT,
    IN p_Item5 INT,
    IN p_Item6 INT,
    IN p_SpellCast1 INT,
    IN p_SpellCast2 INT,
    IN p_SpellCast3 INT,
    IN p_SpellCast4 INT,
    IN p_SummonerSpell1 INT,
    IN p_SummonerSpell2 INT,
    IN p_Perks INT,
    IN p_TeamPosition VARCHAR(10)
)
BEGIN
    CALL insertAccount(p_SummonerPuuid, p_GameName, p_TagLine);
    CALL insertSummoner(p_SummonerId, p_SummonerPuuid, p_GameName,
                        NULL, NULL, NULL, NULL, NULL);

    IF NOT EXISTS (SELECT * FROM Participants WHERE GameId = p_GameId AND Participants.SummonerPuuid = p_SummonerPuuid) THEN
        INSERT INTO Participants
        VALUES (p_GameId, p_SummonerPuuid, p_Champion, p_TeamId, p_Kills, p_Deaths, p_Assists,
                p_Item0, p_Item1, p_Item2, p_Item3, p_Item4, p_Item5, p_Item6,
                p_SpellCast1, p_SpellCast2, p_SpellCast3, p_SpellCast4,
                p_SummonerSpell1, p_SummonerSpell2, p_Perks, p_TeamPosition);
    END IF;
END //

CREATE PROCEDURE insertAccount(
    IN p_Puuid VARCHAR(78),
    IN p_GameName VARCHAR(50),
    IN p_TagLine VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT * FROM Accounts WHERE Puuid = p_Puuid) THEN
        INSERT INTO Accounts (Puuid, GameName, TagLine)
        VALUES (p_Puuid, p_GameName, p_TagLine);
    ELSE
        UPDATE Accounts
        SET GameName = IFNULL(GameName, p_GameName),
            TagLine  = IFNULL(TagLine, p_TagLine)
        WHERE Puuid = p_Puuid;
    END IF;
END //

CREATE PROCEDURE insertSummoner(
    IN p_Id VARCHAR(63),
    IN p_Puuid VARCHAR(78),
    IN p_Name VARCHAR(50),
    IN p_AccountId VARCHAR(56),
    IN p_ProfileIconId INT,
    IN p_RevisionDate BIGINT,
    IN p_Level BIGINT,
    IN p_PlayerName VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT * FROM Summoners WHERE Puuid = p_Puuid) THEN
        INSERT INTO Summoners
        VALUES (p_Id, p_Puuid, p_Name, p_AccountId, p_ProfileIconId, p_RevisionDate, p_Level, p_PlayerName);
    ELSE
        UPDATE Summoners
        SET Name          = IFNULL(Name, p_Name),
            AccountId     = IFNULL(AccountId, p_AccountId),
            ProfileIconId = IFNULL(ProfileIconId, p_ProfileIconId),
            RevisionDate  = IFNULL(RevisionDate, p_RevisionDate),
            Level         = IFNULL(Level, p_Level),
            PlayerName    = IFNULL(PlayerName, p_PlayerName)
        WHERE Puuid = p_Puuid;
    END IF;
END //

CREATE PROCEDURE insertPlayer(
    IN p_PlayerName VARCHAR(50),
    IN p_TwitchChannel VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT * FROM Players WHERE PlayerName = p_PlayerName) THEN
        INSERT INTO Players
        VALUES (p_PlayerName, p_TwitchChannel);
    ELSE
        UPDATE Players
        SET TwitchChannel = IFNULL(TwitchChannel, p_TwitchChannel)
        WHERE PlayerName = p_PlayerName;
    END IF;
END //

CREATE PROCEDURE insertPlayerChampion(
    IN p_PlayerName VARCHAR(50),
    IN p_Champion INT,
    IN p_PlayRate FLOAT
)
BEGIN
    IF NOT EXISTS (SELECT * FROM PlayerChampions WHERE PlayerName = p_PlayerName AND Champion = p_Champion) THEN
        INSERT INTO PlayerChampions
        VALUES (p_PlayerName, p_Champion, p_PlayRate);
    ELSE
        UPDATE PlayerChampions
        SET PlayRate = IFNULL(PlayRate, p_PlayRate)
        WHERE PlayerName = p_PlayerName
          AND Champion = p_Champion;
    END IF;
END //

CREATE PROCEDURE insertGame(
    IN p_GameId BIGINT,
    IN p_GameDuration INT,
    IN p_GameStartTimestamp BIGINT,
    IN p_GameVersion VARCHAR(50),
    IN p_GameType VARCHAR(50),
    IN p_PlatformId VARCHAR(10),
    IN p_Winner INT,
    IN p_MatchId VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT * FROM Games WHERE GameId = p_GameId) THEN
        INSERT INTO Games
        VALUES (p_GameDuration, p_GameStartTimestamp, p_GameId, p_GameVersion, p_GameType, p_MatchId, p_PlatformId,
                p_Winner);
    END IF;
END //

CREATE PROCEDURE insertStatPerks(
    IN p_Defense INT,
    IN p_Flex INT,
    IN p_Offense INT,
    OUT p_Id INT
)
BEGIN
    SELECT id
    INTO p_Id
    FROM StatPerks
    WHERE defense = p_Defense
      AND flex = p_Flex
      AND offense = p_Offense;

    IF p_Id IS NULL THEN
        INSERT INTO StatPerks (defense, flex, offense)
        VALUES (p_Defense, p_Flex, p_Offense);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END //

CREATE PROCEDURE insertStyleSelection(
    IN p_Perk INT,
    IN p_Var1 INT,
    IN p_Var2 INT,
    IN p_Var3 INT,
    OUT p_Id INT
)
BEGIN
    SELECT id
    INTO p_Id
    FROM StyleSelection
    WHERE perk = p_Perk
      AND var1 = p_Var1
      AND var2 = p_Var2
      AND var3 = p_Var3;

    IF p_Id IS NULL THEN
        INSERT INTO StyleSelection (perk, var1, var2, var3)
        VALUES (p_Perk, p_Var1, p_Var2, p_Var3);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END //

CREATE PROCEDURE insertPerksStyle(
    IN p_Description VARCHAR(50),
    IN p_Style INT,
    IN p_StyleSelection1 INT,
    IN p_StyleSelection2 INT,
    IN p_StyleSelection3 INT,
    IN p_StyleSelection4 INT,
    OUT p_Id INT
)
BEGIN
    SELECT id
    INTO p_Id
    FROM PerksStyle
    WHERE description = p_Description
      AND style = p_Style
      AND styleSelection1 = p_StyleSelection1
      AND styleSelection2 = p_StyleSelection2
      AND styleSelection3 = p_StyleSelection3
      AND styleSelection4 = p_StyleSelection4;

    IF p_Id IS NULL THEN
        INSERT INTO PerksStyle (description, style, styleSelection1, styleSelection2, styleSelection3, styleSelection4)
        VALUES (p_Description, p_Style, p_StyleSelection1, p_StyleSelection2, p_StyleSelection3, p_StyleSelection4);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END //

CREATE PROCEDURE insertPerks(
    IN p_StatPerks INT,
    IN p_PrimaryStyle INT,
    IN p_SecondaryStyle INT,
    OUT p_Id INT
)
BEGIN
    SELECT id
    INTO p_Id
    FROM Perks
    WHERE statPerks = p_StatPerks
      AND primaryStyle = p_PrimaryStyle
      AND secondaryStyle = p_SecondaryStyle;

    IF p_Id IS NULL THEN
        INSERT INTO Perks (statPerks, primaryStyle, secondaryStyle)
        VALUES (p_StatPerks, p_PrimaryStyle, p_SecondaryStyle);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END //

DELIMITER //