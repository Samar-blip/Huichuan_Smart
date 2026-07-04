namespace Domain.Entity
{
    //日志类型枚举
    public enum LogTypeEnum
    {
        //登录/登出
        Login = 1,
        //查询操作
        Query = 2,
        //新增操作
        Add = 3,
        //修改操作
        Edit = 4,
        //删除操作
        Delete = 5,
        //导入导出
        ImportExport = 6,
        //权限操作
        Permission = 7,
        //登出操作
        Logout = 8,
        //验证码发送
        SendSms = 9,
        //其他操作
        Other = 99
    }
}
