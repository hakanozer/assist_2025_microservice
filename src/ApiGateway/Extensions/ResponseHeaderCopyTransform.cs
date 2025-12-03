using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

public class ResponseHeaderCopyTransform : ResponseTransform
{
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        foreach (var header in context.ProxyResponse.Headers)
        {
            context.HttpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        return ValueTask.CompletedTask;
    }
}
