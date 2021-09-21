/* Any T-sQL random code or select statement to check integrity of the database... */
IF (NOT EXISTS(select 1 where 1 = 2))
BEGIN 
	/* @SQL:: optional */
	SELECT '@SQL::Name.of.a.script.that.may.fix.the.issue.sql' 
	/* @MESSAGE:: required */
	SELECT '@WARNING::A not-so-friendly message to inform some issue... DEV database only' 
	SELECT '@WARNING::Another warning line or may be a data error, you could block the app from being used ;) ... DEV database only' 
END