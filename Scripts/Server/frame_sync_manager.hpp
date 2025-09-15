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
            frameDataMap[frame_count] = frameData();
        }
        return &frameDataMap[frame_count];
    }

    void add_command_in_map(player_input_command command, const int frame_count)
    {
        frameDataMap[frame_count].player_input_commands.push_back(command);
    }
    void full_null_command_in_frame_data(frameData &frame_data, int client_count)
    {
        int empty_command_count = client_count - frame_data.player_input_commands.size();
        for (int i = 0; i < empty_command_count; i++)
        {
            player_input_command empty_command = player_input_command();
            frame_data.player_input_commands.push_back(empty_command);
        }
    }
};

frame_sync_manager::frame_sync_manager(/* args */)
{
}

frame_sync_manager::~frame_sync_manager()
{
}
