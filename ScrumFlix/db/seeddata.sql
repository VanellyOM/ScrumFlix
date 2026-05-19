-- Seed data file
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE ConcessionSaleItem;
TRUNCATE TABLE ConcessionSale;
TRUNCATE TABLE ConcessionItem;
TRUNCATE TABLE Ticket;
TRUNCATE TABLE Showtime;
TRUNCATE TABLE TheaterScreen;
-- TRUNCATE TABLE Movies;
TRUNCATE TABLE ScheduleAssignments;
TRUNCATE TABLE Shifts;
TRUNCATE TABLE TimeEntries;
TRUNCATE TABLE Timesheets;
TRUNCATE TABLE PayStubs;
TRUNCATE TABLE Payrolls;
TRUNCATE TABLE PayPeriods;
TRUNCATE TABLE Users;
TRUNCATE TABLE Employees;
TRUNCATE TABLE Location;

SET FOREIGN_KEY_CHECKS = 1;

/* INSERT INTO Movies (Title, Rating, Genre, RuntimeMinutes, Description) VALUES
('Eclipse War', 'PG-13', 'Action', 120, 'A sci-fi war across collapsing dimensions.'),
('Neon Shadows', 'R', 'Action', 110, 'A cyberpunk detective uncovers corruption.'),
('Ocean’s Secret', 'PG', 'Action', 95, 'A family discovers a hidden underwater world.'),
('Chainsaw Man: The Reze Arc', 'R', 'Action', 130, 'For the first time, Chainsaw Man slashes his way onto the big screen in an epic, action-fueled adventure that continues the hugely popular anime series. Denji worked as a Devil Hunter for the yakuza, trying to pay off the debt he inherited from his parents, until the yakuza betrayed him and had him killed. As he was losing consciousness, Denji\'s beloved chainsaw-powered devil-dog, Pochita, made a deal with Denji and saved his life. This fused the two together, creating the unstoppable Chainsaw Man. Now, in a brutal war between devils, hunters, and secret enemies, a mysterious girl named Reze has stepped into his world, and Denji faces his deadliest battle yet, fueled by love in a world where survival knows no rules.'),
('Skybound', 'PG-13', 'Action', 105, 'A pilot fights to save a falling aircraft.'),
('Lost Kingdom', 'PG-13', 'Action', 140, 'An adventurer finds a forgotten empire.'),
('Code Zero', 'PG-13', 'Action', 100, 'Hackers race to stop a global shutdown.'),
('Silent Echo', 'PG', 'Action', 90, 'A mystery unfolds in a quiet town.'),
('Inferno Run', 'R', 'Action', 115, 'A high-speed chase through a burning city.'),
('Dream Circuit', 'PG', 'Action', 102, 'A teen enters a digital dream world.'),


('Midnight Protocol', 'PG-13', 'Action', 112, 'A rogue agent races against time to stop a covert global assassination network.'),
('Iron Horizon', 'PG-13', 'Action', 125, 'A group of rebels battles a militarized corporation controlling Earth’s last resources.'),
('Phantom Strike', 'R', 'Action', 118, 'An elite sniper is pulled into a deadly conspiracy after a mission goes wrong.'),
('Crimson Tidefall', 'PG-13', 'Action', 108, 'A naval officer fights to prevent a catastrophic war at sea.'),
('Velocity Point', 'PG-13', 'Action', 104, 'Street racers uncover a smuggling ring tied to high-speed competitions.'),
('Blackout City', 'R', 'Action', 122, 'When power grids fail, a former cop navigates chaos to protect his family.'),
('Thunderline', 'PG-13', 'Action', 109, 'A train hijacking spirals into a nationwide crisis.'),
('Steel Reign', 'PG-13', 'Action', 131, 'A futuristic soldier leads a revolt against AI-controlled governments.'),
('Nightfall Unit', 'R', 'Action', 117, 'A covert task force is sent on a mission that reveals hidden betrayals.'),
('Rogue Velocity', 'PG-13', 'Action', 106, 'A getaway driver is forced into one last job with deadly consequences.'),

('The Last Signal', 'PG', 'Drama', 97, 'A grieving father finds meaning through mysterious radio transmissions.'),
('Golden Silence', 'PG-13', 'Drama', 110, 'A pianist struggles to perform after losing her hearing.'),
('Paper Bridges', 'PG', 'Drama', 102, 'Two strangers form a bond while rebuilding their lives after tragedy.'),
('Harbor Lights', 'PG', 'Drama', 95, 'A small-town fisherman faces life-changing decisions during a harsh winter.'),
('Broken Compass', 'PG-13', 'Drama', 108, 'A traveler searches for purpose after losing everything.'),
('Stillwater Road', 'PG', 'Drama', 100, 'A family reconnects while uncovering secrets from their past.'),
('Echoes of June', 'PG-13', 'Drama', 112, 'A woman revisits her hometown to confront unresolved memories.'),
('Second Chances', 'PG', 'Drama', 98, 'A former athlete mentors troubled youth in a struggling community.'),
('Fading Colors', 'PG-13', 'Drama', 115, 'An artist battles personal demons while chasing inspiration.'),
('Open Skies', 'PG', 'Drama', 101, 'A pilot rediscovers life through unexpected friendships.'),

('Laugh Track', 'PG', 'Comedy', 92, 'A failed comedian accidentally becomes an overnight sensation.'),
('Office Mayhem', 'PG-13', 'Comedy', 97, 'Coworkers compete in absurd ways for a promotion.'),
('Roommate Roulette', 'PG', 'Comedy', 95, 'A college student deals with increasingly bizarre roommates.'),
('Wedding Crash Plan', 'PG-13', 'Comedy', 104, 'A group of friends sabotage a wedding for the right reasons.'),
('Vacation Disaster', 'PG', 'Comedy', 99, 'A family trip spirals into chaos across multiple countries.'),
('Neighbor Wars', 'PG-13', 'Comedy', 101, 'Two neighbors escalate a petty feud into full-scale warfare.'),
('Pet Problems', 'PG', 'Comedy', 88, 'A man struggles to manage a house full of mischievous animals.'),
('Comedy of Errors', 'PG', 'Comedy', 93, 'A series of misunderstandings leads to hilarious consequences.'),
('Startup Shenanigans', 'PG-13', 'Comedy', 102, 'Tech founders fake success to secure funding.'),
('Late Night Mishaps', 'PG', 'Comedy', 94, 'A talk show host deals with unpredictable guests.'),

('Haunted Hollow', 'PG-13', 'Horror', 105, 'A group of teens explores a town cursed by a forgotten legend.'),
('The Red Door', 'R', 'Horror', 110, 'Opening a mysterious door unleashes terrifying consequences.'),
('Shadow House', 'PG-13', 'Horror', 102, 'A family moves into a home that watches them back.'),
('Echoes Below', 'R', 'Horror', 108, 'Explorers uncover something ancient beneath the earth.'),
('Night Whisper', 'PG-13', 'Horror', 99, 'Voices in the dark guide a woman toward danger.'),
('Blood Moon Rising', 'R', 'Horror', 115, 'A rare lunar event awakens something deadly.'),
('The Forgotten Room', 'PG-13', 'Horror', 101, 'A hidden room reveals a sinister past.'),
('Graveyard Shift', 'R', 'Horror', 97, 'Workers encounter horrors during a late-night shift.'),
('Silent Screams', 'PG-13', 'Horror', 103, 'A town hides its darkest secrets behind silence.'),
('Dark Reflection', 'R', 'Horror', 109, 'Mirrors begin showing a terrifying alternate reality.');






-- REAL MOVIES
INSERT INTO Movies (Title, Rating, Genre, RuntimeMinutes, Description) VALUES
('Sinners', 'R', 'Horror', 137, 'A supernatural horror film following twin brothers returning to their hometown.'),
('Thunderbolts*', 'PG-13', 'Action', 126, 'A team of antiheroes is assembled for dangerous government missions.'),
('Captain America: Brave New World', 'PG-13', 'Action', 118, 'Sam Wilson takes on the mantle of Captain America during a global crisis.'),
('Mickey 17', 'R', 'Sci-Fi', 139, 'An expendable worker on a distant colony repeatedly dies and regenerates.'),
('A Minecraft Movie', 'PG', 'Adventure', 101, 'A group of heroes must save the Overworld from destruction.'),
('Mission: Impossible – The Final Reckoning', 'PG-13', 'Action', 163, 'Ethan Hunt faces his most dangerous mission yet.'),
('The Amateur', 'PG-13', 'Thriller', 123, 'A CIA analyst seeks revenge after a personal tragedy.'),
('Final Destination: Bloodlines', 'R', 'Horror', 109, 'Death returns to haunt a new generation of survivors.'),
('Warfare', 'R', 'War', 95, 'A gritty modern combat drama following soldiers behind enemy lines.'),
('Lilo & Stitch', 'PG', 'Family', 108, 'A live-action retelling of the alien friendship story in Hawaii.'),
('The Accountant 2', 'R', 'Action', 131, 'Christian Wolff returns to uncover a deadly financial conspiracy.'),
('Karate Kid: Legends', 'PG-13', 'Drama', 117, 'A new generation learns martial arts under legendary mentors.'),
('How to Train Your Dragon', 'PG', 'Fantasy', 125, 'A live-action adaptation of the Viking and dragon friendship story.'),
('28 Years Later', 'R', 'Horror', 126, 'Humanity struggles to survive decades after the rage virus outbreak.'),
('Jurassic World Rebirth', 'PG-13', 'Adventure', 141, 'Dinosaurs once again threaten humanity after genetic experiments fail.'),
('Superman', 'PG-13', 'Action', 134, 'Clark Kent balances heroism and humanity in a changing world.'),
('Fantastic Four', 'PG-13', 'Sci-Fi', 128, 'Marvels first family gains powers after a scientific accident.'),
('The Naked Gun', 'PG-13', 'Comedy', 102, 'A reboot of the classic slapstick police comedy franchise.'),
('Blade', 'R', 'Action', 129, 'The vampire hunter returns to battle supernatural threats.'),
('Avatar: Fire and Ash', 'PG-13', 'Sci-Fi', 188, 'Jake Sully faces a dangerous new tribe on Pandora.'),
('Michael', 'PG-13', 'Biography', 150, 'A biopic exploring the life and music career of Michael Jackson.'),
('Scream 7', 'R', 'Horror', 114, 'Ghostface returns to terrorize another group of survivors.'),
('Project Hail Mary', 'PG-13', 'Sci-Fi', 146, 'A lone astronaut attempts to save humanity from extinction.'),
('The Super Mario Galaxy Movie', 'PG', 'Animation', 109, 'Mario embarks on a cosmic adventure across galaxies.'),
('Hoppers', 'PG', 'Animation', 97, 'A young inventor discovers technology allowing humans to inhabit robot animals.'),
('The Devil Wears Prada 2', 'PG-13', 'Comedy', 124, 'Miranda Priestly returns to the fashion world amid industry changes.'),
('Mortal Kombat II', 'R', 'Action', 121, 'Earthrealm fighters enter the deadly Mortal Kombat tournament.'),
('Animal Farm', 'PG-13', 'Drama', 112, 'A modern adaptation of George Orwells political allegory.'),
('The Mandalorian & Grogu', 'PG-13', 'Sci-Fi', 132, 'Din Djarin and Grogu embark on a new galactic adventure.'),
('Send Help', 'R', 'Thriller', 113, 'Plane crash survivors struggle to escape a remote island.'),
('Mother Mary', 'R', 'Drama', 137, 'A pop star and fashion designer confront fame and obsession.'),
('Supergirl', 'PG-13', 'Action', 126, 'Kara Zor-El protects Earth while finding her place among heroes.'),
('Spider-Man: Brand New Day', 'PG-13', 'Action', 131, 'Peter Parker starts fresh after losing everything.'),
('Dune: Part Three', 'PG-13', 'Sci-Fi', 166, 'Paul Atreides faces the consequences of his rise to power.'),
('The Odyssey', 'PG-13', 'Adventure', 170, 'An epic adaptation of Homers legendary journey home.'),
('Avengers: Doomsday', 'PG-13', 'Action', 178, 'Earths mightiest heroes unite against a multiversal threat.'),
('The Hunger Games: Sunrise on the Reaping', 'PG-13', 'Sci-Fi', 142, 'A prequel exploring the rise of Haymitch Abernathy.'),
('Lee Cronins The Mummy', 'R', 'Horror', 124, 'An ancient evil awakens beneath modern civilization.'),
('Reminders of Him', 'PG-13', 'Drama', 118, 'A woman seeks redemption after serving time in prison.'),
('Return to Silent Hill', 'R', 'Horror', 127, 'A man searches for his lost love in the haunted town of Silent Hill.'),
('The Strangers: Chapter 3', 'R', 'Horror', 101, 'Masked killers continue their reign of terror.'),
('GOAT', 'PG-13', 'Sports', 116, 'A rising athlete fights to become the greatest of all time.'),
('Blue Heron', 'PG', 'Drama', 104, 'A family reconnects during a summer in rural Canada.'),
('You, Me & Tuscany', 'PG-13', 'Romance', 111, 'Two strangers fall in love while traveling through Italy.'),
('Deep Water', 'R', 'Thriller', 115, 'A detective uncovers secrets surrounding a series of disappearances.'),
('Remarkably Bright Creatures', 'PG', 'Drama', 119, 'An unlikely friendship forms at an aquarium.'),
('Backrooms', 'PG-13', 'Horror', 103, 'A group becomes trapped in an endless maze of eerie rooms.'),
('Power Ballad', 'PG-13', 'Comedy', 107, 'A washed-up musician attempts one final comeback tour.'),
('The Sheep Detectives', 'PG', 'Animation', 92, 'A flock of sheep investigates a mysterious crime in the countryside.'),
('Cold War 1994', 'R', 'Action', 128, 'Agents race to stop an international conspiracy during political unrest.'); */




