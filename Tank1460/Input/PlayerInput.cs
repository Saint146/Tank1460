namespace Tank1460.Input;

public class PlayerInput
{
    /// <summary>
    /// Команды, активные в текущий момент.
    /// </summary>
    public PlayerInputCommands Active { get; set; }

    /// <summary>
    /// Команды, которые только что нажали.
    /// </summary>
    public PlayerInputCommands Pressed { get; set; }

    public PlayerInput()
    {
        Clear();
    }

    public void Clear()
    {
        Active = PlayerInputCommands.None;
        Pressed = PlayerInputCommands.None;
    }
}