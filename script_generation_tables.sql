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
        foreign key (Perks) references Perks (id)
            on update cascade on delete cascade,
    constraint Participants_ibfk_2
        foreign key (SummonerPuuid) references Summoners (Puuid)
            on update cascade on delete cascade,
    constraint Participants_ibfk_3
        foreign key (GameId) references Games (GameId)
            on update cascade on delete cascade
);

create index Perks
    on Participants (Perks);

create index SummonerPuuid
    on Participants (SummonerPuuid);

create trigger after_participants_delete
    after delete
    on Participants
    for each row
BEGIN
    UPDATE SummonnerChampionStats
    SET GamesPlayed = GamesPlayed - 1
    WHERE SummonerPuuid = OLD.SummonerPuuid
      AND Champion = OLD.Champion;

    DELETE
    FROM SummonnerChampionStats
    WHERE GamesPlayed = 0;
END;

create trigger after_participants_insert
    after insert
    on Participants
    for each row
BEGIN
    INSERT INTO SummonnerChampionStats (SummonerPuuid, Champion, GamesPlayed)
    VALUES (NEW.SummonerPuuid, NEW.Champion, 1)
    ON DUPLICATE KEY UPDATE GamesPlayed = GamesPlayed + 1;
END;

create trigger after_participants_update
    after update
    on Participants
    for each row
BEGIN
    IF (OLD.SummonerPuuid != NEW.SummonerPuuid OR OLD.Champion != NEW.Champion) THEN
        UPDATE SummonnerChampionStats
        SET GamesPlayed = GamesPlayed - 1
        WHERE SummonerPuuid = OLD.SummonerPuuid
          AND Champion = OLD.Champion;

        DELETE
        FROM SummonnerChampionStats
        WHERE GamesPlayed = 0;

        INSERT INTO SummonnerChampionStats (SummonerPuuid, Champion, GamesPlayed)
        VALUES (NEW.SummonerPuuid, NEW.Champion, 1)
        ON DUPLICATE KEY UPDATE GamesPlayed = GamesPlayed + 1;
    END IF;
END;

create table Perks
(
    id             int auto_increment
        primary key,
    statPerks      int not null,
    primaryStyle   int not null,
    secondaryStyle int not null,
    constraint idx_statPerks_unq
        unique (statPerks, primaryStyle, secondaryStyle),
    constraint Perks_ibfk_1
        foreign key (statPerks) references StatPerks (id)
            on update cascade on delete cascade,
    constraint Perks_ibfk_2
        foreign key (primaryStyle) references PerksStyle (id)
            on update cascade on delete cascade,
    constraint Perks_ibfk_3
        foreign key (secondaryStyle) references PerksStyle (id)
            on update cascade on delete cascade
);

create index primaryStyle
    on Perks (primaryStyle);

create index secondaryStyle
    on Perks (secondaryStyle);

create index statPerks
    on Perks (statPerks);

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
    constraint idx_perkstyle_unique
        unique (description, style, styleSelection1, styleSelection2, styleSelection3, styleSelection4)
);

create index styleSelection1
    on PerksStyle (styleSelection1);

create index styleSelection2
    on PerksStyle (styleSelection2);

create index styleSelection3
    on PerksStyle (styleSelection3);

create index styleSelection4
    on PerksStyle (styleSelection4);

create table Players
(
    SummonerPuuid varchar(78) not null,
    Champion      int         not null,
    primary key (SummonerPuuid, Champion),
    constraint Players_ibfk_1
        foreign key (SummonerPuuid) references Summoners (Puuid)
            on update cascade on delete cascade
);

create table StatPerks
(
    id      int auto_increment
        primary key,
    defense int not null,
    flex    int not null,
    offense int not null,
    constraint unique_stats
        unique (defense, flex, offense)
);

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
    PlatformId    varchar(10) not null,
    GameName      varchar(50) null,
    TagLine       varchar(5)  null
);

create table SummonnerChampionStats
(
    SummonerPuuid varchar(255) not null,
    Champion      varchar(255) not null,
    GamesPlayed   int          null,
    primary key (SummonerPuuid, Champion)
);

