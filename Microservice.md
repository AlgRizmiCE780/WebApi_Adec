# Microservices E-Commerce Platform - Complete Learning Project

> Build a production-ready e-commerce backend with microservices architecture, learning every .NET concept along the way.

---

## 🎯 Project Overview

You'll build a **full-featured e-commerce platform** with microservices, starting from your existing JWT auth and EF Core foundation. This single project will teach you everything you need to know.

### What You'll Build

**Core Microservices:**
1. **Identity Service** (Your existing JWT auth - we'll enhance it)
2. **Product Catalog Service**
3. **Shopping Cart Service**
4. **Order Service**
5. **Payment Service**
6. **Notification Service**
7. **API Gateway** (Single entry point)

**Shared Infrastructure:**
- Message Bus (RabbitMQ)
- Redis Cache
- API Gateway (Ocelot/YARP)
- Centralized Logging (Serilog + Seq)
- Monitoring (Application Insights)

---

## 🏗️ Architecture Overview

```
                                    ┌─────────────────┐
                                    │   API Gateway   │
                                    │  (Ocelot/YARP)  │
                                    └────────┬────────┘
                                             │
                    ┌────────────────────────┼────────────────────────┐
                    │                        │                        │
            ┌───────▼────────┐      ┌───────▼────────┐      ┌───────▼────────┐
            │Identity Service│      │Product Service │      │  Order Service │
            │  (Your Base)   │      │                │      │                │
            └───────┬────────┘      └───────┬────────┘      └───────┬────────┘
                    │                       │                        │
                    │              ┌────────▼────────┐               │
                    │              │  Cart Service   │               │
                    │              └────────┬────────┘               │
                    │                       │                        │
            ┌───────▼───────────────────────▼────────────────────────▼────────┐
            │                        Message Bus (RabbitMQ)                   │
            └───────┬───────────────────────┬────────────────────────┬────────┘
                    │                       │                        │
            ┌───────▼────────┐      ┌──────▼──────┐        ┌────────▼────────┐
            │Payment Service │      │Notification │        │  Redis Cache    │
            └────────────────┘      │  Service    │        └─────────────────┘
                                    └─────────────┘
```

---

## 📋 Phase-by-Phase Development Plan

## **PHASE 1: Enhance Your Existing Identity Service (Week 1-2)**

### Current State Assessment
You have: JWT auth + EF Core

### What We'll Add:

#### 1.1 Upgrade Authentication & Authorization
**New Topics You'll Learn:**
- Refresh tokens implementation
- Claims-based authorization
- Policy-based authorization
- Role management
- Two-factor authentication (optional)

**Implementation Steps:**

**File Structure:**
```
IdentityService/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   └── RolesController.cs
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── RefreshToken.cs
│   │   └── UserRole.cs
│   └── DTOs/
│       ├── LoginRequest.cs
│       ├── LoginResponse.cs
│       ├── RefreshTokenRequest.cs
│       └── RegisterRequest.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ITokenService.cs
│   └── TokenService.cs
├── Data/
│   ├── IdentityDbContext.cs
│   └── Migrations/
├── Policies/
│   └── PermissionRequirement.cs
└── Program.cs
```

**Code to Add - RefreshToken Entity:**
```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string RevokedByIp { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    public int UserId { get; set; }
    public User User { get; set; }
}
```

**Enhanced TokenService:**
```csharp
public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(string ipAddress);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("userId", user.Id.ToString())
        };
        
        // Add role claims
        foreach (var role in user.UserRoles.Select(ur => ur.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
    
    // Implementation for getting principal from expired token
}
```

**Resources:**
- [ASP.NET Core Identity Deep Dive](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Policy-based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [JWT Refresh Tokens](https://jasonwatmore.com/post/2022/01/24/net-6-jwt-authentication-with-refresh-tokens-tutorial-with-example-api)

---

## **PHASE 2: Build Product Catalog Service (Week 2-3)**

### What You'll Learn:
- Building a new microservice from scratch
- CQRS pattern with MediatR
- Advanced EF Core (relationships, queries)
- Repository pattern
- FluentValidation
- AutoMapper
- Caching with Redis
- API versioning

### Service Structure:

```
ProductCatalogService/
├── API/
│   ├── Controllers/
│   │   ├── ProductsController.cs
│   │   └── CategoriesController.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   └── Program.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── CreateProductCommandValidator.cs
│   │   └── UpdateProduct/
│   ├── Queries/
│   │   ├── GetProducts/
│   │   │   ├── GetProductsQuery.cs
│   │   │   └── GetProductsQueryHandler.cs
│   │   └── GetProductById/
│   ├── DTOs/
│   ├── Mappings/
│   │   └── MappingProfile.cs
│   └── Behaviors/
│       ├── ValidationBehavior.cs
│       └── LoggingBehavior.cs
├── Domain/
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── Category.cs
│   │   ├── ProductImage.cs
│   │   └── ProductReview.cs
│   ├── Interfaces/
│   │   └── IProductRepository.cs
│   └── Specifications/
├── Infrastructure/
│   ├── Data/
│   │   ├── ProductDbContext.cs
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Repositories/
│   │   └── ProductRepository.cs
│   └── Caching/
│       ├── ICacheService.cs
│       └── RedisCacheService.cs
└── Tests/
    ├── UnitTests/
    └── IntegrationTests/
```

### Key Implementation - CQRS with MediatR:

**Install Packages:**
```bash
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.AspNetCore
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package StackExchange.Redis
```

**Product Entity (Domain Layer):**
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string SKU { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Relationships
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    
    public ICollection<ProductImage> Images { get; set; }
    public ICollection<ProductReview> Reviews { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int? ParentCategoryId { get; set; }
    
    public Category ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    public ICollection<Product> Products { get; set; }
}
```

**Create Product Command (CQRS Pattern):**
```csharp
// Command
public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string SKU { get; init; }
    public int CategoryId { get; init; }
}

// Validator
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200);
            
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
            
        RuleFor(x => x.SKU)
            .NotEmpty()
            .Matches("^[A-Z0-9-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");
    }
}

// Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    
    public CreateProductCommandHandler(
        IProductRepository repository,
        IMapper mapper,
        ICacheService cacheService)
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
    }
    
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        
        // Invalidate cache
        await _cacheService.RemoveAsync("products:all");
        
        return _mapper.Map<ProductDto>(product);
    }
}
```

**Query Example:**
```csharp
public record GetProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string SearchTerm { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Check cache first
        var cacheKey = $"products:{request.PageNumber}:{request.PageSize}:{request.SearchTerm}:{request.CategoryId}";
        var cachedResult = await _cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey);
        
        if (cachedResult != null)
            return cachedResult;
        
        // Build query with specifications
        var query = _repository.GetQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(p => p.Name.Contains(request.SearchTerm) || 
                                     p.Description.Contains(request.SearchTerm));
        
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            
        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice.Value);
            
        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        
        var totalCount = await query.CountAsync();
        
        var products = await query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        
        var result = new PagedResult<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        
        // Cache the result for 5 minutes
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }
}
```

**Redis Cache Service:**
```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    
    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;
            
        return JsonSerializer.Deserialize<T>(value);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, serialized, expiration);
    }
    
    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }
}
```

**Controller Implementation:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery { Id = id });
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest();
            
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        await _mediator.Send(new DeleteProductCommand { Id = id });
        return NoContent();
    }
}
```

**Program.cs Configuration:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Repository
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Validation Behavior (Pipeline):**
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        return await next();
    }
}
```

**Resources:**
- [CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MediatR GitHub](https://github.com/jbogard/MediatR/wiki)
- [FluentValidation Docs](https://docs.fluentvalidation.net/)
- [AutoMapper Configuration](https://docs.automapper.org/en/stable/Configuration.html)
- [Redis with .NET](https://stackexchange.github.io/StackExchange.Redis/)

---

## **PHASE 3: Shopping Cart Service (Week 4)**

### What You'll Learn:
- Working with Redis for session data
- Complex business logic
- Service-to-service communication
- Distributed caching patterns
- Background cleanup jobs

### Architecture:
```
CartService/
├── API/
│   └── Controllers/
│       └── CartController.cs
├── Application/
│   ├── Commands/
│   │   ├── AddToCart/
│   │   ├── UpdateCartItem/
│   │   └── ClearCart/
│   ├── Queries/
│   │   └── GetCart/
│   └── Services/
│       ├── ICartService.cs
│       └── CartService.cs
├── Domain/
│   ├── Entities/
│   │   ├── Cart.cs
│   │   ├── CartItem.cs
│   │   └── CartSummary.cs
│   └── Interfaces/
│       └── ICartRepository.cs
├── Infrastructure/
│   ├── Repositories/
│   │   └── RedisCartRepository.cs
│   ├── ExternalServices/
│   │   ├── IProductService.cs
│   │   └── ProductServiceClient.cs
│   └── BackgroundJobs/
│       └── CartCleanupJob.cs
└── Tests/
```

**Cart Entity:**
```csharp
public class Cart
{
    public string UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    public decimal TotalAmount => Items.Sum(item => item.Price * item.Quantity);
    public int TotalItems => Items.Sum(item => item.Quantity);
}

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; }
}
```

**Redis Cart Repository:**
```csharp
public interface ICartRepository
{
    Task<Cart> GetCartAsync(string userId);
    Task SaveCartAsync(Cart cart);
    Task DeleteCartAsync(string userId);
}

