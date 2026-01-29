using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using ResumeRank.Web.Data;
using ResumeRank.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJobService, JobService>();

// Determine agent mode from configuration
var agentMode = builder.Configuration["AgentMode"] ?? "Local";

if (agentMode.Equals("AWS", StringComparison.OrdinalIgnoreCase))
{
    // AWS mode: Use API Gateway and S3
    ConfigureAwsServices(builder);
}
else
{
    // Local mode: Use local FastAPI agents and file system
    ConfigureLocalServices(builder);
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

/// <summary>
/// Configure services for local development mode (FastAPI agents + local file system).
/// </summary>
static void ConfigureLocalServices(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<IResumeParserAgent, HttpResumeParserAgent>(client =>
    {
        var baseUrl = builder.Configuration["AgentServices:ResumeParserUrl"]
            ?? "http://localhost:5100";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddHttpClient<IRankingAgent, HttpRankingAgent>(client =>
    {
        var baseUrl = builder.Configuration["AgentServices:RankingAgentUrl"]
            ?? "http://localhost:5101";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(120);
    });

    builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
}

/// <summary>
/// Configure services for AWS mode (API Gateway + S3).
/// </summary>
static void ConfigureAwsServices(WebApplicationBuilder builder)
{
    var awsConfig = builder.Configuration.GetSection("AWS");
    var apiGatewayUrl = awsConfig["ApiGatewayUrl"]
        ?? throw new InvalidOperationException("AWS:ApiGatewayUrl is required in AWS mode");
    var bucketName = awsConfig["S3BucketName"]
        ?? throw new InvalidOperationException("AWS:S3BucketName is required in AWS mode");
    var region = awsConfig["Region"] ?? "us-east-1";

    // Configure AWS SDK
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

    // Register S3 client
    builder.Services.AddAWSService<IAmazonS3>();

    // Configure S3 storage options
    builder.Services.Configure<S3StorageOptions>(options =>
    {
        options.BucketName = bucketName;
        options.Region = region;
    });

    // Register S3 file storage
    builder.Services.AddScoped<IFileStorage, S3FileStorage>();

    // Ensure base URL ends with / for relative path resolution
    var baseUrl = apiGatewayUrl.TrimEnd('/') + "/";

    // Register AWS agent implementations
    builder.Services.AddHttpClient<IResumeParserAgent, AwsResumeParserAgent>(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for cold starts
    });

    builder.Services.AddHttpClient<IRankingAgent, AwsRankingAgent>(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(180); // Longer timeout for cold starts
    });
}
