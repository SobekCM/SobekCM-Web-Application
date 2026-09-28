-- Adds a second flag, AdditionalWork_MetadataOnly, alongside the existing AdditionalWorkNeeded
-- flag on SobekCM_Item. AdditionalWorkNeeded alone tells the Builder "look at this item again"
-- for any reason (new pages, changed permissions, embargo expiration, etc); the new flag narrows
-- that down to "the only outstanding work is metadata", so a Builder module can later tell a
-- metadata-only pass apart from a full reprocessing pass. Defaults to 'false' so every existing
-- row -- and every existing caller that never sets it -- is unaffected.
--
-- SobekCM_Set_Item_Visibility and Admin_Unembargo_Items_Past_Embargo_Date both set
-- AdditionalWorkNeeded directly (not through the proc below), and both are updated here to flag
-- AdditionalWork_MetadataOnly at the same time -- visibility/embargo changes don't touch any
-- files, so the outstanding work they flag is metadata-only. But in both procs, and in the one
-- below, that flag is only ever a claim of "this particular change is metadata-only" -- it must
-- never overwrite an existing TRUE-AdditionalWorkNeeded/FALSE-AdditionalWork_MetadataOnly row
-- back to metadata-only, since that would silently downgrade an item that still has real,
-- non-metadata work outstanding (e.g. new pages attached) from a prior, unrelated flagging call.
-- All three writers therefore only ever narrow the flag (AND semantics): metadata-only sticks
-- only if every flagging call since the item was last cleared agreed it was metadata-only; a
-- single non-metadata-only call latches AdditionalWork_MetadataOnly to FALSE until the next clear.

if ( NOT EXISTS (select * from sys.columns where Name = N'AdditionalWork_MetadataOnly' and Object_ID = Object_ID(N'SobekCM_Item')))
begin
	alter table dbo.SobekCM_Item add AdditionalWork_MetadataOnly bit not null default('false');
end;
GO

-- Now takes a second flag to set alongside the existing one. Clearing AdditionalWorkNeeded
-- (@newflag = 0) always clears AdditionalWork_MetadataOnly too, regardless of @metadataOnly --
-- a cleared item has no outstanding work of any kind, so the two flags can never end up with
-- AdditionalWorkNeeded false and AdditionalWork_MetadataOnly true.
--
-- Setting @newflag = 1 does NOT just overwrite AdditionalWork_MetadataOnly with @metadataOnly --
-- if the item is already flagged (AdditionalWorkNeeded was already 1), the new value is ANDed
-- with whatever is already there, so a metadata-only call (@metadataOnly = 1) can never clear a
-- previously-flagged non-metadata-only need back to "metadata only". Only a fresh flagging call
-- (item wasn't previously flagged at all) takes @metadataOnly at face value.
ALTER procedure [dbo].[SobekCM_Update_Additional_Work_Needed_Flag]
	@itemid int,
	@newflag bit,
	@metadataOnly bit
as
begin
	if ( @newflag = 0 )
	begin
		update SobekCM_Item set AdditionalWorkNeeded = @newflag, AdditionalWork_MetadataOnly = 0 where ItemID = @itemid;
	end
	else
	begin
		update SobekCM_Item
		set AdditionalWorkNeeded = @newflag,
		    AdditionalWork_MetadataOnly = CASE WHEN AdditionalWorkNeeded = 0 THEN @metadataOnly ELSE (AdditionalWork_MetadataOnly & @metadataOnly) END
		where ItemID = @itemid;
	end;
end;
GO

-- Also return the new metadata-only flag alongside the existing item info, so the Builder can
-- tell a metadata-only pass apart from a full reprocessing pass.
ALTER procedure [dbo].[SobekCM_Get_Items_Needing_Aditional_Work]
as
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the bibid, vid, primary key, and metadata-only flag to the items which are flagged
	select G.BibID, I.VID, I.ItemID, I.AdditionalWork_MetadataOnly
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	    and ( I.AdditionalWorkNeeded = 'true' )
	order by BibID, VID;
end;
GO

-- Also flags visibility/embargo changes as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = 'true' -- but only if the item wasn't already flagged for something
-- else; if it was, that prior flag is left untouched rather than downgraded to metadata-only
-- (see the note above SobekCM_Update_Additional_Work_Needed_Flag). Full body reproduced below
-- since SQL Server requires the complete procedure text on ALTER -- the only change from the
-- prior definition is the added AdditionalWork_MetadataOnly logic in the "Update the main item
-- table" step.
ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Visibility]
	@ItemID int,
	@IpRestrictionMask smallint,
	@DarkFlag bit,
	@EmbargoDate datetime,
	@User varchar(255)
