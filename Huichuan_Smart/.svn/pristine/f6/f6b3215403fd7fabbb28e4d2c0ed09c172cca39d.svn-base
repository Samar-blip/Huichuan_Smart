using System.Threading;

namespace WebApplication1
{
    /// <summary>
    /// 异步上下文，用于在全局拦截器中获取当前登录用户
    /// </summary>
    public static class AsyncHttpContext
    {
        private static AsyncLocal<string?> _currentUser;

        public static string? UserName
        {
            get => _currentUser?.Value;
            set
            {
                if (_currentUser != null)
                    _currentUser.Value = value;
            }
        }

        static AsyncHttpContext()
        {
            _currentUser = new AsyncLocal<string?>();
        }

        /// <summary>
        /// 从 HTTP 上下文中提取当前用户名并设置到 AsyncLocal，在中间件中调用，供 SqlSugar 拦截器使用
        /// </summary>
        /// <param name="userName">当前登录用户名</param>
        public static void SetUserName(string? userName)
        {
            _currentUser.Value = userName;
        }

        /// <summary>
        /// 清除当前用户上下文
        /// </summary>
        public static void Clear()
        {
            _currentUser.Value = null;
        }
    }
}
