using System.Text;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity with Guid keys. The role type must match the one used in AppDbContext.
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Optional, but standard — pairs with AccessFailedCount/LockoutEnd above
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

    options.User.RequireUniqueEmail = true;   // Identity does NOT enforce unique emails by default
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders(); // needed for password-reset / email-confirmation tokens later

// AddIdentity registers COOKIE auth as the default scheme. For a JWT API we must
// override the defaults, otherwise [Authorize] redirects to /Account/Login instead of returning 401.
// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
// })
// .AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = true,
//         ValidIssuer = builder.Configuration["JWT:Issuer"],
//         ValidateAudience = true,
//         ValidAudience = builder.Configuration["JWT:Audience"],
//         ValidateIssuerSigningKey = true,
//         IssuerSigningKey = new SymmetricSecurityKey(
//             Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]
//                 ?? throw new InvalidOperationException("JWT:SigningKey is not configured.")))
//     };
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.Run();
