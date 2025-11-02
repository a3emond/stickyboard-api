# StickyBoard API

StickyBoard is a personal learning project demonstrating full-stack development across modern backend, mobile, and web technologies. This repository contains the backend API for the StickyBoard ecosystem, self-hosted at:

[https://stickyboard.aedev.pro/api](https://stickyboard.aedev.pro/api)

The focus of this backend is clean domain organization, real relational modeling, modular services, and authentication, resembling the architecture of modern collaborative workspace applications.

Note: This is a student project in active development. It is functional and self-hosted, but not production-grade and continues to evolve.

## Purpose and Scope

StickyBoard API powers a collaborative board system including:

* Users and authentication (JWT and refresh tokens)
* Organizations and memberships
* Boards and folders
* Views (tabs), sections, and cards
* Card comments and board chat messaging
* Social graph (user relations)
* Invitation system
* Role-based access controls

The API emphasizes:

* DTO-first API contracts
* Service layer domain logic
* Repository layer with raw SQL via Npgsql
* PostgreSQL enum mapping
* Soft-delete audit trails
* Background worker hooks

## Architecture Overview

| Layer            | Description                                   |
| ---------------- | --------------------------------------------- |
| Controllers      | HTTP routing, maps requests to services       |
| Services         | Business rules, permissions, validation       |
| Repositories     | Raw SQL queries through Npgsql                |
| DTOs             | Input and output models for HTTP payloads     |
| Models           | Domain entities mapped to PostgreSQL tables   |
| Auth             | JWT authentication and refresh token rotation |
| Soft Delete      | Deleted flag with retained history            |
| PostgreSQL Enums | Mapped in Program.cs at startup               |

The project intentionally avoids Entity Framework to reinforce SQL proficiency and provide full control over schema and data operations.

## Key Modules

### Authentication and Users

* Login and refresh tokens
* User profiles
* Basic preferences and avatar fields

### Organizations

* Create and manage organizations
* Member roles and permissions

### Boards

* Organize boards in folders
* Board visibility modes
* Owner and organizational context

### Tabs and Sections

* Tabs represent different board views
* Sections allow structure within tabs

### Cards

* Flexible content via JSON fields
* Tags, status, dates, version numbers
* Assignment and creator tracking

### Collaboration

* Card comments
* Board message threads

### Social Graph and Invitations

* User relations
* Invite flow for boards and organizations

## Database

The backend uses PostgreSQL with:

* UUID primary keys
* JSONB columns for flexible metadata
* Enum types for roles and workflows
* Cascading deletes where appropriate
* Soft deletes and update triggers

Schema diagrams and SQL scripts are maintained in the repository and generated using PlantUML.

## Live Documentation

API documentation is available at:

[https://stickyboard.aedev.pro/api/swagger](https://stickyboard.aedev.pro/api/swagger)

A static HTML reference is also included at:

docs/StickyBoard-API.html

Additional detailed documentation will expand in:

docs/api

docs/diagrams

docs/models

docs/worker

## Hosting

The API is self-hosted on a personal server using HTTPS and a reverse proxy. While reliable for demonstration and development, it is not hardened for enterprise deployment.

This project helps build experience with end-to-end backend deployment and server administration.

## Roadmap

Planned work includes:

* Real-time updates via WebSockets or SignalR
* Background job processing
* Offline-friendly sync strategies
* Expanded permission system
* Additional view modes and card types
* Automated testing

## Project Ecosystem

| Component                   | Status             |
| --------------------------- | ------------------ |
| StickyBoard API (this repo) | Active development |
| Background Worker           | In progress        |
| iOS Client                  | Prototype          |
| Android Client              | Prototype          |
| Web and desktop interface   | Planned            |

This project aims to build skills across backend engineering, database architecture, authentication systems, cross-platform development, and deployment.

## License and Status

This project is for learning and portfolio purposes. No warranties or guarantees are provided. Development is active and ongoing.
