namespace NUnitComposition.ImaginaryLibrary;

public interface IImaginaryDependency
{
    string[] StuffDone { get; }
    Task DoStuff();
}

public class ImaginaryDependency : IImaginaryDependency
{
    private readonly List<string> stuffDone = new();

    public string[] StuffDone => stuffDone.ToArray();

    public Task DoStuff()
    {
        stuffDone.Add($"{TimeOnly.FromDateTime(DateTime.Now)}: Done stuff");
        return Task.CompletedTask;
    }
}