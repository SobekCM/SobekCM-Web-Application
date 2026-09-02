-- 5.2.0: Stored procedures for the Google Drive staging schema
-- (MARK_20260831_Add_GoogleDrive_Staging_Schema.sql).
--
-- SobekCM_User_Process deliberately does NOT get a single wide '_Edit' upsert like the
-- Item Type / Permissions Agreement admin screens -- those are saved whole from one form.
-- A process row instead gets written incrementally by background code across a lifecycle
-- (created once, then ticked forward through progress/status many times, then completed) --
-- so it gets a narrow '_Add' (insert only, mirrors the Item Type shape but there's no
-- "edit an existing process's identity" case) plus a lightweight '_Update_Status' for the
-- frequent partial updates, rather than resending the full row on every tick.
--
-- New-row id return follows the existing '@new_xxxid int output' / '@@IDENTITY' convention
-- (see mySobek_User_Group_Edit in MARK_20260827_Add_User_Submission_Fields.sql), not a
-- SELECT SCOPE_IDENTITY() result set.
--
-- Process_mySobekViewer (planned, not part of this script) needs both a user's own list
-- and an admin's system-wide list -- same split as SobekCM_Item_Type_Get_List (Submissions
-- tab checklist) vs SobekCM_Item_Type_Get_Mgmt_List (admin screen): Get_List_For_User is
-- scope-limited by @UserID and callable by anyone; Get_Mgmt_List has no user filter at all
-- and is trusted to be gated by an admin check in the C# layer, same as every other
-- Get_Mgmt_List proc in this system.
--
-- SobekCM_GoogleDrive_Staged_File_Claim_Next_Pending uses an UPDATE...OUTPUT to select and
-- mark a row Downloading in one atomic statement, so the new BackgroundService worker stays
-- correct even if more than one web app instance ends up running it.
--
-- SobekCM_User_Process_Update_Status also handles 'Skip' notification mode inline: when a
-- process lands in a terminal status, it checks the owning user's mySobek_User_Settings
-- 'ProcessNotificationMode' row and, if 'Skip', auto-stamps UserNotifiedDate right there --
-- centralized in the one proc every producer already calls, rather than requiring every
-- producer to remember it. 'Paused' needs no proc support at all -- it is a pure read-time
-- gate the tray (or someday an email job) checks before delivering, not something that
-- touches this row.
--
-- SobekCM_User_Process_Request_Cancel is two guarded UPDATEs, not a CASE/branch in one
-- statement -- a Pending row (never claimed) resolves straight to 'Cancelled', since
-- nothing is running yet that needs to notice; a Running row instead moves to 'Cancelling'
-- and waits for whatever is doing the work to notice, stop, and confirm via the existing
-- Update_Status proc (Status = 'Cancelled', StatusMessage set to a human summary like
-- "Cancelled after 3 of 12 downloads"). Both branches require IsCancelable = 1; anything
-- else (already terminal, or never cancelable) is a silent no-op, matching the
-- SobekCM_Item_Type_Delete guarded-proc precedent rather than raising an error.


-- =====================================================================================
-- SobekCM_User_Process
-- =====================================================================================

-- Insert only. Status always starts 'Pending'; DateCreated stamped here. Returns the new
-- ProcessID so the caller can immediately link it (e.g. SobekCM_GoogleDrive_Staged_File.ProcessID).
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Add]
	@UserID int,
	@ProcessType varchar(50),
	@Title nvarchar(250),
	@ScopeType varchar(20),
	@ItemID int = NULL,
	@GroupID int = NULL,
	@AggregationID int = NULL,
	@DetailsXml nvarchar(max) = NULL,
	@IsCancelable bit = 0,
	@new_processid int output
AS
BEGIN

	INSERT INTO SobekCM_User_Process
		( UserID, ProcessType, Title, Status, ScopeType, ItemID, GroupID, AggregationID, DetailsXml, IsCancelable, DateCreated )
	VALUES
		( @UserID, @ProcessType, @Title, 'Pending', @ScopeType, @ItemID, @GroupID, @AggregationID, @DetailsXml, @IsCancelable, GETDATE() );

	set @new_processid = @@IDENTITY;

