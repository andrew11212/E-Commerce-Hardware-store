# 🖥️ Future Technology E-Commerce

A modern hardware e-commerce platform built with ASP.NET Core MVC.

## 📁 Project Structure

```
E-Commerce-Hardware-store/
├── .github/                    # GitHub workflows and configurations
│   └── workflows/              # CI/CD pipelines
├── .vscode/                    # VS Code settings
├── FutureTechnologyE-Commerce/ # Main ASP.NET Core MVC Project
│   ├── Areas/                  # ASP.NET Core Areas
│   │   └── Identity/           # Identity UI pages
│   ├── Controllers/            # MVC Controllers
│   ├── Data/                   # Database context and configurations
│   ├── docs/                   # Project documentation
│   │   ├── ADMIN_NAVIGATION.md
│   │   ├── ADMIN_SEEDING.md
│   │   ├── OPTIMIZATION_SUMMARY.md
│   │   ├── PERFORMANCE_OPTIMIZATION.md
│   │   ├── PERFORMANCE_OPTIMIZATION_GUIDE.md
│   │   └── SETUP_REDIS.md
│   ├── Migrations/             # EF Core database migrations
│   ├── Models/                 # Entity models and ViewModels
│   │   ├── DTOs/               # Data Transfer Objects
│   │   └── ViewModels/         # View-specific models
│   ├── Properties/             # Project properties (launchSettings.json)
│   ├── Repository/             # Repository pattern implementation
│   │   └── IRepository/        # Repository interfaces
│   ├── Services/               # Business logic services
│   ├── Utility/                # Utility classes and helpers
│   ├── Views/                  # Razor views
│   │   ├── Admin/              # Admin dashboard views
│   │   ├── Brands/             # Brand management views
│   │   ├── Cart/               # Shopping cart views
│   │   ├── Category/           # Category views
│   │   ├── Checkout/           # Checkout process views
│   │   ├── Home/               # Homepage and product listing
│   │   ├── Inventory/          # Inventory management views
│   │   ├── Laptops/            # Laptop-specific views
│   │   ├── Notifications/      # Notification views
│   │   ├── Order/              # Order management views
│   │   ├── Product/            # Product detail views
│   │   ├── Promotions/         # Promotion views
│   │   ├── Review/             # Review views
│   │   └── Shared/             # Shared layouts and partials
│   ├── wwwroot/                # Static files (CSS, JS, images)
│   │   ├── css/                # Stylesheets
│   │   ├── images/             # Image assets
│   │   ├── js/                 # JavaScript files
│   │   ├── lib/                # Third-party libraries
│   │   └── scripts/            # Utility scripts
│   ├── Program.cs              # Application entry point
│   ├── web.config              # IIS configuration
│   └── FutureTechnologyE-Commerce.csproj
├── .gitattributes
├── .gitignore
├── FutureTechnologyE-Commerce.sln
└── README.md
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server or PostgreSQL
- Redis (optional, for caching)

### Running the Application

1. Clone the repository:
   ```bash
   git clone https://github.com/andrew11212/E-Commerce-Hardware-store.git
   ```

2. Navigate to the solution directory:
   ```bash
   cd E-Commerce-Hardware-store
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Update database:
   ```bash
   dotnet ef database update --project FutureTechnologyE-Commerce
   ```

5. Run the application:
   ```bash
   dotnet run --project FutureTechnologyE-Commerce
   ```

## 📖 Documentation

Detailed documentation can be found in the `FutureTechnologyE-Commerce/docs/` folder:

- **ADMIN_SEEDING.md** - How to seed admin users
- **ADMIN_NAVIGATION.md** - Admin panel navigation guide
- **SETUP_REDIS.md** - Redis caching setup instructions
- **PERFORMANCE_OPTIMIZATION.md** - Performance tuning guide
- **OPTIMIZATION_SUMMARY.md** - Optimization summary

## 🛠️ Architecture

The project follows a clean architecture pattern with:

- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management
- **Service Layer** - Business logic separation
- **MVC Pattern** - Web presentation layer

## 📝 License

This project is for educational purposes.
