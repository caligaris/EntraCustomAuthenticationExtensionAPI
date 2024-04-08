using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Company.Function.Middleware
{
    public static class FunctionContextExtensions
    {
        public static async Task SetHttpResponseStatusCode(
            this FunctionContext context,
            HttpStatusCode statusCode)
        {
            await context.CreateJsonResponse(statusCode, new { Message = "Forbidden Access." });
        }
        public static void SetResponseData(this FunctionContext functionContext, HttpResponseData responseData)
        {   
            var feature = functionContext.Features.FirstOrDefault(f => f.Key.Name == "IFunctionBindingsFeature").Value;
            if (feature == null) throw new Exception("Required binding feature is not present.");
            var pinfo = feature.GetType().GetProperty("InvocationResult");
            if (pinfo != null) 
                pinfo.SetValue(feature, responseData);
        }        

        public static async Task CreateJsonResponse<T>(this FunctionContext functionContext, System.Net.HttpStatusCode statusCode, T data)
        {
            //var request = functionContext.GetHttpRequestData();
            var request = await functionContext.GetHttpRequestDataAsync();

            if (request != null)
            {
                var response = request.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(data);
                response.StatusCode = statusCode;
                functionContext.SetResponseData(response);
            }
        }        

        public static MethodInfo GetTargetFunctionMethod(this FunctionContext context)
        {
            // More terrible reflection code..
            // Would be nice if this was available out of the box on FunctionContext

            // This contains the fully qualified name of the method
            // E.g. IsolatedFunctionAuth.TestFunctions.ScopesAndAppRoles
            var entryPoint = context.FunctionDefinition.EntryPoint;

             var assemblyPath = context.FunctionDefinition.PathToAssembly;
            // var assembly = Assembly.LoadFrom(assemblyPath);
            var assembly = Assembly.GetExecutingAssembly();
            var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
            var type = assembly.GetType(typeName);
            var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
            var method = type?.GetMethod(methodName);
            return method ?? throw new InvalidOperationException(
                $"Could not find method {entryPoint} in assembly {assemblyPath}");
        }
    }
}