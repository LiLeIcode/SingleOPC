using System;
using OPCAutomation;
using Quartz;
using Quartz.Impl;
using ServiceStack.Redis;
using SingleOPC.Models;
using SingleOPC.OPC;
using SingleOPC.Task;

namespace SingleOPC
{
    class Program
    {
        readonly StdSchedulerFactory _factory = new StdSchedulerFactory();
        IScheduler _scheduler;

        public async System.Threading.Tasks.Task Start()
        {
            _scheduler = await _factory.GetScheduler();
            await _scheduler.Start();
            IJobDetail job = JobBuilder.Create<OpcTask>().Build();
            ITrigger trigger = TriggerBuilder.Create().WithIdentity("opcTrigger", "opcGroup").WithSimpleSchedule(
                x => x.WithIntervalInSeconds(60).RepeatForever()).Build();
            Console.WriteLine(job);
            await _scheduler.ScheduleJob(job, trigger);
        }
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Start().Wait();
            Console.Read();
        }
    }
}