public class RedisCartRepository : ICartRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private const string CartKeyPrefix = "cart:";
    
    public RedisCartRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }
    
    public async Task<Cart> GetCartAsync(string userId)
    {
        var key = $"{CartKeyPrefix}{userId}";
        var value = await _database.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
            return new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
        
        return JsonSerializer.Deserialize<Cart>(value);
    }
    
    public async Task SaveCartAsync(Cart cart)
    {
        var key = $"{CartKeyPrefix}{cart.UserId}";
        var serialized = JsonSerializer.Serialize(cart);
        
        // Expire cart after 7 days of inactivity
        await _database.StringSetAsync(key, serialized, TimeSpan.FromDays(7));
    }
    
    public async Task DeleteCartAsync(string userId)
    {
        var key = $"{CartKeyPrefix}{userId}";
        await _database.KeyDeleteAsync(key);
    }
}
```

**Service-to-Service Communication (Product Service Client):**
```csharp
public interface IProductService
{
    Task<ProductDto> GetProductAsync(int productId);
    Task<List<ProductDto>> GetProductsAsync(List<int> productIds);
}

public class ProductServiceClient : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductServiceClient> _logger;
    
    public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<ProductDto> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/products/{productId}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", productId);
            throw;
        }
    }
    
    public async Task<List<ProductDto>> GetProductsAsync(List<int> productIds)
    {
        var queryString = string.Join("&", productIds.Select(id => $"ids={id}"));
        var response = await _httpClient.GetAsync($"/api/v1/products?{queryString}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>();
    }
}
```

**Add to Cart Command:**
```csharp
public record AddToCartCommand : IRequest<CartDto>
{
    public string UserId { get; init; }
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductService _productService;
    private readonly IMapper _mapper;
    
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        // Get product details from Product Service
        var product = await _productService.GetProductAsync(request.ProductId);
        
        if (product == null)
            throw new NotFoundException($"Product {request.ProductId} not found");
        
        if (product.StockQuantity < request.Quantity)
            throw new InvalidOperationException("Insufficient stock");
        
        // Get current cart
        var cart = await _cartRepository.GetCartAsync(request.UserId);
        
        // Check if item already exists
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = request.Quantity,
                ImageUrl = product.Images?.FirstOrDefault()?.Url
            });
        }
        
        cart.LastModifiedAt = DateTime.UtcNow;
        await _cartRepository.SaveCartAsync(cart);
        
        return _mapper.Map<CartDto>(cart);
    }
}
```

**Background Cart Cleanup Job (Hangfire):**
```csharp
public class CartCleanupJob
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CartCleanupJob> _logger;
    
    public async Task CleanupAbandonedCarts()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "cart:*");
        
        foreach (var key in keys)
        {
            // Logic to clean up old carts
        }
    }
}
```

**Resources:**
- [Redis Patterns](https://redis.io/docs/manual/patterns/)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [HTTP Client Best Practices](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)

---

## **PHASE 4: Order Service with Message Queue (Week 5-6)**

### What You'll Learn:
- RabbitMQ message broker
- Event-driven architecture
- Saga pattern for distributed transactions
- Complex state management
- Domain events

### Service Structure:
```
OrderService/
├── API/
│   └── Controllers/
│       └── OrdersController.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateOrder/
│   │   ├── CancelOrder/
│   │   └── UpdateOrderStatus/
│   ├── Queries/
│   │   ├── GetOrders/
│   │   └── GetOrderDetails/
│   ├── Events/
│   │   ├── OrderCreatedEvent.cs
│   │   ├── OrderConfirmedEvent.cs
│   │   └── OrderCancelledEvent.cs
│   └── EventHandlers/
│       ├── OrderCreatedEventHandler.cs
│       └── PaymentProcessedEventHandler.cs
├── Domain/
│   ├── Entities/
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   └── OrderStatus.cs (enum)
│   └── Events/
│       └── IDomainEvent.cs
├── Infrastructure/
│   ├── Data/
│   ├── Messaging/
│   │   ├── IRabbitMQPublisher.cs
│   │   ├── RabbitMQPublisher.cs
│   │   ├── IRabbitMQConsumer.cs
│   │   └── RabbitMQConsumer.cs
│   └── BackgroundServices/
│       └── OrderEventConsumer.cs
└── Tests/
```

**Order Entity with Status:**
```csharp
public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    
    // Shipping Address
    public string ShippingAddress { get; set; }
    public string ShippingCity { get; set; }
    public string ShippingState { get; set; }
    public string ShippingZipCode { get; set; }
    public string ShippingCountry { get; set; }
    
    public ICollection<OrderItem> OrderItems { get; set; }
    
    // Domain Events
    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    
    public Order Order { get; set; }
}
```

**RabbitMQ Configuration:**
```bash
# Install RabbitMQ NuGet package
dotnet add package RabbitMQ.Client
```

**RabbitMQ Publisher:**
```csharp
public interface IRabbitMQPublisher
{
    Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class;
}

