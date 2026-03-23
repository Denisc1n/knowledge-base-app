📚 Knowledge Base App

A full-stack knowledge base application built with ASP.NET Core, MongoDB, and React.
This project demonstrates clean architecture, RESTful API design, and NoSQL data modeling.

🚀 Overview

The Knowledge Base App allows users to create, manage, search, and organize notes efficiently.

It is designed as a portfolio project to showcase:

Clean Architecture in .NET
NoSQL (MongoDB) integration
REST API best practices (CRUD + PATCH)
Validation and structured layering
Full-stack integration with React
🧱 Architecture

The backend follows a Clean Architecture (layered) approach:

src/
  KnowledgeBase.Api            → HTTP layer (controllers, contracts)
  KnowledgeBase.Application    → Business logic, services, validation
  KnowledgeBase.Domain         → Core entities and interfaces
  KnowledgeBase.Infrastructure → Database and external services
🔄 Flow
Controller → Service → Repository → MongoDB
⚙️ Tech Stack
Backend
ASP.NET Core Web API (.NET)
MongoDB Atlas
MongoDB.Driver
FluentValidation
Swagger / OpenAPI
Frontend
React (planned / in progress)
TypeScript
Vite
📦 Features
✅ Implemented
Create, read, update, delete notes
Partial updates using PATCH
Search notes (title, content, tags, category)
Input validation with FluentValidation
Clean architecture separation
MongoDB integration
🚧 Planned / Improvements
Pagination & filtering
Authentication (JWT)
Note version history
Markdown support
Favorites / pinned notes
Docker support
Full React frontend
🧾 API Endpoints
Notes
Method	Endpoint	Description
POST	/api/notes	Create a note
GET	/api/notes	Get all notes
GET	/api/notes/{id}	Get note by id
PUT	/api/notes/{id}	Full update
PATCH	/api/notes/{id}	Partial update
DELETE	/api/notes/{id}	Delete note
GET	/api/notes/search?q=	Search notes
🧠 Example Requests
Create Note
{
  "title": "Intro to CQRS",
  "content": "CQRS separates commands from queries.",
  "tags": ["architecture", "cqrs", "dotnet"],
  "category": "Backend",
  "status": 1
}
Patch Note
{
  "title": "Updated CQRS Notes"
}
⚙️ Configuration
🔐 Environment Setup

This project uses local configuration for secrets.

Create a file:

src/KnowledgeBase.Api/appsettings.json

Or use the example:

cp appsettings.Example.json appsettings.json
Example Configuration
{
  "MongoDb": {
    "ConnectionString": "mongodb+srv://<user>:<password>@cluster.mongodb.net/",
    "DatabaseName": "KnowledgeBaseDb",
    "NotesCollectionName": "notes"
  }
}
🔑 Using User Secrets (recommended)
cd src/KnowledgeBase.Api
dotnet user-secrets init

dotnet user-secrets set "MongoDb:ConnectionString" "your_connection_string"
dotnet user-secrets set "MongoDb:DatabaseName" "KnowledgeBaseDb"
dotnet user-secrets set "MongoDb:NotesCollectionName" "notes"
▶️ Running the Project
1. Restore dependencies
dotnet restore
2. Run the API
dotnet run --project src/KnowledgeBase.Api
3. Open Swagger
https://localhost:<port>/swagger
🧪 Testing

Use Swagger or Postman to test endpoints.

🧩 Design Decisions
🔹 MongoDB + NoSQL
Flexible schema for notes
Efficient for tagging and search scenarios
🔹 PATCH vs PUT
PUT → full replacement
PATCH → partial updates using MongoDB update definitions
🔹 Clean Architecture
Keeps business logic independent from infrastructure
Improves maintainability and testability
📌 Future Improvements
Add pagination and filtering
Implement authentication and authorization
Add version history for notes
Improve search with text indexes
Dockerize the application
Deploy to cloud (Azure / AWS)
🤝 Contributing

This project is for learning and portfolio purposes, but contributions and suggestions are welcome.

📄 License

This project is licensed under the MIT License.

👨‍💻 Author

Developed as part of a portfolio to demonstrate backend and full-stack capabilities with .NET and modern web technologies.