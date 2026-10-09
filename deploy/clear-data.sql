-- Clears all user-generated data from a Masroof database while preserving the
-- seeded category taxonomy (Categories). Safe to run repeatedly.
--
--   docker exec masroof-sql-2025 /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P '<sa-password>' -C -d Masroof -i /path/clear-data.sql
--
-- FK order: children before parents.
SET QUOTED_IDENTIFIER ON;  -- required: tables carry filtered indexes
SET NOCOUNT ON;

DELETE FROM ParseTraces;
DELETE FROM Feedback;
DELETE FROM Outbox;
DELETE FROM Transactions;
DELETE FROM MerchantRules;
DELETE FROM Accounts;
DELETE FROM Users;
-- Categories are reference data required by the parser and are intentionally kept.

PRINT 'Masroof user data cleared (categories preserved).';
