var builder = DistributedApplication.CreateBuilder(args);

var identity = builder.AddProject<Projects.CampusHub_Identity_Api>("identity-api")
    .WithHttpEndpoint(port: 5101, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Issuer", "http://localhost:5101/")
    .WithEnvironment("Gateway__PublicOrigin", "http://localhost:5000");

var catalog = builder.AddProject<Projects.CampusHub_Catalog_Api>("catalog-api")
    .WithHttpEndpoint(port: 5102, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Authority", "http://localhost:5101")
    .WithEnvironment("Enrollment__BaseUrl", "http://localhost:5103")
    .WithReference(identity)
    .WaitFor(identity);

var enrollment = builder.AddProject<Projects.CampusHub_Enrollment_Api>("enrollment-api")
    .WithHttpEndpoint(port: 5103, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Authority", "http://localhost:5101")
    .WithReference(identity)
    .WithReference(catalog)
    .WaitFor(catalog);

var payment = builder.AddProject<Projects.CampusHub_Payment_Api>("payment-api")
    .WithHttpEndpoint(port: 5104, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(enrollment)
    .WaitFor(enrollment);

var notification = builder.AddProject<Projects.CampusHub_Notification_Api>("notification-api")
    .WithHttpEndpoint(port: 5105, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Authority", "http://localhost:5101")
    .WithReference(identity)
    .WaitFor(identity);

var access = builder.AddProject<Projects.CampusHub_Access_Api>("access-api")
    .WithHttpEndpoint(port: 5106, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Authority", "http://localhost:5101")
    .WithReference(identity)
    .WaitFor(identity);

var chat = builder.AddNpmApp("chat-realtime", "../../services/chat")
    .WithHttpEndpoint(port: 5107, name: "http", env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("IDENTITY_AUTHORITY", "http://localhost:5101")
    .WithEnvironment("CATALOG_BASE_URL", "http://localhost:5102")
    .WithEnvironment("ENROLLMENT_BASE_URL", "http://localhost:5103")
    .WaitFor(identity)
    .WaitFor(catalog)
    .WaitFor(enrollment);

builder.AddProject<Projects.CampusHub_Gateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Authority", "http://localhost:5101")
    .WithReference(identity)
    .WithReference(catalog)
    .WithReference(enrollment)
    .WithReference(notification)
    .WithReference(access)
    .WithReference(chat)
    .WaitFor(identity)
    .WaitFor(catalog)
    .WaitFor(enrollment)
    .WaitFor(notification)
    .WaitFor(access)
    .WaitFor(chat);

builder.Build().Run();