create table event_logs
(
    id         int auto_increment
        primary key,
    event_time timestamp default CURRENT_TIMESTAMP null,
    message    text                                null
);

create view championstats as
select P.Champion                                                                                  AS Champion,
       (count(0) / (select count(0) from participants where (participants.Champion = P.Champion))) AS winRate,
       count(0)                                                                                    AS GamesPlayed
from (participants P join games G on ((G.GameId = P.GameId)))
where (P.TeamId = G.Winner)
group by P.Champion
order by count(0);

create view findnewplayers as
select scp.SummonerPuuid    AS SummonerPuuid,
       scp.Champion         AS Champion,
       scp.GamesPlayed      AS GamesPlayed,
       scp.TotalGamesPlayed AS TotalGamesPlayed,
       scp.PlayRate         AS PlayRate
from summonerchampionplayrates SCP
where exists(select 1
             from players P
             where ((P.SummonerPuuid = scp.SummonerPuuid) and (P.Champion = scp.Champion))) is false
order by (scp.PlayRate * scp.GamesPlayed) desc;

create view gamesplayedbyplatformid as
select G.PlatformId AS PlatformId, count(0) AS GamesPlayed
from games G
group by G.PlatformId;

create view gamesview as
select G.GameDuration                 AS GameDuration,
       G.GameStartTimestamp           AS GamestartTimestamp,
       G.GameId                       AS GameId,
       G.GameVersion                  AS GameVersion,
       G.GameType                     AS GameType,
       G.MatchId                      AS MatchId,
       S.PlatformId                   AS PlatformId,
       G.Winner                       AS Winner,
       P.SummonerPuuid                AS SummonerPuuid,
       S.Id                           AS SummonerId,
       S.Name                         AS SummonerName,
       S.Level                        AS SummonerLevel,
       S.GameName                     AS GameName,
       S.TagLine                      AS TagLine,
       P.Champion                     AS Champion,
       P.TeamId                       AS TeamId,
       P.Kills                        AS Kills,
       P.Deaths                       AS Deaths,
       P.Assists                      AS Assists,
       P.item0                        AS item0,
       P.item1                        AS item1,
       P.item2                        AS item2,
       P.item3                        AS item3,
       P.item4                        AS item4,
       P.item5                        AS item5,
       P.item6                        AS item6,
       P.spellCast1                   AS spellCast1,
       P.spellCast2                   AS spellCast2,
       P.spellCast3                   AS spellCast3,
       P.spellCast4                   AS spellCast4,
       P.SummonerSpell1               AS Summonerspell1,
       P.SummonerSpell2               AS Summonerspell2,
       P.TeamPosition                 AS TeamPosition,
       statperks.defense              AS defense,
       statperks.flex                 AS flex,
       statperks.offense              AS offense,
       primaryStyle.description       AS primaryStyleDescription,
       primaryStyle.style             AS primaryStyle,
       primaryStyle.styleSelection1   AS primStyleSelection1,
       primaryStyle.styleSelection2   AS primStyleSelection2,
       primaryStyle.styleSelection3   AS primStyleSelection3,
       primaryStyle.styleSelection4   AS primStyleSelection4,
       secondaryStyle.description     AS secondaryStyleDescription,
       secondaryStyle.style           AS secondaryStyle,
       secondaryStyle.styleSelection1 AS secStyleSelection1,
       secondaryStyle.styleSelection2 AS secStyleSelection2
from ((((((games G join participants P on ((G.GameId = P.GameId))) join summoners S
          on ((P.SummonerPuuid = S.Puuid))) join perks P2 on ((P2.id = P.Perks))) join perksstyle primaryStyle
        on ((primaryStyle.id = P2.primaryStyle))) join perksstyle secondaryStyle
       on ((secondaryStyle.id = P2.secondaryStyle))) join statperks on ((statperks.id = P2.statPerks)));

create view lastgamestarttimestampbyplayerpuuids as
select distinct lst.SummonerPuuid          AS SummonerPuuid,
                lst.LastGamestartTimestamp AS LastGamestartTimestamp,
                lst.PlatformId             AS PlatformId
