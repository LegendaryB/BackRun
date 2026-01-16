<h1 align="center">BackRun</h1>
<div align="center">

[![forthebadge](https://forthebadge.com/images/badges/made-with-c-sharp.svg)](https://forthebadge.com)
[![forthebadge](https://forthebadge.com/images/badges/built-with-love.svg)](https://forthebadge.com)

![GitHub License](https://img.shields.io/github/license/LegendaryB/BackRun)

A lightweight, high-performance background job processing library for ASP.NET Core.  
<sub>Built with ❤ by LegendaryB</sub>
</div>
<hr>

BackRun is designed for developers who need a production-ready job engine without the overhead of massive frameworks. It focuses on clean architecture, minimal dependencies, and extreme extensibility via a middleware-based pipeline.

## 🚀 Features

- **High Performance**: Uses compiled Expressions to invoke handlers, bypassing reflection overhead during execution.
- **Middleware Pipeline**: Built-in support for custom middleware. Add logging, metrics, or custom logic to every job.
- **Resilience**: Official integration with `Microsoft.Extensions.Resilience` (Polly v8) for exponential backoffs and retries.
- **Multiple Storage Providers**: 
    - **In-Memory**: For testing and ephemeral tasks.
    - **JSON Flat-File**: Zero-config persistent storage.
    - **SQL/Redis**: (Extensible via `IBackRunStorage`).
- **Self-Healing**: Automatically recovers "orphaned" jobs stuck in a running state after an application crash or restart.
- **Scheduling**: Built-in support for delayed jobs and future execution.
- **Parallelism Control**: Configure exactly how many jobs run concurrently.

## 📦 Installation

Install the core library and your preferred storage provider via NuGet:

```bash
dotnet add package BackRun
dotnet add package BackRun.Storage.Json
dotnet add package BackRun.Resilience
```

## 🌟 Quick Start

### 1. Define your Payload and Handler
Implement the `IBackRunJobHandler<TPayload>` interface. Your handler is automatically resolved from the DI container with a scoped lifetime.

```csharp
public record SendMailPayload(string To, string Subject);

public class SendMailHandler(IMailService mailService) : IBackRunJobHandler<SendMailPayload>
{
    public async Task ExecuteAsync(
        SendEmailPayload payload,
        CancellationToken cancellationToken = default)
    {
        await mailService.SendWelcomeMailAsync(payload.To);
    }
}
```

### 2. Configure BackRun in Program.cs
Use the unified configuration block to setup your engine, storage, and handlers.

```csharp
builder.Services.AddBackRun(options => 
{
    options.MaxDegreeOfParallelism = 5;
}, 
backrun => 
{
    // Auto-discover handlers from assembly
    backrun.AddHandlersFromAssembly(typeof(Program).Assembly);
    
    // Configure Persistent Storage
    backrun.UseJsonFlatFileStorage(opt => opt.Path = "%AppData%/BackRunJobs");
    
    // Add Microsoft Resilience (3 Retries + 15m Timeout)
    backrun.AddResilience();
});
```

### 3. Enqueue Jobs
Inject `IBackRunJobEngine` into your controllers or services.

```csharp
public class JobsController(IBackRunJobEngine engine) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        // Enqueue for immediate processing
        await engine.EnqueueAsync<SendMailPayload, SendMailHandler>(
            new SendMailPayload("user@example.com", "Welcome!"));

        // Enqueue with a delay
        await engine.EnqueueAsync<SendMailPayload, SendMailHandler>(
            new SendMailPayload("user@example.com", "Check-in"),
            BackRunEnqueueOptions.Delay(TimeSpan.FromDays(1)));

        return Ok();
    }
}
```

### Extending with Middleware
BackRun's pipeline is modeled after the ASP.NET Core middleware pattern. You can intercept the execution of every job to add custom cross-cutting concerns.

```csharp
public class MyAuditMiddleware(ILogger<MyAuditMiddleware> logger) : IBackRunMiddleware
{
    public async Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Job {Id} is about to run",
            job.Id);
        
        await next(job);
        
        logger.LogInformation(
            "Job {Id} finished",
            job.Id);
    }
}

// Register it in the AddBackRun block
backrun.AddMiddleware<MyAuditMiddleware>();
```

## Project Structure
* `BackRun.Abstractions`: Pure contracts and models. Depends only on `Microsoft.Extensions.DependencyInjection.Abstractions`.
* `BackRun`: The core engine and high-performance processor.
* `BackRun.Resilience`: Official Microsoft Resilience integration.
* `BackRun.Storage.*`: Individual storage implementations.

## 🗺️ Roadmap

- [x] Middleware-based execution pipeline.
- [x] High-performance compiled expression invoker.
- [x] Official Microsoft Resilience (Polly v8) integration.
- [x] Basic persistent storage (JSON).
- [x] Self-healing and recovery logic.
- [ ] **NuGet packages**: Official release to NuGet.org.
- [ ] **OpenTelemetry Integration**: Built-in traces and metrics for job execution.
- [ ] **Entity Framework Core Provider**: Support for SQL-based storage.