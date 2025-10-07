

PRAGMA foreign_keys = ON;


DROP TABLE IF EXISTS "Orders";
DROP TABLE IF EXISTS "TicketTypes";
DROP TABLE IF EXISTS "Exhibitions";
DROP TABLE IF EXISTS "Museums";


CREATE TABLE "Museums" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
    "Name"          TEXT NOT NULL,
    "City"          TEXT NOT NULL,
    "Description"   TEXT NULL
);


CREATE TABLE "Exhibitions" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
    "Title"         TEXT NOT NULL,
    "StartDate"     TEXT NOT NULL,   
    "EndDate"       TEXT NULL,
    "Description"   TEXT NULL,
    "MuseumId"      INTEGER NOT NULL,
    FOREIGN KEY ("MuseumId") REFERENCES "Museums"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Exhibitions_MuseumId" ON "Exhibitions"("MuseumId");


CREATE TABLE "TicketTypes" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
    "Name"          TEXT NOT NULL,
    "Price"         REAL NOT NULL,
    "Description"   TEXT NULL,
    "MuseumId"      INTEGER NOT NULL,
    FOREIGN KEY ("MuseumId") REFERENCES "Museums"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_TicketTypes_MuseumId" ON "TicketTypes"("MuseumId");


CREATE TABLE "Orders" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
    "BuyerName"     TEXT NOT NULL,
    "BuyerEmail"    TEXT NULL,
    "Quantity"      INTEGER NOT NULL,
    "OrderedAt"     TEXT NOT NULL,       
    "TicketTypeId"  INTEGER NOT NULL,
    "ExhibitionId"  INTEGER NOT NULL,
    FOREIGN KEY ("TicketTypeId") REFERENCES "TicketTypes"("Id") ON DELETE RESTRICT,
    FOREIGN KEY ("ExhibitionId") REFERENCES "Exhibitions"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Orders_TicketTypeId" ON "Orders"("TicketTypeId");
CREATE INDEX "IX_Orders_ExhibitionId" ON "Orders"("ExhibitionId");



INSERT INTO "Museums" ("Name","City","Description") VALUES
('Narodni muzej','Beograd','Najstarija muzejska institucija u Srbiji.'),
('Muzej savremene umetnosti','Beograd','Savremena i modernistička umetnost.');


INSERT INTO "Exhibitions" ("Title","StartDate","EndDate","Description","MuseumId") VALUES
('Stalna postavka','2025-09-01',NULL,'Glavna stalna postavka.',1),
('Gostujuća izložba','2025-09-10','2025-10-10','Gostovanje kolekcije.',1),
('Moderna kolekcija','2025-09-05',NULL,'Stalna savremena postavka.',2);

 
INSERT INTO "TicketTypes" ("Name","Price","Description","MuseumId") VALUES
('Osnovna',   500, 'Standardna karta', 1),
('Porodična', 1200, '2+2',              1),
('Studentska',300, 'Sa indeksom',      2);


INSERT INTO "Orders" ("BuyerName","BuyerEmail","Quantity","OrderedAt","TicketTypeId","ExhibitionId") VALUES
('Pera Perić','pera@example.com',2,'2025-09-11T09:00:00Z',1,1),
('Mika Mikić','mika@example.com',1,'2025-09-11T10:30:00Z',2,2),
('Ana Anić','ana@example.com',3,'2025-09-12T12:00:00Z',3,3);
