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
#include "utils.hpp"

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
    utils utils;
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
            while (true)
            {
                char buf[1028];
                sockaddr_in client_addr;
                int bytesReceived = network_manager.receive_from_client(buf, 1028, (sockaddr *)&client_addr);
                if (bytesReceived > 0)
                {
                    char *cursor = buf;
                    int packet_index = 0;
                    memcpy(&packet_index, cursor, sizeof(int));
                    packet_type type = (packet_type)packet_index;

                    switch (type)
                    {
                    case packet_type::Join:
                    {
                        std::cout << "接收客户端请求链接：成功"
                                  << std::endl;

                        join_packet response_packet((int)packet_type::Response,
                                                    client_manager.get_client_id(),
                                                    current_server_frame);
                        client receive_client(client_addr);
                        client_manager.add_client_with_check(receive_client); // 添加客户端

                        char send_buf[1028];
                        int buf_len = utils.serialized_packet(response_packet, send_buf);
                        network_manager.send_buf_to_client(current_server_frame, send_buf, buf_len, client_addr);
                        std::cout << "成功发送回应"
                                  << " 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                                  << std::endl;
                        break;
                    }

                    case packet_type::Command:
                    {
                        std::cout << "接收客户端指令 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                                  << "当前服务端逻辑帧:" << current_server_frame
                                  << "当前服务端已连接客户端数量：" << client_manager.get_client_count()
                                  << std::endl;

                        player_input_command receive_command = utils.deserialized_command(buf);
                        frame_sync_manager.add_command_in_map(receive_command, current_server_frame);
                        break;
                    }

                    default:
                    {
                        std::cout << "未知包类型:" << (int)type
                                  << "接收客户端指令 当前时间： " << std::put_time(std::localtime(&now_c), "%F %T")
                                  << std::endl;
                        break;
                    }
                    }
                }
                else
                {
                    break;
                }
            }

            int client_count = client_manager.get_client_count();

            // 2.处理指令阶段
            if (client_count > 0)
            {
                if (frame_sync_manager.check_have_all_command(current_server_frame, client_count))
                {
                    std::vector<sockaddr_in> client_addrs = client_manager.get_all_client_addr();
                    for (auto &&client_addr : client_addrs)
                    {
                        std::vector<player_input_command> command_set = frame_sync_manager.get_command_set(current_server_frame);
                        frame_packet packet((int)packet_type::CommandSet, current_server_frame, command_set.size(), command_set.data());

                        char send_buf[1028];
                        int buf_len = utils.serialized_packet(packet, send_buf);
                        network_manager.send_buf_to_client(current_server_frame, send_buf, buf_len, client_addr);
                    }
                }
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