AS
BEGIN

	-- Build the note text and value
	declare @noteText varchar(200);
	set @noteText = '';

	-- Set the embargo date
	if ( @EmbargoDate is null )
	begin
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID and EmbargoEnd is not null ) )
		begin
			update Tracking_Item set EmbargoEnd=null where ItemID=@ItemID;

			set @noteText = 'Embargo date removed.  ';
		end;
	end
	else
	begin
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID ) )
		begin
			update Tracking_Item set EmbargoEnd=@EmbargoDate where ItemID=@ItemID;
		end
		else
		begin
			insert into Tracking_Item ( ItemID, Original_EmbargoEnd, EmbargoEnd )
			values ( @ItemID, @EmbargoDate, @EmbargoDate );
		end;

		set @noteText = 'Embargo date of ' + convert(varchar(20), @EmbargoDate, 102) + '.  ';
	end;

	-- Set the workflow id
	declare @workflowId int;
	set @workflowId = 34;
	if ( @IpRestrictionMask < 0 )
		set @workflowId = 35;
	if ( @IpRestrictionMask < 0 )
		set @workflowId = 36;
	if ( @DarkFlag = 'true' )
	begin
		set @workflowId = 35;
		set @noteText = @noteText + 'Item made dark.';
	end;

	-- Update the main item table (and set for the builder to review this)
	update SobekCM_Item
	set IP_Restriction_Mask = @IpRestrictionMask, Dark = @DarkFlag, AdditionalWorkNeeded = 'true',
	    AdditionalWork_MetadataOnly = CASE WHEN AdditionalWorkNeeded = 0 THEN 1 ELSE AdditionalWork_MetadataOnly END
	where ItemID=@ItemID;

	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	values ( @ItemID, @workflowId, getdate(), @User, @noteText, getdate() );

	-- If this is being made public, set the public data
	if ( ( @DarkFlag = 'false' ) and ( @IpRestrictionMask >= 0 ) )
	begin
		update SobekCM_Item
		set MadePublicDate = coalesce(MadePublicDate, getdate())
		where ItemID=@ItemID;
	end;
END;
GO

-- Also flags bulk-unembargoed items as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = 'true' -- but only if not already flagged for something else, same
-- non-downgrading rule as SobekCM_Set_Item_Visibility above. Full body reproduced below since
-- SQL Server requires the complete procedure text on ALTER -- the only change from the prior
-- definition is the added AdditionalWork_MetadataOnly logic in the "Actually mark the items as
-- unembargoed" step.
ALTER PROCEDURE [dbo].[Admin_Unembargo_Items_Past_Embargo_Date]
	@subject_line varchar(500),
	@email_message varchar(max),
	@send_email bit
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;

	-- Get the items that need to be processed
	select I.ItemID, G.BibID, I.VID, CONVERT(nvarchar(10), T.EmbargoEnd, 102) as EmbargoEnd, substring(I.Title, 4, 1000) as Title, substring(I.Author, 4, 1000) as Author
	into #Unembargo_Items
	from SobekCM_Item I, Tracking_Item T, SobekCM_Item_Group G
	where ( I.ItemID=T.ItemID )
	    and ( ( I.IP_Restriction_Mask <> 0 ) or ( I.Dark = 'true' ) )
	    and ( T.EmbargoEnd < getdate() )
	    and ( I.GroupID = G.GroupID );

	-- One row per (item, owning-aggregation-contact-email) pair, with the HTML blurb for that item.
	-- Title carried through here so the next step can order the concatenation by it, matching the original cursor's "order by Title".
	select distinct U.ItemID, U.Title, A.ContactEmail,
	              '<br /><br /><i>' + U.Title + '</i>, by ' + U.Author + ' ( ' + U.BibID + ':' + U.VID + ' ) - ' + U.EmbargoEnd as ItemBlurb
	into #Item_Aggregation_Emails
	from #Unembargo_Items U inner join
	          SobekCM_Item_Aggregation_Item_Link L on L.ItemID = U.ItemID and L.impliedLink = 'false' inner join
	          SobekCM_Item_Aggregation A on A.AggregationID = L.AggregationID and len(A.ContactEmail) > 0;

	-- Collapse to one row per contact email, items concatenated in Title order (matches the original cursor's "order by Title").
	-- NOTE FOR POSTGRESQL PORT: WITHIN GROUP (ORDER BY ...) is SQL-Server-only syntax.
	-- PostgreSQL equivalent: string_agg(ItemBlurb, '' ORDER BY Title ASC)
	select ContactEmail as EmailAddress, string_agg(ItemBlurb, '') WITHIN GROUP (ORDER BY Title ASC) as ItemList
	into #EmailPrep
	from #Item_Aggregation_Emails
	group by ContactEmail;

	-- Actually mark the items as unembargoed next
	update SobekCM_Item
	set Dark='false', IP_Restriction_Mask=0, AdditionalWorkNeeded='true',
	    AdditionalWork_MetadataOnly = CASE WHEN AdditionalWorkNeeded = 0 THEN 1 ELSE AdditionalWork_MetadataOnly END
	where exists ( select * from #Unembargo_Items T where T.ItemID=SobekCM_Item.ItemID );

	-- Also add a workflow progress for this
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	select ItemID, 34, getdate(), 'Builder Service', 'Automatically unembargoed ( original unembargo date of ' + EmbargoEnd + ' )', getdate()
	from #Unembargo_Items;

	-- Send emails via database email?
	if ( @send_email = 'true' )
	begin
		declare @emailaddress varchar(255);
		declare @itemlist varchar(max);
		declare @emailbody varchar(max);

		declare email_cursor cursor for
		select EmailAddress, ItemList
		from #EmailPrep;

		open email_cursor;
		fetch next from email_cursor into @emailaddress, @itemlist;

		while ( @@FETCH_STATUS = 0 )
		begin
			set @emailbody = REPLACE(@email_message, '{0}', @itemlist);
			exec [SobekCM_Send_Email] @emailaddress, @subject_line, @emailbody, null, null, 'true', 'false', -1, -1;
			fetch next from email_cursor into @emailaddress, @itemlist;
		end;
		close email_cursor;
		deallocate email_cursor;
	end;

	-- Return the list of items unembargoed
	select * from #Unembargo_Items;

	-- Return the email information as well
	select * from #EmailPrep;

	-- Drop the temporary tables
	drop table #Unembargo_Items;
	drop table #Item_Aggregation_Emails;
	drop table #EmailPrep;
END;
GO
