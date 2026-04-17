using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using Foody_App.DTOs;

namespace Foody_App
{
    [TestFixture]
    public class Foody_App_Tests
    {
        private RestClient client;

        private const string BaseUrl = "http://144.91.123.158:81/api";
        private const string Username = "anelia";
        private const string Password = "123123";

        private static string? lastCreatedFoodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken(Username, Password);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/User/Authentication", Method.Post);

            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(response.Content))
                throw new Exception($"Login failed: {response.Content}");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            if (!json.TryGetProperty("accessToken", out var tokenElement))
                throw new InvalidOperationException("Token not found in response.");

            var token = tokenElement.GetString();

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Token is empty.");

            return token;
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var food = new FoodDTO
            {
                Name = "Test Food",
                Description = "Test description",
                Url = ""
            };

            var request = new RestRequest("/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(data.FoodId, Is.Not.Null.And.Not.Empty);

            lastCreatedFoodId = data.FoodId;
        }

        [Test, Order(2)]
        public void EditFood_ShouldReturnSuccess()
        {
            Assert.That(lastCreatedFoodId, Is.Not.Null.And.Not.Empty);

            var body = new[]
            {
                new { path = "/name", op = "replace", value = "Updated Food" }
            };

            var request = new RestRequest($"/Food/Edit/{lastCreatedFoodId}", Method.Patch);
            request.AddJsonBody(body);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(data.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnNonEmptyList()
        {
            var request = new RestRequest("/Food/All", Method.Get);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(data, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/Food/Delete/{lastCreatedFoodId}", Method.Delete);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(data.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_WithInvalidData_ShouldReturnBadRequest()
        {
            var food = new FoodDTO();

            var request = new RestRequest("/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            var body = new[]
            {
                new { path = "/name", op = "replace", value = "Updated Food" }
            };

            var request = new RestRequest("/Food/Edit/non-existing-id", Method.Patch);
            request.AddJsonBody(body);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(data.Msg, Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/Food/Delete/non-existing-id", Method.Delete);

            var response = client.Execute(request);
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? "");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(data.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }
    }
}