END;
GO


-- Frequent, lightweight status tick -- Status transitions to 'Running' stamp DateProcessStarted
-- the first time only (left alone on later calls); Status of 'Complete', 'Error', or
-- 'Cancelled' stamps DateCompleted. @ReportLocation and @StatusMessage are both optional
-- since most ticks set neither -- StatusMessage is also how a worker reports a human summary
-- of a confirmed cancellation, e.g. "Cancelled after 3 of 12 downloads".
--
-- A terminal Status also auto-stamps UserNotifiedDate, silently, if the owning user's
-- 'ProcessNotificationMode' setting is 'Skip' -- see the schema file's notes and
-- [[project_googledrive_import_process_tracker]]. 'On' (the default when the setting is
-- absent) and 'Paused' both leave UserNotifiedDate untouched here.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Update_Status]
	@ProcessID int,
	@Status varchar(20),
	@PercentComplete int = NULL,
	@StatusMessage nvarchar(500) = NULL,
	@ReportLocation nvarchar(500) = NULL
AS
BEGIN

	DECLARE @notificationMode nvarchar(max);
	SELECT TOP (1) @notificationMode = us.Setting_Value
	FROM mySobek_User_Settings us
		INNER JOIN SobekCM_User_Process p ON p.UserID = us.UserID
	WHERE p.ProcessID = @ProcessID AND us.Setting_Key = 'ProcessNotificationMode';

	UPDATE SobekCM_User_Process
	SET Status = @Status,
		PercentComplete = COALESCE(@PercentComplete, PercentComplete),
		StatusMessage = COALESCE(@StatusMessage, StatusMessage),
		ReportLocation = COALESCE(@ReportLocation, ReportLocation),
		DateProcessStarted = CASE WHEN @Status = 'Running' AND DateProcessStarted IS NULL THEN GETDATE() ELSE DateProcessStarted END,
		DateCompleted = CASE WHEN @Status IN ('Complete', 'Error', 'Cancelled') THEN GETDATE() ELSE DateCompleted END,
		UserNotifiedDate = CASE WHEN (@Status IN ('Complete', 'Error', 'Cancelled')) AND (@notificationMode = 'Skip') THEN GETDATE() ELSE UserNotifiedDate END
	WHERE ProcessID = @ProcessID;

END;
GO


-- Requests cancellation. A Pending row (never claimed by a worker) resolves immediately --
-- nothing is running that needs to notice -- straight to 'Cancelled', with @Reason (or a
-- default) recorded as StatusMessage. A Running row instead moves to the transitional
-- 'Cancelling' and waits for whatever is doing the work to notice (checking between
-- discrete units of work, not attempting to abort mid-transfer), stop, and confirm via
-- Update_Status. Only one of the two branches will ever actually match a given row, since
-- Status is exactly one value at a time. Both require IsCancelable = 1; anything else --
-- already terminal, or never cancelable -- is a silent no-op.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Request_Cancel]
	@ProcessID int,
	@Reason nvarchar(500) = NULL
AS
BEGIN

	UPDATE SobekCM_User_Process
	SET Status = 'Cancelled',
		CancelRequestedDate = GETDATE(),
		DateCompleted = GETDATE(),
		StatusMessage = COALESCE(@Reason, 'Cancelled before starting')
	WHERE ProcessID = @ProcessID AND IsCancelable = 1 AND Status = 'Pending';

	UPDATE SobekCM_User_Process
	SET Status = 'Cancelling',
		CancelRequestedDate = GETDATE()
	WHERE ProcessID = @ProcessID AND IsCancelable = 1 AND Status = 'Running';

END;
GO


-- Fires once the chrome tray has actually delivered a completion notification for this
-- process (a toast shown, or someday an email sent), so a later poll or page refresh does
-- not deliver it again. Channel-agnostic on purpose -- it does not care which delivery
-- mechanism called it.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Mark_Notified]
	@ProcessID int
AS
BEGIN
	UPDATE SobekCM_User_Process SET UserNotifiedDate = GETDATE() WHERE ProcessID = @ProcessID;
END;
GO


