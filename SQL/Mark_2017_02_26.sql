
CREATE TABLE dbo.SobekCM_Item_Aggregation_Result_Types (
	ItemAggregationResultTypeID int IDENTITY(1,1) NOT NULL,
	ResultType varchar(50) NOT NULL,
	DefaultOrder int NOT NULL DEFAULT ((100)),
	DefaultView bit NOT NULL DEFAULT ('false'),
 CONSTRAINT PK_SobekCM_Item_Aggregation_Result_Types PRIMARY KEY CLUSTERED ( ItemAggregationResultTypeID ASC ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT SobekCM_Item_Aggregation_Result_Types_Unique UNIQUE NONCLUSTERED ( ResultType ASC ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE dbo.SobekCM_Item_Aggregation_Result_Views (
	ItemAggregationResultID int IDENTITY(1,1) NOT NULL,
	AggregationID int NOT NULL,
	ItemAggregationResultTypeID int NOT NULL,
	DefaultView bit NOT NULL DEFAULT('false'),
 CONSTRAINT PK_SobekCM_Item_Aggregation_Result_Views PRIMARY KEY CLUSTERED ( ItemAggregationResultID ASC ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE dbo.SobekCM_Item_Aggregation_Result_Views ADD CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Views_AggregationID FOREIGN KEY(AggregationID) REFERENCES [dbo].SobekCM_Item_Aggregation ([AggregationID])
GO

ALTER TABLE dbo.SobekCM_Item_Aggregation_Result_Views ADD CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Views_TypeID FOREIGN KEY(ItemAggregationResultTypeID) REFERENCES dbo.SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID)
GO

-- Add all the standard result types
if ( ( select count(*) from SobekCM_Item_Aggregation_Result_Types ) = 0 )
begin
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView ) values ( 'BRIEF', 1, 1 );
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView ) values ( 'THUMBNAIL', 2, 1 );
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView ) values ( 'TABLE', 3, 1 );
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView ) values ( 'EXPORT', 4, 0 );
	insert into SobekCM_Item_Aggregation_Result_Types ( ResultType, DefaultOrder, DefaultView ) values ( 'GMAP', 5, 1 );
end;
GO

-- Add all the standard result types to the aggregations
insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
select ItemAggregationResultTypeID, AggregationID, 'false'
from SobekCM_Item_Aggregation_Result_Types T, SobekCM_Item_Aggregation A
where ( T.DefaultView = 'true' )
  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views V
                     where V.AggregationID=A.AggregationID 
					   and V.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID));

-- Also, set defaults
declare @briefid int;
set @briefid = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types T where T.ResultType='BRIEF' );

if ( coalesce(@briefid, -1) > 0 )
begin
	with aggrs_no_default as
	( 
		select AggregationID 
		from SobekCM_Item_Aggregation A
		where not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views V where V.AggregationID = A.AggregationID and V.DefaultView='true')
	)
	update SobekCM_Item_Aggregation_Result_Views
	set DefaultView='true'
	where ( exists ( select 1 from aggrs_no_default D where D.AggregationID=SobekCM_Item_Aggregation_Result_Views.AggregationID ))
	  and ( ItemAggregationResultTypeID = @briefid );
end;
GO


alter table 