public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQPublisher> _logger;
    
    public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            Port = int.Parse(configuration["RabbitMQ:Port"]),
            UserName = configuration["RabbitMQ:Username"],
            Password = configuration["RabbitMQ:Password"]
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare exchanges
        _channel.ExchangeDeclare(exchange: "orders", type: ExchangeType.Topic, durable: true);
    }
    
    public Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        
        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);
        
        _logger.LogInformation("Published message to {Exchange} with routing key {RoutingKey}", 
            exchange, routingKey);
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
```

**RabbitMQ Consumer (Background Service):**
```csharp
public class OrderEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventConsumer> _logger;
    
    public OrderEventConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<OrderEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            Port = int.Parse(configuration["RabbitMQ:Port"]),
            UserName = configuration["RabbitMQ:Username"],
            Password = configuration["RabbitMQ:Password"],
            DispatchConsumersAsync = true
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare queue
        _channel.QueueDeclare(
            queue: "order.payment.processed",
            durable: true,
            exclusive: false,
            autoDelete: false);
        
        // Bind to exchange
        _channel.QueueBind(
            queue: "order.payment.processed",
            exchange: "payments",
            routingKey: "payment.processed");
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
                
                // Process the event
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                await mediator.Publish(paymentEvent, stoppingToken);
                
                // Acknowledge message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                
                _logger.LogInformation("Processed payment event for order {OrderId}", paymentEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment event");
                
                // Reject and requeue
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };
        
        _channel.BasicConsume(
            queue: "order.payment.processed",
            autoAck: false,
            consumer: consumer);
        
        return Task.CompletedTask;
    }
    
    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
```

**Create Order Command with Events:**
```csharp
public record CreateOrderCommand : IRequest<OrderDto>
{
    public string UserId { get; init; }
    public string ShippingAddress { get; init; }
    public string ShippingCity { get; init; }
    public string ShippingState { get; init; }
    public string ShippingZipCode { get; init; }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IRabbitMQPublisher _messagePublisher;
    private readonly IMapper _mapper;
    
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Get cart
        var cart = await _cartRepository.GetCartAsync(request.UserId);
        
        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");
        
        // Create order
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = request.ShippingAddress,
            ShippingCity = request.ShippingCity,
            ShippingState = request.ShippingState,
            ShippingZipCode = request.ShippingZipCode,
            ShippingCost = CalculateShippingCost(cart),
            TaxAmount = CalculateTax(cart),
            OrderItems = cart.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.Price,
                Quantity = item.Quantity
            }).ToList()
        };
        
        order.TotalAmount = order.OrderItems.Sum(i => i.TotalPrice) + 
                           order.ShippingCost + 
                           order.TaxAmount;
        
        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();
        
        // Publish event
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.UnitPrice
            }).ToList()
        };
        
        await _messagePublisher.PublishAsync(
            orderCreatedEvent, 
            "orders", 
            "order.created");
        
        // Clear cart
        await _cartRepository.DeleteCartAsync(request.UserId);
        
        return _mapper.Map<OrderDto>(order);
    }
    
    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    }
}
```

**Event Handler for Payment Processed:**
```csharp
public class PaymentProcessedEvent : INotification
{
    public int OrderId { get; set; }
    public string PaymentId { get; set; }
    public bool IsSuccessful { get; set; }
    public string TransactionId { get; set; }
}

