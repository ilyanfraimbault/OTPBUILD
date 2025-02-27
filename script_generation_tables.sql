create table Accounts
(
    Puuid    varchar(78) not null
        primary key,
    GameName varchar(50) null,
    TagLine  varchar(50) null
);

create table Games
(
    GameDuration       int         not null,
    GameStartTimestamp bigint      not null,
    GameId             bigint      not null
        primary key,
    GameVersion        varchar(50) not null,
    GameType           varchar(50) not null,
    MatchId            varchar(50) not null,
    PlatformId         varchar(10) not null,
    Winner             int         not null
);

create table Players
(
    PlayerName    varchar(50) not null
        primary key,
    TwitchChannel varchar(50) null
);

create table PlayerChampions
(
    PlayerName varchar(50) not null,
    Champion   int         not null,
    PlayRate   float       not null,
    primary key (PlayerName, Champion),
    constraint PlayerChampions_ibfk_1
        foreign key (PlayerName) references Players (PlayerName)
);

create table StatPerks
(
    id      int auto_increment
        primary key,
    defense int not null,
    flex    int not null,
    offense int not null
);

create table StyleSelection
(
    id   int auto_increment
        primary key,
    perk int not null,
    var1 int not null,
    var2 int not null,
    var3 int not null
);

create table PerksStyle
(
    id              int auto_increment
        primary key,
    description     varchar(50) not null,
    style           int         not null,
    styleSelection1 int         not null,
    styleSelection2 int         not null,
    styleSelection3 int         null,
    styleSelection4 int         null,
    constraint PerksStyle_ibfk_1
        foreign key (styleSelection1) references StyleSelection (id),
    constraint PerksStyle_ibfk_2
        foreign key (styleSelection2) references StyleSelection (id),
    constraint PerksStyle_ibfk_3
        foreign key (styleSelection3) references StyleSelection (id),
    constraint PerksStyle_ibfk_4
        foreign key (styleSelection4) references StyleSelection (id)
);

create table Perks
(
    id             int auto_increment
        primary key,
    statPerks      int not null,
    primaryStyle   int not null,
    secondaryStyle int not null,
    constraint Perks_ibfk_1
        foreign key (statPerks) references StatPerks (id),
    constraint Perks_ibfk_2
        foreign key (primaryStyle) references PerksStyle (id),
    constraint Perks_ibfk_3
        foreign key (secondaryStyle) references PerksStyle (id)
);

create index primaryStyle
    on Perks (primaryStyle);

create index secondaryStyle
    on Perks (secondaryStyle);

create index statPerks
    on Perks (statPerks);

create index styleSelection1
    on PerksStyle (styleSelection1);

create index styleSelection2
    on PerksStyle (styleSelection2);

create index styleSelection3
    on PerksStyle (styleSelection3);

create index styleSelection4
    on PerksStyle (styleSelection4);

create table Summoners
(
    Id            varchar(63) not null,
    Puuid         varchar(78) not null
        primary key,
    Name          varchar(50) null,
    AccountId     varchar(56) null,
    ProfileIconId int         null,
    RevisionDate  bigint      null,
    Level         bigint      null,
    PlayerName    varchar(50) null,
    PlatformId    varchar(10) not null,
    constraint Summoners_ibfk_1
        foreign key (PlayerName) references Players (PlayerName)
);

create table Participants
(
    GameId         bigint      not null,
    SummonerPuuid  varchar(78) not null,
    Champion       int         not null,
    TeamId         int         not null,
    Kills          int         not null,
    Deaths         int         not null,
    Assists        int         not null,
    item0          int         not null,
    item1          int         not null,
    item2          int         not null,
    item3          int         not null,
    item4          int         not null,
    item5          int         not null,
    item6          int         not null,
    spellCast1     int         not null,
    spellCast2     int         not null,
    spellCast3     int         not null,
    spellCast4     int         not null,
    SummonerSpell1 int         not null,
    SummonerSpell2 int         not null,
    Perks          int         not null,
    TeamPosition   varchar(10) not null,
    primary key (GameId, SummonerPuuid),
    constraint Participants_ibfk_1
        foreign key (Perks) references Perks (id),
    constraint Participants_ibfk_2
        foreign key (SummonerPuuid) references Summoners (Puuid),
    constraint Participants_ibfk_3
        foreign key (GameId) references Games (GameId)
);

create index Perks
    on Participants (Perks);

create index SummonerPuuid
    on Participants (SummonerPuuid);

create index PlayerName
    on Summoners (PlayerName);

create procedure getGame(IN game_id bigint)
BEGIN
    SELECT GameDuration,
           GameStartTimestamp,
           G.GameId,
           GameVersion,
           GameType,
           MatchId,
           S.PlatformId,
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

create procedure insertAccount(IN p_Puuid varchar(78), IN p_GameName varchar(50), IN p_TagLine varchar(50))
BEGIN
    INSERT INTO Accounts (Puuid, GameName, TagLine)
    VALUES (p_Puuid, p_GameName, p_TagLine)
    ON DUPLICATE KEY UPDATE GameName = p_GameName, TagLine = p_TagLine;
END;

