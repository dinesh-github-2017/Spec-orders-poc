-- Stand-in schema for PBI 268 until the real orders database schema is supplied.
-- Target: (localdb)\MSSQLLocalDB, database OrdersPoc
IF DB_ID('OrdersPoc') IS NULL CREATE DATABASE OrdersPoc;
GO
USE OrdersPoc;
GO
IF OBJECT_ID('dbo.Orders') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Customers') IS NOT NULL DROP TABLE dbo.Customers;
GO
CREATE TABLE dbo.Customers (
    CustomerId   nvarchar(20)  NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerName nvarchar(200) NOT NULL
);
GO
CREATE TABLE dbo.Orders (
    OrderId      bigint        NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId   nvarchar(20)  NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(CustomerId),
    OrderDateUtc datetime2(0)  NOT NULL,
    Total        decimal(18,2) NOT NULL,
    Currency     char(3)       NOT NULL CONSTRAINT DF_Orders_Currency DEFAULT ('USD'),
    Status       nvarchar(20)  NOT NULL CONSTRAINT CK_Orders_Status CHECK (Status IN ('Pending','Shipped','Delivered','Cancelled'))
);
GO
CREATE INDEX IX_Orders_CustomerId_OrderDateUtc
    ON dbo.Orders (CustomerId, OrderDateUtc DESC) INCLUDE (Total, Status);
GO