INSERT INTO Location (LocationName, LocationAddress) VALUES
('Alpha Theater', '123 Main St, Dallas, TX 75234'),
('Beta Theater', '456 Elm St, Dallas, TX 75234'),
('Gamma Theater', '111 Street St, Dallas, TX 75234'),
('Delta Theater', '321 Windsor St, Dallas, TX 75234'),
('Epsilon Theater', '611 Penn St, Dallas, TX 75234'),
('Zeta Theater', '44 West St, Dallas, TX 75234');





INSERT INTO TheaterScreen (LocationId, ScreenName, Capacity) VALUES
-- Alpha Theater
(1, 'A Screen 1 Small', 50),
(1, 'A Screen 2 Small', 50),
(1, 'A Screen 3 Small', 50),
(1, 'A Screen 4 Small', 50),
(1, 'A Screen 5 Small', 50),
(1, 'A Screen 6 Medium', 100),
(1, 'A Screen 7 Medium', 100),
(1, 'A Screen 8 Medium', 100),
(1, 'A Screen 9 Medium', 100),
(1, 'A Screen 10 Medium', 100),
(1, 'A Screen 11 Medium', 100),
(1, 'A Screen 12 Medium', 100),
(1, 'A Screen 13 Medium', 100),
(1, 'A Screen 14 Medium', 100),
(1, 'A Screen 15 Medium', 100),
(1, 'A Screen 16 Large', 175),
(1, 'A Screen 17 Large', 175),
(1, 'A Screen 18 Large', 175),
(1, 'A Screen 19 Large', 175),
(1, 'A Screen 20 Large', 175),

