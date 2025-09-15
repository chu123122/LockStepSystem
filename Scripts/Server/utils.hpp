#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <vector>
#include "player_input_command.hpp"

class Utils
{
private:
    /* data */
public:
    Utils(/* args */);
    ~Utils();

    static int serialized_packet(
        const frame_packet &send_packet,
        char *buffer)
    {
        char *cursor = buffer;

        int packet_type = send_packet.packet_type;
        memcpy(cursor, &packet_type, sizeof(int));
        cursor += sizeof(int);

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

    static int serialized_packet(
        const join_packet &send_packet,
        char *buffer)
    {
        char *cursor = buffer;

        int packet_type = send_packet.packet_type;
        memcpy(cursor, &packet_type, sizeof(int));
        cursor += sizeof(int);

        int id = send_packet.id;
        memcpy(cursor, &id, sizeof(int));
        cursor += sizeof(int);

        int frame_number = send_packet.frame_number;
        memcpy(cursor, &frame_number, sizeof(int));
        cursor += sizeof(int);

        return cursor - buffer;
    }

    static player_input_command deserialized_command(char *data)
    {
        char *cursor = data;

        int packet_type;
        memcpy(&packet_type, cursor, sizeof(int));
        cursor += sizeof(int);

        int client_id;
        memcpy(&client_id, cursor, sizeof(int));
        cursor += sizeof(int);

        int command_type;
        memcpy(&command_type, cursor, sizeof(int));
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

        return player_input_command(client_id, command_type, x, y, z);
    }
};

Utils::Utils(/* args */)
{
}

Utils::~Utils()
{
}
