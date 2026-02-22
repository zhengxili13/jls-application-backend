using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace JLSApplicationBackend.hubs;

public class MessageHub : Hub
{
    private readonly IMessageRepository _message;
    private readonly IUserRepository _userRepository;

    public MessageHub(IMessageRepository messageRepository, IUserRepository userRepository)
    {
        _message = messageRepository;
        _userRepository = userRepository;
    }

    /* Publish message to everybody in the channel(show/hide message by font-end), method is not complete for the moment
     * todo:  redefine the message model, build a unique channel between fromUser and toUser ,
     * find a light way to stock the conversation  (actually, we need to write into db everytime for the conversation)
     */
    public async Task NewMessage(Message msg)
    {
        var username = Context.User.Identity.Name;
        await Clients.All.SendAsync("MessageReceived", msg);

        await _userRepository.InsertDialog(msg.message, (int)msg.fromUser, msg.toUser);
    }

    public Task SendPrivateMessage(string user, string message)
    {
        return Clients.User(user).SendAsync("ReceiveMessage", message);
    }
}