

select * from SobekCM_Builder_Module where ModuleSetID = 3 and [Enabled]='true' order by [Order];

update SobekCM_Builder_Module set [Enabled]='false' where ModuleID in ( 3,4,5,6,7,8,9,10,11,12,15,16,39,18,19,20,21,22,23,24,25, 26, 27);
update SobekCM_Item set AdditionalWorkNeeded='true' where Deleted='false';


update SobekCM_Builder_Module set [Enabled]='true' where ModuleID in ( 3,4,5,6,7,8,9,10,11,12,15,16,39,18,19,20,21,22,23,24,25, 26, 27);
update SobekCM_Builder_Module set [Enabled]='true' where ModuleID in ( 13);


select count(*) from SObekCM_Item where AdditionalWorkNeeded='true'; -- 85301

