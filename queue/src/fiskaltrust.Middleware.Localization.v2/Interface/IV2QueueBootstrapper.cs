namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IV2QueueBootstrapper
{
    Func<string, Task<string>> RegisterForSign();

    Func<string, Task<string>> RegisterForEcho();

    Func<string, Task<string>> RegisterForJournal();
}