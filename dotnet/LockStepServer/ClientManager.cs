using System.Net;

namespace LockStepServer;

public class ClientInfo
{
    public int Id { get; set; }
    public IPEndPoint Address { get; set; } = null!;
}

public class ClientManager
{
    private readonly List<ClientInfo> _clients = new();
    private int _currentId;

    public int GetClientCount() => _clients.Count;

    public int GetClientId() => _currentId;

    public void AddClientWithCheck(IPEndPoint from)
    {
        var upcoming = new ClientInfo { Id = GetClientId(), Address = from };
        foreach (var client in _clients)
        {
            if (client.Id == upcoming.Id)
            {
                Console.WriteLine("已连接上客户端,不进行添加");
                return;
            }
        }

        upcoming.Id = _currentId++;
        _clients.Add(upcoming);
        Console.WriteLine($"成功添加客户端 ID为:{upcoming.Id}");
    }

    public void RemoveClient(int id)
    {
        _clients.RemoveAll(c => c.Id == id);
    }

    public ClientInfo? GetClient(int id)
    {
        return _clients.FirstOrDefault(c => c.Id == id);
    }

    public List<IPEndPoint> GetAllClientAddresses()
    {
        return _clients.Select(c => c.Address).ToList();
    }
}
