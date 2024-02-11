namespace MixServer.Application.Sessions.Commands.SetNextSession;

public class SetNextSessionCommand
{
    public int Offset { get; set; }

    public bool ResetSessionState { get; set; }
}