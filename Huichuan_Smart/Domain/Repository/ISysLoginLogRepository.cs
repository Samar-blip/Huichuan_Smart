using Domain.Entity;

namespace Domain.Repository
{
    //登录日志仓储接口
    public interface ISysLoginLogRepository
    {
        //保存登录日志
        Task SaveAsync(SysLoginLog log);

        //分页查询登录日志
        Task<(List<SysLoginLog> List, int Total)> GetListAsync(
            int page = 1,
            int pageSize = 20,
            string? account = null,
            long? userId = null,
            bool? isSuccess = null,
            DateTime? startTime = null,
            DateTime? endTime = null);
    }
}
