public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        using var scope = scopeFactory.CreateScope();
        var svc = scope.ServiceProvider
            .GetRequiredService<IEnrollmentService>();
        var result = svc.EnrollAsync("WORKER-001", "CS-BATCH").Result;

        Console.WriteLine($"Worker processed enrollment: {result.Id}");

    } //scope service disposed  
}











































