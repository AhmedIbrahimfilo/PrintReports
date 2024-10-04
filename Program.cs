
using Microsoft.EntityFrameworkCore;
using Reports.Data;

namespace Reports
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });



            var app = builder.Build();

            using var scope =  app.Services.CreateScope();

            var services = scope.ServiceProvider;

            var _dbContext = services.GetRequiredService<AppDbContext>();

            var _logger = services.GetRequiredService<ILoggerFactory>();


            try
            {
                _dbContext.Database.Migrate();
                
            }catch (Exception ex) 
            {
                var logger = _logger.CreateLogger<Program>();
                logger.LogError(ex,"an Error Has Occure During Migrations");
            }
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseCors("AllowAllOrigins");
            app.UseStaticFiles();
            app.MapControllers();

            app.Run();
        }
    }
}
