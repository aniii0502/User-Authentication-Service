# 🔐 Authentication & Authorization Service

A secure, scalable authentication and authorization microservice built with **JWT, refresh tokens, and role-based access control (RBAC)**.  
This service provides the foundation for user registration, login, token management, and admin-level access.

---

## 📌 Functional Requirements

- **Authentication & User Management**
  - Register new users
  - Optional email verification
  - Login with credentials
  - Access token issuance (**JWT**)
  - Refresh token support
  - Password reset workflow
  - `GET /me` endpoint for authenticated user info

- **Role & Claims**
  - **User** and **Admin** roles
  - Required fields:
    - `username`
    - `email`
    - `password`
    - `createdAt`

---

## 📌 Non-Functional Requirements

- **Scalability** – stateless API, supports horizontal scaling
- **Security** – HTTPS only, JWT & refresh tokens, strong password hashing
- **Performance** – low-latency authentication, optimized response times
- **Observability** – structured logs & monitoring
- **Reliability** – token expiry, refresh token rotation
- **Rate Limiting** – prevent brute-force & abuse
- **Compliance** – follow security best practices & standards

---

## 📌 API Surface & Contracts

### Endpoints

| Method | Endpoint          | Description                   | Auth Required |
|--------|------------------|-------------------------------|---------------|
| POST   | `/auth/register` | Register a new user           | ❌            |
| POST   | `/auth/login`    | Authenticate user & issue JWT | ❌            |
| POST   | `/auth/refresh`  | Get new access token          | ❌            |
| POST   | `/auth/logout`   | Revoke refresh token          | ✅            |
| POST   | `/auth/reset`    | Request password reset        | ❌            |
| GET    | `/auth/me`       | Get current user profile      | ✅            |
| GET    | `/admin/users`   | List all users (Admin only)   | ✅ (Admin)    |