public class PaymentProcessedEventHandler : INotificationHandler<PaymentProcessedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRabbitMQPublisher _messagePublisher;
    private readonly ILogger<PaymentProcessedEventHandler> _logger;
    
    public async Task Handle(PaymentProcessedEvent notification, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(notification.OrderId);
        
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", notification.OrderId);
            return;
        }
        
        if (notification.IsSuccessful)
        {
            order.Status = OrderStatus.Confirmed;
            _logger.LogInformation("Order {OrderId} confirmed with transaction {TransactionId}", 
                notification.OrderId, notification.TransactionId);
            
            // Publish order confirmed event
            await _messagePublisher.PublishAsync(
                new OrderConfirmedEvent { OrderId = order.Id },
                "orders",
                "order.confirmed");
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
            _logger.LogWarning("Order {OrderId} cancelled due to payment failure", notification.OrderId);
            
            // Publish order cancelled event
            await _messagePublisher.PublishAsync(
                new OrderCancelledEvent { OrderId = order.Id },
                "orders",
                "order.cancelled");
        }
        
        await _orderRepository.SaveChangesAsync();
    }
}
```

**Resources:**
- [RabbitMQ .NET Tutorial](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html)
- [Event-Driven Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
- [Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [MassTransit (Alternative)](https://masstransit.io/)

---

## **PHASE 5: Payment Service (Week 7)**

### What You'll Learn:
- External API integration (Stripe)
- Idempotency
- Webhook handling
- Transaction safety
- PCI compliance basics

**Quick Implementation Guide:**

```csharp
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task<PaymentResult> GetPaymentStatusAsync(string paymentId);
    Task HandleWebhookAsync(string payload, string signature);
}

public class StripePaymentService : IPaymentService
{
    private readonly StripeClient _stripeClient;
    private readonly IRabbitMQPublisher _messagePublisher;
    
    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        // Create payment intent with Stripe
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100), // Convert to cents
            Currency = "usd",
            Metadata = new Dictionary<string, string>
            {
                { "order_id", request.OrderId.ToString() },
                { "user_id", request.UserId }
            }
        };
        
        var service = new PaymentIntentService(_stripeClient);
        var paymentIntent = await service.CreateAsync(options);
        
        // Save payment record
        var payment = new Payment
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            Status = PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            ExternalPaymentId = paymentIntent.Id,
            CreatedAt = DateTime.UtcNow
        };
        
        await _paymentRepository.AddAsync(payment);
        await _paymentRepository.SaveChangesAsync();
        
        return new PaymentResult
        {
            PaymentId = payment.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = payment.Status
        };
    }
    
    public async Task HandleWebhookAsync(string payload, string signature)
    {
        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            signature,
            _webhookSecret);
        
        if (stripeEvent.Type == Events.PaymentIntentSucceeded)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            var orderId = int.Parse(paymentIntent.Metadata["order_id"]);
            
            // Update payment status
            var payment = await _paymentRepository.GetByExternalIdAsync(paymentIntent.Id);
            payment.Status = PaymentStatus.Completed;
            payment.CompletedAt = DateTime.UtcNow;
            await _paymentRepository.SaveChangesAsync();
            
            // Publish event to Order Service
            await _messagePublisher.PublishAsync(
                new PaymentProcessedEvent
                {
                    OrderId = orderId,
                    PaymentId = payment.Id.ToString(),
                    IsSuccessful = true,
                    TransactionId = paymentIntent.Id
                },
                "payments",
                "payment.processed");
        }
    }
}
```

**Resources:**
- [Stripe .NET SDK](https://stripe.com/docs/api?lang=dotnet)
- [Webhook Security](https://stripe.com/docs/webhooks/signatures)
- [Idempotency](https://stripe.com/docs/api/idempotent_requests)

---

## **PHASE 6: Notification Service (Week 8)**

### What You'll Learn:
- Background job processing
- Email sending (SendGrid/SMTP)
- Template engines (Razor)
- SMS integration (Twilio)
- Push notifications

**Implementation:**

```csharp
public class EmailNotificationHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserAsync(notification.UserId);
        var order = await _orderService.GetOrderAsync(notification.OrderId);
        
        var emailContent = await _templateEngine.RenderAsync("OrderConfirmation", new
        {
            UserName = user.Name,
            OrderNumber = order.OrderNumber,
            Items = order.Items,
            TotalAmount = order.TotalAmount
        });
        
        await _emailService.SendEmailAsync(new EmailMessage
        {
            To = user.Email,
            Subject = $"Order Confirmation - {order.OrderNumber}",
            Body = emailContent,
            IsHtml = true
        });
    }
}
```

---

## **PHASE 7: API Gateway with Ocelot (Week 9)**

### What You'll Learn:
- API Gateway pattern
- Request routing
- Load balancing
- Rate limiting at gateway
- Authentication aggregation

**Ocelot Configuration:**

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/v1/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "identity-service",
          "Port": 5001
        }
      ],
      "UpstreamPathTemplate": "/auth/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    },
    {
      "DownstreamPathTemplate": "/api/v1/products/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-service",
          "Port": 5002
        }
      ],
      "UpstreamPathTemplate": "/products/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      },
      "RateLimitOptions": {
        "ClientWhitelist": [],
        "EnableRateLimiting": true,
        "Period": "1m",
        "PeriodTimespan": 60,
        "Limit": 100
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddOcelot(builder.Configuration);

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options => { /* config */ });

var app = builder.Build();
await app.UseOcelot();
app.Run();
```

