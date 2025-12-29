# TaxCopilot

A production-ready Retrieval-Augmented Generation (RAG) system for tax document analysis built with .NET 8, OpenAI, Azure AI Search, and a modern Blazor UI.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?style=flat&logo=blazor)
![OpenAI](https://img.shields.io/badge/OpenAI-API-412991?style=flat&logo=openai)
![License](https://img.shields.io/badge/License-MIT-green.svg)

## Features

- 📄 **Document Ingestion** - Upload PDF and DOCX files, extract text with page preservation
- 🔍 **Vector Search** - Azure AI Search with hybrid vector + keyword search
- 🤖 **RAG Q&A** - Ask questions with grounded, citation-backed answers
- 🎨 **Modern UI** - Clean Blazor Server dashboard with MudBlazor
- 📊 **Audit Logging** - Full query history with performance metrics
- 💾 **Flexible Storage** - Local file storage or Azure Blob Storage

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     TaxCopilot.Ui (Blazor)                      │
└─────────────────────────────────────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────────────────────────────────────────┐
│                     TaxCopilot.Api (Web API)                    │
├─────────────────────────────────────────────────────────────────┤
│  Documents │ Chat │ Admin │ Audit                               │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                   TaxCopilot.Infrastructure                     │
├──────────────┬──────────────┬──────────────┬────────────────────┤
│ Local/Azure  │ Azure AI     │ OpenAI       │ Dapper             │
│ Storage      │ Search       │ API          │ (SQL)              │
└──────────────┴──────────────┴──────────────┴────────────────────┘
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (Local or Azure SQL)
- [Azure AI Search](https://azure.microsoft.com/services/search/) service
- [OpenAI API Key](https://platform.openai.com/api-keys)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/TaxCopilot.git
cd TaxCopilot
```

### 2. Set Up the Database

Create a SQL Server database and run the schema scripts:

```bash
# SQL Server (Windows Auth)
sqlcmd -S localhost -d TaxCopilot -E -i sql/001_create_tables.sql
sqlcmd -S localhost -d TaxCopilot -E -i sql/002_add_indexes.sql

# SQL Server (SQL Auth)
sqlcmd -S localhost -d TaxCopilot -U sa -P YourPassword -i sql/001_create_tables.sql
sqlcmd -S localhost -d TaxCopilot -U sa -P YourPassword -i sql/002_add_indexes.sql
```

### 3. Configure Environment Variables

**⚠️ Never commit API keys to source control!** Use environment variables:

#### Windows (PowerShell)

```powershell
# OpenAI
$env:OpenAI__ApiKey = "sk-your-openai-api-key"

# Azure AI Search
$env:AzureSearch__Endpoint = "https://your-search-service.search.windows.net"
$env:AzureSearch__Key = "your-azure-search-admin-key"

# SQL Server
$env:ConnectionStrings__Sql = "Server=localhost;Database=TaxCopilot;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### Windows (Command Prompt)

```cmd
set OpenAI__ApiKey=sk-your-openai-api-key
set AzureSearch__Endpoint=https://your-search-service.search.windows.net
set AzureSearch__Key=your-azure-search-admin-key
set ConnectionStrings__Sql=Server=localhost;Database=TaxCopilot;Trusted_Connection=True;TrustServerCertificate=True;
```

#### Linux / macOS

```bash
export OpenAI__ApiKey="sk-your-openai-api-key"
export AzureSearch__Endpoint="https://your-search-service.search.windows.net"
export AzureSearch__Key="your-azure-search-admin-key"
export ConnectionStrings__Sql="Server=localhost;Database=TaxCopilot;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

#### Using a `.env` file (Development)

Create a `src/TaxCopilot.Api/.env` file (add to `.gitignore`):

```env
OpenAI__ApiKey=sk-your-openai-api-key
AzureSearch__Endpoint=https://your-search-service.search.windows.net
AzureSearch__Key=your-azure-search-admin-key
ConnectionStrings__Sql=Server=localhost;Database=TaxCopilot;Trusted_Connection=True;TrustServerCertificate=True;
```

#### Using User Secrets (Recommended for Development)

```bash
cd src/TaxCopilot.Api

# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-openai-api-key"
dotnet user-secrets set "AzureSearch:Endpoint" "https://your-search-service.search.windows.net"
dotnet user-secrets set "AzureSearch:Key" "your-azure-search-admin-key"
dotnet user-secrets set "ConnectionStrings:Sql" "Server=localhost;Database=TaxCopilot;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 4. Build and Run

```bash
# Build
dotnet build

# Run API (Terminal 1)
cd src/TaxCopilot.Api
dotnet run

# Run UI (Terminal 2)
cd src/TaxCopilot.Ui
dotnet run
```

### 5. Access the Application

- **UI**: https://localhost:7001
- **API**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

## Configuration Reference

### All Configuration Options

| Setting | Environment Variable | Description |
|---------|---------------------|-------------|
| `ConnectionStrings:Sql` | `ConnectionStrings__Sql` | SQL Server connection string |
| `OpenAI:ApiKey` | `OpenAI__ApiKey` | OpenAI API key |
| `OpenAI:ChatModel` | `OpenAI__ChatModel` | Chat model (default: `gpt-4o`) |
| `OpenAI:EmbeddingModel` | `OpenAI__EmbeddingModel` | Embedding model (default: `text-embedding-3-small`) |
| `OpenAI:EmbeddingDimensions` | `OpenAI__EmbeddingDimensions` | Embedding dimensions (default: `1536`) |
| `AzureSearch:Endpoint` | `AzureSearch__Endpoint` | Azure AI Search endpoint URL |
| `AzureSearch:Key` | `AzureSearch__Key` | Azure AI Search admin key |
| `AzureSearch:IndexName` | `AzureSearch__IndexName` | Index name (default: `tax-documents`) |
| `Storage:Provider` | `Storage__Provider` | `Local` or `Azure` (default: `Local`) |
| `LocalStorage:BasePath` | `LocalStorage__BasePath` | Local storage path (default: `./storage`) |
| `AzureBlob:ConnectionString` | `AzureBlob__ConnectionString` | Azure Blob connection string |
| `AzureBlob:ContainerName` | `AzureBlob__ContainerName` | Container name (default: `tax-docs`) |

### Example appsettings.json (without secrets)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Storage": {
    "Provider": "Local"
  },
  "LocalStorage": {
    "BasePath": "./storage",
    "DocumentsFolder": "documents"
  },
  "OpenAI": {
    "ChatModel": "gpt-4o",
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingDimensions": 1536
  },
  "AzureSearch": {
    "IndexName": "tax-documents"
  },
  "Rag": {
    "ChunkSizeChars": 3500,
    "ChunkOverlapChars": 400,
    "TopK": 12,
    "ContextChunks": 8
  }
}
```

## Project Structure

```
TaxCopilot/
├── TaxCopilot.sln
├── README.md
├── .gitignore
├── sql/
│   ├── 001_create_tables.sql      # Database schema
│   └── 002_add_indexes.sql        # Performance indexes
└── src/
    ├── TaxCopilot.Api/            # ASP.NET Core Web API
    ├── TaxCopilot.Application/    # Business logic & interfaces
    ├── TaxCopilot.Infrastructure/ # External service implementations
    ├── TaxCopilot.Domain/         # Entity definitions
    ├── TaxCopilot.Contracts/      # Shared DTOs
    └── TaxCopilot.Ui/             # Blazor Server UI
```

## Usage Workflow

1. **Initialize** - Dashboard → "Initialize Services" (creates search index)
2. **Upload** - Documents → Upload a PDF or DOCX file
3. **Ingest** - Ingest → Enter Document ID → "Ingest Document"
4. **Ask** - Ask → Type a question → Get AI-powered answer with citations
5. **Audit** - Audit → View query history and performance metrics

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/admin/init` | Initialize Azure resources |
| `GET` | `/api/admin/health` | Health check |
| `POST` | `/api/documents/upload` | Upload document |
| `GET` | `/api/documents` | List all documents |
| `GET` | `/api/documents/{id}` | Get document by ID |
| `POST` | `/api/documents/{id}/ingest` | Ingest document |
| `POST` | `/api/chat/ask` | Ask a question (RAG) |
| `GET` | `/api/audit` | Get audit logs |

## Troubleshooting

### "OpenAI API key not configured"

Ensure the environment variable is set:
```bash
echo $OpenAI__ApiKey  # Linux/macOS
echo %OpenAI__ApiKey% # Windows CMD
$env:OpenAI__ApiKey   # PowerShell
```

### "Failed to connect to Azure Search"

1. Verify the endpoint URL includes `https://`
2. Check that you're using the **Admin Key** (not Query Key)
3. Ensure the search service allows your IP

### "SQL Connection Failed"

1. Verify SQL Server is running
2. Check the connection string format
3. For Windows Auth, ensure `Trusted_Connection=True`

### Local Storage Not Working

Ensure the application has write permissions to the `./storage` directory.

## Security Best Practices

1. **Never commit secrets** - Use environment variables or user secrets
2. **Use HTTPS** - Always in production
3. **Rotate keys regularly** - Especially API keys
4. **Limit permissions** - Use least-privilege access
5. **Monitor usage** - Check OpenAI and Azure dashboards

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [MudBlazor](https://mudblazor.com/) - Blazor component library
- [Dapper](https://github.com/DapperLib/Dapper) - Micro ORM
- [PdfPig](https://github.com/UglyToad/PdfPig) - PDF text extraction
- [OpenAI](https://openai.com/) - AI models
- [Azure AI Search](https://azure.microsoft.com/services/search/) - Vector search