-- Beta Theater
(2, 'B Screen 1 Small', 50),
(2, 'B Screen 2 Small', 50),
(2, 'B Screen 3 Small', 50),
(2, 'B Screen 4 Small', 50),
(2, 'B Screen 5 Small', 50),
(2, 'B Screen 6 Medium', 100),
(2, 'B Screen 7 Medium', 100),
(2, 'B Screen 8 Medium', 100),
(2, 'B Screen 9 Medium', 100),
(2, 'B Screen 10 Medium', 100),
(2, 'B Screen 11 Medium', 100),
(2, 'B Screen 12 Medium', 100),
(2, 'B Screen 13 Medium', 100),
(2, 'B Screen 14 Medium', 100),
(2, 'B Screen 15 Medium', 100),
(2, 'B Screen 16 Large', 175),
(2, 'B Screen 17 Large', 175),
(2, 'B Screen 18 Large', 175),
(2, 'B Screen 19 Large', 175),
(2, 'B Screen 20 Large', 175),

-- Gamma Theater
(3, 'G Screen 1 Small', 50),
(3, 'G Screen 2 Small', 50),
(3, 'G Screen 3 Small', 50),
(3, 'G Screen 4 Small', 50),
(3, 'G Screen 5 Small', 50),
(3, 'G Screen 6 Medium', 100),
(3, 'G Screen 7 Medium', 100),
(3, 'G Screen 8 Medium', 100),
(3, 'G Screen 9 Medium', 100),
(3, 'G Screen 10 Medium', 100),
(3, 'G Screen 11 Medium', 100),
(3, 'G Screen 12 Medium', 100),
(3, 'G Screen 13 Medium', 100),
(3, 'G Screen 14 Medium', 100),
(3, 'G Screen 15 Medium', 100),
(3, 'G Screen 16 Large', 175),
(3, 'G Screen 17 Large', 175),
(3, 'G Screen 18 Large', 175),
(3, 'G Screen 19 Large', 175),
(3, 'G Screen 20 Large', 175),

-- Delta Theater
(4, 'D Screen 1 Small', 50),
(4, 'D Screen 2 Small', 50),
(4, 'D Screen 3 Small', 50),
(4, 'D Screen 4 Small', 50),
(4, 'D Screen 5 Small', 50),
(4, 'D Screen 6 Medium', 100),
(4, 'D Screen 7 Medium', 100),
(4, 'D Screen 8 Medium', 100),
(4, 'D Screen 9 Medium', 100),
(4, 'D Screen 10 Medium', 100),
(4, 'D Screen 11 Medium', 100),
(4, 'D Screen 12 Medium', 100),
(4, 'D Screen 13 Medium', 100),
(4, 'D Screen 14 Medium', 100),
(4, 'D Screen 15 Medium', 100),
(4, 'D Screen 16 Large', 175),
(4, 'D Screen 17 Large', 175),
(4, 'D Screen 18 Large', 175),
(4, 'D Screen 19 Large', 175),
(4, 'D Screen 20 Large', 175),

