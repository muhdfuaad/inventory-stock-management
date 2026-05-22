# 📦 Inventory Stock Management System

A full-stack inventory management application built with **ASP.NET Core 8**, featuring a **REST API** backend and a **Blazor Server** frontend with a modern, responsive admin dashboard UI.

## 🏗️ Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
InventorySystem.sln
├── InventorySystem.Domain          # Entities & domain models
├── InventorySystem.Application     # Business logic, DTOs, services & interfaces
├── InventorySystem.Infrastructure  # EF Core, database context, migrations & seeding
├── InventorySystem.API             # REST API controllers & middleware
└── InventorySystem.Blazor          # Blazor Server frontend (UI)
```

### Layer Responsibilities

| Layer | Purpose |
|-------|---------|
| **Domain** | Core entities: `Product`, `ProductVariant`, `ProductVariantCombination`, `Stock` |
| **Application** | Service interfaces, DTOs, business logic, pagination helpers |
| **Infrastructure** | EF Core `AppDbContext`, migrations, data seeding, dependency injection |
| **API** | RESTful controllers for Products, Variants, and Stock management |
| **Blazor** | Interactive Server-Side Rendered UI with responsive admin dashboard |

## ✨ Features

- **Product Management** — Create, view, edit, and delete products
- **Variant System** — Add product variants (e.g., Size, Color) with multiple options
- **Combination Generator** — Auto-generate all variant combinations for a product
- **Stock Tracking** — Add/remove stock for specific product combinations
- **Paginated Product List** — Server-side pagination with stat cards
- **Responsive UI** — Desktop sidebar + mobile hamburger drawer layout
- **Swagger API Docs** — Interactive API documentation in development mode
- **Structured Logging** — Serilog with console output
- **Database Seeding** — Sample data auto-seeded on first run

## 🛠️ Tech Stack

- **Runtime:** .NET 8
- **Backend:** ASP.NET Core Minimal API with Controllers
- **Frontend:** Blazor Server (Interactive Server Render Mode)
- **Database:** SQL Server (via Entity Framework Core)
- **Logging:** Serilog
- **CSS:** Custom design system (Inter font, Bootstrap Icons, responsive flexbox layout)
- **API Docs:** Swagger / Swashbuckle

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (local or Docker)

### Database Setup

Update the connection string in `InventorySystem.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=InventoryDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

Migrations are applied automatically on startup.

### Running the Application

You need to run **both** projects simultaneously:

**Terminal 1 — API Server (port 7002):**

```bash
cd InventorySystem.API
dotnet run
```

**Terminal 2 — Blazor Frontend (port 5080):**

```bash
cd InventorySystem.Blazor
dotnet run
```

Then open **http://localhost:5080** in your browser.

### API Documentation

Swagger UI is available at **http://localhost:7002/swagger** when running in Development mode.

## 📡 API Endpoints

### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/products?page=1&pageSize=10` | List products (paginated) |
| `GET` | `/api/products/{id}` | Get product details |
| `POST` | `/api/products` | Create a new product |
| `PUT` | `/api/products/{id}` | Update a product |
| `DELETE` | `/api/products/{id}` | Delete a product |

### Variants

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/products/{productId}/variants` | Add a variant to a product |
| `PUT` | `/api/variants/{id}` | Update a variant |
| `DELETE` | `/api/variants/{id}` | Delete a variant |

### Stock

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/stock/{productId}` | Get stock for all combinations of a product |
| `POST` | `/api/stock/add` | Add stock to a combination |
| `POST` | `/api/stock/remove` | Remove stock from a combination |

## 🖥️ UI Layout

### Desktop
- Fixed sidebar navigation (250px) on the left
- Main content area with page headers, stat cards, and data tables

### Mobile / Tablet
- Collapsible sidebar drawer with smooth slide-in animation
- Hamburger menu button in a fixed top header bar
- Overlay backdrop that closes sidebar on tap
- Card-style table rows for responsive data display
- Touch-friendly button sizes (42px min-height)

## 📁 Project Structure

```
InventorySystem.Blazor/
├── Components/
│   ├── App.razor                  # Root HTML document
│   └── Layout/
│       ├── MainLayout.razor       # App shell (sidebar + header + content)
│       └── NavMenu.razor          # Navigation component
├── Pages/
│   ├── Products/
│   │   ├── Index.razor            # Product list with pagination
│   │   ├── Create.razor           # Create product form
│   │   └── Detail.razor           # Product detail (edit, variants, combinations)
│   └── Stock/
│       ├── AddStock.razor         # Add stock form
│       └── RemoveStock.razor      # Remove stock form
├── Blazor/
│   ├── Models/ApiModels.cs        # DTOs for API communication
│   └── Services/ApiService.cs     # HTTP client for API calls
└── wwwroot/
    └── app.css                    # Complete design system & responsive styles
```

## 📄 License

This project is part of a machine test for **Vikncodes**.
