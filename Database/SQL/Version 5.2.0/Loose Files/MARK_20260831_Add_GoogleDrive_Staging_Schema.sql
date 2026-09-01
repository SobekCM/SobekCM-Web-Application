-- 5.2.0: Google Drive import -- lets a submitter pull large masters (TIFFs, audio, video)
-- out of their own Google Drive ahead of time, into a per-user GCS staging area, then
-- attach from that staged list during the new submission wizard's Upload step instead of
-- browser-uploading them (which is bounded by Kestrel's MaxRequestBodySize).
--
-- Three tables:
--
-- SobekCM_User_Process is deliberately generic, not Google-Drive-specific -- it backs a
-- planned "running processes" indicator in the site chrome (next to the login/user link
-- in HeaderFooter_HtmlHelper.cs), styled after the Google Cloud Console notification
-- tray: an icon showing pending work, a click-through list, and a completion toast.
-- Google Drive downloads are the first process type to use it, but it is meant to cover
-- any slow user-initiated request, not just this one -- item reprocessing, a Drive
-- staging pull, a large spreadsheet import, a MARC21 report built from search criteria,
-- a weak-metadata audit across a user's materials, and whatever gets added after that.
-- ProcessType is a free string rather than an enum column so new process types never
-- need a schema change. Status/UserID is indexed because the chrome's poll-or-not
-- decision depends on a cheap "does this user have anything active" check done inline
-- during normal page rendering (no separate probe request, no polling at all for users
-- with nothing running) -- see [[project_googledrive_import_process_tracker]] for the
-- full client-side design.
--
-- Scope is deliberately three typed FK columns plus a discriminator rather than one
-- polymorphic (TypeCode, ID) pair -- keeps real FK/cascade integrity for the three known
-- entity levels a process can run against (a single item, a whole item group/series, an
-- aggregation/collection), while ScopeType = 'Custom' (all three columns NULL) covers
-- everything else -- a saved search, arbitrary criteria -- with the specifics living in
-- DetailsXml instead of forcing a fourth kind of ID column that doesn't reference
-- anything. DetailsXml itself is nvarchar(max) holding serialized XML, matching the
-- existing SobekCM_Metadata_Block.BlockXml convention (not a native xml column) --
-- untyped and unqueryable, the known tradeoff of a field like this, but it is what makes
-- one table cover five-plus wildly different process shapes without a column explosion.
-- ReportLocation is the process's output artifact when it has one (an imported
-- spreadsheet with BibID/VID stamped back in, a generated MARC21 report file) --
-- NULL for process types with no discrete output, like a plain reprocess.
--
-- DateCreated (queued) is kept separate from DateProcessStarted (a worker actually
-- picked it up) because a Batch-priority job (see SobekCM_GoogleDrive_Staged_File.
-- Priority) can sit queued behind Interactive work for a while before it starts.
--
-- Notification delivery: UserNotifiedDate (renamed from a plain Notified bit) doubles as
-- flag and timestamp, same convention as the other DateX columns here -- and deliberately
-- channel-agnostic, so it means "notified, by whatever means eventually did it" rather than
-- "toast shown." A process that completes while nobody is logged in needs no special
-- queuing logic -- it simply stays NULL, exactly as honest as "not yet delivered," until
-- the next real delivery opportunity (a login, or someday an email digest) stamps it.
-- The user's global notification preference ('On' | 'Paused' | 'Skip' -- one setting, not
-- per-ProcessType, confirmed) lives as an ordinary mySobek_User_Settings row
-- (Setting_Key = 'ProcessNotificationMode'), not a new column here, following the same
-- precedent as the Type-grid sort-order preference elsewhere in this same 5.2.0 work.
-- 'Paused' needs no schema support at all -- it is purely a "don't deliver right now" gate
-- checked at read/delivery time (the tray, someday an email job), leaving UserNotifiedDate
-- untouched so everything is still there once switched back to 'On'. 'Skip' is the one that
-- needs active handling, baked directly into SobekCM_User_Process_Update_Status: when a
-- process reaches a terminal status, that proc checks the owning user's preference and, if
-- 'Skip', auto-stamps UserNotifiedDate right then -- centralized in the one proc every
-- producer already calls, rather than requiring every current and future producer to
-- remember to do it themselves.
--
-- Cancellation: IsCancelable is set by the producer at creation, defaulting to 0 -- a
-- process only gets a Cancel button once its producer has actually been written to notice
-- and honor a cancel request, not just because it happens to be a type that theoretically
-- could support it. ItemReprocessing's two producers never pass 1. CancelRequestedDate
-- doubles as both flag and timestamp (same convention as ConsumedDate on the Link table
-- below), and is only ever set on a row where IsCancelable = 1 (enforced by the CHECK
-- constraint, not just caller discipline). A process still Pending when cancelled resolves
-- immediately straight to Status = 'Cancelled', since nothing is running yet that needs to
-- notice; one already Running instead moves to the transitional Status = 'Cancelling' and
-- waits for whatever is doing the work to actually stop and confirm -- see
-- SobekCM_User_Process_Request_Cancel and the worker contract note in
-- [[project_googledrive_import_process_tracker]]. StatusMessage (renamed from an
-- error-only field) carries either a real error, or a human summary of how far a
-- cancelled process got before stopping, e.g. "Cancelled after 3 of 12 downloads" --
-- useful well beyond Google Drive imports, for any process type that works through
-- multiple internal items (a spreadsheet import's rows, a weak-metadata audit's items).
--
-- SobekCM_GoogleDrive_Staged_File is the pre-staging area itself. Rows are created the
-- moment a user picks files in the Google Picker (before any item exists) and updated by
-- a new background worker (a BackgroundService in the SobekCM web app, not the Builder --
-- deliberately kept off the Builder's queue, which can be busy for hours with OCR/JP2/
-- conversion work having nothing to do with an interactive submitter waiting on a small
-- pull) as it streams each file from Drive straight into GCS. Priority distinguishes an
-- "I'm sitting here waiting" quick pull (a single small file mid-submission) from an
-- "I queued 12 files and I'm going to go build 12 items" batch pull -- the worker always
-- drains Interactive rows first.
--
-- SobekCM_Item_GoogleDrive_Staged_File_Link is written when a staged file is attached to
-- an item during metadata entry. It is a REFERENCE, not a copy -- attaching does not move
-- any bytes. The item is marked Additional_Work_Needed (existing flag, existing proc:
-- SobekCM_Update_Additional_Work_Needed_Flag) and a new Builder module, running inside
-- the existing ItemProcessModules chain that Worker_BulkLoader.Complete_Any_Recent_Loads_
-- Requiring_Additional_Work already drives, looks up unconsumed links for the item and
-- copies each staged GCS object down into the same local resource folder that reprocessing
-- already expects (Image_Server_Network\{file_root}\{vid}), before the rest of that
-- module chain runs unchanged. ConsumedDate is set -- and the staged GCS object deleted --
-- only after the permanent copy is confirmed written, not eagerly, so a crash mid-pipeline
-- never loses the only remaining copy of a file pulled from someone's personal Drive.
--
-- OAuth client credentials for Drive access are deliberately NOT stored in these tables
-- or in SobekCM_Settings -- following the same precedent as the GCS service-account key
-- (kept off the DB for credential-sensitivity reasons), the Drive OAuth client secret
-- lives in the same file/env convention. Only non-sensitive per-instance config (staging
-- bucket name, staging object prefix, storage class) belongs in SobekCM_Settings, added
-- in a separate loose script the same way the GCS settings rows were.


