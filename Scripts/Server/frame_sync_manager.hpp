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

    bool check_have_all_command(int frame_count, int client_count)
    {
        // 没有指定逻辑帧的指令集
        if (commandSetMap.find(frame_count) == commandSetMap.end())
            return false;

        std::vector<player_input_command> check_commands = commandSetMap[frame_count];
        // 注意该处逻辑进行了简化，从原本的确定每个客户端都发出了指令简化为该逻辑帧内指令数量等同于客户端数量
        if (check_commands.size() != client_count)
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