-- Epsilon Theater
(5, 'E Screen 1 Small', 50),
(5, 'E Screen 2 Small', 50),
(5, 'E Screen 3 Small', 50),
(5, 'E Screen 4 Small', 50),
(5, 'E Screen 5 Small', 50),
(5, 'E Screen 6 Medium', 100),
(5, 'E Screen 7 Medium', 100),
(5, 'E Screen 8 Medium', 100),
(5, 'E Screen 9 Medium', 100),
(5, 'E Screen 10 Medium', 100),
(5, 'E Screen 11 Medium', 100),
(5, 'E Screen 12 Medium', 100),
(5, 'E Screen 13 Medium', 100),
(5, 'E Screen 14 Medium', 100),
(5, 'E Screen 15 Medium', 100),
(5, 'E Screen 16 Large', 175),
(5, 'E Screen 17 Large', 175),
(5, 'E Screen 18 Large', 175),
(5, 'E Screen 19 Large', 175),
(5, 'E Screen 20 Large', 175),

-- Zeta Theater
(6, 'Z Screen 1 Small', 50),
(6, 'Z Screen 2 Small', 50),
(6, 'Z Screen 3 Small', 50),
(6, 'Z Screen 4 Small', 50),
(6, 'Z Screen 5 Small', 50),
(6, 'Z Screen 6 Medium', 100),
(6, 'Z Screen 7 Medium', 100),
(6, 'Z Screen 8 Medium', 100),
(6, 'Z Screen 9 Medium', 100),
(6, 'Z Screen 10 Medium', 100),
(6, 'Z Screen 11 Medium', 100),
(6, 'Z Screen 12 Medium', 100),
(6, 'Z Screen 13 Medium', 100),
(6, 'Z Screen 14 Medium', 100),
(6, 'Z Screen 15 Medium', 100),
(6, 'Z Screen 16 Large', 175),
(6, 'Z Screen 17 Large', 175),
(6, 'Z Screen 18 Large', 175),
(6, 'Z Screen 19 Large', 175),
(6, 'Z Screen 20 Large', 175);







DELIMITER //

DROP PROCEDURE IF EXISTS SeedShowtimes_MarAprMay2026 //

CREATE PROCEDURE SeedShowtimes_MarAprMay2026()
BEGIN
    DECLARE currentDate DATE;
    DECLARE screenIndex INT DEFAULT 1;
    DECLARE screenCount INT;
    DECLARE movieCount INT;
    DECLARE currentStart DATETIME;
    DECLARE nextStart DATETIME;
    DECLARE movieRow INT;
    DECLARE selectedMovieId INT;
    DECLARE selectedRuntime INT;
    DECLARE selectedScreenId INT;
    DECLARE selectedCapacity INT;
    DECLARE selectedPrice DECIMAL(10,2);
    DECLARE showNumber INT;
    DECLARE dayOffset INT;

    DROP TEMPORARY TABLE IF EXISTS TempScreens;
    DROP TEMPORARY TABLE IF EXISTS TempMovies;

    CREATE TEMPORARY TABLE TempScreens (
		rn INT PRIMARY KEY,
		TheaterScreenId INT,
		Capacity INT,
		PricePerTicket DECIMAL(10,2)
	);

	INSERT INTO TempScreens
	SELECT
		ROW_NUMBER() OVER (ORDER BY TheaterScreenId) AS rn,
		TheaterScreenId,
		Capacity,
		CASE
			WHEN ScreenName LIKE '%Small%' THEN 10.00
			WHEN ScreenName LIKE '%Medium%' THEN 12.00
			WHEN ScreenName LIKE '%Large%' THEN 14.00
			ELSE 10.00
		END AS PricePerTicket
	FROM TheaterScreen
	WHERE IsActive = TRUE;

    CREATE TEMPORARY TABLE TempMovies (
		rn INT PRIMARY KEY,
		MovieId INT,
		RuntimeMinutes INT
	);

	INSERT INTO TempMovies
	SELECT
		ROW_NUMBER() OVER (ORDER BY MovieId) AS rn,
		MovieId,
		RuntimeMinutes
	FROM Movies;

    SELECT COUNT(*) INTO screenCount FROM TempScreens;
    SELECT COUNT(*) INTO movieCount FROM TempMovies;

    SET currentDate = '2026-03-01';

    WHILE currentDate <= '2026-05-31' DO
        SET dayOffset = DATEDIFF(currentDate, '2026-03-01');
        SET screenIndex = 1;

        WHILE screenIndex <= screenCount DO
            SELECT TheaterScreenId, Capacity, PricePerTicket
            INTO selectedScreenId, selectedCapacity, selectedPrice
            FROM TempScreens
            WHERE rn = screenIndex;

            SET currentStart = TIMESTAMP(currentDate, '09:00:00');
            SET showNumber = 0;

            WHILE currentStart < TIMESTAMP(DATE_ADD(currentDate, INTERVAL 1 DAY), '00:00:00') DO
                SET movieRow = MOD(screenIndex + dayOffset + showNumber - 1, movieCount) + 1;

                SELECT MovieId, RuntimeMinutes
                INTO selectedMovieId, selectedRuntime
                FROM TempMovies
                WHERE rn = movieRow;

                INSERT INTO Showtime (MovieId, TheaterScreenId, StartTime, Capacity, PricePerTicket)
                VALUES (selectedMovieId, selectedScreenId, currentStart, selectedCapacity, selectedPrice);

                SET nextStart = DATE_ADD(currentStart, INTERVAL (selectedRuntime + 30) MINUTE);

                SET nextStart = DATE_ADD(
                    nextStart,
                    INTERVAL MOD(5 - MOD(MINUTE(nextStart), 5), 5) MINUTE
                );

                SET currentStart = nextStart;
                SET showNumber = showNumber + 1;
            END WHILE;

            SET screenIndex = screenIndex + 1;
        END WHILE;

        SET currentDate = DATE_ADD(currentDate, INTERVAL 1 DAY);
    END WHILE;
END //

DELIMITER ;

CALL SeedShowtimes_MarAprMay2026();

DROP PROCEDURE IF EXISTS SeedShowtimes_MarAprMay2026;








