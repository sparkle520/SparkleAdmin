﻿//using Quartz;

//namespace SparkleAdmin.BackUpServices.QuartzJob;

//public class WordQuartzJob : IJob
//{
//    /// <summary>
//    /// 当前任务执行的Core表达式
//    /// </summary>
//    public static string Cron = "0/1 * * * * ? ";


//    public async Task Execute(IJobExecutionContext context)
//    {
//        try
//        {
//            //业务处理逻辑
//            Console.WriteLine("我是WordQuartzJob，任务2");

//            // 查询数据库表
//            //var date = await SqlsugarHelper.db.Queryable<bs_gallery>()
//            //    .Where(x => x.IsDelete == false)
//            //    .OrderBy(x => SqlFunc.GetRandom())
//            //    .FirstAsync();

//            //Console.WriteLine(JsonConvert.SerializeObject(date));
//        }
//        catch (Exception ex)
//        {
//            FileHelper.Write(ex.Message, "error");
//        }
//    }
//}