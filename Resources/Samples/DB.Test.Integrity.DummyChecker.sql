/* Any T-sQL random code or select statement to check integrity of the database... */
IF (NOT EXISTS(select 1 from project))
BEGIN 
	SELECT '@WARNING::No test data loaded in the Project table. Please run the data setup scripts.' 	
END