-- Full single-row read, for the process tray's click-through detail and Process_mySobekViewer.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Single]
	@ProcessID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select ProcessID, UserID, ProcessType, Title, Status, PercentComplete, StatusMessage,
		ScopeType, ItemID, GroupID, AggregationID, ReportLocation, DetailsXml,
		IsCancelable, CancelRequestedDate,
		DateCreated, DateProcessStarted, DateCompleted, UserNotifiedDate
	from SobekCM_User_Process
	where ProcessID = @ProcessID;
END;
GO


-- A user's own processes -- the tray dropdown and Process_mySobekViewer's "My Processes" list.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_List_For_User]
	@UserID int,
	@ActiveOnly bit = 0
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select ProcessID, ProcessType, Title, Status, PercentComplete, StatusMessage,
		ScopeType, ItemID, GroupID, AggregationID, ReportLocation,
		IsCancelable, CancelRequestedDate,
		DateCreated, DateProcessStarted, DateCompleted, UserNotifiedDate
	from SobekCM_User_Process
	where UserID = @UserID
		and ( (@ActiveOnly = 0) OR (Status in ('Pending', 'Running')) )
	order by DateCreated desc;
END;
GO


-- Every user's processes -- Process_mySobekViewer's admin "All Processes" list. No UserID
-- filter at all; trusted to be gated by an admin check in the C# layer, same as every other
-- _Get_Mgmt_List proc in this system.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Mgmt_List]
	@ActiveOnly bit = 1
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select p.ProcessID, p.UserID, u.UserName, u.EmailAddress, p.ProcessType, p.Title, p.Status,
		p.PercentComplete, p.StatusMessage, p.ScopeType, p.ItemID, p.GroupID, p.AggregationID,
		p.ReportLocation, p.IsCancelable, p.CancelRequestedDate,
		p.DateCreated, p.DateProcessStarted, p.DateCompleted
	from SobekCM_User_Process p
		inner join mySobek_User u on p.UserID = u.UserID
	where (@ActiveOnly = 0) OR (p.Status in ('Pending', 'Running'))
	order by p.DateCreated desc;
END;
GO


-- The cheap check the chrome does inline during normal page rendering, to decide whether
-- the client should even start its poll loop -- deliberately its own tiny proc rather than
-- reusing Get_List_For_User, since this runs on every page view for every logged-in user.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Active_Count_For_User]
	@UserID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select COUNT(*) as ActiveCount
	from SobekCM_User_Process
	where UserID = @UserID and Status in ('Pending', 'Running');
END;
GO


-- Builder-side: finds the active tracked process for a given item + process type, so
-- Worker_BulkLoader can tick it to 'Running'/'Complete'/'Error' as it works through an
-- Additional-Work-Needed reprocess. Most recent active (Pending/Running) row wins -- there
-- should only ever be one, but this is defensive against a second request landing before
-- the first is picked up.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Active_For_Item]
	@ItemID int,
	@ProcessType varchar(50)
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select TOP (1) ProcessID
	from SobekCM_User_Process
	where ItemID = @ItemID and ProcessType = @ProcessType and Status in ('Pending', 'Running')
	order by DateCreated desc;
END;
GO


-- =====================================================================================
-- SobekCM_GoogleDrive_Staged_File
-- =====================================================================================

-- Insert only, fired the moment a file is picked in Google Picker. Status always starts
-- 'Pending'; QueuedDate stamped here. Returns the new StagedFileID.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Add]
	@UserID int,
	@ProcessID int,
	@DriveFileID varchar(200),
	@DriveFileName nvarchar(500),
	@FileSizeBytes bigint = NULL,
	@Priority varchar(20),
	@new_stagedfileid int output
AS
BEGIN

	INSERT INTO SobekCM_GoogleDrive_Staged_File
		( UserID, ProcessID, DriveFileID, DriveFileName, FileSizeBytes, Status, Priority, QueuedDate )
	VALUES
		( @UserID, @ProcessID, @DriveFileID, @DriveFileName, @FileSizeBytes, 'Pending', @Priority, GETDATE() );

	set @new_stagedfileid = @@IDENTITY;

END;
GO


