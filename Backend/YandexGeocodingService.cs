using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public interface IGeocodingService
{
    Task<(string Latitude, string Longitude)> GetCoordinatesAsync(string address);
}

public class YandexGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public YandexGeocodingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["YandexGeocoding:ApiKey"]!;
    }

    public async Task<(string Latitude, string Longitude)> GetCoordinatesAsync(string address)
    {
        string requestUrl = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}&geocode={Uri.EscapeDataString(address)}&format=json";

        var response = await _httpClient.GetStringAsync(requestUrl);
        var json = JObject.Parse(response);
        var pos = json["response"]["GeoObjectCollection"]["featureMember"][0]["GeoObject"]["Point"]["pos"].ToString();
        var coords = pos.Split(' ');

        return (Latitude: coords[1], Longitude: coords[0]);
    }
}