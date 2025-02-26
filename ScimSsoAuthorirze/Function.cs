using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net;

// Lambda function attributes
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScimSsoAuthorize
{
    public class Function
    { 
        private readonly IAmazonAPIGateway _apiGateway;
        private static readonly Dictionary<string, string> _responseHeaders = new()
        {
            { "Access-Control-Allow-Headers", "Content-Type" },
            { "Access-Control-Allow-Origin", "*" },
            { "Access-Control-Allow-Methods", "OPTIONS,POST,GET" }
        };

        public Function() : this(new AmazonAPIGatewayClient()) { }

        public Function(IAmazonAPIGateway apiGateway)
        {
            _apiGateway = apiGateway ?? throw new ArgumentNullException(nameof(apiGateway));
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Extract API key from request headers
                if (!request.Headers.TryGetValue("x-api-key", out string providedApiKey) || string.IsNullOrEmpty(providedApiKey))
                {
                    return GenerateResponse(HttpStatusCode.Forbidden, "Missing API Key");
                }

                // Validate API key with API Gateway
                bool isValidKey = await ValidateApiKey(providedApiKey);

                if (!isValidKey)
                {
                    return GenerateResponse(HttpStatusCode.Forbidden, "Invalid API Key");
                }

                return GenerateResponse(HttpStatusCode.OK, "SCIM API request authorized");
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Error: {ex.Message}");
                return GenerateResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
            }
        }

        private async Task<bool> ValidateApiKey(string apiKey)
        {
            try
            {
                // Retrieve API Key details from AWS API Gateway
                var getApiKeysRequest = new GetApiKeysRequest { IncludeValues = true };
                var apiKeysResponse = await _apiGateway.GetApiKeysAsync(getApiKeysRequest);

                foreach (var apiKeyDetails in apiKeysResponse.Items)
                {
                    if (apiKeyDetails.Value == apiKey)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Error validating API key: {ex.Message}");
                return false;
            }
        }

        private APIGatewayProxyResponse GenerateResponse(HttpStatusCode statusCode, string message)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Headers = _responseHeaders,
                Body = JsonConvert.SerializeObject(new { message })
            };
        }
    }
}