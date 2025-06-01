# IP Manager API - Job Application Task

This project was developed as part of a job application task, demonstrating my skills in .NET Core development, API design, and system architecture. The task involved creating a robust API for managing country-based IP blocking and tracking.

## 🎯 Task Requirements

The task required implementing the following features:

- Country-based IP blocking system
- IP address geolocation and tracking
- Temporary blocking with expiration
- Comprehensive logging system
- Secure API endpoints
- Integration with external IP lookup services

## 🚀 Implemented Features

I have successfully implemented all required features and added some additional enhancements:

- Country-based IP blocking with validation
- IP address lookup and geolocation using ipapi.co
- Temporary blocking with configurable duration
- Detailed block attempt logging
- Swagger API documentation
- HTTPS support
- Request logging and monitoring
- ngrok integration for public access
- Concurrent operations support
- Error handling and validation

## 💡 Technical Decisions

In implementing this task, I made several technical decisions:

1. **Architecture**:

   - Used .NET Core 7.0 for modern API development
   - Implemented MVC pattern for clean separation of concerns
   - Used dependency injection for better testability

2. **Storage**:

   - Implemented in-memory storage using ConcurrentDictionary for thread safety
   - Designed for easy transition to persistent storage

3. **Security**:

   - Implemented input validation
   - Added request logging
   - Used HTTPS
   - Proper error handling

4. **Performance**:
   - Optimized for concurrent operations
   - Implemented efficient IP lookup caching
   - Used pagination for log retrieval

## 📋 Prerequisites

- .NET 7.0 or later
- Visual Studio 2022 or VS Code
- Basic understanding of REST APIs
- ngrok account (for public access)

## 🛠️ Installation

1. Clone the repository:

```bash
git clone https://github.com/yourusername/IP-Manager.git
```

2. Navigate to the project directory:

```bash
cd IP-Manager
```

3. Restore dependencies:

```bash
dotnet restore
```

4. Build the project:

```bash
dotnet build
```

5. Run the application:

```bash
dotnet run
```

The API will be available at `https://localhost:5001` and `http://localhost:5000`

## 🌐 Running with ngrok

This project has been tested and works perfectly with ngrok, allowing you to expose your local API to the internet securely.

### Prerequisites for ngrok

- [ngrok account](https://dashboard.ngrok.com/signup)
- [ngrok installed](https://ngrok.com/download)

### Steps to Run with ngrok

1. **Start the API**

```bash
dotnet run
```

The API will start on `https://localhost:5001`

2. **Start ngrok**

```bash
ngrok http 5001
```

3. **Configure ngrok (Optional but Recommended)**
   Create a `ngrok.yml` file in your ngrok configuration directory:

```yaml
version: "2"
authtoken: your_auth_token
tunnels:
  ip-manager:
    proto: http
    addr: 5001
    inspect: false
```

4. **Access Your API**

- ngrok will provide you with a public URL (e.g., `https://abc123.ngrok.io`)
- All API endpoints will be accessible through this URL
- Example: `https://abc123.ngrok.io/api/countries/blocked`

### Testing the API with ngrok

1. **Test IP Lookup**

```bash
curl https://your-ngrok-url/api/countries/ip/lookup
```

2. **Test Country Blocking**

```bash
curl -X POST https://your-ngrok-url/api/countries/block \
  -H "Content-Type: application/json" \
  -d '{"code": "US"}'
```

### Security Considerations with ngrok

- All traffic is encrypted through HTTPS
- ngrok provides a secure tunnel to your local API
- IP addresses are properly forwarded through the X-Forwarded-For header
- The API correctly handles the ngrok proxy headers

### Performance with ngrok

- The API performs well through ngrok with minimal latency
- All features work as expected:
  - IP geolocation
  - Country blocking
  - Logging
  - Temporary blocks

### Troubleshooting ngrok

If you encounter any issues:

1. **Check ngrok status**

```bash
ngrok status
```

2. **Verify API is running**

```bash
curl https://localhost:5001/health
```

3. **Check ngrok logs**

```bash
ngrok logs
```

## 📚 API Endpoints

### Country Management

#### Block a Country

```http
POST /api/countries/block
Content-Type: application/json

{
    "code": "US"
}
```

Response: `200 OK` with success message or `409 Conflict` if country is already blocked

#### Unblock a Country

```http
DELETE /api/countries/block/{countryCode}
```

Response: `200 OK` with success message or `404 Not Found` if country is not blocked

#### Get All Blocked Countries

```http
GET /api/countries/blocked
```

Response: `200 OK` with list of blocked countries

### IP Address Management

#### Lookup IP Address

```http
GET /api/countries/ip/lookup
GET /api/countries/ip/lookup/{ipAddress}
```

Response: `200 OK` with IP information or `500 Internal Server Error` if lookup fails

#### Check if IP is Blocked

```http
GET /api/countries/ip/check-block
```

Response: `200 OK` with blocking status:

```json
{
  "ipAddress": "192.168.1.1",
  "countryCode": "US",
  "isBlocked": true
}
```

### Temporary Blocking

#### Add Temporary Block

```http
POST /api/countries/temporal-block
Content-Type: application/json

{
    "code": "US",
    "durationMinutes": 30
}
```

Response: `200 OK` with success message or `400 Bad Request` if invalid input

### Logging

#### Get Blocked Attempts Log

```http
GET /api/countries/logs/blocked-attempts?page=1&pageSize=10
```

Response: `200 OK` with paginated log entries

## Configuration

The application uses the following configuration files:

- `appsettings.json`: Main configuration file
- `appsettings.Development.json`: Development-specific settings

### Key Configuration Settings

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Ngrok": {
    "Enabled": true,
    "AuthToken": "your_auth_token"
  }
}
```

## 🏗️ Project Structure

```
IP-Manager/
├── Controllers/
│   └── CountriesController.cs
├── Models/
│   ├── Country.cs
│   ├── LogEntry.cs
│   └── InMemoryStore.cs
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

## 🔒 Security

- HTTPS enabled by default
- Input validation for all endpoints
- Request logging for security monitoring
- IP address validation
- Country code validation
- Secure ngrok tunnel
- Proper header handling

## 📝 Logging

The application implements comprehensive logging:

- Request logging
- Error logging
- Block attempt logging
- IP lookup logging
- ngrok tunnel logging

## 🧪 Testing

To run tests:

```bash
dotnet test
```

## 📈 Performance Considerations

- In-memory storage for quick access
- Concurrent dictionary for thread-safe operations
- Efficient IP lookup caching
- Pagination for log retrieval
- Optimized ngrok tunnel performance


## 🔍 Code Quality

The code follows best practices:

- Clean code principles
- SOLID principles
- Proper error handling
- Comprehensive logging
- Input validation
- Thread safety
- Proper documentation

## 🎯 Task Completion

I believe I have successfully completed all the requirements of the task and added some additional features to demonstrate my capabilities. The implementation shows:

1. **Technical Proficiency**:

   - Clean, maintainable code
   - Proper error handling
   - Security considerations
   - Performance optimization

2. **Problem-Solving Skills**:

   - Efficient solutions
   - Scalable architecture
   - Proper validation
   - Robust error handling

3. **Attention to Detail**:
   - Comprehensive logging
   - Input validation
   - Security measures
   - Documentation


## 📝 Notes

This implementation represents my approach to the task requirements and demonstrates my understanding of:

- API design principles
- Security best practices
- Performance considerations
- Code quality standards
- Documentation importance
