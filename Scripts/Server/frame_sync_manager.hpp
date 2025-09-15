#include <vector>
#include <map>
#include "player_input_command.hpp"

class frame_sync_manager
{
private:
    /* data */
    std::map<int, frameData> frameDataMap;

public:
    frame_sync_manager(/* args */);
    ~frame_sync_manager();

    frameData *get_frame_data(int frame_count)
    {
        if (frameDataMap.find(frame_count) == frameDataMap.end())
        {
            std::cout << "警告,当前帧未存在指定帧数据,请检查check_have_command函数是否存在问题" << std::endl;
            return nullptr;
        }
        return &frameDataMap[frame_count];
    }

    bool check_have_command(int frame_count, int client_count)
    {
        // 没有指定逻辑帧的指令集
        if (frameDataMap.find(frame_count) == frameDataMap.end())
            return false;

        std::vector<player_input_command> &check_commands = frameDataMap[frame_count].player_input_commands;
        // 注意该处逻辑进行了简化,只检查是否有指令数量等于客户端数量
        if (check_commands.size() != client_count)
            return false;

        return true;
    }

    void add_command_in_map(player_input_command command, int frame_count)
    {
        frameDataMap[frame_count].player_input_commands.push_back(command);
    }
    void full_null_command_in_frame_data(frameData &frame_data)
    {
        
    }
};

frame_sync_manager::frame_sync_manager(/* args */)
{
}

frame_sync_manager::~frame_sync_manager()
{
}
