#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <errno.h>
#include <fcntl.h>
#include <vector>
#include <cstring>

class network_manager
{
private:
    /* data */
    sockaddr_in serverAddr;
    int serverSocket;

public:
    network_manager(/* args */);
    ~network_manager();

    void recevie_command_from_client();

    void send_command_set_to_client();

    bool init()
    {
        serverSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
        int flags = fcntl(serverSocket, F_GETFL, 0);
        fcntl(serverSocket, F_SETFL, flags | O_NONBLOCK);

        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(8888);
        serverAddr.sin_addr.s_addr = INADDR_ANY;
        if (bind(serverSocket, (struct sockaddr *)&serverAddr, sizeof(serverAddr)) < 0)
        {
            std::strerror(errno);
            perror("bind failed");
            close(serverSocket);
            exit(EXIT_FAILURE);
            return false;
        }
        else
        {
            printf("Bind successful!\n");
            return true;
        }
    }

    void send_buf_to_all_client(
        int server_frame,
        char *buf,
        int buf_len,
        const std::vector<sockaddr_in> &client_addrs)
    {
        for (auto &&client_addr : client_addrs)
        {
            sendto(serverSocket, buf, buf_len, 0, (sockaddr *)&client_addr, sizeof(sockaddr_in));
        }
    }
    void send_buf_to_client(
        int server_frame,
        char *buf,
        int buf_len,
        const sockaddr_in &client_addr)
    {
        int send_value = sendto(serverSocket, buf, buf_len, 0, (sockaddr *)&client_addr, sizeof(sockaddr_in));
        if (send_value < 0)
        {
            perror("send failed");
        }
        else
        {
            auto frameStartTime = std::chrono::high_resolution_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(frameStartTime);
            // std::cout << "发送信息成功"
            //           << "发送客户端指令集 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
            //           << std::endl;
        }
    }

    int receive_from_client(char *buf, int buf_size, sockaddr *from)
    {
        socklen_t fromLen = sizeof(sockaddr_in);
        int recvLen = recvfrom(serverSocket, buf, buf_size, 0, from, &fromLen);
        if (recvLen < 0)
        {
            if (errno == EWOULDBLOCK || errno == EAGAIN)
            {
                // 邮箱为空
            }
            else
            {
                perror("recvfrom failed");
            }
        }
        else
        {
            auto frameStartTime = std::chrono::high_resolution_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(frameStartTime);
            std::cout << "接收信息成功"
                      << "接收客户端指令 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                      << std::endl;
        }

        return recvLen;
    }
};

network_manager::network_manager(/* args */) {}

network_manager::~network_manager() {}