create procedure insertGame(IN p_GameId bigint, IN p_GameDuration int, IN p_GameStartTimestamp bigint,
                            IN p_GameVersion varchar(50), IN p_GameType varchar(50), IN p_PlatformId varchar(10),
                            IN p_Winner int, IN p_MatchId varchar(50))
BEGIN
    INSERT INTO Games
    VALUES (p_GameDuration, p_GameStartTimestamp, p_GameId, p_GameVersion, p_GameType, p_MatchId, p_PlatformId,
            p_Winner)
    ON DUPLICATE KEY UPDATE GameDuration       = p_GameDuration,
                            GameStartTimestamp = p_GameStartTimestamp,
                            GameVersion        = p_GameVersion,
                            GameType           = p_GameType,
                            MatchId            = p_MatchId,
                            PlatformId         = p_PlatformId,
                            Winner             = p_Winner;
END;

create procedure insertParticipant(IN p_GameId bigint, IN p_SummonerPuuid varchar(78), IN p_SummonerId varchar(63),
                                   IN p_GameName varchar(50), IN p_TagLine varchar(50), IN p_Champion int,
                                   IN p_TeamId int, IN p_Kills int, IN p_Deaths int, IN p_Assists int, IN p_Item0 int,
                                   IN p_Item1 int, IN p_Item2 int, IN p_Item3 int, IN p_Item4 int, IN p_Item5 int,
                                   IN p_Item6 int, IN p_SpellCast1 int, IN p_SpellCast2 int, IN p_SpellCast3 int,
                                   IN p_SpellCast4 int, IN p_SummonerSpell1 int, IN p_SummonerSpell2 int,
                                   IN p_Perks int, IN p_TeamPosition varchar(10), IN p_PlatformId varchar(10))
BEGIN
    CALL insertAccount(p_SummonerPuuid, p_GameName, p_TagLine);
    CALL insertSummoner(p_SummonerId, p_SummonerPuuid, p_GameName,
                        NULL, NULL, NULL, NULL, NULL, p_PlatformId);

    INSERT INTO Participants
    VALUES (p_GameId, p_SummonerPuuid, p_Champion, p_TeamId, p_Kills, p_Deaths, p_Assists,
            p_Item0, p_Item1, p_Item2, p_Item3, p_Item4, p_Item5, p_Item6,
            p_SpellCast1, p_SpellCast2, p_SpellCast3, p_SpellCast4,
            p_SummonerSpell1, p_SummonerSpell2, p_Perks, p_TeamPosition)
    ON DUPLICATE KEY UPDATE GameId = p_GameId;
END;

create procedure insertPerks(IN p_StatPerks int, IN p_PrimaryStyle int, IN p_SecondaryStyle int, OUT p_Id int)
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
END;

create procedure insertPerksStyle(IN p_Description varchar(50), IN p_Style int, IN p_StyleSelection1 int,
                                  IN p_StyleSelection2 int, IN p_StyleSelection3 int, IN p_StyleSelection4 int,
                                  OUT p_Id int)
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
END;

create procedure insertPlayer(IN p_PlayerName varchar(50), IN p_TwitchChannel varchar(50))
BEGIN
    INSERT INTO Players (PlayerName, TwitchChannel)
    VALUES (p_PlayerName, p_TwitchChannel)
    ON DUPLICATE KEY UPDATE
        TwitchChannel = VALUES(TwitchChannel);
END;

create procedure insertPlayerChampion(IN p_PlayerName varchar(50), IN p_Champion int, IN p_PlayRate float)
BEGIN
    INSERT INTO PlayerChampions (PlayerName, Champion, PlayRate)
    VALUES (p_PlayerName, p_Champion, p_PlayRate)
    ON DUPLICATE KEY UPDATE
        PlayRate = IFNULL(PlayRate, p_PlayRate);
END;

create procedure insertStatPerks(IN p_Defense int, IN p_Flex int, IN p_Offense int, OUT p_Id int)
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
END;

create procedure insertStyleSelection(IN p_Perk int, IN p_Var1 int, IN p_Var2 int, IN p_Var3 int, OUT p_Id int)
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
END;

create procedure insertSummoner(IN p_Id varchar(63), IN p_Puuid varchar(78), IN p_Name varchar(50),
                                IN p_AccountId varchar(56), IN p_ProfileIconId int, IN p_RevisionDate bigint,
                                IN p_Level bigint, IN p_PlayerName varchar(50), IN p_PlatformId varchar(50))
BEGIN
    INSERT INTO Summoners (Id, Puuid, Name, AccountId, ProfileIconId, RevisionDate, Level, PlayerName, PlatformId)
    VALUES (p_Id, p_Puuid, p_Name, p_AccountId, p_ProfileIconId, p_RevisionDate, p_Level, p_PlayerName, p_PlatformId)
    ON DUPLICATE KEY UPDATE Name          = IFNULL(Name, p_Name),
                            AccountId     = IFNULL(AccountId, p_AccountId),
                            ProfileIconId = IFNULL(ProfileIconId, p_ProfileIconId),
                            RevisionDate  = IFNULL(RevisionDate, p_RevisionDate),
                            Level         = IFNULL(Level, p_Level),
                            PlayerName    = IFNULL(PlayerName, p_PlayerName),
                            PlatformId    = IFNULL(PlatformId, p_PlatformId);
END;


