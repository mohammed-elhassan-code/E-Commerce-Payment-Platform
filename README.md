# 🛒 E-Commerce & Payment Platform API (.NET 10)

A production-ready, scalable, and secure **E-Commerce & Payment Backend Platform** built with **.NET 10** and **Clean Architecture**. This system handles the complete e-commerce lifecycle—from inventory-guarded shopping carts and distributed caching to cryptographic identity management, transactional order processing, and secure payment webhooks.

---

## 🏗️ Architectural Overview

The solution follows strict **Clean Architecture** principles to ensure separation of concerns, maintainability, and testability:

* **`Ecommerce.Domain`**: Core enterprise entities (`Product`, `Category`, `Customer`, `CartItem`, `Coupon`, `Order`, `OrderItem`, `RefreshToken`), enums, and business rules with zero external dependencies.
* **`Ecommerce.Application`**: Business logic services (`CartService`, `OrderService`), Data Transfer Objects (DTOs), and repository/service interfaces.
* **`Ecommerce.Infrastructure`**: Data access using **Entity Framework Core**, SQL Server configurations, Repository implementations, Distributed Caching, Identity/Security services, and Payment Gateway integration.
* **`Ecommerce.Api`**: Lightweight **ASP.NET Core Minimal APIs**, Global Exception Handling middleware, CORS policies, JWT Authentication, and **Scalar** interactive API documentation.
* **`Ecommerce.Tests`**: Automated **xUnit** unit tests utilizing **Moq** to verify core business calculations and discount rules.

---

## ✨ Key Features & Security Highlights

### 🔐 Advanced Identity & Cybersecurity
* **JWT & Refresh Token Rotation**: Short-lived JWT Access Tokens paired with single-use, database-tracked Refresh Tokens (`IsUsed`, `IsRevoked`, `ExpiresAt`) to prevent token replay attacks.
* **Cryptographic Password Hashing**: Uses ASP.NET Core's `PasswordHasher` (PBKDF2 with random salting).
* **Account Verification & Recovery**: Email confirmation workflows and time-limited password reset tokens.
* **Role-Based Access Control (RBAC)**: Strict endpoint protection separating `Customer` and `Admin` privileges.
* **Real SMTP Email Delivery (MailKit)**: Integrated MailKit and MimeKit for real-time transactional emails (account activation links, password recovery, and order invoices).
* **Ownership & IDOR Protection**: Extracts authenticated user IDs directly from JWT claims (ClaimTypes.NameIdentifier) and enforces resource ownership checks across carts, orders, and payment sessions.
* **Secure Admin Registration**: Supports programmatic Admin role assignment during registration via a protected server-side secret key

### 🛍️ Smart Cart & Inventory Protection
* **Server-Side Price Computation**: Prevents client-side price tampering by fetching authentic product prices directly from the database.
* **Inventory Guard**: Prevents overselling by validating real-time warehouse stock levels before adding items or finalizing orders.
* **Temporal Coupon Engine & Caching**: Validates active coupons and expiration dates, backed by **`IDistributedCache`** (Redis/In-Memory) for sub-millisecond response times under high load.
* **Admin Inventory Restocking**: Added a role-protected PATCH /api/products/{id}/stock endpoint allowing administrators to replenish product stock levels safely

### 💳 Transactional Orders & Fintech Webhooks
* **ACID Database Transactions**: Order creation, stock deduction, tax calculation (15% VAT), and cart clearing are wrapped in explicit database transactions with automatic rollback on failure.
* **Live Stripe Checkout & Webhooks**: Integrated the official Stripe.net SDK to generate real checkout sessions and verify cryptographic Stripe-Signature headers (EventUtility.ConstructEvent).
* **Automated Expired Session Recovery**: Listens to checkout.session.expired webhook events to automatically cancel unpaid sessions and restore reserved items back to warehouse stock.
* **Automated PDF Invoicing (QuestPDF)**: Generates itemized tax invoices in-memory upon payment confirmation and sends them automatically as PDF email attachments.
* **Webhook Signature & Idempotency**: Verifies cryptographic signatures (`401 Unauthorized` on tampering) and ensures duplicate webhook events are safely ignored without double-processing orders or stock.

### 🛠️ DevOps, Resilience & Quality
* **Global Exception Handling**: Centralized `IExceptionHandler` returning standardized RFC-compliant `ProblemDetails` JSON responses without leaking internal stack traces.
* **Structured Logging (Serilog)**: Configured `Serilog` with daily rolling file sinks and structured console logging across core services (`OrderService`, `PaymentService`) for real-time observability and auditing.
* **Containerization**: Full **Docker** and **Docker Compose** setup orchestrating the .NET 10 API, SQL Server, and Redis.
* **CI/CD Automation**: Automated build and unit test verification pipeline powered by **GitHub Actions**.


---

## 🧰 Tech Stack

* **Framework**: .NET 10 / ASP.NET Core Minimal APIs
* **Database & ORM**: Microsoft SQL Server & Entity Framework Core 10
* **Caching**: `IDistributedCache` (Redis / Distributed Memory Cache)
* **Security**: JWT Bearer Authentication, `PasswordHasher`, CORS
* **Documentation**: OpenAPI & Scalar UI (`/scalar`)
* **Testing**: xUnit & Moq
* **DevOps**: Docker, Docker Compose, GitHub Actions
* **Payments, Email & PDF**: Stripe.net SDK, MailKit, QuestPDF 
* **Logging & Observability**: Serilog (`Serilog.AspNetCore`, Console & Rolling File Sinks)

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10 SDK](https://microsoft.com)
* [SQL Server](https://microsoft.com) (or Docker Desktop)

### Option 1: Run Locally
1. **Clone the repository:**
   ```bash
   git clone https://github.com
   cd E-Commerce-Payment-Platform
   ```

2. **Update the connection string** in `Ecommerce.Api/appsettings.json` if needed.

3. **Apply database migrations:**
   ```bash
   dotnet ef database update -p Ecommerce.Infrastructure -s Ecommerce.Api
   ```

4. **Run the API:**
   ```bash
   dotnet run --project Ecommerce.Api
   ```
   Navigate to `http://localhost:5123/scalar` to explore and test the endpoints interactively.

### Option 2: Run with Docker Compose
Spin up the API, SQL Server, and Redis containers with a single command:
```bash
docker-compose up --build
```

---

## 🧪 Running Automated Tests

Execute the xUnit test suite to verify business logic:
```bash
dotnet test
```
