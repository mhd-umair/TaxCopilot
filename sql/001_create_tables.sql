-- TaxCopilot Database Schema
-- Version: 1.0
-- Description: Creates the core tables for the TaxCopilot RAG system

-- Documents table - stores metadata for uploaded tax documents
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Documents')
BEGIN
    CREATE TABLE Documents (
        DocumentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Title NVARCHAR(500) NOT NULL,
        FileName NVARCHAR(500) NOT NULL,
        BlobUrl NVARCHAR(2000) NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,
        FileSizeBytes BIGINT NOT NULL,
        Jurisdiction NVARCHAR(100) NOT NULL,
        TaxType NVARCHAR(100) NOT NULL,
        Version NVARCHAR(50) NOT NULL,
        EffectiveDate DATETIMEOFFSET NULL,
        UploadedBy NVARCHAR(255) NOT NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET NULL,
        Status INT NOT NULL DEFAULT 0, -- 0=Uploaded, 1=Processing, 2=Indexed, 3=Failed
        ChunkCount INT NULL,
        IndexedAt DATETIMEOFFSET NULL
    );

    PRINT 'Created Documents table';
END
ELSE
BEGIN
    PRINT 'Documents table already exists';
END
GO

-- AuditLogs table - stores query audit logs for RAG operations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        AuditLogId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CorrelationId NVARCHAR(100) NOT NULL,
        QueryText NVARCHAR(MAX) NOT NULL,
        FiltersJson NVARCHAR(MAX) NULL,
        RetrievedChunksJson NVARCHAR(MAX) NULL,
        Model NVARCHAR(100) NOT NULL,
        PromptVersion NVARCHAR(50) NOT NULL,
        AnswerText NVARCHAR(MAX) NULL,
        LatencyMs INT NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    PRINT 'Created AuditLogs table';
END
ELSE
BEGIN
    PRINT 'AuditLogs table already exists';
END
GO

-- Document status reference (for documentation)
-- Status values:
-- 0 = Uploaded - Document has been uploaded but not processed
-- 1 = Processing - Document is currently being ingested
-- 2 = Indexed - Document has been successfully indexed
-- 3 = Failed - Document ingestion failed

PRINT 'Schema creation complete';
GO