-- Atomically claims the next pending file for the worker to download -- Interactive rows
-- always drain before Batch ones, oldest first within each. UPDATE...OUTPUT so the claim
-- and the read happen in one statement, safe even if more than one worker instance is running.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Claim_Next_Pending]
AS
BEGIN

	UPDATE TOP (1) SobekCM_GoogleDrive_Staged_File
	SET Status = 'Downloading'
	OUTPUT inserted.StagedFileID, inserted.UserID, inserted.DriveFileID, inserted.DriveFileName, inserted.Priority
	WHERE StagedFileID = (
		select TOP (1) StagedFileID
		from SobekCM_GoogleDrive_Staged_File
		where Status = 'Pending'
		order by
			case Priority when 'Interactive' then 0 else 1 end,
			QueuedDate asc
	);

END;
GO


-- Fired by the worker once a claimed download finishes (Status = 'Ready', with the GCS key
-- it landed at) or fails (Status = 'Error'). ReadyDate stamped only on success.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Update_Status]
	@StagedFileID int,
	@Status varchar(20),
	@GcsStagingObjectKey nvarchar(500) = NULL
AS
BEGIN

	UPDATE SobekCM_GoogleDrive_Staged_File
	SET Status = @Status,
		GcsStagingObjectKey = COALESCE(@GcsStagingObjectKey, GcsStagingObjectKey),
		ReadyDate = CASE WHEN @Status = 'Ready' THEN GETDATE() ELSE ReadyDate END
	WHERE StagedFileID = @StagedFileID;

END;
GO


-- The wizard Upload step's "attach from pre-staged content" list -- everything this user has
-- ready and not yet attached to an item.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Get_Ready_For_User]
	@UserID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select StagedFileID, DriveFileName, FileSizeBytes, ReadyDate
	from SobekCM_GoogleDrive_Staged_File
	where UserID = @UserID and Status = 'Ready';
END;
GO


-- =====================================================================================
-- SobekCM_Item_GoogleDrive_Staged_File_Link
-- =====================================================================================

-- Written when a staged file is attached to an item during metadata entry -- a reference
-- only. The caller is still responsible for separately calling the existing
-- SobekCM_Update_Additional_Work_Needed_Flag proc to flag the item for Builder; that stays
-- a separate call rather than folding it in here, matching how the rest of this system
-- composes several focused proc calls from C# instead of one do-everything proc.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Add]
	@ItemID int,
	@StagedFileID int,
	@TargetFileName nvarchar(500),
	@new_linkid int output
AS
BEGIN

	INSERT INTO SobekCM_Item_GoogleDrive_Staged_File_Link
		( ItemID, StagedFileID, TargetFileName, LinkedDate )
	VALUES
		( @ItemID, @StagedFileID, @TargetFileName, GETDATE() );

	set @new_linkid = @@IDENTITY;

END;
GO


-- Builder-side: everything still owed to this item, joined with the staged file so the new
-- module has the GCS key and target filename in one query.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Get_Pending_For_Item]
	@ItemID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select l.LinkID, l.TargetFileName, s.StagedFileID, s.GcsStagingObjectKey, s.DriveFileName
	from SobekCM_Item_GoogleDrive_Staged_File_Link l
		inner join SobekCM_GoogleDrive_Staged_File s on l.StagedFileID = s.StagedFileID
	where l.ItemID = @ItemID and l.ConsumedDate is null;
END;
GO


-- Fired only after the permanent copy is confirmed durably written -- not eagerly. Marks
-- both the link (ConsumedDate) and the staged file (Status = 'Consumed') together, since
-- this is the one moment both change state at once -- the point where the staged GCS copy
-- is safe to delete.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Mark_Consumed]
	@LinkID int
AS
BEGIN

	UPDATE SobekCM_Item_GoogleDrive_Staged_File_Link
	SET ConsumedDate = GETDATE()
	WHERE LinkID = @LinkID;

	UPDATE s
	SET s.Status = 'Consumed'
	FROM SobekCM_GoogleDrive_Staged_File s
		inner join SobekCM_Item_GoogleDrive_Staged_File_Link l on l.StagedFileID = s.StagedFileID
	WHERE l.LinkID = @LinkID;

END;
GO
