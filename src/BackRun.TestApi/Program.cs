using BackRun.Abstractions;
using BackRun.Extensions;
using BackRun.Storage.InMemory;

namespace BackRun.TestApi
{
    public interface ISendWelcomeEmailHandler : IBackRunJobHandler<SendWelcomeEmailPayload> { }

    public class SendWelcomeEmailHandler : ISendWelcomeEmailHandler
    {
        public SendWelcomeEmailHandler()
        {
        }

        public async Task ExecuteAsync(SendWelcomeEmailPayload payload, CancellationToken cancellationToken)
        {
            var rand = new Random();
            var val = rand.Next(0, 2);
            var bla = true;

            if (bla)
                throw new Exception("Random failure occurred while sending welcome email.");

            await Task.CompletedTask;
        }
    }

    public class SendWelcomeEmailPayload
    {
        public string Email { get; set; } = default!;
        public string UserName { get; set; } = default!;
    }

    public class Program
    {
        

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services
                .AddBackRun()
                .AddSingleton<ISendWelcomeEmailHandler, SendWelcomeEmailHandler>()
                .AddSingleton<IBackRunStorage, BackRunInMemoryStorage>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
