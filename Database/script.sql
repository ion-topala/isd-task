CREATE TABLE Passengers
(
    Id          INT IDENTITY (1,1) PRIMARY KEY,
    FirstName   NVARCHAR(50)         NOT NULL,
    LastName    NVARCHAR(50)         NOT NULL,
    Email       NVARCHAR(100) UNIQUE NOT NULL,
    PhoneNumber NVARCHAR(20),
    DateOfBirth DATE,
    Address     NVARCHAR(200),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    UpdatedDate DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Stations
(
    Id          INT IDENTITY (1,1) PRIMARY KEY,
    StationName NVARCHAR(100)       NOT NULL,
    StationCode NVARCHAR(10) UNIQUE NOT NULL,
    City        NVARCHAR(50)        NOT NULL,
    Country     NVARCHAR(50)        NOT NULL,
    Latitude    DECIMAL(10, 8),
    Longitude   DECIMAL(11, 8),
    IsActive    BIT DEFAULT 1
);

CREATE TABLE Trains
(
    Id            INT IDENTITY (1,1) PRIMARY KEY,
    TrainNumber   NVARCHAR(20) UNIQUE NOT NULL,
    TrainName     NVARCHAR(100)       NOT NULL,
    TrainType     NVARCHAR(50)        NOT NULL,
    TotalCapacity INT                 NOT NULL,
    NumberOfCars  INT                 NOT NULL,
    IsActive      BIT DEFAULT 1
);

CREATE TABLE Routes
(
    Id                       INT IDENTITY (1,1) PRIMARY KEY,
    RouteName                NVARCHAR(100) NOT NULL,
    OriginStationId          INT           NOT NULL,
    DestinationStationId     INT           NOT NULL,
    TotalDistance            DECIMAL(8, 2),
    EstimatedDurationMinutes INT,
    IsActive                 BIT DEFAULT 1,
    CONSTRAINT FK_Routes_OriginStation FOREIGN KEY (OriginStationId) REFERENCES Stations (Id),
    CONSTRAINT FK_Routes_DestinationStation FOREIGN KEY (DestinationStationId) REFERENCES Stations (Id),
    CONSTRAINT CHK_Routes_DifferentStations CHECK (OriginStationId != DestinationStationId)
);

CREATE TABLE RouteStations
(
    Id                     INT IDENTITY (1,1) PRIMARY KEY,
    RouteId                INT           NOT NULL,
    StationId              INT           NOT NULL,
    SequenceOrder          INT           NOT NULL,
    DistanceFromOrigin     DECIMAL(8, 2) NOT NULL DEFAULT 0,
    EstimatedArrivalTime   TIME,
    EstimatedDepartureTime TIME,
    CONSTRAINT FK_RouteStations_Route FOREIGN KEY (RouteId) REFERENCES Routes (Id),
    CONSTRAINT FK_RouteStations_Station FOREIGN KEY (StationId) REFERENCES Stations (Id),
    CONSTRAINT UK_RouteStations UNIQUE (RouteId, SequenceOrder),
    CONSTRAINT UK_RouteStations_Station UNIQUE (RouteId, StationId)
);

CREATE TABLE Journeys
(
    JourneyId      INT IDENTITY (1,1) PRIMARY KEY,
    TrainId        INT            NOT NULL,
    RouteId        INT            NOT NULL,
    DepartureDate  DATE           NOT NULL,
    DepartureTime  TIME           NOT NULL,
    ArrivalDate    DATE           NOT NULL,
    ArrivalTime    TIME           NOT NULL,
    Status         NVARCHAR(20)   NOT NULL DEFAULT 'Scheduled',
    AvailableSeats INT            NOT NULL,
    BasePrice      DECIMAL(10, 2) NOT NULL,
    CreatedDate    DATETIME2               DEFAULT GETDATE(),
    CONSTRAINT FK_Journeys_Train FOREIGN KEY (TrainId) REFERENCES Trains (Id),
    CONSTRAINT FK_Journeys_Route FOREIGN KEY (RouteId) REFERENCES Routes (Id),
);

CREATE TABLE Tickets
(
    Id                   INT IDENTITY (1,1) PRIMARY KEY,
    PassengerId          INT                 NOT NULL,
    JourneyId            INT                 NOT NULL,
    OriginStationId      INT                 NOT NULL,
    DestinationStationId INT                 NOT NULL,
    TicketNumber         NVARCHAR(50) UNIQUE NOT NULL,
    PurchaseDate         DATETIME2                    DEFAULT GETDATE(),
    TravelDate           DATE                NOT NULL,
    Price                DECIMAL(10, 2)      NOT NULL,
    Status               NVARCHAR(20)        NOT NULL DEFAULT 'Booked',
    CONSTRAINT FK_Tickets_Passenger FOREIGN KEY (PassengerId) REFERENCES Passengers (Id),
    CONSTRAINT FK_Tickets_Journey FOREIGN KEY (JourneyId) REFERENCES Journeys (JourneyId),
    CONSTRAINT FK_Tickets_OriginStation FOREIGN KEY (OriginStationId) REFERENCES Stations (Id),
    CONSTRAINT FK_Tickets_DestinationStation FOREIGN KEY (DestinationStationId) REFERENCES Stations (Id),
    CONSTRAINT CHK_Tickets_DifferentStations CHECK (OriginStationId != DestinationStationId)
);

CREATE TABLE Payments
(
    Id                  INT IDENTITY (1,1) PRIMARY KEY,
    TicketId            INT            NOT NULL,
    PaymentMethod       NVARCHAR(50)   NOT NULL,
    PaymentAmount       DECIMAL(10, 2) NOT NULL,
    PaymentDate         DATETIME2               DEFAULT GETDATE(),
    TransactionId       NVARCHAR(100) UNIQUE,
    PaymentStatus       NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    EncryptedRequisites VARBINARY(MAX),
    MaskedCardNumber    NVARCHAR(20),
    AuthorizationCode   NVARCHAR(50),
    PaymentNotes        NVARCHAR(500),
    CONSTRAINT FK_Payments_Ticket FOREIGN KEY (TicketId) REFERENCES Tickets (Id),
);


--stations
INSERT INTO RaildRoad.dbo.Stations (StationName, StationCode, City, Country, Latitude, Longitude, IsActive)
VALUES (N'Iasi', N'RO001', N'Iasi', N'Romania', 40.71280000, -74.00600000, 1);
INSERT INTO RaildRoad.dbo.Stations (StationName, StationCode, City, Country, Latitude, Longitude, IsActive)
VALUES (N'Chisinau', N'RMO001', N'Chisinau', N'Moldova', 41.87810000, -87.62980000, 1);
INSERT INTO RaildRoad.dbo.Stations (StationName, StationCode, City, Country, Latitude, Longitude, IsActive)
VALUES (N'Ungheni', N'RMO002', N'Ungheni', N'Moldova', 34.05220000, -118.24370000, 1);

--routes
INSERT INTO RaildRoad.dbo.Routes (RouteName, OriginStationId, DestinationStationId, TotalDistance,
                                  EstimatedDurationMinutes, IsActive)
VALUES (N'Chisinau - Iasi', 2, 1, 170.00, 330, 1);

--routes stations
INSERT INTO RaildRoad.dbo.RouteStations (RouteId, StationId, SequenceOrder, DistanceFromOrigin, EstimatedArrivalTime,
                                         EstimatedDepartureTime)
VALUES (1, 2, 1, 0.00, null, N'08:00:00.0000000');
INSERT INTO RaildRoad.dbo.RouteStations (RouteId, StationId, SequenceOrder, DistanceFromOrigin, EstimatedArrivalTime,
                                         EstimatedDepartureTime)
VALUES (1, 3, 2, 95.30, null, N'11:00:00.0000000');
INSERT INTO RaildRoad.dbo.RouteStations (RouteId, StationId, SequenceOrder, DistanceFromOrigin, EstimatedArrivalTime,
                                         EstimatedDepartureTime)
VALUES (1, 1, 3, 170.00, null, N'13:00:00.0000000');


--trains
INSERT INTO RaildRoad.dbo.Trains (TrainNumber, TrainName, TrainType, TotalCapacity, NumberOfCars, IsActive)
VALUES (N'TR001', N'Fulger Express', N'HighSpeed', 300, 8, 1);
INSERT INTO RaildRoad.dbo.Trains (TrainNumber, TrainName, TrainType, TotalCapacity, NumberOfCars, IsActive)
VALUES (N'TR002', N'Fulger McQuinn', N'Express', 250, 6, 1);

--passengers
INSERT INTO RaildRoad.dbo.Passengers (FirstName, LastName, Email, PhoneNumber, DateOfBirth, Address, CreatedDate,
                                      UpdatedDate)
VALUES (N'Topala', N'Ion', N'john.doe@email.com', N'+1-555-0101', N'1985-06-15', null, N'2025-09-05 14:04:22.6466667',
        N'2025-09-05 14:04:22.6466667');

--journeys
INSERT INTO RaildRoad.dbo.Journeys (TrainId, RouteId, DepartureDate, DepartureTime, ArrivalDate, ArrivalTime,
                                    Status, AvailableSeats, BasePrice, CreatedDate)
VALUES (1, 1, N'2025-09-10', N'08:00:00.0000000', N'2025-09-05', N'13:00:00.0000000', N'Scheduled', 80, 125.00,
        N'2025-09-05 14:05:35.4766667');


--tickets
INSERT INTO RaildRoad.dbo.Tickets (PassengerId, JourneyId, OriginStationId, DestinationStationId, TicketNumber,
                                   PurchaseDate, TravelDate, Price, Status)
VALUES (1, 1, 2, 1, N'1111', N'2025-09-05 17:12:33.0000000', N'2025-09-06', 125.00, N'Booked');


