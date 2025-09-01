# 📚 Modern E-Commerce Microservices Platform

A scalable, secure, and observable microservices-based e-commerce system built with .NET technologies. This platform enables book catalog management, order processing, and payment handling through a modern distributed architecture.

## 🏗️ System Architecture

The architecture follows microservices principles with the following components:

- **Catalog Service**: Manages book inventory with CRUD operations
- **Order Service**: Handles order creation and validation
- **Payment Service**: Processes payments and publishes stock update events
- **User Service**: Provides authentication using ASP.NET Core Identity
- **API Gateway (Ocelot)**: Single entry point for external clients
- **RabbitMQ**: Message broker for asynchronous communication
- **SQL Server Databases**: Isolated databases for each service
- **Observability Stack**: Serilog + Seq, Health Checks, Prometheus + Grafana

## 🚀 Features

- **Synchronous REST APIs** for immediate operations
- **Asynchronous event-driven communication** for decoupled tasks
- **JWT-based authentication** for secure communication
- **Centralized logging** with Serilog and Seq
- **Health monitoring** with HealthChecks UI
- **Metrics collection** with Prometheus
- **Data visualization** with Grafana
- **Containerized** with Docker

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| Backend | ASP.NET Core |
| Database | SQL Server |
| ORM | Entity Framework Core |
| API Gateway | Ocelot |
| Message Broker | RabbitMQ |
| Authentication | JWT + ASP.NET Core Identity |
| Logging | Serilog + Seq |
| Metrics | Prometheus |
| Visualization | Grafana |
| Containerization | Docker |

## 📦 Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- SQL Server (can run in Docker)

## 🚦 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/ecommerce-microservices.git
cd ecommerce-microservices
```

### 2. Run with Docker Compose

The easiest way to run the entire platform is using Docker Compose:

```bash
docker-compose up -d
```

This will start all services, databases, and the observability stack.

### 3. Manual Setup (Alternative)

If you prefer to run services individually:

```bash
# Run each service in its directory
dotnet run --project src/CatalogService
dotnet run --project src/OrderService
dotnet run --project src/PaymentService
dotnet run --project src/UserService
dotnet run --project src/ApiGateway

# Run RabbitMQ
docker run -d --hostname rabbitmq --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management

# Run Seq for logging
docker run -d --name seq -p 8081:80 datalust/seq

# Run Prometheus
docker run -d --name prometheus -p 9090:9090 -v ./prometheus.yml:/etc/prometheus/prometheus.yml prom/prometheus

# Run Grafana
docker run -d --name grafana -p 3000:3000 grafana/grafana
```

## 🔌 API Endpoints

### API Gateway Routes (Port: 7000)
- `/catalog/*` → Catalog Service
- `/orders/*` → Order Service
- `/payment/*` → Payment Service
- `/auth/*` → User Service

### Service Endpoints

**Catalog Service** (Port: 5231)
- `GET /api/books` - Retrieve all books
- `GET /api/books/{id}` - Get specific book
- `POST /api/books` - Add new book (Admin only)
- `PUT /api/books/{id}` - Update book
- `DELETE /api/books/{id}` - Remove book

**Order Service** (Port: 5001)
- `GET /api/orders` - Retrieve all orders
- `GET /api/orders/{id}` - Get specific order
- `POST /api/orders` - Create order
- `PUT /api/orders/{id}` - Update order
- `DELETE /api/orders/{id}` - Delete order

**User Service** (Port: 5215)
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and receive JWT token
- `POST /api/auth/add-role` - Assign roles to users

## 🔐 Authentication

The system uses JWT tokens for authentication:

1. Register a user: `POST /auth/register`
2. Login: `POST /auth/login` to receive JWT token
3. Include token in requests: `Authorization: Bearer {token}`

## 📊 Monitoring & Observability

- **Logs**: http://localhost:8081 (Seq)
- **Metrics**: http://localhost:9090 (Prometheus)
- **Dashboards**: http://localhost:3000 (Grafana)
- **Health Checks**: http://localhost:5005/healthchecks-ui

Default Grafana credentials: admin/admin

## 🧪 Testing the System

1. **Register a user**:
   ```bash
   curl -X POST "http://localhost:7000/auth/register" \
     -H "Content-Type: application/json" \
     -d '{"username": "testuser", "email": "test@example.com", "password": "Password123!"}'
   ```

2. **Login to get JWT token**:
   ```bash
   curl -X POST "http://localhost:7000/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username": "testuser", "password": "Password123!"}'
   ```

3. **Add a book (Admin)**:
   ```bash
   curl -X POST "http://localhost:7000/catalog/api/books" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
     -d '{"title": "Sample Book", "author": "John Doe", "price": 29.99, "stock": 100}'
   ```

4. **Create an order**:
   ```bash
   curl -X POST "http://localhost:7000/orders/api/orders" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
     -d '{"bookId": 1, "quantity": 2}'
   ```

## 🗂️ Project Structure

```
ecommerce-microservices/
├── src/
│   ├── ApiGateway/          # Ocelot API Gateway
│   ├── CatalogService/      # Book catalog management
│   ├── OrderService/        # Order processing
│   ├── PaymentService/      # Payment handling
│   ├── UserService/         # Authentication service
│   └── HealthChecksDashboard/ # Health monitoring UI
├── docker-compose.yml       # Multi-container setup
├── prometheus.yml          # Prometheus configuration
└── README.md
```

## 🔄 Workflow

1. User authenticates via User Service
2. Client requests order creation through API Gateway
3. Order Service validates order by synchronously calling Catalog Service
4. Order Service publishes OrderPlacedEvent to RabbitMQ
5. Payment Service consumes event, processes payment, publishes PaymentProcessedEvent
6. Catalog Service consumes PaymentProcessedEvent and updates stock

## 🧩 Development

### Adding a New Service

1. Create new ASP.NET Core Web API project
2. Add database context and migrations
3. Implement health check endpoint at `/health`
4. Configure Serilog logging
5. Add to docker-compose.yml
6. Register in API Gateway configuration
7. Add to HealthChecksDashboard

### Database Migrations

```bash
# Create migration
dotnet ef migrations add InitialCreate --project src/CatalogService

# Apply migration
dotnet ef database update --project src/CatalogService
```

## 📈 Scaling

The architecture supports horizontal scaling:
- Stateless services can be scaled by increasing instance count
- API Gateway can load balance between service instances
- RabbitMQ handles message distribution between consumers

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

If you have any questions or issues, please open an issue on GitHub.

---

**Happy Coding!** 🎉
