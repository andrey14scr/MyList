CREATE TABLE [dbo].[Table]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Note] VARCHAR(MAX) NOT NULL, 
    [Date] DATE NOT NULL, 
    [Time] TIME NOT NULL, 
    [IsRepeat] BIT NOT NULL, 
    [IsDo] BIT NOT NULL
)
