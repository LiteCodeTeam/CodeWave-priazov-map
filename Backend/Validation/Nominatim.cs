using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backend.Validation
{
    public class NominatimGeocoder
    {
        private readonly HttpClient _httpClient;

        public NominatimGeocoder()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://nominatim.openstreetmap.org/")
            };

            _httpClient.DefaultRequestHeaders.UserAgent
                .Add(new ProductInfoHeaderValue("Priazov-Impact", "1.0"));

            // Добавляем задержку для соблюдения лимитов Nominatim
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
        }

        public async Task<(bool IsValid, string Error)> ValidateRussianAddressAsync(string address)
        {
            try
            {
                // Нормализация адреса
                var normalizedAddress = NormalizeAddress(address);
                var url = $"search?format=json&q={Uri.EscapeDataString(normalizedAddress)}&addressdetails=1&countrycodes=ru&accept-language=ru";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return (false, $"Ошибка сервера: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content) || content == "[]")
                    return (false, "Адрес не найден в OpenStreetMap");

                using var doc = JsonDocument.Parse(content);
                var firstResult = doc.RootElement.EnumerateArray().FirstOrDefault();

                return CheckAddressComponents(firstResult, address)!;
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при проверке адреса: {ex.Message}");
            }
        }

        private (bool IsValid, string? Error) CheckAddressComponents(JsonElement result, string originalAddress)
        {
            try
            {
                // 1. Проверяем наличие полного адреса в ответе
                if (!result.TryGetProperty("display_name", out var displayName))
                    return (false, "Некорректный формат ответа");

                string foundAddress = displayName.GetString() ?? "";

                // 2. Проверяем номер дома в оригинальном адресе
                var houseNumberMatch = Regex.Match(originalAddress, @"\d+");
                if (!houseNumberMatch.Success)
                    return (false, "Не указан номер дома");

                string houseNumber = houseNumberMatch.Value;

                // 3. Проверяем, что найденный адрес содержит номер дома
                if (!foundAddress.Contains(houseNumber))
                    return (false, $"Номер дома {houseNumber} не найден");

                // 4. Проверяем основные компоненты
                if (!result.TryGetProperty("address", out var address))
                    return (false, "Некорректный формат ответа");

                bool hasCity = address.TryGetProperty("city", out _) ||
                              address.TryGetProperty("town", out _) ||
                              address.TryGetProperty("village", out _);

                bool hasStreet = address.TryGetProperty("road", out _) ||
                                address.TryGetProperty("pedestrian", out _);

                if (!hasCity)
                    return (false, "Не удалось определить населённый пункт");

                if (!hasStreet)
                    return (false, "Не найдена улица в базе");

                return (true, null);
            }
            catch
            {
                return (false, "Ошибка при анализе ответа");
            }
        }

        private string NormalizeAddress(string address)
        {
            // Улучшенная нормализация
            return Regex.Replace(address, @"(ул\.?|улица)\s+", "улица ")
                      .Replace("д.", "дом ")
                      .Replace("дом", "дом ")
                      .Replace("  ", " ")
                      .Trim();
        }
    }
}