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
using System.Threading;
using Quartz;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    /// <summary>
    ///     Job that attempts to standardize and validate existing addresses using the currently active Location Services.
    /// </summary>
    [IntegerField("Max Records Per Run", "The maximum number of records to run per run.", true, 1000)]
    [IntegerField("Throttle Period", "The number of milliseconds to wait between records. This helps to throttle requests to the lookup services.", true, 500)]
    [IntegerField("Retry Period", "The number of days to wait before retrying a previously unsuccessful address lookup.", true, 200)]
    [DisallowConcurrentExecution]
    public class LocationServicesVerify : IJob
    {
        public virtual void Execute(IJobExecutionContext context)
        {
            // Check that we have at least one active Location Service.
            bool hasActiveServices = Rock.Address.VerificationContainer.Instance.Components.Any(x => x.Value.Value.IsActive);

            if (!hasActiveServices)
            {
                context.Result = RockJobResult.NewWarningResult("Address Verification canceled. There are no Active Location Services available to process the request.");
                return;
            }

            // Get the Job configuration settings.
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int maxRecords = Int32.Parse( dataMap.GetString( "MaxRecordsPerRun" ) );
            int throttlePeriod = Int32.Parse( dataMap.GetString( "ThrottlePeriod" ) );
            int retryPeriod = Int32.Parse( dataMap.GetString( "RetryPeriod" ) );

            DateTime retryDate = DateTime.Now.Subtract( new TimeSpan( retryPeriod, 0, 0, 0 ) );

            using (var rockContext = new RockContext())
            {
                var locationService = new LocationService(rockContext);

                // Get a set of Locations to process that:
                // 1. Are Active, Not Locked, and Not a Geofence.
                // 2. Have Not been previously geocoded or standardized, or are due for a retry.
                // 3. Are in order of modification date, so that the most recently updated addresses are given priority.
                var locationQuery = locationService.Queryable()
                                               .Where(l => (
                                                               (l.IsGeoPointLocked == null || l.IsGeoPointLocked == false)
                                                               && l.IsActive
                                                               && l.GeoFence == null
                                                               && (l.GeocodedDateTime == null && (l.GeocodeAttemptedDateTime == null || l.GeocodeAttemptedDateTime < retryDate))
                                                               && (l.StandardizedDateTime == null && (l.StandardizeAttemptedDateTime == null || l.StandardizeAttemptedDateTime < retryDate))
                                                           ))
                                               .OrderByDescending(x => x.ModifiedDateTime);

                var pendingCount = locationQuery.Count();

                var addresses = locationQuery.Take(maxRecords).ToList();

                int processedCount = 0;
                int verifiedCount = 0;
                string currentActivityDescription = "Initializing";

                try
                {
                    foreach (var address in addresses)
                    {
                        currentActivityDescription = string.Format("Verifying Location '{0}' [Id={1}]", address, address.Id);

                        DateTime? lastGeocoded = address.GeocodedDateTime;
                        DateTime? lastStandardized = address.StandardizedDateTime;

                        locationService.Verify(address, false); // currently not reverifying 

                        rockContext.SaveChanges();

                        if ((address.GeocodedDateTime.HasValue && address.GeocodedDateTime.Value.Subtract(lastGeocoded.GetValueOrDefault(DateTime.MinValue)).TotalSeconds > 0)
                            || (address.StandardizedDateTime.HasValue && address.StandardizedDateTime.Value.Subtract(lastStandardized.GetValueOrDefault(DateTime.MinValue)).TotalSeconds > 0))
                        {
                            verifiedCount++;
                        }

                        processedCount++;

                        Thread.Sleep(throttlePeriod);
                    }

                    string resultDescription = string.Format("Job completed.\n[{0} processed, {1} verified, {2} failed, {3} pending].",
                                                             processedCount,
                                                             verifiedCount,
                                                             processedCount - verifiedCount,
                                                             pendingCount - processedCount );

                    // Set the Job result summary.
                    context.Result = RockJobResult.NewSuccessResult(resultDescription);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Job failed at Processing Task \"{0}\".", currentActivityDescription), ex);
                }
            }
        }
    }
}