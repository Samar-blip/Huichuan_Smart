using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Application.Aop
{
    /// <summary>
    /// 方法日志特性 — 用于 Service 层自动记录方法调用、耗时和异常
    /// 在需要拦截的方法上加上 [Log("描述")] 特性即可
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class LogAttribute : Attribute
    {
        /// <summary>
        /// 操作描述，例如 "用户登录"、"数据查询"
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否记录请求参数（默认 true）
        /// </summary>
        public bool LogParameters { get; set; } = true;

        /// <summary>
        /// 是否记录返回值（默认 true）
        /// </summary>
        public bool LogReturnValue { get; set; } = true;

        public LogAttribute(string description = "") => Description = description;
    }

    /// <summary>
    /// 日志拦截器实现 — 使用 ILogger 输出结构化日志
    /// </summary>
    public class LogInterceptor : IInterceptor
    {
        private readonly ILogger<LogInterceptor> _logger;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public LogInterceptor(ILogger<LogInterceptor> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Intercept(IInvocation invocation)
        {
            var logAttr = invocation.Method?.GetCustomAttributes(typeof(LogAttribute), false)
                as LogAttribute[] ?? [];

            if (logAttr == null || logAttr.Length == 0)
            {
                invocation.Proceed();
                return;
            }

            var attr = logAttr[0];
            var desc = string.IsNullOrWhiteSpace(attr.Description) ? "操作" : attr.Description;
            var method = invocation.Method;
            var className = invocation.InvocationTarget?.GetType().FullName ?? "Unknown";
            var methodName = method?.Name ?? "Unknown";
            var paramValues = invocation.Arguments;

            // 开始日志
            var startTime = DateTime.UtcNow;
            var callerIp = GetCallerIp();

            _logger.LogInformation(
                "【{Description}】开始执行 | 类: {ClassName} | 方法: {MethodName} | IP: {IpAddress} | 参数: {@Parameters}",
                desc, className, methodName, callerIp, paramValues);

            if (method != null && attr.LogParameters && paramValues.Length > 0)
            {
                var paramNames = method.GetParameters()
                    .Select(p => $"{(p.Name ?? "param")}: {{}}")
                    .ToArray();

                for (int i = 0; i < paramValues.Length; i++)
                {
                    var paramName = paramNames[i].Replace("{}", "");
                    var 脱敏值 = 脱敏参数(paramName, paramValues[i]?.ToString() ?? "null");
                    _logger.LogDebug("  参数 {Index}: {Param}", i, 脱敏值);
                }
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                invocation.Proceed();
                sw.Stop();

                // 记录成功
                var elapsedMs = sw.ElapsedMilliseconds;

                if (attr.LogReturnValue && invocation.ReturnValue != null)
                {
                    _logger.LogInformation(
                        "【{Description}】执行完成 | 方法: {ClassName}.{MethodName} | IP: {IpAddress} | 状态: 成功 | 耗时: {Elapsed}ms | 返回值类型: {ReturnType}",
                        desc, className, methodName, callerIp, elapsedMs, invocation.ReturnValue.GetType().Name);
                }
                else
                {
                    _logger.LogInformation(
                        "【{Description}】执行完成 | 方法: {ClassName}.{MethodName} | IP: {IpAddress} | 状态: 成功 | 耗时: {Elapsed}ms",
                        desc, className, methodName, callerIp, elapsedMs);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(
                    ex,
                    "【{Description}】执行异常 | 方法: {ClassName}.{MethodName} | IP: {IpAddress} | 耗时: {Elapsed}ms | 异常: {Message}",
                    desc, className, methodName, callerIp, sw.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        /// <summary>
        /// 获取调用者 IP 地址
        /// </summary>
        private string GetCallerIp()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                        return httpContext.Request.Headers["X-Forwarded-For"].ToString();

                    var connection = httpContext.Connection;
                    return connection?.RemoteIpAddress?.ToString() ?? "本地";
                }
            }
            catch { }

            return "未知";
        }

        /// <summary>
        /// 参数脱敏（密码、验证码等敏感信息）
        /// </summary>
        private static string 脱敏参数(string paramName, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var paramNameLower = paramName.ToLowerInvariant();
            if (paramNameLower.Contains("password") || paramNameLower.Contains("pwd") ||
                paramNameLower.Contains("code") || paramNameLower.Contains("captcha"))
            {
                return "***脱敏***";
            }

            return value;
        }
    }
}