-- =====================================================================================
-- SobekCM_User_Process -- generic long-running-process tracker, backs the chrome's
-- process tray/toast. Not specific to Google Drive.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_User_Process](
	[ProcessID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ProcessType] [varchar](50) NOT NULL,			-- e.g. 'GoogleDriveImport', 'ItemReprocessing', 'SpreadsheetImport', 'Marc21Report', 'WeakMetadataAudit' -- open string, new types need no schema change
	[Title] [nvarchar](250) NOT NULL,				-- user-facing text, e.g. 'Downloading masters from Google Drive'
	[Status] [varchar](20) NOT NULL,				-- 'Pending' | 'Running' | 'Cancelling' | 'Cancelled' | 'Complete' | 'Error'
	[PercentComplete] [int] NULL,
	[StatusMessage] [nvarchar](500) NULL,			-- an error detail, OR a human summary of how far a cancelled process got, e.g. "Cancelled after 3 of 12 downloads"
	[ScopeType] [varchar](20) NOT NULL,			-- 'Item' | 'Group' | 'Aggregation' | 'Custom' -- which ID column (if any) applies; 'Custom' means none of them do and DetailsXml carries the specifics
	[ItemID] [int] NULL,
	[GroupID] [int] NULL,
	[AggregationID] [int] NULL,
	[ReportLocation] [nvarchar](500) NULL,			-- link to the process's output artifact, if any
	[DetailsXml] [nvarchar](max) NULL,				-- process-specific structured detail that doesn't earn its own column
	[IsCancelable] [bit] NOT NULL DEFAULT 0,		-- set by the producer at creation -- a process is only cancelable once its producer has actually been written to honor a cancel request
	[CancelRequestedDate] [datetime] NULL,			-- doubles as flag and timestamp, same convention as ConsumedDate on the Link table below
	[DateCreated] [datetime] NOT NULL,				-- when the request was queued
	[DateProcessStarted] [datetime] NULL,			-- when a worker actually picked it up
	[DateCompleted] [datetime] NULL,
	[UserNotifiedDate] [datetime] NULL,			-- doubles as flag and timestamp -- "notified, by whatever means eventually did it" (a tray toast today, maybe email later), not tied to one delivery channel
 CONSTRAINT [PK_SobekCM_User_Process] PRIMARY KEY CLUSTERED ([ProcessID] ASC),
 CONSTRAINT [FK_SobekCM_User_Process_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_User_Process_Item] FOREIGN KEY([ItemID]) REFERENCES [dbo].[SobekCM_Item] ([ItemID]),
 CONSTRAINT [FK_SobekCM_User_Process_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[SobekCM_Item_Group] ([GroupID]),
 CONSTRAINT [FK_SobekCM_User_Process_Aggregation] FOREIGN KEY([AggregationID]) REFERENCES [dbo].[SobekCM_Item_Aggregation] ([AggregationID]),
 CONSTRAINT [CK_SobekCM_User_Process_Scope] CHECK (
	([ScopeType] = 'Item' AND [ItemID] IS NOT NULL AND [GroupID] IS NULL AND [AggregationID] IS NULL) OR
	([ScopeType] = 'Group' AND [GroupID] IS NOT NULL AND [ItemID] IS NULL AND [AggregationID] IS NULL) OR
	([ScopeType] = 'Aggregation' AND [AggregationID] IS NOT NULL AND [ItemID] IS NULL AND [GroupID] IS NULL) OR
	([ScopeType] = 'Custom' AND [ItemID] IS NULL AND [GroupID] IS NULL AND [AggregationID] IS NULL)
 ),
 CONSTRAINT [CK_SobekCM_User_Process_CancelRequiresCancelable] CHECK (
	([CancelRequestedDate] IS NULL) OR ([IsCancelable] = 1)
 )
);
GO

CREATE INDEX [IX_SobekCM_User_Process_User_Status] ON [dbo].[SobekCM_User_Process] ([UserID], [Status]);
GO


-- =====================================================================================
-- SobekCM_GoogleDrive_Staged_File -- the pre-staging area. A row exists from the moment
-- a file is picked in Google Picker, independent of any item.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_GoogleDrive_Staged_File](
	[StagedFileID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ProcessID] [int] NULL,						-- the SobekCM_User_Process row tracking this file's download
	[DriveFileID] [varchar](200) NOT NULL,
	[DriveFileName] [nvarchar](500) NOT NULL,
	[FileSizeBytes] [bigint] NULL,
	[GcsStagingObjectKey] [nvarchar](500) NULL,	-- populated once the worker finishes the pull
	[Status] [varchar](20) NOT NULL,				-- 'Pending' | 'Downloading' | 'Ready' | 'Error' | 'Consumed'
	[Priority] [varchar](20) NOT NULL,				-- 'Interactive' | 'Batch' -- worker always drains Interactive first
	[QueuedDate] [datetime] NOT NULL,
	[ReadyDate] [datetime] NULL,
 CONSTRAINT [PK_SobekCM_GoogleDrive_Staged_File] PRIMARY KEY CLUSTERED ([StagedFileID] ASC),
 CONSTRAINT [FK_SobekCM_GoogleDrive_Staged_File_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_GoogleDrive_Staged_File_Process] FOREIGN KEY([ProcessID]) REFERENCES [dbo].[SobekCM_User_Process] ([ProcessID])
);
GO