**Resources:**
- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [YARP (Alternative)](https://microsoft.github.io/reverse-proxy/)

---

## **PHASE 8: Centralized Logging & Monitoring (Week 10)**

### What You'll Learn:
- Structured logging with Serilog
- Log aggregation with Seq/ELK
- Distributed tracing
- Application Insights
- Health checks

**Serilog Configuration:**

```csharp
// Install packages
// Serilog.AspNetCore
// Serilog.Sinks.Seq
// Serilog.Enrichers.Environment

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "ProductService")
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();
```

**Correlation ID Middleware:**
```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();
        
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

**Health Checks:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"))
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration["RabbitMQ:ConnectionString"]);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## **PHASE 9: Docker & Docker Compose (Week 11)**

### Each Service Dockerfile:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductService/ProductService.csproj", "ProductService/"]
RUN dotnet restore "ProductService/ProductService.csproj"
COPY . .
WORKDIR "/src/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
```

### Docker Compose (Full Stack):

```yaml
version: '3.8'

services:
  # Databases
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      - RABBITMQ_DEFAULT_USER=admin
      - RABBITMQ_DEFAULT_PASS=admin
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      - ACCEPT_EULA=Y
    volumes:
      - seq-data:/data

  # Microservices
  identity-service:
    build:
      context: .
      dockerfile: IdentityService/Dockerfile
    ports:
      - "5001:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=IdentityDb;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - Jwt__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLong
      - Jwt__Issuer=ECommerceAPI
      - Jwt__Audience=ECommerceClient
    depends_on:
      - sqlserver

  product-service:
    build:
      context: .
      dockerfile: ProductService/Dockerfile
    ports:
      - "5002:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ProductDb;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - ConnectionStrings__Redis=redis:6379
      - Jwt__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLong
      - Jwt__Issuer=ECommerceAPI
      - Jwt__Audience=ECommerceClient
      - Serilog__SeqServerUrl=http://seq:80
    depends_on:
      - sqlserver
      - redis
      - seq

  cart-service:
    build:
      context: .
      dockerfile: CartService/Dockerfile
    ports:
      - "5003:80"
    environment:
      - ConnectionStrings__Redis=redis:6379
      - Services__ProductService=http://product-service:80
      - Jwt__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLong
    depends_on:
      - redis
      - product-service

  order-service:
    build:
      context: .
      dockerfile: OrderService/Dockerfile
    ports:
      - "5004:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=OrderDb;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Port=5672
      - RabbitMQ__Username=admin
      - RabbitMQ__Password=admin
    depends_on:
      - sqlserver
      - rabbitmq

  payment-service:
    build:
      context: .
      dockerfile: PaymentService/Dockerfile
    ports:
      - "5005:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=PaymentDb;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
      - Stripe__SecretKey=your_stripe_test_key
      - Stripe__WebhookSecret=your_webhook_secret
    depends_on:
      - sqlserver
      - rabbitmq

  notification-service:
    build:
      context: .
      dockerfile: NotificationService/Dockerfile
    ports:
      - "5006:80"
    environment:
      - RabbitMQ__Host=rabbitmq
      - SendGrid__ApiKey=your_sendgrid_key
    depends_on:
      - rabbitmq

  api-gateway:
    build:
      context: .
      dockerfile: ApiGateway/Dockerfile
    ports:
      - "5000:80"
    environment:
      - Jwt__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLong
    depends_on:
      - identity-service
      - product-service
      - cart-service
      - order-service
      - payment-service

volumes:
  sqlserver-data:
  redis-data:
  rabbitmq-data:
  seq-data:
```

**Run Everything:**
```bash
docker-compose up -d
```

---

## **PHASE 10: Testing Strategy (Week 12)**

### Unit Tests Example:

```csharp
public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheMock;
    
    [Fact]
    public async Task Handle_ValidProduct_ShouldCreateProduct()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Price = 99.99m,
            SKU = "TEST-001",
            CategoryId = 1
        };
        
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        
        var handler = new CreateProductCommandHandler(
            _repositoryMock.Object,
            _mapperMock.Object,
            _cacheMock.Object);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("products:all"), Times.Once);
    }
}
```

### Integration Tests:

```csharp
public class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetProducts_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<PagedResult<ProductDto>>(content);
        
        Assert.NotNull(products);
        Assert.True(products.Items.Count > 0);
    }
}
```

---

## **PHASE 11: CI/CD with GitHub Actions (Week 13)**

### .github/workflows/product-service-ci.yml:

```yaml
name: Product Service CI/CD

on:
  push:
    branches: [ main, develop ]
    paths:
      - 'ProductService/**'
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore ProductService/ProductService.csproj
    
    - name: Build
      run: dotnet build ProductService/ProductService.csproj --no-restore
    
    - name: Run Unit Tests
      run: dotnet test ProductService.Tests/ProductService.Tests.csproj --no-build --verbosity normal
    
    - name: Run Integration Tests
      run: dotnet test ProductService.IntegrationTests/ProductService.IntegrationTests.csproj --no-build --verbosity normal
  
  docker-build-push:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Login to Docker Hub
      uses: docker/login-action@v2
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
    
    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: ProductService/Dockerfile
        push: true
        tags: yourusername/product-service:latest
  
  deploy-to-azure:
    needs: docker-build-push
    runs-on: ubuntu-latest
    
    steps:
    - name: Deploy to Azure App Service
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'your-product-service'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        images: 'yourusername/product-service:latest'
```

---

## **PHASE 12: Deployment to Azure (Week 14)**

### Azure Resources Needed:
- Azure App Service (for each microservice) or AKS (Kubernetes)
- Azure SQL Database
- Azure Cache for Redis
- Azure Service Bus (alternative to RabbitMQ)
- Application Insights
- Azure Key Vault (for secrets)

### Deploy with Azure CLI:

```bash
# Create resource group
az group create --name ecommerce-rg --location eastus

# Create Azure SQL Server
az sql server create \
  --name ecommerce-sql-server \
  --resource-group ecommerce-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password YourPassword123!

# Create databases
az sql db create --resource-group ecommerce-rg \
  --server ecommerce-sql-server \
  --name IdentityDb \
  --service-objective S0

# Create Redis Cache
az redis create \
  --name ecommerce-redis \
  --resource-group ecommerce-rg \
  --location eastus \
  --sku Basic \
  --vm-size c0

# Create App Service Plan
az appservice plan create \
  --name ecommerce-plan \
  --resource-group ecommerce-rg \
  --sku B1 \
  --is-linux

# Create App Service for Product Service
az webapp create \
  --name product-service-app \
  --resource-group ecommerce-rg \
  --plan ecommerce-plan \
  --deployment-container-image-name yourusername/product-service:latest
```

---

## 📊 Progress Tracking

### Checklist:

**Phase 1: Identity Service Enhancement**
- [ ] Implement refresh tokens
- [ ] Add role-based authorization
- [ ] Add policy-based authorization
- [ ] Write unit tests
- [ ] Document API endpoints

**Phase 2: Product Catalog Service**
- [ ] Set up project structure (Clean Architecture)
- [ ] Implement CQRS with MediatR
- [ ] Add FluentValidation
- [ ] Implement Redis caching
- [ ] Create product CRUD operations
- [ ] Add search and filtering
- [ ] Write unit tests
- [ ] Write integration tests

**Phase 3: Shopping Cart Service**
- [ ] Implement cart in Redis
- [ ] Add service-to-service communication
- [ ] Implement background cleanup job
- [ ] Write tests

**Phase 4: Order Service**
- [ ] Set up RabbitMQ integration
- [ ] Implement order creation with events
- [ ] Create event handlers
- [ ] Implement order status workflow
- [ ] Write tests

**Phase 5: Payment Service**
- [ ] Integrate Stripe
- [ ] Implement webhook handling
- [ ] Add idempotency
- [ ] Write tests

**Phase 6: Notification Service**
- [ ] Set up email sending
- [ ] Create email templates
- [ ] Implement event consumers
- [ ] Write tests

**Phase 7: API Gateway**
- [ ] Configure Ocelot
- [ ] Set up routing
- [ ] Add rate limiting
- [ ] Configure authentication

**Phase 8: Logging & Monitoring**
- [ ] Configure Serilog
- [ ] Set up Seq
- [ ] Add correlation IDs
- [ ] Implement health checks
- [ ] Set up Application Insights

**Phase 9: Containerization**
- [ ] Create Dockerfiles for all services
- [ ] Create docker-compose.yml
- [ ] Test local deployment
- [ ] Optimize Docker images

**Phase 10: Testing**
- [ ] Write unit tests (80%+ coverage)
- [ ] Write integration tests
- [ ] Add API tests with Postman
- [ ] Performance testing

**Phase 11: CI/CD**
- [ ] Set up GitHub Actions workflows
- [ ] Configure automated testing
- [ ] Set up Docker image building
- [ ] Configure deployment pipeline

**Phase 12: Azure Deployment**
- [ ] Set up Azure resources
- [ ] Deploy all services
- [ ] Configure monitoring
- [ ] Set up alerts

---

## 📚 Learning Resources by Phase

### Must-Read Books:
- "Building Microservices" by Sam Newman
- "Domain-Driven Design" by Eric Evans
- "Microservices Patterns" by Chris Richardson

### YouTube Channels to Follow:
- Nick Chapsas - .NET best practices
- Milan Jovanović - Software architecture
- Les Jackson - .NET microservices

### Practice Platforms:
- Your actual project (this one!)
- Contribute to open-source .NET projects
- Code reviews on GitHub

---

## 🎯 Final Project Features Checklist

By the end, your e-commerce platform will have:

**User Features:**
- [ ] User registration and login
- [ ] JWT authentication with refresh tokens
- [ ] Browse products with search and filters
- [ ] Add/remove items from cart
- [ ] Place orders
- [ ] Order tracking
- [ ] Email notifications

**Admin Features:**
- [ ] Product management (CRUD)
- [ ] Category management
- [ ] Order management
- [ ] View analytics (optional)

**Technical Features:**
- [ ] Microservices architecture
- [ ] Event-driven communication
- [ ] Distributed caching
- [ ] API Gateway
- [ ] Centralized logging
- [ ] Health monitoring
- [ ] Containerization
- [ ] CI/CD pipeline
- [ ] Cloud deployment

---

## 💡 Tips for Success

1. **Start Small, Iterate Often**: Don't try to build everything at once
2. **Test as You Go**: Write tests immediately after implementing features
3. **Document Your Code**: Future you will thank you
4. **Use Git Properly**: Commit often with meaningful messages
5. **Learn from Errors**: Every bug is a learning opportunity
6. **Stay Consistent**: Code a little every day
7. **Ask for Help**: Use Stack Overflow, Discord, Reddit
8. **Review Your Code**: Refactor regularly
9. **Monitor Your App**: Use logs and metrics
10. **Keep Learning**: Technology evolves, stay updated

---

## 🚀 Next Steps After Completion

1. **Add More Features**:
   - Product reviews and ratings
   - Wishlist functionality
   - Recommendation engine
   - Admin dashboard
   - Real-time inventory updates
   - Multi-vendor support

2. **Advanced Topics to Explore**:
   - Kubernetes orchestration
   - Service mesh (Istio, Linkerd)
   - Event sourcing
   - GraphQL API
   - gRPC for service communication
   - Azure Functions for serverless
   - Machine learning integration

3. **Performance Optimization**:
   - Database query optimization
   - Implement CDN for images
   - Advanced caching strategies
   - Load testing and optimization

4. **Security Hardening**:
   - Penetration testing
   - OWASP compliance
   - Security headers
   - DDoS protection

---

**You've got this! This project will make you interview-ready and give you real-world experience. Build it step by step, and you'll learn everything you need to know about .NET backend development.**

Good luck! 🎯
