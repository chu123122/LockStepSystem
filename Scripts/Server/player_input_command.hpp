#pragma once
#define MAX_COMMANDS_PER_PACKET 10
#include <cstring>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>

#pragma pack(push, 1)

enum class packet_type
{
    Join = 1,
    Response = 2,
    Command = 3,
    CommandSet = 4
};

struct packet_header
{
    int packet_type;
};

struct player_input_command
{
    /* data */
    int packet_type;

    int id;        // 客户端id
    float x, y, z; // 移动位置
    player_input_command() : id(-1), x(0.0f), y(0.0f), z(0.0f)
    {
    }

    player_input_command(int id, float x, float y, float z) : id(id), x(x), y(y), z(z)
    {
    }
};

struct frame_packet
{
    int packet_type;

    int frame_number;
    int command_count;
    player_input_command commands[MAX_COMMANDS_PER_PACKET];

    frame_packet()
    {
    }
    frame_packet(int packet_type, int frame_number, int command_count, const player_input_command *commands) : packet_type(packet_type), frame_number(frame_number), command_count(command_count)
    {
        for (int i = 0; i < command_count && i < MAX_COMMANDS_PER_PACKET; ++i)
        {
            this->commands[i] = commands[i];
        }
    }
};

struct join_packet
{
    int packet_type;

    int id;
    int frame_number;
    join_packet(int packet_type, int id, int frame_number) : packet_type(packet_type), id(id), frame_number(frame_number)
    {
    }
};

#pragma pack(pop)

enum class frameStatus
{
    Collecting, // 正在收集中
    Ready,      // 人齐了，可以广播了
    Dispatched  // 已经广播过了
};

struct frameData
{
    frameStatus status;
    std::chrono::time_point<std::chrono::high_resolution_clock> creationTime;

    std::vector<player_input_command> player_input_commands;
    frameData()
    {
        this->status = frameStatus::Collecting;
        creationTime = std::chrono::high_resolution_clock::now();
    }
};

struct client
{
    int id;
    sockaddr_in addr;
    // time 时间戳
    client(int client_id, sockaddr_in &client_addr) : id(client_id)
    {
        memcpy(&addr, &client_addr, sizeof(sockaddr_in));
    }
    client(sockaddr_in &client_addr)
    {
        id = -1;
        memcpy(&addr, &client_addr, sizeof(sockaddr_in));
    }
    client() : id(-1)
    {
        memset(&addr, 0, sizeof(sockaddr_in));
    }
    client(const client &other) : id(other.id)
    {
        memcpy(&addr, &other.addr, sizeof(sockaddr_in));
    }
    client &operator=(const client &other)
    {
        if (this != &other)
        { // 防止自我赋值
            id = other.id;
            memcpy(&addr, &other.addr, sizeof(sockaddr_in));
        }
        return *this;
    }
};
