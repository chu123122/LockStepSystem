#include <vector>
#include <map>
#include "player_input_command.hpp"

class frame_sync_manager
{
private:
    /* data */
    std::map<int, std::vector<player_input_command>> commandSetMap;

public:
    frame_sync_manager(/* args */);
    ~frame_sync_manager();

    std::vector<player_input_command> get_command_set(int frame_count)
    {
        if (commandSetMap.find(frame_count) == commandSetMap.end())
        {
            std::cout << "警告,当前帧未存在指定指令集,请检查check_have_all_command函数是否存在问题" << std::endl;
            return std::vector<player_input_command>();
        }
        return commandSetMap[frame_count];
    }

    bool check_have_command(int frame_count, int client_count)
    {
        // 没有指定逻辑帧的指令集
        if (commandSetMap.find(frame_count) == commandSetMap.end())
            return false;

        std::vector<player_input_command> &check_commands = commandSetMap[frame_count];
        // 注意该处逻辑进行了简化,只检查是否有指令在指令集中，没有实现等待其他客户端的机制
        if (check_commands.size() == 0)
            return false;

        return true;
    }

    void add_command_in_map(player_input_command command, int frame_count)
    {
        commandSetMap[frame_count].push_back(command);
    }
};

frame_sync_manager::frame_sync_manager(/* args */)
{
}

frame_sync_manager::~frame_sync_manager()
{
}
