using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.Lambda.APIGatewayEvents;
using Moq;
using NUnit.Framework;
using System.Net;

namespace ScimSsoAuthorize.Tests
{
    [TestFixture]
    public class FunctionTests
    {
        private Mock<IAmazonAPIGateway>? _mockApiGateway;
        private Function? _function;

        [SetUp]
        public void Setup()
        {
            //Ensure the mocked API Gateway is passed to the function
            _mockApiGateway = new Mock<IAmazonAPIGateway>();
            _function = new Function(_mockApiGateway.Object);
        }

        [Test]
        public async Task FunctionHandler_ValidApiKey_ReturnsOk()
        {
            // Arrange: Mock API Gateway to return a valid API key
            _mockApiGateway?.Setup(x => x.GetApiKeysAsync(It.IsAny<GetApiKeysRequest>(), default))
                    .ReturnsAsync(new GetApiKeysResponse
                    {
                        Items = new List<ApiKey>
                        {
                            new ApiKey { Value = "95OmsemxXW2z3TYmLQfEJ7pt3ER2sXIO6pjMt5uJ" }
                        }
                    });

            var request = new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string> { { "x-api-key", "95OmsemxXW2z3TYmLQfEJ7pt3ER2sXIO6pjMt5uJ" } }
            };

            // Act: Call the function
            var response = await _function.FunctionHandler(request, null);

            // Assert: Expect 200 OK
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(response.Body, Does.Contain("SCIM API request authorized"));
        }
    }
}
