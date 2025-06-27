create table games
(
    gameduration       bigint      not null,
    gamestarttimestamp bigint      not null,
    gameid             bigint      not null,
    gameversion        varchar(50) not null,
    gametype           varchar(50) not null,
    matchid            varchar(50) not null,
    platformid         varchar(10) not null,
    winner             bigint      not null,
    constraint idx_16444_primary
        primary key (gameid)
);

create table perksstyles
(
    id              bigserial,
    description     varchar(50) not null,
    style           bigint      not null,
    styleselection1 bigint      not null,
    styleselection2 bigint      not null,
    styleselection3 bigint,
    styleselection4 bigint,
    constraint idx_16456_primary
        primary key (id)
);

create table perks
(
    id               bigserial,
    statperksid      bigint not null,
    primarystyleid   bigint not null,
    secondarystyleid bigint not null,
    constraint idx_16451_primary
        primary key (id),
    constraint primarystyle_perksstyles
        foreign key (primarystyleid) references perksstyles
            on update cascade on delete cascade,
    constraint secondarystyle_perksstyles
        foreign key (secondarystyleid) references perksstyles
            on update cascade on delete cascade
);

create index idx_16451_secondarystyle
    on perks (secondarystyleid);

create index idx_16451_statperks
    on perks (statperksid);

create unique index idx_16451_idx_statperks_unq
    on perks (statperksid, primarystyleid, secondarystyleid);

create index idx_16451_primarystyle
    on perks (primarystyleid);

create unique index idx_16456_idx_perkstyle_unique
    on perksstyles (description, style, styleselection1, styleselection2, styleselection3, styleselection4);

create index idx_16456_styleselection3
    on perksstyles (styleselection3);

create index idx_16456_styleselection4
    on perksstyles (styleselection4);

create index idx_16456_styleselection2
    on perksstyles (styleselection2);

create index idx_16456_styleselection1
    on perksstyles (styleselection1);

create table statperks
(
    id      bigserial,
    defense bigint not null,
    flex    bigint not null,
    offense bigint not null,
    constraint idx_16464_primary
        primary key (id)
);

create unique index idx_16464_unique_stats
    on statperks (defense, flex, offense);

create table summoners
(
    id            varchar(63) not null,
    puuid         varchar(78) not null,
    name          varchar(100),
    accountid     varchar(56),
    profileiconid bigint,
    revisiondate  bigint,
    level         bigint,
    platformid    varchar(10) not null,
    gamename      varchar(50),
    tagline       varchar(5),
    lastupdate    timestamp with time zone,
    constraint idx_16468_primary
        primary key (puuid)
);

create table participants
(
    gameid         bigint      not null,
    summonerpuuid  varchar(78) not null,
    champion       bigint      not null,
    teamid         bigint      not null,
    kills          bigint      not null,
    deaths         bigint      not null,
    assists        bigint      not null,
    item0          bigint      not null,
    item1          bigint      not null,
    item2          bigint      not null,
    item3          bigint      not null,
    item4          bigint      not null,
    item5          bigint      not null,
    item6          bigint      not null,
    spellcast1     bigint      not null,
    spellcast2     bigint      not null,
    spellcast3     bigint      not null,
    spellcast4     bigint      not null,
    summonerspell1 bigint      not null,
    summonerspell2 bigint      not null,
    perksid        bigint      not null,
    teamposition   varchar(10) not null,
    itemevents     jsonb,
    constraint idx_16447_primary
        primary key (gameid, summonerpuuid),
    constraint participants_ibfk_1
        foreign key (perksid) references perks
            on update cascade on delete cascade,
    constraint participants_ibfk_2
        foreign key (summonerpuuid) references summoners
            on update cascade on delete cascade,
    constraint participants_ibfk_3
        foreign key (gameid) references games
            on update cascade on delete cascade
);

create index idx_16447_perks
    on participants (perksid);

create index idx_16447_summonerpuuid
    on participants (summonerpuuid);

create table players
(
    summonerpuuid varchar(78) not null,
    champion      bigint      not null,
    constraint idx_16460_primary
        primary key (summonerpuuid, champion),
    constraint players_ibfk_1
        foreign key (summonerpuuid) references summoners
            on update cascade on delete cascade
);

create or replace view gamesview
            (gameduration, gamestarttimestamp, gameid, gameversion, gametype, matchid, platformid, winner,
             summonerpuuid, summonerid, summonername, summonerlevel, gamename, tagline, champion, teamid, kills, deaths,
             assists, item0, item1, item2, item3, item4, item5, item6, spellcast1, spellcast2, spellcast3, spellcast4,
             summonerspell1, summonerspell2, teamposition, itemevents, defense, flex, offense, primarystyledescription,
             primarystyle, primstyleselection1, primstyleselection2, primstyleselection3, primstyleselection4,
             secondarystyledescription, secondarystyle, secstyleselection1, secstyleselection2)
