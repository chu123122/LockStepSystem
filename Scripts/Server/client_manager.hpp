#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <vector>
#include "player_input_command.hpp"

class client_manager
{
private:
    /* data */
    std::vector<client> client_vector;
    int current_id = 0;

public:
    client_manager(/* args */);
    ~client_manager();

    int get_client_count()
    {
        return client_vector.size();
    }

    int get_client_id()
    {
        return current_id;
    }

    void add_client_with_check(client player)
    {
        for (auto &&i : client_vector)
        {
            if (i.id == player.id)
            {
                std::cout << "已连接上客户端，不进行添加"
                          << std::endl;
                return;
            }
        }
        player.id = current_id++;
        client_vector.push_back(player);
        std::cout << "成功添加客户端"
                  << "ID为:" << player.id
                  << std::endl;
    }

    void remove_client(int id)
    {
        for (auto it = client_vector.begin(); it != client_vector.end(); ++it)
        {
            if (it->id == id)
            {
                client_vector.erase(it);
                break;
            }
        }
    }

    client get_client(int id)
    {
        for (auto it = client_vector.begin(); it != client_vector.end(); ++it)
        {
            if (it->id == id)
                return *it;
        }
    }
    std::vector<sockaddr_in> get_all_client_addr()
    {
        std::vector<sockaddr_in> sockaddr;
        for (auto it = client_vector.begin(); it != client_vector.end(); ++it)
        {
            sockaddr.push_back(it->addr);
        }
        return sockaddr;
    }
};

client_manager::client_manager(/* args */)
{
}

client_manager::~client_manager()
{
}
