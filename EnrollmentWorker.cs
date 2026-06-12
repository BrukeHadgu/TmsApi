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











































/*// ── The buggy version (Step A) ────────────────────────────────────────
// DO NOT use this in production — this is intentionally wrong
// We register this first to SEE the error the framework catches

// ── The correct version (after fix) ──────────────────────────────────
// IServiceScopeFactory is always Singleton-safe
// It creates a new scope on demand — does NOT hold a scoped service directly
public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // Create a short-lived scope — like a mini request
        // 'using' ensures the scope (and everything in it) is disposed
        // when this block ends — no memory leak
        using var scope = scopeFactory.CreateScope();

        // Resolve IEnrollmentService from THIS scope — not from the root
        // This gives us a fresh scoped instance for this batch only
        var svc = scope.ServiceProvider
            .GetRequiredService<IEnrollmentService>();

        // Use the service — scoped to this batch only
        // When the 'using' block ends, svc is disposed automatically
        var result = svc.EnrollAsync("WORKER-001", "CS-BATCH").Result;

        Console.WriteLine($"Worker processed enrollment: {result.Id}");

    } // ← scope disposed here — scoped services released
}*/