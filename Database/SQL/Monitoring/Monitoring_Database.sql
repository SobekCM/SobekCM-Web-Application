/*
    SobekCM central monitoring database (SQL Server)

    One database shared by every SobekCM instance. Each instance sends its exceptions and
    rate-limiting events here (see Code/SobekCM_Engine_Library/Monitoring/SqlMonitoringSink.cs)
    instead of only writing temp/exceptions.txt and temp/ratelimiting.txt.

    This is NOT part of the per-instance SobekCM database or its version upgrade scripts.
    Run it against an empty database. It is safe to re-run: tables and the table type are
    only created if missing, and every procedure is CREATE OR ALTER.

    Fingerprints group repeat occurrences of the same defect, and also carry the AutoFix
    workflow state (ticket, branch, pull request) used by the autofix orchestrator and worker.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------------------------- */
/* Tables                                                                                    */
/* ---------------------------------------------------------------------------------------- */

IF OBJECT_ID('dbo.Monitoring_Exception_Fingerprint', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Monitoring_Exception_Fingerprint
    (
        FingerprintId    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Monitoring_Exception_Fingerprint PRIMARY KEY,
        Hash             char(64)       NOT NULL,
        ExceptionType    nvarchar(500)  NOT NULL,
        TopFrame         nvarchar(1000) NULL,
        FirstSeenUtc     datetime2(3)   NOT NULL,
        LastSeenUtc      datetime2(3)   NOT NULL,
        OccurrenceCount  int            NOT NULL CONSTRAINT DF_Monitoring_Exception_Fingerprint_OccurrenceCount DEFAULT (1),
        Status           varchar(20)    NOT NULL CONSTRAINT DF_Monitoring_Exception_Fingerprint_Status DEFAULT ('New'),
        TicketId         int            NULL,
        BranchName       nvarchar(200)  NULL,
        PrUrl            nvarchar(500)  NULL,
        Attempts         int            NOT NULL CONSTRAINT DF_Monitoring_Exception_Fingerprint_Attempts DEFAULT (0),
        LastAttemptUtc   datetime2(3)   NULL,
        MergedUtc        datetime2(3)   NULL,
        AnalysisNotes    nvarchar(max)  NULL,
        CONSTRAINT UQ_Monitoring_Exception_Fingerprint_Hash UNIQUE (Hash),
        CONSTRAINT CK_Monitoring_Exception_Fingerprint_Status CHECK (Status IN
            ('New', 'Ticketed', 'InProgress', 'PrOpen', 'NoFixFound', 'Merged', 'Rejected', 'Regressed', 'Ignored'))
    );

    CREATE INDEX IX_Monitoring_Exception_Fingerprint_Status
        ON dbo.Monitoring_Exception_Fingerprint (Status, OccurrenceCount DESC);
END
GO

IF OBJECT_ID('dbo.Monitoring_Exception_Occurrence', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Monitoring_Exception_Occurrence
    (
        OccurrenceId   bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Monitoring_Exception_Occurrence PRIMARY KEY,
        FingerprintId  int            NOT NULL CONSTRAINT FK_Monitoring_Exception_Occurrence_Fingerprint
                                          REFERENCES dbo.Monitoring_Exception_Fingerprint (FingerprintId),
        InstanceName   nvarchar(100)  NOT NULL,
        ServerName     nvarchar(100)  NULL,
        OccurredUtc    datetime2(3)   NOT NULL,
        Source         varchar(50)    NULL,
        Message        nvarchar(2000) NULL,
        InnerMessage   nvarchar(2000) NULL,
        StackTrace     nvarchar(max)  NULL,
        Url            nvarchar(2000) NULL,
        ClientIp       varchar(45)    NULL,
        TraceText      nvarchar(max)  NULL
    );

    CREATE INDEX IX_Monitoring_Exception_Occurrence_Fingerprint
        ON dbo.Monitoring_Exception_Occurrence (FingerprintId, InstanceName, OccurredUtc);

    CREATE INDEX IX_Monitoring_Exception_Occurrence_OccurredUtc
        ON dbo.Monitoring_Exception_Occurrence (OccurredUtc);
END
GO

IF OBJECT_ID('dbo.Monitoring_RateLimit_Event', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Monitoring_RateLimit_Event
    (
        EventId       bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Monitoring_RateLimit_Event PRIMARY KEY,
        InstanceName  nvarchar(100)  NOT NULL,
        ServerName    nvarchar(100)  NULL,
        OccurredUtc   datetime2(3)   NOT NULL,
        EventName     varchar(50)    NOT NULL,
        LoggedOn      bit            NOT NULL,
        Address       varchar(100)   NULL,
        Details       nvarchar(2000) NULL,
        UserAgent     nvarchar(500)  NULL
    );

    CREATE INDEX IX_Monitoring_RateLimit_Event_Instance
        ON dbo.Monitoring_RateLimit_Event (InstanceName, OccurredUtc);

    CREATE INDEX IX_Monitoring_RateLimit_Event_OccurredUtc
        ON dbo.Monitoring_RateLimit_Event (OccurredUtc);
END
GO

-- UserAgent was added after the first version of this script; bring an existing table up to date.
-- It's the user agent of the one request that tripped the event, not necessarily representative
-- of the traffic that led up to it.
IF COL_LENGTH('dbo.Monitoring_RateLimit_Event', 'UserAgent') IS NULL
    ALTER TABLE dbo.Monitoring_RateLimit_Event ADD UserAgent nvarchar(500) NULL;
GO

-- A table type can't be altered, so a copy from before UserAgent was added is dropped (along with
-- the procedure that uses it) and recreated below. The Roles section re-grants it.
IF TYPE_ID('dbo.Monitoring_RateLimit_Event_Table') IS NOT NULL
   AND NOT EXISTS (SELECT 1
                   FROM sys.table_types tt
                   JOIN sys.columns c ON c.object_id = tt.type_table_object_id
                   WHERE tt.name = 'Monitoring_RateLimit_Event_Table' AND c.name = 'UserAgent')
BEGIN
    DROP PROCEDURE IF EXISTS dbo.Monitoring_Log_RateLimit_Events;
    DROP TYPE dbo.Monitoring_RateLimit_Event_Table;
END
GO

IF TYPE_ID('dbo.Monitoring_RateLimit_Event_Table') IS NULL
BEGIN
    CREATE TYPE dbo.Monitoring_RateLimit_Event_Table AS TABLE
    (
        OccurredUtc  datetime2(3)   NOT NULL,
        EventName    varchar(50)    NOT NULL,
        LoggedOn     bit            NOT NULL,
        Address      varchar(100)   NULL,
        Details      nvarchar(2000) NULL,
        UserAgent    nvarchar(500)  NULL
    );
END
GO

/* ---------------------------------------------------------------------------------------- */
/* Written by the web application                                                            */
/* ---------------------------------------------------------------------------------------- */

-- Adds one exception occurrence. Creates the fingerprint the first time it's seen; after
-- that only bumps its count and last-seen time. Full occurrence rows are kept only for the
-- first 5 per fingerprint, per instance, per hour, so a burst of identical exceptions from
-- one site keeps a few samples without storing every copy.
--
-- A fingerprint marked Merged that shows up again more than 7 days after the merge is moved
-- to Regressed, so the orchestrator reopens it. The grace period covers the time between a
-- fix being merged and that build actually being deployed to every instance.
CREATE OR ALTER PROCEDURE dbo.Monitoring_Log_Exception
    @InstanceName   nvarchar(100),
    @ServerName     nvarchar(100),
    @Hash           char(64),
    @ExceptionType  nvarchar(500),
    @TopFrame       nvarchar(1000),
    @OccurredUtc    datetime2(3),
    @Source         varchar(50),
    @Message        nvarchar(2000),
    @InnerMessage   nvarchar(2000),
    @StackTrace     nvarchar(max),
    @Url            nvarchar(2000),
    @ClientIp       varchar(45),
    @TraceText      nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @FingerprintId int;

    BEGIN TRANSACTION;

    SELECT @FingerprintId = FingerprintId
    FROM dbo.Monitoring_Exception_Fingerprint WITH (UPDLOCK, HOLDLOCK)
    WHERE Hash = @Hash;

    IF @FingerprintId IS NULL
    BEGIN
        INSERT dbo.Monitoring_Exception_Fingerprint (Hash, ExceptionType, TopFrame, FirstSeenUtc, LastSeenUtc)
        VALUES (@Hash, @ExceptionType, @TopFrame, @OccurredUtc, @OccurredUtc);

        SET @FingerprintId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Monitoring_Exception_Fingerprint
        SET OccurrenceCount = OccurrenceCount + 1,
            LastSeenUtc = CASE WHEN @OccurredUtc > LastSeenUtc THEN @OccurredUtc ELSE LastSeenUtc END,
            Status = CASE WHEN (Status = 'Merged') AND (@OccurredUtc > DATEADD(day, 7, MergedUtc)) THEN 'Regressed' ELSE Status END
        WHERE FingerprintId = @FingerprintId;
    END

    IF (SELECT COUNT(*)
        FROM dbo.Monitoring_Exception_Occurrence
        WHERE FingerprintId = @FingerprintId
          AND InstanceName = @InstanceName
          AND OccurredUtc > DATEADD(hour, -1, @OccurredUtc)) < 5
    BEGIN
        INSERT dbo.Monitoring_Exception_Occurrence
            (FingerprintId, InstanceName, ServerName, OccurredUtc, Source, Message, InnerMessage, StackTrace, Url, ClientIp, TraceText)
        VALUES
            (@FingerprintId, @InstanceName, @ServerName, @OccurredUtc, @Source, @Message, @InnerMessage, @StackTrace, @Url, @ClientIp, @TraceText);
    END

    COMMIT TRANSACTION;
END
GO

-- Adds a batch of rate-limiting events from one instance
CREATE OR ALTER PROCEDURE dbo.Monitoring_Log_RateLimit_Events
    @InstanceName  nvarchar(100),
    @ServerName    nvarchar(100),
    @Events        dbo.Monitoring_RateLimit_Event_Table READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT dbo.Monitoring_RateLimit_Event (InstanceName, ServerName, OccurredUtc, EventName, LoggedOn, Address, Details, UserAgent)
    SELECT @InstanceName, @ServerName, OccurredUtc, EventName, LoggedOn, Address, Details, UserAgent
    FROM @Events;
END
GO

/* ---------------------------------------------------------------------------------------- */
/* Used by the autofix orchestrator and worker                                               */
/* ---------------------------------------------------------------------------------------- */

-- Fingerprints that still need a ticket and a fix attempt, most frequent first
CREATE OR ALTER PROCEDURE dbo.Monitoring_Get_AutoFix_Candidates
    @MaxCount     int = 2,
    @MaxAttempts  int = 2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxCount)
        FingerprintId, Hash, ExceptionType, TopFrame, FirstSeenUtc, LastSeenUtc, OccurrenceCount,
        Status, TicketId, Attempts
    FROM dbo.Monitoring_Exception_Fingerprint
    WHERE Status IN ('New', 'Regressed')
      AND Attempts < @MaxAttempts
    ORDER BY OccurrenceCount DESC, FingerprintId;
END
GO

-- All fingerprints in one status (e.g. PrOpen, to sync pull request state)
CREATE OR ALTER PROCEDURE dbo.Monitoring_Get_Fingerprints_By_Status
    @Status  varchar(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT FingerprintId, Hash, ExceptionType, TopFrame, FirstSeenUtc, LastSeenUtc, OccurrenceCount,
        Status, TicketId, BranchName, PrUrl, Attempts, LastAttemptUtc, MergedUtc
    FROM dbo.Monitoring_Exception_Fingerprint
    WHERE Status = @Status
    ORDER BY FingerprintId;
END
GO

-- One fingerprint, followed by its most recent occurrence samples
CREATE OR ALTER PROCEDURE dbo.Monitoring_Get_Fingerprint_Detail
    @FingerprintId   int,
    @MaxOccurrences  int = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT FingerprintId, Hash, ExceptionType, TopFrame, FirstSeenUtc, LastSeenUtc, OccurrenceCount,
        Status, TicketId, BranchName, PrUrl, Attempts, LastAttemptUtc, MergedUtc, AnalysisNotes
    FROM dbo.Monitoring_Exception_Fingerprint
    WHERE FingerprintId = @FingerprintId;

    SELECT TOP (@MaxOccurrences)
        OccurrenceId, InstanceName, ServerName, OccurredUtc, Source, Message, InnerMessage,
        StackTrace, Url, ClientIp, TraceText
    FROM dbo.Monitoring_Exception_Occurrence
    WHERE FingerprintId = @FingerprintId
    ORDER BY OccurredUtc DESC;
END
GO

-- Claims the next ticketed fingerprint for a worker: moves it to InProgress and counts the
-- attempt. READPAST lets two workers claim different rows instead of blocking each other.
-- Returns no rows when there's nothing left to do.
CREATE OR ALTER PROCEDURE dbo.Monitoring_Claim_Next_Fingerprint
AS
BEGIN
    SET NOCOUNT ON;

    WITH next_fingerprint AS
    (
        SELECT TOP (1) *
        FROM dbo.Monitoring_Exception_Fingerprint WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE Status = 'Ticketed'
        ORDER BY OccurrenceCount DESC, FingerprintId
    )
    UPDATE next_fingerprint
    SET Status = 'InProgress',
        Attempts = Attempts + 1,
        LastAttemptUtc = SYSUTCDATETIME()
    OUTPUT inserted.FingerprintId, inserted.Hash, inserted.ExceptionType, inserted.TopFrame,
        inserted.OccurrenceCount, inserted.TicketId, inserted.Attempts, inserted.AnalysisNotes;
END
GO

-- Moves a fingerprint to a new status. Ticket, branch, PR and analysis values are only
-- changed when passed (NULL keeps the current value). Moving to Merged records the merge
-- time used by Monitoring_Log_Exception's regression check.
CREATE OR ALTER PROCEDURE dbo.Monitoring_Set_Fingerprint_Status
    @FingerprintId  int,
    @Status         varchar(20),
    @TicketId       int = NULL,
    @BranchName     nvarchar(200) = NULL,
    @PrUrl          nvarchar(500) = NULL,
    @AnalysisNotes  nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Monitoring_Exception_Fingerprint
    SET Status = @Status,
        TicketId = COALESCE(@TicketId, TicketId),
        BranchName = COALESCE(@BranchName, BranchName),
        PrUrl = COALESCE(@PrUrl, PrUrl),
        AnalysisNotes = COALESCE(@AnalysisNotes, AnalysisNotes),
        MergedUtc = CASE WHEN @Status = 'Merged' THEN SYSUTCDATETIME() ELSE MergedUtc END
    WHERE FingerprintId = @FingerprintId;
END
GO

/* ---------------------------------------------------------------------------------------- */
/* Roles                                                                                     */
/* ---------------------------------------------------------------------------------------- */

-- monitoring_writer: what each SobekCM web instance needs, and nothing more.
-- monitoring_triage: what a process that reads exceptions back and tracks their
--   workflow needs (e.g. the autofix orchestrator and worker).
-- Add each application's database user to one of these (never db_owner). Users and
-- roles share one name space, so don't give a user either of these names.
IF DATABASE_PRINCIPAL_ID('monitoring_writer') IS NULL
    CREATE ROLE monitoring_writer;
GO

IF DATABASE_PRINCIPAL_ID('monitoring_triage') IS NULL
    CREATE ROLE monitoring_triage;
GO

GRANT EXECUTE ON dbo.Monitoring_Log_Exception TO monitoring_writer;
GRANT EXECUTE ON dbo.Monitoring_Log_RateLimit_Events TO monitoring_writer;
GRANT EXECUTE ON TYPE::dbo.Monitoring_RateLimit_Event_Table TO monitoring_writer;
GO

GRANT EXECUTE ON dbo.Monitoring_Get_AutoFix_Candidates TO monitoring_triage;
GRANT EXECUTE ON dbo.Monitoring_Get_Fingerprints_By_Status TO monitoring_triage;
GRANT EXECUTE ON dbo.Monitoring_Get_Fingerprint_Detail TO monitoring_triage;
GRANT EXECUTE ON dbo.Monitoring_Claim_Next_Fingerprint TO monitoring_triage;
GRANT EXECUTE ON dbo.Monitoring_Set_Fingerprint_Status TO monitoring_triage;
GO

/* ---------------------------------------------------------------------------------------- */
/* Maintenance                                                                               */
/* ---------------------------------------------------------------------------------------- */

-- Deletes occurrence samples and rate-limiting events older than the given number of days.
-- Fingerprints themselves are kept, along with their counts and workflow history.
CREATE OR ALTER PROCEDURE dbo.Monitoring_Purge_Old
    @Days  int = 90
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff datetime2(3) = DATEADD(day, -@Days, SYSUTCDATETIME());

    DELETE FROM dbo.Monitoring_Exception_Occurrence WHERE OccurredUtc < @Cutoff;
    DELETE FROM dbo.Monitoring_RateLimit_Event WHERE OccurredUtc < @Cutoff;
END
GO
