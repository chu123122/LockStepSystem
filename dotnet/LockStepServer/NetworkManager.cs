using System.Net;
using System.Net.Sockets;

namespace LockStepServer;

public class NetworkManager
{
    private Socket _serverSocket = null!;

    public bool Init(out string? error, int port = 8888)
    {
        error = null;
        try
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverSocket.Blocking = false;
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch (SocketException ex)
        {
            error = $"bind failed: {ex.SocketErrorCode} ({ex.Message})";
            _serverSocket?.Close();
            return false;
        }
    }

    public void SendBufToClient( byte[] buf, int bufLen, IPEndPoint clientAddr)
    {
        _serverSocket.SendTo(buf, bufLen, SocketFlags.None, clientAddr);
    }

    public void SendBufToAllClient( byte[] buf, int bufLen, List<IPEndPoint> clientAddrs)
    {
        foreach (IPEndPoint clientAddr in clientAddrs)
        {
            _serverSocket.SendTo(buf, bufLen, SocketFlags.None, clientAddr);
        }
    }

    public int ReceiveFromClient(byte[] buf, int bufSize, out IPEndPoint? from)
    {
        from = null;
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            int received = _serverSocket.ReceiveFrom(buf, bufSize, SocketFlags.None, ref remote);
            from = (IPEndPoint)remote;
            return received;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
        {
            return -1;
        }
        
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
        {
            return -1;
        }
    }
}