INSERT INTO Employees (FirstName, MiddleName, LastName, DOB, Phone, Email, Address, PayRate, LocationId) VALUES
('Gilben', 'Oxymoron', 'Herberth', '1998-03-12', '2145551001', 'gilben.herberth@scrumflix.com', '101 Alpha Ave, Dallas, TX 75234', 350.00, 1),
('Mia', 'Rose', 'Bennett', '1999-07-24', '2145551002', 'mia.bennett@scrumflix.com', '102 Alpha Ave, Dallas, TX 75234', 17.00, 1),
('Caleb', 'Dean', 'Foster', '2001-11-08', '2145551003', 'caleb.foster@scrumflix.com', '103 Alpha Ave, Dallas, TX 75234', 15.00, 1),
('Nora', 'Grace', 'Reed', '2000-05-19', '2145551004', 'nora.reed@scrumflix.com', '104 Alpha Ave, Dallas, TX 75234', 24.00, 1),
('Ethan', 'Cole', 'Murphy', '1997-09-30', '2145551005', 'ethan.murphy@scrumflix.com', '105 Alpha Ave, Dallas, TX 75234', 17.00, 1),
('Lily', 'Mae', 'Hayes', '2002-01-14', '2145551006', 'lily.hayes@scrumflix.com', '106 Alpha Ave, Dallas, TX 75234', 15.00, 1),
('Brandon', 'Lee', 'Turner', '1998-04-11', '2145551011', 'brandon.turner@scrumflix.com', '111 Alpha Ave, Dallas, TX 75234', 15.00, 1),
('Kaylee', 'Marie', 'Evans', '2001-09-22', '2145551012', 'kaylee.evans@scrumflix.com', '112 Alpha Ave, Dallas, TX 75234', 17.00, 1),
('Jordan', 'Michael', 'Cruz', '1999-01-30', '2145551013', 'jordan.cruz@scrumflix.com', '113 Alpha Ave, Dallas, TX 75234', 24.00, 1),
('Madelyn', 'Grace', 'Phillips', '2000-07-14', '2145551014', 'madelyn.phillips@scrumflix.com', '114 Alpha Ave, Dallas, TX 75234', 17.00, 1),

('Owen', 'Miles', 'Parker', '1998-04-03', '2145552001', 'owen.parker@scrumflix.com', '201 Beta Blvd, Dallas, TX 75234', 15.00, 2),
('Zoe', 'Claire', 'Sanders', '2001-08-21', '2145552002', 'zoe.sanders@scrumflix.com', '202 Beta Blvd, Dallas, TX 75234', 17.00, 2),
('Logan', 'Scott', 'Brooks', '1999-12-09', '2145552003', 'logan.brooks@scrumflix.com', '203 Beta Blvd, Dallas, TX 75234', 24.00, 2),
('Ella', 'June', 'Peterson', '2000-06-17', '2145552004', 'ella.peterson@scrumflix.com', '204 Beta Blvd, Dallas, TX 75234', 15.00, 2),
('Mason', 'Luke', 'Price', '1997-10-25', '2145552005', 'mason.price@scrumflix.com', '205 Beta Blvd, Dallas, TX 75234', 17.00, 2),
('Avery', 'Skye', 'Cooper', '2002-02-11', '2145552006', 'avery.cooper@scrumflix.com', '206 Beta Blvd, Dallas, TX 75234', 15.00, 2),
('Carter', 'Blake', 'Russell', '1998-07-29', '2145552007', 'carter.russell@scrumflix.com', '207 Beta Blvd, Dallas, TX 75234', 24.00, 2),
('Sofia', 'Elise', 'Ward', '2001-03-06', '2145552008', 'sofia.ward@scrumflix.com', '208 Beta Blvd, Dallas, TX 75234', 17.00, 2),
('Jackson', 'Ray', 'Coleman', '1999-09-13', '2145552009', 'jackson.coleman@scrumflix.com', '209 Beta Blvd, Dallas, TX 75234', 15.00, 2),
('Harper', 'Lynn', 'Bailey', '2000-11-22', '2145552010', 'harper.bailey@scrumflix.com', '210 Beta Blvd, Dallas, TX 75234', 17.00, 2),

('Lucas', 'Ryan', 'Hughes', '1998-01-05', '2145553001', 'lucas.hughes@scrumflix.com', '301 Gamma Rd, Dallas, TX 75234', 15.00, 3),
('Aria', 'Faith', 'Morgan', '2001-04-18', '2145553002', 'aria.morgan@scrumflix.com', '302 Gamma Rd, Dallas, TX 75234', 17.00, 3),
('Henry', 'Paul', 'Rivera', '1999-08-27', '2145553003', 'henry.rivera@scrumflix.com', '303 Gamma Rd, Dallas, TX 75234', 24.00, 3),
('Chloe', 'Paige', 'Kelly', '2000-12-15', '2145553004', 'chloe.kelly@scrumflix.com', '304 Gamma Rd, Dallas, TX 75234', 15.00, 3),
('Wyatt', 'Grant', 'Howard', '1997-05-09', '2145553005', 'wyatt.howard@scrumflix.com', '305 Gamma Rd, Dallas, TX 75234', 17.00, 3),
('Riley', 'Hope', 'Cox', '2002-09-02', '2145553006', 'riley.cox@scrumflix.com', '306 Gamma Rd, Dallas, TX 75234', 15.00, 3),
('Julian', 'Mark', 'Torres', '1998-11-30', '2145553007', 'julian.torres@scrumflix.com', '307 Gamma Rd, Dallas, TX 75234', 24.00, 3),
('Layla', 'Nicole', 'Bell', '2001-06-10', '2145553008', 'layla.bell@scrumflix.com', '308 Gamma Rd, Dallas, TX 75234', 17.00, 3),
('Nathan', 'Joel', 'Ramirez', '1999-02-26', '2145553009', 'nathan.ramirez@scrumflix.com', '309 Gamma Rd, Dallas, TX 75234', 15.00, 3),
('Grace', 'Ivy', 'Flores', '2000-10-04', '2145553010', 'grace.flores@scrumflix.com', '310 Gamma Rd, Dallas, TX 75234', 17.00, 3),

