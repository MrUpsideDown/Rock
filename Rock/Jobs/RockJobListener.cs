// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Jobs
{
    /// <summary>
    /// Summary description for JobListener
    /// </summary>
    public class RockJobListener : IJobListener
    {
        /// <summary>
        /// Get the name of the <see cref="IJobListener"/>.
        /// </summary>
        public string Name
        {
            get
            {
                return "RockJobListener";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RockJobListener"/> class.
        /// </summary>
        public RockJobListener()
        {
        }

        /// <summary>
        /// Called by the <see cref="IScheduler"/> when a <see cref="IJobDetail"/>
        /// is about to be executed (an associated <see cref="ITrigger"/>
        /// has occurred).
        /// <para>
        /// This method will not be invoked if the execution of the Job was vetoed
        /// by a <see cref="ITriggerListener"/>.
        /// </para>
        /// </summary>
        /// <param name="context"></param>
        /// <seealso cref="JobExecutionVetoed(IJobExecutionContext)"/>
        public void JobToBeExecuted( IJobExecutionContext context )
        {
        }

        /// <summary>
        /// Called by the <see cref="IScheduler"/> when a <see cref="IJobDetail"/>
        /// was about to be executed (an associated <see cref="ITrigger"/>
        /// has occurred), but a <see cref="ITriggerListener"/> vetoed it's
        /// execution.
        /// </summary>
        /// <param name="context"></param>
        /// <seealso cref="JobToBeExecuted(IJobExecutionContext)"/>
        public void JobExecutionVetoed( IJobExecutionContext context )
        {
        }

        private void ProcessNotificationMessage( IJobExecutionContext context, ServiceJob job, IRockJobResult resultInfo )
        {
            bool sendMessage = ( job.NotificationStatus == JobNotificationStatus.All );

            var result = resultInfo.Result.GetValueOrDefault( RockJobResultSpecifier.Succeeded );

            if ( result == RockJobResultSpecifier.Failed )
            {
                if ( job.NotificationStatus == JobNotificationStatus.Error )
                {
                    sendMessage = true;
                }
            }

            if ( !sendMessage )
                return;

            // Create a notification message.
            StringBuilder message = new StringBuilder();

            message.Append( string.Format( "The job {0} ran for {1} seconds on {2}.  Below is the results:<p>", job.Name, context.JobRunTime.TotalSeconds,
                                         context.FireTimeUtc.Value.DateTime.ToLocalTime() ) );

            string resultStatusDescription;

            switch ( resultInfo.Result.GetValueOrDefault( RockJobResultSpecifier.Failed ) )
            {
                case RockJobResultSpecifier.Succeeded:
                    resultStatusDescription = "Succeeded";
                    break;
                case RockJobResultSpecifier.CompletedWithWarnings:
                    resultStatusDescription = "Completed with Warnings";
                    break;
                case RockJobResultSpecifier.Failed:
                default:
                    resultStatusDescription = "Failed";
                    break;
            }

            message.Append( "<p>Result:<br>" + resultStatusDescription );

            message.Append( "<p>Message:<br>" + job.LastStatusMessage );

            if ( result == RockJobResultSpecifier.Failed )
            {
                message.Append( "<p>Inner Exception:<br>" + resultInfo.ResultDetails );
            }

            this.SendNotification( message.ToString() );
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void SendNotification( string message )
        {
            try
            {
                // setup merge codes for email
                var mergeObjects = GlobalAttributesCache.GetMergeFields( null );

                mergeObjects.Add( "ExceptionDetails", message );

                // get email addresses to send to
                var globalAttributesCache = GlobalAttributesCache.Read();

                string emailAddressesList = globalAttributesCache.GetValue( "EmailExceptionsList" );

                if ( !string.IsNullOrWhiteSpace( emailAddressesList ) )
                {
                    string[] emailAddresses = emailAddressesList.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

                    var recipients = new List<RecipientData>();
                    foreach ( string emailAddress in emailAddresses )
                    {
                        recipients.Add( new RecipientData( emailAddress, mergeObjects ) );
                    }

                    if ( recipients.Any() )
                    {
                        bool sendNotification = true;

                        Email.Send( Rock.SystemGuid.SystemEmail.CONFIG_EXCEPTION_NOTIFICATION.AsGuid(), recipients );
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Called by the <see cref="IScheduler"/> after a <see cref="IJobDetail"/>
        /// has been executed, and before the associated <see cref="Quartz.Spi.IOperableTrigger"/>'s
        /// <see cref="Quartz.Spi.IOperableTrigger.Triggered"/> method has been called.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="jobException"></param>
        public void JobWasExecuted( IJobExecutionContext context, JobExecutionException jobException )
        {
            RockJobResultSpecifier result;
            IRockJobResult resultInfo;
            Exception exceptionToLog = null;
            ServiceJob job = null;

            // If the Job threw an Exception, create a corresponding RockJobResult object and find the appropriate Exception to log.
            if ( jobException != null )
            {
                exceptionToLog = jobException;

                resultInfo = new RockJobResult();

                resultInfo.Result = RockJobResultSpecifier.Failed;

                // Unpack the Scheduler Exceptions to get the Exception thrown by the Task itself.
                while ( exceptionToLog is SchedulerException
                       && exceptionToLog.InnerException != null )
                {
                    exceptionToLog = exceptionToLog.InnerException;
                }

                var summaryException = exceptionToLog;

                if ( summaryException is AggregateException )
                {
                    var aggregateException = (AggregateException)summaryException;

                    if ( aggregateException.InnerExceptions != null )
                    {
                        if ( aggregateException.InnerExceptions.Count == 1 )
                        {
                            // if it's an aggregate, but there is only one, convert it to a single exception
                            summaryException = aggregateException.InnerExceptions[0];
                        }
                        else
                        {
                            summaryException = aggregateException.Flatten();
                        }
                    }
                }

                resultInfo.ResultDescription = summaryException.Message;

                var ex = summaryException.InnerException;

                string details = string.Empty;;

                while ( ex != null )
                {
                    details += "\n--> " + ex.Message;

                    ex = ex.InnerException;
                }

                resultInfo.ResultDetails = details.Trim('\n');
            }
            else
            {
                resultInfo = context.Result as IRockJobResult;

                // If the Job did not return a result object and did not throw an Exception, assume success.
                if ( resultInfo == null )
                {
                    resultInfo = new RockJobResult();
                    resultInfo.Result = RockJobResultSpecifier.Succeeded;
                }
                else
                {
                    // If the Job returned a failure in the result object, create a corresponding Exception for logging purposes.
                    if ( resultInfo.Result.HasValue
                        && resultInfo.Result.Value == RockJobResultSpecifier.Failed )
                    {
                        exceptionToLog = new Exception( resultInfo.ResultDescription );
                    }
                }
            }

            // Update the Job with the most recent result.
            result = resultInfo.Result.GetValueOrDefault( RockJobResultSpecifier.Succeeded );

            // Retrieve the Job details.
            int jobId = Convert.ToInt16( context.JobDetail.Description );

            using ( var rockContext = new RockContext() )
            {
                var jobService = new ServiceJobService( rockContext );

                job = jobService.Get( jobId );

                // set last run date
                job.LastRunDateTime = RockDateTime.Now;

                // set run time
                job.LastRunDurationSeconds = Convert.ToInt32( context.JobRunTime.TotalSeconds );

                // set the scheduler name
                job.LastRunSchedulerName = context.Scheduler.SchedulerName;
                switch ( result )
                {
                    case RockJobResultSpecifier.Succeeded:
                        job.LastStatus = "Success";
                        job.LastSuccessfulRunDateTime = job.LastRunDateTime;
                        break;
                    case RockJobResultSpecifier.CompletedWithWarnings:
                        job.LastStatus = "Warning";
                        job.LastSuccessfulRunDateTime = job.LastRunDateTime;
                        break;
                    case RockJobResultSpecifier.Failed:
                        job.LastStatus = "Exception";
                        break;
                }

                job.LastStatusMessage = resultInfo.ResultDescription;

                if ( !string.IsNullOrEmpty( resultInfo.ResultDetails ) )
                {
                    job.LastStatusMessage += "\n" + resultInfo.ResultDetails;
                }

                // Save changes to the Job.
                rockContext.SaveChanges();
            }

            if ( result == RockJobResultSpecifier.Failed )
            {
                // log the exception to the database
                ExceptionLogService.LogException( exceptionToLog, null );
            }

            this.ProcessNotificationMessage( context, job, resultInfo );
        }
    }
}