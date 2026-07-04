using SqlSugar;
using Domain.Entity;
using Domain.Repository;

namespace EFCore.Repositorys
{
    //登录日志仓储实现
    public class LoginLogRepositorys : ISysLoginLogRepository
    {
        private readonly ISqlSugarClient _db;

        public LoginLogRepositorys(ISqlSugarClient db)
        {
            _db = db;
        }

        //保存登录日志
        public async Task SaveAsync(SysLoginLog log)
        {
            await _db.Insertable(log).ExecuteCommandAsync();
        }

        //分页查询登录日志
        public async Task<(List<SysLoginLog> List, int Total)> GetListAsync(
            int page = 1,
            int pageSize = 20,
            string? account = null,
            long? userId = null,
            bool? isSuccess = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            var query = _db.Queryable<SysLoginLog>();

            if (!string.IsNullOrWhiteSpace(account))
                query = query.Where(l => l.Account.Contains(account));

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            if (isSuccess.HasValue)
                query = query.Where(l => l.IsSuccess == isSuccess.Value);

            if (startTime.HasValue)
                query = query.Where(l => l.LoginTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(l => l.LoginTime <= endTime.Value);

            RefAsync<int> total = 0;
            var list = await query
                .OrderByDescending(l => l.LoginTime)
                .ToPageListAsync(page, pageSize, total);

            return (list, total);
        }
    }
}
