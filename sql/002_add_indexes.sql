-- TaxCopilot Database Indexes
-- Version: 1.0
-- Description: Creates indexes for optimal query performance

-- Index on AuditLogs.CreatedAt for recent logs queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID('AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt
    ON AuditLogs (CreatedAt DESC);

    PRINT 'Created IX_AuditLogs_CreatedAt index';
END
ELSE
BEGIN
    PRINT 'IX_AuditLogs_CreatedAt index already exists';
END
GO

-- Composite index on Documents for filtering queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_Jurisdiction_TaxType_Version' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documents_Jurisdiction_TaxType_Version
    ON Documents (Jurisdiction, TaxType, Version)
    INCLUDE (Title, Status, CreatedAt);

    PRINT 'Created IX_Documents_Jurisdiction_TaxType_Version index';
END
ELSE
BEGIN
    PRINT 'IX_Documents_Jurisdiction_TaxType_Version index already exists';
END
GO

-- Index on Documents.Status for filtering by processing status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_Status' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documents_Status
    ON Documents (Status)
    INCLUDE (DocumentId, Title, CreatedAt);

    PRINT 'Created IX_Documents_Status index';
END
ELSE
BEGIN
    PRINT 'IX_Documents_Status index already exists';
END
GO

-- Index on Documents.CreatedAt for sorting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_CreatedAt' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documents_CreatedAt
    ON Documents (CreatedAt DESC);

    PRINT 'Created IX_Documents_CreatedAt index';
END
ELSE
BEGIN
    PRINT 'IX_Documents_CreatedAt index already exists';
END
GO

-- Index on AuditLogs.CorrelationId for tracing
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_CorrelationId' AND object_id = OBJECT_ID('AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_CorrelationId
    ON AuditLogs (CorrelationId);

    PRINT 'Created IX_AuditLogs_CorrelationId index';
END
ELSE
BEGIN
    PRINT 'IX_AuditLogs_CorrelationId index already exists';
END
GO

PRINT 'Index creation complete';
GO