('Levi', 'Aaron', 'Simmons', '1998-06-20', '2145554001', 'levi.simmons@scrumflix.com', '401 Delta Dr, Dallas, TX 75234', 15.00, 4),
('Scarlett', 'Noelle', 'Bryant', '2001-01-28', '2145554002', 'scarlett.bryant@scrumflix.com', '402 Delta Dr, Dallas, TX 75234', 17.00, 4),
('Isaac', 'Troy', 'Griffin', '1999-07-07', '2145554003', 'isaac.griffin@scrumflix.com', '403 Delta Dr, Dallas, TX 75234', 24.00, 4),
('Violet', 'Anne', 'Diaz', '2000-03-23', '2145554004', 'violet.diaz@scrumflix.com', '404 Delta Dr, Dallas, TX 75234', 15.00, 4),
('Samuel', 'Evan', 'Wood', '1997-12-12', '2145554005', 'samuel.wood@scrumflix.com', '405 Delta Dr, Dallas, TX 75234', 17.00, 4),
('Hannah', 'Kate', 'Myers', '2002-05-31', '2145554006', 'hannah.myers@scrumflix.com', '406 Delta Dr, Dallas, TX 75234', 15.00, 4),
('Elijah', 'Noah', 'Long', '1998-09-16', '2145554007', 'elijah.long@scrumflix.com', '407 Delta Dr, Dallas, TX 75234', 24.00, 4),
('Addison', 'Ruby', 'Powell', '2001-11-01', '2145554008', 'addison.powell@scrumflix.com', '408 Delta Dr, Dallas, TX 75234', 17.00, 4),
('Gabriel', 'Finn', 'Jenkins', '1999-04-14', '2145554009', 'gabriel.jenkins@scrumflix.com', '409 Delta Dr, Dallas, TX 75234', 15.00, 4),
('Brooklyn', 'Sage', 'Perry', '2000-08-08', '2145554010', 'brooklyn.perry@scrumflix.com', '410 Delta Dr, Dallas, TX 75234', 17.00, 4),

('Daniel', 'Reid', 'Butler', '1998-02-02', '2145555001', 'daniel.butler@scrumflix.com', '501 Epsilon Ln, Dallas, TX 75234', 15.00, 5),
('Natalie', 'Joy', 'Barnes', '2001-06-25', '2145555002', 'natalie.barnes@scrumflix.com', '502 Epsilon Ln, Dallas, TX 75234', 17.00, 5),
('Matthew', 'Kyle', 'Fisher', '1999-10-19', '2145555003', 'matthew.fisher@scrumflix.com', '503 Epsilon Ln, Dallas, TX 75234', 24.00, 5),
('Aubrey', 'Reese', 'Henderson', '2000-01-29', '2145555004', 'aubrey.henderson@scrumflix.com', '504 Epsilon Ln, Dallas, TX 75234', 15.00, 5),
('Anthony', 'Jude', 'Cole', '1997-07-03', '2145555005', 'anthony.cole@scrumflix.com', '505 Epsilon Ln, Dallas, TX 75234', 17.00, 5),
('Stella', 'Marie', 'Hamilton', '2002-12-20', '2145555006', 'stella.hamilton@scrumflix.com', '506 Epsilon Ln, Dallas, TX 75234', 15.00, 5),
('David', 'Shane', 'Graham', '1998-05-13', '2145555007', 'david.graham@scrumflix.com', '507 Epsilon Ln, Dallas, TX 75234', 24.00, 5),
('Savannah', 'Elle', 'Sullivan', '2001-09-27', '2145555008', 'savannah.sullivan@scrumflix.com', '508 Epsilon Ln, Dallas, TX 75234', 17.00, 5),
('Joseph', 'Lane', 'Wallace', '1999-03-11', '2145555009', 'joseph.wallace@scrumflix.com', '509 Epsilon Ln, Dallas, TX 75234', 15.00, 5),
('Claire', 'Madison', 'Woods', '2000-11-06', '2145555010', 'claire.woods@scrumflix.com', '510 Epsilon Ln, Dallas, TX 75234', 17.00, 5),

('Christopher', 'Adam', 'West', '1998-08-05', '2145556001', 'christopher.west@scrumflix.com', '601 Zeta Ct, Dallas, TX 75234', 15.00, 6),
('Penelope', 'Wren', 'Stone', '2001-02-16', '2145556002', 'penelope.stone@scrumflix.com', '602 Zeta Ct, Dallas, TX 75234', 17.00, 6),
('Andrew', 'Clark', 'Murray', '1999-06-01', '2145556003', 'andrew.murray@scrumflix.com', '603 Zeta Ct, Dallas, TX 75234', 24.00, 6),
('Lucy', 'Brielle', 'Dixon', '2000-09-18', '2145556004', 'lucy.dixon@scrumflix.com', '604 Zeta Ct, Dallas, TX 75234', 15.00, 6),
('Joshua', 'Neil', 'Ford', '1997-04-26', '2145556005', 'joshua.ford@scrumflix.com', '605 Zeta Ct, Dallas, TX 75234', 17.00, 6),
('Victoria', 'Eden', 'Marshall', '2002-07-22', '2145556006', 'victoria.marshall@scrumflix.com', '606 Zeta Ct, Dallas, TX 75234', 15.00, 6),
('Thomas', 'Gage', 'Owens', '1998-12-03', '2145556007', 'thomas.owens@scrumflix.com', '607 Zeta Ct, Dallas, TX 75234', 24.00, 6),
('Paisley', 'Jane', 'Bishop', '2001-05-15', '2145556008', 'paisley.bishop@scrumflix.com', '608 Zeta Ct, Dallas, TX 75234', 17.00, 6),
('Ryan', 'Heath', 'Freeman', '1999-01-21', '2145556009', 'ryan.freeman@scrumflix.com', '609 Zeta Ct, Dallas, TX 75234', 15.00, 6),
('Skylar', 'Quinn', 'Harrison', '2000-10-30', '2145556010', 'skylar.harrison@scrumflix.com', '610 Zeta Ct, Dallas, TX 75234', 17.00, 6);






INSERT INTO Users (EmployeeId, UserName, UserPassword, RoleId)
VALUES

-- =====================================
-- LOCATION 1 (ALPHA)
-- =====================================
(1, 'a2', 'a123', 1),     -- Admin
(2, 'mia.b', 'mia123', 2),      -- Manager
(3, 'caleb.f', 'cal123', 3),
(4, 'nora.r', 'nora123', 3),
(5, 'ethan.m', 'eth123', 3),
(6, 'lily.h', 'lily123', 3),
(7, 'brandon.t', 'bran123', 3),
(8, 'kaylee.e', 'kay123', 3),
(9, 'jordan.c', 'jord123', 3),
(10, 'madelyn.p', 'mad123', 3),

