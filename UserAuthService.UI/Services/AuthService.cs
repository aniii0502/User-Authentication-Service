using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UserAuthService.UI.Models;

namespace UserAuthService.UI.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private const string TOKEN_KEY = "authToken";

        public AuthService(HttpClient http, IJSRuntime js)
        {
            // Ensure HttpClient.BaseAddress points to your API
            _http = http;
            _js = js;
        }

        // -------------------
        // REGISTER
        // -------------------
        public async Task<UserResponse?> Register(RegisterRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Registration failed: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<UserResponse>();

                // Store token if backend provides one
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    await SetToken(result.Token);
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        // -------------------
        // LOGIN
        // -------------------
        public async Task<UserResponse?> Login(LoginRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Login failed: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<UserResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    await SetToken(result.Token);
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        // -------------------
        // FORGOT PASSWORD
        // -------------------
        public async Task<bool> ForgotPassword(ForgotPasswordRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/forgot-password", request);
            return response.IsSuccessStatusCode;
        }

        // -------------------
        // RESET PASSWORD
        // -------------------
        public async Task<bool> ResetPassword(ResetPasswordRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/reset-password", request);
            return response.IsSuccessStatusCode;
        }

        // -------------------
        // TOKEN HANDLING
        // -------------------
        public async Task SetToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
                await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
        }

        public async Task<string?> GetToken()
        {
            return await _js.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY);
        }

        public async Task Logout()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
        }

        // -------------------
        // GET CURRENT USER
        // -------------------
        public async Task<UserResponse?> GetCurrentUser()
        {
            var token = await GetToken();
            if (string.IsNullOrEmpty(token)) return null;

            // Add Bearer token header
            if (_http.DefaultRequestHeaders.Authorization == null)
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                return await _http.GetFromJsonAsync<UserResponse>("api/auth/me");
            }
            catch
            {
                return null;
            }
        }
    }
}
