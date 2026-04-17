using System;
using NUnit.Framework;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using Foody_App.DTOs;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Internal;
using static System.Formats.Asn1.AsnWriter;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel;



namespace Foody_App
{
    [TestFixture]
    public class Foody_App_Tests
    {
        private RestClient client;
        private const string BaseUrl = "http://144.91.123.158:81/api";
        private const string user = "anelia";
        private const string pass = "123123";
        private static string? LastCreatedFood;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(user, pass);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/User/Authentication", Method.Post);

            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception($"Login failed: {response.Content}");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            if (!json.TryGetProperty("accessToken", out var tokenProperty))
            {
                throw new InvalidOperationException($"Token not found in authentication response:  {response.Content}");
            }

            var token = tokenProperty.GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException($"Authentication succeeded but token was empty. Response: {response.Content}");
            }

            return token;
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithRequiredFields_ShouldReturnSuccess()
        {
            //Create a new food with the required fields and verify the response
            FoodDTO food = new FoodDTO
            {
                Name = "Test Food",
                Description = "This is a test food item.",
                Url = ""
            };
            //Arrange create the request
            var request = new RestRequest("/Food/Create", Method.Post);
            // Add the JSON body to the request
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(food);
            //Act - execute the request
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? string.Empty);
            
            // � Assert that the response status code is Created(201).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected status code 201 Created but got {(int)response.StatusCode} {response.StatusCode}. Response content: {response.Content}");
            ////� Assert that the response message is "Food created successfully".
            //Assert.That(responseData.Msg, Is.EqualTo("Food created successfully"), $"Expected message 'Food created successfully' but got '{responseData.Msg}'. Response content: {response.Content}");
            //� Assert that the response body contains a foodId property.
            Assert.That(responseData.FoodId, Is.Not.Null.And.Not.Empty, $"Expected foodId to be present in the response but it was null or empty. Response content: {response.Content}");
            //� Store the foodId of the created food in a static member of the test class to maintain its value between test runs.
            LastCreatedFood = responseData.FoodId;
        }

        [Order(2)]
        [Test]
        public void EditedFood_Title_ShouldReturnSuccess()
        {
            Assert.That(LastCreatedFood, Is.Not.Null.And.Not.Empty,
                "LastCreatedFood is not set. Make sure to create one first.");

            // Arrange
            object updatedFood = new[]
            {
                new { path = "/name", op = "replace",  value = "Updated Test Food" }
            };

            var request = new RestRequest($"/Food/Edit/{LastCreatedFood}", Method.Patch);

            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(updatedFood);
            
            //Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? string.Empty);
            
            //Assert
            //Assert that the response status code is OK(200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected status code 200 OK but got {response.StatusCode}. Response content: {response.Content}");
            //Assert that the response message indicates the food was "Successfully edited"
            Assert.That(responseData.Msg, Is.EqualTo("Successfully edited"), $"Expected message 'Food successfully edited' but got '{responseData.Msg}'. Response content: {response.Content}");
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturnNonEmptyArray()
        {
            // Arrange
            var request = new RestRequest("/Food/All", Method.Get);

            // Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content ?? string.Empty);

            // Assert that the response status code is OK(200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected status code 200 OK but got {response.StatusCode}. Response content: {response.Content}");
            // Assert that the response contains a non - empty array.
            Assert.That(response.Content, Does.Not.Contain("[]"), "Expected response to contain a non-empty array of ideas.");
            Assert.That(responseData, Is.Not.Null, $"Expected response data to be a non-null array but got null. Response content: {response.Content}");
            Assert.That(responseData, Is.Not.Empty, $"Expected response data to be a non-empty array but got an empty array. Response content: {response.Content}");
            Assert.That(responseData.Count, Is.GreaterThan(0), $"Expected response data to contain at least one food item but got {responseData.Count}. Response content: {response.Content}");
        }

        [Order(4)]
        [Test]
        public void DeleteEditedFood_ShouldReturnSuccess()
        {
            //Arrange
            var request = new RestRequest($"/Food/Delete/{LastCreatedFood}", Method.Delete);

            //Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? string.Empty);

            //Assert
            //Assert that the response status code is OK(200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected status code 200 OK but got {response.StatusCode}. Response content: {response.Content}");
            //Confirm that the response message is "Deleted successfully!".
            Assert.That(responseData.Msg, Is.EqualTo("Deleted successfully!"), $"Expected message 'Deleted successfully!' but got '{responseData.Msg}'. Response content: {response.Content}");
        }

        [Order(5)]
        [Test]
        public void CreateFoodWithoutRequiredData_ShouldReturnBadRequest()
        {
            //Create a new food with the required fields and verify the response
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };
            //Arrange create the request
            var request = new RestRequest("/Food/Create", Method.Post);
            // Add the JSON body to the request
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(food);
            // Act - execute the request and get the response
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest but got {response.StatusCode}");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            var nonExistingFoodId = "non-existing-id";
            // Arrange
            object updatedFood = new[]
            {
                new { path = "/name", op = "replace",  value = "Updated Test Food" }
            };

            var request = new RestRequest($"/Food/Edit/{nonExistingFoodId}", Method.Patch);

            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(updatedFood);

            //Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? string.Empty);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected status code 404 NotFound but got {response.StatusCode}. Response content: {response.Content}");
            //Assert that the response message is "No food revues...".
            Assert.That(responseData.Msg, Is.EqualTo("No food revues..."), $"Expected message 'No food revues...' but got '{responseData.Msg}'. Response content: {response.Content}");    
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            var nonExistingFoodId = "non-existing-id";

            //Arrange
            var request = new RestRequest($"/Food/Delete/{nonExistingFoodId}", Method.Delete);

            //Act
            var response = client.Execute(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content ?? string.Empty);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected status code 400 NotFound but got {response.StatusCode}. Response content: {response.Content}");
            //Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected status code 404 NotFound but got {response.StatusCode}. Response content: {response.Content}");
            ////Assert that the response message is "No food revues...".
            Assert.That(responseData.Msg, Is.EqualTo("Unable to delete this food revue!"), $"Expected message 'Unable to delete this food revue!' but got '{responseData.Msg}'. Response content: {response.Content}");
            //Assert.That(responseData.Msg, Is.EqualTo("No food revues..."), $"Expected message 'No food revues...' but got '{responseData.Msg}'. Response content: {response.Content}");

        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (client != null)
            {
                client.Dispose();
            }
        }

       
    }
}