CREATE INDEX [IX_SobekCM_GoogleDrive_Staged_File_User_Status] ON [dbo].[SobekCM_GoogleDrive_Staged_File] ([UserID], [Status]);
GO


-- =====================================================================================
-- SobekCM_Item_GoogleDrive_Staged_File_Link -- attach-time reference from an item to a
-- staged file. Written when the user picks a staged file during metadata entry; a
-- reference only, no bytes moved yet. Consumed by the new Builder module that runs
-- inside the existing Additional-Work-Needed ItemProcessModules chain.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link](
	[LinkID] [int] IDENTITY(1,1) NOT NULL,
	[ItemID] [int] NOT NULL,
	[StagedFileID] [int] NOT NULL,
	[TargetFileName] [nvarchar](500) NOT NULL,		-- filename to use once copied into the item's resource folder
	[LinkedDate] [datetime] NOT NULL,
	[ConsumedDate] [datetime] NULL,				-- set only after the permanent copy is confirmed written -- not eagerly, so a mid-pipeline crash can't lose the only copy
 CONSTRAINT [PK_SobekCM_Item_GoogleDrive_Staged_File_Link] PRIMARY KEY CLUSTERED ([LinkID] ASC),
 CONSTRAINT [FK_SobekCM_Item_GoogleDrive_Staged_File_Link_Item] FOREIGN KEY([ItemID]) REFERENCES [dbo].[SobekCM_Item] ([ItemID]),
 CONSTRAINT [FK_SobekCM_Item_GoogleDrive_Staged_File_Link_StagedFile] FOREIGN KEY([StagedFileID]) REFERENCES [dbo].[SobekCM_GoogleDrive_Staged_File] ([StagedFileID])
);
GO

CREATE INDEX [IX_SobekCM_Item_GoogleDrive_Staged_File_Link_Item_Unconsumed] ON [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link] ([ItemID]) WHERE [ConsumedDate] IS NULL;
GO


-- CRUD stored procedures (staged-file listing, process tray polling, link consumption)
-- are intentionally not part of this script -- they follow once the wizard's Upload-step
-- UI and the new Builder module's exact query shapes are worked out, matching how the
-- Item Type schema/procs were split into separate loose files above.