as
SELECT g.gameduration,
       g.gamestarttimestamp,
       g.gameid,
       g.gameversion,
       g.gametype,
       g.matchid,
       s.platformid,
       g.winner,
       p.summonerpuuid,
       s.id AS summonerid,
       s.name AS summonername,
       s.level AS summonerlevel,
       s.gamename,
       s.tagline,
       p.champion,
       p.teamid,
       p.kills,
       p.deaths,
       p.assists,
       p.item0,
       p.item1,
       p.item2,
       p.item3,
       p.item4,
       p.item5,
       p.item6,
       p.spellcast1,
       p.spellcast2,
       p.spellcast3,
       p.spellcast4,
       p.summonerspell1,
       p.summonerspell2,
       p.teamposition,
       p.itemevents,
       sp.defense,
       sp.flex,
       sp.offense,
       psprimary.description AS primarystyledescription,
       psprimary.style AS primarystyle,
       psprimary.styleselection1 AS primstyleselection1,
       psprimary.styleselection2 AS primstyleselection2,
       psprimary.styleselection3 AS primstyleselection3,
       psprimary.styleselection4 AS primstyleselection4,
       pssecondary.description AS secondarystyledescription,
       pssecondary.style AS secondarystyle,
       pssecondary.styleselection1 AS secstyleselection1,
       pssecondary.styleselection2 AS secstyleselection2
FROM games g
         JOIN participants p ON g.gameid = p.gameid
         JOIN summoners s ON p.summonerpuuid::text = s.puuid::text
         JOIN perks pk ON pk.id = p.perksid
         JOIN perksstyles psprimary ON psprimary.id = pk.primarystyleid
         JOIN perksstyles pssecondary ON pssecondary.id = pk.secondarystyleid
         JOIN statperks sp ON sp.id = pk.statperksid;

create or replace procedure insert_summoner(IN p_id character varying, IN p_puuid character varying, IN p_name character varying, IN p_accountid character varying, IN p_profileiconid integer, IN p_revisiondate bigint, IN p_level bigint, IN p_platformid character varying, IN p_gamename character varying, IN p_tagline character varying)
    language plpgsql
as
$$
BEGIN
    INSERT INTO summoners (id, puuid, name, accountid, profileiconid, revisiondate, level, platformid, gamename, tagline, lastupdate)
    VALUES (p_id, p_puuid, p_name, p_accountid, p_profileiconid, p_revisiondate, p_level, p_platformid, p_gamename, p_tagline, CURRENT_TIMESTAMP)
    ON CONFLICT (puuid)
        DO UPDATE SET
                      name = COALESCE(summoners.name, p_name),
                      accountid = COALESCE(summoners.accountid, p_accountid),
                      profileiconid = COALESCE(summoners.profileiconid, p_profileiconid),
                      revisiondate = COALESCE(summoners.revisiondate, p_revisiondate),
                      level = COALESCE(summoners.level, p_level),
                      platformid = COALESCE(summoners.platformid, p_platformid),
                      gamename = COALESCE(summoners.gamename, p_gamename),
                      tagline = COALESCE(summoners.tagline, p_tagline),
                      lastupdate = CURRENT_TIMESTAMP;
END;
$$;

create or replace procedure insert_statperks(IN p_defense integer, IN p_flex integer, IN p_offense integer, INOUT p_id integer)
    language plpgsql
as
$$
BEGIN
    SELECT id INTO p_id
    FROM statperks
    WHERE defense = p_defense
      AND flex = p_flex
      AND offense = p_offense;

    IF p_id IS NULL THEN
        INSERT INTO statperks (defense, flex, offense)
        VALUES (p_defense, p_flex, p_offense)
        RETURNING id INTO p_id;
    END IF;
END;
$$;

create or replace procedure insert_player(IN p_summonerpuuid character varying, IN p_champion integer)
    language plpgsql
as
$$
BEGIN
    INSERT INTO players(summonerpuuid, champion)
    VALUES (p_summonerpuuid, p_champion)
    ON CONFLICT DO NOTHING;
END;
$$;

create or replace procedure insert_perksstyle(IN p_description character varying, IN p_style integer, IN p_styleselection1 integer, IN p_styleselection2 integer, IN p_styleselection3 integer, IN p_styleselection4 integer, INOUT p_id integer)
    language plpgsql
