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
using System.Linq;
using Quartz;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{

    /// <summary>
    /// Job to process communications
    /// </summary>
    [IntegerField( "Delay Period", "The number of minutes to wait before sending any new communication (If the communication block's 'Send When Approved' option is turned on, then a delay should be used here to prevent a send overlap).", false, 30, "", 0 )]
    [IntegerField( "Expiration Period", "The number of days after a communication was created or scheduled to be sent when it should no longer be sent.", false, 3, "", 1 )]
    [DisallowConcurrentExecution]
    public class SendCommunications : IJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommunications"/> class.
        /// </summary>
        public SendCommunications()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var beginWindow = RockDateTime.Now.AddDays( 0 - dataMap.GetInt( "ExpirationPeriod" ));
            var endWindow = RockDateTime.Now.AddMinutes( 0 - dataMap.GetInt( "DelayPeriod" ));

            var communicationQuery = new CommunicationService(new RockContext()).Queryable()
                                                                                .Where(c =>
                                                                                       c.Status == CommunicationStatus.Approved &&
                                                                                       c.Recipients.Any( r => r.Status == CommunicationRecipientStatus.Pending ) &&
                                                                                       (
                                                                                           (!c.FutureSendDateTime.HasValue && c.CreatedDateTime.HasValue
                                                                                            && c.CreatedDateTime.Value.CompareTo(beginWindow) >= 0 && c.CreatedDateTime.Value.CompareTo(endWindow) <= 0)
                                                                                           ||
                                                                                           (c.FutureSendDateTime.HasValue && c.FutureSendDateTime.Value.CompareTo(beginWindow) >= 0
                                                                                            && c.FutureSendDateTime.Value.CompareTo(endWindow) <= 0)
                                                                                       ));
                       
            var pendingCount = communicationQuery.Count();

            var communications = communicationQuery.ToList();

            int processedCount = 0;
            int sendCount = 0;
            string currentActivityDescription = "Initializing";

            try
            {
                foreach (var comm in communications)
                {
                    currentActivityDescription = string.Format( "Sending Communication '{0}' [Id={1}]", comm.ToString(), comm.Id );

                    var medium = comm.Medium;

                    if (medium != null)
                    {
                        medium.Send(comm);

                        sendCount++;
                    }

                    processedCount++;
                }

                pendingCount = pendingCount - processedCount;

                string resultDescription = string.Format("Send Communication processing completed.\n[{0} processed, {1} sent, {2} pending].",
                                                         processedCount,
                                                         sendCount,
                                                         pendingCount);

                // Set the Job result summary.
                context.Result = RockJobResult.NewSuccessResult(resultDescription);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Job Execution failed at Processing Task \"{0}\".", currentActivityDescription), ex);
            }
        }
    }
}