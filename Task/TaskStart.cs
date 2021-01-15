using System;
using Quartz;
using Quartz.Impl;

namespace SingleOPC.Task
{
    public class TaskStart
    {
        readonly StdSchedulerFactory _factory = new StdSchedulerFactory();
        IScheduler _scheduler;
        public TaskStart()
        {
            
        }
        public async System.Threading.Tasks.Task Start()
        {
            _scheduler = await _factory.GetScheduler();
            await _scheduler.Start();
            IJobDetail job = JobBuilder.Create<OpcTask>().Build();
            ITrigger trigger = TriggerBuilder.Create().WithIdentity("opcTrigger", "opcGroup").WithSimpleSchedule(
                x => x.WithIntervalInSeconds(60).RepeatForever()).Build();
            await _scheduler.ScheduleJob(job, trigger);
        }

        public async System.Threading.Tasks.Task Stop()
        {
            if (_scheduler!=null)
            {
                await _scheduler.Shutdown();
            }
        }
    }
}