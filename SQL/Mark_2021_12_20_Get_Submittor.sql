CREATE PROCEDURE [dbo].SobekCM_Get_Submittor
	@bibid varchar(20),
	@vid varchar(10)
AS
BEGIN

	declare @itemid int;
	set @itemid = ( select ItemID from SobekCM_Item_Group G, SobekCM_Item I where I.GroupID = G.GroupID and BibID=@bibid and VID=@vid);

	select U.FirstName + ' ' + U.LastName as UserName, EmailAddress, U.UserID
	from Tracking_Progress P inner join
		 Tracking_Workflow W on P.WorkFlowID=W.WorkFlowID inner join
		 mySobek_User U on U.UserID=P.WorkPerformedById
	where P.ItemID=@itemid 
	  and W.WorkFlowName='Online Submit';

END;
GO
