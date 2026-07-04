namespace Domain.Entity
{
    //每日日志统计结果
    public class DailyLogStats
    {
        //日期
        public DateTime Date { get; set; }

        //成功次数
        public int SuccessCount { get; set; }

        //失败次数
        public int FailCount { get; set; }

        //总次数
        public int TotalCount { get; set; }
    }
}
