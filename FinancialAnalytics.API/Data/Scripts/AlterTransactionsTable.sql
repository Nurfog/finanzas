ALTER TABLE Transactions MODIFY COLUMN LocationId int NULL;
ALTER TABLE Transactions DROP FOREIGN KEY FK_Transactions_Locations_LocationId;
ALTER TABLE Transactions ADD CONSTRAINT FK_Transactions_Locations_LocationId FOREIGN KEY (LocationId) REFERENCES Locations(Id) ON DELETE RESTRICT;