from (lastgamestarttimestampbysummoner LST join players P on ((lst.SummonerPuuid = P.SummonerPuuid)));

create view lastgamestarttimestampbysummoner as
select S.Puuid                                AS SummonerPuuid,
       coalesce(max(G.GameStartTimestamp), 0) AS LastGamestartTimestamp,
       S.PlatformId                           AS PlatformId
from ((summoners S left join participants P on ((S.Puuid = P.SummonerPuuid))) left join games G
      on ((P.GameId = G.GameId)))
group by S.Puuid;

create view playersstats as
select SCS.SummonerPuuid                               AS SummonerPuuid,
       SCS.Champion                                    AS Champion,
       SCS.GamesPlayed                                 AS GamesPlayed,
       totalgamesplayed.TotalGames                     AS TotalGamesPlayed,
       (SCS.GamesPlayed / totalgamesplayed.TotalGames) AS PlayRate
from (summonnerchampionstats SCS join totalgamesplayedbysummoner TotalGamesPlayed
      on ((SCS.SummonerPuuid = totalgamesplayed.SummonerPuuid)))
where SCS.SummonerPuuid in (select players.SummonerPuuid from players);

create view playerswithoutgames as
select distinct S.Id            AS Id,
                S.Puuid         AS Puuid,
                S.Name          AS Name,
                S.AccountId     AS AccountId,
                S.ProfileIconId AS ProfileIconId,
                S.RevisionDate  AS RevisionDate,
                S.Level         AS Level,
                S.PlatformId    AS PlatformId
from ((players P left join participants P2
       on (((P.SummonerPuuid = P2.SummonerPuuid) and (P2.Champion = P.Champion)))) join summoners S
      on ((P.SummonerPuuid = S.Puuid)))
where (P2.GameId is null);

create view sidewinrate as
select (blue.blueSide / (blue.blueSide + red.redSide)) AS blueWinRate,
       (red.redSide / (blue.blueSide + red.redSide))   AS redWinRate
from ((select count(0) AS blueSide from games where (games.Winner = '100')) blue join (select count(0) AS redSide from games where (games.Winner = '200')) red);

create view summonerchampionplayrates as
select SCS.SummonerPuuid                               AS SummonerPuuid,
       SCS.Champion                                    AS Champion,
       SCS.GamesPlayed                                 AS GamesPlayed,
       totalgamesplayed.TotalGames                     AS TotalGamesPlayed,
       (SCS.GamesPlayed / totalgamesplayed.TotalGames) AS PlayRate
from (summonnerchampionstats SCS join totalgamesplayedbysummoner TotalGamesPlayed
      on ((SCS.SummonerPuuid = totalgamesplayed.SummonerPuuid)));

create view summonersbyplatformid as
select S.PlatformId AS PlatformId, count(0) AS Count
from summoners S
group by S.PlatformId;

create view summonerstatsbychampion as
select P.SummonerPuuid AS SummonerPuuid,
       P.Champion      AS Champion,
       sum(P.Kills)    AS Kills,
       sum(P.Deaths)   AS Deaths,
       sum(P.Assists)  AS Assists,
       count(0)        AS GamesPlayed
from participants P
group by P.SummonerPuuid, P.Champion;

create view totalgamesplayedbysummoner as
select SCS.SummonerPuuid AS SummonerPuuid, sum(SCS.GamesPlayed) AS TotalGames
from summonnerchampionstats SCS
group by SCS.SummonerPuuid;

create procedure EventInsertNewPlayers()
BEGIN
    DECLARE rows_inserted INT DEFAULT 0;

    INSERT INTO players (SummonerPuuid, Champion)
    SELECT SummonerPuuid, Champion
    FROM findnewplayers
    WHERE TotalGamesPlayed >= 50
      AND playrate > 0.6;

    SET rows_inserted = ROW_COUNT();

    INSERT INTO event_logs (message)
    VALUES (CONCAT('Inserted ', rows_inserted, ' new players.'));
