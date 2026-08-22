

alter table mySobek_User_Request add Code nvarchar(255);
GO

-- Add a link between a user and a user request
ALTER PROCEDURE [dbo].[mySobek_Add_User_Request]
	@userid int,
	@usergroupid int,
	@requestsubmitpermissions bit,
	@requesturl nvarchar(255),
	@notes nvarchar(2000),
	@code nvarchar(255)
AS
begin

	insert into mySobek_User_Request(UserID, UserGroupID, RequestSubmitPermissions, RequestUrl, Notes, Pending, RequestDate, Approved, Code )
	values ( @Userid, @usergroupid, @requestsubmitpermissions, @requesturl, @notes, 'true', getdate(), 'false', @code);
	
end;
GO

ALTER PROCEDURE [dbo].[mySobek_Get_All_Users] AS
BEGIN
	
	-- Get the list of users and pending user requests
	with pending_cte as 
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests
	from mySobek_User U left join 
		 pending_cte R on U.UserID = R.UserID 
	order by Full_Name;
END;
GO

