#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <stdio.h>
#include <vector>
#include <map>
#include <thread>
#include <chrono>
#include <iostream>
#include <iomanip>

#include "network_manager.hpp"
#include "client_manager.hpp"
#include "frame_sync_manager.hpp"

int serialized_frame_packet(
    const frame_packet &send_packet,
    char *buffer)
{
    char *cursor = buffer;

    int frame_count = send_packet.frame_number;
    memcpy(cursor, &frame_count, sizeof(int));
    cursor += sizeof(int);

    int command_count = send_packet.command_count;
    memcpy(cursor, &command_count, sizeof(int));
    cursor += sizeof(int);

    for (auto &&command : send_packet.commands)
    {
        memcpy(cursor, &command, sizeof(player_input_command));
        cursor += sizeof(player_input_command);
    }
    return cursor - buffer;
}

int serialized_join_packet(const join_packet &send_packet, char *buffer)
{
    char *cursor = buffer;

    int type_index = send_packet.type_index;
    memcpy(cursor, &type_index, sizeof(int));
    cursor += sizeof(int);

    int id = send_packet.id;
    memcpy(cursor, &id, sizeof(int));
    cursor += sizeof(int);

    int frame_number = send_packet.frame_number;
    memcpy(cursor, &frame_number, sizeof(int));
    cursor += sizeof(int);

    return cursor - buffer;
}

player_input_command deserialized_command(char *data)
{
    char *cursor = data;

    int client_id;
    memcpy(&client_id, cursor, sizeof(int));
    cursor += sizeof(int);

    float x;
    memcpy(&x, cursor, sizeof(float));
    cursor += sizeof(float);

    float y;
    memcpy(&y, cursor, sizeof(float));
    cursor += sizeof(float);

    float z;
    memcpy(&z, cursor, sizeof(float));
    cursor += sizeof(float);

    return player_input_command(client_id, x, y, z);
}

const float SERVER_TICK_RATE = 30.0f;
const float TIME_STEP = 1.0f / SERVER_TICK_RATE;
int current_server_frame = 0;

auto lastTime = std::chrono::high_resolution_clock::now();
float accumulator = 0.0f;

int main(void)
{
    network_manager network_manager;
    client_manager client_manager;
    frame_sync_manager frame_sync_manager;
    if (!network_manager.init())
        printf("初始化失败");

    while (true)
    {
        // 时间管理计算
        auto frameStartTime = std::chrono::high_resolution_clock::now();
        std::time_t now_c = std::chrono::system_clock::to_time_t(frameStartTime);

        std::chrono::duration<float> deltaTime_duration = frameStartTime - lastTime;
        float deltaTime = deltaTime_duration.count();
        lastTime = frameStartTime;

        accumulator += deltaTime;
        while (accumulator > TIME_STEP)
        {
            // 0.接收客户端连接中
            while (client_manager.get_client_count() == 0)
            {
                std::cout << "尝试接接收客户端请求链接 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                          << "当前服务端逻辑帧:" << current_server_frame
                          << std::endl;
                char buf[1028];
                sockaddr_in client_addr;
                int bytesReceived = network_manager.receive_from_client(buf, 1028, (sockaddr *)&client_addr);
                if (bytesReceived > 0)
                {
                    std::cout << "接收客户端请求链接：成功"
                              << std::endl;

                    char *cursor = buf;
                    int type_index = -1;
                    memcpy(&type_index, cursor, sizeof(int));
                    cursor += sizeof(int);

                    if (type_index != 1)
                    {
                        std::cout << "接收客户端请求链接：信息类型错误"
                                  << std::endl;
                        break;
                    }
                    client receive_client(client_addr);
                    client_manager.add_client_with_check(receive_client); // 添加客户端
                    std::cout << "成功添加客户端"
                              << std::endl;
                    // 发送回应
                    join_packet response_packet(2, client_manager.get_client_id(), current_server_frame);
                    char send_buf[1028];
                    int buf_len = serialized_join_packet(response_packet, send_buf);
                    network_manager.send_buf_to_client(current_server_frame, send_buf, buf_len, client_addr);
                    std::cout << "成功发送回应"
                              << std::endl;
                }
                else
                {
                    std::cout << "接收客户端请求链接：失败"
                              << std::endl;
                    break;
                }
            }

            // 1.收集指令阶段
            // 在一个逻辑帧中尽可能接收客户端发送过来的信息
            while (true && client_manager.get_client_count() != 0)
            {
                std::cout << "尝试接收客户端指令 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                          << "当前服务端逻辑帧:" << current_server_frame
                          << "当前服务端已连接客户端数量：" << client_manager.get_client_count()
                          << std::endl;
                char buf[1028];
                sockaddr_in client_addr;
                int bytesReceived = network_manager.receive_from_client(buf, 1028, (sockaddr *)&client_addr);
                if (bytesReceived > 0)
                {
                    std::cout << "接收到客户端指令：成功"
                              << std::endl;

                    player_input_command receive_command = deserialized_command(buf);
                    frame_sync_manager.add_command_in_map(receive_command, current_server_frame);

                    client receive_client(client_addr);
                    client_manager.add_client_with_check(receive_client);
                }
                else
                {
                    std::cout << "接收客户端指令：失败"
                              << std::endl;
                    break;
                }
            }

            // 2.处理指令阶段
            int client_count = client_manager.get_client_count();
            std::cout << "当前客户端数量：" << client_count
                      << std::endl;
            if (frame_sync_manager.check_have_all_command(current_server_frame, client_count))
            {
                frame_sync_manager.update_frame_sync_logic();
            }
            accumulator -= TIME_STEP;
            current_server_frame++;

            // 3.休眠阶段
            auto frameEndTime = std::chrono::high_resolution_clock::now();
            auto frameDuration = frameEndTime - frameStartTime;
            auto targetDuration = std::chrono::duration<float>(TIME_STEP);

            if (frameDuration < targetDuration)
            {
                auto sleepDuration = targetDuration - frameDuration;
                std::this_thread::sleep_for(sleepDuration);
            }
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}