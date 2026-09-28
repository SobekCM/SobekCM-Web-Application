-- Adds SobekCM_Get_Item_Aggregation_Statistics, a lean standalone proc that returns just an aggregation's
-- title/item/page counts. Previously these counts were pulled (when @include_counts was passed as true)
-- as a fifth result set from SobekCM_Get_Item_Aggregation2, but nothing on the .NET side actually asked for
-- them that way any more -- Item_Aggregation/Complete_Item_Aggregation no longer carry a Statistics property
-- at all. Counts are now fetched on demand only when a home page actually uses a <%PAGES%>/<%ITEMS%>/<%TITLES%>
-- directive, cached (memory + on-disk protobuf) completely separately from the aggregation object itself with
-- a one-hour expiration, and explicitly invalidated when an item is added to, or edited into/out of, a
-- collection -- see Item_Aggregation_Statistics_Cache in the SobekCM_Engine_Library project.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF object_id('SobekCM_Get_Item_Aggregation_Statistics') IS NULL EXEC ('create procedure dbo.SobekCM_Get_Item_Aggregation_Statistics as select 1;');
GO

-- Returns the title, item, and page count for a single item aggregation, by code
ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation_Statistics]
	@code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select count(distinct(I.GroupID)) as Title_Count, count(*) as Item_Count, isnull(SUM(I.[PageCount]),0) as Page_Count
	from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I, SobekCM_Item_Aggregation A
	where (A.Code = @code)
	  and (A.AggregationID = L.AggregationID)
	  and (L.ItemID = I.ItemID);

end;
GO