-- =====================================
-- LOCATION 2 (BETA)
-- =====================================
(11, 'owen.p', 'owen123', 2),   -- Manager
(12, 'zoe.s', 'zoe123', 3),
(13, 'logan.b', 'log123', 3),
(14, 'ella.p', 'ella123', 3),
(15, 'mason.p', 'mas123', 3),
(16, 'avery.c', 'ave123', 3),
(17, 'carter.r', 'car123', 3),
(18, 'sofia.w', 'sof123', 3),
(19, 'jackson.c', 'jack123', 3),
(20, 'harper.b', 'har123', 3),

-- =====================================
-- LOCATION 3 (GAMMA)
-- =====================================
(21, 'lucas.h', 'luc123', 2),   -- Manager
(22, 'aria.m', 'aria123', 3),
(23, 'henry.r', 'hen123', 3),
(24, 'chloe.k', 'chloe123', 3),
(25, 'wyatt.h', 'wya123', 3),
(26, 'riley.c', 'ril123', 3),
(27, 'julian.t', 'jul123', 3),
(28, 'layla.b', 'lay123', 3),
(29, 'nathan.r', 'nat123', 3),
(30, 'grace.f', 'gra123', 3),

-- =====================================
-- LOCATION 4 (DELTA)
-- =====================================
(31, 'levi.s', 'lev123', 2),    -- Manager
(32, 'scarlett.b', 'scar123', 3),
(33, 'isaac.g', 'isa123', 3),
(34, 'violet.d', 'vio123', 3),
(35, 'samuel.w', 'sam123', 3),
(36, 'hannah.m', 'han123', 3),
(37, 'elijah.l', 'eli123', 3),
(38, 'addison.p', 'add123', 3),
(39, 'gabriel.j', 'gab123', 3),
(40, 'brooklyn.p', 'bro123', 3),

-- =====================================
-- LOCATION 5 (EPSILON)
-- =====================================
(41, 'daniel.b', 'dan123', 2),  -- Manager
(42, 'natalie.b', 'nat123', 3),
(43, 'matthew.f', 'mat123', 3),
(44, 'aubrey.h', 'aub123', 3),
(45, 'anthony.c', 'ant123', 3),
(46, 'stella.h', 'ste123', 3),
(47, 'david.g', 'dav123', 3),
(48, 'savannah.s', 'sav123', 3),
(49, 'joseph.w', 'jos123', 3),
(50, 'claire.w', 'cla123', 3),

-- =====================================
-- LOCATION 6 (ZETA)
-- =====================================
(51, 'christopher.w', 'chr123', 2), -- Manager
(52, 'penelope.s', 'pen123', 3),
(53, 'andrew.m', 'and123', 3),
(54, 'lucy.d', 'lucy123', 3),
(55, 'joshua.f', 'jos123', 3),
(56, 'victoria.m', 'vic123', 3),
(57, 'thomas.o', 'tho123', 3),
(58, 'paisley.b', 'pai123', 3),
(59, 'ryan.f', 'ryan123', 3),
(60, 'skylar.h', 'sky123', 3);





INSERT INTO ConcessionItem
(ItemName, Price, QuantityInStock, Minimum, IsActive, Notes, LocationId)
VALUES

-- =========================
-- ALPHA THEATER (1)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 1),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 1),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 1),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 1),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 1),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 1),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 1),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 1),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 1),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 1),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 1),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 1),

-- =========================
-- BETA THEATER (2)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 2),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 2),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 2),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 2),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 2),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 2),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 2),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 2),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 2),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 2),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 2),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 2),

-- =========================
-- GAMMA THEATER (3)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 3),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 3),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 3),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 3),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 3),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 3),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 3),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 3),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 3),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 3),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 3),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 3),

-- =========================
-- DELTA THEATER (4)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 4),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 4),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 4),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 4),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 4),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 4),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 4),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 4),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 4),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 4),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 4),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 4),

-- =========================
-- EPSILON THEATER (5)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 5),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 5),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 5),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 5),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 5),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 5),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 5),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 5),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 5),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 5),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 5),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 5),

-- =========================
-- ZETA THEATER (6)
-- =========================
('Large Popcorn', 8.99, 240, 40, TRUE, 'Butter popcorn', 6),
('Medium Popcorn', 7.49, 220, 35, TRUE, 'Butter popcorn', 6),
('Small Popcorn', 5.99, 180, 30, TRUE, 'Butter popcorn', 6),
('Large Drink', 6.49, 260, 40, TRUE, 'Fountain drink', 6),
('Medium Drink', 5.49, 240, 35, TRUE, 'Fountain drink', 6),
('Small Drink', 4.49, 220, 30, TRUE, 'Fountain drink', 6),
('Nachos', 7.99, 120, 20, TRUE, 'Nachos with cheese', 6),
('Hot Dog', 6.99, 110, 20, TRUE, 'Classic hot dog', 6),
('Pretzel', 5.99, 100, 15, TRUE, 'Salted pretzel', 6),
('Candy Variety Pack', 4.99, 300, 50, TRUE, 'Assorted candy', 6),
('Ice Cream Cup', 5.49, 90, 15, TRUE, 'Frozen dessert', 6),
('Bottle Water', 3.99, 180, 25, TRUE, 'Bottled water', 6);





DELIMITER //

DROP PROCEDURE IF EXISTS SellOutAllShowtimes //

