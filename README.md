# Smart Ordering System

A demo application for a restaurant ordering system with ASP.NET Core MVC and Python APIs.

## Setup

### Backend (ASP.NET Core)
1. Install .NET 6 SDK
2. Navigate to `SmartOrderingSystem/`
3. Run `dotnet restore`
4. Run `dotnet run`
5. Access Swagger UI at `https://localhost:5001/swagger`

### AI APIs (Python)
1. Install Python 3.8+
2. Navigate to `SmartOrderingSystem.AI/`
3. Install dependencies: `pip install -r requirements.txt`
4. Run `python voice_order_api.py` and `python feedback_analyzer_api.py`

## Features
- Menu Management (CRUD)
- Order Placement and Status Updates
- JWT Authentication (Admin, Kitchen, Customer roles)
- Simulated WhatsApp Notifications
- Voice-to-Order Parsing (Python API)
- Feedback Analysis (Python API)
- PDF Reports (Daily Summary, Order Invoice)

## Database
- In-memory for demo purposes
- SQL schema provided in `SQL/Schema.sql`

## Postman Collection
- Import `SmartOrderingSystem.postman_collection.json` into Postman
- Update `baseUrl` and `aiBaseUrl` variables as needed

## Sample Usage
1. Register a user: `POST /api/Auth/register`
2. Login to get JWT: `POST /api/Auth/login`
3. Place order: `POST /api/Order`
4. Parse voice order: `POST /parse-order` (Python API)
5. Submit feedback: `POST /api/Feedback`
6. Analyze feedback: `POST /analyze-feedback` (Python API)
7. Generate reports: `GET /api/Report/daily-summary`

## Notes
- Uses DinkToPdf for PDF generation (ensure wkhtmltopdf is installed)
- WhatsApp simulation logs to in-memory WhatsAppLogs
- SignalR not implemented for demo; polling can be used for real-time updates
```

# Notes
- **PDF Generation**: Uses DinkToPdf (requires wkhtmltopdf installed). Sample PDFs are generated in-memory and included in `SampleReports/`.
- **AI APIs**: Simplified parsing and sentiment analysis for demo purposes. In production, use NLP libraries like spaCy or TextBlob.
- **WhatsApp Simulation**: Logs messages to an in-memory WhatsAppLogs table.
- **Real-time Updates**: Not implemented (SignalR optional); client can poll `/api/Order/{id}`.
- **Database**: In-memory for demo; SQL schema provided for reference.
- **GitHub**: Project can be hosted in a GitHub repo with commits for each feature (e.g., "Add Menu CRUD", "Implement JWT Auth").
- **Demo Video**: Record a 2-minute walkthrough showing API calls via Postman and PDF generation.

This implementation provides a complete demo for client presentation, with all required features and simplified AI components.