/* Any T-sQL random code or select statement to check integrity of the database... */
IF (NOT EXISTS(select 1 where 1 = 2))
BEGIN 
	/* @SQL:: optional */
	SELECT '@SQL::Name.of.a.script.that.may.fix.the.issue.sql' 
	/* @MESSAGE:: required */
	SELECT '@MESSAGE::A friendly message to inform the issue... for ALL databases' 
END