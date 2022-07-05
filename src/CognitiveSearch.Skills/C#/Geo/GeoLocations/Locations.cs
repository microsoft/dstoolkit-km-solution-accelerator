// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Geo.Skills.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Services.Common.WebApiSkills;
using Newtonsoft.Json;

namespace Geo.Skills
{
    public static class Locations
    {
        // Data Credits https://simplemaps.com/data/world-cities 

        public static List<Country> AllCountries = null ;

        public static List<City> AllCities = null ;

        [FunctionName("locations")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext ctx)
        {

            if (AllCountries == null)
            {
                var filePath = Path.Combine(ctx.FunctionAppDirectory, "Countries.json");
                AllCountries = JsonConvert.DeserializeObject<List<Country>>(File.ReadAllText(filePath));

                filePath = Path.Combine(ctx.FunctionAppDirectory, "WorldCities.json");
                AllCities = JsonConvert.DeserializeObject<List<City>>(File.ReadAllText(filePath));
            }

            log.LogInformation("C# HTTP trigger function processed a request.");

            string recordId = null;
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            if (requestBody.Length <= 0)
            {
                return new BadRequestObjectResult(" Could not find values array");
            }

            //log.LogInformation(requestBody);

            WebApiEnricherResponse data = JsonConvert.DeserializeObject<WebApiEnricherResponse>(requestBody);

            WebApiEnricherResponse response = new WebApiEnricherResponse();

            response.Values = new List<WebApiResponseRecord>();

            foreach (var record in data?.Values)
            {
                try
                {
                    recordId = record.RecordId as string;

                    // Put together response.
                    WebApiResponseRecord responseRecord = new WebApiResponseRecord
                    {
                        Data = new Dictionary<string, object>(),
                        RecordId = recordId
                    };

                    SortedSet<string> countries = new SortedSet<string>();
                    SortedSet<string> capitals = new SortedSet<string>();
                    SortedSet<string> cities = new SortedSet<string>();
                    SortedSet<string> locations = new SortedSet<string>();

                    foreach (var item in record.Data.Keys)
                    {
                        if (record.Data[item] != null)
                        {
                            // Usually the locations entities are given here
                            dynamic list = record.Data[item];

                            if (list?.HasValues == true)
                            {
                                int countItems = 0; 
                                foreach (dynamic iloc in list)
                                {
                                    string lckuploc = ((string)iloc).ToUpperInvariant();

                                    countItems++; 
                                    // Countries
                                    Country countryf = AllCountries.FirstOrDefault(c => c.Name.ToUpperInvariant() == lckuploc);
                                    if (countryf != null)
                                    {
                                        countries.Add(countryf.Name);
                                    }
                                    else
                                    {
                                        // Cities / Capitals (+ Countries)
                                        City found = AllCities.FirstOrDefault(c => c.city.ToUpperInvariant() == lckuploc);
                                        if (found != null)
                                        {
                                            if (found.capital.Equals("primary"))
                                            {
                                                capitals.Add(found.city);
                                            }
                                            else
                                            {
                                                cities.Add(found.city);
                                            }

                                            if (!found.ambiguous)
                                            {
                                                countries.Add(found.country);
                                            }
                                        }
                                        else
                                        {
                                            locations.Add((string)iloc);
                                        }
                                    }
                                }

                                log.LogInformation(($"Processed {item} entity with {countItems} items."));
                            }
                        }
                    }

                    responseRecord.Data.Add("countries", countries);
                    responseRecord.Data.Add("capitals", capitals);
                    responseRecord.Data.Add("cities", cities);
                    responseRecord.Data.Add("locations", locations);

                    response.Values.Add(responseRecord);

                }
                catch (Exception ex)
                {
                    log.LogInformation(ex.StackTrace);

                    WebApiResponseRecord responseRecord = new()
                    {
                        Data = new Dictionary<string, object>(),
                        RecordId = recordId
                    };

                    log.LogInformation(JsonConvert.SerializeObject(responseRecord.Data));

                    response.Values.Add(responseRecord);
                }
            }
            // End Loop 

            return new OkObjectResult(response);
        }
    }
}
