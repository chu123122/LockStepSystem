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

    void update_frame_sync_logic()
    {
    }

    bool check_have_all_command(int frame_count, int client_count)
    {
        if (commandSetMap.find(frame_count) == commandSetMap.end())
            return false;
        std::vector<player_input_command> check_commands = commandSetMap[frame_count];
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
