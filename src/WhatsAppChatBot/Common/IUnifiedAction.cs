using System.Threading.Tasks;

namespace WhatsAppChatBot.Common;

public interface IUnifiedAction
{
    Task<string> PerformAsync();
}
