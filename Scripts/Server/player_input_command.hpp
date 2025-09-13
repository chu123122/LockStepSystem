#pragma once
#define MAX_COMMANDS_PER_PACKET 10
#include <cstring>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>

#pragma pack(push, 1)
struct player_input_command
{
    /* data */
    int id;        // 客户端id
                   // int input_index; // 客户端操作指令编号
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
    int frame_number;
    int command_count;
    player_input_command commands[MAX_COMMANDS_PER_PACKET];
    frame_packet(int frame_number, int command_count, const player_input_command *commands) : frame_number(frame_number), command_count(command_count)
    {
        for (int i = 0; i < command_count && i < MAX_COMMANDS_PER_PACKET; ++i)
        {
            this->commands[i] = commands[i];
        }
    }
};

struct join_packet
{
    int type_index;
    int id;
    int frame_number;
    join_packet(int type_index, int id, int frame_number) : type_index(type_index), id(id), frame_number(frame_number)
    {
    }
};

#pragma pack(pop)

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
