# User Authentication Service

A secure, scalable, and stateless authentication API built with ASP.NET Core. Supports JWT-based authentication, refresh tokens, role-based access, and password management.

---

## **Features**

- **User Management**
  - Register, login, password reset
  - Optional email verification
  - JWT access tokens & refresh tokens
  - Role-based access (User, Admin)
  - GET `/auth/me` to fetch profile

- **Security**
  - Password hashing with strong algorithm
  - Account lockout on repeated failures
  - Token rotation and blacklisting
  - HTTPS enforced, secure headers

- **API & Docs**
  - Swagger/OpenAPI documentation
  - CORS configured for allowed clients
  - Health checks for DB and API readiness

- **Infrastructure**
  - EF Core with migrations
  - Config via appsettings + environment variables
  - Optional Redis for refresh token storage
  - Email integration (SMTP / SendGrid)

- **Non-functional Requirements**
  - Scalable & stateless API
  - Logging, monitoring, and rate limiting
  - Fast response times and robust error handling

