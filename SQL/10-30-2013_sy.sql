USE [SobekDB]
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Item_Bib_VID_from_ItemID]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[Tracking_Get_Item_Info_from_ItemID]
@itemID int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

-- Return the item information
SELECT I.VID, G.BibID, I.Title
FROM SobekCM_Item I, SobekCM_Item_Group G
WHERE I.GroupID = G.GroupID
   AND I.ItemID =@itemID

--Return the item Tracking history information  
   
END