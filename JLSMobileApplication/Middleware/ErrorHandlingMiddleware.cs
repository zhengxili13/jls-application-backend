using System;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace JLSApplicationBackend.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);

            // 如果响应还没有开始，且状态码不是 200，则处理错误消息
            if (!context.Response.HasStarted && context.Response.StatusCode != 200)
            {
                var statusCode = context.Response.StatusCode;
                var msg = statusCode switch
                {
                    401 => "未授权",
                    404 => "未找到服务",
                    502 => "请求错误",
                    _ => "未知错误"
                };
                await HandleExceptionAsync(context, statusCode, msg);
            }
        }
        catch (Exception ex)
        {
            // 如果响应已经开始，我们不能再修改状态码或写入内容
            if (context.Response.HasStarted)
            {
                return;
            }

            var statusCode = context.Response.StatusCode;
            if (statusCode == 200) statusCode = 500; // 默认发生异常时为 500
            if (ex is ArgumentException) statusCode = 200;
            
            await HandleExceptionAsync(context, statusCode, ex.Message);
        }
    }

    // 异常错误信息捕获，将错误信息用 Json 方式返回
    private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string msg)
    {
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var result = JsonConvert.SerializeObject(new ApiResult
        {
            Success = false,
            Msg = msg,
            Type = statusCode.ToString()
        });

        await context.Response.WriteAsync(result);
    }
}

//扩展方法
public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}