CREATE PROCEDURE SellOutAllShowtimes()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE vShowtimeId INT;
    DECLARE vCapacity INT;
    DECLARE vStartTime DATETIME;
    DECLARE ticketCounter INT;
    DECLARE userCounter INT DEFAULT 1;
    DECLARE userCount INT;
    DECLARE selectedUserId INT;
    DECLARE saleTime DATETIME;

    DECLARE showtimeCursor CURSOR FOR
        SELECT ShowtimeId, Capacity, StartTime
        FROM Showtime
        WHERE StartTime >= '2026-03-01'
          AND StartTime < '2026-06-01'
        ORDER BY StartTime, ShowtimeId;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    DROP TEMPORARY TABLE IF EXISTS TempUsers;

    CREATE TEMPORARY TABLE TempUsers (
        rn INT PRIMARY KEY,
        UserId INT NOT NULL
    );

    INSERT INTO TempUsers (rn, UserId)
    SELECT ROW_NUMBER() OVER (ORDER BY UserId), UserId
    FROM Users;

    SELECT COUNT(*) INTO userCount FROM TempUsers;

    IF userCount = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'No users found. Insert Users before generating tickets.';
    END IF;

    OPEN showtimeCursor;

    showtime_loop: LOOP
        FETCH showtimeCursor INTO vShowtimeId, vCapacity, vStartTime;

        IF done = 1 THEN
            LEAVE showtime_loop;
        END IF;

        SET ticketCounter = 1;

        WHILE ticketCounter <= vCapacity DO
            SELECT UserId INTO selectedUserId
            FROM TempUsers
            WHERE rn = userCounter;

            SET saleTime = DATE_SUB(vStartTime, INTERVAL 1 DAY);

            SET saleTime = DATE_ADD(
                DATE(saleTime),
                INTERVAL (9 + MOD(ticketCounter, 12)) HOUR
            );

            SET saleTime = DATE_ADD(
                saleTime,
                INTERVAL MOD((ticketCounter * 7), 60) MINUTE
            );

            SET saleTime = DATE_ADD(
                saleTime,
                INTERVAL MOD((ticketCounter * 13), 60) SECOND
            );

            INSERT INTO Ticket (TicketCode, ShowtimeId, UserAtSale, TimeOfSale)
            VALUES (
                FLOOR(100000 + RAND() * 900000),
                vShowtimeId,
                selectedUserId,
                saleTime
            );

            SET ticketCounter = ticketCounter + 1;
            SET userCounter = userCounter + 1;

            IF userCounter > userCount THEN
                SET userCounter = 1;
            END IF;
        END WHILE;
    END LOOP;

    CLOSE showtimeCursor;
END //

DELIMITER ;

CALL SellOutAllShowtimes();

DROP PROCEDURE IF EXISTS SellOutAllShowtimes;



/*INSERT INTO Ticket (TicketCode, ShowtimeId, UserAtSale, TimeOfSale)
VALUES
('481237', '1', '2', '2026-04-29 14:45:20'),
('438832', '2', '2', '2026-04-29 14:45:31'),
('107416', '3', '2', '2026-04-29 14:45:40'),
('906221', '1', '2', '2026-04-29 14:46:05'),
('955114', '3', '2', '2026-04-29 14:46:14');*/



INSERT INTO Shifts (StartTime, EndTime, RoleId, LocationId)
VALUES
('2026-05-01 08:00:00', '2026-05-01 12:00:00', 3, 1),
('2026-05-01 09:00:00', '2026-05-01 13:00:00', 2, 1),
('2026-05-01 13:00:00', '2026-05-01 17:00:00', 3, 1),
('2026-05-01 15:00:00', '2026-05-01 20:00:00', 2, 1),

('2026-05-02 08:30:00', '2026-05-02 12:30:00', 3, 2),
('2026-05-02 10:00:00', '2026-05-02 14:00:00', 2, 2),
('2026-05-02 14:00:00', '2026-05-02 18:00:00', 3, 2),
('2026-05-02 16:00:00', '2026-05-02 21:00:00', 2, 2);



INSERT INTO TimeEntries (EmployeeId, ClockIn, ClockOut)
VALUES
(1, '2026-05-01 09:00:00', '2026-05-01 17:00:00'),
(2, '2026-05-01 10:00:00', '2026-05-01 16:00:00'),
(3, '2026-05-01 12:00:00', '2026-05-01 18:30:00'),
(4, '2026-05-01 14:00:00', '2026-05-01 20:00:00'),

(1, '2026-05-04 09:00:00', '2026-05-04 17:00:00'),
(2, '2026-05-04 10:00:00', '2026-05-04 16:30:00'),
(3, '2026-05-04 12:00:00', '2026-05-04 18:00:00'),
(4, '2026-05-04 14:00:00', '2026-05-04 21:00:00'),

(1, '2026-05-08 09:30:00', '2026-05-08 16:30:00'),
(2, '2026-05-08 10:00:00', '2026-05-08 15:30:00'),
(3, '2026-05-08 12:30:00', '2026-05-08 19:00:00'),
(4, '2026-05-08 13:00:00', '2026-05-08 20:00:00'),

(1, '2026-05-12 08:30:00', '2026-05-12 16:30:00'),
(2, '2026-05-12 10:30:00', '2026-05-12 17:00:00'),
(3, '2026-05-12 12:00:00', '2026-05-12 18:30:00'),
(4, '2026-05-12 15:00:00', '2026-05-12 21:30:00'),

(1, '2026-05-16 09:00:00', '2026-05-16 15:00:00'),
(2, '2026-05-16 11:00:00', '2026-05-16 17:30:00'),
(3, '2026-05-16 13:00:00', '2026-05-16 19:00:00'),
(4, '2026-05-16 14:30:00', '2026-05-16 22:00:00'),

(1, '2026-05-20 09:00:00', '2026-05-20 17:00:00'),
(2, '2026-05-20 10:00:00', '2026-05-20 16:00:00'),
(3, '2026-05-20 12:00:00', '2026-05-20 18:30:00'),
(4, '2026-05-20 13:30:00', '2026-05-20 20:30:00'),

(1, '2026-05-24 08:30:00', '2026-05-24 16:00:00'),
(2, '2026-05-24 10:00:00', '2026-05-24 16:30:00'),
(3, '2026-05-24 12:30:00', '2026-05-24 19:30:00'),
(4, '2026-05-24 14:00:00', '2026-05-24 21:00:00'),

(1, '2026-05-28 09:00:00', '2026-05-28 17:00:00'),
(2, '2026-05-28 11:00:00', '2026-05-28 17:00:00'),
(3, '2026-05-28 12:00:00', '2026-05-28 18:00:00'),
(4, '2026-05-28 15:00:00', '2026-05-28 22:00:00'),

(1, '2026-05-31 09:30:00', '2026-05-31 16:30:00'),
(2, '2026-05-31 10:00:00', '2026-05-31 15:30:00'),
(3, '2026-05-31 13:00:00', '2026-05-31 19:00:00'),
(4, '2026-05-31 14:00:00', '2026-05-31 20:30:00');