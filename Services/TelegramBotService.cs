using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using itam.Data;
using itam.Models;

namespace itam.Services
{
    public interface ITelegramService
    {
        Task SendAsync(string message);
    }

    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _http;
        private readonly IServiceScopeFactory _scopeFactory;

        public TelegramService(HttpClient http, IServiceScopeFactory scopeFactory)
        {
            _http = http;
            _scopeFactory = scopeFactory;
        }

        public async Task SendAsync(string message)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settings = context.TelegramBotSettings.FirstOrDefault();

            if (settings == null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.BotToken))
                return;

            var chatIds = settings.ChatIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var chatId in chatIds)
            {
                try
                {
                    var payload = new
                    {
                        chat_id = chatId.Trim(),
                        text = message,
                        parse_mode = "Markdown"
                    };
                    var url = $"https://api.telegram.org/bot{settings.BotToken}/sendMessage";
                    await _http.PostAsJsonAsync(url, payload);
                }
                catch
                {
                    // Silently ignore send failures so it doesn't break the main flow
                }
            }
        }
    }
}
