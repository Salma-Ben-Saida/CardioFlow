IF OBJECT_ID('dbo.Resources', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Resources (
		ResourceId INT IDENTITY(1,1) PRIMARY KEY,
		Name NVARCHAR(255) NULL,
		CreatedAt DATETIME2 NULL
	);
END

SELECT TOP 20 *
FROM dbo.Resources;
