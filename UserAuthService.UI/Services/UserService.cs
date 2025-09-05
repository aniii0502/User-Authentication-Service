using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UserAuthService.UI.Models;

namespace UserAuthService.UI.Services
{
    public class UserService
    {
        private readonly HttpClient _http;

        public UserService(HttpClient http)
        {
            _http = http;
        }

        public async Task<UserResponse?> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return await _http.GetFromJsonAsync<UserResponse>($"api/users/{id}");
        }
    }
}
