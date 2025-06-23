using Hestia.LocationsMDM.WebApi.Common.Constants;
using Microsoft.AspNetCore.Server.HttpSys;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MDM.Tools.Common.ValueCalculators
{
    // NOTE: In future this can be moved to shared package that will be used by both MDM backend and MDM Tools
    public class BranchPageUrlCalculator
    {
        private static readonly IDictionary<string, string> KobMapping = new Dictionary<string, string>
        {
            [KOBs.Plumbing] = "plumbing-pvf",
            [KOBs.HVAC] = "hvac",
            [KOBs.MechanicalIndustrial] = "mechanical-industrial",
            [KOBs.Showroom] = "showroom",
            [KOBs.SelectionCenter] = "selection-center",
            [KOBs.FireAndFabrication] = "fire-and-fabrication",
            [KOBs.Waterworks] = "waterworks"
        };

        private static readonly IDictionary<string, string> UrlSuffixKobMapping = new Dictionary<string, string>
        {
            [KOBs.Plumbing] = "plumbing",
            [KOBs.HVAC] = "hvac",
            [KOBs.MechanicalIndustrial] = "industrial",
            [KOBs.Showroom] = "showroom",
            [KOBs.SelectionCenter] = "selection-center",
            [KOBs.FireAndFabrication] = "fire-fabrication",
            [KOBs.Waterworks] = "waterworks"
        };

        public static string Calculate(JObject childLoc)
        {
            #region Rules
            // These only need to be applied to customer-facing location types: Counter, Sales office, and Showrooms.
            // There can't be more than one particular KOB per campus.
            // These will be applied to the "Branch Page URL" attribute.
            // Formula: [Modified City]- [Modified State]-[Modified KOB]-[Cost Center]
            // Modified City = LOWER(SUBSTITUTE(E2," ","-"))
            //   i.e. chantilly or fort-walton-beach
            // Modified City = LOWER(E2)
            //   i.e. co or tx
            // Modified KOB
            // * KOB => Modified KOB
            // * Plumbing/PVF => plumbing-pvf
            // * HVAC => hvac
            // * Mechanical/Industrial => mechanical-industrial
            // * Showroom => showroom
            // * Selection Center => selection-center
            // * Fire & Fabrication => fire-and-fabrication
            // * Waterworks => waterworks
            // Cost Center = Cost Center ID
            // Example: 
            //   chantilly-va-plumbing-pvf-0001
            #endregion

            var address = childLoc.Address();
            string city = address.City();
            string state = address.State().ToLowerInvariant();
            string kob = childLoc.KOB();

            //string cityVal = city.Replace(' ', '-').ToLowerInvariant();
            string stateVal = state.ToLowerInvariant();
            string kobVal = GetKobMapping(kob);
            string costCenterId = GetCostCenterId(childLoc);

            var filteredChars = new List<char>();
            foreach (char c in city)
            {
                if (char.IsLetterOrDigit(c))
                {
                    filteredChars.Add(c);
                }
                else
                {
                    // add '-' in case previous char is alpha-numeric
                    if (filteredChars.Count > 0)
                    {
                        // check last char
                        bool isAlphNum = char.IsLetterOrDigit(filteredChars[filteredChars.Count - 1]);
                        if (isAlphNum)
                        {
                            filteredChars.Add('-');
                        }
                    }
                }
            }

            city = new string(filteredChars.ToArray());
            string branchUrl = $"{city}-{stateVal}-{kobVal}-{costCenterId}";
            return branchUrl;
        }

        public static string CalculateUrlPrefix(JObject childLoc)
        {
            #region Rules
            // [city]-[state] (i.e. richmond-va)
            // If the city contains multiple words, there must be a dash between each word.
            // Example: if we use 'Newport News, VA', then the URL Prefix will be "newport-news-va".
            // All lower case.
            // Cannot contain spaces.
            #endregion

            var address = childLoc.Address();
            string city = address.City().ToLowerInvariant();
            string state = address.State().ToLowerInvariant();
            string urlPrefix = $"{city}-{state}";

            var filteredChars = new List<char>();
            foreach (char c in urlPrefix)
            {
                if (char.IsLetterOrDigit(c))
                {
                    filteredChars.Add(c);
                }
                else
                {
                    // add '-' in case previous char is alpha-numeric
                    if (filteredChars.Count > 0)
                    {
                        // check last char
                        bool isAlphNum = char.IsLetterOrDigit(filteredChars[filteredChars.Count - 1]);
                        if (isAlphNum)
                        {
                            filteredChars.Add('-');
                        }
                    }
                }
            }

            var cleanUrlPrefix = new string(filteredChars.ToArray());
            return cleanUrlPrefix;
        }

        public static string CalculateUrlSuffix(JObject childLoc)
        {
            string kob = childLoc.KOB();
            var suffix = GetUrlSuffixKobMapping(kob);
            return suffix;
        }

        private static string GetKobMapping(string kob)
        {
            if (kob == null)
            {
                return null;
            }

            if (!KobMapping.TryGetValue(kob, out string value))
            {
                throw new NotSupportedException($"KOB '{kob}' is not supported.");
            }

            return value;
        }

        private static string GetUrlSuffixKobMapping(string kob)
        {
            if (kob == null)
            {
                return null;
            }

            if (!UrlSuffixKobMapping.TryGetValue(kob, out string value))
            {
                throw new NotSupportedException($"KOB '{kob}' is not supported for URL Suffix.");
            }

            return value;
        }

        private static string GetCostCenterId(JObject childLoc)
        {
            var finDataArr = childLoc.FinancialData();
            if (finDataArr == null || finDataArr.Count == 0)
            {
                return null;
            }

            var result = finDataArr[0].Value<string>("costCenterId");
            return result;
        }
    }
}
