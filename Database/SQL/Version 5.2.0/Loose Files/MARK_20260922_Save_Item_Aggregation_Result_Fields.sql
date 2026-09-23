-- Adds SobekCM_Save_Item_Aggregation_Result_Fields, so collection admins can choose the result fields shown
-- with each title in a collection's brief results view (and in the thumbnail view's hover tooltip, which shows
-- the same fields) from the Results tab.  Nothing wrote to SobekCM_Item_Aggregation_Result_Fields before this,
-- so every collection used the install-wide defaults.  The fields are stored against the collection's BRIEF
-- and THUMBNAIL result views, and the TABLE view (which does not show them) keeps the defaults.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF object_id('SobekCM_Save_Item_Aggregation_Result_Fields') IS NULL EXEC ('create procedure dbo.SobekCM_Save_Item_Aggregation_Result_Fields as select 1;');
GO

-- Saves the result fields customized for an item aggregation's brief and thumbnail results views.  With
-- @use_defaults set, just removes any customized fields, so the install-wide defaults are used.  Otherwise
-- each line of @fields is a metadata type id, a tab, then an optional override display term (blank to use
-- the standard term), in display order.
ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_Result_Fields]
	@code varchar(20),
	@use_defaults bit,
	@fields nvarchar(max)
AS
begin transaction

	-- Only continue if there is a match on the aggregation code
	declare @id int;
	set @id = ( select AggregationID from SobekCM_Item_Aggregation where Code = @code );

	if ( @id is not null )
	begin
		-- Get this aggregation's brief and thumbnail views, which share the same fields
		declare @views table ( ItemAggregationResultID int primary key );
		insert into @views
		select V.ItemAggregationResultID
		from SobekCM_Item_Aggregation_Result_Views V, SobekCM_Item_Aggregation_Result_Types T
		where ( V.AggregationID = @id )
		  and ( V.ItemAggregationResultTypeID = T.ItemAggregationResultTypeID )
		  and ( T.ResultType in ( 'BRIEF', 'THUMBNAIL' ));

		-- Remove the existing customized fields
		delete from SobekCM_Item_Aggregation_Result_Fields
		where ItemAggregationResultID in ( select ItemAggregationResultID from @views );

		-- Add the new fields, unless going back to the defaults
		if ( isnull(@use_defaults, 'false') = 'false' )
		begin
			declare @remaining nvarchar(max);
			declare @line nvarchar(max);
			declare @newline int;
			declare @tab int;
			declare @metadataid smallint;
			declare @term nvarchar(255);
			declare @order int;
			set @remaining = isnull(@fields, '');
			set @order = 0;

			-- Step through each line
			while ( len(@remaining) > 0 )
			begin
				-- Pull off the next line
				set @newline = charindex(char(10), @remaining);
				if ( @newline = 0 ) set @newline = len(@remaining) + 1;
				set @line = left(@remaining, @newline - 1);
				set @remaining = substring(@remaining, @newline + 1, len(@remaining));

				-- Split it into the metadata type id and the override display term
				set @tab = charindex(char(9), @line);
				if ( @tab > 0 )
				begin
					set @metadataid = try_cast(left(@line, @tab - 1) as smallint);
					set @term = left(ltrim(rtrim(substring(@line, @tab + 1, len(@line)))), 255);
				end
				else
				begin
					set @metadataid = try_cast(@line as smallint);
					set @term = '';
				end;

				-- Add this field to each view, skipping unknown metadata types and repeats
				if (( @metadataid is not null )
				  and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataTypeID = @metadataid ))
				  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields F, @views V where F.ItemAggregationResultID = V.ItemAggregationResultID and F.MetadataTypeID = @metadataid )))
				begin
					set @order = @order + 1;

					insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
					select ItemAggregationResultID, @metadataid, nullif(@term, ''), @order, null
					from @views;
				end;
			end;
		end;
	end;

commit transaction;
GO
