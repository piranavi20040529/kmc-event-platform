using KMC.EventPlatformAPI.Data;
using KMC.EventPlatformAPI.Models;
using KMC.EventPlatformAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = "YourSuperSecretKeyHereWithAtLeast32CharactersLong";
var jwtIssuer = "KMC.EventPlatformAPI";
var jwtAudience = "KMC.EventClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Migration error: {ex.Message}");
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    try
    {
        if (!await roleManager.RoleExistsAsync("Public"))
        {
            await roleManager.CreateAsync(new IdentityRole("Public"));
            Console.WriteLine(" Role created: Public");
        }

        if (!await roleManager.RoleExistsAsync("Organizer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Organizer"));
            Console.WriteLine(" Role created: Organizer");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Seed error: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Content(@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>KMC Event Platform API</title>
    <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600;800&display=swap"" rel=""stylesheet"">
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"">
    <style>
        :root {
            --primary: #00d4aa;
            --primary-hover: #00b894;
            --secondary: #a78bfa;
            --bg-color: #0d0a1a;
            --card-bg: rgba(20, 14, 42, 0.92);
            --card-border: rgba(139, 92, 246, 0.18);
            --text-main: #e6edf3;
            --text-muted: #8b8fa8;
            --success: #3fb950;
            --get-color: #1f6feb;
            --post-color: #3fb950;
            --put-color: #d29922;
            --del-color: #da3633;
        }

        body {
            font-family: 'Poppins', sans-serif;
            background: var(--bg-color);
            background-image:
                radial-gradient(ellipse at top left, rgba(109, 40, 217, 0.20) 0px, transparent 50%),
                radial-gradient(ellipse at bottom right, rgba(79, 70, 229, 0.18) 0px, transparent 50%),
                radial-gradient(ellipse at top right, rgba(14, 165, 233, 0.10) 0px, transparent 40%);
            color: var(--text-main);
            margin: 0;
            padding: 2rem;
            min-height: 100vh;
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
        }

        /* Glassmorphism Header */
        .hero {
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            border: 1px solid var(--card-border);
            border-radius: 20px;
            padding: 3rem 2rem;
            text-align: center;
            box-shadow: 0 1px 0 rgba(255,255,255,0.05), 0 25px 50px -12px rgba(0, 0, 0, 0.6);
            margin-bottom: 3rem;
            animation: slideDown 0.8s ease-out;
        }

        @keyframes slideDown {
            from { opacity: 0; transform: translateY(-30px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .hero h1 {
            font-size: 3rem;
            font-weight: 800;
            margin: 0 0 1rem 0;
            background: linear-gradient(90deg, #00d4aa 0%, #74b9ff 50%, #fd79a8 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .hero p {
            font-size: 1.2rem;
            color: var(--text-muted);
            margin-bottom: 2rem;
        }

        .status-badge {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(16, 185, 129, 0.1);
            color: var(--success);
            padding: 8px 16px;
            border-radius: 50px;
            font-weight: 600;
            font-size: 0.9rem;
            border: 1px solid rgba(16, 185, 129, 0.2);
            margin-bottom: 2rem;
        }

        .pulse {
            width: 10px;
            height: 10px;
            background-color: var(--success);
            border-radius: 50%;
            box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
            animation: pulsing 1.5s infinite;
        }

        @keyframes pulsing {
            0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
            70% { transform: scale(1); box-shadow: 0 0 0 10px rgba(16, 185, 129, 0); }
            100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }

        /* Action Buttons */
        .btn-launch {
            display: inline-block;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            color: white;
            text-decoration: none;
            padding: 1rem 2.5rem;
            border-radius: 12px;
            font-size: 1.1rem;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 10px 20px -5px rgba(79, 70, 229, 0.5);
            border: none;
            cursor: pointer;
        }

        .btn-launch:hover {
            transform: translateY(-3px) scale(1.02);
            box-shadow: 0 15px 25px -5px rgba(79, 70, 229, 0.6);
            color: white;
        }

        /* API Endpoints Section */
        .section-title {
            font-size: 1.8rem;
            font-weight: 600;
            margin-bottom: 1.5rem;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .endpoint-grid {
            display: grid;
            gap: 1rem;
            margin-bottom: 3rem;
            animation: fadeIn 1s ease-out;
        }

        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }

        .endpoint-card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 12px;
            padding: 1.25rem;
            display: flex;
            align-items: center;
            justify-content: space-between;
            transition: all 0.3s ease;
        }

        .endpoint-card:hover {
            transform: translateX(10px);
            border-color: var(--primary);
            background: rgba(30, 41, 59, 0.9);
        }

        .endpoint-info {
            display: flex;
            align-items: center;
            gap: 1.25rem;
        }

        .method {
            padding: 6px 12px;
            border-radius: 8px;
            font-weight: 800;
            font-size: 0.85rem;
            width: 70px;
            text-align: center;
            letter-spacing: 1px;
            color: white;
        }

        .get { background-color: var(--get-color); box-shadow: 0 0 10px rgba(14, 165, 233, 0.4); }
        .post { background-color: var(--post-color); box-shadow: 0 0 10px rgba(16, 185, 129, 0.4); }
        .put { background-color: var(--put-color); box-shadow: 0 0 10px rgba(245, 158, 11, 0.4); }
        .delete { background-color: var(--del-color); box-shadow: 0 0 10px rgba(239, 68, 68, 0.4); }

        .route {
            font-family: monospace;
            font-size: 1.1rem;
            color: #e2e8f0;
            font-weight: 600;
        }

        .description {
            color: var(--text-muted);
            font-size: 0.95rem;
        }

        .auth-tag {
            background: rgba(245, 158, 11, 0.15);
            color: #fcd34d;
            padding: 4px 10px;
            border-radius: 6px;
            font-size: 0.75rem;
            font-weight: 600;
            border: 1px solid rgba(245, 158, 11, 0.3);
        }

        footer {
            text-align: center;
            margin-top: 4rem;
            padding-top: 2rem;
            border-top: 1px solid var(--card-border);
            color: var(--text-muted);
            font-size: 0.9rem;
        }

        /* Responsive */
        @media (max-width: 768px) {
            .endpoint-card { flex-direction: column; align-items: flex-start; gap: 1rem; }
            .hero h1 { font-size: 2rem; }
        }
    </style>
</head>
<body>

    <div class=""container"">
        <!-- Hero Section -->
        <div class=""hero"">
            <div class=""status-badge"">
                <div class=""pulse""></div>
                API Services Operational
            </div>
            <h1>KMC Event Platform API</h1>
            <p>A secure, scalable RESTful backend service built on .NET 8 for the Kandy Municipal Council.</p>
            
            <a href=""http://localhost:5034"" class=""btn-launch"">
                <i class=""fas fa-rocket""></i> Launch KMC Web Client
            </a>
        </div>

        <!-- Authentication Endpoints -->
        <h2 class=""section-title""><i class=""fas fa-shield-alt"" style=""color: #818cf8;""></i> Authentication Services</h2>
        <div class=""endpoint-grid"">
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method post"">POST</span>
                    <span class=""route"">/api/auth/login</span>
                </div>
                <div class=""description"">Authenticate & retrieve secure JWT token</div>
            </div>
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method post"">POST</span>
                    <span class=""route"">/api/auth/register</span>
                </div>
                <div class=""description"">Register a new citizen or organizer</div>
            </div>
        </div>

        <!-- Event Endpoints -->
        <h2 class=""section-title""><i class=""fas fa-calendar-alt"" style=""color: #f472b6;""></i> Event Services</h2>
        <div class=""endpoint-grid"">
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method get"">GET</span>
                    <span class=""route"">/api/events/public</span>
                </div>
                <div class=""description"">Fetch all approved public events</div>
            </div>
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method get"">GET</span>
                    <span class=""route"">/api/events</span>
                </div>
                <div class=""description"">
                    <span class=""auth-tag""><i class=""fas fa-lock""></i> Secured</span>
                    Fetch events created by logged-in organizer
                </div>
            </div>
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method post"">POST</span>
                    <span class=""route"">/api/events</span>
                </div>
                <div class=""description"">
                    <span class=""auth-tag""><i class=""fas fa-lock""></i> Secured</span>
                    Create and publish a new event
                </div>
            </div>
            <div class=""endpoint-card"">
                <div class=""endpoint-info"">
                    <span class=""method put"">PUT</span>
                    <span class=""route"">/api/events/{id}</span>
                </div>
                <div class=""description"">
                    <span class=""auth-tag""><i class=""fas fa-lock""></i> Secured</span>
                    Update an existing event
                </div>
            </div>
        </div>

        <footer>
            <p>&copy; 2026 - Kandy Municipal Council Digital Platform</p>
        </footer>
    </div>

</body>
</html>", "text/html"));

Console.WriteLine(" API Started on http://localhost:5000");

app.Run();