using System;

namespace Client.Unit
{
    
    public enum UnitState
    {
        Idle = 0,
        Move = 1,
    }

    public struct ClientUnit : IEquatable<ClientUnit>
    {
        public int ID;

        public bool Equals(ClientUnit other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            return obj is ClientUnit other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
    public interface IClient
    {
        ClientUnit ClientUnit{get;set;}

        void OnConnectServer(ClientUnit client);

        void ReceiveCommand(player_input_command command);
        void LogicUpdate();

    }
}