as
$$
BEGIN
    SELECT id INTO p_id
    FROM perksstyles
    WHERE description = p_description
      AND style = p_style
      AND styleselection1 = p_styleselection1
      AND styleselection2 = p_styleselection2
      AND ((styleselection3 = p_styleselection3) OR (styleselection3 IS NULL AND p_styleselection3 IS NULL))
      AND ((styleselection4 = p_styleselection4) OR (styleselection4 IS NULL AND p_styleselection4 IS NULL))
    LIMIT 1;

    IF p_id IS NULL THEN
        INSERT INTO perksstyles (description, style, styleselection1, styleselection2, styleselection3, styleselection4)
        VALUES (p_description, p_style, p_styleselection1, p_styleselection2, p_styleselection3, p_styleselection4)
        RETURNING id INTO p_id;
    END IF;
END;
$$;

create or replace procedure insert_perks(IN p_statperks integer, IN p_primarystyle integer, IN p_secondarystyle integer, INOUT p_id integer)
    language plpgsql
as
$$
BEGIN
    SELECT id INTO p_id
    FROM perks
    WHERE statperksid = p_statperks
      AND primarystyleid = p_primarystyle
      AND secondarystyleid = p_secondarystyle;

    IF p_id IS NULL THEN
        INSERT INTO perks (statperksid, primarystyleid, secondarystyleid)
        VALUES (p_statperks, p_primarystyle, p_secondarystyle)
        RETURNING id INTO p_id;
    END IF;
END;
$$;

create or replace procedure insert_game(IN p_gameid bigint, IN p_gameduration bigint, IN p_gamestarttimestamp bigint, IN p_gameversion character varying, IN p_gametype character varying, IN p_platformid character varying, IN p_winner integer, IN p_matchid character varying)
    language plpgsql
as
$$
BEGIN
    INSERT INTO games (gameduration, gamestarttimestamp, gameid, gameversion, gametype, matchid, platformid, winner)
    VALUES (p_gameduration, p_gamestarttimestamp, p_gameid, p_gameversion, p_gametype, p_matchid, p_platformid, p_winner)
    ON CONFLICT (gameid)
        DO UPDATE SET
                      gameduration = p_gameduration,
                      gamestarttimestamp = p_gamestarttimestamp,
                      gameversion = p_gameversion,
                      gametype = p_gametype,
                      matchid = p_matchid,
                      platformid = p_platformid,
                      winner = p_winner;
END;
$$;

create or replace procedure insert_participant(IN p_gameid bigint, IN p_summonerpuuid character varying, IN p_summonerid character varying, IN p_summonerlevel bigint, IN p_summonername character varying, IN p_gamename character varying, IN p_tagline character varying, IN p_champion integer, IN p_teamid integer, IN p_kills integer, IN p_deaths integer, IN p_assists integer, IN p_item0 integer, IN p_item1 integer, IN p_item2 integer, IN p_item3 integer, IN p_item4 integer, IN p_item5 integer, IN p_item6 integer, IN p_spellcast1 integer, IN p_spellcast2 integer, IN p_spellcast3 integer, IN p_spellcast4 integer, IN p_summonerspell1 integer, IN p_summonerspell2 integer, IN p_perks integer, IN p_teamposition character varying, IN p_platformid character varying, IN p_itemevents jsonb)
    language plpgsql
as
$$
BEGIN
    CALL insert_summoner(p_summonerid, p_summonerpuuid, p_summonername,
                         NULL, NULL, NULL, p_summonerlevel, p_platformid, p_gamename, p_tagline);

    INSERT INTO participants
    (gameid, summonerpuuid, champion, teamid, kills, deaths, assists,
     item0, item1, item2, item3, item4, item5, item6,
     spellcast1, spellcast2, spellcast3, spellcast4,
     summonerspell1, summonerspell2, perksid, teamposition, itemevents)
    VALUES (p_gameid, p_summonerpuuid, p_champion, p_teamid, p_kills, p_deaths, p_assists,
            p_item0, p_item1, p_item2, p_item3, p_item4, p_item5, p_item6,
            p_spellcast1, p_spellcast2, p_spellcast3, p_spellcast4,
            p_summonerspell1, p_summonerspell2, p_perks, p_teamposition, p_itemevents)
    ON CONFLICT (gameid, summonerpuuid)
        DO UPDATE SET gameid         = p_gameid
                    , champion       = p_champion
                    , teamid         = p_teamid
                    , kills          = p_kills
                    , deaths         = p_deaths
                    , assists        = p_assists
                    , item0          = p_item0
                    , item1          = p_item1
                    , item2          = p_item2
                    , item3          = p_item3
                    , item4          = p_item4
                    , item5          = p_item5
                    , item6          = p_item6
                    , spellcast1     = p_spellcast1
                    , spellcast2     = p_spellcast2
                    , spellcast3     = p_spellcast3
                    , spellcast4     = p_spellcast4
                    , summonerspell1 = p_summonerspell1
                    , summonerspell2 = p_summonerspell2
                    , perksid        = p_perks
                    , teamposition   = p_teamposition
                    , itemevents     = p_itemevents;
END;
$$;