using System.Net.Http.Json;
using System.Text.Json;
using InventorySystem.Blazor.Blazor.Models;

namespace InventorySystem.Blazor.Blazor.Services;

public class ApiService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string LastErrorMessage { get; private set; } = string.Empty;

    public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(int page, int pageSize)
    {
        LastErrorMessage = string.Empty;

        var response = await httpClient.GetAsync($"api/products?page={page}&pageSize={pageSize}");
        var apiResponse = await ReadApiResponseAsync<PagedResult<ProductResponseDto>>(response);

        return apiResponse.Data ?? new PagedResult<ProductResponseDto>();
    }

    public async Task<ProductResponseDto> GetProductAsync(int id)
    {
        LastErrorMessage = string.Empty;

        var response = await httpClient.GetAsync($"api/products/{id}");
        var apiResponse = await ReadApiResponseAsync<ProductResponseDto>(response);

        return apiResponse.Data ?? new ProductResponseDto();
    }

    public async Task<bool> CreateProductAsync(CreateProductDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/products", dto);
        return await IsSuccessAsync(response);
    }

    public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/products/{id}", dto);
        return await IsSuccessAsync(response);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/products/{id}");
        return await IsSuccessAsync(response);
    }

    public async Task<List<VariantResponseDto>> GetVariantsAsync(int productId)
    {
        LastErrorMessage = string.Empty;

        var response = await httpClient.GetAsync($"api/products/{productId}/variants");
        var apiResponse = await ReadApiResponseAsync<List<VariantResponseDto>>(response);

        return apiResponse.Data ?? [];
    }

    public async Task<bool> AddVariantAsync(int productId, AddVariantDto dto)
    {
        var response = await httpClient.PostAsJsonAsync($"api/products/{productId}/variants", dto);
        return await IsSuccessAsync(response);
    }

    public async Task<bool> UpdateVariantAsync(int variantId, UpdateVariantDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/variants/{variantId}", dto);
        return await IsSuccessAsync(response);
    }

    public async Task<bool> DeleteVariantAsync(int variantId)
    {
        var response = await httpClient.DeleteAsync($"api/variants/{variantId}");
        return await IsSuccessAsync(response);
    }

    public async Task<List<StockResponseDto>> GetProductStockAsync(int productId)
    {
        LastErrorMessage = string.Empty;

        var response = await httpClient.GetAsync($"api/stock/product/{productId}");
        var apiResponse = await ReadApiResponseAsync<List<StockResponseDto>>(response);

        return apiResponse.Data ?? [];
    }

    public async Task<bool> AddStockAsync(AddStockDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/stock/add", dto);
        return await IsSuccessAsync(response);
    }

    public async Task<(bool success, string errorMessage)> RemoveStockAsync(RemoveStockDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/stock/remove", dto);

        if (response.IsSuccessStatusCode)
        {
            LastErrorMessage = string.Empty;
            return (true, string.Empty);
        }

        var errorMessage = await ReadErrorMessageAsync(response);
        LastErrorMessage = errorMessage;

        return (false, errorMessage);
    }

    private async Task<bool> IsSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            LastErrorMessage = string.Empty;
            return true;
        }

        LastErrorMessage = await ReadErrorMessageAsync(response);
        return false;
    }

    private static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);

        if (apiResponse is null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "The API returned an empty response."
            };
        }

        return apiResponse;
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed with status code {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return message.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var values = errors
                    .EnumerateArray()
                    .Select(error => error.GetString())
                    .Where(error => !string.IsNullOrWhiteSpace(error))
                    .ToList();

                if (values.Count > 0)
                {
                    return string.Join(" ", values);
                }
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return body;
    }
}
