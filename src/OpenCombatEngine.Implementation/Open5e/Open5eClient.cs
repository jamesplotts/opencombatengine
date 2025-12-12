using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpenCombatEngine.Implementation.Open5e.Models;

namespace OpenCombatEngine.Implementation.Open5e
{
    public class Open5eClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.open5e.com/v1/";

        public Open5eClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public virtual async Task<Open5eSpell?> GetSpellAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            
            try
            {
                var uri = new Uri($"{BaseUrl}spells/{slug}/");
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<Open5eSpell>(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public virtual async Task<Open5eMonster?> GetMonsterAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            try
            {
                var uri = new Uri($"{BaseUrl}monsters/{slug}/");
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<Open5eMonster>(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
        public virtual async Task<Open5eListResult<Open5eWeapon>?> GetWeaponsAsync(int page = 1)
        {
            return await GetListAsync<Open5eWeapon>("weapons", page).ConfigureAwait(false);
        }

        public virtual async Task<Open5eListResult<Open5eArmor>?> GetArmorAsync(int page = 1)
        {
            return await GetListAsync<Open5eArmor>("armor", page).ConfigureAwait(false);
        }

        public virtual async Task<Open5eListResult<Open5eMagicItem>?> GetMagicItemsAsync(int page = 1)
        {
            return await GetListAsync<Open5eMagicItem>("magicitems", page).ConfigureAwait(false);
        }

        private async Task<Open5eListResult<T>?> GetListAsync<T>(string endpoint, int page)
        {
             try
            {
                var uri = new Uri($"{BaseUrl}{endpoint}/?page={page}");
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<Open5eListResult<T>>(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
