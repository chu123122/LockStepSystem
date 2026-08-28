namespace Client.Unit
{
    public struct ClientUnit
    {
        public int ID;
    }

    public interface IClient
    {
        ClientUnit ClientUnit { get; set; }

        void OnConnectServer(ClientUnit client);

        void ReceiveCommand(PlayerInputCommand command, int entityId);

        void LogicUpdate();
    }
}
