namespace Emaill.Console;

public interface IMailProgram : IDisposable
{
    Task Start();
}