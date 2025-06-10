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
    constraint idx16444primary
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
    constraint idx16456primary
        primary key (id)
);

create table perks
(
    id               bigserial,
    statperksid      bigint not null,
    primarystyleid   bigint not null,
    secondarystyleid bigint not null,
    constraint idx16451primary
        primary key (id),
    constraint primarystyleperksstyles
        foreign key (primarystyleid) references perksstyles
            on update cascade on delete cascade,
    constraint secondarystyleperksstyles
        foreign key (secondarystyleid) references perksstyles
            on update cascade on delete cascade
);

create index idx16451secondarystyle
    on perks (secondarystyleid);

create index idx16451statperks
    on perks (statperksid);

create unique index idx16451idxstatperksunq
    on perks (statperksid, primarystyleid, secondarystyleid);

create index idx16451primarystyle
    on perks (primarystyleid);

create unique index idx16456idxperkstyleunique
    on perksstyles (description, style, styleselection1, styleselection2, styleselection3, styleselection4);

create index idx16456styleselection3
    on perksstyles (styleselection3);

create index idx16456styleselection4
    on perksstyles (styleselection4);

create index idx16456styleselection2
    on perksstyles (styleselection2);

create index idx16456styleselection1
    on perksstyles (styleselection1);

create table statperks
(
    id      bigserial,
    defense bigint not null,
    flex    bigint not null,
    offense bigint not null,
    constraint idx16464primary
        primary key (id)
);

create unique index idx16464uniquestats
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
    constraint idx16468primary
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
    constraint idx16447primary
        primary key (gameid, summonerpuuid),
    constraint participantsibfk1
        foreign key (perksid) references perks
            on update cascade on delete cascade,
    constraint participantsibfk2
        foreign key (summonerpuuid) references summoners
            on update cascade on delete cascade,
    constraint participantsibfk3
        foreign key (gameid) references games
            on update cascade on delete cascade
);

create index idx16447perks
    on participants (perksid);

create index idx16447summonerpuuid
    on participants (summonerpuuid);

create table players
(
    summonerpuuid varchar(78) not null,
    champion      bigint      not null,
    constraint idx16460primary
        primary key (summonerpuuid, champion),
    constraint playersibfk1
        foreign key (summonerpuuid) references summoners
            on update cascade on delete cascade
);

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

create or replace procedure insert_game(IN p_gameid bigint, IN p_gameduration integer, IN p_gamestarttimestamp bigint, IN p_gameversion character varying, IN p_gametype character varying, IN p_platformid character varying, IN p_winner integer, IN p_matchid character varying)
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

create or replace procedure insert_participant(IN p_gameid bigint, IN p_summonerpuuid character varying, IN p_summonerid character varying, IN p_summonerlevel bigint, IN p_summonername character varying, IN p_gamename character varying, IN p_tagline character varying, IN p_champion integer, IN p_teamid integer, IN p_kills integer, IN p_deaths integer, IN p_assists integer, IN p_item0 integer, IN p_item1 integer, IN p_item2 integer, IN p_item3 integer, IN p_item4 integer, IN p_item5 integer, IN p_item6 integer, IN p_spellcast1 integer, IN p_spellcast2 integer, IN p_spellcast3 integer, IN p_spellcast4 integer, IN p_summonerspell1 integer, IN p_summonerspell2 integer, IN p_perks integer, IN p_teamposition character varying, IN p_platformid character varying)
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
 summonerspell1, summonerspell2, perksid, teamposition)
VALUES
    (p_gameid, p_summonerpuuid, p_champion, p_teamid, p_kills, p_deaths, p_assists,
     p_item0, p_item1, p_item2, p_item3, p_item4, p_item5, p_item6,
     p_spellcast1, p_spellcast2, p_spellcast3, p_spellcast4,
     p_summonerspell1, p_summonerspell2, p_perks, p_teamposition)
ON CONFLICT (gameid, summonerpuuid)
DO UPDATE SET gameid = p_gameid;
END;
$$;

