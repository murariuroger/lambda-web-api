using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.APIGateway;
using Weather.Api.IaC.Common;

namespace WeatherApiIaC
{
    public class WeatherApiIaCStack : Stack
    {
        internal WeatherApiIaCStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region IAM
            var lambdaRole = new Role(this, StackConstants.AppName + "-role",
               new RoleProps
               {
                   RoleName = StackConstants.AppName + "-lambda-role",
                   AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),

               });
            lambdaRole.AddToPolicy(new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Resources = new string[] { "*" },
                    Actions = new string[]
                    {
                        "logs:CreateLogGroup",
                        "logs:CreateLogStream",
                        "logs:PutLogEvents"
                    }
                }));
            #endregion

            #region Lambda
            var function = new Function(this, StackConstants.AppName,
                new FunctionProps
                {
                    Description = "Weather Web API.",
                    Handler = "Weather.Api::Weather.Api.LambdaEntryPoint::FunctionHandlerAsync",
                    Code = Code.FromAsset("../Weather.Api/bin/Debug/netcoreapp3.1"),
                    Runtime = Runtime.DOTNET_CORE_3_1,
                    Timeout = Duration.Seconds(90),
                    LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.TWO_MONTHS,
                    Role = lambdaRole,
                    MemorySize = 2500,
                    Tracing = Tracing.ACTIVE
                });
            #endregion

            #region ApiGateway
            var api = new RestApi(this, StackConstants.AppName + "RestApi",
                new RestApiProps
{
                    Description = $"Proxy for {function.FunctionName}",
                    DeployOptions = new StageOptions()
                    {
                        StageName = "Production"
                    }
                });
            var apiResource = api.Root.AddResource("{proxy+}");
            var integration = new LambdaIntegration(function);
            apiResource.AddMethod("GET", integration);;
            #endregion        
        }
    }
}