END;

create procedure EventRemoveInactivePlayers()
BEGIN
    DECLARE rows_deleted INT DEFAULT 0;

    DELETE
    FROM players
    WHERE (SummonerPuuid, Champion) IN (SELECT SummonerPuuid, Champion
                                        FROM summonerchampionplayrates
                                        WHERE TotalGamesPlayed > 35
                                          AND PlayRate < 0.15);

    SET rows_deleted = ROW_COUNT();

    INSERT INTO event_logs (message)
    VALUES (CONCAT('Deleted ', rows_deleted, ' inactive players.'));
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
                                   IN p_SummonerLevel bigint, IN p_SummonerName varchar(50), IN p_GameName varchar(50),
                                   IN p_TagLine varchar(50), IN p_Champion int, IN p_TeamId int, IN p_Kills int,
                                   IN p_Deaths int, IN p_Assists int, IN p_Item0 int, IN p_Item1 int, IN p_Item2 int,
                                   IN p_Item3 int, IN p_Item4 int, IN p_Item5 int, IN p_Item6 int, IN p_SpellCast1 int,
                                   IN p_SpellCast2 int, IN p_SpellCast3 int, IN p_SpellCast4 int,
                                   IN p_SummonerSpell1 int, IN p_SummonerSpell2 int, IN p_Perks int,
                                   IN p_TeamPosition varchar(10), IN p_PlatformId varchar(10))
BEGIN
    CALL insertSummoner(p_SummonerId, p_SummonerPuuid, p_SummonerName,
                        NULL, NULL, NULL, p_SummonerLevel, p_PlatformId, p_GameName, p_TagLine);

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
        VALUES (p_StatPerks, p_PrimaryStyle, p_SecondaryStyle)
        ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
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
        VALUES (p_Description, p_Style, p_StyleSelection1, p_StyleSelection2, p_StyleSelection3, p_StyleSelection4)
        ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END;

create procedure insertPlayer(IN p_SummonerPuuid varchar(78), IN p_Champion int)
BEGIN
    INSERT IGNORE INTO Players(SummonerPuuid, Champion) VALUES (p_SummonerPuuid, p_Champion);
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
        VALUES (p_Defense, p_Flex, p_Offense)
        ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
        SET p_Id = LAST_INSERT_ID();
    END IF;
END;

create procedure insertSummoner(IN p_Id varchar(63), IN p_Puuid varchar(78), IN p_Name varchar(50),
                                IN p_AccountId varchar(56), IN p_ProfileIconId int, IN p_RevisionDate bigint,
                                IN p_Level bigint, IN p_PlatformId varchar(50), IN p_GameName varchar(50),
                                IN p_TagLine varchar(5))
BEGIN
    INSERT INTO Summoners (Id, Puuid, Name, AccountId, ProfileIconId, RevisionDate, Level, PlatformId, GameName,
                           TagLine)
    VALUES (p_Id, p_Puuid, p_Name, p_AccountId, p_ProfileIconId, p_RevisionDate, p_Level, p_PlatformId, p_GameName,
            p_TagLine)
    ON DUPLICATE KEY UPDATE Name          = IFNULL(Name, p_Name),
                            AccountId     = IFNULL(AccountId, p_AccountId),
                            ProfileIconId = IFNULL(ProfileIconId, p_ProfileIconId),
                            RevisionDate  = IFNULL(RevisionDate, p_RevisionDate),
                            Level         = IFNULL(Level, p_Level),
                            PlatformId    = IFNULL(PlatformId, p_PlatformId),
                            GameName      = IFNULL(GameName, p_GameName),
                            TagLine       = IFNULL(TagLine, p_TagLine);
END;

create event insert_new_players on schedule
    every '1' DAY
        starts '2025-04-05 00:00:00'
    enable
    do
    BEGIN
        CALL EventInsertNewPlayers();
    END;

create event remove_inactive_players on schedule
    every '1' DAY
        starts '2025-04-05 00:00:00'
    enable
    do
    BEGIN
        CALL EventRemoveInactivePlayers();
    END;

